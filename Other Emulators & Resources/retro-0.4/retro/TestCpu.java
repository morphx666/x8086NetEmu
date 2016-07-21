/*
 *  TestCpu.java
 *  Joris van Rantwijk
 *
 *  Load a code fragment and run it through the CPU emulation.
 *  Dump the CPU state after each instruction.
 */

package retro;
import java.io.*;

public class TestCpu
{

	static Cpu cpu;
	static Memory mem;
	static IOPorts io;
	static OutputStream dumpf;

	// Setup emulator and load code fragment
	public static void setupvm(String codefile)
	  throws IOException
	{
		
		mem = new Memory();
		io = new IOPorts();
		cpu = new Cpu(null, mem, io);

		// Read code fragment at f000:0000
		FileInputStream fin = new FileInputStream(codefile);
		byte[] buf = new byte[65536];
		int i = fin.read(buf);
		mem.loadData(0xf0000, buf);
		fin.close();
		System.out.println("Loaded " + i + " bytes from " + codefile);

		// Copy start of code fragment to f000:fff0
		if (i < 0xfff0) {
			if (i > 16) i = 16;
			byte startcode[] = new byte[i];
			System.arraycopy(buf, 0, startcode, 0, i);
			mem.loadData(0xffff0, startcode);
			System.out.println("Copied first " + i + " bytes to f000:fff0");
		}
	}

	// Run the code until it stops or fails
	static void runvm()
	  throws IOException
	{
		int count = 0;

		try {

			while (true) {
				cpu.execNext();
				byte[] cpustate = cpu.getStateData();
				dumpf.write(cpustate);
				count++;
				if (cpustate[16] == 0 && cpustate[17] == 0 &&
				    cpustate[18] == 0 && cpustate[19] == 0) {
					/* CS:IP got to 0000:0000, probably due to INT 3 */
					break;
				}
			}

		} catch (Cpu.InvalidOpcodeException e) {
			dumpf.write(cpu.getStateData());
			dumpf.flush();
			e.printStackTrace();
			System.out.println(cpu.getStateString());
			System.exit(1);
		}

		System.out.println("Executed " + count + " instructions");
		System.out.println(cpu.getStateString());
	}

	public static void main(String[] args)
	{
		if (args.length != 2) {
			System.err.println("Usage: java TestCpu <codefile> <outfile>");
			System.exit(1);
		}

		String codefile = args[0];
		String dumpfile = args[1];

		try {
			setupvm(codefile);
			
			FileOutputStream outf = new FileOutputStream(dumpfile);
			dumpf = new BufferedOutputStream(outf);
			runvm();
			dumpf.close();

		} catch (IOException e) {
			e.printStackTrace();
			System.exit(1);
		}
		
	}

}

/* end */

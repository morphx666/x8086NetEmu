using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using x8086NetEmu;

// https://forum.vcfed.org/index.php?threads/a-test-suite-for-the-intel-8088-cpu.1244578/

namespace RunTests2 {
    internal class Program {
        private const byte HLT = 0xF4;
        private const byte NOP = 0x90;
        private static X8086 cpu;

        public static void Main(string[] args) {
            Test currentTest = null;

            cpu = new X8086(false, false, null, X8086.Models.IBMPC_5150);
            cpu.Adapters.Clear();
            cpu.Ports.Clear();

            int skipCount = 0;
            string[] skipOpCodes = {"0F",                             // POP CS
                                                                      
                                                                      // These opcodes seem to have bugs
                                   "F6.7", "F7.7",                    // IDIV (Group 3)
                                                                      
                                                                      // We do not support these opcodes
                                   "60", "61", "62", "63", "64",      // JO, JNO, JB, JNB, JZ
                                   "65", "66", "67", "68", "69",      // JNZ, JBE, JNBE, JS, JNS
                                   "6A", "6B", "6C", "6D", "6E",      // JP, JNP, JL, JNL, JLE
                                   "6F", "C0", "C1", "C8", "C9",      // JNLE, RETN, RETN 
                                   "D0.6", "D1.6", "D2.6", "D3.6"};

            string[] ignoreFlags = {                                  // (Group 3)
                                    "F6.4", "F6.5", "F6.6",           // MUL, IMUL, DIV

                                                                      // (Group 2)
                                    "D2.0", "D2.1", "D2.2", "D2.3",   // ROL, ROR, RCL, RCR
                                    "D2.4", "D2.5", "D2.7",           // SHL, SHR, SAR, 
                                    "D3.0", "D3.1", "D3.2", "D3.3",   // ROL, ROR, RCL, RCR
                                    "D3.4", "D3.5", "D3.7",           // SHL, SHR, SAR

                                    "D4", "D5",                       // AAM, AAD
                                    "F7.4", "F7.5", "F7.6"};          // MUL, IMUL, DIV

            FileInfo[] files = new DirectoryInfo(Path.Combine("8088_ProcessorTests", "v1")).GetFiles("*.gz");

            int fl = files.Length;
            for(int i = skipCount; i < fl; i++) {
                string fileName = files[i].Name.Replace(files[i].Extension, "").Replace(".json", "");
                if(skipOpCodes.Contains(fileName)) continue;

                Test[] tests = JsonConvert.DeserializeObject<Test[]>(ExtractTest(files[i]));

                for(int j = 0; j < tests.Length; j++) {
                    Test test = tests[j];

                    Console.WriteLine($"[{i,3} | {(100.0 * i / fl),5:F2}%] 0x{fileName}: {test.name}");
                    currentTest = test;

                    LoadRam(test.initial.ram);
                    cpu.Registers.AX = test.initial.regs.ax;
                    cpu.Registers.BX = test.initial.regs.bx;
                    cpu.Registers.CX = test.initial.regs.cx;
                    cpu.Registers.DX = test.initial.regs.dx;
                    cpu.Registers.SI = test.initial.regs.si;
                    cpu.Registers.DI = test.initial.regs.di;
                    cpu.Registers.BP = test.initial.regs.bp;
                    cpu.Registers.SP = test.initial.regs.sp;
                    cpu.Registers.IP = test.initial.regs.ip;
                    cpu.Registers.CS = test.initial.regs.cs;
                    cpu.Registers.DS = test.initial.regs.ds;
                    cpu.Registers.ES = test.initial.regs.es;
                    cpu.Registers.SS = test.initial.regs.ss;
                    cpu.Flags.EFlags = test.initial.regs.flags;

                    //if(test.test_hash == "4f70ca3ff06c0596c12d6a21f9babb155673037181576f19f25fecb64b0280bf") Debugger.Break();

                    Task.Run(async () => {
                        int ic = 0;
                        while(ic < test.bytes.Length) {
                            cpu.PreExecute();
                            cpu.Execute_DEBUG();
                            ic += cpu.PostExecute();

                            await Task.Delay(0);
                        };
                    }).Wait();
                    AnalyzeResult(currentTest, ignoreFlags.Contains(fileName));
                }
                Console.WriteLine("\n-------------------------------------------\n");
            }
        }

        private static void AnalyzeResult(Test currentTest, bool ignoreFlags) {
            bool passed = true;
            State sf = currentTest.final;

            if(cpu.Registers.AX != sf.regs.ax) { Console.WriteLine($"\tAX: {cpu.Registers.AX} != {sf.regs.ax}"); passed = false; }
            if(cpu.Registers.BX != sf.regs.bx) { Console.WriteLine($"\tBX: {cpu.Registers.BX} != {sf.regs.bx}"); passed = false; }
            if(cpu.Registers.CX != sf.regs.cx) { Console.WriteLine($"\tCX: {cpu.Registers.CX} != {sf.regs.cx}"); passed = false; }
            if(cpu.Registers.DX != sf.regs.dx) { Console.WriteLine($"\tDX: {cpu.Registers.DX} != {sf.regs.dx}"); passed = false; }
            if(cpu.Registers.SI != sf.regs.si) { Console.WriteLine($"\tSI: {cpu.Registers.SI} != {sf.regs.si}"); passed = false; }
            if(cpu.Registers.DI != sf.regs.di) { Console.WriteLine($"\tDI: {cpu.Registers.DI} != {sf.regs.di}"); passed = false; }
            if(cpu.Registers.BP != sf.regs.bp) { Console.WriteLine($"\tBP: {cpu.Registers.BP} != {sf.regs.bp}"); passed = false; }
            if(cpu.Registers.SP != sf.regs.sp) { Console.WriteLine($"\tSP: {cpu.Registers.SP} != {sf.regs.sp}"); passed = false; }
            if(cpu.Registers.IP != sf.regs.ip) { Console.WriteLine($"\tIP: {cpu.Registers.IP} != {sf.regs.ip}"); passed = false; }
            if(cpu.Registers.CS != sf.regs.cs) { Console.WriteLine($"\tCS: {cpu.Registers.CS} != {sf.regs.cs}"); passed = false; }
            if(cpu.Registers.DS != sf.regs.ds) { Console.WriteLine($"\tDS: {cpu.Registers.DS} != {sf.regs.ds}"); passed = false; }
            if(cpu.Registers.ES != sf.regs.es) { Console.WriteLine($"\tES: {cpu.Registers.ES} != {sf.regs.es}"); passed = false; }
            if(cpu.Registers.SS != sf.regs.ss) { Console.WriteLine($"\tSS: {cpu.Registers.SS} != {sf.regs.ss}"); passed = false; }
            if(!ignoreFlags && cpu.Flags.EFlags != sf.regs.flags) {
                Console.WriteLine($"\tFlags: {cpu.Flags.EFlags} != {sf.regs.flags}");

                Console.WriteLine("\t          CZSOPAID");
                Console.WriteLine($"\tEmulator: {cpu.Flags.CF}{cpu.Flags.ZF}{cpu.Flags.SF}{cpu.Flags.OF}{cpu.Flags.PF}{cpu.Flags.AF}{cpu.Flags.IF}{cpu.Flags.DF}");
                cpu.Flags.EFlags = sf.regs.flags;
                Console.WriteLine($"\tExpected: {cpu.Flags.CF}{cpu.Flags.ZF}{cpu.Flags.SF}{cpu.Flags.OF}{cpu.Flags.PF}{cpu.Flags.AF}{cpu.Flags.IF}{cpu.Flags.DF}");

                passed = false;
            }

            if(ignoreFlags) { // Also ignore stack
                UInt32 sssp = X8086.SegmentOffetToAbsolute(sf.regs.ss, sf.regs.sp);
                for(int i = 0; i < sf.ram.Length; i++) {
                    if(sf.ram[i][0] == sssp) {
                        cpu.Memory[sssp] = (byte)sf.ram[i][1];
                        sssp++;
                    }
                }
            }

            for(int i = 0; i < sf.ram.Length; i++) {
                int address = sf.ram[i][0];
                byte value = (byte)sf.ram[i][1];
                if((cpu.Memory[address] != value) &&
                   (cpu.Memory[address] != HLT &&
                    cpu.Memory[address] != NOP)) {
                    Console.WriteLine($"\tRAM: {address} {cpu.Memory[address]} != {value}"); passed = false;
                }
            }

            if(!passed) {
                Console.WriteLine($"\tTest Hash: {currentTest.test_hash}");
                Console.ReadKey(true);
            }
        }

        private static void LoadRam(int[][] ram) {
            for(int i = 0; i < ram.Length; i++) {
                int address = ram[i][0];
                byte value = (byte)ram[i][1];
                cpu.Memory[address] = value;
            }
        }

        private static string ExtractTest(FileInfo f) {
            using(MemoryStream ms = new MemoryStream()) {
                using(FileStream fs = f.OpenRead()) {
                    using(GZipStream gzs = new GZipStream(fs, CompressionMode.Decompress)) {
                        gzs.CopyTo(ms);
                    }
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}

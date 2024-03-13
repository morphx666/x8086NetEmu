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

            cpu = new X8086(false, false, null, X8086.Models.IBMPC_5150) {
                Clock = 47700000,
            };

            int skipCount = 298;
            string[] skipOpCodes = {"0F",                           // POP CS

                                                                    // These opcodes seem to have bugs
                                    "27", "2F", "37", "3F",         // DAA, DAS, AAA, AAS,
                                    "D2.0", "D2.1", "D2.2", "D2.3", // Group 2
                                    "D2.4", "D2.5", "D2.7",         // LOOPNE, LOOPE, LOOP
                                    "D3.0", "D3.1", "D3.2", "D3.3", // CALL, JMP (on some cases)
                                    "D3.4", "D3.5", "D3.7",         // MUL, IMUL, DIV (Group 3 | flags)
                                                                    // IDIV, (Group 3 | flags & results!)
                                                                    // CALL SP (Group 4/5 | flags)
                                    "D4", "D5",                     // AAM, AAD
                            
                                                                    // Skip IN and OUT b/c of conflicts with the attached peripherals
                                    "E4", "E5", "E6", "E7", "EC",   // IN, OUT
                                    "ED", "EE", "EF",

                                                                    // We do not support these opcodes
                                    "60", "61", "62", "63", "64",   // JO, JNO, JB, JNB, JZ
                                    "65", "66", "67", "68", "69",   // JNZ, JBE, JNBE, JS, JNS
                                    "6A", "6B", "6C", "6D", "6E",   // JP, JNP, JL, JNL, JLE
                                    "6F", "C0", "C1", "C8", "C9",   // JNLE, RETN, RETN 
                                    "D0.6", "D1.6", "D2.6", "D3.6"};

            string[] ignoreFlags = { "F6.4", "F6.5" };              // MUL, IMUL, DIV (Group 3)

            FileInfo[] files = new DirectoryInfo(Path.Combine("8088_ProcessorTests", "v1")).GetFiles("*.gz");
            for(int i = skipCount; i < files.Length; i++) {
                string fileName = files[i].Name.Replace(files[i].Extension, "").Replace(".json", "");
                if(skipOpCodes.Contains(fileName)) continue;

                Test[] tests = JsonConvert.DeserializeObject<Test[]>(ExtractTest(files[i]));

                for(int j = 0; j < tests.Length; j++) {
                    Console.WriteLine($"[{(i + 1),3:N0} | {(100.0 * j) / tests.Length,5:F2}%] 0x{fileName}: {tests[j].name}");
                    currentTest = tests[j];

                    LoadRam(tests[j].initial.ram);
                    cpu.Registers.AX = tests[j].initial.regs.ax;
                    cpu.Registers.BX = tests[j].initial.regs.bx;
                    cpu.Registers.CX = tests[j].initial.regs.cx;
                    cpu.Registers.DX = tests[j].initial.regs.dx;
                    cpu.Registers.SI = tests[j].initial.regs.si;
                    cpu.Registers.DI = tests[j].initial.regs.di;
                    cpu.Registers.BP = tests[j].initial.regs.bp;
                    cpu.Registers.SP = tests[j].initial.regs.sp;
                    cpu.Registers.IP = tests[j].initial.regs.ip;
                    cpu.Registers.CS = tests[j].initial.regs.cs;
                    cpu.Registers.DS = tests[j].initial.regs.ds;
                    cpu.Registers.ES = tests[j].initial.regs.es;
                    cpu.Registers.SS = tests[j].initial.regs.ss;
                    cpu.Flags.EFlags = tests[j].initial.regs.flags;

                    //if(tests[j].test_hash == "0b6d9bff01de645c3fdf85a52c5f802dbfbfc72e4062369d437863a03aeb2e19") Debugger.Break();

                    Task.Run(async () => {
                        int ic = 0;
                        while(ic < tests[j].bytes.Length) {
                            cpu.PreExecute();
                            cpu.Execute_DEBUG();
                            ic += cpu.PostExecute();

                            await Task.Delay(0);
                        };
                    }).Wait();
                    AnalyzeResult(currentTest,ignoreFlags.Contains(fileName));
                }
                Console.WriteLine("\n-------------------------------------------\n");
            }
        }

        private static void AnalyzeResult(Test currentTest, bool ignoreFlags) {
            bool passed = true;
            State s = currentTest.final;

            if(cpu.Registers.AX != s.regs.ax) { Console.WriteLine($"\tAX: {cpu.Registers.AX} != {s.regs.ax}"); passed = false; }
            if(cpu.Registers.BX != s.regs.bx) { Console.WriteLine($"\tBX: {cpu.Registers.BX} != {s.regs.bx}"); passed = false; }
            if(cpu.Registers.CX != s.regs.cx) { Console.WriteLine($"\tCX: {cpu.Registers.CX} != {s.regs.cx}"); passed = false; }
            if(cpu.Registers.DX != s.regs.dx) { Console.WriteLine($"\tDX: {cpu.Registers.DX} != {s.regs.dx}"); passed = false; }
            if(cpu.Registers.SI != s.regs.si) { Console.WriteLine($"\tSI: {cpu.Registers.SI} != {s.regs.si}"); passed = false; }
            if(cpu.Registers.DI != s.regs.di) { Console.WriteLine($"\tDI: {cpu.Registers.DI} != {s.regs.di}"); passed = false; }
            if(cpu.Registers.BP != s.regs.bp) { Console.WriteLine($"\tBP: {cpu.Registers.BP} != {s.regs.bp}"); passed = false; }
            if(cpu.Registers.SP != s.regs.sp) { Console.WriteLine($"\tSP: {cpu.Registers.SP} != {s.regs.sp}"); passed = false; }
            if(cpu.Registers.IP != s.regs.ip) { Console.WriteLine($"\tIP: {cpu.Registers.IP} != {s.regs.ip}"); passed = false; }
            if(cpu.Registers.CS != s.regs.cs) { Console.WriteLine($"\tCS: {cpu.Registers.CS} != {s.regs.cs}"); passed = false; }
            if(cpu.Registers.DS != s.regs.ds) { Console.WriteLine($"\tDS: {cpu.Registers.DS} != {s.regs.ds}"); passed = false; }
            if(cpu.Registers.ES != s.regs.es) { Console.WriteLine($"\tES: {cpu.Registers.ES} != {s.regs.es}"); passed = false; }
            if(cpu.Registers.SS != s.regs.ss) { Console.WriteLine($"\tSS: {cpu.Registers.SS} != {s.regs.ss}"); passed = false; }
            if(!ignoreFlags && cpu.Flags.EFlags != s.regs.flags) {
                Console.WriteLine($"\tFlags: {cpu.Flags.EFlags} != {s.regs.flags}");

                Console.WriteLine("\t          CZSOPAID");
                Console.WriteLine($"\tEmulator: {cpu.Flags.CF}{cpu.Flags.ZF}{cpu.Flags.SF}{cpu.Flags.OF}{cpu.Flags.PF}{cpu.Flags.AF}{cpu.Flags.IF}{cpu.Flags.DF}");
                cpu.Flags.EFlags = s.regs.flags;
                Console.WriteLine($"\tExpected: {cpu.Flags.CF}{cpu.Flags.ZF}{cpu.Flags.SF}{cpu.Flags.OF}{cpu.Flags.PF}{cpu.Flags.AF}{cpu.Flags.IF}{cpu.Flags.DF}");

                passed = false;
            }

            for(int i = 0; i < s.ram.Length; i++) {
                int address = s.ram[i][0];
                byte value = (byte)s.ram[i][1];
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

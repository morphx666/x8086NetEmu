using Eto.Forms;
using Eto.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using x8086NetEmu;
using x8086NetEmuEto.Renderers;

// http://pages.picoe.ca/docs/api/html/R_Project_EtoForms.htm

namespace x8086NetEmuEto {
    public class MainForm : Form {
        private X8086 cpu;
        protected Drawable Canvas;
        private readonly string basePath = @"";
        private string runningApp = "";

        public MainForm() {
            JsonReader.Load(this);

            if(Platform.IsMac) basePath = @"..\..\..\";

            BuildEmulatorMenu();
            StartEmulation();
        }

        private void StartEmulation() {
            cpu = new X8086(true, true, null, X8086.Models.IBMPC_5160, basePath);

            cpu.Adapters.Add(new FloppyControllerAdapter(cpu));

            cpu.Adapters.Add(new CGAEtoForms(cpu, Canvas, VideoAdapter.FontSources.BitmapFile, "asciivga.dat"));

            cpu.Adapters.Add(new KeyboardAdapter(cpu));
            cpu.Adapters.Add(new MouseAdapter(cpu));

            cpu.Adapters.Add(new SpeakerAdapter(cpu));
            AdlibAdapter adlib = new(cpu);
            cpu.Adapters.Add(adlib);
            cpu.Adapters.Add(new SoundBlaster(cpu, adlib));

            SetupCpuEventHandlers();
            LoadSettings();

            cpu.Run();

            AddCustomHooks();
        }

        private void LoadSettings() {
            XDocument xml = XDocument.Load(X8086.FixPath("settings.dat"));
            XElement settings = xml.Element("settings");

            cpu.SimulationMultiplier = int.Parse(settings.Element("simulationMultiplier").Value);
            cpu.Clock = double.Parse(settings.Element("clockSpeed").Value);
            cpu.VideoAdapter.Zoom = double.Parse(settings.Element("videoZoom").Value);

            XElement floppies = settings.Element("floppies");
            foreach(XElement floppy in floppies.Elements("floppy")) {
                int index = int.Parse(floppy.Element("index").Value);
                string image = floppy.Element("image").Value;
                bool readOnly = bool.Parse(floppy.Element("readOnly").Value);

                cpu.FloppyContoller.set_DiskImage(index, new DiskImage(image, readOnly, false));
            }

            XElement disks = settings.Element("disks");
            foreach(XElement disk in disks.Elements("disk")) {
                int index = int.Parse(disk.Element("index").Value);
                string image = disk.Element("image").Value;
                bool readOnly = bool.Parse(disk.Element("readOnly").Value);

                cpu.FloppyContoller.set_DiskImage(index, new DiskImage(image, readOnly, true));
            }
        }

        private void SetupCpuEventHandlers() {
            if(cpu.VideoAdapter != null) {
                cpu.VideoAdapter.KeyDown += (object s1, Adapter.XKeyEventArgs e1) => {
                    if(e1.Shift && e1.Alt && e1.KeyValue == (int)Adapter.XEventArgs.Keys.Home) {
                        Application.Instance.Invoke(() => ContextMenu?.Show());
                        e1.Handled = true;
                    }
                };
            }

            cpu.MIPsUpdated += () => SetTitleText();
            //cpu.DebugModeChanged += () => Invoke(() => ShowDebugger());
        }

        private void SetTitleText() {
            string title = string.Format("x8086NetEmu [Menu: {0}]  {1:F2}MHz ● {2}% | {3} | {4:N2} MIPs | {5} {6}",
                            "Shift + Alt + Home",
                            cpu.Clock / X8086.MHz,
                            cpu.SimulationMultiplier * 100,
                            $"{cpu.VideoAdapter?.Name.Split(' ')[0]} Mode {cpu.VideoAdapter?.VideoMode:X2}{(cpu.VideoAdapter?.MainMode == VideoAdapter.MainModes.Text ? "T" : "G")} | Zoom {cpu.VideoAdapter?.Zoom * 100}%",
                            cpu.MIPs,
                            (cpu.IsHalted ? "Halted" : (cpu.DebugMode ? "Debugging" : (cpu.IsPaused ? "Paused" : "Running"))),
                            (runningApp != "" ? $" | {runningApp}" : ""));

            Application.Instance.Invoke(() => { this.Title = title; });
        }

        // Code demonstration on how to attach custom hooks
        // http://stanislavs.org/helppc/int_21.html
        private void AddCustomHooks() {
            cpu.TryAttachHook(0x19, () => {
                runningApp = "";
                return false;
            });

            cpu.TryAttachHook(0x20, () => {
                runningApp = "";
                return false;
            });

            cpu.TryAttachHook(0x21, () => {
                /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
                switch(cpu.Registers.AH) {
                    case 0x0:
                    case 0x4:
                    case 0x31: {
                        runningApp = "";
                        break;
                    }

                    case 0x4B: { // http://stanislavs.org/helppc/int_21-4b.html
                        string mode = "";

                        runningApp = GetInt21FunctionFileName(false);

                        switch(cpu.Registers.AL) {
                            case 0: {
                                mode = "L&X"; // Load & Execute
                                break;
                            }

                            case 1: {
                                mode = "LOD"; // Load
                                break;
                            }

                            case 2: {
                                mode = "UNK"; // Unknown
                                break;
                            }

                            case 3: {
                                mode = "LOO"; // Load Overlay
                                break;
                            }

                            case 4: {
                                mode = "LXB"; // Load & Execute in background
                                break;
                            }
                        }
                        X8086.Notify($"INT21:{cpu.Registers.AH} {mode}: {runningApp} -> {cpu.Registers.ES}:{cpu.Registers.BX}", X8086.NotificationReasons.Dbg);
                        break;
                    }
                }

                // Return False to notify the emulator that the interrupt was not handled.
                // Code execution will be transferred to the "native" interrupt handler.
                // Return True if you want to prevent the emulator from executing the code associated with this interrupt.
                // See INT13.vb for more information
                return false;
            });
        }

        private string GetInt21FunctionFileName(bool isFCB) {
            List<byte> b = [];
            UInt32 addr = X8086.SegmentOffsetToAbsolute(cpu.Registers.DS, cpu.Registers.DX);
            if(isFCB) {
                for(int i = 1; i <= 11; i++)
                    b.Add(cpu.Memory[addr + i]);
            } else
                while(cpu.Memory[addr] != 0) {
                    b.Add(cpu.Memory[addr]);
                    addr += 1;
                }
            return System.Text.Encoding.ASCII.GetString(b.ToArray());
        }

        private static IEnumerable<RadioMenuItem> CreateRadioMenuItems(params (string Text, bool Checked)[] items) {
            RadioMenuItem controller = null;

            foreach((string text, bool isChecked) in items) {
                RadioMenuItem item = controller == null ? new RadioMenuItem() : new RadioMenuItem(controller);
                item.Text = text;
                item.Checked = isChecked;

                controller ??= item;
                yield return item;
            }
        }

        private void BuildEmulatorMenu() {
            ContextMenu cm = new();

            ButtonMenuItem emulator = new() { Text = "Emulator" };
            {
                ButtonMenuItem cpuClock = new() { Text = "CPU Clock" };
                cpuClock.Items.AddRange(CreateRadioMenuItems(
                    ("4.77 MHz", true),
                    ("9.54 MHz", false),
                    ("19.08 MHz", false),
                    ("38.16 MHz", false),
                    ("47.70 MHz", false)));
                emulator.Items.Add(cpuClock);

                ButtonMenuItem emulationSpeed = new() { Text = "Emulation Speed" };
                emulationSpeed.Items.AddRange(CreateRadioMenuItems(
                    ("25%", false),
                    ("50%", false),
                    ("100%", true),
                    ("150%", false),
                    ("200%", false)));
                emulator.Items.Add(emulationSpeed);

                emulator.Items.Add(new SeparatorMenuItem());

                emulator.Items.Add(new CheckMenuItem() { Text = "Emulate Disk Access (INT13)", Checked = true });
                emulator.Items.Add(new CheckMenuItem() { Text = "V20 Emulation", Checked = true });

                emulator.Items.Add(new SeparatorMenuItem());

                emulator.Items.Add(new ButtonMenuItem() { Text = "Soft Reset (CTRL+ALT+INS)" });
                emulator.Items.Add(new ButtonMenuItem() { Text = "Hard Reset" });

                emulator.Items.Add(new SeparatorMenuItem());

                emulator.Items.Add(new ButtonMenuItem() { Text = "Load State..." });
                emulator.Items.Add(new ButtonMenuItem() { Text = "Save State..." });

                emulator.Items.Add(new SeparatorMenuItem());

                emulator.Items.Add(new ButtonMenuItem() { Text = "Exit", Command = new Command((s, e) => Application.Instance.Quit()) });
            }
            cm.Items.Add(emulator);

            cm.Items.Add(new ButtonMenuItem() { Text = "Media..." });

            ButtonMenuItem zoom = new() { Text = "Zoom" };
            {
                    zoom.Items.AddRange(CreateRadioMenuItems(
                        ("25%", false),
                        ("50%", false),
                        ("100%", true),
                        ("150%", false),
                        ("200%", false)));
            }
            cm.Items.Add(zoom);

            ButtonMenuItem tools = new() { Text = "Tools" };
            {
                tools.Items.Add(new ButtonMenuItem() { Text = "Debugger..." });
                tools.Items.Add(new ButtonMenuItem() { Text = "Console..." });

                tools.Items.Add(new SeparatorMenuItem());

                tools.Items.Add(new ButtonMenuItem() { Text = "Copy Text" });
                tools.Items.Add(new ButtonMenuItem() { Text = "Paste Text" });

                cm.Items.Add(tools);
            }

            this.ContextMenu = cm;
        }

    }
}
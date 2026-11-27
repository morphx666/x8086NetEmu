using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using x8086NetEmu;
using x8086NetEmuEto.Renderers;
using x8086NetEmuEto.Tools;

// http://pages.picoe.ca/docs/api/html/R_Project_EtoForms.htm

namespace x8086NetEmuEto {
    public class MainForm : Form {
        private X8086 cpu;
        protected Drawable Canvas;
        // Resolve ROMs, fonts, settings.dat and disk images relative to the executable's folder,
        // not the process working directory (which differs between Visual Studio, dotnet run,
        // shortcuts, etc.)
        private readonly string basePath = AppContext.BaseDirectory;
        private string runningApp = "";

        private DebuggerForm debugger;
        private ConsoleForm console;
        private bool isClosing;

        // Text selection (Copy Text)
        private ScreenText selectionText;
        private bool isSelecting;
        private bool isDraggingSelection;
        private bool wasPausedBeforeSelection;
        private Point selectionFrom;
        private Point selectionTo;

        public MainForm() {
            JsonReader.Load(this);

            // On macOS the executable lives inside the .app bundle (App.app/Contents/MacOS),
            // so the emulator's data folder is three levels up
            if(Platform.IsMac) basePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(basePath, "../../../"));

            Canvas.MouseDown += SelectionMouseDown;
            Canvas.MouseMove += SelectionMouseMove;
            Canvas.MouseUp += SelectionMouseUp;
            Canvas.KeyDown += SelectionKeyDown;

            StartEmulation();
            BuildEmulatorMenu(); // After StartEmulation so menu check states reflect the loaded settings
            RestoreToolWindows();
        }

        private void StartEmulation() {
            cpu = new X8086(true, true, RestartEmulation, X8086.Models.IBMPC_5160, basePath);

            cpu.Adapters.Add(new FloppyControllerAdapter(cpu));

            cpu.Adapters.Add(new CGAEtoForms(cpu, Canvas));

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

            // Drawn after (on top of) the video adapter's own Paint handler
            Canvas.Paint -= PaintSelection;
            Canvas.Paint += PaintSelection;
        }

        // Invoked by X8086.HardReset() once the old machine has been shut down: build a new one in its place
        private void RestartEmulation() {
            if(isClosing) return;
            X8086 old = cpu;

            Task.Run(async () => {
                await Task.Delay(500);
                Application.Instance.AsyncInvoke(() => {
                    if(isClosing) return;
                    EndTextSelection(false);
                    for(int i = 0; i < 512; i++) old.FloppyController?.get_DiskImage(i)?.Close();

                    runningApp = "";
                    StartEmulation();
                    BuildEmulatorMenu();
                    debugger?.Attach(cpu);
                    SetTitleText();
                });
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
            base.OnClosing(e);
            if(e.Cancel) return;

            isClosing = true;
            SaveExtras(("consoleVisible", console != null), ("debuggerVisible", debugger != null));
            EndTextSelection(false);
            debugger?.Close();
            console?.Close();

            try {
                cpu?.Close();
            } catch {
                // Shutting down anyway
            }
        }

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);

            // Ensure the render surface starts with keyboard focus so the Shift+Alt+Home
            // handler in EtoVideoInputController receives keys without requiring the pointer
            // to hover over the Canvas first.
            Canvas?.Focus();
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

                cpu.FloppyController.set_DiskImage(index, new DiskImage(image, readOnly, false));
            }

            XElement disks = settings.Element("disks");
            foreach(XElement disk in disks.Elements("disk")) {
                int index = int.Parse(disk.Element("index").Value);
                string image = disk.Element("image").Value;
                bool readOnly = bool.Parse(disk.Element("readOnly").Value);

                cpu.FloppyController.set_DiskImage(index, new DiskImage(image, readOnly, true));
            }
        }

        // Updates only the given top-level elements of settings.dat, preserving everything else
        // (floppies, disks, extras) exactly as it is on disk.
        private void SaveSettings(params (string Name, object Value)[] values) {
            string fileName = X8086.FixPath("settings.dat");

            try {
                XDocument xml = XDocument.Load(fileName);
                XElement settings = xml.Element("settings");

                foreach((string name, object value) in values) {
                    settings.SetElementValue(name, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                }

                xml.Save(fileName);
            } catch(Exception ex) {
                X8086.Notify($"Unable to save settings: {ex.Message}", X8086.NotificationReasons.Err);
            }
        }

        private void SetCpuClock(long hz) {
            cpu.Clock = hz;
            SaveSettings(("clockSpeed", hz));
            SetTitleText();
        }

        private void SetupCpuEventHandlers() {
            if(cpu.VideoAdapter != null) {
                cpu.VideoAdapter.KeyDown += (object s1, Adapter.XKeyEventArgs e1) => {
                    if(e1.Shift && e1.Alt && e1.KeyValue == (int)Adapter.XEventArgs.Keys.Home) {
                        ShowEmulatorMenu();
                        e1.Handled = true;
                    }
                };
            }

            cpu.MIPsUpdated += () => SetTitleText();

            // Something switched the CPU into debug mode (e.g. a breakpoint hook): bring up the debugger
            X8086 thisCpu = cpu;
            cpu.DebugModeChanged += (s, e) => Application.Instance.AsyncInvoke(() => {
                if(thisCpu == cpu && cpu.DebugMode && !isClosing) ShowDebugger();
            });

            if(cpu.VideoAdapter is IEtoVideoAdapter eva && eva.InputController != null) {
                eva.InputController.HotKeyPressed += key => {
                    if(key == Keys.C) BeginTextSelection();
                    else if(key == Keys.V) PasteText();
                };
            }
        }

        #region Settings
        private void SaveMediaSettings() {
            string fileName = X8086.FixPath("settings.dat");
            try {
                XDocument xml = XDocument.Load(fileName);
                XElement settings = xml.Element("settings");

                XElement floppies = new("floppies");
                XElement disks = new("disks");
                for(int i = 0; i < 512; i++) {
                    DiskImage di = cpu.FloppyController?.get_DiskImage(i);
                    if(di == null) continue;

                    string image = di.FileName;
                    string root = Path.GetFullPath(X8086.BasePath ?? basePath);
                    if(Path.GetFullPath(image).StartsWith(root, StringComparison.OrdinalIgnoreCase)) {
                        image = "./" + Path.GetRelativePath(root, image).Replace('\\', '/');
                    }

                    XElement entry = new(di.IsHardDisk ? "disk" : "floppy",
                        new XElement("letter", di.IsHardDisk ? (char)('C' + i - 128) : (char)('A' + i)),
                        new XElement("index", i),
                        new XElement("image", image),
                        new XElement("readOnly", di.IsReadOnly.ToString()));
                    (di.IsHardDisk ? disks : floppies).Add(entry);
                }

                settings.Element("floppies")?.Remove();
                settings.Element("disks")?.Remove();
                XElement anchor = settings.Element("videoZoom") ?? settings.Elements().LastOrDefault();
                if(anchor != null) anchor.AddAfterSelf(floppies, disks); else settings.Add(floppies, disks);

                xml.Save(fileName);
            } catch(Exception ex) {
                X8086.Notify($"Unable to save media settings: {ex.Message}", X8086.NotificationReasons.Err);
            }
        }

        // Updates elements under <extras>, preserving the rest of settings.dat
        private void SaveExtras(params (string Name, object Value)[] values) {
            string fileName = X8086.FixPath("settings.dat");
            try {
                XDocument xml = XDocument.Load(fileName);
                XElement settings = xml.Element("settings");
                XElement extras = settings.Element("extras");
                if(extras == null) settings.Add(extras = new XElement("extras"));

                foreach((string name, object value) in values) {
                    extras.SetElementValue(name, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant());
                }
                xml.Save(fileName);
            } catch(Exception ex) {
                X8086.Notify($"Unable to save settings: {ex.Message}", X8086.NotificationReasons.Err);
            }
        }

        private void RestoreToolWindows() {
            try {
                XElement extras = XDocument.Load(X8086.FixPath("settings.dat")).Element("settings")?.Element("extras");
                if(bool.TryParse(extras?.Element("consoleVisible")?.Value, out bool c) && c) ShowConsole();
                if(bool.TryParse(extras?.Element("debuggerVisible")?.Value, out bool d) && d) ShowDebugger();
            } catch {
                // No settings: nothing to restore
            }
        }
        #endregion

        #region Tools
        private void ShowDebugger() {
            if(debugger != null) {
                debugger.BringToFront();
                return;
            }

            debugger = new DebuggerForm(cpu);
            debugger.Closed += (s, e) => {
                debugger = null;
                SetTitleText();
            };
            debugger.Show();
        }

        private void ShowConsole() {
            if(console != null) {
                console.BringToFront();
                return;
            }

            console = new ConsoleForm();
            console.Closed += (s, e) => console = null;
            console.Show();
        }

        private void ShowMediaManager() {
            bool wasPaused = cpu.IsPaused;
            cpu.Pause();

            bool reboot = new MediaManagerDialog(cpu).ShowModal(this);
            SaveMediaSettings();

            if(reboot) {
                cpu.HardReset();
            } else if(!wasPaused) {
                cpu.Resume();
            }
        }

        private void HardReset() {
            EndTextSelection(false);
            runningApp = "";
            cpu.HardReset();
        }

        private void SoftReset() {
            runningApp = "";
            SetTitleText();
            cpu.SoftReset();
        }

        private void PasteText() {
            if(isSelecting || !Clipboard.Instance.ContainsText) return;
            TextPaster.Paste(cpu, Clipboard.Instance.Text);
            Canvas?.Focus();
        }
        #endregion

        #region Text selection (Copy Text)
        // Works in every video mode: see ScreenText. The machine is paused while selecting so the text doesn't
        // change under the selection; drag over the text to copy it, Esc or the right button cancels.
        private void BeginTextSelection() {
            if(isSelecting || cpu?.VideoAdapter is not IEtoVideoAdapter eva) return;

            wasPausedBeforeSelection = cpu.IsPaused;
            cpu.Pause();

            ScreenText text = ScreenText.Capture(cpu);
            if(text == null || text.Columns == 0 || text.Rows == 0) {
                if(!wasPausedBeforeSelection) cpu.Resume();
                MessageBox.Show(this, "There is no text on the screen to copy.", MessageBoxType.Information);
                return;
            }

            selectionText = text;
            isSelecting = true;
            isDraggingSelection = false;
            eva.InputController.Suspended = true;
            Canvas.Cursor = Cursors.IBeam;
            Canvas.Focus();
            SetTitleText();
        }

        private void EndTextSelection(bool copy) {
            if(!isSelecting) return;

            if(copy && selectionText != null) {
                string text = selectionText.GetText(selectionFrom.X, selectionFrom.Y, selectionTo.X, selectionTo.Y);
                if(text.Length > 0) Clipboard.Instance.Text = text;
            }

            isSelecting = false;
            isDraggingSelection = false;
            selectionText = null;
            if(cpu?.VideoAdapter is IEtoVideoAdapter eva) eva.InputController.Suspended = false;
            Canvas.Cursor = Cursors.Default;
            if(!wasPausedBeforeSelection) cpu?.Resume();
            Canvas.Invalidate();
            SetTitleText();
        }

        private Point CellAt(PointF location) {
            int col = (int)(location.X / Math.Max(1, Canvas.Width) * selectionText.Columns);
            int row = (int)(location.Y / Math.Max(1, Canvas.Height) * selectionText.Rows);
            return new Point(Math.Clamp(col, 0, selectionText.Columns - 1), Math.Clamp(row, 0, selectionText.Rows - 1));
        }

        private void SelectionMouseDown(object sender, MouseEventArgs e) {
            if(!isSelecting) return;
            if(e.Buttons == MouseButtons.Alternate) {
                EndTextSelection(false);
            } else if(e.Buttons == MouseButtons.Primary) {
                selectionFrom = selectionTo = CellAt(e.Location);
                isDraggingSelection = true;
                Canvas.Invalidate();
            }
            e.Handled = true;
        }

        private void SelectionMouseMove(object sender, MouseEventArgs e) {
            if(!isSelecting || !isDraggingSelection) return;
            Point cell = CellAt(e.Location);
            if(cell != selectionTo) {
                selectionTo = cell;
                Canvas.Invalidate();
            }
            e.Handled = true;
        }

        private void SelectionMouseUp(object sender, MouseEventArgs e) {
            if(!isSelecting || !isDraggingSelection) return;
            selectionTo = CellAt(e.Location);
            EndTextSelection(true);
            e.Handled = true;
        }

        private void SelectionKeyDown(object sender, KeyEventArgs e) {
            if(isSelecting && e.Key == Keys.Escape) {
                EndTextSelection(false);
                e.Handled = true;
            }
        }

        private void PaintSelection(object sender, PaintEventArgs e) {
            if(!isSelecting || !isDraggingSelection || selectionText == null) return;

            Point a = selectionFrom, b = selectionTo;
            if(b.Y < a.Y || (b.Y == a.Y && b.X < a.X)) (a, b) = (b, a);

            float cw = Canvas.Width / (float)selectionText.Columns;
            float ch = Canvas.Height / (float)selectionText.Rows;
            Color highlight = Color.FromArgb(72, 61, 139, 140);

            for(int row = a.Y; row <= b.Y; row++) {
                int c0 = row == a.Y ? a.X : 0;
                int c1 = row == b.Y ? b.X : selectionText.Columns - 1;
                e.Graphics.FillRectangle(highlight, c0 * cw, row * ch, (c1 - c0 + 1) * cw, ch);
            }
        }
        #endregion

        private void ShowEmulatorMenu() {
            if(cpu?.Mouse != null) {
                cpu.Mouse.IsCaptured = false;
            }

            Application.Instance.Invoke(() => {
                if(Canvas.IsMouseCaptured) {
                    Canvas.ReleaseMouseCapture();
                }

                ContextMenu?.Show();
            });
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
            if(isSelecting) title = "Drag over the text to copy (Esc cancels) | " + title;

            Application.Instance.AsyncInvoke(() => { this.Title = title; });
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
                long[] clockSpeeds = [4_770_000, 9_540_000, 19_080_000, 38_160_000, 47_700_000];
                int clockIndex = 0;
                foreach(RadioMenuItem item in CreateRadioMenuItems([.. clockSpeeds.Select(hz => (
                            $"{hz / (double)X8086.MHz:F2} MHz",
                            Math.Abs(cpu.Clock - hz) < 10_000))])) { // 10KHz tolerance: the default clock is BASECLOCK (4.77273 MHz)
                    long hz = clockSpeeds[clockIndex++];
                    item.Click += (s, e) => SetCpuClock(hz);
                    cpuClock.Items.Add(item);
                }
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

                emulator.Items.Add(new ButtonMenuItem() { Text = "Soft Reset (CTRL+ALT+DEL)", Command = new Command((s, e) => SoftReset()) });
                emulator.Items.Add(new ButtonMenuItem() { Text = "Hard Reset", Command = new Command((s, e) => HardReset()) });

                emulator.Items.Add(new SeparatorMenuItem());

                emulator.Items.Add(new ButtonMenuItem() { Text = "Load State..." });
                emulator.Items.Add(new ButtonMenuItem() { Text = "Save State..." });

                emulator.Items.Add(new SeparatorMenuItem());

                emulator.Items.Add(new ButtonMenuItem() { Text = "Exit", Command = new Command((s, e) => Application.Instance.Quit()) });
            }
            cm.Items.Add(emulator);

            cm.Items.Add(new ButtonMenuItem() { Text = "Media...", Command = new Command((s, e) => ShowMediaManager()) });

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
                tools.Items.Add(new ButtonMenuItem() { Text = "Debugger...", Command = new Command((s, e) => ShowDebugger()) });
                tools.Items.Add(new ButtonMenuItem() { Text = "Console...", Command = new Command((s, e) => ShowConsole()) });

                tools.Items.Add(new SeparatorMenuItem());

                // AsyncInvoke: let the menu close (and the surface regain focus) first
                tools.Items.Add(new ButtonMenuItem() { Text = "Copy Text (Shift+Alt+C)", Command = new Command((s, e) => Application.Instance.AsyncInvoke(BeginTextSelection)) });
                tools.Items.Add(new ButtonMenuItem() { Text = "Paste Text (Shift+Alt+V)", Command = new Command((s, e) => Application.Instance.AsyncInvoke(PasteText)) });

                cm.Items.Add(tools);
            }

            // Hand keyboard focus back to the emulator surface once the menu is dismissed
            cm.Closed += (s, e) => Application.Instance.AsyncInvoke(() => Canvas?.Focus());

            this.ContextMenu = cm;
        }

    }
}
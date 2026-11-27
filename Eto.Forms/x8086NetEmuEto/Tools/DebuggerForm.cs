using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Tools {
    // CPU debugger: disassembly with breakpoints, registers and flags (editable), stack and memory views.
    //
    // While the debugger is open the CPU runs in debug mode, executing one instruction per StepInto() and
    // raising InstructionDecoded after each one. Everything the UI shows is captured on the emulation thread
    // inside that event (the CPU can't execute while the handler runs), so the decoder, which temporarily
    // borrows CS:IP, never races the execution of an instruction.
    public class DebuggerForm : Form {
        private const int CODE_LINES = 160;
        private const int STACK_WORDS_ABOVE = 8;
        private const int STACK_WORDS_BELOW = 48;
        private const int MEM_ROWS = 16;
        private const int RUN_REFRESH_MS = 150;

        // Disassembly palette (in the spirit of VS Code's Dark+)
        private static readonly Color CurrentBack = Color.FromArgb(0x45, 0x45, 0x1E);
        private static readonly Color BreakBack = Color.FromArgb(0x4B, 0x1E, 0x1E);
        private static readonly Color CurrentBreakBack = Color.FromArgb(0x5C, 0x34, 0x1C);
        private static readonly Color CurrentArrow = Color.FromArgb(0xFF, 0xCC, 0x00);
        private static readonly Color AddressFore = Color.FromArgb(0x85, 0x85, 0x85);
        private static readonly Color BytesFore = Color.FromArgb(0x6E, 0x6E, 0x6E);
        private static readonly Color FlowFore = Color.FromArgb(0xC5, 0x86, 0xC0);     // Jumps, calls, returns, interrupts
        private static readonly Color MoveFore = Color.FromArgb(0x56, 0x9C, 0xD6);     // Data movement and stack
        private static readonly Color OtherFore = Color.FromArgb(0x4E, 0xC9, 0xB0);    // Arithmetic, logic, everything else
        private static readonly Color OperandsFore = Color.FromArgb(0x9C, 0xDC, 0xFE);
        private static readonly Color StackTopBack = Color.FromArgb(0x26, 0x4F, 0x78);

        public sealed class CodeLine {
            public uint Address { get; init; }
            public ushort Segment { get; init; }
            public ushort Offset { get; init; }
            public byte[] Raw { get; init; }
            public string AddressText => $"{Segment:X4}:{Offset:X4}";
            public string Mnemonic { get; init; }
            public string Operands { get; init; }
        }

        public sealed class StackLine {
            public string Address { get; init; }
            public string Value { get; init; }
            public bool IsTop { get; init; }
        }

        private sealed class Snapshot {
            public X8086.GPRegisters Registers;
            public ushort Flags;
            public string ActiveSegment;
            public bool Halted;
            public List<CodeLine> Code;
            public int CurrentIndex;
            public StackLine[] Stack;
            public uint MemAddress;
            public byte[] Memory;
        }

        private X8086 cpu;
        private readonly object bpLock = new();
        private readonly HashSet<uint> breakpoints = [];
        private uint? tempBreakpoint;
        private uint? runToAddress;

        private List<CodeLine> code = [];          // Owned by the capture side (emulation thread / idle UI)
        private volatile uint memAddress = 0x400;  // Linear address shown in the memory view
        private volatile bool bytesAsChars;

        private volatile bool running;
        private volatile bool closing;
        private Thread runThread;
        private readonly AutoResetEvent stepped = new(false);
        private int uiUpdatePending;
        private Snapshot lastSnapshot;

        // UI
        private readonly GridView codeGrid;
        private readonly GridView stackGrid;
        private readonly TextArea memoryView;
        private readonly Dictionary<X8086.GPRegisters.RegistersTypes, TextBox> registerBoxes = [];
        private readonly Dictionary<string, CheckBox> flagBoxes = [];
        private readonly Label statusLabel = new();
        private readonly Label stateLabel = new();
        private readonly Label breakpointsLabel = new();
        private readonly Button runButton = new() { Text = "▶  Run", ToolTip = "Run until a breakpoint (F5)" };
        private readonly TextBox memSegment = new() { Text = "0040", Width = 56 };
        private readonly TextBox memOffset = new() { Text = "0000", Width = 56 };
        private readonly TextBox breakCS = new() { Width = 56 };
        private readonly TextBox breakIP = new() { Width = 56 };
        private readonly TextBox searchBox = new() { PlaceholderText = "Find text in memory" };
        private readonly CheckBox searchVideo = new() { Text = "Text video memory" };
        private readonly Font mono = Theme.Mono(10);
        private bool updatingUI;
        private uint? currentAddress;                       // CS:IP shown as the current instruction
        private X8086.GPRegisters previousRegisters;        // To highlight what the last step changed
        private ushort previousFlags;

        public DebuggerForm(X8086 emulator) {
            Title = "x8086NetEmu Debugger";
            ClientSize = new Size(1120, 880);
            MinimumSize = new Size(900, 600);

            // Disassembly
            codeGrid = new GridView {
                GridLines = GridLines.None,
                RowHeight = 20,
                Columns = {
                    new GridColumn { HeaderText = "", Width = 34, Resizable = false, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => Gutter(l)), TextAlignment = TextAlignment.Center } },
                    new GridColumn { HeaderText = "Address", Width = 96, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => l.AddressText) } },
                    new GridColumn { HeaderText = "Bytes", Width = 160, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => BytesText(l.Raw)) } },
                    new GridColumn { HeaderText = "Instruction", Width = 90, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => l.Mnemonic) } },
                    new GridColumn { HeaderText = "Operands", Expand = true, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => l.Operands) } }
                }
            };
            codeGrid.CellFormatting += (s, e) => {
                if(e.Item is not CodeLine line) return;
                e.Font = mono;
                bool current = currentAddress == line.Address;
                bool bp = IsBreakpoint(line.Address);
                // Always set the background (rows are recycled); transparent lets hover and selection show through
                e.BackgroundColor = current ? (bp ? CurrentBreakBack : CurrentBack) : (bp ? BreakBack : Colors.Transparent);
                e.ForegroundColor = e.Column.HeaderText switch {
                    "" => current ? CurrentArrow : Theme.Error,
                    "Address" => current ? Theme.Text : AddressFore,
                    "Bytes" => BytesFore,
                    "Instruction" => MnemonicColor(line.Mnemonic),
                    _ => OperandsFore
                };
            };
            codeGrid.CellDoubleClick += (s, e) => {
                if(e.Item is CodeLine line) ToggleBreakpoint(line);
            };

            ContextMenu codeMenu = new();
            codeMenu.Items.Add(new ButtonMenuItem { Text = "Toggle Breakpoint", Shortcut = Keys.F9, Command = new Command((s, e) => ToggleSelectedBreakpoint()) });
            codeMenu.Items.Add(new ButtonMenuItem { Text = "Run to Cursor", Shortcut = Keys.F4, Command = new Command((s, e) => RunToSelected()) });
            codeMenu.Items.Add(new ButtonMenuItem { Text = "Set Next Instruction (CS:IP)", Command = new Command((s, e) => SetIPToSelected()) });
            codeMenu.Items.Add(new SeparatorMenuItem());
            codeMenu.Items.Add(new ButtonMenuItem { Text = "Clear All Breakpoints", Command = new Command((s, e) => ClearBreakpoints()) });
            codeGrid.ContextMenu = codeMenu;

            // Registers
            TableLayout regs = new() { Spacing = new Size(6, 4) };
            (string, X8086.GPRegisters.RegistersTypes)[][] regRows = [
                [("AX", X8086.GPRegisters.RegistersTypes.AX), ("SI", X8086.GPRegisters.RegistersTypes.SI), ("CS", X8086.GPRegisters.RegistersTypes.CS)],
                [("BX", X8086.GPRegisters.RegistersTypes.BX), ("DI", X8086.GPRegisters.RegistersTypes.DI), ("DS", X8086.GPRegisters.RegistersTypes.DS)],
                [("CX", X8086.GPRegisters.RegistersTypes.CX), ("BP", X8086.GPRegisters.RegistersTypes.BP), ("ES", X8086.GPRegisters.RegistersTypes.ES)],
                [("DX", X8086.GPRegisters.RegistersTypes.DX), ("SP", X8086.GPRegisters.RegistersTypes.SP), ("SS", X8086.GPRegisters.RegistersTypes.SS)],
                [("IP", X8086.GPRegisters.RegistersTypes.IP)]
            ];
            List<Label> dimLabels = [];
            foreach(var row in regRows) {
                TableRow tr = new();
                foreach((string name, X8086.GPRegisters.RegistersTypes reg) in row) {
                    TextBox tb = new() { Width = 58, Font = mono };
                    X8086.GPRegisters.RegistersTypes r = reg;
                    tb.KeyDown += (s, e) => {
                        if(e.Key == Keys.Enter) { CommitRegister(r, tb); e.Handled = true; }
                    };
                    tb.LostFocus += (s, e) => CommitRegister(r, tb);
                    registerBoxes[reg] = tb;
                    Label l = new() { Text = name, Font = mono, VerticalAlignment = VerticalAlignment.Center };
                    dimLabels.Add(l);
                    tr.Cells.Add(l);
                    tr.Cells.Add(tb);
                }
                tr.Cells.Add(null);
                regs.Rows.Add(tr);
            }

            // Flags, in two rows so they never get clipped
            TableLayout flags = new() { Spacing = new Size(10, 4) };
            foreach(string[] flagRow in new[] { new[] { "OF", "DF", "IF", "TF", "SF" }, new[] { "ZF", "AF", "PF", "CF" } }) {
                TableRow tr = new();
                foreach(string f in flagRow) {
                    CheckBox cb = new() { Text = f, Font = mono };
                    string flag = f;
                    cb.CheckedChanged += (s, e) => {
                        if(updatingUI) return;
                        SetFlag(flag, cb.Checked == true);
                    };
                    flagBoxes[f] = cb;
                    tr.Cells.Add(cb);
                }
                tr.Cells.Add(null);
                flags.Rows.Add(tr);
            }

            // Stack
            stackGrid = new GridView {
                GridLines = GridLines.None,
                RowHeight = 20,
                Columns = {
                    new GridColumn { HeaderText = "SS:SP", Width = 120, DataCell = new TextBoxCell { Binding = Binding.Delegate((StackLine l) => (l.IsTop ? "➜ " : "  ") + l.Address) } },
                    new GridColumn { HeaderText = "Value", Expand = true, DataCell = new TextBoxCell { Binding = Binding.Delegate((StackLine l) => l.Value) } }
                }
            };
            stackGrid.CellFormatting += (s, e) => {
                if(e.Item is not StackLine line) return;
                e.Font = mono;
                e.BackgroundColor = line.IsTop ? StackTopBack : Colors.Transparent;
                e.ForegroundColor = line.IsTop ? Colors.White : (e.Column.HeaderText == "Value" ? Theme.Text : AddressFore);
            };

            // Memory
            memoryView = new TextArea { ReadOnly = true, Wrap = false, Font = mono };
            Button memBack = new() { Text = "◀", Width = 34, ToolTip = "Previous 256 bytes" };
            memBack.Click += (s, e) => MoveMemory(-0x100);
            Button memForward = new() { Text = "▶", Width = 34, ToolTip = "Next 256 bytes" };
            memForward.Click += (s, e) => MoveMemory(0x100);
            memSegment.Font = mono;
            memOffset.Font = mono;
            memSegment.KeyDown += (s, e) => { if(e.Key == Keys.Enter) { ApplyMemoryAddress(); e.Handled = true; } };
            memOffset.KeyDown += (s, e) => { if(e.Key == Keys.Enter) { ApplyMemoryAddress(); e.Handled = true; } };
            Button memGo = new() { Text = "Go" };
            memGo.Click += (s, e) => ApplyMemoryAddress();
            Button searchButton = new() { Text = "Find Next" };
            searchButton.Click += (s, e) => SearchMemory();
            searchBox.KeyDown += (s, e) => { if(e.Key == Keys.Enter) { SearchMemory(); e.Handled = true; } };
            searchBox.TextChanged += (s, e) => searchBox.BackgroundColor = Theme.Input;

            // Toolbar
            Button stepInto = new() { Text = "↓  Step Into", ToolTip = "Execute one instruction (F8)" };
            stepInto.Click += (s, e) => StepInto();
            Button stepOver = new() { Text = "↷  Step Over", ToolTip = "Run calls, interrupts, loops and REP instructions to completion (F10)" };
            stepOver.Click += (s, e) => StepOver();
            Button runToCursor = new() { Text = "⇥  Run to Cursor", ToolTip = "Run until the selected instruction (F4)" };
            runToCursor.Click += (s, e) => RunToSelected();
            runButton.Click += (s, e) => ToggleRun();
            Button refresh = new() { Text = "⟳  Refresh", ToolTip = "Disassemble again from CS:IP" };
            refresh.Click += (s, e) => RefreshNow(true);
            Button decIP = new() { Text = "IP − 1", ToolTip = "Move the instruction pointer back one byte" };
            decIP.Click += (s, e) => { if(!running) { cpu.Registers.IP--; RefreshNow(true); } };
            Button reboot = new() { Text = "Reboot", ToolTip = "Hard reset the emulated machine" };
            reboot.Click += (s, e) => {
                if(Prompt.Show(this, "Hard reset the emulated machine?", MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes) {
                    StopRunning();
                    cpu.HardReset();
                }
            };
            CheckBox chars = new() { Text = "Bytes as characters" };
            chars.CheckedChanged += (s, e) => { bytesAsChars = chars.Checked == true; ReloadCode(); };
            breakCS.Font = mono;
            breakIP.Font = mono;
            breakCS.KeyDown += (s, e) => { if(e.Key == Keys.Enter) { UpdateRunTo(); e.Handled = true; } };
            breakIP.KeyDown += (s, e) => { if(e.Key == Keys.Enter) { UpdateRunTo(); e.Handled = true; } };
            breakCS.LostFocus += (s, e) => UpdateRunTo();
            breakIP.LostFocus += (s, e) => UpdateRunTo();

            StackLayout toolbar = new() {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                VerticalContentAlignment = VerticalAlignment.Center,
                Items = { runButton, stepInto, stepOver, runToCursor, new Panel { Width = 8 }, refresh, decIP, reboot }
            };

            Label colon1 = new() { Text = ":", VerticalAlignment = VerticalAlignment.Center };
            TableLayout breakAt = new(new TableRow(breakCS, colon1, breakIP, null)) { Spacing = new Size(4, 0) };

            TableLayout right = new() {
                Padding = new Padding(8, 0, 0, 0),
                Spacing = new Size(4, 10),
                Rows = {
                    new GroupBox { Text = "Registers", Content = regs, Padding = 8 },
                    new GroupBox { Text = "Flags", Content = flags, Padding = 8 },
                    new GroupBox { Text = "Run Until CS:IP  (Enter sets, empty clears)", Content = breakAt, Padding = 8 },
                    new TableRow(new GroupBox { Text = "Stack", Content = stackGrid, Padding = 0 }) { ScaleHeight = true }
                }
            };

            Label addressLabel = new() { Text = "Address", VerticalAlignment = VerticalAlignment.Center };
            Label colon2 = new() { Text = ":", VerticalAlignment = VerticalAlignment.Center };
            TableLayout memory = new() {
                Spacing = new Size(4, 6),
                Rows = {
                    new TableLayout(new TableRow(addressLabel, memSegment, colon2, memOffset, memGo, memBack, memForward, new Panel { Width = 12 },
                                                 new TableCell(searchBox, true), searchVideo, searchButton)) { Spacing = new Size(6, 0) },
                    new TableRow(memoryView) { ScaleHeight = true }
                }
            };

            TableLayout statusBar = new(new TableRow(stateLabel, statusLabel, null, breakpointsLabel)) { Spacing = new Size(14, 0), Padding = new Padding(2, 0) };

            // Resizable panes: disassembly | registers, flags and stack; memory below. The side and bottom panes keep
            // their size when the window is resized.
            Splitter top = new() {
                Orientation = Orientation.Horizontal,
                FixedPanel = SplitterFixedPanel.Panel2,
                Panel1 = new GroupBox { Text = "Disassembly", Content = codeGrid, Padding = 0 },
                Panel2 = right,
                Panel1MinimumSize = 420,
                Panel2MinimumSize = 300,
                Position = 740
            };
            Splitter main = new() {
                Orientation = Orientation.Vertical,
                FixedPanel = SplitterFixedPanel.Panel2,
                Panel1 = top,
                Panel2 = new TableLayout { Padding = new Padding(0, 8, 0, 0), Rows = { new TableRow(new GroupBox { Text = "Memory", Content = memory, Padding = 8 }) { ScaleHeight = true } } },
                Panel1MinimumSize = 260,
                Panel2MinimumSize = 140,
                Position = 520
            };

            Content = new TableLayout {
                Padding = new Padding(10, 8, 10, 6),
                Spacing = new Size(10, 8),
                Rows = {
                    new TableLayout(new TableRow(toolbar, null, chars)) { Spacing = new Size(8, 0) },
                    new TableRow(main) { ScaleHeight = true },
                    statusBar
                }
            };

            // Menu shortcuts work regardless of which control has the focus
            ButtonMenuItem debugMenu = new() { Text = "&Debug" };
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Run / Break", Shortcut = Keys.F5, Command = new Command((s, e) => ToggleRun()) });
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Step Into", Shortcut = Keys.F8, Command = new Command((s, e) => StepInto()) });
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Step Over", Shortcut = Keys.F10, Command = new Command((s, e) => StepOver()) });
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Step Over (alternate)", Shortcut = Keys.Shift | Keys.F8, Command = new Command((s, e) => StepOver()) });
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Run to Cursor", Shortcut = Keys.F4, Command = new Command((s, e) => RunToSelected()) });
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Toggle Breakpoint", Shortcut = Keys.F9, Command = new Command((s, e) => ToggleSelectedBreakpoint()) });
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Clear All Breakpoints", Command = new Command((s, e) => ClearBreakpoints()) });
            debugMenu.Items.Add(new SeparatorMenuItem());
            debugMenu.Items.Add(new ButtonMenuItem { Text = "Close", Command = new Command((s, e) => Close()) });
            Menu = new MenuBar { Items = { debugMenu }, IncludeSystemItems = MenuBarSystemItems.None };

            Theme.Apply(this);
            foreach(Label l in dimLabels) l.TextColor = Theme.DimText;
            foreach(Label l in new[] { colon1, colon2, addressLabel, statusLabel, breakpointsLabel }) l.TextColor = Theme.DimText;
            UpdateBreakpointsLabel();

            Attach(emulator);
        }

        // Gutter: breakpoint dot and/or the current-instruction arrow
        private string Gutter(CodeLine l) {
            bool bp = IsBreakpoint(l.Address);
            bool current = currentAddress == l.Address;
            return bp && current ? "●➜" : bp ? "●" : current ? "➜" : "";
        }

        private static Color MnemonicColor(string mnemonic) {
            string m = (mnemonic ?? "").Trim().ToUpperInvariant();
            if(m.StartsWith('J') || m.StartsWith("CALL") || m.StartsWith("RET") || m.StartsWith("INT") || m == "IRET" ||
               m.StartsWith("LOOP") || m == "HLT" || m == "INTO") return FlowFore;
            if(m.StartsWith("MOV") || m.StartsWith("PUSH") || m.StartsWith("POP") || m == "XCHG" || m == "LEA" || m == "LDS" ||
               m == "LES" || m.StartsWith("LODS") || m.StartsWith("STOS") || m == "XLAT" || m == "IN" || m == "OUT") return MoveFore;
            return OtherFore;
        }

        private void UpdateBreakpointsLabel() {
            int n;
            lock(bpLock) n = breakpoints.Count;
            breakpointsLabel.Text = n == 0 ? "No breakpoints" : $"{n} breakpoint{(n > 1 ? "s" : "")}";
        }

        // Connects the debugger to an emulator instance (also used after a hard reset creates a new one)
        public void Attach(X8086 emulator) {
            StopRunning();
            if(cpu != null) cpu.InstructionDecoded -= OnInstructionDecoded;

            cpu = emulator;
            code = [];
            cpu.InstructionDecoded += OnInstructionDecoded;
            cpu.DoReschedule = true;
            cpu.DebugMode = true;

            // Execute a single instruction so the CPU stops at a known instruction boundary and reports its state
            cpu.StepInto();
        }

        protected override void OnClosed(EventArgs e) {
            closing = true;
            StopRunning();
            if(cpu != null) {
                cpu.InstructionDecoded -= OnInstructionDecoded;
                cpu.DebugMode = false;
            }
            stepped.Set();
            base.OnClosed(e);
        }

        #region Execution control
        // Emulation thread
        private void OnInstructionDecoded() {
            if(closing) return;

            if(running) {
                stepped.Set(); // The run loop decides when to refresh
            } else {
                Snapshot snap = Capture();
                stepped.Set();
                PostSnapshot(snap);
            }
        }

        private void StepInto() {
            if(running || cpu.IsHalted) return;
            cpu.StepInto();
        }

        // Runs calls, interrupts, loops and repeated string instructions to completion
        private void StepOver() {
            if(running) return;
            X8086.Instruction ins = cpu.Decode(cpu.Registers.CS, cpu.Registers.IP);
            if(ins.IsValid && IsSteppable(ins.Bytes)) {
                tempBreakpoint = X8086.SegmentOffsetToAbsolute(cpu.Registers.CS, (ushort)(cpu.Registers.IP + ins.Size));
                StartRunning();
            } else {
                StepInto();
            }
        }

        private static bool IsSteppable(byte[] bytes) {
            if(bytes == null) return false;
            int i = 0;
            while(i < bytes.Length && bytes[i] is 0x26 or 0x2E or 0x36 or 0x3E or 0xF0 or 0xF2 or 0xF3) {
                if(bytes[i] is 0xF2 or 0xF3) return true; // REP prefix
                i++;
            }
            if(i >= bytes.Length) return false;

            return bytes[i] switch {
                0xE8 or 0x9A or 0xCC or 0xCD or 0xCE or 0xE0 or 0xE1 or 0xE2 => true,
                0xFF => i + 1 < bytes.Length && ((bytes[i + 1] >> 3) & 7) is 2 or 3,
                _ => false
            };
        }

        private void ToggleRun() {
            if(running) StopRunning(); else StartRunning();
        }

        private void StartRunning() {
            if(running || cpu.IsHalted) return;
            running = true;
            runButton.Text = "⏸  Break";
            SetState("RUNNING");

            X8086 target = cpu;
            runThread = new Thread(() => RunLoop(target)) { IsBackground = true, Name = "Debugger run loop" };
            runThread.Start();
        }

        // The run loop notices the request after the instruction in flight completes
        private void StopRunning() {
            if(!running) return;
            running = false;
            if(runThread != null && runThread != Thread.CurrentThread) runThread.Join(2000);
            runThread = null;
        }

        private void RunLoop(X8086 target) {
            Stopwatch sw = Stopwatch.StartNew();
            string reason = "Stopped";
            bool done = true;

            while(running && !closing && target == cpu) {
                stepped.Reset();
                target.StepInto();

                // Wait for the instruction to complete; the CPU must be idle before anything is captured
                while(!(done = stepped.WaitOne(100))) {
                    if(closing || !target.DebugMode || X8086.IsClosing) break;
                }
                if(!done || !running) break;

                uint ip = X8086.SegmentOffsetToAbsolute(target.Registers.CS, target.Registers.IP);
                if(target.IsHalted) { reason = "CPU halted"; break; }
                if(tempBreakpoint == ip) { reason = "Stepped over"; break; }
                if(runToAddress == ip) { reason = "Reached break address"; break; }
                if(IsBreakpoint(ip)) { reason = $"Breakpoint at {target.Registers.CS:X4}:{target.Registers.IP:X4}"; break; }

                if(sw.ElapsedMilliseconds >= RUN_REFRESH_MS) {
                    sw.Restart();
                    PostSnapshot(Capture()); // Safe: the CPU is waiting for the next step
                }
            }

            running = false;
            tempBreakpoint = null;
            if(closing || target != cpu) return;
            if(!done) { // The CPU left debug mode (or is shutting down): nothing safe to capture
                Application.Instance.AsyncInvoke(() => { runButton.Text = "▶  Run"; SetState("RUNNING (NOT DEBUGGING)"); });
                return;
            }

            Snapshot snap = Capture();
            Application.Instance.AsyncInvoke(() => {
                runButton.Text = "▶  Run";
                Apply(snap);
                statusLabel.Text = $"{reason}   ·   {statusLabel.Text}";
            });
        }

        private void SetState(string state) {
            stateLabel.Text = "● " + state;
            stateLabel.TextColor = state.StartsWith("PAUSED") ? Theme.Warning : state.StartsWith("HALTED") ? Theme.Error : Theme.Success;
        }

        private void RefreshNow(bool rebuildCode) {
            if(running) return;
            if(rebuildCode) code = [];
            Apply(Capture());
        }
        #endregion

        #region Snapshots
        // Must only run while the CPU is not executing (emulation thread inside InstructionDecoded, or the UI
        // thread while the CPU is waiting for a step)
        private Snapshot Capture() {
            X8086.GPRegisters r = cpu.Registers;
            Snapshot s = new() {
                Registers = (X8086.GPRegisters)r.Clone(),
                Flags = cpu.Flags.EFlags,
                ActiveSegment = r.ActiveSegmentRegister.ToString(),
                Halted = cpu.IsHalted
            };

            uint current = X8086.SegmentOffsetToAbsolute(r.CS, r.IP);
            int index = code.FindIndex(l => l.Address == current);
            if(index < 0 || index > code.Count - 12) {
                code = Disassemble(r.CS, r.IP);
                index = 0;
            }
            s.Code = code;
            s.CurrentIndex = index;

            List<StackLine> stack = [];
            for(int w = -STACK_WORDS_ABOVE; w <= STACK_WORDS_BELOW; w++) {
                ushort sp = (ushort)(r.SP + w * 2);
                uint a = X8086.SegmentOffsetToAbsolute(r.SS, sp);
                stack.Add(new StackLine { Address = $"{r.SS:X4}:{sp:X4}", Value = $"{cpu.Memory[a] | (cpu.Memory[(a + 1) & 0xFFFFF] << 8):X4}", IsTop = w == 0 });
            }
            s.Stack = [.. stack];

            s.MemAddress = memAddress;
            s.Memory = new byte[MEM_ROWS * 16];
            for(int i = 0; i < s.Memory.Length; i++) s.Memory[i] = cpu.Memory[(s.MemAddress + i) & 0xFFFFF];

            return s;
        }

        private List<CodeLine> Disassemble(ushort cs, ushort ip) {
            List<CodeLine> lines = new(CODE_LINES);
            for(int n = 0; n < CODE_LINES; n++) {
                X8086.Instruction ins = cpu.Decode(cs, ip);
                if(!ins.IsValid || ins.Size == 0) break;

                string operands;
                if(string.IsNullOrEmpty(ins.Message)) {
                    string so = ins.SegmentOverride ?? "";
                    string p1 = ins.Parameter1?.Replace("[", "[" + so) ?? "";
                    string p2 = ins.Parameter2?.Replace("[", "[" + so) ?? "";
                    operands = p2 == "" ? p1 : $"{p1}, {p2}";
                } else {
                    operands = ins.Message;
                }

                lines.Add(new CodeLine {
                    Address = X8086.SegmentOffsetToAbsolute(cs, ip),
                    Segment = cs,
                    Offset = ip,
                    Raw = ins.Bytes ?? [],
                    Mnemonic = ins.Mnemonic ?? "",
                    Operands = operands
                });
                ip = (ushort)(ip + ins.Size);
            }
            return lines;
        }

        private void PostSnapshot(Snapshot snap) {
            Volatile.Write(ref pendingSnapshot, snap);
            if(Interlocked.Exchange(ref uiUpdatePending, 1) == 1) return;
            Application.Instance.AsyncInvoke(() => {
                Volatile.Write(ref uiUpdatePending, 0);
                Snapshot s = Volatile.Read(ref pendingSnapshot);
                if(s != null && !closing) Apply(s);
            });
        }
        private Snapshot pendingSnapshot;

        // UI thread
        private void Apply(Snapshot s) {
            updatingUI = true;
            try {
                bool codeChanged = lastSnapshot == null || !ReferenceEquals(lastSnapshot.Code, s.Code);
                int previousIndex = lastSnapshot?.CurrentIndex ?? -1;
                lastSnapshot = s;
                currentAddress = s.CurrentIndex < s.Code.Count ? s.Code[s.CurrentIndex].Address : null;

                if(codeChanged) {
                    codeGrid.DataStore = s.Code;
                } else {
                    if(previousIndex >= 0 && previousIndex < s.Code.Count) codeGrid.ReloadData(previousIndex);
                    if(s.CurrentIndex < s.Code.Count) codeGrid.ReloadData(s.CurrentIndex);
                }
                if(s.CurrentIndex < s.Code.Count) codeGrid.ScrollToRow(Math.Min(s.Code.Count - 1, s.CurrentIndex + 8));
                if(s.CurrentIndex < s.Code.Count) codeGrid.ScrollToRow(s.CurrentIndex);

                X8086.GPRegisters r = s.Registers;
                foreach((X8086.GPRegisters.RegistersTypes reg, TextBox tb) in registerBoxes) {
                    if(tb.HasFocus) continue;
                    ushort v = r.get_Val(reg);
                    tb.Text = v.ToString("X4");
                    tb.BackgroundColor = Theme.Input;
                    tb.TextColor = previousRegisters != null && previousRegisters.get_Val(reg) != v ? Theme.Changed : Theme.Text;
                    tb.ToolTip = $"{v}d\n{v:X4}h\n{Convert.ToString(v, 2).PadLeft(16, '0')}b";
                }

                string[] order = ["CF", "", "PF", "", "AF", "", "ZF", "SF", "TF", "IF", "DF", "OF"];
                for(int bit = 0; bit < order.Length; bit++) {
                    if(order[bit] == "") continue;
                    flagBoxes[order[bit]].Checked = (s.Flags & (1 << bit)) != 0;
                    flagBoxes[order[bit]].TextColor = previousRegisters != null && ((previousFlags ^ s.Flags) & (1 << bit)) != 0 ? Theme.Changed : Theme.Text;
                }
                previousRegisters = r;
                previousFlags = s.Flags;

                stackGrid.DataStore = s.Stack;
                stackGrid.ScrollToRow(s.Stack.Length - 1);     // Scroll past SP first so it ends up near the top,
                stackGrid.ScrollToRow(STACK_WORDS_ABOVE - 2);  // with the pushed values below it in view

                memoryView.Text = FormatMemory(s.MemAddress, s.Memory);

                SetState(s.Halted ? "HALTED" : running ? "RUNNING" : "PAUSED");
                statusLabel.Text = $"CS:IP {r.CS:X4}:{r.IP:X4}   Active segment {s.ActiveSegment}";
            } finally {
                updatingUI = false;
            }
        }

        private void ReloadCode() {
            if(lastSnapshot?.Code is { Count: > 0 } c) codeGrid.ReloadData(new Range<int>(0, c.Count - 1));
        }

        private static string FormatMemory(uint address, byte[] data) {
            StringBuilder sb = new();
            sb.Append("Address    ");
            for(int k = 0; k < 16; k++) sb.Append(k == 8 ? $"- {k:X2} " : $"{k:X2} ");
            sb.Append("  0123456789ABCDEF");
            sb.AppendLine();
            for(int row = 0; row < data.Length / 16; row++) {
                uint a = (address + (uint)row * 16) & 0xFFFFF;
                sb.Append($"{X8086.AbsoluteToSegment(a):X4}:{X8086.AbsoluteToOffset(a):X4}  ");
                for(int k = 0; k < 16; k++) {
                    if(k == 8) sb.Append("- ");
                    sb.Append($"{data[row * 16 + k]:X2} ");
                }
                sb.Append("  ");
                for(int k = 0; k < 16; k++) {
                    byte b = data[row * 16 + k];
                    sb.Append(b < 32 || b > 126 ? '.' : (char)b);
                }
                if(row < data.Length / 16 - 1) sb.AppendLine();
            }
            return sb.ToString();
        }

        private string BytesText(byte[] raw) {
            if(raw == null) return "";
            return bytesAsChars
                ? string.Join(" ", raw.Select(b => b < 32 || b > 126 ? "." : ((char)b).ToString()))
                : string.Join(" ", raw.Select(b => b.ToString("X2")));
        }
        #endregion

        #region Breakpoints
        private bool IsBreakpoint(uint address) {
            lock(bpLock) return breakpoints.Contains(address);
        }

        private void ToggleBreakpoint(CodeLine line) {
            lock(bpLock) {
                if(!breakpoints.Remove(line.Address)) breakpoints.Add(line.Address);
            }
            ReloadCode();
            UpdateBreakpointsLabel();
        }

        private void ToggleSelectedBreakpoint() {
            if(codeGrid.SelectedItem is CodeLine line) ToggleBreakpoint(line);
        }

        private void ClearBreakpoints() {
            lock(bpLock) breakpoints.Clear();
            ReloadCode();
            UpdateBreakpointsLabel();
        }

        private void RunToSelected() {
            if(running || codeGrid.SelectedItem is not CodeLine line) return;
            tempBreakpoint = line.Address;
            StartRunning();
        }

        private void SetIPToSelected() {
            if(running || codeGrid.SelectedItem is not CodeLine line) return;
            cpu.Registers.CS = line.Segment;
            cpu.Registers.IP = line.Offset;
            RefreshNow(false);
        }

        private void UpdateRunTo() {
            if(string.IsNullOrWhiteSpace(breakCS.Text) && string.IsNullOrWhiteSpace(breakIP.Text)) {
                runToAddress = null;
                return;
            }
            bool okCS = TryEvaluate(breakCS.Text, out int cs);
            bool okIP = TryEvaluate(breakIP.Text, out int ip);
            breakCS.BackgroundColor = okCS ? Theme.Input : Theme.Invalid;
            breakIP.BackgroundColor = okIP ? Theme.Input : Theme.Invalid;
            runToAddress = okCS && okIP ? X8086.SegmentOffsetToAbsolute((ushort)cs, (ushort)ip) : null;
        }
        #endregion

        #region Registers, flags and memory
        private void CommitRegister(X8086.GPRegisters.RegistersTypes reg, TextBox tb) {
            if(updatingUI || cpu == null) return;
            if(!TryEvaluate(tb.Text, out int value)) {
                tb.BackgroundColor = Theme.Invalid;
                return;
            }
            tb.BackgroundColor = Theme.Input;
            if(running || cpu.Registers.get_Val(reg) == (ushort)value) return;

            cpu.Registers.set_Val(reg, (ushort)value);
            RefreshNow(reg is X8086.GPRegisters.RegistersTypes.CS or X8086.GPRegisters.RegistersTypes.IP);
        }

        private void SetFlag(string flag, bool value) {
            if(running) return;
            byte v = (byte)(value ? 1 : 0);
            X8086.GPFlags f = cpu.Flags;
            switch(flag) {
                case "CF": f.CF = v; break;
                case "PF": f.PF = v; break;
                case "AF": f.AF = v; break;
                case "ZF": f.ZF = v; break;
                case "SF": f.SF = v; break;
                case "TF": f.TF = v; break;
                case "IF": f.IF = v; break;
                case "DF": f.DF = v; break;
                case "OF": f.OF = v; break;
            }
        }

        private void ApplyMemoryAddress() {
            bool okSeg = TryEvaluate(memSegment.Text, out int seg);
            bool okOff = TryEvaluate(memOffset.Text, out int off);
            memSegment.BackgroundColor = okSeg ? Theme.Input : Theme.Invalid;
            memOffset.BackgroundColor = okOff ? Theme.Input : Theme.Invalid;
            if(!okSeg || !okOff) return;

            memAddress = X8086.SegmentOffsetToAbsolute((ushort)seg, (ushort)off);
            RefreshMemory();
        }

        private void MoveMemory(int delta) {
            ShowMemory((uint)((memAddress + delta) & 0xFFFFF));
        }

        private void ShowMemory(uint address) {
            memAddress = address;
            memSegment.Text = X8086.AbsoluteToSegment(address).ToString("X4");
            memOffset.Text = X8086.AbsoluteToOffset(address).ToString("X4");
            RefreshMemory();
        }

        private void RefreshMemory() {
            uint a = memAddress;
            byte[] data = new byte[MEM_ROWS * 16];
            for(int i = 0; i < data.Length; i++) data[i] = cpu.Memory[(a + i) & 0xFFFFF];
            memoryView.Text = FormatMemory(a, data);
        }

        // Case-insensitive ASCII search, starting after the current memory address and wrapping around.
        // With "Text video memory" the characters are matched every other byte (character/attribute pairs).
        private void SearchMemory() {
            string text = searchBox.Text;
            if(string.IsNullOrEmpty(text)) return;

            byte[] mem = cpu.Memory;
            int size = mem.Length;
            int stride = searchVideo.Checked == true ? 2 : 1;
            byte[] needle = Encoding.ASCII.GetBytes(text.ToLowerInvariant());
            int span = (needle.Length - 1) * stride + 1;

            int start = (int)((memAddress + 1) % size);
            for(int n = 0; n < size; n++) {
                int i = (start + n) % size;
                if(i + span > size) continue;

                bool found = true;
                for(int j = 0; j < needle.Length; j++) {
                    byte b = mem[i + j * stride];
                    if(b >= 'A' && b <= 'Z') b += 32;
                    if(b != needle[j]) { found = false; break; }
                }
                if(found) {
                    searchBox.BackgroundColor = Theme.Valid;
                    ShowMemory((uint)i);
                    return;
                }
            }
            searchBox.BackgroundColor = Theme.Invalid;
        }

        // Expressions: numbers are hexadecimal unless suffixed with 'd' (decimal), 'b' (binary) or 'o' (octal)
        // ('h' is also accepted); register names (AX, AL, ..., IP) and AS (the active segment) can be used;
        // operators + - * / % and parentheses.
        private bool TryEvaluate(string expression, out int value) {
            value = 0;
            if(string.IsNullOrWhiteSpace(expression)) return false;
            try {
                ExpressionParser p = new(expression, ResolveSymbol);
                value = p.Parse() & 0xFFFF;
                return true;
            } catch(FormatException) {
                return false;
            } catch(DivideByZeroException) {
                return false;
            }
        }

        private int? ResolveSymbol(string name) {
            string n = name.ToUpperInvariant();
            if(n == "AS") return cpu.Registers.ActiveSegmentValue;
            if(n.Length == 2 && Enum.TryParse(n, out X8086.GPRegisters.RegistersTypes reg) && reg != X8086.GPRegisters.RegistersTypes.NONE) {
                return cpu.Registers.get_Val(reg);
            }
            return null;
        }

        private sealed class ExpressionParser(string text, Func<string, int?> resolve) {
            private int pos;

            public int Parse() {
                int v = ParseSum();
                SkipSpaces();
                if(pos != text.Length) throw new FormatException();
                return v;
            }

            private int ParseSum() {
                int v = ParseProduct();
                while(true) {
                    SkipSpaces();
                    if(Accept('+')) v += ParseProduct();
                    else if(Accept('-')) v -= ParseProduct();
                    else return v;
                }
            }

            private int ParseProduct() {
                int v = ParseUnary();
                while(true) {
                    SkipSpaces();
                    if(Accept('*')) v *= ParseUnary();
                    else if(Accept('/')) v /= ParseUnary();
                    else if(Accept('%')) v %= ParseUnary();
                    else return v;
                }
            }

            private int ParseUnary() {
                SkipSpaces();
                if(Accept('-')) return -ParseUnary();
                if(Accept('+')) return ParseUnary();
                if(Accept('(')) {
                    int v = ParseSum();
                    SkipSpaces();
                    if(!Accept(')')) throw new FormatException();
                    return v;
                }

                int start = pos;
                while(pos < text.Length && char.IsLetterOrDigit(text[pos])) pos++;
                if(start == pos) throw new FormatException();
                string token = text[start..pos];

                if(resolve(token) is int reg) return reg;

                int numberBase = 16;
                char last = token[^1];
                if(token.Length > 1) {
                    switch(last) {
                        case 'h' or 'H': numberBase = 16; token = token[..^1]; break;
                        case 'd': numberBase = 10; token = token[..^1]; break;
                        case 'b': numberBase = 2; token = token[..^1]; break;
                        case 'o': numberBase = 8; token = token[..^1]; break;
                    }
                }
                try {
                    return Convert.ToInt32(token, numberBase);
                } catch(Exception ex) when(ex is ArgumentException or OverflowException) {
                    throw new FormatException();
                }
            }

            private bool Accept(char c) {
                if(pos < text.Length && text[pos] == c) { pos++; return true; }
                return false;
            }

            private void SkipSpaces() {
                while(pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
            }
        }
        #endregion
    }
}

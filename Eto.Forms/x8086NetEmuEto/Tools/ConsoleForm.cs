using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Tools {
    // Log viewer: everything the emulator reports through X8086.Notify, color-coded by reason.
    // Consecutive repeats of the same message are collapsed into a single "repeated N times" line.
    public class ConsoleForm : Form {
        private const int MAX_LINES = 10_000;   // Oldest lines are discarded beyond this
        private const int MAX_PENDING = 2_000;  // Lines kept between refreshes while the emulator floods the log

        private static readonly Color BackColor = Color.FromArgb(24, 26, 27);
        private static readonly Color TimeColor = Color.FromArgb(0, 192, 0);

        public sealed class LogLine {
            public string Time { get; init; }
            public X8086.NotificationReasons Reason { get; init; }
            public string Kind => Reason.ToString().ToUpperInvariant();
            public string Message { get; init; }

            public override string ToString() => $"{Time}  {Kind,-4}  {Message}";
        }

        private readonly GridView grid;
        private readonly CheckBox autoScroll;
        private readonly ObservableCollection<LogLine> lines = [];
        private readonly UITimer refreshTimer;

        private readonly object sync = new();
        private List<LogLine> pending = [];
        private int droppedCount;
        private string lastMessage;
        private object[] lastArgs = [];
        private int repeatCount;

        public ConsoleForm() {
            Title = "x8086NetEmu Console";
            ClientSize = new Size(900, 420);
            Minimizable = true;

            Font mono = Fonts.Monospace(10);
            grid = new GridView {
                DataStore = lines,
                AllowMultipleSelection = true,
                ShowHeader = true,
                GridLines = GridLines.None,
                BackgroundColor = BackColor,
                Columns = {
                    new GridColumn { HeaderText = "Time", Width = 115, DataCell = new TextBoxCell { Binding = Binding.Property((LogLine l) => l.Time) } },
                    new GridColumn { HeaderText = "Type", Width = 50, DataCell = new TextBoxCell { Binding = Binding.Property((LogLine l) => l.Kind) } },
                    new GridColumn { HeaderText = "Message", AutoSize = true, Expand = true, DataCell = new TextBoxCell { Binding = Binding.Property((LogLine l) => l.Message) } }
                }
            };
            grid.CellFormatting += (s, e) => {
                e.Font = mono;
                e.BackgroundColor = BackColor;
                e.ForegroundColor = e.Column.HeaderText == "Time" ? TimeColor : ReasonColor(((LogLine)e.Item).Reason);
            };

            ContextMenu menu = new();
            menu.Items.Add(new ButtonMenuItem { Text = "Copy", Shortcut = Application.Instance.CommonModifier | Keys.C, Command = new Command((s, e) => CopyLines()) });
            menu.Items.Add(new ButtonMenuItem { Text = "Select All", Shortcut = Application.Instance.CommonModifier | Keys.A, Command = new Command((s, e) => grid.SelectAll()) });
            menu.Items.Add(new SeparatorMenuItem());
            menu.Items.Add(new ButtonMenuItem { Text = "Clear", Command = new Command((s, e) => Clear()) });
            grid.ContextMenu = menu;
            grid.KeyDown += (s, e) => {
                if(e.KeyData == (Application.Instance.CommonModifier | Keys.C)) { CopyLines(); e.Handled = true; }
            };

            autoScroll = new CheckBox { Text = "Auto scroll", Checked = true };
            Button copy = new() { Text = "Copy" };
            copy.Click += (s, e) => CopyLines();
            Button clear = new() { Text = "Clear" };
            clear.Click += (s, e) => Clear();

            Content = new TableLayout {
                Padding = 4,
                Spacing = new Size(4, 4),
                Rows = {
                    new TableRow(grid) { ScaleHeight = true },
                    new TableLayout(new TableRow(autoScroll, null, copy, clear)) { Spacing = new Size(6, 0) }
                }
            };

            refreshTimer = new UITimer { Interval = 0.25 };
            refreshTimer.Elapsed += (s, e) => Flush();
            refreshTimer.Start();

            X8086.Output += OnOutput;
        }

        protected override void OnClosed(EventArgs e) {
            X8086.Output -= OnOutput;
            refreshTimer.Stop();
            base.OnClosed(e);
        }

        private static Color ReasonColor(X8086.NotificationReasons reason) => reason switch {
            X8086.NotificationReasons.Info => Color.FromArgb(192, 192, 0),
            X8086.NotificationReasons.Warn => Color.FromArgb(215, 90, 215),
            X8086.NotificationReasons.Err => Color.FromArgb(255, 70, 70),
            X8086.NotificationReasons.Fck => Color.FromArgb(255, 70, 70),
            _ => Color.FromArgb(110, 130, 255)
        };

        // Raised on whatever thread notified (usually the emulation thread)
        private void OnOutput(string message, X8086.NotificationReasons reason, object[] arg) {
            arg ??= [];

            lock(sync) {
                if(message == lastMessage && SameArgs(arg)) {
                    repeatCount++;
                    return;
                }
                lastMessage = message;
                lastArgs = arg;

                if(repeatCount > 0) {
                    Enqueue(X8086.NotificationReasons.Dbg, $"^^^ Last message repeated {repeatCount} time{(repeatCount > 1 ? "s" : "")}");
                    repeatCount = 0;
                }

                string text;
                try {
                    text = arg.Length == 0 ? message : string.Format(message, arg);
                } catch(FormatException) {
                    text = message;
                }
                Enqueue(reason, text);
            }
        }

        private bool SameArgs(object[] arg) {
            if(arg.Length != lastArgs.Length) return false;
            for(int i = 0; i < arg.Length; i++) {
                if(!Equals(arg[i], lastArgs[i])) return false;
            }
            return true;
        }

        private void Enqueue(X8086.NotificationReasons reason, string text) {
            if(pending.Count >= MAX_PENDING) {
                pending.RemoveAt(0);
                droppedCount++;
            }
            pending.Add(new LogLine { Time = DateTime.Now.ToString("HH:mm:ss.fff"), Reason = reason, Message = text });
        }

        private void Flush() {
            List<LogLine> batch;
            int dropped;
            lock(sync) {
                if(pending.Count == 0) return;
                batch = pending;
                pending = [];
                dropped = droppedCount;
                droppedCount = 0;
            }

            if(dropped > 0) {
                lines.Add(new LogLine { Time = DateTime.Now.ToString("HH:mm:ss.fff"), Reason = X8086.NotificationReasons.Warn, Message = $"... {dropped} messages skipped (log flooded)" });
            }
            foreach(LogLine l in batch) lines.Add(l);

            if(lines.Count > MAX_LINES) {
                int excess = lines.Count - MAX_LINES + MAX_LINES / 10;
                for(int i = 0; i < excess; i++) lines.RemoveAt(0);
            }

            if(autoScroll.Checked == true && lines.Count > 0) grid.ScrollToRow(lines.Count - 1);
        }

        private void CopyLines() {
            IEnumerable<LogLine> selected = grid.SelectedItems.Cast<LogLine>();
            if(!selected.Any()) selected = lines;

            StringBuilder sb = new();
            foreach(LogLine l in selected) sb.AppendLine(l.ToString());
            if(sb.Length > 0) Clipboard.Instance.Text = sb.ToString();
        }

        private void Clear() {
            lock(sync) {
                pending.Clear();
                repeatCount = 0;
                lastMessage = null;
            }
            lines.Clear();
        }
    }
}

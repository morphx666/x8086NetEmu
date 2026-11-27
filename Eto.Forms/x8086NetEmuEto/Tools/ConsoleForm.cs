using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;
using R = x8086NetEmu.X8086.NotificationReasons;

namespace x8086NetEmuEto.Tools {
    // Log viewer: everything the emulator reports through X8086.Notify, including what was reported before the
    // window was opened (see LogStore). Lines can be filtered by level and by text.
    public class ConsoleForm : Form {
        public sealed class LogLine {
            public string Time { get; init; }
            public R Reason { get; init; }
            public string Message { get; init; }
            public string Level => Reason switch {
                R.Info => "INFO",
                R.Warn => "WARN",
                R.Err => "ERROR",
                R.Fck => "FATAL",
                _ => "DEBUG"
            };

            public override string ToString() => $"{Time}  {Level,-5}  {Message}";
        }

        // Collects the emulator's output from startup, whether or not the console is open. Consecutive repeats of
        // the same message are collapsed into a single "repeated N more times" line.
        public static class LogStore {
            public const int MAX_LINES = 20_000;

            private static readonly object sync = new();
            private static readonly List<LogLine> lines = [];
            private static long firstIndex;          // Sequence number of lines[0]
            private static bool started;
            private static string lastMessage;
            private static object[] lastArgs = [];
            private static int repeatCount;

            public static void Start() {
                if(started) return;
                started = true;
                X8086.Output += OnOutput;
            }

            // Raised on whatever thread notified (usually the emulation thread)
            private static void OnOutput(string message, R reason, object[] arg) {
                arg ??= [];
                lock(sync) {
                    if(message == lastMessage && SameArgs(arg)) {
                        repeatCount++;
                        return;
                    }
                    lastMessage = message;
                    lastArgs = arg;

                    if(repeatCount > 0) {
                        Add(R.Dbg, $"↑ Last message repeated {repeatCount} more time{(repeatCount > 1 ? "s" : "")}");
                        repeatCount = 0;
                    }

                    string text;
                    try {
                        text = arg.Length == 0 ? message : string.Format(message, arg);
                    } catch(FormatException) {
                        text = message;
                    }
                    Add(reason, text);
                }
            }

            private static bool SameArgs(object[] arg) {
                if(arg.Length != lastArgs.Length) return false;
                for(int i = 0; i < arg.Length; i++) {
                    if(!Equals(arg[i], lastArgs[i])) return false;
                }
                return true;
            }

            private static void Add(R reason, string text) {
                lines.Add(new LogLine { Time = DateTime.Now.ToString("HH:mm:ss.fff"), Reason = reason, Message = text });
                if(lines.Count > MAX_LINES) {
                    int n = MAX_LINES / 10;
                    lines.RemoveRange(0, n);
                    firstIndex += n;
                }
            }

            // Copies the lines from sequence number 'from' on; returns the sequence number to continue from.
            // 'trimmed' tells the caller that lines it already has were discarded meanwhile.
            public static long Fetch(long from, List<LogLine> into, out bool trimmed) {
                lock(sync) {
                    trimmed = from < firstIndex;
                    for(long i = Math.Max(from, firstIndex); i < firstIndex + lines.Count; i++) into.Add(lines[(int)(i - firstIndex)]);
                    return firstIndex + lines.Count;
                }
            }

            public static void Clear() {
                lock(sync) {
                    firstIndex += lines.Count;
                    lines.Clear();
                    repeatCount = 0;
                    lastMessage = null;
                }
            }
        }

        private readonly GridView grid;
        private readonly List<LogLine> all = [];
        private ObservableCollection<LogLine> visible = [];
        private readonly UITimer refreshTimer;
        private long nextIndex;

        private readonly Dictionary<R, CheckBox> levelFilters = [];
        private readonly Dictionary<R, int> counts = [];
        private readonly TextBox textFilter = new() { PlaceholderText = "Filter messages", Width = 220 };
        private readonly CheckBox autoScroll = new() { Text = "Auto scroll", Checked = true };
        private readonly Label status = new();

        public ConsoleForm() {
            Title = "x8086NetEmu Console";
            ClientSize = new Size(940, 460);
            MinimumSize = new Size(560, 240);

            Font mono = Theme.Mono(9.5f);
            grid = new GridView {
                DataStore = visible,
                AllowMultipleSelection = true,
                GridLines = GridLines.None,
                RowHeight = 20,
                Columns = {
                    new GridColumn { HeaderText = "Time", Width = 100, DataCell = new TextBoxCell { Binding = Binding.Delegate((LogLine l) => l.Time) } },
                    new GridColumn { HeaderText = "Level", Width = 64, DataCell = new TextBoxCell { Binding = Binding.Delegate((LogLine l) => l.Level) } },
                    new GridColumn { HeaderText = "Message", Expand = true, DataCell = new TextBoxCell { Binding = Binding.Delegate((LogLine l) => l.Message) } }
                }
            };
            grid.CellFormatting += (s, e) => {
                if(e.Item is not LogLine line) return;
                e.Font = mono;
                e.BackgroundColor = Colors.Transparent; // Row hover/selection shows through
                e.ForegroundColor = e.Column.HeaderText switch {
                    "Time" => Theme.FaintText,
                    "Level" => LevelColor(line.Reason),
                    _ => line.Reason is R.Err or R.Fck ? Theme.ErrorText : Theme.Text
                };
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

            StackLayout filters = new() { Orientation = Orientation.Horizontal, Spacing = 14, VerticalContentAlignment = VerticalAlignment.Center };
            foreach(R r in new[] { R.Info, R.Warn, R.Err, R.Dbg }) {
                CheckBox cb = new() { Checked = true };
                cb.CheckedChanged += (s, e) => RebuildVisible();
                levelFilters[r] = cb;
                filters.Items.Add(cb);
            }
            textFilter.TextChanged += (s, e) => RebuildVisible();

            Button copy = new() { Text = "Copy" };
            copy.Click += (s, e) => CopyLines();
            Button clear = new() { Text = "Clear" };
            clear.Click += (s, e) => Clear();

            Content = new TableLayout {
                Padding = new Padding(8, 8, 8, 6),
                Spacing = new Size(6, 6),
                Rows = {
                    new TableLayout(new TableRow(filters, null, textFilter, autoScroll, copy, clear)) { Spacing = new Size(8, 0) },
                    new TableRow(grid) { ScaleHeight = true },
                    status
                }
            };

            Theme.Apply(this);
            status.TextColor = Theme.DimText;

            LogStore.Start();
            Flush();
            UpdateCounts();

            refreshTimer = new UITimer { Interval = 0.25 };
            refreshTimer.Elapsed += (s, e) => Flush();
            refreshTimer.Start();
        }

        protected override void OnClosed(EventArgs e) {
            refreshTimer.Stop();
            base.OnClosed(e);
        }

        private static Color LevelColor(R reason) => reason switch {
            R.Info => Theme.Info,
            R.Warn => Theme.Warning,
            R.Err or R.Fck => Theme.Error,
            _ => Theme.Debug
        };

        // Errors and fatal errors share the "Errors" filter
        private static R FilterKey(R reason) => reason == R.Fck ? R.Err : reason;

        private void Flush() {
            List<LogLine> batch = [];
            nextIndex = LogStore.Fetch(nextIndex, batch, out bool trimmed);
            if(batch.Count == 0 && !trimmed) return;

            all.AddRange(batch);
            bool rebuild = trimmed || all.Count > LogStore.MAX_LINES;
            if(all.Count > LogStore.MAX_LINES) all.RemoveRange(0, all.Count - LogStore.MAX_LINES);

            if(rebuild) {
                // Trim in one go and rebuild the view, rather than removing rows one by one
                counts.Clear();
                foreach(LogLine l in all) counts[FilterKey(l.Reason)] = counts.GetValueOrDefault(FilterKey(l.Reason)) + 1;
                RebuildVisible();
                return;
            }

            foreach(LogLine l in batch) {
                R key = FilterKey(l.Reason);
                counts[key] = counts.GetValueOrDefault(key) + 1;
                if(Matches(l)) visible.Add(l);
            }

            UpdateCounts();
            if(autoScroll.Checked == true && visible.Count > 0) grid.ScrollToRow(visible.Count - 1);
        }

        #region Filtering
        private bool Matches(LogLine l) {
            if(levelFilters.TryGetValue(FilterKey(l.Reason), out CheckBox cb) && cb.Checked != true) return false;
            string f = textFilter.Text;
            return string.IsNullOrEmpty(f) || l.Message.Contains(f, StringComparison.OrdinalIgnoreCase);
        }

        private void RebuildVisible() {
            visible = new ObservableCollection<LogLine>(all.Where(Matches));
            grid.DataStore = visible;
            UpdateCounts();
            if(autoScroll.Checked == true && visible.Count > 0) grid.ScrollToRow(visible.Count - 1);
        }

        private void UpdateCounts() {
            levelFilters[R.Info].Text = $"Info ({counts.GetValueOrDefault(R.Info)})";
            levelFilters[R.Warn].Text = $"Warnings ({counts.GetValueOrDefault(R.Warn)})";
            levelFilters[R.Err].Text = $"Errors ({counts.GetValueOrDefault(R.Err)})";
            levelFilters[R.Dbg].Text = $"Debug ({counts.GetValueOrDefault(R.Dbg)})";
            status.Text = visible.Count == all.Count ? $"{all.Count:N0} messages" : $"{visible.Count:N0} of {all.Count:N0} messages shown";
        }
        #endregion

        private void CopyLines() {
            IEnumerable<LogLine> selected = grid.SelectedItems.Cast<LogLine>();
            if(!selected.Any()) selected = visible;

            StringBuilder sb = new();
            foreach(LogLine l in selected) sb.AppendLine(l.ToString());
            if(sb.Length > 0) Clipboard.Instance.Text = sb.ToString();
        }

        private void Clear() {
            LogStore.Clear();
            nextIndex = 0; // Fetch skips to the current start
            all.Clear();
            counts.Clear();
            RebuildVisible();
            nextIndex = LogStore.Fetch(nextIndex, [], out _);
        }
    }
}

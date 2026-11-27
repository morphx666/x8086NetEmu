using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Eto.Drawing;
using Eto.Forms;

namespace x8086NetEmuEto {
    // Dark palette shared by every window of the Eto frontend.
    //
    // Theme.Apply colors what Eto exposes (window and input backgrounds, text colors). Native widget chrome
    // (buttons, check boxes, scroll bars, grid headers, menus, title bars) is themed by each launcher:
    // a WPF style dictionary on Windows, GTK's dark theme on Linux and the dark Aqua appearance on macOS.
    public static class Theme {
        public static readonly Color Window = Color.FromArgb(0x1F, 0x1F, 0x1F);
        public static readonly Color Surface = Color.FromArgb(0x18, 0x18, 0x18);   // Grids, text areas
        public static readonly Color Input = Color.FromArgb(0x2B, 0x2B, 0x2B);
        public static readonly Color Border = Color.FromArgb(0x45, 0x45, 0x45);
        public static readonly Color Text = Color.FromArgb(0xE6, 0xE6, 0xE6);
        public static readonly Color DimText = Color.FromArgb(0x9A, 0x9A, 0x9A);
        public static readonly Color FaintText = Color.FromArgb(0x6E, 0x6E, 0x6E);
        public static readonly Color Accent = Color.FromArgb(0x37, 0x94, 0xFF);
        public static readonly Color Selection = Color.FromArgb(0x26, 0x4F, 0x78);
        public static readonly Color Invalid = Color.FromArgb(0x5A, 0x1D, 0x1D);
        public static readonly Color Valid = Color.FromArgb(0x1E, 0x4D, 0x2B);

        // Semantic colors
        public static readonly Color Info = Color.FromArgb(0x4F, 0xC1, 0xFF);
        public static readonly Color Warning = Color.FromArgb(0xE5, 0xC0, 0x7B);
        public static readonly Color Error = Color.FromArgb(0xF1, 0x4C, 0x4C);
        public static readonly Color ErrorText = Color.FromArgb(0xF4, 0x87, 0x71);
        public static readonly Color Debug = Color.FromArgb(0xB1, 0x80, 0xD7);
        public static readonly Color Success = Color.FromArgb(0x89, 0xD1, 0x85);
        public static readonly Color Changed = Color.FromArgb(0xFF, 0x9E, 0x64);

        private static readonly ConditionalWeakTable<Control, object> keep = [];
        private static Font monoFont;
        private static string monoFamily;

        // Excludes a control from Theme.Apply (it keeps the colors set by its owner)
        public static T Keep<T>(T control) where T : Control {
            keep.AddOrUpdate(control, null);
            return control;
        }

        // Applies the dark palette to a window and all its controls. Call once its content is built.
        public static void Apply(Window window) {
            window.BackgroundColor = Window;
            if(window.Content != null) Style(window.Content);
            if(window.Content is Container container) {
                foreach(Control c in container.Children) Style(c);
            }
        }

        private static void Style(Control c) {
            if(keep.TryGetValue(c, out _)) return;

            switch(c) {
                case Grid grid:
                    grid.BackgroundColor = Surface;
                    break;
                case TextArea area:
                    area.BackgroundColor = Surface;
                    area.TextColor = Text;
                    break;
                case TextBox box:
                    box.BackgroundColor = Input;
                    box.TextColor = Text;
                    break;
                case CheckBox check:
                    check.TextColor = Text;
                    break;
                case GroupBox group:
                    group.TextColor = DimText;
                    break;
                case Label label:
                    label.TextColor = Text;
                    break;
            }
        }

        // A monospaced font that looks good on each platform, falling back to the system's default one
        public static Font Mono(float size = 10) {
            if(monoFamily == null) {
                string[] preferred = ["Cascadia Mono", "Consolas", "SF Mono", "Menlo", "DejaVu Sans Mono", "Liberation Mono"];
                try {
                    var available = Fonts.AvailableFontFamilies.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    monoFamily = preferred.FirstOrDefault(available.Contains) ?? "";
                } catch {
                    monoFamily = "";
                }
            }

            if(monoFamily == "") return Fonts.Monospace(size);
            if(monoFont != null && Math.Abs(monoFont.Size - size) < 0.01) return monoFont;
            Font f = new(monoFamily, size);
            if(Math.Abs(size - 10) < 0.01) monoFont = f;
            return f;
        }
    }
}

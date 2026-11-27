using System;
using Eto.Forms;

namespace x8086NetEmuEto.Gtk {
    class Program {
        [STAThread]
        public static void Main(string[] args) {
            Application app = new(Eto.Platforms.Gtk);

            // Dark by default: use the GTK theme's dark variant for the native widgets (see x8086NetEmuEto.Theme)
            try {
                global::Gtk.Settings.Default.ApplicationPreferDarkTheme = true;
            } catch {
                // Theme without a dark variant: Theme.Apply still darkens the windows themselves
            }

            app.Run(new MainForm());
        }
    }
}

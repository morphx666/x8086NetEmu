using System;
using Eto.Forms;

namespace x8086NetEmuEto.Mac {
    class Program {
        [STAThread]
        public static void Main(string[] args) {
            Application app = new(Eto.Platforms.Mac64);

            // Dark by default: the dark Aqua appearance for all native controls (macOS 10.14+; see x8086NetEmuEto.Theme)
            try {
                MonoMac.AppKit.NSApplication.SharedApplication.Appearance =
                    MonoMac.AppKit.NSAppearance.GetAppearance(MonoMac.AppKit.NSAppearance.NameDarkAqua);
            } catch {
                // Older macOS: Theme.Apply still darkens the windows themselves
            }

            app.Run(new MainForm());
        }
    }
}

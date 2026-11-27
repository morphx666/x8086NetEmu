using System;
using Eto.Forms;

namespace x8086NetEmuEto.Wpf {
    class Program {
        [STAThread]
        public static void Main(string[] args) {
            Application app = new(Eto.Platforms.Wpf);
            DarkTheme.Install();
            app.Run(new MainForm());
        }
    }
}

using Eto.Forms;
using Eto.Serialization.Json;
using x8086NetEmu;
using x8086NetEmuEto.Renderers;

// http://pages.picoe.ca/docs/api/html/R_Project_EtoForms.htm

namespace x8086NetEmuEto {
    public class MainForm : Form {
        private X8086 cpu;
        protected Drawable Canvas;

        public MainForm() {
            JsonReader.Load(this);

            cpu = new X8086(true, true, null, X8086.Models.IBMPC_5160, "..");

            cpu.Adapters.Add(new FloppyControllerAdapter(cpu));

            cpu.Adapters.Add(new CGAEtoForms(cpu, Canvas, VideoAdapter.FontSources.BitmapFile, "asciivga.dat"));

            cpu.Adapters.Add(new KeyboardAdapter(cpu));
            cpu.Adapters.Add(new MouseAdapter(cpu));

            cpu.Adapters.Add(new SpeakerAdapter(cpu));
            var adlib = new AdlibAdapter(cpu);
            cpu.Adapters.Add(adlib);
            cpu.Adapters.Add(new SoundBlaster(cpu, adlib));

            cpu.Run();
        }
    }
}
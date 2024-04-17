using Eto.Forms;
using Eto.Serialization.Json;
using System.Xml.Linq;
using x8086NetEmu;
using x8086NetEmuEto.Renderers;

// http://pages.picoe.ca/docs/api/html/R_Project_EtoForms.htm

namespace x8086NetEmuEto {
    public class MainForm : Form {
        private X8086 cpu;
        protected Drawable Canvas;
        private readonly string basePath = @"..\..\";

        public MainForm() {
            JsonReader.Load(this);

            if(Platform.IsMac) basePath = @"..\..\..\";

            StartEmulation();
        }

        private void StartEmulation() {
            cpu = new X8086(true, true, null, X8086.Models.IBMPC_5160, basePath);

            cpu.Adapters.Add(new FloppyControllerAdapter(cpu));

            cpu.Adapters.Add(new CGAEtoForms(cpu, Canvas, VideoAdapter.FontSources.BitmapFile, "asciivga.dat"));

            cpu.Adapters.Add(new KeyboardAdapter(cpu));
            cpu.Adapters.Add(new MouseAdapter(cpu));

            cpu.Adapters.Add(new SpeakerAdapter(cpu));
            var adlib = new AdlibAdapter(cpu);
            cpu.Adapters.Add(adlib);
            cpu.Adapters.Add(new SoundBlaster(cpu, adlib));

            LoadSettings();

            cpu.Run();
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
    }
}
using System;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Tools {
    // Mounts/ejects the floppy and hard disk images. Each drive row also accepts an image file dropped from
    // the host, and "Explore" opens the image in the Disk Explorer (files can be dragged in and out there).
    // Result: true when the user asked to reboot the emulated machine.
    public class MediaManagerDialog : Dialog<bool> {
        private sealed class DriveRow {
            public int Index;
            public bool HardDisk;
            public TextBox FileName;
            public CheckBox ReadOnly;
            public Button Eject;
            public Button Explore;
        }

        private readonly X8086 cpu;
        private readonly DriveRow[] rows;
        private bool updating;

        public MediaManagerDialog(X8086 cpu) {
            this.cpu = cpu;
            Title = "Media Manager";
            Resizable = true;
            Padding = 10;

            rows = [
                new DriveRow { Index = 0, HardDisk = false },
                new DriveRow { Index = 1, HardDisk = false },
                new DriveRow { Index = 128, HardDisk = true },
                new DriveRow { Index = 129, HardDisk = true }
            ];

            TableLayout drives = new() { Spacing = new Size(6, 8) };
            foreach(DriveRow row in rows) drives.Rows.Add(BuildRow(row));

            TableLayout layout = new() {
                Spacing = new Size(6, 10),
                Rows = {
                    drives,
                    new Label {
                        Text = "Tip: drop an image file on a drive to mount it.\nAfter changing a mounted disk from the Disk Explorer, reboot so DOS doesn't keep using its cached copy.",
                        TextColor = SystemColors.DisabledText
                    },
                    null
                }
            };

            Button ok = new() { Text = "OK" };
            ok.Click += (s, e) => Close(false);
            Button reboot = new() { Text = "OK && Reboot" };
            reboot.Click += (s, e) => Close(true);

            DefaultButton = ok;
            AbortButton = ok;
            PositiveButtons.Add(reboot);
            PositiveButtons.Add(ok);

            Content = layout;
            UpdateRows();
        }

        private TableRow BuildRow(DriveRow row) {
            char letter = row.HardDisk ? (char)('C' + row.Index - 128) : (char)('A' + row.Index);
            Label label = new() { Text = $"{(row.HardDisk ? "Hard Disk" : "Floppy")} {letter}:", VerticalAlignment = VerticalAlignment.Center, Width = 90 };

            // Fixed width: WPF text boxes otherwise grow to fit long paths, widening the whole dialog
            row.FileName = new TextBox { ReadOnly = true, PlaceholderText = "(empty)", Width = 380 };
            row.ReadOnly = new CheckBox { Text = "Read only" };
            row.ReadOnly.CheckedChanged += (s, e) => {
                if(updating) return;
                DiskImage di = Image(row);
                if(di != null && di.IsReadOnly != (row.ReadOnly.Checked == true)) Mount(row, di.FileName, row.ReadOnly.Checked == true);
            };

            Button load = new() { Text = "Load..." };
            load.Click += (s, e) => Browse(row);
            row.Eject = new Button { Text = "Eject" };
            row.Eject.Click += (s, e) => { Eject(row); UpdateRows(); };
            row.Explore = new Button { Text = "Explore..." };
            row.Explore.Click += (s, e) => {
                DiskImage di = Image(row);
                if(di == null) return;
                try {
                    new DiskExplorerForm(di.FileName).ShowModal(this);
                } catch(Exception ex) {
                    MessageBox.Show(this, $"Unable to open the image: {ex.Message}", MessageBoxType.Error);
                }
            };

            // Drop a disk image from the host onto the row to mount it
            foreach(Control c in new Control[] { label, row.FileName }) {
                c.AllowDrop = true;
                c.DragOver += (s, e) => e.Effects = DroppedFile(e) != null ? DragEffects.Copy : DragEffects.None;
                c.DragDrop += (s, e) => {
                    string file = DroppedFile(e);
                    if(file != null) Mount(row, file, row.ReadOnly.Checked == true);
                };
            }

            return new TableRow(label, new TableCell(row.FileName, true), row.ReadOnly, load, row.Eject, row.Explore);
        }

        private static string DroppedFile(DragEventArgs e) {
            if(!e.Data.ContainsUris) return null;
            Uri uri = e.Data.Uris?.FirstOrDefault(u => u.IsFile);
            return uri != null && File.Exists(uri.LocalPath) ? uri.LocalPath : null;
        }

        private DiskImage Image(DriveRow row) => cpu.FloppyController?.get_DiskImage(row.Index);

        private static string FullPath(string fileName) {
            try {
                return Path.GetFullPath(fileName); // Collapses "Release\./disks/..." from relative settings paths
            } catch {
                return fileName;
            }
        }

        private void Browse(DriveRow row) {
            using OpenFileDialog dlg = new() { Title = $"Select {(row.HardDisk ? "Hard Disk" : "Floppy")} Image" };
            dlg.Filters.Add(new FileFilter("Disk Images", ".img", ".ima", ".vfd", ".flp", ".dsk"));
            dlg.Filters.Add(new FileFilter("All Files", ".*"));

            DiskImage di = Image(row);
            string dir = di != null && File.Exists(di.FileName) ? Path.GetDirectoryName(di.FileName) : Path.Combine(X8086.BasePath ?? "", "disks");
            if(Directory.Exists(dir)) dlg.Directory = new Uri(Path.GetFullPath(dir) + Path.DirectorySeparatorChar);

            if(dlg.ShowDialog(this) == DialogResult.Ok) Mount(row, dlg.FileName, row.ReadOnly.Checked == true);
        }

        private void Mount(DriveRow row, string fileName, bool readOnly) {
            Eject(row);
            try {
                DiskImage di = new(fileName, readOnly, row.HardDisk);
                if(di.Status != DiskImage.ImageStatus.DiskLoaded) {
                    di.Close();
                    MessageBox.Show(this, $"Unable to mount '{fileName}': {di.Status}", MessageBoxType.Error);
                } else {
                    cpu.FloppyController.set_DiskImage(row.Index, di);
                }
            } catch(Exception ex) {
                MessageBox.Show(this, $"Unable to mount '{fileName}': {ex.Message}", MessageBoxType.Error);
            }
            UpdateRows();
        }

        private void Eject(DriveRow row) {
            DiskImage di = Image(row);
            if(di == null) return;
            di.Close();
            cpu.FloppyController.set_DiskImage(row.Index, null);
        }

        private void UpdateRows() {
            updating = true;
            foreach(DriveRow row in rows) {
                DiskImage di = Image(row);
                row.FileName.Text = di == null ? "" : FullPath(di.FileName);
                row.FileName.ToolTip = row.FileName.Text;
                row.ReadOnly.Checked = di?.IsReadOnly ?? false;
                row.ReadOnly.Enabled = di != null;
                row.Eject.Enabled = di != null;
                row.Explore.Enabled = di != null && File.Exists(di.FileName);
            }
            updating = false;
        }
    }
}

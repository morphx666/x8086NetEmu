using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;
using static x8086NetEmu.StandardDiskFormat;

namespace x8086NetEmuEto.Tools {
    // Browses the FAT12/FAT16 file system inside a floppy or hard disk image.
    //  - Drag files/folders from the list to the host to extract them
    //  - Drop host files on the list to copy them into the selected folder of the image
    //  - Delete removes the selected files; double-click opens a file with the host's default application
    public class DiskExplorerForm : Dialog {
        public sealed class FileItem {
            public dynamic Entry { get; init; }   // FAT12.DirectoryEntry or FAT32.DirectoryEntry (boxed)
            public TreeGridItem Node { get; init; } // Directory node, for folders
            public string Name { get; init; }
            public string Size { get; init; }
            public string Type { get; init; }
            public string Modified { get; init; }
            public bool IsDirectory { get; init; }
        }

        public sealed class CodeLine {
            public string Address { get; init; }
            public string Bytes { get; init; }
            public string Mnemonic { get; init; }
            public string Operands { get; init; }
        }

        private static X8086 decoder; // Shared, never run: only used to disassemble boot code

        private readonly string imageFile;
        private readonly FileStream stream;
        private readonly bool writable;
        private readonly StandardDiskFormat sdf;
        private readonly List<int> partitionIndexes = [];
        private int partition;

        private readonly DropDown partitions = new();
        private readonly Label info = new();
        private readonly TreeGridView tree;
        private readonly GridView files;
        private readonly GridView bootCode;
        private readonly TreeGridItemCollection treeItems = [];
        private readonly Label status = new();

        private PointF mouseDownLocation;
        private bool mouseDown;
        private bool confirmedWrite;

        public DiskExplorerForm(string fileName) {
            imageFile = fileName;
            Title = $"Disk Explorer - {Path.GetFileName(fileName)}";
            ClientSize = new Size(860, 560);
            Resizable = true;
            Padding = 8;

            try {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                writable = true;
            } catch(UnauthorizedAccessException) {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            sdf = new StandardDiskFormat(stream);

            Font mono = Theme.Mono(10);

            tree = new TreeGridView {
                ShowHeader = false,
                Columns = { new GridColumn { DataCell = new TextBoxCell(0), Expand = true } }
            };
            tree.SelectedItemChanged += (s, e) => {
                if(tree.SelectedItem is TreeGridItem node) ShowDirectory(node);
            };

            files = new GridView {
                AllowMultipleSelection = true,
                AllowDrop = writable,
                Columns = {
                    new GridColumn { HeaderText = "Name", Width = 160, DataCell = new TextBoxCell { Binding = Binding.Delegate((FileItem f) => f.Name) } },
                    new GridColumn { HeaderText = "Size", Width = 80, DataCell = new TextBoxCell { Binding = Binding.Delegate((FileItem f) => f.Size), TextAlignment = TextAlignment.Right } },
                    new GridColumn { HeaderText = "Type", Width = 110, DataCell = new TextBoxCell { Binding = Binding.Delegate((FileItem f) => f.Type) } },
                    new GridColumn { HeaderText = "Modified", Expand = true, DataCell = new TextBoxCell { Binding = Binding.Delegate((FileItem f) => f.Modified) } }
                }
            };
            files.CellDoubleClick += (s, e) => { mouseDown = false; if(e.Item is FileItem f) Open(f); };
            files.KeyDown += (s, e) => {
                if(e.Key == Keys.Delete) { DeleteSelected(); e.Handled = true; }
                else if(e.Key == Keys.Enter && files.SelectedItem is FileItem f) { Open(f); e.Handled = true; }
            };
            files.MouseDown += (s, e) => { mouseDown = e.Buttons == MouseButtons.Primary; mouseDownLocation = e.Location; };
            files.MouseUp += (s, e) => mouseDown = false;
            files.MouseMove += (s, e) => {
                if(!mouseDown || e.Buttons != MouseButtons.Primary) return;
                PointF d = e.Location - mouseDownLocation;
                if(Math.Sqrt(d.X * d.X + d.Y * d.Y) < 6) return;
                mouseDown = false;
                DragOut();
            };
            files.DragOver += (s, e) => {
                e.Effects = e.Source == null && e.Data.ContainsUris && e.Data.Uris.Any(u => u.IsFile) ? DragEffects.Copy : DragEffects.None;
            };
            files.DragDrop += (s, e) => {
                if(e.Source == null && e.Data.ContainsUris) DropIn([.. e.Data.Uris.Where(u => u.IsFile).Select(u => u.LocalPath)]);
            };

            ContextMenu menu = new();
            menu.Items.Add(new ButtonMenuItem { Text = "Open", Command = new Command((s, e) => { if(files.SelectedItem is FileItem f) Open(f); }) });
            menu.Items.Add(new ButtonMenuItem { Text = "Extract To...", Command = new Command((s, e) => ExtractTo()) });
            menu.Items.Add(new ButtonMenuItem { Text = "Copy Files Into Image...", Enabled = writable, Command = new Command((s, e) => CopyInto()) });
            menu.Items.Add(new SeparatorMenuItem());
            menu.Items.Add(new ButtonMenuItem { Text = "Delete", Enabled = writable, Command = new Command((s, e) => DeleteSelected()) });
            files.ContextMenu = menu;

            bootCode = new GridView {
                Columns = {
                    new GridColumn { HeaderText = "Address", Width = 90, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => l.Address) } },
                    new GridColumn { HeaderText = "Bytes", Width = 150, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => l.Bytes) } },
                    new GridColumn { HeaderText = "Instruction", Width = 80, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => l.Mnemonic) } },
                    new GridColumn { HeaderText = "Operands", Expand = true, DataCell = new TextBoxCell { Binding = Binding.Delegate((CodeLine l) => l.Operands) } }
                }
            };
            bootCode.CellFormatting += (s, e) => {
                e.Font = mono;
                e.ForegroundColor = e.Column.HeaderText switch { "Address" => Theme.DimText, "Bytes" => Theme.FaintText, "Instruction" => Theme.Info, _ => Theme.Text };
            };

            TabControl tabs = new() {
                Pages = {
                    new TabPage { Text = "Files", Content = new Splitter { Panel1 = tree, Panel2 = files, Position = 220, FixedPanel = SplitterFixedPanel.Panel1 } },
                    new TabPage { Text = "Boot Code", Content = bootCode }
                }
            };

            Button close = new() { Text = "Close" };
            close.Click += (s, e) => Close();
            AbortButton = close;

            partitions.SelectedIndexChanged += (s, e) => {
                if(partitions.SelectedIndex >= 0) LoadPartition(partitionIndexes[partitions.SelectedIndex]);
            };

            status.Text = writable
                ? "Drag files out to extract them, drop host files in to copy them into the selected folder. Del deletes."
                : "The image file is read only: files can only be extracted.";
            

            Content = new TableLayout {
                Spacing = new Size(6, 6),
                Rows = {
                    new TableLayout(new TableRow(new Label { Text = "Partition", VerticalAlignment = VerticalAlignment.Center }, new TableCell(partitions, true))) { Spacing = new Size(6, 0) },
                    info,
                    new TableRow(tabs) { ScaleHeight = true },
                    new TableLayout(new TableRow(new TableCell(status, true), close)) { Spacing = new Size(6, 0) }
                }
            };

            Theme.Apply(this);
            status.TextColor = Theme.DimText;
            info.TextColor = Theme.DimText;

            LoadPartitions();
        }

        protected override void OnClosed(EventArgs e) {
            stream?.Dispose();
            base.OnClosed(e);
        }

        private void LoadPartitions() {
            MBR mbr = sdf.MasterBootRecord;
            for(int i = 0; i < mbr.Partitions.Length; i++) {
                SystemIds id = mbr.Partitions[i].SystemId;
                if(id is SystemIds.FAT_12 or SystemIds.FAT_16 or SystemIds.FAT_BIGDOS) {
                    partitionIndexes.Add(i);
                    partitions.Items.Add($"{i + 1}: {id}{(sdf.get_IsBootable(i) ? " BOOT" : "")} [H:{sdf.get_Heads(i)} C:{sdf.get_Cylinders(i)} S:{sdf.get_Sectors(i)}]");
                }
            }

            if(partitionIndexes.Count > 0) {
                partitions.SelectedIndex = 0;
            } else {
                // No FAT file system: a boot sector program, show its code only
                partitions.Enabled = false;
                info.Text = "No supported file system (FAT12, FAT16 or FAT16B) found";
                ShowBootCode();
            }
        }

        private void LoadPartition(int index) {
            partition = index;
            dynamic bs = sdf.get_BootSector(index);

            string volume = "";
            string oem = "", serial = "";
            try {
                volume = bs.ExtendedBIOSParameterBlock.VolumeLabel;
                serial = ((uint)bs.ExtendedBIOSParameterBlock.SerialNumber).ToString("X8");
                oem = bs.OemId;
            } catch {
                // Older boot sectors don't have an extended parameter block
            }
            if(string.IsNullOrWhiteSpace(volume)) volume = "unlabeled";
            info.Text = $"Volume: {volume}    File system: {sdf.MasterBootRecord.Partitions[index].SystemId}    OEM: {oem}    Serial: {serial}";

            treeItems.Clear();
            TreeGridItem root = new() { Values = [volume], Tag = null, Expanded = true };
            treeItems.Add(root);
            AddSubdirectories(root, sdf.get_RootDirectoryEntries(index), 0);
            tree.DataStore = treeItems;
            tree.SelectedItem = root;
            ShowDirectory(root);

            ShowBootCode();
        }

        private static bool IsDirectory(dynamic e) => (((byte)e.Attribute) & (byte)FAT12.EntryAttributes.Directory) != 0;
        private static bool IsVolumeName(dynamic e) => (((byte)e.Attribute) & (byte)FAT12.EntryAttributes.VolumeName) != 0;
        private static bool IsDeleted(dynamic e) => ((byte[])e.FileNameChars)[0] >= 0x5E; // E5h: deleted entry

        private void AddSubdirectories(TreeGridItem parent, object[] entries, int depth) {
            if(entries == null || depth > 16) return;
            foreach(dynamic d in entries.Where(e => IsDirectory(e) && !IsDeleted(e)).OrderBy(e => (string)((dynamic)e).FileName)) {
                string name = d.FileName;
                if(name is "." or "..") continue;
                TreeGridItem node = new() { Values = [name], Tag = d };
                parent.Children.Add(node);
                AddSubdirectories(node, Entries(d), depth + 1);
            }
        }

        private object[] Entries(dynamic directory) {
            int cluster = directory == null ? -1 : (int)directory.StartingClusterValue;
            return sdf.GetDirectoryEntries(partition, cluster == 0 ? -1 : cluster);
        }

        private TreeGridItem CurrentNode => tree.SelectedItem as TreeGridItem ?? (treeItems.Count > 0 ? (TreeGridItem)treeItems[0] : null);

        private void ShowDirectory(TreeGridItem node) {
            object[] entries = Entries(node.Tag);
            List<FileItem> items = [];
            if(entries != null) {
                foreach(dynamic d in entries.Where(e => IsDirectory(e) && !IsDeleted(e)).OrderBy(e => (string)((dynamic)e).FileName)) {
                    string name = d.FileName;
                    if(name is "." or "..") continue;
                    TreeGridItem child = node.Children.OfType<TreeGridItem>().FirstOrDefault(c => (string)c.Values[0] == name);
                    items.Add(new FileItem { Entry = d, Node = child, Name = name, Type = "Folder", Size = "", Modified = Modified(d), IsDirectory = true });
                }
                foreach(dynamic f in entries.Where(e => !IsDirectory(e) && !IsVolumeName(e) && !IsDeleted(e))
                                            .OrderBy(e => (string)((dynamic)e).FileExtension).ThenBy(e => (string)((dynamic)e).FileName)) {
                    string ext = f.FileExtension;
                    items.Add(new FileItem {
                        Entry = f,
                        Name = f.FullFileName,
                        Type = ext == "" ? "File" : $"{ext} File",
                        Size = $"{Math.Ceiling((uint)f.FileSize / 1024.0):N0} KB",
                        Modified = Modified(f)
                    });
                }
            }
            files.DataStore = items;
        }

        private static string Modified(dynamic e) {
            DateTime d = e.WriteDateTime;
            return $"{d.ToShortDateString()} {d.ToLongTimeString()}";
        }

        #region Extract (drag out, open, extract to)
        private string SaveFile(string directory, dynamic entry) {
            string target = Path.Combine(directory, (string)entry.FullFileName);
            File.WriteAllBytes(target, (byte[])sdf.ReadFile(partition, entry));
            try { File.SetLastWriteTime(target, (DateTime)entry.WriteDateTime); } catch { }
            return target;
        }

        private void SaveDirectory(string directory, dynamic entry, int depth = 0) {
            Directory.CreateDirectory(directory);
            if(depth > 16) return;
            object[] entries = Entries(entry);
            if(entries == null) return;
            foreach(dynamic e in entries) {
                if(IsDeleted(e) || IsVolumeName(e)) continue;
                string name = e.FileName;
                if(name.StartsWith('.')) continue;
                if(IsDirectory(e)) SaveDirectory(Path.Combine(directory, name), e, depth + 1);
                else SaveFile(directory, e);
            }
        }

        private List<string> Extract(string directory, IEnumerable<FileItem> items) {
            List<string> paths = [];
            foreach(FileItem f in items) {
                if(f.IsDirectory) {
                    string sub = Path.Combine(directory, f.Name);
                    SaveDirectory(sub, f.Entry);
                    paths.Add(sub);
                } else {
                    paths.Add(SaveFile(directory, f.Entry));
                }
            }
            return paths;
        }

        private static string NewTempDirectory() {
            string dir = Path.Combine(Path.GetTempPath(), "x8086NetEmu", Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(dir);
            return dir;
        }

        private void DragOut() {
            List<FileItem> selected = [.. files.SelectedItems.OfType<FileItem>()];
            if(selected.Count == 0) return;

            try {
                List<string> paths = Extract(NewTempDirectory(), selected);
                DataObject data = new() { Uris = [.. paths.Select(p => new Uri(p))] };
                files.DoDragDrop(data, DragEffects.Copy);
            } catch(Exception ex) {
                Prompt.Show(this, $"Unable to extract: {ex.Message}", MessageBoxType.Error);
            }
        }

        private void Open(FileItem f) {
            if(f.IsDirectory) {
                if(f.Node != null) {
                    tree.SelectedItem = f.Node;
                    ShowDirectory(f.Node);
                }
                return;
            }

            try {
                string path = SaveFile(NewTempDirectory(), f.Entry);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            } catch(Exception ex) {
                Prompt.Show(this, ex.Message, MessageBoxType.Warning);
            }
        }

        private void ExtractTo() {
            List<FileItem> selected = [.. files.SelectedItems.OfType<FileItem>()];
            if(selected.Count == 0) return;

            using SelectFolderDialog dlg = new() { Title = "Extract to" };
            if(dlg.ShowDialog(this) != DialogResult.Ok) return;
            try {
                Extract(dlg.Directory, selected);
            } catch(Exception ex) {
                Prompt.Show(this, $"Unable to extract: {ex.Message}", MessageBoxType.Error);
            }
        }
        #endregion

        #region Write (drop in, copy into, delete)
        private bool ConfirmWrite() {
            if(!writable) return false;
            if(confirmedWrite) return true;
            confirmedWrite = Prompt.Show(this,
                "Writing to disk images is still experimental and may corrupt the image (keep a backup).\n\n" +
                "If the image is mounted, reboot the emulated machine afterwards so DOS doesn't overwrite the changes with its cached copy.\n\n" +
                "Continue?",
                MessageBoxButtons.YesNo, MessageBoxType.Warning) == DialogResult.Yes;
            return confirmedWrite;
        }

        // DOS 8.3 name the file will get inside the image (mirrors DirectoryEntry.FileName/FileExtension)
        private static string ShortName(string fileName) {
            string ext = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
            string name = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant().Replace(' ', '_');
            if(name.Length > 8) name = name[..6] + "~1";
            if(ext.Length > 3) ext = ext[..3];
            return ext == "" ? name : $"{name}.{ext}";
        }

        private void DropIn(string[] paths) {
            if(paths.Length == 0 || !ConfirmWrite()) return;
            if(paths.Any(Directory.Exists)) {
                Prompt.Show(this, "Copying folders into an image is not supported yet; drop the files instead.", MessageBoxType.Warning);
                paths = [.. paths.Where(File.Exists)];
            }

            TreeGridItem node = CurrentNode;
            if(node == null) return;
            dynamic parent = node.Tag;

            foreach(string path in paths) {
                try {
                    string shortName = ShortName(path);
                    object[] existing = Entries(parent);
                    dynamic same = existing?.FirstOrDefault(e => !IsDeleted(e) && !IsDirectory(e) && string.Equals((string)((dynamic)e).FullFileName, shortName, StringComparison.OrdinalIgnoreCase));
                    if(same != null) {
                        if(Prompt.Show(this, $"{shortName} already exists. Replace it?", MessageBoxButtons.YesNo, MessageBoxType.Question) != DialogResult.Yes) continue;
                        sdf.DeleteFile(partition, parent, same);
                    }
                    sdf.WriteFile(partition, parent, new FileInfo(path));
                } catch(Exception ex) {
                    Prompt.Show(this, $"Unable to copy '{Path.GetFileName(path)}': {ex.Message}", MessageBoxType.Error);
                    break;
                }
            }

            ShowDirectory(node);
        }

        private void CopyInto() {
            using OpenFileDialog dlg = new() { Title = "Copy files into the image", MultiSelect = true };
            if(dlg.ShowDialog(this) == DialogResult.Ok) DropIn([.. dlg.Filenames]);
        }

        private void DeleteSelected() {
            List<FileItem> selected = [.. files.SelectedItems.OfType<FileItem>()];
            if(selected.Count == 0 || !writable) return;
            if(selected.Any(f => f.IsDirectory)) {
                Prompt.Show(this, "Deleting folders is not supported yet.", MessageBoxType.Warning);
                return;
            }
            if(!ConfirmWrite()) return;
            if(Prompt.Show(this, $"Delete {selected.Count} file{(selected.Count > 1 ? "s" : "")}?", MessageBoxButtons.YesNo, MessageBoxType.Question) != DialogResult.Yes) return;

            TreeGridItem node = CurrentNode;
            try {
                foreach(FileItem f in selected) sdf.DeleteFile(partition, node?.Tag, f.Entry);
            } catch(Exception ex) {
                Prompt.Show(this, $"Unable to delete: {ex.Message}", MessageBoxType.Error);
            }
            if(node != null) ShowDirectory(node);
        }
        #endregion

        private void ShowBootCode() {
            try {
                byte[] code = (byte[])((dynamic)sdf.get_BootSector(partition)).BootStrapCode;
                if(code == null || !sdf.MasterBootRecord.IsBootable) {
                    bootCode.DataStore = Array.Empty<CodeLine>();
                    return;
                }

                decoder ??= new X8086(true, true, null, X8086.Models.IBMPC_5160, X8086.BasePath);
                Array.Clear(decoder.Memory, 0, 0x10000);
                Array.Copy(code, 0, decoder.Memory, 0, code.Length);

                List<CodeLine> lines = [];
                for(int ip = 0; ip < code.Length;) {
                    X8086.Instruction ins = decoder.Decode(0, (ushort)ip);
                    int size = Math.Max(1, (int)ins.Size);
                    string operands = !string.IsNullOrEmpty(ins.Message) ? ins.Message
                                    : string.IsNullOrEmpty(ins.Parameter2) ? ins.Parameter1 : $"{ins.Parameter1}, {ins.Parameter2}";
                    lines.Add(new CodeLine {
                        Address = $"0000:{ip:X4}",
                        Bytes = string.Join(" ", (ins.Bytes ?? []).Select(b => b.ToString("X2"))),
                        Mnemonic = ins.Mnemonic,
                        Operands = operands
                    });
                    ip += size;
                }
                bootCode.DataStore = lines;
            } catch {
                bootCode.DataStore = Array.Empty<CodeLine>();
            }
        }
    }
}

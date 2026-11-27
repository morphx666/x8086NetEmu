using Eto.Drawing;
using Eto.Forms;

namespace x8086NetEmuEto.Tools {
    // Dark replacement for MessageBox.Show (the native message box can't be themed)
    public static class Prompt {
        public static DialogResult Show(Control parent, string text, MessageBoxType type = MessageBoxType.Information) =>
            Show(parent, text, MessageBoxButtons.OK, type);

        public static DialogResult Show(Control parent, string text, MessageBoxButtons buttons, MessageBoxType type = MessageBoxType.Information) {
            (string glyph, Color color, string title) = type switch {
                MessageBoxType.Warning => ("⚠", Theme.Warning, "Warning"),
                MessageBoxType.Error => ("✖", Theme.Error, "Error"),
                MessageBoxType.Question => ("?", Theme.Accent, "Question"),
                _ => ("ℹ", Theme.Info, "x8086NetEmu")
            };

            Dialog<DialogResult> dlg = new() {
                Title = title,
                Resizable = false,
                Padding = new Padding(20, 16),
                Result = DialogResult.Cancel
            };

            Label icon = Theme.Keep(new Label { Text = glyph, Font = SystemFonts.Default(22), TextColor = color, VerticalAlignment = VerticalAlignment.Top });
            Label message = new() { Text = text, Width = 420, Wrap = WrapMode.Word, VerticalAlignment = VerticalAlignment.Center };

            void AddButton(string caption, DialogResult result, bool isDefault = false, bool isAbort = false) {
                Button b = new() { Text = caption, MinimumSize = new Size(80, 0) };
                b.Click += (s, e) => dlg.Close(result);
                dlg.PositiveButtons.Add(b);
                if(isDefault) dlg.DefaultButton = b;
                if(isAbort) dlg.AbortButton = b;
            }

            switch(buttons) {
                case MessageBoxButtons.YesNo:
                    AddButton("Yes", DialogResult.Yes, isDefault: true);
                    AddButton("No", DialogResult.No, isAbort: true);
                    dlg.Result = DialogResult.No;
                    break;
                case MessageBoxButtons.YesNoCancel:
                    AddButton("Yes", DialogResult.Yes, isDefault: true);
                    AddButton("No", DialogResult.No);
                    AddButton("Cancel", DialogResult.Cancel, isAbort: true);
                    break;
                case MessageBoxButtons.OKCancel:
                    AddButton("OK", DialogResult.Ok, isDefault: true);
                    AddButton("Cancel", DialogResult.Cancel, isAbort: true);
                    break;
                default:
                    AddButton("OK", DialogResult.Ok, isDefault: true, isAbort: true);
                    dlg.Result = DialogResult.Ok;
                    break;
            }

            dlg.Content = new TableLayout(new TableRow(icon, new TableCell(message, true))) { Spacing = new Size(16, 0) };
            Theme.Apply(dlg);

            return parent != null ? dlg.ShowModal(parent) : dlg.ShowModal();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using x8086NetEmu;
using x8086NetEmuEto.Renderers;

namespace x8086NetEmuEto.Tools {
    // A snapshot of the characters on the emulated screen, in any video mode.
    //  - Text modes: the character codes are read from the adapter's text memory.
    //  - Graphics modes: there are no character codes, only pixels, so each character cell of the frame is
    //    matched against the font the BIOS draws text with in that mode (the same font DOS, the BIOS
    //    teletype and most programs use). Cells that don't contain a font glyph read as spaces.
    internal sealed class ScreenText {
        public int Columns { get; }
        public int Rows { get; }

        private readonly byte[] codes;

        private ScreenText(int columns, int rows, byte[] codes) {
            Columns = columns;
            Rows = rows;
            this.codes = codes;
        }

        public byte this[int col, int row] => codes[row * Columns + col];

        public static ScreenText Capture(X8086 cpu) {
            VideoAdapter va = cpu.VideoAdapter;
            if(va == null) return null;

            if(va.MainMode == VideoAdapter.MainModes.Text) {
                int cols = va.TextResolution.Width;
                int rows = va.TextResolution.Height;
                if(cols <= 0 || rows <= 0) return null;

                byte[] codes = new byte[cols * rows];
                for(int r = 0; r < rows; r++)
                    for(int c = 0; c < cols; c++)
                        codes[r * cols + c] = va.GetTextCharacter(c, r);
                return new ScreenText(cols, rows, codes);
            }

            if(va is not IEtoVideoAdapter eva) return null;
            return CaptureGraphics(cpu, eva.CaptureFrame(out int width, out int height), width, height);
        }

        private static ScreenText CaptureGraphics(X8086 cpu, int[] frame, int width, int height) {
            if(width <= 0 || height <= 0 || frame.Length < width * height) return null;

            byte[] mem = cpu.Memory;
            bool ega = cpu.VideoAdapter is VGAAdapter; // EGA/VGA BIOS data area fields are valid

            // Character grid as the BIOS sees it (BIOS data area: 40:4A columns, 40:84 rows - 1, 40:85 points)
            int cols = mem[0x44A] | (mem[0x44B] << 8);
            if(cols <= 0 || cols > 132) cols = width >= 640 ? 80 : 40;
            int charHeight = ega ? mem[0x485] : 8;
            if(charHeight < 8 || charHeight > 16) charHeight = 8;
            int rows = ega ? mem[0x484] + 1 : 25;
            if(rows <= 1 || rows > 60) rows = 25;

            Dictionary<string, byte> glyphs = LoadFont(cpu, ega, charHeight);
            byte[] codes = new byte[cols * rows];
            if(glyphs.Count == 0) return new ScreenText(cols, rows, codes);

            // Map the native character grid (cols*8 x rows*charHeight pixels) onto the frame, whatever its scaling
            double sx = (double)width / (cols * 8);
            double sy = (double)height / (rows * charHeight);
            int[] cell = new int[8 * charHeight];
            char[] mask = new char[charHeight];
            char[] inverted = new char[charHeight];
            Dictionary<int, int> histogram = [];

            for(int r = 0; r < rows; r++) {
                for(int c = 0; c < cols; c++) {
                    for(int y = 0; y < charHeight; y++) {
                        int fy = Math.Min(height - 1, (int)((r * charHeight + y + 0.5) * sy));
                        for(int x = 0; x < 8; x++) {
                            int fx = Math.Min(width - 1, (int)((c * 8 + x + 0.5) * sx));
                            cell[y * 8 + x] = frame[fy * width + fx];
                        }
                    }

                    // The most frequent color is the background, anything else is ink
                    histogram.Clear();
                    int background = cell[0], best = 0;
                    foreach(int p in cell) {
                        histogram[p] = histogram.TryGetValue(p, out int n) ? ++n : (n = 1);
                        if(n > best) { best = n; background = p; }
                    }

                    byte code = 0x20;
                    if(best != cell.Length) {
                        for(int y = 0; y < charHeight; y++) {
                            int bits = 0;
                            for(int x = 0; x < 8; x++) {
                                if(cell[y * 8 + x] != background) bits |= 0x80 >> x;
                            }
                            mask[y] = (char)bits;
                            inverted[y] = (char)(bits ^ 0xFF);
                        }

                        // Glyphs with more ink than paper (e.g. full blocks) have their ink as the majority color
                        if(!glyphs.TryGetValue(new string(mask), out code) &&
                           !glyphs.TryGetValue(new string(inverted), out code)) code = 0x20;
                    }
                    codes[r * cols + c] = code;
                }
            }

            return new ScreenText(cols, rows, codes);
        }

        // Builds a glyph bitmap -> character code lookup from the font the BIOS uses in graphics modes
        private static Dictionary<string, byte> LoadFont(X8086 cpu, bool ega, int charHeight) {
            byte[] mem = cpu.Memory;
            Dictionary<string, byte> glyphs = [];

            uint Vector(int interrupt) => X8086.SegmentOffsetToAbsolute(
                (ushort)(mem[interrupt * 4 + 2] | (mem[interrupt * 4 + 3] << 8)),
                (ushort)(mem[interrupt * 4] | (mem[interrupt * 4 + 1] << 8)));

            // EGA/VGA: INT 43h points at the full 256-character font for the current mode.
            // CGA: characters 0-127 live at F000:FA6E in the system BIOS, 128-255 at INT 1Fh (if installed).
            uint lowFont, highFont;
            if(ega) {
                lowFont = Vector(0x43);
                highFont = lowFont + (uint)(128 * charHeight);
            } else {
                lowFont = 0xFFA6E;
                highFont = Vector(0x1F);
            }

            // Preferred codes first, so that glyphs shared by several codes (blanks, duplicates) map to the
            // most useful one
            List<int> order = [];
            for(int i = 0x21; i <= 0x7E; i++) order.Add(i);
            for(int i = 0x80; i <= 0xFE; i++) order.Add(i);
            for(int i = 0x01; i <= 0x1F; i++) order.Add(i);
            order.Add(0x7F);

            char[] key = new char[charHeight];
            foreach(int code in order) {
                uint address = code < 128 ? lowFont + (uint)(code * charHeight) : highFont + (uint)((code - 128) * charHeight);
                if(code >= 128 && !ega && highFont == 0) continue;
                if(address + charHeight > mem.Length) continue;

                bool blank = true;
                for(int y = 0; y < charHeight; y++) {
                    key[y] = (char)mem[address + y];
                    if(key[y] != 0) blank = false;
                }
                if(blank) continue;

                glyphs.TryAdd(new string(key), (byte)code);
            }

            return glyphs;
        }

        // Stream selection from (fromCol, fromRow) to (toCol, toRow), inclusive, as host text
        public string GetText(int fromCol, int fromRow, int toCol, int toRow) {
            if((toRow < fromRow) || (toRow == fromRow && toCol < fromCol)) {
                (fromCol, toCol) = (toCol, fromCol);
                (fromRow, toRow) = (toRow, fromRow);
            }
            fromRow = Math.Clamp(fromRow, 0, Rows - 1);
            toRow = Math.Clamp(toRow, 0, Rows - 1);

            StringBuilder text = new();
            StringBuilder line = new();
            for(int row = fromRow; row <= toRow; row++) {
                int c0 = row == fromRow ? Math.Clamp(fromCol, 0, Columns - 1) : 0;
                int c1 = row == toRow ? Math.Clamp(toCol, 0, Columns - 1) : Columns - 1;

                line.Clear();
                for(int col = c0; col <= c1; col++) line.Append(CP437.ToUnicode(this[col, row]));
                text.Append(line.ToString().TrimEnd());
                if(row < toRow) text.Append(Environment.NewLine);
            }
            return text.ToString();
        }
    }

    // IBM PC character set (code page 437) <-> Unicode
    internal static class CP437 {
        private const string Table =
            " ☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼" +
            " !\"#$%&'()*+,-./0123456789:;<=>?" +
            "@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_" +
            "`abcdefghijklmnopqrstuvwxyz{|}~⌂" +
            "ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒ" +
            "áíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐" +
            "└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀" +
            "αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ ";

        private static readonly Dictionary<char, byte> reverse = BuildReverse();

        private static Dictionary<char, byte> BuildReverse() {
            Dictionary<char, byte> r = [];
            for(int i = Table.Length - 1; i >= 1; i--) r[Table[i]] = (byte)i;
            r[' '] = 0x20;
            return r;
        }

        public static char ToUnicode(byte code) => Table[code];

        public static bool TryFromUnicode(char c, out byte code) => reverse.TryGetValue(c, out code);
    }
}

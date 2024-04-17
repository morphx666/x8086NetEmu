using Eto.Drawing;
using Eto.Forms;
using System;
using System.Linq;

namespace x8086NetEmuEto.Renderers {
    internal class VideoChar {
        public readonly int CGAChar;
        public readonly Color ForeColor;
        public readonly Color BackColor;

        private Bitmap bitmap;

        public static byte[] FontBitmaps;

        public VideoChar(int c, Color fc, Color bc) {
            CGAChar = c;
            ForeColor = fc;
            BackColor = bc;
        }

        public void Paint(Graphics g, Point p, SizeF scale) {
            g.DrawImage(bitmap, p);
        }

        public void Render(int w, int h) {
            if(bitmap == null) {
                bitmap = new Bitmap(w, h, PixelFormat.Format32bppRgb);

                int offset = CGAChar * w * h;
                for(int y = 0; y < h; y++) {
                    for(int x = 0; x < w; x++) {
                        if(FontBitmaps[offset + y * w + x] == 1) {
                            bitmap.SetPixel(x, y, ForeColor);
                        } else {
                            bitmap.SetPixel(x, y, BackColor);
                        }
                    }
                }
            }
        }

        public static bool operator ==(VideoChar a, VideoChar b) {
            return a.CGAChar == b.CGAChar && a.ForeColor == b.ForeColor && a.BackColor == b.BackColor;
        }

        public static bool operator !=(VideoChar a, VideoChar b) {
            return !(a == b);
        }

        public override bool Equals(object obj) {
            return obj is VideoChar vc && this == vc;
        }
    }
}

using Eto.Drawing;
using static x8086NetEmu.Adapter;

namespace x8086NetEmuEto {
    public static class Extensions {
        public static Point ToPoint(this XPoint v) {
            return new Point(v.X, v.Y);
        }

        public static Size ToSize(this XSize v) {
            return new Size(v.Width, v.Height);
        }

        public static Rectangle ToRectangle(this XRectangle v) {
            return new Rectangle(v.X, v.Y, v.Width, v.Height);
        }

        public static Color ToColor(this XColor v) {
            return Color.FromArgb(v.R, v.G, v.B, v.A);
        }
    }
}

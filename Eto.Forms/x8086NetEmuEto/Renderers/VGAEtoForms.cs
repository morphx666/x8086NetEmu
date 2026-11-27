using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Renderers {
    // Eto presenter for the register-level VGAAdapter: the adapter generates complete frames (see
    // VGAAdapter.GetFrame); this class only copies them into a bitmap and paints it, scaled to the surface.
    public class VGAEtoForms : VGAAdapter {
        private Drawable renderControl;
        private EtoVideoInputController inputController;
        private Bitmap[] videoBMP = [new(1, 1, PixelFormat.Format32bppRgb), new(1, 1, PixelFormat.Format32bppRgb)];
        private int bmpIndex;
        private int repaintPending;

        public VGAEtoForms(X8086 cpu, Drawable renderControl) : base(cpu) {
            RenderControl = renderControl;
        }

        protected Drawable RenderControl {
            get => renderControl;
            set {
                DetachRenderControl();
                renderControl = value;

                InitAdapter();

                inputController = new EtoVideoInputController(renderControl, this);
                inputController.Attach();
                renderControl.Paint += Paint;
            }
        }

        private void DetachRenderControl() {
            if(renderControl != null) {
                inputController?.Detach();
                renderControl.Paint -= Paint;
            }
        }

        public override void CloseAdapter() {
            base.CloseAdapter();
            DetachRenderControl();
        }

        protected override void AutoSize() => ResizeRenderControl();

        protected override void ResizeRenderControl() {
            Size frmSize = new((int)(640 * Zoom), (int)(400 * Zoom));
            Application.Instance.AsyncInvoke(() => {
                if(renderControl?.FindParent(typeof(Window)) is Window frm && frm.ClientSize != frmSize) frm.ClientSize = frmSize;
            });
        }

        // Called on the emulation thread after every generated frame
        protected override void Render() {
            int[] frame = GetFrame(out int w, out int h);
            if(frame.Length < w * h || renderControl == null) return;

            int next = bmpIndex == 0 ? 1 : 0;
            if(videoBMP[next].Width != w || videoBMP[next].Height != h) {
                videoBMP[next].Dispose();
                videoBMP[next] = new Bitmap(w, h, PixelFormat.Format32bppRgb);
            }

            using(BitmapData data = videoBMP[next].Lock()) {
                for(int y = 0; y < h; y++) {
                    int row = data.Flipped ? h - 1 - y : y;
                    Marshal.Copy(frame, y * w, IntPtr.Add(data.Data, row * data.ScanWidth), w);
                }
            }
            bmpIndex = next;

            // Coalesce repaint requests so frames are skipped (not queued) while the UI thread is busy
            if(Interlocked.Exchange(ref repaintPending, 1) == 1) return;
            Application.Instance.AsyncInvoke(() => {
                Volatile.Write(ref repaintPending, 0);
                if(renderControl != null && !renderControl.IsDisposed) renderControl.Invalidate();
            });
        }

        private void Paint(object sender, PaintEventArgs e) {
            Bitmap bmp = videoBMP[bmpIndex];
            if(bmp.IsDisposed) return;

            Graphics g = e.Graphics;
            g.AntiAlias = false;
            g.ImageInterpolation = ImageInterpolation.None;

            XPaintEventArgs ex = new(g, new XRectangle((int)e.ClipRectangle.X, (int)e.ClipRectangle.Y,
                                                       (int)e.ClipRectangle.Width, (int)e.ClipRectangle.Height));
            OnPreRender(sender, ex);
            g.DrawImage(bmp, new RectangleF(PointF.Empty, renderControl.Size));
            OnPostRender(sender, ex);
        }

        public override string Description => "VGA Eto.Forms Adapter";

        public override string Name => "VGA Eto.Forms";
    }
}

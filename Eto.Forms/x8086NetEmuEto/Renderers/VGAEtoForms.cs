using System.Threading.Tasks;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Renderers {
    public abstract class VGAEtoForms : VGAAdapter {
        private Drawable renderControl;
        private EtoVideoInputController inputController;

        protected VGAEtoForms(X8086 cpu, Drawable renderControl)
            : base(cpu) {
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

        public override void InitAdapter() {
            if(!isInit) {
                base.InitAdapter();
                Task.Run(async () => {
                    while(!X8086.IsClosing && renderControl != null && !renderControl.IsDisposed) {
                        await Task.Delay((int)(2 * 1000 / VERTSYNC));
                        Application.Instance.Invoke(() => renderControl.Invalidate());
                    }
                });
            }
        }

        public override void CloseAdapter() {
            base.CloseAdapter();
            DetachRenderControl();
        }

        protected abstract void Paint(object sender, PaintEventArgs e);
    }
}

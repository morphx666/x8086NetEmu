//using Eto.Drawing;
//using Eto.Forms;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using x8086NetEmu;

//namespace x8086NetEmuEto.Renderers {
//    internal class VGAEtoForms : VGAAdapter {
//        private int blinkCounter;
//        private int frameRate = 30;
//        private List<int> cursorAddress = new List<int>();

//        private readonly string preferredFont = "Perfect DOS VGA 437";
//        private Font mFont;

//        private Color[] brushCache;
//        private Drawable mRenderControl;

//        public VGAEtoForms(X8086 cpu, 
//                           Drawable renderControl, 
//                           FontSources fontSource, 
//                           string bitmapFile = "", 
//                           bool enableWebUI = false) : base(cpu, true, enableWebUI) {

//            mFont = new Font(new FontFamily(preferredFont), 16);
//            brushCache = new Color[CGAPalette.Length];

//            RenderControl = renderControl;

//            mRenderControl.KeyDown += (sender, e) => HandleKeyDown(this, e);
//        }

//        public Drawable RenderControl {
//            get => mRenderControl;
//            set {
//                DetachRenderControl();
//                mRenderControl = value;

//                InitAdapter();

//                mRenderControl.Paint += Paint;
//            }
//        }



//        private void DetachRenderControl() {
//            if(mRenderControl != null) mRenderControl.Paint -= Paint;
//        }

//        private void Paint(object sender, PaintEventArgs e) {
//            throw new NotImplementedException();
//        }
//    }
//}

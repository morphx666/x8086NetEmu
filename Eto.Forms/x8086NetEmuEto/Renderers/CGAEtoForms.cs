using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Renderers {
    public class CGAEtoForms : CGAAdapter {
        private int blinkCounter;
        private Drawable renderControl;
        private SizeF scale = new SizeF(1, 1);

        private readonly Color[] brushCache;
        private Bitmap videoBMP = new Bitmap(1, 1, PixelFormat.Format32bppRgb);
        private readonly List<VideoChar> charsCache = new List<VideoChar>();
        private readonly Dictionary<int, Size> charSizeCache = new Dictionary<int, Size>();

        public CGAEtoForms(X8086 cpu,
                            Drawable renderControl,
                            FontSources fontSource = FontSources.BitmapFile,
                            string bitmapFontFile = "asciivga.dat") : base(cpu) {
            RenderControl = renderControl;

            string fontCGAPath = X8086.FixPath(@"misc\" + bitmapFontFile);
            if(File.Exists(fontCGAPath)) {
                VideoChar.FontBitmaps = File.ReadAllBytes(fontCGAPath);
                CellSize = new XSize(8, 16);
            }

            brushCache = new Color[CGAPalette.Length];

            SetupEventHandlers();
        }

        private void SetupEventHandlers() {
            renderControl.KeyDown += (sender, e) => {
                HandleKeyDown(this, new XKeyEventArgs(KeyToInt(e.Key), KeyToInt(e.Modifiers)));
                e.Handled = true;
            };

            renderControl.KeyUp += (sender, e) => {
                HandleKeyUp(this, new XKeyEventArgs(KeyToInt(e.Key), KeyToInt(e.Modifiers)));
                e.Handled = true;
            };
        }

        private int KeyToInt(Keys k) {
            return (int)(XEventArgs.Keys)Enum.Parse(typeof(XEventArgs.Keys), k.ToString());
        }

        public Drawable RenderControl {
            get => renderControl;
            set {
                DetachRenderControl();
                renderControl = value;

                InitAdapter();

                renderControl.Paint += Paint;
            }
        }

        private void DetachRenderControl() {
            if(renderControl != null) {
                renderControl.Paint -= Paint;
            }
        }

        public override void InitAdapter() {
            if(!isInit) {
                base.InitAdapter();
                Task.Run(async () => {
                    while(true) {
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

        protected override void AutoSize() {
            ResizeRenderControl();
        }

        protected override void OnPaletteRegisterChanged() {
            base.OnPaletteRegisterChanged();

            if(brushCache != null) {
                for(int i = 0; i < CGAPalette.Length; i++) {
                    brushCache[i] = CGAPalette[i].ToColor();
                }

                charsCache.Clear();
            }
        }

        protected override void ResizeRenderControl() {
            Size ctrlSize;

            if(MainMode == MainModes.Text) {
                ctrlSize = new Size(CellSize.Width * TextResolution.Width, CellSize.Height * TextResolution.Height);
            } else {
                ctrlSize = new Size(GraphicsResolution.Width, GraphicsResolution.Height);
            }

            Size frmSize = new Size((int)(640 * Zoom), (int)(480 * Zoom));
            Window frm = (Window)renderControl.FindParent(typeof(Window));
            //Application.Instance.Invoke(() => {
            frm.ClientSize = frmSize;
            renderControl.Size = frmSize;
            //});

            scale = new SizeF((float)frmSize.Width / ctrlSize.Width, (float)frmSize.Height / ctrlSize.Height);
        }

        private void Paint(object sender, PaintEventArgs e) {
            Graphics g = e.Graphics;

            g.AntiAlias = false;
            g.ScaleTransform(scale.Width, scale.Height);

            XPaintEventArgs ex = new XPaintEventArgs(g, new XRectangle((int)e.ClipRectangle.X,
                                                                       (int)e.ClipRectangle.Y,
                                                                       (int)e.ClipRectangle.Width,
                                                                       (int)e.ClipRectangle.Height));

            OnPreRender(sender, ex);
            lock(chars) g.DrawImage(videoBMP, 0, 0);
            OnPostRender(sender, ex);
        }

        protected override void Render() {
            if(VideoEnabled) {
                switch(MainMode) {
                    case MainModes.Text:
                        lock(chars) RenderText();
                        break;
                    case MainModes.Graphics:
                        RenderGraphics();
                        break;
                }
            }
        }

        private void RenderText() {
            int col = 0;
            int row = 0;

            // FIXME: This should be cached
            Rectangle r = new Rectangle(Point.Empty, CellSize.ToSize());

            using(Graphics g = new Graphics(videoBMP)) {
                for(UInt32 address = StartTextVideoAddress; address < EndTextVideoAddress; address += 2) {
                    byte chr = CPU.Memory[address];
                    byte atr = CPU.Memory[address + 1];

                    if(BlinkCharOn && (atr & 0b1000_0000) != 0) {
                        if(blinkCounter < BlinkRate) atr = 0;
                    }

                    RenderChar(chr, g, brushCache[atr & 0xF], brushCache[atr >> 4], r.Location);

                    if(CursorVisible && row == CursorRow && col == CursorCol) {
                        if(blinkCounter < BlinkRate) {
                            g.FillRectangle(brushCache[atr & 0xF],
                                            r.X + 0, r.Y - 1 + mCellSize.Height - (CursorEnd - CursorStart) - 1,
                                            CellSize.Width, CursorEnd - CursorStart + 1);
                        }

                        if(blinkCounter >= 2 * BlinkRate) {
                            blinkCounter = 0;
                        } else {
                            blinkCounter++;
                        }
                    }

                    r.X += CellSize.Width;
                    col++;
                    if(col == TextResolution.Width) {
                        col = 0;
                        row++;
                        if(row == TextResolution.Height) break;

                        r.X = 0;
                        r.Y += mCellSize.Height;
                    }
                }
            }
        }

        private void RenderChar(byte c, Graphics g, Color fb, Color bb, Point p) {
            VideoChar ccc = new VideoChar(c, fb, bb);
            int idx = charsCache.IndexOf(ccc);
            if(idx == -1) {
                ccc.Render(CellSize.Width, CellSize.Height);
                charsCache.Add(ccc);
                idx = charsCache.Count - 1;
            }
            charsCache[idx].Paint(g, p, scale);
        }

        private void RenderGraphics() {

        }

        protected override void InitVideoMemory(bool clearScreen) {
            base.InitVideoMemory(clearScreen);

            if(renderControl != null && GraphicsResolution.Width != 0) {
                if(clearScreen) {
                    charSizeCache.Clear();

                    for(int c = 0; c < 256; c++) MeasureChar(c);

                    // Monospace, so we can use any char
                    Size cs = charSizeCache[65];
                    CellSize = new XSize(cs.Width, cs.Height);
                }

                lock(chars) {
                    if(videoBMP != null) videoBMP.Dispose();
                    videoBMP = new Bitmap(GraphicsResolution.Width, GraphicsResolution.Height, PixelFormat.Format32bppRgb);
                }
            }
        }

        private void MeasureChar(int code) {
            charSizeCache.Add(code, CellSize.ToSize());
        }
    }
}
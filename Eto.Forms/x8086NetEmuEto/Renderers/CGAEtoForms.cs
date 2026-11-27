using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Renderers {
    public class CGAEtoForms : CGAAdapter {
        private int blinkCounter;
        private Drawable renderControl;
        private SizeF scale = new(1, 1);

        private Bitmap[] videoBMP;
        private int bmpIndex = 0;

        private readonly Color[] brushCache;
        private int[] textScanline = [];
        private int repaintPending;
        private readonly Dictionary<int, Size> charSizeCache = [];
        private readonly int[] paletteDataCache = new int[16];
        private int[] graphicsScanline = [];
        private bool paletteDataCacheDirty = true;
        private EtoVideoInputController inputController;

        public CGAEtoForms(X8086 cpu, Drawable renderControl) : base(cpu) {
            RenderControl = renderControl;

            // Text-mode font built from the BIOS ROM's character set (see TextFont)
            VideoChar.FontBitmaps = TextFont.Build(cpu);
            CellSize = new XSize(TextFont.CharWidth, TextFont.CharHeight);

            brushCache = new Color[CGAPalette.Length];

            videoBMP = [
                new(1, 1, PixelFormat.Format32bppRgb),
                new(1, 1, PixelFormat.Format32bppRgb)
            ];

        }

        public Drawable RenderControl {
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
            // Repaints are requested by Render() whenever a new frame is ready (see RequestRepaint),
            // instead of polling on a timer that was not synchronized with frame production
            if(!isInit) base.InitAdapter();
        }

        // Called from the emulation thread after each rendered frame. Coalesces requests so that,
        // if the UI thread is busy, frames are skipped rather than queued up.
        private void RequestRepaint() {
            if(Interlocked.Exchange(ref repaintPending, 1) == 1) return;

            Application.Instance.AsyncInvoke(() => {
                Volatile.Write(ref repaintPending, 0);
                if(renderControl != null && !renderControl.IsDisposed) renderControl.Invalidate();
            });
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

                paletteDataCacheDirty = true;
            }
        }

        protected override void ResizeRenderControl() {
            Size ctrlSize;

            if(MainMode == MainModes.Text) {
                ctrlSize = new(CellSize.Width * TextResolution.Width, CellSize.Height * TextResolution.Height);
            } else {
                ctrlSize = new(GraphicsResolution.Width, GraphicsResolution.Height);
            }

            Size frmSize = new((int)(640 * Zoom), (int)(400 * Zoom));
            Window frm = (Window)renderControl.FindParent(typeof(Window));

            Task.Run(async () => {
                await Task.Delay(250);

                Application.Instance.Invoke(() => {
                    if(frm.ClientSize.Width != frmSize.Width || frm.ClientSize.Height != frmSize.Height) {
                        frm.ClientSize = frmSize;
                    }

                    scale = new((float)frmSize.Width / ctrlSize.Width, (float)frmSize.Height / ctrlSize.Height);
                });
            });
        }

        private void Paint(object sender, PaintEventArgs e) {
            if(videoBMP[bmpIndex].IsDisposed) return;

            Graphics g = e.Graphics;

            g.AntiAlias = false;
            g.ImageInterpolation = ImageInterpolation.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.ScaleTransform(scale.Width, scale.Height);

            XPaintEventArgs ex = new(g, new XRectangle((int)e.ClipRectangle.X,
                                                       (int)e.ClipRectangle.Y,
                                                       (int)e.ClipRectangle.Width,
                                                       (int)e.ClipRectangle.Height));

            OnPreRender(sender, ex);
            g.DrawImage(videoBMP[bmpIndex], 0, 0);
            OnPostRender(sender, ex);
        }

        protected override void Render() {
            if(VideoEnabled) {
                int nextBmpIndex = bmpIndex == 0 ? 1 : 0;
                switch(MainMode) {
                    case MainModes.Text:
                        RenderText(nextBmpIndex);
                        break;
                    case MainModes.Graphics:
                        RenderGraphics(nextBmpIndex);
                        break;
                }
                bmpIndex = nextBmpIndex;
                RequestRepaint();
            }
        }

        // Text mode: expands each glyph straight from the font bitmap into the frame's pixel data,
        // one scanline at a time (same approach as RenderGraphics). This replaced drawing 2000
        // cached glyph images per frame through Eto's Graphics, which was too slow for 60 fps and
        // ran on the emulation thread, taking ~25% of the emulated CPU's throughput.
        private void RenderText(int bmpIndex) {
            int cw = CellSize.Width;
            int ch = CellSize.Height;
            int textCols = TextResolution.Width;
            byte[] font = VideoChar.FontBitmaps;

            using BitmapData bitmapData = videoBMP[bmpIndex].Lock();

            // Clip to the bitmap, in case a mode's text grid doesn't fit its graphics resolution
            int cols = Math.Min(textCols, GraphicsResolution.Width / cw);
            int rows = Math.Min(TextResolution.Height, GraphicsResolution.Height / ch);
            if(cols <= 0 || rows <= 0) return;

            if(paletteDataCacheDirty) {
                for(int i = 0; i < CGAPalette.Length; i++) {
                    paletteDataCache[i] = bitmapData.TranslateArgbToData(unchecked((int)CGAPalette[i].ToArgb()));
                }
                paletteDataCacheDirty = false;
            }

            int lineWidth = cols * cw;
            if(textScanline.Length < lineWidth) textScanline = new int[lineWidth];

            bool blinkFirstHalf = blinkCounter < BlinkRate;
            int cursorHeight = CursorEnd - CursorStart + 1;
            int cursorTop = ch - (CursorEnd - CursorStart) - 2; // same placement as the previous renderer
            int cursorBottom = cursorTop + cursorHeight;

            for(int row = 0; row < rows; row++) {
                for(int y = 0; y < ch; y++) {
                    int x = 0;
                    for(int col = 0; col < cols; col++) {
                        uint address = StartTextVideoAddress + (uint)((row * textCols + col) * 2);
                        int fg = 0;
                        int bg = 0;
                        byte chr = 0;

                        if(address + 1 < EndTextVideoAddress) {
                            chr = CPU.Memory[address];
                            byte atr = CPU.Memory[address + 1];

                            if(BlinkCharOn && (atr & 0b1000_0000) != 0) {
                                if(blinkFirstHalf) atr = 0;
                            }

                            fg = paletteDataCache[atr & 0xF];
                            bg = paletteDataCache[atr >> 4];

                            if(CursorVisible && blinkFirstHalf && row == CursorRow && col == CursorCol &&
                               y >= cursorTop && y < cursorBottom) {
                                bg = fg; // cursor scanline: solid foreground color
                            }
                        }

                        if(font == null) {
                            for(int px = 0; px < cw; px++) textScanline[x++] = bg;
                        } else {
                            int glyphRow = (chr * ch + y) * cw;
                            for(int px = 0; px < cw; px++) {
                                textScanline[x++] = font[glyphRow + px] == 1 ? fg : bg;
                            }
                        }
                    }

                    int pixelRow = row * ch + y;
                    int rowIndex = bitmapData.Flipped ? GraphicsResolution.Height - 1 - pixelRow : pixelRow;
                    Marshal.Copy(textScanline, 0, IntPtr.Add(bitmapData.Data, rowIndex * bitmapData.ScanWidth), lineWidth);
                }
            }

            // One blink cycle = BlinkRate frames on + BlinkRate frames off (60 Hz frames)
            blinkCounter = blinkCounter >= 2 * BlinkRate ? 0 : blinkCounter + 1;
        }

        private void RenderGraphics(int bmpIndex) {
            int b;
            int xDiv = PixelsPerByte == 4 ? 2 : 3;
            using BitmapData bitmapData = videoBMP[bmpIndex].Lock();

            if(paletteDataCacheDirty) {
                for(int i = 0; i < CGAPalette.Length; i++) {
                    paletteDataCache[i] = bitmapData.TranslateArgbToData(unchecked((int)CGAPalette[i].ToArgb()));
                }

                paletteDataCacheDirty = false;
            }

            for(int y = 0; y < GraphicsResolution.Height; y++) {
                int cy = ((y >> 1) * 80) + ((y & 1) * 0x2000);
                for(int x = 0; x < GraphicsResolution.Width; x++) {
                    b = CPU.Memory[mStartGraphicsVideoAddress + cy + (x >> xDiv)];

                    if(PixelsPerByte == 4) {
                        switch(x & 3) {
                            case 3:
                                b &= 3;
                                break;
                            case 2:
                                b = (b >> 2) & 3;
                                break;
                            case 1:
                                b = (b >> 4) & 3;
                                break;
                            case 0:
                                b = (b >> 6) & 3;
                                break;
                        }
                    } else {
                        b = (b >> (7 - (x & 7))) & 1;
                    }

                    graphicsScanline[x] = paletteDataCache[b];
                }

                int rowIndex = bitmapData.Flipped ? GraphicsResolution.Height - 1 - y : y;
                Marshal.Copy(graphicsScanline, 0, IntPtr.Add(bitmapData.Data, rowIndex * bitmapData.ScanWidth), GraphicsResolution.Width);
            }
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

                if(videoBMP[0].Size != GraphicsResolution.ToSize()) {
                    videoBMP[0]?.Dispose();
                    videoBMP[1]?.Dispose();
                    videoBMP = [
                        new(GraphicsResolution.Width, GraphicsResolution.Height, PixelFormat.Format32bppRgb),
                        new(GraphicsResolution.Width, GraphicsResolution.Height, PixelFormat.Format32bppRgb)
                    ];
                    graphicsScanline = new int[GraphicsResolution.Width];
                    bmpIndex = 0;
                    paletteDataCacheDirty = true;
                }
            }
        }

        private void MeasureChar(int code) {
            charSizeCache.Add(code, CellSize.ToSize());
        }
    }
}
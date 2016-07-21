/*
 *  Cga.java
 *  Joris van Rantwijk
 */

package retro;
import java.awt.Color;
import java.awt.image.*;

/**
 * Emulation of the IBM Color Graphics Adapter.
 */
public class Cga implements IOPortHandler
{

    public static final int BASEADDR = 0xb8000;
    public static final int MEMSIZE =  0x04000; // 16 kByte
    public static final int BASEPORT = 0x03d0;

    public static final int VERTSYNC  = 60;
    public static final int HORIZSYNC = 60 * 250;

    /** Standard CGA color palette (source: Wikipedia) */
    public static final Color[] cgaBasePalette = {
        Color.black,            //  0 - black
        new Color(0x0000aa),    //  1 - blue
        new Color(0x00aa00),    //  2 - green
        new Color(0x00aaaa),    //  3 - cyan
        new Color(0xaa0000),    //  4 - red
        new Color(0xaa00aa),    //  5 - magenta
        new Color(0xaa5500),    //  6 - brown
        new Color(0xaaaaaa),    //  7 - white (light gray)
        new Color(0x555555),    //  8 - dark gray
        new Color(0x5555ff),    //  9 - bright blue
        new Color(0x55ff55),    // 10 - bright green
        new Color(0x55ffff),    // 11 - bright cyan
        new Color(0xff5555),    // 12 - bright red
        new Color(0xff55ff),    // 13 - bright magenta
        new Color(0xffff55),    // 14 - yellow
        Color.white };          // 15 - bright white

    // Our friend simulation components
    private Scheduler sched;
    private Memory mem;
    private Display display;

    // State of the CGA controller
    private int crtIndex;
    private byte[] crtReg;
    private byte modeReg;
    private byte colorReg;

    private int videoMode;
    private boolean blinkEnabled;
    private boolean enabledVideo;
    private boolean fullUpdate;
    private boolean cursorChanged;
    private boolean paletteChanged;

    // Graphics framebuffer conversion
    private ColorModel targetColorModel;
    private int[] colorTable;
    private int[] pixels;


    /** Construct and initialize the CGA emulator. */
    public Cga(Scheduler s, Memory m, Display d, long updateperiod)
    {
        // We rely on 256-byte page size
        if (Memory.PAGESHIFT != 8)
            throw new RuntimeException("Memory.PAGESHIFT must be 8");

        sched = s;
        mem = m;
        display = d;
        crtReg = new byte[32];
        reset();

        Scheduler.Task tsk = new Scheduler.Task() {
            public void run() {
                updateDisplay();
            }
        };
        sched.runTaskEach(tsk, updateperiod);
    }


    /** Construct and initialize the CGA emulater
        with 8 Hz default update rate. */
    public Cga(Scheduler s, Memory m, Display d)
    {
        this(s, m, d, Scheduler.CLOCKRATE / 8);
    }


    /** Reset CGA card.  */
    public void reset()
    {
        modeReg = 0x29;
        colorReg = 0;
        crtReg[0] = 0x71;
        crtReg[1] = 0x50;
        crtReg[2] = 0x5A;
        crtReg[3] = 0x0A;
        crtReg[4] = 0x1F;
        crtReg[5] = 0x06;
        crtReg[6] = 0x19;
        crtReg[7] = 0x1C;
        crtReg[8] = 0x02;
        crtReg[9] = 0x07;
        crtReg[10] = 0x06;
        crtReg[11] = 0x07;
        for (int i = 12; i < 32; i++)
            crtReg[i] = 0;

        videoMode = 1;
        enabledVideo = true;
        fullUpdate = true;

        if (display != null) {
            display.setTextMode(80, 25);
            display.enableVideo(true);
            updateDisplay();
        }
    }


    /** Handle write operations from the CPU. */
    public void outb(int v, int port)
    {
        if ((port & 0x09) == 0) {
            // CRT index register
            crtIndex = v;
        } else if ((port & 0x09) == 0x01) {
            // CRT data register
            crtReg[crtIndex & 31] = (byte)v;
            if ((crtIndex&31) == 14 || (crtIndex&31) == 15)
                cursorChanged = true;
            else
                fullUpdate = true;
        } else if ((port & 0x0f) == 0x08) {
            // CGA mode register
            modeReg = (byte)v;
            updateModeReg();
        } else if ((port & 0x0f) == 0x09) {
            // CGA color register
            colorReg = (byte)v;
            paletteChanged = true;
        }
    }


    /** Handle read operations from the CPU. */
    public int inb(int port)
    {
        if ((port & 0x09) == 0) {
            // CRT index register
            return crtIndex;
        } else if ((port & 0x09) == 0x01) {
            // CRT data register
            return crtReg[crtIndex&31] & 0xff;
        } else if ((port & 0x0f) == 0x08) {
            // CGA mode register
            return modeReg & 0xff;
        } else if ((port & 0x0f) == 0x09) {
            // CGA color register
            return colorReg & 0xff;
        } else if ((port & 0x0f) == 0x0a) {
            // CGA status register
            return statusReg();
        } else {
            return 0xff;
        }
    }


    /** Handle a change in the CGA video mode register. */
    private final void updateModeReg()
    {
        int newmode;
        if ((modeReg & 0x02) == 0)
            newmode = ((modeReg & 0x01) == 0) ? 0 : 1;
        else
            newmode = ((modeReg & 0x10) == 0) ? 2 : 3;
        if (newmode != videoMode && display != null) {
            videoMode = newmode;
            targetColorModel = null;
            pixels = null;
            switch (videoMode) {
              case 0: // 40x25 text mode
                display.setTextMode(40, 25);
                break;
              case 	1: // 80x25 text mode
                display.setTextMode(80, 25);
                break;
              case 2: // 320x200 (color) graphics mode
                display.setGraphicsMode(320, 200);
                pixels = new int[320 * 200];
                break;
              case 3: // 640x200 (mono) graphics mode
                display.setGraphicsMode(640, 200);
                pixels = new int[640 * 200];
                break;
            }
            fullUpdate = true;
            paletteChanged = true;
            updateDisplay();
        }

        boolean newBlink = ((modeReg & 0x20) != 0);
        if (newBlink != blinkEnabled && videoMode < 2) {
            blinkEnabled = newBlink;
            if (display != null)
            display.setBlinkMode(blinkEnabled);
        }

        boolean newVideo = ((modeReg & 0x08) != 0);
        if (newVideo != enabledVideo) {
            enabledVideo = newVideo;
            if (display != null)
                display.enableVideo(enabledVideo);
        }
    }


    /** Return CGA status register. */
    private final int statusReg()
    {
        final long ht = Scheduler.CLOCKRATE / HORIZSYNC;
        final long vt = (Scheduler.CLOCKRATE / HORIZSYNC) * (HORIZSYNC / VERTSYNC);

        // Determine current retrace state
        long t = sched.getCurrentTime();
        boolean hretrace = (t % ht) <= (ht / 10);
        boolean vretrace = (t % vt) <= (vt / 10);

        return (hretrace ? 0x01 : 0) |
               (vretrace ? 0x08 : 0);
    }


    /**
     * (Re)Construct a lookup table that maps framebuffer pixel values
     * into pixel values in the target <code>ColorModel</code>.
     */
    private final void updatePalette()
    {
        // Construct RGB palette table
        Color[] colors;
        if (videoMode == 2) {
            // 320x200, 4 colors
            // background: configurable through border color
            // foreground: palette selectable from fixed combinations
            int intense = (colorReg & 0x10) >> 1;
            int pal1 = ((colorReg >> 5)) & (~(modeReg >> 2)) & 1;
            int pal2 = (~colorReg >> 5) & (~(modeReg >> 2)) & 1;
            colors = new Color[] {
                cgaBasePalette[colorReg & 0x0f],
                cgaBasePalette[(3 ^ pal2) | intense],
                cgaBasePalette[(4 ^ pal1) | intense],
                cgaBasePalette[(7 ^ pal2) | intense] };
        } else {
            // 640x200, 2 colors
            // background: fixed black
            // foreground: configurable through border color
            colors = new Color[] {
              cgaBasePalette[0],
              cgaBasePalette[colorReg & 0x0f] };
        }

        // Pick a target color model
        int ncolors = colors.length;
        targetColorModel = display.getPreferredColorModel();
        if ( !(targetColorModel instanceof IndexColorModel) &&
             !(targetColorModel instanceof PackedColorModel) ) {
            // Unsupported color model (probably ComponentColorModel);
            // construct an IndexColorModel instead.
            byte[] cr = new byte[ncolors];
            byte[] cg = new byte[ncolors];
            byte[] cb = new byte[ncolors];
            for (int i = 0; i < ncolors; i++) {
                cr[i] = (byte) colors[i].getRed();
                cg[i] = (byte) colors[i].getGreen();
                cb[i] = (byte) colors[i].getBlue();
            }
            targetColorModel = new IndexColorModel(8, ncolors, cr, cg, cb);
        }

        // Map RGB colors to the target ColorModel
        colorTable = new int[ncolors];
        switch (targetColorModel.getTransferType()) {
          case DataBuffer.TYPE_BYTE:
            byte[] tbyte = new byte[1];
            for (int i = 0; i < ncolors; i++) {
                targetColorModel.getDataElements(colors[i].getRGB(), tbyte);
                colorTable[i] = tbyte[0] & 0xff;
            }
            break;
          case DataBuffer.TYPE_USHORT:
            short[] tshort = new short[1];
            for (int i = 0; i < ncolors; i++) {
                targetColorModel.getDataElements(colors[i].getRGB(), tshort);
                colorTable[i] = tshort[0] & 0xffff;
            }
            break;
          case DataBuffer.TYPE_INT:
            int[] tint = new int[1];
            for (int i = 0; i < ncolors; i++) {
                targetColorModel.getDataElements(colors[i].getRGB(), tint);
                colorTable[i] = tint[0];
            }
            break;
          default:
            throw new IllegalArgumentException("Unsupported target ColorModel");
        }
    }


    /** Push state changes to the display object. */
    private final void updateDisplay()
    {
        // This only makes sense if we have a display attached
        if (display == null)
            return;

        // Read start address register
        int startOffset = ((crtReg[12] & 0x3f) << 8) | (crtReg[13] & 0xff);

        // Poll update flag
        boolean doUpdate = fullUpdate;
        fullUpdate = false;

        if (videoMode == 0 || videoMode == 1) {

            // Text modes:
            // Even addresses are for characters;
            // odd addresses are for attributes.

            int w = (videoMode == 0) ? 40 : 80;

            // Process palette changes
            if (paletteChanged) {
            	paletteChanged = false;
                colorTable = null;
                display.setTextPalette(cgaBasePalette, (colorReg & 0x10) == 0x10);
            }

            // Update cursor position
            if (cursorChanged) {
                cursorChanged = false;
                int p = ((crtReg[14] & 0x3f) << 8) | (crtReg[15] & 0xff);
                p = (p - startOffset) & 8191;
                if (p < 0)
                    display.setCursorLocation(0, 50);
                else
                    display.setCursorLocation(p % w, p / w);
            }

            // Determine modified region on the screen.
            // This assumes that the dirty-block size is 256 bytes (PAGESHIFT == 8).
            startOffset &= 8191;
            int ymodstart = 0, ymodend = 25;
            if (!doUpdate) {
                ymodend = 0;
                long dirtymask =
                  ((long)mem.dirty[(BASEADDR >> 13)] & 0xffffffffL) |
                  ((long)mem.dirty[(BASEADDR >> 13) + 1] << 32);
                if (dirtymask != 0) {
                    int k = startOffset >>> 7;
                    while ((dirtymask & (1L << (k & 63))) == 0) k++;
                    ymodstart = ((k << 7) - startOffset) / w;
                    if (ymodstart < 0) ymodstart = 0;
                    k = (startOffset + 25 * w - 1) >>> 7;
                    while ((dirtymask & (1L << (k & 63))) == 0) k--;
                    ymodend = 1 + ((k << 7) + 127 - startOffset) / w;
                    if (ymodend > 25) ymodend = 25;
                    doUpdate = (ymodstart < ymodend);
                }
            }

            // Push video memory to display
            if (doUpdate) {
                int offs = (startOffset << 1) + 2 * ymodstart * w;
                display.updateText(ymodstart, ymodend - ymodstart,
                  mem.mem, BASEADDR + offs, BASEADDR, BASEADDR + MEMSIZE);
            }

        } else {

            // Graphics modes:
            // Even scanlines are in the first 8 kB of video memory;
            // odd scanlines are in the second 8 kB.
            // Each scanline takes 80 bytes (320 x 2 bits or 640 x 1 bit).

            // Process palette changes
            if (paletteChanged) {
                paletteChanged = false;
                doUpdate = true;
                updatePalette();
            }

            // Determine modified region on the screen.
            // This assumes that the dirty-block size is 256 bytes (PAGESHIFT == 8).
            startOffset &= 4095;
            int ymodstart = 0, ymodend = 200;
            if (!doUpdate) {
                ymodend = 0;
                int dirtymask = mem.dirty[(BASEADDR >> 13)] |
                                mem.dirty[(BASEADDR >> 13) + 1];
                if (dirtymask != 0) {
                    int k = startOffset >>> 7;
                    while ((dirtymask & (1 << (k & 31))) == 0) k++;
                    ymodstart = (((k << 7) - startOffset) / 40) << 1;
                    if (ymodstart < 0) ymodstart = 0;
                    k = (startOffset + 3999) >>> 7;
                    while ((dirtymask & (1 << (k & 31))) == 0) k--;
                    ymodend = (1 + ((k << 7) + 127 - startOffset) / 40) << 1;
                    if (ymodend > 200) ymodend = 200;
                    doUpdate = (ymodstart < ymodend);
                }
            }

            if (doUpdate && videoMode == 2) {
                // Convert 320x200 framebuffer to pixels
                int addr = (startOffset << 1) + 40 * ymodstart;
                for (int y = ymodstart; y < ymodend; y += 2) {
                    int k = y * 320;
                    for (int x = 0; x < 80; x++) {
                        // even scanline
                        byte b = mem.mem[BASEADDR + addr];
                        pixels[k] = colorTable[(b >>> 6) & 3];
                        pixels[k+1] = colorTable[(b >>> 4) & 3];
                        pixels[k+2] = colorTable[(b >>> 2) & 3];
                        pixels[k+3] = colorTable[b & 3];
                        // odd scanline
                        b = mem.mem[BASEADDR + 8192 + addr];
                        pixels[k+320] = colorTable[(b >>> 6) & 3];
                        pixels[k+321] = colorTable[(b >>> 4) & 3];
                        pixels[k+322] = colorTable[(b >>> 2) & 3];
                        pixels[k+323] = colorTable[b & 3];
                        // next address
                        addr = (addr + 1) & 8191;
                        k += 4;
                    }
                }
                // Push pixels to display
                display.updateGraphics(ymodstart, ymodend - ymodstart,
                  targetColorModel, pixels, 320 * ymodstart, 320);
            } else if (doUpdate && videoMode == 3) {
                // Convert 640x200 framebuffer to pixels
                int addr = (startOffset << 1) + 40 * ymodstart;
                for (int y = ymodstart; y < ymodend; y += 2) {
                    int k = y * 640;
                    for (int x = 0; x < 80; x++) {
                        // even scanline
                        byte b = mem.mem[BASEADDR + addr];
                        pixels[k] = colorTable[(b >>> 7) & 1];
                        pixels[k+1] = colorTable[(b >>> 6) & 1];
                        pixels[k+2] = colorTable[(b >>> 5) & 1];
                        pixels[k+3] = colorTable[(b >>> 4) & 1];
                        pixels[k+4] = colorTable[(b >>> 3) & 1];
                        pixels[k+5] = colorTable[(b >>> 2) & 1];
                        pixels[k+6] = colorTable[(b >>> 1) & 1];
                        pixels[k+7] = colorTable[b & 1];
                        // odd scanline
                        b = mem.mem[BASEADDR + 8192 + addr];
                        pixels[k+640] = colorTable[(b >>> 7) & 1];
                        pixels[k+641] = colorTable[(b >>> 6) & 1];
                        pixels[k+642] = colorTable[(b >>> 5) & 1];
                        pixels[k+643] = colorTable[(b >>> 4) & 1];
                        pixels[k+644] = colorTable[(b >>> 3) & 1];
                        pixels[k+645] = colorTable[(b >>> 2) & 1];
                        pixels[k+646] = colorTable[(b >>> 1) & 1];
                        pixels[k+647] = colorTable[b & 1];
                        // next address
                        addr = (addr + 1) & 8191;
                        k += 8;
                    }
                }
                // Push pixels to display
                display.updateGraphics(ymodstart, ymodend - ymodstart,
                  targetColorModel, pixels, 640 * ymodstart, 640);
            }

        }

        // Clear dirty flags on video memory
        mem.dirty[(BASEADDR >> 13)] = 0;
        mem.dirty[(BASEADDR >> 13) + 1] = 0;

    }

}

/* end */

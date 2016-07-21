/*
 *  TerminalCanvas.java
 *  Joris van Rantwijk
 */

package retro;
import java.awt.*;
import java.awt.image.*;
import java.awt.event.KeyListener;
import java.awt.event.KeyEvent;

/**
 * An AWT component which draws screen output and grabs keyboard events.
 */
public class TerminalCanvas extends Canvas implements Display, KeyListener
{

    protected static Logger log = Logger.getLogger("TerminalCanvas");

    private ExternalInputHandler inputHandler;

    private volatile Dimension displaySize;
    private boolean videoEnabled;
    private boolean isTextMode;
    private int width, height;

    // text mode
    private int cursorX, cursorY;
    private byte[] textbuffer;
    private char[] codepage;
    private Color[] textPalette;
    private int charWidth, charHeight, charBaseline;

    // graphics mode
    int[] graphicsPixels;
    int graphicsOffset;
    int graphicsStride;
    ColorModel graphicsColorModel;
    MemoryImageSource graphicsImageSource;
    Image graphicsImage;
    int graphScaleX, graphScaleY;


    /** Create and initialize the canvas. */
    public TerminalCanvas()
    {
        codepage = (char[]) Codepage.cp437_tbl.clone();
        codepage[0] = ' ';

        videoEnabled = true;
        isTextMode = true;
        width = 80;
        height = 25;
        textbuffer = new byte[2 * width * height];
        cursorX = 0;
        cursorY = 0;

        setBackground(Color.black);
        setFont(new Font("Monospaced", Font.BOLD, 12));

        // Handle keyboard events
        addKeyListener(this);

        // We want to handle TAB events ourselves
        // Unfortunately this requires JDK 1.4
        // Unfortunately this does not work on Kaffe and GCC
        setFocusTraversalKeysEnabled(false);
    }


    /** Register the handler for keyboard input events. */
    public void setInputHandler(ExternalInputHandler h)
    {
        inputHandler = h;
    }


    /** Change the font for text mode display. */
    public synchronized void setFont(Font f)
    {
        super.setFont(f);
        FontMetrics fm = getFontMetrics(getFont());
        charWidth = fm.charWidth(' ');
        charHeight = fm.getHeight();
        charBaseline = fm.getAscent() + fm.getLeading() / 2;
        if (isTextMode) {
            displaySize = new Dimension(width * charWidth, height * charHeight);
            invalidate();
            repaint();
        }
    }


    public Dimension getPreferredSize()
    {
        return displaySize;
    }


    public Dimension getMaximumSize()
    {
        return displaySize;
    }


    /** Paint handler. */
    public synchronized void paint(Graphics g)
    {
        if (isTextMode) {
            // paint in text mode
            if (textPalette == null)
                return;
            Rectangle r = g.getClipBounds();
            log.debug("cliprect=" + r);
            int minX = r.x / charWidth;
            int minY = r.y / charHeight;
            int endX = (r.x + r.width - 1) / charWidth + 1;
            int endY = (r.y + r.height - 1) / charHeight + 1;
            if (endX > width) endX = width;
            if (endY > height) endY = height;
            g.setPaintMode();
            g.setFont(getFont());
            paintText(g, minX, minY, endX, endY);
        } else {
            // paint in graphics mode
            Rectangle r = g.getClipBounds();
            log.debug("cliprect=" + r);
            int minx = r.x / graphScaleX;
            int miny = r.y / graphScaleY;
            int endx = (r.x + r.width - 1) / graphScaleX + 1;
            int endy = (r.y + r.height - 1) / graphScaleY + 1;
            if (endx > width) endx = width;
            if (endy > height) endy = height;
            paintGraphics(g, minx, miny, endx, endy);
        }
    }


    /** Paint subhandler for text modes. */
    private final void paintText(Graphics g, int minX, int minY, int endX, int endY)
    {
        char[] s = new char[width];

        // Draw text
        for (int y = minY; y < endY; y++) {
            int bp = 2 * width * y;
            int yTop = y * charHeight;
            int yBase = yTop + charBaseline;

            for (int x = minX; x < endX; ) {
                int tx = x;
                byte a = textbuffer[bp + 2*x + 1];
                do {
                    s[x] = codepage[textbuffer[bp + 2*x] & 0xff];
                    x++;
                } while (x < endX && textbuffer[bp + 2*x + 1] == a);
                g.setColor(textPalette[(a & 0xf0) >> 4]);
                g.fillRect(tx * charWidth, yTop, (x - tx) * charWidth, charHeight);
                g.setColor(textPalette[a & 0x0f]);
                g.drawChars(s, tx, x - tx, tx * charWidth, yBase);
            }
        }

        // Draw cursor
        int cx = cursorX, cy = cursorY;
        if (cx >= minX && cx < endX && cy >= minY && cy < endY) {
            byte a = textbuffer[2 * (width * cy + cx) + 1];
            g.setColor(textPalette[a & 0x0f]);
            g.drawRect(cx * charWidth, cy * charHeight, charWidth - 1, charHeight - 1);
        }
    }


    /** Paint subhandler for graphics modes. */
    public final void paintGraphics(Graphics g, int minx, int miny, int endx, int endy)
    {
        if (graphicsImage == null)
            return;
        g.drawImage(graphicsImage,
                    minx * graphScaleX, miny * graphScaleY,
                    endx * graphScaleX, endy * graphScaleY,
                    minx, miny, endx, endy, null);
    }


    /** Redraw the component. */
    public void update(Graphics g)
    {
        // Call normal paint handler without erasing background
        paint(g);
    }


    /** Enable or disable video output.
        TODO: actually implement this. */
    public void enableVideo(boolean enable)
    {
        videoEnabled = enable;
        // TODO : implement this
    }


    /** Set parent to its preferred size after the display size changed. */
    private void changeComponentSize()
    {
        invalidate();
        getParent().setSize(getParent().getPreferredSize());
        getParent().validate();
        repaint();
    }


    /** Prepare terminal for text display with specified height and width. */
    public synchronized void setTextMode(int w, int h)
    {
        isTextMode = true;
        graphicsPixels = null;
        graphicsColorModel = null;
        graphicsImageSource = null;
        graphicsImage = null;
        width = w;
        height = h;
        textbuffer = new byte[2 * width * height];
        displaySize = new Dimension(width * charWidth, height * charHeight);
        changeComponentSize();
    }


    /** Update position of the text mode cursor. */
    public void setCursorLocation(int x, int y)
    {
        // Repaint an area containing old and new cursor position
        int minX = 0, endX = 0, minY = 0, endY = 0;
        if (cursorX >= 0 && cursorX < width && cursorY >= 0 && cursorY < height) {
            minX = cursorX; endX = cursorX + 1;
            minY = cursorY; endY = cursorY + 1;
        }
        if (x >= 0 && x < width && y >= 0 && y < height) {
            if (x < minX || endX == 0) minX = x;
            if (x >= endX) endX = x + 1;
            if (y < minY || endY == 0) minY = y;
            if (y >= endY) endY = y + 1;
        }
        cursorX = x;
        cursorY = y;
        if (endX > minX && endY > minY)
            repaint(minX * charWidth, minY * charHeight,
                    (endX - minX) * charWidth, (endY - minY) * charHeight);
    }


    /** Enable or disable text mode blinking.
        TODO: implement blinking */
    public void setBlinkMode(boolean enable)
    {
        // TODO
    }


    /** Update text mode palette.
        TODO: implement intenseBackground */
    public void setTextPalette(Color[] palette, boolean intenseBackground)
    {
        // TODO : use intenseBackground info
        textPalette = palette;
        repaint();
    }


    /** Update content on a region of the display. */
    public void updateText(int y, int h, byte[] data, int off, int wrapstart, int wrapend)
    {
        log.debug("updateText y=" + y + " h=" + h);
        int p = 2 * y * width;
        int n = 2 * h * width;
        if (wrapend > 0 && off + n > wrapend) {
            System.arraycopy(data, off, textbuffer, p, wrapend - off);
            System.arraycopy(data, wrapstart, textbuffer, p + wrapend - off, off + n - wrapend);
        } else {
            System.arraycopy(data, off, textbuffer, p, n);
        }
        repaint(0, y * charHeight, charWidth * width, h * charHeight);
    }


    /** Prepare the terminal for graphics display. */
    public synchronized void setGraphicsMode(int w, int h)
    {
        isTextMode = false;
        textbuffer = null;
        width = w;
        height = h;
        graphScaleX = 1;
        graphScaleY = 1;
        if (width < 400) graphScaleX = 2;
        if (height < 400) graphScaleY = 2;
        displaySize = new Dimension(width * graphScaleX, height * graphScaleY);
        graphicsPixels = new int[width * height];
        graphicsOffset = 0;
        graphicsStride = w;
        graphicsColorModel = getPreferredColorModel();
        graphicsImageSource = new MemoryImageSource(w, h,
          graphicsColorModel, graphicsPixels, 0, w);
        graphicsImageSource.setAnimated(true);
        graphicsImageSource.setFullBufferUpdates(false);
        graphicsImage = createImage(graphicsImageSource);
        changeComponentSize();
    }


    /** Update graphics on a region of the screen. */
    public void updateGraphics(int y, int h, ColorModel cm, int[] pixels, int off, int scanstride)
    {
        log.debug("updateGraphics y=" + y + " h=" + h);
        if (pixels != graphicsPixels || cm != graphicsColorModel ||
            off != graphicsOffset + y * scanstride || scanstride != graphicsStride) {
            // Switch to a different pixel buffer
            graphicsPixels = pixels;
            graphicsColorModel = cm;
            graphicsOffset = off - y * scanstride;
            graphicsStride = scanstride;
            graphicsImageSource.newPixels(pixels, cm, graphicsOffset, scanstride);
        } else {
            // Incremental update
            graphicsImageSource.newPixels(0, y, width, h);
        }
        repaint(0, y * graphScaleY, width * graphScaleX, h * graphScaleY);
    }


    /** Return the component ColorModel. */
    public ColorModel getPreferredColorModel()
    {
        return getColorModel();
    }


    /** Ignore keyTyped events. */
    public void keyTyped(KeyEvent e)
    {
    }


    /** Forward keyPressed event to the keyboard simulation. */
    public void keyPressed(KeyEvent e)
    {
        log.debug("T keyPressed " + e.getKeyCode());
        inputHandler.handleInput(new KeyInputEvent(e));
    }


    /** Forward keyReleased event to the keyboard simulation. */
    public void keyReleased(KeyEvent e) {
        log.debug("T keyReleased " + e.getKeyCode());
        inputHandler.handleInput(new KeyInputEvent(e));
    }

}

/* end */

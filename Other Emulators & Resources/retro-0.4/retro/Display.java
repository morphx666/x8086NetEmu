/*
 *  Display.java
 *  Joris van Rantwijk
 */

package retro;
import java.awt.Color;
import java.awt.image.ColorModel;

/**
 * Interface to a display device.
 */
public interface Display
{

    /** Enable or disable video output. */
    void enableVideo(boolean enable);

    /**
     * Configure the display for text in w columns and h rows.
     *
     * Set up the display for the specified text mode, and clear
     * the screen to blank characters with attribute 0.
     * 
     * @param w screen width in characters
     * @param h screen height in text rows
     */
    void setTextMode(int w, int h);

    /** Move the text mode cursor to column x and row y. */
    void setCursorLocation(int x, int y);

    /** Switch between text blink mode or intensive background mode. */
    void setBlinkMode(boolean enable);

    /** Update text mode palette. */
    void setTextPalette(Color[] palette, boolean intenseBackground);

    /**
     * Update characters and attributes on a region of the display.
     *
     * @param y first row that needs updating
     * @param h number of rows that need updating
     * @param data array containing character and attribute bytes
     * @param off offset into <code>data</code> of the first updated row
     * @param wrapstart start of data buffer; pointer wraps here when
     *        reaching <code>wrapend</code>
     * @param wrapend end of data buffer; pointer wraps back
     *        to <code>wrapstart</code>
     */
    void updateText(int y, int h, byte[] data, int off, int wrapstart, int wrapend);

    /**
     * Configure the display in graphics mode.
     *
     * Set up the display for the specified graphics mode and clear
     * the screen to black.
     *
     * @param w screen width in pixels
     * @param h screen height in pixels
     */
    void setGraphicsMode(int w, int h);

    /**
     * Update pixels in a region of the screen.
     * The <code>pixels</code> buffer contains valid pixel data for
     * the full screen, not just the updated region; the buffer may
     * be reused on subsequent calls; the implementation of the method
     * can use this for optimization.
     *
     * @param y first scanline that needs updating
     * @param h number of scanlines that need updating
     * @param cm ColorModel used to represent the updated pixels
     * @param pixels array containing the modified pixels, one element per pixel
     * @param off offset into <code>pixels</code> of the first modified scanline
     * @param scanstride distance between subsequent rows in
     *        the <code>pixels</code> array
     */
    void updateGraphics(int y, int h, ColorModel cm, int[] pixels,
                        int off, int scanstride);

    /**
     * Return the preferred ColorModel to use when passing
     * screen updates through the ImageConsumer interface.
     */
    ColorModel getPreferredColorModel();

}

/* end */

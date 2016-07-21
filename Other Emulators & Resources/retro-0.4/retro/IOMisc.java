/*
 *  IOMisc.java
 *  Joris van Rantwijk
 */

package retro;

/**
 * Simulator-specific I/O ports for debugging purposes.
 */
public class IOMisc implements IOPortHandler
{

    /** Output to port 0xe601 is written to stdout as a character. */
    public static final int STDOUT_PORT = 0xe601;

    /** Output to port 0xe602 is written to stdout as a hexadecimal number. */
    public static final int HEX_PORT = 0xe602;

    public void outb(int v, int port)
    {
        if (port == STDOUT_PORT)
            System.out.print((char)(v & 0xff));
        if (port == HEX_PORT || port == HEX_PORT + 1)
            System.out.print(Misc.byteToHex(v));
    }

    public int inb(int port)
    {
        return 0xff;
    }

}

/* end */

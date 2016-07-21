/*
 *  IOPortHandler.java
 *  Joris van Rantwijk
 */

package retro;

import java.util.EventListener;

/**
 * Implementations of this interface handle I/O requests from
 * the CPU for a specific address range.
 */
public interface IOPortHandler extends EventListener
{
    void outb(int v, int port);
    int inb(int port);
}

/* end */

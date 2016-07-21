/*
 *  IOPorts.java
 *  Joris van Rantwijk
 */

package retro;

/**
 *  Emulation of the CPU interface to the 16-bit I/O address space.
 *
 *  Passes I/O operations down to registered IOPortHandlers.
 */
public class IOPorts
{

    /** Linked list of IOPortHandler registrations */
    private static final class Handlers {
        Handlers next;
        IOPortHandler h;
        int base, n;
    }

    protected static Logger log = Logger.getLogger("IOPorts");

    private static final int N1 = 256;
    private static final int N2 = 16;
    private static final int S1 = 8;
    private static final int S2 = 4;

    private Handlers[][] handlers;      // 256 x 16 sets of handlers


    /**
     * Constructs IOPorts object, initially without any I/O handlers.
     */
    public IOPorts()
    {
        handlers = new Handlers[N1][];
    }


    /**
     * Registers a new IOPortHandler.
     * The address range is not checked for conflicts.
     * @param h the IOPortHandler
     * @param base first handled port address
     * @param n number of handled port addresses
     */
    public void registerHandler(IOPortHandler h, int base, int n)
    {
        int rb = base + n - 1;

        for (int k = (base >> S2); k <= (rb >> S2); k++) {
            int i = k >> (S1-S2);
            int j = k & (N2-1);

            if (handlers[i] == null)
                handlers[i] = new Handlers[N2];

            Handlers p = new Handlers();
            p.next = handlers[i][j];
            p.base = base;
            p.n = n;
            p.h = h;
            handlers[i][j] = p;
        }
    }


    /**
     * Removes all registrations of the IOPortHandler.
     */
    public void removeHandler(IOPortHandler h)
    {
        for (int i = 0; i < N1; i++) {
            if (handlers[i] != null) {
                for (int j = 0; j < N2; j++) {
                    Handlers p = handlers[i][j];
                    if (p != null) {
                        while (p != null && p.h == h) {
                            p = p.next;
                            handlers[i][j] = p;
                        }
                        while (p != null && p.next != null) {
                            if (p.next.h == h)
                                p.next = p.next.next;
                            p = p.next;
                        }
                    }
                }
            }
        }
    }


    /**
     * Write byte to I/O port.
     * Passes the write request down to all handlers that registered
     * this address.
     */
    public void outb(int v, int port)
    {
        boolean unhandled = true;

        // Dispatch to all registered handlers
        Handlers p = null;
        if (handlers[port >> S1] != null)
            p = handlers[port >> S1][(port >> S2) & (N2-1)];
        while (p != null) {
            if (port >= p.base && port < p.base + p.n) {
                p.h.outb(v, port);
                unhandled = false;
            }
            p = p.next;
        }

        if (unhandled) {
            log.warn("IOPorts: unhandled OUTB " + Misc.byteToHex(v) + " -> " + Misc.wordToHex(port));
        }
    }


    /**
     * Write word to I/O port.
     * Handled as two subsequent byte-sized write requests.
     */
    public void outw(int v, int port)
    {
        // Word I/O is implemented as a byte operation for LSB,
        // directly followed by a separated byte operation for MSB
        outb(v & 0xff, port);
        outb((v >> 8) & 0xff, (port + 1) & 0xffff);
    }


    /**
     * Read byte from I/O port.
     * Passes the read request to all handlers that registered this address.
     */
    public int inb(int port)
    {
        boolean unhandled = true;
        int ret = 0xff;

        // Dispatch to all registered handlers and AND the results
        Handlers p = null;
        if (handlers[port >> S1] != null)
            p = handlers[port >> S1][(port >> S2) & (N2-1)];
        while (p != null) {
            if (port >= p.base && port < p.base + p.n) {
                ret &= p.h.inb(port);
                unhandled = false;
            }
            p = p.next;
        }

        if (unhandled)
            System.err.println("IOPorts: unhandled INB " +
              Misc.wordToHex(port));

        return ret;
    }


    /**
     * Read word from I/O port.
     * Handled as two subsequent byte-sized read requests.
     */
    public int inw(int port) {
        // Word I/O is implemented as a byte operation for LSB,
        // directly followed by a separated byte operation for MSB
        int ret;
        ret = inb(port);
        ret = ret | (inb(port+1) << 8);
        return ret;
    }

}

/* end */

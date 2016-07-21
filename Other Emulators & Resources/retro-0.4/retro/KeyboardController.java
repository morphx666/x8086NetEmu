/*
 *  KeyboardController.java
 *  Joris van Rantwijk
 */

package retro;

/**
 * Simplified simulation of the 8255 XT keyboard controller
 * and Programmable Peripheral Interface.
 *
 * We don't even try to be precise, since the AT keyboard controller
 * is very different and most software works with both controllers anyway.
 * <p>
 * This simulation acknowledges scan codes automatically as they are read
 * from port 0x60, while a real XT requires explicit acknowledgement for
 * every byte.
 * <p>
 * The 8255 command byte is fixed at 0x99 (the value normally used in a PC):
 * <ul>
 *  <li> Port A fixed in input mode 0 (reads keyboard data)
 *  <li> Port B fixed in output mode 0 (system control byte)
 *  <li> Port C fixed in input mode 0 (reads system status byte)
 * </ul>
 */
public class KeyboardController implements IOPortHandler
{

    protected static Logger log = Logger.getLogger("KeyboardController");

    private Scheduler sched;
    private InterruptRequest irq;
    private int switchS2;
    private I8254 timer;

    private int port61;
    private String keyBuf;
    private int lastKeyCode;
    boolean keyShiftPending;
    private Scheduler.Task keyShiftTask;


    /** Construct and initialize keyboard controller simulation. */
    public KeyboardController(Scheduler sched, InterruptRequest irq)
    {
        this.sched = sched;
        this.irq = irq;
        keyBuf = new String();
        keyShiftPending = false;
        keyShiftTask = new Scheduler.Task() {
            public void run() {
                // raise interrupt again for the next pending byte
                keyShiftPending = false;
                keyBuf = keyBuf.substring(1);
                if (keyBuf.length() > 0 && KeyboardController.this.irq != null)
                    KeyboardController.this.irq.raise(true);
            }
        };
    }


    /**
     * Set configuration switch data to be reported by PPI.
     *
     * <ul>
     *  <li> bit 0: diskette drive present
     *  <li> bit 1: math coprocessor present
     *  <li> bits 3-2: memory size:
     *       00=256k, 01=512k, 10=576k, 11=640k
     *  <li> bits 5-4: initial video mode:
     *       00=EGA/VGA, 01=CGA 40x25, 10=CGA 80x25 color, 11=MDA 80x25
     *  <li> bits 7-6: one less than number of diskette drives (1 - 4 drives)
     * </ul>
     */
    public void setSwitchData(int S2)
    {
        switchS2 = S2;
    }


    /**
     * Connect bit 0 of PPI port B to the channel 2 gate signal of
     * an I8254 programmable interval timer.
     */
    public void setTimer(I8254 t)
    {
        timer = t;
        if (timer != null)
            timer.setCh2Gate((port61 & 1) != 0);
    }


    /** Store a scancode byte in the buffer. */
    public void putKeyData(int v)
    {
        if (keyBuf.length() == 0 && irq != null)
            irq.raise(true);
        keyBuf = keyBuf + ((char)v);
    }


    /** Store scancode bytes in the buffer. */
    public void putKeyData(byte[] b)
    {
        if (keyBuf.length() == 0 && b.length > 0 && irq != null)
            irq.raise(true);
        StringBuffer sb = new StringBuffer(keyBuf);
        for (int i = 0; i < b.length; i++)
            sb.append((char)(b[i] & 0x00ff));
        keyBuf = sb.toString();
    }


    /** Get a scancode byte from the buffer. */
    private final int getKeyData()
    {
        // release interrupt
        if (irq != null)
            irq.raise(false);
        // if the buffer is empty, we just return the most recent byte 
        if (keyBuf.length() > 0) {
            // read byte from buffer
            lastKeyCode = keyBuf.charAt(0);
            // wait .5 msec before going to the next byte
            if (!keyShiftPending) {
                keyShiftPending = true;
                sched.runTaskAfter(keyShiftTask, 500000);
            }
        }
        // return scancode byte
        return lastKeyCode;
    }


    /**
     * Get status byte for Port C read.
     *
     * <ul>
     *   <li> bits 3-0: low/high nibble of S2 byte depending on bit 3 of port B
     *   <li> bit 4: inverted speaker signal
     *   <li> bit 5: timer 2 output status
     *   <li> bit 6: I/O channel parity error occurred (we always set it to 0)
     *   <li> bit 7: RAM parity error occurred (we always set it to 0)
     * </ul>
     */
    private int getStatusByte()
    {
        boolean timerout = (timer != null) && timer.getOutput(2);
        boolean speakerout = timerout && ((port61 & 2) != 0);
        int vh = (speakerout ? 0 : 0x10) | (timerout ? 0x20 : 0);
        int vl = ((port61 & 0x08) == 0) ? switchS2 : (switchS2 >>> 4);
        return (vh & 0xf0) | (vl & 0x0f);
    }


    /** Handle write request from Cpu. */
    public void outb(int v, int port)
    {
        if ((port & 3) == 1) {
            // Write to port 0x61 (system control port)
            // bit 0: gate signal for timer channel 2
            // bit 1: speaker control: 0=disconnect, 1=connect to timer 2
            // bit 3: read low(0) or high(1) nibble of S2 switches
            // bit 4: NMI RAM parity check disable
            // bit 5: NMI I/O check disable
            // bit 6: enable(1) or disable(0) keyboard clock ??
            // bit 7: pulse 1 to reset keyboard and IRQ1
            int oldv = port61;
            port61 = v;
            if (timer != null && ((oldv ^ v) & 1) != 0)
                timer.setCh2Gate((port61 & 1) != 0);
            // TODO : notify speaker
        } else {
            log.warn("OUTB " + Misc.byteToHex(v) + " -> " +
                     Misc.wordToHex(port) + " not implemented");
        }
    }


    /** Handle read request from Cpu. */
    public int inb(int port)
    {
        switch (port & 3) {
          case 0: // port 0x60 (PPI port A)
            // Return keyboard data if bit 7 in port B is cleared.
            return ((port61 & 0x80) == 0) ? getKeyData() : 0;
          case 1:
            // port 0x61 (PPI port B)
            // Return last value written to the port.
            return port61;
           case 2:
            // port 0x62 (PPI port C)
            return getStatusByte();
          default:
            // Reading from port 0x63 is not supported
            log.warn("INB " + Misc.byteToHex(port) + " not implemented");
            return 0xff;
        }
    }

}

/* end */

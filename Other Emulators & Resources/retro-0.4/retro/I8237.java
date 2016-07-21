/*
 *  I8237.java
 *  Joris van Rantwijk
 */

/* TODO : test */

package retro;

/**
 * Simulation of an Intel 8237 DMA Controller and DMA page registers.
 * <p>
 * The simulation is very PC-specific: cascaded mode and memory-to-memory
 * transfers are not supported.  Channel 0 only supports single-mode dummy
 * read transactions with autoinitialization enabled and DREQ driven by a
 * rate generator.
 * <p>
 * In reality, the DMA controller executes transactions in the background,
 * stealing bus cycles from the CPU. This simulation is not so detailed.
 * Block transfers are simulated as atomic events, and even series of single
 * transfers on a channel may be grouped into atomic events. The implications
 * are that<ul>
 * <li>to a simulated device, it appears as if the transfer takes zero time;
 * <li>the simulated CPU never observes partially completed transfers;
 * <li>the simulated CPU does not slow down during DMA transfers.
 * </ul>
 * <p>
 * This should not matter much, except that software which monitors single
 * mode transfers may be surprised to see them occur in atomic groups.
 */
public class I8237 implements IOPortHandler
{

    public static final long CYCLETIME = Scheduler.CLOCKRATE / 4772700;

    protected static Logger log = Logger.getLogger("I8237");

    /** Switches between low/high byte of address and count registers. */
    private boolean msbFlipFlop;

    /** Command register (8-bit) */
    private int cmdreg;

    /** Status register (8-bit) */
    private int statusreg;

    /** Temporary register (8-bit) */
    private int tempreg;

    /** Bitmask of active software DMA requests (bits 0-3) */
    private int reqreg;

    /** Mask register (bits 0-3) */
    private int maskreg;

    /** Channel with highest priority. */
    private int priochannel;
 
    /** Four DMA channels in the I8237 chip */
    private Channel[] channel;

    /** Simulation scheduler */
    private Scheduler sched;

    /** Memory */
    private Memory mem;

    /** True if the background task is currently scheduled */
    private boolean pendingTask;

    /** Channel 0 DREQ trigger period for lazy simulation */
    private long ch0TriggerPeriod;

    /** Scheduler time stamp for the first channel 0 DREQ trigger
        that has not yet been accounted for, or -1 to disable */
    private long ch0NextTrigger;

    /** Scheduled task for pending requests */
    private Scheduler.Task task = new Scheduler.Task() {
        public void run() {
            pendingTask = false;
            tryHandleRequest();
        }
    };

    /** Simulation of one of the four independent DMA channels. */
    private final class Channel implements DmaChannel
    {

        /** Base address register (16-bit) */
        public int baseaddr;

        /** Base count register (16-bit) */
        public int basecount;

        /** Current address register (16-bit) */
        public int curaddr;

        /** Current count register (16-bit) */
        public int curcount;

        /** Mode register (bits 2-7) */
        public int mode;

        /** Page (address bits 16 - 23) for this channel. */
        public int page;

        /** Device with which this channel is currently associated. */
        public DmaDevice dev;

        /** True if DREQ is active for this channel. */
        public boolean pendingRequest;

        /** True if the device signalled external EOP. */
        public boolean externalEop;

        /** Constructs channel. */
        public Channel()
        {
        }

        /** Called by a device to request a DMA transaction. */
        public void dmaRequest(boolean enable)
        {
            pendingRequest = enable;
            tryHandleRequest();
        }

        /** Called by a device to signal external EOP. */
        public void dmaEop()
        {
            externalEop = true;
        }

    }


    /** Constructs and initializes the I8237 controller */
    public I8237(Scheduler sched, Memory mem)
    {
        this.sched = sched;
        this.mem = mem;
        channel = new Channel[4];
        for (int i = 0; i < 4; i++)
            channel[i] = new Channel();
        maskreg = 0x0f; // mask all channels
        ch0NextTrigger = -1;
    }


    /** Return the DmaChannel object for a specific DMA channel. */
    public DmaChannel getChannel(int channr)
    {
        return channel[channr];
    }


    /**
     * Binds a device to a DMA channel.
     * @param channr DMA channel to use (0 ... 3)
     * @param dev    device object to use for callbacks on this channel
     * @return the DmaChannel object
     */
    public DmaChannel bindChannel(int channr, DmaDevice dev)
    {
        if (channr == 0)
            throw new IllegalArgumentException("Can not bind DMA channel 0");
        channel[channr].dev = dev;
        channel[channr].pendingRequest = false;
        channel[channr].externalEop = false;
        return channel[channr];
    }


    /**
     * Changes the DREQ trigger period for channel 0.
     * @param period tregger period in nanoseconds, or 0 to disable
     */
    public void setCh0Period(long period)
    {
        updateCh0();
        ch0TriggerPeriod = period;
        if (ch0NextTrigger == -1 && period > 0)
            ch0NextTrigger = sched.getCurrentTime() + period;
    }


    /** Updates the lazy simulation of the periodic channel 0 DREQ trigger. */
    protected void updateCh0()
    {
        // Figure out how many channel 0 DREQ triggers have occurred since
        // the last update, and update channel 0 status to account for
        // these triggers.

        long t = sched.getCurrentTime();
        long ntrigger = 0;
        if (ch0NextTrigger >= 0 && ch0NextTrigger <= t) {
            // Rounding errors cause some divergence between DMA channel 0 and
            // timer channel 1, but probably nobody will notice.
            if (ch0TriggerPeriod > 0) {
                long d = t - ch0NextTrigger;
                ntrigger = 1 + d / ch0TriggerPeriod;
                ch0NextTrigger = t + ch0TriggerPeriod - d % ch0TriggerPeriod;
            } else {
                ntrigger = 1;
                ch0NextTrigger = -1;
            }
        }

        if (ntrigger == 0)
            return;

        // Ignore triggers if DMA controller is disabled
        if ((cmdreg & 0x04) != 0)
            return;

        // Ignore triggers if channel 0 is masked
        if ((maskreg & 1) == 1)
            return;

        // The only sensible mode for channel 0 in a PC is
        // autoinitialized single read mode, so we simply assume that.

        // Update count, address and status registers to account for
        // the past triggers.
        int addrstep = ((cmdreg & 0x02) == 0) ? 
                        (((channel[0].mode & 0x20) == 0) ? 1 : -1) : 0;
        if (ntrigger <= channel[0].curcount) {
            // no terminal count
            int n = (int)ntrigger;
            channel[0].curcount -= n;
            channel[0].curaddr = (channel[0].curaddr + n * addrstep) & 0xffff;
        } else {
            // terminal count occurred
            int n = (int)( (ntrigger - channel[0].curcount - 1) %
                           (channel[0].basecount + 1) );
            channel[0].curcount = channel[0].basecount - n;
            channel[0].curaddr = (channel[0].baseaddr + n * addrstep) & 0xffff;
            statusreg |= 1;
        }
    }


    /** Try to start a new transaction on a channel with a pending request. */
    protected void tryHandleRequest()
    {
        // Update request bits in status register
        int rbits = reqreg;
        for (int i = 0; i < 4; i++)
            if (channel[i].pendingRequest)
                rbits |= (1 << i);
        statusreg = (statusreg & 0x0f) | (rbits << 4);

        // Don't start a transfer during dead time after a previous transfer
        if (pendingTask)
            return;

        // Don't start a transfer if the controller is disabled
        if ((cmdreg & 0x04) != 0)
            return;

        // Select a channel with pending request
        rbits &= ~maskreg;
        rbits &= ~1; // never select channel 0
        if (rbits == 0)
            return;
        int i = priochannel;
        while (((rbits >> i) & 1) == 0)
             i = (i + 1) & 3;

        // Just decided to start a transfer on channel i
        Channel chan = channel[i];
        DmaDevice dev = chan.dev;
        int mode = chan.mode;
        int page = chan.page;

        // Update dynamic priority
        if ((cmdreg & 10) != 0)
            priochannel = (i + 1) & 3;

        // Block further transactions until this one completes
        pendingTask = true;
        long transfertime = 0;

        if ((mode & 0xc0) == 0xc0) {
            log.warn("cascade mode not implemented (channel " + i + ")");
        } else if ((mode & 0x0c) == 0x0c) {
            log.warn("invalid mode on channel " + i);
        } else {
            if (log.isDebugEnabled()) {
                String typ, rw;
                switch (mode & 0xc0) {
                  case 0x00: typ = "demand"; break;
                  case 0x40: typ = "single"; break;
                  case 0x80: typ = "block"; break;
                  default: typ = null;
                }
                switch (mode & 0x0c) {
                  case 0x00: rw = "verify"; break;
                  case 0x04: rw = "write"; break;
                  case 0x08: rw = "read"; break;
                  default: rw = null;
                }
                log.debug("starting " + typ + "-" + rw + " transfer on channel " + i);
            }

            // Prepare for transfer
            boolean blockmode = ((mode & 0xc0) == 0x80);
            boolean singlemode = ((mode & 0xc0) == 0x40);
            int curcount = chan.curcount;
            int maxlen = curcount + 1;
            int curaddr = chan.curaddr;
            int addrstep = ((chan.mode & 0x20) == 0) ? 1 : -1;
            chan.externalEop = false;

            // Don't combine too much single transfers in one atomic action
            if (singlemode && maxlen > 25)
                maxlen = 25;

            // Execute transfer
            switch (mode & 0x0c) {
              case 0x00:
                // DMA verify
                curcount -= maxlen;
                curaddr = (curaddr + maxlen * addrstep) & 0xffff;
                transfertime += 3 * maxlen * CYCLETIME;
                break;
              case 0x04:
                // DMA write
                while (maxlen > 0 &&
                       !chan.externalEop &&
                       (blockmode || chan.pendingRequest)) {
                    if (dev != null) {
                        byte b = dev.dmaWrite();
                        mem.storeByte((page << 16) | curaddr, b);
                    }
                    maxlen--;
                    curcount--;
                    curaddr = (curaddr + addrstep) & 0xffff;
                    transfertime += 3 * CYCLETIME;
                }
                break;
              case 0x08:
                // DMA read
                while (maxlen > 0 &&
                       !chan.externalEop &&
                       (blockmode || chan.pendingRequest)) {
                    if (dev != null) {
                        byte b = (byte) mem.loadByte((page << 16) | curaddr);
                        dev.dmaRead(b);
                    }
                    maxlen--;
                    curcount--;
                    curaddr = (curaddr + addrstep) & 0xffff;
                    transfertime += 3 * CYCLETIME;
                }
                break;
            }

            // Update registers
            boolean termcount = (curcount < 0);
            chan.curcount = termcount ? 0xffff : curcount;
            chan.curaddr = curaddr;

            // Handle terminal count or external EOP
            if (termcount || chan.externalEop) {
                if ((mode & 0x10) == 0) {
                    // Set mask bit
                    maskreg |= (1 << i);
                } else {
                    // Autoinitialize
                    chan.curcount = chan.basecount;
                    chan.curaddr = chan.baseaddr;
                }
                // Clear software request
                reqreg &= ~(1 << i);
                // Set TC bit in status register
                statusreg |= (1 << i);
            }

            // Send EOP to device
            if (termcount && !chan.externalEop && dev != null)
                dev.dmaEop();
        }

        // Schedule a task to run when the simulated DMA transfer completes
        sched.runTaskAfter(task, transfertime);
    }


    /** Handles I/O write request from the Cpu */
    public void outb(int v, int port)
    {
        updateCh0();
        if ((port & 0xfff8) == 0) {

            // DMA controller: channel setup
            Channel chan = channel[(port >> 1) & 3];
            int x, y;
            if ((port & 1) == 0) {
                // base/current address
                x = chan.baseaddr;
                y = chan.curaddr;
            } else {
                x = chan.basecount;
                y = chan.curcount;
            }
            boolean p = msbFlipFlop;
            msbFlipFlop = !p;
            if (p) {
                x = (x & 0x00ff) | ((v << 8) & 0xff00);
                y = (y & 0x00ff) | ((v << 8) & 0xff00);
            } else {
                x = (x & 0xff00) | (v & 0x00ff);
                y = (y & 0xff00) | (v & 0x00ff);
            }
            if ((port & 1) == 0) {
                chan.baseaddr = x;
                chan.curaddr = y;
            } else {
                chan.basecount = x;
                chan.curcount = y;
            }

        } else if ((port & 0xfff8) == 0x08) {

            // DMA controller: operation registers
            switch (port & 0x0f) {
              case 8:  // write command register
                cmdreg = v;
                if ((v & 0x10) == 0)
                    priochannel = 0; // enable fixed priority
                if ((v & 1) == 1)
                    log.warn("memory-to-memory transfer not implemented");
                break;
              case 9:  // set/reset request register
                if ((v & 4) == 0)
                    reqreg = reqreg & ~(1 << (v & 3)); // reset request bit
                else
                    reqreg = reqreg | (1 << (v & 3));  // set request bit
                if ((v & 7) == 4)
                    log.warn("software request on channel 0 not implemented");
                break;
              case 10: // set/reset mask register
                if ((v & 4) == 0)
                    maskreg = maskreg & ~(1 << (v & 3)); // reset mask bit
                else
                    maskreg = maskreg | (1 << (v & 3));  // set mask bit
                break;
              case 11: // write mode register
                channel[v & 3].mode = v;
                if ((v & 3) == 0 && (v & 0xdc) != 0x58)
                    log.warn("unsupported mode on channel 0");
                break;
              case 12: // clear msb flipflop
                msbFlipFlop = false;
                break;
              case 13: // master clear
                msbFlipFlop = false;
                cmdreg = 0;
                statusreg = 0;
                reqreg = 0;
                tempreg = 0;
                maskreg = 0x0f;
                break;
              case 14: // clear mask register
                maskreg = 0;
                break;
              case 15: // write mask register
                maskreg = v;
                break;
            }
            tryHandleRequest();

        } else if ((port & 0xfff8) == 0x80) {

            // DMA page registers
            switch (port) {
              case 0x81: channel[2].page = v; break;
              case 0x82: channel[3].page = v; break;
              case 0x83: channel[1].page = v; break;
              case 0x87: channel[0].page = v; break;
            }

        } else {

            // write to unknown port
            log.warn("OUTB " +
                     Misc.byteToHex(v) + " -> " + Misc.wordToHex(port));

        }
    }


    /** Handles I/O read request from the Cpu */
    public int inb(int port)
    {
        updateCh0();
        if ((port & 0xfff8) == 0) {

            // DMA controller: channel status
            Channel chan = channel[(port >> 1) & 3];
            int x = ((port & 1) == 0) ? chan.curaddr : chan.curcount;
            boolean p = msbFlipFlop;
            msbFlipFlop = !p;
            return (p) ? ((x >> 8) & 0xff) : (x & 0xff);

        } else if ((port & 0xfff8) == 0x08) {

            // DMA controller: operation registers
            int v;
            switch (port & 0x0f) {
              case 8:  // read status register
                v = statusreg;
                statusreg &= 0xf0;
                return v;
              case 13: // read temporary register
                return tempreg;
            }

        }

        // read from unknown port
        log.warn("INB " + Misc.wordToHex(port));
        return 0xff;
    }

}

/* end */

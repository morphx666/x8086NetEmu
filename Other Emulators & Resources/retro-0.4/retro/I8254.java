/*
 *  I8254.java
 *  Joris van Rantwijk
 */

package retro;

/**
 * Simulation of an Intel 82C4 Programmable Interval Timer
 */
public class I8254 implements IOPortHandler
{

    protected static Logger log = Logger.getLogger("I8254");

    /** Simulation of one of the three independent counter modules. */
    private final class Counter
    {
        /** Mode (0..5) */
        int countMode;

        /** Count format (1=lsb, 2=msb, 3=lsb+msb) */
        int rwMode;

        /** True when counting in BCD instead of binary */
        boolean bcdMode;

        /** Contents of count register */
        int countRegister;

        /** True if next write to count register will set the MSB */
        boolean countRegisterMsb;

        /** Contents of output latch */
        int outputLatch;

        /** True if the output value is latched */
        boolean outputLatched;

        /** True if the next read from the output latch will get the MSB */
        boolean outputLatchMsb;

        /** Status latch register */
        int statusLatch;

        /** True if the status is latched */
        boolean statusLatched;

        /** Signal on gate input pin */
        boolean gate;

        /** True if triggered after the last clock was processed */
        boolean trigger;

        // Internal counter state (lazy)
        long timeStamp;
        int counterValue;
        boolean outputValue;
        boolean nullCount;
        boolean active;

        /** Constructs and resets counter. */
        public Counter()
        {
            // assume no gate signal
            gate = false;
            // set undefined mode
            countMode = -1;
            outputValue = false;
        }

        /** Reprograms counter mode */
        public void setMode(int countm, int rwm, boolean bcdm)
        {
            // set mode
            countMode = countm;
            rwMode = rwm;
            bcdMode = bcdm;
            // reset registers
            countRegister = 0;
            countRegisterMsb = false;
            outputLatched = false;
            outputLatchMsb = false;
            statusLatched = false;
            // reset internal state
            timeStamp = currentTime;
            counterValue = 0;
            outputValue = (countMode == 0) ? false : true;
            nullCount = true;
            trigger = false;
            active = false;
        }

        /** Activates output latch */
        public void latchOutput()
        {
            if (countMode >= 0 && !outputLatched) {
                update();
                // copy counter value to output latch
                outputLatch = counterValue;
                outputLatched = true;
                outputLatchMsb = false;
            }
        }

        /** Activates status latch */
        public void latchStatus()
        {
            if (countMode >= 0 && !statusLatched) {
                update();
                // fill status latch register:
                // bit7   = output
                // bit6   = nullcount
                // bit4-5 = rwMode
                // bit1-3 = countMode
                // bit0   = bcdMode
                statusLatch =
                  (outputValue ? 0x80 : 0x00) |
                  (nullCount ? 0x40 : 0x00) |
                  (rwMode << 4) |
                  (countMode << 1) |
                  (bcdMode ? 0x01 : 0x00);
                statusLatched = true;
            }
        }

        /** Reads byte from counter */
        public int getByte()
        {
            if (countMode < 0)
                return 0xff; // undefined state
            if (statusLatched) {
                // read status latch register
                statusLatched = false;
                return statusLatch;
            }
            if (!outputLatched) {
                // output latch directly follows counter
                update();
                outputLatch = counterValue;
            }
            // read output latch register
            switch (rwMode) {
              case 1: // LSB only
                outputLatched = false;
                return outputLatch & 0xff;
              case 2: // MSB only
                outputLatched = false;
                return outputLatch >> 8;
              case 3: // LSB followed by MSB
                if (outputLatchMsb) {
                    outputLatched = false;
                    outputLatchMsb = false;
                    return outputLatch >> 8;
                } else {
                    outputLatchMsb = true;
                    return outputLatch & 0xff;
                }
              default: // cannot happen
                throw new RuntimeException("I8254.java: cannot happen");
            }
        }

        /** Writes byte to counter */
        public void putByte(int v)
        {
            if (countMode < 0)
                return; // undefined state
            // write to count register
            switch (rwMode) {
              case 1: // LSB only
                countRegister = v & 0xff;
                changeCount();
                break;
              case 2: // MSB only
                countRegister = (v << 8) & 0xff00;
                changeCount();
                break;
              case 3: // LSB followed by MSB
                if (countRegisterMsb) {
                    countRegister = (countRegister & 0x00ff) |
                                    ((v << 8) & 0xff00);
                    countRegisterMsb = false;
                    changeCount();
                } else {
                    countRegister = (countRegister & 0xff00) | (v & 0xff);
                    countRegisterMsb = true;
                }
            }
        }

        /** Sets gate input state */
        public void setGate(boolean v)
        {
            if (countMode >= 0)
                update();
            // trigger on rising edge of the gate signal
            if (v && !gate)
                trigger = true;
            gate = v;
            // mode 2 and mode 3: when gate goes low, output
            // is set high immediately
            if (!gate && (countMode == 2 || countMode == 3))
                outputValue = true;
        }

        /** Returns current output state */
        public boolean getOutput()
        {
            if (countMode >= 0)
                update();
            return outputValue;
        }

        /**
         * Returns the time when the output state will change,
         * or returns 0 if the output will not change spontaneously.
         */
        public long nextOutputChangeTime()
        {
            int clocks = 0;
            if (countMode < 0)
                return 0;
            update();
            switch (countMode) {
              case 0:
                // output goes high on terminal count
                if (active && gate && !outputValue)
                    clocks = fromCounter(counterValue) + (nullCount ? 1 : 0);
                break;
              case 1:
                // output goes high on terminal count
                if (!outputValue)
                    clocks = fromCounter(counterValue) + (trigger ? 1 : 0);
                // output goes low on next clock after trigger
                if (outputValue && trigger)
                    clocks = 1;
                break;
              case 2:
                // output goes high on reaching one
                if (active && gate && outputValue)
                    clocks = fromCounter(counterValue) + (trigger ? 0 : -1);
                // strobe ends on next clock
                if (!outputValue)
                    clocks = 1;
                break;
              case 3:
                // trigger pulls output high
                if (!outputValue && trigger)
                    clocks = 1;
                // output goes low on reaching zero
                if (active && gate && outputValue)
                    clocks = fromCounter(counterValue) / 2 +
                             (trigger ? 1 : 0) +
                             (countRegister & 1);
                // output goes high on reaching zero
                if (active && gate && !outputValue && !trigger)
                    clocks = fromCounter(counterValue) / 2;
                break;
              case 4:
                // strobe starts on terminal count
                if (active && gate && outputValue)
                    clocks = fromCounter(counterValue) + (nullCount ? 1 : 0);
                // strobe ends on next clock
                if (!outputValue)
                    clocks = 1;
                break;
              case 5:
                // strobe starts on terminal count
                if (active && outputValue)
                    clocks = fromCounter(counterValue);
                // strobe ends on next clock
                if (!outputValue)
                    clocks = 1;
                break;
            }
            if (clocks == 0)
                return 0;
            else
                return clocksToTime(timeToClocks(currentTime) + clocks);
        }

        /**
         * Returns the full period for mode 3 (square wave),
         * or returns 0 in other modes.
         */
        public long getSquareWavePeriod()
        {
            if (countMode != 3 || !active || !gate)
                return 0;
            update();
            return clocksToTime(fromCounter(countRegister));
        }

        /**
         * Returns the full period, or 0 if not enabled.
         */
        public long getPeriod()
        {
            if (!active || !gate)
                return 0;
            update();
            return clocksToTime(fromCounter(countRegister));
        }

        /**
         * Converts an internal counter value to a number,
         * wrapping the zero value to the maximum value.
         */
        private final int fromCounter(int v)
        {
            if (v == 0) {
                return (bcdMode) ? 10000 : 0x10000;
            } else if (bcdMode) {
                return
                  ((v >> 12) & 0xf) * 1000 +
                  ((v >> 8)  & 0xf) * 100 +
                  ((v >> 4)  & 0xf) * 10 +
                  (v & 0xf);
            } else {
                return v;
            }
        }

        /**
         * Converts a number to an internal counter value,
         * using zero to represent the maximum counter value.
         */
        private final int toCounter(int v) {
            if (bcdMode) {
                v %= 10000;
                return 
                  ((v / 1000) % 10) << 12 |
                  ((v /  100) % 10) << 8  |
                  ((v /   10) % 10) << 4  |
                  (v % 10);
            } else {
                return v % 0x10000;
            }
        }

        /**
         * Substracts c from the counter and
         * returns true if the zero value was reached.
         */
        private final boolean countDown(long c)
        {
            boolean zero;
            if (bcdMode) {
                int v =
                  ((counterValue >> 12) & 0xf) * 1000 +
                  ((counterValue >> 8)  & 0xf) * 100 +
                  ((counterValue >> 4)  & 0xf) * 10 +
                  (counterValue & 0xf);
                zero = (c >= 10000 || (v != 0 && c >= v));
                v += 10000 - (c % 10000);
                counterValue =
                  ((v / 1000) % 10) << 12 |
                  ((v /  100) % 10) << 8  |
                  ((v /   10) % 10) << 4  |
                  (v % 10);
            } else {
                zero = (c > 0xffff || (counterValue != 0 && c >= counterValue));
                counterValue = (int)((counterValue - c) & 0xffff);
            }
            return zero;
        }

        /**
         * Recomputes the internal state of the counter at the
         * current time from the last computed state.
         */
        private final void update()
        {
            // compute elapsed clock pulses since last update
            long clocks = timeToClocks(currentTime) - timeToClocks(timeStamp);
            // call mode-dependent update function
            switch (countMode) {
              case 0: updMode0(clocks); break;
              case 1: updMode1(clocks); break;
              case 2: updMode2(clocks); break;
              case 3: updMode3(clocks); break;
              case 4: updMode4(clocks); break;
              case 5: updMode5(clocks); break;
            }
            // put timestamp on new state
            trigger = false;
            timeStamp = currentTime;
        }

        // MODE 0 - INTERRUPT ON TERMINAL COUNT
        private final void updMode0(long clocks)
        {
            // init:      output low, stop counter
            // set count: output low, start counter
            // on zero:   output high, counter wraps
            if (active && nullCount) {
                // load counter on next clock after writing
                counterValue = countRegister;
                nullCount = false;
                clocks--;
            }
            if (clocks < 0)
                return;
            if (active && gate) {
                // count down, zero sets output high
                if (countDown(clocks))
                    outputValue = true;
            }
        }

        // MODE 1 - HARD-TRIGGERED ONE-SHOT
        private final void updMode1(long clocks)
        {
            // init:      output high, counter running
            // set count: nop
            // trigger:   load counter, output low
            // on zero:   output high, counter wraps
            if (trigger) {
                // load counter on next clock after trigger
                counterValue = countRegister;
                nullCount = false;
                outputValue = false;
                clocks--;
            }
            // count down, zero sets output high
            if (clocks < 0)
                return;
            if (countDown(clocks))
                outputValue = true;
        }

        // MODE 2 - RATE GENERATOR
        private final void updMode2(long clocks)
        {
            // init:      output high, stop counter
            // initial c: load and start counter
            // trigger:   reload counter
            // on one:    output strobes low
            // on zero:   reload counter
            if (trigger) {
                // load counter on trigger
                counterValue = countRegister;
                nullCount = false;
                clocks--;
            }
            if (clocks < 0)
                return;
            if (active && gate) {
                // count down
                int v = fromCounter(counterValue);
                if (clocks < v) {
                    v -= clocks;
                } else {
                    // zero reached, reload counter
                    clocks -= v;
                    v = fromCounter(countRegister);
                    v -= clocks % v;
                    nullCount = false;
                }
                counterValue = toCounter(v);
            }
            // output strobes low on decrement to 1
            outputValue = (!gate || counterValue != 1);
        }

        // MODE 3 - SQUARE WAVE
        private final void updMode3(long clocks)
        {
            // init:      output high, stop counter
            // initial c: load and start counter
            // trigger:   reload counter
            // on one:    switch phase, reload counter
            if (trigger) {
                // load counter on trigger
                counterValue = countRegister & (~2);
                nullCount = false;
                outputValue = true;
                clocks--;
            }
            if (clocks < 0)
                return;
            if (active && gate) {
                // count down
                int v = fromCounter(counterValue);
                if (counterValue == 0 && outputValue &&
                        (countRegister & 1) != 0)
                    v = 0;
                if (2 * clocks < v) {
                    v -= 2 * clocks;
                } else {
                    // zero reached, reload counter
                    clocks -= v / 2;
                    v = fromCounter(countRegister);
                    int c = (int)(clocks % v);
                    v &= (~2);
                    nullCount = false;
                    if (!outputValue) {
                        // zero reached in low phase
                        // switch to high phase
                        outputValue = true;
                        // continue counting
                        if (2 * c < v) {
                            v -= 2 * c;
                            counterValue = toCounter(v);
                            return;
                        }
                        c -= v / 2;
                    }
                    // zero reached in high phase
                    if ((countRegister & 1) != 0) {
                        // wait one more clock
                        if (clocks == 0) {
                            counterValue = 0;
                            return;
                        }
                        clocks--;
                    }
                    // switch to low phase
                    outputValue = false;
                    // continue counting
                    if (2 * c >= v) {
                        // zero reached again
                        c -= v / 2;
                        // switch to high phase
                        outputValue = true;
                    }
                    // continue counting
                    v -= 2 * c;
                }
                counterValue = toCounter(v);
            }
        }

        // MODE 4 - SOFT-TRIGGERED STROBE
        final private void updMode4(long clocks)
        {
            // init:      output high, counter running
            // set count: load counter
            // on zero:   output strobes low, counter wraps
            if (active && nullCount) {
                // load counter on first clock
                counterValue = countRegister;
                nullCount = false;
                clocks--;
            }
            if (clocks < 0)
                return;
            if (gate) {
                // count down
                countDown(clocks);
                // output strobes low on zero
                outputValue = (!active || counterValue != 0);
            } else {
                // end previous strobe
                outputValue = true;
            }
        }

        // MODE 5 - HARD-TRIGGERED STROBE
        private final void updMode5(long clocks)
        {
            // init:      output high, counter running
            // set count: nop
            // trigger:   reload counter
            // on zero:   output strobes low, counter wraps
            outputValue = true;
            if (trigger) {
                // load counter on trigger
                counterValue = countRegister;
                nullCount = false;
                active = true;
                clocks--;
            }
            if (clocks < 0)
                return;
            // count down
            countDown(clocks);
            // output strobes low on zero
            outputValue = (!active || counterValue != 0);
        }

        /** Called when a new count is written to the Count Register */
        private final void changeCount()
        {
            update();
            if (countMode == 0) {
                // mode 0 is restarted by writing a count
                outputValue = false;
            } else {
                // modes 2 and 3 are soft-triggered by
                // writing the initial count
                if (!active)
                    trigger = true;
            }
            nullCount = true;
            // mode 5 is only activated by a trigger
            if (countMode != 5)
                active = true;
        }

    }


    /** Scheduled task to drive IRQ 0 based on counter 0 output signal */
    private Scheduler.Task irqTask = new Scheduler.Task() {
        private boolean lastValue = false;
        public void run() {
            currentTime = sched.getCurrentTime();
            // set IRQ 0 signal equal to counter 0 output
            boolean s;
            s = channel[0].getOutput();
            if (s != lastValue) {
                irq.raise(s);
                lastValue = s;
            }
            // reschedule task for next output change
            long t = channel[0].nextOutputChangeTime();
            if (t > 0)
                sched.runTaskAt(this, t);
        }
    };
		

    /** Global counter clock rate (1.19318 MHz) */
    public static final long COUNTRATE = 1193180;

    /** Current time mirrored from Scheduler */
    private long currentTime;

    /** Three counters in the I8254 chip */
    private Counter[] channel;

    /** Simulation scheduler */
    private Scheduler sched;

    /** Interrupt request line for channel 0 */
    private InterruptRequest irq;

    /** DMA controller connected to channel 1*/
    private I8237 dmactl; // should go through an interface


    /** Constructs and initializes the I8254 controller */
    public I8254(Scheduler sched, InterruptRequest irq, I8237 dmactl)
    {
        this.sched = sched;
        this.irq = irq;
        this.dmactl = dmactl;

        // construct 3 timer channels
        channel = new Counter[3];
        channel[0] = new Counter();
        channel[1] = new Counter();
        channel[2] = new Counter();

        // gate input for channels 0 and 1 is always high
        channel[0].setGate(true);
        channel[1].setGate(true);
    }


    /** Returns the output status of a timer channel */
    public boolean getOutput(int c)
    {
        return channel[c].getOutput();
    }

    /** Sets the value of the channel 2 gate input signal */
    public void setCh2Gate(boolean v)
    {
        currentTime = sched.getCurrentTime();
        channel[2].setGate(v);
        updateCh2();
    }

    /** Handles write request from the Cpu */
    public void outb(int v, int port)
    {
        currentTime = sched.getCurrentTime();
        int c = port & 3;
        if (c == 3) {
            // write Control Word
            c = (v >> 6) & 3;
            if (c == 3) {
                // Read Back command
                for (int i = 0; i < 3; i++) {
                    int s = (2 << i);
                    if ((v & (0x10 | s)) == s)
                        channel[i].latchStatus();
                    if ((v & (0x20 | s)) == s)
                        channel[i].latchOutput();
                }
            } else {
                // Channel Control Word
                if ((v & 0x30) == 0) {
                    // Counter Latch command
                    channel[c].latchOutput();
                    log.debug("latch channel=" + c);
                } else {
                    // reprogram counter mode
                    int countm = (v >> 1) & 7;
                    if (countm > 5) countm &= 3;
                    int rwm = (v >> 4) & 3;
                    boolean bcdm = ((v & 1) != 0);
                    channel[c].setMode(countm, rwm, bcdm);
                    log.debug("mode channel=" + c + " countm=" + countm + " rwm=" + rwm + " bcdm=" + bcdm);
                    switch (c) {
                      case 0: updateCh0(); break;
                      case 1: updateCh1(); break;
                      case 2: updateCh2(); break;
                    }
                }
            }
        } else {
            // write to counter
            channel[c].putByte(v);
            log.debug("write channel=" + c + " v=" + v);
            switch (c) {
              case 0: updateCh0(); break;
              case 1: updateCh1(); break;
              case 2: updateCh2(); break;
            }
        }
    }


    /** Handles read request from the Cpu */
    public int inb(int port)
    {
        currentTime = sched.getCurrentTime();
        int c = port & 3;
        if (c == 3) {
            // invalid read
            log.warn("INB " + Misc.byteToHex(port));
            return 0xff;
        } else {
            // read from counter
            return channel[c].getByte();
        }
    }


    /** Called after changing settings for channel 0 */
    private final void updateCh0()
    {
        // State of channel 0 may have changed;
        // run the IRQ task immediately to take this into account
        if (irq != null) {
            irqTask.cancel();
            irqTask.run();
        }
    }


    /** Called after changing settings for channel 1 */
    private final void updateCh1()
    {
        // Notify the DMA controller of the new frequency
        if (dmactl != null)
            dmactl.setCh0Period(channel[1].getPeriod());
    }


    /** Called after changing settings for channel 2 */
    private final void updateCh2()
    {
        // TODO : drive pc-speaker simulation
    }

    /** Converts scheduler time value to counter time value */
    private static final long timeToClocks(long t)
    {
        return
          (t / Scheduler.CLOCKRATE) * COUNTRATE +
          ((t % Scheduler.CLOCKRATE) * COUNTRATE) / Scheduler.CLOCKRATE;
    }


    /** Returns smallest t such that timeToClocks(t) == c */
    private static final long clocksToTime(long c)
    {
        return
          (c / COUNTRATE) * Scheduler.CLOCKRATE +
          ((c % COUNTRATE) * Scheduler.CLOCKRATE + COUNTRATE - 1) / COUNTRATE;
    }

}

/* end */

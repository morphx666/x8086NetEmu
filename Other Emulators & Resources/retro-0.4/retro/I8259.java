/*
 *  I8259.java
 *  Joris van Rantwijk
 *
 *  Simulation of an Intel 8059A Programmable Interrupt Controller
 */

package retro;

public class I8259 implements InterruptController, IOPortHandler
{
        protected static Logger log = Logger.getLogger("I8259");

	private static final int stREADY = 0;
	private static final int stICW1 = 1;
	private static final int stICW2 = 2;
	private static final int stICW3 = 3;
	private static final int stICW4 = 4;

	// Initialisation state
	private int state;	// one of stXXXX
	private boolean expectICW3;
	private boolean expectICW4;

	// Master/slave relations
	private I8259[] slave;	// our slave controller for each IRQ
	private I8259 master;	// our master controller
	private int masterIrq;	// IRQ line on master which handlers our output

	// Controller state
	private boolean levelTriggered;
	private boolean autoEOI;
	private boolean autoRotate;
	private int baseVector;
	private boolean specialMask;
	private boolean specialNest;
	private boolean pollMode;
	private boolean readISR;
	private int lowPrio;	// IRQ with lowest priority
	private int slaveInput;	// bitmask of slave IRQ lines
	private int cascadeId;	// our slave cascade ID
	private int rIMR;	// interrupt mask register
	private int rIRR;	// interrupt request register
	private int rISR;	// interrupt service register


	// An IrqLine represents a specific IRQ pin of an I8259
	public class IrqLine implements InterruptRequest {
		private int irq;
		public IrqLine(int i) { irq = i; }
		public void raise(boolean enable) { raiseIrq(irq, enable); }
	}


	// Construct and initialize I8259 controller
	public I8259(Scheduler sched)
	{
		slave = new I8259[8];
		state = stICW1; // wait for initialization
	}


	// Connect this PIC as a slave to the specified master PIC
	public void setMaster(I8259 mpic, int mirq)
	{
		if (master != null) master.slave[cascadeId] = null;
		master = mpic;
		masterIrq = mirq;
		if (master != null) master.slave[cascadeId] = this;
	}


	// Return the vector number of a pending interrupt request,
	// or return -1 if no interrupt was pending.
	// Calling this method also acknowledges the interrupt.
	public synchronized int getPendingInterrupt()
	{
		if (state != stREADY)
			return -1;

		// Determine set of pending interrupt requests
		int reqmask = rIRR & (~ rIMR);
		if (specialNest)
			reqmask &= (~ rISR) | slaveInput;
		else
			reqmask &= (~ rISR);

		// Select non-masked request with highest priority
		if (reqmask == 0)
			return -1;
		int irq = (lowPrio + 1) & 7;
		while ((reqmask & (1 << irq)) == 0) {
			if (!specialMask && (rISR & (1 << irq)) != 0)
				return -1;  // ISR bit blocks all lower-prio requests
			irq = (irq + 1) & 7;
		}
		int irqbit = (1 << irq);

		// Update controller state
		if (!autoEOI)
			rISR |= irqbit;
		if (!levelTriggered)
			rIRR &= (~ irqbit);
		if (autoEOI && autoRotate)
			lowPrio = irq;
		if (master != null)
			updateSlaveOutput();

		log.debug("pendinginterrupt irq=" + irq + " vector=" + ((baseVector+irq) & 0xff));

		// Return vector number or pass down to slave controller
		if ((slaveInput & irqbit) != 0 && slave[irq] != null)
			return slave[irq].getPendingInterrupt();
		else
			return (baseVector + irq) & 0xff;
	}


	// Called by hardware components to raise or drop an IRQ signal
	public synchronized void raiseIrq(int irq, boolean enable)
	{
		log.debug("raiseIrq " + irq + " " + enable);
		if (enable)
			rIRR |= (1 << irq);
		else
			rIRR &= ~ (1 << irq);
		if (master != null)
			updateSlaveOutput();
	}


	// Return IrqLine object for specified IRQ channel
	public IrqLine getIrqLine(int i)
	{
		return new IrqLine(i);
	}


	// Handle write request from the Cpu
	public synchronized void outb(int v, int port)
	{
		if ((port & 1) == 0) {
			// A0 == 0
			if ((v & 0x10) != 0) doICW1(v);
			else if ((v & 0x08) == 0) doOCW2(v);
			else doOCW3(v);
		} else {
			// A0 == 1
			switch (state) {
			  case stICW2: doICW2(v); break;
			  case stICW3: doICW3(v); break;
			  case stICW4: doICW4(v); break;
			  default: doOCW1(v); break;
			}
		}
	}


	// Handle read request from the Cpu
	public int inb(int port)
	{
		if ((port & 1) == 0) {
			// A0 == 0
			if (pollMode) {
				int a = getPendingInterrupt();
				return (a == -1) ? 0 : (0x80 | a);
			}
			return (readISR) ? rISR : rIRR;
		} else {
			// A0 == 1
			return rIMR;
		}
	}


	// Update our output signal to the master controller
	private final void updateSlaveOutput()
	{
		int reqmask = rIRR & (~ rIMR);
		if (!specialMask)
			reqmask &= (~ rISR);
		I8259 m = master;
		if (m != null)
			m.raiseIrq(masterIrq, (reqmask != 0));
	}


	// Handle Initialization Control Word 1
	private final void doICW1(int v)
	{
		state = stICW2;
		rIMR = 0;
		rISR = 0;
		specialMask = false;
		specialNest = false;
		autoEOI = false;
		autoRotate = false;
		pollMode = false;
		readISR = false;
		lowPrio = 7;
		slaveInput = 0;
		if (master != null) master.slave[cascadeId] = null;
		cascadeId = 7;
		if (master != null) master.slave[cascadeId] = this;
		levelTriggered = ((v & 0x08) != 0);
		expectICW3 = ((v & 0x02) == 0);
		expectICW4 = ((v & 0x01) != 0);
		if (master != null)
			updateSlaveOutput();
                log.debug("levelTriggered=" + levelTriggered);
	}


	// Handle Initialization Control Word 2
	private final void doICW2(int v)
	{
		baseVector = v & 0xf8;
		state = (expectICW3) ?
		  ((expectICW4) ? stICW4 : stREADY) : stICW3;
				log.debug("baseVector=" + Misc.byteToHex(baseVector));
	}


	// Handle Initialization Control Word 3
	private final void doICW3(int v)
	{
		slaveInput = v;
		if (master != null) master.slave[cascadeId] = null;
		cascadeId = v & 0x07;
		if (master != null) master.slave[cascadeId] = this;
		state = (expectICW4) ? stICW4 : stREADY;
	}


	// Handle Initialization Control Word 4
	private final void doICW4(int v)
	{
		specialNest = ((v & 0x10) != 0);
		autoEOI = ((v & 0x02) != 0);
		state = stREADY;
                log.debug("autoEOI=" + autoEOI + " specialNest=" + specialNest);
	}


	// Handle Operation Control Word 1
	private final void doOCW1(int v)
	{
		rIMR = v;
		if (master != null)
			updateSlaveOutput();
                log.debug("OCW1: IMR=" + Misc.byteToHex(rIMR));
	}


	// Handle Operation Control Word 2
	private final void doOCW2(int v)
	{
		int irq = v & 0x07;
		boolean rotate = ((v & 0x80) != 0);
		boolean specific = ((v & 0x40) != 0);
		boolean eoi = ((v & 0x20) != 0);

		// Resolve non-specific EOI
		if (!specific) {
			int m = (specialMask) ? (rISR & (~ rIMR)) : rISR;
			int i = lowPrio;
			do {
				i = (i + 1) & 7;
				if ((m & (1 << i)) != 0) {
					irq = i;
					break;
				}
			} while (i != lowPrio);
		}

		// Handle EOI
		if (eoi) {
			rISR &= ~ (1 << irq);
			if (master != null)
				updateSlaveOutput();
		}

		// Handle rotation stuff
		if (!eoi && !specific)
			autoRotate = rotate;
		else if (rotate)
			lowPrio = irq;
	}


	// Handle Operation Control Word 3
	private final void doOCW3(int v)
	{
		if ((v & 0x40) != 0) {
			specialMask = ((v & 0x20) != 0);
			if (master != null)
				updateSlaveOutput();
		}
		pollMode = ((v & 0x04) != 0);
		if ((v & 0x02) != 0)
			readISR = ((v & 0x01) != 0);
	}

}

/* end */

/*
 *  Scheduler.java
 *  Joris van Rantwijk
 */

package retro;
import java.util.ArrayList;


/**
 * The Scheduler manages pending events and controls the simulation.
 *
 * A priority queue is used to keep track of pending discrete events
 * produced by the simulation itself.  A separate list contains pending
 * external input events.  Handling of pending events is interleaved
 * with execution of the Cpu simulation.
 * <p>
 * The scheduler is based on a single-threaded design.  All tasks in
 * the simulation, including the Cpu simulation, are executed from the
 * same thread.  This approach avoids synchronization issues in most
 * of the simulator code.  The exceptions are interfaces where the
 * simulator touches the external world; mostly the user interface.
 * The rule to remember is that methods of simulator components may
 * not be called directly from asynchronous contexts, unless their
 * documentation says otherwise.
 */
public class Scheduler implements ExternalInputHandler
{

/*
 * TODO:
 * Much of the careful thread synchronization in this file is
 * a leftover from the time when we allowed tasks to be queued
 * and cancelled asynchronously. It could eventually be removed,
 * but for now I want to keep it because we may need it again at
 * some point and it is pretty delicate and difficult to recreate.
 */

    protected static Logger log = Logger.getLogger("Scheduler");

    private static final long NOTASK = Long.MAX_VALUE;
    private static final long STOPPING = Long.MIN_VALUE;

    /** Number of scheduler time units per simulated second (1 GHz). */
    public static final long CLOCKRATE = 1000000000; 

    /** Current simulation time in scheduler time units (ns). */
    private volatile long currentTime;

    /** Scheduled time of next event, or NOTASK or STOPPING. */
    private long nextTime;

    /** Enables slowing the simulation to keep it in sync with wall time. */
    private boolean syncScheduler = true;

    /** Determines how often the time synchronization is checked. */
    private long syncQuantum = CLOCKRATE / 20;

    /** Determines speed of the simulation. */
    private long syncSimTimePerWallMs = CLOCKRATE / 1000;

    /** Gain on wall time since last synchronization, plus one syncQuantum. */
    private long syncTimeSaldo;

    /** Most recent value of <code>currentTimeMillis</code>. */
    private long syncWallTimeMillis;

    /** Queue containing pending synchronous events. */
    private PriorityQueue pq;

    /** Ordered list of pending asynchronous events (external input events). */
    private ArrayList pendingInput;

    /** The Cpu component controlled by this Scheduler. */
    private Cpu cpu;

    /** The dispatcher for external input events. */
    private ExternalInputHandler inputHandler;


    /**
     * A Task represents a pending discrete event, and is queued for
     * execution at a particular point in simulated time.
     */
    public static abstract class Task implements Runnable {

        private static final long NOSCHED = Long.MIN_VALUE;
        private volatile long lastTime;
        private long nextTime;
        private long interval;

        /** Constructs new task in the state NOSCHED. */
        public Task() {
            lastTime = NOSCHED;
            nextTime = NOSCHED;
            interval = 0;
        }

        /**
         * Cancels this task and resets it to state NOSCHED.
         * Returns true if the task was really cancelled, or false if
         * it was not even scheduled.
         */
        public synchronized boolean cancel() {
            if (nextTime == NOSCHED)
                return false;
            nextTime = NOSCHED;
            interval = 0;
            return true;
        }

	/** Returns the scheduled time of this task's most recent execution. */
        public long lastExecutionTime() {
            return lastTime;
        }
    }


    /** Constructs the Scheduler object. */
    public Scheduler()
    {
        currentTime = 0;
        nextTime = NOTASK;
        syncWallTimeMillis = System.currentTimeMillis();
        syncTimeSaldo = 0;
        pq = new PriorityQueue();
        pendingInput = new ArrayList();
    }


    /**
     * Set simulation synchronization parameters.
     * @param enable Enables slowing the simulation to keep it
     *   in sync with real time.
     * @param quantum Determines how often the synchronization is checked
     *   (in simulated nanoseconds).
     * @param simTimePerWallMs Determines the speed of the simulation
     *   (in simulated nanoseconds per real millisecond).
     */
    public void setSynchronization(boolean enable, long quantum, long simTimePerWallMs)
    {
        if (enable && quantum < 1)
            throw new IllegalArgumentException(
                "Invalid value for syncQuantum");
        if (enable && simTimePerWallMs < 1000)
            throw new IllegalArgumentException(
                "Invalid value for syncSimTimePerWallMs");
        syncScheduler = enable;
        syncQuantum = quantum;
        syncSimTimePerWallMs = simTimePerWallMs;
        syncTimeSaldo = 0;
        syncWallTimeMillis = System.currentTimeMillis();
    }


    /** Registers the Cpu component that will be driven by this Scheduler. */
    public void setCpu(Cpu c)
    {
        cpu = c;
    }


    /** Registers the object that will handle external input events. */
    public void setInputHandler(ExternalInputHandler h)
    {
        inputHandler = h;
    }


    /**
     * Returns current time in nanoseconds.
     * This method may safely be called from asynchronous context.
     */
    public long getCurrentTime()
    {
        return currentTime;
    }


    /** Queues a task for one-time execution at time t (in nanoseconds). */
    public synchronized void runTaskAt(Task tsk, long t)
    {
        synchronized (tsk) {
            if (tsk.nextTime != Task.NOSCHED)
                throw new IllegalStateException("Task already scheduled");
            tsk.nextTime = t;
        }
        pq.add(tsk, t);
        if (t < nextTime)
            nextTime = t;
    }


    /** Queues a task for one-time execution after a delay of d nanoseconds. */
    public synchronized void runTaskAfter(Task tsk, long d)
    {
        long t = currentTime + d;
        synchronized (tsk) {
            if (tsk.nextTime != Task.NOSCHED)
                throw new IllegalStateException("Task already scheduled");
            tsk.nextTime = t;
        }
        pq.add(tsk, t);
        if (t < nextTime)
            nextTime = t;
    }


    /** Queues a task for repeated execution at the specified interval. */
    public synchronized void runTaskEach(Task tsk, long interval)
    {
        long t = currentTime + interval;
        synchronized (tsk) {
            if (tsk.nextTime != Task.NOSCHED)
                throw new IllegalStateException("Task already scheduled");
            tsk.nextTime = t;
            tsk.interval = interval;
        }
        pq.add(tsk, t);
        if (t < nextTime)
            nextTime = t;
    }


    /**
     * Cancels all pending events and stop the simulation.
     * This method may safely be called from asynchronous context.
     */
    public synchronized void stopSimulation()
    {
        Task tsk = (Task) pq.removeFirst();
        while (tsk != null) {
            tsk.cancel();
            tsk = (Task) pq.removeFirst();
        }
        pq.clear();
        nextTime = STOPPING;
        // Kick simulation thread
        notify();
        cpu.setReschedule();
    }


    /**
     * Adds an external input event to the queue.
     * The simulation thread will push the event to the appropriate handler.
     * This should be the only way through which asynchronous input can enter
     * the simulation. This method may safely be called from asynchronous
     * context.
     */
    public synchronized void handleInput(ExternalInputEvent evt)
    {
        if (pendingInput.isEmpty()) {
            // Wake up the scheduler in case it is sleeping
            notify();
            // Kick the Cpu simulation to make it yield
            cpu.setReschedule();
        }
        pendingInput.add(evt);
    }


    /**
     * Returns the time in nanoseconds until the next pending event.
     * The Cpu component uses this method to find out how much work it can do.
     */
    public synchronized long getTimeToNextEvent()
    {
        if (nextTime == STOPPING || !pendingInput.isEmpty())
            return 0;
        else if (syncScheduler && nextTime > currentTime + syncQuantum)
            return syncQuantum;
        else
            return nextTime - currentTime;
    }


    /**
     * Advances the simulation clock by t nanoseconds.
     * This method is called to add to the simulation clock an amount
     * of time already spent by the Cpu simulation.
     * If synchronization to real time is enabled, the simulation will
     * be paused when it runs faster than wall clock time.
     */
    public void advanceTime(long t)
    {
        currentTime += t;
        if (syncScheduler) {
            syncTimeSaldo += t;
            if (syncTimeSaldo > 3 * syncQuantum) {
                // Check the wall clock
                long wallTime = System.currentTimeMillis();
                long wallDelta = wallTime - syncWallTimeMillis;
                syncWallTimeMillis = wallTime;
                if (wallDelta < 0)
                    wallDelta = 0; // some clown has set the system clock back
                syncTimeSaldo -= wallDelta * syncSimTimePerWallMs;
                if (syncTimeSaldo < 0)
                    syncTimeSaldo = 0;
                if (syncTimeSaldo > 2 * syncQuantum) {
                    // The simulation has gained more than one time quantum
                    long sleepTime =
                      (syncTimeSaldo - syncQuantum) / syncSimTimePerWallMs;
                    try {
                        if (syncTimeSaldo > 4 * syncQuantum) {
                            // Force a hard sleep
                            long s = syncQuantum / syncSimTimePerWallMs;
                            log.debug("sleep " + s + " ms");
                            Thread.sleep(s);
                            sleepTime -= s;
                        }
                        synchronized (this) {
                            // Sleep, but wake up on asynchronous events
                            log.debug("wait " + sleepTime + " ms");
                            if (pendingInput.isEmpty())
                                wait(sleepTime);
                        }
                    } catch (InterruptedException e) {
                        // should not happen
                        log.error("interrupted while sleeping", e);
                    }
                }
            }
        }
    }


    /**
     * Advances the simulation clock to the next pending event.
     * This method is called to update the simulation clock past an interval
     * in which there is nothing to do (Cpu halted, no other events scheduled).
     * If synchronization to real time is enabled, the simulation will
     * be paused when it runs faster than wall clock time. An incoming
     * asynchronous event may interrupt this pause.
     */
    private synchronized void skipToNextEvent()
    {

        // Check if advancing the time is even needed
        if (nextTime <= currentTime || !pendingInput.isEmpty())
            return;

        // Detect end of simulation
        if (nextTime == NOTASK) {
            nextTime = STOPPING;
            log.info("Cpu halted with no pending events; stopping");
        }

        if (syncScheduler) {
            syncTimeSaldo += nextTime - currentTime;
            if (syncTimeSaldo > 3 * syncQuantum) {
                // Check the wall clock
                long wallTime = System.currentTimeMillis();
                long wallDelta = wallTime - syncWallTimeMillis;
                syncWallTimeMillis = wallTime;
                if (wallDelta < 0)
                    wallDelta = 0; // some clown has set the system clock back
                syncTimeSaldo -= wallDelta * syncSimTimePerWallMs;
                if (syncTimeSaldo < 0)
                    syncTimeSaldo = 0;
                if (syncTimeSaldo > 2 * syncQuantum) {
                    // Skipping would give a gain of more than one time quantum
                    long sleepTime =
                      (syncTimeSaldo - syncQuantum) / syncSimTimePerWallMs;
                    try {
                        // Sleep, but wake up on asynchronous events
                        log.debug("pause " + sleepTime + " ms");
                        wait(sleepTime);
                    } catch (InterruptedException e) {
                        // should not happen
                        log.error("interrupted while pausing", e);
                    }
                    if (!pendingInput.isEmpty()) {
                        // We woke up from our sleep; find out how long
                        // we slept and how much simulated time has passed
                        wallTime = System.currentTimeMillis();
                        wallDelta = wallTime - syncWallTimeMillis;
                        syncWallTimeMillis = wallTime;
                        if (wallDelta < 0)
                            wallDelta = 0; // same clown again
                        syncTimeSaldo -= wallDelta * syncSimTimePerWallMs;
                        if ( syncTimeSaldo >
                             syncQuantum + nextTime - currentTime ) {
                            // No simulated time passed at all
                            syncTimeSaldo -= nextTime - currentTime;
                        } else if (syncTimeSaldo > syncQuantum) {
                            // Some simulated time passed, but not enough
                            currentTime =
                              nextTime - (syncTimeSaldo - syncQuantum);
                            syncTimeSaldo = syncQuantum;
                        } else {
                            // Oops, we even overslept
                            currentTime = nextTime;
                        }
                    } else {
                        // Assume we slept the whole interval
                        currentTime = nextTime;
                    }
                    return;
                }
            }
        }

        // Skip to the next pending event
        currentTime = nextTime;
    }


    /**
     * Dequeues the next task and return it if it is ready to run,
     * otherwise return null.
     */
    private final synchronized Task nextTask()
    {

        /*
         * TODO:
         * Also consider items from an ordered list of recorded
         * external input events.
         */

        if (nextTime > currentTime || nextTime == STOPPING)
            return null;

        Task tsk = (Task) pq.removeFirst();
        nextTime = pq.minPrio();
        if (tsk == null)
            return null;

        synchronized (tsk) {
            if (tsk.nextTime == Task.NOSCHED || tsk.nextTime > currentTime) {
                // Cancelled or rescheduled
                tsk = null;
            } else {
                // Task is ok to run
                tsk.lastTime = tsk.nextTime;
                if (tsk.interval > 0) {
                    // Schedule next execution
                    long t = tsk.nextTime + tsk.interval;
                    tsk.nextTime = t;
                    pq.add(tsk, t);
                    if (t < nextTime) nextTime = t;
                } else {
                    // Done with this task
                    tsk.nextTime = Task.NOSCHED;
                }
            }
        }

        return tsk;
    }


    /**
     * Runs the simulation.
     * This method returns when the simulation ends, usually after a call
     * to <code>stopSimulation()</code>.
     */
    public void run()
      throws Cpu.InvalidOpcodeException
    {
        ArrayList cleanInputBuf = new ArrayList();

        // Main loop
        while (true) {
            ArrayList inputBuf = null;
            Task tsk = null;

            synchronized (this) {

                // Detect the end of the simulation run
                if (nextTime == STOPPING) {
                    nextTime = pq.minPrio();
                    break;
                }

                if (!pendingInput.isEmpty()) {

                    // Fetch pending input events
                    inputBuf = pendingInput;
                    pendingInput = cleanInputBuf;

                } else if (nextTime <= currentTime) {
                    
                    // Fetch the next pending task
                    tsk = nextTask();
                    if (tsk == null) {
                        // This task was cancelled, go round again
                        continue;
                    }

                }

            }

            if (inputBuf != null) {

                // Process pending input events
                int n = inputBuf.size();
                for (int i = 0; i < n; i++) {
                    ExternalInputEvent evt = (ExternalInputEvent) inputBuf.get(i);
                    evt.timestamp = currentTime;
                    inputHandler.handleInput(evt);
                }
                inputBuf.clear();
                cleanInputBuf = inputBuf;

            } else if (tsk != null) {

                // Run the first pending task
                tsk.run();

            } else {

                // Run the Cpu simulation for a bit
                cpu.exec();
                if (cpu.isHalted()) {
                    // The Cpu is halted, skip immediately to the next event
                    skipToNextEvent();
                }

            }

        }

    }

}

/* end */

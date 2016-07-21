/*
 *  PriorityQueue.java
 *  Joris van Rantwijk
 */

package retro;

/**
 * Simple heap based priority queue.
 *
 * Adding elements and removing the element with minimum priority
 * both take O(log(n)) time in most cases.  Adding an element may
 * take O(n) time only when the underlying array needs to grow, but
 * this extra cost disappears when amortized over all operations.
 * Removing a specific (non-minimum) object from the queue requires
 * sequential search and therefore takes O(n) time.
 * <p>
 * The queue is non-stable: objects with equal priority are returned
 * in arbitrary order (but the order is still deterministic).
 * <p>
 * Methods are not synchronized; external synchronization is required when
 * a priority queue is used by multiple threads.
 */
public final class PriorityQueue
{

    private int nHeap;
    private Object[] heapObj;
    private long[] heapPri;

    /** Constructs a new empty priority queue. */
    public PriorityQueue()
    {
        nHeap = 0;
        heapObj = new Object[16];
        heapPri = new long[16];
    }

    /** Removes all elements from the priority queue. */
    public void clear()
    {
        nHeap = 0;
        heapObj = new Object[16];
        heapPri = new long[16];
    }

    /** Adds an object with specified priority to the queue. */
    public void add(Object a, long prio)
    {
        nHeap++;
        if (nHeap >= heapObj.length) {
            Object[] oldHeapObj = heapObj;
            long[] oldHeapPri = heapPri;
            heapObj = new Object[2*nHeap];
            heapPri = new long[2*nHeap];
            System.arraycopy(oldHeapObj, 0, heapObj, 0, nHeap);
            System.arraycopy(oldHeapPri, 0, heapPri, 0, nHeap);
        }

        heapPri[0] = Long.MIN_VALUE;    // element 0 is a sentinel
        int k = nHeap;
        while (heapPri[k / 2] > prio) {
            heapObj[k] = heapObj[k / 2];
            heapPri[k] = heapPri[k / 2];
            k = k / 2;
        }

        heapObj[k] = a;
        heapPri[k] = prio;
    }

    /**
     * Returns the lowest priority in the queue,
     * or returns Long.MAX_VALUE if the queue is empty.
     */
    public long minPrio()
    {
        return (nHeap > 0) ? heapPri[1] : Long.MAX_VALUE;
    }

    /**
     * Removes the object with lowest priority from the queue and returns it,
     * or returns null if the queue is empty.
     */
    public Object removeFirst()
    {
        if (nHeap == 0)
            return null;

        Object a = heapObj[1];

        Object vo = heapObj[nHeap];
        long   vp = heapPri[nHeap];
        nHeap--;

        int k = 1;
        while (k <= nHeap / 2) {
            int j = 2 * k;
            if (j < nHeap && heapPri[j] > heapPri[j+1])
                j++;
            if (vp <= heapPri[j])
                break;
            heapObj[k] = heapObj[j];
            heapPri[k] = heapPri[j];
            k = j;
        }
        heapObj[k] = vo;
        heapPri[k] = vp;

        return a;
    }

    /** Removes the specified object from the queue. */
    public void remove(Object a)
    {
        int k;
        for (k = 1; k <= nHeap && heapObj[k] != a; k++) ;

        if (k <= nHeap) {
            Object vo = heapObj[nHeap];
            long   vp = heapPri[nHeap];
            nHeap--;

            while (k <= nHeap / 2) {
                int j = 2 * k;
                if (j < nHeap && heapPri[j] > heapPri[j+1])
                    j++;
                if (vp <= heapPri[j])
                    break;
                heapObj[k] = heapObj[j];
                heapPri[k] = heapPri[j];
                k = j;
            }
            heapObj[k] = vo;
            heapPri[k] = vp;
        }
    }

    /** Returns the number of elements in the queue. */
    public int size()
    {
        return nHeap;
    }

    /** Returns true if the queue does not contain any elements. */
    public boolean isEmpty()
    {
        return (nHeap == 0);
    }

}

/* end */

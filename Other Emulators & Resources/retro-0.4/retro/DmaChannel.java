/*
 *  DmaChannel.java
 *  Joris van Rantwijk
 */

package retro;

/**
 * A device uses this interface to request DMA transactions from a specific
 * channel of a DMA controller.
 */
public interface DmaChannel
{

    /**
     * Called by a device to enable or disable a DMA request (DREQ).
     *
     * In response to dmaRequest(true), the DMA controller will schedule
     * a DMA transaction and make one or more calls to methods on the
     * DmaDevice interface that corresponds to this channel.  These callbacks
     * may corrur immediately from the context of the dmaRequest() invocation
     * or at a later time in the simulation.
     * <p>
     * When the device no longer wants DMA service, it must explicitly call
     * dmaRequest(false) to de-assert its DREQ line.
     */
    void dmaRequest(boolean enable);

    /**
     * Called by a device to signal external EOP to the DMA controller.
     */
    void dmaEop();

}

/* end */

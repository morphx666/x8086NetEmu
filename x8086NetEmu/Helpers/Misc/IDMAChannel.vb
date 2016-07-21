Public Interface IDMAChannel

    ' Called by a device to enable or disable a DMA request (DREQ).
    ' In response to dmaRequest(true), the DMA controller will schedule
    ' a DMA transaction and make one or more calls to methods on the
    ' DmaDevice interface that corresponds to this channel.  These callbacks
    ' may corrur immediately from the context of the dmaRequest() invocation
    ' or at a later time in the simulation.
    ' <p>
    ' When the device no longer wants DMA service, it must explicitly call
    ' dmaRequest(false) to de-assert its DREQ line.

    Sub DMARequest(enable As Boolean)

    ' Called by a device to signal external EOP to the DMA controller.
    Sub DMAEOP()
End Interface

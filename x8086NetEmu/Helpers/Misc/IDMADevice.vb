Public Interface IDMADevice
    ' Transfers a byte from memory to the device.
    ' This method is called one or more times by the DMA controller
    ' in response to a DMA request in read mode.  The device must
    ' read and process one byte.
    ' <p>
    ' The device may optionally call channel.dmaRequest(false) and/or
    ' channel.dmaEop() to indicate that no more bytes should be transferred
    ' in the current transaction.
    ' @param v data byte
    Sub DMARead(v As Byte)

    ' Transfers a byte from the device to memory.
    ' This method is called one or more times by the DMA controller
    ' in response to a DMA request in write mode.  The device must
    ' produce and write one byte.
    ' <p>
    ' The device may optionally call channel.dmaRequest(false) and/or
    ' channel.dmaEop() to indicate that no more bytes should be transferred
    ' in the current transaction.
    ' @return data byte
    Function DMAWrite() As Byte

    ' Called by the DMA controller to signal an internally generated EOP
    ' (caused by terminal count).
    Sub DMAEOP()
End Interface

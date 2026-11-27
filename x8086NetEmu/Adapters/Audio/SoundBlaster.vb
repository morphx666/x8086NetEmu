' Creative Sound Blaster 2.0 (DSP 2.01) at 220h, IRQ 5, 8-bit DMA channel 1.
'
' Written from the documented DSP programming interface:
'   2x0h-2x3h, 2x8h-2x9h  FM synthesizer (forwarded to the OPL2 / AdlibAdapter)
'   2x4h/2x5h             Mixer address/data (a plain register file; SB Pro-compatible software may probe it)
'   2x6h                  DSP reset (write 1 then 0; the DSP answers AAh)
'   2xAh                  DSP read data
'   2xCh                  DSP write command/data; read = write-buffer status (bit 7 clear = ready)
'   2xEh                  Read-buffer status (bit 7 set = data available); reading it acknowledges the 8-bit IRQ
'
' Sample data moves through the emulated 8237 DMA controller: the DSP raises DREQ once per sample period and
' the controller transfers the byte, so the guest sees the DMA address/count registers advance as on real
' hardware. The end-of-block interrupt is driven by the DSP's own block length, as on the real card.
Public Class SoundBlaster
    Inherits AudioProvider
    Implements IDMADevice

    Private Const DSP_VERSION_MAJOR As Byte = 2
    Private Const DSP_VERSION_MINOR As Byte = 1

    Private Enum TransferModes
        None
        Output8        ' 8-bit PCM playback (DMA read: memory -> DSP)
        Input8         ' 8-bit recording (DMA write: DSP -> memory), fed with silence
        Silence        ' Command 80h: timed silence, no DMA
        OutputAdpcm    ' ADPCM playback: bytes are consumed at the right pace (played as silence)
    End Enum

    ' ---- DSP state ----
    Private ReadOnly readBuffer As New Queue(Of Byte)()
    Private command As Integer = -1                 ' Command waiting for its arguments
    Private ReadOnly args As New List(Of Byte)()
    Private argsNeeded As Integer
    Private resetLatch As Boolean
    Private testRegister As Byte
    Private speakerOn As Boolean
    Private dacValue As Byte = 128                  ' Current output sample (unsigned 8-bit)

    Private sampleRate As Integer = 22050
    Private blockSize As Integer = &H7FF + 1        ' Set by 48h (stored as length + 1)
    Private mode As TransferModes = TransferModes.None
    Private autoInit As Boolean
    Private exitAutoInit As Boolean
    Private paused As Boolean
    Private bytesPerTick As Integer = 1             ' For ADPCM: samples per byte
    Private remaining As Integer                    ' Bytes left in the current block
    Private currentBlockSize As Integer

    ' ---- Mixer ----
    Private mixerIndex As Byte
    Private ReadOnly mixerRegs(256 - 1) As Byte

    ' ---- Wiring ----
    Private ReadOnly adLib As AdlibAdapter
    Private ReadOnly irq As InterruptRequest
    Private ReadOnly dma As IDMAChannel
    Private irqPending As Boolean
    Private mVolume As Double

    ' Exact sample clock: tick N of the current second is due at secondStartTime + N * HOSTCLOCK / rate
    Private secondStartTime As Long
    Private tickIndex As Integer
    Private tickRate As Integer

    Private Class TaskSC
        Inherits Scheduler.SchTask

        Public Sub New(owner As IOPortHandler)
            MyBase.New(owner)
        End Sub

        Public Overrides Sub Run()
            Owner.Run()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return Owner.Name
            End Get
        End Property
    End Class
    Private ReadOnly task As New TaskSC(Me)

    Public Sub New(cpu As X8086, adlib As AdlibAdapter, Optional port As UInt16 = &H220, Optional irq As Byte = 5, Optional dmaChannel As Byte = 1)
        MyBase.New(cpu)
        Me.adLib = adlib
        Me.irq = cpu.PIC?.GetIrqLine(irq)
        Me.dma = cpu.DMA?.BindChannel(dmaChannel, Me)

        For i As UInt16 = port To port + &HF
            RegisteredPorts.Add(i)
        Next

        ResetMixer()
    End Sub

    Public Overrides Sub InitAdapter()
        mVolume = 0.8
    End Sub

    Public Overrides Sub CloseAdapter()
        task.Cancel()
    End Sub

#Region "Ports"
    Public Overrides Function [In](port As UInt16) As Byte
        Select Case port And &HF
            Case &H0, &H2, &H8 : Return adLib.In(&H388)
            Case &H1, &H3, &H9 : Return adLib.In(&H389)
            Case &H5 : Return mixerRegs(mixerIndex)
            Case &HA : Return If(readBuffer.Count > 0, readBuffer.Dequeue(), CByte(0))
            Case &HC : Return &H7F ' Write buffer always ready (bit 7 clear)
            Case &HE
                ' Reading the read-buffer status also acknowledges the 8-bit DMA interrupt
                If irqPending Then
                    irqPending = False
                    irq?.Raise(False)
                End If
                Return If(readBuffer.Count > 0, &HFF, &H7F)
            Case Else : Return &HFF
        End Select
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        Select Case port And &HF
            Case &H0, &H2, &H8 : adLib.Out(&H388, value)
            Case &H1, &H3, &H9 : adLib.Out(&H389, value)
            Case &H4 : mixerIndex = value
            Case &H5
                If mixerIndex = 0 Then
                    ResetMixer()
                Else
                    mixerRegs(mixerIndex) = value
                End If
            Case &H6
                ' Reset: writing 1 then 0 resets the DSP, which then answers AAh
                If (value And 1) <> 0 Then
                    resetLatch = True
                ElseIf resetLatch Then
                    resetLatch = False
                    ResetDsp()
                End If
            Case &HC : WriteDsp(value)
        End Select
    End Sub

    Private Sub ResetMixer()
        Array.Clear(mixerRegs, 0, mixerRegs.Length)
        mixerRegs(&H4) = &H99 ' Voice volume
        mixerRegs(&H22) = &H99 ' Master volume
        mixerRegs(&H26) = &H99 ' FM volume
    End Sub
#End Region

#Region "DSP commands"
    Private Sub ResetDsp()
        StopTransfer()
        readBuffer.Clear()
        command = -1
        args.Clear()
        speakerOn = False
        dacValue = 128
        If irqPending Then
            irqPending = False
            irq?.Raise(False)
        End If
        readBuffer.Enqueue(&HAA)
    End Sub

    Private Sub WriteDsp(value As Byte)
        If command >= 0 Then
            args.Add(value)
            If args.Count < argsNeeded Then Exit Sub
            Dim c As Integer = command
            command = -1
            Execute(c, args.ToArray())
            args.Clear()
            Exit Sub
        End If

        Dim needed As Integer = ArgumentCount(value)
        If needed = 0 Then
            Execute(value, Array.Empty(Of Byte)())
        Else
            command = value
            argsNeeded = needed
        End If
    End Sub

    Private Shared Function ArgumentCount(cmd As Byte) As Integer
        Select Case cmd
            Case &H10, &H38, &H40, &HE0, &HE2, &HE4 : Return 1
            Case &H14, &H16, &H17, &H24, &H41, &H42, &H48, &H74, &H75, &H76, &H77, &H80 : Return 2
            Case Else : Return 0
        End Select
    End Function

    Private Sub Execute(cmd As Integer, a As Byte())
        Select Case cmd
            Case &H10 ' Direct 8-bit output
                dacValue = a(0)

            Case &H14, &H91 ' 8-bit single-cycle DMA output (91h: high-speed, length from 48h)
                StartTransfer(TransferModes.Output8, If(cmd = &H14, Length(a), blockSize), False)

            Case &H1C, &H90 ' 8-bit auto-init DMA output (90h: high-speed)
                StartTransfer(TransferModes.Output8, blockSize, True)

            Case &H16, &H17 : StartTransfer(TransferModes.OutputAdpcm, Length(a), False, 4) ' 2-bit ADPCM
            Case &H76, &H77 : StartTransfer(TransferModes.OutputAdpcm, Length(a), False, 3) ' 2.6-bit ADPCM
            Case &H74, &H75 : StartTransfer(TransferModes.OutputAdpcm, Length(a), False, 2) ' 4-bit ADPCM
            Case &H1F : StartTransfer(TransferModes.OutputAdpcm, blockSize, True, 4)
            Case &H7F : StartTransfer(TransferModes.OutputAdpcm, blockSize, True, 3)
            Case &H7D : StartTransfer(TransferModes.OutputAdpcm, blockSize, True, 2)

            Case &H20 ' Direct 8-bit input: no microphone, return silence
                readBuffer.Enqueue(128)

            Case &H24, &H99 ' 8-bit single-cycle DMA input
                StartTransfer(TransferModes.Input8, If(cmd = &H24, Length(a), blockSize), False)

            Case &H2C, &H98 ' 8-bit auto-init DMA input
                StartTransfer(TransferModes.Input8, blockSize, True)

            Case &H40 ' Time constant: rate = 1,000,000 / (256 - TC)
                sampleRate = 1_000_000 \ (256 - a(0))
                RestartClock()

            Case &H41, &H42 ' Output/input sample rate (high byte first)
                sampleRate = Math.Max(1, (CInt(a(0)) << 8) Or a(1))
                RestartClock()

            Case &H48 : blockSize = Length(a) ' Block transfer size for auto-init / high-speed
            Case &H80 : StartTransfer(TransferModes.Silence, Length(a), False) ' Timed silence

            Case &HD0                                       ' Pause 8-bit DMA (withdraw any request still queued)
                paused = True
                dma?.DMARequest(False)
            Case &HD4 : paused = False : RestartClock()     ' Continue 8-bit DMA
            Case &HD1 : speakerOn = True
            Case &HD3 : speakerOn = False
            Case &HD8 : readBuffer.Enqueue(If(speakerOn, &HFF, &H0))
            Case &HDA : exitAutoInit = True                 ' Exit auto-init after the current block

            Case &HE0 : readBuffer.Enqueue(Not a(0))        ' DSP identification: inverted argument
            Case &HE1                                       ' DSP version
                readBuffer.Enqueue(DSP_VERSION_MAJOR)
                readBuffer.Enqueue(DSP_VERSION_MINOR)
            Case &HE4 : testRegister = a(0)                 ' Write test register
            Case &HE8 : readBuffer.Enqueue(testRegister)    ' Read test register
            Case &HF2 : RaiseIrq()                          ' Force 8-bit IRQ
            Case &HF8 : readBuffer.Enqueue(0)               ' Undocumented
            ' 30h-38h (MIDI), E2h (DMA identification) and unknown commands are accepted and ignored
        End Select
    End Sub

    ' Lengths are programmed as "number of bytes - 1", low byte first
    Private Shared Function Length(a As Byte()) As Integer
        Return ((CInt(a(1)) << 8) Or a(0)) + 1
    End Function
#End Region

#Region "Transfers"
    Private Sub StartTransfer(newMode As TransferModes, size As Integer, isAutoInit As Boolean, Optional samplesPerByte As Integer = 1)
        mode = newMode
        ' Software written for later cards (where the speaker command has no effect) often never sends D1h,
        ' so starting a playback transfer turns the output on
        If newMode = TransferModes.Output8 OrElse newMode = TransferModes.OutputAdpcm Then speakerOn = True
        autoInit = isAutoInit
        exitAutoInit = False
        paused = False
        bytesPerTick = samplesPerByte
        currentBlockSize = size
        remaining = size
        RestartClock()
    End Sub

    Private Sub StopTransfer()
        mode = TransferModes.None
        paused = False
        task.Cancel()
        dma?.DMARequest(False)
    End Sub

    Private Sub RestartClock()
        task.Cancel()
        If mode = TransferModes.None OrElse paused Then Exit Sub
        ' ADPCM bytes carry several samples each, so they are requested proportionally less often
        tickRate = Math.Max(1, sampleRate \ bytesPerTick)
        secondStartTime = CPU.Sched.CurrentTime
        tickIndex = 0
        ScheduleNextTick()
    End Sub

    Private Sub ScheduleNextTick()
        Dim hostClock As Long = CLng(Scheduler.HOSTCLOCK) ' Long math (mixing in ULong would promote to Decimal)
        tickIndex += 1
        If tickIndex >= tickRate Then
            ' Rebase once per simulated second so the multiplication below never overflows
            secondStartTime += hostClock
            tickIndex = 0
        End If
        CPU.Sched.RunTaskAt(task, secondStartTime + CLng(tickIndex) * hostClock \ tickRate)
    End Sub

    ' Scheduler task: one sample period has elapsed
    Public Overrides Sub Run()
        If mode = TransferModes.None OrElse paused Then Exit Sub
        ScheduleNextTick()

        Select Case mode
            Case TransferModes.Silence
                ByteTransferred()
            Case Else
                ' Ask the DMA controller for the next byte; it calls DMARead/DMAWrite, which drop the request
                dma?.DMARequest(True)
        End Select
    End Sub

    ' DMA read: memory -> DSP
    Public Sub DMARead(b As Byte) Implements IDMADevice.DMARead
        dma?.DMARequest(False)
        If mode = TransferModes.Output8 Then dacValue = b
        If mode = TransferModes.Output8 OrElse mode = TransferModes.OutputAdpcm Then ByteTransferred()
    End Sub

    ' DMA write: DSP -> memory (recording; there is no input source, so this is silence)
    Public Function DMAWrite() As Byte Implements IDMADevice.DMAWrite
        dma?.DMARequest(False)
        If mode = TransferModes.Input8 Then ByteTransferred()
        Return 128
    End Function

    ' The DMA controller reached terminal count. The DSP's own block length decides when the transfer ends
    ' and when the interrupt fires, so nothing to do here.
    Public Sub DMAEOP() Implements IDMADevice.DMAEOP
    End Sub

    Private Sub ByteTransferred()
        remaining -= 1
        If remaining > 0 Then Exit Sub

        RaiseIrq()
        If autoInit AndAlso Not exitAutoInit Then
            remaining = If(blockSize > 0, blockSize, currentBlockSize)
        Else
            mode = TransferModes.None
            task.Cancel()
        End If
    End Sub

    Private Sub RaiseIrq()
        irqPending = True
        irq?.Raise(True)
    End Sub
#End Region

    Public Overrides ReadOnly Property Sample As Int16
        Get
            If Not speakerOn Then Return 0
            Return (CInt(dacValue) - 128) * mVolume
        End Get
    End Property

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.AudioDevice
        End Get
    End Property

    Public Overrides ReadOnly Property Vendor As String
        Get
            Return "Creative Technology Pte Ltd"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return DSP_VERSION_MAJOR
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return DSP_VERSION_MINOR
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Sound Blaster 2.0"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Sound Blaster 2.0"
        End Get
    End Property
End Class

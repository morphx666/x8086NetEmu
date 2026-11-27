' Serial mouse on COM1 (3F8h-3FFh, IRQ 4): an 8250 UART with a Microsoft 2-button serial mouse attached.
'
' UART side
'   Implements the registers a mouse driver relies on: RBR/THR, IER, IIR, LCR, MCR, LSR, MSR, SCR and the
'   divisor latch (DLAB), with proper interrupt identification, the PC's OUT2 gating of the IRQ line and
'   loopback mode (used by drivers to detect the UART).
'   Received bytes are delivered one at a time at the programmed baud rate (a mouse uses 1200 baud 7N1,
'   ~7.5 ms per byte), so - like on real hardware - each byte raises its own interrupt and the driver
'   always finds a consistent receiver state.
'
' Mouse side (Microsoft protocol)
'   Raising DTR and RTS powers the mouse, which identifies itself by sending an "M".
'   Movement reported by the host is accumulated between packets (never dropped) and sent as 3-byte
'   packets with the full -128..127 range per packet; button changes are queued so quick clicks are not
'   lost when they happen faster than packets can be sent.
Public Class MouseAdapter
    Inherits Adapter
    Implements IExternalInputHandler

    Private Const BASEPORT As UInt16 = &H3F8
    Private Const UART_CLOCK As Integer = 115_200 ' 1.8432 MHz crystal / 16

    ' LSR bits
    Private Const LSR_DR As Byte = &H1   ' Data ready
    Private Const LSR_OE As Byte = &H2   ' Overrun error
    Private Const LSR_THRE As Byte = &H20 ' Transmitter holding register empty
    Private Const LSR_TEMT As Byte = &H40 ' Transmitter empty

    ' MCR bits
    Private Const MCR_DTR As Byte = &H1
    Private Const MCR_RTS As Byte = &H2
    Private Const MCR_OUT2 As Byte = &H8  ' On the PC, gates the UART's interrupt output onto the IRQ line
    Private Const MCR_LOOP As Byte = &H10

    ' ---- UART state ----
    Private rbr As Byte
    Private rbrFull As Boolean
    Private ier As Byte
    Private lcr As Byte
    Private mcr As Byte
    Private scr As Byte
    Private dll As Byte = &H60 ' Divisor latch: 96 = 1200 baud
    Private dlm As Byte
    Private lsrErrors As Byte
    Private thrEmptyIntPending As Boolean
    Private msrLines As Byte   ' CTS/DSR/RI/DCD (bits 4-7), only driven in loopback mode
    Private msrDelta As Byte   ' DCTS/DDSR/TERI/DDCD (bits 0-3)
    Private irqLevel As Boolean
    Private ReadOnly irq As InterruptRequest

    ' ---- Mouse state ----
    Private powered As Boolean
    Private ReadOnly txQueue As New Queue(Of Byte)() ' Bytes the mouse is sending to the UART
    Private accX As Integer                         ' Movement not yet sent
    Private accY As Integer
    Private hostButtons As Byte                     ' Latest host button state (Left = 2, Right = 1)
    Private ReadOnly buttonQueue As New Queue(Of Byte)()

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
    Private ReadOnly byteTask As New TaskSC(Me)

    ' Used by frontends that report absolute positions and re-center the host cursor (legacy WinForms)
    Public Property MidPointOffset As XPoint

    Public Property IsCaptured As Boolean

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
        If MyBase.CPU.PIC IsNot Nothing Then irq = MyBase.CPU.PIC.GetIrqLine(4)

        For i As UInt16 = BASEPORT To BASEPORT + 7
            RegisteredPorts.Add(i)
        Next
    End Sub

#Region "UART registers"
    Private ReadOnly Property Dlab As Boolean
        Get
            Return (lcr And &H80) <> 0
        End Get
    End Property

    Private ReadOnly Property LoopbackMode As Boolean
        Get
            Return (mcr And MCR_LOOP) <> 0
        End Get
    End Property

    Public Overrides Function [In](port As UInt16) As Byte
        Select Case port - BASEPORT
            Case 0
                If Dlab Then Return dll
                Dim value As Byte = rbr
                rbrFull = False
                UpdateIrq()
                Return value

            Case 1 : Return If(Dlab, dlm, ier)
            Case 2 : Return ReadIIR()
            Case 3 : Return lcr
            Case 4 : Return mcr

            Case 5
                Dim value As Byte = LSR_THRE Or LSR_TEMT Or lsrErrors Or If(rbrFull, LSR_DR, 0)
                lsrErrors = 0 ' Error bits are cleared by reading the LSR
                UpdateIrq()
                Return value

            Case 6
                Dim value As Byte = msrLines Or msrDelta
                msrDelta = 0 ' Delta bits are cleared by reading the MSR
                UpdateIrq()
                Return value

            Case 7 : Return scr
        End Select

        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        Select Case port - BASEPORT
            Case 0
                If Dlab Then
                    dll = value
                Else
                    WriteTHR(value)
                End If

            Case 1
                If Dlab Then
                    dlm = value
                Else
                    ' Enabling the THRE interrupt while the THR is empty raises it immediately
                    If (value And 2) <> 0 AndAlso (ier And 2) = 0 Then thrEmptyIntPending = True
                    ier = value And &HF
                    UpdateIrq()
                End If

            Case 2 ' FCR: the 8250 has no FIFO
            Case 3 : lcr = value
            Case 4 : WriteMCR(value)
            Case 5, 6 ' LSR/MSR are read-only
            Case 7 : scr = value
        End Select
    End Sub

    Private Function InterruptPending() As Boolean
        Return ((ier And 4) <> 0 AndAlso lsrErrors <> 0) OrElse
               ((ier And 1) <> 0 AndAlso rbrFull) OrElse
               ((ier And 2) <> 0 AndAlso thrEmptyIntPending) OrElse
               ((ier And 8) <> 0 AndAlso msrDelta <> 0)
    End Function

    ' Interrupt identification, in 8250 priority order
    Private Function ReadIIR() As Byte
        If (ier And 4) <> 0 AndAlso lsrErrors <> 0 Then Return &H6 ' Receiver line status
        If (ier And 1) <> 0 AndAlso rbrFull Then Return &H4        ' Received data available
        If (ier And 2) <> 0 AndAlso thrEmptyIntPending Then         ' THR empty (cleared by reading IIR)
            thrEmptyIntPending = False
            UpdateIrq()
            Return &H2
        End If
        If (ier And 8) <> 0 AndAlso msrDelta <> 0 Then Return &H0  ' Modem status
        Return &H1                                                  ' No interrupt pending
    End Function

    Private Sub WriteTHR(value As Byte)
        ' The mouse ignores anything sent to it; in loopback mode the byte comes straight back
        If LoopbackMode Then ReceiveByte(value)

        ' Transmission completes immediately
        If (ier And 2) <> 0 Then thrEmptyIntPending = True
        UpdateIrq()
    End Sub

    Private Sub WriteMCR(value As Byte)
        Dim wasPowered As Boolean = powered
        mcr = value And &H1F

        If LoopbackMode Then
            ' Modem outputs are looped back to the modem inputs: RTS->CTS, DTR->DSR, OUT1->RI, OUT2->DCD
            Dim lines As Byte = ((mcr And &HF) << 4)
            lines = (lines And &HC0) Or ((mcr And MCR_RTS) << 3) Or ((mcr And MCR_DTR) << 5)
            Dim changed As Byte = lines Xor msrLines
            If (changed And &H10) <> 0 Then msrDelta = msrDelta Or &H1                               ' DCTS
            If (changed And &H20) <> 0 Then msrDelta = msrDelta Or &H2                               ' DDSR
            If (changed And &H40) <> 0 AndAlso (lines And &H40) = 0 Then msrDelta = msrDelta Or &H4 ' TERI
            If (changed And &H80) <> 0 Then msrDelta = msrDelta Or &H8                               ' DDCD
            msrLines = lines
        Else
            msrLines = 0 ' A serial mouse doesn't drive any modem status line
        End If

        ' The mouse is powered from DTR and RTS (and is disconnected while in loopback mode)
        powered = (mcr And (MCR_DTR Or MCR_RTS)) = (MCR_DTR Or MCR_RTS) AndAlso Not LoopbackMode
        If powered AndAlso Not wasPowered Then PowerUpMouse()
        If wasPowered AndAlso Not powered Then ResetMouseState()

        UpdateIrq()
    End Sub

    ' A byte arrives at the receiver
    Private Sub ReceiveByte(value As Byte)
        If rbrFull Then lsrErrors = lsrErrors Or LSR_OE
        rbr = value
        rbrFull = True
        UpdateIrq()
    End Sub

    ' On the PC the UART's INTR output reaches the PIC only when OUT2 is set (and never in loopback mode)
    Private Sub UpdateIrq()
        Dim level As Boolean = (mcr And MCR_OUT2) <> 0 AndAlso Not LoopbackMode AndAlso InterruptPending()
        If level <> irqLevel Then
            irqLevel = level
            irq?.Raise(level)
        End If
    End Sub

    ' Time to transmit one character at the programmed baud rate and frame format
    Private Function CharTime() As Long
        Dim divisor As Integer = (CInt(dlm) << 8) Or dll
        If divisor = 0 Then divisor = &H60
        Dim bits As Integer = 1 +                                ' start
                              5 + (lcr And 3) +                  ' data
                              If((lcr And 8) <> 0, 1, 0) +       ' parity
                              If((lcr And 4) <> 0, 2, 1)         ' stop
        Return CLng(Scheduler.HOSTCLOCK) * bits * divisor \ UART_CLOCK
    End Function
#End Region

#Region "Mouse"
    Private Sub PowerUpMouse()
        ResetMouseState()
        txQueue.Enqueue(Asc("M")) ' Microsoft mouse identification
        ScheduleNextByte()
    End Sub

    Private Sub ResetMouseState()
        txQueue.Clear()
        buttonQueue.Clear()
        accX = 0
        accY = 0
    End Sub

    ' Builds the next 3-byte packet from pending button changes and accumulated movement
    Private Function BuildPacket() As Boolean
        Dim buttons As Byte = hostButtons
        Dim buttonChange As Boolean = buttonQueue.Count > 0
        If buttonChange Then buttons = buttonQueue.Dequeue()
        If Not buttonChange AndAlso accX = 0 AndAlso accY = 0 Then Return False

        Dim dx As Integer = Math.Max(-128, Math.Min(127, accX))
        Dim dy As Integer = Math.Max(-128, Math.Min(127, accY))
        accX -= dx
        accY -= dy

        '         1 L R Y7 Y6 X7 X6
        txQueue.Enqueue(CByte(&H40 Or (buttons << 4) Or (((dy >> 6) And 3) << 2) Or ((dx >> 6) And 3)))
        txQueue.Enqueue(CByte(dx And &H3F)) ' X5-X0
        txQueue.Enqueue(CByte(dy And &H3F)) ' Y5-Y0
        Return True
    End Function

    Private Sub ScheduleNextByte()
        If Not powered Then Exit Sub
        If byteTask.NextTime <> Scheduler.SchTask.NOSCHED Then Exit Sub ' Already scheduled
        If txQueue.Count = 0 AndAlso Not BuildPacket() Then Exit Sub
        CPU.Sched.RunTaskAfter(byteTask, CharTime())
    End Sub

    ' Scheduler task: one character time has elapsed, deliver the next byte to the UART
    Public Overrides Sub Run()
        If Not powered OrElse txQueue.Count = 0 Then Exit Sub

        If rbrFull Then
            ' The driver hasn't read the previous byte yet; hold this one instead of overrunning, so a
            ' slow (or not yet installed) driver never receives a broken packet
            CPU.Sched.RunTaskAfter(byteTask, CharTime())
            Exit Sub
        End If

        ReceiveByte(txQueue.Dequeue())
        ScheduleNextByte()
    End Sub

    ' Host mouse input (runs on the emulation thread, via the scheduler's input queue)
    Public Sub HandleInput(e As ExternalInputEvent) Implements IExternalInputHandler.HandleInput
        Dim m As XMouseEventArgs = CType(e.Event, XMouseEventArgs)

        Dim buttons As Byte = 0
        If (m.Button And XEventArgs.MouseButtons.Left) <> 0 Then buttons = buttons Or 2
        If (m.Button And XEventArgs.MouseButtons.Right) <> 0 Then buttons = buttons Or 1

        If Not powered Then
            hostButtons = buttons
            Exit Sub
        End If

        ' Bound the backlog so a long stall can't queue minutes of movement
        accX = Math.Max(-4096, Math.Min(4096, accX + m.X - MidPointOffset.X))
        accY = Math.Max(-4096, Math.Min(4096, accY + m.Y - MidPointOffset.Y))

        If buttons <> hostButtons Then
            If buttonQueue.Count < 16 Then buttonQueue.Enqueue(buttons)
            hostButtons = buttons
        End If

        ScheduleNextByte()
    End Sub
#End Region

    Public Overrides Sub CloseAdapter()
        byteTask.Cancel()
    End Sub

    Public Overrides Sub InitAdapter()
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Mouse"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Serial Mouse at COM1"
        End Get
    End Property

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.SerialMouseCOM1
        End Get
    End Property

    Public Overrides ReadOnly Property Vendor As String
        Get
            Return "xFX JumpStart"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return 2
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 0
        End Get
    End Property
End Class

Public Class PPI8255
    Inherits IOPortHandler

    Private sched As Scheduler
    Private irq As InterruptRequest
    Private timer As PIT8254

    Private ppiB As UInt32
    Private keyBuf As String
    Private lastKeyCode As UInt16 = 0
    Private keyShiftPending As Boolean

    Private keyMap As KeyMap
    Private keyUpStates(16 - 1) As Boolean

    Private cpu As X8086

    Private Class TaskSC
        Inherits Scheduler.Task

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
    Private task As Scheduler.Task = New TaskSC(Me)

    ' Set configuration switch data to be reported by PPI.
    ' bit 0: diskette drive present
    ' bit 1: math coprocessor present
    ' bits 3-2: memory size:
    '   00=256k, 01=512k, 10=576k, 11=640k
    ' bits 5-4: initial video mode:
    '   00=EGA/VGA, 01=CGA 40x25, 10=CGA 80x25 color, 11=MDA 80x25
    ' bits 7-6: one less than number of diskette drives (1 - 4 drives)
    Public Property SwitchData As Byte

    Public Sub New(cpu As X8086, irq As InterruptRequest)
        For i As Integer = &H60 To &H6F
            RegisteredPorts.Add(i)
        Next

        'PPISystemControl = x8086.WordToBitsArray(&HA5, PPISystemControl.Length)
        'PPI = x8086.WordToBitsArray(&HA, PPISystemControl.Length)
        'PPICommandModeRegister = &H99

        Me.cpu = cpu
        Me.sched = cpu.Sched
        Me.irq = irq
        If cpu.PIT IsNot Nothing Then
            timer = cpu.PIT
            timer.SetCh2Gate((ppiB And 1) <> 0)
        End If

        keyBuf = ""
        keyShiftPending = False
        keyMap = New KeyMap()
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Programmable Peripheral Interface"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "8255"
        End Get
    End Property

    Public Overrides Function [In](port As UInt16) As Byte
        Select Case port And 3
            Case 0 ' port &h60 (PPI port A)
                ' Return keyboard data if bit 7 in port B is cleared.
                Return If((ppiB And &H80) = 0, GetKeyData(), 0)
            Case 1 ' port &h61 (PPI port B)
                ' Return last value written to the port.
                Return ppiB
            Case 2 ' port &h62 (PPI port C)
                Return GetStatusByte()
            Case Else
                ' Reading from port &h63 is not supported
                Return &HFF
        End Select
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        Select Case port And 3
            Case 1
                ' Write to port 0x61 (system control port)
                ' bit 0: gate signal for timer channel 2
                ' bit 1: speaker control: 0=disconnect, 1=connect to timer 2
                ' bit 3: read low(0) or high(1) nibble of S2 switches
                ' bit 4: NMI RAM parity check disable
                ' bit 5: NMI I/O check disable
                ' bit 6: enable(1) or disable(0) keyboard clock ??
                ' bit 7: pulse 1 to reset keyboard and IRQ1
                Dim oldv As UInt32 = ppiB
                ppiB = value
                If (timer IsNot Nothing) AndAlso ((oldv Xor value) And 1) <> 0 Then
                    timer.SetCh2Gate((ppiB And 1) <> 0)
#If Win32 Then
                    If timer.Speaker IsNot Nothing Then timer.Speaker.Enabled = (value And 1) = 1
#End If
                End If

        End Select
    End Sub

    Public Overrides Sub Run()
        keyShiftPending = False
        TrimBuffer()
        If keyBuf.Length() > 0 AndAlso irq IsNot Nothing Then irq.Raise(True)
    End Sub

    Private Sub TrimBuffer()
        SyncLock keyBuf
            keyBuf = keyBuf.Substring(1)
            Array.Copy(keyUpStates, 1, keyUpStates, 0, keyUpStates.Length - 1)
        End SyncLock
    End Sub

    ' Store a scancode byte in the buffer
    Public Sub PutKeyData(v As Integer, isKeyUp As Boolean)
        If keyBuf.Length = 16 Then TrimBuffer()

        SyncLock keyBuf
            keyBuf += Convert.ToChar(v)
            keyUpStates(keyBuf.Length - 1) = isKeyUp

            If keyBuf.Length = 1 AndAlso irq IsNot Nothing Then irq.Raise(True)
        End SyncLock
    End Sub

    Public Function Reset() As Boolean
        Dim r As Boolean

        SyncLock keyBuf
            If keyBuf.Length = 0 Then
                r = False
            Else
                keyBuf = ""
                lastKeyCode = -1
                keyShiftPending = False

                For i As Integer = 0 To keyUpStates.Length - 1
                    keyUpStates(i) = False
                Next

                r = True
            End If
        End SyncLock

        Return r
    End Function

    ' Get a scancode byte from the buffer
    Public Function GetKeyData() As UInt16
        ' release interrupt
        If irq IsNot Nothing Then irq.Raise(False)
        ' if the buffer is empty, we just return the most recent byte 

        SyncLock keyBuf
            If keyBuf.Length() > 0 Then
                ' read byte from buffer
                lastKeyCode = keyMap.GetScanCode(AscW(keyBuf(0))) And &HFF
                If keyUpStates(0) Then lastKeyCode = lastKeyCode Or &H80

                ' wait .5 msec before going to the next byte
                If Not keyShiftPending Then
                    keyShiftPending = True
                    sched.RunTaskAfter(task, 500_000)
                End If
            End If
        End SyncLock

        ' return scancode byte
        Return lastKeyCode
    End Function

    ' Get status byte for Port C read.
    ' bits 3-0: low/high nibble of S2 byte depending on bit 3 of port B
    ' bit 4: inverted speaker signal
    ' bit 5: timer 2 output status
    ' bit 6: I/O channel parity error occurred (we always set it to 0)
    ' bit 7: RAM parity error occurred (we always set it to 0)
    Private Function GetStatusByte() As Byte
        Dim timerout As Boolean = If(timer Is Nothing, 0, timer.GetOutput(2))
        Dim speakerout As Boolean = timerout AndAlso ((ppiB And 2) <> 0)
        Dim vh As Integer = If(speakerout, 0, &H10) Or If(timerout, &H20, 0)
        Dim vl As Integer = If((ppiB And &H8) = 0, SwitchData, SwitchData >> 4)
        Return (vh And &HF0) Or (vl And &HF)
    End Function
End Class

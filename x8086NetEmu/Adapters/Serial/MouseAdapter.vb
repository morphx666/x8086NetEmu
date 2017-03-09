Public Class MouseAdapter
    Inherits Adapter
    Implements IExternalInputHandler

    Private Class SerialMouse
        Public reg(8 - 1) As Integer
        Public buf(16 - 1) As Integer
        Public bufPtr As Integer
    End Class

    Private mCPU As x8086
    Private sm As New SerialMouse()
    Private irq As PIC8259.IRQLine

    Private lastX As Integer = Integer.MinValue
    Private lastY As Integer = Integer.MinValue

    Private Const M As Integer = Asc("M")

    Public Sub New(cpu As x8086)
        mCPU = cpu
        If mCPU.PIC IsNot Nothing Then irq = mCPU.PIC.GetIrqLine(4)

        For i As Integer = &H3F8 To &H3F8 + 7
            ValidPortAddress.Add(i)
        Next
    End Sub

    Public Overrides Function [In](port As Integer) As Integer
        Dim tmp As Integer

        Select Case port
            Case &H3F8 ' Transmit/Receive Buffer
                If irq IsNot Nothing Then
                    tmp = sm.buf(0)
                    Array.Copy(sm.buf, 1, sm.buf, 0, 15)
                    sm.bufPtr -= 1

                    If sm.bufPtr < 0 Then sm.bufPtr = 0
                    If sm.bufPtr > 0 Then irq.Raise(True)

                    sm.reg(4) = (Not sm.reg(4)) And 1

                    Return tmp
                End If

                Return tmp
            Case &H3FD ' Line Status Register - LSR
                If sm.bufPtr > 0 Then
                    tmp = 1
                Else
                    tmp = 0
                End If

                'Return tmp
                'Return &H60 Or tmp
                Return &H1
        End Select

        Return sm.reg(port And 7)
    End Function

    Public Overrides Sub Out(port As Integer, value As Integer)
        Dim oldReg As Integer = sm.reg(port And 7)
        sm.reg(port And 7) = value

        Select Case port
            Case &H3FC ' Modem Control Register - MCR
                If (value And 1) <> (oldReg And 1) Then ' Software toggling of this register
                    sm.bufPtr = 0 '                       causes the mouse to reset and fill the buffer
                    BufSerMouseData(M) '                  with a bunch of ASCII 'M' characters.
                    BufSerMouseData(M) '                  this is intended to be a way for
                    BufSerMouseData(M) '                  drivers to verify that there is
                    BufSerMouseData(M) '                  actually a mouse connected to the port.
                    BufSerMouseData(M)
                End If
        End Select
    End Sub

    Private Sub BufSerMouseData(value As Byte)
        If irq IsNot Nothing Then
            If sm.bufPtr = 16 Then Exit Sub
            If sm.bufPtr = 0 Then irq.Raise(True)

            sm.buf(sm.bufPtr) = value
            sm.bufPtr += 1
        End If
    End Sub

    Public Sub HandleInput(e As ExternalInputEvent) Implements IExternalInputHandler.HandleInput
        Dim m As MouseEventArgs = CType(e.TheEvent, MouseEventArgs)

        If lastX = Integer.MinValue Then lastX = m.X
        If lastY = Integer.MinValue Then lastY = m.Y

        Dim rX As Integer = (m.X - lastX) / (mCPU.VideoAdapter.Zoom * 2)
        Dim rY As Integer = (m.Y - lastY) / ( mCPU.VideoAdapter.Zoom * 2)

        Debug.WriteLine($"{m.X}, {m.Y}")

        Dim highbits As Integer = 0
        If rX < 0 Then highbits = 3
        If rY < 0 Then highbits = highbits Or &HC

        Dim btns As Integer = 0
        If m.Button And MouseButtons.Left Then btns = btns Or 2
        If m.Button And MouseButtons.Right Then btns = btns Or 1

        BufSerMouseData(&H40 Or (btns << 4) Or highbits)
        BufSerMouseData(rX And &H3F)
        BufSerMouseData(rY And &H3F)

        lastX = m.X
        lastY = m.Y
    End Sub

    Public Overrides Sub CloseAdapter()

    End Sub

    Public Overrides Sub InitiAdapter()

    End Sub

    Public Overrides Sub Run()

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

    Public Overrides ReadOnly Property Type As Adapter.AdapterType
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
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 1
        End Get
    End Property
End Class

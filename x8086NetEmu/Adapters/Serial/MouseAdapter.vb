Public Class MouseAdapter
    Inherits Adapter
    Implements IExternalInputHandler

    Private Class SerialMouse
        Public reg(8 - 1) As UInt32
        Public buf(16 - 1) As UInt32
        Public bufPtr As UInt32
    End Class

    Private sm As New SerialMouse()
    Private irq As PIC8259.IRQLine

    Public Property MidPointOffset As Point

    Public Property IsCaptured As Boolean

    Private Const M As Byte = Asc("M")

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
        If MyBase.CPU.PIC IsNot Nothing Then irq = MyBase.CPU.PIC.GetIrqLine(4)

        For i As UInt32 = &H3F8 To &H3F8 + 7
            ValidPortAddress.Add(i)
        Next
    End Sub

    Public Overrides Function [In](port As UInt16) As Byte
        Dim tmp As Integer

        Select Case port
            Case &H3F8 ' Transmit/Receive Buffer
                If irq IsNot Nothing Then
                    tmp = sm.buf(0)
                    Array.Copy(sm.buf, 1, sm.buf, 0, 15)
                    sm.bufPtr = (sm.bufPtr - 1) And &HF

                    If sm.bufPtr < 0 Then sm.bufPtr = 0
                    If sm.bufPtr > 0 Then irq.Raise(True)

                    sm.reg(4) = (Not sm.reg(4)) And 1
                End If

                Return tmp
            Case &H3FD ' Line Status Register - LSR
                tmp = If(sm.bufPtr > 0, 1, 0)

                Return tmp
                'Return &H60 Or tmp
                'Return &H1
        End Select

        Return sm.reg(port And 7)
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
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
                MyBase.CPU.Flags.OF = 1
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
        Dim m As MouseEventArgs = CType(e.Event, MouseEventArgs)
        Dim p As New Point(m.X - MidPointOffset.X, (m.Y - MidPointOffset.Y) / 1.2)

        Dim highBits As Byte = 0
        If p.X < 0 Then highBits = &B11
        If p.Y < 0 Then highBits = highBits Or &B1100

        Dim btns As Byte = 0
        If (m.Button And MouseButtons.Left) = MouseButtons.Left Then btns = btns Or 2
        If (m.Button And MouseButtons.Right) = MouseButtons.Right Then btns = btns Or 1

        BufSerMouseData(&H40 Or (btns << 4) Or highBits)
        BufSerMouseData(p.X And &H3F)
        BufSerMouseData(p.Y And &H3F)
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
            Return 1
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
Imports System.Threading
Imports System.Runtime.InteropServices

Public Class PPI8255_ALT
    Inherits IOPortHandler

    Private sched As Scheduler
    Private irq As InterruptRequest

    Private keyBuf As String
    Private lastKeyCode As Integer
    Private keyShiftPending As Boolean
    Private keyMap As KeyMap
    Private keyUpStates(16 - 1) As Boolean

    Private cpu As X8086

    Private Delegate Function ReadFunction() As Byte
    Private Delegate Sub WriteFunction(v As Byte)

    Public PortA(2 - 1) As Byte
    Public PortB As Byte
    Public PortC(2 - 1) As Byte

    Private Structure Port
        Public Input As Byte
        Public Output As Byte

        Public Inp As Byte

        Public Read As ReadFunction
        Public Write As WriteFunction
    End Structure

    Private modeGroupA As Byte
    Private modeGroupB As Byte
    Private mode As Byte
    Private ports(3 - 1) As Port

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

    Public Sub New(cpu As X8086, irq As InterruptRequest)
        Me.cpu = cpu
        Me.sched = cpu.Sched
        Me.irq = irq

        keyBuf = ""
        keyShiftPending = False
        keyMap = New KeyMap()

        modeGroupA = 0
        modeGroupB = 0
        mode = &H80

        For i As Integer = 0 To 3 - 1
            ports(i).Input = 0
            ports(i).Output = 0
            ports(i).Inp = 0

            Select Case i
                Case 0 ' A
                    ports(i).Read = New ReadFunction(Function() As Byte
                                                         If (PortB And &H80) <> 0 Then
                                                             Return PortA(0)
                                                         Else
                                                             PortA(1) = GetKeyData() And &HFF
                                                             Return PortA(1)
                                                         End If
                                                     End Function)
                Case 1 ' B
                    ports(i).Write = New WriteFunction(Sub(v As Byte)
                                                           Dim old As Integer = PortB
                                                           PortB = v

                                                           cpu.PIT.SetCh2Gate((v And 1) <> 0)

                                                           If ((old Xor v) And 2) <> 0 Then
#If Win32 Then
                                                               cpu.PIT.Speaker.Enabled = ((v And 2) = 2)
#End If
                                                           End If
                                                       End Sub)
                Case 2 ' C
                    ports(i).Read = New ReadFunction(Function()
                                                         If cpu.Model = X8086.Models.IBMPC_5160 Then
                                                             If (PortB And &H8) <> 0 Then
                                                                 Return PortC(1)
                                                             Else
                                                                 Return PortC(0)
                                                             End If
                                                         Else
                                                             ' No CAS -------------
                                                             PortC(0) = PortC(0) And (Not &H10)
                                                             PortC(1) = PortC(1) And (Not &H10)
                                                             ' --------------------

                                                             If (PortB And &H4) <> 0 Then
                                                                 Return PortC(0)
                                                             Else
                                                                 Return PortC(1)
                                                             End If
                                                         End If
                                                     End Function)
            End Select
        Next

        For i As Integer = &H60 To &H6F
            ValidPortAddress.Add(i)
        Next
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Programmable Peripheral Interface 8255"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "8255"
        End Get
    End Property

    Public Overrides Function [In](port As UInteger) As UInteger
        Select Case (port And 3)
            Case 0 ' A
                Return ReadFromPort(0)
            Case 1 ' B
                Return ReadFromPort(1)
            Case 2 ' C
                Return ReadFromPort(2)
            Case 3
                Return mode
            Case Else
                Return &HFF
        End Select
    End Function

    Public Overrides Sub Out(port As UInteger, v As UInteger)
        Select Case port And 3
            Case 0 ' A
                WriteToPort(0, v)
            Case 1 ' B
                WriteToPort(1, v)
            Case 2 ' C
                WriteToPort(2, v)
            Case 3
                If (v And &H80) <> 0 Then
                    mode = v

                    modeGroupA = (v >> 5) And &H3
                    modeGroupB = (v >> 2) And &H1
                    ports(0).Inp = If((v And &H10) <> 0, &HFF, 0)
                    ports(1).Inp = If((v And &H2) <> 0, &HFF, 0)
                    ports(2).Inp = If((v And &H1) <> 0, &HF, 0)
                    ports(2).Inp = ports(2).Inp Or If((v And &H8) <> 0, &HF0, 0)
                Else
                    Dim bit As Byte = (v >> 1) And &H7
                    If (v And 1) <> 0 Then
                        v = ports(2).Output Or (1 << bit)
                    Else
                        v = ports(2).Output And (Not (1 << bit))
                    End If

                    WriteToPort(2, v)
                End If
        End Select
    End Sub

    Private Sub WriteToPort(port As Integer, value As Byte)
        ports(port).Output = value

        If ports(port).Inp <> &HFF AndAlso ports(port).Write IsNot Nothing Then
            value = value And (Not ports(port).Inp)
            ports(port).Write(value)
        End If
    End Sub

    Private Function ReadFromPort(port As Integer) As Byte
        Dim v As Integer

        If ports(port).Inp <> 0 AndAlso ports(port).Read IsNot Nothing Then
            ports(port).Input = ports(port).Read()
        End If

        v = ports(port).Input And ports(port).Inp
        v = v Or (ports(port).Output And (Not (ports(port).Inp)))

        Return v
    End Function

    Public Overrides Sub Run()
        keyShiftPending = False
        keyBuf = keyBuf.Substring(1)
        If keyBuf.Length() > 0 AndAlso irq IsNot Nothing Then irq.Raise(True)
    End Sub

    ' Store a scancode byte in the buffer
    Public Sub PutKeyData(v As Integer, isKeyUp As Boolean)
        If keyBuf.Length = 16 Then TrimBuffer()

        SyncLock keyBuf
            keyBuf = keyBuf + Chr(v)
            keyUpStates(keyBuf.Length - 1) = isKeyUp

            If keyBuf.Length = 1 AndAlso irq IsNot Nothing Then irq.Raise(True)
        End SyncLock
    End Sub

    Private Sub TrimBuffer()
        SyncLock keyBuf
            keyBuf = keyBuf.Substring(1)
            Array.Copy(keyUpStates, 1, keyUpStates, 0, keyUpStates.Length - 1)
        End SyncLock
    End Sub

    ' Get a scancode byte from the buffer
    Public Function GetKeyData() As Integer
        ' release interrupt
        If irq IsNot Nothing Then irq.Raise(False)
        ' if the buffer is empty, we just return the most recent byte 

        SyncLock keyBuf
            If keyBuf.Length() > 0 Then
                ' read byte from buffer
                lastKeyCode = keyMap.GetScanCode(Asc(keyBuf(0))) And &HFF
                If keyUpStates(0) Then lastKeyCode = lastKeyCode Or &H80

                ' wait ~.05 msec before going to the next byte
                If Not keyShiftPending Then
                    keyShiftPending = True
                    sched.RunTaskAfter(task, 500000 / 1000)
                End If
            End If
        End SyncLock

        ' return scancode byte
        Return lastKeyCode
    End Function
End Class

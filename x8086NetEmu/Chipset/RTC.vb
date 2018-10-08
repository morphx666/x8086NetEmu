Public Class RTC
    Inherits IOPortHandler

    Private irq As InterruptRequest

    Private Delegate Function ReadFunction() As Integer
    Private Delegate Sub WriteFunction(v As Integer)

    Private index As Integer

    Private cmosA As Integer = &H26
    Private cmosB As Integer = &H2
    Private cmosC As Integer = 0
    Private cmosData(128 - 1) As Integer

    Private periodicInt As Long
    Private nextInt As Long
    Private lastUpdate As Long
    Private ticks As Long

    Private Const baseFrequency As Integer = 32.768 * X8086.KHz

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
        Me.irq = irq

        For i As Integer = &H70 To &H71
            ValidPortAddress.Add(i)
        Next

        For i As Integer = &H2C0 To &H2CF
            ValidPortAddress.Add(i)
        Next

        'cpu.TryAttachHook(&H8, New X8086.IntHandler(Function()
        '                                                Dim t As Long = Now.Ticks
        '                                                ticks += 2
        '                                                lastUpdate = t

        '                                                If nextInt < t Then
        '                                                    cmosC = cmosC Or ((1 << 6) Or (1 << 7))
        '                                                    nextInt += periodicInt * Math.Ceiling((t - nextInt) / periodicInt)
        '                                                End If

        '                                                Return False
        '                                            End Function))

        'cpu.TryAttachHook(&H1A, New X8086.IntHandler(Function()
        '                                                 Select Case cpu.Registers.AH
        '                                                     Case 0
        '                                                         cpu.Registers.AL = 0
        '                                                         cpu.Registers.CX = (ticks >> 16) And &HFFFF
        '                                                         cpu.Registers.DX = ticks And &HFFFF
        '                                                         Return True
        '                                                     Case 2
        '                                                         cpu.Registers.CH = [In](4)
        '                                                         cpu.Registers.CL = [In](2)
        '                                                         cpu.Registers.DH = [In](0)
        '                                                         cpu.Registers.DL = 0
        '                                                         cpu.Flags.CF = 0
        '                                                         Return True
        '                                                 End Select

        '                                                 Return False
        '                                             End Function))
    End Sub

    Private Function EncodeTime(t As UInt16) As UInt16
        If (cmosB And &H4) <> 0 Then
            Return t
        Else
            Return ToBCD(t)
        End If
    End Function

    Private Function ToBCD(v As UInt16) As UInt16
        'If v >= 100 Then v = v Mod 100
        'Return (v Mod 10) + 16 * (v / 10)

        Dim i As Integer = 0
        Dim r As Integer = 0
        Dim d As Integer = 0

        While v <> 0
            d = v Mod 10
            r = r Or (d << (4 * i))
            i += 1
            v = (v - d) \ 10
        End While
        Return r
    End Function

    Private Function FromBCD(v As UInt16) As UInt16
        If (v And &HF) > &H9 Then v += &H6
        If (v And &HF0) > &H90 Then v += &H60
        Return (v And &HF) + 10 * ((v >> 4) And &HF0)
    End Function

    Public Overrides Function [In](port As UInt32) As UInt16
        Select Case index
            Case &H0 : Return EncodeTime(Now.ToUniversalTime().Second)
            Case &H2 : Return EncodeTime(Now.ToUniversalTime().Minute)
            Case &H4 : Return EncodeTime(Now.ToUniversalTime().Hour)
            Case &H7 : Return EncodeTime(Now.ToUniversalTime().Day)
            Case &H8 : Return EncodeTime(Now.ToUniversalTime().Month + 1)
            Case &H9 : Return EncodeTime(Now.ToUniversalTime().Year Mod 100)

            Case &HA : Return cmosA
            Case &HB : Return cmosB
            Case &HC : Return cmosC And (Not &HF0)
            Case &HD : Return &HFF

            Case &H32 : Return EncodeTime(Now.ToUniversalTime().Year \ 100)
        End Select

        Return cmosData(index)
    End Function

    Public Overrides Sub Out(port As UInt32, value As UInt16)
        If (port = &H70) OrElse (port = &H2C0) Then
            index = value And &H7F
        Else
            Select Case index
                Case &HA
                    cmosA = value And &H7F
                    periodicInt = 1000 / (32768 >> (cmosA And &HF) - 1)
                Case &HB : cmosB = value
                Case Else : cmosData(index) = value
            End Select
        End If
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "RTC"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Real Time Clock"
        End Get
    End Property

    Public Overrides Sub Run()
        Stop
    End Sub
End Class

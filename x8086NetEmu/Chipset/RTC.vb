Public Class RTC
    Inherits IOPortHandler

    Private irq As InterruptRequest

    Private Delegate Sub ReadFunction()
    Private Delegate Sub WriteFunction(v As Integer)

    Private Const YEAR = 7
    Private Const MONTH = 6
    Private Const DAY = 5
    Private Const DOW = 4
    Private Const HOUR = 3
    Private Const MINUTE = 2
    Private Const SECOND = 1
    Private Const CENTURY = 0

    ' CENTURY
    Private Const CENTURY_W = &H80
    Private Const CENTURY_R = &H40

    ' DOW
    Private Const DOW_BF = &H80
    Private Const DOW_FT = &H40

    ' SECONDS
    Private Const SECOND_NOSC = &H80

    Private Const baseFrequency As Integer = 32.768 * x8086.KHz

    Private Class TaskSC
        Inherits Scheduler.Task

        Public Sub New(owner As IOPortHandler)
            MyBase.New(owner)
        End Sub

        Public Overrides Sub Run()
            Owner.Run()
        End Sub

        Public Overrides ReadOnly Property Name As Object
            Get
                Return Owner.Name
            End Get
        End Property
    End Class
    Private task As Scheduler.Task = New TaskSC(Me)

    Private Structure DS1743
        Public Read As ReadFunction
        Public Write As WriteFunction

        Public Count As UInt16
        Public RAM() As Byte

        Public Year As UInt16
        Public Month As UInt16
        Public Day As UInt16
        Public Dow As UInt16
        Public Hour As UInt16
        Public Minute As UInt16
        Public Second As UInt16
    End Structure

    Private data As DS1743

    Public Sub New(cpu As x8086, irq As InterruptRequest)
        Me.irq = irq

        For i As Integer = &H70 To &H71
            ValidPortAddress.Add(i)
        Next

        ReDim data.RAM(8192 - 1)
        data.Count = 8
        data.Year = 2000

        data.Read = New ReadFunction(Sub()
                                         data.Year = Now.Year
                                         data.Month = Now.Month
                                         data.Day = Now.Day

                                         data.Hour = Now.Hour
                                         data.Minute = Now.Minute
                                         data.Second = Now.Second

                                         data.Dow = Now.DayOfWeek
                                     End Sub)

        data.Write = New WriteFunction(Sub()

                                       End Sub)

        Clock2RAM()
    End Sub

    Private Sub Clock2RAM()
        If data.RAM(CENTURY) And CENTURY_R Then Exit Sub

        Dim v As UInt16

        v = ToBCD(data.Year / 100)
        data.RAM(CENTURY) = data.RAM(CENTURY) And &HC0
        data.RAM(CENTURY) = data.RAM(CENTURY) Or (v And &H3F)

        v = ToBCD(data.Year Mod 100)
        data.RAM(YEAR) = v

        v = ToBCD(data.Month + 1)
        data.RAM(MONTH) = data.RAM(MONTH) And &HE0
        data.RAM(MONTH) = data.RAM(MONTH) Or (v And &H1F)

        v = ToBCD(data.Dow + 1)
        data.RAM(DOW) = data.RAM(DOW) And &HC0
        data.RAM(DOW) = data.RAM(DOW) Or (v And &H3F)

        v = ToBCD(data.Hour)
        data.RAM(HOUR) = data.RAM(HOUR) And &H80
        data.RAM(HOUR) = data.RAM(HOUR) Or (v And &H3F)

        v = ToBCD(data.Minute)
        data.RAM(MINUTE) = data.RAM(MINUTE) And &H80
        data.RAM(MINUTE) = data.RAM(MINUTE) Or (v And &H7F)

        v = ToBCD(data.Second)
        data.RAM(SECOND) = data.RAM(SECOND) And &H80
        data.RAM(SECOND) = data.RAM(SECOND) Or (v And &H7F)
    End Sub

    Private Sub RAM2Clock()
        If data.RAM(CENTURY) And CENTURY_W Then Exit Sub

        Dim v As UInt16

        v = FromBCD(data.RAM(CENTURY) And &H3F)
        data.Year = 10 * v

        v = FromBCD(data.RAM(YEAR))
        data.Year += v

        v = FromBCD(data.RAM(MONTH) And &H1F)
        data.Month = v - 1

        v = FromBCD(data.RAM(DAY) And &H3F)
        data.Day = v - 1

        v = FromBCD(data.RAM(DOW) And &H7)
        data.Dow = v - 1

        v = FromBCD(data.RAM(HOUR) And &H3F)
        data.Hour = v

        v = FromBCD(data.RAM(MINUTE) And &H7F)
        data.Minute = v

        v = FromBCD(data.RAM(SECOND) And &H7F)
        data.Second = v
    End Sub

    Private Function ToBCD(v As UInt16) As UInt16
        If v >= 100 Then v = v Mod 100
        Return ((v Mod 10) + 16 * (v / 10))
    End Function

    Private Function FromBCD(v As UInt16) As UInt16
        If (v And &HF) > &H9 Then v += &H6
        If (v And &HF0) > &H90 Then v += &H60
        Return ((v And &HF) + 10 * ((v >> 4) And &HF0))
    End Function

    Public Overrides Function [In](port As Integer) As Integer
        Clock2RAM()

        data.Read()

        Stop

        Return 0 ' Just to suppress the warning
    End Function

    Public Overrides Sub Out(port As Integer, value As Integer)
        Stop
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

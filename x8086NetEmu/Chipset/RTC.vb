Public Class RTC
    Inherits IOPortHandler

    Private registers(&H34 - 1) As Integer
    Private registerIndex As Integer

    Public Sub New(cpu As x8086)
        For i As Integer = &H70 To &H71
            ValidPortAddress.Add(i)
        Next
    End Sub

    Public Overrides Function [In](port As UInteger) As UInteger
        Return registers(registerIndex)
    End Function

    Public Overrides Sub Out(port As UInteger, value As UInteger)
        registerIndex = value
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
    End Sub
End Class

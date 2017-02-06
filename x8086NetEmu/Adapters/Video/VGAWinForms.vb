Public Class VGAWinForms
    Inherits CGAWinForms

    Dim vga As VGAAdapter

    Public Sub New(cpu As x8086, renderControl As Control, Optional tryUseCGAFont As Boolean = True)
        MyBase.New(cpu, renderControl, tryUseCGAFont)
        vga = New VGAAdapter(cpu)

        MyBase.videoGraphicsSegment = &HA000
    End Sub

    Public Overrides Function [In](port As Integer) As Integer
        If port >= &H3C0 AndAlso port <= &H3DF Then
            Return vga.In(port)
        Else
            Return MyBase.In(port)
        End If
    End Function

    Public Overrides Sub Out(port As Integer, value As Integer)
        If port >= &H3C0 AndAlso port <= &H3DF Then
            vga.Out(port, value)
        Else
            MyBase.Out(port, value)
        End If
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "VGA WinForms Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "VGA WinForms"
        End Get
    End Property
End Class

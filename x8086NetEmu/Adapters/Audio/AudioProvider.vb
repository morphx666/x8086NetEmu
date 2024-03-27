Public MustInherit Class AudioProvider
    Inherits Adapter

    Public MustOverride Function GenerateSample() As Int16

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
    End Sub
End Class

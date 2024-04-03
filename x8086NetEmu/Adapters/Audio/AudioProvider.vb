Public MustInherit Class AudioProvider
    Inherits Adapter

    Public Volume As Double

    Public MustOverride ReadOnly Property Sample As Int16

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
    End Sub
End Class

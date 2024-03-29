Public MustInherit Class AudioProvider
    Inherits Adapter

    Public LastTick As Long
    Public SampleTicks As Long
    Public MustOverride Property Volume As Double

    Public MustOverride Function GetSample() As Int16
    Public MustOverride Sub Tick()

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
    End Sub
End Class

Public MustInherit Class InterruptController
    Implements IInterruptController
    Public MustOverride Function GetPendingInterrupt() As Byte Implements IInterruptController.GetPendingInterrupt
End Class

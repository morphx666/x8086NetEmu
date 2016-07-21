Public MustInherit Class InterruptController
    Implements IInterruptController
    Public MustOverride Function GetPendingInterrupt() As Integer Implements IInterruptController.GetPendingInterrupt
End Class

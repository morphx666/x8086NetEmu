Public MustInherit Class InterruptRequest
    Implements IInterruptRequest
    Public MustOverride Sub Raise(enable As Boolean) Implements IInterruptRequest.RaiseIrq
End Class

' http://jorisvr.nl/retro/
Public MustInherit Class IOPortHandler
    Implements IInterruptController, IIOPortHandler

    Private mEmulator As x8086
    Private mValidPortAddresses As List(Of Integer)

    Public Sub New()
        mValidPortAddresses = New List(Of Integer)
    End Sub

    Public ReadOnly Property ValidPortAddress As List(Of Integer) Implements IIOPortHandler.ValidPortAddress
        Get
            Return mValidPortAddresses
        End Get
    End Property

    Public MustOverride Sub Out(port As UInteger, value As UInteger) Implements IIOPortHandler.Out
    Public MustOverride Function [In](port As UInteger) As UInteger Implements IIOPortHandler.In
    Public MustOverride ReadOnly Property Description As String Implements IIOPortHandler.Description
    Public MustOverride ReadOnly Property Name As String Implements IIOPortHandler.Name
    Public MustOverride Sub Run() Implements IIOPortHandler.Run

    Public Overridable Function GetPendingInterrupt() As Integer Implements IInterruptController.GetPendingInterrupt
        Return -1
    End Function
End Class

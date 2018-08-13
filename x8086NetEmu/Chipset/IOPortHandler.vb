' http://jorisvr.nl/retro/
Public MustInherit Class IOPortHandler
    Implements IInterruptController, IIOPortHandler

    Private mEmulator As X8086
    Private mValidPortAddresses As List(Of UInt32)

    Public Sub New()
        mValidPortAddresses = New List(Of UInt32)
    End Sub

    Public ReadOnly Property ValidPortAddress As List(Of UInt32) Implements IIOPortHandler.ValidPortAddress
        Get
            Return mValidPortAddresses
        End Get
    End Property

    Public MustOverride Sub Out(port As UInt32, value As UInt32) Implements IIOPortHandler.Out
    Public MustOverride Function [In](port As UInt32) As UInt32 Implements IIOPortHandler.In
    Public MustOverride ReadOnly Property Description As String Implements IIOPortHandler.Description
    Public MustOverride ReadOnly Property Name As String Implements IIOPortHandler.Name
    Public MustOverride Sub Run() Implements IIOPortHandler.Run

    Public Overridable Function GetPendingInterrupt() As Integer Implements IInterruptController.GetPendingInterrupt
        Return -1
    End Function

    Public Overridable Function Read(address As UInt32) As UInt32
        Return 0
    End Function

    Public Overridable Sub Write(address As UInt32, value As UInt32)
    End Sub
End Class

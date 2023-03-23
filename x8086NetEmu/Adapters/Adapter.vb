Public MustInherit Class Adapter
    Inherits IOPortHandler

    Private mCPU As X8086

    Public Enum AdapterType
        Video
        Keyboard
        Floppy
        IC
        AudioDevice
        Other
        SerialMouseCOM1
        Memory
    End Enum

    Public Sub New()
    End Sub

    Public Sub New(cpu As X8086)
        mCPU = cpu
        Threading.Tasks.Task.Run(AddressOf InitiAdapter)
    End Sub

    Public ReadOnly Property CPU As X8086
        Get
            Return mCPU
        End Get
    End Property

    Public MustOverride Sub InitiAdapter()
    Public MustOverride Sub CloseAdapter()
    Public MustOverride ReadOnly Property Type As AdapterType
    Public MustOverride ReadOnly Property Vendor As String
    Public MustOverride ReadOnly Property VersionMajor As Integer
    Public MustOverride ReadOnly Property VersionMinor As Integer
    Public MustOverride ReadOnly Property VersionRevision As Integer

    'Public MustOverride Function [In](port As Integer) As integer Implements IIOPortHandler.In
    'Public MustOverride Sub Out(port As Integer, value As integer) Implements IIOPortHandler.Out
    'Public MustOverride ReadOnly Property Description As String Implements IIOPortHandler.Description
    'Public MustOverride ReadOnly Property Name As String Implements IIOPortHandler.Name

    Public Overloads ReadOnly Property ValidPortAddress As List(Of UInt32)
        Get
            Return MyBase.ValidPortAddress
        End Get
    End Property

    Public MustOverride Overrides ReadOnly Property Description As String
    Public MustOverride Overrides Function [In](port As UInt16) As Byte
    Public MustOverride Overrides Sub Out(port As UInt16, value As Byte)
    Public MustOverride Overrides ReadOnly Property Name As String
End Class

Public MustInherit Class Adapter
    Inherits IOPortHandler

    Private mCPU As x8086

    Public Enum AdapterType
        Video
        Keyboard
        Floppy
        IC
        Speaker
        Other
        SerialMouseCOM1
        Memory
    End Enum

    Public Sub New()
    End Sub

    Public Property CPU As x8086
        Get
            Return mCPU
        End Get
        Set(value As x8086)
            mCPU = value
            InitiAdapter()
        End Set
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

    Public Overloads ReadOnly Property ValidPortAddress As System.Collections.Generic.List(Of Integer)
        Get
            Return MyBase.ValidPortAddress
        End Get
    End Property

    Public MustOverride Overrides ReadOnly Property Description As String
    Public MustOverride Overrides Function [In](port As Integer) As Integer
    Public MustOverride Overrides ReadOnly Property Name As String
    Public MustOverride Overrides Sub Out(port As Integer, value As Integer)
End Class

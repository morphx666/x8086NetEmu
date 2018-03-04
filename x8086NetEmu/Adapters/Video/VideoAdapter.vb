Public MustInherit Class VideoAdapter
    Inherits Adapter

    Public Enum FontSources
        TrueType
        BitmapFile
        ROM
    End Enum

    Public Enum MainModes
        Unknown = -1
        Text = 0
        Graphics = 2
    End Enum

    Public Event KeyDown(sender As Object, e As KeyEventArgs)
    Public Event KeyUp(sender As Object, e As KeyEventArgs)

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.Video
        End Get
    End Property

    Public MustOverride Overrides ReadOnly Property Description As String
    Public MustOverride Overrides ReadOnly Property Name As String
    Public MustOverride Overrides ReadOnly Property Vendor As String
    Public MustOverride Overrides ReadOnly Property VersionMajor As Integer
    Public MustOverride Overrides ReadOnly Property VersionMinor As Integer
    Public MustOverride Overrides ReadOnly Property VersionRevision As Integer

    Public MustOverride Property VideoMode As UInteger
    Public MustOverride Property Zoom As Double

    Public MustOverride Overrides Sub CloseAdapter()
    Public MustOverride Overrides Sub InitiAdapter()
    Public MustOverride Overrides Sub Out(port As UInteger, value As UInteger)
    Public MustOverride Overrides Function [In](port As UInteger) As UInteger
    Public MustOverride Overrides Sub Run()

    Public MustOverride Sub Reset()
    Public MustOverride Sub AutoSize()

    Protected mStartTextVideoAddress As Integer
    Protected mEndTextVideoAddress As Integer

    Protected mStartGraphicsVideoAddress As Integer
    Protected mEndGraphicsVideoAddress As Integer
    Protected mMainMode As MainModes

    Protected mTextResolution As Size = New Size(40, 25)
    Protected mVideoResolution As Size = New Size(0, 0)

    Private Memory(X8086.MemSize - 1) As Boolean

    Protected Overridable Sub OnKeyDown(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyDown(sender, e)
    End Sub

    Protected Overridable Sub OnKeyUp(sender As Object, e As KeyEventArgs)
        RaiseEvent KeyUp(sender, e)
    End Sub

    Public ReadOnly Property StartGraphicsVideoAddress As Integer
        Get
            Return mStartGraphicsVideoAddress
        End Get
    End Property

    Public ReadOnly Property EndGraphicsVideoAddress As Integer
        Get
            Return mEndGraphicsVideoAddress
        End Get
    End Property

    Public ReadOnly Property StartTextVideoAddress As Integer
        Get
            Return mStartTextVideoAddress
        End Get
    End Property

    Public ReadOnly Property EndTextVideoAddress As Integer
        Get
            Return mEndTextVideoAddress
        End Get
    End Property

    Public ReadOnly Property TextResolution As Size
        Get
            Return mTextResolution
        End Get
    End Property

    Public ReadOnly Property GraphicsResolution As Size
        Get
            Return mVideoResolution
        End Get
    End Property

    Public Property IsDirty(address As UInteger) As Boolean
        Get
            Dim r As Boolean = Memory(address)
            Memory(address) = False
            Return r
        End Get
        Set(value As Boolean)
            Memory(address) = value
        End Set
    End Property

    Public ReadOnly Property MainMode As MainModes
        Get
            Return mMainMode
        End Get
    End Property
End Class

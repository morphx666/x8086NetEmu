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

    Public Event KeyDown(sender As Object, e As XKeyEventArgs)
    Public Event KeyUp(sender As Object, e As XKeyEventArgs)
    Public Event PreRender(sender As Object, e As XPaintEventArgs)
    Public Event PostRender(sender As Object, e As XPaintEventArgs)

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

    Public MustOverride Property VideoMode As UInt32
    Public MustOverride Property Zoom As Double

    Public MustOverride Overrides Sub CloseAdapter()
    Public MustOverride Overrides Sub InitAdapter()
    Public MustOverride Overrides Sub Out(port As UInt16, value As Byte)
    Public MustOverride Overrides Function [In](port As UInt16) As Byte
    Public MustOverride Overrides Sub Run()
    Public MustOverride Sub UpdateClock()

    Public MustOverride Sub Reset()
    Protected MustOverride Sub AutoSize()
    Protected MustOverride Sub ResizeRenderControl()

    Protected mStartTextVideoAddress As Integer = &HB0000
    Protected mEndTextVideoAddress As Integer = &HA0000

    Protected mStartGraphicsVideoAddress As Integer
    Protected mEndGraphicsVideoAddress As Integer
    Protected mMainMode As MainModes

    Protected mTextResolution As XSize = New XSize(40, 25)
    Protected mGraphicsResolution As XSize = New XSize(0, 0)
    Protected mCellSize As XSize

    Protected keyMap As New KeyMap() ' Used to filter unsupported keystrokes

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
    End Sub

    Protected Overridable Sub OnKeyDown(sender As Object, e As XKeyEventArgs)
        RaiseEvent KeyDown(sender, e)
    End Sub

    Protected Overridable Sub OnKeyUp(sender As Object, e As XKeyEventArgs)
        RaiseEvent KeyUp(sender, e)
    End Sub

    Protected Overridable Sub OnPreRender(sender As Object, e As XPaintEventArgs)
        RaiseEvent PreRender(sender, e)
    End Sub

    Protected Overridable Sub OnPostRender(sender As Object, e As XPaintEventArgs)
        RaiseEvent PostRender(sender, e)
    End Sub

    Public ReadOnly Property StartGraphicsVideoAddress As UInt32
        Get
            Return mStartGraphicsVideoAddress
        End Get
    End Property

    Public ReadOnly Property EndGraphicsVideoAddress As UInt32
        Get
            Return mEndGraphicsVideoAddress
        End Get
    End Property

    Public ReadOnly Property StartTextVideoAddress As UInt32
        Get
            Return mStartTextVideoAddress
        End Get
    End Property

    Public ReadOnly Property EndTextVideoAddress As UInt32
        Get
            Return mEndTextVideoAddress
        End Get
    End Property

    Public ReadOnly Property TextResolution As XSize
        Get
            Return mTextResolution
        End Get
    End Property

    Public ReadOnly Property GraphicsResolution As XSize
        Get
            Return mGraphicsResolution
        End Get
    End Property

    Public Property CellSize As XSize
        Get
            Return mCellSize
        End Get
        Protected Set(value As XSize)
            mCellSize = value
        End Set
    End Property

    Public ReadOnly Property MainMode As MainModes
        Get
            Return mMainMode
        End Get
    End Property

    Public Function ColRowToRectangle(col As Integer, row As Integer) As XRectangle
        Return New XRectangle(col * mCellSize.Width,
                             row * mCellSize.Height,
                             mCellSize)
    End Function

    Public Function ColRowToAddress(col As Integer, row As Integer) As Integer
        Return StartTextVideoAddress + row * TextResolution.Width * 2 + (col * 2)
    End Function
End Class

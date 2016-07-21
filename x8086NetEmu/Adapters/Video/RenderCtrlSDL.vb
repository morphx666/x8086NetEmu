Imports SdlDotNet.Core
Imports SdlDotNet.Graphics
Imports SdlDotNet.Graphics.Primitives
Imports System.Threading

Public Class RenderCtrlSDL
    Private mSDLSurface As SdlDotNet.Windows.SurfaceControl
    Private mScreen As Surface
    Private mFont As SdlDotNet.Graphics.Font
    Private mFontSize As Size
    Private mCGAAdapter As CGAAdapter

    Private sdlPollThread As Thread

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        mSDLSurface = New SdlDotNet.Windows.SurfaceControl()
        AddHandler Me.SizeChanged, Sub() SetResolution(Me.Width, Me.Height)

        sdlPollThread = New Thread(AddressOf PollLoop)
        sdlPollThread.Start()
    End Sub

    Public ReadOnly Property SDLFont As SdlDotNet.Graphics.Font
        Get
            Return mFont
        End Get
    End Property

    Public ReadOnly Property SDLFontSize As Size
        Get
            Return mFontSize
        End Get
    End Property

    Public ReadOnly Property SDLScreen As Surface
        Get
            Return mScreen
        End Get
    End Property

    Private Sub PollLoop()
        Do
            If SdlDotNet.Core.Events.Poll() Then
                Dim evts() As SdlDotNet.Core.SdlEventArgs = SdlDotNet.Core.Events.Retrieve()

                For i As Integer = 0 To evts.Length - 1
                    Select Case evts(i).Type
                        Case EventTypes.KeyDown
                            Dim e = CType(evts(i), SdlDotNet.Input.KeyboardEventArgs)
                            mCGAAdapter.OnKeyUp(Me, New KeyEventArgs(e.Key))
                        Case EventTypes.KeyUp
                            Dim e = CType(evts(i), SdlDotNet.Input.KeyboardEventArgs)
                            mCGAAdapter.OnKeyDown(Me, New KeyEventArgs(e.Key))
                    End Select
                Next
            End If

            Thread.Sleep(1)
        Loop
    End Sub

    Public Sub Init(cgaAdapter As CGAAdapter, fontName As String, size As Integer)
        mCGAAdapter = cgaAdapter
        mFont = New SdlDotNet.Graphics.Font("c:\windows\fonts\" + fontName + ".ttf", size)
        mFontSize = mFont.SizeText("X")
        SetResolution(Me.Width, Me.Height)
    End Sub

    Private Sub SetResolution(w As Integer, h As Integer)
        If mScreen IsNot Nothing Then mScreen.Close()
        mScreen = Video.SetVideoMode(w, h, False)
    End Sub
End Class

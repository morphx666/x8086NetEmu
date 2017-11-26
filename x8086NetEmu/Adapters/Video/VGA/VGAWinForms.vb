Public Class VGAWinForms
    Inherits VGAAdapter

    Private Class CGAChar
        Private mCGAChar As Integer
        Private mForeColor As Color
        Private mBackColor As Color
        Private mBitmap As DirectBitmap

        Public Sub New(c As Integer, fb As Color, bb As Color)
            mCGAChar = c
            mForeColor = fb
            mBackColor = bb
        End Sub

        Public ReadOnly Property CGAChar As Integer
            Get
                Return mCGAChar
            End Get
        End Property

        Public ReadOnly Property ForeColor As Color
            Get
                Return mForeColor
            End Get
        End Property

        Public ReadOnly Property BackColor As Color
            Get
                Return mBackColor
            End Get
        End Property

        Public Sub Paint(dbmp As DirectBitmap, p As Point, scale As SizeF)
            Dim w4s As Integer = mBitmap.Width * 4
            Dim w4d As Integer = dbmp.Width * 4
            p.X *= 4
            For y As Integer = 0 To mBitmap.Height - 1
                Array.Copy(mBitmap.Bits, y * w4s, dbmp.Bits, (y + p.Y) * w4d + p.X, w4s)
            Next
        End Sub

        Public Sub Render()
            If mBitmap Is Nothing Then
                mBitmap = New DirectBitmap(8, 16)

                For y As Integer = 0 To 16 - 1
                    For x As Integer = 0 To 8 - 1
                        If fontCGA(mCGAChar * 128 + y * 8 + x) = 1 Then
                            mBitmap.Pixel(x, y) = mForeColor
                        Else
                            mBitmap.Pixel(x, y) = mBackColor
                        End If
                    Next
                Next
            End If
        End Sub

        Public Shared Operator =(c1 As CGAChar, c2 As CGAChar) As Boolean
            Return c1.CGAChar = c2.CGAChar AndAlso
                    c1.ForeColor = c2.ForeColor AndAlso
                    c1.BackColor = c2.BackColor
        End Operator

        Public Shared Operator <>(c1 As CGAChar, c2 As CGAChar) As Boolean
            Return Not (c1 = c2)
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me = CType(obj, CGAChar)
        End Function

        Public Overrides Function ToString() As String
            Return String.Format("{0:000} [{1:000}:{2:000}:{3:000}] [{4:000}:{5:000}:{6:000}]",
                                 mCGAChar,
                                 mForeColor.R,
                                 mForeColor.G,
                                 mForeColor.B,
                                 mBackColor.R,
                                 mBackColor.G,
                                 mBackColor.B)
        End Function
    End Class
    Private cgaCharsCache As New List(Of CGAChar)
    Private videoBMP As DirectBitmap

    Private charSize As Size
    Private cursorSize As Size
    Private blinkCounter As Integer

    Private preferredFont As String = "Perfect DOS VGA 437"
    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
    Private textFormat As StringFormat = New StringFormat(StringFormat.GenericTypographic)

    Private charSizeCache As New Dictionary(Of Integer, Size)

    Private brushCache(16 - 1) As Color
    Private cursorBrush As Color = Color.FromArgb(128, Color.White)
    Private cursorYOffset As Integer

    Private Shared fontCGA() As Byte
    Private useCGAFont As Boolean

    Private scale As New SizeF(1, 1)

    Private mCPU As X8086
    Private mRenderControl As Control
    Private mHideHostCursor As Boolean = True

    Public Event PreRender(sender As Object, e As PaintEventArgs)
    Public Event PostRender(sender As Object, e As PaintEventArgs)

    Private Class TaskSC
        Inherits Scheduler.Task

        Public Sub New(owner As IOPortHandler)
            MyBase.New(owner)
        End Sub

        Public Overrides Sub Run()
            Owner.Run()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return Owner.Name
            End Get
        End Property
    End Class
    Private task As Scheduler.Task = New TaskSC(Me)

    Public Sub New(cpu As X8086, renderControl As Control, Optional tryUseCGAFont As Boolean = True)
        MyBase.New(cpu)
        useCGAFont = tryUseCGAFont
        mCPU = cpu
        Me.RenderControl = renderControl

        AddHandler mRenderControl.KeyDown, Sub(sender As Object, e As KeyEventArgs) HandleKeyDown(Me, e)
        AddHandler mRenderControl.KeyUp, Sub(sender As Object, e As KeyEventArgs) HandleKeyUp(Me, e)

        AddHandler mRenderControl.MouseDown, Sub(sender As Object, e As MouseEventArgs) OnMouseDown(Me, e)
        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs) OnMouseMove(Me, e)
        AddHandler mRenderControl.MouseUp, Sub(sender As Object, e As MouseEventArgs) OnMouseUp(Me, e)

        Dim fontCGAPath As String = X8086.FixPath("roms\asciivga.dat")
        Dim fontCGAError As String = ""

        If useCGAFont Then
            If IO.File.Exists(fontCGAPath) Then
                Try
                    fontCGA = IO.File.ReadAllBytes(fontCGAPath)
                Catch ex As Exception
                    fontCGAError = ex.Message
                    useCGAFont = False
                End Try
            Else
                fontCGAError = "File not found"
                useCGAFont = False
            End If
        End If

        If Not useCGAFont Then
            If mFont.Name <> preferredFont Then
                MsgBox(If(useCGAFont, "ASCII VGA Font Data not found at '" + fontCGAPath + "'" + If(fontCGAError <> "", ": " + fontCGAError, "") +
                       vbCrLf + vbCrLf, "") +
                       "CGAWinForms requires the '" + preferredFont + "' font. Please install it before using this adapter", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
                mFont = New Font("Consolas", 16, FontStyle.Regular, GraphicsUnit.Pixel)
                If mFont.Name <> "Consolas" Then
                    mFont = New Font("Andale Mono", 16, FontStyle.Regular, GraphicsUnit.Pixel)
                    If mFont.Name <> "Andale Mono" Then
                        mFont = New Font("Courier New", 16, FontStyle.Regular, GraphicsUnit.Pixel)
                    End If
                End If
            End If
        End If

        textFormat.FormatFlags = StringFormatFlags.NoWrap Or
                                   StringFormatFlags.MeasureTrailingSpaces Or
                                   StringFormatFlags.FitBlackBox Or
                                   StringFormatFlags.NoClip
    End Sub

    Public Property RenderControl As Control
        Get
            Return mRenderControl
        End Get
        Set(value As Control)
            DetachRenderControl()
            mRenderControl = value

            'useSDL = TypeOf mRenderControl Is RenderCtrlSDL
            'If useSDL Then
            '    sdlCtrl = CType(mRenderControl, RenderCtrlSDL)
            '    sdlCtrl.Init(Me, mFont.FontFamily.Name, mFont.Size)
            'End If

            InitiAdapter()

            AddHandler mRenderControl.Paint, AddressOf Paint
        End Set
    End Property

    Protected Sub DetachRenderControl()
        If mRenderControl IsNot Nothing Then RemoveHandler mRenderControl.Paint, AddressOf Paint
    End Sub

    Public Overrides Sub CloseAdapter()
        MyBase.CloseAdapter()

        DisposeColorCaches()
        DetachRenderControl()
    End Sub

    Public Overrides Sub AutoSize()
        If mRenderControl IsNot Nothing Then
            If mRenderControl.InvokeRequired Then
                mRenderControl.Invoke(New MethodInvoker(AddressOf ResizeRenderControl))
            Else
                ResizeRenderControl()
            End If
        End If
    End Sub

    Private Sub ResizeRenderControl()
        Dim ctrlSize As Size

        If MainMode = MainModes.Text Then
            '    'Using g As Graphics = mRenderControl.CreateGraphics()
            ctrlSize = New Size(charSize.Width * TextResolution.Width, charSize.Height * TextResolution.Height)
            '    'End Using
        Else
            ctrlSize = New Size(GraphicsResolution.Width, GraphicsResolution.Height)
        End If

        Dim frmSize = New Size(640 * Zoom, 400 * Zoom)
        mRenderControl.FindForm.ClientSize = frmSize
        mRenderControl.Size = frmSize
        If charSize.Width = 0 OrElse charSize.Height = 0 Then Exit Sub

        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
    End Sub

    Protected Overrides Sub Render()
        mRenderControl.Invalidate()
    End Sub

    Private Sub Paint(sender As Object, e As PaintEventArgs)
        SyncLock MyBase.lockObject
            Dim g As Graphics = e.Graphics

            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed
            g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
            g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed

            g.ScaleTransform(scale.Width, scale.Height)

            RaiseEvent PreRender(sender, e)
            g.CompositingMode = Drawing2D.CompositingMode.SourceCopy

            Select Case MainMode
                Case MainModes.Text
                    'If useSDL Then
                    '    RenderTextSDL()
                    'Else
                    RenderText()
                    'End If
                Case MainModes.Graphics
                    RenderGraphics()
            End Select

            g.DrawImageUnscaled(videoBMP, 0, 0)

            g.CompositingMode = Drawing2D.CompositingMode.SourceOver
            RaiseEvent PostRender(sender, e)

            'RenderWaveform(g)
        End SyncLock
    End Sub

    Private Sub RenderGraphics()
    End Sub

    Private Sub RenderText()

    End Sub

    Public Function ColRowToRectangle(col As Integer, row As Integer) As Rectangle
        Return New Rectangle(New Point(col * charSize.Width, row * charSize.Height), charSize)
    End Function

    Public Function ColRowToAddress(col As Integer, row As Integer) As Integer
        Return StartTextVideoAddress + row * (TextResolution.Width * 2) + (col * 2)
    End Function

    Private Sub RenderChar(c As Integer, dbmp As DirectBitmap, fb As Color, bb As Color, p As Point)
        Dim ccc As New CGAChar(c, fb, bb)
        Dim idx As Integer = cgaCharsCache.IndexOf(ccc)
        If idx = -1 Then
            ccc.Render()
            cgaCharsCache.Add(ccc)
            idx = cgaCharsCache.Count - 1
        End If
        cgaCharsCache(idx).Paint(dbmp, p, scale)
    End Sub

    Private Sub RenderWaveform(g As Graphics)
#If Win32 Then
        If mCPU.PIT.Speaker IsNot Nothing Then
            g.ResetTransform()

            Dim h As Integer = mRenderControl.Height * 0.6
            Dim h2 As Integer = h / 2
            Dim p1 As Point = New Point(0, mCPU.PIT.Speaker.AudioBuffer(0) / Byte.MaxValue * h + h * 0.4)
            Dim p2 As Point
            Dim len As Integer = mCPU.PIT.Speaker.AudioBuffer.Length

            Using p As New Pen(Brushes.Red, 3)
                For i As Integer = 1 To len - 1
                    Try
                        p2 = New Point(i / len * mRenderControl.Width, mCPU.PIT.Speaker.AudioBuffer(i) / Byte.MaxValue * h + h * 0.4)
                        g.DrawLine(p, p1, p2)
                        p1 = p2
                    Catch
                        Exit For
                    End Try
                Next
            End Using
        End If
#End If
    End Sub

    Private Function MeasureChar(graphics As Graphics, code As Integer, text As Char, font As Font) As Size
        Dim size As Size

        If useCGAFont Then
            size = New Size(8, 16)
            charSizeCache.Add(code, size)
        Else
            If charSizeCache.ContainsKey(code) Then Return charSizeCache(code)

            Dim rect As System.Drawing.RectangleF = New System.Drawing.RectangleF(0, 0, 1000, 1000)
            Dim ranges() As System.Drawing.CharacterRange = {New System.Drawing.CharacterRange(0, 1)}
            Dim regions() As System.Drawing.Region = {New System.Drawing.Region()}

            textFormat.SetMeasurableCharacterRanges(ranges)

            regions = graphics.MeasureCharacterRanges(text, font, rect, textFormat)
            rect = regions(0).GetBounds(graphics)

            size = New Size(rect.Right - 1, rect.Bottom)
            charSizeCache.Add(code, size)
        End If

        Return size
    End Function

    Private Sub DisposeColorCaches()
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "VGA WinForms Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "VGA WinForms"
        End Get
    End Property

    Public Overrides ReadOnly Property Vendor As String
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Overrides Property Zoom As Double
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As Double)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Overrides Property VideoMode As VideoModes
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As VideoModes)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Overrides Sub Run()
        If mRenderControl IsNot Nothing Then mRenderControl.Invalidate()
    End Sub

    Public Overrides Sub InitiAdapter()
        Throw New NotImplementedException()
    End Sub

    Public Overrides Sub Reset()
        Throw New NotImplementedException()
    End Sub
End Class

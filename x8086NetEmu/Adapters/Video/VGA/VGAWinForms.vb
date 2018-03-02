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
            Try
                For y As Integer = 0 To mBitmap.Height - 1
                    Array.Copy(mBitmap.Bits, y * w4s, dbmp.Bits, (y + p.Y) * w4d + p.X, w4s)
                Next
            Catch
            End Try
        End Sub

        Public Sub Render(cellSize As Size)
            If mBitmap Is Nothing Then
                mBitmap = New DirectBitmap(cellSize.Width, cellSize.Height)

                For y As Integer = 0 To cellSize.Height - 1
                    For x As Integer = 0 To cellSize.Width - 1
                        If x > 7 OrElse y > 15 Then
                            mBitmap.Pixel(x, y) = mBackColor
                        Else
                            If fontCGA(mCGAChar * 128 + y * 8 + x) = 1 Then
                                mBitmap.Pixel(x, y) = mForeColor
                            Else
                                mBitmap.Pixel(x, y) = mBackColor
                            End If
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

    Private cursorSize As Size
    Private blinkCounter As Integer
    Private frameRate As Integer = 30
    Private cursorAddress As New List(Of Integer)

    Private preferredFont As String = "Perfect DOS VGA 437"
    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
    Private textFormat As StringFormat = New StringFormat(StringFormat.GenericTypographic)

    Private charSizeCache As New Dictionary(Of Integer, Size)

    Private brushCache(CGAPalette.Length - 1) As Color
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
            ctrlSize = New Size(CellSize.Width * TextResolution.Width, CellSize.Height * TextResolution.Height)
            '    'End Using
        Else
            ctrlSize = New Size(GraphicsResolution.Width, GraphicsResolution.Height)
        End If

        Dim frmSize = New Size(640 * Zoom, 400 * Zoom)
        mRenderControl.FindForm.ClientSize = frmSize
        mRenderControl.Size = frmSize
        If CellSize.Width = 0 OrElse CellSize.Height = 0 Then Exit Sub

        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
    End Sub

    Protected Overrides Sub Render()
        mRenderControl.Invalidate()
    End Sub

    Private Sub Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed

        g.ScaleTransform(scale.Width, scale.Height)

        RaiseEvent PreRender(sender, e)
        g.CompositingMode = Drawing2D.CompositingMode.SourceCopy

        Select Case MainMode
            Case MainModes.Text
                RenderText()
            Case MainModes.Graphics
                RenderGraphics()
        End Select

        g.DrawImageUnscaled(videoBMP, 0, 0)

        g.CompositingMode = Drawing2D.CompositingMode.SourceOver
        RaiseEvent PostRender(sender, e)

        'RenderWaveform(g)
    End Sub

    Private Sub RenderGraphics()
        Dim b As Byte
        Dim xDiv As Integer = If(PixelsPerByte = 4, 2, 3)
        Dim usePal As Integer = (portRAM(&H3D9 - &H3C0) >> 5) & 1
        Dim intensity As Integer = ((portRAM(&H3D9 - &H3C0) >> 4) & 1) << 3

        ' For mode &h12 and &h13
        Dim planeMode As Boolean = (VGA_SC(4) And 6) <> 0
        Dim vgaPage As UInteger = (VGA_CRTC(&HC) << 8) + VGA_CRTC(&HD)

        Dim a1 As UInteger
        Dim a2 As UInteger
        Dim a3 As UInteger

        For y As Integer = 0 To GraphicsResolution.Height - 1
            For x As Integer = 0 To GraphicsResolution.Width - 1
                Select Case mVideoMode
                    Case 4, 5
                        b = mCPU.Memory(mStartGraphicsVideoAddress + ((y >> 1) * 80) + ((y And 1) * &H2000) + (x >> 2))
                        Select Case x And 3
                            Case 3 : b = b And 3
                            Case 2 : b = (b >> 2) And 3
                            Case 1 : b = (b >> 4) And 3
                            Case 0 : b = (b >> 6) And 3
                        End Select
                        If mVideoMode = 4 Then
                            b = b * 2 + usePal + intensity
                            If b = (usePal + intensity) Then b = 0
                        Else
                            b = b * 63
                        End If
                        videoBMP.Pixel(x, y) = CGAPalette(b)

                    Case 6
                        b = mCPU.Memory(mStartGraphicsVideoAddress + ((y >> 1) * 80) + ((y And 1) * &H2000) + (x >> 3))
                        b = (b >> (7 - (x And 7))) And 1
                        b *= 15
                        videoBMP.Pixel(x, y) = CGAPalette(b)

                    Case &HD, &HE
                        a1 = x >> 1
                        a2 = y >> 1
                        a3 = a2 * 40 + (a1 >> 3)
                        a2 = 7 - (a1 And 7)
                        b = (VRAM(a3) >> a2) And 1
                        b = b + ((VRAM(a3 + &H10000) >> a2) And 1) << 1
                        b = b + ((VRAM(a3 + &H20000) >> a2) And 1) << 2
                        b = b + ((VRAM(a3 + &H30000) >> a2) And 1) << 3
                        videoBMP.Pixel(x, y) = Color.FromArgb(VGAPalette(b))

                    Case &H10
                        a1 = (y * 80) + (x >> 3)
                        a2 = 7 - (x And 7)
                        b = (VRAM(a1) >> a2) And 1
                        b += ((VRAM(a1 + &H10000) >> a2) And 1) << 1
                        b += ((VRAM(a1 + &H20000) >> a2) And 1) << 2
                        b += ((VRAM(a1 + &H30000) >> a2) And 1) << 3
                        videoBMP.Pixel(x, y) = Color.FromArgb(VGAPalette(b))

                    Case &H12
                        a1 = (y * 80) + (x / 8)
                        a2 = ((Not x) And 7)
                        b = (VRAM(a1) >> a2) And 1
                        b = b Or ((VRAM(a1 + &H10000) >> a2) And 1) << 1
                        b = b Or ((VRAM(a1 + &H20000) >> a2) And 1) << 2
                        b = b Or ((VRAM(a1 + &H30000) >> a2) And 1) << 3
                        videoBMP.Pixel(x, y) = Color.FromArgb(VGAPalette(b))

                    Case &H13
                        If planeMode Then
                            a1 = (y * mVideoResolution.Width + x) / 4 + (x And 3) * &H10000 + vgaPage - (VGA_ATTR(&H13) And &H15)
                            b = VRAM(a1)
                        Else
                            b = mCPU.Memory(mStartGraphicsVideoAddress + y * mVideoResolution.Width + x)
                        End If
                        videoBMP.Pixel(x, y) = Color.FromArgb(VGAPalette(b))

                    Case 127
                        b = mCPU.Memory(mStartGraphicsVideoAddress + ((y And 3) << 13) + ((y >> 2) * 90) + (x >> 3))
                        b = (b >> (7 - (x And 7))) And 1
                        videoBMP.Pixel(x, y) = CGAPalette(b)

                    Case Else
                        b = mCPU.Memory(mStartGraphicsVideoAddress + ((y >> 1) * 80) + ((y And 1) * &H2000) + (x >> xDiv))
                        If PixelsPerByte = 4 Then
                            Select Case x And 3
                                Case 3 : b = b And 3
                                Case 2 : b = (b >> 2) And 3
                                Case 1 : b = (b >> 4) And 3
                                Case 0 : b = (b >> 6) And 3
                            End Select
                        Else
                            b = (b >> (7 - (x And 7))) And 1
                        End If
                        videoBMP.Pixel(x, y) = CGAPalette(b)

                End Select
            Next
        Next
    End Sub

    Private Sub RenderText()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0

        Dim r As New Rectangle(Point.Empty, CellSize)

        Dim vgaPage As Integer = (VGA_CRTC(&HC) << 8) + VGA_CRTC(&HD)
        Dim intensity As Boolean = (portRAM(&H3D8 - &H3C0) And &H80) <> 0
        Dim mode As Boolean = (portRAM(&H3D8 - &H3C0) = 9) AndAlso (portRAM(&H3D4 - &H3C0) = 9)

        For address As Integer = StartTextVideoAddress To EndTextVideoAddress Step 2
            b0 = mCPU.Memory(address)
            b1 = mCPU.Memory(address + 1)

            If mVideoMode = 7 OrElse mVideoMode = 127 Then
                If (b1 And &H70) <> 0 Then
                    If b0 = 0 Then
                        b1 = 7
                    Else
                        b1 = 0
                    End If
                Else
                    If b0 = 0 Then
                        b1 = 0
                    Else
                        b1 = 7
                    End If
                End If
            End If

            'If IsDirty(address) OrElse IsDirty(address + 1) OrElse cursorAddress.Contains(address) Then
            RenderChar(b0, videoBMP, brushCache(b1.LowNib()), brushCache(b1.HighNib() And If(intensity, 7, &HF)), r.Location)
            cursorAddress.Remove(address)
            'End If

            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
                If (blinkCounter < BlinkRate AndAlso CursorVisible) Then
                    videoBMP.FillRectangle(brushCache(b1.LowNib()),
                                           r.X + 0, r.Y - 1 + CellSize.Height - (CursorEnd - CursorStart) - 1,
                                           CellSize.Width, (CursorEnd - CursorStart) + 1)
                    cursorAddress.Add(address)
                End If

                If blinkCounter >= 2 * BlinkRate Then
                    blinkCounter = 0
                Else
                    blinkCounter += 1
                End If
            End If

            r.X += CellSize.Width
            col += 1
            If col = TextResolution.Width Then
                col = 0
                row += 1
                If row = TextResolution.Height Then Exit For

                r.X = 0
                r.Y += CellSize.Height
            End If
        Next
    End Sub

    Public Function ColRowToRectangle(col As Integer, row As Integer) As Rectangle
        Return New Rectangle(New Point(col * CellSize.Width, row * CellSize.Height), CellSize)
    End Function

    Public Function ColRowToAddress(col As Integer, row As Integer) As Integer
        Return StartTextVideoAddress + row * (TextResolution.Width * 2) + (col * 2)
    End Function

    Private Sub RenderChar(c As Integer, dbmp As DirectBitmap, fb As Color, bb As Color, p As Point)
        Dim ccc As New CGAChar(c, fb, bb)
        Dim idx As Integer = cgaCharsCache.IndexOf(ccc)
        If idx = -1 Then
            ccc.Render(CellSize)
            cgaCharsCache.Add(ccc)
            idx = cgaCharsCache.Count - 1
        End If
        cgaCharsCache(idx).Paint(dbmp, p, scale)
    End Sub

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

    Protected Overrides Sub OnPaletteRegisterChanged()
        MyBase.OnPaletteRegisterChanged()

        DisposeColorCaches()
        For i As Integer = 0 To CGAPalette.Length - 1
            brushCache(i) = CGAPalette(i)
        Next
    End Sub

    Private Function MeasureChar(graphics As Graphics, code As Integer, text As Char, font As Font) As Size
        Dim size As Size

        If useCGAFont Then
            size = New Size(8, 16)
            charSizeCache.Add(code, size)
        Else
            If charSizeCache.ContainsKey(code) Then Return charSizeCache(code)

            Dim rect As RectangleF = New RectangleF(0, 0, 1000, 1000)
            Dim ranges() As CharacterRange = {New CharacterRange(0, 1)}
            Dim regions() As Region = {New Region()}

            textFormat.SetMeasurableCharacterRanges(ranges)

            regions = graphics.MeasureCharacterRanges(text, font, rect, textFormat)
            rect = regions(0).GetBounds(graphics)

            size = New Size(rect.Right - 1, rect.Bottom)
            charSizeCache.Add(code, size)
        End If

        Return size
    End Function

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)

        If mRenderControl IsNot Nothing Then
            If clearScreen OrElse charSizeCache.Count = 0 Then
                charSizeCache.Clear()
                Using g = mRenderControl.CreateGraphics()
                    For i As Integer = 0 To 255
                        MeasureChar(g, i, chars(i), mFont)
                    Next
                End Using
            End If

            cgaCharsCache.Clear()

            If videoBMP IsNot Nothing Then videoBMP.Dispose()
            If GraphicsResolution.Width = 0 Then
                videoBMP = New DirectBitmap(640, 480)
            Else
                videoBMP = New DirectBitmap(GraphicsResolution.Width, GraphicsResolution.Height)
            End If
        End If
    End Sub

    Public Overrides Sub Run()
        If mRenderControl IsNot Nothing Then mRenderControl.Invalidate()
    End Sub
End Class

Public Class VGAWinForms
    Inherits VGAAdapter

    Private charsCache As New List(Of VideoChar)
    Private charSizeCache As New Dictionary(Of Integer, Size)

    Private cursorSize As Size
    Private frameRate As Integer = 30
    Private cursorAddress As New List(Of Integer)

    Private ReadOnly preferredFont As String = "Perfect DOS VGA 437"
    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
    Private textFormat As StringFormat = New StringFormat(StringFormat.GenericTypographic)

    Private ReadOnly brushCache(CGAPalette.Length - 1) As Color
    Private cursorBrush As Color = Color.FromArgb(128, Color.White)
    Private cursorYOffset As Integer

    Private ReadOnly fontSourceMode As FontSources
    Private g As Graphics

    Private scale As New SizeF(1, 1)

    Private mCPU As X8086
    Private mRenderControl As Control
    Private mHideHostCursor As Boolean = True

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

    Public Sub New(cpu As X8086, renderControl As Control, Optional fontSource As FontSources = FontSources.BitmapFile, Optional bitmapFontFile As String = "")
        MyBase.New(cpu)
        fontSourceMode = fontSource
        mCPU = cpu
        Me.RenderControl = renderControl

        AddHandler mRenderControl.KeyDown, Sub(sender As Object, e As KeyEventArgs) HandleKeyDown(Me, e)
        AddHandler mRenderControl.KeyUp, Sub(sender As Object, e As KeyEventArgs) HandleKeyUp(Me, e)

        AddHandler mRenderControl.MouseDown, Sub(sender As Object, e As MouseEventArgs) OnMouseDown(Me, e)
        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs)
                                                 If mCPU.Mouse?.IsCaptured Then
                                                     OnMouseMove(Me, e)
                                                     Cursor.Position = mRenderControl.PointToScreen(mCPU.Mouse.MidPoint)
                                                 End If
                                             End Sub
        AddHandler mRenderControl.MouseUp, Sub(sender As Object, e As MouseEventArgs) OnMouseUp(Me, e)

        Dim fontCGAPath As String = X8086.FixPath("roms\" + bitmapFontFile)
        Dim fontCGAError As String = ""

        Select Case fontSource
            Case FontSources.BitmapFile
                If IO.File.Exists(fontCGAPath) Then
                    Try
                        VideoChar.FontBitmaps = IO.File.ReadAllBytes(fontCGAPath)
                    Catch ex As Exception
                        fontCGAError = ex.Message
                        fontSourceMode = FontSources.TrueType
                    End Try
                Else
                    fontCGAError = "File not found"
                    fontSourceMode = FontSources.TrueType
                End If
            Case FontSources.ROM
                VideoChar.BuildFontBitmapsFromROM(8, 16, 14, &HC0000 + &H3310, mCPU.Memory)
        End Select

        If fontSourceMode = FontSources.TrueType Then
            If mFont.Name <> preferredFont Then
                MsgBox(If(fontSource = FontSources.BitmapFile, "ASCII VGA Font Data not found at '" + fontCGAPath + "'" + If(fontCGAError <> "", ": " + fontCGAError, "") +
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

        Dim tmp As New Threading.Thread(Sub()
                                            Do
                                                Threading.Thread.Sleep(1000 \ frameRate)
                                                mRenderControl.Invalidate()
                                            Loop Until cancelAllThreads
                                        End Sub)
        tmp.Start()

        InitVideoMemory(False)
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
                mRenderControl.Invoke(Sub() ResizeRenderControl())
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

        'Dim frmSize As New Size(If(ctrlSize.Width = 0, 640, ctrlSize.Width) * Zoom, If(ctrlSize.Height = 0, 640, ctrlSize.Height) * Zoom)
        Dim r As Double = 1 '(If(ctrlSize.Width = 0, 640, ctrlSize.Width) / 640) / (If(ctrlSize.Height = 0, 400, ctrlSize.Height) / 400)
        Dim frmSize As New Size(640 * Zoom * r, 400 * Zoom / r)
        mRenderControl.FindForm.ClientSize = frmSize
        mRenderControl.Size = frmSize
        If CellSize.Width = 0 OrElse CellSize.Height = 0 Then Exit Sub

        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
    End Sub

    Protected Overrides Sub Render()
        If VideoEnabled Then
            SyncLock videoBMP
                Try ' FIXME: This sill fails (sometimes) when switching modes
                    Select Case MainMode
                        Case MainModes.Text : RenderText()
                        Case MainModes.Graphics : RenderGraphics()
                    End Select
                Catch
                End Try
            End SyncLock
        End If
    End Sub

    Private Sub Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed

        g.ScaleTransform(scale.Width, scale.Height)

        OnPreRender(sender, e)
        g.CompositingMode = Drawing2D.CompositingMode.SourceCopy

        g.DrawImageUnscaled(videoBMP, 0, 0)

        g.CompositingMode = Drawing2D.CompositingMode.SourceOver
        OnPostRender(sender, e)

        'RenderWaveform(g)
    End Sub

    Private Sub RenderGraphics()
        Dim b As Byte
        Dim xDiv As Integer = If(PixelsPerByte = 4, 2, 3)
        Dim usePal As Integer = (portRAM(&H3D9) >> 5) And 1
        Dim intensity As Integer = ((portRAM(&H3D9) >> 4) And 1) << 3

        ' For modes &h12 and &h13
        Dim planeMode As Boolean = If(mVideoMode = &H12 OrElse mVideoMode = &H13, (VGA_SC(4) And 6) <> 0, False)
        Dim vgaPage As UInt32 = If(mVideoMode = &H12 OrElse mVideoMode = &H13, (CUInt(VGA_CRTC(&HC)) << 8) + CUInt(VGA_CRTC(&HD)), 0)

        Dim address As UInt32
        Dim h1 As UInt32
        Dim h2 As UInt32

        For y As Integer = 0 To GraphicsResolution.Height - 1
            For x As Integer = 0 To GraphicsResolution.Width - 1
                Select Case mVideoMode
                    Case 4, 5
                        b = mCPU.RAM(mStartGraphicsVideoAddress + ((y >> 1) * 80) + ((y And 1) * &H2000) + (x >> 2))
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
                            b = b * &H3F
                            b = b Mod CGAPalette.Length
                        End If
                        videoBMP.Pixel(x, y) = CGAPalette(b)

                    Case 6
                        b = mCPU.Memory(mStartGraphicsVideoAddress + ((y >> 1) * 80) + ((y And 1) * &H2000) + (x >> 3))
                        b = (b >> (7 - (x And 7))) And 1
                        b *= 15
                        videoBMP.Pixel(x, y) = CGAPalette(b)

                    Case &HD, &HE
                        h1 = x '>> 1
                        h2 = y '>> 1
                        address = h2 * 40 + (h1 >> 3)
                        h1 = 7 - (h1 And 7)
                        b = (vRAM(address) >> h1) And 1
                        b = b + ((vRAM(address + &H10000) >> h1) And 1) << 1
                        b = b + ((vRAM(address + &H20000) >> h1) And 1) << 2
                        b = b + ((vRAM(address + &H30000) >> h1) And 1) << 3
                        videoBMP.Pixel(x, y) = vgaPalette(b)

                    Case &H10, &H12
                        address = (y * 80) + (x >> 3)
                        h1 = 7 - (x And 7)
                        b = (vRAM(address) >> h1) And 1
                        b = b Or ((vRAM(address + &H10000) >> h1) And 1) << 1
                        b = b Or ((vRAM(address + &H20000) >> h1) And 1) << 2
                        b = b Or ((vRAM(address + &H30000) >> h1) And 1) << 3
                        videoBMP.Pixel(x, y) = vgaPalette(b)

                    Case &H13
                        If planeMode Then
                            address = ((y * mVideoResolution.Width + x) >> 2) + (x And 3) * &H10000 + vgaPage - (VGA_ATTR(&H13) And &HF)
                            b = vRAM(address)
                        Else
                            b = mCPU.Memory(mStartGraphicsVideoAddress + vgaPage + y * mVideoResolution.Width + x)
                        End If
                        videoBMP.Pixel(x, y) = vgaPalette(b)

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
        Dim intensity As Boolean = (portRAM(&H3D8) And &H80) <> 0
        Dim mode As Boolean = (portRAM(&H3D8) = 9) AndAlso (portRAM(&H3D4) = 9)

        For address As Integer = 0 To MEMSIZE - 2 Step 2
            b0 = vRAM(address)
            b1 = vRAM(address + 1)

            If mVideoMode = 7 OrElse mVideoMode = 127 Then
                If (b1 And &H70) <> 0 Then
                    b1 = If(b0 = 0, 7, 0)
                Else
                    b1 = If(b0 = 0, 0, 7)
                End If
            End If

            'If IsDirty(address) OrElse IsDirty(address + 1) OrElse cursorAddress.Contains(address) Then
            RenderChar(b0, videoBMP, brushCache(b1.LowNib()), brushCache(b1.HighNib() And If(intensity, 7, &HF)), r.Location)
            cursorAddress.Remove(address)
            'End If

            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
                videoBMP.FillRectangle(brushCache(b1.LowNib()),
                                           r.X + 0, r.Y - 1 + CellSize.Height - (CursorEnd - CursorStart) - 1,
                                           CellSize.Width, CursorEnd - CursorStart + 1)
                cursorAddress.Add(address)
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

    Private Sub RenderChar(c As Integer, dbmp As DirectBitmap, fb As Color, bb As Color, p As Point)
        If fontSourceMode = FontSources.TrueType Then
            Using bbb As New SolidBrush(bb)
                g.FillRectangle(bbb, New Rectangle(p, CellSize))
                Using bfb As New SolidBrush(fb)
                    g.DrawString(Char.ConvertFromUtf32(c), mFont, bfb, p.X - CellSize.Width / 2 + 2, p.Y)
                End Using
            End Using
        Else
            Dim ccc As New VideoChar(c, fb, bb)
            Dim idx As Integer = charsCache.IndexOf(ccc)
            If idx = -1 Then
                ccc.Render(CellSize.Width, CellSize.Height)
                charsCache.Add(ccc)
                idx = charsCache.Count - 1
            End If
            charsCache(idx).Paint(dbmp, p, scale)
        End If
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

        Select Case fontSourceMode
            Case FontSources.BitmapFile
                charSizeCache.Add(code, CellSize)
            Case FontSources.TrueType
                If charSizeCache.ContainsKey(code) Then Return charSizeCache(code)

                Dim rect As RectangleF = New RectangleF(0, 0, 1000, 1000)
                Dim ranges() As CharacterRange = {New CharacterRange(0, 1)}
                Dim regions() As Region = {New Region()}

                textFormat.SetMeasurableCharacterRanges(ranges)

                regions = graphics.MeasureCharacterRanges(text, font, rect, textFormat)
                rect = regions(0).GetBounds(graphics)

                size = New Size(rect.Right - 1, rect.Bottom)
                charSizeCache.Add(code, size)
            Case FontSources.ROM
                charSizeCache.Add(code, CellSize)
        End Select

        Return CellSize
    End Function

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)

        If mRenderControl IsNot Nothing Then
            SyncLock videoBMP
                If videoBMP IsNot Nothing Then videoBMP.Dispose()
                If GraphicsResolution.Width = 0 Then
                    VideoMode = 3
                    Exit Sub
                End If
                videoBMP = New DirectBitmap(GraphicsResolution.Width, GraphicsResolution.Height)

                If clearScreen OrElse charSizeCache.Count = 0 Then
                    charSizeCache.Clear()
                    Using g = mRenderControl.CreateGraphics()
                        For i As Integer = 0 To 255
                            MeasureChar(g, i, chars(i), mFont)
                        Next
                    End Using
                End If

                charsCache.Clear()

                If fontSourceMode = FontSources.TrueType Then
                    If g IsNot Nothing Then g.Dispose()
                    g = Graphics.FromImage(videoBMP)
                End If
            End SyncLock
        End If
    End Sub

    Public Overrides Sub Run()
        If mRenderControl IsNot Nothing Then mRenderControl.Invalidate()
    End Sub
End Class

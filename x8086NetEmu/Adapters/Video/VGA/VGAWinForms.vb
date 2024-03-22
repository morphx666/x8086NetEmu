Imports System.Net
Imports System.Security.Cryptography

Public Class VGAWinForms
    Inherits VGAAdapter

    Private blinkCounter As Integer
    Private frameRate As Integer = 30
    Private cursorAddress As New List(Of Integer)

    Private ReadOnly preferredFont As String = "Perfect DOS VGA 437"
    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
    Private textFormat As StringFormat = New StringFormat(StringFormat.GenericTypographic)

    Private ReadOnly brushCache(cgaPalette.Length - 1) As Color

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

    Public Sub New(cpu As X8086, renderControl As Control, Optional fontSource As FontSources = FontSources.BitmapFile, Optional bitmapFontFile As String = "", Optional enableWebUI As Boolean = False)
        MyBase.New(cpu, , enableWebUI)
        fontSourceMode = fontSource
        mCPU = cpu
        Me.RenderControl = renderControl

        AddHandler mRenderControl.KeyDown, Sub(sender As Object, e As KeyEventArgs) HandleKeyDown(Me, e)
        AddHandler mRenderControl.KeyUp, Sub(sender As Object, e As KeyEventArgs) HandleKeyUp(Me, e)

        AddHandler mRenderControl.MouseDown, Sub(sender As Object, e As MouseEventArgs) OnMouseDown(Me, e)
        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs)
                                                 If MyBase.CPU.Mouse?.IsCaptured Then
                                                     OnMouseMove(Me, e)
                                                     Cursor.Position = mRenderControl.PointToScreen(MyBase.CPU.Mouse.MidPointOffset)
                                                 End If
                                             End Sub
        AddHandler mRenderControl.MouseUp, Sub(sender As Object, e As MouseEventArgs) OnMouseUp(Me, e)

        Dim fontCGAPath As String = X8086.FixPath("misc\" + bitmapFontFile)
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
                MyBase.CellSize = New Size(8, 14)
                VideoChar.BuildFontBitmapsFromROM(8, 14, 14, &HC0000 + &H3310, mCPU.Memory)
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
        DetachRenderControl()
    End Sub

    Protected Overrides Sub AutoSize()
        If mRenderControl IsNot Nothing Then
            If mRenderControl.InvokeRequired Then
                mRenderControl.Invoke(Sub() ResizeRenderControl())
            Else
                ResizeRenderControl()
            End If
        End If
    End Sub

    Protected Overrides Sub ResizeRenderControl()
        Dim ctrlSize As Size

        If MainMode = MainModes.Text Then
            ctrlSize = New Size(mCellSize.Width * TextResolution.Width, mCellSize.Height * TextResolution.Height)
        Else
            ctrlSize = New Size(GraphicsResolution.Width, GraphicsResolution.Height)
        End If

        Dim frmSize As New Size(640 * Zoom, 400 * Zoom)
        Dim frm As Form = mRenderControl.FindForm
        frm.ClientSize = frmSize
        mRenderControl.Location = Point.Empty
        mRenderControl.Size = frmSize
        If mCellSize.Width = 0 OrElse mCellSize.Height = 0 Then Exit Sub

        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
    End Sub

    Protected Overrides Sub Render()
        If mVideoEnabled Then
            Try
                SyncLock chars
                    Select Case MainMode
                        Case MainModes.Text : RenderText()
                        Case MainModes.Graphics : RenderGraphics()
                    End Select
                End SyncLock
            Catch : End Try
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
        Dim b0 As UInt32
        Dim b1 As UInt32
        Dim usePal As Integer = If(mVideoMode = 5, 1, (portRAM(&H3D9) >> 5) And 1)
        Dim intensity As Integer = If(mVideoMode = 5, 8, ((portRAM(&H3D9) >> 4) And 1) << 3)
        Dim xDiv As Integer = If(PixelsPerByte = 4, 2, 3)

        Dim address As UInt32
        Dim h1 As UInt32
        Dim h2 As UInt32
        Dim k As UInt32 = mCellSize.Width * mCellSize.Height
        Dim r As New Rectangle(Point.Empty, CellSize)

        For y As Integer = 0 To GraphicsResolution.Height - 1 Step If(mVideoMode = 6, 2, 1)
            For x As Integer = 0 To GraphicsResolution.Width - 1
                Select Case mVideoMode
                    Case 4, 5
                        b0 = CPU.Memory(mStartGraphicsVideoAddress + ((y >> 1) * (mTextResolution.Width << 1)) +
                                                                     ((y And 1) * &H2000) + (x >> 2))
                        Select Case x And 3
                            Case 3 : b0 = b0 And 3
                            Case 2 : b0 = (b0 >> 2) And 3
                            Case 1 : b0 = (b0 >> 4) And 3
                            Case 0 : b0 = (b0 >> 6) And 3
                        End Select
                        b0 = b0 * 2 + usePal + intensity
                        If b0 = (usePal + intensity) Then b0 = 0
                        videoBMP.Pixel(x, y) = cgaPalette(b0 And &HF)

                    Case 6
                        h2 = y >> 1
                        b0 = CPU.Memory(mStartGraphicsVideoAddress + ((h2 >> 1) * mTextResolution.Width) +
                                                                     ((h2 And 1) * &H2000) + (x >> 3))
                        b0 = (b0 >> (7 - (x And 7))) And 1
                        b0 *= 15
                        videoBMP.Pixel(x, y) = cgaPalette(b0)
                        videoBMP.Pixel(x, y + 1) = cgaPalette(b0)

                    Case &HD
                        address = y * mTextResolution.Width + (x >> 3)
                        h1 = 7 - (x And 7)
                        b0 = (vRAM(address) >> h1) And 1
                        b0 = b0 Or ((vRAM(address + &H10000) >> h1) And 1) << 1
                        b0 = b0 Or ((vRAM(address + &H20000) >> h1) And 1) << 2
                        b0 = b0 Or ((vRAM(address + &H30000) >> h1) And 1) << 3
                        videoBMP.Pixel(x, y) = vgaPalette(b0)

                    Case &HE
                        h1 = x >> 1
                        h2 = y >> 1
                        address = h2 * (mTextResolution.Width << 1) + (h1 >> 3)
                        h1 = 7 - (h1 And 7)
                        b0 = (vRAM(address) >> h1) And 1
                        b0 = b0 Or ((vRAM(address + &H10000) >> h1) And 1) << 1
                        b0 = b0 Or ((vRAM(address + &H20000) >> h1) And 1) << 2
                        b0 = b0 Or ((vRAM(address + &H30000) >> h1) And 1) << 3
                        videoBMP.Pixel(x, y) = vgaPalette(b0)

                    Case &HF
                        h1 = x >> 1
                        h2 = y >> 1
                        address = h2 * (mTextResolution.Width << 1) + (h1 >> 3)
                        h1 = 7 - (h1 And 7)
                        b0 = (vRAM(address) >> h1) And 1
                        b0 = b0 Or ((vRAM(address + &H10000) >> h1) And 1) << 1
                        b0 = b0 Or ((vRAM(address + &H20000) >> h1) And 1) << 2
                        b0 = b0 Or ((vRAM(address + &H30000) >> h1) And 1) << 3
                        videoBMP.Pixel(x, y) = vgaPalette(b0)

                    Case &H10
                        address = (y * mTextResolution.Width) + (x >> 3)
                        h1 = 7 - (x And 7)
                        b0 = (vRAM(address) >> h1) And 1
                        b0 = b0 Or ((vRAM(address + &H10000) >> h1) And 1) << 1
                        b0 = b0 Or ((vRAM(address + &H20000) >> h1) And 1) << 2
                        b0 = b0 Or ((vRAM(address + &H30000) >> h1) And 1) << 3
                        videoBMP.Pixel(x, y) = vgaPalette(b0)

                    Case &H12
                        address = (y * (mTextResolution.Width << 1)) + (x >> 3)
                        h1 = (Not x) And 7
                        b0 = (vRAM(address) >> h1) And 1
                        b0 = b0 Or ((vRAM(address + &H10000) >> h1) And 1) << 1
                        b0 = b0 Or ((vRAM(address + &H20000) >> h1) And 1) << 2
                        b0 = b0 Or ((vRAM(address + &H30000) >> h1) And 1) << 3
                        videoBMP.Pixel(x, y) = vgaPalette(b0)

                    Case &H13
                        ' This "fixes" PETSCII Robots
                        'CPU.RAM8(CPU.Registers.SS, &H1151) = 0
                        'CPU.RAM8(CPU.Registers.SS, &H1153) = 0

                        Dim planeMode As Boolean = (VGA_SC(4) And 6) <> 0
                        Dim vgaPage As UInt32 = CUInt(VGA_CRTC(&HC) << 8) + VGA_CRTC(&HD)
                        If planeMode Then
                            address = y * mVideoResolution.Width + x
                            address = (address >> 2) + (x And 3) * &H10000
                            address += vgaPage - (VGA_ATTR(&H13) And &HF)
                            b0 = vRAM(address)
                        Else
                            b0 = mCPU.Memory(mStartGraphicsVideoAddress + ((vgaPage + y * mVideoResolution.Width + x) And &HFFFF))
                        End If
                        videoBMP.Pixel(x, y) = vgaPalette(b0)

                    Case 127
                        b0 = mCPU.Memory(mStartGraphicsVideoAddress + ((y And 3) << 13) + ((y >> 2) * 90) + (x >> 3))
                        b0 = (b0 >> (7 - (x And 7))) And 1
                        videoBMP.Pixel(x, y) = cgaPalette(b0)

                    Case Else
                        b0 = mCPU.Memory(mStartGraphicsVideoAddress + ((y >> 1) * mTextResolution.Width) + ((y And 1) * &H2000) + (x >> xDiv))
                        If PixelsPerByte = 4 Then
                            Select Case x And 3
                                Case 3 : b0 = b0 And 3
                                Case 2 : b0 = (b0 >> 2) And 3
                                Case 1 : b0 = (b0 >> 4) And 3
                                Case 0 : b0 = (b0 >> 6) And 3
                            End Select
                        Else
                            b0 = (b0 >> (7 - (x And 7))) And 1
                        End If
                        videoBMP.Pixel(x, y) = cgaPalette(b0)

                End Select
            Next
        Next
    End Sub

    Private Sub RenderText()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim r As New Rectangle(Point.Empty, CellSize)

        Dim vgaPage As Integer = (VGA_CRTC(&HC) << 8) + VGA_CRTC(&HD)
        Dim intensity As Boolean = (portRAM(&H3D8) And &H80) <> 0
        Dim mode As Boolean = (portRAM(&H3D8) = 9) AndAlso (portRAM(&H3D4) = 9)

        If mVideoMode = 7 OrElse mVideoMode = 127 Then
            ' FIXME: Dummy workaround to support the cursor; Haven't found a better way yet...
            mCursorCol = mCPU.Memory(&H450)
            mCursorRow = mCPU.Memory(&H451)

            mCursorStart = mCPU.Memory(&H461) And &B0001_1111
            mCursorEnd = mCPU.Memory(&H460) And &B0001_1111

            mCursorVisible = True
        End If

        Dim address As UInt32
        For y = 0 To mTextResolution.Height - 1
            For x = 0 To mTextResolution.Width - 1
                address = mStartTextVideoAddress + (y * mTextResolution.Width + x) * 2

                If mode Then ' TODO: vgaPage mode not implemented
                    Stop
                Else
                    b0 = CPU.Memory(address)
                    b1 = CPU.Memory(address + 1)
                End If

                ' http://www.osdever.net/FreeVGA/vga/attrreg.htm
                ' FIXME: This doesn't work
                'If ((VGA_ATTR(&H10) And &B0000_1000) <> 0) AndAlso (b1 And &B1000_0000) <> 0 Then 
                '    If blinkCounter < BlinkRate Then b0 = 0
                'End If

                ' Force B&W
                'If mVideoMode = 7 OrElse mVideoMode = 127 Then
                '    If (b1 And &H70) <> 0 Then
                '        b1 = If(b0 = 0, 7, 0)
                '    Else
                '        b1 = If(b0 = 0, 0, 7)
                '    End If
                'End If

                r.X = x * CellSize.Width
                RenderChar(b0, videoBMP, brushCache(b1.LowNib()), brushCache(b1.HighNib() And If(intensity, 7, &HF)), r.Location)
                cursorAddress.Remove(address)

                If mCursorVisible AndAlso y = mCursorRow AndAlso x = mCursorCol Then
                    If blinkCounter < mBlinkRate Then
                        videoBMP.FillRectangle(brushCache(b1.LowNib()),
                                               r.X + 0, r.Y - 1 + mCellSize.Height - (mCursorEnd - mCursorStart) - 1,
                                               mCellSize.Width, mCursorEnd - mCursorStart + 1)
                        cursorAddress.Add(address)
                    End If

                    If blinkCounter >= 2 * mBlinkRate Then
                        blinkCounter = 0
                    Else
                        blinkCounter += 1
                    End If
                End If
            Next

            r.X = 0
            r.Y += mCellSize.Height
        Next
    End Sub

    Private Sub RenderChar(c As Integer, dbmp As DirectBitmap, fb As Color, bb As Color, p As Point)
        If fontSourceMode = FontSources.TrueType Then
            Using bbb As New SolidBrush(bb)
                g.FillRectangle(bbb, New Rectangle(p, mCellSize))
                Using bfb As New SolidBrush(fb)
                    g.DrawString(Char.ConvertFromUtf32(c), mFont, bfb, p.X - mCellSize.Width / 2 + 2, p.Y)
                End Using
            End Using
        Else
            Dim ccc As New VideoChar(c, fb, bb)
            Dim idx As Integer

            idx = charsCache.IndexOf(ccc)
            If idx = -1 Then
                ccc.Render(mCellSize.Width, mCellSize.Height)
                charsCache.Add(ccc)
                idx = charsCache.Count - 1
            End If
            charsCache(idx).Paint(dbmp, p, scale)
        End If
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
        Dim vm As Integer = mVideoMode

        If vm = 5 Then mVideoMode = 6
        MyBase.OnPaletteRegisterChanged()
        mVideoMode = vm

        If brushCache IsNot Nothing Then
            For i As Integer = 0 To cgaPalette.Length - 1
                brushCache(i) = cgaPalette(i)
            Next
        End If
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
            SyncLock chars
                If videoBMP IsNot Nothing Then videoBMP.Dispose()
                If GraphicsResolution.Width = 0 Then
                    VideoMode = 3
                    Exit Sub
                End If
                videoBMP = New DirectBitmap(GraphicsResolution.Width, GraphicsResolution.Height)

                If wui IsNot Nothing Then wui.Bitmap = videoBMP
            End SyncLock

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
        End If
    End Sub

    Public Overrides Sub Run()
        If mRenderControl IsNot Nothing Then mRenderControl.Invalidate()
    End Sub
End Class

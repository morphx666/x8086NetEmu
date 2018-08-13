' MODE 0x13: http://www.brackeen.com/vga/basics.html
' Color Graphics Adapter (CGA) http://webpages.charter.net/danrollins/techhelp/0066.HTM

Public Class CGAWinForms
    Inherits CGAAdapter

    Private charsCache As New List(Of VideoChar)
    Private charSizeCache As New Dictionary(Of Integer, Size)

    Private blinkCounter As Integer
    Private frameRate As Integer = 30
    Private cursorAddress As New List(Of Integer)

    Private brushCache(CGAPalette.Length - 1) As Color
    Private cursorBrush As Color = Color.FromArgb(128, Color.White)

    Private preferredFont As String = "Perfect DOS VGA 437"
    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
    Private textFormat As StringFormat = New StringFormat(StringFormat.GenericTypographic)

    Private fontSourceMode As FontSources
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
        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs) OnMouseMove(Me, e)
        AddHandler mRenderControl.MouseUp, Sub(sender As Object, e As MouseEventArgs) OnMouseUp(Me, e)

        Dim fontCGAPath As String = X8086.FixPath("roms\" + bitmapFontFile)
        Dim fontCGAError As String = ""

        Select Case fontSource
            Case FontSources.BitmapFile
                If IO.File.Exists(fontCGAPath) Then
                    Try
                        VideoChar.FontBitmaps = IO.File.ReadAllBytes(fontCGAPath)
                        mCellSize = New Size(8, 16)
                    Catch ex As Exception
                        fontCGAError = ex.Message
                        fontSourceMode = FontSources.TrueType
                    End Try
                Else
                    fontCGAError = "File not found"
                    fontSourceMode = FontSources.TrueType
                End If
            Case FontSources.ROM
                VideoChar.BuildFontBitmapsFromROM(8, 8, 8, &HFE00 + &H1A6E, mCPU.Memory)
                mCellSize = New Size(8, 8)
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
            ctrlSize = New Size(mCellSize.Width * TextResolution.Width, mCellSize.Height * TextResolution.Height)
            '    'End Using
        Else
            ctrlSize = New Size(GraphicsResolution.Width, GraphicsResolution.Height)
        End If

        Dim frmSize = New Size(640 * Zoom, 400 * Zoom)
        mRenderControl.FindForm.ClientSize = frmSize
        mRenderControl.Size = frmSize
        If mCellSize.Width = 0 OrElse mCellSize.Height = 0 Then Exit Sub

        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
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

    Protected Overrides Sub OnPaletteRegisterChanged()
        MyBase.OnPaletteRegisterChanged()

        DisposeColorCaches()
        For i As Integer = 0 To CGAPalette.Length - 1
            brushCache(i) = CGAPalette(i)
        Next
    End Sub

    Protected Overrides Sub Render()
        If VideoEnabled Then
            SyncLock videoBMP
                Select Case MainMode
                    Case MainModes.Text : RenderText()
                    Case MainModes.Graphics : RenderGraphics()
                End Select
            End SyncLock
        End If
    End Sub

    Private Sub RenderText()
        Dim b0 As Byte
        Dim b1 As Byte

        Dim col As Integer = 0
        Dim row As Integer = 0

        Dim r As New Rectangle(Point.Empty, mCellSize)

        For address As Integer = StartTextVideoAddress To EndTextVideoAddress Step 2
            b0 = mCPU.Memory(address)
            b1 = mCPU.Memory(address + 1)

            If BlinkCharOn AndAlso (b1 And &B1000_0000) Then
                If (blinkCounter < BlinkRate) Then b0 = 0
                IsDirty(address) = True
            End If

            If IsDirty(address) OrElse IsDirty(address + 1) OrElse cursorAddress.Contains(address) Then
                RenderChar(b0, videoBMP, brushCache(b1.LowNib()), brushCache(b1.HighNib()), r.Location)
                cursorAddress.Remove(address)
            End If

            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
                If (blinkCounter < BlinkRate AndAlso CursorVisible) Then
                    videoBMP.FillRectangle(brushCache(b1.LowNib()),
                                           r.X + 0, r.Y - 1 + mCellSize.Height - (MyBase.CursorEnd - MyBase.CursorStart) - 1,
                                           mCellSize.Width, (MyBase.CursorEnd - MyBase.CursorStart) + 1)
                    cursorAddress.Add(address)
                End If

                If blinkCounter >= 2 * BlinkRate Then
                    blinkCounter = 0
                Else
                    blinkCounter += 1
                End If
            End If

            r.X += mCellSize.Width
            col += 1
            If col = TextResolution.Width Then
                col = 0
                row += 1
                If row = TextResolution.Height Then Exit For

                r.X = 0
                r.Y += mCellSize.Height
            End If
        Next
    End Sub

    ' FIXME: IsDirty is not working here. Also, scrolling games present a flickering issue
    Private Sub RenderGraphics()
        Dim b As Byte
        Dim address As UInt32
        Dim xDiv As Integer = If(PixelsPerByte = 4, 2, 3)

        For y As Integer = 0 To GraphicsResolution.Height - 1
            For x As Integer = 0 To GraphicsResolution.Width - 1
                address = StartGraphicsVideoAddress + ((y >> 1) * 80) + ((y And 1) * &H2000) + (x >> xDiv)
                b = CPU.Memory(address)

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
            Next
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
            Dim idx As Integer = charsCache.IndexOf(ccc)
            If idx = -1 Then
                ccc.Render(mCellSize.Width, mCellSize.Height)
                charsCache.Add(ccc)
                idx = charsCache.Count - 1
            End If
            charsCache(idx).Paint(dbmp, p, scale)
        End If
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

        Select Case fontSourceMode
            Case FontSources.BitmapFile
                charSizeCache.Add(code, mCellSize)
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
                size = New Size(8, 8)
                charSizeCache.Add(code, size)
        End Select

        Return size
    End Function

    Private Sub DisposeColorCaches()
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "CGA WinForms Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "CGA WinForms"
        End Get
    End Property

    ' http://www.powernet.co.za/info/BIOS/Mem/
    ' http://www-ivs.cs.uni-magdeburg.de/~zbrog/asm/memory.html
    Private Sub UpdateSystemInformationArea()
        '' Display Mode
        'Emulator.RAM8(&H40, &H49) = CByte(MyBase.VideoMode)

        '' Number of columns on screen
        'Emulator.RAM16(&H40, &H4A) = TextResolution.Width

        '' Length of Regen Buffer
        'Emulator.RAM16(&H40, &H4C) = MyBase.EndTextVideoAddress - MyBase.StartTextVideoAddress

        '' Current video page start address in video memory (after 0B800:0000)
        '' Starting Address of Regen Buffer. Offset from the beginning of the display adapter memory
        'Emulator.RAM16(&H40, &H4E) = &HB800

        '' Current video page start address in video memory (after 0B800:0000)
        'For i As Integer = 0 To 1 'MyBase.pagesCount
        '    Emulator.RAM8(&H40, &H50 + i * 2 + 0) = CursorCol
        '    Emulator.RAM8(&H40, &H50 + i * 2 + 1) = CursorRow
        'Next

        '' Cursor Start and End Scan Lines
        'Emulator.RAM8(&H40, &H60) = 0 ' ????????
        'Emulator.RAM8(&H40, &H61) = 0 ' ????????

        '' Current Display Page
        'Emulator.RAM8(&H40, &H62) = 1 'activePage

        '' CRT Controller Base Address 
        'Emulator.RAM16(&H40, &H63) = &H3D4

        '' Current Setting of the Mode Control Register
        'Emulator.RAM8(&H40, &H65) = x8086.BitsArrayToWord(CGAModeControlRegister)

        '' Current Setting of the Color Select Register
        'Emulator.RAM16(&H40, &H66) = Emulator.RAM16(&H40, &H63) + 5

        '' Rows on screen minus one
        'Emulator.RAM8(&H40, &H84) = TextResolution.Height - 1
    End Sub

    Public Overrides Sub Run()
        If mRenderControl IsNot Nothing Then mRenderControl.Invalidate()
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        MyBase.InitVideoMemory(clearScreen)

        If mRenderControl IsNot Nothing Then
            If clearScreen OrElse charSizeCache.Count = 0 Then
                charSizeCache.Clear()
                Using g As Graphics = mRenderControl.CreateGraphics()
                    For i As Integer = 0 To 255
                        MeasureChar(g, i, chars(i), mFont)
                    Next
                End Using
            End If

            ' Monospace... duh!
            mCellSize = charSizeCache(65)

            If videoBMP IsNot Nothing Then videoBMP.Dispose()
            Select Case MainMode
                Case MainModes.Text
                    videoBMP = New DirectBitmap(640, 400)
                Case MainModes.Graphics
                    videoBMP = New DirectBitmap(GraphicsResolution.Width, GraphicsResolution.Height)
            End Select

            If fontSourceMode = FontSources.TrueType Then
                If g IsNot Nothing Then g.Dispose()
                g = Graphics.FromImage(videoBMP)
            End If

            UpdateSystemInformationArea()
        End If
    End Sub
End Class

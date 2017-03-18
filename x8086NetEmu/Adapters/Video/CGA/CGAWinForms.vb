'Imports System.Drawing.Imaging
'Imports System.Runtime.InteropServices

'' MODE 0x13: http://www.brackeen.com/vga/basics.html
'' Color Graphics Adapter (CGA) http://webpages.charter.net/danrollins/techhelp/0066.HTM

'Public Class CGAWinForms
'    Inherits CGAAdapter

'    Private Class CGAChar
'        Private mCGAChar As Integer
'        Private mForeColor As SolidBrush
'        Private mBackColor As SolidBrush
'        Private mBitmap As Bitmap

'        Public Sub New(c As Integer, fb As SolidBrush, bb As SolidBrush)
'            mCGAChar = c
'            mForeColor = fb
'            mBackColor = bb
'        End Sub

'        Public ReadOnly Property CGAChar As Integer
'            Get
'                Return mCGAChar
'            End Get
'        End Property

'        Public ReadOnly Property ForeColor As SolidBrush
'            Get
'                Return mForeColor
'            End Get
'        End Property

'        Public ReadOnly Property BackColor As SolidBrush
'            Get
'                Return mBackColor
'            End Get
'        End Property

'        Public Sub Paint(g As Graphics, p As Point, scale As SizeF)
'            g.DrawImageUnscaled(mBitmap, p)
'        End Sub

'        Public Sub Render()
'            If mBitmap Is Nothing Then
'                mBitmap = New Bitmap(8, 16)

'                Using g As Graphics = Graphics.FromImage(mBitmap)
'                    For y As Integer = 0 To 16 - 1
'                        For x As Integer = 0 To 8 - 1
'                            If fontCGA(mCGAChar * 128 + y * 8 + x) = 1 Then
'                                g.FillRectangle(mForeColor, x, y, 1, 1)
'                            Else
'                                g.FillRectangle(mBackColor, x, y, 1, 1)
'                            End If
'                        Next
'                    Next
'                End Using
'            End If
'        End Sub

'        Public Shared Operator =(c1 As CGAChar, c2 As CGAChar) As Boolean
'            Return c1.CGAChar = c2.CGAChar AndAlso
'                    c1.ForeColor.Color = c2.ForeColor.Color AndAlso
'                    c1.BackColor.Color = c2.BackColor.Color
'        End Operator

'        Public Shared Operator <>(c1 As CGAChar, c2 As CGAChar) As Boolean
'            Return Not (c1 = c2)
'        End Operator

'        Public Overrides Function Equals(obj As Object) As Boolean
'            Return Me = CType(obj, CGAChar)
'        End Function

'        Public Overrides Function ToString() As String
'            Return String.Format("{0:000} [{1:000}:{2:000}:{3:000}] [{4:000}:{5:000}:{6:000}]",
'                                 mCGAChar,
'                                 mForeColor.Color.R,
'                                 mForeColor.Color.G,
'                                 mForeColor.Color.B,
'                                 mBackColor.Color.R,
'                                 mBackColor.Color.G,
'                                 mBackColor.Color.B)
'        End Function
'    End Class
'    Private cgaCharsCache As New List(Of CGAChar)

'    Private mRenderControl As Control
'    Private videoBMP As Bitmap
'    Private videoBMPRect As Rectangle

'    Private charSize As Size
'    Private cursorSize As Size
'    Private blinkCounter As Integer

'    Private preferredFont As String = "Perfect DOS VGA 437"
'    Private mFont As Font = New Font(preferredFont, 16, FontStyle.Regular, GraphicsUnit.Pixel)
'    Private textFormat As StringFormat = New System.Drawing.StringFormat(StringFormat.GenericTypographic)

'    Private charSizeCache As New Dictionary(Of Integer, Size)

'    Private penCache(16 - 1) As Pen
'    Private brushCache(16 - 1) As SolidBrush
'    Private cursorBrush = New SolidBrush(Color.FromArgb(128, Color.White))
'    Private cursorYOffset As Integer

'    Private Shared fontCGA() As Byte
'    Private useCGAFont As Boolean

'    Private scale As New SizeF(1, 1)

'    Private mCPU As x8086
'    Private mHideHostCursor As Boolean = True

'    Public Event PreRender(sender As Object, e As PaintEventArgs)
'    Public Event PostRender(sender As Object, e As PaintEventArgs)

'    Private Class TaskSC
'        Inherits Scheduler.Task

'        Public Sub New(owner As IOPortHandler)
'            MyBase.New(owner)
'        End Sub

'        Public Overrides Sub Run()
'            Owner.Run()
'        End Sub

'        Public Overrides ReadOnly Property Name As String
'            Get
'                Return Owner.Name
'            End Get
'        End Property
'    End Class
'    Private task As Scheduler.Task = New TaskSC(Me)

'    Public Sub New(cpu As x8086, renderControl As Control, Optional tryUseCGAFont As Boolean = True)
'        MyBase.New(cpu)
'        useCGAFont = tryUseCGAFont
'        mCPU = cpu
'        Me.RenderControl = renderControl

'        AddHandler mRenderControl.KeyDown, Sub(sender As Object, e As KeyEventArgs) HandleKeyDown(Me, e)
'        AddHandler mRenderControl.KeyUp, Sub(sender As Object, e As KeyEventArgs) HandleKeyUp(Me, e)

'        AddHandler mRenderControl.MouseDown, Sub(sender As Object, e As MouseEventArgs) OnMouseDown(Me, e)
'        AddHandler mRenderControl.MouseMove, Sub(sender As Object, e As MouseEventArgs) OnMouseMove(Me, e)
'        AddHandler mRenderControl.MouseUp, Sub(sender As Object, e As MouseEventArgs) OnMouseUp(Me, e)

'        Dim fontCGAPath As String = x8086.FixPath("roms\asciivga.dat")
'        Dim fontCGAError As String = ""

'        If useCGAFont Then
'            If IO.File.Exists(fontCGAPath) Then
'                Try
'                    fontCGA = IO.File.ReadAllBytes(fontCGAPath)
'                Catch ex As Exception
'                    fontCGAError = ex.Message
'                    useCGAFont = False
'                End Try
'            Else
'                fontCGAError = "File not found"
'                useCGAFont = False
'            End If
'        End If

'        If Not useCGAFont Then
'            If mFont.Name <> preferredFont Then
'                MsgBox(If(useCGAFont, "ASCII VGA Font Data not found at '" + fontCGAPath + "'" + If(fontCGAError <> "", ": " + fontCGAError, "") +
'                       vbCrLf + vbCrLf, "") +
'                       "CGAWinForms requires the '" + preferredFont + "' font. Please install it before using this adapter", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
'                mFont = New Font("Consolas", 16, FontStyle.Regular, GraphicsUnit.Pixel)
'                If mFont.Name <> "Consolas" Then
'                    mFont = New Font("Andale Mono", 16, FontStyle.Regular, GraphicsUnit.Pixel)
'                    If mFont.Name <> "Andale Mono" Then
'                        mFont = New Font("Courier New", 16, FontStyle.Regular, GraphicsUnit.Pixel)
'                    End If
'                End If
'            End If
'        End If

'        textFormat.FormatFlags = StringFormatFlags.NoWrap Or
'                                   StringFormatFlags.MeasureTrailingSpaces Or
'                                   StringFormatFlags.FitBlackBox Or
'                                   StringFormatFlags.NoClip
'    End Sub

'    Public Property HideHostCursor As Boolean
'        Get
'            Return mHideHostCursor
'        End Get
'        Set(value As Boolean)
'            mHideHostCursor = value
'            If mHideHostCursor Then
'                Cursor.Hide()
'            Else
'                Cursor.Show()
'            End If
'        End Set
'    End Property

'    Public Property RenderControl As Control
'        Get
'            Return mRenderControl
'        End Get
'        Set(value As Control)
'            DetachRenderControl()
'            mRenderControl = value

'            'useSDL = TypeOf mRenderControl Is RenderCtrlSDL
'            'If useSDL Then
'            '    sdlCtrl = CType(mRenderControl, RenderCtrlSDL)
'            '    sdlCtrl.Init(Me, mFont.FontFamily.Name, mFont.Size)
'            'End If

'            InitiAdapter()

'            AddHandler mRenderControl.Paint, AddressOf Paint
'        End Set
'    End Property

'    Protected Sub DetachRenderControl()
'        If mRenderControl IsNot Nothing Then RemoveHandler mRenderControl.Paint, AddressOf Paint
'    End Sub

'    Public Overrides Sub CloseAdapter()
'        MyBase.CloseAdapter()

'        DisposeColorCaches()
'        DetachRenderControl()
'    End Sub

'    Public Overrides Sub AutoSize()
'        If mRenderControl IsNot Nothing Then
'            If mRenderControl.InvokeRequired Then
'                mRenderControl.Invoke(New MethodInvoker(AddressOf ResizeRenderControl))
'            Else
'                ResizeRenderControl()
'            End If
'        End If
'    End Sub

'    Private Sub ResizeRenderControl()
'        Dim ctrlSize As Size

'        If MainMode = MainModes.Text Then
'            '    'Using g As Graphics = mRenderControl.CreateGraphics()
'            ctrlSize = New Size(charSize.Width * TextResolution.Width, charSize.Height * TextResolution.Height)
'            '    'End Using
'        Else
'            ctrlSize = New Size(GraphicsResolution.Width, GraphicsResolution.Height)
'        End If

'        Dim frmSize = New Size(640 * Zoom, 400 * Zoom)
'        mRenderControl.FindForm.ClientSize = frmSize
'        mRenderControl.Size = frmSize
'        If charSize.Width = 0 OrElse charSize.Height = 0 Then Exit Sub

'        scale = New SizeF(frmSize.Width / ctrlSize.Width, frmSize.Height / ctrlSize.Height)
'    End Sub

'    Protected Overrides Sub Render()
'        mRenderControl.Invalidate()
'    End Sub

'    Private Sub Paint(sender As Object, e As PaintEventArgs)
'        SyncLock MyBase.lockObject
'            Dim g As Graphics = e.Graphics

'            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.Half
'            g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
'            g.CompositingQuality = Drawing2D.CompositingQuality.HighSpeed

'            g.ScaleTransform(scale.Width, scale.Height)

'            RaiseEvent PreRender(sender, e)

'            Select Case MainMode
'                Case MainModes.Text
'                    'If useSDL Then
'                    '    RenderTextSDL()
'                    'Else
'                    RenderText(g)
'                    'End If
'                Case MainModes.Graphics
'                    RenderGraphics(g)
'            End Select

'            RaiseEvent PostRender(sender, e)

'            'RenderWaveform(g)
'        End SyncLock
'    End Sub

'    Protected Overrides Sub OnPaletteRegisterChanged()
'        MyBase.OnPaletteRegisterChanged()

'        SyncLock MyBase.lockObject
'            DisposeColorCaches()
'            For i As Integer = 0 To CGAPalette.Length - 1
'                penCache(i) = New Pen(CGAPalette(i))
'                brushCache(i) = New SolidBrush(CGAPalette(i))
'            Next
'        End SyncLock
'    End Sub

'    Private Sub RenderGraphics(g As Graphics)
'        Dim b As Byte
'        Dim c As Color
'        Dim pixelsPerByte As Integer = If(VideoMode = VideoModes.Mode6_Graphic_Color_640x200, 8, 4)
'        Dim yOffset As Integer
'        Dim v As Byte

'        Dim sourceData = videoBMP.LockBits(videoBMPRect, ImageLockMode.WriteOnly, videoBMP.PixelFormat)
'        Dim sourcePointer = sourceData.Scan0
'        Dim sourceStride = sourceData.Stride
'        Dim sourceOffset As Integer
'        Dim yStride As Integer

'        For y As Integer = 0 To 200 - 1
'            If y < 100 Then ' Even Scan Lines
'                yOffset = StartGraphicsVideoAddress + y * 80
'                yStride = y * 2
'            Else            ' Odd Scan Lines
'                yStride = y Mod 100
'                yOffset = StartGraphicsVideoAddress + yStride * 80 + &H2000
'                yStride = yStride * 2 + 1
'            End If
'            yStride *= sourceStride

'            For x As Integer = 0 To 80 - 1
'                b = CPU.RAM(x + yOffset)

'                For pixel As Integer = 0 To pixelsPerByte - 1
'                    If VideoMode = VideoModes.Mode4_Graphic_Color_320x200 Then
'                        Select Case pixel And 3
'                            Case 3 : v = b And 3
'                            Case 2 : v = (b >> 2) And 3
'                            Case 1 : v = (b >> 4) And 3
'                            Case 0 : v = (b >> 6) And 3
'                        End Select
'                    Else
'                        v = (b >> (7 - (pixel And 7))) And 1
'                    End If

'                    'If mVideoMode = VideoModes.Mode4_Graphic_Color_320x200 Then
'                    'b *= 2
'                    'Else
'                    'b *= 63
'                    'End If
'                    c = CGAPalette(v)

'                    sourceOffset = (x * pixelsPerByte + pixel) * 3 + yStride
'                    Marshal.WriteByte(sourcePointer, sourceOffset + 0, c.B)      ' B
'                    Marshal.WriteByte(sourcePointer, sourceOffset + 1, c.G)      ' G
'                    Marshal.WriteByte(sourcePointer, sourceOffset + 2, c.R)      ' R
'                Next
'            Next
'        Next

'        videoBMP.UnlockBits(sourceData)
'        g.DrawImageUnscaled(videoBMP, 0, 0)
'    End Sub

'    Private Sub RenderText(g As Graphics)
'        Dim b0 As Byte
'        Dim b1 As Byte

'        Dim col As Integer = 0
'        Dim row As Integer = 0

'        Dim r As New Rectangle(Point.Empty, charSize)

'        For address As Integer = StartTextVideoAddress To EndTextVideoAddress Step 2
'            'row = (address - StartTextVideoAddress) / 2 \ TextResolution.Width
'            'col = (address - StartTextVideoAddress) / 2 Mod TextResolution.Width

'            'r.X = col * charSize.Width
'            'r.Y = row * charSize.Height

'            ' Ideally, we should use cpu.RAM as it's safer, but using cpu.Memory should be faster
'            b0 = mCPU.Memory(address)
'            b1 = mCPU.Memory(address + 1)

'            If (blinkCounter < BlinkRate) AndAlso BlinkCharOn AndAlso (b1 And &H80) Then b0 = 0

'            If useCGAFont Then
'                RenderChar(b0, g, brushCache(b1.LowNib()), brushCache(b1.HighNib()), r.Location)
'            Else
'                g.FillRectangle(brushCache(b1.HighNib()), r)
'                If b0 > 32 Then g.DrawString(chars(b0), mFont, brushCache(b1.LowNib()), r.Location, textFormat)
'            End If

'            If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
'                If (blinkCounter < BlinkRate) Then
'                    g.FillRectangle(brushCache(b1.LowNib()), r.X + 1, r.Y + cursorYOffset, cursorSize.Width, cursorSize.Height)
'                End If

'                If blinkCounter >= 2 * BlinkRate Then
'                    blinkCounter = 0
'                Else
'                    blinkCounter += 1
'                End If
'            End If

'            r.X += charSize.Width
'            col += 1
'            If col = TextResolution.Width Then
'                col = 0
'                row += 1
'                If row = TextResolution.Height Then Exit For

'                r.X = 0
'                r.Y += charSize.Height
'            End If
'        Next
'    End Sub

'    Public Function ColRowToRectangle(col As Integer, row As Integer) As Rectangle
'        Return New Rectangle(New Point(col * charSize.Width, row * charSize.Height), charSize)
'    End Function

'    Public Function ColRowToAddress(col As Integer, row As Integer) As Integer
'        Return StartTextVideoAddress + row * (TextResolution.Width * 2) + (col * 2)
'    End Function

'    Private Sub RenderChar(c As Integer, g As Graphics, fb As SolidBrush, bb As SolidBrush, p As Point)
'        Dim ccc As New CGAChar(c, fb, bb)
'        Dim idx As Integer = cgaCharsCache.IndexOf(ccc)
'        If idx = -1 Then
'            ccc.Render()
'            cgaCharsCache.Add(ccc)
'            idx = cgaCharsCache.Count - 1
'        End If
'        cgaCharsCache(idx).Paint(g, p, scale)
'    End Sub

'    'Private Sub RenderTextSDL()
'    '    Dim b0 As Byte
'    '    Dim b1 As Byte

'    '    Dim col As Integer = 0
'    '    Dim row As Integer = 0

'    '    Dim p As New Point(0, 0)

'    '    For address As Integer = StartTextVideoAddress To EndTextVideoAddress Step 2
'    '        b0 = mCPU.RAM(address)
'    '        b1 = mCPU.RAM(address + 1)

'    '        Dim text = sdlCtrl.SDLFont.Render(chars(b0), CGAPalette(b1.LowNib()), CGAPalette(b1.HighNib()))
'    '        sdlCtrl.SDLScreen.Blit(text, p)

'    '        'If CursorVisible AndAlso row = CursorRow AndAlso col = CursorCol Then
'    '        '    If (Not BlinkCursor) OrElse (blinkCounter < 10) Then
'    '        '        g.FillRectangle(cursorBrush, r.X + 1, r.Y, cursorSize.Width, cursorSize.Height - 2)
'    '        '    End If
'    '        '    blinkCounter = (blinkCounter + 1) Mod 20
'    '        'End If

'    '        p.X += sdlCtrl.SDLFontSize.Width
'    '        col += 1
'    '        If col = TextResolution.Width Then
'    '            col = 0
'    '            row += 1
'    '            If row = TextResolution.Height Then Exit For

'    '            p.X = 0
'    '            p.Y += sdlCtrl.SDLFontSize.Height
'    '        End If
'    '    Next

'    '    sdlCtrl.SDLScreen.Update()
'    'End Sub

'    Private Sub RenderWaveform(g As Graphics)
'#If Win32 Then
'        If mCPU.PIT.Speaker IsNot Nothing Then
'            g.ResetTransform()

'            Dim h As Integer = mRenderControl.Height * 0.6
'            Dim h2 As Integer = h / 2
'            Dim p1 As Point = New Point(0, mCPU.PIT.Speaker.AudioBuffer(0) / Byte.MaxValue * h + h * 0.4)
'            Dim p2 As Point
'            Dim len As Integer = mCPU.PIT.Speaker.AudioBuffer.Length

'            Using p As New Pen(Brushes.Red, 3)
'                For i As Integer = 1 To len - 1
'                    Try
'                        p2 = New Point(i / len * mRenderControl.Width, mCPU.PIT.Speaker.AudioBuffer(i) / Byte.MaxValue * h + h * 0.4)
'                        g.DrawLine(p, p1, p2)
'                        p1 = p2
'                    Catch
'                        Exit For
'                    End Try
'                Next
'            End Using
'        End If
'#End If
'    End Sub

'    Private Function MeasureChar(graphics As Graphics, code As Integer, text As Char, font As Font) As Size
'        Dim size As Size

'        If useCGAFont Then
'            size = New Size(8, 16)
'            charSizeCache.Add(code, size)
'        Else
'            If charSizeCache.ContainsKey(code) Then Return charSizeCache(code)

'            Dim rect As System.Drawing.RectangleF = New System.Drawing.RectangleF(0, 0, 1000, 1000)
'            Dim ranges() As System.Drawing.CharacterRange = {New System.Drawing.CharacterRange(0, 1)}
'            Dim regions() As System.Drawing.Region = {New System.Drawing.Region()}

'            textFormat.SetMeasurableCharacterRanges(ranges)

'            regions = graphics.MeasureCharacterRanges(text, font, rect, textFormat)
'            rect = regions(0).GetBounds(graphics)

'            size = New Size(rect.Right - 1, rect.Bottom)
'            charSizeCache.Add(code, size)
'        End If

'        Return size
'    End Function

'    Private Sub DisposeColorCaches()
'        If penCache(0) IsNot Nothing Then
'            For i As Integer = 0 To CGAPalette.Length - 1
'                penCache(i).Dispose()
'                brushCache(i).Dispose()
'            Next
'        End If
'    End Sub

'    Public Overrides ReadOnly Property Description As String
'        Get
'            Return "CGA WinForms Adapter"
'        End Get
'    End Property

'    Public Overrides ReadOnly Property Name As String
'        Get
'            Return "CGA WinForms"
'        End Get
'    End Property

'    ' http://www.powernet.co.za/info/BIOS/Mem/
'    ' http://www-ivs.cs.uni-magdeburg.de/~zbrog/asm/memory.html
'    Private Sub UpdateSystemInformationArea()
'        '' Display Mode
'        'Emulator.RAM8(&H40, &H49) = CByte(MyBase.VideoMode)

'        '' Number of columns on screen
'        'Emulator.RAM16(&H40, &H4A) = TextResolution.Width

'        '' Length of Regen Buffer
'        'Emulator.RAM16(&H40, &H4C) = MyBase.EndTextVideoAddress - MyBase.StartTextVideoAddress

'        '' Current video page start address in video memory (after 0B800:0000)
'        '' Starting Address of Regen Buffer. Offset from the beginning of the display adapter memory
'        'Emulator.RAM16(&H40, &H4E) = &HB800

'        '' Current video page start address in video memory (after 0B800:0000)
'        'For i As Integer = 0 To 1 'MyBase.pagesCount
'        '    Emulator.RAM8(&H40, &H50 + i * 2 + 0) = CursorCol
'        '    Emulator.RAM8(&H40, &H50 + i * 2 + 1) = CursorRow
'        'Next

'        '' Cursor Start and End Scan Lines
'        'Emulator.RAM8(&H40, &H60) = 0 ' ????????
'        'Emulator.RAM8(&H40, &H61) = 0 ' ????????

'        '' Current Display Page
'        'Emulator.RAM8(&H40, &H62) = 1 'activePage

'        '' CRT Controller Base Address 
'        'Emulator.RAM16(&H40, &H63) = &H3D4

'        '' Current Setting of the Mode Control Register
'        'Emulator.RAM8(&H40, &H65) = x8086.BitsArrayToWord(CGAModeControlRegister)

'        '' Current Setting of the Color Select Register
'        'Emulator.RAM16(&H40, &H66) = Emulator.RAM16(&H40, &H63) + 5

'        '' Rows on screen minus one
'        'Emulator.RAM8(&H40, &H84) = TextResolution.Height - 1
'    End Sub

'    Public Overrides Sub Run()
'        If mRenderControl IsNot Nothing Then mRenderControl.Invalidate()
'    End Sub

'    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
'        MyBase.InitVideoMemory(clearScreen)

'        If mRenderControl IsNot Nothing Then
'            If clearScreen OrElse charSizeCache.Count = 0 Then
'                SyncLock MyBase.lockObject
'                    charSizeCache.Clear()
'                    Using g = mRenderControl.CreateGraphics()
'                        For i As Integer = 0 To 255
'                            MeasureChar(g, i, chars(i), mFont)
'                        Next
'                    End Using
'                End SyncLock
'            End If

'            SyncLock MyBase.lockObject
'                If videoBMP IsNot Nothing Then videoBMP.Dispose()
'                If MainMode = MainModes.Graphics Then
'                    videoBMP = New Bitmap(GraphicsResolution.Width, GraphicsResolution.Height, PixelFormat.Format24bppRgb)
'                    videoBMPRect = New Rectangle(0, 0, GraphicsResolution.Width, GraphicsResolution.Height)
'                End If
'            End SyncLock

'            ' Monospace... duh!
'            charSize = charSizeCache(65)

'            ' Line Cursor
'            cursorSize = New Size(charSize.Width - 1, 2)
'            ' Block Cursor
'            'cursorSize = New Size(charSize.Width - 1, charSize.Height - 2)

'            cursorYOffset = charSize.Height - cursorSize.Height - 2

'            UpdateSystemInformationArea()
'        End If
'    End Sub
'End Class

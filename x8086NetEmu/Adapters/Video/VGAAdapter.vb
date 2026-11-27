' IBM VGA, emulated at the register level, with a Tseng ET4000 video BIOS (loaded at C000:0000).
'
' Written from the documented VGA hardware programming model (sequencer, CRT controller, graphics controller,
' attribute controller, DAC, and the four 64 KB memory planes). There is no INT 10h interception: the video
' BIOS programs the registers exactly as it would on a real card, and everything the adapter shows is
' derived from those registers.
'
' Memory: 4 planes x 64 KB, CPU access through the graphics controller's memory map (A0000h-BFFFFh), with
' chain-4, odd/even and planar addressing, read modes 0-1, write modes 0-3, latches, data rotate, logical
' operations, set/reset and bit mask.
'
' Display: a complete ARGB frame is generated from the registers (text from the font in plane 2 with 8/9-dot
' cells, cursor and blink; 16-color planar; CGA-compatible 2bpp and 1bpp modes including their scanline
' interleave; 256-color chain-4 and unchained; double scanning; line compare). Host renderers only need to
' display it: see GetFrame.
'
' Tseng extension registers that the BIOS probes are stored and read back, but SVGA modes are not emulated.
Public MustInherit Class VGAAdapter
    Inherits CGAAdapter

    Private Const PLANE_SIZE As Integer = &H10000
    Private Const H_FREQUENCY As Double = 31_469 ' Horizontal scan rate of standard VGA modes (Hz)
    Private Shared ReadOnly fontMapOffsets() As Integer = {&H0, &H4000, &H8000, &HC000, &H2000, &H6000, &HA000, &HE000}

    ' ---- Video memory ----
    Private ReadOnly planes(4 - 1)() As Byte
    Private ReadOnly latch(4 - 1) As Byte

    ' ---- Registers ----
    Private miscOutput As Byte
    Private featureControl As Byte
    Private seqIndex As Byte
    Private ReadOnly seq(256 - 1) As Byte
    Private crtcIndex As Byte
    Private ReadOnly crtc(256 - 1) As Byte
    Private gcIndex As Byte
    Private ReadOnly gc(256 - 1) As Byte
    Private attrIndex As Byte          ' Includes the palette address source bit (20h)
    Private attrFlipFlop As Boolean    ' False: the next write to 3C0h is an index
    Private ReadOnly attr(32 - 1) As Byte
    Private dacMask As Byte = &HFF
    Private dacReadIndex As Integer
    Private dacWriteIndex As Integer
    Private dacComponent As Integer
    Private dacReadMode As Boolean
    Private ReadOnly dac(256 * 3 - 1) As Byte      ' 6-bit R, G, B
    Private ReadOnly dacColor(256 - 1) As Integer  ' Cached ARGB
    Private ReadOnly extraPorts As New Dictionary(Of UInt16, Byte)()

    ' ---- Frame output ----
    Private frontBuffer As Integer() = New Integer(0) {}
    Private backBuffer As Integer() = New Integer(0) {}
    Private mFrameWidth As Integer = 1
    Private mFrameHeight As Integer = 1
    Private ReadOnly frameLock As New Object()
    Private frameCount As Integer
    Private ReadOnly pal16(16 - 1) As Integer

    Private Class FrameTaskSC
        Inherits Scheduler.SchTask

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
    Private ReadOnly frameTask As New FrameTaskSC(Me)

    Public Sub New(cpu As X8086, Optional useInternalTimer As Boolean = True)
        MyBase.New(cpu, useInternalTimer)

        For p As Integer = 0 To 3
            planes(p) = New Byte(PLANE_SIZE - 1) {}
        Next
        For i As Integer = 0 To dacColor.Length - 1
            dacColor(i) = &HFF000000 ' Opaque black until the BIOS loads the DAC
        Next
        mCellSize = New XSize(8, 16)

        cpu.LoadBIN("roms\et4000(4-7-93).BIN", &HC000, &H0)

        ' VGA registers plus the Tseng ports the BIOS uses (3BFh/3D8h KEY, 3CDh segment select). 3BCh-3BEh are
        ' left alone: that's the MDA parallel port, which the system BIOS probes
        RegisteredPorts.Clear()
        For Each p As UInt16 In New UInt16() {&H3B4, &H3B5, &H3BA, &H3BF, &H3D4, &H3D5, &H3D8, &H3DA}
            RegisteredPorts.Add(p)
        Next
        For i As UInt16 = &H3C0 To &H3CF
            RegisteredPorts.Add(i)
        Next

        cpu.TryAttachHook(New X8086.MemHandler(AddressOf MemoryHook))
    End Sub

    Public Overrides Sub InitAdapter()
        If Not isInit Then
            CPU.Sched.RunTaskEach(frameTask, CLng(Scheduler.HOSTCLOCK) \ CLng(VERTSYNC))
            isInit = True
        End If
    End Sub

    Public Overrides Sub CloseAdapter()
        frameTask.Cancel()
        MyBase.CloseAdapter()
    End Sub

    ' Called by the CGA base constructor (before this class' fields exist) and on resets: the registers'
    ' power-on state is undefined; the video BIOS programs them
    Public Overrides Sub Reset()
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
    End Sub

    ' The current BIOS video mode, as recorded by the video BIOS in the BIOS data area
    Public Overrides Property VideoMode As UInt32
        Get
            Return CPU.Memory(&H449)
        End Get
        Set(value As UInt32)
        End Set
    End Property

    ' Frame task (one per vertical refresh): generate the frame and let the host present it
    Public Overrides Sub Run()
        frameCount += 1
        GenerateFrame()
        Render()
    End Sub

    ''' <summary>
    ''' Returns the most recent complete frame (ARGB, row-major) and its size. Thread-safe; the returned array
    ''' is not modified afterwards.
    ''' </summary>
    Public Function GetFrame(<Runtime.InteropServices.Out> ByRef width As Integer, <Runtime.InteropServices.Out> ByRef height As Integer) As Integer()
        SyncLock frameLock
            width = mFrameWidth
            height = mFrameHeight
            Return frontBuffer
        End SyncLock
    End Function

#Region "Ports"
    Private ReadOnly Property ColorEmulation As Boolean
        Get
            Return (miscOutput And 1) <> 0 ' 3Dxh (color) vs 3Bxh (monochrome) CRTC/status addresses
        End Get
    End Property

    Public Overrides Function [In](port As UInt16) As Byte
        Select Case port
            Case &H3C0 : Return attrIndex
            Case &H3C1 : Return attr(attrIndex And &H1F)
            Case &H3C2 : Return 0 ' Input status 0 (switch sense / vertical retrace interrupt)
            Case &H3C4 : Return seqIndex
            Case &H3C5 : Return seq(seqIndex)
            Case &H3C6 : Return dacMask
            Case &H3C7 : Return If(dacReadMode, CByte(3), CByte(0))
            Case &H3C8 : Return CByte(dacWriteIndex)
            Case &H3C9
                Dim v As Byte = dac(dacReadIndex * 3 + dacComponent)
                dacComponent += 1
                If dacComponent = 3 Then
                    dacComponent = 0
                    dacReadIndex = (dacReadIndex + 1) And &HFF
                End If
                Return v
            Case &H3CA : Return featureControl
            Case &H3CC : Return miscOutput
            Case &H3CE : Return gcIndex
            Case &H3CF : Return gc(gcIndex)

            Case &H3B4, &H3D4
                If ColorEmulation = (port = &H3D4) Then Return crtcIndex
            Case &H3B5, &H3D5
                If ColorEmulation = (port = &H3D5) Then Return crtc(crtcIndex)
            Case &H3BA, &H3DA
                If ColorEmulation = (port = &H3DA) Then
                    attrFlipFlop = False ' Reading input status 1 resets the attribute controller flip-flop
                    Return InputStatus1()
                End If
        End Select

        Dim v2 As Byte
        If extraPorts.TryGetValue(port, v2) Then Return v2
        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        Select Case port
            Case &H3C0
                If Not attrFlipFlop Then
                    attrIndex = value And &H3F
                Else
                    attr(attrIndex And &H1F) = value
                End If
                attrFlipFlop = Not attrFlipFlop
            Case &H3C2 : miscOutput = value
            Case &H3C4 : seqIndex = value
            Case &H3C5 : seq(seqIndex) = value
            Case &H3C6 : dacMask = value
            Case &H3C7
                dacReadIndex = value
                dacComponent = 0
                dacReadMode = True
            Case &H3C8
                dacWriteIndex = value
                dacComponent = 0
                dacReadMode = False
            Case &H3C9
                dac(dacWriteIndex * 3 + dacComponent) = value And &H3F
                dacComponent += 1
                If dacComponent = 3 Then
                    UpdateDacColor(dacWriteIndex)
                    dacComponent = 0
                    dacWriteIndex = (dacWriteIndex + 1) And &HFF
                End If
            Case &H3CE : gcIndex = value
            Case &H3CF : gc(gcIndex) = value

            Case &H3B4, &H3D4
                If ColorEmulation = (port = &H3D4) Then crtcIndex = value
            Case &H3B5, &H3D5
                If ColorEmulation = (port = &H3D5) Then WriteCrtc(value)
            Case &H3BA, &H3DA
                If ColorEmulation = (port = &H3DA) Then featureControl = value

            Case Else
                extraPorts(port) = value ' Tseng extensions (KEY, segment select, ...) and unused ports
        End Select
    End Sub

    Private Sub WriteCrtc(value As Byte)
        ' CRTC 11h bit 7 write-protects registers 0-7 (except the line compare bit 4 of register 7)
        If (crtc(&H11) And &H80) <> 0 AndAlso crtcIndex <= 7 Then
            If crtcIndex = 7 Then crtc(7) = (crtc(7) And Not &H10) Or (value And &H10)
            Exit Sub
        End If
        crtc(crtcIndex) = value
    End Sub

    Private Sub UpdateDacColor(index As Integer)
        Dim r As Integer = dac(index * 3), g As Integer = dac(index * 3 + 1), b As Integer = dac(index * 3 + 2)
        ' 6-bit to 8-bit: replicate the top bits into the low bits
        dacColor(index) = &HFF000000 Or (((r << 2) Or (r >> 4)) << 16) Or (((g << 2) Or (g >> 4)) << 8) Or ((b << 2) Or (b >> 4))
    End Sub

    ' Input status 1: bit 0 = display disabled (horizontal or vertical blanking), bit 3 = vertical retrace.
    ' Timed from emulated time using the programmed CRTC geometry at the standard VGA line rate.
    Private Function InputStatus1() As Byte
        Dim vde As Integer = VerticalDisplayEnd()
        Dim vTotal As Integer = crtc(6) Or ((crtc(7) And 1) << 8) Or ((crtc(7) And &H20) << 4)
        vTotal += 2
        If vTotal <= vde Then vTotal = vde + 45

        Dim lineTime As Double = CPU.Sched.CurrentTime * H_FREQUENCY / Scheduler.HOSTCLOCK
        Dim line As Long = CLng(Math.Floor(lineTime)) Mod vTotal
        Dim linePos As Double = lineTime - Math.Floor(lineTime)

        Dim vrs As Integer = crtc(&H10) Or ((crtc(7) And 4) << 6) Or ((crtc(7) And &H80) << 2)
        Dim vrLength As Integer = ((crtc(&H11) And &HF) - (vrs And &HF)) And &HF
        If vrLength = 0 Then vrLength = 16

        Dim hTotal As Integer = crtc(0) + 5
        Dim hde As Integer = crtc(1) + 1
        Dim hBlank As Boolean = hTotal > hde AndAlso linePos * hTotal >= hde

        Dim status As Byte = 0
        If line >= vde OrElse hBlank Then status = status Or &H1
        If line >= vrs AndAlso line < vrs + vrLength Then status = status Or &H8
        Return status
    End Function

    Private Function VerticalDisplayEnd() As Integer
        Return (crtc(&H12) Or ((crtc(7) And 2) << 7) Or ((crtc(7) And &H40) << 3)) + 1
    End Function
#End Region

#Region "CPU memory access (A0000h-BFFFFh)"
    Private Function MemoryHook(address As UInt32, ByRef value As UInt16, mode As X8086.MemHookMode) As Boolean
        If address < &HA0000UI OrElse address >= &HC0000UI Then Return False

        Dim base As UInt32, size As UInt32
        Select Case (gc(6) >> 2) And 3
            Case 0 : base = &HA0000UI : size = &H20000UI
            Case 1 : base = &HA0000UI : size = &H10000UI
            Case 2 : base = &HB0000UI : size = &H8000UI
            Case Else : base = &HB8000UI : size = &H8000UI
        End Select
        If address < base OrElse address >= base + size Then Return False

        Dim offset As Integer = CInt(address - base) And &HFFFF
        If mode = X8086.MemHookMode.Read Then
            value = ReadMemory(offset)
        Else
            WriteMemory(offset, CByte(value And &HFF))
        End If
        Return True
    End Function

    Private Function ReadMemory(offset As Integer) As Byte
        Dim addr As Integer
        Dim plane As Integer

        If (seq(4) And 8) <> 0 Then
            ' Chain-4: the low two address bits select the plane
            plane = offset And 3
            addr = offset And &HFFFC
        ElseIf (gc(5) And &H10) <> 0 Then
            ' Odd/even: even addresses in planes 0/2, odd in planes 1/3
            plane = (offset And 1) Or (gc(4) And 2)
            addr = offset And &HFFFE
        Else
            plane = gc(4) And 3
            addr = offset
        End If

        For p As Integer = 0 To 3
            latch(p) = planes(p)(addr)
        Next

        If (gc(5) And 8) = 0 OrElse (seq(4) And 8) <> 0 Then Return latch(plane) ' Read mode 0

        ' Read mode 1: color compare, ignoring planes cleared in "color don't care"
        Dim diff As Integer = 0
        For p As Integer = 0 To 3
            If ((gc(7) >> p) And 1) <> 0 Then
                diff = diff Or (latch(p) Xor If(((gc(2) >> p) And 1) <> 0, &HFF, 0))
            End If
        Next
        Return CByte((Not diff) And &HFF)
    End Function

    Private Sub WriteMemory(offset As Integer, value As Byte)
        Dim mapMask As Integer = seq(2) And &HF
        Dim addr As Integer

        If (seq(4) And 8) <> 0 Then
            ' Chain-4
            Dim plane As Integer = offset And 3
            If ((mapMask >> plane) And 1) <> 0 Then planes(plane)(offset And &HFFFC) = value
            Exit Sub
        ElseIf (seq(4) And 4) = 0 Then
            ' Odd/even
            addr = offset And &HFFFE
            mapMask = mapMask And If((offset And 1) <> 0, &HA, &H5)
        Else
            addr = offset
        End If

        Dim writeMode As Integer = gc(5) And 3
        Dim rotateCount As Integer = gc(3) And 7
        Dim rotated As Integer = ((value >> rotateCount) Or (value << (8 - rotateCount))) And &HFF
        Dim logicOp As Integer = (gc(3) >> 3) And 3

        For p As Integer = 0 To 3
            If ((mapMask >> p) And 1) = 0 Then Continue For

            If writeMode = 1 Then
                planes(p)(addr) = latch(p)
                Continue For
            End If

            Dim data As Integer
            Dim bitMask As Integer = gc(8)
            Select Case writeMode
                Case 0
                    If ((gc(1) >> p) And 1) <> 0 Then
                        data = If(((gc(0) >> p) And 1) <> 0, &HFF, 0)
                    Else
                        data = rotated
                    End If
                Case 2
                    data = If(((value >> p) And 1) <> 0, &HFF, 0)
                Case Else ' 3
                    bitMask = rotated And gc(8)
                    data = If(((gc(0) >> p) And 1) <> 0, &HFF, 0)
            End Select

            Select Case logicOp
                Case 1 : data = data And latch(p)
                Case 2 : data = data Or latch(p)
                Case 3 : data = data Xor latch(p)
            End Select

            planes(p)(addr) = CByte((data And bitMask) Or (latch(p) And (Not bitMask) And &HFF))
        Next
    End Sub
#End Region

#Region "Display"
    ' Memory address for a CRTC character position, following byte/word/doubleword addressing and the
    ' CGA/Hercules-compatible scanline interleave (CRTC 17h bits 0-1)
    Private Function DisplayAddress(counter As Integer, rowScan As Integer) As Integer
        Dim addr As Integer
        If (crtc(&H14) And &H40) <> 0 Then
            addr = (counter << 2) Or ((counter >> 14) And 3)
        ElseIf (crtc(&H17) And &H40) = 0 Then
            addr = (counter << 1) Or ((counter >> If((crtc(&H17) And &H20) <> 0, 15, 13)) And 1)
        Else
            addr = counter
        End If
        If (crtc(&H17) And 1) = 0 Then addr = (addr And Not &H2000) Or ((rowScan And 1) << 13)
        If (crtc(&H17) And 2) = 0 Then addr = (addr And Not &H4000) Or ((rowScan And 2) << 13)
        Return addr And &HFFFF
    End Function

    ' 4-bit attribute -> ARGB, through the color plane enable, the attribute controller palette, the color
    ' select register and the DAC
    Private Function AttributeColor(index As Integer) As Integer
        Dim p As Integer = attr((index And attr(&H12)) And &HF) And &H3F
        Dim dacIndex As Integer
        If (attr(&H10) And &H80) <> 0 Then
            dacIndex = (p And &HF) Or ((attr(&H14) And &HF) << 4)
        Else
            dacIndex = p Or ((attr(&H14) And &HC) << 4)
        End If
        Return dacColor(dacIndex And dacMask)
    End Function

    Private Sub GenerateFrame()
        Dim hde As Integer = crtc(1) + 1
        Dim vde As Integer = VerticalDisplayEnd()
        Dim isGraphics As Boolean = (attr(&H10) And 1) <> 0
        Dim nineDots As Boolean = Not isGraphics AndAlso (seq(1) And 1) = 0
        Dim dotsPerChar As Integer = If(nineDots, 9, 8)
        Dim dotScale As Integer = If((seq(1) And 8) <> 0, 2, 1) ' Dot clock / 2 doubles every pixel
        Dim width As Integer = hde * dotsPerChar * dotScale
        Dim height As Integer = vde
        If width < 8 OrElse height < 8 OrElse width > 2048 OrElse height > 1024 Then Exit Sub

        If backBuffer.Length <> width * height Then backBuffer = New Integer(width * height - 1) {}
        Dim buf As Integer() = backBuffer

        Dim blanked As Boolean = (seq(1) And &H20) <> 0 OrElse (attrIndex And &H20) = 0
        If blanked Then
            Array.Fill(buf, &HFF000000)
        Else
            For i As Integer = 0 To 15
                pal16(i) = AttributeColor(i)
            Next

            Dim startAddress As Integer = (CInt(crtc(&HC)) << 8) Or crtc(&HD)
            Dim rowAdvance As Integer = crtc(&H13) * 2
            Dim maxScan As Integer = crtc(9) And &H1F
            Dim doubleScan As Boolean = (crtc(9) And &H80) <> 0
            Dim lineCompare As Integer = crtc(&H18) Or ((crtc(7) And &H10) << 4) Or ((crtc(9) And &H40) << 3)

            Dim rowCounter As Integer = startAddress
            Dim rowScan As Integer = 0
            For y As Integer = 0 To height - 1
                If y = lineCompare Then ' Split screen: restart from address 0
                    rowCounter = 0
                    rowScan = 0
                End If

                Dim line As Integer = y * width
                If isGraphics Then
                    GraphicsScanline(buf, line, hde, rowCounter, rowScan, dotScale)
                Else
                    TextScanline(buf, line, hde, rowCounter, rowScan, dotsPerChar, dotScale)
                End If

                If Not doubleScan OrElse (y And 1) = 1 Then
                    If rowScan >= maxScan Then
                        rowScan = 0
                        rowCounter += rowAdvance
                    Else
                        rowScan += 1
                    End If
                End If
            Next
        End If

        SyncLock frameLock
            Dim t As Integer() = frontBuffer
            frontBuffer = buf
            backBuffer = If(t.Length = buf.Length, t, New Integer(buf.Length - 1) {})
            mFrameWidth = width
            mFrameHeight = height
        End SyncLock

        ' Expose the mode geometry through the common VideoAdapter properties
        mMainMode = If(isGraphics, MainModes.Graphics, MainModes.Text)
        mGraphicsResolution = New XSize(width, height)
        mTextResolution = New XSize(hde, Math.Max(1, height \ (maxScanOrOne())))
        mCellSize = New XSize(dotsPerChar, maxScanOrOne())
    End Sub

    Private Function maxScanOrOne() As Integer
        Return (crtc(9) And &H1F) + 1
    End Function

    Private Sub TextScanline(buf As Integer(), line As Integer, hde As Integer, rowCounter As Integer, rowScan As Integer, dotsPerChar As Integer, dotScale As Integer)
        Dim blinkEnabled As Boolean = (attr(&H10) And 8) <> 0
        Dim blinkOff As Boolean = (frameCount And 32) <> 0          ' Characters blink every 32 frames
        Dim cursorPhaseOn As Boolean = (frameCount And 16) = 0      ' The cursor every 16
        Dim lineGraphics As Boolean = (attr(&H10) And 4) <> 0

        Dim cursorEnabled As Boolean = (crtc(&HA) And &H20) = 0
        Dim cursorStartLine As Integer = crtc(&HA) And &H1F
        Dim cursorEndLine As Integer = crtc(&HB) And &H1F
        Dim cursorAddress As Integer = (CInt(crtc(&HE)) << 8) Or crtc(&HF)

        Dim charMapSelect As Integer = seq(3)
        Dim mapA As Integer = fontMapOffsets(((charMapSelect >> 2) And 3) Or ((charMapSelect >> 3) And 4))
        Dim mapB As Integer = fontMapOffsets((charMapSelect And 3) Or ((charMapSelect >> 2) And 4))

        Dim plane0 As Byte() = planes(0), plane1 As Byte() = planes(1), plane2 As Byte() = planes(2)
        Dim x As Integer = line

        For col As Integer = 0 To hde - 1
            Dim counter As Integer = rowCounter + col
            Dim addr As Integer = DisplayAddress(counter, rowScan)
            Dim ch As Integer = plane0(addr)
            Dim at As Integer = plane1(addr)

            Dim fg As Integer = at And &HF
            Dim bg As Integer = at >> 4
            If blinkEnabled Then
                bg = bg And 7
                If (at And &H80) <> 0 AndAlso blinkOff Then fg = bg
            End If

            Dim fontBase As Integer = If((at And 8) <> 0, mapA, mapB)
            Dim glyph As Integer = plane2((fontBase + ch * 32 + rowScan) And &HFFFF)

            If cursorEnabled AndAlso cursorPhaseOn AndAlso (counter And &HFFFF) = cursorAddress AndAlso
               rowScan >= cursorStartLine AndAlso rowScan <= cursorEndLine Then glyph = &HFF

            Dim fgColor As Integer = pal16(fg)
            Dim bgColor As Integer = pal16(bg)

            For b As Integer = 0 To 7
                Dim c As Integer = If((glyph And (&H80 >> b)) <> 0, fgColor, bgColor)
                For s As Integer = 1 To dotScale
                    buf(x) = c : x += 1
                Next
            Next

            If dotsPerChar = 9 Then
                ' 9th column: repeats the 8th for the line-drawing characters, background otherwise
                Dim c As Integer = If(lineGraphics AndAlso ch >= &HC0 AndAlso ch <= &HDF AndAlso (glyph And 1) <> 0, fgColor, bgColor)
                For s As Integer = 1 To dotScale
                    buf(x) = c : x += 1
                Next
            End If
        Next
    End Sub

    Private Sub GraphicsScanline(buf As Integer(), line As Integer, hde As Integer, rowCounter As Integer, rowScan As Integer, dotScale As Integer)
        Dim color256 As Boolean = (gc(5) And &H40) <> 0
        Dim interleave As Boolean = (gc(5) And &H20) <> 0
        Dim p0 As Byte() = planes(0), p1 As Byte() = planes(1), p2 As Byte() = planes(2), p3 As Byte() = planes(3)
        Dim mask As Integer = dacMask
        Dim x As Integer = line

        For col As Integer = 0 To hde - 1
            Dim addr As Integer = DisplayAddress(rowCounter + col, rowScan)

            If color256 Then
                ' One byte per pixel, four pixels (one per plane) per character clock, each two dots wide
                Dim w As Integer = 2 * dotScale
                Dim c As Integer
                c = dacColor(p0(addr) And mask) : For s As Integer = 1 To w : buf(x) = c : x += 1 : Next
                c = dacColor(p1(addr) And mask) : For s As Integer = 1 To w : buf(x) = c : x += 1 : Next
                c = dacColor(p2(addr) And mask) : For s As Integer = 1 To w : buf(x) = c : x += 1 : Next
                c = dacColor(p3(addr) And mask) : For s As Integer = 1 To w : buf(x) = c : x += 1 : Next

            ElseIf interleave Then
                ' CGA-compatible 2 bits per pixel: four pixels from plane 0, then four from plane 1
                Dim b0 As Integer = p0(addr), b1 As Integer = p1(addr)
                For i As Integer = 0 To 7
                    Dim src As Integer = If(i < 4, b0, b1)
                    Dim c As Integer = pal16((src >> (6 - 2 * (i And 3))) And 3)
                    For s As Integer = 1 To dotScale
                        buf(x) = c : x += 1
                    Next
                Next

            Else
                ' Planar: one bit per plane per pixel
                Dim b0 As Integer = p0(addr), b1 As Integer = p1(addr), b2 As Integer = p2(addr), b3 As Integer = p3(addr)
                For i As Integer = 7 To 0 Step -1
                    Dim idx As Integer = ((b0 >> i) And 1) Or (((b1 >> i) And 1) << 1) Or (((b2 >> i) And 1) << 2) Or (((b3 >> i) And 1) << 3)
                    Dim c As Integer = pal16(idx)
                    For s As Integer = 1 To dotScale
                        buf(x) = c : x += 1
                    Next
                Next
            End If
        Next
    End Sub
#End Region

    Public Overrides ReadOnly Property Name As String
        Get
            Return "VGA"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Video Graphics Array"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return 2
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 0
        End Get
    End Property
End Class

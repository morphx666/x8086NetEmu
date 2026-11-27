' Builds the 256-character, 8x16 text-mode font used by the renderers (one byte per pixel, 1 = foreground,
' character by character and row by row: 256 x 16 x 8 bytes).
'
'   00h-7Fh  Taken from the 8x8 graphics character set in the system BIOS ROM (F000:FA6E on IBM-compatible
'            BIOSes, e.g. the public-domain Turbo XT BIOS the emulator boots).
'   80h-FFh  Not present in the BIOS (on a real PC they live in the video card's character ROM), so they are
'            generated here: box drawing, shade and block characters are constructed geometrically at 8x16
'            (so lines connect between cells), accented letters are composed from the BIOS's own letters
'            plus accent marks, and the remaining symbols are original 8x8 designs.
'
' 8x8 glyphs are doubled vertically to 8x16, which is exactly how CGA text looks on a 640x400 surface.
Public NotInheritable Class TextFont
    Public Const CharWidth As Integer = 8
    Public Const CharHeight As Integer = 16

    Private Const BIOS_FONT_ADDRESS As Integer = &HFFA6E

    Private Sub New()
    End Sub

    Public Shared Function Build(cpu As X8086) As Byte()
        Dim glyphs(256 - 1)() As Byte ' 8x8, one byte per row, bit 7 = leftmost pixel
        For c As Integer = 0 To 127
            glyphs(c) = New Byte(8 - 1) {}
            For r As Integer = 0 To 7
                glyphs(c)(r) = cpu.Memory(BIOS_FONT_ADDRESS + c * 8 + r)
            Next
        Next
        BuildUpperHalf(glyphs)

        Dim font(256 * CharHeight * CharWidth - 1) As Byte
        For c As Integer = 0 To 255
            Dim cell As Boolean(,) = If(c >= &HB0 AndAlso c <= &HDF, DrawGraphicsChar(c), Double8x8(glyphs(c)))
            For y As Integer = 0 To CharHeight - 1
                For x As Integer = 0 To CharWidth - 1
                    font((c * CharHeight + y) * CharWidth + x) = If(cell(x, y), 1, 0)
                Next
            Next
        Next
        Return font
    End Function

    Private Shared Function Double8x8(glyph As Byte()) As Boolean(,)
        Dim cell(CharWidth - 1, CharHeight - 1) As Boolean
        If glyph Is Nothing Then Return cell
        For r As Integer = 0 To 7
            For x As Integer = 0 To 7
                Dim isSet As Boolean = (glyph(r) And (&H80 >> x)) <> 0
                cell(x, r * 2) = isSet
                cell(x, r * 2 + 1) = isSet
            Next
        Next
        Return cell
    End Function

#Region "80h-AFh and E0h-FFh"
    Private Enum Accent
        None
        Acute
        Grave
        Circumflex
        Diaeresis
        Ring
        Tilde
        Cedilla
    End Enum

    ' Two-row accent marks placed above lowercase letters (whose x-height starts at row 2)
    Private Shared ReadOnly lowerAccents As New Dictionary(Of Accent, String()) From {
        {Accent.Acute, {"....##..", "...##..."}},
        {Accent.Grave, {".##.....", "..##...."}},
        {Accent.Circumflex, {".####...", "##..##.."}},
        {Accent.Diaeresis, {"##..##..", "........"}},
        {Accent.Ring, {"..###...", "..#.#..."}},
        {Accent.Tilde, {".###.##.", "##.###.."}}
    }

    ' One-row accent marks for capitals (the capital is shifted down one row to make room)
    Private Shared ReadOnly upperAccents As New Dictionary(Of Accent, String) From {
        {Accent.Acute, "...##..."},
        {Accent.Diaeresis, "##..##.."},
        {Accent.Ring, "..##...."},
        {Accent.Tilde, ".#####.."}
    }

    ' 80h-AFh: (base character, accent) for composed letters; Nothing for hand-drawn symbols
    Private Shared ReadOnly composed As New Dictionary(Of Integer, (Base As Char, Mark As Accent)) From {
        {&H80, ("C"c, Accent.Cedilla)}, {&H81, ("u"c, Accent.Diaeresis)}, {&H82, ("e"c, Accent.Acute)},
        {&H83, ("a"c, Accent.Circumflex)}, {&H84, ("a"c, Accent.Diaeresis)}, {&H85, ("a"c, Accent.Grave)},
        {&H86, ("a"c, Accent.Ring)}, {&H87, ("c"c, Accent.Cedilla)}, {&H88, ("e"c, Accent.Circumflex)},
        {&H89, ("e"c, Accent.Diaeresis)}, {&H8A, ("e"c, Accent.Grave)}, {&H8B, ("ı"c, Accent.Diaeresis)},
        {&H8C, ("ı"c, Accent.Circumflex)}, {&H8D, ("ı"c, Accent.Grave)}, {&H8E, ("A"c, Accent.Diaeresis)},
        {&H8F, ("A"c, Accent.Ring)}, {&H90, ("E"c, Accent.Acute)}, {&H93, ("o"c, Accent.Circumflex)},
        {&H94, ("o"c, Accent.Diaeresis)}, {&H95, ("o"c, Accent.Grave)}, {&H96, ("u"c, Accent.Circumflex)},
        {&H97, ("u"c, Accent.Grave)}, {&H98, ("y"c, Accent.Diaeresis)}, {&H99, ("O"c, Accent.Diaeresis)},
        {&H9A, ("U"c, Accent.Diaeresis)}, {&HA0, ("a"c, Accent.Acute)}, {&HA1, ("ı"c, Accent.Acute)},
        {&HA2, ("o"c, Accent.Acute)}, {&HA3, ("u"c, Accent.Acute)}, {&HA4, ("n"c, Accent.Tilde)},
        {&HA5, ("N"c, Accent.Tilde)}
    }

    ' Original 8x8 designs, rows separated by "|"
    Private Shared ReadOnly drawn As New Dictionary(Of Integer, String) From {
        {&H91, "........|........|.###.##.|...##..#|.#######|#..##...|.###.###|........"}, ' æ
        {&H92, "..######|.##.##..|##..##..|#######.|##..##..|##..##..|##..####|........"}, ' Æ
        {&H9B, "...##...|...##...|.######.|##......|##......|.######.|...##...|...##..."}, ' ¢
        {&H9C, "..###...|.##.##..|.##..#..|####....|.##.....|###..##.|######..|........"}, ' £
        {&H9D, "##..##..|##..##..|.####...|######..|..##....|######..|..##....|..##...."}, ' ¥
        {&H9E, "####....|##.##...|##.##.#.|####.###|##....#.|##....#.|##.....#|........"}, ' ₧
        {&H9F, "...###..|..##.##.|..##....|.####...|..##....|..##....|##.##...|.###...."}, ' ƒ
        {&HA6, ".####...|....##..|.#####..|##..##..|.#####..|........|######..|........"}, ' ª
        {&HA7, ".####...|##..##..|##..##..|##..##..|.####...|........|######..|........"}, ' º
        {&HA9, "........|........|........|######..|##......|##......|........|........"}, ' ⌐
        {&HAA, "........|........|........|######..|....##..|....##..|........|........"}, ' ¬
        {&HAB, "##...##.|##..##..|##.##...|..##.##.|.##...##|##...##.|#...##..|....####"}, ' ½
        {&HAC, "##...##.|##..##..|##.##...|..##.##.|.##.###.|##.#.##.|#..####.|.....##."}, ' ¼
        {&HAE, "........|..##..##|.##..##.|##..##..|.##..##.|..##..##|........|........"}, ' «
        {&HAF, "........|##..##..|.##..##.|..##..##|.##..##.|##..##..|........|........"}, ' »
        {&HE0, "........|........|.###.##.|##.###..|##..#...|##.###..|.###.##.|........"}, ' α
        {&HE1, "........|.####...|##..##..|#####...|##..##..|#####...|##......|##......"}, ' ß
        {&HE2, "######..|##..##..|##......|##......|##......|##......|##......|........"}, ' Γ
        {&HE3, "........|#######.|.##.##..|.##.##..|.##.##..|.##.##..|.##.##..|........"}, ' π
        {&HE4, "######..|##..##..|.##.....|..##....|.##.....|##..##..|######..|........"}, ' Σ
        {&HE5, "........|........|.######.|##.##...|##.##...|##.##...|.###....|........"}, ' σ
        {&HE6, "........|.##..##.|.##..##.|.##..##.|.#####..|.##.....|##......|........"}, ' µ
        {&HE7, "........|.###.##.|##.###..|...##...|...##...|...##...|...##...|........"}, ' τ
        {&HE8, "######..|..##....|.####...|##..##..|.####...|..##....|######..|........"}, ' Φ
        {&HE9, "..###...|.##.##..|##...##.|#######.|##...##.|.##.##..|..###...|........"}, ' Θ
        {&HEA, "..###...|.##.##..|##...##.|##...##.|.##.##..|.##.##..|###.###.|........"}, ' Ω
        {&HEB, "...###..|..##....|...##...|.#####..|##..##..|##..##..|.####...|........"}, ' δ
        {&HEC, "........|........|.######.|##.##.##|##.##.##|.######.|........|........"}, ' ∞
        {&HED, ".....##.|....##..|.######.|##.##.##|##.##.##|.######.|.##.....|##......"}, ' φ
        {&HEE, "..###...|.##.....|##......|#####...|##......|.##.....|..###...|........"}, ' ε
        {&HEF, ".####...|##..##..|##..##..|##..##..|##..##..|##..##..|##..##..|........"}, ' ∩
        {&HF0, "........|######..|........|######..|........|######..|........|........"}, ' ≡
        {&HF1, "..##....|..##....|######..|..##....|..##....|........|######..|........"}, ' ±
        {&HF2, ".##.....|..##....|...##...|..##....|.##.....|........|######..|........"}, ' ≥
        {&HF3, "...##...|..##....|.##.....|..##....|...##...|........|######..|........"}, ' ≤
        {&HF4, "....###.|...##.##|...##.##|...##...|...##...|...##...|...##...|...##..."}, ' ⌠
        {&HF5, "...##...|...##...|...##...|...##...|...##...|##.##...|##.##...|.###...."}, ' ⌡
        {&HF6, "........|..##....|........|######..|........|..##....|........|........"}, ' ÷
        {&HF7, "........|.###.##.|##.###..|........|.###.##.|##.###..|........|........"}, ' ≈
        {&HF8, "..###...|.##.##..|.##.##..|..###...|........|........|........|........"}, ' °
        {&HF9, "........|........|........|...##...|...##...|........|........|........"}, ' ∙
        {&HFA, "........|........|........|........|...#....|........|........|........"}, ' ·
        {&HFB, "....####|....##..|....##..|....##..|###.##..|.##.##..|..####..|...###.."}, ' √
        {&HFC, "####....|##.##...|##.##...|##.##...|........|........|........|........"}, ' ⁿ
        {&HFD, ".###....|...##...|..##....|.##.....|####....|........|........|........"}, ' ²
        {&HFE, "........|........|..####..|..####..|..####..|..####..|........|........"}, ' ■
        {&HFF, "........|........|........|........|........|........|........|........"}  ' non-breaking space
    }

    Private Shared Sub BuildUpperHalf(glyphs As Byte()())
        For c As Integer = &H80 To &HFF
            If drawn.ContainsKey(c) Then
                glyphs(c) = ParseRows(drawn(c).Split("|"c))
            ElseIf composed.ContainsKey(c) Then
                glyphs(c) = Compose(glyphs, composed(c).Base, composed(c).Mark)
            End If
        Next

        ' ¿ and ¡ are the BIOS's ? and ! turned upside down
        glyphs(&HA8) = Rotate180(glyphs(AscW("?"c)))
        glyphs(&HAD) = Rotate180(glyphs(AscW("!"c)))
    End Sub

    Private Shared Function Compose(glyphs As Byte()(), base As Char, mark As Accent) As Byte()
        Dim isDotlessI As Boolean = base = "ı"c
        Dim source As Byte() = glyphs(If(isDotlessI, AscW("i"c), AscW(base)))
        Dim g As Byte() = CType(source.Clone(), Byte())

        If isDotlessI Then
            g(0) = 0 ' Remove the i's dot
            g(1) = 0
        End If

        If mark = Accent.Cedilla Then
            g(7) = ParseRow("...##...")
            Return g
        End If

        If Char.IsUpper(base) Then
            ' Shift the capital down one row and put a one-row accent on top
            For r As Integer = 7 To 1 Step -1
                g(r) = g(r - 1)
            Next
            g(0) = ParseRow(upperAccents(mark))
        Else
            Dim rows As String() = lowerAccents(mark)
            g(0) = ParseRow(rows(0))
            g(1) = ParseRow(rows(1))
        End If
        Return g
    End Function

    ' Rotates a glyph 180 degrees within its own bounding box
    Private Shared Function Rotate180(source As Byte()) As Byte()
        Dim minX As Integer = 8, maxX As Integer = -1, minY As Integer = 8, maxY As Integer = -1
        For y As Integer = 0 To 7
            For x As Integer = 0 To 7
                If (source(y) And (&H80 >> x)) <> 0 Then
                    minX = Math.Min(minX, x) : maxX = Math.Max(maxX, x)
                    minY = Math.Min(minY, y) : maxY = Math.Max(maxY, y)
                End If
            Next
        Next
        Dim g(8 - 1) As Byte
        If maxX < 0 Then Return g
        For y As Integer = minY To maxY
            For x As Integer = minX To maxX
                If (source(y) And (&H80 >> x)) <> 0 Then
                    Dim nx As Integer = minX + maxX - x
                    Dim ny As Integer = minY + maxY - y
                    g(ny) = g(ny) Or CByte(&H80 >> nx)
                End If
            Next
        Next
        Return g
    End Function

    Private Shared Function ParseRows(rows As String()) As Byte()
        Dim g(8 - 1) As Byte
        For r As Integer = 0 To 7
            g(r) = ParseRow(rows(r))
        Next
        Return g
    End Function

    Private Shared Function ParseRow(row As String) As Byte
        Dim b As Integer = 0
        For x As Integer = 0 To 7
            If row(x) = "#"c Then b = b Or (&H80 >> x)
        Next
        Return CByte(b)
    End Function
#End Region

#Region "B0h-DFh: shades, box drawing and blocks (drawn at 8x16)"
    ' Line positions: single vertical at x=3, double at x=2 and x=4; single horizontal at y=7, double at y=6 and y=8.
    ' Each segment is "v<x>,<y1>,<y2>" or "h<y>,<x1>,<x2>".
    Private Shared ReadOnly boxChars As New Dictionary(Of Integer, String) From {
        {&HB3, "v3,0,15"},                                              ' │
        {&HB4, "v3,0,15;h7,0,3"},                                       ' ┤
        {&HB5, "v3,0,15;h6,0,3;h8,0,3"},                                ' ╡
        {&HB6, "v2,0,15;v4,0,15;h7,0,2"},                               ' ╢
        {&HB7, "v2,7,15;v4,7,15;h7,0,4"},                               ' ╖
        {&HB8, "v3,6,15;h6,0,3;h8,0,3"},                                ' ╕
        {&HB9, "v4,0,15;v2,0,6;v2,8,15;h6,0,2;h8,0,2"},                 ' ╣
        {&HBA, "v2,0,15;v4,0,15"},                                      ' ║
        {&HBB, "h6,0,4;v4,6,15;h8,0,2;v2,8,15"},                        ' ╗
        {&HBC, "h8,0,4;v4,0,8;h6,0,2;v2,0,6"},                          ' ╝
        {&HBD, "v2,0,7;v4,0,7;h7,0,4"},                                 ' ╜
        {&HBE, "v3,0,8;h6,0,3;h8,0,3"},                                 ' ╛
        {&HBF, "h7,0,3;v3,7,15"},                                       ' ┐
        {&HC0, "v3,0,7;h7,3,7"},                                        ' └
        {&HC1, "v3,0,7;h7,0,7"},                                        ' ┴
        {&HC2, "h7,0,7;v3,7,15"},                                       ' ┬
        {&HC3, "v3,0,15;h7,3,7"},                                       ' ├
        {&HC4, "h7,0,7"},                                               ' ─
        {&HC5, "v3,0,15;h7,0,7"},                                       ' ┼
        {&HC6, "v3,0,15;h6,3,7;h8,3,7"},                                ' ╞
        {&HC7, "v2,0,15;v4,0,15;h7,4,7"},                               ' ╟
        {&HC8, "v2,0,8;h8,2,7;v4,0,6;h6,4,7"},                          ' ╚
        {&HC9, "h6,2,7;v2,6,15;h8,4,7;v4,8,15"},                        ' ╔
        {&HCA, "h8,0,7;h6,0,2;h6,4,7;v2,0,6;v4,0,6"},                   ' ╩
        {&HCB, "h6,0,7;h8,0,2;h8,4,7;v2,8,15;v4,8,15"},                 ' ╦
        {&HCC, "v2,0,15;v4,0,6;v4,8,15;h6,4,7;h8,4,7"},                 ' ╠
        {&HCD, "h6,0,7;h8,0,7"},                                        ' ═
        {&HCE, "h6,0,2;h6,4,7;h8,0,2;h8,4,7;v2,0,6;v2,8,15;v4,0,6;v4,8,15"}, ' ╬
        {&HCF, "h6,0,7;h8,0,7;v3,0,6"},                                 ' ╧
        {&HD0, "h7,0,7;v2,0,7;v4,0,7"},                                 ' ╨
        {&HD1, "h6,0,7;h8,0,7;v3,8,15"},                                ' ╤
        {&HD2, "h7,0,7;v2,7,15;v4,7,15"},                               ' ╥
        {&HD3, "v2,0,7;v4,0,7;h7,2,7"},                                 ' ╙
        {&HD4, "v3,0,8;h6,3,7;h8,3,7"},                                 ' ╘
        {&HD5, "v3,6,15;h6,3,7;h8,3,7"},                                ' ╒
        {&HD6, "v2,7,15;v4,7,15;h7,2,7"},                               ' ╓
        {&HD7, "v2,0,15;v4,0,15;h7,0,7"},                               ' ╫
        {&HD8, "v3,0,15;h6,0,7;h8,0,7"},                                ' ╪
        {&HD9, "v3,0,7;h7,0,3"},                                        ' ┘
        {&HDA, "v3,7,15;h7,3,7"}                                        ' ┌
    }

    Private Shared Function DrawGraphicsChar(c As Integer) As Boolean(,)
        Dim cell(CharWidth - 1, CharHeight - 1) As Boolean

        For y As Integer = 0 To CharHeight - 1
            For x As Integer = 0 To CharWidth - 1
                Select Case c
                    Case &HB0 : cell(x, y) = (x Mod 4) = (y Mod 2) * 2       ' ░ 25%
                    Case &HB1 : cell(x, y) = ((x + y) Mod 2) = 0             ' ▒ 50%
                    Case &HB2 : cell(x, y) = (x Mod 4) <> ((y Mod 2) * 2)    ' ▓ 75%
                    Case &HDB : cell(x, y) = True                            ' █
                    Case &HDC : cell(x, y) = y >= CharHeight \ 2             ' ▄
                    Case &HDD : cell(x, y) = x < CharWidth \ 2               ' ▌
                    Case &HDE : cell(x, y) = x >= CharWidth \ 2              ' ▐
                    Case &HDF : cell(x, y) = y < CharHeight \ 2              ' ▀
                End Select
            Next
        Next

        If boxChars.ContainsKey(c) Then
            For Each segment As String In boxChars(c).Split(";"c)
                Dim p As String() = segment.Substring(1).Split(","c)
                Dim at As Integer = CInt(p(0)), from As Integer = CInt(p(1)), [to] As Integer = CInt(p(2))
                For i As Integer = from To [to]
                    If segment(0) = "v"c Then cell(at, i) = True Else cell(i, at) = True
                Next
            Next
        End If

        Return cell
    End Function
#End Region
End Class

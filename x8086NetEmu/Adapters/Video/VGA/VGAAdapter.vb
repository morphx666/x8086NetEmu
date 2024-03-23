' http://www.osdever.net/FreeVGA/home.htm
' https://pdos.csail.mit.edu/6.828/2007/readings/hardware/vgadoc/VGABIOS.TXT
' http://stanislavs.org/helppc/ports.html

Imports System.Threading
Imports System.Threading.Tasks

Public MustInherit Class VGAAdapter
    Inherits CGAAdapter

    Private ReadOnly VGABasePalette() As Color = {
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, &HAA),
        Color.FromArgb(0, &HAA, 0),
        Color.FromArgb(0, &HAA, &HAA),
        Color.FromArgb(&HAA, 0, 0),
        Color.FromArgb(&HAA, 0, &HAA),
        Color.FromArgb(&HAA, &H55, 0),
        Color.FromArgb(&HAA, &HAA, &HAA),
        Color.FromArgb(&H55, &H55, &H55),
        Color.FromArgb(&H55, &H55, &HFF),
        Color.FromArgb(&H55, &HFF, &H55),
        Color.FromArgb(&H55, &HFF, &HFF),
        Color.FromArgb(&HFF, &H55, &H55),
        Color.FromArgb(&HFF, &H55, &HFF),
        Color.FromArgb(&HFF, &HFF, &H55),
        Color.FromArgb(&HFF, &HFF, &HFF),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 169),
        Color.FromArgb(0, 169, 0),
        Color.FromArgb(0, 169, 169),
        Color.FromArgb(169, 0, 0),
        Color.FromArgb(169, 0, 169),
        Color.FromArgb(169, 169, 0),
        Color.FromArgb(169, 169, 169),
        Color.FromArgb(0, 0, 84),
        Color.FromArgb(0, 0, 255),
        Color.FromArgb(0, 169, 84),
        Color.FromArgb(0, 169, 255),
        Color.FromArgb(169, 0, 84),
        Color.FromArgb(169, 0, 255),
        Color.FromArgb(169, 169, 84),
        Color.FromArgb(169, 169, 255),
        Color.FromArgb(0, 84, 0),
        Color.FromArgb(0, 84, 169),
        Color.FromArgb(0, 255, 0),
        Color.FromArgb(0, 255, 169),
        Color.FromArgb(169, 84, 0),
        Color.FromArgb(169, 84, 169),
        Color.FromArgb(169, 255, 0),
        Color.FromArgb(169, 255, 169),
        Color.FromArgb(0, 84, 84),
        Color.FromArgb(0, 84, 255),
        Color.FromArgb(0, 255, 84),
        Color.FromArgb(0, 255, 255),
        Color.FromArgb(169, 84, 84),
        Color.FromArgb(169, 84, 255),
        Color.FromArgb(169, 255, 84),
        Color.FromArgb(169, 255, 255),
        Color.FromArgb(84, 0, 0),
        Color.FromArgb(84, 0, 169),
        Color.FromArgb(84, 169, 0),
        Color.FromArgb(84, 169, 169),
        Color.FromArgb(255, 0, 0),
        Color.FromArgb(255, 0, 169),
        Color.FromArgb(255, 169, 0),
        Color.FromArgb(255, 169, 169),
        Color.FromArgb(84, 0, 84),
        Color.FromArgb(84, 0, 255),
        Color.FromArgb(84, 169, 84),
        Color.FromArgb(84, 169, 255),
        Color.FromArgb(255, 0, 84),
        Color.FromArgb(255, 0, 255),
        Color.FromArgb(255, 169, 84),
        Color.FromArgb(255, 169, 255),
        Color.FromArgb(84, 84, 0),
        Color.FromArgb(84, 84, 169),
        Color.FromArgb(84, 255, 0),
        Color.FromArgb(84, 255, 169),
        Color.FromArgb(255, 84, 0),
        Color.FromArgb(255, 84, 169),
        Color.FromArgb(255, 255, 0),
        Color.FromArgb(255, 255, 169),
        Color.FromArgb(84, 84, 84),
        Color.FromArgb(84, 84, 255),
        Color.FromArgb(84, 255, 84),
        Color.FromArgb(84, 255, 255),
        Color.FromArgb(255, 84, 84),
        Color.FromArgb(255, 84, 255),
        Color.FromArgb(255, 255, 84),
        Color.FromArgb(255, 255, 255),
        Color.FromArgb(255, 125, 125),
        Color.FromArgb(255, 157, 125),
        Color.FromArgb(255, 190, 125),
        Color.FromArgb(255, 222, 125),
        Color.FromArgb(255, 255, 125),
        Color.FromArgb(222, 255, 125),
        Color.FromArgb(190, 255, 125),
        Color.FromArgb(157, 255, 125),
        Color.FromArgb(125, 255, 125),
        Color.FromArgb(125, 255, 157),
        Color.FromArgb(125, 255, 190),
        Color.FromArgb(125, 255, 222),
        Color.FromArgb(125, 255, 255),
        Color.FromArgb(125, 222, 255),
        Color.FromArgb(125, 190, 255),
        Color.FromArgb(125, 157, 255),
        Color.FromArgb(182, 182, 255),
        Color.FromArgb(198, 182, 255),
        Color.FromArgb(218, 182, 255),
        Color.FromArgb(234, 182, 255),
        Color.FromArgb(255, 182, 255),
        Color.FromArgb(255, 182, 234),
        Color.FromArgb(255, 182, 218),
        Color.FromArgb(255, 182, 198),
        Color.FromArgb(255, 182, 182),
        Color.FromArgb(255, 198, 182),
        Color.FromArgb(255, 218, 182),
        Color.FromArgb(255, 234, 182),
        Color.FromArgb(255, 255, 182),
        Color.FromArgb(234, 255, 182),
        Color.FromArgb(218, 255, 182),
        Color.FromArgb(198, 255, 182),
        Color.FromArgb(182, 255, 182),
        Color.FromArgb(182, 255, 198),
        Color.FromArgb(182, 255, 218),
        Color.FromArgb(182, 255, 234),
        Color.FromArgb(182, 255, 255),
        Color.FromArgb(182, 234, 255),
        Color.FromArgb(182, 218, 255),
        Color.FromArgb(182, 198, 255),
        Color.FromArgb(0, 0, 113),
        Color.FromArgb(28, 0, 113),
        Color.FromArgb(56, 0, 113),
        Color.FromArgb(84, 0, 113),
        Color.FromArgb(113, 0, 113),
        Color.FromArgb(113, 0, 84),
        Color.FromArgb(113, 0, 56),
        Color.FromArgb(113, 0, 28),
        Color.FromArgb(113, 0, 0),
        Color.FromArgb(113, 28, 0),
        Color.FromArgb(113, 56, 0),
        Color.FromArgb(113, 84, 0),
        Color.FromArgb(113, 113, 0),
        Color.FromArgb(84, 113, 0),
        Color.FromArgb(56, 113, 0),
        Color.FromArgb(28, 113, 0),
        Color.FromArgb(0, 113, 0),
        Color.FromArgb(0, 113, 28),
        Color.FromArgb(0, 113, 56),
        Color.FromArgb(0, 113, 84),
        Color.FromArgb(0, 113, 113),
        Color.FromArgb(0, 84, 113),
        Color.FromArgb(0, 56, 113),
        Color.FromArgb(0, 28, 113),
        Color.FromArgb(56, 56, 113),
        Color.FromArgb(68, 56, 113),
        Color.FromArgb(84, 56, 113),
        Color.FromArgb(97, 56, 113),
        Color.FromArgb(113, 56, 113),
        Color.FromArgb(113, 56, 97),
        Color.FromArgb(113, 56, 84),
        Color.FromArgb(113, 56, 68),
        Color.FromArgb(113, 56, 56),
        Color.FromArgb(113, 68, 56),
        Color.FromArgb(113, 84, 56),
        Color.FromArgb(113, 97, 56),
        Color.FromArgb(113, 113, 56),
        Color.FromArgb(97, 113, 56),
        Color.FromArgb(84, 113, 56),
        Color.FromArgb(68, 113, 56),
        Color.FromArgb(56, 113, 56),
        Color.FromArgb(56, 113, 68),
        Color.FromArgb(56, 113, 84),
        Color.FromArgb(56, 113, 97),
        Color.FromArgb(56, 113, 113),
        Color.FromArgb(56, 97, 113),
        Color.FromArgb(56, 84, 113),
        Color.FromArgb(56, 68, 113),
        Color.FromArgb(80, 80, 113),
        Color.FromArgb(89, 80, 113),
        Color.FromArgb(97, 80, 113),
        Color.FromArgb(105, 80, 113),
        Color.FromArgb(113, 80, 113),
        Color.FromArgb(113, 80, 105),
        Color.FromArgb(113, 80, 97),
        Color.FromArgb(113, 80, 89),
        Color.FromArgb(113, 80, 80),
        Color.FromArgb(113, 89, 80),
        Color.FromArgb(113, 97, 80),
        Color.FromArgb(113, 105, 80),
        Color.FromArgb(113, 113, 80),
        Color.FromArgb(105, 113, 80),
        Color.FromArgb(97, 113, 80),
        Color.FromArgb(89, 113, 80),
        Color.FromArgb(80, 113, 80),
        Color.FromArgb(80, 113, 89),
        Color.FromArgb(80, 113, 97),
        Color.FromArgb(80, 113, 105),
        Color.FromArgb(80, 113, 113),
        Color.FromArgb(80, 105, 113),
        Color.FromArgb(80, 97, 113),
        Color.FromArgb(80, 89, 113),
        Color.FromArgb(0, 0, 64),
        Color.FromArgb(16, 0, 64),
        Color.FromArgb(32, 0, 64),
        Color.FromArgb(48, 0, 64),
        Color.FromArgb(64, 0, 64),
        Color.FromArgb(64, 0, 48),
        Color.FromArgb(64, 0, 32),
        Color.FromArgb(64, 0, 16),
        Color.FromArgb(64, 0, 0),
        Color.FromArgb(64, 16, 0),
        Color.FromArgb(64, 32, 0),
        Color.FromArgb(64, 48, 0),
        Color.FromArgb(64, 64, 0),
        Color.FromArgb(48, 64, 0),
        Color.FromArgb(32, 64, 0),
        Color.FromArgb(16, 64, 0),
        Color.FromArgb(0, 64, 0),
        Color.FromArgb(0, 64, 16),
        Color.FromArgb(0, 64, 32),
        Color.FromArgb(0, 64, 48),
        Color.FromArgb(0, 64, 64),
        Color.FromArgb(0, 48, 64),
        Color.FromArgb(0, 32, 64),
        Color.FromArgb(0, 16, 64),
        Color.FromArgb(32, 32, 64),
        Color.FromArgb(40, 32, 64),
        Color.FromArgb(48, 32, 64),
        Color.FromArgb(56, 32, 64),
        Color.FromArgb(64, 32, 64),
        Color.FromArgb(64, 32, 56),
        Color.FromArgb(64, 32, 48),
        Color.FromArgb(64, 32, 40),
        Color.FromArgb(64, 32, 32),
        Color.FromArgb(64, 40, 32),
        Color.FromArgb(64, 48, 32),
        Color.FromArgb(64, 56, 32),
        Color.FromArgb(64, 64, 32),
        Color.FromArgb(56, 64, 32),
        Color.FromArgb(48, 64, 32),
        Color.FromArgb(40, 64, 32),
        Color.FromArgb(32, 64, 32),
        Color.FromArgb(32, 64, 40),
        Color.FromArgb(32, 64, 48),
        Color.FromArgb(32, 64, 56),
        Color.FromArgb(32, 64, 64),
        Color.FromArgb(32, 56, 64),
        Color.FromArgb(32, 48, 64),
        Color.FromArgb(32, 40, 64),
        Color.FromArgb(44, 44, 64),
        Color.FromArgb(48, 44, 64),
        Color.FromArgb(52, 44, 64),
        Color.FromArgb(60, 44, 64),
        Color.FromArgb(64, 44, 64),
        Color.FromArgb(64, 44, 60),
        Color.FromArgb(64, 44, 52),
        Color.FromArgb(64, 44, 48),
        Color.FromArgb(64, 44, 44),
        Color.FromArgb(64, 48, 44),
        Color.FromArgb(64, 52, 44),
        Color.FromArgb(64, 60, 44),
        Color.FromArgb(64, 64, 44),
        Color.FromArgb(60, 64, 44),
        Color.FromArgb(52, 64, 44),
        Color.FromArgb(48, 64, 44),
        Color.FromArgb(44, 64, 44),
        Color.FromArgb(44, 64, 48),
        Color.FromArgb(44, 64, 52),
        Color.FromArgb(44, 64, 60),
        Color.FromArgb(44, 64, 64),
        Color.FromArgb(44, 60, 64),
        Color.FromArgb(44, 52, 64),
        Color.FromArgb(44, 48, 64),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0)
        }

    Private ReadOnly VGABasePalette2() As Color = {
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 169),
        Color.FromArgb(0, 169, 0),
        Color.FromArgb(0, 169, 169),
        Color.FromArgb(169, 0, 0),
        Color.FromArgb(169, 0, 169),
        Color.FromArgb(169, 169, 0),
        Color.FromArgb(169, 169, 169),
        Color.FromArgb(0, 0, 84),
        Color.FromArgb(0, 0, 255),
        Color.FromArgb(0, 169, 84),
        Color.FromArgb(0, 169, 255),
        Color.FromArgb(169, 0, 84),
        Color.FromArgb(169, 0, 255),
        Color.FromArgb(169, 169, 84),
        Color.FromArgb(169, 169, 255),
        Color.FromArgb(0, 84, 0),
        Color.FromArgb(0, 84, 169),
        Color.FromArgb(0, 255, 0),
        Color.FromArgb(0, 255, 169),
        Color.FromArgb(169, 84, 0),
        Color.FromArgb(169, 84, 169),
        Color.FromArgb(169, 255, 0),
        Color.FromArgb(169, 255, 169),
        Color.FromArgb(0, 84, 84),
        Color.FromArgb(0, 84, 255),
        Color.FromArgb(0, 255, 84),
        Color.FromArgb(0, 255, 255),
        Color.FromArgb(169, 84, 84),
        Color.FromArgb(169, 84, 255),
        Color.FromArgb(169, 255, 84),
        Color.FromArgb(169, 255, 255),
        Color.FromArgb(84, 0, 0),
        Color.FromArgb(84, 0, 169),
        Color.FromArgb(84, 169, 0),
        Color.FromArgb(84, 169, 169),
        Color.FromArgb(255, 0, 0),
        Color.FromArgb(255, 0, 169),
        Color.FromArgb(255, 169, 0),
        Color.FromArgb(255, 169, 169),
        Color.FromArgb(84, 0, 84),
        Color.FromArgb(84, 0, 255),
        Color.FromArgb(84, 169, 84),
        Color.FromArgb(84, 169, 255),
        Color.FromArgb(255, 0, 84),
        Color.FromArgb(255, 0, 255),
        Color.FromArgb(255, 169, 84),
        Color.FromArgb(255, 169, 255),
        Color.FromArgb(84, 84, 0),
        Color.FromArgb(84, 84, 169),
        Color.FromArgb(84, 255, 0),
        Color.FromArgb(84, 255, 169),
        Color.FromArgb(255, 84, 0),
        Color.FromArgb(255, 84, 169),
        Color.FromArgb(255, 255, 0),
        Color.FromArgb(255, 255, 169),
        Color.FromArgb(84, 84, 84),
        Color.FromArgb(84, 84, 255),
        Color.FromArgb(84, 255, 84),
        Color.FromArgb(84, 255, 255),
        Color.FromArgb(255, 84, 84),
        Color.FromArgb(255, 84, 255),
        Color.FromArgb(255, 255, 84),
        Color.FromArgb(255, 255, 255),
        Color.FromArgb(255, 125, 125),
        Color.FromArgb(255, 157, 125),
        Color.FromArgb(255, 190, 125),
        Color.FromArgb(255, 222, 125),
        Color.FromArgb(255, 255, 125),
        Color.FromArgb(222, 255, 125),
        Color.FromArgb(190, 255, 125),
        Color.FromArgb(157, 255, 125),
        Color.FromArgb(125, 255, 125),
        Color.FromArgb(125, 255, 157),
        Color.FromArgb(125, 255, 190),
        Color.FromArgb(125, 255, 222),
        Color.FromArgb(125, 255, 255),
        Color.FromArgb(125, 222, 255),
        Color.FromArgb(125, 190, 255),
        Color.FromArgb(125, 157, 255),
        Color.FromArgb(182, 182, 255),
        Color.FromArgb(198, 182, 255),
        Color.FromArgb(218, 182, 255),
        Color.FromArgb(234, 182, 255),
        Color.FromArgb(255, 182, 255),
        Color.FromArgb(255, 182, 234),
        Color.FromArgb(255, 182, 218),
        Color.FromArgb(255, 182, 198),
        Color.FromArgb(255, 182, 182),
        Color.FromArgb(255, 198, 182),
        Color.FromArgb(255, 218, 182),
        Color.FromArgb(255, 234, 182),
        Color.FromArgb(255, 255, 182),
        Color.FromArgb(234, 255, 182),
        Color.FromArgb(218, 255, 182),
        Color.FromArgb(198, 255, 182),
        Color.FromArgb(182, 255, 182),
        Color.FromArgb(182, 255, 198),
        Color.FromArgb(182, 255, 218),
        Color.FromArgb(182, 255, 234),
        Color.FromArgb(182, 255, 255),
        Color.FromArgb(182, 234, 255),
        Color.FromArgb(182, 218, 255),
        Color.FromArgb(182, 198, 255),
        Color.FromArgb(0, 0, 113),
        Color.FromArgb(28, 0, 113),
        Color.FromArgb(56, 0, 113),
        Color.FromArgb(84, 0, 113),
        Color.FromArgb(113, 0, 113),
        Color.FromArgb(113, 0, 84),
        Color.FromArgb(113, 0, 56),
        Color.FromArgb(113, 0, 28),
        Color.FromArgb(113, 0, 0),
        Color.FromArgb(113, 28, 0),
        Color.FromArgb(113, 56, 0),
        Color.FromArgb(113, 84, 0),
        Color.FromArgb(113, 113, 0),
        Color.FromArgb(84, 113, 0),
        Color.FromArgb(56, 113, 0),
        Color.FromArgb(28, 113, 0),
        Color.FromArgb(0, 113, 0),
        Color.FromArgb(0, 113, 28),
        Color.FromArgb(0, 113, 56),
        Color.FromArgb(0, 113, 84),
        Color.FromArgb(0, 113, 113),
        Color.FromArgb(0, 84, 113),
        Color.FromArgb(0, 56, 113),
        Color.FromArgb(0, 28, 113),
        Color.FromArgb(56, 56, 113),
        Color.FromArgb(68, 56, 113),
        Color.FromArgb(84, 56, 113),
        Color.FromArgb(97, 56, 113),
        Color.FromArgb(113, 56, 113),
        Color.FromArgb(113, 56, 97),
        Color.FromArgb(113, 56, 84),
        Color.FromArgb(113, 56, 68),
        Color.FromArgb(113, 56, 56),
        Color.FromArgb(113, 68, 56),
        Color.FromArgb(113, 84, 56),
        Color.FromArgb(113, 97, 56),
        Color.FromArgb(113, 113, 56),
        Color.FromArgb(97, 113, 56),
        Color.FromArgb(84, 113, 56),
        Color.FromArgb(68, 113, 56),
        Color.FromArgb(56, 113, 56),
        Color.FromArgb(56, 113, 68),
        Color.FromArgb(56, 113, 84),
        Color.FromArgb(56, 113, 97),
        Color.FromArgb(56, 113, 113),
        Color.FromArgb(56, 97, 113),
        Color.FromArgb(56, 84, 113),
        Color.FromArgb(56, 68, 113),
        Color.FromArgb(80, 80, 113),
        Color.FromArgb(89, 80, 113),
        Color.FromArgb(97, 80, 113),
        Color.FromArgb(105, 80, 113),
        Color.FromArgb(113, 80, 113),
        Color.FromArgb(113, 80, 105),
        Color.FromArgb(113, 80, 97),
        Color.FromArgb(113, 80, 89),
        Color.FromArgb(113, 80, 80),
        Color.FromArgb(113, 89, 80),
        Color.FromArgb(113, 97, 80),
        Color.FromArgb(113, 105, 80),
        Color.FromArgb(113, 113, 80),
        Color.FromArgb(105, 113, 80),
        Color.FromArgb(97, 113, 80),
        Color.FromArgb(89, 113, 80),
        Color.FromArgb(80, 113, 80),
        Color.FromArgb(80, 113, 89),
        Color.FromArgb(80, 113, 97),
        Color.FromArgb(80, 113, 105),
        Color.FromArgb(80, 113, 113),
        Color.FromArgb(80, 105, 113),
        Color.FromArgb(80, 97, 113),
        Color.FromArgb(80, 89, 113),
        Color.FromArgb(0, 0, 64),
        Color.FromArgb(16, 0, 64),
        Color.FromArgb(32, 0, 64),
        Color.FromArgb(48, 0, 64),
        Color.FromArgb(64, 0, 64),
        Color.FromArgb(64, 0, 48),
        Color.FromArgb(64, 0, 32),
        Color.FromArgb(64, 0, 16),
        Color.FromArgb(64, 0, 0),
        Color.FromArgb(64, 16, 0),
        Color.FromArgb(64, 32, 0),
        Color.FromArgb(64, 48, 0),
        Color.FromArgb(64, 64, 0),
        Color.FromArgb(48, 64, 0),
        Color.FromArgb(32, 64, 0),
        Color.FromArgb(16, 64, 0),
        Color.FromArgb(0, 64, 0),
        Color.FromArgb(0, 64, 16),
        Color.FromArgb(0, 64, 32),
        Color.FromArgb(0, 64, 48),
        Color.FromArgb(0, 64, 64),
        Color.FromArgb(0, 48, 64),
        Color.FromArgb(0, 32, 64),
        Color.FromArgb(0, 16, 64),
        Color.FromArgb(32, 32, 64),
        Color.FromArgb(40, 32, 64),
        Color.FromArgb(48, 32, 64),
        Color.FromArgb(56, 32, 64),
        Color.FromArgb(64, 32, 64),
        Color.FromArgb(64, 32, 56),
        Color.FromArgb(64, 32, 48),
        Color.FromArgb(64, 32, 40),
        Color.FromArgb(64, 32, 32),
        Color.FromArgb(64, 40, 32),
        Color.FromArgb(64, 48, 32),
        Color.FromArgb(64, 56, 32),
        Color.FromArgb(64, 64, 32),
        Color.FromArgb(56, 64, 32),
        Color.FromArgb(48, 64, 32),
        Color.FromArgb(40, 64, 32),
        Color.FromArgb(32, 64, 32),
        Color.FromArgb(32, 64, 40),
        Color.FromArgb(32, 64, 48),
        Color.FromArgb(32, 64, 56),
        Color.FromArgb(32, 64, 64),
        Color.FromArgb(32, 56, 64),
        Color.FromArgb(32, 48, 64),
        Color.FromArgb(32, 40, 64),
        Color.FromArgb(44, 44, 64),
        Color.FromArgb(48, 44, 64),
        Color.FromArgb(52, 44, 64),
        Color.FromArgb(60, 44, 64),
        Color.FromArgb(64, 44, 64),
        Color.FromArgb(64, 44, 60),
        Color.FromArgb(64, 44, 52),
        Color.FromArgb(64, 44, 48),
        Color.FromArgb(64, 44, 44),
        Color.FromArgb(64, 48, 44),
        Color.FromArgb(64, 52, 44),
        Color.FromArgb(64, 60, 44),
        Color.FromArgb(64, 64, 44),
        Color.FromArgb(60, 64, 44),
        Color.FromArgb(52, 64, 44),
        Color.FromArgb(48, 64, 44),
        Color.FromArgb(44, 64, 44),
        Color.FromArgb(44, 64, 48),
        Color.FromArgb(44, 64, 52),
        Color.FromArgb(44, 64, 60),
        Color.FromArgb(44, 64, 64),
        Color.FromArgb(44, 60, 64),
        Color.FromArgb(44, 52, 64),
        Color.FromArgb(44, 48, 64),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0),
        Color.FromArgb(0, 0, 0)
    }

    Protected VGA_SC(&HFF) As UInt16 ' Sequencer Registers
    Protected VGA_CRTC(&HFF) As UInt16 ' CRT Controller Registers
    Protected VGA_ATTR(&HFF) As UInt16 ' Attribute Controller Registers
    Private ReadOnly VGA_GC(&HFF) As UInt16 ' Graphics Controller Registers
    Private ReadOnly VGA_Latch(4 - 1) As Byte

    Private flip3C0 As Boolean = False
    Private stateDAC As Byte
    Private latchReadRGB As Integer
    Private latchReadPal As Integer
    Private latchWriteRGB As Integer
    Private latchWritePal As Integer

    Protected portRAM(&HFFF) As Byte
    Protected vgaPalette(VGABasePalette.Length - 1) As Color

    Protected Const MEMSIZE As UInt32 = &H40000 ' 256KB
    Protected vRAM(MEMSIZE - 1) As Byte

    Private Const planeSize As UInt32 = &H10000 ' 4HB
    Private tmpRGB As UInt32
    Private tmpVal As Byte

    Private activeStartAddress As UInt32
    Private activeEndAddress As UInt32

    Private mCPU As X8086

    ' Video Modes: http://www.columbia.edu/~em36/wpdos/videomodes.txt
    '              http://webpages.charter.net/danrollins/techhelp/0114.HTM
    ' Ports: http://stanislavs.org/helppc/ports.html

    Public Sub New(cpu As X8086, Optional useInternalTimer As Boolean = True, Optional enableWebUI As Boolean = False)
        MyBase.New(cpu, useInternalTimer, enableWebUI)
        mCPU = cpu
        mCellSize = New Size(8, 16) ' Temporary hack until we can stretch the fonts' bitmaps

        mCPU.LoadBIN("roms\et4000(4-7-93).BIN", &HC000, &H0)

        RegisteredPorts.Clear()
        For i As UInt32 = &H3C0 To &H3CF ' EGA/VGA
            RegisteredPorts.Add(i)
        Next
        RegisteredPorts.Add(&H3B4)
        RegisteredPorts.Add(&H3D4)
        RegisteredPorts.Add(&H3B5)
        RegisteredPorts.Add(&H3D5)

        RegisteredPorts.Add(&H3D8)

        RegisteredPorts.Add(&H3BA)
        RegisteredPorts.Add(&H3DA)

        'RegisteredPorts.Add(&H3B8) ' Hercules

        Array.Copy(VGABasePalette, vgaPalette, VGABasePalette.Length)

        mCPU.TryAttachHook(New X8086.MemHandler(Function(address As UInt32, ByRef value As UInt16, mode As X8086.MemHookMode) As Boolean
                                                    If address >= activeStartAddress AndAlso address < activeEndAddress Then
                                                        Select Case mode
                                                            Case X8086.MemHookMode.Read
                                                                If address < activeStartAddress Then Stop
                                                                value = VideoRAM(address)
                                                            Case X8086.MemHookMode.Write
                                                                VideoRAM(address) = value
                                                        End Select
                                                        Return True
                                                    End If

                                                    Return False
                                                End Function))

        mCPU.TryAttachHook(&H10, New X8086.IntHandler(Function() As Boolean
                                                          If mCPU.Registers.AH = &H0 OrElse mCPU.Registers.AH = &H10 Then
                                                              SetVideoMode()

                                                              If mCPU.Registers.AH = &H10 Then Return True
                                                              If mVideoMode = 9 Then Return True
                                                          End If

                                                          If mCPU.Registers.AH = &H1A Then
                                                              mCPU.Registers.AL = &H1A ' http://stanislavs.org/helppc/int_10-1a.html
                                                              mCPU.Registers.BL = &H8
                                                              Return True
                                                          End If

                                                          Return False
                                                      End Function))

        Dim waitHandle As New EventWaitHandle(False, EventResetMode.AutoReset)
        Task.Run(Sub()
                     Dim scanLineTiming As Long = (Scheduler.HOSTCLOCK / 31500) / 1_000_000 ' 31.5KHz
                     Dim curScanLine As Integer = 0

                     While True
                         waitHandle.WaitOne(TimeSpan.FromTicks(scanLineTiming))

                         curScanLine = (curScanLine + 1) Mod 525
                         If curScanLine > 479 Then
                             portRAM(&H3DA) = portRAM(&H3DA) Or &B0000_1000
                         Else
                             portRAM(&H3DA) = portRAM(&H3DA) And &B1111_0110
                         End If
                         If (curScanLine And 1) <> 0 Then portRAM(&H3DA) = portRAM(&H3DA) Or &H1
                     End While
                 End Sub)
    End Sub

    Public Property VideoRAM(address As UInt32) As Byte
        Get
            If mVideoMode = &HD OrElse mVideoMode = &HE OrElse mVideoMode = &H10 OrElse mVideoMode = &H12 Then Return Read(address - mStartGraphicsVideoAddress)
            If mVideoMode <> &H13 AndAlso mVideoMode <> &H12 AndAlso mVideoMode <> &HD Then Return mCPU.Memory(address)
            If (VGA_SC(4) And 6) = 0 Then
                Return mCPU.Memory(address)
            Else
                Return Read(address - mStartGraphicsVideoAddress)
            End If
        End Get
        Set(value As Byte)
            If mVideoMode <> &H13 AndAlso mVideoMode <> &H12 AndAlso mVideoMode <> &HD AndAlso mVideoMode <> &H10 Then
                mCPU.Memory(address) = value
            ElseIf (VGA_SC(4) And 6) = 0 AndAlso mVideoMode <> &HD AndAlso mVideoMode <> &H10 AndAlso mVideoMode <> &H12 Then
                mCPU.Memory(address) = value
            Else
                Write(address - mStartGraphicsVideoAddress, value)
            End If
        End Set
    End Property

    Public Overrides Property VideoMode As UInt32
        Get
            Return mVideoMode
        End Get
        Set(value As UInt32)
            Dim oldAX As UInt16 = mCPU.Registers.AX
            mCPU.Registers.AX = value
            SetVideoMode()
            mCPU.Registers.AX = oldAX
        End Set
    End Property

    Private Sub SetVideoMode()
        Select Case mCPU.Registers.AH
            Case &H0
                For i As Integer = 0 To VGABasePalette.Length - 1
                    vgaPalette(i) = VGABasePalette(i)
                Next

                VGA_SC(4) = 0
                mVideoMode = mCPU.Registers.AL And &H7F ' http://stanislavs.org/helppc/ports.html
                X8086.Notify($"VGA Video Mode: {CShort(mVideoMode):X2}", X8086.NotificationReasons.Info)

                ' http://www.o3one.org/hwdocs/vga/vga_app.html
                Select Case mVideoMode
                    Case 0 ' TEXT 40x25 Mono Text
                        mStartTextVideoAddress = &HB8000
                        mStartGraphicsVideoAddress = &HB8000
                        mTextResolution = New Size(40, 25)
                        mVideoResolution = New Size(360, 400)
                        'mCellSize = New Size(9, 16)
                        mMainMode = MainModes.Text
                        mPixelsPerByte = 1

                    Case 1 ' TEXT 40x25 Color Text
                        mStartTextVideoAddress = &HB8000
                        mStartGraphicsVideoAddress = &HB8000
                        mTextResolution = New Size(40, 25)
                        mVideoResolution = New Size(360, 400)
                        'mCellSize = New Size(9, 16)
                        mMainMode = MainModes.Text
                        mPixelsPerByte = 4
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case 2 ' TEXT 80x25 Mono Text
                        mStartTextVideoAddress = &HB8000
                        mStartGraphicsVideoAddress = &HB8000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(640, 400)
                        'mCellSize = New Size(9, 16)
                        mMainMode = MainModes.Text
                        mPixelsPerByte = 1
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case 3 ' TEXT 80x25 Color Text
                        mStartTextVideoAddress = &HB8000
                        mStartGraphicsVideoAddress = &HB8000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(720, 400)
                        'mCellSize = New Size(9, 16)
                        mMainMode = MainModes.Text
                        mPixelsPerByte = 4
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case 4, 5 ' CGA 320x200 4 Colors
                        mStartTextVideoAddress = &HB8000
                        mStartGraphicsVideoAddress = &HB8000
                        mTextResolution = New Size(40, 25)
                        mVideoResolution = New Size(320, 200)
                        'mCellSize = New Size(8, 8)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 4
                        'portRAM(&H3D9) = If(value And &HF = 4, 48, 0)
                        If mCPU.Registers.AL = 4 Then
                            portRAM(&H3D9) = 48
                        Else
                            portRAM(&H3D9) = 0
                        End If

                    Case 6 ' CGA 640x200 2 Colors (double scan)
                        mStartTextVideoAddress = &HB8000
                        mStartGraphicsVideoAddress = &HB8000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(640, 400)
                        'mCellSize = New Size(8, 8)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 2
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case 7 ' MDA 640x200 2 Colors
                        mStartTextVideoAddress = &HB0000
                        mStartGraphicsVideoAddress = &HB0000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(720, 400)
                        'mCellSize = New Size(9, 16)
                        mMainMode = MainModes.Text
                        mPixelsPerByte = 1

                    Case 9 ' PCjr 320x200 16 Colors
                        mStartTextVideoAddress = &HB8000
                        mStartGraphicsVideoAddress = &HB8000
                        mTextResolution = New Size(40, 25)
                        mVideoResolution = New Size(320, 200)
                        'mCellSize = New Size(8, 8)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 4
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case &HD ' EGA/VGA 320x200 16 Colors
                        mStartTextVideoAddress = &HA0000
                        mStartGraphicsVideoAddress = &HA0000
                        mTextResolution = New Size(40, 25)
                        mVideoResolution = New Size(320, 200)
                        'mCellSize = New Size(8, 8)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 4
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case &HE ' EGA/VGA 640x200 16 Colors
                        mStartTextVideoAddress = &HA0000
                        mStartGraphicsVideoAddress = &HA0000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(640, 200)
                        'mCellSize = New Size(8, 8)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 4

                    Case &HF ' EGA/VGA 640x350 3 Colors
                        mStartTextVideoAddress = &HA0000
                        mStartGraphicsVideoAddress = &HA0000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(640, 350)
                        'mCellSize = New Size(8, 8)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 4

                    Case &H10 ' EGA/VGA 640x350 16 Colors
                        mStartTextVideoAddress = &HA0000
                        mStartGraphicsVideoAddress = &HA0000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(640, 350)
                        'mCellSize = New Size(8, 14)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 1

                    Case &H11 ' VGA 640*480 2 Colors
                        mStartTextVideoAddress = &HA0000
                        mStartGraphicsVideoAddress = &HA0000
                        mTextResolution = New Size(80, 25)
                        mVideoResolution = New Size(640, 480)
                        'mCellSize = New Size(8, 14)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 1

                    Case &H12 ' VGA 640x480 16 Colors
                        mStartTextVideoAddress = &HA0000
                        mStartGraphicsVideoAddress = &HA0000
                        mTextResolution = New Size(40, 25)
                        mVideoResolution = New Size(640, 480)
                        'mCellSize = New Size(8, 16)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 4
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case &H13 ' VGA 320x200 256 Colors
                        mStartTextVideoAddress = &HA0000
                        mStartGraphicsVideoAddress = &HA0000
                        mTextResolution = New Size(40, 25)
                        mVideoResolution = New Size(320, 200)
                        'mCellSize = New Size(8, 8)
                        mMainMode = MainModes.Graphics
                        mPixelsPerByte = 4
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                    Case 127 ' 90x25 Mono Text
                        mStartTextVideoAddress = &HB0000
                        mStartGraphicsVideoAddress = &HB0000
                        mTextResolution = New Size(90, 25)
                        mVideoResolution = New Size(720, 400)
                        'mCellSize = New Size(8, 16)
                        mMainMode = MainModes.Text
                        mPixelsPerByte = 1
                        portRAM(&H3D8) = portRAM(&H3D8) And &HFE

                End Select

                mCPU.Memory(&H449) = mVideoMode
                mCPU.Memory(&H44A) = mTextResolution.Width
                mCPU.Memory(&H44B) = 0
                mCPU.Memory(&H484) = mTextResolution.Height - 1

                InitVideoMemory(False)

            Case &H10
                Select Case mCPU.Registers.AL
                    Case &H10 ' Set individual DAC register
                        vgaPalette(mCPU.Registers.BX Mod 256) = Color.FromArgb(RGBToUInt(CUInt(mCPU.Registers.DH And &H3F) << 2,
                                                                                         CUInt(mCPU.Registers.CH And &H3F) << 2,
                                                                                         CUInt(mCPU.Registers.CL And &H3F) << 2))

                    Case &H12 ' Set block of DAC registers
                        Dim addr As Integer = mCPU.Registers.ES * 16UI + mCPU.Registers.DX
                        For n As Integer = mCPU.Registers.BX To mCPU.Registers.BX + mCPU.Registers.CX - 1
                            vgaPalette(n) = Color.FromArgb(RGBToUInt(mCPU.Memory(addr + 0) << 2,
                                                                     mCPU.Memory(addr + 1) << 2,
                                                                     mCPU.Memory(addr + 2) << 2))
                            addr += 3
                        Next
                End Select

        End Select
    End Sub

    Public Overrides Function [In](port As UInt16) As Byte
        Select Case port
            Case &H3C1 : Return VGA_ATTR(portRAM(&H3C0))

            Case &H3C5 : Return VGA_SC(portRAM(&H3C4))

            Case &H3B5 : Return VGA_CRTC(portRAM(&H3D4))
            Case &H3D5 : Return VGA_CRTC(portRAM(&H3D4))

            Case &H3C7 : Return stateDAC

            Case &H3C8 : Return latchReadPal

            Case &H3C9
                Select Case latchReadRGB
                    Case 0 ' R
                        tmpRGB = (vgaPalette(latchReadPal).ToArgb() >> 18) And &H3F
                    Case 1 ' G
                        tmpRGB = (vgaPalette(latchReadPal).ToArgb() >> 10) And &H3F
                    Case 2 ' B
                        tmpRGB = (vgaPalette(latchReadPal).ToArgb() >> 2) And &H3F
                        latchReadPal += 1
                        latchReadRGB = -1
                End Select
                latchReadRGB += 1
                Return tmpRGB And &H3F

            Case &H3BA, &H3DA
                flip3C0 = True ' https://wiki.osdev.org/VGA_Hardware#Port_0x3C0
                Return portRAM(&H3DA)

            Case Else
                Return portRAM(port)

        End Select
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        Select Case port
            Case &H3B8
                If ((value & 2) = 2) AndAlso (mVideoMode <> 127) Then
                    Dim oldAX As UInt16 = mCPU.Registers.AX
                    mCPU.Registers.AH = 0
                    mCPU.Registers.AL = 127
                    'mCPU.intHooks(&H10).Invoke()
                    'mCPU.HandleInterrupt(&H10, False)
                    'SetVideoMode()
                    mCPU.Registers.AX = oldAX
                End If
                If (value And &H80) <> 0 Then
                    mStartTextVideoAddress = &HB8000
                Else
                    mStartTextVideoAddress = &HB0000
                End If
                InitVideoMemory(False)

            Case &H3C0 ' https://wiki.osdev.org/VGA_Hardware#Port_0x3C0
                If flip3C0 Then
                    portRAM(&H3C0) = value
                Else
                    VGA_ATTR(portRAM(&H3C0)) = value
                End If
                flip3C0 = Not flip3C0

            Case &H3C4 ' Sequence controller index
                portRAM(&H3C4) = value Mod 4 ' FIXME: Fugly hack that fixes many things

            Case &H3C5 ' Sequence controller data
                VGA_SC(portRAM(&H3C4)) = value

                'mVideoEnabled = (VGA_SC(1) And &B0010_0000) = 0

            Case &H3C7 ' Color index register (read operations)
                latchReadPal = value
                latchReadRGB = 0
                stateDAC = 0

            Case &H3C8 ' Color index register (write operations)
                latchWritePal = value
                latchWriteRGB = 0
                tmpRGB = 0
                stateDAC = 3

            Case &H3C9 ' RGB data register
                Dim cv As UInt32 = value And &H3F
                Select Case latchWriteRGB
                    Case 0 ' R
                        tmpRGB = cv << 18
                    Case 1 ' G
                        tmpRGB = tmpRGB Or (cv << 10)
                    Case 2 ' B
                        tmpRGB = tmpRGB Or (cv << 2)
                        vgaPalette(latchWritePal) = Color.FromArgb(tmpRGB)
                        latchWritePal += 1
                End Select
                latchWriteRGB = (latchWriteRGB + 1) Mod 3

            Case &H3B4, &H3D4 ' CRT Controller index register
                portRAM(&H3D4) = value Mod &H18

            Case &H3B5, &H3D5 ' CRT Controller data register
                'If VGA_CRTC(&H11) And &B1000_0000 Then
                '    If value < &H7 Then Return
                '    If value = &H7 Then
                '        VGA_CRTC(portRAM(&H3D5)) = VGA_CRTC(portRAM(&H3D5)) Or (value And &B0001_0000)
                '    End If
                'End If

                VGA_CRTC(portRAM(&H3D4)) = value

                UpdateCursor()

            'Case &H3BA, &H3DA
            '    portRAM(&H3DA) = value Or (portRAM(&H3DA) And &B0000_1001)

            Case &H3CE ' Graphics Registers index
                portRAM(&H3CE) = value Mod 8 ' FIXME: Fugly hack that fixes many things

            Case &H3CF ' Graphics Registers
                VGA_GC(portRAM(&H3CE)) = value

            Case Else
                portRAM(port) = value

        End Select
    End Sub

    Private Sub UpdateCursor()
        ' https://www.scs.stanford.edu/22wi-cs212/pintos/specs/freevga/vga/crtcreg.htm#0C
        mCursorVisible = (VGA_CRTC(&HA) And &B0010_0000) = 0
        mCursorStart = VGA_CRTC(&HA)
        mCursorEnd = VGA_CRTC(&HB)

        Dim startOffset As Integer = ((VGA_CRTC(&HC) And &H3F) << 8) Or (VGA_CRTC(&HD) And &HFF)
        Dim p As Integer = (VGA_CRTC(&HE) << 8) Or VGA_CRTC(&HF)
        p = (p - startOffset) And &H1FFF
        mCursorCol = p Mod mTextResolution.Width
        mCursorRow = p / mTextResolution.Width
    End Sub

    Public Overrides Sub Reset()
        MyBase.Reset()
        InitVideoMemory(False)
    End Sub

    Protected Overrides Sub InitVideoMemory(clearScreen As Boolean)
        If Not isInit OrElse mStartGraphicsVideoAddress = 0 Then Exit Sub

        MyBase.InitVideoMemory(clearScreen)

        mEndTextVideoAddress = mStartTextVideoAddress + &H4000
        mEndGraphicsVideoAddress = &HC0000

        Select Case mMainMode
            Case MainModes.Text
                activeStartAddress = mStartTextVideoAddress
                activeEndAddress = mEndTextVideoAddress
            Case MainModes.Graphics
                activeStartAddress = mStartGraphicsVideoAddress
                activeEndAddress = mEndGraphicsVideoAddress
        End Select

        If clearScreen Then
            Select Case mMainMode
                Case MainModes.Text
                    For i As Integer = mStartTextVideoAddress To mEndTextVideoAddress - 2 Step 2
                        CPU.Memory(i) = 0
                        CPU.Memory(i + 1) = 7
                    Next

                Case MainModes.Graphics
                    Array.Clear(vRAM, 0, vRAM.Length)
            End Select
        End If

        AutoSize()
    End Sub

    Public Overrides Sub Write(address As UInt32, value As Byte)
        Dim curValue As Byte

        Select Case VGA_GC(5) And 3
            Case 0
                ShiftVGA(value)

                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(1) And 1) <> 0 Then
                        curValue = If((VGA_GC(0) And 1) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(0))
                    vRAM(address + planeSize * 0) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(0))
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(1) And 2) <> 0 Then
                        curValue = If((VGA_GC(0) And 2) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(1))
                    vRAM(address + planeSize * 1) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(1))
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(1) And 4) <> 0 Then
                        curValue = If((VGA_GC(0) And 4) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(2))
                    vRAM(address + planeSize * 2) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(2))
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(1) And 8) <> 0 Then
                        curValue = If((VGA_GC(0) And 8) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(3))
                    vRAM(address + planeSize * 3) = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(3))
                End If

            Case 1
                If (VGA_SC(2) And 1) <> 0 Then vRAM(address + planeSize * 0) = VGA_Latch(0)
                If (VGA_SC(2) And 2) <> 0 Then vRAM(address + planeSize * 1) = VGA_Latch(1)
                If (VGA_SC(2) And 4) <> 0 Then vRAM(address + planeSize * 2) = VGA_Latch(2)
                If (VGA_SC(2) And 8) <> 0 Then vRAM(address + planeSize * 3) = VGA_Latch(3)

            Case 2
                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(1) And 1) <> 0 Then
                        curValue = If((value And 1) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(0))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(0))
                    vRAM(address + planeSize * 0) = curValue
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(1) And 2) <> 0 Then
                        curValue = If((value And 2) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(1))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(1))
                    vRAM(address + planeSize * 1) = curValue
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(1) And 4) <> 0 Then
                        curValue = If((value And 4) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(2))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(2))
                    vRAM(address + planeSize * 2) = curValue
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(1) And 8) <> 0 Then
                        curValue = If((value And 8) <> 0, 255, 0)
                    Else
                        curValue = value
                    End If
                    LogicVGA(curValue, VGA_Latch(3))
                    curValue = ((Not VGA_GC(8)) And curValue) Or (VGA_SC(8) And VGA_Latch(3))
                    vRAM(address + planeSize * 3) = curValue
                End If

            Case 3
                tmpVal = value And VGA_GC(8)
                ShiftVGA(value)

                If (VGA_SC(2) And 1) <> 0 Then
                    If (VGA_GC(0) And 1) <> 0 Then
                        If (value And 1) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    LogicVGA(curValue, VGA_Latch(0))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(0))
                    vRAM(address + planeSize * 0) = curValue
                End If

                If (VGA_SC(2) And 2) <> 0 Then
                    If (VGA_GC(0) And 2) <> 0 Then
                        If (value And 2) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    LogicVGA(curValue, VGA_Latch(1))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(1))
                    vRAM(address + planeSize * 1) = curValue
                End If

                If (VGA_SC(2) And 4) <> 0 Then
                    If (VGA_GC(0) And 4) <> 0 Then
                        If (value And 4) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    LogicVGA(curValue, VGA_Latch(2))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(2))
                    vRAM(address + planeSize * 2) = curValue
                End If

                If (VGA_SC(2) And 8) <> 0 Then
                    If (VGA_GC(0) And 8) <> 0 Then
                        If (value And 8) <> 0 Then
                            curValue = 255
                        Else
                            curValue = 0
                        End If
                    End If
                    LogicVGA(curValue, VGA_Latch(3))
                    curValue = ((Not tmpVal) And curValue) Or (tmpVal And VGA_Latch(3))
                    vRAM(address + planeSize * 3) = curValue
                End If
        End Select
    End Sub

    Public Overrides Function Read(address As UInt32) As Byte
        VGA_Latch(0) = vRAM(address + planeSize * 0)
        VGA_Latch(1) = vRAM(address + planeSize * 1)
        VGA_Latch(2) = vRAM(address + planeSize * 2)
        VGA_Latch(3) = vRAM(address + planeSize * 3)

        If (VGA_SC(2) And 1) <> 0 Then Return VGA_Latch(0)
        If (VGA_SC(2) And 2) <> 0 Then Return VGA_Latch(1)
        If (VGA_SC(2) And 4) <> 0 Then Return VGA_Latch(2)
        If (VGA_SC(2) And 8) <> 0 Then Return VGA_Latch(3)

        Return 0
    End Function

    Private Sub ShiftVGA(ByRef value As Byte)
        For i As Integer = 0 To (VGA_GC(3) And 7) - 1
            value = (value >> 1) Or ((value And 1) << 7)
        Next
    End Sub

    Private Sub LogicVGA(ByRef curValue As Byte, latchValue As Byte)
        Select Case (VGA_GC(3) >> 3) And 3 ' Raster Op
            Case 1 : curValue = curValue And latchValue
            Case 2 : curValue = curValue Or latchValue
            Case 3 : curValue = curValue Xor latchValue
        End Select
    End Sub

    Private Function RGBToUInt(r As UInt16, g As UInt16, b As UInt16) As UInt16
        Return r Or (g << 8) Or (b << 16)
    End Function

    Public Overrides ReadOnly Property Name As String
        Get
            Return "VGA"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "VGA Video Adapter"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 1
        End Get
    End Property
End Class

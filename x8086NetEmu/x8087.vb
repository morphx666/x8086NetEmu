' http://www.csn.ul.ie/~darkstar/assembler/manual/a07.txt
' http://www.ousob.com/ng/masm/ng2e21c.php
' http://x86.renejeschke.de/html/file_module_x86_id_79.html

' Code adapted from DOSBox: https://sourceforge.net/p/dosbox/code-0/HEAD/tree/dosbox/trunk/src/fpu/

Public Class x8087
    Private Structure REGs
        Public d As Double
        Public Structure l
            Public upper As Int32
            Public lower As UInt32
        End Structure
        Public ll As Int64
    End Structure

    Private Structure P_REGs
        Public m1 As UInt32
        Public m2 As UInt32
        Public m3 As UInt16

        Public d1 As UInt16
        Public d2 As UInt32
    End Structure

    Private Enum TAGe
        Valid = 0
        Zero = 1
        Weird = 2
        Empty = 3
    End Enum

    Private Enum ROUNDe
        Nearest = 0
        Down = 1
        Up = 2
        Chop = 3
    End Enum

    Private Class FPUc
        Public Regs(9 - 1) As REGs
        Public P_Regs(9 - 1) As P_REGs
        Public Tags(9 - 1) As TAGe
        Public CW As UInt16 ' Control Word
        Public CW_MaskAll As UInt16
        Public SW As UInt16 ' Status Word
        Public TOP As UInt32
        Public Rounding As ROUNDe
    End Class

    Private cpu As X8086
    Private fpu As New FPUc

    Public Sub New(cpu As X8086)
        Me.cpu = cpu
    End Sub

    Public Sub Execute(opCode As Byte, am As X8086.AddressingMode)
        Dim opCode2 As Integer = cpu.RAM8(cpu.Registers.CS, cpu.Registers.IP + 1)

        ' 10/87 instructions implemented

        Select Case opCode
            Case &HD8
                Select Case opCode2
                    Case &HD1 ' FCOM
                        FCOM()
                    Case &HD9 ' FCOMP
                        FCOM()
                        If fpu.Tags(TOP) = TAGe.Empty Then Stop ' E_Exit("FPU stack underflow")
                        fpu.Tags(TOP) = TAGe.Empty
                        TOP = ((TOP + 1) And 7)
                End Select
            Case &HD9
                Select Case opCode2
                    Case &HD0 ' FNOP
                    Case &HE0 ' FCHS
                        fpu.Regs(TOP).d = -1.0 * (fpu.Regs(TOP).d)
                    Case &HE1 ' FABS
                        fpu.Regs(TOP).d = Math.Abs(fpu.Regs(TOP).d)
                    Case &HF0 ' F2XM1
                        fpu.Regs(TOP).d = Math.Pow(2.0, fpu.Regs(TOP).d) - 1
                    Case &H3C, &H3E ' FNSTCW
                        cpu.RAMn = fpu.CW
                End Select
            Case &HDE
                Select Case opCode2
                    Case &HC1 ' FADD
                        fpu.Regs(TOP).d += fpu.Regs(8).d
                    Case &HC9 ' FMUL
                        fpu.Regs(TOP).d *= fpu.Regs(8).d
                End Select
            Case &HDB
                Select Case opCode2
                    Case &HE3 ' FINIT
                        SetCW(&H37F)
                        fpu.SW = 0
                        TOP = GetTOP()
                        For i As Integer = 0 To fpu.Tags.Length - 2
                            fpu.Tags(i) = TAGe.Empty
                        Next
                        fpu.Tags(8) = TAGe.Valid ' Is only used by us
                        cpu.Registers.AX = 1
                End Select
        End Select
    End Sub

    Private Sub FCOM()
        If ((fpu.Tags(TOP) <> TAGe.Valid) AndAlso (fpu.Tags(TOP) <> TAGe.Zero)) OrElse
                    ((fpu.Tags(8) <> TAGe.Valid) AndAlso (fpu.Tags(8) <> TAGe.Zero)) Then
            SetC3(1) : SetC2(1) : SetC0(1)
            Exit Sub
        End If
        If fpu.Regs(TOP).d = fpu.Regs(8).d Then
            SetC3(1) : SetC2(0) : SetC0(0)
            Exit Sub
        End If
        If fpu.Regs(TOP).d < fpu.Regs(8).d Then
            SetC3(0) : SetC2(0) : SetC0(1)
            Exit Sub
        End If
        SetC3(0) : SetC2(0) : SetC0(0)
    End Sub

#Region "Helpers"
    Private ReadOnly Property STV(index As Integer) As UInt32
        Get
            Return (fpu.TOP + index) And &H7
        End Get
    End Property

    Private Property TOP As UInt32
        Get
            Return fpu.TOP
        End Get
        Set(value As UInt32)
            fpu.TOP = value
        End Set
    End Property

    Public Sub SetTag(tag As UInt16)
        For i As Integer = 0 To fpu.Tags.Length - 1
            fpu.Tags(i) = CType((tag >> (2 * i)) And 3, TAGe)
        Next
    End Sub

    Public Function GetTag() As UInt16
        Dim result As UInt16 = 0
        For i As Integer = 0 To fpu.Tags.Length - 1
            result = result Or ((fpu.Tags(i) And 3) << (2 * i))
        Next
        Return result
    End Function

    Public Sub SetCW(word As UInt16)
        fpu.CW = word
        fpu.CW_MaskAll = word And &H3F
        fpu.Rounding = CType((word >> 10) And 3, ROUNDe)
    End Sub

    Public Function GetTOP() As UInt32
        Return (fpu.SW And &H3800) >> 11
    End Function

    Public Sub SetTOP(value As UInt32)
        fpu.SW = fpu.SW And (Not &H3800)
        fpu.SW = fpu.SW Or (value And 7) << 11
    End Sub

    Public Sub SetC0(C As UInt16)
        fpu.SW = fpu.SW And (Not &H100)
        If C <> 0 Then fpu.SW = fpu.SW Or &H100
    End Sub

    Public Sub SetC1(C As UInt16)
        fpu.SW = fpu.SW And (Not &H200)
        If C <> 0 Then fpu.SW = fpu.SW Or &H200
    End Sub

    Public Sub SetC2(C As UInt16)
        fpu.SW = fpu.SW And (Not &H400)
        If C <> 0 Then fpu.SW = fpu.SW Or &H400
    End Sub

    Public Sub SetC3(C As UInt16)
        fpu.SW = fpu.SW And (Not &H4000)
        If C <> 0 Then fpu.SW = fpu.SW Or &H4000
    End Sub

    Private Function GetParam32() As UInteger
        Return (cpu.RAM16(cpu.Registers.CS, cpu.Registers.IP, 4) << 16) Or cpu.RAM16(cpu.Registers.CS, cpu.Registers.IP)
    End Function
#End Region
End Class

' http://www.csn.ul.ie/~darkstar/assembler/manual/a07.txt
' http://www.ousob.com/ng/masm/ng2e21c.php

Public Class x8087
    Private ST(7) As Double
    Private i As Integer

    Private controlWord As Integer
    Private statusWord As Integer
    Private tagWord As Integer

    Private cpu As x8086

    Public Sub New(cpu As x8086)
        Me.cpu = cpu
    End Sub

    Public Sub Execute(opCode As Byte)
        Dim opCode2 As Integer = cpu.RAM8(cpu.Registers.CS, cpu.Registers.IP + 1)

        Select Case opCode
            Case &HD8 ' FADD

            Case &HD9
                Select Case opCode2
                    Case &H3C ' FNSTCW
                        cpu.RAM16(&H40, &H200) = controlWord
                    Case &HF0 ' F2XM1
                        ST(0) = 2.0 ^ ST(0) - 1.0
                    Case &HE1 ' FABS            
                        ST(0) = Math.Abs(ST(0))
                    Case &HE0 ' FCHS
                        ST(0) -= ST(0)
                    Case &HFF ' FCOS
                        ' 386 Only
                    Case &HF6 ' FDECSTP
                    Case &HF7 ' FINCSTP

                End Select
            Case &HDE ' FADD
                ST(1) += ST(0)
            Case &HD8 ' FADDi
                ST(1) += ST(i)
            Case &HDB ' FNINIT
                Select Case opCode2
                    Case &HE3 ' Nowait Initialize 8087
                        For i As Integer = 0 To ST.Length - 1
                            ST(i) = 0.0
                        Next
                        controlWord = &H37F
                End Select

                cpu.Registers.AX = 1
        End Select
    End Sub
End Class

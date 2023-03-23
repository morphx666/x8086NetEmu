Partial Public Class X8086
    Public Structure Instruction
        Public OpCode As Byte
        Public Mnemonic As String
        Public Parameter1 As String
        Public Parameter2 As String
        Public Size As Byte
        Public CS As Integer
        Public IP As Integer
        Public Message As String
        Public JumpAddress As Integer
        Public IndMemoryData As Integer
        Public IndAddress As Integer
        Public Bytes() As Byte
        Public IsValid As Boolean
        Public ClockCycles As Byte
        Public SegmentOverride As String

        Private str As String
        Private strFull As String

        Public Overloads Function ToString(includeOpCode As Boolean) As String
            If str = "" Then
                Dim s1 As String

                If includeOpCode Then
                    Dim r As String = ""
                    If Bytes IsNot Nothing Then
                        For i As Integer = 0 To Bytes.Length - 1
                            r += Bytes(i).ToString("X") + " "
                        Next
                    End If

                    s1 = String.Format("{0}:{1} {2} {3}", CS.ToString("X4"),
                                                            IP.ToString("X4"),
                                                            r.PadRight(6 * 3),
                                                            Mnemonic.PadRight(7, " "))
                Else
                    s1 = String.Format("{0}:{1} {2}", CS.ToString("X4"),
                                                        IP.ToString("X4"),
                                                        Mnemonic.PadRight(7, " "))
                End If

                If Parameter1 <> "" Then
                    If Parameter2 <> "" Then
                        s1 += String.Format("{0}, {1}", Parameter1, Parameter2)
                    Else
                        s1 += Parameter1
                    End If
                End If
                Return s1
            Else
                Return str
            End If
        End Function

        Public Overrides Function ToString() As String
            Return ToString(False)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Not TypeOf obj Is Instruction Then Return False
            Return Me = CType(obj, Instruction)
        End Function

        Public Shared Operator =(i1 As Instruction, i2 As Instruction) As Boolean
            If i1.Size = i2.Size AndAlso (i1.IP = i2.IP) AndAlso (i1.CS = i2.CS) Then
                For i As Integer = 0 To i1.Size - 1
                    If i1.Bytes(i) <> i2.Bytes(i) Then Return False
                Next
                Return True
            Else
                Return False
            End If
        End Operator

        Public Shared Operator <>(i1 As Instruction, i2 As Instruction) As Boolean
            Return Not i1 = i2
        End Operator
    End Structure

    Private indASM As String
    Private opCodeASM As String
    Private segOvr As String = ""
    Private decoderClkCyc As Byte
    Private decoderIPAddrOff As Integer
    Private decoderAddrMode As AddressingMode
    Private decoderSyncObj As New Object()

    Private Function InvalidOpCode() As Instruction
        Dim inst = New Instruction With {
            .Mnemonic = "",
            .IsValid = False
        }
        Return inst
    End Function

    Public Function Decode(Optional force As Boolean = False) As Instruction
        Return Decode(mRegisters.CS, mRegisters.IP)
    End Function

    Public Function Decode(segment As UInt16, offset As UInt16, Optional force As Boolean = False) As Instruction
        Dim cs As UInt16 = mRegisters.CS
        Dim ip As UInt16 = mRegisters.IP
        Dim asc As Boolean = mRegisters.ActiveSegmentChanged
        Dim asr As GPRegisters.RegistersTypes = mRegisters.ActiveSegmentRegister
        Dim ins As Instruction

        mRegisters.CS = segment
        mRegisters.IP = offset

        If force Then
            ins = DoDecode()
        Else
            SyncLock decoderSyncObj
                ins = DoDecode()
            End SyncLock
        End If

        mRegisters.CS = cs
        mRegisters.IP = ip

        If asc Then
            mRegisters.ActiveSegmentRegister = asr
        Else
            mRegisters.ResetActiveSegment()
        End If

        Return ins
    End Function

    Private Function DoDecode() As Instruction
        newPrefix = False
        opCodeSize = 1
        decoderIPAddrOff = 0
        opCodeASM = ""
        decoderClkCyc = 0

        opCode = RAM8(mRegisters.CS, mRegisters.IP)

        Select Case opCode
            Case &H0 To &H3 ' add
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "ADD " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "ADD " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "ADD " + indASM + ", " + decoderAddrMode.Register1.ToString()
                        decoderClkCyc += 16
                    Else
                        opCodeASM = "ADD " + decoderAddrMode.Register1.ToString() + ", " + indASM
                        decoderClkCyc += 9
                    End If
                End If

            Case &H4 ' add al and imm
                opCodeASM = "ADD AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 4

            Case &H5 ' add ax and imm
                opCodeASM = "ADD AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 4

            Case &H6 ' push es
                opCodeASM = "PUSH ES"
                decoderClkCyc += 10

            Case &H7 ' pop es
                opCodeASM = "POP ES"
                decoderClkCyc += 8

            Case &H8 To &HB ' or
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "OR " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "OR " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "OR " + indASM + ", " + decoderAddrMode.Register1.ToString()
                        decoderClkCyc += 16
                    Else
                        opCodeASM = "OR " + decoderAddrMode.Register1.ToString() + ", " + indASM
                        decoderClkCyc += 9
                    End If
                End If

            Case &HC ' or al and imm
                opCodeASM = "OR AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 4

            Case &HD ' or ax and imm
                opCodeASM = "OR AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 4

            Case &HE ' push cs
                opCodeASM = "PUSH CS"
                decoderClkCyc += 10

            Case &H10 To &H13 ' adc
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "ADC " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "ADC " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "ADC " + indASM + ", " + decoderAddrMode.Register1.ToString()
                        decoderClkCyc += 16
                    Else
                        opCodeASM = "ADC " + decoderAddrMode.Register1.ToString() + ", " + indASM
                        decoderClkCyc += 9
                    End If
                End If

            Case &H14 ' adc al and imm
                opCodeASM = "ADC AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 3

            Case &H15 ' adc ax and imm
                opCodeASM = "ADC AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 3

            Case &H16 ' push ss
                opCodeASM = "PUSH SS"
                decoderClkCyc += 10

            Case &H17 ' pop ss
                opCodeASM = "POP SS"
                decoderClkCyc += 8

            Case &H18 To &H1B ' sbb
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "SBB " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "SBB " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "SBB " + indASM + ", " + decoderAddrMode.Register1.ToString()
                        decoderClkCyc += 16
                    Else
                        opCodeASM = "SBB " + decoderAddrMode.Register1.ToString() + ", " + indASM
                        decoderClkCyc += 3
                    End If
                End If

            Case &H1C ' sbb al and imm
                opCodeASM = "SBB AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 4

            Case &H1D ' sbb ax and imm
                opCodeASM = "SBB AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 4

            Case &H1E ' push ds
                opCodeASM = "PUSH DS"
                decoderClkCyc += 10

            Case &H1F ' pop ds
                opCodeASM = "POP DS"

                decoderClkCyc += 8

            Case &H20 To &H23 ' and reg/mem and reg to either | and imm to acc
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "AND " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "AND " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "AND " + indASM + ", " + decoderAddrMode.Register1.ToString()
                        decoderClkCyc += 16
                    Else
                        opCodeASM = "AND " + decoderAddrMode.Register1.ToString() + ", " + indASM
                        decoderClkCyc += 9
                    End If
                End If

            Case &H24 ' and al and imm
                opCodeASM = "AND AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 4

            Case &H25 ' and ax and imm
                opCodeASM = "AND AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 4

            Case &H27 ' daa
                opCodeASM = "DAA"
                decoderClkCyc += 4

            Case &H28 To &H2B ' sub reg/mem with reg to either | sub imm from acc
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "SUB " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "SUB " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "SUB " + indASM + ", " + decoderAddrMode.Register1.ToString()
                        decoderClkCyc += 16
                    Else
                        opCodeASM = "SUB " + decoderAddrMode.Register1.ToString() + ", " + indASM
                        decoderClkCyc += 9
                    End If
                End If

            Case &H2C ' sub al and imm
                opCodeASM = "SUB AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 4

            Case &H2D ' sub ax and imm
                opCodeASM = "SUB AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 4

            Case &H2F ' das
                opCodeASM = "DAS"
                decoderClkCyc += 4

            Case &H30 To &H33 ' xor reg/mem and reg to either | xor imm to acc
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "XOR " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "XOR " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "XOR " + indASM + ", " + decoderAddrMode.Register1.ToString()
                        decoderClkCyc += 16
                    Else
                        opCodeASM = "XOR " + decoderAddrMode.Register1.ToString() + ", " + indASM
                        decoderClkCyc += 9
                    End If
                End If

            Case &H34 ' xor al and imm
                opCodeASM = "XOR AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 4

            Case &H35 ' xor ax and imm
                opCodeASM = "XOR AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 4

            Case &H37 ' aaa
                opCodeASM = "AAA"
                decoderClkCyc += 8

            Case &H38 To &H3B ' cmp reg/mem and reg
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "CMP " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "CMP " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    End If
                    decoderClkCyc += 3
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "CMP " + indASM + ", " + decoderAddrMode.Register1.ToString()
                    Else
                        opCodeASM = "CMP " + decoderAddrMode.Register1.ToString() + ", " + indASM
                    End If
                    decoderClkCyc += 9
                End If

            Case &H3C ' cmp al and imm
                opCodeASM = "CMP AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 4

            Case &H3D ' cmp ax and imm
                opCodeASM = "CMP AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 4

            Case &H3F ' aas
                opCodeASM = "AAS"
                decoderClkCyc += 8

            Case &H26, &H2E, &H36, &H3E ' segment override prefix
                decoderAddrMode.Decode(opCode, opCode)
                decoderAddrMode.Register1 = decoderAddrMode.Register1 - GPRegisters.RegistersTypes.AH + GPRegisters.RegistersTypes.ES
                opCodeASM = decoderAddrMode.Register1.ToString() + ":"
                segOvr = opCodeASM
                newPrefix = True
                decoderClkCyc += 2

            Case &H40 To &H47 ' inc reg
                DecoderSetRegister1Alt(opCode)
                opCodeASM = "INC " + decoderAddrMode.Register1.ToString()

            Case &H48 To &H4F ' dec reg
                DecoderSetRegister1Alt(opCode)
                opCodeASM = "DEC " + decoderAddrMode.Register1.ToString()
                decoderClkCyc += 2

            Case &H50 To &H57 ' push reg
                DecoderSetRegister1Alt(opCode)
                opCodeASM = "PUSH " + decoderAddrMode.Register1.ToString()
                decoderClkCyc += 11

            Case &H58 To &H5F ' pop reg
                DecoderSetRegister1Alt(opCode)
                opCodeASM = "POP " + decoderAddrMode.Register1.ToString()
                decoderClkCyc += 8

            Case &H60 ' pusha
                opCodeASM = "PUSHA"
                decoderClkCyc += 19

            Case &H61 ' popa
                opCodeASM = "POPA"
                decoderClkCyc += 19

            Case &H70 ' jo
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JO " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.OF = 1, 16, 4)

            Case &H71 ' jno
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNO " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.OF = 0, 16, 4)

            Case &H72 ' jb/jnae
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JB " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.CF = 1, 16, 4)

            Case &H73 ' jnb/jae
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNB " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.CF = 0, 16, 4)

            Case &H74 ' je/jz
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.ZF = 1, 16, 4)

            Case &H75 ' jne/jnz
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.ZF = 0, 16, 4)

            Case &H76 ' jbe/jna
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JBE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.CF = 1 OrElse mFlags.ZF = 1, 16, 4)

            Case &H77 ' jnbe/ja
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNBE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.CF = 0 AndAlso mFlags.ZF = 0, 16, 4)

            Case &H78 ' js
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JS " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.SF = 1, 16, 4)

            Case &H79 ' jns
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNS " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.SF = 0, 16, 4)

            Case &H7A ' jp/jpe
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JP " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.PF = 1, 16, 4)

            Case &H7B ' jnp/jpo
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNP " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.PF = 0, 16, 4)

            Case &H7C ' jl/jnge
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JL " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.SF <> mFlags.OF, 16, 4)

            Case &H7D ' jnl/jge
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNL " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.SF = mFlags.OF, 16, 4)

            Case &H7E ' jle/jng
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JLE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.ZF = 1 OrElse (mFlags.SF <> mFlags.OF), 16, 4)

            Case &H7F ' jnle/jg
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JNLE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mFlags.ZF = 0 OrElse (mFlags.SF = mFlags.OF), 16, 4)

            Case &H80 To &H83 : DecodeGroup1()

            Case &H84 To &H85 ' test reg with reg/mem
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "TEST " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += 3
                Else
                    opCodeASM = "TEST " + indASM + ", " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += 9
                End If

            Case &H86 To &H87 ' xchg reg/mem with reg
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "XCHG " + decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += 4
                Else
                    opCodeASM = "XCHG " + indASM + ", " + decoderAddrMode.Register1.ToString()
                    decoderClkCyc += 17
                End If

            Case &H88 To &H8B ' mov ind <-> reg8/reg16
                SetDecoderAddressing()
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "MOV " + decoderAddrMode.Dst.ToString() + ", " + decoderAddrMode.Src.ToString()
                    decoderClkCyc += 2
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "MOV " + indASM + ", " + decoderAddrMode.Src.ToString()
                        decoderClkCyc += 9
                    Else
                        opCodeASM = "MOV " + decoderAddrMode.Dst.ToString() + ", " + indASM
                        decoderClkCyc += 8
                    End If
                End If

            Case &H8C ' mov Ew, Sw
                SetDecoderAddressing(DataSize.Word)
                decoderAddrMode.Src += GPRegisters.RegistersTypes.ES
                If decoderAddrMode.Dst > GPRegisters.RegistersTypes.BL Then
                    decoderAddrMode.Dst = (decoderAddrMode.Dst + GPRegisters.RegistersTypes.ES) Or shl3
                Else
                    decoderAddrMode.Dst = decoderAddrMode.Dst Or shl3
                End If

                If decoderAddrMode.IsDirect Then
                    opCodeASM = "MOV " + decoderAddrMode.Dst.ToString() + ", " + decoderAddrMode.Src.ToString()
                    decoderClkCyc += 2
                Else
                    If decoderAddrMode.Direction = 0 Then
                        opCodeASM = "MOV " + indASM + ", " + decoderAddrMode.Src.ToString()
                        decoderClkCyc += 9
                    Else
                        opCodeASM = "MOV " + decoderAddrMode.Dst.ToString() + ", " + indASM
                        decoderClkCyc += 8
                    End If
                End If

            Case &H8D ' lea
                SetDecoderAddressing()
                opCodeASM = "LEA " + decoderAddrMode.Register1.ToString() + ", " + indASM
                decoderClkCyc += 2

            Case &H8E ' mov Sw, Ew
                SetDecoderAddressing(DataSize.Word)
                DecoderSetRegister2ToSegReg()
                If decoderAddrMode.IsDirect Then
                    DecoderSetRegister1Alt(RAM8(mRegisters.CS, mRegisters.IP + 1))
                    opCodeASM = "MOV " + decoderAddrMode.Register2.ToString() + ", " + decoderAddrMode.Register1.ToString()
                    decoderClkCyc += 2
                Else
                    opCodeASM = "MOV " + decoderAddrMode.Register2.ToString() + ", " + indASM
                    decoderClkCyc += 8
                End If

            Case &H8F ' pop reg/mem
                SetDecoderAddressing()
                decoderAddrMode.Decode(opCode, opCode)
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "POP " + decoderAddrMode.Register1.ToString()
                Else
                    opCodeASM = "POP " + indASM
                End If
                decoderClkCyc += 17

            Case &H90 ' nop
                opCodeASM = "NOP"
                decoderClkCyc += 3

            Case &H91 To &H97 ' xchg reg with acc
                DecoderSetRegister1Alt(opCode)
                opCodeASM = "XCHG AX, " + decoderAddrMode.Register1.ToString()
                decoderClkCyc += 3

            Case &H98 ' cbw
                opCodeASM = "CBW"
                decoderClkCyc += 2

            Case &H99 ' cwd
                opCodeASM = "CWD"
                decoderClkCyc += 5

            Case &H9A ' call direct intersegment
                opCodeASM = "CALL " + DecoderParam(ParamIndex.Second, , DataSize.Word).ToString("X4") + ":" + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 28

            Case &H9B ' wait
                opCodeASM = "FWAIT"

            Case &H9C ' pushf
                opCodeASM = "PUSHF"
                decoderClkCyc += 10

            Case &H9D ' popf
                opCodeASM = "POPF"
                decoderClkCyc += 8

            Case &H9E ' sahf
                opCodeASM = "SAHF"
                decoderClkCyc += 4

            Case &H9F ' lahf
                opCodeASM = "LAHF"
                decoderClkCyc += 4

            Case &HA0 To &HA3 ' mov mem to acc | mov acc to mem
                decoderAddrMode.Decode(opCode, opCode)
                If decoderAddrMode.Direction = 0 Then
                    If decoderAddrMode.Size = DataSize.Byte Then
                        opCodeASM = "MOV AL, [" + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4") + "]"
                    Else
                        opCodeASM = "MOV AX, [" + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4") + "]"
                    End If
                Else
                    If decoderAddrMode.Size = DataSize.Byte Then
                        opCodeASM = "MOV [" + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4") + "], AL"
                    Else
                        opCodeASM = "MOV [" + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4") + "], AX"
                    End If
                End If
                decoderClkCyc += 10

            Case &HA4 ' movsb
                opCodeASM = "MOVSB"
                decoderClkCyc += 18

            Case &HA5 ' movsw
                opCodeASM = "MOVSW"
                decoderClkCyc += 18

            Case &HA6 ' cmpsb
                opCodeASM = "CMPSB"
                decoderClkCyc += 22

            Case &HA7 ' cmpsw
                opCodeASM = "CMPSW"
                decoderClkCyc += 22

            Case &HA8 To &HA9 ' test
                If (opCode And &H1) = 0 Then
                    opCodeASM = "TEST AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                Else
                    opCodeASM = "TEST AX, " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                End If
                decoderClkCyc += 4

            Case &HAA ' stosb
                opCodeASM = "STOSB"
                decoderClkCyc += 11

            Case &HAB 'stosw
                opCodeASM = "STOSW"
                decoderClkCyc += 11

            Case &HAC ' lodsb
                opCodeASM = "LODSB"
                decoderClkCyc += 12

            Case &HAD ' lodsw
                opCodeASM = "LODSW"
                decoderClkCyc += 16

            Case &HAE ' scasb
                opCodeASM = "SCASB"
                decoderClkCyc += 15

            Case &HAF ' scasw
                opCodeASM = "SCASW"
                decoderClkCyc += 15

            Case &HB0 To &HBF ' mov imm to reg
                decoderAddrMode.Register1 = opCode And &H7
                If (opCode And &H8) = &H8 Then
                    decoderAddrMode.Register1 += GPRegisters.RegistersTypes.AX
                    If (opCode And &H4) = &H4 Then decoderAddrMode.Register1 += GPRegisters.RegistersTypes.ES
                    decoderAddrMode.Size = DataSize.Word
                Else
                    decoderAddrMode.Size = DataSize.Byte
                End If
                opCodeASM = "MOV " + decoderAddrMode.Register1.ToString() + ", " + DecoderParam(ParamIndex.First).ToHex(decoderAddrMode.Size)
                decoderClkCyc += 4

            Case &HC0, &HC1 : DecodeGroup2()

            Case &HC2 ' ret within segment adding imm to sp
                opCodeASM = "RET " + DecoderParam(ParamIndex.First).ToHex()
                decoderClkCyc += 20

            Case &HC3 ' ret within segment
                opCodeASM = "RET"
                decoderClkCyc += 16

            Case &HC4 To &HC5 ' les | lds
                SetDecoderAddressing()
                Dim targetRegister As GPRegisters.RegistersTypes
                If opCode = &HC4 Then
                    opCodeASM = "LES "
                    targetRegister = GPRegisters.RegistersTypes.ES
                Else
                    opCodeASM = "LDS "
                    targetRegister = GPRegisters.RegistersTypes.DS
                End If

                If (decoderAddrMode.Register1 And shl2) = shl2 Then
                    decoderAddrMode.Register1 = (decoderAddrMode.Register1 + GPRegisters.RegistersTypes.ES) Or shl3
                Else
                    decoderAddrMode.Register1 = decoderAddrMode.Register1 Or shl3
                End If
                'If decoderAddrMode.IsDirect Then
                '    If (decoderAddrMode.Register2 And shl2) = shl2 Then
                '        decoderAddrMode.Register2 = (decoderAddrMode.Register2 + GPRegisters.RegistersTypes.BX + 1) Or shl3
                '    Else
                '        decoderAddrMode.Register2 = (decoderAddrMode.Register2 Or shl3)
                '    End If

                '    opCodeASM += decoderAddrMode.Register1.ToString() + ", " + decoderAddrMode.Register2.ToString()
                'Else
                opCodeASM += decoderAddrMode.Register1.ToString() + ", " + indASM
                'End If
                decoderClkCyc += 16

            Case &HC6 To &HC7 ' mov imm to reg/mem
                SetDecoderAddressing()
                opCodeASM = "MOV " + indASM + ", " + DecoderParam(ParamIndex.First, opCodeSize).ToHex(decoderAddrMode.Size)
                decoderClkCyc += 10

            Case &HC8 ' enter
                opCodeASM = "ENTER"
                opCodeSize += 3

            Case &HC9 ' leave
                opCodeASM = "LEAVE"

            Case &HCA ' ret intersegment adding imm to sp
                opCodeASM = "RETF " + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 17

            Case &HCB ' ret intersegment (retf)
                opCodeASM = "RETF"
                decoderClkCyc += 18

            Case &HCC ' int with type 3
                opCodeASM = "INT 3"
                decoderClkCyc += 52

            Case &HCD ' int with type specified
                opCodeASM = "INT " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 51

            Case &HCE ' into
                opCodeASM = "INTO"
                decoderClkCyc += If(mFlags.OF = 1, 53, 4)

            Case &HCF ' iret
                opCodeASM = "IRET"
                decoderClkCyc += 32

            Case &HD0 To &HD3 : DecodeGroup2()

            Case &HD4 ' aam
                opCodeASM = "AAM " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 83

            Case &HD5 ' aad
                opCodeASM = "AAD " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 60

            Case &HD6 ' xlat
                opCodeASM = "XLAT"
                decoderClkCyc += 4

            Case &HD7 ' xlatb
                opCodeASM = "XLATB"
                decoderClkCyc += 11

            Case &HD9 ' fnstsw (required for BIOS to boot)?
                If DecoderParamNOPS(ParamIndex.First, , DataSize.Byte) = &H3C Then
                    opCodeASM = "FNSTSW {NOT IMPLEMENTED}"
                Else
                    opCodeASM = opCode.ToString("X2") + " {NOT IMPLEMENTED}"
                End If
                opCodeSize += 1

            Case &HDB ' fninit (required for BIOS to boot)?
                If DecoderParamNOPS(ParamIndex.First, , DataSize.Byte) = &HE3 Then
                    opCodeASM = "FNINIT {NOT IMPLEMENTED}"
                Else
                    opCodeASM = opCode.ToString("X2") + " {NOT IMPLEMENTED}"
                End If
                opCodeSize += 1

            Case &HE0 ' loopne
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "LOOPNE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mRegisters.CX <> 0 AndAlso mFlags.ZF = 0, 19, 5)

            Case &HE1 ' loope
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "LOOPE " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mRegisters.CX <> 0 AndAlso mFlags.ZF = 1, 18, 6)

            Case &HE2 ' loop
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "LOOP " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mRegisters.CX <> 0, 17, 5)

            Case &HE3 ' jcxz
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JCXZ " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += If(mRegisters.CX = 0, 18, 6)

            Case &HE4 ' in to al from fixed port
                opCodeASM = "IN AL, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 10

            Case &HE5 ' inw to ax from fixed port
                opCodeASM = "IN AX, " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2")
                decoderClkCyc += 10

            Case &HE6  ' out to al to fixed port
                opCodeASM = "OUT " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2") + ", AL"
                decoderClkCyc += 10

            Case &HE7  ' outw to ax to fixed port
                opCodeASM = "OUT " + DecoderParam(ParamIndex.First, , DataSize.Byte).ToString("X2") + ", Ax"
                decoderClkCyc += 10

            Case &HE8 ' call direct within segment
                opCodeASM = "CALL " + OffsetIP(DataSize.Word).ToString("X4")
                decoderClkCyc += 19

            Case &HE9 ' jmp direct within segment
                opCodeASM = "JMP " + OffsetIP(DataSize.Word).ToString("X4")
                decoderClkCyc += 15

            Case &HEA ' jmp direct intersegment
                opCodeASM = "JMP " + DecoderParam(ParamIndex.Second, , DataSize.Word).ToString("X4") + ":" + DecoderParam(ParamIndex.First, , DataSize.Word).ToString("X4")
                decoderClkCyc += 15

            Case &HEB ' jmp direct within segment short
                decoderIPAddrOff = OffsetIP(DataSize.Byte)
                opCodeASM = "JMP " + decoderIPAddrOff.ToString("X4")
                decoderClkCyc += 15

            Case &HEC  ' in to al from variable port
                opCodeASM = "IN AL, DX"
                decoderClkCyc += 8

            Case &HED ' inw to ax from variable port
                opCodeASM = "IN AX, DX"
                decoderClkCyc += 8

            Case &HEE ' out to port dx from al
                opCodeASM = "OUT DX, AL"
                decoderClkCyc += 8

            Case &HEF ' out to port dx from ax
                opCodeASM = "OUT DX, AX"
                decoderClkCyc += 8

            Case &HF0 ' lock
                opCodeASM = "LOCK"
                decoderClkCyc += 2

            Case &HF2 ' repne/repnz
                opCodeASM = "REPNE"
                newPrefix = True
                decoderClkCyc += 2

            Case &HF3 ' rep/repe
                opCodeASM = "REPE"
                newPrefix = True
                decoderClkCyc += 2

            Case &HF4 ' hlt
                opCodeASM = "HLT"
                decoderClkCyc += 2

            Case &HF5 ' cmc
                opCodeASM = "CMC"
                decoderClkCyc += 2

            Case &HF6 To &HF7 : DecodeGroup3()

            Case &HF8 ' clc
                opCodeASM = "CLC"
                decoderClkCyc += 2

            Case &HF9 ' stc
                opCodeASM = "STC"
                decoderClkCyc += 2

            Case &HFA ' cli
                opCodeASM = "CLI"
                decoderClkCyc += 2

            Case &HFB ' sti
                opCodeASM = "STI"
                decoderClkCyc += 2

            Case &HFC ' cld
                opCodeASM = "CLD"
                decoderClkCyc += 2

            Case &HFD ' std
                opCodeASM = "STD"
                decoderClkCyc += 2

            Case &HFE To &HFF : DecodeGroup4_And_5()

            Case Else
                opCodeASM = opCode.ToString("X2") + ": {NOT IMPLEMENTED}"
        End Select

        If opCodeSize = 0 Then
            Throw New Exception("Decoding error for opCode " + opCode.ToString("X2"))
        End If

        If segOvr = "" Then segOvr = mRegisters.ActiveSegmentRegister.ToString() + ":"

        Dim info As Instruction = New Instruction() With {
            .IsValid = True,
            .OpCode = opCode,
            .CS = mRegisters.CS,
            .IP = mRegisters.IP,
            .Size = opCodeSize,
            .JumpAddress = decoderIPAddrOff,
            .IndMemoryData = decoderAddrMode.IndMem,
            .IndAddress = decoderAddrMode.IndAdr,
            .ClockCycles = decoderClkCyc,
            .SegmentOverride = segOvr
        }

        If opCodeASM <> "" Then
            If opCodeSize > 0 Then
                ReDim info.Bytes(opCodeSize - 1)
                info.Bytes(0) = opCode
            End If
            For i As Integer = 1 To opCodeSize - 1
                info.Bytes(i) = DecoderParamNOPS(ParamIndex.First, i, DataSize.Byte)
            Next
            If opCodeASM.Contains(" ") Then
                Dim space As Integer = opCodeASM.IndexOf(" ")
                info.Mnemonic = opCodeASM.Substring(0, space)
                opCodeASM = opCodeASM.Substring(space + 1)
                If opCodeASM.Contains("{") Then
                    info.Message = opCodeASM
                Else
                    If opCodeASM.Contains(",") Then
                        info.Parameter1 = opCodeASM.Split(",")(0)
                        info.Parameter2 = opCodeASM.Split(",")(1).Trim()
                    Else
                        info.Parameter1 = opCodeASM
                    End If
                End If
            Else
                info.Mnemonic = opCodeASM
            End If
        End If
        If segOvr <> "" AndAlso info.Mnemonic <> segOvr Then segOvr = ""
        decoderClkCyc += opCodeSize * 4

        If Not newPrefix AndAlso mRegisters.ActiveSegmentChanged Then mRegisters.ResetActiveSegment()

        Return info
    End Function

    Private Sub DecodeGroup1()
        SetDecoderAddressing()
        Dim paramSize As DataSize = If(opCode = &H81, DataSize.Word, DataSize.Byte)
        Select Case decoderAddrMode.Reg
            Case 0 ' 000    --   add imm to reg/mem
                opCodeASM = "ADD"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 17)
            Case 1 ' 001    --  or imm to reg/mem
                opCodeASM = "OR"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 17)
            Case 2 ' 010    --  adc imm to reg/mem
                opCodeASM = "ADC"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 17)
            Case 3 ' 011    --  sbb imm from reg/mem
                opCodeASM = "SBB"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 17)
            Case 4 ' 100    --  and imm to reg/mem
                opCodeASM = "AND"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 17)
            Case 5 ' 101    --  sub imm from reg/mem
                opCodeASM = "SUB"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 17)
            Case 6 ' 110    --  xor imm to reg/mem
                opCodeASM = "XOR"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 17)
            Case 7 ' 111    --  cmp imm with reg/mem
                opCodeASM = "CMP"
                decoderClkCyc += If(decoderAddrMode.IsDirect, 4, 10)
        End Select
        If decoderAddrMode.IsDirect Then
            opCodeASM += " " + decoderAddrMode.Register2.ToString() + ", " + DecoderParam(ParamIndex.First, opCodeSize, paramSize).ToHex(paramSize)
        Else
            opCodeASM += " " + indASM + ", " + DecoderParam(ParamIndex.First, opCodeSize, paramSize).ToHex(paramSize)
        End If
    End Sub

    Private Sub DecodeGroup2()
        SetDecoderAddressing()

        Select Case opCode
            Case &HD0, &HD1
                opCodeASM = decoderAddrMode.Register2.ToString() + ", 1"
                decoderClkCyc += 2
            Case &HD2, &HD3
                opCodeASM = decoderAddrMode.Register2.ToString() + ", CL"
                decoderClkCyc += 8 + 4 '* count
            Case &HC0, &HC1
                opCodeASM = decoderAddrMode.Register2.ToString() + ", " + DecoderParam(ParamIndex.Second,  , DataSize.Byte).ToHex()
                decoderClkCyc += 2
        End Select

        'If decoderAddrMode.IsDirect Then
        '    If opCode >= &HD2 Then
        '        opCodeASM = decoderAddrMode.Register2.ToString() + ", CL"
        '        decoderClkCyc += 8 + 4 '* count
        '    Else
        '        opCodeASM = decoderAddrMode.Register2.ToString() + ", 1"
        '        decoderClkCyc += 2
        '    End If
        'Else
        '    If (opCode And &H2) = &H2 Then
        '        opCodeASM = indASM + ", CL"
        '        decoderClkCyc += 20 + 4 '* count
        '    Else
        '        opCodeASM = indASM + ", 1"
        '        decoderClkCyc += 15
        '    End If
        'End If

        Select Case decoderAddrMode.Reg
            Case 0 : opCodeASM = "ROL " + opCodeASM
            Case 1 : opCodeASM = "ROR " + opCodeASM
            Case 2 : opCodeASM = "RCL " + opCodeASM
            Case 3 : opCodeASM = "RCR " + opCodeASM
            Case 4, 6 : opCodeASM = "SHL " + opCodeASM
            Case 5 : opCodeASM = "SHR " + opCodeASM
            Case 7 : opCodeASM = "SAR " + opCodeASM
        End Select
    End Sub

    Private Sub DecodeGroup3()
        SetDecoderAddressing()

        Select Case decoderAddrMode.Reg
            Case 0 ' 000    --  test
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "TEST " + decoderAddrMode.Register2.ToString() + ", " + DecoderParam(ParamIndex.First, opCodeSize).ToHex(decoderAddrMode.Size)
                    decoderClkCyc += 5
                Else
                    opCodeASM = "TEST " + indASM + ", " + DecoderParam(ParamIndex.First, opCodeSize).ToHex(decoderAddrMode.Size)
                    decoderClkCyc += 11
                End If
            Case 2 ' 010    --  not
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "NOT " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += 3
                Else
                    opCodeASM = "NOT " + indASM
                    decoderClkCyc += 16
                End If
            Case 3 ' 010    --  neg
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "NEG " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += 3
                Else
                    opCodeASM = "NEG " + indASM
                    decoderClkCyc += 16
                End If

            Case 4 ' 100    --  mul
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "MUL " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 70, 118)
                Else
                    opCodeASM = "MUL " + indASM
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 76, 124)
                End If
            Case 5 ' 101    --  imul
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "IMUL " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 80, 128)
                Else
                    opCodeASM = "IMUL " + indASM
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 86, 134)
                End If

            Case 6 ' 110    --  div
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "DIV " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 80, 144)
                Else
                    opCodeASM = "DIV " + indASM
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 86, 168)
                End If

            Case 7 ' 111    --  idiv
                Dim div As Integer = mRegisters.Val(decoderAddrMode.Register2)
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "IDIV " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 101, 165)
                Else
                    opCodeASM = "IDIV " + indASM
                    decoderClkCyc += If(decoderAddrMode.Size = DataSize.Byte, 107, 171)
                End If
        End Select
    End Sub

    Private Sub DecodeGroup4_And_5()
        SetDecoderAddressing()

        Select Case decoderAddrMode.Reg
            Case 0 ' 000    --  inc reg/mem
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "INC " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += 3
                Else
                    opCodeASM = "INC " + indASM
                    decoderClkCyc += 15
                End If

            Case 1 ' 001    --  dec reg/mem
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "DEC " + decoderAddrMode.Register2.ToString()
                    decoderClkCyc += 3
                Else
                    opCodeASM = "DEC " + indASM
                    decoderClkCyc += 15
                End If

            Case 2 ' 010    --  call indirect within segment
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "CALL " + decoderAddrMode.Register2.ToString()
                Else
                    opCodeASM = "CALL " + indASM
                End If
                decoderClkCyc += 11

            Case 3 ' 011    --  call indirect intersegment
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "CALL " + decoderAddrMode.Register2.ToString() + " {NOT IMPLEMENTED}"
                Else
                    opCodeASM = "CALL " + indASM
                End If

                decoderClkCyc += 37

            Case 4 ' 100    --  jmp indirect within segment
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "JMP " + decoderAddrMode.Register2.ToString()
                Else
                    opCodeASM = "JMP " + indASM
                End If
                decoderClkCyc += 15

            Case 5 ' 101    --  jmp indirect intersegment
                If decoderAddrMode.IsDirect Then
                    opCodeASM = "JMP " + decoderAddrMode.Register2.ToString() + " {NOT IMPLEMENTED}"
                Else
                    opCodeASM = "JMP " + indASM
                End If
                decoderClkCyc += 24

            Case 6 ' 110    --  push reg/mem
                opCodeASM = "PUSH " + indASM
                decoderClkCyc += 16

            Case 7 ' 111    --  BIOS DI
                opCodeASM = "BIOS DI"
                opCodeSize = 2
                decoderClkCyc += 0
        End Select
    End Sub

    Private Sub SetDecoderAddressing(Optional forceSize As DataSize = DataSize.UseAddressingMode)
#If DEBUG Then
        decoderAddrMode.Decode(opCode, RAM8(mRegisters.CS, mRegisters.IP + 1))
#Else
        decoderAddrMode = decoderCache((Convert.ToUInt16(opCode) << 8) Or RAM8(mRegisters.CS, mRegisters.IP + 1))
#End If

        If forceSize <> DataSize.UseAddressingMode Then decoderAddrMode.Size = forceSize

        ' AS = SS when Rm = 2 or 3
        ' If Rm = 6, AS will be set to SS, except for Modifier = 0
        ' http://www.ic.unicamp.br/~celio/mc404s2-03/addr_modes/intel_addr.html

        If Not mRegisters.ActiveSegmentChanged Then
            Select Case decoderAddrMode.Rm
                Case 2, 3 : mRegisters.ActiveSegmentRegister = GPRegisters.RegistersTypes.SS
                Case 6 : If decoderAddrMode.Modifier <> 0 Then mRegisters.ActiveSegmentRegister = GPRegisters.RegistersTypes.SS
            End Select
        End If

        ' http://umcs.maine.edu/~cmeadow/courses/cos335/Asm07-MachineLanguage.pdf
        ' http://maven.smith.edu/~thiebaut/ArtOfAssembly/CH04/CH04-2.html#HEADING2-35
        Select Case decoderAddrMode.Modifier
            Case 0 ' 00
                decoderAddrMode.IsDirect = False
                Select Case decoderAddrMode.Rm
                    Case 0 : decoderAddrMode.IndAdr = mRegisters.BX + mRegisters.SI : indASM = "[BX + SI]" : decoderClkCyc += 7 ' 000 [BX+SI]
                    Case 1 : decoderAddrMode.IndAdr = mRegisters.BX + mRegisters.DI : indASM = "[BX + DI]" : decoderClkCyc += 8 ' 001 [BX+DI]
                    Case 2 : decoderAddrMode.IndAdr = mRegisters.BP + mRegisters.SI : indASM = "[BP + SI]" : decoderClkCyc += 8 ' 010 [BP+SI]
                    Case 3 : decoderAddrMode.IndAdr = mRegisters.BP + mRegisters.DI : indASM = "[BP + DI]" : decoderClkCyc += 7 ' 011 [BP+DI]
                    Case 4 : decoderAddrMode.IndAdr = mRegisters.SI : indASM = "[SI]" : decoderClkCyc += 5                      ' 100 [SI]
                    Case 5 : decoderAddrMode.IndAdr = mRegisters.DI : indASM = "[DI]" : decoderClkCyc += 5                      ' 101 [DI]
                    Case 6                                                                                               ' 110 Direct Addressing
                        decoderAddrMode.IndAdr = DecoderParamNOPS(ParamIndex.First, 2, DataSize.Word)
                        indASM = "[" + DecoderParamNOPS(ParamIndex.First, 2, DataSize.Word).ToString("X4") + "]"
                        opCodeSize += 2
                        decoderClkCyc += 9
                    Case 7 : decoderAddrMode.IndAdr = mRegisters.BX : indASM = "[BX]" : decoderClkCyc += 5                      ' 111 [BX]
                End Select
                decoderAddrMode.IndMem = RAMn

            Case 1 ' 01 - 8bit
                decoderAddrMode.IsDirect = False
                Select Case decoderAddrMode.Rm
                    Case 0 : decoderAddrMode.IndAdr = mRegisters.BX + mRegisters.SI : indASM = "[BX + SI]" : decoderClkCyc += 7 ' 000 [BX+SI]
                    Case 1 : decoderAddrMode.IndAdr = mRegisters.BX + mRegisters.DI : indASM = "[BX + DI]" : decoderClkCyc += 8 ' 001 [BX+DI]
                    Case 2 : decoderAddrMode.IndAdr = mRegisters.BP + mRegisters.SI : indASM = "[BP + SI]" : decoderClkCyc += 8 ' 010 [BP+SI]
                    Case 3 : decoderAddrMode.IndAdr = mRegisters.BP + mRegisters.DI : indASM = "[BP + DI]" : decoderClkCyc += 7 ' 011 [BP+DI]
                    Case 4 : decoderAddrMode.IndAdr = mRegisters.SI : indASM = "[SI]" : decoderClkCyc += 5                      ' 100 [SI]
                    Case 5 : decoderAddrMode.IndAdr = mRegisters.DI : indASM = "[DI]" : decoderClkCyc += 5                      ' 101 [DI]
                    Case 6 : decoderAddrMode.IndAdr = mRegisters.BP : indASM = "[BP]" : decoderClkCyc += 5                      ' 110 [BP]
                    Case 7 : decoderAddrMode.IndAdr = mRegisters.BX : indASM = "[BX]" : decoderClkCyc += 5                      ' 111 [BX]
                End Select

                Dim p As Byte = DecoderParamNOPS(ParamIndex.First, 2, DataSize.Byte)
                Dim s As Integer
                If p > &H80 Then
                    p = &H100 - p
                    s = -1
                Else
                    s = 1
                End If
                indASM = indASM.Replace("]", If(s = -1, " - ", " + ") + p.ToString("X2") + "]")
                decoderAddrMode.IndAdr += s * p
                decoderAddrMode.IndMem = RAMn
                opCodeSize += 1

            Case 2 ' 10 - 16bit
                decoderAddrMode.IsDirect = False
                Select Case decoderAddrMode.Rm
                    Case 0 : decoderAddrMode.IndAdr = mRegisters.BX + mRegisters.SI : indASM = "[BX + SI]" : decoderClkCyc += 7 ' 000 [BX+SI]
                    Case 1 : decoderAddrMode.IndAdr = mRegisters.BX + mRegisters.DI : indASM = "[BX + DI]" : decoderClkCyc += 8 ' 001 [BX+DI]
                    Case 2 : decoderAddrMode.IndAdr = mRegisters.BP + mRegisters.SI : indASM = "[BP + SI]" : decoderClkCyc += 8 ' 010 [BP+SI]
                    Case 3 : decoderAddrMode.IndAdr = mRegisters.BP + mRegisters.DI : indASM = "[BP + DI]" : decoderClkCyc += 7 ' 011 [BP+DI]
                    Case 4 : decoderAddrMode.IndAdr = mRegisters.SI : indASM = "[SI]" : decoderClkCyc += 5                      ' 100 [SI]
                    Case 5 : decoderAddrMode.IndAdr = mRegisters.DI : indASM = "[DI]" : decoderClkCyc += 5                      ' 101 [DI]
                    Case 6 : decoderAddrMode.IndAdr = mRegisters.BP : indASM = "[BP]" : decoderClkCyc += 5                      ' 110 [BP]
                    Case 7 : decoderAddrMode.IndAdr = mRegisters.BX : indASM = "[BX]" : decoderClkCyc += 5                      ' 111 [BX]
                End Select

                indASM = indASM.Replace("]", " + " + DecoderParamNOPS(ParamIndex.First, 2, DataSize.Word).ToString("X4") + "]")
                decoderAddrMode.IndAdr += DecoderParamNOPS(ParamIndex.First, 2, DataSize.Word)
                decoderAddrMode.IndMem = RAMn
                opCodeSize += 2

            Case 3 ' 11
                decoderAddrMode.IsDirect = True

        End Select
        opCodeSize += 1
    End Sub

    Private Sub DecoderSetRegister1Alt(data As Byte)
        decoderAddrMode.Register1 = (data And &H7) Or shl3
        If decoderAddrMode.Register1 >= GPRegisters.RegistersTypes.ES Then decoderAddrMode.Register1 += GPRegisters.RegistersTypes.ES
        decoderAddrMode.Size = DataSize.Word
    End Sub

    Private Sub DecoderSetRegister2ToSegReg()
        decoderAddrMode.Register2 = decoderAddrMode.Reg + GPRegisters.RegistersTypes.ES
        decoderAddrMode.Size = DataSize.Word
    End Sub

    Private ReadOnly Property DecoderParam(index As ParamIndex, Optional ipOffset As UInt16 = 1, Optional size As DataSize = DataSize.UseAddressingMode) As UInt16
        Get
            If size = DataSize.UseAddressingMode Then size = decoderAddrMode.Size
            opCodeSize += (size + 1)
            Return DecoderParamNOPS(index, ipOffset, size)
        End Get
    End Property

    Private ReadOnly Property DecoderParamNOPS(index As ParamIndex, Optional ipOffset As UInt16 = 1, Optional size As DataSize = DataSize.UseAddressingMode) As UInt16
        Get
            ' Extra cycles for address misalignment
            ' This is too CPU expensive, with few benefits, if any... not worth it
            'If (mRegisters.IP Mod 2) <> 0 Then clkCyc += 4

            Return If(size = DataSize.Byte OrElse (size = DataSize.UseAddressingMode AndAlso decoderAddrMode.Size = DataSize.Byte),
                        RAM8(mRegisters.CS, mRegisters.IP, ipOffset + index, True),
                        RAM16(mRegisters.CS, mRegisters.IP, ipOffset + index * 2, True))
        End Get
    End Property
End Class
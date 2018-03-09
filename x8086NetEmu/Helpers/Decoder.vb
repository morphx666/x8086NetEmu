Partial Public Class X8086
    Public Structure Instruction
        Public decOpCode As Byte
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
                                                                        Mnemonic.PadRight(6, " "))
                Else
                    s1 = String.Format("{0}:{1} {2}", CS.ToString("X4"),
                                                                        IP.ToString("X4"),
                                                                        Mnemonic.PadRight(6, " "))
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
            Return Not (i1 = i2)
        End Operator
    End Structure

    Private indASM As String
    Private opCodeASM As String
    Private segOvr As String = ""
    Private decOpCode As Byte
    Private isDecoding As Boolean
    Private clkCycDecoder As Byte
    Private ipAddrOffDecoder As Integer

    Private Function InvalidOpCode() As Instruction
        Dim inst = New Instruction With {
            .Mnemonic = "",
            .IsValid = False
        }
        Return inst
    End Function

    Public Function Decode(emulator As X8086, Optional force As Boolean = False) As Instruction
        Return Decode(emulator.Registers.CS, emulator.Registers.IP, force)
    End Function

    Public Function Decode(segment As Integer, offset As Integer, Optional force As Boolean = False) As Instruction
        'Threading.Monitor.Enter(Sched)
        If (Not force) AndAlso (mIsExecuting OrElse isDecoding) Then
            Return InvalidOpCode()
        Else
            Return DoDecode(segment, offset)
        End If
        'Threading.Monitor.Exit(Sched)
    End Function

    Private Function DoDecode(segment As Integer, offset As Integer) As Instruction
        isDecoding = True

        Dim CS As Integer = mRegisters.CS
        Dim IP As Integer = mRegisters.IP
        Dim activeSegment As GPRegisters.RegistersTypes = mRegisters.ActiveSegmentRegister
        Dim activeSegmentChanged As Boolean = mRegisters.ActiveSegmentChanged

        mRegisters.CS = segment
        mRegisters.IP = offset

        ipAddrOffDecoder = 0
        opCodeASM = ""
        clkCycDecoder = 0

        decOpCode = RAM8(mRegisters.CS, mRegisters.IP)
        opCodeSize = 1

        Select Case decOpCode
            Case &H0 To &H3 ' add
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "ADD " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "ADD " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "ADD " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 16
                    Else
                        opCodeASM = "ADD " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 9
                    End If
                End If

            Case &H4 ' add al and imm
                opCodeASM = "ADD AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 4

            Case &H5 ' add ax and imm
                opCodeASM = "ADD AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 4

            Case &H6 ' push es
                opCodeASM = "PUSH ES"
                clkCycDecoder += 10

            Case &H7 ' pop es
                opCodeASM = "POP ES"
                clkCycDecoder += 8

            Case &H8 To &HB ' or
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "OR " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "OR " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "OR " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 16
                    Else
                        opCodeASM = "OR " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 9
                    End If
                End If

            Case &HC ' or al and imm
                opCodeASM = "OR AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 4

            Case &HD ' or ax and imm
                opCodeASM = "OR AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 4

            Case &HE ' push cs
                opCodeASM = "PUSH CS"
                clkCycDecoder += 10

            Case &H10 To &H13 ' adc
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "ADC " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "ADC " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "ADC " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 16
                    Else
                        opCodeASM = "ADC " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 9
                    End If
                End If

            Case &H14 ' adc al and imm
                opCodeASM = "ADC AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 3

            Case &H15 ' adc ax and imm
                opCodeASM = "ADC AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 3

            Case &H16 ' push ss
                opCodeASM = "PUSH SS"
                clkCycDecoder += 10

            Case &H17 ' pop ss
                opCodeASM = "POP SS"
                clkCycDecoder += 8

            Case &H18 To &H1B ' sbb
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "SBB " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "SBB " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "SBB " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 16
                    Else
                        opCodeASM = "SBB " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 3
                    End If
                End If

            Case &H1C ' sbb al and imm
                opCodeASM = "SBB AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 4

            Case &H1D ' sbb ax and imm
                opCodeASM = "SBB AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 4

            Case &H1E ' push ds
                opCodeASM = "PUSH DS"
                clkCycDecoder += 10

            Case &H1F ' pop ds
                opCodeASM = "POP DS"

                clkCycDecoder += 8

            Case &H20 To &H23 ' and reg/mem and reg to either | and imm to acc
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "AND " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "AND " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "AND " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 16
                    Else
                        opCodeASM = "AND " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 9
                    End If
                End If

            Case &H24 ' and al and imm
                opCodeASM = "AND AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 4

            Case &H25 ' and ax and imm
                opCodeASM = "AND AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 4

            Case &H27 ' daa
                opCodeASM = "DAA"
                clkCycDecoder += 4

            Case &H28 To &H2B ' sub reg/mem with reg to either | sub imm from acc
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "SUB " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "SUB " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "SUB " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 16
                    Else
                        opCodeASM = "SUB " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 9
                    End If
                End If

            Case &H2C ' sub al and imm
                opCodeASM = "SUB AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 4

            Case &H2D ' sub ax and imm
                opCodeASM = "SUB AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 4

            Case &H2F ' das
                opCodeASM = "DAS"
                clkCycDecoder += 4

            Case &H30 To &H33 ' xor reg/mem and reg to either | xor imm to acc
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "XOR " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "XOR " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "XOR " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 16
                    Else
                        opCodeASM = "XOR " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 9
                    End If
                End If

            Case &H34 ' xor al and imm
                opCodeASM = "XOR AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 4

            Case &H35 ' xor ax and imm
                opCodeASM = "XOR AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 4

            Case &H37 ' aaa
                opCodeASM = "AAA"
                clkCycDecoder += 8

            Case &H38 To &H3B ' cmp reg/mem and reg
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "CMP " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "CMP " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 3
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "CMP " + indASM + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "CMP " + addrMode.Register1.ToString() + ", " + indASM
                    End If
                    clkCycDecoder += 9
                End If

            Case &H3C ' cmp al and imm
                opCodeASM = "CMP AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 4

            Case &H3D ' cmp ax and imm
                opCodeASM = "CMP AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 4

            Case &H3F ' aas
                opCodeASM = "AAS"
                clkCycDecoder += 8

            Case &H26, &H2E, &H36, &H3E ' segment override prefix
                addrMode.Decode(decOpCode, decOpCode)
                addrMode.Register1 = (addrMode.Register1 - GPRegisters.RegistersTypes.AH) + GPRegisters.RegistersTypes.ES
                opCodeASM = addrMode.Register1.ToString() + ":"
                segOvr = opCodeASM
                clkCycDecoder += 2

            Case &H40 To &H47 ' inc reg
                SetRegister1Alt(decOpCode)
                opCodeASM = "INC " + addrMode.Register1.ToString()

            Case &H48 To &H4F ' dec reg
                SetRegister1Alt(decOpCode)
                opCodeASM = "DEC " + addrMode.Register1.ToString()
                clkCycDecoder += 2

            Case &H50 To &H57 ' push reg
                SetRegister1Alt(decOpCode)
                opCodeASM = "PUSH " + addrMode.Register1.ToString()
                clkCycDecoder += 11

            Case &H58 To &H5F ' pop reg
                SetRegister1Alt(decOpCode)
                opCodeASM = "POP " + addrMode.Register1.ToString()
                clkCycDecoder += 8

            Case &H60 ' pusha
                opCodeASM = "PUSHA"
                clkCycDecoder += 0 ' TODO: Need to investigate this...

            Case &H61 ' popa
                opCodeASM = "POPA"
                clkCycDecoder += 0 ' TODO: Need to investigate this...

            Case &H70 ' jo
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JO " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.OF = 1, 16, 4)

            Case &H71 ' jno
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNO " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.OF = 0, 16, 4)

            Case &H72 ' jb/jnae
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JB " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.CF = 1, 16, 4)

            Case &H73 ' jnb/jae
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNB " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.CF = 0, 16, 4)

            Case &H74 ' je/jz
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.ZF = 1, 16, 4)

            Case &H75 ' jne/jnz
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.ZF = 0, 16, 4)

            Case &H76 ' jbe/jna
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JBE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.CF = 1 OrElse mFlags.ZF = 1, 16, 4)

            Case &H77 ' jnbe/ja
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNBE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.CF = 0 AndAlso mFlags.ZF = 0, 16, 4)

            Case &H78 ' js
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JS " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.SF = 1, 16, 4)

            Case &H79 ' jns
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNS " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.SF = 0, 16, 4)

            Case &H7A ' jp/jpe
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JP " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.PF = 1, 16, 4)

            Case &H7B ' jnp/jpo
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNP " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.PF = 0, 16, 4)

            Case &H7C ' jl/jnge
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JL " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.SF <> mFlags.OF, 16, 4)

            Case &H7D ' jnl/jge
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNL " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.SF = mFlags.OF, 16, 4)

            Case &H7E ' jle/jng
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JLE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.ZF = 1 OrElse (mFlags.SF <> mFlags.OF), 16, 4)

            Case &H7F ' jnle/jg
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JNLE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mFlags.ZF = 0 OrElse (mFlags.SF = mFlags.OF), 16, 4)

            Case &H80 To &H83 : DecodeGroup1()

            Case &H84 To &H85 ' test reg with reg/mem
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    opCodeASM = "TEST " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    clkCycDecoder += 3
                Else
                    opCodeASM = "TEST " + indASM + ", " + addrMode.Register2.ToString()
                    clkCycDecoder += 9
                End If

            Case &H86 To &H87 ' xchg reg/mem with reg
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    opCodeASM = "XCHG " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    clkCycDecoder += 4
                Else
                    opCodeASM = "XCHG " + indASM + ", " + addrMode.Register1.ToString()
                    clkCycDecoder += 17
                End If

            Case &H88 To &H8C ' mov ind <-> reg8/reg16
                SetDecoderAddressing()
                If decOpCode = &H8C Then
                    If (addrMode.Register1 And &H4) = &H4 Then
                        addrMode.Register1 = addrMode.Register1 And (Not (1 << 2))
                    Else
                        addrMode.Register1 += GPRegisters.RegistersTypes.ES
                        If addrMode.Register2 > &H3 Then
                            addrMode.Register2 = (addrMode.Register2 + GPRegisters.RegistersTypes.ES) Or shl3
                        Else
                            addrMode.Register2 += GPRegisters.RegistersTypes.AX
                        End If
                    End If
                End If

                If addrMode.IsDirect Then
                    If addrMode.Direction = 0 Then
                        opCodeASM = "MOV " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    Else
                        opCodeASM = "MOV " + addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                    End If
                    clkCycDecoder += 2
                Else
                    If addrMode.Direction = 0 Then
                        opCodeASM = "MOV " + indASM + ", " + addrMode.Register1.ToString()
                        clkCycDecoder += 9
                    Else
                        opCodeASM = "MOV " + addrMode.Register1.ToString() + ", " + indASM
                        clkCycDecoder += 8
                    End If
                End If

            Case &H8D ' lea
                SetDecoderAddressing()
                opCodeASM = "LEA " + addrMode.Register1.ToString() + ", " + indASM
                clkCycDecoder += 2

            Case &H8E ' mov reg/mem to seg reg
                SetDecoderAddressing(DataSize.Word)
                SetRegister2ToSegReg() 'ParamNOPS(SelPrmIndex.First, , DataSize.Byte))
                If addrMode.IsDirect Then
                    SetRegister1Alt(ParamNOPS(SelPrmIndex.First, , DataSize.Byte))
                    opCodeASM = "MOV " + addrMode.Register2.ToString() + ", " + addrMode.Register1.ToString()
                    clkCycDecoder += 2
                Else
                    opCodeASM = "MOV " + addrMode.Register2.ToString() + ", " + indASM
                    clkCycDecoder += 8
                End If

            Case &H8F ' pop reg/mem
                SetDecoderAddressing()
                opCodeASM = "POP " + indASM
                clkCycDecoder += 17

            Case &H90 ' nop
                opCodeASM = "NOP"
                clkCycDecoder += 3

            Case &H91 To &H97 ' xchg reg with acc
                SetRegister1Alt(decOpCode)
                opCodeASM = "XCHG AX, " + addrMode.Register1.ToString()
                clkCycDecoder += 3

            Case &H98 ' cbw
                opCodeASM = "CBW"
                clkCycDecoder += 2

            Case &H99 ' cwd
                opCodeASM = "CWD"
                clkCycDecoder += 5

            Case &H9A ' call direct intersegment
                opCodeASM = "CALL " + Param(SelPrmIndex.Second, , DataSize.Word).ToString("X4") + ":" + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 28

            Case &H9B ' wait
                opCodeASM = "FWAIT"

            Case &H9C ' pushf
                opCodeASM = "PUSHF"
                clkCycDecoder += 10

            Case &H9D ' popf
                opCodeASM = "POPF"
                clkCycDecoder += 8

            Case &H9E ' sahf
                opCodeASM = "SAHF"
                clkCycDecoder += 4

            Case &H9F ' lahf
                opCodeASM = "LAHF"
                clkCycDecoder += 4

            Case &HA0 To &HA3 ' mov mem to acc | mov acc to mem
                addrMode.Decode(decOpCode, decOpCode)
                If addrMode.Direction = 0 Then
                    If addrMode.Size = DataSize.Byte Then
                        opCodeASM = "MOV AL, [" + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4") + "]"
                    Else
                        opCodeASM = "MOV AX, [" + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4") + "]"
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        opCodeASM = "MOV [" + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4") + "], AL"
                    Else
                        opCodeASM = "MOV [" + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4") + "], AX"
                    End If
                End If
                clkCycDecoder += 10

            Case &HA4 ' movsb
                opCodeASM = "MOVSB"
                clkCycDecoder += 18

            Case &HA5 ' movsw
                opCodeASM = "MOVSW"
                clkCycDecoder += 18

            Case &HA6 ' cmpsb
                opCodeASM = "CMPSB"
                clkCycDecoder += 22

            Case &HA7 ' cmpsw
                opCodeASM = "CMPSW"
                clkCycDecoder += 22

            Case &HA8 To &HA9 ' test
                If (decOpCode And &H1) = 0 Then
                    opCodeASM = "TEST AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                Else
                    opCodeASM = "TEST AX, " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                End If
                clkCycDecoder += 4

            Case &HAA ' stosb
                opCodeASM = "STOSB"
                clkCycDecoder += 11

            Case &HAB 'stosw
                opCodeASM = "STOSW"
                clkCycDecoder += 11

            Case &HAC ' lodsb
                opCodeASM = "LODSB"
                clkCycDecoder += 12

            Case &HAD ' lodsw
                opCodeASM = "LODSW"
                clkCycDecoder += 16

            Case &HAE ' scasb
                opCodeASM = "SCASB"
                clkCycDecoder += 15

            Case &HAF ' scasw
                opCodeASM = "SCASW"
                clkCycDecoder += 15

            Case &HB0 To &HBF ' mov imm to reg
                addrMode.Register1 = (decOpCode And &H7)
                If (decOpCode And &H8) = &H8 Then
                    addrMode.Register1 += GPRegisters.RegistersTypes.AX
                    If (decOpCode And &H4) = &H4 Then addrMode.Register1 += GPRegisters.RegistersTypes.ES
                    addrMode.Size = DataSize.Word
                Else
                    addrMode.Size = DataSize.Byte
                End If
                opCodeASM = "MOV " + addrMode.Register1.ToString() + ", " + Param(SelPrmIndex.First).ToHex(addrMode.Size)
                clkCycDecoder += 4

            Case &HC0, &HC1 : DecodeGroup2()

            Case &HC2 ' ret within segment adding imm to sp
                opCodeASM = "RET " + Param(SelPrmIndex.First).ToString("X4")
                clkCycDecoder += 20

            Case &HC3 ' ret within segment
                opCodeASM = "RET"
                clkCycDecoder += 16

            Case &HC4 To &HC5 ' les | lds
                SetDecoderAddressing()
                Dim targetRegister As GPRegisters.RegistersTypes
                If decOpCode = &HC4 Then
                    opCodeASM = "LES "
                    targetRegister = GPRegisters.RegistersTypes.ES
                Else
                    opCodeASM = "LDS "
                    targetRegister = GPRegisters.RegistersTypes.DS
                End If

                If (addrMode.Register1 And shl2) = shl2 Then
                    addrMode.Register1 = (addrMode.Register1 + GPRegisters.RegistersTypes.ES) Or shl3
                Else
                    addrMode.Register1 = (addrMode.Register1 Or shl3)
                End If
                'If addrMode.IsDirect Then
                '    If (addrMode.Register2 And shl2) = shl2 Then
                '        addrMode.Register2 = (addrMode.Register2 + GPRegisters.RegistersTypes.BX + 1) Or shl3
                '    Else
                '        addrMode.Register2 = (addrMode.Register2 Or shl3)
                '    End If

                '    opCodeASM += addrMode.Register1.ToString() + ", " + addrMode.Register2.ToString()
                'Else
                opCodeASM += addrMode.Register1.ToString() + ", " + indASM
                'End If
                clkCycDecoder += 16

            Case &HC6 To &HC7 ' mov imm to reg/mem
                SetDecoderAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register1) = Param(SelPrmIndex.First, , DataSize.Byte)
                    clkCycDecoder += 4
                Else
                    opCodeASM = "MOV " + indASM + ", " + Param(SelPrmIndex.First, opCodeSize).ToHex(addrMode.Size)
                    clkCycDecoder += 10
                End If

            Case &HC8 ' enter
                opCodeASM = "ENTER"
                opCodeSize += 3

            Case &HC9 ' leave
                opCodeASM = "LEAVE"

            Case &HCA ' ret intersegment adding imm to sp
                opCodeASM = "RETF " + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 17

            Case &HCB ' ret intersegment (retf)
                opCodeASM = "RETF"
                clkCycDecoder += 18

            Case &HCC ' int with type 3
                opCodeASM = "INT 3"
                clkCycDecoder += 52

            Case &HCD ' int with type specified
                opCodeASM = "INT " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 51

            Case &HCE ' into
                opCodeASM = "INTO"
                clkCycDecoder += IIf(mFlags.OF = 1, 53, 4)

            Case &HCF ' iret
                opCodeASM = "IRET"
                clkCycDecoder += 32

            Case &HD0 To &HD3 : DecodeGroup2()

            Case &HD4 ' aam
                opCodeASM = "AAM " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 83

            Case &HD5 ' aad
                opCodeASM = "AAD " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 60

            Case &HD6 ' xlat
                opCodeASM = "XLAT"
                clkCycDecoder += 4

            Case &HD7 ' xlatb
                opCodeASM = "XLATB"
                clkCycDecoder += 11

            Case &HD9 ' fnstsw (required for BIOS to boot)?
                If ParamNOPS(SelPrmIndex.First, , DataSize.Byte) = &H3C Then
                    opCodeASM = "FNSTSW {NOT IMPLEMENTED}"
                Else
                    opCodeASM = decOpCode.ToString("X2") + " {NOT IMPLEMENTED}"
                End If
                opCodeSize += 1

            Case &HDB ' fninit (required for BIOS to boot)?
                If ParamNOPS(SelPrmIndex.First, , DataSize.Byte) = &HE3 Then
                    opCodeASM = "FNINIT {NOT IMPLEMENTED}"
                Else
                    opCodeASM = decOpCode.ToString("X2") + " {NOT IMPLEMENTED}"
                End If
                opCodeSize += 1

            Case &HE0 ' loopne
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "LOOPNE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mRegisters.CX <> 0 AndAlso mFlags.ZF = 0, 19, 5)

            Case &HE1 ' loope
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "LOOPE " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mRegisters.CX <> 0 AndAlso mFlags.ZF = 1, 18, 6)

            Case &HE2 ' loop
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "LOOP " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mRegisters.CX <> 0, 17, 5)

            Case &HE3 ' jcxz
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JCXZ " + ipAddrOffDecoder.ToString("X4")
                clkCycDecoder += If(mRegisters.CX = 0, 18, 6)

            Case &HE4 ' in to al from fixed port
                opCodeASM = "IN AL, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 10

            Case &HE5 ' inw to ax from fixed port
                opCodeASM = "IN AX, " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2")
                clkCycDecoder += 10

            Case &HE6  ' out to al to fixed port
                opCodeASM = "OUT " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2") + ", AL"
                clkCycDecoder += 10

            Case &HE7  ' outw to ax to fixed port
                opCodeASM = "OUT " + Param(SelPrmIndex.First, , DataSize.Byte).ToString("X2") + ", Ax"
                clkCycDecoder += 10

            Case &HE8 ' call direct within segment
                opCodeASM = "CALL " + OffsetIP(DataSize.Word).ToString("X4")
                clkCycDecoder += 19

            Case &HE9 ' jmp direct within segment
                opCodeASM = "JMP " + OffsetIP(DataSize.Word).ToString("X4")
                clkCycDecoder += 15

            Case &HEA ' jmp direct intersegment
                opCodeASM = "JMP " + Param(SelPrmIndex.Second, , DataSize.Word).ToString("X4") + ":" + Param(SelPrmIndex.First, , DataSize.Word).ToString("X4")
                clkCycDecoder += 15

            Case &HEB ' jmp direct within segment short
                ipAddrOffDecoder = OffsetIP(DataSize.Byte)
                opCodeASM = "JMP " + ipAddrOffDecoder.ToString("X2")
                clkCycDecoder += 15

            Case &HEC  ' in to al from variable port
                opCodeASM = "IN AL, DX"
                clkCycDecoder += 8

            Case &HED ' inw to ax from variable port
                opCodeASM = "IN AX, DX"
                clkCycDecoder += 8

            Case &HEE ' out to port dx from al
                opCodeASM = "OUT DX, AL"
                clkCycDecoder += 8

            Case &HEF ' out to port dx from ax
                opCodeASM = "OUT DX, AX"
                clkCycDecoder += 8

            Case &HF0 ' lock
                opCodeASM = "LOCK"
                clkCycDecoder += 2

            Case &HF2 ' repne/repnz
                opCodeASM = "REPNE"
                clkCycDecoder += 2

            Case &HF3 ' rep/repe
                opCodeASM = "REPE"
                clkCycDecoder += 2

            Case &HF4 ' hlt
                opCodeASM = "HLT"
                clkCycDecoder += 2

            Case &HF5 ' cmc
                opCodeASM = "CMC"
                clkCycDecoder += 2

            Case &HF6 To &HF7 : DecodeGroup3()

            Case &HF8 ' clc
                opCodeASM = "CLC"
                clkCycDecoder += 2

            Case &HF9 ' stc
                opCodeASM = "STC"
                clkCycDecoder += 2

            Case &HFA ' cli
                opCodeASM = "CLI"
                clkCycDecoder += 2

            Case &HFB ' sti
                opCodeASM = "STI"
                clkCycDecoder += 2

            Case &HFC ' cld
                opCodeASM = "CLD"
                clkCycDecoder += 2

            Case &HFD ' std
                opCodeASM = "STD"
                clkCycDecoder += 2

            Case &HFE To &HFF : DecodeGroup4_And_5()

            Case Else
                opCodeASM = decOpCode.ToString("X2") + ": {NOT IMPLEMENTED}"
        End Select

        If opCodeSize = 0 Then
            Throw New Exception("Decoding error for decOpCode " + decOpCode.ToString("X2"))
        End If

        Dim info As Instruction = New Instruction With {
            .IsValid = True,
            .decOpCode = decOpCode,
            .CS = mRegisters.CS,
            .IP = mRegisters.IP,
            .Size = opCodeSize,
            .JumpAddress = ipAddrOffDecoder,
            .IndMemoryData = addrMode.IndMem,
            .IndAddress = addrMode.IndAdr,
            .ClockCycles = clkCycDecoder,
            .SegmentOverride = segOvr
        }

        If opCodeASM <> "" Then
            If opCodeSize > 0 Then
                ReDim info.Bytes(opCodeSize - 1)
                info.Bytes(0) = decOpCode
            End If
            For i As Integer = 1 To opCodeSize - 1
                info.Bytes(i) = ParamNOPS(SelPrmIndex.First, i, DataSize.Byte)
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
        clkCycDecoder += opCodeSize * 4

        mRegisters.CS = CS
        mRegisters.IP = IP

        If activeSegmentChanged Then
            mRegisters.ActiveSegmentRegister = activeSegment
        Else
            mRegisters.ResetActiveSegment()
        End If

        isDecoding = False

        Return info
    End Function

    Private Sub DecodeGroup1()
        SetDecoderAddressing()
        Dim paramSize As DataSize = IIf(decOpCode = &H81, DataSize.Word, DataSize.Byte)
        Select Case addrMode.Reg
            Case 0 ' 000    --   add imm to reg/mem
                opCodeASM = "ADD"
                clkCycDecoder += If(addrMode.IsDirect, 4, 17)
            Case 1 ' 001    --  or imm to reg/mem
                opCodeASM = "OR"
                clkCycDecoder += If(addrMode.IsDirect, 4, 17)
            Case 2 ' 010    --  adc imm to reg/mem
                opCodeASM = "ADC"
                clkCycDecoder += If(addrMode.IsDirect, 4, 17)
            Case 3 ' 011    --  sbb imm from reg/mem
                opCodeASM = "SBB"
                clkCycDecoder += If(addrMode.IsDirect, 4, 17)
            Case 4 ' 100    --  and imm to reg/mem
                opCodeASM = "AND"
                clkCycDecoder += If(addrMode.IsDirect, 4, 17)
            Case 5 ' 101    --  sub imm from reg/mem
                opCodeASM = "SUB"
                clkCycDecoder += If(addrMode.IsDirect, 4, 17)
            Case 6 ' 110    --  xor imm to reg/mem
                opCodeASM = "XOR"
                clkCycDecoder += If(addrMode.IsDirect, 4, 17)
            Case 7 ' 111    --  cmp imm with reg/mem
                opCodeASM = "CMP"
                clkCycDecoder += If(addrMode.IsDirect, 4, 10)
        End Select
        If addrMode.IsDirect Then
            opCodeASM += " " + addrMode.Register2.ToString() + ", " + Param(SelPrmIndex.First, opCodeSize, paramSize).ToHex(paramSize)
        Else
            opCodeASM += " " + indASM + ", " + Param(SelPrmIndex.First, opCodeSize, paramSize).ToHex(paramSize)
        End If
    End Sub

    Private Sub DecodeGroup2()
        SetDecoderAddressing()

        If addrMode.IsDirect Then
            If decOpCode >= &HD2 Then
                opCodeASM = addrMode.Register2.ToString() + ", CL"
                clkCycDecoder += 8 + 4 '* count
            Else
                opCodeASM = addrMode.Register2.ToString() + ", 1"
                clkCycDecoder += 2
            End If
        Else
            If (decOpCode And &H2) = &H2 Then
                opCodeASM = indASM + ", CL"
                clkCycDecoder += 20 + 4 '* count
            Else
                opCodeASM = indASM + ", 1"
                clkCycDecoder += 15
            End If
        End If

        Select Case addrMode.Reg
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
        Select Case addrMode.Reg
            Case 0 ' 000    --  test
                If addrMode.IsDirect Then
                    opCodeASM = "TEST " + addrMode.Register2.ToString() + ", " + Param(SelPrmIndex.First, opCodeSize).ToHex(addrMode.Size)
                    clkCycDecoder += 5
                Else
                    opCodeASM = "TEST " + indASM + ", " + Param(SelPrmIndex.First, opCodeSize).ToHex(addrMode.Size)
                    clkCycDecoder += 11
                End If
            Case 2 ' 010    --  not
                If addrMode.IsDirect Then
                    opCodeASM = "NOT " + addrMode.Register2.ToString()
                    clkCycDecoder += 3
                Else
                    opCodeASM = "NOT " + indASM
                    clkCycDecoder += 16
                End If
            Case 3 ' 010    --  neg
                If addrMode.IsDirect Then
                    opCodeASM = "NEG " + addrMode.Register2.ToString()
                    clkCycDecoder += 3
                Else
                    opCodeASM = "NEG " + indASM
                    clkCycDecoder += 16
                End If

            Case 4 ' 100    --  mul
                If addrMode.IsDirect Then
                    opCodeASM = "MUL " + addrMode.Register2.ToString()
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 70, 118)
                Else
                    opCodeASM = "MUL " + indASM
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 76, 124)
                End If
            Case 5 ' 101    --  imul
                If addrMode.IsDirect Then
                    opCodeASM = "IMUL " + addrMode.Register2.ToString()
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 80, 128)
                Else
                    opCodeASM = "IMUL " + indASM
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 86, 134)
                End If

            Case 6 ' 110    --  div
                If addrMode.IsDirect Then
                    opCodeASM = "DIV " + addrMode.Register2.ToString()
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 80, 144)
                Else
                    opCodeASM = "DIV " + indASM
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 86, 168)
                End If

            Case 7 ' 111    --  idiv
                Dim div As Integer = mRegisters.Val(addrMode.Register2)
                If addrMode.IsDirect Then
                    opCodeASM = "IDIV " + addrMode.Register2.ToString()
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 101, 165)
                Else
                    opCodeASM = "IDIV " + indASM
                    clkCycDecoder += If(addrMode.Size = DataSize.Byte, 107, 171)
                End If
        End Select
    End Sub

    Private Sub DecodeGroup4_And_5()
        SetDecoderAddressing()
        Select Case addrMode.Reg
            Case 0 ' 000    --  inc reg/mem
                If addrMode.IsDirect Then
                    opCodeASM = "INC " + addrMode.Register2.ToString()
                    clkCycDecoder += 3
                Else
                    opCodeASM = "INC " + indASM
                    clkCycDecoder += 15
                End If

            Case 1 ' 001    --  dec reg/mem
                If addrMode.IsDirect Then
                    opCodeASM = "DEC " + addrMode.Register2.ToString()
                    clkCycDecoder += 3
                Else
                    opCodeASM = "DEC " + indASM
                    clkCycDecoder += 15
                End If

            Case 2 ' 010    --  call indirect within segment
                If addrMode.IsDirect Then
                    opCodeASM = "CALL " + addrMode.Register2.ToString()
                Else
                    opCodeASM = "CALL " + indASM
                End If
                clkCycDecoder += 11

            Case 3 ' 011    --  call indirect intersegment
                If addrMode.IsDirect Then
                    opCodeASM = "CALL " + addrMode.Register2.ToString() + " {NOT IMPLEMENTED}"
                Else
                    opCodeASM = "CALL " + indASM
                End If

                clkCycDecoder += 37

            Case 4 ' 100    --  jmp indirect within segment
                If addrMode.IsDirect Then
                    opCodeASM = "JMP " + addrMode.Register2.ToString()
                Else
                    opCodeASM = "JMP " + indASM
                End If
                clkCycDecoder += 15

            Case 5 ' 101    --  jmp indirect intersegment
                If addrMode.IsDirect Then
                    opCodeASM = "JMP " + addrMode.Register2.ToString() + " {NOT IMPLEMENTED}"
                Else
                    opCodeASM = "JMP " + indASM
                End If
                clkCycDecoder += 24

            Case 6 ' 110    --  push reg/mem
                opCodeASM = "PUSH " + indASM
                clkCycDecoder += 16

            Case 7 ' 111    --  BIOS DI
                opCodeASM = "BIOS DI"
                opCodeSize = 2
                clkCycDecoder += 0
        End Select
    End Sub

    Private Sub SetDecoderAddressing(Optional forceSize As DataSize = DataSize.UseAddressingMode)
        addrMode.Decode(decOpCode, ParamNOPS(SelPrmIndex.First, , DataSize.Byte))

        If forceSize <> DataSize.UseAddressingMode Then addrMode.Size = forceSize

        ' AS = SS when Rm = 2 or 3
        ' If Rm = 6, AS will be set to SS, except for Modifier = 0
        ' http://www.ic.unicamp.br/~celio/mc404s2-03/addr_modes/intel_addr.html
        If (Not mRegisters.ActiveSegmentChanged) AndAlso (addrMode.Modifier <> 3) AndAlso
                (
                    addrMode.Rm = 2 OrElse
                    addrMode.Rm = 3 OrElse
                    (addrMode.Rm = 6 AndAlso addrMode.Modifier <> 0)
                ) Then
            mRegisters.ActiveSegmentRegister = GPRegisters.RegistersTypes.SS
            clkCyc += 2
        End If

        ' http://umcs.maine.edu/~cmeadow/courses/cos335/Asm07-MachineLanguage.pdf
        ' http://maven.smith.edu/~thiebaut/ArtOfAssembly/CH04/CH04-2.html#HEADING2-35
        Select Case addrMode.Modifier
            Case 0 ' 00
                addrMode.IsDirect = False
                Select Case addrMode.Rm
                    Case 0 : addrMode.IndAdr = mRegisters.BX + mRegisters.SI : indASM = "[BX + SI]" : clkCyc += 7 ' 000 [BX+SI]
                    Case 1 : addrMode.IndAdr = mRegisters.BX + mRegisters.DI : indASM = "[BX + DI]" : clkCyc += 8 ' 001 [BX+DI]
                    Case 2 : addrMode.IndAdr = mRegisters.BP + mRegisters.SI : indASM = "[BP + SI]" : clkCyc += 8 ' 010 [BP+SI]
                    Case 3 : addrMode.IndAdr = mRegisters.BP + mRegisters.DI : indASM = "[BP + DI]" : clkCyc += 7 ' 011 [BP+DI]
                    Case 4 : addrMode.IndAdr = mRegisters.SI : indASM = "[SI]" : clkCyc += 5                                               ' 100 [SI]
                    Case 5 : addrMode.IndAdr = mRegisters.DI : indASM = "[DI]" : clkCyc += 5                                               ' 101 [DI]
                    Case 6                                                                                                                 ' 110 Direct Addressing
                        addrMode.IndAdr = ParamNOPS(SelPrmIndex.First, 2, DataSize.Word)
                        indASM = "[" + ParamNOPS(SelPrmIndex.First, 2, DataSize.Word).ToString("X4") + "]"
                        opCodeSize += 2
                        clkCyc += 9
                    Case 7 : addrMode.IndAdr = mRegisters.BX : indASM = "[BX]" : clkCyc += 5                                               ' 111 [BX]
                End Select
                addrMode.IndMem = RAMn

            Case 1 ' 01 - 8bit
                addrMode.IsDirect = False
                Select Case addrMode.Rm
                    Case 0 : addrMode.IndAdr = mRegisters.BX + mRegisters.SI : indASM = "[BX + SI]" : clkCyc += 7 ' 000 [BX+SI]
                    Case 1 : addrMode.IndAdr = mRegisters.BX + mRegisters.DI : indASM = "[BX + DI]" : clkCyc += 8 ' 001 [BX+DI]
                    Case 2 : addrMode.IndAdr = mRegisters.BP + mRegisters.SI : indASM = "[BP + SI]" : clkCyc += 8 ' 010 [BP+SI]
                    Case 3 : addrMode.IndAdr = mRegisters.BP + mRegisters.DI : indASM = "[BP + DI]" : clkCyc += 7 ' 011 [BP+DI]
                    Case 4 : addrMode.IndAdr = mRegisters.SI : indASM = "[SI]" : clkCyc += 5                                               ' 100 [SI]
                    Case 5 : addrMode.IndAdr = mRegisters.DI : indASM = "[DI]" : clkCyc += 5                                               ' 101 [DI]
                    Case 6 : addrMode.IndAdr = mRegisters.BP : indASM = "[BP]" : clkCyc += 5                                               ' 110 [BP]
                    Case 7 : addrMode.IndAdr = mRegisters.BX : indASM = "[BX]" : clkCyc += 5                                               ' 111 [BX]
                End Select

                Dim p As Byte = ParamNOPS(SelPrmIndex.First, 2, DataSize.Byte)
                Dim s As Integer
                If p > &H80 Then
                    p = &H100 - p
                    s = -1
                Else
                    s = 1
                End If
                indASM = indASM.Replace("]", If(s = -1, " - ", " + ") + p.ToString("X2") + "]")
                addrMode.IndAdr += s * p
                addrMode.IndMem = RAMn
                opCodeSize += 1

            Case 2 ' 10 - 16bit
                addrMode.IsDirect = False
                Select Case addrMode.Rm
                    Case 0 : addrMode.IndAdr = mRegisters.BX + mRegisters.SI : indASM = "[BX + SI]" : clkCyc += 7 ' 000 [BX+SI]
                    Case 1 : addrMode.IndAdr = mRegisters.BX + mRegisters.DI : indASM = "[BX + DI]" : clkCyc += 8 ' 001 [BX+DI]
                    Case 2 : addrMode.IndAdr = mRegisters.BP + mRegisters.SI : indASM = "[BP + SI]" : clkCyc += 8 ' 010 [BP+SI]
                    Case 3 : addrMode.IndAdr = mRegisters.BP + mRegisters.DI : indASM = "[BP + DI]" : clkCyc += 7 ' 011 [BP+DI]
                    Case 4 : addrMode.IndAdr = mRegisters.SI : indASM = "[SI]" : clkCyc += 5                                               ' 100 [SI]
                    Case 5 : addrMode.IndAdr = mRegisters.DI : indASM = "[DI]" : clkCyc += 5                                               ' 101 [DI]
                    Case 6 : addrMode.IndAdr = mRegisters.BP : indASM = "[BP]" : clkCyc += 5                                               ' 110 [BP]
                    Case 7 : addrMode.IndAdr = mRegisters.BX : indASM = "[BX]" : clkCyc += 5                                               ' 111 [BX]
                End Select

                indASM = indASM.Replace("]", " + " + ParamNOPS(SelPrmIndex.First, 2, DataSize.Word).ToString("X4") + "]")
                addrMode.IndAdr += ParamNOPS(SelPrmIndex.First, 2, DataSize.Word)
                addrMode.IndMem = RAMn
                opCodeSize += 2

            Case 3 ' 11
                addrMode.IsDirect = True

        End Select
        opCodeSize += 1
    End Sub
End Class
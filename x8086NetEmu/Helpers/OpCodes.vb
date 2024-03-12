Partial Public Class X8086
    Private Delegate Sub ExecOpcode()
    Private opCodes() As ExecOpcode = {
        AddressOf _00_03,' ADD Eb Gb
AddressOf _00_03,' ADD Ev Gv
AddressOf _00_03,' ADD Gb Eb
AddressOf _00_03,' ADD Gv Ev
AddressOf _04,' ADD AL Ib
AddressOf _05,' ADD AX Iv
AddressOf _06,' PUSH ES
AddressOf _07,' POP ES
AddressOf _08_0B,' OR Eb Gb
AddressOf _08_0B,' OR Ev Gv
AddressOf _08_0B,' OR Gb Eb
AddressOf _08_0B,' OR Gv Ev
AddressOf _0C,' OR AL Ib
AddressOf _0D,' OR AX Iv
AddressOf _0E,' PUSH CS
AddressOf _0F,' POP CS
AddressOf _10_13,' ADC Eb Gb
AddressOf _10_13,' ADC Ev Gv
AddressOf _10_13,' ADC Gb Eb
AddressOf _10_13,' ADC Gv Ev
AddressOf _14,' ADC AL Ib
AddressOf _15,' ADC AX Iv
AddressOf _16,' PUSH SS
AddressOf _17,' POP SS
AddressOf _18_1B,' SBB Eb Gb
AddressOf _18_1B,' SBB Ev Gv
AddressOf _18_1B,' SBB Gb Eb
AddressOf _18_1B,' SBB Gv Ev
AddressOf _1C,' SBB AL Ib
AddressOf _1D,' SBB AX Iv
AddressOf _1E,' PUSH DS
AddressOf _1F,' POP DS
AddressOf _20_23,' AND Eb Gb
AddressOf _20_23,' AND Ev Gv
AddressOf _20_23,' AND Gb Eb
AddressOf _20_23,' AND Gv Ev
AddressOf _24,' AND AL Ib
AddressOf _25,' AND AX Iv
AddressOf _26_2E_36_3E,' ES, CS, SS and DS segment override prefix
AddressOf _27,' DAA
AddressOf _28_2B,' SUB Eb Gb
AddressOf _28_2B,' SUB Ev Gv
AddressOf _28_2B,' SUB Gb Eb
AddressOf _28_2B,' SUB Gv Ev
AddressOf _2C,' SUB AL Ib
AddressOf _2D,' SUB AX, Iv
AddressOf _26_2E_36_3E,' ES, CS, SS and DS segment override prefix
AddressOf _2F,' DAS
AddressOf _30_33,' XOR Eb Gb
AddressOf _30_33,' XOR Ev Gv
AddressOf _30_33,' XOR Gb Eb
AddressOf _30_33,' XOR Gv Ev
AddressOf _34,' XOR AL Ib
AddressOf _35,' XOR AX Iv
AddressOf _26_2E_36_3E,' ES, CS, SS and DS segment override prefix
AddressOf _37,' AAA
AddressOf _38_3B,' CMP Eb Gb
AddressOf _38_3B,' CMP Ev Gv
AddressOf _38_3B,' CMP Gb Eb
AddressOf _38_3B,' CMP Gv Ev
AddressOf _3C,' CMP AL Ib
AddressOf _3D,' CMP AX Iv
AddressOf _26_2E_36_3E,' ES, CS, SS and DS segment override prefix
AddressOf _3F,' AAS
AddressOf _40_47,' INC AX
AddressOf _40_47,' INC CX
AddressOf _40_47,' INC DX
AddressOf _40_47,' INC BX
AddressOf _40_47,' INC SP
AddressOf _40_47,' INC BP
AddressOf _40_47,' INC SI
AddressOf _40_47,' INC DI
AddressOf _48_4F,' DEC AX
AddressOf _48_4F,' DEC CX
AddressOf _48_4F,' DEC DX
AddressOf _48_4F,' DEC BX
AddressOf _48_4F,' DEC SP
AddressOf _48_4F,' DEC BP
AddressOf _48_4F,' DEC SI
AddressOf _48_4F,' DEC DI
AddressOf _50_57,' PUSH AX
AddressOf _50_57,' PUSH CX
AddressOf _50_57,' PUSH DX
AddressOf _50_57,' PUSH BX
AddressOf _50_57,' PUSH SP
AddressOf _50_57,' PUSH BP
AddressOf _50_57,' PUSH SI
AddressOf _50_57,' PUSH DI
AddressOf _58_5F,' POP AX
AddressOf _58_5F,' POP CX
AddressOf _58_5F,' POP DX
AddressOf _58_5F,' POP BX
AddressOf _58_5F,' POP SP
AddressOf _58_5F,' POP BP
AddressOf _58_5F,' POP SI
AddressOf _58_5F,' POP DI
AddressOf _60,' PUSHA (80186)
AddressOf _61,' POPA (80186)
AddressOf _62,' BOUND (80186)
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf _68,' PUSH Iv (80186)
AddressOf _69,' IMUL (80186)
AddressOf _6A,' PUSH Ib (80186)
AddressOf _6B,' IMUL (80186)
AddressOf _6C_6F,' Ignore 80186/V20 port operations... for now...
AddressOf _6C_6F,' Ignore 80186/V20 port operations... for now...
AddressOf _6C_6F,' Ignore 80186/V20 port operations... for now...
AddressOf _6C_6F,' Ignore 80186/V20 port operations... for now...
AddressOf _70,' JO Jb
AddressOf _71,' JNO  Jb
AddressOf _72,' JB/JNAE/JC Jb
AddressOf _73,' JNB/JAE/JNC Jb
AddressOf _74,' JZ/JE Jb
AddressOf _75,' JNZ/JNE Jb
AddressOf _76,' JBE/JNA Jb
AddressOf _77,' JA/JNBE Jb
AddressOf _78,' JS Jb
AddressOf _79,' JNS Jb
AddressOf _7A,' JPE/JP Jb
AddressOf _7B,' JPO/JNP Jb
AddressOf _7C,' JL/JNGE Jb
AddressOf _7D,' JGE/JNL Jb
AddressOf _7E,' JLE/JNG Jb
AddressOf _7F,' JG/JNLE Jb
AddressOf _80_83,
AddressOf _80_83,
AddressOf _80_83,
AddressOf _80_83,
AddressOf _84_85,' TEST Gb Eb
AddressOf _84_85,' TEST Gv Ev
AddressOf _86_87,' XCHG Gb Eb
AddressOf _86_87,' XCHG Gv Ev
AddressOf _88_8B,' MOV Eb Gb
AddressOf _88_8B,' MOV Ev Gv
AddressOf _88_8B,' MOV Gb Eb
AddressOf _88_8B,' MOV Gv Ev
AddressOf _8C,' MOV Ew Sw
AddressOf _8D,' LEA Gv M
AddressOf _8E,' MOV Sw Ew
AddressOf _8F,' POP Ev
AddressOf _90,' NOP
AddressOf _91,' XCHG CX AX
AddressOf _92,' XCHG DX AX
AddressOf _93,' XCHG BX AX
AddressOf _94,' XCHG SP AX
AddressOf _95,' XCHG BP AX
AddressOf _96,' XCHG SI AX
AddressOf _97,' XCHG DI AX
AddressOf _98,' CBW
AddressOf _99,' CWD
AddressOf _9A,' CALL Ap
AddressOf _9B,' WAIT
AddressOf _9C,' PUSHF
AddressOf _9D,' POPF
AddressOf _9E,' SAHF
AddressOf _9F,' LAHF
AddressOf _A0,' MOV AL Ob
AddressOf _A1,' MOV AX Ov
AddressOf _A2,' MOV Ob AL
AddressOf _A3,' MOV Ov AX
AddressOf _A4_A7,
AddressOf _A4_A7,
AddressOf _A4_A7,
AddressOf _A4_A7,
AddressOf _A8,' TEST AL Ib
AddressOf _A9,' TEST AX Iv
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _B0,' MOV AL Ib
AddressOf _B1,' MOV CL Ib
AddressOf _B2,' MOV DL Ib
AddressOf _B3,' MOV BL Ib
AddressOf _B4,' MOV AH Ib
AddressOf _B5,' MOV CH Ib
AddressOf _B6,' MOV DH Ib
AddressOf _B7,' MOV BH Ib
AddressOf _B8,' MOV AX Ib
AddressOf _B9,' MOV CX Ib
AddressOf _BA,' MOV DX Ib
AddressOf _BB,' MOV BX Ib
AddressOf _BC,' MOV SP Ib
AddressOf _BD,' MOV BP Ib
AddressOf _BE,' MOV SI Ib
AddressOf _BF,' MOV DI Ib
AddressOf _C0_C1,' GRP2 byte/word imm8/16 ??? (80186)
AddressOf _C0_C1,' GRP2 byte/word imm8/16 ??? (80186)
AddressOf _C2,' RET Iw
AddressOf _C3,' RET
AddressOf _C4,' LES Gv Mp
AddressOf _C5,' LDS Gv Mp
AddressOf _C6_C7,' MOV Eb Ib
AddressOf _C6_C7,' MOV MOV Ev Iv
AddressOf _C8,' ENTER (80186)
AddressOf _C9,' LEAVE (80186)
AddressOf _CA,' RETF Iw
AddressOf _CB,' RETF
AddressOf _CC,' INT 3
AddressOf _CD,' INT Ib
AddressOf _CE,' INTO
AddressOf _CF,' IRET
AddressOf _D0_D3,
AddressOf _D0_D3,
AddressOf _D0_D3,
AddressOf _D0_D3,
AddressOf _D4,' AAM I0
AddressOf _D5,' AAD I0
AddressOf _D6,' XLAT for V20 / SALC
AddressOf _D7,' XLATB
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _D8_DF,' Ignore 8087 co-processor instructions
AddressOf _E0,' LOOPNE/LOOPNZ
AddressOf _E1,' LOOPE/LOOPZ
AddressOf _E2,' LOOP
AddressOf _E3,' JCXZ/JECXZ
AddressOf _E4,' IN AL Ib
AddressOf _E5,' IN AX Ib
AddressOf _E6,' OUT Ib AL
AddressOf _E7,' OUT Ib AX
AddressOf _E8,' CALL Jv
AddressOf _E9,' JMP Jv
AddressOf _EA,' JMP Ap
AddressOf _EB,' JMP Jb
AddressOf _EC,' IN AL DX
AddressOf _ED,' IN AX DX
AddressOf _EE,' OUT DX AL
AddressOf _EF,' OUT DX AX
AddressOf _F0,' LOCK
AddressOf OpCodeNotImplemented,
AddressOf _F2,' REPBE/REPNZ
AddressOf _F3,' REPE/REPZ
AddressOf _F4,' HLT
AddressOf _F5,' CMC
AddressOf _F6_F7,
AddressOf _F6_F7,
AddressOf _F8,' CLC
AddressOf _F9,' STC
AddressOf _FA,' CLI
AddressOf _FB,' STI
AddressOf _FC,' CLD
AddressOf _FD,' STD
AddressOf _FE_FF,
AddressOf _FE_FF}

    Private Sub _00_03() ' ADD Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Add, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Add, addrMode.Size)
            clkCyc += 16
        Else
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.Add, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _04() ' ADD AL Ib
        mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Add, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _05() ' ADD AX Iv
        mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Add, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _06() ' PUSH ES
        PushIntoStack(mRegisters.ES)
        clkCyc += 10
    End Sub

    Private Sub _07() ' POP ES
        mRegisters.ES = PopFromStack()
        ignoreINTs = True
        clkCyc += 8
    End Sub

    Private Sub _08_0B() ' OR Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.LogicOr, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.LogicOr, addrMode.Size)
            clkCyc += 16
        Else
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.LogicOr, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _0C() ' OR AL Ib
        mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicOr, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _0D() ' OR AX Iv
        mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicOr, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _0E() ' PUSH CS
        PushIntoStack(mRegisters.CS)
        clkCyc += 10
    End Sub

    Private Sub _0F() ' POP CS
        If mVic20 Then
            PopFromStack()
        Else
            mRegisters.CS = PopFromStack()
        End If
        ignoreINTs = True
        clkCyc += 8
    End Sub

    Private Sub _10_13() ' ADC Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.AddWithCarry, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.AddWithCarry, addrMode.Size)
            clkCyc += 16
        Else
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.AddWithCarry, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _14() ' ADC AL Ib
        mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.AddWithCarry, DataSize.Byte)
        clkCyc += 3
    End Sub

    Private Sub _15() ' ADC AX Iv
        mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.AddWithCarry, DataSize.Word)
        clkCyc += 3
    End Sub

    Private Sub _16() ' PUSH SS
        PushIntoStack(mRegisters.SS)
        clkCyc += 10
    End Sub

    Private Sub _17() ' POP SS
        mRegisters.SS = PopFromStack()
        ' Lesson 4: http://ntsecurity.nu/onmymind/2007/2007-08-22.html
        ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
        ' http://www.vcfed.org/forum/archive/index.php/t-41453.html
        ignoreINTs = True
        clkCyc += 8
    End Sub

    Private Sub _18_1B() ' SBB Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.SubstractWithCarry, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.SubstractWithCarry, addrMode.Size)
            clkCyc += 16
        Else
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.SubstractWithCarry, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _1C() ' SBB AL Ib
        mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.SubstractWithCarry, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _1D() ' SBB AX Iv
        mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.SubstractWithCarry, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _1E() ' PUSH DS
        PushIntoStack(mRegisters.DS)
        clkCyc += 10
    End Sub

    Private Sub _1F() ' POP DS
        mRegisters.DS = PopFromStack()
        ignoreINTs = True
        clkCyc += 8
    End Sub

    Private Sub _20_23() ' AND Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.LogicAnd, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.LogicAnd, addrMode.Size)
            clkCyc += 16
        Else
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.LogicAnd, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _24() ' AND AL Ib
        mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicAnd, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _25() ' AND AX Iv
        mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicAnd, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _26_2E_36_3E() ' ES, CS, SS and DS segment override prefix
        addrMode.Decode(opCode, opCode)
        mRegisters.ActiveSegmentRegister = addrMode.Dst - GPRegisters.RegistersTypes.AH + GPRegisters.RegistersTypes.ES
        newPrefix = True
        clkCyc += 2
    End Sub

    Private Sub _27() ' DAA
        If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
            tmpUVal1 = CUInt(mRegisters.AL) + 6
            mRegisters.AL += 6
            mFlags.AF = 1
            mFlags.CF = mFlags.CF Or If((tmpUVal1 And &HFF00) = 0, 0, 1)
        Else
            mFlags.AF = 0
        End If
        If (mRegisters.AL And &HF0) > &H90 OrElse mFlags.CF = 1 Then
            tmpUVal1 = CUInt(mRegisters.AL) + &H60
            mRegisters.AL += &H60
            mFlags.CF = 1
        Else
            mFlags.CF = 0
        End If
        SetSZPFlags(tmpUVal1, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _28_2B() ' SUB Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Substract, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Substract, addrMode.Size)
            clkCyc += 16
        Else
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.Substract, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _2C() ' SUB AL Ib
        mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Substract, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _2D() ' SUB AX, Iv
        mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Substract, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _2F() ' DAS
        tmpUVal2 = mRegisters.AL
        If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
            tmpUVal1 = mRegisters.AL - 6
            mRegisters.AL -= 6
            mFlags.AF = 1
            mFlags.CF = mFlags.CF Or If((tmpUVal1 And &HFF00) = 0, 0, 1)
        Else
            mFlags.AF = 0
        End If
        If tmpUVal2 > &H99 OrElse mFlags.CF = 1 Then
            tmpUVal1 = mRegisters.AL - &H60
            mRegisters.AL -= &H60
            mFlags.CF = 1
        Else
            mFlags.CF = 0
        End If
        SetSZPFlags(tmpUVal1, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _30_33() ' XOR Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.LogicXor, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.LogicXor, addrMode.Size)
            clkCyc += 16
        Else
            mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.LogicXor, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _34() ' XOR AL Ib
        mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicXor, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _35() ' XOR AX Iv
        mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicXor, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _37() ' AAA
        If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
            mRegisters.AX += &H106
            mFlags.AF = 1
            mFlags.CF = 1
        Else
            mFlags.AF = 0
            mFlags.CF = 0
        End If
        mRegisters.AL = mRegisters.AL.LowNib()
        clkCyc += 8
    End Sub

    Private Sub _38_3B() ' CMP Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Compare, addrMode.Size)
            clkCyc += 3
        ElseIf addrMode.Direction = 0 Then
            Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Compare, addrMode.Size)
            clkCyc += 9
        Else
            Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.Compare, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _3C() ' CMP AL Ib
        Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Compare, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _3D() ' CMP AX Iv
        Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Compare, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _3F() ' AAS
        If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
            mRegisters.AX -= &H106
            mFlags.AF = 1
            mFlags.CF = 1
        Else
            mFlags.AF = 0
            mFlags.CF = 0
        End If
        mRegisters.AL = mRegisters.AL.LowNib()
        clkCyc += 8
    End Sub

    Private Sub _40_47() ' INC DI
        SetRegister1Alt(opCode)
        mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Increment, DataSize.Word)
        clkCyc += 3
    End Sub

    Private Sub _48_4F() ' DEC DI
        SetRegister1Alt(opCode)
        mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Decrement, DataSize.Word)
        clkCyc += 3
    End Sub

    Private Sub _50_57() ' PUSH DI
        If opCode = &H54 Then ' SP
            ' The 8086/8088 pushes the value of SP after it has been decremented
            ' http://css.csail.mit.edu/6.858/2013/readings/i386/s15_06.htm
            PushIntoStack(mRegisters.SP - 2)
        Else
            SetRegister1Alt(opCode)
            PushIntoStack(mRegisters.Val(addrMode.Register1))
        End If
        clkCyc += 11
    End Sub

    Private Sub _58_5F() ' POP DI
        SetRegister1Alt(opCode)
        mRegisters.Val(addrMode.Register1) = PopFromStack()
        clkCyc += 8
    End Sub

    Private Sub _60() ' PUSHA (80186)
        If mVic20 Then
            tmpUVal1 = mRegisters.SP
            PushIntoStack(mRegisters.AX)
            PushIntoStack(mRegisters.CX)
            PushIntoStack(mRegisters.DX)
            PushIntoStack(mRegisters.BX)
            PushIntoStack(tmpUVal1)
            PushIntoStack(mRegisters.BP)
            PushIntoStack(mRegisters.SI)
            PushIntoStack(mRegisters.DI)
            clkCyc += 19
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _61() ' POPA (80186)
        If mVic20 Then
            mRegisters.DI = PopFromStack()
            mRegisters.SI = PopFromStack()
            mRegisters.BP = PopFromStack()
            PopFromStack() ' SP
            mRegisters.BX = PopFromStack()
            mRegisters.DX = PopFromStack()
            mRegisters.CX = PopFromStack()
            mRegisters.AX = PopFromStack()
            clkCyc += 19
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _62() ' BOUND (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            SetAddressing()
            If To32bitsWithSign(mRegisters.Val(addrMode.Register1)) < RAM16(addrMode.IndAdr >> 4, addrMode.IndAdr And 15) Then
                HandleInterrupt(5, False)
            Else
                addrMode.IndAdr += 2
                If To32bitsWithSign(mRegisters.Val(addrMode.Register1)) < RAM16(addrMode.IndAdr >> 4, addrMode.IndAdr And 15) Then
                    HandleInterrupt(5, False)
                End If
            End If
            clkCyc += 34
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _68() ' PUSH Iv (80186)
        ' PRE ALPHA CODE - UNTESTED
        If mVic20 Then
            PushIntoStack(Param(ParamIndex.First, , DataSize.Word))
            clkCyc += 3
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _69() ' IMUL (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            SetAddressing()
            Dim tmp1 As UInt32 = mRegisters.Val(addrMode.Register1)
            Dim tmp2 As UInt32 = Param(ParamIndex.First, , DataSize.Word)
            If (tmp1 And &H8000) = &H8000 Then tmp1 = tmp1 Or &HFFFF_0000UI
            If (tmp2 And &H8000) = &H8000 Then tmp2 = tmp2 Or &HFFFF_0000UI
            Dim tmp3 As UInt32 = tmp1 * tmp2
            mRegisters.Val(addrMode.Register1) = tmp3 And &HFFFF
            If (tmp3 And &HFFFF_0000UI) <> 0 Then
                mFlags.CF = 1
                mFlags.OF = 1
            Else
                mFlags.CF = 0
                mFlags.OF = 0
            End If
            clkCyc += 27
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _6A() ' PUSH Ib (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            PushIntoStack(Param(ParamIndex.First, , DataSize.Byte))
            clkCyc += 3
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _6B() ' IMUL (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            SetAddressing()
            Dim tmp1 As UInt32 = mRegisters.Val(addrMode.Register1)
            Dim tmp2 As UInt32 = To16bitsWithSign(Param(ParamIndex.First, , DataSize.Byte))
            If (tmp1 And &H8000) = &H8000 Then tmp1 = tmp1 Or &HFFFF_0000UI
            If (tmp2 And &H8000) = &H8000 Then tmp2 = tmp2 Or &HFFFF_0000UI
            Dim tmp3 As UInt32 = tmp1 * tmp2
            mRegisters.Val(addrMode.Register1) = tmp3 And &HFFFF
            If (tmp3 And &HFFFF_0000UI) <> 0 Then
                mFlags.CF = 1
                mFlags.OF = 1
            Else
                mFlags.CF = 0
                mFlags.OF = 0
            End If
            clkCyc += 27
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _6C_6F() ' Ignore 80186/V20 port operations... for now...
        opCodeSize += 1
        clkCyc += 3
    End Sub

    Private Sub _70() ' JO Jb
        If mFlags.OF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _71() ' JNO  Jb
        If mFlags.OF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _72() ' JB/JNAE/JC Jb
        If mFlags.CF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _73() ' JNB/JAE/JNC Jb
        If mFlags.CF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _74() ' JZ/JE Jb
        If mFlags.ZF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _75() ' JNZ/JNE Jb
        If mFlags.ZF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _76() ' JBE/JNA Jb
        If mFlags.CF = 1 OrElse mFlags.ZF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _77() ' JA/JNBE Jb
        If mFlags.CF = 0 AndAlso mFlags.ZF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _78() ' JS Jb
        If mFlags.SF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _79() ' JNS Jb
        If mFlags.SF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7A() ' JPE/JP Jb
        If mFlags.PF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7B() ' JPO/JNP Jb
        If mFlags.PF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7C() ' JL/JNGE Jb
        If mFlags.SF <> mFlags.OF Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7D() ' JGE/JNL Jb
        If mFlags.SF = mFlags.OF Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7E() ' JLE/JNG Jb
        If mFlags.ZF = 1 OrElse (mFlags.SF <> mFlags.OF) Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7F() ' JG/JNLE Jb
        If mFlags.ZF = 0 AndAlso (mFlags.SF = mFlags.OF) Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _80_83()
        ExecuteGroup1()
    End Sub

    Private Sub _84_85() ' TEST Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Test, addrMode.Size)
            clkCyc += 3
        Else
            Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Test, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _86_87() ' XCHG Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            tmpUVal1 = mRegisters.Val(addrMode.Dst)
            mRegisters.Val(addrMode.Dst) = mRegisters.Val(addrMode.Src)
            mRegisters.Val(addrMode.Src) = tmpUVal1
            clkCyc += 4
        Else
            RAMn = mRegisters.Val(addrMode.Dst)
            mRegisters.Val(addrMode.Dst) = addrMode.IndMem
            clkCyc += 17
        End If
    End Sub

    Private Sub _88_8B() ' MOV Gv Ev
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Dst) = mRegisters.Val(addrMode.Src)
            clkCyc += 2
        Else
            If addrMode.Direction = 0 Then
                RAMn = mRegisters.Val(addrMode.Src)
                clkCyc += 9
            Else
                mRegisters.Val(addrMode.Dst) = addrMode.IndMem
                clkCyc += 8
            End If
        End If
    End Sub

    Private Sub _8C() ' MOV Ew Sw
        SetAddressing(DataSize.Word)
        SetRegister2ToSegReg()
        If addrMode.IsDirect Then
            SetRegister1Alt(RAM8(mRegisters.CS, mRegisters.IP + 1))
            mRegisters.Val(addrMode.Register1) = mRegisters.Val(addrMode.Register2)
            clkCyc += 2
        Else
            RAMn = mRegisters.Val(addrMode.Register2)
            clkCyc += 8
        End If
    End Sub

    Private Sub _8D() ' LEA Gv M
        SetAddressing()
        mRegisters.Val(addrMode.Src) = addrMode.IndAdr
        clkCyc += 2
    End Sub

    Private Sub _8E() ' MOV Sw Ew
        SetAddressing(DataSize.Word)
        SetRegister2ToSegReg()
        If addrMode.IsDirect Then
            SetRegister1Alt(RAM8(mRegisters.CS, mRegisters.IP + 1))
            mRegisters.Val(addrMode.Register2) = mRegisters.Val(addrMode.Register1)
            clkCyc += 2
        Else
            mRegisters.Val(addrMode.Register2) = addrMode.IndMem
            clkCyc += 8
        End If
        ignoreINTs = True
        If addrMode.Register2 = GPRegisters.RegistersTypes.CS Then DoReschedule = True
    End Sub

    Private Sub _8F() ' POP Ev
        SetAddressing()
        If addrMode.IsDirect Then
            addrMode.Decode(opCode, opCode)
            mRegisters.Val(addrMode.Register1) = PopFromStack()
        Else
            RAMn = PopFromStack()
        End If
        clkCyc += 17
    End Sub

    Private Sub _90() ' NOP
        clkCyc += 3
    End Sub

    Private Sub _91() ' XCHG CX AX
        tmpUVal1 = mRegisters.AX
        mRegisters.AX = mRegisters.CX
        mRegisters.CX = tmpUVal1
        clkCyc += 3
    End Sub

    Private Sub _92() ' XCHG DX AX
        tmpUVal1 = mRegisters.AX
        mRegisters.AX = mRegisters.DX
        mRegisters.DX = tmpUVal1
        clkCyc += 3
    End Sub

    Private Sub _93() ' XCHG BX AX
        tmpUVal1 = mRegisters.AX
        mRegisters.AX = mRegisters.BX
        mRegisters.BX = tmpUVal1
        clkCyc += 3
    End Sub

    Private Sub _94() ' XCHG SP AX
        tmpUVal1 = mRegisters.AX
        mRegisters.AX = mRegisters.SP
        mRegisters.SP = tmpUVal1
        clkCyc += 3
    End Sub

    Private Sub _95() ' XCHG BP AX
        tmpUVal1 = mRegisters.AX
        mRegisters.AX = mRegisters.BP
        mRegisters.BP = tmpUVal1
        clkCyc += 3
    End Sub

    Private Sub _96() ' XCHG SI AX
        tmpUVal1 = mRegisters.AX
        mRegisters.AX = mRegisters.SI
        mRegisters.SI = tmpUVal1
        clkCyc += 3
    End Sub

    Private Sub _97() ' XCHG DI AX
        tmpUVal1 = mRegisters.AX
        mRegisters.AX = mRegisters.DI
        mRegisters.DI = tmpUVal1
        clkCyc += 3
    End Sub

    Private Sub _98() ' CBW
        mRegisters.AX = To16bitsWithSign(mRegisters.AL)
        clkCyc += 2
    End Sub

    Private Sub _99() ' CWD
        mRegisters.DX = If((mRegisters.AH And &H80) <> 0, &HFFFF, &H0)
        clkCyc += 5
    End Sub

    Private Sub _9A() ' CALL Ap
        IPAddrOffet = Param(ParamIndex.First, , DataSize.Word)
        tmpUVal1 = Param(ParamIndex.Second, , DataSize.Word)
        PushIntoStack(mRegisters.CS)
        PushIntoStack(mRegisters.IP + opCodeSize)
        mRegisters.CS = tmpUVal1
        clkCyc += 28
    End Sub

    Private Sub _9B() ' WAIT
        clkCyc += 4
    End Sub

    Private Sub _9C() ' PUSHF
        PushIntoStack(If(mModel = Models.IBMPC_5150, &HFFF, &HFFFF) And mFlags.EFlags)
        clkCyc += 10
    End Sub

    Private Sub _9D() ' POPF
        mFlags.EFlags = PopFromStack()
        clkCyc += 8
    End Sub

    Private Sub _9E() ' SAHF
        mFlags.EFlags = (mFlags.EFlags And &HFF00) Or mRegisters.AH
        clkCyc += 4
    End Sub

    Private Sub _9F() ' LAHF
        mRegisters.AH = mFlags.EFlags
        clkCyc += 4
    End Sub

    Private Sub _A0() ' MOV AL Ob
        mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) ' 
        clkCyc += 10
    End Sub

    Private Sub _A1() ' MOV AX Ov
        mRegisters.AX = RAM16(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) ' 
        clkCyc += 10
    End Sub

    Private Sub _A2() ' MOV Ob AL
        RAM8(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) = mRegisters.AL ' 
        clkCyc += 10
    End Sub

    Private Sub _A3() ' MOV Ov AX
        RAM16(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) = mRegisters.AX ' 
        clkCyc += 10
    End Sub

    Private Sub _A4_A7()
        HandleREPMode()
    End Sub

    Private Sub _AA_AF()
        HandleREPMode()
    End Sub

    Private Sub _A8() ' TEST AL Ib
        Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Test, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _A9() ' TEST AX Iv
        Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Test, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _B0() ' MOV AL Ib
        mRegisters.AL = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B1() ' MOV CL Ib
        mRegisters.CL = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B2() ' MOV DL Ib
        mRegisters.DL = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B3() ' MOV BL Ib
        mRegisters.BL = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B4() ' MOV AH Ib
        mRegisters.AH = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B5() ' MOV CH Ib
        mRegisters.CH = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B6() ' MOV DH Ib
        mRegisters.DH = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B7() ' MOV BH Ib
        mRegisters.BH = Param(ParamIndex.First,, DataSize.Byte) ' 
        clkCyc += 4
    End Sub

    Private Sub _B8() ' MOV AX Ib
        mRegisters.AX = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _B9() ' MOV CX Ib
        mRegisters.CX = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _BA() ' MOV DX Ib
        mRegisters.DX = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _BB() ' MOV BX Ib
        mRegisters.BX = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _BC() ' MOV SP Ib
        mRegisters.SP = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _BD() ' MOV BP Ib
        mRegisters.BP = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _BE() ' MOV SI Ib
        mRegisters.SI = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _BF() ' MOV DI Ib
        mRegisters.DI = Param(ParamIndex.First,, DataSize.Word) ' 
        clkCyc += 4
    End Sub

    Private Sub _C0_C1() ' GRP2 byte/word imm8/16 ??? (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            ExecuteGroup2()
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _C2() ' RET Iw
        IPAddrOffet = PopFromStack()
        mRegisters.SP += Param(ParamIndex.First, , DataSize.Word)
        clkCyc += 20
    End Sub

    Private Sub _C3() ' RET
        IPAddrOffet = PopFromStack()
        clkCyc += 16
    End Sub

    Private Sub _C4() ' LES Gv Mp
        SetAddressing(DataSize.Word)
        addrMode.Decode(&HC5, RAM8(mRegisters.CS, mRegisters.IP + 1))
        mRegisters.Val(addrMode.Register1) = addrMode.IndMem
        mRegisters.ES = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
        ignoreINTs = True
        clkCyc += 16
    End Sub

    Private Sub _C5() ' LDS Gv Mp
        SetAddressing(DataSize.Word)
        addrMode.Decode(&HC5, RAM8(mRegisters.CS, mRegisters.IP + 1))
        mRegisters.Val(addrMode.Register1) = addrMode.IndMem
        mRegisters.DS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
        ignoreINTs = True
        clkCyc += 16
    End Sub

    Private Sub _C6_C7() ' MOV MOV Ev Iv
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Src) = Param(ParamIndex.First, opCodeSize)
        Else
            RAMn = Param(ParamIndex.First, opCodeSize)
        End If
        clkCyc += 10
    End Sub

    Private Sub _C8() ' ENTER (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            Dim stackSize As UInt16 = Param(ParamIndex.First, , DataSize.Word)
            Dim nestLevel As UInt16 = Param(ParamIndex.Second, , DataSize.Byte) And &H1F
            PushIntoStack(mRegisters.BP)
            Dim frameTemp = mRegisters.SP
            If nestLevel > 0 Then
                For i As Integer = 1 To nestLevel - 1
                    mRegisters.BP -= 2
                    'PushIntoStack(RAM16(frameTemp, mRegisters.BP))
                    PushIntoStack(mRegisters.BP)
                Next
                PushIntoStack(frameTemp)
            End If
            mRegisters.BP = frameTemp
            mRegisters.SP -= stackSize

            Select Case nestLevel
                Case 0 ' 
                    clkCyc += 15
                Case 1 ' 
                    clkCyc += 25
                Case Else ' 
                    clkCyc += 22 + 16 * (nestLevel - 1)
            End Select
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _C9() ' LEAVE (80186)
        If mVic20 Then
            mRegisters.SP = mRegisters.BP
            mRegisters.BP = PopFromStack()
            clkCyc += 8
        Else
            OpCodeNotImplemented()
        End If
    End Sub

    Private Sub _CA() ' RETF Iw
        tmpUVal1 = Param(ParamIndex.First, , DataSize.Word)
        IPAddrOffet = PopFromStack()
        mRegisters.CS = PopFromStack()
        mRegisters.SP += tmpUVal1
        clkCyc += 17
    End Sub

    Private Sub _CB() ' RETF
        IPAddrOffet = PopFromStack()
        mRegisters.CS = PopFromStack()
        clkCyc += 18
    End Sub

    Private Sub _CC() ' INT 3
        HandleInterrupt(3, False)
        clkCyc += 1
    End Sub

    Private Sub _CD() ' INT Ib
        HandleInterrupt(Param(ParamIndex.First, , DataSize.Byte), False)
        clkCyc += 0
    End Sub

    Private Sub _CE() ' INTO
        If mFlags.OF = 1 Then
            HandleInterrupt(4, False)
            clkCyc += 3
        Else
            clkCyc += 4
        End If
    End Sub

    Private Sub _CF() ' IRET
        IPAddrOffet = PopFromStack()
        mRegisters.CS = PopFromStack()
        mFlags.EFlags = PopFromStack()
        clkCyc += 32
    End Sub

    Private Sub _D0_D3()
        ExecuteGroup2()
    End Sub

    Private Sub _D4() ' AAM I0
        tmpUVal1 = Param(ParamIndex.First, , DataSize.Byte)
        If tmpUVal1 = 0 Then
            HandleInterrupt(0, True)
            Exit Sub
        End If
        mRegisters.AH = mRegisters.AL \ tmpUVal1
        mRegisters.AL = mRegisters.AL Mod tmpUVal1
        SetSZPFlags(mRegisters.AX, DataSize.Word)
        clkCyc += 83
    End Sub

    Private Sub _D5() ' AAD I0
        tmpUVal1 = Param(ParamIndex.First, , DataSize.Byte)
        tmpUVal1 = tmpUVal1 * mRegisters.AH + mRegisters.AL
        mRegisters.AL = tmpUVal1
        mRegisters.AH = 0
        SetSZPFlags(tmpUVal1, DataSize.Word)
        mFlags.SF = 0
        clkCyc += 60
    End Sub

    Private Sub _D6() ' XLAT for V20 / SALC
        If mVic20 Then
            mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.BX + mRegisters.AL)
        Else
            mRegisters.AL = If(mFlags.CF = 1, &HFF, &H0)
            clkCyc += 4
        End If
    End Sub

    Private Sub _D7() ' XLATB
        mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.BX + mRegisters.AL)
        clkCyc += 11
    End Sub

    Private Sub _D8_DF() ' Ignore 8087 co-processor instructions
        SetAddressing()
        'FPU.Execute(opCode, addrMode)

        ' Lesson 2
        ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

        'HandleInterrupt(7, False)
        OpCodeNotImplemented("FPU Not Available")
        clkCyc += 2
    End Sub

    Private Sub _E0() ' LOOPNE/LOOPNZ
        mRegisters.CX -= 1
        If mRegisters.CX > 0 AndAlso mFlags.ZF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 19
        Else
            opCodeSize += 1
            clkCyc += 5
        End If
    End Sub

    Private Sub _E1() ' LOOPE/LOOPZ
        mRegisters.CX -= 1
        If mRegisters.CX > 0 AndAlso mFlags.ZF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 18
        Else
            opCodeSize += 1
            clkCyc += 6
        End If
    End Sub

    Private Sub _E2() ' LOOP
        mRegisters.CX -= 1
        If mRegisters.CX > 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 17
        Else
            opCodeSize += 1
            clkCyc += 5
        End If
    End Sub

    Private Sub _E3() ' JCXZ/JECXZ
        If mRegisters.CX = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 18
        Else
            opCodeSize += 1
            clkCyc += 6
        End If
    End Sub

    Private Sub _E4() ' IN AL Ib
        mRegisters.AL = ReceiveFromPort(Param(ParamIndex.First, , DataSize.Byte))
        clkCyc += 10
    End Sub

    Private Sub _E5() ' IN AX Ib
        mRegisters.AX = ReceiveFromPort(Param(ParamIndex.First, , DataSize.Byte))
        clkCyc += 10
    End Sub

    Private Sub _E6() ' OUT Ib AL
        SendToPort(Param(ParamIndex.First, , DataSize.Byte), mRegisters.AL)
        clkCyc += 10
    End Sub

    Private Sub _E7() ' OUT Ib AX
        SendToPort(Param(ParamIndex.First, , DataSize.Byte), mRegisters.AX)
        clkCyc += 10
    End Sub

    Private Sub _E8() ' CALL Jv
        IPAddrOffet = OffsetIP(DataSize.Word)
        PushIntoStack(Registers.IP + opCodeSize)
        clkCyc += 19
    End Sub

    Private Sub _E9() ' JMP Jv
        IPAddrOffet = OffsetIP(DataSize.Word)
        clkCyc += 15
    End Sub

    Private Sub _EA() ' JMP Ap
        IPAddrOffet = Param(ParamIndex.First, , DataSize.Word)
        mRegisters.CS = Param(ParamIndex.Second, , DataSize.Word)
        clkCyc += 15
    End Sub

    Private Sub _EB() ' JMP Jb
        IPAddrOffet = OffsetIP(DataSize.Byte)
        clkCyc += 15
    End Sub

    Private Sub _EC() ' IN AL DX
        mRegisters.AL = ReceiveFromPort(mRegisters.DX)
        clkCyc += 8
    End Sub

    Private Sub _ED() ' IN AX DX
        mRegisters.AX = ReceiveFromPort(mRegisters.DX)
        clkCyc += 8
    End Sub

    Private Sub _EE() ' OUT DX AL
        SendToPort(mRegisters.DX, mRegisters.AL)
        clkCyc += 8
    End Sub

    Private Sub _EF() ' OUT DX AX
        SendToPort(mRegisters.DX, mRegisters.AX)
        clkCyc += 8
    End Sub

    Private Sub _F0() ' LOCK
        OpCodeNotImplemented("LOCK")
        clkCyc += 2
    End Sub

    Private Sub _F2() ' REPBE/REPNZ
        mRepeLoopMode = REPLoopModes.REPENE
        newPrefix = True
        clkCyc += 2
    End Sub

    Private Sub _F3() ' REPE/REPZ
        mRepeLoopMode = REPLoopModes.REPE
        newPrefix = True
        clkCyc += 2
    End Sub

    Private Sub _F4() ' HLT
        If Not mIsHalted Then SystemHalted()
        mRegisters.IP -= 1
        clkCyc += 2
    End Sub

    Private Sub _F5() ' CMC
        mFlags.CF = If(mFlags.CF = 0, 1, 0)
        clkCyc += 2
    End Sub

    Private Sub _F6_F7()
        ExecuteGroup3()
    End Sub

    Private Sub _F8() ' CLC
        mFlags.CF = 0
        clkCyc += 2
    End Sub

    Private Sub _F9() ' STC
        mFlags.CF = 1
        clkCyc += 2
    End Sub

    Private Sub _FA() ' CLI
        mFlags.IF = 0
        clkCyc += 2
    End Sub

    Private Sub _FB() ' STI
        mFlags.IF = 1
        ignoreINTs = True ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
        clkCyc += 2
    End Sub

    Private Sub _FC() ' CLD
        mFlags.DF = 0
        clkCyc += 2
    End Sub

    Private Sub _FD() ' STD
        mFlags.DF = 1
        clkCyc += 2
    End Sub

    Private Sub _FE_FF()
        ExecuteGroup4_And_5()
    End Sub


End Class
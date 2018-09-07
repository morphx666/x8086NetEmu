Partial Public Class X8086
    Private Delegate Sub ExecOpcode()
    Private opCodes() As ExecOpcode = {
                                    AddressOf _00_03,   ' add reg<->reg / reg<->mem
AddressOf _00_03,
AddressOf _00_03,
AddressOf _00_03,
AddressOf _04,  ' add al, imm
AddressOf _05,  ' add ax, imm
AddressOf _06,  ' push es
AddressOf _07,  ' pop es
AddressOf _08_0B,   ' or
AddressOf _08_0B,
AddressOf _08_0B,
AddressOf _08_0B,
AddressOf _0C,  ' or al and imm
AddressOf _0D,  ' or ax and imm
AddressOf _0E,  ' push cs
AddressOf _0F,  ' pop cs
AddressOf _10_13,   ' adc
AddressOf _10_13,
AddressOf _10_13,
AddressOf _10_13,
AddressOf _14,  ' adc al and imm
AddressOf _15,  ' adc ax and imm
AddressOf _16,  ' push ss
AddressOf _17,  ' pop ss
AddressOf _18_1B,   ' sbb
AddressOf _18_1B,
AddressOf _18_1B,
AddressOf _18_1B,
AddressOf _1C,  ' sbb al and imm
AddressOf _1D,  ' sbb ax and imm
AddressOf _1E,  ' push ds
AddressOf _1F,  ' pop ds
AddressOf _20_23,   ' and reg/mem and reg to either
AddressOf _20_23,
AddressOf _20_23,
AddressOf _20_23,
AddressOf _24,  ' and al and imm
AddressOf _25,  ' and ax and imm
AddressOf _26_2E_36_3E,
AddressOf _27,  ' daa
AddressOf _28_2B,   ' sub reg/mem with reg to either
AddressOf _28_2B,
AddressOf _28_2B,
AddressOf _28_2B,
AddressOf _2C,  ' sub al and imm
AddressOf _2D,  ' sub ax and imm
AddressOf _26_2E_36_3E,
AddressOf _2F,  ' das
AddressOf _30_33,   ' xor reg/mem and reg to either
AddressOf _30_33,
AddressOf _30_33,
AddressOf _30_33,
AddressOf _34,  ' xor al and imm
AddressOf _35,  ' xor ax and imm
AddressOf _26_2E_36_3E,
AddressOf _37,  ' aaa
AddressOf _38_3B,   ' cmp reg/mem and reg
AddressOf _38_3B,
AddressOf _38_3B,
AddressOf _38_3B,
AddressOf _3C,  ' cmp al and imm
AddressOf _3D,  ' cmp ax and imm
AddressOf _26_2E_36_3E,
AddressOf _3F,  ' aas
AddressOf _40_47,   ' inc reg
AddressOf _40_47,
AddressOf _40_47,
AddressOf _40_47,
AddressOf _40_47,
AddressOf _40_47,
AddressOf _40_47,
AddressOf _40_47,
AddressOf _48_4F,   ' dec reg
AddressOf _48_4F,
AddressOf _48_4F,
AddressOf _48_4F,
AddressOf _48_4F,
AddressOf _48_4F,
AddressOf _48_4F,
AddressOf _48_4F,
AddressOf _50_57,   ' push reg
AddressOf _50_57,
AddressOf _50_57,
AddressOf _50_57,
AddressOf _50_57,
AddressOf _50_57,
AddressOf _50_57,
AddressOf _50_57,
AddressOf _58_5F,   ' pop reg
AddressOf _58_5F,
AddressOf _58_5F,
AddressOf _58_5F,
AddressOf _58_5F,
AddressOf _58_5F,
AddressOf _58_5F,
AddressOf _58_5F,
AddressOf _60,  ' pusha (80186)
AddressOf _61,  ' popa (80186)
AddressOf _62,  ' bound (80186)
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf OpCodeNotImplemented,
AddressOf _68,  ' push (80186)
AddressOf _69,  ' imul (80186)
AddressOf _6A,  ' push (80186)
AddressOf _6B,  ' imul (80186)
AddressOf _6C_6F,   ' Ignore 80186/V20 port operations... for now...
AddressOf _6C_6F,
AddressOf _6C_6F,
AddressOf _6C_6F,
AddressOf _70,  ' jo
AddressOf _71,  ' jno
AddressOf _72,  ' jb/jnae
AddressOf _73,  ' jnb/jae
AddressOf _74,  ' je/jz
AddressOf _75,  ' jne/jnz
AddressOf _76,  ' jbe/jna
AddressOf _77,  ' jnbe/ja
AddressOf _78,  ' js
AddressOf _79,  ' jns
AddressOf _7A,  ' jp/jpe
AddressOf _7B,  ' jnp/jpo
AddressOf _7C,  ' jl/jnge
AddressOf _7D,  ' jnl/jge
AddressOf _7E,  ' jle/jng
AddressOf _7F,  ' jnle/jg
AddressOf _80_83,   ' 
AddressOf _80_83,
AddressOf _80_83,
AddressOf _80_83,
AddressOf _84_85,   ' test reg with reg/mem
AddressOf _84_85,
AddressOf _86_87,   ' xchg reg/mem with reg
AddressOf _86_87,
AddressOf _88_8C,   ' mov ind <-> reg8/reg16
AddressOf _88_8C,
AddressOf _88_8C,
AddressOf _88_8C,
AddressOf _88_8C,
AddressOf _8D,  ' lea
AddressOf _8E,  ' mov reg/mem to seg reg
AddressOf _8F,  ' pop reg/mem
AddressOf _90_97,   ' xchg reg with acc
AddressOf _90_97,
AddressOf _90_97,
AddressOf _90_97,
AddressOf _90_97,
AddressOf _90_97,
AddressOf _90_97,
AddressOf _90_97,
AddressOf _98,  ' cbw
AddressOf _99,  ' cwd
AddressOf _9A,  ' call direct intersegment
AddressOf _9B,  ' wait
AddressOf _9C,  ' pushf
AddressOf _9D,  ' popf
AddressOf _9E,  ' sahf
AddressOf _9F,  ' lahf
AddressOf _A0_A3,   ' mov mem to acc | mov acc to mem
AddressOf _A0_A3,
AddressOf _A0_A3,
AddressOf _A0_A3,
AddressOf _A4_A7,
AddressOf _A4_A7,
AddressOf _A4_A7,
AddressOf _A4_A7,
AddressOf _A8,  ' test al imm8
AddressOf _A9,  ' test ax imm16
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _AA_AF,
AddressOf _B0_BF,   ' mov imm to reg
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _B0_BF,
AddressOf _C0_C1,
AddressOf _C0_C1,
AddressOf _C2,  ' ret (ret n) within segment adding imm to sp
AddressOf _C3,  ' ret within segment
AddressOf _C4_C5,   ' les | lds
AddressOf _C4_C5,
AddressOf _C6_C7,   ' mov imm to reg/mem
AddressOf _C6_C7,
AddressOf _C8,  ' enter (80186)
AddressOf _C9,  ' leave (80186)
AddressOf _CA,  ' ret intersegment adding imm to sp (ret n /retf)
AddressOf _CB,  ' ret intersegment (retf)
AddressOf _CC,  ' int with type 3
AddressOf _CD,  ' int with type specified
AddressOf _CE,  ' into
AddressOf _CF,  ' iret
AddressOf _D0_D3,   ' 
AddressOf _D0_D3,
AddressOf _D0_D3,
AddressOf _D0_D3,
AddressOf _D4,  ' aam
AddressOf _D5,  ' aad
AddressOf _D6,  ' xlat 
AddressOf _D7,  ' xlatb
AddressOf _D8_DF,   ' Ignore co-processor instructions
AddressOf _D8_DF,
AddressOf _D8_DF,
AddressOf _D8_DF,
AddressOf _D8_DF,
AddressOf _D8_DF,
AddressOf _D8_DF,
AddressOf _D8_DF,
AddressOf _E0,  ' loopne/loopnz
AddressOf _E1,  ' loope/loopz
AddressOf _E2,  ' loop
AddressOf _E3,  ' jcxz
AddressOf _E4,  ' in to al from fixed port
AddressOf _E5,  ' inw to ax from fixed port
AddressOf _E6,  ' out to al to fixed port
AddressOf _E7,  ' outw to ax to fixed port
AddressOf _E8,  ' call direct within segment
AddressOf _E9,  ' jmp direct within segment
AddressOf _EA,  ' jmp direct intersegment
AddressOf _EB,  ' jmp direct within segment short
AddressOf _EC,  ' in to al from variable port
AddressOf _ED,  ' inw to ax from variable port
AddressOf _EE,  ' out to port dx from al
AddressOf _EF,  ' out to port dx from ax
AddressOf _F0,  ' lock
AddressOf OpCodeNotImplemented,
AddressOf _F2,  ' repne/repnz
AddressOf _F3,  ' repe/repz
AddressOf _F4,  ' hlt
AddressOf _F5,  ' cmc
AddressOf _F6_F7,   ' 
AddressOf _F6_F7,
AddressOf _F8,  ' clc
AddressOf _F9,  ' stc
AddressOf _FA,  ' cli
AddressOf _FB,  ' sti
AddressOf _FC,  ' cld
AddressOf _FD,  ' std
AddressOf _FE_FF,
AddressOf _FE_FF}

    Private Sub _00_03()    ' add reg<->reg / reg<->mem
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.Add, addrMode.Size)
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.Add, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.Add, addrMode.Size)
                clkCyc += 16
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.Add, addrMode.Size)
                clkCyc += 9
            End If
        End If
    End Sub

    Private Sub _04()   ' add al, imm
        mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Add, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _05()   ' add ax, imm
        mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Add, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _06()   ' push es
        PushIntoStack(mRegisters.ES)
        clkCyc += 10
    End Sub

    Private Sub _07()   ' pop es
        mRegisters.ES = PopFromStack()
        clkCyc += 8
    End Sub

    Private Sub _08_0B()    ' or
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.LogicOr, addrMode.Size)
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.LogicOr, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.LogicOr, addrMode.Size)
                clkCyc += 16
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.LogicOr, addrMode.Size)
                clkCyc += 9
            End If
        End If
    End Sub

    Private Sub _0C()   ' or al and imm
        mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.LogicOr, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _0D()   ' or ax and imm
        mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.LogicOr, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _0E()   ' push cs
        PushIntoStack(mRegisters.CS)
        clkCyc += 10
    End Sub

    Private Sub _0F()   ' pop cs
        If Not mVic20 Then
            mRegisters.CS = PopFromStack()
            clkCyc += 8
        End If
    End Sub

    Private Sub _10_13()    ' adc
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.AddWithCarry, addrMode.Size)
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.AddWithCarry, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.AddWithCarry, addrMode.Size)
                clkCyc += 16
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.AddWithCarry, addrMode.Size)
                clkCyc += 9
            End If
        End If
    End Sub

    Private Sub _14()   ' adc al and imm
        mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.AddWithCarry, DataSize.Byte)
        clkCyc += 3
    End Sub

    Private Sub _15()   ' adc ax and imm
        mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.AddWithCarry, DataSize.Word)
        clkCyc += 3
    End Sub

    Private Sub _16()   ' push ss
        PushIntoStack(mRegisters.SS)
        clkCyc += 10
    End Sub

    Private Sub _17()   ' pop ss
        mRegisters.SS = PopFromStack()
        ' Lesson 4: http://ntsecurity.nu/onmymind/2007/2007-08-22.html
        ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
        ignoreINTs = True
        clkCyc += 8
    End Sub

    Private Sub _18_1B()    ' sbb
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.SubstractWithCarry, addrMode.Size)
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.SubstractWithCarry, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.SubstractWithCarry, addrMode.Size)
                clkCyc += 16
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.SubstractWithCarry, addrMode.Size)
                clkCyc += 9
            End If
        End If
    End Sub

    Private Sub _1C()   ' sbb al and imm
        mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.SubstractWithCarry, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _1D()   ' sbb ax and imm
        mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.SubstractWithCarry, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _1E()   ' push ds
        PushIntoStack(mRegisters.DS)
        clkCyc += 10
    End Sub

    Private Sub _1F()   ' pop ds
        mRegisters.DS = PopFromStack()
        clkCyc += 8
    End Sub

    Private Sub _20_23()    ' and reg/mem and reg to either
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.LogicAnd, addrMode.Size)
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.LogicAnd, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.LogicAnd, addrMode.Size)
                clkCyc += 16
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.LogicAnd, addrMode.Size)
                clkCyc += 9
            End If
        End If
    End Sub

    Private Sub _24()   ' and al and imm
        mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.LogicAnd, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _25()   ' and ax and imm
        mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.LogicAnd, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _26_2E_36_3E()
        addrMode.Decode(opCode, opCode)
        mRegisters.ActiveSegmentRegister = (addrMode.Register1 - GPRegisters.RegistersTypes.AH) + GPRegisters.RegistersTypes.ES
        isStringOp = True
        clkCyc += 2
    End Sub

    Private Sub _27()   ' daa
        If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
            tmpVal = CUInt(mRegisters.AL) + 6
            mRegisters.AL += 6
            mFlags.AF = 1
            mFlags.CF = mFlags.CF Or If((tmpVal And &HFF00) <> 0, 1, 0)
        Else
            mFlags.AF = 0
        End If
        If (mRegisters.AL And &HF0) > &H90 OrElse mFlags.CF = 1 Then
            tmpVal = CUInt(mRegisters.AL) + &H60
            mRegisters.AL += &H60
            mFlags.CF = 1
        Else
            mFlags.CF = 0
        End If
        SetSZPFlags(tmpVal, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _28_2B()    ' sub reg/mem with reg to either
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.Substract, addrMode.Size)
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.Substract, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.Substract, addrMode.Size)
                clkCyc += 16
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.Substract, addrMode.Size)
                clkCyc += 9
            End If
        End If
    End Sub

    Private Sub _2C()   ' sub al and imm
        mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Substract, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _2D()   ' sub ax and imm
        mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Substract, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _2F()   ' das
        Dim al = mRegisters.AL
        If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
            tmpVal = CShort(mRegisters.AL) - 6
            mRegisters.AL -= 6
            mFlags.AF = 1
            mFlags.CF = mFlags.CF Or If((tmpVal And &HFF00) <> 0, 1, 0)
        Else
            mFlags.AF = 0
        End If
        If al > &H99 OrElse mFlags.CF = 1 Then
            tmpVal = CShort(mRegisters.AL) - &H60
            mRegisters.AL -= &H60
            mFlags.CF = 1
        Else
            mFlags.CF = 0
        End If
        SetSZPFlags(tmpVal, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _30_33()    ' xor reg/mem and reg to either
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.LogicXor, addrMode.Size)
            Else
                mRegisters.Val(addrMode.Register1) = Eval(Registers.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.LogicXor, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.LogicXor, addrMode.Size)
                clkCyc += 16
            Else
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.LogicXor, addrMode.Size)
                clkCyc += 9
            End If
        End If
    End Sub

    Private Sub _34()   ' xor al and imm
        mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.LogicXor, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _35()   ' xor ax and imm
        mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.LogicXor, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _37()   ' aaa
        If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
            mRegisters.AX += &H106
            mFlags.AF = 1
            mFlags.CF = 1
        Else
            mFlags.AF = 0
            mFlags.CF = 0
        End If
        mRegisters.AL = mRegisters.AL And &HF
        mFlags.OF = 0
        mFlags.SF = 0
        clkCyc += 8
    End Sub

    Private Sub _38_3B()    ' cmp reg/mem and reg
        SetAddressing()
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                Eval(mRegisters.Val(addrMode.Register2), mRegisters.Val(addrMode.Register1), Operation.Compare, addrMode.Size)
            Else
                Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.Compare, addrMode.Size)
            End If
            clkCyc += 3
        Else
            If addrMode.Direction = 0 Then
                Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register1), Operation.Compare, addrMode.Size)
            Else
                Eval(mRegisters.Val(addrMode.Register1), addrMode.IndMem, Operation.Compare, addrMode.Size)
            End If
            clkCyc += 9
        End If
    End Sub

    Private Sub _3C()   ' cmp al and imm
        Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Compare, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _3D()   ' cmp ax and imm
        Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Compare, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _3F()   ' aas
        If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
            mRegisters.AX -= &H106
            mFlags.AF = 1
            mFlags.CF = 1
        Else
            mFlags.AF = 0
            mFlags.CF = 0
        End If
        mRegisters.AL = mRegisters.AL And &HF
        mFlags.OF = 0
        mFlags.SF = 0
        clkCyc += 8
    End Sub

    Private Sub _40_47()    ' inc reg
        SetRegister1Alt(opCode)
        mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Increment, DataSize.Word)
        clkCyc += 3
    End Sub

    Private Sub _48_4F()    ' dec reg
        SetRegister1Alt(opCode)
        mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Decrement, DataSize.Word)
        clkCyc += 3
    End Sub

    Private Sub _50_57()    ' push reg
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

    Private Sub _58_5F()    ' pop reg
        SetRegister1Alt(opCode)
        mRegisters.Val(addrMode.Register1) = PopFromStack()
        clkCyc += 8
    End Sub

    Private Sub _60()   ' pusha (80186)
        If mVic20 Then
            tmpVal = mRegisters.SP
            PushIntoStack(mRegisters.AX)
            PushIntoStack(mRegisters.CX)
            PushIntoStack(mRegisters.DX)
            PushIntoStack(mRegisters.BX)
            PushIntoStack(tmpVal)
            PushIntoStack(mRegisters.BP)
            PushIntoStack(mRegisters.SI)
            PushIntoStack(mRegisters.DI)
            clkCyc += 19
        End If
    End Sub

    Private Sub _61()   ' popa (80186)
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
        End If
    End Sub

    Private Sub _62()   ' bound (80186)
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
        End If
    End Sub

    Private Sub _68()   ' push (80186)
        ' PRE ALPHA CODE - UNTESTED
        If mVic20 Then
            PushIntoStack(Param(SelPrmIndex.First, , DataSize.Word))
            clkCyc += 3
        End If
    End Sub

    Private Sub _69()   ' imul (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            SetAddressing()
            Dim tmp1 As UInt32 = mRegisters.Val(addrMode.Register1)
            Dim tmp2 As UInt32 = Param(SelPrmIndex.First, , DataSize.Word)
            If (tmp1 And &H8000) = &H8000 Then tmp1 = tmp1 Or &HFFFF0000UI
            If (tmp2 And &H8000) = &H8000 Then tmp2 = tmp2 Or &HFFFF0000UI
            Dim tmp3 As UInt32 = tmp1 * tmp2
            mRegisters.Val(addrMode.Register1) = tmp3 And &HFFFFUI
            If (tmp3 And &HFFFF0000UI) <> 0 Then
                mFlags.CF = 1
                mFlags.OF = 1
            Else
                mFlags.CF = 0
                mFlags.OF = 0
            End If
            clkCyc += 27
        End If
    End Sub

    Private Sub _6A()   ' push (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            PushIntoStack(Param(SelPrmIndex.First, , DataSize.Byte))
            clkCyc += 3
        End If
    End Sub

    Private Sub _6B()   ' imul (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            SetAddressing()
            Dim tmp1 As UInt32 = mRegisters.Val(addrMode.Register1)
            Dim tmp2 As UInt32 = To16bitsWithSign(Param(SelPrmIndex.First, , DataSize.Byte))
            If (tmp1 And &H8000) = &H8000 Then tmp1 = tmp1 Or &HFFFF0000UI
            If (tmp2 And &H8000) = &H8000 Then tmp2 = tmp2 Or &HFFFF0000UI
            Dim tmp3 As UInt32 = tmp1 * tmp2
            mRegisters.Val(addrMode.Register1) = tmp3 And &HFFFFUI
            If (tmp3 And &HFFFF0000UI) <> 0 Then
                mFlags.CF = 1
                mFlags.OF = 1
            Else
                mFlags.CF = 0
                mFlags.OF = 0
            End If
            clkCyc += 27
        End If
    End Sub

    Private Sub _6C_6F()    ' Ignore 80186/V20 port operations... for now...
        opCodeSize += 1
        clkCyc += 3
    End Sub

    Private Sub _70()   ' jo
        If mFlags.OF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _71()   ' jno
        If mFlags.OF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _72()   ' jb/jnae
        If mFlags.CF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _73()   ' jnb/jae
        If mFlags.CF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _74()   ' je/jz
        If mFlags.ZF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _75()   ' jne/jnz
        If mFlags.ZF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _76()   ' jbe/jna
        If mFlags.CF = 1 OrElse mFlags.ZF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _77()   ' jnbe/ja
        If mFlags.CF = 0 AndAlso mFlags.ZF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _78()   ' js
        If mFlags.SF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _79()   ' jns
        If mFlags.SF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7A()   ' jp/jpe
        If mFlags.PF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7B()   ' jnp/jpo
        If mFlags.PF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7C()   ' jl/jnge
        If mFlags.SF <> mFlags.OF Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7D()   ' jnl/jge
        If mFlags.SF = mFlags.OF Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7E()   ' jle/jng
        If mFlags.ZF = 1 OrElse (mFlags.SF <> mFlags.OF) Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _7F()   ' jnle/jg
        If mFlags.ZF = 0 AndAlso (mFlags.SF = mFlags.OF) Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 16
        Else
            opCodeSize += 1
            clkCyc += 4
        End If
    End Sub

    Private Sub _80_83()    ' 
        ExecuteGroup1()
    End Sub

    Private Sub _84_85()    ' test reg with reg/mem
        SetAddressing()
        If addrMode.IsDirect Then
            Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.Test, addrMode.Size)
            clkCyc += 3
        Else
            Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register2), Operation.Test, addrMode.Size)
            clkCyc += 9
        End If
    End Sub

    Private Sub _86_87()    ' xchg reg/mem with reg
        SetAddressing()
        If addrMode.IsDirect Then
            tmpVal = mRegisters.Val(addrMode.Register1)
            mRegisters.Val(addrMode.Register1) = mRegisters.Val(addrMode.Register2)
            mRegisters.Val(addrMode.Register2) = tmpVal
            clkCyc += 4
        Else
            RAMn = mRegisters.Val(addrMode.Register1)
            mRegisters.Val(addrMode.Register1) = addrMode.IndMem
            clkCyc += 17
        End If
    End Sub

    Private Sub _88_8C()    ' mov ind <-> reg8/reg16
        SetAddressing()

        If opCode = &H8C Then ' mov r/m16, sreg
            'If (addrMode.Register1 And &H4) = &H4 Then
            'addrMode.Register1 = addrMode.Register1 And (Not shl2)
            'Else
            addrMode.Register1 += GPRegisters.RegistersTypes.ES
            If addrMode.Register2 > &H3 Then
                addrMode.Register2 = (addrMode.Register2 + GPRegisters.RegistersTypes.ES) Or shl3
            Else
                addrMode.Register2 += GPRegisters.RegistersTypes.AX
            End If
            'End If
        End If

        addrMode.Size = If(addrMode.Register1 < GPRegisters.RegistersTypes.AX, DataSize.Byte, DataSize.Word)
        If addrMode.IsDirect Then
            If addrMode.Direction = 0 Then
                mRegisters.Val(addrMode.Register2) = mRegisters.Val(addrMode.Register1)
            Else
                mRegisters.Val(addrMode.Register1) = mRegisters.Val(addrMode.Register2)
            End If
            clkCyc += 2
        Else
            If addrMode.Direction = 0 Then
                RAMn = mRegisters.Val(addrMode.Register1)
                clkCyc += 9
            Else
                mRegisters.Val(addrMode.Register1) = addrMode.IndMem
                clkCyc += 8
            End If
        End If
    End Sub

    Private Sub _8D()   ' lea
        SetAddressing()
        mRegisters.Val(addrMode.Register1) = addrMode.IndAdr
        clkCyc += 2
    End Sub

    Private Sub _8E()   ' mov reg/mem to seg reg
        SetAddressing(DataSize.Word)
        SetRegister2ToSegReg()
        If addrMode.IsDirect Then
            SetRegister1Alt(ParamNOPS(SelPrmIndex.First, , DataSize.Byte))
            mRegisters.Val(addrMode.Register2) = mRegisters.Val(addrMode.Register1)
            clkCyc += 2
        Else
            mRegisters.Val(addrMode.Register2) = addrMode.IndMem
            clkCyc += 8
        End If
        ignoreINTs = ignoreINTs Or
                        (addrMode.Register2 = GPRegisters.RegistersTypes.CS) Or
                        (addrMode.Register2 = GPRegisters.RegistersTypes.SS)
    End Sub

    Private Sub _8F()   ' pop reg/mem
        SetAddressing()
        addrMode.Decode(opCode, opCode)
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Register1) = PopFromStack()
        Else
            RAMn = PopFromStack()
        End If
        clkCyc += 17
    End Sub

    Private Sub _90()   ' nop
        clkCyc += 3
    End Sub

    Private Sub _90_97()    ' xchg reg with acc
        SetRegister1Alt(opCode)
        tmpVal = mRegisters.AX
        mRegisters.AX = mRegisters.Val(addrMode.Register1)
        mRegisters.Val(addrMode.Register1) = tmpVal
        clkCyc += 3
    End Sub

    Private Sub _98()   ' cbw
        mRegisters.AX = To16bitsWithSign(mRegisters.AL)
        clkCyc += 2
    End Sub

    Private Sub _99()   ' cwd
        mRegisters.DX = If((mRegisters.AH And &H80) = 0, &H0, &HFFFF)
        clkCyc += 5
    End Sub

    Private Sub _9A()   ' call direct intersegment
        IPAddrOffet = Param(SelPrmIndex.First, , DataSize.Word)
        tmpVal = Param(SelPrmIndex.Second, , DataSize.Word)

        PushIntoStack(mRegisters.CS)
        PushIntoStack(mRegisters.IP + opCodeSize)

        mRegisters.CS = tmpVal

        clkCyc += 28
    End Sub

    Private Sub _9B()   ' wait
        clkCyc += 4
    End Sub

    Private Sub _9C()   ' pushf
        PushIntoStack(If(mModel = Models.IBMPC_5150, &HFFF, &HFFFF) And mFlags.EFlags)
        clkCyc += 10
    End Sub

    Private Sub _9D()   ' popf
        mFlags.EFlags = PopFromStack()
        clkCyc += 8
    End Sub

    Private Sub _9E()   ' sahf
        mFlags.EFlags = (mFlags.EFlags And &HFF00) Or mRegisters.AH
        clkCyc += 4
    End Sub

    Private Sub _9F()   ' lahf
        mRegisters.AH = mFlags.EFlags And &HFF
        clkCyc += 4
    End Sub

    Private Sub _A0_A3()    ' mov mem to acc | mov acc to mem
        addrMode.Decode(opCode, opCode)
        addrMode.IndAdr = Param(SelPrmIndex.First, , DataSize.Word)
        addrMode.Register1 = If(addrMode.Size = DataSize.Byte, GPRegisters.RegistersTypes.AL, GPRegisters.RegistersTypes.AX)
        If addrMode.Direction = 0 Then
            mRegisters.Val(addrMode.Register1) = RAMn
        Else
            RAMn = mRegisters.Val(addrMode.Register1)
        End If
        clkCyc += 10
    End Sub

    Private Sub _A4_A7()
        HandleREPMode()
        isStringOp = True
    End Sub

    Private Sub _AA_AF()
        HandleREPMode()
        isStringOp = True
    End Sub

    Private Sub _A8()   ' test al imm8
        Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Test, DataSize.Byte)
        clkCyc += 4
    End Sub

    Private Sub _A9()   ' test ax imm16
        Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Test, DataSize.Word)
        clkCyc += 4
    End Sub

    Private Sub _B0_BF()    ' mov imm to reg
        addrMode.Register1 = (opCode And &H7)
        If (opCode And &H8) = &H8 Then
            addrMode.Register1 += GPRegisters.RegistersTypes.AX
            If (opCode And &H4) = &H4 Then addrMode.Register1 += GPRegisters.RegistersTypes.ES
            addrMode.Size = DataSize.Word
        Else
            addrMode.Size = DataSize.Byte
        End If
        mRegisters.Val(addrMode.Register1) = Param(SelPrmIndex.First)
        clkCyc += 4
    End Sub

    Private Sub _C0_C1()
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            ExecuteGroup2()
        End If
    End Sub

    Private Sub _C2()   ' ret (ret n) within segment adding imm to sp
        IPAddrOffet = PopFromStack()
        mRegisters.SP += Param(SelPrmIndex.First, , DataSize.Word)
        clkCyc += 20
    End Sub

    Private Sub _C3()   ' ret within segment
        IPAddrOffet = PopFromStack()
        clkCyc += 16
    End Sub

    Private Sub _C4_C5()    ' les | lds
        SetAddressing(DataSize.Word)
        If (addrMode.Register1 And shl2) = shl2 Then
            addrMode.Register1 = (addrMode.Register1 + GPRegisters.RegistersTypes.ES) Or shl3
        Else
            addrMode.Register1 = (addrMode.Register1 Or shl3)
        End If
        mRegisters.Val(addrMode.Register1) = addrMode.IndMem
        mRegisters.Val(If(opCode = &HC4, GPRegisters.RegistersTypes.ES, GPRegisters.RegistersTypes.DS)) = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
        clkCyc += 16
    End Sub

    Private Sub _C6_C7()    ' mov imm to reg/mem
        SetAddressing()
        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Register1) = Param(SelPrmIndex.First, opCodeSize)
            clkCyc += 4
        Else
            RAMn = Param(SelPrmIndex.First, opCodeSize)
            clkCyc += 10
        End If
    End Sub

    Private Sub _C8()   ' enter (80186)
        If mVic20 Then
            ' PRE ALPHA CODE - UNTESTED
            Dim stackSize = Param(SelPrmIndex.First, , DataSize.Word)
            Dim nestLevel = Param(SelPrmIndex.Second, , DataSize.Byte) And &H1F
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
        End If
    End Sub

    Private Sub _C9()   ' leave (80186)
        If mVic20 Then
            mRegisters.SP = mRegisters.BP
            mRegisters.BP = PopFromStack()
            clkCyc += 8
        End If
    End Sub

    Private Sub _CA()   ' ret intersegment adding imm to sp (ret n /retf)
        tmpVal = Param(SelPrmIndex.First, , DataSize.Word)
        IPAddrOffet = PopFromStack()
        mRegisters.CS = PopFromStack()
        mRegisters.SP += tmpVal
        clkCyc += 17
    End Sub

    Private Sub _CB()   ' ret intersegment (retf)
        IPAddrOffet = PopFromStack()
        mRegisters.CS = PopFromStack()
        clkCyc += 18
    End Sub

    Private Sub _CC()   ' int with type 3
        HandleInterrupt(3, False)
        clkCyc += 52
    End Sub

    Private Sub _CD()   ' int with type specified
        HandleInterrupt(Param(SelPrmIndex.First, , DataSize.Byte), False)
        clkCyc += 51
    End Sub

    Private Sub _CE()   ' into
        If mFlags.OF = 1 Then
            HandleInterrupt(4, False)
            clkCyc += 3
        Else
            clkCyc += 4
        End If
    End Sub

    Private Sub _CF()   ' iret
        IPAddrOffet = PopFromStack()
        mRegisters.CS = PopFromStack()
        mFlags.EFlags = PopFromStack()
        clkCyc += 32
    End Sub

    Private Sub _D0_D3()    ' 
        ExecuteGroup2()
    End Sub

    Private Sub _D4()   ' aam
        Dim div As Byte = Param(SelPrmIndex.First, , DataSize.Byte)
        If div = 0 Then
            HandleInterrupt(0, True)
            Exit Sub
        End If
        mRegisters.AH = mRegisters.AL \ div
        mRegisters.AL = mRegisters.AL Mod div
        SetSZPFlags(mRegisters.AX, DataSize.Word)
        clkCyc += 83
    End Sub

    Private Sub _D5()   ' aad
        mRegisters.AL += mRegisters.AH * Param(SelPrmIndex.First, , DataSize.Byte)
        mRegisters.AH = 0
        SetSZPFlags(mRegisters.AX, DataSize.Word)
        mFlags.SF = 0
        clkCyc += 60
    End Sub

    Private Sub _D6()   ' xlat 
        If Not mVic20 Then
            mRegisters.AL = If(mFlags.CF = 1, &HFF, &H0)
            clkCyc += 4
        End If
    End Sub

    Private Sub _D7()   ' xlatb
        mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.BX + mRegisters.AL)
        clkCyc += 11
    End Sub

    Private Sub _D8_DF()    ' Ignore co-processor instructions
        SetAddressing()

        'FPU.Execute(opCode, addrMode)

        ' Lesson 2
        ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

        'HandleInterrupt(7, False)

        'OpCodeNotImplemented(opCode, "FPU Not Available")
        clkCyc += 2
    End Sub

    Private Sub _E0()   ' loopne/loopnz
        mRegisters.CX -= 1
        If mRegisters.CX > 0 AndAlso mFlags.ZF = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 19
        Else
            opCodeSize += 1
            clkCyc += 5
        End If
    End Sub

    Private Sub _E1()   ' loope/loopz
        mRegisters.CX -= 1
        If mRegisters.CX > 0 AndAlso mFlags.ZF = 1 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 18
        Else
            opCodeSize += 1
            clkCyc += 6
        End If
    End Sub

    Private Sub _E2()   ' loop
        mRegisters.CX -= 1
        If mRegisters.CX > 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 17
        Else
            opCodeSize += 1
            clkCyc += 5
        End If
    End Sub

    Private Sub _E3()   ' jcxz
        If mRegisters.CX = 0 Then
            IPAddrOffet = OffsetIP(DataSize.Byte)
            clkCyc += 18
        Else
            opCodeSize += 1
            clkCyc += 6
        End If
    End Sub

    Private Sub _E4()   ' in to al from fixed port
        mRegisters.AL = ReceiveFromPort(Param(SelPrmIndex.First, , DataSize.Byte))
        clkCyc += 10
    End Sub

    Private Sub _E5()   ' inw to ax from fixed port
        mRegisters.AX = ReceiveFromPort(Param(SelPrmIndex.First, , DataSize.Byte))
        clkCyc += 10
    End Sub

    Private Sub _E6()   ' out to al to fixed port
        FlushCycles()
        SendToPort(Param(SelPrmIndex.First, , DataSize.Byte), mRegisters.AL)
        clkCyc += 10
    End Sub

    Private Sub _E7()   ' outw to ax to fixed port
        FlushCycles()
        SendToPort(Param(SelPrmIndex.First, , DataSize.Byte), mRegisters.AX)
        clkCyc += 10
    End Sub

    Private Sub _E8()   ' call direct within segment
        IPAddrOffet = OffsetIP(DataSize.Word)
        PushIntoStack(Registers.IP + opCodeSize)
        clkCyc += 19
    End Sub

    Private Sub _E9()   ' jmp direct within segment
        IPAddrOffet = OffsetIP(DataSize.Word)
        clkCyc += 15
    End Sub

    Private Sub _EA()   ' jmp direct intersegment
        IPAddrOffet = Param(SelPrmIndex.First, , DataSize.Word)
        mRegisters.CS = Param(SelPrmIndex.Second, , DataSize.Word)
        clkCyc += 15
    End Sub

    Private Sub _EB()   ' jmp direct within segment short
        IPAddrOffet = OffsetIP(DataSize.Byte)
        clkCyc += 15
    End Sub

    Private Sub _EC()   ' in to al from variable port
        mRegisters.AL = ReceiveFromPort(mRegisters.DX)
        clkCyc += 8
    End Sub

    Private Sub _ED()   ' inw to ax from variable port
        mRegisters.AX = ReceiveFromPort(mRegisters.DX)
        clkCyc += 8
    End Sub

    Private Sub _EE()   ' out to port dx from al
        SendToPort(mRegisters.DX, mRegisters.AL)
        clkCyc += 8
    End Sub

    Private Sub _EF()   ' out to port dx from ax
        SendToPort(mRegisters.DX, mRegisters.AX)
        clkCyc += 8
    End Sub

    Private Sub _F0()   ' lock
        OpCodeNotImplemented("LOCK")
        clkCyc += 2
    End Sub

    Private Sub _F2()   ' repne/repnz
        repeLoopMode = REPLoopModes.REPENE
        isStringOp = True
        clkCyc += 2
    End Sub

    Private Sub _F3()   ' repe/repz
        repeLoopMode = REPLoopModes.REPE
        isStringOp = True
        clkCyc += 2
    End Sub

    Private Sub _F4()   ' hlt
        clkCyc += 2
        If Not mIsHalted Then SystemHalted()
        IncIP(-1)
    End Sub

    Private Sub _F5()   ' cmc
        mFlags.CF = If(mFlags.CF = 0, 1, 0)
        clkCyc += 2
    End Sub

    Private Sub _F6_F7()    ' 
        ExecuteGroup3()
    End Sub

    Private Sub _F8()   ' clc
        mFlags.CF = 0
        clkCyc += 2
    End Sub

    Private Sub _F9()   ' stc
        mFlags.CF = 1
        clkCyc += 2
    End Sub

    Private Sub _FA()   ' cli
        mFlags.IF = 0
        clkCyc += 2
    End Sub

    Private Sub _FB()   ' sti
        mFlags.IF = 1
        ignoreINTs = True ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
        clkCyc += 2
    End Sub

    Private Sub _FC()   ' cld
        mFlags.DF = 0
        clkCyc += 2
    End Sub

    Private Sub _FD()   ' std
        mFlags.DF = 1
        clkCyc += 2
    End Sub

    Private Sub _FE_FF()
        ExecuteGroup4_And_5()
    End Sub


End Class
Imports System.Runtime.InteropServices

Partial Public Class X8086
    Public Const MemSize As UInt32 = &H10_0000UI  ' 1MB
    Public Const ROMStart As UInt32 = &HC_0000UI

    Public ReadOnly Memory(MemSize - 1) As Byte

    Private address As UInt32
    Private Const shl2 As UInt16 = 1 << 2
    Private Const shl3 As UInt16 = 1 << 3

    Public Class MemoryAccessEventArgs
        Inherits EventArgs

        Public Enum AccessModes
            Read
            Write
        End Enum

        Public ReadOnly Property AccessMode As AccessModes
        Public ReadOnly Property Address As UInt32

        Public Sub New(address As UInt32, accesMode As AccessModes)
            Me.Address = address
            Me.AccessMode = accesMode
        End Sub
    End Class

    Public Event MemoryAccess(sender As Object, e As MemoryAccessEventArgs)

    <StructLayout(LayoutKind.Explicit)>
    Public Class GPRegisters
        Implements ICloneable

        Public Enum RegistersTypes
            NONE = -1

            AL = 0
            AH = AL Or shl2
            AX = AL Or shl3

            BL = 3
            BH = BL Or shl2
            BX = BL Or shl3

            CL = 1
            CH = CL Or shl2
            CX = CL Or shl3

            DL = 2
            DH = DL Or shl2
            DX = DL Or shl3

            ES = 12
            CS = ES + 1
            SS = ES + 2
            DS = ES + 3

            SP = 24
            BP = SP + 1
            SI = SP + 2
            DI = SP + 3
            IP = SP + 4
        End Enum

        <FieldOffset(0)> Public AX As UInt16
        <FieldOffset(0)> Public AL As Byte
        <FieldOffset(1)> Public AH As Byte

        <FieldOffset(2)> Public BX As UInt16
        <FieldOffset(2)> Public BL As Byte
        <FieldOffset(3)> Public BH As Byte

        <FieldOffset(4)> Public CX As UInt16
        <FieldOffset(4)> Public CL As Byte
        <FieldOffset(5)> Public CH As Byte

        <FieldOffset(6)> Public DX As UInt16
        <FieldOffset(6)> Public DL As Byte
        <FieldOffset(7)> Public DH As Byte

        <FieldOffset(8)> Public CS As UInt16
        <FieldOffset(10)> Public IP As UInt16

        <FieldOffset(12)> Public SS As UInt16
        <FieldOffset(14)> Public SP As UInt16

        <FieldOffset(16)> Public DS As UInt16
        <FieldOffset(18)> Public SI As UInt16

        <FieldOffset(20)> Public ES As UInt16
        <FieldOffset(22)> Public DI As UInt16

        <FieldOffset(24)> Public BP As UInt16

        <FieldOffset(26)> Private mActiveSegmentRegister As RegistersTypes
        <FieldOffset(30)> Private mActiveSegmentChanged As Boolean

        <FieldOffset(32)> Private ReadOnly extraRegs As New Dictionary(Of Integer, UInt16)

        Public Property Val(reg As RegistersTypes) As UInt16
            Get
                Select Case reg
                    Case RegistersTypes.AX : Return AX
                    Case RegistersTypes.AH : Return AH
                    Case RegistersTypes.AL : Return AL

                    Case RegistersTypes.BX : Return BX
                    Case RegistersTypes.BH : Return BH
                    Case RegistersTypes.BL : Return BL

                    Case RegistersTypes.CX : Return CX
                    Case RegistersTypes.CH : Return CH
                    Case RegistersTypes.CL : Return CL

                    Case RegistersTypes.DX : Return DX
                    Case RegistersTypes.DH : Return DH
                    Case RegistersTypes.DL : Return DL

                    Case RegistersTypes.CS : Return CS
                    Case RegistersTypes.IP : Return IP

                    Case RegistersTypes.SS : Return SS
                    Case RegistersTypes.SP : Return SP

                    Case RegistersTypes.DS : Return DS
                    Case RegistersTypes.SI : Return SI

                    Case RegistersTypes.ES : Return ES
                    Case RegistersTypes.DI : Return DI

                    Case RegistersTypes.BP : Return BP

                    Case Else
                        If extraRegs.ContainsKey(reg) Then
                            Return extraRegs(reg)
                        Else
                            Return 0
                        End If
                End Select
            End Get
            Set(value As UInt16)
                Select Case reg
                    Case RegistersTypes.AX : AX = value
                    Case RegistersTypes.AH : AH = value
                    Case RegistersTypes.AL : AL = value

                    Case RegistersTypes.BX : BX = value
                    Case RegistersTypes.BH : BH = value
                    Case RegistersTypes.BL : BL = value

                    Case RegistersTypes.CX : CX = value
                    Case RegistersTypes.CH : CH = value
                    Case RegistersTypes.CL : CL = value

                    Case RegistersTypes.DX : DX = value
                    Case RegistersTypes.DH : DH = value
                    Case RegistersTypes.DL : DL = value

                    Case RegistersTypes.CS : CS = value
                    Case RegistersTypes.IP : IP = value

                    Case RegistersTypes.SS : SS = value
                    Case RegistersTypes.SP : SP = value

                    Case RegistersTypes.DS : DS = value
                    Case RegistersTypes.SI : SI = value

                    Case RegistersTypes.ES : ES = value
                    Case RegistersTypes.DI : DI = value

                    Case RegistersTypes.BP : BP = value

                    Case Else
                        If extraRegs.ContainsKey(reg) Then
                            extraRegs(reg) = value
                        Else
                            extraRegs.Add(reg, value)
                        End If
                End Select
            End Set
        End Property

        Public Sub ResetActiveSegment()
            mActiveSegmentChanged = False
            mActiveSegmentRegister = RegistersTypes.DS
        End Sub

        Public Property ActiveSegmentRegister As RegistersTypes
            Get
                Return mActiveSegmentRegister
            End Get
            Set(value As RegistersTypes)
                mActiveSegmentRegister = value
                mActiveSegmentChanged = True
            End Set
        End Property

        Public ReadOnly Property ActiveSegmentValue As UInt16
            Get
                Return Val(mActiveSegmentRegister)
            End Get
        End Property

        Public ReadOnly Property ActiveSegmentChanged As Boolean
            Get
                Return mActiveSegmentChanged
            End Get
        End Property

        Public ReadOnly Property PointerAddressToString() As String
            Get
                Return CS.ToString("X4") + ":" + IP.ToString("X4")
            End Get
        End Property

        Public Function Clone() As Object Implements ICloneable.Clone
            Dim reg As New GPRegisters With {
                .AX = AX,
                .BX = BX,
                .CX = CX,
                .DX = DX,
                .ES = ES,
                .CS = CS,
                .SS = SS,
                .DS = DS,
                .SP = SP,
                .BP = BP,
                .SI = SI,
                .DI = DI,
                .IP = IP
            }
            If mActiveSegmentChanged Then reg.ActiveSegmentRegister = mActiveSegmentRegister
            Return reg
        End Function
    End Class

    Public Class GPFlags
        Implements ICloneable

        <Flags>
        Public Enum FlagsTypes
            CF = 2 ^ 0
            PF = 2 ^ 2
            AF = 2 ^ 4
            ZF = 2 ^ 6
            SF = 2 ^ 7
            TF = 2 ^ 8
            [IF] = 2 ^ 9
            DF = 2 ^ 10
            [OF] = 2 ^ 11
        End Enum

        Public Property CF As Byte
        Public Property PF As Byte
        Public Property AF As Byte
        Public Property ZF As Byte
        Public Property SF As Byte
        Public Property TF As Byte
        Public Property [IF] As Byte
        Public Property DF As Byte
        Public Property [OF] As Byte

        Public Property EFlags() As UInt16
            Get
                Return CF * FlagsTypes.CF Or
                        1 * 2 ^ 1 Or
                       PF * FlagsTypes.PF Or
                        0 * 2 ^ 3 Or
                       AF * FlagsTypes.AF Or
                        0 * 2 ^ 5 Or
                       ZF * FlagsTypes.ZF Or
                       SF * FlagsTypes.SF Or
                       TF * FlagsTypes.TF Or
                     [IF] * FlagsTypes.IF Or
                       DF * FlagsTypes.DF Or
                     [OF] * FlagsTypes.OF Or
                            &HF800 ' IOPL, NT and bit 15 are always "1" on 8086
            End Get
            Set(value As UInt16)
                CF = If((value And FlagsTypes.CF) = FlagsTypes.CF, 1, 0)
                ' Reserved 1
                PF = If((value And FlagsTypes.PF) = FlagsTypes.PF, 1, 0)
                ' Reserved 0
                AF = If((value And FlagsTypes.AF) = FlagsTypes.AF, 1, 0)
                ' Reserved 0
                ZF = If((value And FlagsTypes.ZF) = FlagsTypes.ZF, 1, 0)
                SF = If((value And FlagsTypes.SF) = FlagsTypes.SF, 1, 0)
                TF = If((value And FlagsTypes.TF) = FlagsTypes.TF, 1, 0)
                [IF] = If((value And FlagsTypes.IF) = FlagsTypes.IF, 1, 0)
                DF = If((value And FlagsTypes.DF) = FlagsTypes.DF, 1, 0)
                [OF] = If((value And FlagsTypes.OF) = FlagsTypes.OF, 1, 0)
            End Set
        End Property

        Public Function Clone() As Object Implements ICloneable.Clone
            Return New GPFlags With {.EFlags = EFlags}
        End Function
    End Class

    Public Sub LoadBIN(fileName As String, segment As UInt16, offset As UInt16)
        X8086.Notify($"Loading: {fileName} @ {segment:X4}:{offset:X4}", NotificationReasons.Info)
        fileName = X8086.FixPath(fileName)

        If IO.File.Exists(fileName) Then
            CopyToMemory(IO.File.ReadAllBytes(fileName), segment, offset)
        Else
            ThrowException("File Not Found: " + vbCrLf + fileName)
        End If
    End Sub

    Public Sub CopyToMemory(bytes() As Byte, segment As UInt16, offset As UInt16)
        CopyToMemory(bytes, X8086.SegmentOffetToAbsolute(segment, offset))
    End Sub

    Public Sub CopyToMemory(bytes() As Byte, address As UInt32)
        ' TODO: We need to implement some checks to prevent loading code into ROM areas.
        '       Something like this, for example:
        '       If address + bytes.Length >= ROMStart Then ...
        Array.Copy(bytes, 0, Memory, address, bytes.Length)
    End Sub

    Public Sub CopyFromMemory(bytes() As Byte, address As UInt32)
        Array.Copy(Memory, address, bytes, 0, bytes.Length)
    End Sub

    Public Property Registers As GPRegisters
        Get
            Return mRegisters
        End Get
        Set(value As GPRegisters)
            mRegisters = value
        End Set
    End Property

    Public Property Flags As GPFlags
        Get
            Return mFlags
        End Get
        Set(value As GPFlags)
            mFlags = value
        End Set
    End Property

    Private Sub PushIntoStack(value As UInt16)
        mRegisters.SP -= 2
        RAM16(mRegisters.SS, mRegisters.SP,, True) = value
    End Sub

    Private Function PopFromStack() As UInt16
        mRegisters.SP += 2
        Return RAM16(mRegisters.SS, mRegisters.SP - 2,, True)
    End Function

    Public Shared Function SegmentOffetToAbsolute(segment As UInt16, offset As UInt16) As UInt32
        Return ((CUInt(segment) << 4) + offset)
    End Function

    Public Shared Function AbsoluteToSegment(address As UInt32) As UInt16
        Return (address >> 4) And &HF_FF00
    End Function

    Public Shared Function AbsoluteToOffset(address As UInt32) As UInt16
        Return address And &HFFF
    End Function

    Public Property RAM(address As UInt32, Optional ignoreHooks As Boolean = False) As Byte
        Get
            'If mDebugMode Then RaiseEvent MemoryAccess(Me, New MemoryAccessEventArgs(address, MemoryAccessEventArgs.AccessModes.Read))
            'Return FromPreftch(address)

            If Not ignoreHooks Then
                For i As Integer = 0 To memHooks.Count - 1
                    If memHooks(i).Invoke(address, tmpUVal1, MemHookMode.Read) Then Return tmpUVal1
                Next
            End If

            Return Memory(address And &HF_FFFF) ' "Call 5" Legacy Interface: http://www.os2museum.com/wp/?p=734
        End Get
        Set(value As Byte)
            ' For edit.com:
            ' Create a breakpoint when writing to address AS:0380
            ' As should be 4F0A
            'Dim tmp = SegmentOffetToAbsolute(&H4F0A, &H380)
            'If tmp = address Then DebugMode = True

            If Not ignoreHooks Then
                For i As Integer = 0 To memHooks.Count - 1
                    If memHooks(i).Invoke(address, value, MemHookMode.Write) Then Exit Property
                Next
            End If

            Memory(address) = value

            'If mDebugMode Then RaiseEvent MemoryAccess(Me, New MemoryAccessEventArgs(address, MemoryAccessEventArgs.AccessModes.Write))
        End Set
    End Property

    Public Property RAM8(segment As UInt16, offset As UInt16, Optional inc As Byte = 0, Optional ignoreHooks As Boolean = False) As Byte
        Get
            Return RAM(SegmentOffetToAbsolute(segment, offset + inc), ignoreHooks)
        End Get
        Set(value As Byte)
            RAM(SegmentOffetToAbsolute(segment, offset + inc), ignoreHooks) = value
        End Set
    End Property

    Public Property RAM16(segment As UInt16, offset As UInt16, Optional inc As Byte = 0, Optional ignoreHooks As Boolean = False) As UInt16
        Get
            address = SegmentOffetToAbsolute(segment, offset + inc)
            Return (CUInt(RAM(address + 1, ignoreHooks)) << 8) Or RAM(address, ignoreHooks)
        End Get
        Set(value As UInt16)
            address = SegmentOffetToAbsolute(segment, offset + inc)
            RAM(address, ignoreHooks) = value
            RAM(address + 1, ignoreHooks) = value >> 8
        End Set
    End Property

    Public Property RAMn(Optional ignoreHooks As Boolean = False) As UInt16
        Get
            Return If(addrMode.Size = DataSize.Byte,
                        RAM8(mRegisters.ActiveSegmentValue, addrMode.IndAdr,, ignoreHooks),
                        RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr,, ignoreHooks))
        End Get
        Set(value As UInt16)
            If addrMode.Size = DataSize.Byte Then
                RAM8(mRegisters.ActiveSegmentValue, addrMode.IndAdr,, ignoreHooks) = value
            Else
                RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr,, ignoreHooks) = value
            End If
        End Set
    End Property
End Class
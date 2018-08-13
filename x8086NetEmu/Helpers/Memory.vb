﻿Partial Public Class X8086
    Public Const MemSize As UInteger = &H100000UI  ' 1MB
    Public Const ROMStart As UInteger = &HC0000UI

    Public Memory(MemSize - 1) As Byte

    Private Const shl2 As UInteger = 1 << 2
    Private Const shl3 As UInteger = 1 << 3

    Public Class MemoryAccessEventArgs
        Inherits EventArgs

        Public Enum AccessModes
            Read
            Write
        End Enum

        Public ReadOnly Property AccessMode As AccessModes
        Public ReadOnly Property Address As UInteger

        Public Sub New(address As UInteger, accesMode As AccessModes)
            Me.Address = address
            Me.AccessMode = accesMode
        End Sub
    End Class

    Public Event MemoryAccess(sender As Object, e As MemoryAccessEventArgs)

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

            'ES = (AL + BX + 1) Or shl3
            'CS = ES + 1
            'SS = ES + 2
            'DS = ES + 3
            'SP = (AH + BX + 1) Or shl3
            'BP = (CH + BX + 1) Or shl3
            'SI = (DH + BX + 1) Or shl3
            'DI = (BH + BX + 1) Or shl3
            'IP = DI + 1
        End Enum

        Private mActiveSegmentRegister As RegistersTypes = RegistersTypes.DS
        Private mActiveSegmentChanged As Boolean = False

        Public Property Val(reg As RegistersTypes) As UShort
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

                    Case Else : Throw New Exception("Invalid Register")
                End Select
            End Get
            Set(value As UShort)
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

                    Case Else : Throw New Exception("Invalid Register")
                End Select
            End Set
        End Property

        Public Property AX As UShort
            Get
                Return ((CShort(AH) << 8) Or AL)
            End Get
            Set(value As UShort)
                AH = value >> 8
                AL = value
            End Set
        End Property
        Public Property AL As Byte
        Public Property AH As Byte

        Public Property BX As UShort
            Get
                Return ((CShort(BH) << 8) Or BL)
            End Get
            Set(value As UShort)
                BH = value >> 8
                BL = value
            End Set
        End Property
        Public Property BL As Byte
        Public Property BH As Byte

        Public Property CX As UShort
            Get
                Return ((CShort(CH) << 8) Or CL)
            End Get
            Set(value As UShort)
                CH = value >> 8
                CL = value
            End Set
        End Property
        Public Property CL As Byte
        Public Property CH As Byte

        Public Property DX As UShort
            Get
                Return ((CShort(DH) << 8) Or DL)
            End Get
            Set(value As UShort)
                DH = value >> 8
                DL = value
            End Set
        End Property
        Public Property DL As Byte
        Public Property DH As Byte

        Public Property CS As UShort
        Public Property IP As UShort

        Public Property SS As UShort
        Public Property SP As UShort

        Public Property DS As UShort
        Public Property SI As UShort

        Public Property ES As UShort
        Public Property DI As UShort

        Public Property BP As UShort

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

        Public ReadOnly Property ActiveSegmentValue As UInteger
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
            Dim reg = New GPRegisters With {
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
            Return reg
        End Function
    End Class

    Public Class GPFlags
        Implements ICloneable

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

        Public Property EFlags() As UShort
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
                            &HF000 ' IOPL, NT and bit 15 are always "1" on 8086
            End Get
            Set(value As UShort)
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

    Public Sub LoadBIN(fileName As String, segment As UShort, offset As UShort)
        'Debug.WriteLine($"Loading: {fileName} @ {segment:X4}:{offset:X4}")
        fileName = X8086.FixPath(fileName)

        If IO.File.Exists(fileName) Then
            CopyToRAM(IO.File.ReadAllBytes(fileName), segment, offset)
        Else
            ThrowException("File Not Found: " + vbCrLf + fileName)
        End If
    End Sub

    Public Sub CopyToRAM(bytes() As Byte, segment As UShort, offset As UShort)
        CopyToRAM(bytes, X8086.SegmentOffetToAbsolute(segment, offset))
    End Sub

    Public Sub CopyToRAM(bytes() As Byte, address As UInteger)
        ' TODO: We need to implement some checks to prevent loading code into ROM areas.
        '       Something like this, for example:
        '       If address + bytes.Length >= ROMStart Then Stop
        Array.Copy(bytes, 0, Memory, address, bytes.Length)
    End Sub

    Public Sub CopyFromRAM(bytes() As Byte, address As UInteger)
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

    Private Sub PushIntoStack(value As UShort)
        mRegisters.SP -= 2
        RAM16(mRegisters.SS, mRegisters.SP) = value
    End Sub

    Private Function PopFromStack() As UShort
        Dim value As Integer = RAM16(mRegisters.SS, mRegisters.SP)
        mRegisters.SP += 2
        Return value
    End Function

    Public Shared Function SegmentOffetToAbsolute(segment As UShort, offset As UShort) As UInteger
        Return (CUInt(segment) << 4) + offset
    End Function

    Public Shared Function AbsoluteToSegment(address As UInteger) As UShort
        Return (address >> 4) And &HFFF00
    End Function

    Public Shared Function AbsoluteToOffset(address As UInteger) As UShort
        Return address And &HFFF
    End Function

    Public Property RAM(address As UInteger) As Byte
        Get
            'If mDebugMode Then RaiseEvent MemoryAccess(Me, New MemoryAccessEventArgs(address, MemoryAccessEventArgs.AccessModes.Read))
            'Return FromPreftch(address)

            For i As Integer = 0 To memHooks.Count - 1
                If memHooks(i).Invoke(address, tmpVal, MemHookMode.Read) Then Return tmpVal
            Next

            Return Memory(address And &HFFFFFUI) ' "Call 5" Legacy Interface: http://www.os2museum.com/wp/?p=734
        End Get
        Set(value As Byte)
            'If address >= ROMStart OrElse Memory(address) = value Then Exit Property
            If address >= ROMStart Then Exit Property

            For i As Integer = 0 To memHooks.Count - 1
                If memHooks(i).Invoke(address, value, MemHookMode.Write) Then Exit Property
            Next

            Memory(address) = value

            'If mDebugMode Then RaiseEvent MemoryAccess(Me, New MemoryAccessEventArgs(address, MemoryAccessEventArgs.AccessModes.Write))
        End Set
    End Property

    Public Property RAM8(segment As UShort, offset As UShort, Optional inc As Byte = 0) As Byte
        Get
            Return RAM(SegmentOffetToAbsolute(segment, offset + inc))
        End Get
        Set(value As Byte)
            RAM(SegmentOffetToAbsolute(segment, offset + inc)) = value
        End Set
    End Property

    Public Property RAM16(segment As UShort, offset As UShort, Optional inc As Byte = 0) As UInteger
        Get
            Dim address As UInteger = SegmentOffetToAbsolute(segment, offset + inc)
            Return (CUInt(RAM(address + 1UI)) << 8UI Or RAM(address))
        End Get
        Set(value As UInteger)
            Dim address As UInteger = SegmentOffetToAbsolute(segment, offset + inc)
            RAM(address) = value
            RAM(address + 1UI) = (value >> 8UI)
        End Set
    End Property

    Public Property RAMn() As UInteger
        Get
            If addrMode.Size = DataSize.Byte Then
                Return RAM8(mRegisters.ActiveSegmentValue, addrMode.IndAdr)
            Else
                Return RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr)
            End If
        End Get
        Set(value As UInteger)
            If addrMode.Size = DataSize.Byte Then
                RAM8(mRegisters.ActiveSegmentValue, addrMode.IndAdr) = CByte(value)
            Else
                RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr) = value
            End If
        End Set
    End Property
End Class
Partial Public Class x8086
    Public Const MemSize As UInteger = &H100000
    Public Const ROMStart As UInteger = &HC0000

    Public Memory(MemSize - 1) As Byte

    Private Const shl3 As UInteger = 1 << 3
    Private Const shl2 As UInteger = 1 << 2

    Public Class MemoryAccessEventArgs
        Inherits EventArgs

        Public Enum AccessModes
            Read
            Write
        End Enum

        Private mAccessMode As AccessModes
        Private mAddress As UInteger

        Public Sub New(address As UInteger, accesMode As AccessModes)
            mAddress = address
            mAccessMode = accesMode
        End Sub

        Public ReadOnly Property AccessMode As AccessModes
            Get
                Return mAccessMode
            End Get
        End Property

        Public ReadOnly Property Address As UInteger
            Get
                Return mAddress
            End Get
        End Property
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

            ES = (AL + BX + 1) Or shl3
            CS = ES + 1
            SS = ES + 2
            DS = ES + 3
            SP = (AH + BX + 1) Or shl3
            BP = (CH + BX + 1) Or shl3
            SI = (DH + BX + 1) Or shl3
            DI = (BH + BX + 1) Or shl3
            IP = DI + 1
        End Enum

        Private mActiveSegmentRegister As GPRegisters.RegistersTypes = RegistersTypes.DS
        Private mActiveSegmentChanged As Boolean = False
        Private mSS As UInteger

        Public Property Val(reg As RegistersTypes) As UInteger
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
            Set(value As UInteger)
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

        Public Property AX As UInteger
            Get
                Return ((AH << 8) Or AL)
            End Get
            Set(value As UInteger)
                AH = value >> 8
                AL = value And &HFF
            End Set
        End Property
        Public Property AL As UShort
        Public Property AH As UShort

        Public Property BX As UInteger
            Get
                Return ((BH << 8) Or BL)
            End Get
            Set(value As UInteger)
                BH = value >> 8
                BL = value And &HFF
            End Set
        End Property
        Public Property BL As UShort
        Public Property BH As UShort

        Public Property CX As UInteger
            Get
                Return ((CH << 8) Or CL)
            End Get
            Set(value As UInteger)
                CH = value >> 8
                CL = value And &HFF
            End Set
        End Property
        Public Property CL As UShort
        Public Property CH As UShort

        Public Property DX As UInteger
            Get
                Return ((DH << 8) Or DL)
            End Get
            Set(value As UInteger)
                DH = value >> 8
                DL = value And &HFF
            End Set
        End Property
        Public Property DL As UShort
        Public Property DH As UShort

        Public Property CS As UInteger
        Public Property IP As UInteger

        Public Property SS As UInteger
            Get
                Return mSS
            End Get
            Set(value As UInteger)
                mSS = value
                ignoreINTs = True ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
            End Set
        End Property
        Public Property SP As UInteger

        Public Property DS As UInteger
        Public Property SI As UInteger

        Public Property ES As UInteger
        Public Property DI As UInteger

        Public Property BP As UInteger

        Public Sub ResetSegmentRegister()
            mActiveSegmentChanged = False
            mActiveSegmentRegister = RegistersTypes.DS
        End Sub

        Public Property ActiveSegmentRegister As GPRegisters.RegistersTypes
            Get
                Return mActiveSegmentRegister
            End Get
            Set(value As GPRegisters.RegistersTypes)
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
                Return CS.ToHex(DataSize.Word) + ":" + IP.ToHex(DataSize.Word)
            End Get
        End Property

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Dim reg = New GPRegisters()
            reg.AX = AX
            reg.BX = BX
            reg.CX = CX
            reg.DX = DX
            reg.ES = ES
            reg.CS = CS
            reg.SS = SS
            reg.DS = DS
            reg.SP = SP
            reg.BP = BP
            reg.SI = SI
            reg.DI = DI
            reg.IP = IP
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

        Public Property CF As UShort
        Public Property PF As UShort
        Public Property AF As UShort
        Public Property ZF As UShort
        Public Property SF As UShort
        Private mTF As UShort
        Public Property [IF] As UShort
        Public Property DF As UShort
        Public Property [OF] As UShort
        Public Property TF As UShort

        Public Property EFlags() As UShort
            Get
                Return CF * FlagsTypes.CF +
                        1 * 2 ^ 1 +
                       PF * FlagsTypes.PF +
                        0 * 2 ^ 3 +
                       AF * FlagsTypes.AF +
                        0 * 2 ^ 5 +
                       ZF * FlagsTypes.ZF +
                       SF * FlagsTypes.SF +
                       TF * FlagsTypes.TF +
                     [IF] * FlagsTypes.IF +
                       DF * FlagsTypes.DF +
                     [OF] * FlagsTypes.OF
            End Get
            Set(value As UShort)
                CF = If((value And FlagsTypes.CF) = FlagsTypes.CF, 1, 0)
                PF = If((value And FlagsTypes.PF) = FlagsTypes.PF, 1, 0)
                AF = If((value And FlagsTypes.AF) = FlagsTypes.AF, 1, 0)
                ZF = If((value And FlagsTypes.ZF) = FlagsTypes.ZF, 1, 0)
                SF = If((value And FlagsTypes.SF) = FlagsTypes.SF, 1, 0)
                TF = If((value And FlagsTypes.TF) = FlagsTypes.TF, 1, 0)
                [IF] = If((value And FlagsTypes.IF) = FlagsTypes.IF, 1, 0)
                DF = If((value And FlagsTypes.DF) = FlagsTypes.DF, 1, 0)
                [OF] = If((value And FlagsTypes.OF) = FlagsTypes.OF, 1, 0)
            End Set
        End Property

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Dim f As GPFlags = New GPFlags()
            f.EFlags = EFlags
            Return f
        End Function
    End Class

    Public Sub LoadBIN(fileName As String, segment As UInteger, offset As UInteger)
        fileName = x8086.FixPath(fileName)

        If IO.File.Exists(fileName) Then
            CopyToRAM(IO.File.ReadAllBytes(fileName), segment, offset)
        Else
            ThrowException("File Not Found: " + vbCrLf + fileName)
        End If
    End Sub

    Public Sub CopyToRAM(bytes() As Byte, segment As UInteger, offset As UInteger)
        CopyToRAM(bytes, x8086.SegOffToAbs(segment, offset))
    End Sub

    Public Sub CopyToRAM(bytes() As Byte, address As UInteger)
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
        mRegisters.SP = AddValues(mRegisters.SP, -2, DataSize.Word)
        RAM16(mRegisters.SS, mRegisters.SP) = value
    End Sub

    Private Function PopFromStack() As UInteger
        Dim value As UInteger = RAM16(mRegisters.SS, mRegisters.SP)
        mRegisters.SP = AddValues(mRegisters.SP, 2, DataSize.Word)
        Return value
    End Function

    Public Shared Function SegOffToAbs(segment As UInteger, offset As UInteger) As UInteger
        Return (segment << 4) + offset
    End Function

    Public Shared Function AbsToSeg(address As UInteger) As UInteger
        Return (address >> 4) And &HFFF00
    End Function

    Public Shared Function AbsoluteToOff(address As UInteger) As UInteger
        Return address And &HFFF
    End Function

    Public Property RAM(address As UInteger) As Byte
        Get
            'If mDebugMode Then RaiseEvent MemoryAccess(Me, New MemoryAccessEventArgs(address, MemoryAccessEventArgs.AccessModes.Read))
            Return FromPreftch(address)
            'Return Memory(address And &HFFFFF) ' "Call 5" Legacy Interface: http://www.os2museum.com/wp/?p=734
        End Get
        Set(value As Byte)
            address = address And &HFFFFF
            If address < ROMStart Then
                Memory(address) = value

                ' This considerably speeds up the emulator, but breaks cursor blinking...
                'If mVideoAdapter IsNot Nothing Then
                '    Select Case mVideoAdapter.MainMode
                '        Case CGAAdapter.MainModes.Text
                '            If address >= mVideoAdapter.StartTextVideoAddress AndAlso address <= mVideoAdapter.EndTextVideoAddress Then mVideoAdapter.Update()
                '        Case CGAAdapter.MainModes.Graphics
                '            If address >= mVideoAdapter.StartGraphicsVideoAddress AndAlso address <= mVideoAdapter.EndTextVideoAddress Then mVideoAdapter.Update()
                '    End Select
                'End If
            End If
            'If mDebugMode Then RaiseEvent MemoryAccess(Me, New MemoryAccessEventArgs(address, MemoryAccessEventArgs.AccessModes.Write))
        End Set
    End Property

    Public Property RAM8(segment As UInteger, offset As UInteger, Optional inc As Integer = 0) As Byte
        Get
            Return RAM(SegOffToAbs(segment, offset + inc))
        End Get
        Set(value As Byte)
            RAM(SegOffToAbs(segment, offset + inc)) = value
        End Set
    End Property

    Public Property RAM16(segment As UInteger, offset As UInteger, Optional inc As Integer = 0) As UShort
        Get
            Dim address As UInteger = SegOffToAbs(segment, offset + inc)
            'Return RAM(address + 1) * 256 + RAM(address)
            Return CUShort(RAM(address + 1)) << 8 Or RAM(address)
        End Get
        Set(value As UShort)
            Dim address As UInteger = SegOffToAbs(segment, offset + inc)
            RAM(address) = value And &HFF
            RAM(address + 1) = (value >> 8)
        End Set
    End Property

    Public Property RAMn() As UShort
        Get
            If addrMode.Size = DataSize.Byte Then
                Return RAM8(mRegisters.ActiveSegmentValue, addrMode.IndAdr)
            Else
                Return RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr)
            End If
        End Get
        Set(value As UShort)
            If addrMode.Size = DataSize.Byte Then
                RAM8(mRegisters.ActiveSegmentValue, addrMode.IndAdr) = value
            Else
                RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr) = value
            End If
        End Set
    End Property
End Class
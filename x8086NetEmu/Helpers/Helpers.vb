Partial Public Class X8086
    Private tmpCF As Byte
    Private portsCache As New Dictionary(Of UInt32, IOPortHandler)
    Private ReadOnly szpLUT8(256 - 1) As GPFlags.FlagsTypes
    Private ReadOnly szpLUT16(65536 - 1) As GPFlags.FlagsTypes
    Private ReadOnly decoderCache(65536 - 1) As AddressingMode

    Public Enum ParamIndex
        First = 0
        Second = 1
        Thrid = 2
    End Enum

    Public Enum DataSize
        UseAddressingMode = -1
        [Byte] = 0
        Word = 1
        DWord = 2
    End Enum

    Public Enum Operation
        Add
        AddWithCarry
        Substract
        SubstractWithCarry
        LogicOr
        LogicAnd
        LogicXor
        Increment
        Decrement
        Compare
        Test
        Unknown
    End Enum

    Public Structure AddressingMode
        Public Direction As Byte
        Public Size As DataSize
        Public Modifier As Byte
        Public Rm As Byte
        Public Reg As Byte
        Public Register1 As GPRegisters.RegistersTypes
        Public Register2 As GPRegisters.RegistersTypes
        Public IsDirect As Boolean
        Public IndAdr As UInt16    ' Indirect Address
        Public IndMem As UInt16    ' Indirect Memory Contents

        Public Src As GPRegisters.RegistersTypes
        Public Dst As GPRegisters.RegistersTypes

        Private regOffset As Byte

        ' http://aturing.umcs.maine.edu/~meadow/courses/cos335/8086-instformat.pdf
        Public Sub Decode(data As Byte, addressingModeByte As Byte)
            Size = data And 1                                 ' (0000 0001)
            Direction = (data >> 1) And 1                     ' (0000 0010)
            Modifier = addressingModeByte >> 6                ' (1100 0000)
            Reg = (addressingModeByte >> 3) And 7             ' (0011 1000)
            Rm = addressingModeByte And 7                     ' (0000 0111)

            regOffset = Size << 3

            Register1 = Reg Or regOffset
            If Register1 >= GPRegisters.RegistersTypes.ES Then Register1 += GPRegisters.RegistersTypes.ES

            Register2 = Rm Or regOffset
            If Register2 >= GPRegisters.RegistersTypes.ES Then Register2 += GPRegisters.RegistersTypes.ES

            If Direction = 0 Then
                Src = Register1
                Dst = Register2
            Else
                Src = Register2
                Dst = Register1
            End If
        End Sub
    End Structure

    Private Sub SetRegister1Alt(data As Byte)
        addrMode.Register1 = (data And &H7) Or shl3
        If addrMode.Register1 >= GPRegisters.RegistersTypes.ES Then addrMode.Register1 += GPRegisters.RegistersTypes.ES
        addrMode.Size = DataSize.Word
    End Sub

    Private Sub SetRegister2ToSegReg()
        addrMode.Register2 = (addrMode.Reg And &H3) + GPRegisters.RegistersTypes.ES
        addrMode.Size = DataSize.Word
    End Sub

    Private Sub SetAddressing(Optional forceSize As DataSize = DataSize.UseAddressingMode)
#If DEBUG Then
        addrMode.Decode(opCode, RAM8(mRegisters.CS, mRegisters.IP + 1))
#Else
        addrMode = decoderCache((Convert.ToUInt16(opCode) << 8) Or RAM8(mRegisters.CS, mRegisters.IP + 1))
#End If

        If forceSize <> DataSize.UseAddressingMode Then addrMode.Size = forceSize

        ' AS = Active Segment
        ' AS = SS when Rm = 2, 3 or 6
        ' If Rm = 6 and Modifier = 0, AS will be set to DS instead
        ' http://www.ic.unicamp.br/~celio/mc404s2-03/addr_modes/intel_addr.html

        If Not mRegisters.ActiveSegmentChanged Then
            If addrMode.Rm = 2 OrElse addrMode.Rm = 3 OrElse
                (addrMode.Rm = 6 AndAlso addrMode.Modifier <> 0) Then
                mRegisters.ActiveSegmentRegister = GPRegisters.RegistersTypes.SS
            End If
        End If

        ' http://umcs.maine.edu/~cmeadow/courses/cos335/Asm07-MachineLanguage.pdf
        ' http://maven.smith.edu/~thiebaut/ArtOfAssembly/CH04/CH04-2.html#HEADING2-35
        Select Case addrMode.Modifier
            Case 0 ' 00
                addrMode.IsDirect = False
                Select Case addrMode.Rm
                    Case 0 : addrMode.IndAdr = mRegisters.BX + mRegisters.SI : clkCyc += 7                        ' 000 [BX+SI]
                    Case 1 : addrMode.IndAdr = mRegisters.BX + mRegisters.DI : clkCyc += 8                        ' 001 [BX+DI]
                    Case 2 : addrMode.IndAdr = mRegisters.BP + mRegisters.SI : clkCyc += 8                        ' 010 [BP+SI]
                    Case 3 : addrMode.IndAdr = mRegisters.BP + mRegisters.DI : clkCyc += 7                        ' 011 [BP+DI]
                    Case 4 : addrMode.IndAdr = mRegisters.SI : clkCyc += 5                                        ' 100 [SI]
                    Case 5 : addrMode.IndAdr = mRegisters.DI : clkCyc += 5                                        ' 101 [DI]
                    Case 6 : addrMode.IndAdr = To32bitsWithSign(Param(ParamIndex.First, 2, DataSize.Word)) : clkCyc += 9  ' 110 Direct Addressing
                    Case 7 : addrMode.IndAdr = mRegisters.BX : clkCyc += 5                                        ' 111 [BX]
                End Select
                addrMode.IndMem = RAMn

            Case 1 ' 01 - 8bit
                addrMode.IsDirect = False
                Select Case addrMode.Rm
                    Case 0 : addrMode.IndAdr = mRegisters.BX + mRegisters.SI : clkCyc += 7                        ' 000 [BX+SI]
                    Case 1 : addrMode.IndAdr = mRegisters.BX + mRegisters.DI : clkCyc += 8                        ' 001 [BX+DI]
                    Case 2 : addrMode.IndAdr = mRegisters.BP + mRegisters.SI : clkCyc += 8                        ' 010 [BP+SI]
                    Case 3 : addrMode.IndAdr = mRegisters.BP + mRegisters.DI : clkCyc += 7                        ' 011 [BP+DI]
                    Case 5 : addrMode.IndAdr = mRegisters.DI : clkCyc += 5                                        ' 101 [DI]
                    Case 4 : addrMode.IndAdr = mRegisters.SI : clkCyc += 5                                        ' 100 [SI]
                    Case 6 : addrMode.IndAdr = mRegisters.BP : clkCyc += 5                                        ' 110 [BP]
                    Case 7 : addrMode.IndAdr = mRegisters.BX : clkCyc += 5                                        ' 111 [BX]
                End Select
                addrMode.IndAdr += To16bitsWithSign(Param(ParamIndex.First, 2, DataSize.Byte))
                addrMode.IndMem = RAMn

            Case 2 ' 10 - 16bit
                addrMode.IsDirect = False
                Select Case addrMode.Rm
                    Case 0 : addrMode.IndAdr = mRegisters.BX + mRegisters.SI : clkCyc += 7                        ' 000 [BX+SI]
                    Case 1 : addrMode.IndAdr = mRegisters.BX + mRegisters.DI : clkCyc += 8                        ' 001 [BX+DI]
                    Case 2 : addrMode.IndAdr = mRegisters.BP + mRegisters.SI : clkCyc += 8                        ' 010 [BP+SI]
                    Case 3 : addrMode.IndAdr = mRegisters.BP + mRegisters.DI : clkCyc += 7                        ' 011 [BP+DI]
                    Case 4 : addrMode.IndAdr = mRegisters.SI : clkCyc += 5                                        ' 100 [SI]
                    Case 5 : addrMode.IndAdr = mRegisters.DI : clkCyc += 5                                        ' 101 [DI]
                    Case 6 : addrMode.IndAdr = mRegisters.BP : clkCyc += 5                                        ' 110 [BP]
                    Case 7 : addrMode.IndAdr = mRegisters.BX : clkCyc += 5                                        ' 111 [BX]
                End Select
                addrMode.IndAdr += To32bitsWithSign(Param(ParamIndex.First, 2, DataSize.Word))
                addrMode.IndMem = RAMn

            Case 3 ' 11
                addrMode.IsDirect = True

        End Select

        opCodeSize += 1
    End Sub

    Private Function To16bitsWithSign(v As UInt16) As UInt16
        Return If((v And &H80) = 0, v, &HFF00 Or v)
    End Function

    Private Function To32bitsWithSign(v As UInt16) As UInt32
        Return If((v And &H8000) = 0, v, &HFFFF_0000UI Or v)
    End Function

    Private Function ToXbitsWithSign(v As UInt32) As UInt32
        Return If(addrMode.Size = DataSize.Byte, To16bitsWithSign(v), To32bitsWithSign(v))
    End Function

    Private Sub SendToPort(portAddress As UInt32, value As Byte)
        DoReschedule = True

        If portsCache.ContainsKey(portAddress) Then
            portsCache(portAddress).Out(portAddress, value)
            'X8086.Notify(String.Format("Write {0} to Port {1} on Adapter '{2}'", value.ToString("X2"), portAddress.ToString("X4"), portsCache(portAddress).Name), NotificationReasons.Info)
            Exit Sub
        Else
            For Each p As IOPortHandler In mPorts
                If p.RegisteredPorts.Contains(portAddress) Then
                    p.Out(portAddress, value)
                    'X8086.Notify(String.Format("Write {0} to Port {1} on Adapter '{2}'", value.ToString("X2"), portAddress.ToString("X4"), p.Name), NotificationReasons.Info)
                    portsCache.Add(portAddress, p)
                    Exit Sub
                End If
            Next

            For Each a As Adapter In mAdapters
                If a.RegisteredPorts.Contains(portAddress) Then
                    a.Out(portAddress, value)
                    'X8086.Notify(String.Format("Write {0} to Port {1} on Adapter '{2}'", value.ToString("X2"), portAddress.ToString("X4"), a.Name), NotificationReasons.Info)
                    portsCache.Add(portAddress, a)
                    Exit Sub
                End If
            Next
        End If

        NoIOPort(portAddress)
    End Sub

    Private Function ReceiveFromPort(portAddress As UInt32) As Byte
        DoReschedule = True

        If portsCache.ContainsKey(portAddress) Then
            'X8086.Notify(String.Format("Read From Port {0} on Adapter '{1}'", portAddress.ToString("X4"), portsCache(portAddress).Name), NotificationReasons.Info)
            Return portsCache(portAddress).In(portAddress)
        Else
            For Each p As IOPortHandler In mPorts
                If p.RegisteredPorts.Contains(portAddress) Then
                    'X8086.Notify(String.Format("Read From Port {0} on Adapter '{1}'", portAddress.ToString("X4"), p.Name), NotificationReasons.Info)
                    portsCache.Add(portAddress, p)
                    Return p.In(portAddress)
                End If
            Next

            For Each a As Adapter In mAdapters
                If a.RegisteredPorts.Contains(portAddress) Then
                    'X8086.Notify(String.Format("Read From Port {0} on Adapter '{1}'", portAddress.ToString("X4"), a.Name), NotificationReasons.Info)
                    portsCache.Add(portAddress, a)
                    Return a.In(portAddress)
                End If
            Next
        End If

        NoIOPort(portAddress)
        Return &HFF
    End Function

    Private ReadOnly Property Param(index As ParamIndex, Optional ipOffset As UInt16 = 1, Optional size As DataSize = DataSize.UseAddressingMode) As UInt16
        Get
            If size = DataSize.UseAddressingMode Then size = addrMode.Size
            opCodeSize += (size + 1)
            Return ParamNOPS(index, ipOffset, size)
        End Get
    End Property

    Private ReadOnly Property ParamNOPS(index As ParamIndex, Optional ipOffset As UInt16 = 1, Optional size As DataSize = DataSize.UseAddressingMode) As UInt16
        Get
            ' Extra cycles for address misalignment
            ' This is too CPU expensive, with few benefits, if any... not worth it
            'If (mRegisters.IP Mod 2) <> 0 Then clkCyc += 4

            Return If(size = DataSize.Byte OrElse (size = DataSize.UseAddressingMode AndAlso addrMode.Size = DataSize.Byte),
                        RAM8(mRegisters.CS, mRegisters.IP, ipOffset + index, True),
                        RAM16(mRegisters.CS, mRegisters.IP, ipOffset + index * 2, True))
        End Get
    End Property

    Private Function OffsetIP(size As DataSize) As UInt16
        Return mRegisters.IP +
                If(size = DataSize.Byte,
                    To16bitsWithSign(Param(ParamIndex.First, , size)),
                    Param(ParamIndex.First, , size)) +
                    opCodeSize
    End Function

    Private Function Eval(v1 As UInt32, v2 As UInt32, opMode As Operation, size As DataSize) As UInt16
        Dim result As UInt32

        Select Case opMode
            Case Operation.Add
                result = v1 + v2
                SetAddSubFlags(result, v1, v2, size, False)

            Case Operation.AddWithCarry
                result = v1 + v2 + mFlags.CF
                SetAddSubFlags(result, v1, v2, size, False)

            Case Operation.Substract, Operation.Compare
                result = v1 - v2
                SetAddSubFlags(result, v1, v2, size, True)

            Case Operation.SubstractWithCarry
                result = v1 - v2 - mFlags.CF
                SetAddSubFlags(result, v1, v2, size, True)

            Case Operation.LogicOr
                result = v1 Or v2
                SetLogicFlags(result, size)

            Case Operation.LogicAnd, Operation.Test
                result = v1 And v2
                SetLogicFlags(result, size)

            Case Operation.LogicXor
                result = v1 Xor v2
                SetLogicFlags(result, size)

            Case Operation.Increment
                result = v1 + v2
                tmpCF = mFlags.CF
                SetAddSubFlags(result, v1, v2, size, False)
                mFlags.CF = tmpCF

            Case Operation.Decrement
                result = v1 - v2
                tmpCF = mFlags.CF
                SetAddSubFlags(result, v1, v2, size, True)
                mFlags.CF = tmpCF

        End Select

        Return result
    End Function

    Private Sub SetSZPFlags(result As UInt32, size As DataSize)
        Dim ft As GPFlags.FlagsTypes = If(size = DataSize.Byte,
                                            szpLUT8(result And &HFF),
                                            szpLUT16(result And &HFFFF))

        mFlags.SF = If((ft And GPFlags.FlagsTypes.SF) = 0, 0, 1)
        mFlags.ZF = If((ft And GPFlags.FlagsTypes.ZF) = 0, 0, 1)
        mFlags.PF = If((ft And GPFlags.FlagsTypes.PF) = 0, 0, 1)
    End Sub

    Private Sub SetLogicFlags(result As UInt32, size As DataSize)
        SetSZPFlags(result, size)

        mFlags.CF = 0
        mFlags.OF = 0
    End Sub

    Private Sub SetAddSubFlags(result As UInt32, v1 As UInt32, v2 As UInt32, size As DataSize, isSubstraction As Boolean)
        SetSZPFlags(result, size)

        If size = DataSize.Byte Then
            mFlags.CF = If((result And &HFF00) = 0, 0, 1)
            mFlags.OF = If(((result Xor v1) And (If(isSubstraction, v1, result) Xor v2) And &H80) = 0, 0, 1)
        Else
            mFlags.CF = If((result And &HFFFF_0000UI) = 0, 0, 1)
            mFlags.OF = If(((result Xor v1) And (If(isSubstraction, v1, result) Xor v2) And &H8000) = 0, 0, 1)
        End If

        mFlags.AF = If(((v1 Xor v2 Xor result) And &H10) = 0, 0, 1)
    End Sub

    Protected Friend Sub SetUpAdapter(adapter As Adapter)
        Select Case adapter.Type
            Case Adapter.AdapterType.Keyboard
                mKeyboard = adapter
            Case Adapter.AdapterType.SerialMouseCOM1
                mMouse = adapter
            Case Adapter.AdapterType.Video
                mVideoAdapter = adapter
                SetupSystem()
            Case Adapter.AdapterType.Floppy
                mFloppyController = adapter
            Case Adapter.AdapterType.AudioDevice
                audioSubSystem.Providers.Add(adapter)
        End Select
    End Sub

    Private Sub AddInternalHooks()
        If mEmulateINT13 Then TryAttachHook(&H13, AddressOf HandleINT13) ' Disk I/O Emulation
    End Sub

    Private Sub BuildDecoderCache()
        For i As Integer = 0 To 255
            For j As Integer = 0 To 255
                addrMode.Decode(i, j)
                decoderCache((i << 8) Or j) = addrMode
            Next
        Next
    End Sub

    Private Sub BuildSZPTables()
        Dim d As UInt32

        For c As Integer = 0 To szpLUT8.Length - 1
            d = 0
            If (c And 1) <> 0 Then d += 1
            If (c And 2) <> 0 Then d += 1
            If (c And 4) <> 0 Then d += 1
            If (c And 8) <> 0 Then d += 1
            If (c And 16) <> 0 Then d += 1
            If (c And 32) <> 0 Then d += 1
            If (c And 64) <> 0 Then d += 1
            If (c And 128) <> 0 Then d += 1

            szpLUT8(c) = If((d And 1) <> 0, 0, GPFlags.FlagsTypes.PF)
            If c = 0 Then szpLUT8(c) = szpLUT8(c) Or GPFlags.FlagsTypes.ZF
            If (c And &H80) <> 0 Then szpLUT8(c) = szpLUT8(c) Or GPFlags.FlagsTypes.SF
        Next

        For c As Integer = 0 To szpLUT16.Length - 1
            d = 0
            If (c And 1) <> 0 Then d += 1
            If (c And 2) <> 0 Then d += 1
            If (c And 4) <> 0 Then d += 1
            If (c And 8) <> 0 Then d += 1
            If (c And 16) <> 0 Then d += 1
            If (c And 32) <> 0 Then d += 1
            If (c And 64) <> 0 Then d += 1
            If (c And 128) <> 0 Then d += 1

            szpLUT16(c) = If((d And 1) <> 0, 0, GPFlags.FlagsTypes.PF)
            If c = 0 Then szpLUT16(c) = szpLUT16(c) Or GPFlags.FlagsTypes.ZF
            If (c And &H8000) <> 0 Then szpLUT16(c) = szpLUT16(c) Or GPFlags.FlagsTypes.SF
        Next
    End Sub

    ' If necessary, in future versions we could implement support for
    '   multiple hooks attached to the same interrupt and execute them based on some priority condition
    Public Function TryAttachHook(intNum As Byte, handler As IntHandler) As Boolean
        If intHooks.ContainsKey(intNum) Then intHooks.Remove(intNum)
        intHooks.Add(intNum, handler)
        Return True
    End Function

    Public Function TryAttachHook(handler As MemHandler) As Boolean
        memHooks.Add(handler)
        Return True
    End Function

    Public Function TryDetachHook(intNum As Byte) As Boolean
        If Not intHooks.ContainsKey(intNum) Then Return False
        intHooks.Remove(intNum)
        Return True
    End Function

    Public Function TryDetachHook(memHandler As MemHandler) As Boolean
        If Not memHooks.Contains(memHandler) Then Return False
        memHooks.Remove(memHandler)
        Return True
    End Function

    Public Function GetAdaptersByType(adapterType As Adapter.AdapterType) As List(Of Adapter)
        Return (From adptr In mAdapters Where adptr.Type = adapterType Select adptr).ToList()
    End Function

    Public Shared ReadOnly Property IsRunningOnMono As Boolean
        Get
            Return Type.GetType("Mono.Runtime") IsNot Nothing
        End Get
    End Property

    Public Shared Function FixPath(fileName As String) As String
        If HostRuntime.Platform = HostRuntime.Platforms.Windows Then
            Return fileName
        Else
            Return If(Environment.OSVersion.Platform = PlatformID.Unix,
                    fileName.Replace("\", IO.Path.DirectorySeparatorChar),
                    fileName)
        End If
    End Function

    Private Sub PrintOpCodes(n As UInt16)
        For i As Integer = mRegisters.IP To mRegisters.IP + n - 1
            Debug.Write(RAM8(mRegisters.CS, i).ToString("X") + " ")
        Next
    End Sub

    Private Sub PrintRegisters()
        X8086.Notify("AX: {0}   SP: {1} ", NotificationReasons.Info, mRegisters.AX.ToString("X4"), mRegisters.SP.ToString("X4"))
        X8086.Notify("BX: {0}   DI: {1} ", NotificationReasons.Info, mRegisters.BX.ToString("X4"), mRegisters.DI.ToString("X4"))
        X8086.Notify("CX: {0}   BP: {1} ", NotificationReasons.Info, mRegisters.CX.ToString("X4"), mRegisters.BP.ToString("X4"))
        X8086.Notify("DX: {0}   SI: {1} ", NotificationReasons.Info, mRegisters.DX.ToString("X4"), mRegisters.SI.ToString("X4"))
        X8086.Notify("ES: {0}   CS: {1} ", NotificationReasons.Info, mRegisters.ES.ToString("X4"), mRegisters.CS.ToString("X4"))
        X8086.Notify("SS: {0}   DS: {1} ", NotificationReasons.Info, mRegisters.SS.ToString("X4"), mRegisters.DS.ToString("X4"))
        X8086.Notify("IP: {0} FLGS: {1}{2}{3}{4}{5}{6}{7}{8}", NotificationReasons.Info,
                                                        mRegisters.IP.ToString("X4"),
                                                        mFlags.CF,
                                                        mFlags.ZF,
                                                        mFlags.SF,
                                                        mFlags.OF,
                                                        mFlags.PF,
                                                        mFlags.AF,
                                                        mFlags.IF,
                                                        mFlags.DF)
        X8086.Notify("                CZSOPAID", NotificationReasons.Info)
        For i As Integer = 0 To 3
            Debug.Write(RAM8(mRegisters.CS, mRegisters.IP + i).ToString("X2") + " ")
        Next
    End Sub

    Private Sub PrintFlags()
        With mFlags
            X8086.Notify($"{ .CF}{ .ZF}{ .SF}{ .OF}{ .PF}{ .AF}{ .IF}{ .DF}", NotificationReasons.Info)
        End With
        X8086.Notify("CZSOPAID", NotificationReasons.Info)
    End Sub

    Private Sub PrintStack()
        Dim f As Integer = Math.Min(mRegisters.SP + (&HFFFF - mRegisters.SP) - 1, mRegisters.SP + 10)
        Dim t As Integer = Math.Max(0, mRegisters.SP - 10)

        For i As Integer = f To t Step -2
            X8086.Notify("{0}:{1}  {2}{3}", NotificationReasons.Info,
                                    mRegisters.SS.ToString("X4"),
                                    i.ToString("X4"),
                                    RAM16(mRegisters.SS, i,, True).ToString("X4"),
                                    If(i = mRegisters.SP, "<<", ""))
        Next
    End Sub
End Class
' Map Of Instructions: http://www.mlsite.net/8086/ and http://www.sandpile.org/x86/opc_1.htm
' http://en.wikibooks.org/wiki/X86_Assembly/Machine_Language_Conversion
' http://www.xs4all.nl/~ganswijk/chipdir/iset/8086bin.txt
' The Intel 8086 / 8088/ 80186 / 80286 / 80386 / 80486 Instruction Set: http://zsmith.co/intel.html
' http://www.felixcloutier.com/x86/
' https://c9x.me/x86/

Imports System.Threading

Public Class X8086
    Public Enum Models
        IBMPC_5150
        IBMPC_5160
    End Enum

    Private mModel As Models = Models.IBMPC_5160

    Private mRegisters As GPRegisters = New GPRegisters()
    Private mFlags As GPFlags = New GPFlags()

    Public Enum MemHookMode
        Read
        Write
    End Enum
    Public Delegate Function MemHandler(address As UInt32, ByRef value As UInt16, mode As MemHookMode) As Boolean
    Private memHooks As New List(Of MemHandler)
    Public Delegate Function IntHandler() As Boolean
    Private intHooks As New Dictionary(Of Byte, IntHandler)

    Private opCode As Byte
    Private opCodeSize As Byte

    Private tmpUVal As UInt32
    Private tmpVal As Int32

    Private addrMode As AddressingMode
    Private mIsExecuting As Boolean = False

    Private mipsThread As Thread
    Private mipsWaiter As AutoResetEvent
    Private instrucionsCounter As UInt32
    Private newPrefix As Boolean = False
    Private newPrefixLast As Integer = 0

    Public Enum REPLoopModes
        None
        REPE
        REPENE
    End Enum
    Private mRepeLoopMode As REPLoopModes

    Private forceNewIPAddress As UInt16
    Private Property IPAddrOffet As UInt16
        Get
            useIPAddrOffset = False
            Return forceNewIPAddress
        End Get
        Set(value As UInt16)
            forceNewIPAddress = value
            useIPAddrOffset = True
        End Set
    End Property
    Private useIPAddrOffset As Boolean

    Public Const KHz As ULong = 1000
    Public Const MHz As ULong = KHz * KHz
    Public Const GHz As ULong = MHz * KHz
    Public Shared BASECLOCK As ULong = 4.77273 * MHz ' http://dosmandrivel.blogspot.com/2009/03/ibm-pc-design-antics.html
    Private mCyclesPerSecond As ULong = BASECLOCK
    Private clkCyc As ULong = 0

    Private mDoReSchedule As Boolean
    Private mSimulationMultiplier As Double = 1.0
    Private leftCycleFrags As ULong

    Private cancelAllThreads As Boolean
    Private debugWaiter As AutoResetEvent

    'Private trapEnabled As Boolean
    Private Shared ignoreINTs As Boolean

    Public Sched As Scheduler
    Public DMA As DMAI8237
    Public PIC As PIC8259
    Public PIT As PIT8254
    Public PPI As PPI8255
    'Public PPI As PPI8255_ALT
    Public RTC As RTC
    Public FPU As x8087

    Private picIsAvailable As Boolean

    Public Event EmulationTerminated()
    Public Event EmulationHalted()
    Public Event InstructionDecoded()
    Public Shared Event [Error](sender As Object, e As EmulatorErrorEventArgs)
    Public Shared Event Output(message As String, reason As NotificationReasons, arg() As Object)
    Public Event MIPsUpdated()

    Public Delegate Sub RestartEmulation()
    Private restartCallback As RestartEmulation

    Public Sub New(Optional v20 As Boolean = True,
                   Optional int13 As Boolean = True,
                   Optional restartEmulationCallback As RestartEmulation = Nothing,
                   Optional model As Models = Models.IBMPC_5160)

        mVic20 = v20
        mEmulateINT13 = int13
        restartCallback = restartEmulationCallback
        mModel = model

        debugWaiter = New AutoResetEvent(False)
        addrMode = New AddressingMode()

        BASECLOCK = GetCpuSpeed() * X8086.MHz

        BuildSZPTables()
        BuildDecoderCache()
        Init()
    End Sub

    Private Sub Init()
        Sched = New Scheduler(Me)

        'FPU = New x8087(Me)
        PIC = New PIC8259(Me)
        DMA = New DMAI8237(Me)
        PIT = New PIT8254(Me, PIC.GetIrqLine(0))
        PPI = New PPI8255(Me, PIC.GetIrqLine(1))
        'PPI = New PPI8255_ALT(Me, PIC.GetIrqLine(1))
        RTC = New RTC(Me, PIC.GetIrqLine(8))

        mPorts.Add(PIC)
        mPorts.Add(DMA)
        mPorts.Add(PIT)
        mPorts.Add(PPI)
        mPorts.Add(RTC)

        SetupSystem()

        Array.Clear(Memory, 0, Memory.Length)

        StopAllThreads()

        If mipsWaiter Is Nothing Then
            mipsWaiter = New AutoResetEvent(False)
            mipsThread = New Thread(AddressOf MIPSCounterLoop)
            mipsThread.Start()
        End If

        portsCache.Clear()

        mIsHalted = False
        mIsExecuting = False
        mEnableExceptions = False
        mIsPaused = False
        mDoReSchedule = False

        ignoreINTs = False
        mRepeLoopMode = REPLoopModes.None
        IPAddrOffet = 0
        useIPAddrOffset = False

        mRegisters.ResetActiveSegment()

        mRegisters.AX = 0
        mRegisters.BX = 0
        mRegisters.CX = 0
        mRegisters.DX = 0

        mRegisters.BP = 0
        mRegisters.IP = 0
        mRegisters.SP = 0

        mRegisters.CS = 0
        mRegisters.DS = 0
        mRegisters.ES = 0
        mRegisters.SS = 0

        mRegisters.SI = 0
        mRegisters.DI = 0

        mFlags.EFlags = 0

        AddInternalHooks()
        LoadBIOS()
    End Sub

    Private Sub SetupSystem()
        picIsAvailable = PIC IsNot Nothing

        ' http://docs.huihoo.com/help-pc/int-int_11.html
        Dim equipmentByte As Byte = Binary.From("0 0 0 0 0 0 0 0 0 1 1 0 1 1 0 1".Replace(" ", ""))
        '                                       │F│E│D│C│B│A│9│8│7│6│5│4│3│2│1│0│  AX
        '                                        │ │ │ │ │ │ │ │ │ │ │ │ │ │ │ └──── IPL diskette installed
        '                                        │ │ │ │ │ │ │ │ │ │ │ │ │ │ └───── math co-processor
        '                                        │ │ │ │ │ │ │ │ │ │ │ │ ├─┼────── old PC system board RAM < 256K (00=256k, 01=512k, 10=576k, 11=640k)
        '                                        │ │ │ │ │ │ │ │ │ │ │ │ │ └───── pointing device installed (PS/2)
        '                                        │ │ │ │ │ │ │ │ │ │ │ │ └────── not used on PS/2
        '                                        │ │ │ │ │ │ │ │ │ │ └─┴─────── initial video mode (00=EGA/VGA, 01=CGA 40x25, 10=CGA 80x25 color, 11=MDA 80x25)
        '                                        │ │ │ │ │ │ │ │ └─┴────────── # of diskette drives, less 1
        '                                        │ │ │ │ │ │ │ └───────────── 0 if DMA installed
        '                                        │ │ │ │ └─┴─┴────────────── number of serial ports
        '                                        │ │ │ └─────────────────── game adapter installed
        '                                        │ │ └──────────────────── unused, internal modem (PS/2)
        '                                        └─┴───────────────────── number of printer ports

        If mVideoAdapter IsNot Nothing AndAlso TypeOf mVideoAdapter Is VGAAdapter Then equipmentByte = equipmentByte And &B11111111111001111
        If FPU IsNot Nothing Then equipmentByte = equipmentByte Or &B10
        If PPI IsNot Nothing Then PPI.SwitchData = equipmentByte
    End Sub

    Private Sub LoadBIOS()
        ' BIOS
        LoadBIN("roms\pcxtbios.rom", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios2\EPROMS\2764\XTBIOS.ROM", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios25\EPROMS\2764\PCXTBIOS.ROM", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\pcxtbios.ROM", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios31\pcxtbios.bin", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\PCemV0.7\roms\genxt\pcxt.rom", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\fake86-0.12.9.19-win32\Binaries\pcxtbios.bin", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\award-2.05.rom", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\phoenix-2.51.rom", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\PCE - PC Emulator\bin\rom\ibm-pc-1982.rom", &HFE00, &H0)

        ' BASIC C1.10
        LoadBIN("roms\basicc11.bin", &HF600, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicf6.rom", &HF600, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicf8.rom", &HF800, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicfa.rom", &HFA00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicfc.rom", &HFC00, &H0)

        ' Lots of ROMs: http://www.hampa.ch/pce/download.html

        'LoadBIN("..\..\Other Emulators & Resources\PCemV13.1\roms\ide_xt.bin", &HC800, &H0)
    End Sub

    Public Sub Close()
        mRepeLoopMode = REPLoopModes.None
        StopAllThreads()

        If DebugMode Then debugWaiter.Set()
        If Sched IsNot Nothing Then Sched.Stop()

        If mipsWaiter IsNot Nothing Then mipsWaiter.Set()

        For Each adapter As Adapter In mAdapters
            adapter.CloseAdapter()
        Next
        mAdapters.Clear()
        mPorts.Clear()

        memHooks.Clear()
        intHooks.Clear()

        Sched = Nothing
        mipsWaiter = Nothing
    End Sub

    Public Sub SoftReset()
        ' Just as Bill would've have wanted it... ;)
        PPI.PutKeyData(Keys.ControlKey, False)
        PPI.PutKeyData(Keys.Menu, False)
        PPI.PutKeyData(Keys.Delete, False)
    End Sub

    Public Sub HardReset()
        If restartCallback IsNot Nothing Then
            Close()
            restartCallback.Invoke()
        Else
            Close()
            Init()
        End If
    End Sub

    Public Sub StepInto()
        debugWaiter.Set()
    End Sub

    Public Sub Run(Optional debugMode As Boolean = False, Optional cs As UInt16 = &HFFFF, Optional ip As UInt16 = 0)
        SetSynchronization()

        mDebugMode = debugMode
        cancelAllThreads = False

#If Win32 Then
        If PIT?.Speaker IsNot Nothing Then PIT.Speaker.Enabled = True
        If mVideoAdapter IsNot Nothing Then mVideoAdapter.Reset()
#End If

        If mDebugMode Then RaiseEvent InstructionDecoded()

        mRegisters.CS = cs
        mRegisters.IP = ip

        Sched.Start()
    End Sub

    Private Sub StopAllThreads()
        cancelAllThreads = True

        'If mVideoAdapter IsNot Nothing Then mVideoAdapter.Update()

        If mipsThread IsNot Nothing Then
            Do
                Thread.Sleep(100)
            Loop While mipsThread.ThreadState = ThreadState.Running

            mipsThread = Nothing
        End If
    End Sub

    Private Sub MIPSCounterLoop()
        Const delay As Integer = 1000
        Do
            mipsWaiter.WaitOne(delay)

            mMPIs = instrucionsCounter / delay / 1000
            instrucionsCounter = 0

            If cancelAllThreads Then Exit Do
            RaiseEvent MIPsUpdated()
        Loop
    End Sub

    Public Sub Pause()
        If mIsExecuting Then
            mIsPaused = True

            Do
                Thread.Sleep(10)
            Loop While mIsExecuting

#If Win32 Then
            If PIT?.Speaker IsNot Nothing Then PIT.Speaker.Enabled = False
#End If
        End If
    End Sub

    Public Sub [Resume]()
        mDoReSchedule = False
        mIsPaused = False
    End Sub

    Private Sub FlushCycles()
        Dim t As Long = clkCyc * Scheduler.BASECLOCK + leftCycleFrags
        Sched.AdvanceTime(t \ mCyclesPerSecond)
        leftCycleFrags = t Mod mCyclesPerSecond
        clkCyc = 0

        mDoReSchedule = False
    End Sub

    Public Property DoReschedule As Boolean
        Get
            Return mDoReSchedule
        End Get
        Set(value As Boolean)
            mDoReSchedule = value
        End Set
    End Property

    Private Sub SetSynchronization()
        mDoReSchedule = True

        Sched.SetSynchronization(True,
                                Scheduler.BASECLOCK \ 100,
                                Scheduler.BASECLOCK \ 1000)

        PIT?.UpdateClock()
    End Sub

    Public Sub RunEmulation()
        If mIsExecuting OrElse mIsPaused Then Exit Sub

        Dim maxRunTime As ULong = Sched.GetTimeToNextEvent()
        If maxRunTime <= 0 Then Exit Sub
        If maxRunTime > Scheduler.BASECLOCK Then maxRunTime = Scheduler.BASECLOCK
        Dim maxRunCycl As ULong = (maxRunTime * mCyclesPerSecond - leftCycleFrags + Scheduler.BASECLOCK - 1) \ Scheduler.BASECLOCK

        If mDebugMode Then
            While (clkCyc < maxRunCycl AndAlso Not mDoReSchedule AndAlso mDebugMode)
                debugWaiter.WaitOne()

                SyncLock decoderSyncObj
                    mIsExecuting = True
                    PreExecute()
#If DEBUG Then
                    Execute_DEBUG()
#Else
                    opCodes(opCode).Invoke()
#End If
                    PostExecute()
                    mIsExecuting = False
                End SyncLock

                RaiseEvent InstructionDecoded()
            End While
        Else
            mIsExecuting = True
            While clkCyc < maxRunCycl AndAlso Not mDoReSchedule
                PreExecute()
#If DEBUG Then
                Execute_DEBUG()
#Else
                opCodes(opCode).Invoke()
#End If
                PostExecute()
            End While
            mIsExecuting = False
        End If

        FlushCycles()
    End Sub

    Private Sub PreExecute()
        If mFlags.TF = 1 Then
            ' The addition of the "If ignoreINTs Then" not only fixes the dreaded "Interrupt Check" in CheckIt,
            ' but it even allows it to pass it successfully!!!
            If ignoreINTs Then HandleInterrupt(1, False)
        ElseIf ignoreINTs Then
            ignoreINTs = False
        Else
            HandlePendingInterrupt()
        End If

        opCodeSize = 1
        newPrefix = False
        instrucionsCounter += 1

        opCode = RAM8(mRegisters.CS, mRegisters.IP)
    End Sub

    Private Sub Execute_DEBUG()
        Select Case opCode
            Case &H0 To &H3 ' add reg<->reg / reg<->mem
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Add, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Add, addrMode.Size)
                        clkCyc += 16
                    Else
                        mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.Add, addrMode.Size)
                        clkCyc += 9
                    End If
                End If

            Case &H4 ' add al, imm
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Add, DataSize.Byte)
                clkCyc += 4

            Case &H5 ' add ax, imm
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Add, DataSize.Word)
                clkCyc += 4

            Case &H6 ' push es
                PushIntoStack(mRegisters.ES)
                clkCyc += 10

            Case &H7 ' pop es
                mRegisters.ES = PopFromStack()
                ignoreINTs = True
                clkCyc += 8

            Case &H8 To &HB ' or
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.LogicOr, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.LogicOr, addrMode.Size)
                        clkCyc += 16
                    Else
                        mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.LogicOr, addrMode.Size)
                        clkCyc += 9
                    End If
                End If

            Case &HC ' or al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicOr, DataSize.Byte)
                clkCyc += 4

            Case &HD ' or ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicOr, DataSize.Word)
                clkCyc += 4

            Case &HE ' push cs
                PushIntoStack(mRegisters.CS)
                clkCyc += 10

            Case &HF ' pop cs
                If Not mVic20 Then
                    mRegisters.CS = PopFromStack()
                    ignoreINTs = True
                    clkCyc += 8
                End If

            Case &H10 To &H13 ' adc
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.AddWithCarry, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.AddWithCarry, addrMode.Size)
                        clkCyc += 16
                    Else
                        mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.AddWithCarry, addrMode.Size)
                        clkCyc += 9
                    End If
                End If

            Case &H14 ' adc al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.AddWithCarry, DataSize.Byte)
                clkCyc += 3

            Case &H15 ' adc ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.AddWithCarry, DataSize.Word)
                clkCyc += 3

            Case &H16 ' push ss
                PushIntoStack(mRegisters.SS)
                clkCyc += 10

            Case &H17 ' pop ss
                mRegisters.SS = PopFromStack()
                ' Lesson 4: http://ntsecurity.nu/onmymind/2007/2007-08-22.html
                ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
                ' http://www.vcfed.org/forum/archive/index.php/t-41453.html
                ignoreINTs = True
                clkCyc += 8

            Case &H18 To &H1B ' sbb
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.SubstractWithCarry, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.SubstractWithCarry, addrMode.Size)
                        clkCyc += 16
                    Else
                        mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.SubstractWithCarry, addrMode.Size)
                        clkCyc += 9
                    End If
                End If

            Case &H1C ' sbb al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.SubstractWithCarry, DataSize.Byte)
                clkCyc += 4

            Case &H1D ' sbb ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.SubstractWithCarry, DataSize.Word)
                clkCyc += 4

            Case &H1E ' push ds
                PushIntoStack(mRegisters.DS)
                clkCyc += 10

            Case &H1F ' pop ds
                mRegisters.DS = PopFromStack()
                ignoreINTs = True
                clkCyc += 8

            Case &H20 To &H23 ' and reg/mem and reg to either
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.LogicAnd, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.LogicAnd, addrMode.Size)
                        clkCyc += 16
                    Else
                        mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.LogicAnd, addrMode.Size)
                        clkCyc += 9
                    End If
                End If

            Case &H24 ' and al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicAnd, DataSize.Byte)
                clkCyc += 4

            Case &H25 ' and ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicAnd, DataSize.Word)
                clkCyc += 4

            Case &H26, &H2E, &H36, &H3E ' ES, CS, SS and DS segment override prefix
                addrMode.Decode(opCode, opCode)
                mRegisters.ActiveSegmentRegister = addrMode.Dst - GPRegisters.RegistersTypes.AH + GPRegisters.RegistersTypes.ES
                newPrefix = True
                clkCyc += 2

            Case &H27 ' daa
                If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
                    tmpUVal = CUInt(mRegisters.AL) + 6
                    mRegisters.AL += 6
                    mFlags.AF = 1
                    mFlags.CF = mFlags.CF Or If((tmpUVal And &HFF00) <> 0, 1, 0)
                Else
                    mFlags.AF = 0
                End If
                If (mRegisters.AL And &HF0) > &H90 OrElse mFlags.CF = 1 Then
                    tmpUVal = CUInt(mRegisters.AL) + &H60
                    mRegisters.AL += &H60
                    mFlags.CF = 1
                Else
                    mFlags.CF = 0
                End If
                SetSZPFlags(tmpUVal, DataSize.Byte)
                clkCyc += 4

            Case &H28 To &H2B ' sub reg/mem with reg to either
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Substract, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Substract, addrMode.Size)
                        clkCyc += 16
                    Else
                        mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.Substract, addrMode.Size)
                        clkCyc += 9
                    End If
                End If

            Case &H2C ' sub al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Substract, DataSize.Byte)
                clkCyc += 4

            Case &H2D ' sub ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Substract, DataSize.Word)
                clkCyc += 4

            Case &H2F ' das
                tmpVal = mRegisters.AL
                If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
                    tmpUVal = CShort(mRegisters.AL) - 6
                    mRegisters.AL -= 6
                    mFlags.AF = 1
                    mFlags.CF = mFlags.CF Or If((tmpUVal And &HFF00) <> 0, 1, 0)
                Else
                    mFlags.AF = 0
                End If
                If tmpVal > &H99 OrElse mFlags.CF = 1 Then
                    tmpUVal = CShort(mRegisters.AL) - &H60
                    mRegisters.AL -= &H60
                    mFlags.CF = 1
                Else
                    mFlags.CF = 0
                End If
                SetSZPFlags(tmpUVal, DataSize.Byte)
                clkCyc += 4

            Case &H30 To &H33 ' xor reg/mem and reg to either
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.LogicXor, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        RAMn = Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.LogicXor, addrMode.Size)
                        clkCyc += 16
                    Else
                        mRegisters.Val(addrMode.Dst) = Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.LogicXor, addrMode.Size)
                        clkCyc += 9
                    End If
                End If

            Case &H34 ' xor al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicXor, DataSize.Byte)
                clkCyc += 4

            Case &H35 ' xor ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicXor, DataSize.Word)
                clkCyc += 4

            Case &H37 ' aaa
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

            Case &H38 To &H3B ' cmp reg/mem and reg
                SetAddressing()
                If addrMode.IsDirect Then
                    Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Compare, addrMode.Size)
                    clkCyc += 3
                Else
                    If addrMode.Direction = 0 Then
                        Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Compare, addrMode.Size)
                    Else
                        Eval(mRegisters.Val(addrMode.Dst), addrMode.IndMem, Operation.Compare, addrMode.Size)
                    End If
                    clkCyc += 9
                End If

            Case &H3C ' cmp al and imm
                Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Compare, DataSize.Byte)
                clkCyc += 4

            Case &H3D ' cmp ax and imm
                Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Compare, DataSize.Word)
                clkCyc += 4

            Case &H3F ' aas
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

            Case &H40 To &H47 ' inc reg
                SetRegister1Alt(opCode)
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Increment, DataSize.Word)
                clkCyc += 3

            Case &H48 To &H4F ' dec reg
                SetRegister1Alt(opCode)
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Decrement, DataSize.Word)
                clkCyc += 3

            Case &H50 To &H57 ' push reg
                If opCode = &H54 Then ' SP
                    ' The 8086/8088 pushes the value of SP after it has been decremented
                    ' http://css.csail.mit.edu/6.858/2013/readings/i386/s15_06.htm
                    PushIntoStack(mRegisters.SP - 2)
                Else
                    SetRegister1Alt(opCode)
                    PushIntoStack(mRegisters.Val(addrMode.Register1))
                End If
                clkCyc += 11

            Case &H58 To &H5F ' pop reg
                SetRegister1Alt(opCode)
                mRegisters.Val(addrMode.Register1) = PopFromStack()
                clkCyc += 8

            Case &H60 ' pusha (80186)
                If mVic20 Then
                    tmpUVal = mRegisters.SP
                    PushIntoStack(mRegisters.AX)
                    PushIntoStack(mRegisters.CX)
                    PushIntoStack(mRegisters.DX)
                    PushIntoStack(mRegisters.BX)
                    PushIntoStack(tmpUVal)
                    PushIntoStack(mRegisters.BP)
                    PushIntoStack(mRegisters.SI)
                    PushIntoStack(mRegisters.DI)
                    clkCyc += 19
                Else
                    OpCodeNotImplemented()
                End If

            Case &H61 ' popa (80186)
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

            Case &H62 ' bound (80186)
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

            Case &H68 ' push (80186)
                ' PRE ALPHA CODE - UNTESTED
                If mVic20 Then
                    PushIntoStack(Param(ParamIndex.First, , DataSize.Word))
                    clkCyc += 3
                Else
                    OpCodeNotImplemented()
                End If

            Case &H69 ' imul (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    SetAddressing()
                    Dim tmp1 As UInt32 = mRegisters.Val(addrMode.Register1)
                    Dim tmp2 As UInt32 = Param(ParamIndex.First, , DataSize.Word)
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
                Else
                    OpCodeNotImplemented()
                End If

            Case &H6A ' push (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    PushIntoStack(Param(ParamIndex.First, , DataSize.Byte))
                    clkCyc += 3
                Else
                    OpCodeNotImplemented()
                End If

            Case &H6B ' imul (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    SetAddressing()
                    Dim tmp1 As UInt32 = mRegisters.Val(addrMode.Register1)
                    Dim tmp2 As UInt32 = To16bitsWithSign(Param(ParamIndex.First, , DataSize.Byte))
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
                Else
                    OpCodeNotImplemented()
                End If

            Case &H6C To &H6F ' Ignore 80186/V20 port operations... for now...
                opCodeSize += 1
                clkCyc += 3

            Case &H70 ' jo
                If mFlags.OF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H71 ' jno
                If mFlags.OF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H72 ' jb/jnae/jc (unsigned)
                If mFlags.CF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H73 ' jnb/jae/jnc (unsigned)
                If mFlags.CF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H74 ' je/jz
                If mFlags.ZF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H75 ' jne/jnz
                If mFlags.ZF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H76 ' jbe/jna (unsigned)
                If mFlags.CF = 1 OrElse mFlags.ZF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If


            Case &H77 ' ja/jnbe (unsigned)
                If mFlags.CF = 0 AndAlso mFlags.ZF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H78 ' js
                If mFlags.SF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H79 ' jns
                If mFlags.SF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7A ' jp/jpe
                If mFlags.PF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7B ' jnp/jpo
                If mFlags.PF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7C ' jl/jnge (signed)
                If mFlags.SF <> mFlags.OF Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7D ' jnl/jge (signed)
                If mFlags.SF = mFlags.OF Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7E ' jle/jng (signed)
                If mFlags.ZF = 1 OrElse (mFlags.SF <> mFlags.OF) Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7F ' jg/jnle (signed)
                If mFlags.ZF = 0 AndAlso (mFlags.SF = mFlags.OF) Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H80 To &H83 : ExecuteGroup1()

            Case &H84 To &H85 ' test reg with reg/mem
                SetAddressing()
                If addrMode.IsDirect Then
                    Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Test, addrMode.Size)
                    clkCyc += 3
                Else
                    Eval(addrMode.IndMem, mRegisters.Val(addrMode.Dst), Operation.Test, addrMode.Size)
                    clkCyc += 9
                End If

            Case &H86 To &H87 ' xchg reg/mem with reg
                SetAddressing()
                If addrMode.IsDirect Then
                    tmpUVal = mRegisters.Val(addrMode.Dst)
                    mRegisters.Val(addrMode.Dst) = mRegisters.Val(addrMode.Src)
                    mRegisters.Val(addrMode.Src) = tmpUVal
                    clkCyc += 4
                Else
                    RAMn = mRegisters.Val(addrMode.Dst)
                    mRegisters.Val(addrMode.Dst) = addrMode.IndMem
                    clkCyc += 17
                End If

            Case &H88 To &H8B ' mov ind <-> reg8/reg16
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

            Case &H8C ' mov Ew, Sw
                SetAddressing(DataSize.Word)
                addrMode.Src += GPRegisters.RegistersTypes.ES
                If addrMode.Dst > GPRegisters.RegistersTypes.BL Then
                    addrMode.Dst = (addrMode.Dst + GPRegisters.RegistersTypes.ES) Or shl3
                Else
                    addrMode.Dst = addrMode.Dst Or shl3
                End If

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

            Case &H8D ' lea
                SetAddressing()
                mRegisters.Val(addrMode.Src) = addrMode.IndAdr
                clkCyc += 2

            Case &H8E  ' mov Sw, Ew
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
                ignoreINTs = ignoreINTs Or
                        (addrMode.Register2 = GPRegisters.RegistersTypes.CS) Or
                        (addrMode.Register2 = GPRegisters.RegistersTypes.SS) Or
                        (addrMode.Register2 = GPRegisters.RegistersTypes.DS) Or
                        (addrMode.Register2 = GPRegisters.RegistersTypes.ES)
                If addrMode.Register2 = GPRegisters.RegistersTypes.CS Then mDoReSchedule = True

            Case &H8F ' pop reg/mem
                SetAddressing()
                If addrMode.IsDirect Then
                    addrMode.Decode(opCode, opCode)
                    mRegisters.Val(addrMode.Register1) = PopFromStack()
                Else
                    RAMn = PopFromStack()
                End If
                clkCyc += 17

            Case &H90 ' nop
                clkCyc += 3

            Case &H90 To &H97 ' xchg reg with acc
                SetRegister1Alt(opCode)
                tmpUVal = mRegisters.AX
                mRegisters.AX = mRegisters.Val(addrMode.Register1)
                mRegisters.Val(addrMode.Register1) = tmpUVal
                clkCyc += 3

            Case &H98 ' cbw
                mRegisters.AX = To16bitsWithSign(mRegisters.AL)
                clkCyc += 2

            Case &H99 ' cwd
                mRegisters.DX = If((mRegisters.AH And &H80) <> 0, &HFFFF, &H0)
                clkCyc += 5

            Case &H9A ' call direct inter-segment
                IPAddrOffet = Param(ParamIndex.First, , DataSize.Word)
                tmpUVal = Param(ParamIndex.Second, , DataSize.Word)
                PushIntoStack(mRegisters.CS)
                PushIntoStack(mRegisters.IP + opCodeSize)
                mRegisters.CS = tmpUVal
                clkCyc += 28

            Case &H9B ' wait
                clkCyc += 4

            Case &H9C ' pushf
                PushIntoStack(If(mModel = Models.IBMPC_5150, &HFFF, &HFFFF) And mFlags.EFlags)
                clkCyc += 10

            Case &H9D ' popf
                mFlags.EFlags = PopFromStack()
                clkCyc += 8

            Case &H9E ' sahf
                mFlags.EFlags = (mFlags.EFlags And &HFF00) Or mRegisters.AH
                clkCyc += 4

            Case &H9F ' lahf
                mRegisters.AH = mFlags.EFlags
                clkCyc += 4

            Case &HA0 To &HA3 ' mov mem to acc | mov acc to mem
                addrMode.Size = opCode And 1
                addrMode.Direction = (opCode >> 1) And 1
                addrMode.IndAdr = Param(ParamIndex.First, , DataSize.Word)
                addrMode.Register1 = If(addrMode.Size = DataSize.Byte, GPRegisters.RegistersTypes.AL, GPRegisters.RegistersTypes.AX)
                If addrMode.Direction = 0 Then
                    mRegisters.Val(addrMode.Register1) = RAMn
                Else
                    RAMn = mRegisters.Val(addrMode.Register1)
                End If
                clkCyc += 10

            Case &HA4 To &HA7, &HAA To &HAF : HandleREPMode()

            Case &HA8 ' test al imm8
                Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Test, DataSize.Byte)
                clkCyc += 4

            Case &HA9 ' test ax imm16
                Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Test, DataSize.Word)
                clkCyc += 4

            Case &HB0 To &HBF ' mov imm to reg
                addrMode.Register1 = opCode And &H7
                If (opCode And &H8) = &H8 Then
                    addrMode.Register1 += GPRegisters.RegistersTypes.AX
                    If (opCode And &H4) = &H4 Then addrMode.Register1 += GPRegisters.RegistersTypes.ES
                    addrMode.Size = DataSize.Word
                Else
                    addrMode.Size = DataSize.Byte
                End If
                mRegisters.Val(addrMode.Register1) = Param(ParamIndex.First)
                clkCyc += 4

            Case &HC0, &HC1 ' GRP2 byte/word imm8/16 ??? (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    ExecuteGroup2()
                Else
                    OpCodeNotImplemented()
                End If

            Case &HC2 ' ret (ret n) within segment adding imm to sp
                IPAddrOffet = PopFromStack()
                mRegisters.SP += Param(ParamIndex.First, , DataSize.Word)
                clkCyc += 20

            Case &HC3 ' ret within segment
                IPAddrOffet = PopFromStack()
                clkCyc += 16

            Case &HC4 To &HC5 ' les | lds
                SetAddressing(DataSize.Word)
                If (addrMode.Register1 And shl2) = shl2 Then addrMode.Register1 += GPRegisters.RegistersTypes.ES
                mRegisters.Val(addrMode.Register1 Or shl3) = addrMode.IndMem
                mRegisters.Val(If(opCode = &HC4, GPRegisters.RegistersTypes.ES, GPRegisters.RegistersTypes.DS)) = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                ignoreINTs = True
                clkCyc += 16

            Case &HC6 To &HC7 ' mov imm to reg/mem
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Src) = Param(ParamIndex.First, opCodeSize)
                Else
                    RAMn = Param(ParamIndex.First, opCodeSize)
                End If
                clkCyc += 10

            Case &HC8 ' enter (80186)
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
                        Case 0 : clkCyc += 15
                        Case 1 : clkCyc += 25
                        Case Else : clkCyc += 22 + 16 * (nestLevel - 1)
                    End Select
                Else
                    OpCodeNotImplemented()
                End If

            Case &HC9 ' leave (80186)
                If mVic20 Then
                    mRegisters.SP = mRegisters.BP
                    mRegisters.BP = PopFromStack()
                    clkCyc += 8
                Else
                    OpCodeNotImplemented()
                End If

            Case &HCA ' ret intersegment adding imm to sp (ret n /retf)
                tmpUVal = Param(ParamIndex.First, , DataSize.Word)
                IPAddrOffet = PopFromStack()
                mRegisters.CS = PopFromStack()
                mRegisters.SP += tmpUVal
                clkCyc += 17

            Case &HCB ' ret intersegment (retf)
                IPAddrOffet = PopFromStack()
                mRegisters.CS = PopFromStack()
                clkCyc += 18

            Case &HCC ' int with type 3
                HandleInterrupt(3, False)
                clkCyc += 1

            Case &HCD ' int with type specified
                HandleInterrupt(Param(ParamIndex.First, , DataSize.Byte), False)
                clkCyc += 0

            Case &HCE ' into
                If mFlags.OF = 1 Then
                    HandleInterrupt(4, False)
                    clkCyc += 3
                Else
                    clkCyc += 4
                End If

            Case &HCF ' iret
                IPAddrOffet = PopFromStack()
                mRegisters.CS = PopFromStack()
                mFlags.EFlags = PopFromStack()
                clkCyc += 32

            Case &HD0 To &HD3 : ExecuteGroup2()

            Case &HD4 ' aam
                tmpUVal = Param(ParamIndex.First, , DataSize.Byte)
                If tmpUVal = 0 Then
                    HandleInterrupt(0, True)
                    Exit Select
                End If
                mRegisters.AH = mRegisters.AL \ tmpUVal
                mRegisters.AL = mRegisters.AL Mod tmpUVal
                SetSZPFlags(mRegisters.AX, DataSize.Word)
                clkCyc += 83

            Case &HD5 ' aad
                mRegisters.AL += mRegisters.AH * Param(ParamIndex.First, , DataSize.Byte)
                mRegisters.AH = 0
                SetSZPFlags(mRegisters.AX, DataSize.Word)
                mFlags.SF = 0
                clkCyc += 60

            Case &HD6 ' xlat / salc
                If mVic20 Then
                    mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.BX + mRegisters.AL)
                Else
                    mRegisters.AL = If(mFlags.CF = 1, &HFF, &H0)
                    clkCyc += 4
                End If

            Case &HD7 ' xlatb
                mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.BX + mRegisters.AL)
                clkCyc += 11

            Case &HD8 To &HDF ' Ignore co-processor instructions
                SetAddressing()
                'FPU.Execute(opCode, addrMode)

                ' Lesson 2
                ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

                'HandleInterrupt(7, False)
                OpCodeNotImplemented("FPU Not Available")
                clkCyc += 2

            Case &HE0 ' loopne/loopnz
                mRegisters.CX -= 1
                If mRegisters.CX > 0 AndAlso mFlags.ZF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 19
                Else
                    opCodeSize += 1
                    clkCyc += 5
                End If

            Case &HE1 ' loope/loopz
                mRegisters.CX -= 1
                If mRegisters.CX > 0 AndAlso mFlags.ZF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 18
                Else
                    opCodeSize += 1
                    clkCyc += 6
                End If

            Case &HE2 ' loop
                mRegisters.CX -= 1
                If mRegisters.CX > 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 17
                Else
                    opCodeSize += 1
                    clkCyc += 5
                End If

            Case &HE3 ' jcxz/jecxz
                If mRegisters.CX = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 18
                Else
                    opCodeSize += 1
                    clkCyc += 6
                End If

            Case &HE4 ' in to al from fixed port
                mRegisters.AL = ReceiveFromPort(Param(ParamIndex.First, , DataSize.Byte))
                clkCyc += 10

            Case &HE5 ' inw to ax from fixed port
                mRegisters.AX = ReceiveFromPort(Param(ParamIndex.First, , DataSize.Byte))
                clkCyc += 10

            Case &HE6  ' out to al to fixed port
                SendToPort(Param(ParamIndex.First, , DataSize.Byte), mRegisters.AL)
                clkCyc += 10

            Case &HE7  ' outw to ax to fixed port
                SendToPort(Param(ParamIndex.First, , DataSize.Byte), mRegisters.AX)
                clkCyc += 10

            Case &HE8 ' call direct within segment
                IPAddrOffet = OffsetIP(DataSize.Word)
                PushIntoStack(Registers.IP + opCodeSize)
                clkCyc += 19

            Case &HE9 ' jmp direct within segment
                IPAddrOffet = OffsetIP(DataSize.Word)
                clkCyc += 15

            Case &HEA ' jmp direct intersegment
                IPAddrOffet = Param(ParamIndex.First, , DataSize.Word)
                mRegisters.CS = Param(ParamIndex.Second, , DataSize.Word) 
                clkCyc += 15

            Case &HEB ' jmp direct within segment short
                IPAddrOffet = OffsetIP(DataSize.Byte)
                clkCyc += 15

            Case &HEC  ' in to al from variable port in dx
                mRegisters.AL = ReceiveFromPort(mRegisters.DX)
                clkCyc += 8

            Case &HED ' inw to ax from variable port in dx
                mRegisters.AX = ReceiveFromPort(mRegisters.DX)
                clkCyc += 8

            Case &HEE ' out to port dx from al
                SendToPort(mRegisters.DX, mRegisters.AL)
                clkCyc += 8

            Case &HEF ' out to port dx from ax
                SendToPort(mRegisters.DX, mRegisters.AX)
                clkCyc += 8

            Case &HF0 ' lock
                OpCodeNotImplemented("LOCK")
                clkCyc += 2

            Case &HF2 ' repne/repnz
                mRepeLoopMode = REPLoopModes.REPENE
                newPrefix = True
                clkCyc += 2

            Case &HF3 ' repe/repz
                mRepeLoopMode = REPLoopModes.REPE
                newPrefix = True
                clkCyc += 2

            Case &HF4 ' hlt
                If Not mIsHalted Then SystemHalted()
                mRegisters.IP -= 1
                clkCyc += 2

            Case &HF5 ' cmc
                mFlags.CF = If(mFlags.CF = 0, 1, 0)
                clkCyc += 2

            Case &HF6 To &HF7 : ExecuteGroup3()

            Case &HF8 ' clc
                mFlags.CF = 0
                clkCyc += 2

            Case &HF9 ' stc
                mFlags.CF = 1
                clkCyc += 2

            Case &HFA ' cli
                mFlags.IF = 0
                clkCyc += 2

            Case &HFB ' sti
                mFlags.IF = 1
                ignoreINTs = True ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
                clkCyc += 2

            Case &HFC ' cld
                mFlags.DF = 0
                clkCyc += 2

            Case &HFD ' std
                mFlags.DF = 1
                clkCyc += 2

            Case &HFE, &HFF : ExecuteGroup4_And_5()

            Case Else
                OpCodeNotImplemented()
        End Select
    End Sub

    Private Sub PostExecute()
        If useIPAddrOffset Then
            mRegisters.IP = IPAddrOffet
        Else
            mRegisters.IP += opCodeSize
        End If

        'clkCyc += CUInt(Fix(opCodeSize * 4 * clockFactor))
        clkCyc += opCodeSize * 4

        If Not newPrefix Then
            If mRepeLoopMode <> REPLoopModes.None Then mRepeLoopMode = REPLoopModes.None
            If mRegisters.ActiveSegmentChanged Then mRegisters.ResetActiveSegment()
            newPrefixLast = 0
        Else
            newPrefixLast += 1
        End If
    End Sub

    Private Sub ExecuteGroup1() ' &H80 To &H83
        SetAddressing()

        Dim arg1 As UInt16 = If(addrMode.IsDirect, mRegisters.Val(addrMode.Register2), addrMode.IndMem)              ' reg
        Dim arg2 As UInt16 = Param(ParamIndex.First, opCodeSize, If(opCode = &H83, DataSize.Byte, addrMode.Size))    ' imm
        If opCode = &H83 Then arg2 = To16bitsWithSign(arg2)

        Select Case addrMode.Reg
            Case 0 ' 000    --   add imm to reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.Add, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.Add, addrMode.Size)
                    clkCyc += 17
                End If

            Case 1 ' 001    --  or imm to reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.LogicOr, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.LogicOr, addrMode.Size)
                    clkCyc += 17
                End If

            Case 2 ' 010    --  adc imm to reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.AddWithCarry, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.AddWithCarry, addrMode.Size)
                    clkCyc += 17
                End If

            Case 3 ' 011    --  sbb imm from reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.SubstractWithCarry, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.SubstractWithCarry, addrMode.Size)
                    clkCyc += 17
                End If

            Case 4 ' 100    --  and imm to reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.LogicAnd, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.LogicAnd, addrMode.Size)
                    clkCyc += 17
                End If

            Case 5 ' 101    --  sub imm from reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.Substract, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.Substract, addrMode.Size)
                    clkCyc += 17
                End If

            Case 6 ' 110    --  xor imm to reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.LogicXor, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.LogicXor, addrMode.Size)
                    clkCyc += 17
                End If

            Case 7 ' 111    --  cmp imm with reg/mem
                Eval(arg1, arg2, Operation.Compare, addrMode.Size)
                clkCyc += If(addrMode.IsDirect, 4, 10)

        End Select
    End Sub

    Private Sub ExecuteGroup2() ' &HD0 To &HD3 / &HC0 To &HC1
        SetAddressing()

        Dim newValue As UInt32
        Dim count As UInt32
        Dim oldValue As UInt32

        Dim mask80_8000 As UInt32
        Dim mask07_15 As UInt32
        Dim maskFF_FFFF As UInt32
        Dim mask8_16 As UInt32
        Dim mask9_17 As UInt32
        Dim mask100_10000 As UInt32
        Dim maskFF00_FFFF0000 As UInt32

        If addrMode.Size = DataSize.Byte Then
            mask80_8000 = &H80
            mask07_15 = &H7
            maskFF_FFFF = &HFF
            mask8_16 = 8
            mask9_17 = 9
            mask100_10000 = &H100
            maskFF00_FFFF0000 = &HFF00
        Else
            mask80_8000 = &H8000
            mask07_15 = &HF
            maskFF_FFFF = &HFFFF
            mask8_16 = 16
            mask9_17 = 17
            mask100_10000 = &H10000
            maskFF00_FFFF0000 = &HFFFF0000UI
        End If

        If addrMode.IsDirect Then
            oldValue = mRegisters.Val(addrMode.Register2)
            If opCode >= &HD2 Then
                clkCyc += 8
            Else
                clkCyc += 2
            End If
        Else
            oldValue = addrMode.IndMem
            If opCode >= &HD2 Then
                clkCyc += 20
            Else
                clkCyc += 13
            End If
        End If

        Select Case opCode
            Case &HD0, &HD1 : count = 1
            Case &HD2, &HD3 : count = mRegisters.CL
            Case &HC0, &HC1 : count = Param(ParamIndex.First,  , DataSize.Byte)
        End Select

        ' 80186/V20 class CPUs limit shift count to 31
        If mVic20 Then count = count And &H1F
        clkCyc += 4 * count

        If count = 0 Then newValue = oldValue

        Select Case addrMode.Reg
            Case 0 ' 000    --  rol
                If count = 1 Then
                    newValue = (oldValue << 1) Or (oldValue >> mask07_15)
                    mFlags.CF = If((oldValue And mask80_8000) <> 0, 1, 0)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                ElseIf count > 1 Then
                    newValue = (oldValue << (count And mask07_15)) Or (oldValue >> (mask8_16 - (count And mask07_15)))
                    mFlags.CF = newValue And 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                End If

            Case 1 ' 001    --  ror
                If count = 1 Then
                    newValue = (oldValue >> 1) Or (oldValue << mask07_15)
                    mFlags.CF = oldValue And 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                ElseIf count > 1 Then
                    newValue = (oldValue >> (count And mask07_15)) Or (oldValue << (mask8_16 - (count And mask07_15)))
                    mFlags.CF = If((newValue And mask80_8000) <> 0, 1, 0)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                End If

            Case 2 ' 010    --  rcl
                If count = 1 Then
                    newValue = (oldValue << 1) Or mFlags.CF
                    mFlags.CF = If((oldValue And mask80_8000) <> 0, 1, 0)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                ElseIf count > 1 Then
                    oldValue = oldValue Or (CUInt(mFlags.CF) << mask8_16)
                    newValue = (oldValue << (count Mod mask9_17)) Or (oldValue >> (mask9_17 - (count Mod mask9_17)))
                    mFlags.CF = If((newValue And mask100_10000) <> 0, 1, 0)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                End If

            Case 3 ' 011    --  rcr
                If count = 1 Then
                    newValue = (oldValue >> 1) Or (CUInt(mFlags.CF) << mask07_15)
                    mFlags.CF = oldValue And 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                ElseIf count > 1 Then
                    oldValue = oldValue Or (CUInt(mFlags.CF) << mask8_16)
                    newValue = (oldValue >> (count Mod mask9_17)) Or (oldValue << (mask9_17 - (count Mod mask9_17)))
                    mFlags.CF = If((newValue And mask100_10000) <> 0, 1, 0)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                Else
                    mFlags.OF = 0
                End If

            Case 4, 6 ' 100/110    --  shl/sal
                If count = 1 Then
                    newValue = oldValue << 1
                    mFlags.CF = If((oldValue And mask80_8000) <> 0, 1, 0)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                ElseIf count > 1 Then
                    newValue = If(count > mask8_16, 0, oldValue << count)
                    mFlags.CF = If((newValue And mask100_10000) <> 0, 1, 0)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                Else
                    mFlags.OF = 0
                End If
                SetSZPFlags(newValue, addrMode.Size)

            Case 5 ' 101    --  shr
                If count = 1 Then
                    newValue = oldValue >> 1
                    mFlags.CF = oldValue And 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                ElseIf count > 1 Then
                    newValue = If(count > mask8_16, 0, oldValue >> (count - 1))
                    mFlags.CF = newValue And 1
                    newValue >>= 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) <> 0, 1, 0)
                Else
                    mFlags.OF = 0
                End If
                SetSZPFlags(newValue, addrMode.Size)

            Case 7 ' 111    --  sar
                If count = 1 Then
                    newValue = (oldValue >> 1) Or (oldValue And mask80_8000)
                    mFlags.CF = oldValue And 1
                ElseIf count > 1 Then
                    oldValue = oldValue Or If((oldValue And mask80_8000) <> 0, maskFF00_FFFF0000, 0)
                    newValue = oldValue >> If(count >= mask8_16, mask07_15, count - 1)
                    mFlags.CF = newValue And 1
                    newValue = (newValue >> 1) And maskFF_FFFF
                End If
                mFlags.OF = 0
                SetSZPFlags(newValue, addrMode.Size)

            Case Else
                OpCodeNotImplemented($"Unknown Reg Mode {addrMode.Reg} for Opcode {opCode:X} (Group2)")
        End Select

        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Register2) = newValue
        Else
            RAMn = newValue
        End If
    End Sub

    Private Sub ExecuteGroup3() ' &HF6 To &HF7
        SetAddressing()

        Select Case addrMode.Reg
            Case 0 ' 000    --  test
                If addrMode.IsDirect Then
                    Eval(mRegisters.Val(addrMode.Register2), Param(ParamIndex.First, opCodeSize), Operation.Test, addrMode.Size)
                    clkCyc += 5
                Else
                    Eval(addrMode.IndMem, Param(ParamIndex.First, opCodeSize), Operation.Test, addrMode.Size)
                    clkCyc += 11
                End If

            Case 2 ' 010    --  not
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Not mRegisters.Val(addrMode.Register2)
                    clkCyc += 3
                Else
                    RAMn = Not addrMode.IndMem
                    clkCyc += 16
                End If

            Case 3 ' 011    --  neg
                If addrMode.IsDirect Then
                    Eval(0, mRegisters.Val(addrMode.Register2), Operation.Substract, addrMode.Size)
                    tmpUVal = (Not mRegisters.Val(addrMode.Register2)) + 1
                    mRegisters.Val(addrMode.Register2) = tmpUVal
                    clkCyc += 3
                Else
                    Eval(0, addrMode.IndMem, Operation.Substract, addrMode.Size)
                    tmpUVal = (Not addrMode.IndMem) + 1
                    RAMn = tmpUVal
                    clkCyc += 16
                End If

            Case 4 ' 100    --  mul
                If addrMode.IsDirect Then
                    If addrMode.Size = DataSize.Byte Then
                        tmpUVal = mRegisters.Val(addrMode.Register2) * mRegisters.AL
                        clkCyc += 70
                    Else
                        tmpUVal = CUInt(mRegisters.Val(addrMode.Register2)) * mRegisters.AX
                        mRegisters.DX = tmpUVal >> 16
                        clkCyc += 118
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        tmpUVal = addrMode.IndMem * mRegisters.AL
                        clkCyc += 76
                    Else
                        tmpUVal = CUInt(addrMode.IndMem) * mRegisters.AX
                        mRegisters.DX = tmpUVal >> 16
                        clkCyc += 134
                    End If
                End If
                mRegisters.AX = tmpUVal

                SetSZPFlags(tmpUVal, addrMode.Size)
                If (tmpUVal And If(addrMode.Size = DataSize.Byte, &HFF00, &HFFFF0000UI)) <> 0 Then
                    mFlags.CF = 1
                    mFlags.OF = 1
                Else
                    mFlags.CF = 0
                    mFlags.OF = 0
                End If
                mFlags.ZF = If(mVic20, If(tmpUVal <> 0, 1, 0), 0) ' This is the test the BIOS uses to detect a VIC20 (8018x)

            Case 5 ' 101    --  imul
                If addrMode.IsDirect Then
                    If addrMode.Size = DataSize.Byte Then
                        Dim m1 As UInt32 = To16bitsWithSign(mRegisters.AL)
                        Dim m2 As UInt32 = To16bitsWithSign(mRegisters.Val(addrMode.Register2))

                        m1 = If((m1 And &H80) <> 0, m1 Or &HFFFFFF00UI, m1)
                        m2 = If((m2 And &H80) <> 0, m2 Or &HFFFFFF00UI, m2)

                        tmpUVal = m1 * m2
                        mRegisters.AX = tmpUVal
                        clkCyc += 70
                    Else
                        Dim m1 As UInt32 = To32bitsWithSign(mRegisters.AX)
                        Dim m2 As UInt32 = To32bitsWithSign(mRegisters.Val(addrMode.Register2))

                        m1 = If((m1 And &H8000) <> 0, m1 Or &HFFFF0000UI, m1)
                        m2 = If((m2 And &H8000) <> 0, m2 Or &HFFFF0000UI, m2)

                        tmpUVal = m1 * m2
                        mRegisters.AX = tmpUVal
                        mRegisters.DX = tmpUVal >> 16
                        clkCyc += 118
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        Dim m1 As UInt32 = To16bitsWithSign(mRegisters.AL)
                        Dim m2 As UInt32 = To16bitsWithSign(addrMode.IndMem)

                        m1 = If((m1 And &H80) <> 0, m1 Or &HFFFFFF00UI, m1)
                        m2 = If((m2 And &H80) <> 0, m2 Or &HFFFFFF00UI, m2)

                        tmpUVal = m1 * m2
                        mRegisters.AX = tmpUVal
                        clkCyc += 76
                    Else
                        Dim m1 As UInt32 = To32bitsWithSign(mRegisters.AX)
                        Dim m2 As UInt32 = To32bitsWithSign(addrMode.IndMem)

                        m1 = If((m1 And &H8000) <> 0, m1 Or &HFFFF0000UI, m1)
                        m2 = If((m2 And &H8000) <> 0, m2 Or &HFFFF0000UI, m2)

                        tmpUVal = m1 * m2
                        mRegisters.AX = tmpUVal
                        mRegisters.DX = tmpUVal >> 16
                        clkCyc += 134
                    End If
                End If

                If If(addrMode.Size = DataSize.Byte, mRegisters.AH, mRegisters.DX) <> 0 Then
                    mFlags.CF = 1
                    mFlags.OF = 1
                Else
                    mFlags.CF = 0
                    mFlags.OF = 0
                End If
                If Not mVic20 Then mFlags.ZF = 0

            Case 6 ' 110    --  div
                Dim div As UInt32
                Dim num As UInt32
                Dim result As UInt32
                Dim remain As UInt32

                If addrMode.IsDirect Then
                    div = mRegisters.Val(addrMode.Register2)
                Else
                    div = addrMode.IndMem
                End If

                If addrMode.Size = DataSize.Byte Then
                    num = mRegisters.AX
                    clkCyc += 86
                Else
                    num = (CUInt(mRegisters.DX) << 16) Or mRegisters.AX
                    clkCyc += 150
                End If

                If div = 0 Then
                    HandleInterrupt(0, True)
                    Exit Select
                End If

                result = num \ div
                remain = num Mod div

                If addrMode.Size = DataSize.Byte Then
                    If result > &HFF Then
                        HandleInterrupt(0, True)
                        Exit Select
                    End If
                    mRegisters.AL = result
                    mRegisters.AH = remain
                Else
                    If result > &HFFFF Then
                        HandleInterrupt(0, True)
                        Exit Select
                    End If
                    mRegisters.AX = result
                    mRegisters.DX = remain
                End If

            Case 7 ' 111    --  idiv
                Dim div As UInt32
                Dim num As UInt32
                Dim result As UInt32
                Dim remain As UInt32
                Dim signN As Boolean
                Dim signD As Boolean

                If addrMode.IsDirect Then
                    If addrMode.Size = DataSize.Byte Then
                        num = mRegisters.AX
                        div = To16bitsWithSign(mRegisters.Val(addrMode.Register2))

                        signN = (num And &H8000) <> 0
                        signD = (div And &H8000) <> 0
                        num = If(signN, ((Not num) + 1) And &HFFFF, num)
                        div = If(signD, ((Not div) + 1) And &HFFFF, div)

                        clkCyc += 80
                    Else
                        num = (CUInt(mRegisters.DX) << 16) Or mRegisters.AX
                        div = To32bitsWithSign(mRegisters.Val(addrMode.Register2))

                        signN = (num And &H80000000UI) <> 0
                        signD = (div And &H80000000UI) <> 0
                        num = If(signN, ((Not num) + 1) And &HFFFFFFFFUI, num)
                        div = If(signD, ((Not div) + 1) And &HFFFFFFFFUI, div)

                        clkCyc += 144
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        num = mRegisters.AX
                        div = To16bitsWithSign(addrMode.IndMem)

                        signN = (num And &H8000) <> 0
                        signD = (div And &H8000) <> 0
                        num = If(signN, ((Not num) + 1) And &HFFFF, num)
                        div = If(signD, ((Not div) + 1) And &HFFFF, div)

                        clkCyc += 86
                    Else
                        num = (CUInt(mRegisters.DX) << 16) Or mRegisters.AX
                        div = To32bitsWithSign(addrMode.IndMem)

                        signN = (num And &H80000000UI) <> 0
                        signD = (div And &H80000000UI) <> 0
                        num = If(signN, ((Not num) + 1) And &HFFFFFFFFUI, num)
                        div = If(signD, ((Not div) + 1) And &HFFFFFFFFUI, div)

                        clkCyc += 150
                    End If
                End If

                If div = 0 Then
                    HandleInterrupt(0, True)
                    Exit Select
                End If

                result = num \ div
                remain = num Mod div

                If signN <> signD Then
                    If result > If(addrMode.Size = DataSize.Byte, &H80, &H8000) Then
                        HandleInterrupt(0, True)
                        Exit Select
                    End If
                    result = (Not result) + 1
                Else
                    If result > If(addrMode.Size = DataSize.Byte, &H7F, &H7FFF) Then
                        HandleInterrupt(0, True)
                        Exit Select
                    End If
                End If

                If signN Then remain = (Not remain) + 1

                If addrMode.Size = DataSize.Byte Then
                    mRegisters.AL = result
                    mRegisters.AH = remain
                Else
                    mRegisters.AX = result
                    mRegisters.DX = remain
                End If

            Case Else
                OpCodeNotImplemented($"Unknown Reg Mode {addrMode.Reg} for Opcode {opCode:X} (Group3)")
        End Select
    End Sub

    Private Sub ExecuteGroup4_And_5() ' &HFE, &hFF
        SetAddressing()

        Select Case addrMode.Reg
            Case 0 ' 000 inc reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), 1, Operation.Increment, addrMode.Size)
                    clkCyc += 3
                Else
                    RAMn = Eval(addrMode.IndMem, 1, Operation.Increment, addrMode.Size)
                    clkCyc += 15
                End If

            Case 1 ' 001 dec reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), 1, Operation.Decrement, addrMode.Size)
                    clkCyc += 3
                Else
                    RAMn = Eval(addrMode.IndMem, 1, Operation.Decrement, addrMode.Size)
                    clkCyc += 15
                End If

            Case 2 ' 010 call indirect within segment
                PushIntoStack(mRegisters.IP + opCodeSize)
                IPAddrOffet = If(addrMode.IsDirect,
                                    mRegisters.Val(addrMode.Register2),
                                    addrMode.IndMem)
                clkCyc += 11

            Case 3 ' 011 call indirect inter-segment
                PushIntoStack(mRegisters.CS)
                PushIntoStack(mRegisters.IP + opCodeSize)
                IPAddrOffet = addrMode.IndMem
                mRegisters.CS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                clkCyc += 37

            Case 4 ' 100 jmp indirect within segment
                IPAddrOffet = If(addrMode.IsDirect,
                                    mRegisters.Val(addrMode.Register2),
                                    addrMode.IndMem)
                clkCyc += 15

            Case 5 ' 101 jmp indirect inter-segment
                IPAddrOffet = addrMode.IndMem
                mRegisters.CS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                clkCyc += 24

            Case 6 ' 110 push reg/mem
                If addrMode.IsDirect Then
                    If addrMode.Register2 = GPRegisters.RegistersTypes.SP Then
                        PushIntoStack(mRegisters.SP - 2)
                    Else
                        PushIntoStack(mRegisters.Val(addrMode.Register2))
                    End If
                Else
                    PushIntoStack(addrMode.IndMem)
                End If
                clkCyc += 16

            Case Else
                OpCodeNotImplemented($"Unknown Reg Mode {addrMode.Reg} for Opcode {opCode:X} (Group4&5)")
        End Select
    End Sub

    Private Sub HandleREPMode()
        tmpUVal = mRegisters.ActiveSegmentValue
        tmpVal = If((opCode And 1) = 1, 2, 1) * If(mFlags.DF = 0, 1, -1)

        If mRepeLoopMode = REPLoopModes.None Then
            ExecStringOpCode()
        ElseIf mDebugMode AndAlso mRegisters.CX > 0 Then
            mRegisters.CX -= 1
            If ExecStringOpCode() Then
                If (mRepeLoopMode = REPLoopModes.REPE AndAlso mFlags.ZF = 0) OrElse
                   (mRepeLoopMode = REPLoopModes.REPENE AndAlso mFlags.ZF = 1) Then
                    Exit Sub
                End If
            End If

            mRegisters.IP -= (opCodeSize + 1)
        Else
            While mRegisters.CX > 0
                mRegisters.CX -= 1
                If ExecStringOpCode() Then
                    If (mRepeLoopMode = REPLoopModes.REPE AndAlso mFlags.ZF = 0) OrElse
                       (mRepeLoopMode = REPLoopModes.REPENE AndAlso mFlags.ZF = 1) Then
                        Exit While
                    End If
                End If
            End While
        End If
    End Sub

    Private Function ExecStringOpCode() As Boolean
        instrucionsCounter += 1

        Select Case opCode
            Case &HA4  ' movsb
                RAM8(mRegisters.ES, mRegisters.DI,, True) = RAM8(tmpUVal, mRegisters.SI,, True)
                mRegisters.SI += tmpVal
                mRegisters.DI += tmpVal
                clkCyc += 18
                Return False

            Case &HA5 ' movsw
                RAM16(mRegisters.ES, mRegisters.DI,, True) = RAM16(tmpUVal, mRegisters.SI,, True)
                mRegisters.SI += tmpVal
                mRegisters.DI += tmpVal
                clkCyc += 18
                Return False

            Case &HA6  ' cmpsb
                Eval(RAM8(tmpUVal, mRegisters.SI,, True), RAM8(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Byte)
                mRegisters.SI += tmpVal
                mRegisters.DI += tmpVal
                clkCyc += 22
                Return True

            Case &HA7 ' cmpsw
                Eval(RAM16(tmpUVal, mRegisters.SI,, True), RAM16(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Word)
                mRegisters.SI += tmpVal
                mRegisters.DI += tmpVal
                clkCyc += 22
                Return True

            Case &HAA ' stosb
                RAM8(mRegisters.ES, mRegisters.DI,, True) = mRegisters.AL
                mRegisters.DI += tmpVal
                clkCyc += 11
                Return False

            Case &HAB 'stosw
                RAM16(mRegisters.ES, mRegisters.DI,, True) = mRegisters.AX
                mRegisters.DI += tmpVal
                clkCyc += 11
                Return False

            Case &HAC ' lodsb
                mRegisters.AL = RAM8(tmpUVal, mRegisters.SI,, True)
                mRegisters.SI += tmpVal
                clkCyc += 12
                Return False

            Case &HAD ' lodsw
                mRegisters.AX = RAM16(tmpUVal, mRegisters.SI,, True)
                mRegisters.SI += tmpVal
                clkCyc += 16
                Return False

            Case &HAE ' scasb
                Eval(mRegisters.AL, RAM8(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Byte)
                mRegisters.DI += tmpVal
                clkCyc += 15
                Return True

            Case &HAF ' scasw
                Eval(mRegisters.AX, RAM16(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Word)
                mRegisters.DI += tmpVal
                clkCyc += 15
                Return True

        End Select

        Return False
    End Function
End Class
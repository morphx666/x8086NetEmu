' Map Of Instructions: http://www.mlsite.net/8086/ and http://www.sandpile.org/x86/opc_1.htm
' http://en.wikibooks.org/wiki/X86_Assembly/Machine_Language_Conversion
' http://www.xs4all.nl/~ganswijk/chipdir/iset/8086bin.txt
' The Intel 8086 / 8088/ 80186 / 80286 / 80386 / 80486 Instruction Set: http://zsmith.co/intel.html
' http://www.felixcloutier.com/x86/
' https://c9x.me/x86/
' http://www.o3one.org/hwdocs/vga/vga_app.html

Imports System.Threading
Imports System.Threading.Tasks

Public Class X8086
    Public Enum Models
        IBMPC_5150
        IBMPC_5160
    End Enum

    Private mModel As Models = Models.IBMPC_5160

    Private mRegisters As New GPRegisters()
    Private mFlags As New GPFlags()

    Public Enum MemHookMode
        Read
        Write
    End Enum
    Public Delegate Function MemHandler(address As UInt32, ByRef value As UInt16, mode As MemHookMode) As Boolean
    Private ReadOnly memHooks As New List(Of MemHandler)
    Public Delegate Function IntHandler() As Boolean
    Protected Friend ReadOnly intHooks As New Dictionary(Of Byte, IntHandler)

    Private opCode As Byte
    Private opCodeSize As Byte

    Private tmpUVal1 As UInt32
    Private tmpUVal2 As UInt32

    Private addrMode As AddressingMode
    Private mIsExecuting As Boolean = False

    Private instructionsCounter As UInt32
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
    Public Const BASECLOCK As ULong = 4.77273 * MHz ' http://dosmandrivel.blogspot.com/2009/03/ibm-pc-design-antics.html
    Private mClock As ULong = BASECLOCK
    Private clkCyc As ULong = 0

    Private mDoReSchedule As Boolean
    Private mSimulationMultiplier As Double = 1.0
    Private leftCycleFrags As ULong

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
    Public Event DebugModeChanged(sender As Object, e As EventArgs)
    Public Event MIPsUpdated()

    Public Delegate Sub RestartEmulation()
    Private restartCallback As RestartEmulation

    Public Shared IsClosing As Boolean = False

    Public Shared BasePath As String

    Public Sub New(Optional v20 As Boolean = True,
                   Optional int13 As Boolean = True,
                   Optional restartEmulationCallback As RestartEmulation = Nothing,
                   Optional model As Models = Models.IBMPC_5160,
                   Optional basePath As String = ".\")

        IsClosing = False
        Scheduler.HOSTCLOCK = Stopwatch.Frequency
        'Scheduler.HOSTCLOCK = GetCpuSpeed() * 10000

        mV20 = v20
        mEmulateINT13 = int13
        restartCallback = restartEmulationCallback
        mModel = model
        X8086.BasePath = basePath

        debugWaiter = New AutoResetEvent(False)
        addrMode = New AddressingMode()

        BuildSZPTables()
        BuildDecoderCache()
        Init()

        Task.Run(action:=Async Sub()
                             Do
                                 Await Task.Delay(1000)
                                 MIPSCounterLoop()
                             Loop Until IsClosing
                         End Sub)
    End Sub

    Private Sub Init()
        Sched = New Scheduler(Me)

        ' If FPU Is Nothing Then FPU = New x8087(Me)
        If PIC Is Nothing Then PIC = New PIC8259(Me)
        If DMA Is Nothing Then DMA = New DMAI8237(Me)
        If PIT Is Nothing Then PIT = New PIT8254(Me, PIC.GetIrqLine(0))
        If PPI Is Nothing Then PPI = New PPI8255(Me, PIC.GetIrqLine(1))
        If RTC Is Nothing Then RTC = New RTC(Me, PIC.GetIrqLine(8))

        'mPorts.Add(FPU)
        mPorts.Add(PIC)
        mPorts.Add(DMA)
        mPorts.Add(PIT)
        mPorts.Add(PPI)
        mPorts.Add(RTC)

        SetupSystem()

        Array.Clear(Memory, 0, Memory.Length)

        portsCache.Clear()

        AddInternalHooks()
        LoadBIOS()

        audioSubSystem = New AudioSubsystem(Me)

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

        If mVideoAdapter IsNot Nothing AndAlso
            TypeOf mVideoAdapter Is VGAAdapter Then equipmentByte = equipmentByte And &B11111111111001111
        If FPU IsNot Nothing Then equipmentByte = equipmentByte Or &B10
        If PPI IsNot Nothing Then PPI.SwitchData = equipmentByte
    End Sub

    Private Sub LoadBIOS()
        ' BIOS
        LoadBIN("roms\pcxtbios.rom", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\xtbios2\EPROMS\2764\XTBIOS.ROM", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\xtbios25\EPROMS\2764\PCXTBIOS.ROM", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\xtbios30\eproms\2764\pcxtbios.ROM", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\xtbios31\pcxtbios.bin", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\PCemV14Win\roms\genxt\pcxt.rom", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\fake86-0.12.9.19-win32\Binaries\pcxtbios.bin", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\award-2.05.rom", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\phoenix-2.51.rom", &HFE00, &H0)
        'LoadBIN("..\Other Emulators & Resources\PCE - PC Emulator\bin\rom\ibm-pc-1982.rom", &HFE00, &H0)
        'LoadBIN("C:\Users\XavierFlix\Downloads\MartyPC_win64_0_1_3\roms\GLABIOS_0.2.5_8XC.ROM", &HFE00, &H0)
        'LoadBIN("C:\Users\XavierFlix\Downloads\MartyPC_win64_0_1_3\roms\GLABIOS_0.2.5_8PC.ROM", &HFE00, &H0)

        ' BASIC C1.10
        LoadBIN("roms\basicc11.bin", &HF600, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicf6.rom", &HF600, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicf8.rom", &HF800, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicfa.rom", &HFA00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios30\eproms\2764\basicfc.rom", &HFC00, &H0)

        ' Lots of ROMs: http://www.hampa.ch/pce/download.html

        ' XT IDE
        'LoadBIN("roms\ide_xt.bin", &HD000, &H0)
    End Sub

    Public Sub Close()
        IsClosing = True
        mRepeLoopMode = REPLoopModes.None

        If DebugMode Then debugWaiter.Set()
        If Sched IsNot Nothing Then Sched.Stop()

        For Each adapter As Adapter In mAdapters
            adapter.CloseAdapter()
        Next
        mAdapters.Clear()
        mPorts.Clear()

        memHooks.Clear()
        intHooks.Clear()

        audioSubSystem.Dispose()

        Sched = Nothing
    End Sub

    Public Sub SoftReset()
        PPI.PutKeyData(Adapter.XEventArgs.Keys.ControlKey, False)
        PPI.PutKeyData(Adapter.XEventArgs.Keys.Menu, False)
        PPI.PutKeyData(Adapter.XEventArgs.Keys.Delete, False)
    End Sub

    Public Sub FastHardReset()
        Sched.Stop()

        Task.Run(action:=Async Sub()
                             Do
                                 If Not mDoReSchedule OrElse mIsExecuting Then
                                     Await Task.Delay(1)
                                 Else
                                     Exit Do
                                 End If
                             Loop
                         End Sub).Wait()

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
    End Sub

    Public Sub HardReset()
        Close()
        If restartCallback IsNot Nothing Then
            restartCallback.Invoke()
        Else
            Init()
        End If
    End Sub

    Public Sub StepInto()
        debugWaiter.Set()
    End Sub

    Public Sub Run(Optional debugMode As Boolean = False, Optional cs As UInt16 = &HFFFF, Optional ip As UInt16 = 0)
        SetSynchronization()

        mDebugMode = debugMode

        If PIT?.Speaker IsNot Nothing Then PIT.Speaker.Enabled = True
        If mVideoAdapter IsNot Nothing Then mVideoAdapter.Reset()

        If debugMode Then RaiseEvent InstructionDecoded()

        mRegisters.CS = cs
        mRegisters.IP = ip

        For Each adptr In mAdapters
            adptr.InitAdapter()
        Next

        Sched.Start()
        audioSubSystem.Init()
    End Sub

    Private Sub MIPSCounterLoop()
        mMPIs = instructionsCounter / 1_000_000
        instructionsCounter = 0

        RaiseEvent MIPsUpdated()
    End Sub

    Public Sub Pause()
        mIsPaused = True
        If PIT?.Speaker IsNot Nothing Then PIT.Speaker.Enabled = False
    End Sub

    Public Sub [Resume]()
        DoReschedule = False
        mIsPaused = False
    End Sub

    Public Property DoReschedule As Boolean
        Get
            Return mDoReSchedule
        End Get
        Set(value As Boolean)
            mDoReSchedule = value
        End Set
    End Property

    Private Sub FlushCycles()
        Dim t As Long = clkCyc * Scheduler.HOSTCLOCK + leftCycleFrags
        Sched.AdvanceTime(t \ BASECLOCK)
        leftCycleFrags = t Mod BASECLOCK

        clkCyc = 0
        DoReschedule = True
    End Sub

    Private Sub SetSynchronization()
        Sched.SetSynchronization(True,
                                (Scheduler.HOSTCLOCK \ 100),
                                (Scheduler.HOSTCLOCK \ 1000) * mSimulationMultiplier)

        'PIT?.UpdateClock()
        'VideoAdapter?.UpdateClock()
    End Sub

    Public Sub RunEmulation()
        If mIsExecuting OrElse mIsPaused Then Exit Sub

        Dim maxRunTime As Long = Sched.GetTimeToNextEvent()
        If maxRunTime > Scheduler.HOSTCLOCK Then maxRunTime = Scheduler.HOSTCLOCK
        Dim maxRunCycl As Long = (maxRunTime * BASECLOCK - leftCycleFrags + Scheduler.HOSTCLOCK - 1) \ Scheduler.HOSTCLOCK

        DoReschedule = False

        If DebugMode Then
            While clkCyc < maxRunCycl AndAlso Not DoReschedule AndAlso DebugMode
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
            While clkCyc < maxRunCycl AndAlso Not DoReschedule
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

        If clkCyc > 0 OrElse DoReschedule Then FlushCycles()
    End Sub

    Public Sub PreExecute()
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
        instructionsCounter += 1

        opCode = RAM8(mRegisters.CS, mRegisters.IP)
    End Sub

    Public Sub Execute_DEBUG()
        Select Case opCode
            Case &H0 To &H3 ' ADD Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &H4 ' ADD AL Ib
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Add, DataSize.Byte)
                clkCyc += 4
            Case &H5 ' ADD AX Iv
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Add, DataSize.Word)
                clkCyc += 4

            Case &H6 ' PUSH ES
                PushIntoStack(mRegisters.ES)
                clkCyc += 10

            Case &H7 ' POP ES
                mRegisters.ES = PopFromStack()
                ignoreINTs = True
                clkCyc += 8

            Case &H8 To &HB ' OR Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &HC ' OR AL Ib
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicOr, DataSize.Byte)
                clkCyc += 4
            Case &HD ' OR AX Iv
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicOr, DataSize.Word)
                clkCyc += 4

            Case &HE ' PUSH CS
                PushIntoStack(mRegisters.CS)
                clkCyc += 10

            Case &HF ' POP CS
                If mV20 Then
                    PopFromStack()
                Else
                    mRegisters.CS = PopFromStack()
                End If
                ignoreINTs = True
                clkCyc += 8

            Case &H10 To &H13 ' ADC Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &H14 ' ADC AL Ib
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.AddWithCarry, DataSize.Byte)
                clkCyc += 3
            Case &H15 ' ADC AX Iv
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.AddWithCarry, DataSize.Word)
                clkCyc += 3

            Case &H16 ' PUSH SS
                PushIntoStack(mRegisters.SS)
                clkCyc += 10

            Case &H17 ' POP SS
                mRegisters.SS = PopFromStack()
                ' Lesson 4: http://ntsecurity.nu/onmymind/2007/2007-08-22.html
                ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
                ' http://www.vcfed.org/forum/archive/index.php/t-41453.html
                ignoreINTs = True
                clkCyc += 8

            Case &H18 To &H1B ' SBB Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &H1C ' SBB AL Ib
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.SubstractWithCarry, DataSize.Byte)
                clkCyc += 4
            Case &H1D ' SBB AX Iv
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.SubstractWithCarry, DataSize.Word)
                clkCyc += 4

            Case &H1E ' PUSH DS
                PushIntoStack(mRegisters.DS)
                clkCyc += 10

            Case &H1F ' POP DS
                mRegisters.DS = PopFromStack()
                ignoreINTs = True
                clkCyc += 8

            Case &H20 To &H23 ' AND Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &H24 ' AND AL Ib
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicAnd, DataSize.Byte)
                clkCyc += 4
            Case &H25 ' AND AX Iv
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicAnd, DataSize.Word)
                clkCyc += 4

            Case &H26, &H2E, &H36, &H3E ' ES, CS, SS and DS segment override prefix
                addrMode.Decode(opCode, opCode)
                mRegisters.ActiveSegmentRegister = addrMode.Dst - GPRegisters.RegistersTypes.AH + GPRegisters.RegistersTypes.ES
                newPrefix = True
                clkCyc += 2

            Case &H27 ' DAA
                Dim oldAL As Byte = mRegisters.AL
                Dim tmpAL As Byte = If(mFlags.AF = 1, &H9F, &H99)
                Dim oldCF As Byte = mFlags.CF

                mFlags.CF = 0
                mFlags.OF = 0
                If oldCF = 1 Then
                    If mRegisters.AL >= &H1A AndAlso mRegisters.AL <= &H7F Then
                        mFlags.OF = 1
                    End If
                ElseIf mRegisters.AL >= &H7A AndAlso mRegisters.AL <= &H7F Then
                    mFlags.OF = 1
                End If

                If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
                    mRegisters.AL += 6
                    mFlags.AF = 1
                Else
                    mFlags.AF = 0
                End If
                If oldAL > tmpAL OrElse oldCF = 1 Then
                    mRegisters.AL += &H60
                    mFlags.CF = 1
                Else
                    mFlags.CF = 0
                End If

                SetSZPFlags(mRegisters.AX, DataSize.Byte)
                clkCyc += 4

            Case &H28 To &H2B ' SUB Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &H2C ' SUB AL Ib
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Substract, DataSize.Byte)
                clkCyc += 4
            Case &H2D ' SUB AX, Iv
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Substract, DataSize.Word)
                clkCyc += 4

            Case &H2F ' DAS
                Dim oldAL As Byte = mRegisters.AL
                Dim tmpAL As Byte = If(mFlags.AF = 1, &H9F, &H99)
                Dim oldCF As Byte = mFlags.CF
                Dim oldAF As Byte = mFlags.AF

                mFlags.CF = 0
                mFlags.OF = 0
                If oldAF = 0 AndAlso oldCF = 0 AndAlso mRegisters.AL >= &H9A AndAlso mRegisters.AL <= &HDF Then mFlags.OF = 1
                If oldAF = 1 AndAlso oldCF = 0 AndAlso mRegisters.AL >= &H80 AndAlso mRegisters.AL <= &H85 Then mFlags.OF = 1
                If oldAF = 1 AndAlso oldCF = 0 AndAlso mRegisters.AL >= &HA0 AndAlso mRegisters.AL <= &HE5 Then mFlags.OF = 1
                If oldAF = 0 AndAlso oldCF = 1 AndAlso mRegisters.AL >= &H80 AndAlso mRegisters.AL <= &HDF Then mFlags.OF = 1
                If oldAF = 1 AndAlso oldCF = 1 AndAlso mRegisters.AL >= &H80 AndAlso mRegisters.AL <= &HE5 Then mFlags.OF = 1

                If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
                    mRegisters.AL -= 6
                    mFlags.AF = 1
                Else
                    mFlags.AF = 0
                End If

                If oldAL > tmpAL OrElse oldCF = 1 Then
                    mRegisters.AL -= &H60
                    mFlags.CF = 1
                Else
                    mFlags.CF = 0
                End If

                SetSZPFlags(mRegisters.AX, DataSize.Byte)
                clkCyc += 4

            Case &H30 To &H33 ' XOR Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &H34 ' XOR AL Ib
                mRegisters.AL = Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.LogicXor, DataSize.Byte)
                clkCyc += 4
            Case &H35 ' XOR AX Iv
                mRegisters.AX = Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.LogicXor, DataSize.Word)
                clkCyc += 4

            Case &H37 ' AAA
                Dim oldAL As Byte = mRegisters.AL

                If mRegisters.AL.LowNib() > 9 OrElse mFlags.AF = 1 Then
                    mRegisters.AH += 1
                    tmpUVal1 = (mRegisters.AL + 6) And &HFF
                    mRegisters.AL = tmpUVal1 And &HF
                    mFlags.AF = 1
                    mFlags.CF = 1
                Else
                    tmpUVal1 = mRegisters.AL
                    mRegisters.AL = mRegisters.AL And &HF
                    mFlags.AF = 0
                    mFlags.CF = 0
                End If

                SetSZPFlags(tmpUVal1, DataSize.Word)
                mFlags.ZF = 0
                mFlags.SF = 0
                mFlags.OF = 0
                If tmpUVal1 = 0 Then mFlags.ZF = 1
                If oldAL >= &H7A AndAlso oldAL <= &H7F Then mFlags.OF = 1
                If oldAL >= &H7A AndAlso oldAL <= &HF9 Then mFlags.SF = 1

                clkCyc += 8

            Case &H38 To &H3B ' CMP Eb Gb | Ev Gv | Gb Eb | Gv Ev
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
            Case &H3C ' CMP AL Ib
                Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Compare, DataSize.Byte)
                clkCyc += 4
            Case &H3D ' CMP AX Iv
                Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Compare, DataSize.Word)
                clkCyc += 4

            Case &H3F ' AAS
                Dim oldAL As Byte = mRegisters.AL
                Dim oldAF As Byte = mFlags.AF

                If mRegisters.AL.LowNib() > 9 OrElse oldAF = 1 Then
                    tmpUVal1 = (mRegisters.AL - 6) And &HFF
                    mRegisters.AH -= 1
                    mRegisters.AL = tmpUVal1 And &HF
                    mFlags.AF = 1
                    mFlags.CF = 1
                Else
                    tmpUVal1 = mRegisters.AL
                    mRegisters.AL = mRegisters.AL And &HF
                    mFlags.AF = 0
                    mFlags.CF = 0
                End If

                SetSZPFlags(tmpUVal1, DataSize.Word)
                mFlags.ZF = 0
                mFlags.SF = 0
                mFlags.OF = 0
                If tmpUVal1 = 0 Then mFlags.ZF = 1
                If oldAF = 1 AndAlso oldAL >= &H80 AndAlso oldAL <= &H85 Then mFlags.OF = 1
                If oldAF = 0 AndAlso oldAL >= &H80 Then mFlags.SF = 1
                If oldAF = 1 AndAlso oldAL <= &H5 OrElse oldAL >= &H86 Then mFlags.SF = 1
                clkCyc += 8

            Case &H40 To &H47 ' INC AX | CX | DX | BX | SP | BP | SI | DI
                SetRegister1Alt(opCode)
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Increment, DataSize.Word)
                clkCyc += 3

            Case &H48 To &H4F ' DEC AX | CX | DX | BX | SP | BP | SI | DI
                SetRegister1Alt(opCode)
                mRegisters.Val(addrMode.Register1) = Eval(mRegisters.Val(addrMode.Register1), 1, Operation.Decrement, DataSize.Word)
                clkCyc += 3

            Case &H50 To &H57 ' PUSH AX | CX | DX | BX | SP | BP | SI | DI
                If opCode = &H54 Then ' SP
                    ' The 8086/8088 pushes the value of SP after it has been decremented
                    ' http://css.csail.mit.edu/6.858/2013/readings/i386/s15_06.htm
                    PushIntoStack(mRegisters.SP - 2)
                Else
                    SetRegister1Alt(opCode)
                    PushIntoStack(mRegisters.Val(addrMode.Register1))
                End If
                clkCyc += 11

            Case &H58 To &H5F ' POP AX | CX | DX | BX | SP | BP | SI | DI
                SetRegister1Alt(opCode)
                mRegisters.Val(addrMode.Register1) = PopFromStack()
                clkCyc += 8

            Case &H60 ' PUSHA (80186)
                If mV20 Then
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

            Case &H61 ' POPA (80186)
                If mV20 Then
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

            Case &H62 ' BOUND (80186)
                If mV20 Then
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

            Case &H68 ' PUSH Iv (80186)
                ' PRE ALPHA CODE - UNTESTED
                If mV20 Then
                    PushIntoStack(Param(ParamIndex.First, , DataSize.Word))
                    clkCyc += 3
                Else
                    OpCodeNotImplemented()
                End If

            Case &H69 ' IMUL (80186)
                If mV20 Then
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

            Case &H6A ' PUSH Ib (80186)
                If mV20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    PushIntoStack(Param(ParamIndex.First, , DataSize.Byte))
                    clkCyc += 3
                Else
                    OpCodeNotImplemented()
                End If

            Case &H6B ' IMUL (80186)
                If mV20 Then
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

            Case &H6C To &H6F ' Ignore 80186/V20 port operations... for now...
                opCodeSize += 1
                clkCyc += 3

            Case &H70 ' JO Jb
                If mFlags.OF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H71 ' JNO  Jb
                If mFlags.OF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H72 ' JB/JNAE/JC Jb
                If mFlags.CF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H73 ' JNB/JAE/JNC Jb
                If mFlags.CF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H74 ' JZ/JE Jb
                If mFlags.ZF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H75 ' JNZ/JNE Jb
                If mFlags.ZF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H76 ' JBE/JNA Jb
                If mFlags.CF = 1 OrElse mFlags.ZF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H77 ' JA/JNBE Jb
                If mFlags.CF = 0 AndAlso mFlags.ZF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H78 ' JS Jb
                If mFlags.SF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H79 ' JNS Jb
                If mFlags.SF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7A ' JPE/JP Jb
                If mFlags.PF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7B ' JPO/JNP Jb
                If mFlags.PF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7C ' JL/JNGE Jb
                If mFlags.SF <> mFlags.OF Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7D ' JGE/JNL Jb
                If mFlags.SF = mFlags.OF Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7E ' JLE/JNG Jb
                If mFlags.ZF = 1 OrElse (mFlags.SF <> mFlags.OF) Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7F ' JG/JNLE Jb
                If mFlags.ZF = 0 AndAlso (mFlags.SF = mFlags.OF) Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H80 To &H83 : ExecuteGroup1()

            Case &H84 To &H85 ' TEST Gb Eb | Gv Ev
                SetAddressing()
                If addrMode.IsDirect Then
                    Eval(mRegisters.Val(addrMode.Dst), mRegisters.Val(addrMode.Src), Operation.Test, addrMode.Size)
                    clkCyc += 3
                Else
                    Eval(addrMode.IndMem, mRegisters.Val(addrMode.Src), Operation.Test, addrMode.Size)
                    clkCyc += 9
                End If

            Case &H86 To &H87 ' XCHG Gb Eb | Gv Ev
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

            Case &H88 To &H8B ' MOV Eb Gb | Ev Gv | Gb Eb | Gv Ev
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

            Case &H8C ' MOV Ew Sw
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

            Case &H8D ' LEA Gv M
                SetAddressing()
                mRegisters.Val(addrMode.Src) = addrMode.IndAdr
                clkCyc += 2

            Case &H8E ' MOV Sw Ew
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

            Case &H8F ' POP Ev
                SetAddressing()
                If addrMode.IsDirect Then
                    addrMode.Decode(opCode, opCode)
                    mRegisters.Val(addrMode.Register1) = PopFromStack()
                Else
                    RAMn = PopFromStack()
                End If
                clkCyc += 17

            Case &H90 ' NOP
                clkCyc += 3

            Case &H91 ' XCHG CX AX
                tmpUVal1 = mRegisters.AX
                mRegisters.AX = mRegisters.CX
                mRegisters.CX = tmpUVal1
                clkCyc += 3
            Case &H92 ' XCHG DX AX
                tmpUVal1 = mRegisters.AX
                mRegisters.AX = mRegisters.DX
                mRegisters.DX = tmpUVal1
                clkCyc += 3
            Case &H93 ' XCHG BX AX
                tmpUVal1 = mRegisters.AX
                mRegisters.AX = mRegisters.BX
                mRegisters.BX = tmpUVal1
                clkCyc += 3
            Case &H94 ' XCHG SP AX
                tmpUVal1 = mRegisters.AX
                mRegisters.AX = mRegisters.SP
                mRegisters.SP = tmpUVal1
                clkCyc += 3
            Case &H95 ' XCHG BP AX
                tmpUVal1 = mRegisters.AX
                mRegisters.AX = mRegisters.BP
                mRegisters.BP = tmpUVal1
                clkCyc += 3
            Case &H96 ' XCHG SI AX
                tmpUVal1 = mRegisters.AX
                mRegisters.AX = mRegisters.SI
                mRegisters.SI = tmpUVal1
                clkCyc += 3
            Case &H97 ' XCHG DI AX
                tmpUVal1 = mRegisters.AX
                mRegisters.AX = mRegisters.DI
                mRegisters.DI = tmpUVal1
                clkCyc += 3

            Case &H98 ' CBW
                mRegisters.AX = To16bitsWithSign(mRegisters.AL)
                clkCyc += 2

            Case &H99 ' CWD
                mRegisters.DX = If((mRegisters.AH And &H80) <> 0, &HFFFF, &H0)
                clkCyc += 5

            Case &H9A ' CALL Ap
                IPAddrOffet = Param(ParamIndex.First, , DataSize.Word)
                tmpUVal1 = Param(ParamIndex.Second, , DataSize.Word)
                PushIntoStack(mRegisters.CS)
                PushIntoStack(mRegisters.IP + opCodeSize)
                mRegisters.CS = tmpUVal1
                clkCyc += 28

            Case &H9B ' WAIT
                clkCyc += 4

            Case &H9C ' PUSHF
                PushIntoStack(If(mModel = Models.IBMPC_5150, &HFFF, &HFFFF) And mFlags.EFlags)
                clkCyc += 10

            Case &H9D ' POPF
                mFlags.EFlags = PopFromStack()
                clkCyc += 8

            Case &H9E ' SAHF
                mFlags.EFlags = (mFlags.EFlags And &HFF00) Or mRegisters.AH
                clkCyc += 4

            Case &H9F ' LAHF
                mRegisters.AH = mFlags.EFlags
                clkCyc += 4

            Case &HA0 ' MOV AL Ob
                mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) : clkCyc += 10
            Case &HA1 ' MOV AX Ov
                mRegisters.AX = RAM16(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) : clkCyc += 10
            Case &HA2 ' MOV Ob AL
                RAM8(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) = mRegisters.AL : clkCyc += 10
            Case &HA3 ' MOV Ov AX
                RAM16(mRegisters.ActiveSegmentValue, Param(ParamIndex.First,, DataSize.Word)) = mRegisters.AX : clkCyc += 10

            Case &HA4 To &HA7, &HAA To &HAF : HandleREPMode()

            Case &HA8 ' TEST AL Ib
                Eval(mRegisters.AL, Param(ParamIndex.First, , DataSize.Byte), Operation.Test, DataSize.Byte)
                clkCyc += 4
            Case &HA9 ' TEST AX Iv
                Eval(mRegisters.AX, Param(ParamIndex.First, , DataSize.Word), Operation.Test, DataSize.Word)
                clkCyc += 4

            Case &HB0 ' MOV AL Ib
                mRegisters.AL = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB1 ' MOV CL Ib
                mRegisters.CL = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB2 ' MOV DL Ib
                mRegisters.DL = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB3 ' MOV BL Ib
                mRegisters.BL = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB4 ' MOV AH Ib
                mRegisters.AH = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB5 ' MOV CH Ib
                mRegisters.CH = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB6 ' MOV DH Ib
                mRegisters.DH = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB7 ' MOV BH Ib
                mRegisters.BH = Param(ParamIndex.First,, DataSize.Byte) : clkCyc += 4
            Case &HB8 ' MOV AX Ib
                mRegisters.AX = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4
            Case &HB9 ' MOV CX Ib
                mRegisters.CX = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4
            Case &HBA ' MOV DX Ib
                mRegisters.DX = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4
            Case &HBB ' MOV BX Ib
                mRegisters.BX = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4
            Case &HBC ' MOV SP Ib
                mRegisters.SP = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4
            Case &HBD ' MOV BP Ib
                mRegisters.BP = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4
            Case &HBE ' MOV SI Ib
                mRegisters.SI = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4
            Case &HBF ' MOV DI Ib
                mRegisters.DI = Param(ParamIndex.First,, DataSize.Word) : clkCyc += 4

            Case &HC0, &HC1 ' GRP2 byte/word imm8/16 ??? (80186)
                If mV20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    ExecuteGroup2()
                Else
                    OpCodeNotImplemented()
                End If

            Case &HC2 ' RET Iw
                IPAddrOffet = PopFromStack()
                mRegisters.SP += Param(ParamIndex.First, , DataSize.Word)
                clkCyc += 20

            Case &HC3 ' RET
                IPAddrOffet = PopFromStack()
                clkCyc += 16

            Case &HC4 ' LES Gv Mp
                SetAddressing(DataSize.Word)
                addrMode.Decode(&HC5, RAM8(mRegisters.CS, mRegisters.IP + 1))
                mRegisters.Val(addrMode.Register1) = addrMode.IndMem
                mRegisters.ES = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                ignoreINTs = True
                clkCyc += 16

            Case &HC5 ' LDS Gv Mp
                SetAddressing(DataSize.Word)
                addrMode.Decode(&HC5, RAM8(mRegisters.CS, mRegisters.IP + 1))
                mRegisters.Val(addrMode.Register1) = addrMode.IndMem
                mRegisters.DS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                ignoreINTs = True
                clkCyc += 16

            Case &HC6 To &HC7 ' MOV Eb Ib | MOV Ev Iv
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Src) = Param(ParamIndex.First, opCodeSize)
                Else
                    RAMn = Param(ParamIndex.First, opCodeSize)
                End If
                clkCyc += 10

            Case &HC8 ' ENTER (80186)
                If mV20 Then
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

            Case &HC9 ' LEAVE (80186)
                If mV20 Then
                    mRegisters.SP = mRegisters.BP
                    mRegisters.BP = PopFromStack()
                    clkCyc += 8
                Else
                    OpCodeNotImplemented()
                End If

            Case &HCA ' RETF Iw
                tmpUVal1 = Param(ParamIndex.First, , DataSize.Word)
                IPAddrOffet = PopFromStack()
                mRegisters.CS = PopFromStack()
                mRegisters.SP += tmpUVal1
                clkCyc += 17

            Case &HCB ' RETF
                IPAddrOffet = PopFromStack()
                mRegisters.CS = PopFromStack()
                clkCyc += 18

            Case &HCC ' INT 3
                HandleInterrupt(3, False)
                clkCyc += 1

            Case &HCD ' INT Ib
                HandleInterrupt(Param(ParamIndex.First, , DataSize.Byte), False)
                clkCyc += 0

            Case &HCE ' INTO
                If mFlags.OF = 1 Then
                    HandleInterrupt(4, False)
                    clkCyc += 3
                Else
                    clkCyc += 4
                End If

            Case &HCF ' IRET
                IPAddrOffet = PopFromStack()
                mRegisters.CS = PopFromStack()
                mFlags.EFlags = PopFromStack()
                clkCyc += 32

            Case &HD0 To &HD3 : ExecuteGroup2()

            Case &HD4 ' AAM I0
                tmpUVal1 = Param(ParamIndex.First, , DataSize.Byte)
                If tmpUVal1 = 0 Then
                    HandleInterrupt(0, True)
                    Exit Select
                End If
                mRegisters.AH = mRegisters.AL \ tmpUVal1
                mRegisters.AL = mRegisters.AL Mod tmpUVal1
                SetSZPFlags(mRegisters.AX, DataSize.Word)
                clkCyc += 83

            Case &HD5 ' AAD I0
                tmpUVal1 = Param(ParamIndex.First, , DataSize.Byte)
                tmpUVal1 = tmpUVal1 * mRegisters.AH + mRegisters.AL
                mRegisters.AL = tmpUVal1
                mRegisters.AH = 0
                SetSZPFlags(tmpUVal1, DataSize.Word)
                mFlags.SF = 0
                clkCyc += 60

            Case &HD6 ' XLAT for V20 / SALC
                If mV20 Then
                    mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.BX + mRegisters.AL)
                Else
                    mRegisters.AL = If(mFlags.CF = 1, &HFF, &H0)
                    clkCyc += 4
                End If

            Case &HD7 ' XLATB
                mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.BX + mRegisters.AL)
                clkCyc += 11

            Case &HD8 To &HDF ' Ignore 8087 co-processor instructions
                SetAddressing()
                'FPU.Execute(opCode, addrMode)

                ' Lesson 2
                ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

                'HandleInterrupt(7, False)
                OpCodeNotImplemented("FPU Not Available")
                clkCyc += 2

            Case &HE0 ' LOOPNE/LOOPNZ
                mRegisters.CX -= 1
                If mRegisters.CX > 0 AndAlso mFlags.ZF = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 19
                Else
                    opCodeSize += 1
                    clkCyc += 5
                End If

            Case &HE1 ' LOOPE/LOOPZ
                mRegisters.CX -= 1
                If mRegisters.CX > 0 AndAlso mFlags.ZF = 1 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 18
                Else
                    opCodeSize += 1
                    clkCyc += 6
                End If

            Case &HE2 ' LOOP
                mRegisters.CX -= 1
                If mRegisters.CX > 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 17
                Else
                    opCodeSize += 1
                    clkCyc += 5
                End If

            Case &HE3 ' JCXZ/JECXZ
                If mRegisters.CX = 0 Then
                    IPAddrOffet = OffsetIP(DataSize.Byte)
                    clkCyc += 18
                Else
                    opCodeSize += 1
                    clkCyc += 6
                End If

            Case &HE4 ' IN AL Ib
                mRegisters.AL = ReceiveFromPort(Param(ParamIndex.First, , DataSize.Byte))
                clkCyc += 10

            Case &HE5 ' IN AX Ib
                tmpUVal1 = ReceiveFromPort(Param(ParamIndex.First, , DataSize.Byte))
                mRegisters.AL = tmpUVal1
                mRegisters.AH = tmpUVal1
                clkCyc += 10

            Case &HE6  ' OUT Ib AL
                SendToPort(Param(ParamIndex.First, , DataSize.Byte), mRegisters.AL)
                clkCyc += 10

            Case &HE7  ' OUT Ib AX
                tmpUVal1 = Param(ParamIndex.First, , DataSize.Byte)
                SendToPort(tmpUVal1, mRegisters.AL)
                SendToPort(tmpUVal1 + 1, mRegisters.AH)
                clkCyc += 10

            Case &HE8 ' CALL Jv
                IPAddrOffet = OffsetIP(DataSize.Word)
                PushIntoStack(Registers.IP + opCodeSize)
                clkCyc += 19

            Case &HE9 ' JMP Jv
                IPAddrOffet = OffsetIP(DataSize.Word)
                clkCyc += 15

            Case &HEA ' JMP Ap
                IPAddrOffet = Param(ParamIndex.First, , DataSize.Word)
                mRegisters.CS = Param(ParamIndex.Second, , DataSize.Word)
                clkCyc += 15

            Case &HEB ' JMP Jb
                IPAddrOffet = OffsetIP(DataSize.Byte)
                clkCyc += 15

            Case &HEC ' IN AL DX
                mRegisters.AL = ReceiveFromPort(mRegisters.DX)
                clkCyc += 8

            Case &HED ' IN AX DX
                tmpUVal1 = ReceiveFromPort(mRegisters.DX)
                mRegisters.AL = tmpUVal1
                mRegisters.AH = tmpUVal1
                clkCyc += 8

            Case &HEE ' OUT DX AL
                SendToPort(mRegisters.DX, mRegisters.AL)
                clkCyc += 8

            Case &HEF ' OUT DX AX
                SendToPort(mRegisters.DX, mRegisters.AL)
                SendToPort(mRegisters.DX + 1, mRegisters.AH)
                clkCyc += 8

            Case &HF0 ' LOCK
                OpCodeNotImplemented("LOCK")
                clkCyc += 2

            Case &HF2 ' REPBE/REPNZ
                mRepeLoopMode = REPLoopModes.REPENE
                newPrefix = True
                clkCyc += 2

            Case &HF3 ' REPE/REPZ
                mRepeLoopMode = REPLoopModes.REPE
                newPrefix = True
                clkCyc += 2

            Case &HF4 ' HLT
                If Not mIsHalted Then SystemHalted()
                mRegisters.IP -= 1
                clkCyc += 2

            Case &HF5 ' CMC
                mFlags.CF = If(mFlags.CF = 0, 1, 0)
                clkCyc += 2

            Case &HF6 To &HF7 : ExecuteGroup3()

            Case &HF8 ' CLC
                mFlags.CF = 0
                clkCyc += 2

            Case &HF9 ' STC
                mFlags.CF = 1
                clkCyc += 2

            Case &HFA ' CLI
                mFlags.IF = 0
                clkCyc += 2

            Case &HFB ' STI
                mFlags.IF = 1
                ignoreINTs = True ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
                clkCyc += 2

            Case &HFC ' CLD
                mFlags.DF = 0
                clkCyc += 2

            Case &HFD ' STD
                mFlags.DF = 1
                clkCyc += 2

            Case &HFE, &HFF : ExecuteGroup4_And_5()

            Case Else : OpCodeNotImplemented()
        End Select
    End Sub

    Public Function PostExecute() As Byte
        If useIPAddrOffset Then
            mRegisters.IP = IPAddrOffet
        Else
            mRegisters.IP += opCodeSize
        End If

        clkCyc += opCodeSize

        If Not newPrefix Then
            mRepeLoopMode = REPLoopModes.None
            mRegisters.ResetActiveSegment()
            newPrefixLast = 0
        Else
            newPrefixLast += 1
        End If

        Return opCodeSize
    End Function

    Private Sub ExecuteGroup1() ' &H80 To &H83
        SetAddressing()

        Dim arg1 As UInt16 = If(addrMode.IsDirect, mRegisters.Val(addrMode.Register2), addrMode.IndMem)
        Dim arg2 As UInt16 = Param(ParamIndex.First, opCodeSize, If(opCode = &H83, DataSize.Byte, addrMode.Size))
        If opCode = &H83 Then arg2 = To16bitsWithSign(arg2)

        Select Case addrMode.Reg
            Case 0 ' ADD Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.Add, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.Add, addrMode.Size)
                    clkCyc += 17
                End If

            Case 1 ' OR Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.LogicOr, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.LogicOr, addrMode.Size)
                    clkCyc += 17
                End If

            Case 2 ' ADC Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.AddWithCarry, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.AddWithCarry, addrMode.Size)
                    clkCyc += 17
                End If

            Case 3 ' SBB Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.SubstractWithCarry, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.SubstractWithCarry, addrMode.Size)
                    clkCyc += 17
                End If

            Case 4 ' AND Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.LogicAnd, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.LogicAnd, addrMode.Size)
                    clkCyc += 17
                End If

            Case 5 ' SUB Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.Substract, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.Substract, addrMode.Size)
                    clkCyc += 17
                End If

            Case 6 ' XOR Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(arg1, arg2, Operation.LogicXor, addrMode.Size)
                    clkCyc += 4
                Else
                    RAMn = Eval(arg1, arg2, Operation.LogicXor, addrMode.Size)
                    clkCyc += 17
                End If

            Case 7 ' CMP Eb Ib | Ev Iv | Ev Ib (opcode 83h only)
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
            mask100_10000 = &H1_0000UI
            maskFF00_FFFF0000 = &HFFFF_0000UI
        End If

        If addrMode.IsDirect Then
            oldValue = mRegisters.Val(addrMode.Register2)
            clkCyc += If(opCode >= &HD2, 8, 2)
        Else
            oldValue = addrMode.IndMem
            clkCyc += If(opCode >= &HD2, 20, 13)
        End If

        Select Case opCode
            Case &HD0, &HD1 : count = 1
            Case &HD2, &HD3 : count = mRegisters.CL
            Case &HC0, &HC1 : count = Param(ParamIndex.Second,  , DataSize.Byte)
        End Select

        ' 80186/V20 class CPUs limit shift count to 31 (&H1F)
        If mV20 Then count = count And &H1F
        If count = 0 Then Exit Sub
        clkCyc += 4 * count

        Select Case addrMode.Reg
            Case 0 ' ROL Gb CL/Ib | Gv CL/Ib
                If count = 1 Then
                    newValue = (oldValue << 1) Or (oldValue >> mask07_15)
                    mFlags.CF = If((oldValue And mask80_8000) = 0, 0, 1)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                Else
                    newValue = (oldValue << (count And mask07_15)) Or (oldValue >> (mask8_16 - (count And mask07_15)))
                    mFlags.CF = newValue And 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                End If

            Case 1 ' ROR Gb CL/Ib | Gv CL/Ib
                If count = 1 Then
                    newValue = (oldValue >> 1) Or (oldValue << mask07_15)
                    mFlags.CF = oldValue And 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                Else
                    newValue = (oldValue >> (count And mask07_15)) Or (oldValue << (mask8_16 - (count And mask07_15)))
                    mFlags.CF = If((newValue And mask80_8000) = 0, 0, 1)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                End If

            Case 2 ' RCL Gb CL/Ib | Gv CL/Ib
                If count = 1 Then
                    newValue = (oldValue << 1) Or mFlags.CF
                    mFlags.CF = If((oldValue And mask80_8000) = 0, 0, 1)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                Else
                    oldValue = oldValue Or (CUInt(mFlags.CF) << mask8_16)
                    newValue = (oldValue << (count Mod mask9_17)) Or (oldValue >> (mask9_17 - (count Mod mask9_17)))
                    mFlags.CF = If((newValue And mask100_10000) = 0, 0, 1)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                End If

            Case 3 ' RCR Gb CL/Ib | Gv CL/Ib
                If count = 1 Then
                    newValue = (oldValue >> 1) Or (CUInt(mFlags.CF) << mask07_15)
                    mFlags.CF = oldValue And 1
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                Else
                    oldValue = oldValue Or (CUInt(mFlags.CF) << mask8_16)
                    newValue = (oldValue >> (count Mod mask9_17)) Or (oldValue << (mask9_17 - (count Mod mask9_17)))
                    mFlags.CF = If((newValue And mask100_10000) = 0, 0, 1)
                    mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                End If

            Case 4, 6 ' SHL/SAL Gb CL/Ib | Gv CL/Ib
                If count = 1 Then
                    newValue = oldValue << 1
                    mFlags.CF = If((oldValue And mask80_8000) = 0, 0, 1)
                Else
                    newValue = If(count > mask8_16, 0, oldValue << count)
                    mFlags.CF = If((newValue And mask100_10000) = 0, 0, 1)
                End If
                mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                SetSZPFlags(newValue, addrMode.Size)

            Case 5 ' SHR Gb CL/Ib | Gv CL/Ib
                If count = 1 Then
                    newValue = oldValue >> 1
                    mFlags.CF = oldValue And 1
                Else
                    newValue = If(count > mask8_16, 0, oldValue >> (count - 1))
                    mFlags.CF = newValue And 1
                    newValue >>= 1
                End If
                mFlags.OF = If(((oldValue Xor newValue) And mask80_8000) = 0, 0, 1)
                SetSZPFlags(newValue, addrMode.Size)

            Case 7 ' SAR Gb CL/Ib | Gv CL/Ib
                If count = 1 Then
                    newValue = (oldValue >> 1) Or (oldValue And mask80_8000)
                    mFlags.CF = oldValue And 1
                Else
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
            Case 0, 1 ' TEST Eb Ib | Ev Iv
                If addrMode.IsDirect Then
                    Eval(mRegisters.Val(addrMode.Register2), Param(ParamIndex.First, opCodeSize), Operation.Test, addrMode.Size)
                    clkCyc += 5
                Else
                    Eval(addrMode.IndMem, Param(ParamIndex.First, opCodeSize), Operation.Test, addrMode.Size)
                    clkCyc += 11
                End If

            Case 2 ' NOT Eb | Ev
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Not mRegisters.Val(addrMode.Register2)
                    clkCyc += 3
                Else
                    RAMn = Not addrMode.IndMem
                    clkCyc += 16
                End If

            Case 3 ' NEG Eb | Ev
                If addrMode.IsDirect Then
                    Eval(0, mRegisters.Val(addrMode.Register2), Operation.Substract, addrMode.Size)
                    mRegisters.Val(addrMode.Register2) = (Not mRegisters.Val(addrMode.Register2)) + 1
                    clkCyc += 3
                Else
                    Eval(0, addrMode.IndMem, Operation.Substract, addrMode.Size)
                    RAMn = (Not addrMode.IndMem) + 1
                    clkCyc += 16
                End If

            Case 4 ' MUL
                If addrMode.IsDirect Then
                    If addrMode.Size = DataSize.Byte Then
                        tmpUVal1 = mRegisters.Val(addrMode.Register2) * mRegisters.AL
                        clkCyc += 70
                    Else
                        tmpUVal1 = CUInt(mRegisters.Val(addrMode.Register2)) * mRegisters.AX
                        mRegisters.DX = tmpUVal1 >> 16
                        clkCyc += 118
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        tmpUVal1 = addrMode.IndMem * mRegisters.AL
                        clkCyc += 76
                    Else
                        tmpUVal1 = CUInt(addrMode.IndMem) * mRegisters.AX
                        mRegisters.DX = tmpUVal1 >> 16
                        clkCyc += 134
                    End If
                End If
                mRegisters.AX = tmpUVal1

                SetSZPFlags(mRegisters.AX, addrMode.Size)
                If (tmpUVal1 And If(addrMode.Size = DataSize.Byte, &HFF00, &HFFFF_0000UI)) <> 0 Then
                    mFlags.CF = 1
                    mFlags.OF = 1
                Else
                    mFlags.CF = 0
                    mFlags.OF = 0
                End If
                mFlags.SF = 0
                mFlags.ZF = If(mV20, If(tmpUVal1 = 0, 0, 1), 0) ' This is the test the BIOS uses to detect a V20 (8018x)

            Case 5 ' IMUL
                Dim m1 As UInt32
                Dim m2 As UInt32

                If addrMode.IsDirect Then
                    If addrMode.Size = DataSize.Byte Then
                        m1 = To16bitsWithSign(mRegisters.AL)
                        m2 = To16bitsWithSign(mRegisters.Val(addrMode.Register2))

                        m1 = If((m1 And &H80) <> 0, m1 Or &HFFFF_FF00UI, m1)
                        m2 = If((m2 And &H80) <> 0, m2 Or &HFFFF_FF00UI, m2)

                        tmpUVal1 = m1 * m2
                        mRegisters.AX = tmpUVal1
                        clkCyc += 70
                    Else
                        m1 = To32bitsWithSign(mRegisters.AX)
                        m2 = To32bitsWithSign(mRegisters.Val(addrMode.Register2))

                        m1 = If((m1 And &H8000) <> 0, m1 Or &HFFFF_0000UI, m1)
                        m2 = If((m2 And &H8000) <> 0, m2 Or &HFFFF_0000UI, m2)

                        tmpUVal1 = m1 * m2
                        mRegisters.AX = tmpUVal1
                        mRegisters.DX = tmpUVal1 >> 16
                        clkCyc += 118
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        m1 = To16bitsWithSign(mRegisters.AL)
                        m2 = To16bitsWithSign(addrMode.IndMem)

                        m1 = If((m1 And &H80) <> 0, m1 Or &HFFFF_FF00UI, m1)
                        m2 = If((m2 And &H80) <> 0, m2 Or &HFFFF_FF00UI, m2)

                        tmpUVal1 = m1 * m2
                        mRegisters.AX = tmpUVal1
                        clkCyc += 76
                    Else
                        m1 = To32bitsWithSign(mRegisters.AX)
                        m2 = To32bitsWithSign(addrMode.IndMem)

                        m1 = If((m1 And &H8000) <> 0, m1 Or &HFFFF_0000UI, m1)
                        m2 = If((m2 And &H8000) <> 0, m2 Or &HFFFF_0000UI, m2)

                        tmpUVal1 = m1 * m2
                        mRegisters.AX = tmpUVal1
                        mRegisters.DX = tmpUVal1 >> 16
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
                If Not mV20 Then mFlags.ZF = 0

            Case 6 ' DIV
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

            Case 7 ' IDIV
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

                        signN = (num And &H8000_0000UI) <> 0
                        signD = (div And &H8000_0000UI) <> 0
                        num = If(signN, ((Not num) + 1) And &HFFFF_FFFF, num)
                        div = If(signD, ((Not div) + 1) And &HFFFF_FFFF, div)

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

                        signN = (num And &H8000_0000UI) <> 0
                        signD = (div And &H8000_0000UI) <> 0
                        num = If(signN, ((Not num) + 1) And &HFFFF_FFFF, num)
                        div = If(signD, ((Not div) + 1) And &HFFFF_FFFF, div)

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
            Case 0 ' INC Eb | Ev
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), 1, Operation.Increment, addrMode.Size)
                    clkCyc += 3
                Else
                    RAMn = Eval(addrMode.IndMem, 1, Operation.Increment, addrMode.Size)
                    clkCyc += 15
                End If

            Case 1 ' DEC Eb | Ev
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), 1, Operation.Decrement, addrMode.Size)
                    clkCyc += 3
                Else
                    RAMn = Eval(addrMode.IndMem, 1, Operation.Decrement, addrMode.Size)
                    clkCyc += 15
                End If

            Case 2 ' CALL Ev
                tmpUVal1 = mRegisters.Val(addrMode.Register2)
                PushIntoStack(mRegisters.IP + opCodeSize)
                IPAddrOffet = If(addrMode.IsDirect,
                                    tmpUVal1,
                                    addrMode.IndMem)
                clkCyc += 11

            Case 3 ' CALL Mp
                PushIntoStack(mRegisters.CS)
                PushIntoStack(mRegisters.IP + opCodeSize)
                IPAddrOffet = addrMode.IndMem
                mRegisters.CS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                clkCyc += 37

            Case 4 ' JMP Ev
                IPAddrOffet = If(addrMode.IsDirect,
                                    mRegisters.Val(addrMode.Register2),
                                    addrMode.IndMem)
                clkCyc += 15

            Case 5 ' JMP Mp
                IPAddrOffet = addrMode.IndMem
                mRegisters.CS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                clkCyc += 24

            Case 6, 7 ' PUSH Ev
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
        tmpUVal1 = mRegisters.ActiveSegmentValue
        tmpUVal2 = If((opCode And 1) <> 0, 2, 1) * If(mFlags.DF <> 0, -1, 1)

        If mRepeLoopMode = REPLoopModes.None Then
            ExecStringOpCode()
        ElseIf DebugMode AndAlso mRegisters.CX > 0 Then
            mRegisters.CX -= 1
            If ExecStringOpCode() Then
                If (mRepeLoopMode = REPLoopModes.REPE AndAlso mFlags.ZF = 0) OrElse
                   (mRepeLoopMode = REPLoopModes.REPENE AndAlso mFlags.ZF = 1) Then
                    Exit Sub
                End If
            End If

            If mRegisters.CX > 0 Then mRegisters.IP -= (opCodeSize + 1)
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
        instructionsCounter += 1

        Select Case opCode
            Case &HA4 ' MOVSB
                RAM8(mRegisters.ES, mRegisters.DI,, True) = RAM8(tmpUVal1, mRegisters.SI,, True)
                mRegisters.SI += tmpUVal2
                mRegisters.DI += tmpUVal2
                clkCyc += 18
                Return False

            Case &HA5 ' MOVSW
                RAM16(mRegisters.ES, mRegisters.DI,, True) = RAM16(tmpUVal1, mRegisters.SI,, True)
                mRegisters.SI += tmpUVal2
                mRegisters.DI += tmpUVal2
                clkCyc += 18
                Return False

            Case &HA6 ' CMPSB
                Eval(RAM8(tmpUVal1, mRegisters.SI,, True), RAM8(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Byte)
                mRegisters.SI += tmpUVal2
                mRegisters.DI += tmpUVal2
                clkCyc += 22
                Return True

            Case &HA7 ' CMPSW
                Eval(RAM16(tmpUVal1, mRegisters.SI,, True), RAM16(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Word)
                mRegisters.SI += tmpUVal2
                mRegisters.DI += tmpUVal2
                clkCyc += 22
                Return True

            Case &HAA ' STOSB
                RAM8(mRegisters.ES, mRegisters.DI,, True) = mRegisters.AL
                mRegisters.DI += tmpUVal2
                clkCyc += 11
                Return False

            Case &HAB ' STOSW
                RAM16(mRegisters.ES, mRegisters.DI,, True) = mRegisters.AX
                mRegisters.DI += tmpUVal2
                clkCyc += 11
                Return False

            Case &HAC ' LODSB
                mRegisters.AL = RAM8(tmpUVal1, mRegisters.SI,, True)
                mRegisters.SI += tmpUVal2
                clkCyc += 12
                Return False

            Case &HAD ' LODSW
                mRegisters.AX = RAM16(tmpUVal1, mRegisters.SI,, True)
                mRegisters.SI += tmpUVal2
                clkCyc += 16
                Return False

            Case &HAE ' SCASB
                Eval(mRegisters.AL, RAM8(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Byte)
                mRegisters.DI += tmpUVal2
                clkCyc += 15
                Return True

            Case &HAF ' SCASW
                Eval(mRegisters.AX, RAM16(mRegisters.ES, mRegisters.DI,, True), Operation.Compare, DataSize.Word)
                mRegisters.DI += tmpUVal2
                clkCyc += 15
                Return True

        End Select

        Return False
    End Function
End Class
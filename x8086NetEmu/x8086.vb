' Map Of Instructions: http://www.mlsite.net/8086/ and http://www.sandpile.org/x86/opc_1.htm
' http://en.wikibooks.org/wiki/X86_Assembly/Machine_Language_Conversion
' http://www.xs4all.nl/~ganswijk/chipdir/iset/8086bin.txt
' The Intel 8086 / 8088/ 80186 / 80286 / 80386 / 80486 Instruction Set: http://zsmith.co/intel.html

Imports System.Threading

Public Class x8086
    Public Enum Models
        IBMPC_5150
        IBMPC_5160
    End Enum

    Private mModel As Models = Models.IBMPC_5160
    Private mVic20 As Boolean

    Private mRegisters As GPRegisters = New GPRegisters()
    Private mFlags As GPFlags = New GPFlags()
    Private mVideoAdapter As CGAAdapter
    Private mKeyboard As KeyboardAdapter
    Private mMouse As MouseAdapter
    Private mFloppyController As FloppyControllerAdapter
    Private mAdapters As Adapters = New Adapters(Me)
    Private mPorts As IOPorts = New IOPorts(Me)
    Private mEnableExceptions As Boolean
    Private mDebugMode As Boolean
    Private mIsPaused As Boolean

    Private opCode As Byte
    Private opCodeSize As Byte = 0

    Private addrMode As AddressingMode
    Private mIsExecuting As Boolean = False

    Private mipsThread As Thread
    Private mipsWaiter As AutoResetEvent
    Private mMPIs As Double
    Private instrucionsCounter As UInteger

    Public Shared Property LogToConsole As Boolean

    Private mEmulateINT13 As Boolean = True

    Private Enum REPLoopModes
        None
        REPE
        REPENE
    End Enum
    Private repeLoopMode As REPLoopModes

    Private forceNewIPAddress As UInteger
    Private Property IPAddrOff As UInteger
        Get
            useIPAddrOff = False
            Return forceNewIPAddress
        End Get
        Set(value As UInteger)
            forceNewIPAddress = value
            useIPAddrOff = True
        End Set
    End Property
    Private useIPAddrOff As Boolean

    Private clkCyc As Long = 0

    Public Const KHz As Long = 1000
    Public Const MHz As Long = KHz * KHz
    Public Const GHz As Long = KHz * MHz
    Private Const BaseClock As Long = 4.7727 * MHz
    Private mCyclesPerSecond As Long = BaseClock

    Private mDoReSchedule As Boolean
    Private mSimulationMultiplier As Double = 1.0
    Private leftCycleFrags As Long

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

    Private picIsAvailable As Boolean

    Private FPU As x8087

    Public Event EmulationTerminated()
    Public Event EmulationHalted()
    Public Event InstructionDecoded()
    Public Event [Error](sender As Object, e As EmulatorErrorEventArgs)
    Public Shared Event Output(message As String, reason As NotificationReasons, arg() As Object)
    Public Event MIPsUpdated()

    Public Sub New(Optional v20 As Boolean = True, Optional int13 As Boolean = True)
        mVic20 = v20
        mEmulateINT13 = int13

        debugWaiter = New AutoResetEvent(False)
        addrMode = New AddressingMode()

        Sched = New Scheduler(Me)

        FPU = New x8087(Me)
        PIC = New PIC8259(Me)
        DMA = New DMAI8237(Me)
        PIT = New PIT8254(Me, PIC.GetIrqLine(0))
        PPI = New PPI8255(Me, PIC.GetIrqLine(1))
        'PPI = New PPI8255_ALT(Me, PIC.GetIrqLine(1))
        'RTC = New RTC(Me, PIC.GetIrqLine(8))

        mPorts.Add(PIC)
        mPorts.Add(DMA)
        mPorts.Add(PIT)
        mPorts.Add(PPI)
        'mPorts.Add(RTC)

        Init()
    End Sub

    Public Shared ReadOnly Property IsRunningOnMono As Boolean
        Get
            Return Type.GetType("Mono.Runtime") IsNot Nothing
        End Get
    End Property

    Public Shared Function FixPath(fileName As String) As String
#If Win32 Then
        Return fileName
#Else
        If Environment.OSVersion.Platform = PlatformID.Unix Then
            Return fileName.Replace("\", IO.Path.DirectorySeparatorChar)
        Else
            Return fileName
        End If
#End If
    End Function

    Public Sub Init()
        Sched.StopSimulation()

        InitSystem()
        LoadBIOS()
        FlushCycles()

        SetSynchronization()

        SetupSystem()

        mEnableExceptions = False
        mIsExecuting = False
        mIsPaused = False
        mIsHalted = False
        mDoReSchedule = False
    End Sub

    Private Sub InitSystem()
        For i As Integer = 0 To Memory.Length - 1
            Memory(i) = 0
        Next

        mIsExecuting = True

        StopAllThreads()

        mipsWaiter = New AutoResetEvent(False)
        mipsThread = New Thread(AddressOf MIPSCounterLoop)
        mipsThread.Start()

        mIsHalted = False
        mIsExecuting = False
        isDecoding = False
        'trapEnabled = False
        ignoreINTs = False
        repeLoopMode = REPLoopModes.None
        IPAddrOff = 0
        useIPAddrOff = False

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

        Registers.CS = &HFFFF
        Registers.IP = &H0

        mFlags.EFlags = 0
    End Sub

    Private Sub SetupSystem()
        If PPI Is Nothing Then Exit Sub

        ' http://docs.huihoo.com/help-pc/int-int_11.html
        PPI.SetSwitchData(Binary.From("0 0 0 0 0 0 0 0 0 1 1 0 0 0 0 1".Replace(" ", "")))
        '                             │F│E│D│C│B│A│9│8│7│6│5│4│3│2│1│0│  AX
        '                              │ │ │ │ │ │ │ │ │ │ │ │ │ │ │ └──── IPL diskette installed
        '                              │ │ │ │ │ │ │ │ │ │ │ │ │ │ └───── math coprocessor
        '                              │ │ │ │ │ │ │ │ │ │ │ │ ├─┴────── old PC system board RAM < 256K
        '                              │ │ │ │ │ │ │ │ │ │ │ │ │ └───── pointing device installed (PS/2)
        '                              │ │ │ │ │ │ │ │ │ │ │ │ └────── not used on PS/2
        '                              │ │ │ │ │ │ │ │ │ │ └─┴─────── initial video mode
        '                              │ │ │ │ │ │ │ │ └─┴────────── # of diskette drives, less 1
        '                              │ │ │ │ │ │ │ └───────────── 0 if DMA installed
        '                              │ │ │ │ └─┴─┴────────────── number of serial ports
        '                              │ │ │ └─────────────────── game adapter installed
        '                              │ │ └──────────────────── unused, internal modem (PS/2)
        '                              └─┴───────────────────── number of printer ports

        'PPI.PortA(0) = &H30 Or &HC
        'PPI.PortA(1) = &H0
        'PPI.PortB = &H8
        'PPI.PortC(0) = If(mModel = Models.IBMPC_5160, 1, 0)
        'PPI.PortC(1) = 0

        '' Floppy count
        'Dim count = 2 ' Forced, for now...
        'Select Case mModel
        '    Case Models.IBMPC_5150
        '        PPI.PortA(0) = PPI.PortA(0) And (Not &HC1)
        '        If count > 0 Then
        '            PPI.PortA(0) = PPI.PortA(0) Or &H1
        '            PPI.PortA(0) = PPI.PortA(0) Or (((count - 1) And &H3) << 6)
        '        End If
        '    Case Models.IBMPC_5160
        '        PPI.PortC(1) = PPI.PortC(1) And (Not &HC)
        '        If count > 0 Then
        '            PPI.PortC(1) = PPI.PortC(1) Or (((count - 1) And &H3) << 2)
        '        End If
        'End Select

        '' Video Mode
        'Dim videoMode As CGAAdapter.VideoModes = CGAAdapter.VideoModes.Mode4_Graphic_Color_320x200  ' Forced, for now...
        'Select Case mModel
        '    Case Models.IBMPC_5150
        '        PPI.PortA(0) = PPI.PortA(0) And (Not &H30)
        '        PPI.PortA(0) = PPI.PortA(0) Or ((videoMode And &H3) << 4)
        '    Case Models.IBMPC_5160
        '        PPI.PortC(1) = PPI.PortC(1) And (Not &H3)
        '        PPI.PortC(1) = PPI.PortC(1) Or (videoMode And &H3)
        'End Select

        '' RAM size
        'Dim size = Memory.Length ' Forced, for now...
        'Select Case mModel
        '    Case Models.IBMPC_5150
        '        size = If(size < 65536, 0, (size - 65536) / 32768)
        '        PPI.PortC(0) = PPI.PortC(0) And &HF0
        '        PPI.PortC(1) = PPI.PortC(1) And &HFE
        '        PPI.PortC(0) = PPI.PortC(0) Or (size And &HF)
        '        PPI.PortC(1) = PPI.PortC(1) Or ((size >> 4) And &H1)
        '    Case Models.IBMPC_5160
        '        size = size >> 16
        '        If size > 0 Then
        '            size -= 1
        '            If size > 3 Then size = 3
        '        End If
        '        PPI.PortC(0) = PPI.PortC(0) And &HF3
        '        PPI.PortC(0) = PPI.PortC(0) Or ((size << 2) And &HC)
        'End Select
    End Sub

    Private Sub LoadBIOS()
        ' BIOS
        LoadBIN("roms\PCXTBIOS.ROM", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios2\EPROMS\2764\XTBIOS.ROM", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios25\EPROMS\2764\PCXTBIOS.ROM", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\PCemV0.7\roms\genxt\pcxt.rom", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\fake86-0.12.9.19-win32\Binaries\pcxtbios.bin", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\award-2.05.rom", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\phoenix-2.51.rom", &HFE00, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\PCE - PC Emulator\bin\rom\ibm-pc-1982.rom", &HFE00, &H0)

        ' VGA
        'LoadBIN("..\..\Other Emulators & Resources\PCemV0.7\roms\TRIDENT.BIN", &HC000, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios2\TEST\ET4000.BIN", &HC000, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\fake86-0.12.9.19-win32\Binaries\videorom.bin", &HC000, &H0)

        ' BASIC C1.1
        LoadBIN("roms\BASICC11.BIN", &HF600, &H0)
        'LoadBIN("..\..\Other Emulators & Resources\xtbios2\TEST\BASICC11.BIN", &HF600, &H0)

        ' Lots of ROMs: http://www.hampa.ch/pce/download.html
    End Sub

    Public Sub Close()
        repeLoopMode = REPLoopModes.None
        StopAllThreads()

        If DebugMode Then debugWaiter.Set()
        Sched.StopSimulation()

        mipsWaiter.Set()

        For Each adapter As Adapter In Adapters
            adapter.CloseAdapter()
        Next
    End Sub

    Public Sub SoftReset()
        ' Just as Bill would've have wanted it... ;)
        PPI.PutKeyData(Keys.ControlKey, False)
        PPI.PutKeyData(Keys.Menu, False)
        PPI.PutKeyData(Keys.Delete, False)
    End Sub

    Public Sub HardReset()
        Init()
        Run(mDebugMode)
    End Sub

    Public Sub StepInto()
        debugWaiter.Set()
    End Sub

    Public Sub Run(Optional debugMode As Boolean = False)
        mDebugMode = debugMode
        cancelAllThreads = False
        picIsAvailable = (PIC IsNot Nothing)

#If Win32 Then
        If PIT IsNot Nothing Then PIT.Speaker.Enabled = False
        If mVideoAdapter IsNot Nothing Then mVideoAdapter.Reset()
#End If

        If mDebugMode Then RaiseEvent InstructionDecoded()
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

            mMPIs = (instrucionsCounter / delay) / 1000
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
            PIT.Speaker.Enabled = False
#End If
        End If
    End Sub

    Public Sub [Resume]()
        mDoReSchedule = False
        mIsPaused = False
    End Sub

    Private Sub FlushCycles()
        Dim t As Long = clkCyc * Scheduler.CLOCKRATE + leftCycleFrags
        Sched.AdvanceTime(t / mCyclesPerSecond)
        leftCycleFrags = t Mod mCyclesPerSecond
        clkCyc = 0

        mDoReSchedule = True
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
        Dim syncQuantum As Double = 0.05
        Sched.SetSynchronization(True, Scheduler.CLOCKRATE * syncQuantum, Scheduler.CLOCKRATE * mSimulationMultiplier / 1000)
    End Sub

    Public Sub PreExecute()
        If mIsExecuting OrElse isDecoding OrElse mIsPaused Then Exit Sub

        mDoReSchedule = False

        Dim maxRunTime As Long = Sched.GetTimeToNextEvent()
        If maxRunTime > Scheduler.CLOCKRATE Then maxRunTime = Scheduler.CLOCKRATE
        Dim maxRunCycl As Long = (maxRunTime * mCyclesPerSecond - leftCycleFrags + Scheduler.CLOCKRATE - 1) / Scheduler.CLOCKRATE

        If mDebugMode Then
            While (clkCyc < maxRunCycl AndAlso Not mDoReSchedule)
                debugWaiter.WaitOne()

                If isDecoding Then
                    Thread.Sleep(1)
                Else
                    Execute()
                    instrucionsCounter += 1
                End If

                RaiseEvent InstructionDecoded()
            End While
        Else
            While (clkCyc < maxRunCycl AndAlso Not mDoReSchedule AndAlso Not mDebugMode) OrElse repeLoopMode <> REPLoopModes.None
                Execute()
                instrucionsCounter += 1
            End While
        End If

        If clkCyc > 0 Then FlushCycles()
    End Sub

    Public Sub Execute()
        mIsExecuting = True

        If mFlags.TF = 1 Then
            ' The addition of the "If ignoreINTs Then" not only fixes the dreaded "Interrupt Check" in CheckIt,
            ' but it even allows it to pass it successfully!!!
            If ignoreINTs Then HandleInterrupt(1, False)
        ElseIf ignoreINTs Then
            ignoreINTs = False
        Else
            HandlePendingInterrupt()
        End If

        'Prefetch()
        'opCode = Prefetch.Buffer(0)
        opCode = RAM8(mRegisters.CS, mRegisters.IP)
        opCodeSize = 1

        ' Hack from fake86 to force BIOS into detecting a EGA/VGA adapter
        ' Memory(&H410) = &H41

        Select Case opCode
            Case &H0 To &H3 ' add reg<->reg / reg<->mem
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

            Case &H4 ' add al, imm
                mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Add, DataSize.Byte)
                clkCyc += 4

            Case &H5 ' add ax, imm
                mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Add, DataSize.Word)
                clkCyc += 4

            Case &H6 ' push es
                PushIntoStack(mRegisters.ES)
                clkCyc += 10

            Case &H7 ' pop es
                mRegisters.ES = PopFromStack()
                clkCyc += 8

            Case &H8 To &HB ' or
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

            Case &HC ' or al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.LogicOr, DataSize.Byte)
                clkCyc += 4

            Case &HD ' or ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.LogicOr, DataSize.Word)
                clkCyc += 4

            Case &HE ' push cs
                PushIntoStack(mRegisters.CS)
                clkCyc += 10

            Case &HF ' pop cs
                If Not mVic20 Then
                    mRegisters.CS = PopFromStack()
                    clkCyc += 8
                End If

            Case &H10 To &H13 ' adc
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

            Case &H14 ' adc al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.AddWithCarry, DataSize.Byte)
                clkCyc += 3

            Case &H15 ' adc ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.AddWithCarry, DataSize.Word)
                clkCyc += 3

            Case &H16 ' push ss
                PushIntoStack(mRegisters.SS)
                clkCyc += 10

            Case &H17 ' pop ss
                mRegisters.SS = PopFromStack()
                ' Lesson 4: http://ntsecurity.nu/onmymind/2007/2007-08-22.html
                ' http://zet.aluzina.org/forums/viewtopic.php?f=6&t=287
                ignoreINTs = True
                clkCyc += 8

            Case &H18 To &H1B ' sbb
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

            Case &H1C ' sbb al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.SubstractWithCarry, DataSize.Byte)
                clkCyc += 4

            Case &H1D ' sbb ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.SubstractWithCarry, DataSize.Word)
                clkCyc += 4

            Case &H1E ' push ds
                PushIntoStack(mRegisters.DS)
                clkCyc += 10

            Case &H1F ' pop ds
                mRegisters.DS = PopFromStack()
                clkCyc += 8

            Case &H20 To &H23 ' and reg/mem and reg to either
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

            Case &H24 ' and al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.LogicAnd, DataSize.Byte)
                clkCyc += 4

            Case &H25 ' and ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.LogicAnd, DataSize.Word)
                clkCyc += 4

            Case &H26, &H2E, &H36, &H3E ' ES, CS, SS and DS segment override prefix
                addrMode.Decode(opCode, opCode)
                mRegisters.ActiveSegmentRegister = (addrMode.Register1 - GPRegisters.RegistersTypes.AH) + GPRegisters.RegistersTypes.ES

            Case &H27 ' daa
                If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
                    mRegisters.AL = AddValues(mRegisters.AL, 6, DataSize.Byte)
                    mFlags.AF = 1
                    mFlags.CF = mFlags.CF Or If((mRegisters.AL And &HFF00) <> 0, 1, 0)
                Else
                    mFlags.AF = 0
                End If
                If (mRegisters.AL And &HF0) > &H90 OrElse mFlags.CF = 1 Then
                    mRegisters.AL = AddValues(mRegisters.AL, &H60, DataSize.Byte)
                    mFlags.CF = 1
                Else
                    mFlags.CF = 0
                End If
                SetSZPFlags(mRegisters.AL, DataSize.Byte)
                clkCyc += 4

            Case &H28 To &H2B ' sub reg/mem with reg to either
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

            Case &H2C ' sub al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Substract, DataSize.Byte)
                clkCyc += 4

            Case &H2D ' sub ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Substract, DataSize.Word)
                clkCyc += 4

            Case &H2F ' das
                If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
                    mRegisters.AL = AddValues(mRegisters.AL, -6, DataSize.Byte)
                    mFlags.AF = 1
                    mFlags.CF = mFlags.CF Or If((mRegisters.AL And &HFF00) <> 0, 1, 0)
                Else
                    mFlags.AF = 0
                End If
                If (mRegisters.AL And &HF0) > &H90 OrElse mFlags.CF = 1 Then
                    mRegisters.AL = AddValues(mRegisters.AL, -&H60, DataSize.Byte)
                    mFlags.CF = 1
                Else
                    mFlags.CF = 0
                End If
                SetSZPFlags(mRegisters.AL, DataSize.Byte)
                clkCyc += 4

            Case &H30 To &H33 ' xor reg/mem and reg to either
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

            Case &H34 ' xor al and imm
                mRegisters.AL = Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.LogicXor, DataSize.Byte)
                clkCyc += 4

            Case &H35 ' xor ax and imm
                mRegisters.AX = Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.LogicXor, DataSize.Word)
                clkCyc += 4

            Case &H37 ' aaa
                Dim c As Integer = (mRegisters.AL > &HF9) And &HFF
                If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
                    mRegisters.AL = AddValues(mRegisters.AL, 6, DataSize.Byte) And &HF
                    mRegisters.AH = AddValues(mRegisters.AH, 1 + c, DataSize.Byte)
                    mFlags.AF = 1
                    mFlags.CF = 1
                Else
                    mFlags.AF = 0
                    mFlags.CF = 0
                    mRegisters.AL = mRegisters.AL And &HF
                End If
                clkCyc += 8

            Case &H38 To &H3B ' cmp reg/mem and reg
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

            Case &H3C ' cmp al and imm
                Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Compare, DataSize.Byte)
                clkCyc += 4

            Case &H3D ' cmp ax and imm
                Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Compare, DataSize.Word)
                clkCyc += 4

            Case &H3F ' aas
                Dim b As Integer = (mRegisters.AL < 6) And &HFF
                If (mRegisters.AL And &HF) > 9 OrElse mFlags.AF = 1 Then
                    mRegisters.AL = AddValues(mRegisters.AL, -6, DataSize.Byte) And &HF
                    mRegisters.AH = AddValues(mRegisters.AH, -1 - b, DataSize.Byte)
                    mFlags.AF = 1
                    mFlags.CF = 1
                Else
                    mFlags.AF = 0
                    mFlags.CF = 0
                    mRegisters.AL = mRegisters.AL And &HF
                End If
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
                    ' The 8086/8088 pushes the value of SP after it is incremented
                    ' http://css.csail.mit.edu/6.858/2013/readings/i386/s15_06.htm
                    PushIntoStack(AddValues(mRegisters.SP, -2, DataSize.Word))
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
                    Dim sp = mRegisters.SP
                    PushIntoStack(mRegisters.AX)
                    PushIntoStack(mRegisters.CX)
                    PushIntoStack(mRegisters.DX)
                    PushIntoStack(mRegisters.BX)
                    PushIntoStack(sp)
                    PushIntoStack(mRegisters.BP)
                    PushIntoStack(mRegisters.SI)
                    PushIntoStack(mRegisters.DI)
                    clkCyc += 19
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
                End If

            ' Case &H66 ' hook

            Case &H68 ' push (80186)
                ' PRE ALPHA CODE - UNTESTED
                If mVic20 Then
                    PushIntoStack(Param(SelPrmIndex.First, , DataSize.Word))
                    clkCyc += 3
                End If

            Case &H69 ' imul (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    SetAddressing()
                    Dim tmp1 As Long = mRegisters.Val(addrMode.Register1)
                    Dim tmp2 As Long = Param(SelPrmIndex.First, , DataSize.Word)
                    If (tmp1 And &H8000) = &H8000 Then tmp1 = tmp1 Or &HFFFF0000L
                    If (tmp2 And &H8000) = &H8000 Then tmp1 = tmp1 Or &HFFFF0000L
                    Dim tmp3 As Long = tmp1 * tmp2
                    mRegisters.Val(addrMode.Register1) = tmp3 And &HFFFFL
                    If (tmp3 And &HFFFF0000L) <> 0 Then
                        mFlags.CF = 1
                        mFlags.OF = 1
                    Else
                        mFlags.CF = 0
                        mFlags.OF = 0
                    End If
                    clkCyc += 27
                End If

            Case &H6A ' push (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    PushIntoStack(Param(SelPrmIndex.First, , DataSize.Byte))
                    clkCyc += 3
                End If

            Case &H6B ' imul (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    SetAddressing()
                    Dim tmp1 As Long = mRegisters.Val(addrMode.Register1)
                    Dim tmp2 As Long = To16bitsWithSign(Param(SelPrmIndex.First, , DataSize.Byte))
                    If (tmp1 And &H8000) = &H8000 Then tmp1 = tmp1 Or &HFFFF0000L
                    If (tmp2 And &H8000) = &H8000 Then tmp2 = tmp2 Or &HFFFF0000L
                    Dim tmp3 As Long = tmp1 * tmp2
                    mRegisters.Val(addrMode.Register1) = tmp3 And &HFFFFL
                    If (tmp3 And &HFFFF0000L) <> 0 Then
                        mFlags.CF = 1
                        mFlags.OF = 1
                    Else
                        mFlags.CF = 0
                        mFlags.OF = 0
                    End If
                    clkCyc += 27
                End If

            'Case &H6C
            '    If Not (repeLoopMode <> REPLoopModes.None AndAlso mRegisters.CX = 0) Then

            '    End If

            Case &H6C To &H6F ' Ignore 80186/V20 port operations... for now...
                opCodeSize += 1
                clkCyc += 3

            Case &H70 ' jo
                If mFlags.OF = 1 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H71 ' jno
                If mFlags.OF = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H72 ' jb/jnae
                If mFlags.CF = 1 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H73 ' jnb/jae
                If mFlags.CF = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H74 ' je/jz
                If mFlags.ZF = 1 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H75 ' jne/jnz
                If mFlags.ZF = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H76 ' jbe/jna
                If mFlags.CF = 1 OrElse mFlags.ZF = 1 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If


            Case &H77 ' jnbe/ja
                If mFlags.CF = 0 AndAlso mFlags.ZF = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H78 ' js
                If mFlags.SF = 1 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H79 ' jns
                If mFlags.SF = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7A ' jp/jpe
                If mFlags.PF = 1 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7B ' jnp/jpo
                If mFlags.PF = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7C ' jl/jnge
                If mFlags.SF <> mFlags.OF Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7D ' jnl/jge
                If mFlags.SF = mFlags.OF Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7E ' jle/jng
                If mFlags.ZF = 1 OrElse (mFlags.SF <> mFlags.OF) Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H7F ' jnle/jg
                If mFlags.ZF = 0 AndAlso (mFlags.SF = mFlags.OF) Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 16
                Else
                    opCodeSize += 1
                    clkCyc += 4
                End If

            Case &H80 To &H83 : ExecuteGroup1()

            Case &H84 To &H85 ' test reg with reg/mem
                SetAddressing()
                If addrMode.IsDirect Then
                    Eval(mRegisters.Val(addrMode.Register1), mRegisters.Val(addrMode.Register2), Operation.Test, addrMode.Size)
                    clkCyc += 3
                Else
                    Eval(addrMode.IndMem, mRegisters.Val(addrMode.Register2), Operation.Test, addrMode.Size)
                    clkCyc += 9
                End If

            Case &H86 To &H87 ' xchg reg/mem with reg
                SetAddressing()
                If addrMode.IsDirect Then
                    Dim tmp As UInteger = mRegisters.Val(addrMode.Register1)
                    mRegisters.Val(addrMode.Register1) = mRegisters.Val(addrMode.Register2)
                    mRegisters.Val(addrMode.Register2) = tmp
                    clkCyc += 4
                Else
                    RAMn = mRegisters.Val(addrMode.Register1)
                    mRegisters.Val(addrMode.Register1) = addrMode.IndMem
                    clkCyc += 17
                End If

            Case &H88 To &H8C ' mov ind <-> reg8/reg16
                SetAddressing()

                If opCode = &H8C Then
                    If (addrMode.Register1 And &H4) = &H4 Then
                        addrMode.Register1 = addrMode.Register1 And (Not shl2)
                    Else
                        addrMode.Register1 += GPRegisters.RegistersTypes.ES
                        If addrMode.Register2 > &H3 Then
                            addrMode.Register2 = (addrMode.Register2 + GPRegisters.RegistersTypes.ES) Or shl3
                        Else
                            addrMode.Register2 += GPRegisters.RegistersTypes.AX
                        End If
                    End If
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

            Case &H8D ' lea
                SetAddressing()
                'If addrMode.IsDirect Then
                'OpCodeNotImplemented(opCode)
                'If mVic20 Then HandleInterrupt(6, False)
                'Else
                mRegisters.Val(addrMode.Register1) = addrMode.IndAdr
                'End If
                clkCyc += 2

            Case &H8E  ' mov reg/mem to seg reg
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
                ignoreINTs = ignoreINTs Or (addrMode.Register2 = GPRegisters.RegistersTypes.CS)

            Case &H8F ' pop reg/mem
                SetAddressing()
                RAMn = PopFromStack()
                clkCyc += 17

            Case &H90 ' nop
                clkCyc += 3

            Case &H90 To &H97 ' xchg reg with acc
                SetRegister1Alt(opCode)
                Dim tmp As UInteger = mRegisters.AX
                mRegisters.AX = mRegisters.Val(addrMode.Register1)
                mRegisters.Val(addrMode.Register1) = tmp
                clkCyc += 3

            Case &H98 ' cbw
                mRegisters.AX = To16bitsWithSign(mRegisters.AL)
                clkCyc += 2

            Case &H99 ' cwd
                mRegisters.DX = If((mRegisters.AH And &H80) = 0, &H0, &HFFFF)
                clkCyc += 5

            Case &H9A ' call direct intersegment
                IPAddrOff = Param(SelPrmIndex.First, , DataSize.Word)
                Dim cs As UInteger = Param(SelPrmIndex.Second, , DataSize.Word)

                PushIntoStack(mRegisters.CS)
                PushIntoStack(mRegisters.IP + opCodeSize)

                mRegisters.CS = cs

                clkCyc += 28

            Case &H9B ' wait
                clkCyc += 4

            Case &H9C ' pushf
                PushIntoStack((mFlags.EFlags And &HFD5) Or &HF002)
                clkCyc += 10

            Case &H9D ' popf
                mFlags.EFlags = (PopFromStack() And &HFD5) Or &HF002
                clkCyc += 8

            Case &H9E ' sahf
                mFlags.EFlags = (mFlags.EFlags And &HFF00) Or (mRegisters.AH And &HD5) Or 2
                clkCyc += 4

            Case &H9F ' lahf
                mRegisters.AH = (mFlags.EFlags And &HD5) Or 2
                clkCyc += 4

            Case &HA0 To &HA3 ' mov mem to acc | mov acc to mem
                addrMode.Decode(opCode, opCode)
                addrMode.IndAdr = Param(SelPrmIndex.First, , DataSize.Word)
                addrMode.Register1 = If(addrMode.Size = DataSize.Byte, GPRegisters.RegistersTypes.AL, GPRegisters.RegistersTypes.AX)
                If addrMode.Direction = 0 Then
                    mRegisters.Val(addrMode.Register1) = RAMn
                Else
                    RAMn = mRegisters.Val(addrMode.Register1)
                End If
                clkCyc += 10

            Case &HA4 To &HA7, &HAA To &HAF : HandleREPMode()

            Case &HA8 ' test al imm8
                Eval(mRegisters.AL, Param(SelPrmIndex.First, , DataSize.Byte), Operation.Test, DataSize.Byte)
                clkCyc += 4

            Case &HA9 ' test ax imm16
                Eval(mRegisters.AX, Param(SelPrmIndex.First, , DataSize.Word), Operation.Test, DataSize.Word)
                clkCyc += 4

            Case &HB0 To &HBF ' mov imm to reg
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

            Case &HC0, &HC1 ' GRP2 byte/word imm8/16 ??? (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    ExecuteGroup2()
                End If

            Case &HC2 ' ret (ret n) within segment adding imm to sp
                IPAddrOff = PopFromStack()
                mRegisters.SP = AddValues(mRegisters.SP, Param(SelPrmIndex.First, , DataSize.Word), DataSize.Word)
                clkCyc += 20

            Case &HC3 ' ret within segment
                IPAddrOff = PopFromStack()
                clkCyc += 16

            Case &HC4 To &HC5 ' les | lds
                SetAddressing(DataSize.Word)
                If (addrMode.Register1 And shl2) = shl2 Then
                    addrMode.Register1 = (addrMode.Register1 + GPRegisters.RegistersTypes.ES) Or shl3
                Else
                    addrMode.Register1 = (addrMode.Register1 Or shl3)
                End If
                mRegisters.Val(addrMode.Register1) = addrMode.IndMem
                mRegisters.Val(If(opCode = &HC4, GPRegisters.RegistersTypes.ES, GPRegisters.RegistersTypes.DS)) = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                clkCyc += 16

            Case &HC6 To &HC7 ' mov imm to reg/mem
                SetAddressing()
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register1) = Param(SelPrmIndex.First, opCodeSize)
                    clkCyc += 4
                Else
                    RAMn = Param(SelPrmIndex.First, opCodeSize)
                    clkCyc += 10
                End If

            Case &HC8 ' enter (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    Dim stackSize = Param(SelPrmIndex.First, , DataSize.Word)
                    Dim nestLevel = Param(SelPrmIndex.Second, , DataSize.Byte) And &H1F
                    PushIntoStack(mRegisters.BP)
                    Dim frameTemp = mRegisters.SP
                    If nestLevel > 0 Then
                        For i As Integer = 1 To nestLevel - 1
                            mRegisters.BP = AddValues(mRegisters.BP, -2, DataSize.Word)
                            PushIntoStack(RAM16(frameTemp, mRegisters.BP))
                        Next
                        PushIntoStack(frameTemp)
                    End If
                    mRegisters.BP = frameTemp
                    mRegisters.SP = AddValues(frameTemp, -stackSize, DataSize.Word)

                    Select Case nestLevel
                        Case 0 : clkCyc += 15
                        Case 1 : clkCyc += 25
                        Case Else : clkCyc += 22 + 16 * (nestLevel - 1)
                    End Select
                End If

            Case &HC9 ' leave (80186)
                If mVic20 Then
                    ' PRE ALPHA CODE - UNTESTED
                    mRegisters.SP = mRegisters.BP
                    mRegisters.BP = PopFromStack()
                    clkCyc += 8
                End If

            Case &HCA ' ret intersegment adding imm to sp (retf)
                Dim n As UInteger = Param(SelPrmIndex.First, , DataSize.Word)
                IPAddrOff = PopFromStack()
                mRegisters.CS = PopFromStack()
                mRegisters.SP = AddValues(mRegisters.SP, n, DataSize.Word)
                clkCyc += 17

            Case &HCB ' ret intersegment (retf)
                IPAddrOff = PopFromStack()
                mRegisters.CS = PopFromStack()
                clkCyc += 18

            Case &HCC ' int with type 3
                HandleInterrupt(3, False)
                clkCyc += 52

            Case &HCD ' int with type specified
                HandleInterrupt(Param(SelPrmIndex.First, , DataSize.Byte), False)
                clkCyc += 51

            Case &HCE ' into
                If mFlags.OF = 1 Then
                    HandleInterrupt(4, False)
                    clkCyc += 3
                Else
                    clkCyc += 4
                End If

            Case &HCF ' iret
                IPAddrOff = PopFromStack()
                mRegisters.CS = PopFromStack()
                mFlags.EFlags = PopFromStack()
                clkCyc += 32

            Case &HD0 To &HD3 : ExecuteGroup2()

            Case &HD4 ' aam
                Dim div As UInteger = Param(SelPrmIndex.First, , DataSize.Byte)
                If div = 0 Then
                    HandleInterrupt(0, False)
                    Exit Select
                End If
                mRegisters.AH = mRegisters.AL \ div
                mRegisters.AL = mRegisters.AL Mod div
                SetSZPFlags(mRegisters.AX, DataSize.Word)
                clkCyc += 83

            Case &HD5 ' aad
                mRegisters.AL = AddValues(mRegisters.AL, mRegisters.AH * Param(SelPrmIndex.First, , DataSize.Byte), DataSize.Byte)
                mRegisters.AH = 0
                SetSZPFlags(mRegisters.AX, DataSize.Word)
                mFlags.SF = 0
                clkCyc += 60

            Case &HD6 ' xlat 
                If Not mVic20 Then
                    mRegisters.AL = If(mFlags.CF = 1, &HFF, &H0)
                    clkCyc += 4
                End If

            Case &HD7 ' xlatb
                mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, AddValues(mRegisters.BX, mRegisters.AL, DataSize.Word))
                clkCyc += 11

            Case &HD8 To &HDF ' Ignore coprocessor instructions
                SetAddressing()

                'FPU.Execute(opCode)

                ' Lesson 2
                ' http://ntsecurity.nu/onmymind/2007/2007-08-22.html

                'HandleInterrupt(7, False)

                'OpCodeNotImplemented(opCode, "FPU Not Available")
                clkCyc += 2

            Case &HE0 ' loopne/loopnz
                mRegisters.CX = AddValues(mRegisters.CX, -1, DataSize.Word)
                If mRegisters.CX <> 0 AndAlso mFlags.ZF = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 19
                Else
                    opCodeSize += 1
                    clkCyc += 5
                End If

            Case &HE1 ' loope/loopz
                mRegisters.CX = AddValues(mRegisters.CX, -1, DataSize.Word)
                If mRegisters.CX <> 0 AndAlso mFlags.ZF = 1 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 18
                Else
                    opCodeSize += 1
                    clkCyc += 6
                End If

            Case &HE2 ' loop
                mRegisters.CX = AddValues(mRegisters.CX, -1, DataSize.Word)
                If mRegisters.CX <> 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 17
                Else
                    opCodeSize += 1
                    clkCyc += 5
                End If

            Case &HE3 ' jcxz
                If mRegisters.CX = 0 Then
                    IPAddrOff = OffsetIP(DataSize.Byte)
                    clkCyc += 18
                Else
                    opCodeSize += 1
                    clkCyc += 6
                End If

            Case &HE4 ' in to al from fixed port
                mRegisters.AL = ReceiveFromPort(Param(SelPrmIndex.First, , DataSize.Byte))
                clkCyc += 10

            Case &HE5 ' inw to ax from fixed port
                mRegisters.AX = ReceiveFromPort(Param(SelPrmIndex.First, , DataSize.Byte))
                clkCyc += 10

            Case &HE6  ' out to al to fixed port
                FlushCycles()
                SendToPort(Param(SelPrmIndex.First, , DataSize.Byte), mRegisters.AL)
                clkCyc += 10

            Case &HE7  ' outw to ax to fixed port
                FlushCycles()
                SendToPort(Param(SelPrmIndex.First, , DataSize.Byte), mRegisters.AX)
                clkCyc += 10

            Case &HE8 ' call direct within segment
                IPAddrOff = OffsetIP(DataSize.Word)
                PushIntoStack(AddValues(Registers.IP, opCodeSize, DataSize.Word))
                clkCyc += 19

            Case &HE9 ' jmp direct within segment
                IPAddrOff = OffsetIP(DataSize.Word)
                clkCyc += 15

            Case &HEA ' jmp direct intersegment
                IPAddrOff = Param(SelPrmIndex.First, , DataSize.Word)
                mRegisters.CS = Param(SelPrmIndex.Second, , DataSize.Word)
                clkCyc += 15

            Case &HEB ' jmp direct within segment short
                IPAddrOff = OffsetIP(DataSize.Byte)
                clkCyc += 15

            Case &HEC  ' in to al from variable port
                mRegisters.AL = ReceiveFromPort(mRegisters.DX)
                clkCyc += 8

            Case &HED ' inw to ax from variable port
                mRegisters.AX = ReceiveFromPort(mRegisters.DX)
                clkCyc += 8

            Case &HEE ' out to port dx from al
                SendToPort(mRegisters.DX, mRegisters.AL)
                clkCyc += 8

            Case &HEF ' out to port dx from ax
                SendToPort(mRegisters.DX, mRegisters.AX)
                clkCyc += 8

            Case &HF0 ' lock
                OpCodeNotImplemented(opCode, "LOCK")
                clkCyc += 2

            Case &HF2 ' repne/repnz
                repeLoopMode = REPLoopModes.REPENE
                clkCyc += 2

            Case &HF3 ' repe/repz
                repeLoopMode = REPLoopModes.REPE
                clkCyc += 2

            Case &HF4 ' hlt
                clkCyc += 2
                'mIsHalted = True
                If Not mIsHalted Then SystemHalted()
                IncIP(-1)

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
                OpCodeNotImplemented(opCode)
                If mVic20 Then HandleInterrupt(6, False) ' 80186
        End Select

        If useIPAddrOff Then
            mRegisters.IP = IPAddrOff
        Else
            IncIP(opCodeSize)
        End If
        clkCyc += opCodeSize * 4

        If mRegisters.ActiveSegmentChanged AndAlso repeLoopMode = REPLoopModes.None Then
            Select Case opCode
                Case &H26, &H2E, &H36, &H3E ' Keep Active Segment / Do Not Reset
                Case Else
                    mRegisters.ResetActiveSegment()
                    clkCyc += 2
            End Select
        End If

        mIsExecuting = False
    End Sub

    Private Sub ExecuteGroup1() ' &H80 To &H83
        SetAddressing()

        Dim arg1 As UInteger = If(addrMode.IsDirect, mRegisters.Val(addrMode.Register2), addrMode.IndMem)               ' reg
        Dim arg2 As UInteger = Param(SelPrmIndex.First, opCodeSize, If(opCode = &H83, DataSize.Byte, addrMode.Size))    ' imm
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
                clkCyc += If(addrMode.IsDirect, 4, 20)

        End Select
    End Sub

    Private Sub ExecuteGroup2_SLOW() ' &HD0 To &HD3 (fake86 version)
        SetAddressing()

        Dim value As UInteger
        Dim count As UInteger

        Dim mask80_8000 As UInteger
        Dim mask07_15 As UInteger
        Dim maskFF_FFFF As UInteger

        If addrMode.Size = DataSize.Byte Then
            mask80_8000 = &H80
            mask07_15 = &H7
            maskFF_FFFF = &HFF
        Else
            mask80_8000 = &H8000
            mask07_15 = &HF
            maskFF_FFFF = &HFFFF
        End If

        If addrMode.IsDirect Then
            value = mRegisters.Val(addrMode.Register2)
            If opCode >= &HD2 Then
                clkCyc += 8
            Else
                clkCyc += 2
            End If
        Else
            value = addrMode.IndMem
            If opCode >= &HD2 Then
                clkCyc += 20
            Else
                clkCyc += 13
            End If
        End If

        If opCode >= &HD2 Then
            count = mRegisters.CL
            If count = 0 Then
                clkCyc += 8
                Exit Sub
            Else
                clkCyc += 4 * count
            End If
        Else
            count = 1
            clkCyc += 2
        End If

        ' 80186/V20 class CPUs limit shift count to 31 (fake86)
        If mVic20 Then count = count And &H1F

        Dim shift As UInteger
        Dim oldCF As UInteger
        Dim msb As UInteger

        Select Case addrMode.Reg
            Case 0 ' 000    --  rol
                For shift = 1 To count
                    If (value And mask80_8000) = mask80_8000 Then
                        mFlags.CF = 1
                    Else
                        mFlags.CF = 0
                    End If

                    value = value << 1
                    value = value Or mFlags.CF
                Next
                If count = 1 Then mFlags.OF = mFlags.CF Xor ((value >> mask07_15) And 1)

            Case 1 ' 001    --  ror
                For shift = 1 To count
                    mFlags.CF = value And 1
                    value = (value >> 1) Or (mFlags.CF << mask07_15)
                Next
                If count = 1 Then mFlags.OF = (value >> mask07_15) Xor ((value >> (mask07_15 - 1)) And 1)

            Case 2 ' 010    --  rcl
                For shift = 1 To count
                    oldCF = mFlags.CF
                    If (value And mask80_8000) = mask80_8000 Then
                        mFlags.CF = 1
                    Else
                        mFlags.CF = 0
                    End If

                    value = value << 1
                    value = value Or oldCF
                Next
                If count = 1 Then mFlags.OF = mFlags.CF Xor ((value >> mask07_15) And 1)

            Case 3 ' 011    --  rcr
                For shift = 1 To count
                    oldCF = mFlags.CF
                    mFlags.CF = value And 1
                    value = (value >> 1) Or (oldCF << mask07_15)
                Next
                If count = 1 Then mFlags.OF = (value >> mask07_15) Xor ((value >> (mask07_15 - 1)) And 1)

            Case 4, 6 ' 100/110    --  shl/sal
                For shift = 1 To count
                    If (value And mask80_8000) = mask80_8000 Then
                        mFlags.CF = 1
                    Else
                        mFlags.CF = 0
                    End If

                    value = (value << 1) And maskFF_FFFF
                Next
                If (count = 1) AndAlso (mFlags.CF = (value >> mask07_15)) Then
                    mFlags.OF = 0
                Else
                    mFlags.OF = 1
                End If
                SetSZPFlags(value, addrMode.Size)

            Case 5 ' 101    --  shr
                If (count = 1) AndAlso ((value And mask80_8000) = mask80_8000) Then
                    mFlags.OF = 1
                Else
                    mFlags.OF = 0
                End If
                For shift = 1 To count
                    mFlags.CF = value And 1
                    value = value >> 1
                Next
                SetSZPFlags(value, addrMode.Size)

            Case 7 ' 111    --  sar
                For shift = 1 To count
                    msb = value And mask80_8000
                    mFlags.CF = value And 1
                    value = (value >> 1) Or msb
                Next
                mFlags.OF = 0
                SetSZPFlags(value, addrMode.Size)

            Case Else
                OpCodeNotImplemented(opCode, "Unknown Reg Mode for Opcode")
        End Select

        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Register2) = value And maskFF_FFFF
        Else
            RAMn = value And maskFF_FFFF
        End If
    End Sub

    Private Sub ExecuteGroup2() ' &HD0 To &HD3 / &HC0 To &HC1
        SetAddressing()

        ' Other Emulators & Resources\PCE - PC Emulator\src\src\cpu\e8086\opcodes.c

        Dim newValue As UInteger
        Dim count As UInteger
        Dim oldValue As UInteger

        Dim mask80_8000 As UInteger
        Dim mask07_15 As UInteger
        Dim maskFF_FFFF As UInteger
        Dim mask8_16 As UInteger
        Dim mask9_17 As UInteger
        Dim mask100_10000 As UInteger
        Dim maskFF00_FFFF0000 As UInteger

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
            maskFF00_FFFF0000 = &HFFFF0000L
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
            Case &HD0, &HD1
                count = 1
            Case &HD2, &HD3
                count = mRegisters.CL
            Case &HC0, &HC1
                count = Param(SelPrmIndex.First,  , DataSize.Byte)
        End Select

        If count = 0 Then
            clkCyc += 8
            Exit Sub
        Else
            ' 80186/V20 class CPUs limit shift count to 31 (fake86)
            If mVic20 Then count = count And &H1F

            clkCyc += 4 * count
        End If

        Select Case addrMode.Reg
            Case 0 ' 000    --  rol
                newValue = (oldValue << (count And mask07_15)) Or (oldValue >> (mask8_16 - (count And mask07_15)))
                mFlags.CF = newValue And &H1

            Case 1 ' 001    --  ror
                newValue = (oldValue >> (count And mask07_15)) Or (oldValue << (mask8_16 - (count And mask07_15)))
                mFlags.CF = If((newValue And mask80_8000) = mask80_8000, 1, 0)

            Case 2 ' 010    --  rcl
                oldValue = oldValue Or (mFlags.CF << mask8_16)
                newValue = (oldValue << (count Mod mask9_17)) Or (oldValue >> (mask9_17 - (count Mod mask9_17)))
                mFlags.CF = If((newValue And mask100_10000) = mask100_10000, 1, 0)

            Case 3 ' 011    --  rcr
                oldValue = oldValue Or (mFlags.CF << mask8_16)
                newValue = (oldValue >> (count Mod mask9_17)) Or (oldValue << (mask9_17 - (count Mod mask9_17)))
                mFlags.CF = If((newValue And mask100_10000) = mask100_10000, 1, 0)

            Case 4, 6 ' 100/110    --  shl/sal
                newValue = If(count > mask8_16, 0, (oldValue << count))
                mFlags.CF = If((newValue And mask100_10000) = mask100_10000, 1, 0)
                SetSZPFlags(newValue, addrMode.Size)

            Case 5 ' 101    --  shr
                newValue = If(count > mask8_16, 0, (oldValue >> (count - 1)))
                mFlags.CF = newValue And &H1
                newValue = (newValue >> 1)
                SetSZPFlags(newValue, addrMode.Size)

            Case 7 ' 111    --  sar
                oldValue = oldValue Or If((oldValue And mask80_8000) = mask80_8000, maskFF00_FFFF0000, 0)
                newValue = oldValue >> If(count >= mask8_16, mask07_15, count - 1)
                mFlags.CF = newValue And &H1
                newValue = (newValue >> 1) And maskFF_FFFF
                SetSZPFlags(newValue, addrMode.Size)

            Case Else
                OpCodeNotImplemented(opCode, "Unknown Reg Mode for Opcode")
        End Select

        If addrMode.IsDirect Then
            mRegisters.Val(addrMode.Register2) = newValue And maskFF_FFFF
        Else
            RAMn = newValue And maskFF_FFFF
        End If

        'If addrMode.Reg = 7 Then ' sar
        'mFlags.OF = 0
        'Else
        mFlags.OF = If(((newValue Xor oldValue) And mask80_8000) <> 0, 1, 0)
        'End If
    End Sub

    Private Sub ExecuteGroup3() ' &HF6 To &HF7
        SetAddressing()

        Select Case addrMode.Reg
            Case 0, 1 ' 000    --  test
                If addrMode.IsDirect Then
                    Eval(mRegisters.Val(addrMode.Register2), Param(SelPrmIndex.First, opCodeSize), Operation.Test, addrMode.Size)
                    clkCyc += 5
                Else
                    Eval(addrMode.IndMem, Param(SelPrmIndex.First, opCodeSize), Operation.Test, addrMode.Size)
                    clkCyc += 11
                End If

            Case 2 ' 010    --  not
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = AddValues(Not mRegisters.Val(addrMode.Register2), 0, addrMode.Size)
                    clkCyc += 3
                Else
                    RAMn = AddValues(Not addrMode.IndMem, 0, addrMode.Size)
                    clkCyc += 16
                End If

            Case 3 ' 011    --  neg
                Dim result As UInteger
                If addrMode.IsDirect Then
                    result = AddValues(Not mRegisters.Val(addrMode.Register2), 1, addrMode.Size)
                    Eval(0, mRegisters.Val(addrMode.Register2), Operation.Substract, addrMode.Size)
                    mRegisters.Val(addrMode.Register2) = result
                    clkCyc += 3
                Else
                    result = AddValues(Not addrMode.IndMem, 1, addrMode.Size)
                    Eval(0, addrMode.IndMem, Operation.Substract, addrMode.Size)
                    RAMn = result
                    clkCyc += 16
                End If

            Case 4 ' 100    --  mul
                Dim result As UInteger

                If addrMode.IsDirect Then
                    If addrMode.Size = DataSize.Byte Then
                        result = mRegisters.Val(addrMode.Register2) * mRegisters.AL
                        mRegisters.AX = result And &HFFFF
                        clkCyc += 70
                    Else
                        result = mRegisters.Val(addrMode.Register2) * mRegisters.AX
                        mRegisters.AX = result And &HFFFF
                        mRegisters.DX = (result >> 16) And &HFFFF
                        clkCyc += 118
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        result = addrMode.IndMem * mRegisters.AL
                        mRegisters.AX = result And &HFFFF
                        clkCyc += 76
                    Else
                        result = addrMode.IndMem * mRegisters.AX
                        mRegisters.AX = result And &HFFFF
                        mRegisters.DX = (result >> 16) And &HFFFF
                        clkCyc += 134
                    End If
                End If

                ' This prevents an overflow error in C:\GAMES\SW.EXE
                'SetSZPFlags(result And If(addrMode.Size = DataSize.Byte, &HFF, &HFFFF), addrMode.Size)
                ' Apparently, this is no longer required(?)

                If (result And If(addrMode.Size = DataSize.Byte, &HFF00, &HFFFF0000L)) <> 0 Then
                    mFlags.CF = 1
                    mFlags.OF = 1
                Else
                    mFlags.CF = 0
                    mFlags.OF = 0
                End If
                If Not mVic20 Then mFlags.ZF = If(result = 0, 1, 0) ' This is the test the BIOS uses to detect a VIC20 (80186)

            Case 5 ' 101    --  imul
                Dim result As UInteger

                If addrMode.IsDirect Then
                    If addrMode.Size = DataSize.Byte Then
                        Dim m1 As Long = To16bitsWithSign(mRegisters.AL)
                        Dim m2 As Long = To16bitsWithSign(mRegisters.Val(addrMode.Register2))

                        result = m1 * m2
                        mRegisters.AX = result And &HFFFF
                        clkCyc += 70
                    Else
                        Dim m1 As Long = mRegisters.AX
                        If (m1 And &H8000) = &H8000 Then m1 = m1 Or &HFFFF0000L
                        Dim m2 As Long = mRegisters.Val(addrMode.Register2)
                        If (m2 And &H8000) = &H8000 Then m2 = m2 Or &HFFFF0000L

                        result = m1 * m2
                        mRegisters.AX = result And &HFFFF
                        mRegisters.DX = (result >> 16) And &HFFFF
                        clkCyc += 118
                    End If
                Else
                    If addrMode.Size = DataSize.Byte Then
                        Dim m1 As Long = To16bitsWithSign(mRegisters.AL)
                        Dim m2 As Long = To16bitsWithSign(addrMode.IndMem)

                        result = m1 * m2
                        mRegisters.AX = result And &HFFFF
                        clkCyc += 76
                    Else
                        Dim m1 As Long = mRegisters.AX
                        If (m1 And &H8000) = &H8000 Then m1 = m1 Or &HFFFF0000L
                        Dim m2 As Long = addrMode.IndMem
                        If (m2 And &H8000) = &H8000 Then m2 = m2 Or &HFFFF0000L

                        result = m1 * m2
                        mRegisters.AX = result And &HFFFF
                        mRegisters.DX = (result >> 16) And &HFFFF
                        clkCyc += 134
                    End If
                End If

                Dim mask As Long = If(addrMode.Size = DataSize.Byte, &HFF00, &HFFFF0000L)
                result = result And mask
                If result <> 0 AndAlso result <> mask Then
                    mFlags.CF = 1
                    mFlags.OF = 1
                Else
                    mFlags.CF = 0
                    mFlags.OF = 0
                End If

            Case 6 ' 110    --  div
                Dim div As UInteger
                Dim num As UInteger
                Dim result As UInteger
                Dim remain As UInteger

                If addrMode.IsDirect Then
                    div = mRegisters.Val(addrMode.Register2)
                Else
                    div = addrMode.IndMem
                End If

                If addrMode.Size = DataSize.Byte Then
                    num = mRegisters.AX
                    clkCyc += 86
                Else
                    num = (mRegisters.DX << 16) Or mRegisters.AX
                    clkCyc += 150
                End If

                If div = 0 Then
                    HandleInterrupt(0, False)
                    Exit Select
                End If

                result = num \ div
                remain = num Mod div

                If addrMode.Size = DataSize.Byte Then
                    If (result And &HFF00) <> 0 Then
                        HandleInterrupt(0, False)
                        Exit Select
                    End If
                    mRegisters.AL = result And &HFF
                    mRegisters.AH = remain And &HFF
                Else
                    If (result And &HFFFF0000L) <> 0 Then
                        HandleInterrupt(0, False)
                        Exit Select
                    End If
                    mRegisters.AX = result And &HFFFF
                    mRegisters.DX = remain And &HFFFF
                End If

            Case 7 ' 111    --  idiv
                Dim div As UInteger
                Dim num As UInteger
                Dim result As UInteger
                Dim remain As UInteger
                Dim sign1 As Boolean
                Dim sign2 As Boolean

                If addrMode.IsDirect Then
                    div = mRegisters.Val(addrMode.Register2)
                    If addrMode.Size = DataSize.Byte Then
                        num = mRegisters.AX
                        div = To16bitsWithSign(div)

                        sign1 = (num And &H8000) <> 0
                        sign2 = (div And &H8000) <> 0
                        num = If(sign1, ((Not num) + 1) And &HFFFF, num)
                        div = If(sign2, ((Not div) + 1) And &HFFFF, div)

                        clkCyc += 80
                    Else
                        num = (mRegisters.DX << 16) Or mRegisters.AX
                        div = If(div And &H8000, div Or &HFFFF0000L, div)

                        sign1 = (num And &H80000000L) <> 0
                        sign2 = (div And &H80000000L) <> 0
                        num = If(sign1, ((Not num) + 1) And &HFFFFFFFFL, num)
                        div = If(sign2, ((Not div) + 1) And &HFFFFFFFFL, div)

                        clkCyc += 144
                    End If
                Else
                    div = addrMode.IndMem
                    If addrMode.Size = DataSize.Byte Then
                        num = mRegisters.AX
                        div = To16bitsWithSign(div)

                        sign1 = (num And &H8000) <> 0
                        sign2 = (div And &H8000) <> 0
                        num = If(sign1, ((Not num) + 1) And &HFFFF, num)
                        div = If(sign2, ((Not div) + 1) And &HFFFF, div)

                        clkCyc += 86
                    Else
                        num = (mRegisters.DX << 16) Or mRegisters.AX
                        div = If(div And &H8000, div Or &HFFFF0000L, div)

                        sign1 = (num And &H80000000L) <> 0
                        sign2 = (div And &H80000000L) <> 0
                        num = If(sign1, ((Not num) + 1) And &HFFFFFFFFL, num)
                        div = If(sign2, ((Not div) + 1) And &HFFFFFFFFL, div)

                        clkCyc += 150
                    End If
                End If

                If div = 0 Then
                    HandleInterrupt(0, False)
                    Exit Select
                End If

                result = num \ div
                remain = num Mod div

                If sign1 <> sign2 Then
                    If result > If(addrMode.Size = DataSize.Byte, &H80, &H8000) Then
                        HandleInterrupt(0, False)
                        Exit Select
                    End If
                    result = ((Not result) + 1) And If(addrMode.Size = DataSize.Byte, &HFF, &HFFFF)
                ElseIf result > If(addrMode.Size = DataSize.Byte, &H7F, &H7FFF) Then
                    HandleInterrupt(0, False)
                    Exit Select
                End If

                If sign1 Then remain = ((Not remain) + 1) And If(addrMode.Size = DataSize.Byte, &HFF, &HFFFF)

                If addrMode.Size = DataSize.Byte Then
                    mRegisters.AL = result And &HFF
                    mRegisters.AH = remain And &HFF
                Else
                    mRegisters.AX = result And &HFFFF
                    mRegisters.DX = remain And &HFFFF
                End If

            Case Else
                OpCodeNotImplemented(opCode, "Unknown Reg Mode for Opcode")
        End Select
    End Sub

    Private Sub ExecuteGroup4_And_5() ' &HFE, &hFF
        SetAddressing()

        Select Case addrMode.Reg
            Case 0 ' 000    --  inc reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), 1, Operation.Increment, addrMode.Size)
                    clkCyc += 3
                Else
                    RAMn = Eval(addrMode.IndMem, 1, Operation.Increment, addrMode.Size)
                    clkCyc += 15
                End If

            Case 1 ' 001    --  dec reg/mem
                If addrMode.IsDirect Then
                    mRegisters.Val(addrMode.Register2) = Eval(mRegisters.Val(addrMode.Register2), 1, Operation.Decrement, addrMode.Size)
                    clkCyc += 3
                Else
                    RAMn = Eval(addrMode.IndMem, 1, Operation.Decrement, addrMode.Size)
                    clkCyc += 15
                End If

            Case 2 ' 010    --  call indirect within segment
                PushIntoStack(mRegisters.IP + opCodeSize)
                If addrMode.IsDirect Then
                    IPAddrOff = mRegisters.Val(addrMode.Register2)
                Else
                    IPAddrOff = addrMode.IndMem
                End If
                clkCyc += 11

            Case 3 ' 011    --  call indirect intersegment
                PushIntoStack(mRegisters.CS)
                PushIntoStack(mRegisters.IP + opCodeSize)
                IPAddrOff = addrMode.IndMem
                mRegisters.CS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                clkCyc += 37

            Case 4 ' 100    --  jmp indirect within segment
                If addrMode.IsDirect Then
                    IPAddrOff = mRegisters.Val(addrMode.Register2)
                Else
                    IPAddrOff = addrMode.IndMem
                End If
                clkCyc += 15

            Case 5 ' 101    --  jmp indirect intersegment
                IPAddrOff = addrMode.IndMem
                mRegisters.CS = RAM16(mRegisters.ActiveSegmentValue, addrMode.IndAdr, 2)
                clkCyc += 24

            Case 6 ' 110    --  push reg/mem
                If addrMode.IsDirect Then
                    If addrMode.Register2 = GPRegisters.RegistersTypes.SP Then
                        PushIntoStack(AddValues(mRegisters.SP, -2, DataSize.Word))
                    Else
                        PushIntoStack(mRegisters.Val(addrMode.Register2))
                    End If
                Else
                    PushIntoStack(addrMode.IndMem)
                End If
                clkCyc += 16

            Case Else
                OpCodeNotImplemented(opCode, "Unknown Reg Mode for Opcode")
        End Select
    End Sub

    Private Sub HandleREPMode()
        If repeLoopMode = REPLoopModes.None Then
            ExecStringOpCode()
        Else
            If mRegisters.CX = 0 Then
                repeLoopMode = REPLoopModes.None
            Else
                While mRegisters.CX > 0
                    mRegisters.CX = AddValues(mRegisters.CX, -1, DataSize.Word)
                    If ExecStringOpCode() Then
                        If (repeLoopMode = REPLoopModes.REPE AndAlso mFlags.ZF = 0) OrElse
                           (repeLoopMode = REPLoopModes.REPENE AndAlso mFlags.ZF = 1) Then
                            repeLoopMode = REPLoopModes.None
                            Exit Sub
                        End If
                    End If
                    If mDebugMode Then Exit While
                End While
                IncIP(-opCodeSize)
            End If
        End If
    End Sub

    Private Function ExecStringOpCode() As Boolean
        Select Case opCode
            Case &HA4  ' movsb
                RAM8(mRegisters.ES, mRegisters.DI) = RAM8(mRegisters.ActiveSegmentValue, mRegisters.SI)
                If mFlags.DF = 0 Then
                    mRegisters.SI = AddValues(mRegisters.SI, 1, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, 1, DataSize.Word)
                Else
                    mRegisters.SI = AddValues(mRegisters.SI, -1, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, -1, DataSize.Word)
                End If
                clkCyc += 18
                Return False

            Case &HA5 ' movsw
                RAM16(mRegisters.ES, mRegisters.DI) = RAM16(mRegisters.ActiveSegmentValue, mRegisters.SI)
                If mFlags.DF = 0 Then
                    mRegisters.SI = AddValues(mRegisters.SI, 2, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, 2, DataSize.Word)
                Else
                    mRegisters.SI = AddValues(mRegisters.SI, -2, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, -2, DataSize.Word)
                End If
                clkCyc += 18
                Return False

            Case &HA6  ' cmpsb
                Eval(RAM8(mRegisters.ActiveSegmentValue, mRegisters.SI), RAM8(mRegisters.ES, mRegisters.DI), Operation.Compare, DataSize.Byte)
                If mFlags.DF = 0 Then
                    mRegisters.SI = AddValues(mRegisters.SI, 1, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, 1, DataSize.Word)
                Else
                    mRegisters.SI = AddValues(mRegisters.SI, -1, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, -1, DataSize.Word)
                End If
                clkCyc += 22
                Return True

            Case &HA7 ' cmpsw
                Eval(RAM16(mRegisters.ActiveSegmentValue, mRegisters.SI), RAM16(mRegisters.ES, mRegisters.DI), Operation.Compare, DataSize.Word)
                If mFlags.DF = 0 Then
                    mRegisters.SI = AddValues(mRegisters.SI, 2, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, 2, DataSize.Word)
                Else
                    mRegisters.SI = AddValues(mRegisters.SI, -2, DataSize.Word)
                    mRegisters.DI = AddValues(mRegisters.DI, -2, DataSize.Word)
                End If
                clkCyc += 22
                Return True

            Case &HAA ' stosb
                RAM8(mRegisters.ES, mRegisters.DI) = mRegisters.AL
                If mFlags.DF = 0 Then
                    mRegisters.DI = AddValues(mRegisters.DI, 1, DataSize.Word)
                Else
                    mRegisters.DI = AddValues(mRegisters.DI, -1, DataSize.Word)
                End If
                clkCyc += 11
                Return False

            Case &HAB 'stosw
                RAM16(mRegisters.ES, mRegisters.DI) = mRegisters.AX
                If mFlags.DF = 0 Then
                    mRegisters.DI = AddValues(mRegisters.DI, 2, DataSize.Word)
                Else
                    mRegisters.DI = AddValues(mRegisters.DI, -2, DataSize.Word)
                End If
                clkCyc += 11
                Return False

            Case &HAC ' lodsb
                mRegisters.AL = RAM8(mRegisters.ActiveSegmentValue, mRegisters.SI)
                If mFlags.DF = 0 Then
                    mRegisters.SI = AddValues(mRegisters.SI, 1, DataSize.Word)
                Else
                    mRegisters.SI = AddValues(mRegisters.SI, -1, DataSize.Word)
                End If
                clkCyc += 12
                Return False

            Case &HAD ' lodsw
                mRegisters.AX = RAM16(mRegisters.ActiveSegmentValue, mRegisters.SI)
                If mFlags.DF = 0 Then
                    mRegisters.SI = AddValues(mRegisters.SI, 2, DataSize.Word)
                Else
                    mRegisters.SI = AddValues(mRegisters.SI, -2, DataSize.Word)
                End If
                clkCyc += 16
                Return False

            Case &HAE ' scasb
                Eval(RAM8(mRegisters.ES, mRegisters.DI), mRegisters.AL, Operation.Compare, DataSize.Byte)
                If mFlags.DF = 0 Then
                    mRegisters.DI = AddValues(mRegisters.DI, 1, DataSize.Word)
                Else
                    mRegisters.DI = AddValues(mRegisters.DI, -1, DataSize.Word)
                End If
                clkCyc += 15
                Return True

            Case &HAF ' scasw
                Eval(RAM16(mRegisters.ES, mRegisters.DI), mRegisters.AX, Operation.Compare, DataSize.Word)
                If mFlags.DF = 0 Then
                    mRegisters.DI = AddValues(mRegisters.DI, 2, DataSize.Word)
                Else
                    mRegisters.DI = AddValues(mRegisters.DI, -2, DataSize.Word)
                End If
                clkCyc += 15
                Return True
        End Select

        Return False
    End Function
End Class
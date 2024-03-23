Imports System.Threading
Imports System.Runtime.InteropServices

Public Class FloppyControllerAdapter
    Inherits Adapter
    Implements IDMADevice

    ' Command execution time (50 us, arbitrary value)
    Private Const COMMANDDELAY = 50000

    ' Time to transfer one byte of a sector in non-DMA mode (27 us, from 8272 spec)
    Private Const BYTEDELAY = 27000

    ' Time to transfer one sector in DMA mode (5 ms, arbitrary value)
    Private Const SECTORDELAY = 5000000

    ' Status register 0
    Private regSt0 As Byte

    ' Digital output register
    Private regDOR As Byte

    ' Configuration 
    Private ctlStepRateTime As Byte
    Private ctlHeadUnloadTime As Byte
    Private ctlHeadLoadTime As Byte
    Private ctlNonDma As Boolean

    ' Current cylinder for each drive
    Private curCylinder() As Byte

    ' Bit mask of drives with a seek operation in progress
    Private driveSeeking As Byte

    ' Bit mask of drives with a pending ready notification
    Private pendingReadyChange As Byte

    ' Input buffer for command bytes
    Private commandbuf() As Byte
    Private commandlen As Integer
    Private commandptr As Integer
    Private cmdCmd As Commands
    Private cmdDrive, cmdHead, cmdCylinder, cmdRecord, cmdEot As Byte
    Private cmdMultitrack As Boolean

    Private Enum States
        IDLE = 0
        COMMAND = 1
        EXECUTE = 2
        TRANSFER_IN = 3
        TRANSFER_OUT = 4
        TRANSWAIT_IN = 5
        TRANSWAIT_OUT = 6
        TRANSFER_EOP = 7
        RESULT = 8
    End Enum
    Private state As States

    ' Output buffer for result bytes
    Private resultbuf() As Byte
    Private resultptr As Integer

    ' Data buffer
    Private databuf() As Byte
    Private dataptr As Integer

    ' Floppy disk drives
    Private diskimg() As DiskImage

    ' Simulation scheduler
    Private sched As Scheduler

    ' Interrupt request signal
    Private irq As InterruptRequest

    ' DMA request signal
    Private dma As IDMAChannel

    Private Enum Commands
        READ = &H6
        READ_DEL = &HC
        WRITE = &H5
        WRITE_DEL = &H9
        READ_TRACK = &H2
        READ_ID = &HA
        FORMAT = &HD
        SCAN_EQ = &H11
        SCAN_LE = &H19
        SCAN_GE = &H1D
        CALIBRATE = &H7
        SENSE_INT = &H8
        SPECIFY = &H3
        SENSE_DRIVE = &H4
        SEEK = &HF
    End Enum

    Private Class TaskSC
        Inherits Scheduler.Task

        Public Sub New(owner As IOPortHandler)
            MyBase.New(owner)
        End Sub

        Public Overrides Sub Run()
            Owner.Run()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return Owner.Name
            End Get
        End Property
    End Class
    Private task As Scheduler.Task = New TaskSC(Me)

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)

        Me.sched = cpu.Sched
        If cpu.PIC IsNot Nothing Then Me.irq = cpu.PIC.GetIrqLine(6)
        If cpu.DMA IsNot Nothing Then
            Me.dma = cpu.DMA.GetChannel(2)
            cpu.DMA.BindChannel(2, Me)
        End If

        ReDim curCylinder(4 - 1)
        ReDim diskimg(512 - 1)
        regDOR = &HC
        ctlStepRateTime = 0
        ctlHeadUnloadTime = 0
        ctlHeadLoadTime = 0
        ctlNonDma = False

        For i As UInt32 = &H3F0 To &H3F7
            RegisteredPorts.Add(i)
        Next
    End Sub

    Public Overrides Sub InitiAdapter()
        Reset()
    End Sub

    Public Property DiskImage(driveNumber As Integer) As DiskImage
        Get
            If driveNumber >= diskimg.Length Then
                Return Nothing
            Else
                Return diskimg(driveNumber)
            End If
        End Get
        Set(value As DiskImage)
            If diskimg(driveNumber) IsNot Nothing Then diskimg(driveNumber).Close()

            diskimg(driveNumber) = value
        End Set
    End Property

    ' Resets the controller
    Public Sub Reset()
        driveSeeking = 0
        pendingReadyChange = 0
        regSt0 = 0
        ReDim commandbuf(9 - 1)
        commandptr = 0
        resultbuf = Nothing
        databuf = Nothing
        If irq IsNot Nothing Then irq.Raise(False)
        If dma IsNot Nothing Then dma.DMARequest(False)
        state = States.IDLE
        task?.Cancel()
    End Sub

    ' Prepare to transfer next byte(s)
    Public Sub KickTransfer()
        If ctlNonDma Then
            ' prepare to transfer next byte in non-DMA mode
            sched.RunTaskAfter(task, BYTEDELAY)
            If irq IsNot Nothing Then irq.Raise(True)
        Else
            ' prepare to transfer multiple bytes in DMA mode
            sched.RunTaskAfter(task, SECTORDELAY)
            If dma IsNot Nothing Then dma.DMARequest(True)
        End If
    End Sub

    ' Determines the length of a command from the first command byte
    Private Function CommandLength() As Integer
        Select Case cmdCmd
            Case Commands.READ, Commands.READ_DEL, Commands.WRITE, Commands.WRITE_DEL
                Return 9
            Case Commands.READ_TRACK
                Return 9
            Case Commands.READ_ID
                Return 2
            Case Commands.FORMAT
                Return 6
            Case Commands.SCAN_EQ, Commands.SCAN_LE, Commands.SCAN_GE
                Return 9
            Case Commands.CALIBRATE
                Return 2
            Case Commands.SENSE_INT
                Return 1
            Case Commands.SPECIFY
                Return 3
            Case Commands.SENSE_DRIVE
                Return 2
            Case Commands.SEEK
                Return 3
            Case Else
                Return 1
        End Select
    End Function

    Private Sub CommandStart()
        ' Decode command parameters
        cmdMultitrack = (commandbuf(0) And &H80) = &H80
        cmdDrive = CByte(commandbuf(1) And 3)
        cmdCylinder = commandbuf(2)
        cmdRecord = commandbuf(4)
        cmdEot = commandbuf(6)
        Select Case cmdCmd
            Case Commands.READ,
                 Commands.READ_DEL,
                 Commands.WRITE,
                 Commands.WRITE_DEL,
                 Commands.READ_TRACK,
                 Commands.SCAN_EQ,
                 Commands.SCAN_LE,
                 Commands.SCAN_GE
                cmdHead = commandbuf(3)
            Case Else
                cmdHead = (commandbuf(1) >> 2) And 1
        End Select

        ' Start execution
        Select Case cmdCmd
            Case Commands.READ, Commands.READ_DEL '  READ: go to EXECUTE state
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Commands.WRITE, Commands.WRITE_DEL ' WRITE: go to EXECUTE state
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Commands.READ_TRACK  ' READ TRACK: go to EXECUTE state
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Commands.READ_ID ' READ ID: go to execute state
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Commands.FORMAT ' FORMAT: go to EXECUTE state
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Commands.SCAN_EQ, Commands.SCAN_LE, Commands.SCAN_GE ' SCAN: go to EXECUTE state
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Commands.CALIBRATE ' CALIBRATE: go to EXECUTE state
                cmdCylinder = 0
                driveSeeking = driveSeeking Or (1 << cmdDrive)
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Commands.SENSE_INT   ' SENSE INTERRUPT: respond immediately
                If irq IsNot Nothing Then irq.Raise(False)

                ' Respond to a completed seek command.
                For i As Integer = 0 To 4 - 1
                    If (driveSeeking And (1 << i)) <> 0 Then
                        driveSeeking = driveSeeking And CByte((Not 1 << i) And &HFF)
                        pendingReadyChange = pendingReadyChange And CByte((Not 1 << i) And &HFF)
                        CommandEndSense(&H20 Or i, curCylinder(i))
                        Exit Sub
                    End If
                Next

                ' Respond to a disk-ready change.
                For i As Integer = 0 To 4 - 1
                    If (pendingReadyChange And (1 << i)) <> 0 Then
                        pendingReadyChange = pendingReadyChange And CByte((Not 1 << i) And &HFF)
                        CommandEndSense(&HC0 Or i, curCylinder(i))
                        Exit Sub
                    End If
                Next

                ' No pending interrupt condition respond with invalid command.
                CommandEndSense(&H80)

            Case Commands.SPECIFY ' SPECIFY: no response
                ctlStepRateTime = (commandbuf(1) >> 4) And &HF
                ctlHeadUnloadTime = commandbuf(1) And &HF
                ctlHeadLoadTime = (commandbuf(2) >> 1) And &H7F
                ctlNonDma = (commandbuf(2) And 1) = 1
                CommandEndVoid()

            Case Commands.SENSE_DRIVE ' SENSE DRIVE: respond immediately
                Dim st3 As Byte = commandbuf(1) And &H7
                If curCylinder(cmdDrive) = 0 Then st3 = st3 Or &H10 ' track 0
                st3 = st3 Or &H20            ' ready line is tied to true
                If diskimg(cmdDrive) IsNot Nothing Then
                    If diskimg(cmdDrive).Heads() > 1 Then st3 = st3 Or &H8 ' two side
                    If diskimg(cmdDrive).IsReadOnly() Then st3 = st3 Or &H40 ' write protected
                End If
                CommandEndSense(st3)

            Case Commands.SEEK        ' SEEK: go to EXECUTE state
                driveSeeking = driveSeeking Or CByte(1 << cmdDrive)
                state = States.EXECUTE
                sched.RunTaskAfter(task, COMMANDDELAY)

            Case Else ' INVALID: respond immediately
                regSt0 = &H80
                CommandEndSense(regSt0)
        End Select
    End Sub

    ' Next step in the execution of a command
    Private Sub CommandExecute()
        Dim offs As Long
        Dim n, k As Integer

        ' Handle Seek and Recalibrate commands.
        Select Case cmdCmd
            Case Commands.CALIBRATE, Commands.SEEK
                curCylinder(cmdDrive) = cmdCylinder
                CommandEndSeek()
                Exit Sub
        End Select

        ' Check for NOT READY.
        If diskimg(cmdDrive) Is Nothing Then
            If state = States.EXECUTE Then
                ' No floppy image attached at start of command respond with NOT READY.
                CommandEndIO(&H48, &H0, &H0)     ' abnormal, not ready
            Else
                ' Drive changed ready state during command respond with NOT READY.
                CommandEndIO(&HC8, &H0, &H0)     ' abnormal (ready change), not ready
            End If
            Exit Sub
        End If

        ' Check for valid cylinder/head/sector numbers.
        Select Case cmdCmd
            Case Commands.READ,
                 Commands.READ_DEL,
                 Commands.WRITE,
                 Commands.WRITE_DEL,
                 Commands.READ_TRACK,
                 Commands.SCAN_EQ,
                 Commands.SCAN_LE,
                 Commands.SCAN_GE

                ' Check cylinder number.
                If (cmdCylinder And &HFF) <> curCylinder(cmdDrive) Then
                    ' Requested cylinder does not match current head position
                    ' respond with NO DATA and WRONG CYLINDER.
                    CommandEndIO(&H40, &H4, &H10)     ' abnormal, no data, wrong cylinder
                    Exit Sub
                End If

                ' Check head number.
                If (cmdHead And &HFF) >= diskimg(cmdDrive).Heads() Then
                    ' Head out-of-range respond with NOT READY
                    CommandEndIO(&H48, &H0, &H0)     ' abnormal, not ready
                    Exit Sub
                End If

                If cmdCmd = Commands.READ_TRACK Then Exit Select

                ' Check sector number.
                If cmdRecord = 0 OrElse (cmdRecord And &HFF) > diskimg(cmdDrive).Sectors() Then
                    ' Sector out-of-range respond with NO DATA.
                    CommandEndIO(&H40, &H4, &H0)     ' abnormal, no data
                    Exit Sub
                End If
        End Select

        Select Case cmdCmd
            Case Commands.READ_DEL,
                 Commands.WRITE_DEL,
                 Commands.FORMAT
                ' Not implemented respond with NO DATA and MISSING ADDRESS.
                CommandEndIO(&H40, &H5, &H1)         ' abnormal, no data, missing address

            Case Commands.READ
                ' Read sector.
                ReDim databuf(512 - 1)
                offs = diskimg(cmdDrive).LBA(cmdCylinder, cmdHead, cmdRecord)
                k = diskimg(cmdDrive).Read(offs, databuf)
                If k < 0 Then
                    ' Read error respond with DATA ERROR.
                    CommandEndIO(&H40, &H20, &H0)     ' abnormal, data error
                    Exit Sub
                End If

                ' Go to TRANSFER state.
                dataptr = 0
                state = States.TRANSFER_OUT
                KickTransfer()

            Case Commands.WRITE
                ' Check for WRITE PROTECTED.
                If diskimg(cmdDrive).IsReadOnly() Then
                    CommandEndIO(&H40, &H2, &H0)     ' abnormal, write protected
                    Exit Sub
                End If

                ' Go to TRANSFER state.
                ReDim databuf(512 - 1)
                dataptr = 0
                state = States.TRANSFER_IN
                KickTransfer()

            Case Commands.READ_TRACK
                ' Read track.
                n = diskimg(cmdDrive).Sectors()
                If (cmdEot And &HFF) < n Then n = cmdEot And &HFF
                ReDim databuf(n * 512 - 1)
                offs = diskimg(cmdDrive).LBA(cmdCylinder, cmdHead, 1)
                k = diskimg(cmdDrive).Read(offs, databuf)
                If k < 0 Then
                    ' Read error respond with DATA ERROR.
                    CommandEndIO(&H40, &H20, &H0)     ' abnormal, data error
                    Exit Sub
                End If

                ' Go to TRANSFER state.
                dataptr = 0
                state = States.TRANSFER_OUT
                KickTransfer()

            Case Commands.READ_ID
                ' Exit Sub current cylinder, sector 1.
                cmdCylinder = curCylinder(cmdDrive)
                cmdRecord = 1
                CommandEndIO(&H0, &H0, &H0)         ' normal termination

            Case Commands.SCAN_EQ,
                 Commands.SCAN_LE,
                 Commands.SCAN_GE
                ' Go to TRANSFER state.
                ReDim databuf(512 - 1)
                dataptr = 0
                state = States.TRANSFER_IN
                KickTransfer()
        End Select
    End Sub

    ' Called when a buffer has been transferred (or EOP-ed)
    Private Sub CommandTransferDone()
        Dim offs As Long
        Dim n, k As Integer
        Dim tmpbuf() As Byte
        Dim scanEq, scanLe, scanGe As Boolean
        Dim st2 As Byte = &H0
        Dim sectorStep As Byte = 1

        Select Case cmdCmd
            Case Commands.READ
                Exit Select

            Case Commands.WRITE
                ' Write sector.
                If diskimg(cmdDrive) Is Nothing Then
                    ' No floppy image attached respond with NOT READY.
                    CommandEndIO(&HC8, &H0, &H0)     ' abnormal (ready change), not ready
                    Exit Sub
                End If
                offs = diskimg(cmdDrive).LBA(cmdCylinder, cmdHead, cmdRecord)
                k = diskimg(cmdDrive).Write(offs, databuf)
                If k < 0 Then
                    ' Write error respond with DATA ERROR.
                    CommandEndIO(&H40, &H20, &H0)     ' abnormal, data error
                    Exit Sub
                End If

            Case Commands.READ_TRACK
                ' Track done.
                ' Did we encounter a sector matching cmdRecord?
                n = (dataptr + 511) / 512
                If cmdRecord <> 0 AndAlso (cmdRecord And &HFF) <= n Then
                    CommandEndIO(&H0, &H0, &H0)     ' normal termination
                Else
                    CommandEndIO(&H0, &H4, &H0)     ' normal termination, no data
                    Exit Sub
                End If

            Case Commands.SCAN_EQ,
                 Commands.SCAN_LE,
                 Commands.SCAN_GE
                ' Read sector from disk.
                If diskimg(cmdDrive) Is Nothing Then
                    ' No floppy image attached respond with NOT READY.
                    CommandEndIO(&HC8, &H0, &H0)     ' abnormal (ready change), not ready
                    Exit Sub
                End If
                offs = diskimg(cmdDrive).LBA(cmdCylinder, cmdHead, cmdRecord)
                ReDim tmpbuf(512 - 1)
                k = diskimg(cmdDrive).Read(offs, tmpbuf)
                If k < 0 Then
                    ' Read error respond with DATA ERROR.
                    CommandEndIO(&H40, &H20, &H0)     ' abnormal, data error
                    Exit Sub
                End If
                ' Compare supplied data to on-disk data.
                scanEq = scanLe = scanGe = True
                For i As Integer = 0 To 512 - 1
                    If (databuf(i) And &HFF) < (tmpbuf(i) And &HFF) Then
                        scanEq = False
                        scanGe = False
                    ElseIf (databuf(i) And &HFF) > (tmpbuf(i) And &HFF) Then
                        scanEq = False
                        scanLe = False
                    End If
                Next
                If (cmdCmd = Commands.SCAN_EQ AndAlso scanEq) OrElse
                     (cmdCmd = Commands.SCAN_LE AndAlso scanLe) OrElse
                     (cmdCmd = Commands.SCAN_GE AndAlso scanGe) Then
                    ' Scan condition met.
                    st2 = If(scanEq, &H8, &H0) ' if equal, set scan hit flag
                    CommandEndIO(&H0, &H0, st2)      ' normal termination
                    Exit Sub
                End If
                st2 = &H4                             ' set scan not satisfied flag
                sectorStep = commandbuf(8)             ' sector increment supplied by command word
        End Select

        If dataptr = 512 Then
            ' Complete sector transferred increment sector number.
            If cmdRecord = cmdEot Then
                cmdRecord = sectorStep
                If cmdMultitrack Then cmdHead = cmdHead Xor 1
                If Not cmdMultitrack OrElse (cmdHead And 1) = 0 Then cmdCylinder += 1
            Else
                cmdRecord += sectorStep
            End If
        End If

        If state = States.TRANSFER_EOP OrElse (cmdRecord = 1 AndAlso (Not cmdMultitrack OrElse (cmdHead And 1) = 0)) Then
            ' Transferred last sector or got EOP.
            CommandEndIO(&H0, &H0, st2)          ' normal termination
        Else
            ' Start transfer of next sector.
            CommandExecute()
        End If
    End Sub

    ' Ends a command which does not return response data
    Private Sub CommandEndVoid()
        commandptr = 0
        state = States.IDLE
    End Sub

    ' Ends a command which returns data without an IRQ signal
    Private Sub CommandEndSense(st As Byte)
        ReDim resultbuf(1 - 1)
        resultbuf(0) = st
        resultptr = 0
        state = States.RESULT
    End Sub

    ' Ends a command which returns data without an IRQ signal
    Private Sub CommandEndSense(st As Byte, pcn As Byte)
        ReDim resultbuf(2 - 1)
        resultbuf(0) = st
        resultbuf(1) = pcn
        resultptr = 0
        state = States.RESULT
    End Sub

    ' Ends a command which returns no data but raises an IRQ signal
    Private Sub CommandEndSeek()
        commandptr = 0
        state = States.IDLE
        If irq IsNot Nothing Then irq.Raise(True)
    End Sub

    ' Ends a command which reports I/O status and raises an IRQ signal
    Private Sub CommandEndIO(st0 As Integer, st1 As Integer, st2 As Integer)
        ReDim resultbuf(7 - 1)
        regSt0 = CByte(st0 Or (commandbuf(1) And 7))
        resultbuf(0) = regSt0
        resultbuf(1) = st1
        resultbuf(2) = st2
        resultbuf(3) = cmdCylinder
        resultbuf(4) = cmdHead
        resultbuf(5) = cmdRecord
        resultbuf(6) = 2 ' always assume 512-byte sectors
        resultptr = 0
        state = States.RESULT

        'For i As Integer = 0 To 7 - 1
        '    mCPU.Memory(X8086.SegmentOffetToAbsolute(&H40, &H42 + i)) = resultbuf(i)
        'Next

        If irq IsNot Nothing Then irq.Raise(True)
    End Sub

    ' Called from the scheduled task to handle the next step of a command
    Private Sub Update()
        Select Case state
            Case States.EXECUTE
                ' Start/continue command execution.
                CommandExecute()

            Case States.TRANSFER_IN,
                 States.TRANSFER_OUT
                ' Timeout during I/O transfer terminate command.
                If dma IsNot Nothing Then dma.DMARequest(False)
                ' A real floppy controller would probably complete the current sector here.
                ' But a timeout is in itself a pretty serious error, so we don't care so much
                ' about the exact behavior. (TODO)
                CommandEndIO(&H48, &H10, &H0)         ' abnormal, overrun

            Case States.TRANSWAIT_IN,
                 States.TRANSWAIT_OUT
                If dataptr < databuf.Length Then
                    ' Continue the current transfer.
                    state = If(state = States.TRANSWAIT_IN, States.TRANSFER_IN, States.TRANSFER_OUT)
                    KickTransfer()
                Else
                    ' Transfer completed.
                    CommandTransferDone()
                End If

            Case States.TRANSFER_EOP
                ' Transfer EOP-ed.
                CommandTransferDone()
            Case States.RESULT
                Stop
        End Select
    End Sub

    ' Returns current value of main status register
    Private Function GetMainStatus() As Byte
        Dim stmain As Byte
        Select Case state
            Case States.IDLE
                stmain = &H80            ' RQM, WR

            Case States.COMMAND
                stmain = &H90            ' RQM, WR, CMDBSY

            Case States.EXECUTE
                stmain = &H10            ' CMDBSY

            Case States.TRANSFER_IN
                stmain = &H10            ' CMDBSY
                If ctlNonDma Then stmain = stmain Or &HC0 ' RQM, WR, NONDMA

            Case States.TRANSFER_OUT
                stmain = &H10
                If ctlNonDma Then stmain = stmain Or &HE0 ' RQM, RD, NONDMA

            Case States.RESULT
                stmain = &HD0            ' RQM, RD, CMDBSY

            Case Else
                stmain = &H10            ' CMDBSY
                If ctlNonDma Then stmain = stmain Or &H20 ' NONDMA
        End Select

        stmain = stmain Or driveSeeking ' bit mask of seeking drives
        'mCPU.Memory(X8086.SegmentOffetToAbsolute(&H40, &H3E)) = stmain

        Return stmain
    End Function

    Public Overrides Sub CloseAdapter()

    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "I8272 Floppy Disk Controller"
        End Get
    End Property

    Public Overrides Function [In](port As UInt16) As Byte
        If (port And 3) = 0 Then
            ' main status register
            Return GetMainStatus()
        ElseIf (port And 3) = 1 Then
            ' read from data register
            If irq IsNot Nothing Then irq.Raise(False)
            If state = States.RESULT Then
                ' read next byte of result
                Dim v As Integer = resultbuf(resultptr) And &HFF
                resultptr += 1
                If resultptr = resultbuf.Length Then
                    ' end of result phase
                    commandptr = 0
                    databuf = Nothing
                    resultbuf = Nothing
                    dataptr = 0
                    resultptr = 0
                    state = States.IDLE
                End If
                Return v
            ElseIf state = States.TRANSFER_OUT AndAlso ctlNonDma Then
                ' read next I/O byte in non-DMA mode
                Dim v As Integer = databuf(dataptr) And &HFF
                dataptr += 1
                state = States.TRANSWAIT_OUT
                Return v
            End If
        End If

        ' unexpected read
        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        If (port And 3) = 2 Then
            ' write to digital output register
            If (value And &H4) = 0 Then
                ' reset controller
                Reset()
            ElseIf (regDOR And &H4) = 0 Then
                ' awake from reset condition send disk-ready notification
                If irq IsNot Nothing Then irq.Raise(True)
                pendingReadyChange = &HF
            End If
            regDOR = value And &HFF

        ElseIf (port And 3) = 1 Then
            ' write to data register
            If state = States.IDLE Then
                ' CPU writes first command byte
                state = States.COMMAND
                cmdCmd = value And &H1F
                commandlen = CommandLength()
            End If

            If state = States.COMMAND Then
                ' CPU writes a command byte
                commandbuf(commandptr) = value
                commandptr += 1
                If commandptr = commandlen Then CommandStart()
            ElseIf state = States.TRANSFER_IN AndAlso ctlNonDma Then
                ' CPU writes data byte
                databuf(dataptr) = value
                dataptr += 1
                state = States.TRANSWAIT_IN
                If irq IsNot Nothing Then irq.Raise(False)
            Else
                ' unexpected write
            End If
        Else
            ' write to unknown port
        End If
    End Sub

    Public Overrides Sub Run()
        Update()
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Floppy"
        End Get
    End Property

    Public Overrides ReadOnly Property Type As Adapter.AdapterType
        Get
            Return AdapterType.Floppy
        End Get
    End Property

    Public Overrides ReadOnly Property Vendor As String
        Get
            Return "xFX JumpStart"
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMajor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 7
        End Get
    End Property

    Public Sub DMARead(b As Byte) Implements IDMADevice.DMARead
        If state = States.TRANSFER_IN Then
            databuf(dataptr) = b
            dataptr += 1
            If dataptr = databuf.Length Then
                state = States.TRANSWAIT_IN
                If dma IsNot Nothing Then dma.DMARequest(False)
            End If
        Else
            ' unexpected dmaRead
        End If
    End Sub

    Public Function DMAWrite() As Byte Implements IDMADevice.DMAWrite
        If state = States.TRANSFER_OUT Then
            Dim v As Byte = databuf(dataptr)
            dataptr += 1
            If dataptr = databuf.Length Then
                state = States.TRANSWAIT_OUT
                If dma IsNot Nothing Then dma.DMARequest(False)
            End If
            Return v
        Else
            ' unexpected dmaWrite
            Return &HFF
        End If
    End Function

    ' Handles EOP signal from the DMA controller
    Public Sub DMAEOP() Implements IDMADevice.DMAEOP
        Select Case state
            Case States.TRANSFER_IN,
                 States.TRANSFER_OUT,
                 States.TRANSWAIT_IN,
                 States.TRANSWAIT_OUT
                ' Terminate command
                If dma IsNot Nothing Then dma.DMARequest(False)
                state = States.TRANSFER_EOP
            Case Else
                ' unexpected dmaEop
        End Select
    End Sub
End Class

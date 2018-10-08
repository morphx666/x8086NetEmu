Public Class DMAI8237
    Inherits IOPortHandler

    ' Switches between low/high byte of address and count registers
    Private msbFlipFlop As Boolean

    ' Command register (8-bit)
    Private cmdReg As Integer

    ' Status register (8-bit)
    Private statusReg As Integer

    ' Temporary register (8-bit)
    Private tempReg As Integer

    ' Bitmask of active software DMA requests (bits 0-3)
    Private reqReg As Integer

    ' Mask register (bits 0-3)
    Public MaskReg As Integer

    ' Channel with highest priority
    Private prioChannel As Integer

    ' Four DMA channels in the I8237 chip
    Private ReadOnly channels() As Channel

    ' CPU
    Private cpu As X8086

    ' True if the background task is currently scheduled
    Private pendingTask As Boolean

    ' Channel 0 DREQ trigger period for lazy simulation
    Private ch0TriggerPeriod As Long

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

    ' Scheduler time stamp for the first channel 0 DREQ trigger
    ' that has not yet been accounted for, or -1 to disable
    Private ch0NextTrigger As Long

    Public Class Channel
        Implements IDMAChannel

        ' Base address register (16-bit)
        Public BaseAddress As Integer

        ' Base count register (16-bit)
        Public BaseCount As Integer

        ' Current address register (16-bit)
        Public CurrentAddress As Integer

        ' Current count register (16-bit)
        Public CurrentCount As Integer

        ' Mode register (bits 2-7)
        Public Mode As Integer

        Public Masked As Integer
        Public Direction As Integer
        Public AutoInit As Integer
        Public WriteMode As Integer

        ' Page (address bits 16 - 23) for this channel.
        Public Page As Integer

        ' Device with which this channel is currently associated.
        Public Device As IDMADevice

        ' True if DREQ is active for this channel.
        Public PendingRequest As Boolean

        ' True if the device signaled external EOP.
        Public ExternalEop As Boolean

        Private PortDevice As DMAI8237

        ' Constructs channel.
        Public Sub New(dmaDev As DMAI8237)
            Me.PortDevice = dmaDev
        End Sub

        Public Sub DMAEOP() Implements IDMAChannel.DMAEOP
            ExternalEop = True
        End Sub

        Public Sub DMARequest(enable As Boolean) Implements IDMAChannel.DMARequest
            PendingRequest = enable
            PortDevice.TryHandleRequest()
        End Sub
    End Class

    Public Overrides Sub Run()
        pendingTask = False
        TryHandleRequest()
    End Sub

    Public Sub New(cpu As X8086)
        Me.cpu = cpu
        ReDim channels(4)
        For i As Integer = 0 To 4 - 1
            channels(i) = New Channel(Me)
        Next
        MaskReg = &HF '  mask all channels
        ch0NextTrigger = -1

        For i As UInt32 = &H0 To &HF
            ValidPortAddress.Add(i)
        Next

        For i As UInt32 = &H80 To &H8F
            ValidPortAddress.Add(i)
        Next
    End Sub

    Public Function GetChannel(channelNumber As Integer) As IDMAChannel
        Return channels(channelNumber)
    End Function

    ' Binds a device to a DMA channel.
    ' @param change DMA channel to use (0 ... 3)
    ' @param dev    device object to use for callbacks on this channel
    ' @return the DmaChannel object
    Public Function BindChannel(channelNumber As Integer, dev As IDMADevice) As IDMAChannel
        If channelNumber = 0 Then Throw New ArgumentException("Can not bind DMA channel 0")
        channels(channelNumber).Device = dev
        channels(channelNumber).PendingRequest = False
        channels(channelNumber).ExternalEop = False
        Return channels(channelNumber)
    End Function

    ' Changes the DREQ trigger period for channel 0.
    '@param period trigger period in nanoseconds, or 0 to disable
    Public Sub SetCh0Period(period As Long)
        UpdateCh0()
        ch0TriggerPeriod = period
        If ch0NextTrigger = -1 AndAlso period > 0 Then ch0NextTrigger = cpu.Sched.CurrentTime + period
    End Sub

    ' Updates the lazy simulation of the periodic channel 0 DREQ trigger.
    Protected Sub UpdateCh0()
        ' Figure out how many channel 0 DREQ triggers have occurred since
        ' the last update, and update channel 0 status to account for
        ' these triggers.

        Dim t As Long = cpu.Sched.CurrentTime
        Dim ntrigger As Long = 0
        If ch0NextTrigger >= 0 AndAlso ch0NextTrigger <= t Then
            ' Rounding errors cause some divergence between DMA channel 0 and
            ' timer channel 1, but probably nobody will notice.
            If ch0TriggerPeriod > 0 Then
                Dim d As Long = t - ch0NextTrigger
                ntrigger = 1 + d / ch0TriggerPeriod
                ch0NextTrigger = t + ch0TriggerPeriod - (d Mod ch0TriggerPeriod)
            Else
                ntrigger = 1
                ch0NextTrigger = -1
            End If
        End If

        If ntrigger = 0 Then Exit Sub

        ' Ignore triggers if DMA controller is disabled
        If (cmdReg And &H4) <> 0 Then Exit Sub

        ' Ignore triggers if channel 0 is masked
        If (MaskReg And 1) = 1 Then Exit Sub

        ' The only sensible mode for channel 0 in a PC is
        ' auto-initialized single read mode, so we simply assume that.

        ' Update count, address and status registers to account for
        ' the past triggers.
        Dim addrstep As Integer = If((cmdReg And &H2) = 0, If((channels(0).Mode And &H20) = 0, 1, -1), 0)
        If ntrigger <= channels(0).CurrentCount Then
            ' no terminal count
            Dim n As Integer = CInt(ntrigger)
            channels(0).CurrentCount -= n
            channels(0).CurrentAddress = (channels(0).CurrentAddress + n * addrstep) And &HFFFF
        Else
            ' terminal count occurred
            Dim n As Integer = CInt((ntrigger - channels(0).CurrentCount - 1) Mod (channels(0).BaseCount + 1))
            channels(0).CurrentCount = channels(0).BaseCount - n
            channels(0).CurrentAddress = (channels(0).BaseAddress + n * addrstep) And &HFFFF
            statusReg = statusReg Or 1
        End If
    End Sub

    Protected Sub TryHandleRequest()
        Dim i As Integer

        ' Update request bits in status register
        Dim rbits As Integer = reqReg
        For i = 0 To 4 - 1
            If channels(i).PendingRequest Then rbits = rbits Or (1 << i)
        Next
        statusReg = (statusReg And &HF) Or (rbits << 4)

        ' Don't start a transfer during dead time after a previous transfer
        If pendingTask Then Exit Sub

        ' Don't start a transfer if the controller is disabled
        If (cmdReg And &H4) <> 0 Then Exit Sub

        ' Select a channel with pending request
        rbits = rbits And (Not MaskReg)
        rbits = rbits And (Not 1) ' never select channel 0
        If rbits = 0 Then Exit Sub

        i = prioChannel
        While ((rbits >> i) And 1) = 0
            i = (i + 1) And 3
        End While

        ' Just decided to start a transfer on channel i
        Dim chan As Channel = channels(i)
        Dim dev As IDMADevice = chan.Device
        Dim mode As Integer = chan.Mode
        Dim page As Integer = chan.Page

        ' Update dynamic priority
        If (cmdReg And 10) <> 0 Then prioChannel = (i + 1) And 3

        ' Block further transactions until this one completes
        pendingTask = True
        Dim transferTime As Long = 0

        If (mode And &HC0) = &HC0 Then
            'log.warn("cascade mode not implemented (channel " + i + ")")
            Stop
        ElseIf (mode And &HC) = &HC Then
            'log.warn("invalid mode on channel " + i)
        Else
            ' Prepare for transfer
            Dim blockmode As Boolean = (mode And &HC0) = &H80
            Dim singlemode As Boolean = (mode And &HC0) = &H40
            Dim curcount As Integer = chan.CurrentCount
            Dim maxlen As Integer = curcount + 1
            Dim curaddr As Integer = chan.CurrentAddress
            Dim addrstep As Integer = If((chan.Mode And &H20) = 0, 1, -1)
            chan.ExternalEop = False

            ' Don't combine too much single transfers in one atomic action
            If singlemode AndAlso maxlen > 25 Then maxlen = 25

            ' Execute transfer
            Select Case mode And &HC
                Case &H0
                    ' DMA verify
                    curcount -= maxlen
                    curaddr = (curaddr + maxlen * addrstep) And &HFFFF
                    transferTime += 3 * maxlen * Scheduler.BASECLOCK / cpu.Clock
                Case &H4
                    ' DMA write
                    While (maxlen > 0) AndAlso (Not chan.ExternalEop) AndAlso (blockmode OrElse chan.PendingRequest)
                        If dev IsNot Nothing Then cpu.Memory((page << 16) Or curaddr) = dev.DMAWrite()
                        maxlen -= 1
                        curcount -= 1
                        curaddr = (curaddr + addrstep) And &HFFFF
                        transferTime += 3 * Scheduler.BASECLOCK / cpu.Clock
                    End While
                Case &H8
                    ' DMA read
                    While maxlen > 0 AndAlso Not chan.ExternalEop AndAlso (blockmode OrElse chan.PendingRequest)
                        If dev IsNot Nothing Then dev.DMARead(cpu.Memory((page << 16) Or curaddr))
                        maxlen -= 1
                        curcount -= 1
                        curaddr = (curaddr + addrstep) And &HFFFF
                        transferTime += 3 * Scheduler.BASECLOCK / cpu.Clock
                    End While
            End Select

            ' Update registers
            Dim termcount As Boolean = curcount < 0
            chan.CurrentCount = If(termcount, &HFFFF, curcount)
            chan.CurrentAddress = curaddr

            ' Handle terminal count or external EOP
            If termcount OrElse chan.ExternalEop Then
                If (mode And &H10) = 0 Then
                    ' Set mask bit
                    MaskReg = MaskReg Or (1 << i)
                Else
                    ' Auto-initialize
                    chan.CurrentCount = chan.BaseCount
                    chan.CurrentAddress = chan.BaseAddress
                End If
                ' Clear software request
                reqReg = reqReg And (Not 1 << i)
                ' Set TC bit in status register
                statusReg = statusReg Or (1 << i)
            End If

            ' Send EOP to device
            If termcount AndAlso (Not chan.ExternalEop) AndAlso dev IsNot Nothing Then dev.DMAEOP()
        End If

        ' Schedule a task to run when the simulated DMA transfer completes
        cpu.Sched.RunTaskAfter(task, transferTime)
    End Sub

    Public Overrides Function [In](port As UInt32) As UInt16
        UpdateCh0()
        If (port And &HFFF8) = 0 Then
            ' DMA controller: channel status
            Dim chan As Channel = channels((port >> 1) And 3)
            Dim x As Integer = If((port And 1) = 0, chan.CurrentAddress, chan.CurrentCount)
            Dim p As Boolean = msbFlipFlop
            msbFlipFlop = Not p
            Return If(p, (x >> 8) And &HFF, x And &HFF)
        ElseIf (port And &HFFF8) = &H8 Then
            ' DMA controller: operation registers
            Dim v As Integer
            Select Case port
                Case 8 ' read status register
                    v = statusReg
                    statusReg = statusReg And &HF0
                    Return v
                Case 13 ' read temporary register
                    Return tempReg
            End Select
        End If

        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInt32, value As UInt16)
        UpdateCh0()

        If (port And &HFFF8) = 0 Then
            ' DMA controller: channel setup
            Dim chan As Channel = channels((port >> 1) And 3)

            Dim x As Integer
            Dim y As Integer

            If (port And 1) = 0 Then
                ' base/current address
                x = chan.BaseAddress
                y = chan.CurrentAddress
            Else
                x = chan.BaseCount
                y = chan.CurrentCount
            End If
            Dim p As Boolean = msbFlipFlop
            msbFlipFlop = Not p
            If p Then
                x = (x And &HFF) Or ((value << 8) And &HFF00)
                y = (y And &HFF) Or ((value << 8) And &HFF00)
            Else
                x = (x And &HFF00) Or (value And &HFF)
                y = (y And &HFF00) Or (value And &HFF)
            End If
            If (port And 1) = 0 Then
                chan.BaseAddress = x
                chan.CurrentAddress = y
            Else
                chan.BaseCount = x
                chan.CurrentCount = y
            End If
        ElseIf (port And &HFFF8) = &H8 Then
            ' DMA controller: operation registers
            Select Case port And &HF
                Case 8 ' write command register
                    cmdReg = value
                    If (value And &H10) = 0 Then prioChannel = 0 ' enable fixed priority
                    If (value And 1) = 1 Then cpu.RaiseException("DMA8237: memory-to-memory transfer not implemented")

                Case 9 ' set/reset request register
                    If (value And 4) = 0 Then
                        reqReg = reqReg And (Not 1 << (value And 3)) ' reset request bit
                    Else
                        reqReg = reqReg Or (1 << (value And 3))  ' set request bit
                        If (value And 7) = 4 Then cpu.RaiseException("DMA8237: software request on channel 0 not implemented")
                    End If

                Case 10 ' set/reset mask register
                    If (value And 4) = 0 Then
                        MaskReg = MaskReg And (Not 1 << (value And 3)) ' reset mask bit
                    Else
                        MaskReg = MaskReg Or (1 << (value And 3))  ' set mask bit
                    End If
                    channels(value And 3).Masked = (value >> 2) And 1

                Case 11 ' write mode register
                    channels(value And 3).Mode = value
                    channels(value And 3).Direction = (value >> 5) And 1
                    channels(value And 3).AutoInit = (value >> 4) And 1
                    channels(value And 3).WriteMode = (value >> 2) And 1
                    If (value And 3) = 0 AndAlso (value And &HDC) <> &H58 Then cpu.RaiseException("DMA8237: unsupported mode on channel 0")

                Case 12 ' clear msb flipflop
                    msbFlipFlop = False

                Case 13 ' master clear
                    msbFlipFlop = False
                    cmdReg = 0
                    statusReg = 0
                    reqReg = 0
                    tempReg = 0
                    MaskReg = &HF

                Case 14 ' clear mask register
                    MaskReg = 0

                Case 15 ' write mask register
                    MaskReg = value
            End Select
            TryHandleRequest()

        ElseIf (port And &HFFF8) = &H80 Then
            ' DMA page registers
            Select Case port
                Case &H81 : channels(2).Page = value
                Case &H82 : channels(3).Page = value
                Case &H83 : channels(1).Page = value
                Case &H87 : channels(0).Page = value
            End Select
        End If
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "DMA Controller"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "8237"
        End Get
    End Property
End Class


'Public Class DMAI8237
'    Inherits IOPortHandler

'    ' Switches between low/high byte of address and count registers
'    Private msbFlipFlop As Boolean

'    ' Command register (8-bit)
'    Private cmdReg As Integer

'    ' Status register (8-bit)
'    Private statusReg As Integer

'    ' Temporary register (8-bit)
'    Private tempReg As Integer

'    ' Bitmask of active software DMA requests (bits 0-3)
'    Private reqReg As Integer

'    ' Mask register (bits 0-3)
'    Public MaskReg As Integer

'    ' Channel with highest priority
'    Private prioChannel As Integer

'    ' Four DMA channels in the I8237 chip
'    Private ReadOnly channels() As Channel

'    ' CPU
'    Private cpu As X8086

'    ' True if the background task is currently scheduled
'    Private pendingTask As Boolean

'    ' Channel 0 DREQ trigger period for lazy simulation
'    Private ch0TriggerPeriod As Long

'    Private Class TaskSC
'        Inherits Scheduler.Task

'        Public Sub New(owner As IOPortHandler)
'            MyBase.New(owner)
'        End Sub

'        Public Overrides Sub Run()
'            Owner.Run()
'        End Sub

'        Public Overrides ReadOnly Property Name As String
'            Get
'                Return Owner.Name
'            End Get
'        End Property
'    End Class
'    Private task As Scheduler.Task = New TaskSC(Me)

'    ' Scheduler time stamp for the first channel 0 DREQ trigger
'    ' that has not yet been accounted for, or -1 to disable
'    Private ch0NextTrigger As Long

'    Public Class Channel
'        Implements IDMAChannel

'        ' Base address register (16-bit)
'        Public BaseAddress As Integer

'        ' Base count register (16-bit)
'        Public BaseCount As Integer

'        ' Current address register (16-bit)
'        Public CurrentAddress As Integer

'        ' Current count register (16-bit)
'        Public CurrentCount As Integer

'        ' Mode register (bits 2-7)
'        Public Mode As Integer

'        ' Page (address bits 16 - 23) for this channel.
'        Public Page As Integer

'        ' Device with which this channel is currently associated.
'        Public Device As IDMADevice

'        ' True if DREQ is active for this channel.
'        Public PendingRequest As Boolean

'        ' True if the device signaled external EOP.
'        Public ExternalEop As Boolean

'        Private PortDevice As DMAI8237

'        ' Constructs channel.
'        Public Sub New(dmaDev As DMAI8237)
'            Me.PortDevice = dmaDev
'        End Sub

'        Public Sub DMAEOP() Implements IDMAChannel.DMAEOP
'            ExternalEop = True
'        End Sub

'        Public Sub DMARequest(enable As Boolean) Implements IDMAChannel.DMARequest
'            PendingRequest = enable
'            PortDevice.TryHandleRequest()
'        End Sub
'    End Class

'    Public Overrides Sub Run()
'        pendingTask = False
'        TryHandleRequest()
'    End Sub

'    Public Sub New(cpu As X8086)
'        Me.cpu = cpu
'        ReDim channels(4)
'        For i As Integer = 0 To 4 - 1
'            channels(i) = New Channel(Me)
'        Next
'        MaskReg = &HF '  mask all channels
'        ch0NextTrigger = -1

'        For i As UInt32 = &H0 To &HF
'            ValidPortAddress.Add(i)
'        Next

'        For i As UInt32 = &H80 To &H8F
'            ValidPortAddress.Add(i)
'        Next
'    End Sub

'    Public Function GetChannel(channelNumber As Integer) As IDMAChannel
'        Return channels(channelNumber)
'    End Function

'    ' Binds a device to a DMA channel.
'    ' @param change DMA channel to use (0 ... 3)
'    ' @param dev    device object to use for callbacks on this channel
'    ' @return the DmaChannel object
'    Public Function BindChannel(channelNumber As Integer, dev As IDMADevice) As IDMAChannel
'        If channelNumber = 0 Then Throw New ArgumentException("Can not bind DMA channel 0")
'        channels(channelNumber).Device = dev
'        channels(channelNumber).PendingRequest = False
'        channels(channelNumber).ExternalEop = False
'        Return channels(channelNumber)
'    End Function

'    ' Changes the DREQ trigger period for channel 0.
'    '@param period trigger period in nanoseconds, or 0 to disable
'    Public Sub SetCh0Period(period As Long)
'        UpdateCh0()
'        ch0TriggerPeriod = period
'        If ch0NextTrigger = -1 AndAlso period > 0 Then ch0NextTrigger = cpu.Sched.CurrentTime + period
'    End Sub

'    ' Updates the lazy simulation of the periodic channel 0 DREQ trigger.
'    Protected Sub UpdateCh0()
'        ' Figure out how many channel 0 DREQ triggers have occurred since
'        ' the last update, and update channel 0 status to account for
'        ' these triggers.

'        Dim t As Long = cpu.Sched.CurrentTime
'        Dim ntrigger As Long = 0
'        If ch0NextTrigger >= 0 AndAlso ch0NextTrigger <= t Then
'            ' Rounding errors cause some divergence between DMA channel 0 and
'            ' timer channel 1, but probably nobody will notice.
'            If ch0TriggerPeriod > 0 Then
'                Dim d As Long = t - ch0NextTrigger
'                ntrigger = 1 + d / ch0TriggerPeriod
'                ch0NextTrigger = t + ch0TriggerPeriod - (d Mod ch0TriggerPeriod)
'            Else
'                ntrigger = 1
'                ch0NextTrigger = -1
'            End If
'        End If

'        If ntrigger = 0 Then Exit Sub

'        ' Ignore triggers if DMA controller is disabled
'        If (cmdReg And &H4) <> 0 Then Exit Sub

'        ' Ignore triggers if channel 0 is masked
'        If (MaskReg And 1) = 1 Then Exit Sub

'        ' The only sensible mode for channel 0 in a PC is
'        ' auto-initialized single read mode, so we simply assume that.

'        ' Update count, address and status registers to account for
'        ' the past triggers.
'        Dim addrstep As Integer = If((cmdReg And &H2) = 0, If((channels(0).Mode And &H20) = 0, 1, -1), 0)
'        If ntrigger <= channels(0).CurrentCount Then
'            ' no terminal count
'            Dim n As Integer = CInt(ntrigger)
'            channels(0).CurrentCount -= n
'            channels(0).CurrentAddress = (channels(0).CurrentAddress + n * addrstep) And &HFFFF
'        Else
'            ' terminal count occurred
'            Dim n As Integer = CInt((ntrigger - channels(0).CurrentCount - 1) Mod (channels(0).BaseCount + 1))
'            channels(0).CurrentCount = channels(0).BaseCount - n
'            channels(0).CurrentAddress = (channels(0).BaseAddress + n * addrstep) And &HFFFF
'            statusReg = statusReg Or 1
'        End If
'    End Sub

'    Protected Sub TryHandleRequest()
'        Dim i As Integer

'        ' Update request bits in status register
'        Dim rbits As Integer = reqReg
'        For i = 0 To 4 - 1
'            If channels(i).PendingRequest Then rbits = rbits Or (1 << i)
'        Next
'        statusReg = (statusReg And &HF) Or (rbits << 4)

'        ' Don't start a transfer during dead time after a previous transfer
'        If pendingTask Then Exit Sub

'        ' Don't start a transfer if the controller is disabled
'        If (cmdReg And &H4) <> 0 Then Exit Sub

'        ' Select a channel with pending request
'        rbits = rbits And (Not MaskReg)
'        rbits = rbits And (Not 1) ' never select channel 0
'        If rbits = 0 Then Exit Sub

'        i = prioChannel
'        While ((rbits >> i) And 1) = 0
'            i = (i + 1) And 3
'        End While

'        ' Just decided to start a transfer on channel i
'        Dim chan As Channel = channels(i)
'        Dim dev As IDMADevice = chan.Device
'        Dim mode As Integer = chan.Mode
'        Dim page As Integer = chan.Page

'        ' Update dynamic priority
'        If (cmdReg And 10) <> 0 Then prioChannel = (i + 1) And 3

'        ' Block further transactions until this one completes
'        pendingTask = True
'        Dim transferTime As Long = 0

'        If (mode And &HC0) = &HC0 Then
'            'log.warn("cascade mode not implemented (channel " + i + ")")
'            Stop
'        ElseIf (mode And &HC) = &HC Then
'            'log.warn("invalid mode on channel " + i)
'        Else
'            ' Prepare for transfer
'            Dim blockmode As Boolean = (mode And &HC0) = &H80
'            Dim singlemode As Boolean = (mode And &HC0) = &H40
'            Dim curcount As Integer = chan.CurrentCount
'            Dim maxlen As Integer = curcount + 1
'            Dim curaddr As Integer = chan.CurrentAddress
'            Dim addrstep As Integer = If((chan.Mode And &H20) = 0, 1, -1)
'            chan.ExternalEop = False

'            ' Don't combine too much single transfers in one atomic action
'            If singlemode AndAlso maxlen > 25 Then maxlen = 25

'            ' Execute transfer
'            Select Case mode And &HC
'                Case &H0
'                    ' DMA verify
'                    curcount -= maxlen
'                    curaddr = (curaddr + maxlen * addrstep) And &HFFFF
'                    transferTime += 3 * maxlen * Scheduler.BASECLOCK / cpu.Clock
'                Case &H4
'                    ' DMA write
'                    While (maxlen > 0) AndAlso (Not chan.ExternalEop) AndAlso (blockmode OrElse chan.PendingRequest)
'                        If dev IsNot Nothing Then cpu.Memory((page << 16) Or curaddr) = dev.DMAWrite()
'                        maxlen -= 1
'                        curcount -= 1
'                        curaddr = (curaddr + addrstep) And &HFFFF
'                        transferTime += 3 * Scheduler.BASECLOCK / cpu.Clock
'                    End While
'                Case &H8
'                    ' DMA read
'                    While maxlen > 0 AndAlso Not chan.ExternalEop AndAlso (blockmode OrElse chan.PendingRequest)
'                        If dev IsNot Nothing Then dev.DMARead(cpu.Memory((page << 16) Or curaddr))
'                        maxlen -= 1
'                        curcount -= 1
'                        curaddr = (curaddr + addrstep) And &HFFFF
'                        transferTime += 3 * Scheduler.BASECLOCK / cpu.Clock
'                    End While
'            End Select

'            ' Update registers
'            Dim termcount As Boolean = curcount < 0
'            chan.CurrentCount = If(termcount, &HFFFF, curcount)
'            chan.CurrentAddress = curaddr

'            ' Handle terminal count or external EOP
'            If termcount OrElse chan.ExternalEop Then
'                If (mode And &H10) = 0 Then
'                    ' Set mask bit
'                    MaskReg = MaskReg Or (1 << i)
'                Else
'                    ' Auto-initialize
'                    chan.CurrentCount = chan.BaseCount
'                    chan.CurrentAddress = chan.BaseAddress
'                End If
'                ' Clear software request
'                reqReg = reqReg And (Not 1 << i)
'                ' Set TC bit in status register
'                statusReg = statusReg Or (1 << i)
'            End If

'            ' Send EOP to device
'            If termcount AndAlso (Not chan.ExternalEop) AndAlso dev IsNot Nothing Then dev.DMAEOP()
'        End If

'        ' Schedule a task to run when the simulated DMA transfer completes
'        cpu.Sched.RunTaskAfter(task, transferTime)
'    End Sub

'    Public Overrides Function [In](port As UInt32) As UInt16
'        UpdateCh0()
'        If (port And &HFFF8) = 0 Then
'            ' DMA controller: channel status
'            Dim chan As Channel = channels((port >> 1) And 3)
'            Dim x As Integer = If((port And 1) = 0, chan.CurrentAddress, chan.CurrentCount)
'            Dim p As Boolean = msbFlipFlop
'            msbFlipFlop = Not p
'            Return If(p, (x >> 8) And &HFF, x And &HFF)
'        ElseIf (port And &HFFF8) = &H8 Then
'            ' DMA controller: operation registers
'            Dim v As Integer
'            Select Case port
'                Case 8 ' read status register
'                    v = statusReg
'                    statusReg = statusReg And &HF0
'                    Return v
'                Case 13 ' read temporary register
'                    Return tempReg
'            End Select
'        End If

'        Return &HFF
'    End Function

'    Public Overrides Sub Out(port As UInt32, value As UInt16)
'        UpdateCh0()

'        If (port And &HFFF8) = 0 Then
'            ' DMA controller: channel setup
'            Dim chan As Channel = channels((port >> 1) And 3)

'            Dim x As Integer
'            Dim y As Integer

'            If (port And 1) = 0 Then
'                ' base/current address
'                x = chan.BaseAddress
'                y = chan.CurrentAddress
'            Else
'                x = chan.BaseCount
'                y = chan.CurrentCount
'            End If
'            Dim p As Boolean = msbFlipFlop
'            msbFlipFlop = Not p
'            If p Then
'                x = (x And &HFF) Or ((value << 8) And &HFF00)
'                y = (y And &HFF) Or ((value << 8) And &HFF00)
'            Else
'                x = (x And &HFF00) Or (value And &HFF)
'                y = (y And &HFF00) Or (value And &HFF)
'            End If
'            If (port And 1) = 0 Then
'                chan.BaseAddress = x
'                chan.CurrentAddress = y
'            Else
'                chan.BaseCount = x
'                chan.CurrentCount = y
'            End If
'        ElseIf (port And &HFFF8) = &H8 Then
'            ' DMA controller: operation registers
'            Select Case port And &HF
'                Case 8 ' write command register
'                    cmdReg = value
'                    If (value And &H10) = 0 Then prioChannel = 0 ' enable fixed priority
'                    If (value And 1) = 1 Then cpu.RaiseException("DMA8237: memory-to-memory transfer not implemented")

'                Case 9 ' set/reset request register
'                    If (value And 4) = 0 Then
'                        reqReg = reqReg And (Not 1 << (value And 3)) ' reset request bit
'                    Else
'                        reqReg = reqReg Or (1 << (value And 3))  ' set request bit
'                        If (value And 7) = 4 Then cpu.RaiseException("DMA8237: software request on channel 0 not implemented")
'                    End If

'                Case 10 ' set/reset mask register
'                    If (value And 4) = 0 Then
'                        MaskReg = MaskReg And (Not 1 << (value And 3)) ' reset mask bit
'                    Else
'                        MaskReg = MaskReg Or (1 << (value And 3))  ' set mask bit
'                    End If

'                Case 11 ' write mode register
'                    channels(value And 3).Mode = value
'                    If (value And 3) = 0 AndAlso (value And &HDC) <> &H58 Then cpu.RaiseException("DMA8237: unsupported mode on channel 0")

'                Case 12 ' clear msb flipflop
'                    msbFlipFlop = False

'                Case 13 ' master clear
'                    msbFlipFlop = False
'                    cmdReg = 0
'                    statusReg = 0
'                    reqReg = 0
'                    tempReg = 0
'                    MaskReg = &HF

'                Case 14 ' clear mask register
'                    MaskReg = 0

'                Case 15 ' write mask register
'                    MaskReg = value
'            End Select
'            TryHandleRequest()

'        ElseIf (port And &HFFF8) = &H80 Then
'            ' DMA page registers
'            Select Case port
'                Case &H81 : channels(2).Page = value
'                Case &H82 : channels(3).Page = value
'                Case &H83 : channels(1).Page = value
'                Case &H87 : channels(0).Page = value
'            End Select
'        End If
'    End Sub

'    Public Overrides ReadOnly Property Description As String
'        Get
'            Return "DMA Controller"
'        End Get
'    End Property

'    Public Overrides ReadOnly Property Name As String
'        Get
'            Return "8237"
'        End Get
'    End Property
'End Class

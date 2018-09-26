Public Class DMAI8237
    Inherits IOPortHandler

    ' Switches between low/high byte of address and count registers
    Private msbFlipFlop As Boolean

    ' Command register (8-bit)
    Private cmdreg As Integer

    ' Status register (8-bit)
    Private statusreg As Integer

    ' Temporary register (8-bit)
    Private tempreg As Integer

    ' Bitmask of active software DMA requests (bits 0-3)
    Private reqreg As Integer

    ' Mask register (bits 0-3)
    Private maskreg As Integer

    ' Channel with highest priority
    Private priochannel As Integer

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

    Private Class Channel
        Implements IDMAChannel

        ' Base address register (16-bit)
        Public baseaddr As Integer

        ' Base count register (16-bit)
        Public basecount As Integer

        ' Current address register (16-bit)
        Public curaddr As Integer

        ' Current count register (16-bit)
        Public curcount As Integer

        ' Mode register (bits 2-7)
        Public mode As Integer

        ' Page (address bits 16 - 23) for this channel.
        Public page As Integer

        ' Device with which this channel is currently associated.
        Public dev As IDMADevice

        ' True if DREQ is active for this channel.
        Public pendingRequest As Boolean

        ' True if the device signalled external EOP.
        Public externalEop As Boolean

        Private portDev As DMAI8237

        ' Constructs channel.
        Public Sub New(dmaDev As DMAI8237)
            Me.portDev = dmaDev
        End Sub

        Public Sub DMAEOP() Implements IDMAChannel.DMAEOP
            externalEop = True
        End Sub

        Public Sub DMARequest(enable As Boolean) Implements IDMAChannel.DMARequest
            pendingRequest = enable
            portDev.TryHandleRequest()
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
        maskreg = &HF '  mask all channels
        ch0NextTrigger = -1

        For i As Integer = &H0 To &HF
            ValidPortAddress.Add(i)
        Next

        For i As Integer = &H80 To &H8F
            ValidPortAddress.Add(i)
        Next
    End Sub

    Public Function GetChannel(channelNumber) As IDMAChannel
        Return channels(channelNumber)
    End Function

    ' Binds a device to a DMA channel.
    ' @param change DMA channel to use (0 ... 3)
    ' @param dev    device object to use for callbacks on this channel
    ' @return the DmaChannel object
    Public Function BindChannel(channelNumber As Integer, dev As IDMADevice) As IDMAChannel
        If channelNumber = 0 Then Throw New ArgumentException("Can not bind DMA channel 0")
        channels(channelNumber).dev = dev
        channels(channelNumber).pendingRequest = False
        channels(channelNumber).externalEop = False
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
        If (cmdreg And &H4) <> 0 Then Exit Sub

        ' Ignore triggers if channel 0 is masked
        If (maskreg And 1) = 1 Then Exit Sub

        ' The only sensible mode for channel 0 in a PC is
        ' auto-initialized single read mode, so we simply assume that.

        ' Update count, address and status registers to account for
        ' the past triggers.
        Dim addrstep As Integer = If((cmdreg And &H2) = 0, If((channels(0).mode And &H20) = 0, 1, -1), 0)
        If ntrigger <= channels(0).curcount Then
            ' no terminal count
            Dim n As Integer = CInt(ntrigger)
            channels(0).curcount -= n
            channels(0).curaddr = (channels(0).curaddr + n * addrstep) And &HFFFF
        Else
            ' terminal count occurred
            Dim n As Integer = CInt((ntrigger - channels(0).curcount - 1) Mod (channels(0).basecount + 1))
            channels(0).curcount = channels(0).basecount - n
            channels(0).curaddr = (channels(0).baseaddr + n * addrstep) And &HFFFF
            statusreg = statusreg Or 1
        End If
    End Sub

    Protected Sub TryHandleRequest()
        Dim i As Integer

        ' Update request bits in status register
        Dim rbits As Integer = reqreg
        For i = 0 To 4 - 1
            If channels(i).pendingRequest Then rbits = rbits Or (1 << i)
        Next
        statusreg = (statusreg And &HF) Or (rbits << 4)

        ' Don't start a transfer during dead time after a previous transfer
        If pendingTask Then Exit Sub

        ' Don't start a transfer if the controller is disabled
        If (cmdreg And &H4) <> 0 Then Exit Sub

        ' Select a channel with pending request
        rbits = rbits And (Not maskreg)
        rbits = rbits And (Not 1) ' never select channel 0
        If rbits = 0 Then Exit Sub

        i = priochannel
        While ((rbits >> i) And 1) = 0
            i = (i + 1) And 3
        End While

        ' Just decided to start a transfer on channel i
        Dim chan As Channel = channels(i)
        Dim dev As IDMADevice = chan.dev
        Dim mode As Integer = chan.mode
        Dim page As Integer = chan.page

        ' Update dynamic priority
        If (cmdreg And 10) <> 0 Then priochannel = (i + 1) And 3

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
            Dim curcount As Integer = chan.curcount
            Dim maxlen As Integer = curcount + 1
            Dim curaddr As Integer = chan.curaddr
            Dim addrstep As Integer = If((chan.mode And &H20) = 0, 1, -1)
            chan.externalEop = False

            ' Don't combine too much single transfers in one atomic action
            If singlemode AndAlso maxlen > 25 Then maxlen = 25

            ' Execute transfer
            Select Case (mode And &HC)
                Case &H0
                    ' DMA verify
                    curcount -= maxlen
                    curaddr = (curaddr + maxlen * addrstep) And &HFFFF
                    transferTime += 3 * maxlen * Scheduler.BASECLOCK / cpu.Clock
                Case &H4
                    ' DMA write
                    While (maxlen > 0) AndAlso (Not chan.externalEop) AndAlso (blockmode OrElse chan.pendingRequest)
                        If dev IsNot Nothing Then
                            Dim b As Byte = dev.DMAWrite()
                            cpu.RAM((page << 16) Or curaddr) = b
                        End If
                        maxlen -= 1
                        curcount -= 1
                        curaddr = (curaddr + addrstep) And &HFFFF
                        transferTime += 3 * Scheduler.BASECLOCK / cpu.Clock
                    End While
                Case &H8
                    ' DMA read
                    While maxlen > 0 AndAlso Not chan.externalEop AndAlso (blockmode OrElse chan.pendingRequest)
                        If dev IsNot Nothing Then
                            Dim b As Byte = cpu.RAM((page << 16) Or curaddr)
                            dev.DMARead(b)
                        End If
                        maxlen -= 1
                        curcount -= 1
                        curaddr = (curaddr + addrstep) And &HFFFF
                        transferTime += 3 * Scheduler.BASECLOCK / cpu.Clock
                    End While
            End Select

            ' Update registers
            Dim termcount As Boolean = (curcount < 0)
            chan.curcount = If(termcount, &HFFFF, curcount)
            chan.curaddr = curaddr

            ' Handle terminal count or external EOP
            If termcount OrElse chan.externalEop Then
                If (mode And &H10) = 0 Then
                    ' Set mask bit
                    maskreg = maskreg Or (1 << i)
                Else
                    ' Auto-initialize
                    chan.curcount = chan.basecount
                    chan.curaddr = chan.baseaddr
                End If
                ' Clear software request
                reqreg = reqreg And (Not (1 << i))
                ' Set TC bit in status register
                statusreg = statusreg Or (1 << i)
            End If

            ' Send EOP to device
            If termcount AndAlso (Not chan.externalEop) AndAlso dev IsNot Nothing Then dev.DMAEOP()
        End If

        ' Schedule a task to run when the simulated DMA transfer completes
        cpu.Sched.RunTaskAfter(task, transferTime)
    End Sub

    Public Overrides Function [In](port As UInt32) As UInt32
        UpdateCh0()
        If (port And &HFFF8) = 0 Then
            ' DMA controller: channel status
            Dim chan As Channel = channels((port >> 1) And 3)
            Dim x As Integer = If((port And 1) = 0, chan.curaddr, chan.curcount)
            Dim p As Boolean = msbFlipFlop
            msbFlipFlop = Not p
            Return If(p, (x >> 8) And &HFF, x And &HFF)
        ElseIf (port And &HFFF8) = &H8 Then
            ' DMA controller: operation registers
            Dim v As Integer
            Select Case port
                Case 8 ' read status register
                    v = statusreg
                    statusreg = statusreg And &HF0
                    Return v
                Case 13 ' read temporary register
                    Return tempreg
            End Select
        End If

        Return &HFF
    End Function

    Public Overrides Sub Out(port As UInt32, v As UInt32)
        UpdateCh0()
        If (port And &HFFF8) = 0 Then
            ' DMA controller: channel setup
            Dim chan As Channel = channels((port >> 1) And 3)

            Dim x As Integer
            Dim y As Integer

            If (port And 1) = 0 Then
                ' base/current address
                x = chan.baseaddr
                y = chan.curaddr
            Else
                x = chan.basecount
                y = chan.curcount
            End If
            Dim p As Boolean = msbFlipFlop
            msbFlipFlop = Not p
            If p Then
                x = (x And &HFF) Or ((v << 8) And &HFF00)
                y = (y And &HFF) Or ((v << 8) And &HFF00)
            Else
                x = (x And &HFF00) Or (v And &HFF)
                y = (y And &HFF00) Or (v And &HFF)
            End If
            If (port And 1) = 0 Then
                chan.baseaddr = x
                chan.curaddr = y
            Else
                chan.basecount = x
                chan.curcount = y
            End If
        ElseIf (port And &HFFF8) = &H8 Then
            ' DMA controller: operation registers
            Select Case port And &HF
                Case 8 ' write command register
                    cmdreg = v
                    If (v And &H10) = 0 Then priochannel = 0 ' enable fixed priority
                    If (v And 1) = 1 Then cpu.RaiseException("DMA8237: memory-to-memory transfer not implemented")

                Case 9 ' set/reset request register
                    If ((v And 4) = 0) Then
                        reqreg = reqreg And (Not (1 << (v And 3))) ' reset request bit
                    Else
                        reqreg = reqreg Or (1 << (v And 3))  ' set request bit
                        If ((v And 7) = 4) Then cpu.RaiseException("DMA8237: software request on channel 0 not implemented")
                    End If

                Case 10 ' set/reset mask register
                    If ((v And 4) = 0) Then
                        maskreg = maskreg And (Not (1 << (v And 3))) ' reset mask bit
                    Else
                        maskreg = maskreg Or (1 << (v And 3))  ' set mask bit
                    End If

                Case 11 ' write mode register
                    channels(v And 3).mode = v
                    If ((v And 3) = 0 AndAlso (v And &HDC) <> &H58) Then cpu.RaiseException("DMA8237: unsupported mode on channel 0")

                Case 12 ' clear msb flipflop
                    msbFlipFlop = False

                Case 13 ' master clear
                    msbFlipFlop = False
                    cmdreg = 0
                    statusreg = 0
                    reqreg = 0
                    tempreg = 0
                    maskreg = &HF

                Case 14 ' clear mask register
                    maskreg = 0

                Case 15 ' write mask register
                    maskreg = v
            End Select
            TryHandleRequest()

        ElseIf (port And &HFFF8) = &H80 Then
            ' DMA page registers
            Select Case port
                Case &H81 : channels(2).page = v
                Case &H82 : channels(3).page = v
                Case &H83 : channels(1).page = v
                Case &H87 : channels(0).page = v
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

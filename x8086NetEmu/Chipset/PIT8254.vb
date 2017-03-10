Public Class PIT8254
    Inherits IOPortHandler

    Private Class Counter
        ' Mode (0..5)
        Private countMode As Integer

        ' Count format (1=lsb, 2=msb, 3=lsb+msb)
        Private rwMode As Integer

        ' True when counting in BCD instead of binary
        Private bcdMode As Boolean

        ' Contents of count register
        Private countRegister As Integer

        ' True if next write to count register will set the MSB
        Private countRegisterMsb As Boolean

        ' Contents of output latch
        Private outputLatch As Integer

        ' True if the output value is latched
        Private outputLatched As Boolean

        ' True if the next read from the output latch will get the MSB
        Private outputLatchMsb As Boolean

        ' Status latch register
        Private statusLatch As Integer

        ' True if the status is latched
        Private statusLatched As Boolean

        ' Signal on gate input pin
        Private mGgate As Boolean

        ' True if triggered after the last clock was processed
        Private trigger As Boolean

        ' Internal counter state (lazy)
        Private timeStamp As Long
        Private counterValue As Integer
        Private outputValue As Boolean
        Private nullCount As Boolean
        Private active As Boolean

        Private owner As PIT8254

        ' Constructs and resets counter
        Public Sub New(owner As PIT8254)
            Me.owner = owner

            ' assume no gate signal
            mGgate = False
            ' set undefined mode
            countMode = -1
            outputValue = False
        End Sub

        ' Reprograms counter mode
        Public Sub SetMode(countMode As Integer, countFormat As Integer, bcdMode As Boolean)
            ' set mode
            Me.countMode = countMode
            Me.rwMode = countFormat
            Me.bcdMode = bcdMode
            ' reset registers
            countRegister = 0
            countRegisterMsb = False
            outputLatched = False
            outputLatchMsb = False
            statusLatched = False
            ' reset internal state
            timeStamp = owner.currentTime
            counterValue = 0
            outputValue = If(countMode = 0, False, True)
            nullCount = True
            trigger = False
            active = False
        End Sub

        Public Sub LatchOutput()
            If countMode >= 0 AndAlso Not outputLatched Then
                Update()
                ' copy counter value to output latch
                outputLatch = counterValue
                outputLatched = True
                outputLatchMsb = False
            End If
        End Sub

        Public Sub LatchStatus()
            If countMode >= 0 AndAlso Not statusLatched Then
                Update()
                ' fill status latch register:
                ' bit7   = output
                ' bit6   = nullcount
                ' bit4-5 = rwMode
                ' bit1-3 = countMode
                ' bit0   = bcdMode
                statusLatch =
                  If(outputValue, &H80, &H0) Or
                  If(nullCount, &H40, &H0) Or
                  (rwMode << 4) Or
                  (countMode << 1) Or
                  If(bcdMode, &H1, &H0)
                statusLatched = True
            End If
        End Sub

        Public Function GetByte()
            If countMode < 0 Then Return &HFF ' undefined state

            If statusLatched Then
                ' read status latch register
                statusLatched = False
                Return statusLatch
            End If

            If Not outputLatched Then
                ' output latch directly follows counter
                Update()
                outputLatch = counterValue
            End If

            ' read output latch register
            Select Case rwMode
                Case 1 ' LSB only
                    outputLatched = False
                    Return outputLatch And &HFF
                Case 2 ' MSB only
                    outputLatched = False
                    Return outputLatch >> 8
                Case 3 ' LSB followed by MSB
                    If outputLatchMsb Then
                        outputLatched = False
                        outputLatchMsb = False
                        Return outputLatch >> 8
                    Else
                        outputLatchMsb = True
                        Return outputLatch And &HFF
                    End If
                Case Else ' cannot happen
                    Throw New Exception("PIT8254: Invalid GetByte")
            End Select
        End Function

        Public Sub PutByte(v As Integer)
            If (countMode < 0) Then Return ' undefined state

            ' write to count register
            Select Case rwMode
                Case 1 ' LSB only
                    countRegister = v And &HFF
                    ChangeCount()
                Case 2 ' MSB only
                    countRegister = (v << 8) And &HFF00
                    ChangeCount()
                Case 3 ' LSB followed by MSB
                    If countRegisterMsb Then
                        countRegister = (countRegister And &HFF) Or ((v << 8) And &HFF00)
                        countRegisterMsb = False
                        ChangeCount()
                    Else
                        countRegister = (countRegister And &HFF00) Or (v And &HFF)
                        countRegisterMsb = True
                    End If
            End Select
        End Sub

        Public Property Gate As Boolean
            Get
                Return mGgate
            End Get
            Set(value As Boolean)
                If countMode >= 0 Then Update()
                ' trigger on rising edge of the gate signal
                If value AndAlso (Not mGgate) Then trigger = True
                mGgate = value
                ' mode 2 and mode 3: when gate goes low, output
                ' is set high immediately
                If ((Not mGgate) AndAlso ((countMode = 2) OrElse (countMode = 3))) Then outputValue = True
            End Set
        End Property

        ' Returns current output state
        Public Function GetOutput() As Boolean
            If (countMode >= 0) Then Update()
            Return outputValue
        End Function

        ' Returns the time when the output state will change,
        ' or returns 0 if the output will not change spontaneously.
        Public Function NextOutputChangeTime() As Long
            If countMode < 0 Then Return 0
            Dim clocks As Integer = 0
            Update()
            Select Case countMode
                Case 0
                    ' output goes high on terminal count
                    If active AndAlso mGgate AndAlso (Not outputValue) Then clocks = FromCounter(counterValue) + If(nullCount, 1, 0)
                Case 1
                    ' output goes high on terminal count
                    If (Not outputValue) Then clocks = FromCounter(counterValue) + If(trigger, 1, 0)
                    ' output goes low on next clock after trigger
                    If outputValue AndAlso trigger Then clocks = 1
                Case 2
                    ' output goes high on reaching one
                    If active AndAlso mGgate AndAlso outputValue Then clocks = FromCounter(counterValue) + If(trigger, 0, -1)
                    ' strobe ends on next clock
                    If Not outputValue Then clocks = 1
                Case 3
                    ' trigger pulls output high
                    If (Not outputValue) AndAlso trigger Then clocks = 1
                    ' output goes low on reaching zero
                    If active AndAlso mGgate AndAlso outputValue Then clocks = FromCounter(counterValue) / 2 + If(trigger, 1, 0) + (countRegister And 1)
                    ' output goes high on reaching zero
                    If active AndAlso mGgate AndAlso (Not outputValue) AndAlso (Not trigger) Then clocks = FromCounter(counterValue) / 2
                Case 4
                    ' strobe starts on terminal count
                    If active AndAlso mGgate AndAlso outputValue Then clocks = FromCounter(counterValue) + If(nullCount, 1, 0)
                    ' strobe ends on next clock
                    If Not outputValue Then clocks = 1
                Case 5
                    ' strobe starts on terminal count
                    If active AndAlso outputValue Then clocks = FromCounter(counterValue)
                    ' strobe ends on next clock
                    If Not outputValue Then clocks = 1
            End Select

            If clocks = 0 Then
                Return 0
            Else
                Return ClocksToTime(TimeToClocks(owner.currentTime) + clocks)
            End If
        End Function

        ' Returns the full period for mode 3 (square wave),
        ' or returns 0 in other modes.
        Public Function GetSquareWavePeriod() As Long
            If (countMode <> 3) OrElse (Not active) OrElse (Not mGgate) Then Return 0
            Update()
            Return ClocksToTime(FromCounter(countRegister))
        End Function

        ' Returns the full period, or 0 if not enabled.
        Public Function GetPeriod() As Long
            If (Not active) OrElse (Not mGgate) Then Return 0
            Update()
            Return ClocksToTime(FromCounter(countRegister))
        End Function

        ' Converts an internal counter value to a number,
        ' wrapping the zero value to the maximum value.
        Private Function FromCounter(v As Integer) As Integer
            If v = 0 Then
                Return If(bcdMode, 10000, &H10000)
            ElseIf bcdMode Then
                Return ((v >> 12) And &HF) * 1000 +
                        ((v >> 8) And &HF) * 100 +
                        ((v >> 4) And &HF) * 10 +
                        (v And &HF)
            Else
                Return v
            End If
        End Function

        ' Converts a number to an internal counter value,
        ' using zero to represent the maximum counter value.
        Private Function ToCounter(v As Integer) As Integer
            If bcdMode Then
                v = v Mod 10000
                Return ((v \ 1000) Mod 10) << 12 Or
                        ((v \ 100) Mod 10) << 8 Or
                         ((v \ 10) Mod 10) << 4 Or
                           (v Mod 10)
            Else
                Return v Mod &H10000
            End If
        End Function

        ' Substracts c from the counter and
        ' returns true if the zero value was reached.
        Private Function CountDown(c As Long) As Boolean
            Dim zero As Boolean
            If bcdMode Then
                Dim v As Integer = ((counterValue >> 12) And &HF) * 1000 +
                                    ((counterValue >> 8) And &HF) * 100 +
                                    ((counterValue >> 4) And &HF) * 10 +
                                    (counterValue And &HF)
                zero = (c >= 10000 OrElse (v <> 0 AndAlso c >= v))
                v += 10000 - (c Mod 10000)
                counterValue =
                  ((v \ 1000) Mod 10) << 12 Or
                   ((v \ 100) Mod 10) << 8 Or
                    ((v \ 10) Mod 10) << 4 Or
                      (v Mod 10)
            Else
                zero = (c > &HFFFF OrElse (counterValue <> 0 AndAlso c >= counterValue))
                counterValue = (counterValue - c) And &HFFFF
            End If

            Return zero
        End Function

        ' Recomputes the internal state of the counter at the
        ' current time from the last computed state.
        Private Sub Update()
            ' compute elapsed clock pulses since last update
            Dim clocks As Long = TimeToClocks(owner.currentTime) - TimeToClocks(timeStamp)

            ' call mode-dependent update function
            Select Case countMode
                Case 0 : UpdMode0(clocks)
                Case 1 : UpdMode1(clocks)
                Case 2 : UpdMode2(clocks)
                Case 3 : UpdMode3(clocks)
                Case 4 : UpdMode4(clocks)
                Case 5 : UpdMode5(clocks)
            End Select
            ' put timestamp on new state
            trigger = False
            timeStamp = owner.currentTime
        End Sub

        ' MODE 0 - INTERRUPT ON TERMINAL COUNT
        Private Sub UpdMode0(clocks As Long)
            ' init:      output low, stop counter
            ' set count: output low, start counter
            ' on zero:   output high, counter wraps
            If active AndAlso nullCount Then
                ' load counter on next clock after writing
                counterValue = countRegister
                nullCount = False
                clocks -= 1
            End If
            If clocks < 0 Then Exit Sub
            If active AndAlso mGgate Then
                ' count down, zero sets output high
                If CountDown(clocks) Then outputValue = True
            End If
        End Sub

        ' MODE 1 - HARD-TRIGGERED ONE-SHOT
        Private Sub UpdMode1(clocks As Long)
            ' init:      output high, counter running
            ' set count: nop
            ' trigger:   load counter, output low
            ' on zero:   output high, counter wraps
            If trigger Then
                ' load counter on next clock after trigger
                counterValue = countRegister
                nullCount = False
                outputValue = False
                clocks -= 1
            End If
            ' count down, zero sets output high
            If clocks < 0 Then Return
            If CountDown(clocks) Then outputValue = True
        End Sub

        ' MODE 2 - RATE GENERATOR
        Private Sub UpdMode2(clocks As Long)
            ' init:      output high, stop counter
            ' initial c: load and start counter
            ' trigger:   reload counter
            ' on one:    output strobes low
            ' on zero:   reload counter
            If (trigger) Then
                ' load counter on trigger
                counterValue = countRegister
                nullCount = False
                clocks -= 1
            End If
            If clocks < 0 Then Exit Sub
            If active AndAlso mGgate Then
                ' count down
                Dim v As Integer = FromCounter(counterValue)
                If clocks < v Then
                    v -= clocks
                Else
                    ' zero reached, reload counter
                    clocks -= v
                    v = FromCounter(countRegister)
                    v -= clocks Mod v
                    nullCount = False
                End If
                counterValue = ToCounter(v)
            End If
            ' output strobes low on decrement to 1
            outputValue = (Not mGgate OrElse counterValue <> 1)
        End Sub

        '  MODE 3 - SQUARE WAVE
        Private Sub UpdMode3(clocks As Long)
            '  init:      output high, stop counter
            '  initial c: load and start counter
            '  trigger:   reload counter
            '  on one:    switch phase, reload counter
            If trigger Then
                '  load counter on trigger
                counterValue = countRegister And (Not 2)
                nullCount = False
                outputValue = True
                clocks -= 1
            End If
            If clocks < 0 Then Return
            If active AndAlso mGgate Then
                '  count down
                Dim v As Integer = FromCounter(counterValue)
                If (counterValue = 0) AndAlso outputValue AndAlso ((countRegister And 1) <> 0) Then v = 0
                If 2 * clocks < v Then
                    v -= 2 * clocks
                Else
                    '  zero reached, reload counter
                    clocks -= v / 2
                    v = FromCounter(countRegister)
                    Dim c As Integer = clocks Mod v
                    v = v And (Not 2)
                    nullCount = False
                    If Not outputValue Then
                        '  zero reached in low phase
                        '  switch to high phase
                        outputValue = True
                        '  continue counting
                        If 2 * c < v Then
                            v -= 2 * c
                            counterValue = ToCounter(v)
                            Exit Sub
                        End If
                        c -= v / 2
                    End If
                    '  zero reached in high phase
                    If (countRegister And 1) <> 0 Then
                        '  wait one more clock
                        If clocks = 0 Then
                            counterValue = 0
                            Exit Sub
                        End If
                        clocks -= 1
                    End If
                    '  switch to low phase
                    outputValue = False
                    '  continue counting
                    If 2 * c >= v Then
                        '  zero reached again
                        c -= v / 2
                        '  switch to high phase
                        outputValue = True
                    End If
                    '  continue counting
                    v -= 2 * c
                End If
                counterValue = ToCounter(v)
            End If
        End Sub

        ' MODE 4 - SOFT-TRIGGERED STROBE
        Private Sub UpdMode4(clocks As Long)
            '  init:      output high, counter running
            '  set count: load counter
            '  on zero:   output strobes low, counter wraps
            If (active AndAlso nullCount) Then
                '  load counter on first clock
                counterValue = countRegister
                nullCount = False
                clocks -= 1
            End If
            If clocks < 0 Then Exit Sub
            If mGgate Then
                '  count down
                CountDown(clocks)
                '  output strobes low on zero
                outputValue = (Not active OrElse counterValue <> 0)
            Else
                '  end previous strobe
                outputValue = True
            End If
        End Sub

        '  MODE 5 - HARD-TRIGGERED STROBE
        Private Sub UpdMode5(clocks As Long)
            '  init:      output high, counter running
            '  set count: nop
            '  trigger:   reload counter
            '  on zero:   output strobes low, counter wraps
            outputValue = True
            If trigger Then
                '  load counter on trigger
                counterValue = countRegister
                nullCount = False
                active = True
                clocks -= 1
            End If
            If (clocks < 0) Then Return
            '  count down
            CountDown(clocks)
            '  output strobes low on zero
            outputValue = (Not active OrElse counterValue <> 0)
        End Sub

        ' Called when a new count is written to the Count Register
        Private Sub ChangeCount()
            Update()
            If countMode = 0 Then
                ' mode 0 is restarted by writing a count
                outputValue = False
            Else
                ' modes 2 and 3 are soft-triggered by
                ' writing the initial count
                If Not active Then trigger = True
            End If
            nullCount = True
            ' mode 5 is only activated by a trigger
            If countMode <> 5 Then active = True
        End Sub
    End Class

    ' Global counter clock rate (1.19318 MHz) 
    Public Const COUNTRATE As Long = 1.19318 * x8086.MHz

    ' Three counters in the I8254 chip 
    Private channels(3 - 1) As Counter

    ' Interrupt request line for channel 0 
    Private irq As InterruptRequest

    ' Speaker Adapter connected to channel 2
    Private mSpeaker As SpeakerAdpater

    ' Current time mirrored from Scheduler
    Private currentTime As Long

    Private cpu As x8086

    Private Class TaskSC
        Inherits Scheduler.Task

        Public Sub New(owner As IOPortHandler)
            MyBase.New(owner)
        End Sub

        Public Overrides Sub Run()
            Owner.Run()
        End Sub

        Public Overrides ReadOnly Property Name As Object
            Get
                Return Owner.Name
            End Get
        End Property
    End Class
    Private task As Scheduler.Task = New TaskSC(Me)

    Public Sub New(cpu As x8086, irq As InterruptRequest)
        Me.cpu = cpu
        Me.irq = irq
        Me.currentTime = cpu.Sched.CurrentTime

        ' construct 3 timer channels
        channels(0) = New Counter(Me)
        channels(1) = New Counter(Me)
        channels(2) = New Counter(Me)

        ' gate input for channels 0 and 1 is always high
        channels(0).Gate = True
        channels(1).Gate = True

        For i As Integer = &H40 To &H43
            ValidPortAddress.Add(i)
        Next
    End Sub

    Public Function GetOutput(c As Integer) As Boolean
        Return channels(c).GetOutput()
    End Function

    Public Sub SetCh2Gate(v As Boolean)
        currentTime = cpu.Sched.CurrentTime
        channels(2).Gate = v
        UpdateCh2(0)
    End Sub

    Public Overrides Function [In](port As Integer) As Integer
        currentTime = cpu.Sched.CurrentTime
        Dim c As Integer = port And 3
        If c = 3 Then
            ' invalid read
            Return &HFF
        Else
            ' read from counter
            Return channels(c).GetByte()
        End If
    End Function

    Public Overrides Sub Out(port As Integer, v As Integer)
        currentTime = cpu.Sched.CurrentTime
        Dim c As Integer = port And 3
        If c = 3 Then
            '  write Control Word
            Dim s As Integer
            c = (v >> 6) And 3
            If c = 3 Then
                '  Read Back command
                For i As Integer = 0 To 3 - 1
                    s = (2 << i)
                    If (v And (&H10 Or s)) = s Then channels(i).LatchStatus()
                    If (v And (&H20 Or s)) = s Then channels(i).LatchOutput()
                Next
            Else
                '  Channel Control Word
                If (v And &H30) = 0 Then
                    '  Counter Latch command
                    channels(c).LatchOutput()
                Else
                    '  reprogram counter mode
                    Dim countm As Integer = (v >> 1) And 7
                    If countm > 5 Then countm = countm And 3
                    Dim rwm As Integer = (v >> 4) And 3
                    Dim bcdm As Boolean = (v And 1) <> 0
                    channels(c).SetMode(countm, rwm, bcdm)
                    Select Case c
                        Case 0 : UpdateCh0()
                        Case 1 : UpdateCh1()
                        Case 2 : UpdateCh2(v)
                    End Select
                End If
            End If
        Else
            '  write to counter
            channels(c).PutByte(v)
            Select Case c
                Case 0 : UpdateCh0()
                Case 1 : UpdateCh1()
                Case 2 : UpdateCh2(v)
            End Select
        End If
    End Sub

    Private Sub UpdateCh0()
        ' State of channel 0 may have changed;
        '' run the IRQ task immediately to take this into account
        If irq IsNot Nothing Then
            task.Cancel()
            task.Start()
        End If
    End Sub

    Private Sub UpdateCh1()
        ' Notify the DMA controller of the new frequency
        If cpu.DMA IsNot Nothing Then cpu.DMA.SetCh0Period(channels(1).GetPeriod())
    End Sub

    Private Sub UpdateCh2(v As Integer)
        'If cpu.PPI IsNot Nothing Then
        '    If cpu.Model = x8086.Models.PCE_IBMPC_5150 Then
        '        If v <> 0 Then
        '            cpu.PPI.PortC(0) = cpu.PPI.PortC(0) Or &H20
        '            cpu.PPI.PortC(1) = cpu.PPI.PortC(1) Or &H20
        '        Else
        '            cpu.PPI.PortC(0) = cpu.PPI.PortC(0) And (Not &H20)
        '            cpu.PPI.PortC(1) = cpu.PPI.PortC(1) And (Not &H20)
        '        End If
        '    End If
        'End If

#If Win32 Then
        If mSpeaker IsNot Nothing Then
            Dim period As Long = channels(2).GetSquareWavePeriod()
            If period = 0 Then
                mSpeaker.Frequency = 0
            Else
                mSpeaker.Frequency = COUNTRATE / period * 1000
            End If
        End If
#End If
    End Sub

    Public Shared Function TimeToClocks(t As Long) As Long
        Return (t \ Scheduler.CLOCKRATE) * COUNTRATE +
               ((t Mod Scheduler.CLOCKRATE) * COUNTRATE) \ Scheduler.CLOCKRATE
    End Function

    Public Shared Function ClocksToTime(c As Long) As Long
        Return (c \ COUNTRATE) * Scheduler.CLOCKRATE +
               ((c Mod COUNTRATE) * Scheduler.CLOCKRATE + COUNTRATE - 1) \ COUNTRATE
    End Function

    Public Property Speaker As SpeakerAdpater
        Get
            Return mSpeaker
        End Get
        Set(value As SpeakerAdpater)
            mSpeaker = value
        End Set
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Programmable Interval Timer"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "8254"
        End Get
    End Property

    Private lastValue As Boolean = False
    ' Scheduled task to drive IRQ 0 based on counter 0 output signal
    Public Overrides Sub Run()
        currentTime = cpu.Sched.CurrentTime
        ' set IRQ 0 signal equal to counter 0 output
        Dim s As Boolean = channels(0).GetOutput()
        If s <> lastValue Then
            irq.Raise(s)
            lastValue = s
        End If

        ' reschedule task for next output change
        Dim t As Long = channels(0).NextOutputChangeTime()
        If t > 0 Then cpu.Sched.RunTaskAt(task, t)
    End Sub
End Class

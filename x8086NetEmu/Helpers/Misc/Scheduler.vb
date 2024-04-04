Imports System.Threading

' This code is a port from the "Scheduler" class in Retro 0.4

Public Class Scheduler
    Implements IExternalInputHandler

    Private Const NOTASK As Long = Long.MaxValue
    Private Const STOPPING As Long = Long.MinValue

    ' Number of scheduler time units per simulated second (~1.0 GHz)
    Public Shared HOSTCLOCK As ULong = 1_193_182

    ' Current simulation time in scheduler time units (ns)
    Private mCurrentTime As Long

    ' Scheduled time of next event, or NOTASK or STOPPING
    Private nextTime As Long

    ' Enables slowing the simulation to keep it in sync with wall time
    Private syncScheduler As Boolean = True

    ' Determines how often the time synchronization is checked
    Private syncQuantum As Long

    ' Determines speed of the simulation
    Private syncSimTimePerWallMs As Long

    ' Gain on wall time since last synchronization, plus one syncQuantum
    Private syncTimeSaldo As Long

    ' Most recent value of <code>currentTimeMs</code>
    Private syncWallTimeMs As Long

    ' Queue containing pending synchronous events
    Private pq As PriorityQueue

    ' Ordered list of pending asynchronous events (external input events)
    Public pendingInput As ArrayList

    ' The CPU component controlled by this Scheduler
    Private ReadOnly mCPU As X8086

    Private isCtrlDown As Boolean
    Private isAltDown As Boolean
    Private cadCounter As Integer

    ' The dispatcher for external input events
    'Private inputHandler As ExternalInputHandler

    ' A Task represents a pending discrete event, and is queued for
    ' execution at a particular point in simulated time.
    Public MustInherit Class SchTask
        Inherits Runnable
        Public Const NOSCHED As Long = Long.MinValue
        Public Property LastTime As Long
        Public Property NextTime As Long
        Public Property Interval As Long

        'Private mThread As Thread
        Private mOwner As IOPortHandler

        Public ReadOnly Property Owner As IOPortHandler
            Get
                Return mOwner
            End Get
        End Property

        Public Sub New(owner As IOPortHandler)
            mOwner = owner
            LastTime = NOSCHED
            NextTime = NOSCHED
            Interval = 0
        End Sub

        Public Function Cancel() As Boolean
            If NextTime = NOSCHED Then Return False
            NextTime = NOSCHED
            Interval = 0
            Return True
        End Function

        Public Function LastExecutionTime() As Long
            Return LastTime
        End Function

        Public Sub Start()
            Me.Run()
        End Sub

        Public MustOverride Overrides Sub Run()
    End Class

    Public Sub New(cpu As X8086)
        mCPU = cpu
        pq = New PriorityQueue()
        pendingInput = New ArrayList()

        syncQuantum = HOSTCLOCK \ 20
        syncSimTimePerWallMs = HOSTCLOCK \ 1000
    End Sub

    Public ReadOnly Property CurrentTime As Long
        Get
            Return mCurrentTime
        End Get
    End Property

    Private ReadOnly Property CurrentTimeMs As Long
        Get
            Return Stopwatch.GetTimestamp()
        End Get
    End Property

    Public Sub SetSynchronization(enabled As Boolean, quantum As Long, simTimePerWallMs As Long)
#If DEBUG Then
        If enabled And quantum < 1 Then Throw New ArgumentException("Invalid quantum value")
        If enabled And simTimePerWallMs < 1000 Then Throw New ArgumentException("Invalid simTimePerWallMs value")
#End If

        syncScheduler = enabled
        syncQuantum = quantum
        syncSimTimePerWallMs = simTimePerWallMs
        syncTimeSaldo = 0
        syncWallTimeMs = CurrentTimeMs
    End Sub

    Public Function RunTaskAt(tsk As SchTask, t As Long) As Boolean
        SyncLock tsk
#If DEBUG Then
            If tsk.NextTime <> SchTask.NOSCHED Then
                'Throw New Exception("Task already scheduled")
                Return False
            End If
#End If
            tsk.NextTime = t
        End SyncLock

        pq.Add(tsk, t)
        If t < nextTime Then nextTime = t

        Return True
    End Function

    Public Sub RunTaskAfter(tsk As SchTask, d As Long)
        Dim t As Long = mCurrentTime + d

        SyncLock tsk
#If DEBUG Then
            If tsk.NextTime <> SchTask.NOSCHED Then Throw New Exception("Task already scheduled")
#End If
            tsk.NextTime = t
        End SyncLock

        pq.Add(tsk, t)
        If t < nextTime Then nextTime = t
    End Sub

    Public Sub RunTaskEach(tsk As SchTask, interval As Long)
        Dim t As Long = mCurrentTime + interval

        SyncLock tsk
#If DEBUG Then
            If tsk.NextTime <> SchTask.NOSCHED Then Throw New Exception("Task already scheduled")
#End If
            tsk.NextTime = t
            tsk.Interval = interval
        End SyncLock

        pq.Add(tsk, t)
        If t < nextTime Then nextTime = t
    End Sub

    Public Sub [Stop]()
        Dim tsk As SchTask
        Do
            tsk = CType(pq.RemoveFirst(), SchTask)
            If tsk Is Nothing Then Exit Do
            tsk.Cancel()
        Loop

        pq.Clear()
        nextTime = STOPPING

        ' Kick simulation thread
        mCPU.DoReschedule = True
    End Sub

    Public Function GetTimeToNextEvent() As Long
        If nextTime = STOPPING OrElse pendingInput.Count <> 0 Then
            Return 0
        ElseIf syncScheduler AndAlso (nextTime > mCurrentTime + syncQuantum) Then
            Return syncQuantum
        Else
            Return nextTime - mCurrentTime
        End If
    End Function

    Public Sub AdvanceTime(t As Long)
        mCurrentTime += t
        If syncScheduler Then
            syncTimeSaldo += t
            If syncTimeSaldo > 3 * syncQuantum Then
                ' Check the wall clock
                Dim wallTime As Long = CurrentTimeMs
                Dim wallDelta As Long = wallTime - syncWallTimeMs
                syncWallTimeMs = wallTime
                If wallDelta < 0 Then wallDelta = 0 ' Some clown has set the system clock back
                syncTimeSaldo -= wallDelta * syncSimTimePerWallMs
                If syncTimeSaldo < 0 Then syncTimeSaldo = 0
                If syncTimeSaldo > 2 * syncQuantum Then
                    ' The simulation has gained more than one time quantum
                    Dim sleepTime As Integer = (syncTimeSaldo - syncQuantum) \ syncSimTimePerWallMs
                    If syncTimeSaldo > 4 * syncQuantum Then
                        ' Force a hard sleep
                        Dim s As Integer = syncQuantum / syncSimTimePerWallMs
                        Thread.Sleep(s)
                        sleepTime -= s
                    End If

                    SyncLock Me
                        ' Sleep, but wake up on asynchronous events
                        If pendingInput.Count = 0 Then Wait(sleepTime)
                    End SyncLock
                End If
            End If
        End If
    End Sub

    Public Sub SkipToNextEvent()
        If nextTime <= mCurrentTime OrElse pendingInput.Count <> 0 Then Exit Sub

        ' Detect end of simulation
        If nextTime = NOTASK Then nextTime = STOPPING

        If syncScheduler Then
            If nextTime <> STOPPING Then syncTimeSaldo += nextTime - mCurrentTime
            If syncTimeSaldo > 3 * syncQuantum Then
                ' Check the wall clock
                Dim wallTime As Long = CurrentTimeMs
                Dim wallDelta As Long = wallTime - syncWallTimeMs
                syncWallTimeMs = wallTime
                If wallDelta < 0 Then wallDelta = 0 ' some clown has set the system clock back
                syncTimeSaldo -= wallDelta * syncSimTimePerWallMs
                If syncTimeSaldo < 0 Then syncTimeSaldo = 0
                If syncTimeSaldo > 2 * syncQuantum Then
                    ' Skipping would give a gain of more than one time quantum
                    Dim sleepTime As Integer = (syncTimeSaldo - syncQuantum) \ syncSimTimePerWallMs
                    Wait(sleepTime)
                    If pendingInput.Count > 0 Then
                        ' We woke up from our sleep; find out how long
                        '   we slept and how much simulated time has passed
                        wallTime = CurrentTimeMs
                        wallDelta = wallTime - syncWallTimeMs
                        syncWallTimeMs = wallTime
                        If wallDelta < 0 Then wallDelta = 0 ' Same clown again
                        syncTimeSaldo -= wallDelta * syncSimTimePerWallMs
                        If syncTimeSaldo > syncQuantum + nextTime - mCurrentTime Then
                            ' No simulated time passed at all
                            syncTimeSaldo -= nextTime - mCurrentTime
                        ElseIf syncTimeSaldo > syncQuantum Then
                            ' Some simulated time passed, but not enough
                            mCurrentTime = nextTime - (syncTimeSaldo - syncQuantum)
                            syncTimeSaldo = syncQuantum
                        Else
                            ' Oops, we even overslept
                            mCurrentTime = nextTime
                        End If
                    Else
                        ' Assume we slept the whole interval
                        mCurrentTime = nextTime
                    End If
                    Exit Sub
                End If
            End If
        End If

        ' Skip to the next pending event
        mCurrentTime = nextTime
    End Sub

    Public Function NextTask() As SchTask
        If nextTime > mCurrentTime OrElse nextTime = STOPPING Then Return Nothing

        Dim tsk As SchTask = CType(pq.RemoveFirst(), SchTask)
        nextTime = pq.MinPriority()
        If tsk Is Nothing Then Return Nothing

        SyncLock tsk
            If (tsk.NextTime = SchTask.NOSCHED) OrElse (tsk.NextTime > mCurrentTime) Then
                ' Canceled or rescheduled
                tsk = Nothing
            Else
                ' Task is ok to run
                tsk.LastTime = tsk.NextTime
                If tsk.Interval > 0 Then
                    ' Schedule next execution
                    Dim t As Long = tsk.NextTime + tsk.Interval
                    tsk.NextTime = t
                    pq.Add(tsk, t)
                    If t < nextTime Then nextTime = t
                Else
                    ' Done with this task
                    tsk.NextTime = SchTask.NOSCHED
                End If
            End If
        End SyncLock

        Return tsk
    End Function

    Public Sub Start()
        mCurrentTime = 0
        nextTime = NOTASK
        syncWallTimeMs = CurrentTimeMs
        syncTimeSaldo = 0

        Tasks.Task.Run(AddressOf Run)
    End Sub

    Private Sub Run()
        Dim cleanInputBuf As New ArrayList()
        Dim inputBuf As New ArrayList()
        Dim tsk As SchTask = Nothing
        Dim evt As ExternalInputEvent

        Do
            ' Detect the end of the simulation run
            If nextTime = STOPPING Then
                nextTime = pq.MinPriority()
                Exit Do
            End If

            If pendingInput.Count > 0 Then
                ' Fetch pending input events
                inputBuf = pendingInput
                pendingInput = cleanInputBuf
            ElseIf nextTime <= mCurrentTime Then
                ' Fetch the next pending task
                tsk = NextTask()
                If tsk Is Nothing Then Continue Do ' This task was canceled, go round again
                inputBuf.Clear()
            Else
                tsk = Nothing
            End If

            If inputBuf.Count > 0 Then
                ' Process pending input events
                For i As Integer = 0 To inputBuf.Count - 1
                    evt = CType(inputBuf.Item(i), ExternalInputEvent)
                    evt.TimeStamp = mCurrentTime
                    evt.Handler.HandleInput(evt)
                Next
                inputBuf.Clear()
                cleanInputBuf = inputBuf
            ElseIf tsk IsNot Nothing Then
                ' Run the first pending task
                tsk.Start()
            Else
                ' Run the CPU simulation for a bit (maxRunCycl)
                Try
                    mCPU.RunEmulation()
                Catch ex As Exception
                    X8086.Notify("Shit happens at {0}:{1}: {2}", X8086.NotificationReasons.Fck,
                                                                mCPU.Registers.CS.ToString("X4"),
                                                                mCPU.Registers.IP.ToString("X4"),
                                                                ex.Message)
                    mCPU.RaiseException($"Scheduler Main Loop Error: {ex.Message}")
                End Try

                If mCPU.IsHalted() Then SkipToNextEvent() ' The CPU is halted, skip immediately to the next event
            End If
        Loop Until X8086.IsClosing
    End Sub

    Private Sub Wait(delay As Integer)
        Monitor.Enter(Me)
        Monitor.Wait(Me, delay)
        Monitor.Exit(Me)
    End Sub

    Private Sub Notify()
        Monitor.Enter(Me)
        Monitor.PulseAll(Me)
        Monitor.Exit(Me)
    End Sub

    Public Sub HandleInput(e As ExternalInputEvent) Implements IExternalInputHandler.HandleInput
        If e.Handler Is Nothing Then Exit Sub

        If TypeOf e.Event Is KeyEventArgs Then
            Dim theEvent = CType(e.Event, KeyEventArgs)

            If cadCounter > 0 Then
                cadCounter -= 1
                Exit Sub
            End If

            If (theEvent.Modifiers And Keys.Control) = Keys.Control Then
                isCtrlDown = Not CType(e.Extra, Boolean)
            Else
                isCtrlDown = False
            End If
            If (theEvent.Modifiers And Keys.Alt) = Keys.Alt Then
                isAltDown = Not CType(e.Extra, Boolean)
            Else
                isAltDown = False
            End If

            If isCtrlDown AndAlso isAltDown AndAlso (theEvent.KeyCode And Keys.Insert) = Keys.Insert Then
                cadCounter = 3 ' Ignore the next three events, which will be the release of CTRL, ALT and DEL
                e.Event = New KeyEventArgs(Keys.Delete)
                X8086.Notify("Sending CTRL+ALT+DEL", X8086.NotificationReasons.Info)
            End If
        End If

        If pendingInput.Count = 0 Then
            ' Wake up the scheduler in case it is sleeping
            Notify()
            ' Kick the CPU simulation to make it yield
            mCPU.DoReschedule = True
        End If

        pendingInput.Add(e)
    End Sub
End Class

Public Interface IExternalInputHandler
    Sub HandleInput(e As ExternalInputEvent)
End Interface

Public Class ExternalInputEvent
    Inherits EventArgs

    Public Property Handler As IExternalInputHandler
    Public Property [Event] As EventArgs
    Public Property TimeStamp As Long
    Public Property Extra As Object

    Public Sub New(handler As IExternalInputHandler, theEvent As EventArgs)
        Me.Handler = handler
        Me.Event = theEvent
    End Sub

    Public Sub New(handler As IExternalInputHandler, theEvent As EventArgs, extra As Object)
        Me.Handler = handler
        Me.Event = theEvent
        Me.Extra = extra
    End Sub

    'Public Shared Operator =(e1 As ExternalInputEvent, e2 As ExternalInputEvent) As Boolean
    '    Dim e1k = CType(e1.TheEvent, KeyEventArgs)
    '    Dim e2k = CType(e2.TheEvent, KeyEventArgs)
    '    Return e1k.KeyCode = e2k.KeyCode AndAlso
    '            e1k.Modifiers = e2k.Modifiers
    'End Operator

    'Public Shared Operator <>(e1 As ExternalInputEvent, e2 As ExternalInputEvent) As Boolean
    '    Return Not (e1 = e2)
    'End Operator
End Class

Public MustInherit Class Runnable
    Public MustOverride ReadOnly Property Name As String
    Public MustOverride Sub Run()
End Class
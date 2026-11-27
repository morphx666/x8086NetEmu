Imports System.Runtime.InteropServices
Imports ManagedBass

Public Class AudioSubsystem
    Inherits IOPortHandler
    Implements IDisposable

    Private handle As Integer
    Private ReadOnly mProviders As New List(Of AudioProvider)

    ' Samples are produced by the scheduler thread at exactly SampleRate per *simulated* second
    ' and consumed by BASS at SampleRate per *wall* second. Since the scheduler keeps simulated
    ' time locked to wall time, both rates match on average; this ring buffer absorbs the jitter
    ' (the scheduler runs in bursts of up to a few sync quanta ahead of wall time).
    Private ReadOnly ring As Byte() = New Byte(SpeakerAdapter.SampleRate - 1) {} ' 1s capacity
    Private readPos As Integer
    Private writePos As Integer
    Private buffered As Integer
    Private ReadOnly ringLock As New Object()
    Private lastSample As Byte = 128
    Private primed As Boolean

    ' Amount of audio to accumulate before (re)starting playback after an underrun
    Private Const PreRollMs As Integer = 60
    ' If the buffer grows beyond this (emulation speed > 100%, host hiccups, etc.), drop the
    ' oldest samples to keep audio latency bounded
    Private Const MaxLatencyMs As Integer = 250
    Private ReadOnly preRollSamples As Integer = SpeakerAdapter.SampleRate * PreRollMs \ 1000
    Private ReadOnly maxBufferedSamples As Integer = SpeakerAdapter.SampleRate * MaxLatencyMs \ 1000

    ' Exact sample clock: sample N of the current second is due at
    ' secondStartTime + N * HOSTCLOCK / SampleRate (no accumulated rounding error)
    Private secondStartTime As Long
    Private sampleIndex As Integer

    Private cpu As X8086

    Private Class TaskSC
        Inherits Scheduler.SchTask

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
    Private sTask As New TaskSC(Me)

    Public Sub New(cpu As X8086)
        Me.cpu = cpu
    End Sub

    ' The output stream is opened when the machine starts running (not on construction), so an X8086 that is
    ' never run (e.g. one used only to disassemble code) doesn't open the audio device
    Private Sub OpenStream()
        If handle <> 0 Then Exit Sub
        BassHelpers.Setup()

        fillProc = AddressOf FillAudioBuffer ' Keep the delegate alive while native code holds it
        handle = Bass.CreateStream(SpeakerAdapter.SampleRate, 1, BassFlags.Byte, fillProc, IntPtr.Zero)
        Bass.ChannelSetAttribute(handle, ChannelAttribute.Buffer, 0)
        Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, 1.0)
        Bass.ChannelPlay(handle)
    End Sub
    Private fillProc As StreamProcedure

    ' Scheduler thread: mix one sample from all providers, then schedule the next one
    Public Overrides Sub Run()
        Dim mixed As Integer = 0
        For Each provider In mProviders
            mixed += provider.Sample
        Next
        mixed += 128
        If mixed < 0 Then mixed = 0 Else If mixed > 255 Then mixed = 255

        SyncLock ringLock
            If buffered >= maxBufferedSamples Then
                ' Too far ahead of the audio device: drop the oldest sample
                readPos = (readPos + 1) Mod ring.Length
                buffered -= 1
            End If
            ring(writePos) = mixed
            writePos = (writePos + 1) Mod ring.Length
            buffered += 1
        End SyncLock

        ScheduleNextSample()
    End Sub

    Private Sub ScheduleNextSample()
        Dim hostClock As Long = CLng(Scheduler.HOSTCLOCK) ' Long math (mixing in ULong would promote to Decimal)
        sampleIndex += 1
        If sampleIndex = SpeakerAdapter.SampleRate Then
            ' Rebase once per simulated second so the multiplication below never overflows
            secondStartTime += hostClock
            sampleIndex = 0
        End If
        cpu.Sched.RunTaskAt(sTask, secondStartTime + CLng(sampleIndex) * hostClock \ SpeakerAdapter.SampleRate)
    End Sub

    ' BASS thread: hand out buffered samples; on underrun hold the last sample (avoids clicks)
    ' and wait for a small pre-roll before resuming so we don't stutter sample-by-sample
    Private Function FillAudioBuffer(handle As Integer, buffer As IntPtr, length As Integer, user As IntPtr) As Integer
        Dim out(length - 1) As Byte

        SyncLock ringLock
            If Not primed AndAlso buffered >= preRollSamples Then primed = True

            Dim n As Integer = 0
            If primed Then
                n = Math.Min(length, buffered)
                For i As Integer = 0 To n - 1
                    out(i) = ring(readPos)
                    readPos = (readPos + 1) Mod ring.Length
                Next
                buffered -= n
                If n > 0 Then lastSample = out(n - 1)
                If n < length Then primed = False ' underrun
            End If

            For i As Integer = n To length - 1
                out(i) = lastSample
            Next
        End SyncLock

        Marshal.Copy(out, 0, buffer, length)
        Return length
    End Function

    Public Sub Init()
        OpenStream()

        SyncLock ringLock
            readPos = 0
            writePos = 0
            buffered = 0
            primed = False
        End SyncLock

        sTask.Cancel()
        secondStartTime = cpu.Sched.CurrentTime
        sampleIndex = 0
        ScheduleNextSample()
    End Sub

    Public ReadOnly Property Providers As List(Of AudioProvider)
        Get
            Return mProviders
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Audio Subsystem"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Audio Subsystem"
        End Get
    End Property

#Region "IDisposable Support"
    Private disposedValue As Boolean
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects)
                If handle <> 0 Then
                    Bass.ChannelStop(handle)
                    Bass.StreamFree(handle)
                    handle = 0
                End If
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            disposedValue = True
        End If
    End Sub

    ' ' TODO: override finalizer only if 'Dispose(disposing As Boolean)' has code to free unmanaged resources
    ' Protected Overrides Sub Finalize()
    '     ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

    Public Overrides Sub Out(port As UShort, value As Byte)
    End Sub

    Public Overrides Function [In](port As UShort) As Byte
        Return &HFF
    End Function
#End Region
End Class

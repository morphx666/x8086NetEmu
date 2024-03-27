Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports ManagedBass

Public Class AudioSubSystem
    Implements IDisposable

    Private handle As Integer
    Private ReadOnly mProviders As New List(Of AudioProvider)
    Private ReadOnly audioBuffer As Byte() = New Byte(96000 - 1) {}
    Private bufferIndex As Integer = 0

    Private latency As Integer = 200
    Private bufferMax As Integer = (SpeakerAdapter.SampleRate / 1000) * latency

    Public Sub New()
        BassHelpers.Setup()

        handle = Bass.CreateStream(SpeakerAdapter.SampleRate, 1, BassFlags.Byte, AddressOf FillAudioBuffer, IntPtr.Zero)
        Bass.ChannelSetAttribute(handle, ChannelAttribute.Buffer, bufferMax)
        Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, 0.03)
        Bass.ChannelPlay(handle)

        For i As Integer = 0 To audioBuffer.Length - 1
            audioBuffer(i) = 128
        Next

        Task.Run(action:=Async Sub()
                             Await Task.Delay(1000) ' FIXME: This is a hack to let the CPU add all the audio adapters

                             Dim sampleTicks As Long = Scheduler.HOSTCLOCK / SpeakerAdapter.SampleRate
                             Dim lastSampleTick As Long = Stopwatch.GetTimestamp()

                             Do
                                 Dim curTick As Long = Stopwatch.GetTimestamp()

                                 If bufferIndex < audioBuffer.Length AndAlso curTick >= (lastSampleTick + sampleTicks) Then
                                     TickAudio()

                                     lastSampleTick = curTick - (curTick - (lastSampleTick + sampleTicks))
                                 End If
                             Loop
                         End Sub)
    End Sub

    Private Function FillAudioBuffer(handle As Integer, buffer As IntPtr, length As Integer, user As IntPtr) As Integer
        length = bufferIndex

        Marshal.Copy(audioBuffer, 0, buffer, length)
        Array.Copy(audioBuffer, length, audioBuffer, 0, audioBuffer.Length - length)

        bufferIndex -= length
        If bufferIndex < 0 Then bufferIndex = 0

        Return length
    End Function

    Public ReadOnly Property Providers As List(Of AudioProvider)
        Get
            Return mProviders
        End Get
    End Property

    Private Sub TickAudio()
        If bufferIndex >= bufferMax Then Return

        Dim sample As Int16
        For Each provider In mProviders
            sample += provider.GenerateSample()
        Next

        If sample <= 0 Then
            sample = 128 + sample
        Else
            sample += 127
        End If

        If bufferIndex < audioBuffer.Length Then
            audioBuffer(bufferIndex) = sample
            bufferIndex += 1
        End If
    End Sub

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
#End Region
End Class

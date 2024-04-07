Imports System.Runtime.InteropServices
Imports ManagedBass

Public Class AudioSubsystem
    Inherits IOPortHandler
    Implements IDisposable

    Private handle As Integer
    Private ReadOnly mProviders As New List(Of AudioProvider)
    Private ReadOnly audioBuffer As Byte() = New Byte(96_000 - 1) {}
    Private bufferIndex As Integer
    Private ReadOnly bufferMax As Integer
    Private ReadOnly latency As Integer = 100
    Private sampleTicks As Integer

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
        BassHelpers.Setup()

        bufferMax = (SpeakerAdapter.SampleRate / 1000) * latency
        bufferIndex = bufferMax

        handle = Bass.CreateStream(SpeakerAdapter.SampleRate, 1, BassFlags.Byte, AddressOf FillAudioBuffer, IntPtr.Zero)
        Bass.ChannelSetAttribute(handle, ChannelAttribute.Buffer, 0)
        Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, 1.0)
        Bass.ChannelPlay(handle)

        For i As Integer = 0 To audioBuffer.Length - 1
            audioBuffer(i) = 128
        Next
    End Sub

    Public Overrides Sub Run()
        If bufferIndex >= bufferMax Then Exit Sub

        Dim sample As Int16
        For Each provider In mProviders
            sample += provider.Sample
        Next

        audioBuffer(bufferIndex) = sample + 128
        bufferIndex += 1
    End Sub

    Private Function FillAudioBuffer(handle As Integer, buffer As IntPtr, length As Integer, user As IntPtr) As Integer
        Marshal.Copy(audioBuffer, 0, buffer, length)
        Array.Copy(audioBuffer, length, audioBuffer, 0, bufferMax - length)

        bufferIndex -= length
        If bufferIndex < 0 Then bufferIndex = 0

        Return length
    End Function

    Public Sub Init()
        ' FIXME: This 1/10 factor is due to the factor used in the PIT8254
#If DEBUG Then
        sampleTicks = Scheduler.HOSTCLOCK \ SpeakerAdapter.SampleRate
#Else
        sampleTicks = 10 * Scheduler.HOSTCLOCK \ SpeakerAdapter.SampleRate
#End If
        cpu.Sched.RunTaskEach(sTask, sampleTicks)
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

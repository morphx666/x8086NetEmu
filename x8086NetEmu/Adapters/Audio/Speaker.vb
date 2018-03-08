#If Win32 Then
Imports System.Runtime.InteropServices
Imports NAudio.Wave

Public Class CustomBufferProvider
    Implements IWaveProvider

    Public Delegate Sub FillBuffer(buffer() As Byte)

    Private wf As WaveFormat
    Private fb As FillBuffer

    Public Sub New(bufferFiller As FillBuffer)
        wf = New WaveFormat(SpeakerAdpater.SampleRate, 8, 1)
        fb = bufferFiller
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements IWaveProvider.WaveFormat
        Get
            Return wf
        End Get
    End Property

    Public Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer Implements IWaveProvider.Read
        fb.Invoke(buffer)
        Return count
    End Function
End Class

Public Class SpeakerAdpater
    Inherits Adapter

    Private Enum WaveForms
        Squared
        Sinusoidal
    End Enum

    Private waveForm As WaveForms = WaveForms.Squared

    Private Const ToRad As Double = Math.PI / 180

    Private waveOut As WaveOut
    Private audioProvider As CustomBufferProvider
    Private mAudioBuffer() As Byte

    Public Const SampleRate As Integer = 44100

    Private mCPU As X8086
    Private mEnabled As Boolean

    Private mFrequency As Double
    Private waveLength As Integer
    Private halfWaveLength As Integer
    Private currentStep As Integer

    Private mVolume As Double

    Public Sub New(cpu As X8086)
        mCPU = cpu
        If mCPU.PIT IsNot Nothing Then mCPU.PIT.Speaker = Me
        mVolume = 0.05
    End Sub

    Public Property Frequency As Double
        Get
            Return mFrequency
        End Get
        Set(value As Double)
            mFrequency = value
            UpdateWaveformParameters()
        End Set
    End Property

    Public Property Enabled As Boolean
        Get
            Return mEnabled
        End Get
        Set(value As Boolean)
            mEnabled = value
            UpdateWaveformParameters()
        End Set
    End Property

    Public Property Volume As Double
        Get
            Return mVolume
        End Get
        Set(value As Double)
            mVolume = value
        End Set
    End Property

    Public ReadOnly Property AudioBuffer As Byte()
        Get
            Return mAudioBuffer
        End Get
    End Property

    Private Sub UpdateWaveformParameters()
        If mFrequency > 0 Then
            waveLength = SampleRate / mFrequency
        Else
            waveLength = 0
        End If

        halfWaveLength = waveLength / 2
    End Sub

    Private Sub FillAudioBuffer(buffer() As Byte)
        Dim v As Double

        For i As Integer = 0 To buffer.Length - 1
            Select Case waveForm
                Case WaveForms.Squared
                    If mEnabled Then
                        If currentStep <= halfWaveLength Then
                            v = -128
                        Else
                            v = 127
                        End If
                    Else
                        v = 0
                    End If
                Case WaveForms.Sinusoidal
                    If mEnabled AndAlso waveLength > 0 Then
                        v = Math.Floor(Math.Sin((currentStep / waveLength) * (mFrequency / 2) * ToRad) * 128)
                    Else
                        v = 0
                    End If
            End Select

            v *= mVolume
            If v <= 0 Then
                v = 128 + v
            Else
                v += 127
            End If
            buffer(i) = v

            currentStep += 1
            If currentStep >= waveLength Then currentStep = 0
        Next

        'mAudioBuffer = buffer
    End Sub

    Public Overrides Sub CloseAdapter()
        waveOut.Stop()
        waveOut.Dispose()
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "PC Speaker"
        End Get
    End Property

    Public Overrides Sub InitiAdapter()
        waveOut = New WaveOut() With {
            .NumberOfBuffers = 32,
            .DesiredLatency = 200
        }
        audioProvider = New CustomBufferProvider(AddressOf FillAudioBuffer)
        waveOut.Init(audioProvider)
        waveOut.Play()
    End Sub

    Public Overrides Function [In](port As UInteger) As UInteger
        Return &HFF ' Avoid warning BC42353
    End Function

    Public Overrides Sub Out(port As UInteger, value As UInteger)

    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Speaker"
        End Get
    End Property

    Public Overrides Sub Run()
        X8086.Notify("Speaker Running", X8086.NotificationReasons.Info)
    End Sub

    Public Overrides ReadOnly Property Type As Adapter.AdapterType
        Get
            Return AdapterType.AudioDevice
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
            Return 1
        End Get
    End Property
End Class
#Else
Public Class SpeakerAdpater
    Inherits Adapter

    Public Sub New(cpu As x8086)

    End Sub

    Public Overrides Sub CloseAdapter()

    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Null PC Speaker"
        End Get
    End Property

    Public Overrides Function [In](port As UInteger) As UInteger
        Return 0
    End Function

    Public Overrides Sub Out(port As UInteger, value As UInteger)

    End Sub

    Public Overrides Sub InitiAdapter()

    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Null Speaker"
        End Get
    End Property

    Public Overrides Sub Run()

    End Sub

    Public Overrides ReadOnly Property Type As Adapter.AdapterType
        Get
            Return AdapterType.Speaker
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
            Return 1
        End Get
    End Property
End Class
#End If
#If Win32 Then
Imports SlimDX.DirectSound
Imports SlimDX.Multimedia
Imports System.Runtime.InteropServices
Imports System.Threading

Public Class SpeakerAdpater
    Inherits Adapter

    <DllImport("user32.dll", CharSet:=CharSet.Auto, ExactSpelling:=True)>
    Private Shared Function GetDesktopWindow() As IntPtr
    End Function

    Private Enum WaveForms
        Squared
        Sinusoidal
    End Enum

    Private waveForm As WaveForms = WaveForms.Squared

    Private Const hi As Integer = Byte.MaxValue
    Private Const lo As Integer = Byte.MinValue
    Private Const ToRad As Double = Math.PI / 180

    Private audioDev As DirectSound
    Private bufPlayDesc As SoundBufferDescription
    Private playBuf As SecondarySoundBuffer
    Private notifySize As Integer
    Private numberPlaybackNotifications As Integer = 8 ' 4
    Private nextPlaybackOffset As Integer

    Private waiter As AutoResetEvent = New AutoResetEvent(False)

    Private mAudioBuffer() As Byte
    Private audioWriteBufferPosition As Integer
    Private Const sampleRate As Integer = 44100

    Private mCPU As x8086
    Private mEnabled As Boolean
    Private playbackThread As Thread
    Private cancelAllThreads As Boolean

    Private mFrequency As Double
    Private waveLength As Integer
    Private halfWaveLength As Integer
    Private bufferWritePosition As Integer
    Private bufferReadPosition As Integer
    Private currentStep As Integer

    Private Structure DirtyByte
        Private mValue As Byte
        Private mIsDirty As Boolean

        Public Property Value As Byte
            Get
                mIsDirty = False
                Return mValue
            End Get
            Set(value As Byte)
                mValue = value
                mIsDirty = True
            End Set
        End Property

        Public ReadOnly Property IsDirty As Boolean
            Get
                Return mIsDirty
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return String.Format("{0}: {1}", mValue, If(mIsDirty, "Y", "N"))
        End Function
    End Structure

    Public Sub New(cpu As x8086)
        mCPU = cpu
        If mCPU.PIT IsNot Nothing Then mCPU.PIT.Speaker = Me
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

    Public ReadOnly Property AudioBuffer As Byte()
        Get
            Return mAudioBuffer
        End Get
    End Property

    Private Sub UpdateWaveformParameters()
        If mFrequency > 0 Then
            waveLength = sampleRate / mFrequency
        Else
            waveLength = 0
        End If

        halfWaveLength = waveLength / 2
    End Sub

    Private Sub FillAudioBuffer()
        Do
            Select Case waveForm
                Case WaveForms.Squared
                    If mEnabled Then
                        If currentStep <= halfWaveLength Then
                            mAudioBuffer(bufferWritePosition) = hi
                        Else
                            mAudioBuffer(bufferWritePosition) = lo
                        End If
                    Else
                        mAudioBuffer(bufferWritePosition) = 128
                    End If
                Case WaveForms.Sinusoidal
                    If mEnabled AndAlso waveLength > 0 Then
                        Dim v = Math.Floor(Math.Sin((currentStep / waveLength) * (mFrequency / 2) * ToRad) * 128)
                        If v < 0 Then
                            v = 128 + v
                        Else
                            v += 127
                        End If

                        mAudioBuffer(bufferWritePosition) = v
                    Else
                        mAudioBuffer(bufferWritePosition) = 128
                    End If
            End Select

            currentStep += 1
            If currentStep >= waveLength Then currentStep = 0

            bufferWritePosition += 1
            bufferWritePosition = bufferWritePosition Mod mAudioBuffer.Length
        Loop Until cancelAllThreads OrElse bufferWritePosition = 0
    End Sub

    Private Sub MainLoop()
        Do
            waiter.WaitOne()

            FillAudioBuffer()
            Write()
        Loop Until cancelAllThreads
    End Sub

    Public Overrides Sub CloseAdapter()
        cancelAllThreads = True

        Do
            Thread.Sleep(10)
        Loop While playbackThread.ThreadState <> ThreadState.Stopped

        playBuf.Stop()
        waiter.Set()

        playBuf.Dispose()
        audioDev.Dispose()
    End Sub

    Public Overrides ReadOnly Property Description As String
        Get
            Return "PC Speaker"
        End Get
    End Property

    Public Overrides Function [In](port As UInteger) As UInteger
        Return &HFF
    End Function

    Public Overrides Sub InitiAdapter()
        ReDim mAudioBuffer(sampleRate / 100 - 1)

        ' Define the capture format
        Dim format As WaveFormat = New WaveFormat()
        With format
            .BitsPerSample = 8
            .Channels = 1
            .FormatTag = WaveFormatTag.Pcm
            .SamplesPerSecond = sampleRate
            .BlockAlignment = CShort(.Channels * .BitsPerSample / 8)
            .AverageBytesPerSecond = .SamplesPerSecond * .BlockAlignment
        End With

        ' Define the size of the notification chunks
        notifySize = mAudioBuffer.Length
        notifySize -= notifySize Mod format.BlockAlignment

        ' Create a buffer description object
        bufPlayDesc = New SoundBufferDescription()
        With bufPlayDesc
            .Format = format
            .Flags = BufferFlags.ControlPositionNotify Or
                    BufferFlags.GetCurrentPosition2 Or
                    BufferFlags.GlobalFocus Or
                    BufferFlags.Static Or
                    BufferFlags.ControlVolume Or
                    BufferFlags.ControlPan Or
                    BufferFlags.ControlFrequency
            .SizeInBytes = notifySize * numberPlaybackNotifications
        End With

        audioDev = New DirectSound()
        Dim windowHandle As IntPtr = GetDesktopWindow()
        audioDev.SetCooperativeLevel(windowHandle, CooperativeLevel.Priority)
        playBuf = New SecondarySoundBuffer(audioDev, bufPlayDesc)

        ' Define the notification events
        Dim np(numberPlaybackNotifications - 1) As NotificationPosition

        For i As Integer = 0 To numberPlaybackNotifications - 1
            np(i) = New NotificationPosition()
            np(i).Offset = (notifySize * i) + notifySize - 1
            np(i).Event = waiter
        Next
        playBuf.SetNotificationPositions(np)

        nextPlaybackOffset = 0
        playBuf.Play(0, PlayFlags.Looping)

        playbackThread = New Thread(AddressOf MainLoop)
        playbackThread.Start()
    End Sub

    Public Sub Write()
        Dim lockSize As Integer

        lockSize = playBuf.CurrentWritePosition - nextPlaybackOffset
        If lockSize < 0 Then lockSize += bufPlayDesc.SizeInBytes

        ' Block align lock size so that we always read on a boundary
        lockSize -= lockSize Mod notifySize
        If lockSize = 0 Then Exit Sub

        playBuf.Write(Of Byte)(mAudioBuffer, nextPlaybackOffset, LockFlags.None)

        nextPlaybackOffset += mAudioBuffer.Length
        nextPlaybackOffset = nextPlaybackOffset Mod bufPlayDesc.SizeInBytes ' Circular buffer
    End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Speaker"
        End Get
    End Property

    Public Overrides Sub Out(port As UInteger, value As UInteger)

    End Sub

    Public Overrides Sub Run()
        x8086.Notify("Speaker Running", x8086.NotificationReasons.Info)
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
            Return 23
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

    Public Overloads Overrides Function [In](port As UInteger) As UInteger
        Return 0
    End Function

    Public Overloads Overrides Sub Out(port As UInteger, value As UInteger)

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
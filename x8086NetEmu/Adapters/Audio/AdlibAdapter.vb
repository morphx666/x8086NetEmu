#If Win32 Then
Imports System.Threading
Imports NAudio.Wave

Public Class AdlibAdapter ' Based on fake86's implementation
    Inherits Adapter

    Private waveOut As WaveOut
    Private audioProvider As SpeakerAdpater.CustomBufferProvider
    Private ReadOnly mAudioBuffer() As Byte

    Private ReadOnly mCPU As X8086

    Private ReadOnly waveForm()() As Byte = {
        New Byte() {1, 8, 13, 20, 25, 32, 36, 42, 46, 50, 54, 57, 60, 61, 62, 64, 63, 65, 61, 61, 58, 55, 51, 49, 44, 38, 34, 28, 23, 16, 11, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        New Byte() {1, 8, 13, 21, 25, 31, 36, 43, 45, 50, 54, 57, 59, 62, 63, 63, 63, 64, 63, 59, 59, 55, 52, 48, 44, 38, 34, 28, 23, 16, 10, 4, 2, 7, 14, 20, 26, 31, 36, 42, 45, 51, 54, 56, 60, 62, 62, 63, 65, 63, 62, 60, 58, 55, 52, 48, 44, 38, 34, 28, 23, 17, 10, 3},
        New Byte() {1, 8, 13, 20, 26, 31, 36, 42, 46, 51, 53, 57, 60, 62, 61, 66, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 7, 13, 21, 25, 32, 36, 41, 47, 50, 54, 56, 60, 62, 61, 67, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        New Byte() {1, 8, 13, 20, 26, 31, 37, 41, 47, 49, 54, 58, 58, 62, 63, 63, 64, 63, 62, 61, 58, 55, 52, 47, 45, 38, 34, 27, 23, 17, 10, 4, -2, -8, -15, -21, -26, -34, -36, -42, -48, -51, -54, -59, -60, -62, -64, -65, -65, -63, -64, -61, -59, -56, -53, -48, -46, -39, -36, -28, -24, -17, -11, -6}
    }

    Private ReadOnly oplWave()() As Byte = {
    New Byte() {
        0, 1, 3, 4, 6, 7, 9, 11, 12, 14, 15, 17, 18, 20, 22, 23, 24, 26, 27, 29, 30, 31, 33, 34, 36, 37, 38, 40, 40, 42, 43, 44, 46, 46, 48, 49, 50, 51, 51, 53,
        53, 54, 55, 56, 57, 57, 58, 59, 59, 60, 61, 61, 62, 62, 63, 63, 63, 64, 64, 64, 116, 116, 116, 116, 116, 116, 116, 116, 116, 64, 64, 64, 63, 63, 63, 62, 62, 61, 61, 60,
        59, 59, 58, 57, 57, 56, 55, 54, 53, 53, 51, 51, 50, 49, 48, 46, 46, 44, 43, 42, 40, 40, 38, 37, 36, 34, 33, 31, 30, 29, 27, 26, 24, 23, 22, 20, 18, 17, 15, 14,
        12, 11, 9, 7, 6, 4, 3, 1, 0, -1, -3, -4, -6, -7, -9, -11, -12, -14, -15, -17, -18, -20, -22, -23, -24, -26, -27, -29, -30, -31, -33, -34, -36, -37, -38, -40, -40, -42, -43, -44,
        -46, -46, -48, -49, -50, -51, -51, -53, -53, -54, -55, -56, -57, -57, -58, -59, -59, -60, -61, -61, -62, -62, -63, -63, -63, -64, -64, -64, -116, -116, -116, -116, -116, -116, -116, -116, -116, -64, -64, -64,
        -63, -63, -63, -62, -62, -61, -61, -60, -59, -59, -58, -57, -57, -56, -55, -54, -53, -53, -51, -51, -50, -49, -48, -46, -46, -44, -43, -42, -40, -40, -38, -37, -36, -34, -33, -31, -30, -29, -27, -26,
        -24, -23, -22, -20, -18, -17, -15, -14, -12, -11, -9, -7, -6, -4, -3, -1
    },
    New Byte() {
        0, 1, 3, 4, 6, 7, 9, 11, 12, 14, 15, 17, 18, 20, 22, 23, 24, 26, 27, 29, 30, 31, 33, 34, 36, 37, 38, 40, 40, 42, 43, 44, 46, 46, 48, 49, 50, 51, 51, 53,
        53, 54, 55, 56, 57, 57, 58, 59, 59, 60, 61, 61, 62, 62, 63, 63, 63, 64, 64, 64, 116, 116, 116, 116, 116, 116, 116, 116, 116, 64, 64, 64, 63, 63, 63, 62, 62, 61, 61, 60,
        59, 59, 58, 57, 57, 56, 55, 54, 53, 53, 51, 51, 50, 49, 48, 46, 46, 44, 43, 42, 40, 40, 38, 37, 36, 34, 33, 31, 30, 29, 27, 26, 24, 23, 22, 20, 18, 17, 15, 14,
        12, 11, 9, 7, 6, 4, 3, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    },
    New Byte() {
        0, 1, 3, 4, 6, 7, 9, 11, 12, 14, 15, 17, 18, 20, 22, 23, 24, 26, 27, 29, 30, 31, 33, 34, 36, 37, 38, 40, 40, 42, 43, 44, 46, 46, 48, 49, 50, 51, 51, 53,
        53, 54, 55, 56, 57, 57, 58, 59, 59, 60, 61, 61, 62, 62, 63, 63, 63, 64, 64, 64, 116, 116, 116, 116, 116, 116, 116, 116, 116, 64, 64, 64, 63, 63, 63, 62, 62, 61, 61, 60,
        59, 59, 58, 57, 57, 56, 55, 54, 53, 53, 51, 51, 50, 49, 48, 46, 46, 44, 43, 42, 40, 40, 38, 37, 36, 34, 33, 31, 30, 29, 27, 26, 24, 23, 22, 20, 18, 17, 15, 14,
        12, 11, 9, 7, 6, 4, 3, 1, 0, 1, 3, 4, 6, 7, 9, 11, 12, 14, 15, 17, 18, 20, 22, 23, 24, 26, 27, 29, 30, 31, 33, 34, 36, 37, 38, 40, 40, 42, 43, 44,
        46, 46, 48, 49, 50, 51, 51, 53, 53, 54, 55, 56, 57, 57, 58, 59, 59, 60, 61, 61, 62, 62, 63, 63, 63, 64, 64, 64, 116, 116, 116, 116, 116, 116, 116, 116, 116, 64, 64, 64,
        63, 63, 63, 62, 62, 61, 61, 60, 59, 59, 58, 57, 57, 56, 55, 54, 53, 53, 51, 51, 50, 49, 48, 46, 46, 44, 43, 42, 40, 40, 38, 37, 36, 34, 33, 31, 30, 29, 27, 26,
        24, 23, 22, 20, 18, 17, 15, 14, 12, 11, 9, 7, 6, 4, 3, 1
    },
    New Byte() {
        0, 1, 3, 4, 6, 7, 9, 11, 12, 14, 15, 17, 18, 20, 22, 23, 24, 26, 27, 29, 30, 31, 33, 34, 36, 37, 38, 40, 40, 42, 43, 44, 46, 46, 48, 49, 50, 51, 51, 53,
        53, 54, 55, 56, 57, 57, 58, 59, 59, 60, 61, 61, 62, 62, 63, 63, 63, 64, 64, 64, 116, 116, 116, 116, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 3, 4, 6, 7, 9, 11, 12, 14, 15, 17, 18, 20, 22, 23, 24, 26, 27, 29, 30, 31, 33, 34, 36, 37, 38, 40, 40, 42, 43, 44,
        46, 46, 48, 49, 50, 51, 51, 53, 53, 54, 55, 56, 57, 57, 58, 59, 59, 60, 61, 61, 62, 62, 63, 63, 63, 64, 64, 64, 116, 116, 116, 116, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    }
    }

    Private ReadOnly oplStep() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0}

    Private Structure OplStruct
        Public wave As Byte
    End Structure
    Private Opl(9 - 1)() As OplStruct

    Private Structure ChanStruct
        Public Frequency As UInt16
        Public ConvFreq As Double
        Public KeyOn As Boolean
        Public Octave As UInt16
        Public WaveformSelect As Byte
    End Structure
    Private ReadOnly channel(9 - 1) As ChanStruct

    Private ReadOnly attackTable() As Double = {1.0003, 1.00025, 1.0002, 1.00015, 1.0001, 1.00009, 1.00008, 1.00007, 1.00006, 1.00005, 1.00004, 1.00003, 1.00002, 1.00001, 1.000005}
    Private ReadOnly decayTable() As Double = {0.99999, 0.999985, 0.99998, 0.999975, 0.99997, 0.999965, 0.99996, 0.999955, 0.99995, 0.999945, 0.99994, 0.999935, 0.99994, 0.999925, 0.99992, 0.99991}
    Private ReadOnly oplTable() As Byte = {0, 0, 0, 1, 1, 1, 255, 255, 0, 0, 0, 1, 1, 1, 255, 255, 0, 0, 0, 1, 1, 1}

    Private ReadOnly envelope(9 - 1) As Double
    Private ReadOnly decay(9 - 1) As Double
    Private ReadOnly attack(9 - 1) As Double
    Private ReadOnly attack2(9 - 1) As Boolean

    Private ReadOnly regMem(&HFF - 1) As UInt16
    Private address As UInt16 = 0
    Private precussion As Boolean = False
    Private status As Byte = 0
    Private ReadOnly oplSstep(9 - 1) As Double

    Public Sub New(cpu As X8086)
        mCPU = cpu

        For i As Integer = 0 To Opl.Length - 1
            ReDim Opl(i)(2 - 1)
        Next

        ValidPortAddress.Add(&H388)
        ValidPortAddress.Add(&H389)

        ReDim Preserve attackTable(16 - 1)
        ReDim Preserve decayTable(16 - 1)
        ReDim Preserve oplTable(16 - 1)
    End Sub

    Public Property Volume As Double
        Get
            Return waveOut.Volume
        End Get
        Set(value As Double)
            waveOut.Volume = value
        End Set
    End Property

    Public ReadOnly Property AudioBuffer As Byte()
        Get
            Return mAudioBuffer
        End Get
    End Property

    Public Overrides Sub CloseAdapter()
        waveOut.Stop()
        waveOut.Dispose()
    End Sub

    Public Overrides Sub InitiAdapter()
        waveOut = New WaveOut() With {
            .NumberOfBuffers = 32,
            .DesiredLatency = 200
        }

        audioProvider = New SpeakerAdpater.CustomBufferProvider(AddressOf FillAudioBuffer, SpeakerAdpater.SampleRate, 8, 1)
        waveOut.Init(audioProvider)
        waveOut.Play()

        Tasks.Task.Run(Sub()
                           Dim maxTicks As Long = Scheduler.BASECLOCK \ SpeakerAdpater.SampleRate
                           Dim curTick As Long
                           Dim lastTick As Long
                           Do
                               curTick = mCPU.Sched.CurrentTimeMillis

                               If curTick >= (lastTick + maxTicks) Then
                                   lastTick = curTick - (curTick - (lastTick + maxTicks))

                                   SyncLock channel
                                       For currentChannel As Byte = 0 To 9 - 1
                                           If Frequency(currentChannel) <> 0 Then
                                               If attack2(currentChannel) Then
                                                   envelope(currentChannel) *= decay(currentChannel)
                                               Else
                                                   envelope(currentChannel) *= attack(currentChannel)
                                                   If envelope(currentChannel) >= 1.0 Then attack2(currentChannel) = True
                                               End If
                                           End If
                                       Next
                                   End SyncLock
                               End If

                               Thread.Sleep(50)
                           Loop While waveOut.PlaybackState = PlaybackState.Playing
                       End Sub)
    End Sub

    Private Sub FillAudioBuffer(buffer() As Byte)
        For i As Integer = 0 To buffer.Length - 1
            buffer(i) = GenerateSample() + 128
        Next
    End Sub

    Public Overrides Function [In](port As UInt32) As UInt16
        status = If(regMem(4) = 0, 0, &H80)
        status += (regMem(4) And 1) * &H40 + (regMem(4) And 2) * &H10
        Return status
    End Function

    Public Overrides Sub Out(port As UInt32, value As UInt16)
        If port = &H388 Then
            address = value
            Exit Sub
        End If

        port = address
        regMem(port) = value

        Select Case port
            Case 4 ' Timer Control
                If (value And &H80) <> 0 Then
                    status = 0
                    regMem(4) = 0
                End If
            Case &HBD
                precussion = (value And &H10) <> 0
        End Select

        If port >= &H60 AndAlso port <= &H75 Then ' Attack / Decay
            port = (port And 15) Mod 9
            attack(port) = attackTable(15 - (value >> 4)) * 1.006
            decay(port) = decayTable(value And 15)
        ElseIf port >= &HA0 AndAlso port <= &HB8 Then ' Octave / Frequency / Key On
            port = port And 15
            If Not channel(port Mod 9).KeyOn AndAlso ((regMem(&HB0 + port) >> 5) And 1) = 1 Then
                attack2(port Mod 9) = False
                envelope(port Mod 9) = 0.0025
            End If

            SyncLock channel
                channel(port Mod 9).Frequency = regMem(&HA0 + port) Or ((regMem(&HB0 + port) And 3) << 8)
                channel(port Mod 9).ConvFreq = channel(port Mod 9).Frequency * 0.7626459
                channel(port Mod 9).KeyOn = ((regMem(&HB0 + port) >> 5) And 1) = 1
                channel(port Mod 9).Octave = (regMem(&HB0 + port) >> 2) And 7
            End SyncLock
        ElseIf port >= &HE0 And port <= &HF5 Then ' Waveform select
            channel((port And 15) Mod 9).WaveformSelect = value And 3
        End If
    End Sub

    Private Function Frequency(channel As Byte) As UInt16
        Dim tmpFrequency As UInt16

        If Not Me.channel(channel).KeyOn Then Return 0
        tmpFrequency = Me.channel(channel).ConvFreq

        Select Case Me.channel(channel).Octave
            Case 0 : tmpFrequency = tmpFrequency >> 4
            Case 1 : tmpFrequency = tmpFrequency >> 3
            Case 2 : tmpFrequency = tmpFrequency >> 2
            Case 3 : tmpFrequency = tmpFrequency >> 1
            Case 5 : tmpFrequency = tmpFrequency << 1
            Case 6 : tmpFrequency = tmpFrequency << 2
            Case 7 : tmpFrequency = tmpFrequency << 3
        End Select

        Return tmpFrequency
    End Function

    Private Function Sample(channel As Byte) As Int32
        If precussion AndAlso channel >= 6 AndAlso channel <= 8 Then Return 0

        Dim fullStep As UInt32 = SpeakerAdpater.SampleRate \ Frequency(channel)
        Dim idx As Byte = (oplSstep(channel) / (fullStep / 256.0)) Mod 255
        Dim tmpSample As Integer = oplWave(Me.channel(channel).WaveformSelect)(idx)
        Dim tmpStep As Double = envelope(channel)
        If tmpStep > 1.0 Then tmpStep = 1.0
        tmpSample = tmpSample * tmpStep * 12.0

        oplSstep(channel) += 1
        If oplSstep(channel) > fullStep Then oplSstep(channel) = 0
        Return tmpSample
    End Function

    Private Function GenerateSample() As Int16
        Dim accumulator As Int16 = 0
        SyncLock channel
            For currentChannel As Byte = 0 To 9 - 1
                If Frequency(currentChannel) <> 0 Then accumulator += Sample(currentChannel)
            Next
        End SyncLock
        Return accumulator
    End Function

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Adlib OPL2" ' FM OPerator Type-L
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Yamaha YM3526"
        End Get
    End Property

    Public Overrides Sub Run()
        X8086.Notify($"{Name} Running", X8086.NotificationReasons.Info)
    End Sub

    Public Overrides ReadOnly Property Type As Adapter.AdapterType
        Get
            Return AdapterType.AudioDevice
        End Get
    End Property

    Public Overrides ReadOnly Property Vendor As String
        Get
            Return "Ad Lib, Inc."
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
#End If
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

    Private oplStep() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0}

    Private Structure AdlibOpStruct
        Public wave As Byte
    End Structure
    Private adlibOp(9 - 1)() As AdlibOpStruct

    Private Structure AdlibChanStruct
        Public Frequency As UInt16
        Public ConvFreq As Double
        Public KeyOn As Boolean
        Public Octave As UInt16
        Public WaveformSelect As Byte
    End Structure
    Private ReadOnly adlibChan(9 - 1) As AdlibChanStruct

    Private ReadOnly attackTable() As Double = {1.0003, 1.00025, 1.0002, 1.00015, 1.0001, 1.00009, 1.00008, 1.00007, 1.00006, 1.00005, 1.00004, 1.00003, 1.00002, 1.00001, 1.000005}
    Private ReadOnly decayTable() As Double = {0.99999, 0.999985, 0.99998, 0.999975, 0.99997, 0.999965, 0.99996, 0.999955, 0.99995, 0.999945, 0.99994, 0.999935, 0.99994, 0.999925, 0.99992, 0.99991}
    Private ReadOnly opTable() As Byte = {0, 0, 0, 1, 1, 1, 255, 255, 0, 0, 0, 1, 1, 1, 255, 255, 0, 0, 0, 1, 1, 1}

    Private ReadOnly adlibEnv(9 - 1) As Double
    Private ReadOnly adlibDecay(9 - 1) As Double
    Private ReadOnly adlibAttack(9 - 1) As Double

    Private Const SampleRate As UInt32 = 44100

    Private ReadOnly adlibRegMem(&HFF - 1) As UInt16
    Private adlibAddr As UInt16 = 0
    Private adlibPrecussion As Boolean = False
    Private adlibStatus As Byte = 0
    Private ReadOnly adlibStep(9 - 1) As Double

    Public Sub New(cpu As X8086)
        mCPU = cpu

        For i As Integer = 0 To adlibOp.Length - 1
            ReDim adlibOp(i)(2 - 1)
        Next

        ValidPortAddress.Add(&H388)
        ValidPortAddress.Add(&H389)

        ReDim Preserve attackTable(16 - 1)
        ReDim Preserve decayTable(16 - 1)
        ReDim Preserve opTable(16 - 1)
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
            .NumberOfBuffers = 4,
            .DesiredLatency = 200
        }
        audioProvider = New SpeakerAdpater.CustomBufferProvider(AddressOf FillAudioBuffer, SampleRate, 8, 1)
        waveOut.Init(audioProvider)
        waveOut.Play()
    End Sub

    Public Sub FillAudioBuffer(buffer() As Byte)
        'Dim t As Long = Now.Ticks
        'If t >= (lastAdlibTicks + adlibTicks) Then
        For i As Integer = 0 To buffer.Length - 1
            buffer(i) = AdlibGenerateSample() + 128
        Next

        'lastAdlibTicks = t - (t - (lastAdlibTicks + adlibTicks))
        'End If

        'If (lastAdlibTicks Mod adlibTicks) = 8 Then AdlibTick()
    End Sub

    Public Overrides Function [In](port As UInt32) As UInt16
        If adlibRegMem(4) = 0 Then
            adlibStatus = 0
        Else
            adlibStatus = &H80
        End If
        adlibStatus += (adlibRegMem(4) And 1) * &H40 + (adlibRegMem(4) And 2) * &H10
        Return adlibStatus
    End Function

    Public Overrides Sub Out(port As UInt32, value As UInt16)
        If port = &H388 Then
            adlibAddr = value
            Exit Sub
        End If

        port = adlibAddr
        adlibRegMem(port) = value

        Select Case port
            Case 4 ' Timer Control
                If (value And &H80) <> 0 Then
                    adlibStatus = 0
                    adlibRegMem(4) = 0
                End If
            Case &HBD
                adlibPrecussion = (value And &H10) <> 0
        End Select

        If port >= &H60 AndAlso port <= &H75 Then ' Attack / Decay
            port = (port And 15) Mod 9
            adlibAttack(port) = attackTable(15 - (value >> 4)) * 1.006
            adlibDecay(port) = decayTable(value And 15)
        ElseIf port >= &HA0 AndAlso port <= &HB8 Then ' Octave / Frequency / Key On
            port = port And 15
            If Not adlibChan(port Mod 9).KeyOn AndAlso ((adlibRegMem(&HB0 + port) >> 5) And 1) = 1 Then
                adlibAttack(port Mod 9) = 0
                adlibEnv(port Mod 9) = 0.0025
            End If

            SyncLock adlibChan
                adlibChan(port Mod 9).Frequency = adlibRegMem(&HA0 + port) Or ((adlibRegMem(&HB0 + port) And 3) << 8)
                adlibChan(port Mod 9).ConvFreq = adlibChan(port Mod 9).Frequency * 0.7626459
                adlibChan(port Mod 9).KeyOn = ((adlibRegMem(&HB0 + port) >> 5) And 1) = 1
                adlibChan(port Mod 9).Octave = (adlibRegMem(&HB0 + port) >> 2) And 7
            End SyncLock
        ElseIf port >= &HE0 And port <= &HF5 Then ' Waveform select
            adlibChan((port And 15) Mod 9).WaveformSelect = value And 3
        End If
    End Sub

    Private Function AdlibFrequency(channel As Byte) As UInt16
        Dim tmpFrequency As UInt16

        If Not adlibChan(channel).KeyOn Then Return 0
        tmpFrequency = adlibChan(channel).ConvFreq

        Select Case adlibChan(channel).Octave
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

    Private Function AdlibSample(channel As Byte) As Int32
        If adlibPrecussion AndAlso channel >= 6 AndAlso channel <= 8 Then Return 0

        Dim fullStep As UInt32 = SampleRate \ AdlibFrequency(channel)
        Dim idx As Byte = (adlibStep(channel) / (fullStep / 256.0)) Mod 255
        Dim tmpSample As Int32 = oplWave(adlibChan(channel).WaveformSelect)(idx)
        Dim tmpStep As Double = adlibEnv(channel)
        If tmpStep > 1.0 Then tmpStep = 1.0
        tmpSample = CDbl(tmpSample) * tmpStep * 3.0

        adlibStep(channel) += 1
        If adlibStep(channel) > fullStep Then adlibStep(channel) = 0
        Return tmpSample
    End Function

    Private Function AdlibGenerateSample() As Int16
        Dim adlibAccumulator As Int16 = 0
        SyncLock adlibChan
            For currentChannel As Byte = 0 To 9 - 1
                If AdlibFrequency(currentChannel) <> 0 Then adlibAccumulator += AdlibSample(currentChannel)
            Next
        End SyncLock
        Return adlibAccumulator
    End Function

    'Private Sub AdlibTick()
    '    For currentChannel As Byte = 0 To 9 - 1
    '        If AdlibFrequency(currentChannel) <> 0 Then
    '            If adlibAttack(currentChannel) <> 0 Then
    '                adlibEnv(currentChannel) *= adlibDecay(currentChannel)
    '            Else
    '                adlibEnv(currentChannel) *= adlibAttack(currentChannel)
    '                If adlibEnv(currentChannel) >= 1 Then adlibAttack(currentChannel) = 1
    '            End If
    '        End If
    '    Next
    'End Sub

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Adlib OPL2"
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Adlib OPL2"
        End Get
    End Property

    Public Overrides Sub Run()
        X8086.Notify("Adlib Running", X8086.NotificationReasons.Info)
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
#end if
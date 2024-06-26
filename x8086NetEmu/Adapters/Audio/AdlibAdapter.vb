﻿Public Class AdlibAdapter ' Based on fake86's implementation
    Inherits AudioProvider

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

    Private Structure OplStruct
        Public wave As Byte
    End Structure
    Private Opl(9 - 1)() As OplStruct

    Private Structure Channel
        Public Frequency As UInt16
        Public ConvFreq As Double
        Public KeyOn As Boolean
        Public Octave As UInt16
        Public WaveformSelect As Byte
    End Structure
    Private ReadOnly channels(9 - 1) As Channel

    Private ReadOnly attackTable() As Double = {1.0003, 1.00025, 1.0002, 1.00015, 1.0001, 1.00009, 1.00008, 1.00007, 1.00006, 1.00005, 1.00004, 1.00003, 1.00002, 1.00001, 1.000005}
    Private ReadOnly decayTable() As Double = {0.99999, 0.999985, 0.99998, 0.999975, 0.99997, 0.999965, 0.99996, 0.999955, 0.99995, 0.999945, 0.99994, 0.999935, 0.99994, 0.999925, 0.99992, 0.99991}
    Private ReadOnly oplTable() As Byte = {0, 0, 0, 1, 1, 1, 255, 255, 0, 0, 0, 1, 1, 1, 255, 255, 0, 0, 0, 1, 1, 1}

    Private ReadOnly envelope(9 - 1) As Double
    Private ReadOnly decay(9 - 1) As Double
    Private ReadOnly attack(9 - 1) As Double
    Private ReadOnly attack2(9 - 1) As Boolean

    Private ReadOnly regMem(&HFF - 1) As UInt16
    Private address As UInt16 = 0
    Private percussion As Boolean = False
    Private ReadOnly oplStep(9 - 1) As UInt64

    Private status As Byte

    Private mVolume As Double

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
    Private ReadOnly task As New TaskSC(Me)

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)

        For i As Integer = 0 To Opl.Length - 1
            ReDim Opl(i)(2 - 1)
        Next

        RegisteredPorts.Add(&H388)
        RegisteredPorts.Add(&H389)

        SampleTicks = 10 * Scheduler.HOSTCLOCK \ SpeakerAdapter.SampleRate
        cpu.Sched.RunTaskEach(task, SampleTicks)
    End Sub

    Public Overrides Sub InitAdapter()
        mVolume = 0.7
    End Sub

    Public Overrides Sub CloseAdapter()
    End Sub

    Public Overrides Sub Run()
        For channel As Integer = 0 To 9 - 1
            If Frequency(channel) <> 0 Then
                If attack2(channel) Then
                    envelope(channel) *= decay(channel)
                Else
                    envelope(channel) *= attack(channel)
                    If envelope(channel) >= 1.0 Then attack2(channel) = True
                End If
            End If
        Next
    End Sub

    Public Overrides Function [In](port As UInt16) As Byte
        status = If(regMem(4) <> 0, &H80, 0)
        status += (regMem(4) And 1) * &H40 + (regMem(4) And 2) * &H10
        Return status
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        If port = &H388 Then
            address = value
            Exit Sub
        End If

        port = address
        regMem(port) = value

        Select Case port
            Case 4 ' Timer Control
                If (value And &H80) <> 0 Then regMem(4) = 0

            Case &HBD
                percussion = (value And &H10) <> 0

        End Select

        If port >= &H60 AndAlso port <= &H75 Then ' Attack / Decay
            port = (port And 15) Mod 9

            attack(port) = attackTable((15 - (value >> 4)) Mod 15) * 1.006
            decay(port) = decayTable(value And 15)

        ElseIf port >= &HA0 AndAlso port <= &HB8 Then ' Octave / Frequency / Key On
            port = (port And 15) Mod 9

            If Not channels(port).KeyOn AndAlso ((regMem(&HB0 + port) >> 5) And 1) <> 0 Then
                attack2(port) = False
                envelope(port) = 0.0025
            End If

            channels(port).Frequency = regMem(&HA0 + port) Or ((regMem(&HB0 + port) And 3) << 8)
            channels(port).ConvFreq = channels(port).Frequency * 0.7626459
            channels(port).KeyOn = ((regMem(&HB0 + port) >> 5) And 1) <> 0
            channels(port).Octave = (regMem(&HB0 + port) >> 2) And 7

        ElseIf port >= &HE0 AndAlso port <= &HF5 Then ' Waveform Select
            port = port And 15
            If port < 9 Then channels(port).WaveformSelect = value And 3

        End If
    End Sub

    Private Function Frequency(channel As Byte) As UInt16
        If Not channels(channel).KeyOn Then Return 0
        Dim tmpFreq As UInt16 = channels(channel).ConvFreq

        Select Case channels(channel).Octave
            Case 0 : tmpFreq >>= 4
            Case 1 : tmpFreq >>= 3
            Case 2 : tmpFreq >>= 2
            Case 3 : tmpFreq >>= 1
            Case 5 : tmpFreq <<= 1
            Case 6 : tmpFreq <<= 2
            Case 7 : tmpFreq <<= 3
        End Select

        Return tmpFreq
    End Function

    Private Function AdLibSample(channel As Byte) As Int32
        If percussion AndAlso channel >= 6 AndAlso channel <= 8 Then Return 0

        channel = channel Mod 9

        Dim fullStep As UInt64 = SpeakerAdapter.SampleRate \ Frequency(channel)
        Dim idx As Byte = oplStep(channel) / (fullStep / 256.0)
        Dim tmpSample As Int32 = oplWave(channels(channel).WaveformSelect)(idx)
        Dim tmpStep As Double = envelope(channel)
        If tmpStep > 1.0 Then tmpStep = 1.0
        tmpSample = 2.0 * tmpSample * tmpStep

        oplStep(channel) += 1
        If oplStep(channel) > fullStep Then oplStep(channel) = 0

        Return tmpSample
    End Function

    Private Function GenSample() As Int16
        Dim accumulator As Int16 = 0
        For channel As Byte = 0 To 9 - 1
            If Frequency(channel) <> 0 Then accumulator += AdLibSample(channel)
        Next

        Return accumulator
    End Function

    Public Overrides ReadOnly Property Sample As Int16
        Get
            Return (GenSample() >> 4) * mVolume
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Adlib OPL2" ' FM Operator Type-L
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Yamaha YM3526"
        End Get
    End Property

    Public Overrides ReadOnly Property Type As AdapterType
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
﻿Public Class SpeakerAdapter
    Inherits AudioProvider

    Private Enum WaveForms
        Squared
        Sinusoidal
    End Enum

    Private ReadOnly waveForm As WaveForms = WaveForms.Squared

    Private Const ToRad As Double = Math.PI / 180

    Public Const SampleRate As Integer = 48000

    Private mEnabled As Boolean

    Private mFrequency As Double
    Private waveLength As Integer
    Private halfWaveLength As Integer
    Private currentStep As Integer

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)
        If MyBase.CPU.PIT IsNot Nothing Then MyBase.CPU.PIT.Speaker = Me
    End Sub

    Public Overrides Property Volume As Double = 0.1

    Public Overrides Sub InitAdapter()
        SampleTicks = Scheduler.HOSTCLOCK / SpeakerAdapter.SampleRate
        LastTick = Stopwatch.GetTimestamp()
    End Sub

    Public Property Frequency As Double
        Get
            Return mFrequency
        End Get
        Set(value As Double)
            If mFrequency <> value Then
                mFrequency = value
                UpdateWaveformParameters()
            End If
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

    Private Sub UpdateWaveformParameters()
        If mFrequency > 0 Then
            waveLength = SampleRate / mFrequency
        Else
            waveLength = 0
        End If

        halfWaveLength = waveLength / 2
    End Sub

    Public Overrides Sub CloseAdapter()
    End Sub

    Public Overrides Function GetSample() As Int16
        Dim value As Double
        Select Case waveForm
            Case WaveForms.Squared
                If mEnabled Then
                    If currentStep <= halfWaveLength Then
                        value = -128
                    Else
                        value = 127
                    End If
                Else
                    value = 0
                End If
            Case WaveForms.Sinusoidal
                If mEnabled AndAlso waveLength > 0 Then
                    value = Math.Floor(Math.Sin(currentStep / waveLength * (mFrequency / 2) * ToRad) * 128)
                Else
                    value = 0
                End If
        End Select

        currentStep += 1
        If currentStep >= waveLength Then currentStep = 0

        Return value
    End Function

    Public Overrides Sub Tick()
    End Sub

#Disable Warning BC42353
    Public Overrides Function [In](port As UInt16) As Byte
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)

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

    Public Overrides ReadOnly Property Description As String
        Get
            Return "PC Speaker"
        End Get
    End Property
End Class
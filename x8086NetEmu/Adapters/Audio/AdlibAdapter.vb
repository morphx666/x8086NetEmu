' Yamaha YM3812 (OPL2) FM synthesizer, as found on the AdLib Music Synthesizer Card (ports 388h/389h) and on
' Sound Blaster cards (which forward their FM ports here).
'
' Written from the chip's documented behavior (register map, envelope rate/level scales, waveforms, rhythm
' mode and timers). It is a functional model, not a die-level bit-exact one:
'   - 9 channels x 2 operators, FM or additive connection, modulator self-feedback
'   - Per operator: ADSR envelope with key scaling of rates (KSR), total level, key scaling of level (KSL),
'     frequency multiplier, tremolo (AM) and vibrato, 4 waveforms (when enabled via register 01h)
'   - Rhythm mode (bass drum, snare, tom-tom, top cymbal, hi-hat) with the noise generator
'   - Timer 1 (80 us) / Timer 2 (320 us) and the status register, timed against emulated time
' The chip is clocked at its native rate (3.579545 MHz / 72 = 49716 Hz) and resampled to the mixer's rate.
Public Class AdlibAdapter
    Inherits AudioProvider

    Private Const CHIP_CLOCK As Double = 3_579_545.0
    Private Const CHIP_RATE As Double = CHIP_CLOCK / 72 ' ~49716 Hz

    ' Mixer units for a full-scale channel (the mixer works in signed 8-bit units)
    Private Const OUTPUT_LEVEL As Double = 40

    ' Envelope generator works in attenuation units of 0.1875 dB: 0 = full level, 511 = ~96 dB (silent)
    Private Const ENV_MAX As Double = 511
    Private Const ENV_UNIT_DB As Double = 0.1875

    ' Phase modulation depth: a full-scale modulator shifts the carrier's phase by up to 4 waveform cycles
    Private Const MOD_DEPTH As Double = 4

    Private Shared ReadOnly sineTable(1024 - 1) As Double
    Private Shared ReadOnly levelTable(1024 - 1) As Double       ' Attenuation units -> linear amplitude
    Private Shared ReadOnly attackCoef(64 - 1) As Double         ' Per-sample attack coefficient by effective rate
    Private Shared ReadOnly decayStep(64 - 1) As Double          ' Per-sample decay/release step by effective rate
    Private Shared ReadOnly multTable() As Double = {0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 12, 12, 15, 15}
    Private Shared ReadOnly kslBaseDb() As Double = {0, 9, 12, 13.875, 15, 16.125, 16.875, 17.625,
                                                     18, 18.75, 19.125, 19.5, 19.875, 20.25, 20.625, 21}
    Private Shared ReadOnly kslScale() As Double = {0, 0.5, 0.25, 1} ' KSL 0 / 3.0 / 1.5 / 6.0 dB per octave

    ' Register offset (0-15h) -> operator slot (0-17), -1 for unused offsets
    Private Shared ReadOnly slotFromOffset() As Integer = {0, 1, 2, 3, 4, 5, -1, -1, 6, 7, 8, 9, 10, 11, -1, -1,
                                                           12, 13, 14, 15, 16, 17, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
    ' Channel -> modulator slot (the carrier is always 3 slots later)
    Private Shared ReadOnly modulatorSlot() As Integer = {0, 1, 2, 6, 7, 8, 12, 13, 14}

    Shared Sub New()
        For i As Integer = 0 To 1023
            sineTable(i) = Math.Sin(2 * Math.PI * i / 1024)
            levelTable(i) = Math.Pow(10, -(i * ENV_UNIT_DB) / 20)
        Next
        levelTable(1023) = 0

        ' Rate R = 4 * rate + key scale offset (0-63). From the YM3812 manual: attack (0 -> 100%) takes
        ' 2826 ms and decay/release (0 -> -96 dB) 39280 ms at R = 4; each rate step halves the time and the
        ' two low bits of R subdivide it in quarter steps.
        For r As Integer = 0 To 63
            If r < 4 Then
                attackCoef(r) = 0
                decayStep(r) = 0
                Continue For
            End If
            Dim scale As Double = 4.0 / ((4 + (r And 3)) * Math.Pow(2, (r >> 2) - 1))
            Dim attackSeconds As Double = 2.826 * scale
            Dim decaySeconds As Double = 39.28 * scale
            ' Attack is exponential in the attenuation domain: from 511 down to ~0 takes ln(511) time constants
            attackCoef(r) = If(r >= 60, 1, 1 - Math.Exp(-Math.Log(ENV_MAX) / (attackSeconds * CHIP_RATE)))
            decayStep(r) = (ENV_MAX + 1) / (decaySeconds * CHIP_RATE)
        Next
    End Sub

    Private Enum EnvelopeStates
        Off
        Attack
        Decay
        Sustain
        Release
    End Enum

    Private Class OperatorSlot
        ' Registers
        Public Tremolo As Boolean      ' 20h bit 7
        Public Vibrato As Boolean      ' 20h bit 6
        Public Sustaining As Boolean   ' 20h bit 5 (EG type)
        Public KeyScaleRate As Boolean ' 20h bit 4
        Public Mult As Integer         ' 20h bits 0-3
        Public KSL As Integer          ' 40h bits 6-7
        Public TL As Integer           ' 40h bits 0-5
        Public AR As Integer           ' 60h bits 4-7
        Public DR As Integer           ' 60h bits 0-3
        Public SL As Integer           ' 80h bits 4-7
        Public RR As Integer           ' 80h bits 0-3
        Public WS As Integer           ' E0h bits 0-1

        ' State
        Public KeyOn As Boolean
        Public Phase As Double         ' In waveform cycles
        Public Env As Double = ENV_MAX
        Public State As EnvelopeStates = EnvelopeStates.Off
        Public Out As Double
        Public PrevOut As Double

        Public ReadOnly Property SustainLevel As Double
            Get
                Return If(SL = 15, 496, SL * 16) ' 3 dB steps; SL=15 means 93 dB
            End Get
        End Property
    End Class

    Private Class ChannelState
        Public FNum As Integer
        Public Block As Integer
        Public KeyOn As Boolean
        Public Feedback As Integer
        Public Additive As Boolean
        Public Modulator As OperatorSlot
        Public Carrier As OperatorSlot
    End Class

    Private ReadOnly slots(18 - 1) As OperatorSlot
    Private ReadOnly channels(9 - 1) As ChannelState

    Private address As Byte
    Private waveformSelectEnabled As Boolean
    Private noteSelect As Boolean        ' 08h bit 6 (NTS)
    Private deepTremolo As Boolean       ' BDh bit 7: 4.8 dB instead of 1 dB
    Private deepVibrato As Boolean       ' BDh bit 6: 14 cents instead of 7
    Private rhythmMode As Boolean        ' BDh bit 5
    Private drumKeys As Byte             ' BDh bits 0-4: BD, SD, TOM, TC, HH

    Private tremoloPhase As Double
    Private vibratoPhase As Double
    Private noise As UInteger = 1

    ' Resampling from the chip's rate to the mixer's rate
    Private nativePos As Double
    Private prevNative As Double
    Private curNative As Double

    ' Timers
    Private t1Count As Byte
    Private t2Count As Byte
    Private t1Running As Boolean
    Private t2Running As Boolean
    Private t1Masked As Boolean
    Private t2Masked As Boolean
    Private t1Start As Long
    Private t2Start As Long
    Private t1Ref As Long ' Overflows at or before this time have already been reported/cleared
    Private t2Ref As Long
    Private t1Flag As Boolean
    Private t2Flag As Boolean

    Private mVolume As Double

    Public Sub New(cpu As X8086)
        MyBase.New(cpu)

        For i As Integer = 0 To slots.Length - 1
            slots(i) = New OperatorSlot()
        Next
        For c As Integer = 0 To channels.Length - 1
            channels(c) = New ChannelState() With {
                .Modulator = slots(modulatorSlot(c)),
                .Carrier = slots(modulatorSlot(c) + 3)
            }
        Next

        RegisteredPorts.Add(&H388)
        RegisteredPorts.Add(&H389)
    End Sub

    Public Overrides Sub InitAdapter()
        mVolume = 0.7
    End Sub

    Public Overrides Sub CloseAdapter()
    End Sub

    Public Overrides Sub Run()
    End Sub

#Region "Ports and registers"
    Public Overrides Function [In](port As UInt16) As Byte
        UpdateTimers()
        Dim status As Byte = &H6 ' Bits 1-2 always read as 1 on the OPL2 (0 on the OPL3)
        If t1Flag Then status = status Or &H40
        If t2Flag Then status = status Or &H20
        If t1Flag OrElse t2Flag Then status = status Or &H80
        Return status
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        If (port And 1) = 0 Then
            address = value
        Else
            WriteRegister(address, value)
        End If
    End Sub

    Private Sub WriteRegister(reg As Byte, value As Byte)
        Select Case reg
            Case &H1 : waveformSelectEnabled = (value And &H20) <> 0
            Case &H2 : t1Count = value
            Case &H3 : t2Count = value
            Case &H4 : WriteTimerControl(value)
            Case &H8 : noteSelect = (value And &H40) <> 0

            Case &H20 To &H35, &H40 To &H55, &H60 To &H75, &H80 To &H95, &HE0 To &HF5
                Dim slot As Integer = slotFromOffset(reg And &H1F)
                If slot < 0 Then Exit Sub
                Dim op As OperatorSlot = slots(slot)

                Select Case reg And &HE0
                    Case &H20
                        op.Tremolo = (value And &H80) <> 0
                        op.Vibrato = (value And &H40) <> 0
                        op.Sustaining = (value And &H20) <> 0
                        op.KeyScaleRate = (value And &H10) <> 0
                        op.Mult = value And &HF
                    Case &H40
                        op.KSL = value >> 6
                        op.TL = value And &H3F
                    Case &H60
                        op.AR = value >> 4
                        op.DR = value And &HF
                    Case &H80
                        op.SL = value >> 4
                        op.RR = value And &HF
                    Case &HE0
                        op.WS = value And 3
                End Select

            Case &HA0 To &HA8
                Dim ch As ChannelState = channels(reg - &HA0)
                ch.FNum = (ch.FNum And &H300) Or value

            Case &HB0 To &HB8
                Dim ch As ChannelState = channels(reg - &HB0)
                ch.FNum = (ch.FNum And &HFF) Or ((value And 3) << 8)
                ch.Block = (value >> 2) And 7
                ch.KeyOn = (value And &H20) <> 0
                UpdateKeys()

            Case &HBD
                deepTremolo = (value And &H80) <> 0
                deepVibrato = (value And &H40) <> 0
                rhythmMode = (value And &H20) <> 0
                drumKeys = value And &H1F
                UpdateKeys()

            Case &HC0 To &HC8
                Dim ch As ChannelState = channels(reg - &HC0)
                ch.Feedback = (value >> 1) And 7
                ch.Additive = (value And 1) <> 0
        End Select
    End Sub

    ' An operator is keyed on by its channel's KON bit, or (in rhythm mode) by its drum bit
    Private Sub UpdateKeys()
        For c As Integer = 0 To 8
            Dim ch As ChannelState = channels(c)
            Dim modKey As Boolean = ch.KeyOn
            Dim carKey As Boolean = ch.KeyOn

            If rhythmMode Then
                Select Case c
                    Case 6 ' Bass drum: both operators
                        modKey = modKey OrElse (drumKeys And &H10) <> 0
                        carKey = carKey OrElse (drumKeys And &H10) <> 0
                    Case 7 ' Hi-hat (modulator), snare drum (carrier)
                        modKey = modKey OrElse (drumKeys And &H1) <> 0
                        carKey = carKey OrElse (drumKeys And &H8) <> 0
                    Case 8 ' Tom-tom (modulator), top cymbal (carrier)
                        modKey = modKey OrElse (drumKeys And &H4) <> 0
                        carKey = carKey OrElse (drumKeys And &H2) <> 0
                End Select
            End If

            SetKey(ch.Modulator, modKey)
            SetKey(ch.Carrier, carKey)
        Next
    End Sub

    Private Shared Sub SetKey(op As OperatorSlot, keyOn As Boolean)
        If keyOn = op.KeyOn Then Exit Sub
        op.KeyOn = keyOn
        If keyOn Then
            op.Phase = 0
            op.State = EnvelopeStates.Attack ' Attack starts from the current level
        ElseIf op.State <> EnvelopeStates.Off Then
            op.State = EnvelopeStates.Release
        End If
    End Sub
#End Region

#Region "Timers"
    Private Function TimerPeriod(count As Byte, microseconds As Integer) As Long
        Return (256 - CInt(count)) * microseconds * CLng(Scheduler.HOSTCLOCK) \ 1_000_000
    End Function

    ' Timers count in emulated time; overflow flags are evaluated lazily when the status is read
    Private Sub UpdateTimers()
        Dim now As Long = CPU.Sched.CurrentTime
        If t1Running AndAlso Not t1Masked AndAlso Not t1Flag Then t1Flag = Overflowed(now, t1Start, t1Ref, TimerPeriod(t1Count, 80))
        If t2Running AndAlso Not t2Masked AndAlso Not t2Flag Then t2Flag = Overflowed(now, t2Start, t2Ref, TimerPeriod(t2Count, 320))
    End Sub

    ' The timer reloads and keeps running after each overflow: was there an overflow after 'ref'?
    Private Shared Function Overflowed(now As Long, start As Long, ref As Long, period As Long) As Boolean
        If period <= 0 Then Return False
        Return (now - start) \ period > (ref - start) \ period
    End Function

    Private Sub WriteTimerControl(value As Byte)
        Dim now As Long = CPU.Sched.CurrentTime

        If (value And &H80) <> 0 Then
            ' IRQ reset: clears both flags (the other bits are ignored)
            UpdateTimers()
            t1Flag = False
            t2Flag = False
            t1Ref = now
            t2Ref = now
            Exit Sub
        End If

        t1Masked = (value And &H40) <> 0
        t2Masked = (value And &H20) <> 0

        Dim start1 As Boolean = (value And 1) <> 0
        Dim start2 As Boolean = (value And 2) <> 0
        If start1 AndAlso Not t1Running Then t1Start = now : t1Ref = now
        If start2 AndAlso Not t2Running Then t2Start = now : t2Ref = now
        t1Running = start1
        t2Running = start2
    End Sub
#End Region

#Region "Synthesis"
    ' Called by the mixer at its own sample rate
    Public Overrides ReadOnly Property Sample As Int16
        Get
            nativePos += CHIP_RATE / SpeakerAdapter.SampleRate
            While nativePos >= 1
                prevNative = curNative
                curNative = GenerateChipSample()
                nativePos -= 1
            End While

            ' Linear interpolation between the two most recent chip samples
            Dim v As Double = (prevNative + (curNative - prevNative) * nativePos) * OUTPUT_LEVEL * mVolume
            Return CShort(Math.Max(-32768, Math.Min(32767, Math.Round(v))))
        End Get
    End Property

    Private Function GenerateChipSample() As Double
        ' LFOs: tremolo ~3.7 Hz, vibrato ~6.1 Hz (triangle shaped)
        tremoloPhase += 3.7 / CHIP_RATE
        If tremoloPhase >= 1 Then tremoloPhase -= 1
        vibratoPhase += 6.07 / CHIP_RATE
        If vibratoPhase >= 1 Then vibratoPhase -= 1
        Dim tremoloUnits As Double = (1 - Math.Abs(2 * tremoloPhase - 1)) * If(deepTremolo, 4.8, 1.0) / ENV_UNIT_DB
        Dim vibratoFactor As Double = Math.Pow(2, (1 - 4 * Math.Abs(vibratoPhase - 0.5)) * If(deepVibrato, 14, 7) / 1200)

        ' 23-bit noise generator (used by the rhythm section)
        Dim bit As UInteger = ((noise >> 22) Xor (noise >> 8) Xor (noise >> 1) Xor noise) And 1UI
        noise = ((noise << 1) Or bit) And &H7FFFFFUI

        Dim sum As Double = 0
        Dim melodicChannels As Integer = If(rhythmMode, 6, 9)

        For c As Integer = 0 To melodicChannels - 1
            Dim ch As ChannelState = channels(c)
            If IsSilent(ch.Carrier) AndAlso IsSilent(ch.Modulator) Then
                ' Keep the modulator's feedback history consistent while the channel is silent
                ch.Modulator.PrevOut = 0
                ch.Modulator.Out = 0
                Continue For
            End If
            sum += ChannelOutput(ch, tremoloUnits, vibratoFactor)
        Next

        If rhythmMode Then sum += RhythmOutput(tremoloUnits, vibratoFactor)

        Return sum
    End Function

    Private Shared Function IsSilent(op As OperatorSlot) As Boolean
        Return op.State = EnvelopeStates.Off
    End Function

    Private Function ChannelOutput(ch As ChannelState, tremoloUnits As Double, vibratoFactor As Double) As Double
        Dim m As OperatorSlot = ch.Modulator
        Dim feedback As Double = If(ch.Feedback = 0, 0, (m.Out + m.PrevOut) * MOD_DEPTH / (1 << (9 - ch.Feedback)))
        Dim modOut As Double = OperatorOutput(m, ch, feedback, tremoloUnits, vibratoFactor)
        m.PrevOut = m.Out
        m.Out = modOut

        If ch.Additive Then
            Return modOut + OperatorOutput(ch.Carrier, ch, 0, tremoloUnits, vibratoFactor)
        Else
            Return OperatorOutput(ch.Carrier, ch, modOut * MOD_DEPTH, tremoloUnits, vibratoFactor)
        End If
    End Function

    ' Rhythm mode: channel 6 = bass drum (normal 2-op), channel 7 = hi-hat (mod) + snare (car),
    ' channel 8 = tom-tom (mod) + top cymbal (car). Hi-hat, snare and cymbal derive their waveform from
    ' bits of the hi-hat and cymbal operators' phases mixed with noise. Rhythm outputs are doubled.
    Private Function RhythmOutput(tremoloUnits As Double, vibratoFactor As Double) As Double
        Dim bd As ChannelState = channels(6)
        Dim ch7 As ChannelState = channels(7)
        Dim ch8 As ChannelState = channels(8)
        Dim hh As OperatorSlot = ch7.Modulator
        Dim sd As OperatorSlot = ch7.Carrier
        Dim tom As OperatorSlot = ch8.Modulator
        Dim tc As OperatorSlot = ch8.Carrier

        Dim sum As Double = 0
        If Not (IsSilent(bd.Carrier) AndAlso IsSilent(bd.Modulator)) Then sum += ChannelOutput(bd, tremoloUnits, vibratoFactor)

        ' Advance every rhythm operator's phase (the phase bits are shared between drums)
        AdvancePhase(hh, ch7, vibratoFactor)
        AdvancePhase(sd, ch7, vibratoFactor)
        AdvancePhase(tom, ch8, vibratoFactor)
        AdvancePhase(tc, ch8, vibratoFactor)

        Dim p13 As Integer = CInt(Math.Floor(hh.Phase * 1024)) And 1023
        Dim p17 As Integer = CInt(Math.Floor(tc.Phase * 1024)) And 1023
        Dim phaseBit As Integer = ((p13 >> 2) Xor (p13 >> 7)) Or (p13 >> 3)
        If (((p17 >> 3) Xor (p17 >> 5)) And 1) <> 0 Then phaseBit = 1
        phaseBit = phaseBit And 1
        Dim noiseBit As Integer = CInt(noise And 1UI)

        If Not IsSilent(hh) Then
            Dim idx As Integer = (phaseBit << 9) Or If((phaseBit Xor noiseBit) <> 0, &HD0, &H34)
            sum += EnvelopeAndLevel(hh, ch7, tremoloUnits) * Waveform(hh, idx)
        End If
        If Not IsSilent(sd) Then
            Dim bit8 As Integer = (p13 >> 8) And 1
            Dim idx As Integer = (bit8 << 9) Or ((bit8 Xor noiseBit) << 8)
            sum += EnvelopeAndLevel(sd, ch7, tremoloUnits) * Waveform(sd, idx)
        End If
        If Not IsSilent(tom) Then
            sum += EnvelopeAndLevel(tom, ch8, tremoloUnits) * Waveform(tom, CInt(Math.Floor(tom.Phase * 1024)) And 1023)
        End If
        If Not IsSilent(tc) Then
            Dim idx As Integer = (phaseBit << 9) Or &H80
            sum += EnvelopeAndLevel(tc, ch8, tremoloUnits) * Waveform(tc, idx)
        End If

        Return sum * 2
    End Function

    Private Function OperatorOutput(op As OperatorSlot, ch As ChannelState, phaseMod As Double, tremoloUnits As Double, vibratoFactor As Double) As Double
        Dim level As Double = EnvelopeAndLevel(op, ch, tremoloUnits)
        Dim p As Double = op.Phase + phaseMod
        AdvancePhase(op, ch, vibratoFactor)
        If level = 0 Then Return 0

        Dim idx As Integer = CInt(Math.Floor((p - Math.Floor(p)) * 1024)) And 1023
        Return level * Waveform(op, idx)
    End Function

    Private Sub AdvancePhase(op As OperatorSlot, ch As ChannelState, vibratoFactor As Double)
        ' f = FNum * 2^Block * CHIP_RATE / 2^20 * Mult  ->  cycles per chip sample = FNum * 2^Block / 2^20 * Mult
        Dim inc As Double = ch.FNum * (1 << ch.Block) / 1048576.0 * multTable(op.Mult)
        If op.Vibrato Then inc *= vibratoFactor
        op.Phase += inc
        If op.Phase >= 1 Then op.Phase -= Math.Floor(op.Phase)
    End Sub

    ' Waveforms: 0 sine, 1 half-sine, 2 absolute sine, 3 quarter-sine pulses (only with WSE set in 01h)
    Private Function Waveform(op As OperatorSlot, idx As Integer) As Double
        Dim s As Double = sineTable(idx)
        Select Case If(waveformSelectEnabled, op.WS, 0)
            Case 1 : Return If(s < 0, 0, s)
            Case 2 : Return Math.Abs(s)
            Case 3 : Return If((idx And 256) <> 0, 0, Math.Abs(s))
            Case Else : Return s
        End Select
    End Function

    ' Advances the envelope one chip sample and returns the operator's linear output level
    Private Function EnvelopeAndLevel(op As OperatorSlot, ch As ChannelState, tremoloUnits As Double) As Double
        ' Key scale rate offset from the key code (block + one F-number bit selected by NTS)
        Dim keyCode As Integer = (ch.Block << 1) Or If(noteSelect, (ch.FNum >> 8) And 1, (ch.FNum >> 9) And 1)
        Dim rof As Integer = If(op.KeyScaleRate, keyCode, keyCode >> 2)

        Select Case op.State
            Case EnvelopeStates.Attack
                If op.AR <> 0 Then
                    op.Env -= op.Env * attackCoef(Math.Min(63, op.AR * 4 + rof))
                    If op.Env < 1 Then
                        op.Env = 0
                        op.State = EnvelopeStates.Decay
                    End If
                End If

            Case EnvelopeStates.Decay
                Dim target As Double = op.SustainLevel
                If op.DR <> 0 Then op.Env += decayStep(Math.Min(63, op.DR * 4 + rof))
                If op.Env >= target Then
                    op.Env = target
                    op.State = EnvelopeStates.Sustain
                End If

            Case EnvelopeStates.Sustain
                ' Sustaining instruments hold the level while keyed on; percussive ones keep fading at the release rate
                If Not op.Sustaining AndAlso op.RR <> 0 Then op.Env = Math.Min(ENV_MAX, op.Env + decayStep(Math.Min(63, op.RR * 4 + rof)))

            Case EnvelopeStates.Release
                If op.RR <> 0 Then op.Env += decayStep(Math.Min(63, op.RR * 4 + rof))
                If op.Env >= ENV_MAX Then
                    op.Env = ENV_MAX
                    op.State = EnvelopeStates.Off
                End If

            Case EnvelopeStates.Off
                Return 0
        End Select

        ' Total attenuation: envelope + total level (0.75 dB steps) + key scale level + tremolo
        Dim kslDb As Double = Math.Max(0, kslBaseDb(ch.FNum >> 6) - 6 * (7 - ch.Block)) * kslScale(op.KSL)
        Dim units As Double = op.Env + op.TL * 4 + kslDb / ENV_UNIT_DB + If(op.Tremolo, tremoloUnits, 0)
        If units >= 1023 Then Return 0
        Return levelTable(CInt(units))
    End Function
#End Region

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Adlib OPL2" ' FM Operator Type-L II
        End Get
    End Property

    Public Overrides ReadOnly Property Description As String
        Get
            Return "Yamaha YM3812"
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
            Return 2
        End Get
    End Property

    Public Overrides ReadOnly Property VersionMinor As Integer
        Get
            Return 0
        End Get
    End Property

    Public Overrides ReadOnly Property VersionRevision As Integer
        Get
            Return 0
        End Get
    End Property
End Class

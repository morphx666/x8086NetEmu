#If Win32 Then
Imports System.Threading
Imports System.Threading.Tasks
Imports NAudio.Wave

Public Class SoundBlaster ' Based on fake86's implementation
    Inherits Adapter
    Implements IDMADevice

    Private waveOut As WaveOut
    Private audioProvider As SpeakerAdpater.CustomBufferProvider

    Private Structure BlasterData
        Public Mem() As Byte
        Public MemPtr As UInt16
        Public SampleRate As UInt16
        Public DspMaj As Byte
        Public DspMin As Byte
        Public OutputEnabled As Boolean
        Public LastResetVal As Byte
        Public LastCmdVal As Byte
        Public LastTestVal As Byte
        Public WaitForArg As Byte
        Public Paused8 As Boolean
        Public Paused16 As Boolean
        Public Sample As Byte
        Public Irq As InterruptRequest
        Public Dma As Byte
        Public UsingDma As Boolean
        Public MaskDma As Byte
        Public UseAutoInit As Boolean
        Public BlockSize As UInt32
        Public BlockStep As UInt32
        Public SampleTicks As UInt64

        Public Structure Mixer
            Public Index As Byte
            Public Reg() As Byte
        End Structure
        Dim MixerData As Mixer
    End Structure
    Private blaster As BlasterData

    Private mixer(256 - 1) As Byte
    Private mixerIndex As Byte

    Private dmaChannel As DMAI8237.Channel

    Private adLib As AdlibAdapter

    Public Sub New(cpu As X8086, adlib As AdlibAdapter, Optional port As UInt16 = &H220, Optional irq As Byte = 5, Optional dmaChannel As Byte = 1)
        MyBase.New(cpu)
        Me.adLib = adlib

        ReDim blaster.Mem(1024 - 1)
        ReDim blaster.MixerData.Reg(256 - 1)

        blaster.Irq = cpu.PIC?.GetIrqLine(irq)
        blaster.Dma = dmaChannel

        Me.dmaChannel = cpu.DMA.GetChannel(blaster.Dma)
        cpu.DMA.BindChannel(blaster.Dma, Me)

        For i As UInt16 = port To port + &HE
            RegisteredPorts.Add(i)
        Next
    End Sub

    Private Sub SetSampleTicks()
        If blaster.SampleRate = 0 Then
            blaster.SampleTicks = 0
        Else
            blaster.SampleTicks = Scheduler.HOSTCLOCK / blaster.SampleRate

            If blaster.SampleRate <> audioProvider.WaveFormat.SampleRate Then
                waveOut.Dispose()

                audioProvider = New SpeakerAdpater.CustomBufferProvider(AddressOf FillAudioBuffer, blaster.SampleRate, 8, 1)
                waveOut.Init(audioProvider)
                waveOut.Play()
            End If
        End If
    End Sub

    Public Overrides Sub InitiAdapter()
        blaster.DspMaj = 2 ' Emulate a Sound Blaster Pro 2.0
        blaster.DspMin = 0
        MixerReset()

        waveOut = New WaveOut() With {
            .NumberOfBuffers = 16
        }

        audioProvider = New SpeakerAdpater.CustomBufferProvider(AddressOf FillAudioBuffer, SpeakerAdpater.SampleRate, 8, 1)
        waveOut.Init(audioProvider)
        waveOut.Play()
    End Sub

    Private Sub CmdBlaster(value As Byte)
        Dim recognized As Boolean = True

        If blaster.WaitForArg <> 0 Then
            Select Case blaster.LastCmdVal
                Case &H10 ' direct 8-bit sample output
                    blaster.Sample = value

                Case &H14, &H24, &H91 ' 8-bit single block DMA output
                    If blaster.WaitForArg = 2 Then
                        blaster.BlockSize = (blaster.BlockSize And &HFF00) Or value
                        blaster.WaitForArg = 3
                        Exit Sub
                    Else
                        blaster.BlockSize = (blaster.BlockSize And &HFF) Or (CUInt(value) << 8)

                        blaster.UsingDma = True
                        blaster.BlockStep = 0
                        blaster.UseAutoInit = False
                        blaster.Paused8 = False
                        blaster.OutputEnabled = True
                    End If
                Case &H40 ' set time constant
                    blaster.SampleRate = 1000000 / (256 - value)
                    SetSampleTicks()

                Case &H48 ' set DSP block transfer size
                    If blaster.WaitForArg = 2 Then
                        blaster.BlockSize = (blaster.BlockSize And &HFF00) Or value
                        blaster.WaitForArg = 3
                        Exit Sub
                    Else
                        blaster.BlockSize = (blaster.BlockSize And &HFF) Or (CUInt(value) << 8)
                        blaster.BlockStep = 0
                    End If

                Case &HE0 ' DSP identification for Sound Blaster 2.0 and newer (invert each bit and put in read buffer)
                    BufNewData(Not value)

                Case &HE4 ' DSP write test, put data value into read buffer
                    BufNewData(value)
                    blaster.LastTestVal = value

                Case Else
                    recognized = False

            End Select

            If recognized Then Exit Sub
        End If

        Select Case value
            Case &H10, &H40, &HE0, &HE4
                blaster.WaitForArg = 1

            Case &H14, &H24, &H48, &H91 ' 8-bit single block DMA output
                blaster.WaitForArg = 2

            Case &H1C, &H2C ' 8-bit auto-init DMA output
                blaster.UsingDma = True
                blaster.BlockStep = 0
                blaster.UseAutoInit = True
                blaster.Paused8 = False
                blaster.OutputEnabled = True

            Case &HD0 ' pause 8-bit DMA I/O
                blaster.Paused8 = True

            Case &HD1 ' speaker output on
                blaster.OutputEnabled = True

            Case &HD3 ' speaker output off
                blaster.OutputEnabled = True

            Case &HD4 ' continue 8-bit DMA I/O
                blaster.Paused8 = False

            Case &HD8 ' get speaker status
                If blaster.OutputEnabled Then
                    BufNewData(&HFF)
                Else
                    BufNewData(&H0)
                End If

            Case &HDA ' exit 8-bit auto-init DMA I/O mode
                blaster.UsingDma = False

            Case &HE1   ' get DSP version info
                blaster.MemPtr = 0
                BufNewData(blaster.DspMaj)
                BufNewData(blaster.DspMin)

            Case &HE8 ' DSP read test
                blaster.MemPtr = 0
                BufNewData(blaster.LastTestVal)

            Case &HF2 ' force 8-bit IRQ
                blaster.Irq.Raise(True)

            Case &HF8 ' undocumented command, clears in-buffer And inserts a null byte
                blaster.MemPtr = 0
                BufNewData(0)

        End Select
    End Sub

    Public Overrides Sub CloseAdapter()
        waveOut.Stop()
        waveOut.Dispose()
    End Sub

    Private Sub MixerReset()
        Array.Clear(blaster.MixerData.Reg, 0, blaster.MixerData.Reg.Length)

        Dim v As Byte = (4 << 5) Or (4 << 1)
        blaster.MixerData.Reg(&H4) = v
        blaster.MixerData.Reg(&H22) = v
        blaster.MixerData.Reg(&H26) = v
    End Sub

    Private Sub FillAudioBuffer(buffer() As Byte)
        If blaster.SampleRate > 0 Then
            For i As Integer = 0 To buffer.Length - 1
                TickBlaster()
                buffer(i) = GetBlasterSample()
            Next
        End If
    End Sub

    Private Function GetBlasterSample() As UInt16
        If Not blaster.OutputEnabled Then Return 128
        Return CUInt(blaster.Sample) - 128
    End Function

    Private Sub BufNewData(value As Byte)
        If blaster.MemPtr < blaster.Mem.Length Then
            blaster.Mem(blaster.MemPtr) = value
            blaster.MemPtr += 1
        End If
    End Sub

    Public Overrides Function [In](port As UInt16) As Byte
        Select Case port And &HF
            Case &H0, &H8 : Return adLib.In(&H388)
            Case &H1, &H9 : Return adLib.In(&H389)
            Case &H5 : Return mixer(mixerIndex)
            Case &HA ' read data
                If blaster.MemPtr = 0 Then
                    Return 0
                Else
                    Dim r As Byte = blaster.Mem(0)
                    Array.Copy(blaster.Mem, 0, blaster.Mem, 1, blaster.Mem.Length - 1)
                    blaster.MemPtr -= 1
                    Return r
                End If

            Case &HE ' read-buffer status
                If blaster.MemPtr > 0 Then
                    Return &H80
                Else
                    Return &H0
                End If

            Case Else : Return &H0

        End Select
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        Select Case port And &HF
            Case &H0, &H8 : adLib.Out(&H388, value)
            Case &H1, &H9 : adLib.Out(&H389, value)
            Case &H4 : mixerIndex = value
            Case &H5 : mixer(mixerIndex) = value
            Case &H6 ' reset port
                If (value = &H0) And (blaster.LastResetVal = &H1) Then
                    blaster.OutputEnabled = False
                    blaster.Sample = 128
                    blaster.WaitForArg = 0
                    blaster.MemPtr = 0
                    blaster.UsingDma = False
                    blaster.BlockSize = 65535
                    blaster.BlockStep = 0
                    BufNewData(&HAA)
                    For i As Integer = 0 To mixer.Length - 1 : mixer(i) = &HEE : Next
                End If
                blaster.LastResetVal = value

            Case &HC ' write command/data
                CmdBlaster(value)
                If blaster.WaitForArg <> 3 Then blaster.LastCmdVal = value

        End Select
    End Sub

    Private Sub TickBlaster()
        If Not blaster.UsingDma Then Exit Sub

        ReadDMA()
        blaster.BlockStep += 1
        If blaster.BlockStep >= blaster.BlockSize Then
            blaster.Irq.Raise(True)

            If blaster.UseAutoInit Then
                blaster.BlockStep = 0
            Else
                blaster.UsingDma = False
            End If
        End If
    End Sub

    Public Sub ReadDMA()
        If dmaChannel.Masked <> 0 Then blaster.Sample = 128
        If dmaChannel.AutoInit <> 0 AndAlso dmaChannel.CurrentCount > dmaChannel.BaseCount Then dmaChannel.CurrentCount = 0
        If dmaChannel.CurrentCount > dmaChannel.BaseCount Then blaster.Sample = 128

        ' page = 524288
        ' addr = 38686
        ' count = 0
        If dmaChannel.Direction = 0 Then
            blaster.Sample = CPU.Memory((dmaChannel.Page << 16) + dmaChannel.CurrentAddress + dmaChannel.CurrentCount)
        Else
            blaster.Sample = CPU.Memory((dmaChannel.Page << 16) + dmaChannel.CurrentAddress - dmaChannel.CurrentCount)
        End If
        dmaChannel.CurrentCount += 1
    End Sub

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.AudioDevice
        End Get
    End Property

    Public Overrides Sub Run()
        X8086.Notify($"{Name} Running", X8086.NotificationReasons.Info)
    End Sub

    Public Sub DMARead(b As Byte) Implements IDMADevice.DMARead
        Throw New NotImplementedException()
    End Sub

    Public Function DMAWrite() As Byte Implements IDMADevice.DMAWrite
        Throw New NotImplementedException()
    End Function

    Public Sub DMAEOP() Implements IDMADevice.DMAEOP
        Throw New NotImplementedException()
    End Sub

    Public Overrides ReadOnly Property Vendor As String
        Get
            Return "Creative Technology Pte Ltd"
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
            Return "Sound Blaster Pro 2.0"
        End Get
    End Property

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Sound Blaster Pro 2.0"
        End Get
    End Property
End Class
#End If
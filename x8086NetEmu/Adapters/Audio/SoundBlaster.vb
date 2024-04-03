Public Class SoundBlaster ' Based on fake86's implementation
    Inherits AudioProvider
    Implements IDMADevice

    Private Structure BlasterData
        Public Mem() As Byte
        Public MemPtr As UInt16
        Public SampleRate As UInt16
        Public DspMajor As Byte
        Public DspMinor As Byte
        Public OutputEnabled As Boolean
        Public LastResetVal As Byte
        Public LastCmdVal As Byte
        Public LastTestValue As Byte
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

        Public Structure MixerData
            Public Index As Byte
            Public Register() As Byte
        End Structure
        Dim Mixer As MixerData
    End Structure
    Private blaster As BlasterData

    'Private mixer(256 - 1) As Byte
    'Private mixerIndex As Byte

    Private ReadOnly dmaChannel As DMAI8237.Channel

    Private adLib As AdlibAdapter
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

    Public Sub New(cpu As X8086, adlib As AdlibAdapter, Optional port As UInt16 = &H220, Optional irq As Byte = 5, Optional dmaChannel As Byte = 1)
        MyBase.New(cpu)
        Me.adLib = adlib

        ReDim blaster.Mem(1024 - 1)
        ReDim blaster.Mixer.Register(256 - 1)

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
            blaster.SampleTicks = 10 * Scheduler.HOSTCLOCK \ blaster.SampleRate

            task.Cancel()
            CPU.Sched.RunTaskEach(task, blaster.SampleTicks)
        End If
    End Sub

    Public Overrides Sub InitAdapter()
        blaster.DspMajor = 2 ' Emulate a Sound Blaster Pro 2.0
        blaster.DspMinor = 0
        MixerReset()

        mVolume = 1.0
    End Sub

    Private Sub ProcessCommand(value As Byte)
        Dim recognized As Boolean = True

        If blaster.WaitForArg <> 0 Then
            Select Case blaster.LastCmdVal
                Case &H10 ' Direct 8-bit Sample Output
                    blaster.Sample = value

                Case &H14, &H24, &H91 ' 8-bit Single Block DMA Output
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
                Case &H40 ' Set Time Constant
                    blaster.SampleRate = 1000000 / (256 - value)
                    SetSampleTicks()

                Case &H48 ' Set DSP Block Transfer Size
                    If blaster.WaitForArg = 2 Then
                        blaster.BlockSize = (blaster.BlockSize And &HFF00) Or value
                        blaster.WaitForArg = 3
                        Exit Sub
                    Else
                        blaster.BlockSize = (blaster.BlockSize And &HFF) Or (CUInt(value) << 8)
                        blaster.BlockStep = 0
                    End If

                Case &HE0 ' DSP Identification For Sound Blaster 2.0 And Newer (Invert Each Bit And Put In Read Buffer)
                    WriteByteToBuffer(Not value)

                Case &HE4 ' DSP Write Test, Put Data Value Into Read Buffer
                    WriteByteToBuffer(value)
                    blaster.LastTestValue = value

                Case Else
                    recognized = False

            End Select

            If recognized Then Exit Sub
        End If

        Select Case value
            Case &H10, &H40, &HE0, &HE4
                blaster.WaitForArg = 1

            Case &H14, &H24, &H48, &H91 ' 8-bit Single Block DMA Output
                blaster.WaitForArg = 2

            Case &H1C, &H2C ' 8-bit Auto-Init DMA Output
                blaster.UsingDma = True
                blaster.BlockStep = 0
                blaster.UseAutoInit = True
                blaster.Paused8 = False
                blaster.OutputEnabled = True

            Case &HD0 ' Pause 8-bit DMA I/O
                blaster.Paused8 = True

            Case &HD1 ' Speaker Output On
                blaster.OutputEnabled = True

            Case &HD3 ' Speaker Output Off
                blaster.OutputEnabled = False

            Case &HD4 ' Continue 8-bit DMA I/O
                blaster.Paused8 = False

            Case &HD8 ' Get Speaker Status
                If blaster.OutputEnabled Then
                    WriteByteToBuffer(&HFF)
                Else
                    WriteByteToBuffer(&H0)
                End If

            Case &HDA ' Exit 8-bit Auto-Init DMA I/O Mode
                blaster.UsingDma = False

            Case &HE1   ' Get DSP Version Info
                blaster.MemPtr = 0
                WriteByteToBuffer(blaster.DspMajor)
                WriteByteToBuffer(blaster.DspMinor)

            Case &HE8 ' DSP Read Test
                blaster.MemPtr = 0
                WriteByteToBuffer(blaster.LastTestValue)

            Case &HF2 ' Force 8-bit IRQ
                blaster.Irq.Raise(True)

            Case &HF8 ' Undocumented Command, Clears In-Buffer And Inserts A Null Byte
                blaster.MemPtr = 0
                WriteByteToBuffer(0)

        End Select
    End Sub

    Public Overrides Sub CloseAdapter()
    End Sub

    Private Sub MixerReset()
        Array.Clear(blaster.Mixer.Register, 0, blaster.Mixer.Register.Length)

        Dim v As Byte = (4 << 5) Or (4 << 1)
        blaster.Mixer.Register(&H4) = v
        blaster.Mixer.Register(&H22) = v
        blaster.Mixer.Register(&H26) = v
    End Sub

    Public Function GetSample() As Int16
        If Not blaster.OutputEnabled Then Return 0
        Return blaster.Sample
    End Function

    Public Overrides ReadOnly Property Sample As Int16
        Get
            Dim s = GetSample()

            'If s <= 0 Then
            '    s = 128 + s
            'Else
            '    s += 127
            'End If

            Return s * mVolume
        End Get
    End Property

    Private Sub WriteByteToBuffer(value As Byte)
        If blaster.MemPtr < blaster.Mem.Length Then
            blaster.Mem(blaster.MemPtr) = value
            blaster.MemPtr += 1
        End If
    End Sub

    Public Overrides Function [In](port As UInt16) As Byte
        Select Case port And &HF
            Case &H0, &H8 : Return adLib.In(&H388)
            Case &H1, &H9 : Return adLib.In(&H389)
            Case &H5 : Return blaster.Mixer.Register(blaster.Mixer.Index) ' mixer(mixerIndex)
            Case &HA ' read data
                If blaster.MemPtr = 0 Then
                    Return 0
                Else
                    Dim r As Byte = blaster.Mem(0)
                    Array.Copy(blaster.Mem, 1, blaster.Mem, 0, blaster.Mem.Length - 1)
                    blaster.MemPtr -= 1
                    Return r
                End If

            Case &HE ' read-buffer status
                If blaster.MemPtr > 0 Then
                    Return &H80
                Else
                    Return 0
                End If

            Case Else : Return &H0

        End Select
    End Function

    Public Overrides Sub Out(port As UInt16, value As Byte)
        Select Case port And &HF
            Case &H0, &H8 : adLib.Out(&H388, value)
            Case &H1, &H9 : adLib.Out(&H389, value)
            Case &H4 : blaster.Mixer.Index = value ' mixerIndex = value
            Case &H5 : blaster.Mixer.Register(blaster.Mixer.Index) = value ' mixer(mixerIndex) = value
            Case &H6 ' Reset Port
                If (value = &H0) And (blaster.LastResetVal = &H1) Then
                    blaster.OutputEnabled = False
                    blaster.Sample = 128
                    blaster.WaitForArg = 0
                    blaster.MemPtr = 0
                    blaster.UsingDma = False
                    blaster.BlockSize = 65535
                    blaster.BlockStep = 0
                    WriteByteToBuffer(&HAA)
                    'For i As Integer = 0 To mixer.Length - 1 : mixer(i) = &HEE : Next
                    For i As Integer = 0 To blaster.Mixer.Register.Length - 1 : blaster.Mixer.Register(i) = &HEE : Next
                End If
                blaster.LastResetVal = value

            Case &HC ' Write Command/Data
                ProcessCommand(value)
                If blaster.WaitForArg <> 3 Then blaster.LastCmdVal = value

        End Select
    End Sub

    Public Overrides Sub Run()
        If blaster.UsingDma Then
            ReadDMA()

            blaster.BlockStep += 1
            If blaster.BlockStep > blaster.BlockSize Then
                blaster.Irq.Raise(True)

                If blaster.UseAutoInit Then
                    blaster.BlockStep = 0
                Else
                    blaster.UsingDma = False
                End If
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
            ' fake: 562974
            ' ours: 568697
            blaster.Sample = CPU.Memory(dmaChannel.Page + dmaChannel.CurrentAddress + dmaChannel.CurrentCount)
        Else
            blaster.Sample = CPU.Memory(dmaChannel.Page + dmaChannel.CurrentAddress - dmaChannel.CurrentCount)
        End If
        dmaChannel.CurrentCount += 1
    End Sub

    Public Overrides ReadOnly Property Type As AdapterType
        Get
            Return AdapterType.AudioDevice
        End Get
    End Property

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
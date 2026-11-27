' TEMPORARY DEBUGGING CODE - remove once the crash it was added for is solved
' (also remove the two "CrashCapture" lines in X8086.PreExecute).
'
' Keeps a ring buffer with the CPU state before each of the last HIST_SIZE instructions. When execution runs into
' a long run of "00 00" (ADD [BX+SI],AL: the CPU is executing empty memory, i.e. it has derailed), recording
' stops and the history plus a full memory dump are written next to the emulator:
'   crash-history-<time>.txt   one line per instruction, oldest first
'   crash-memory-<time>.bin    the CPU's memory array at the moment of detection
Partial Public Class X8086
    Public Shared CrashCaptureEnabled As Boolean = True

    Private Const HIST_SIZE As Integer = 65536 ' Power of 2
    Private Const HIST_FIELDS As Integer = 14
    Private Const ZERO_RUN_LIMIT As Integer = 16

    Private ReadOnly hist(HIST_SIZE * HIST_FIELDS - 1) As UInt16
    Private histIndex As Integer
    Private histCount As Long
    Private histFrozen As Boolean
    Private zeroRun As Integer

    ' Called before each instruction is fetched (after any interrupt was taken, so handler entries show up)
    Private Sub CrashCaptureRecord()
        If histFrozen Then Exit Sub

        Dim i As Integer = histIndex * HIST_FIELDS
        With mRegisters
            hist(i) = .CS : hist(i + 1) = .IP
            hist(i + 2) = .SS : hist(i + 3) = .SP
            hist(i + 4) = .DS : hist(i + 5) = .ES
            hist(i + 6) = .AX : hist(i + 7) = .BX
            hist(i + 8) = .CX : hist(i + 9) = .DX
            hist(i + 10) = .SI : hist(i + 11) = .DI
            hist(i + 12) = .BP
        End With
        hist(i + 13) = mFlags.EFlags
        histIndex = (histIndex + 1) And (HIST_SIZE - 1)
        histCount += 1

        ' X8086_CAPTURE_AT=seg:off[,seg:off...] saves the history the first time execution reaches each address
        If captureAt Is Nothing Then
            captureAt = New HashSet(Of UInteger)()
            For Each p As String In If(Environment.GetEnvironmentVariable("X8086_CAPTURE_AT"), "").Split(","c, StringSplitOptions.RemoveEmptyEntries)
                Dim t() As String = p.Trim().Split(":"c)
                If t.Length = 2 Then captureAt.Add((Convert.ToUInt32(t(0), 16) << 16) Or Convert.ToUInt32(t(1), 16))
            Next
            SetUpWriteWatch()
            ' X8086_CAPTURE_SP=ss:sp saves the history the first time SP drops below sp while SS = ss (stack leaks)
            Dim spSpec As String = Environment.GetEnvironmentVariable("X8086_CAPTURE_SP")
            If Not String.IsNullOrWhiteSpace(spSpec) Then
                Dim t() As String = spSpec.Trim().Split(":"c)
                spWatchSS = Convert.ToUInt16(t(0), 16)
                spWatchSP = Convert.ToUInt16(t(1), 16)
                spWatchArmed = True
            End If
        End If
        If spWatchArmed AndAlso mRegisters.SS = spWatchSS AndAlso mRegisters.SP >= spWatchSP Then spWatchAbove = True
        ' A leak shrinks SP gradually; a value far below is a stack switch in progress (SS loaded, SP not yet)
        If spWatchArmed AndAlso spWatchAbove AndAlso mRegisters.SS = spWatchSS AndAlso
           mRegisters.SP < spWatchSP AndAlso mRegisters.SP >= spWatchSP - &H400 Then
            spWatchArmed = False
            captureReason = $"SP dropped below {spWatchSS:X4}:{spWatchSP:X4} (X8086_CAPTURE_SP)"
            CrashCaptureDump()
            histFrozen = False ' Keep recording
            Exit Sub
        End If
        If captureAt.Count > 0 Then
            Dim key As UInteger = (CUInt(mRegisters.CS) << 16) Or mRegisters.IP
            If captureAt.Remove(key) Then
                captureReason = $"execution reached {mRegisters.CS:X4}:{mRegisters.IP:X4} (X8086_CAPTURE_AT)"
                CrashCaptureDump()
                histFrozen = False ' Keep recording
                Exit Sub
            End If
        End If

        ' HLT with interrupts disabled stops the machine for good
        If opCode = &HF4 AndAlso mFlags.IF = 0 Then
            captureReason = "HLT with interrupts disabled"
            CrashCaptureDump()
            Exit Sub
        End If

        ' Count "00 00" instructions that follow each other in memory (a derailed CPU sliding through empty memory)
        If opCode = 0 AndAlso RAM8(mRegisters.CS, mRegisters.IP, 1, True) = 0 Then
            If zeroRun > 0 AndAlso mRegisters.CS = zeroRunCS AndAlso mRegisters.IP = CUShort((zeroRunIP + 2) And &HFFFF) Then
                zeroRun += 1
            Else
                zeroRun = 1
            End If
            zeroRunCS = mRegisters.CS
            zeroRunIP = mRegisters.IP
            If zeroRun >= ZERO_RUN_LIMIT Then
                captureReason = $"{ZERO_RUN_LIMIT} consecutive '00 00' instructions"
                CrashCaptureDump()
            End If
        Else
            zeroRun = 0
        End If
    End Sub
    Private captureAt As HashSet(Of UInteger)
    Private spWatchArmed As Boolean
    Private spWatchAbove As Boolean
    Private spWatchSS As UInt16
    Private spWatchSP As UInt16
    Private zeroRunCS As UInt16
    Private zeroRunIP As UInt16
    Private captureReason As String = ""

    ' X8086_WATCH=seg:off[:length] saves the history whenever the CPU changes a byte in that range (up to
    ' WATCH_LIMIT times). The last line of each history is the instruction that wrote it.
    Private Const WATCH_LIMIT As Integer = 6
    Private Const WATCH_ARM_AFTER As Long = 50_000_000 ' Instructions: skip the BIOS memory test and the DOS boot
    Private watchStart As UInteger
    Private watchEnd As UInteger
    Private watchHits As Integer

    Private Sub SetUpWriteWatch()
        Dim spec As String = Environment.GetEnvironmentVariable("X8086_WATCH")
        If String.IsNullOrWhiteSpace(spec) Then Exit Sub
        Dim t() As String = spec.Trim().Split(":"c)
        watchStart = SegmentOffsetToAbsolute(Convert.ToUInt16(t(0), 16), Convert.ToUInt16(t(1), 16))
        watchEnd = watchStart + If(t.Length > 2, Convert.ToUInt32(t(2), 16), 1UI)
        TryAttachHook(New MemHandler(Function(address As UInt32, ByRef value As UInt16, mode As MemHookMode) As Boolean
                                         If mode = MemHookMode.Write AndAlso address >= watchStart AndAlso address < watchEnd AndAlso
                                            watchHits < WATCH_LIMIT AndAlso histCount > WATCH_ARM_AFTER AndAlso
                                            Memory(address) <> (value And &HFF) Then
                                             watchHits += 1
                                             CrashCaptureNow($"write #{watchHits} to watched {address:X5}: {Memory(address):X2} -> {value And &HFF:X2}")
                                             histFrozen = False ' Keep recording
                                         End If
                                         Return False
                                     End Function))
        Notify("CRASH CAPTURE: watching writes to {0:X5}-{1:X5}", NotificationReasons.Warn, watchStart, watchEnd - 1)
    End Sub

    ' Saves the history now (e.g. from a memory watch hook that detected the corruption being written)
    Public Sub CrashCaptureNow(reason As String)
        If histFrozen Then Exit Sub
        captureReason = reason
        CrashCaptureDump()
    End Sub

    Private Sub CrashCaptureDump()
        histFrozen = True

        Try
            Dim stamp As String = Now.ToString("yyyyMMdd-HHmmss-fff")
            Dim folder As String = If(String.IsNullOrEmpty(BasePath), AppContext.BaseDirectory, BasePath)
            Dim historyFile As String = IO.Path.Combine(folder, $"crash-history-{stamp}.txt")
            Dim memoryFile As String = IO.Path.Combine(folder, $"crash-memory-{stamp}.bin")

            IO.File.WriteAllBytes(memoryFile, Memory)

            Dim n As Integer = CInt(Math.Min(histCount, HIST_SIZE))
            Dim first As Integer = (histIndex - n + HIST_SIZE) And (HIST_SIZE - 1)
            Using w As New IO.StreamWriter(historyFile)
                w.WriteLine($"x8086NetEmu crash capture {Now}: {histCount:N0} instructions recorded, last {n:N0} listed (oldest first)")
                w.WriteLine("Trigger: {0}. Memory dump: {1}", captureReason, IO.Path.GetFileName(memoryFile))
                Try
                    Dim f = Function(field As String) CByte(GetType(PIC8259).GetField(field, Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance).GetValue(PIC))
                    Dim b = Function(field As String) GetType(PIC8259).GetField(field, Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance).GetValue(PIC)
                    w.WriteLine("PIC: IRR={0:X2} ISR={1:X2} IMR={2:X2} autoEOI={3} specialMask={4} specialNest={5} levelTriggered={6} state={7} lowPrio={8}",
                                f("rIRR"), f("rISR"), f("rIMR"), b("autoEOI"), b("specialMask"), b("specialNest"), b("levelTriggered"), b("state"), b("lowPrio"))
                Catch
                End Try
                w.WriteLine("#        CS:IP     bytes              SS:SP     DS   ES   AX   BX   CX   DX   SI   DI   BP   FLAGS")
                For k As Integer = 0 To n - 1
                    Dim i As Integer = ((first + k) And (HIST_SIZE - 1)) * HIST_FIELDS
                    Dim cs As UInt16 = hist(i), ip As UInt16 = hist(i + 1)
                    Dim bytes As New Text.StringBuilder()
                    For b As Integer = 0 To 5
                        bytes.Append(Memory(SegmentOffsetToAbsolute(cs, CUShort((ip + b) And &HFFFF))).ToString("X2")).Append(" "c)
                    Next
                    w.WriteLine($"{k,-8} {cs:X4}:{ip:X4} {bytes} {hist(i + 2):X4}:{hist(i + 3):X4} {hist(i + 4):X4} {hist(i + 5):X4} {hist(i + 6):X4} {hist(i + 7):X4} {hist(i + 8):X4} {hist(i + 9):X4} {hist(i + 10):X4} {hist(i + 11):X4} {hist(i + 12):X4} {hist(i + 13):X4}")
                Next
            End Using

            Notify("CRASH CAPTURE: execution derailed at {0:X4}:{1:X4}. History saved to {2}", NotificationReasons.Err, mRegisters.CS, mRegisters.IP, historyFile)
        Catch ex As Exception
            Notify("CRASH CAPTURE: unable to save the history: {0}", NotificationReasons.Err, ex.Message)
        End Try
    End Sub
End Class

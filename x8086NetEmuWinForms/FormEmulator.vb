Imports x8086NetEmu

Public Class FormEmulator
#If Win32 Then
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Keys) As Short
    End Function

    Private Const WM_NCRBUTTONDOWN As Long = &HA4
    Private Const WM_NCRBUTTONUP As Long = &HA5

    Protected Overrides Sub WndProc(ByRef m As Message)
        Select Case m.Msg
            Case WM_NCRBUTTONUP
                ContextMenuStripMain.Show(Cursor.Position)
            Case WM_NCRBUTTONDOWN
            Case Else
                MyBase.WndProc(m)
        End Select
    End Sub
#End If

    Private cpu As X8086
    Private cpuState As EmulatorState

    Private fDebugger As FormDebugger
    Private fConsole As FormConsole

    Private videoPort As Control

    Private isLeftMouseButtonDown As Boolean
    Private isSelectingText As Boolean
    Private fromColRow As Point
    Private toColRow As Point

    Private v20Emulation As Boolean
    Private int13Emulation As Boolean

    Private mCursorVisible As Boolean = True

    Private runningApp As String

    Private Sub FormEmulator_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SaveSettings()
        StopEmulation()
    End Sub

    Private Sub FormEmulator_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Color.Black

        'RunChecks()

        ' New settings that are recommend to be turned on
        INT13EmulationToolStripMenuItem.Checked = True
        int13Emulation = True
        VIC20EmulationToolStripMenuItem.Checked = True
        v20Emulation = True

        LoadSettings(False)  ' For pre-emulation settings
        StartEmulation()

        SetupEventHandlers()
        SetTitleText()
    End Sub

    Private Sub SetupEventHandlers()
        AddHandler DebuggerToolStripMenuItem.Click, Sub() ShowDebugger()
        AddHandler ConsoleToolStripMenuItem.Click, Sub() ShowConsole()
        AddHandler SoftResetToolStripMenuItem.Click, Sub()
                                                         runningApp = ""
                                                         Me.Invoke(New MethodInvoker(AddressOf SetTitleText))
                                                         cpu.SoftReset()
                                                     End Sub
        AddHandler HardResetToolStripMenuItem.Click, Sub()
                                                         runningApp = ""
                                                         Me.Invoke(New MethodInvoker(AddressOf SetTitleText))
                                                         cpu.HardReset()
                                                     End Sub
        AddHandler MediaToolStripMenuItem.Click, Sub() RunMediaManager()

        AddHandler PasteTextToolStripMenuItem.Click, Sub() PasteTextFromClipboard()
        AddHandler CopyTextToolStripMenuItem.Click, Sub() CopyTextFromEmulator()

        'AddHandler cpu.EmulationHalted, Sub()
        '                                    MsgBox(String.Format("System Halted at {0:X4}:{1:X4}", cpu.Registers.CS, cpu.Registers.IP),
        '                                           MsgBoxStyle.Critical, "Emulation Stopped")
        '                                End Sub

        AddHandler INT13EmulationToolStripMenuItem.Click, Sub()
                                                              int13Emulation = Not int13Emulation
                                                              INT13EmulationToolStripMenuItem.Checked = int13Emulation
                                                              WarnAboutRestart()
                                                          End Sub
        AddHandler VIC20EmulationToolStripMenuItem.Click, Sub()
                                                              v20Emulation = Not v20Emulation
                                                              VIC20EmulationToolStripMenuItem.Checked = v20Emulation
                                                              WarnAboutRestart()
                                                          End Sub
    End Sub

    Private Sub RunMediaManager()
        cpu.Pause()
        Using dlg As New FormMediaManager()
            dlg.Emulator = cpu
            Dim ans As DialogResult = dlg.ShowDialog(Me)
            SaveSettings()
            If ans = DialogResult.Yes Then
                cpu.HardReset()
            Else
                cpu.Resume()
            End If
        End Using
    End Sub

    Private Sub SetupCpuEventHandlers()
#If Win32 Then
        AddHandler cpu.VideoAdapter.KeyDown, Sub(s1 As Object, e1 As KeyEventArgs)
                                                 If (e1.KeyData And Keys.Control) = Keys.Control AndAlso Convert.ToBoolean(GetAsyncKeyState(Keys.RControlKey)) Then
                                                     Cursor.Clip = Rectangle.Empty
                                                     CursorVisible = True

                                                     Select Case e1.KeyCode
                                                         Case Keys.Home
                                                             ContextMenuStripMain.Show(Cursor.Position)
                                                         Case Keys.Add
                                                             Dim zoom = cpu.VideoAdapter.Zoom
                                                             If zoom < 4 Then SetZoomLevel(zoom + 0.25)
                                                         Case Keys.Subtract
                                                             Dim zoom = cpu.VideoAdapter.Zoom
                                                             If zoom > 0.25 Then SetZoomLevel(zoom - 0.25)
                                                         Case Keys.NumPad0
                                                             SetZoomLevel(1)
                                                         Case Keys.C
                                                             CopyTextFromEmulator()
                                                         Case Keys.V
                                                             PasteTextFromClipboard()
                                                     End Select

                                                     e1.Handled = True
                                                 End If
                                             End Sub
#Else
        AddHandler videoPort.MouseDown, Sub(s1 As Object, e1 As MouseEventArgs)
                                            If e1.Button = Windows.Forms.MouseButtons.Middle Then ContextMenuStripMain.Show(Cursor.Position)
                                        End Sub
#End If
        AddHandler cpu.MIPsUpdated, Sub() Me.Invoke(New MethodInvoker(AddressOf SetTitleText))
    End Sub

    Private Sub WarnAboutRestart()
        cpu.Pause()
        Me.Hide()
        MsgBox("Changes to this option require restarting the emulator", MsgBoxStyle.Information)
        Me.Show()
        cpu.Resume()
    End Sub

    Private Sub CopyTextFromEmulator()
        If isSelectingText Then Exit Sub
        If TypeOf cpu.VideoAdapter Is CGAWinForms OrElse TypeOf cpu.VideoAdapter Is VGAWinForms Then
            CursorVisible = True
            Me.Cursor = Cursors.IBeam
            cpu.Pause()
            isSelectingText = True
        Else
            MsgBox("Text copying is only supported on CGAWinForms and VGAWinForms video adapters", MsgBoxStyle.Information)
        End If
    End Sub

    Private Property CursorVisible As Boolean
        Get
            Return mCursorVisible
        End Get
        Set(value As Boolean)
            If mCursorVisible = value Then
                Exit Property
            Else
                If value Then
                    Cursor.Show()
                Else
                    Cursor.Hide()
                End If
            End If
            mCursorVisible = value
        End Set
    End Property

    Private Sub PasteTextFromClipboard()
        If Not isSelectingText AndAlso Clipboard.ContainsText(TextDataFormat.Text) Then
            Dim cbc As String = Clipboard.GetText(TextDataFormat.Text)
            Dim tmp As New Threading.Thread(Sub()
                                                Threading.Thread.Sleep(500)
                                                Dim gd() As Char = {"(", ")", "{", "}", "+", "^"}
                                                For Each c As Char In cbc
                                                    If gd.Contains(c) Then
                                                        SendKeys.SendWait($"{gd(2)}{c}{gd(3)}")
                                                    Else
                                                        SendKeys.SendWait(c)
                                                    End If
                                                    Threading.Thread.Sleep(5)
                                                Next
                                            End Sub)
            tmp.Start()
        End If
    End Sub

    Private Sub SetTitleText()
        Dim sysMenIntegercut As String

#If Win32 Then
        sysMenIntegercut = "RCtrl + Home"
#Else
        sysMenIntegercut = "Ctrl + MButton"
#End If

        Me.Text = String.Format("x8086NetEmu [Menu: {0}]  {1:F2}MHz ● {2}% | {3} | {4:N2} MIPs | {5} {6}",
                                    sysMenIntegercut,
                                    cpu.Clock / X8086.MHz,
                                    cpu.SimulationMultiplier * 100,
                                    $"{cpu.VideoAdapter.Name.Split(" "c)(0)} Mode {cpu.VideoAdapter.VideoMode:X2}{If(cpu.VideoAdapter.MainMode = VideoAdapter.MainModes.Text, "🅣", "🅖")} | Zoom {cpu.VideoAdapter.Zoom * 100}%",
                                    cpu.MIPs,
                                    If(cpu.IsHalted, "Halted", If(cpu.DebugMode, "Debugging", If(cpu.IsPaused, "Paused", "Running"))),
                                    If(runningApp <> "", $" | {runningApp}", ""))
    End Sub

    Private Sub StopEmulation()
        If cpu IsNot Nothing Then
            If fDebugger IsNot Nothing Then fDebugger.Close()
            If fConsole IsNot Nothing Then fConsole.Close()
            If cpuState IsNot Nothing Then cpuState = Nothing

            cpu.Close()
            cpu = Nothing
        End If
    End Sub

    Private Sub StartEmulation()
        cpu = New X8086(v20Emulation, True, AddressOf StartEmulation)

        cpuState = New EmulatorState(cpu)

        If videoPort IsNot Nothing Then
            Me.Controls.Remove(videoPort)
            videoPort.Dispose()
        End If

        videoPort = New RenderCtrlGDI() With {.Dock = DockStyle.Fill}
        Me.Controls.Add(videoPort)
        videoPort.Focus()

        cpu.Adapters.Add(New FloppyControllerAdapter(cpu))
        cpu.Adapters.Add(New CGAWinForms(cpu, videoPort, If(ConsoleCrayon.RuntimeIsMono, VideoAdapter.FontSources.TrueType, VideoAdapter.FontSources.BitmapFile), "asciivga.dat"))
        'cpu.Adapters.Add(New VGAWinForms(cpu, videoPort, If(ConsoleCrayon.RuntimeIsMono, VideoAdapter.FontSources.TrueType, VideoAdapter.FontSources.BitmapFile), "asciivga.dat"))
        cpu.Adapters.Add(New KeyboardAdapter(cpu))
        'cpu.Adapters.Add(New MouseAdapter(cpu)) ' This breaks many things (For example, MINIX won't start)

#If Win32 Then
        cpu.Adapters.Add(New SpeakerAdpater(cpu))
        'cpu.Adapters.Add(New AdlibAdapter(cpu))
#End If

        cpu.VideoAdapter.AutoSize()

        X8086.LogToConsole = False

        'cpu.LoadBIN("80186_tests\rep.bin", &HF000, &H0)
        'cpu.Run(True, &HF000, 0)
        'ShowDebugger()
        cpu.Run()

        SetupVideoPortEventHandlers()
        SetupCpuEventHandlers()
        AddCustomHooks()
        LoadSettings(True)
    End Sub

    ' Code demonstration on how to attach hooks to the CPU
    ' http://stanislavs.org/helppc/int_21.html
    Private Sub AddCustomHooks()
        cpu.TryAttachHook(&H19, Function() As Boolean ' Reset running program on bootstrap
                                    runningApp = ""
                                    Return False
                                End Function)

        cpu.TryAttachHook(&H20, Function() As Boolean ' Older programs still rely on INT20 to terminate (http://stanislavs.org/helppc/int_20.html)
                                    runningApp = ""
                                    Return False
                                End Function)

        cpu.TryAttachHook(&H21, Function() As Boolean
                                    Select Case cpu.Registers.AH
                                        Case &H0, &H4C, &H31
                                            runningApp = ""
                                        Case &H4B ' http://stanislavs.org/helppc/int_21-4b.html
                                            Dim mode As String = ""

                                            Dim GetFileName = Function() As String
                                                                  Dim b As New List(Of Byte)
                                                                  Dim addr As UInt32 = X8086.SegmentOffetToAbsolute(cpu.Registers.DS, cpu.Registers.DX)
                                                                  While cpu.RAM(addr) <> 0
                                                                      b.Add(cpu.RAM(addr))
                                                                      addr += 1
                                                                  End While
                                                                  Return System.Text.Encoding.ASCII.GetString(b.ToArray())
                                                              End Function

                                            runningApp = GetFileName() ' ParseRunningApp()

                                            Select Case cpu.Registers.AL
                                                Case 0 : mode = "L&X" ' Load & Execute
                                                Case 1 : mode = "UND" ' Undocumented
                                                Case 2 : mode = "UNK" ' Unknown
                                                Case 3 : mode = "LOD" ' Load
                                                Case 4 : mode = "MSC" ' Whatever this means: Called by MSC spawn() when P_NOWAIT is specified
                                            End Select

                                            Const offset As UInt32 = &H12
                                            Dim cs As UInt32 = cpu.RAM16(cpu.Registers.ES, cpu.Registers.BX, offset)
                                            Dim ip As UInt32 = cpu.RAM16(cpu.Registers.ES, cpu.Registers.BX, offset + If(cpu.Registers.AL = 1, 4, 2))
                                            X8086.Notify($"DOS {mode}: {runningApp} -> {cpu.Registers.ES:X4}:{cpu.Registers.BX:X4}", X8086.NotificationReasons.Dbg)
                                            X8086.Notify($"DOS {mode}: {runningApp} -> {cs:X4}:{ip:X4}", X8086.NotificationReasons.Dbg)
                                    End Select

                                    ' Return False to notify the emulator that the interrupt was not handled.
                                    '   Code execution will be transfered to the "native" interrupt handler.
                                    ' Return True if you want to prevent the emulator from executing the code associated with this interrupt.
                                    '   See INT13.vb for more information
                                    Return False
                                End Function)
    End Sub

    Private Sub SetupVideoPortEventHandlers()
        If Not (TypeOf cpu.VideoAdapter Is CGAWinForms OrElse TypeOf cpu.VideoAdapter Is VGAWinForms) Then Exit Sub

        AddHandler videoPort.MouseUp, Sub(s As Object, e As MouseEventArgs)
                                          If e.Button = MouseButtons.Left AndAlso isLeftMouseButtonDown Then
                                              Me.Cursor = Cursors.Default

                                              Dim c As Integer
                                              Dim text As String = ""
                                              Dim fromCol As Integer = Math.Min(fromColRow.X, toColRow.X)
                                              Dim toCol As Integer = Math.Max(fromColRow.X, toColRow.X)
                                              Dim fromRow As Integer = Math.Min(fromColRow.Y, toColRow.Y)
                                              Dim toRow As Integer = Math.Max(fromColRow.Y, toColRow.Y)
                                              For row As Integer = fromRow To toRow
                                                  For col As Integer = If(row = fromRow, fromCol, 0) To If(row = toRow, toCol, cpu.VideoAdapter.TextResolution.Width) - 1
                                                      c = cpu.Memory(cpu.VideoAdapter.ColRowToAddress(col, row))
                                                      text += If(c >= 32, Convert.ToChar(c), " ")
                                                  Next
                                                  text += Environment.NewLine
                                              Next
                                              Clipboard.SetText(text, TextDataFormat.Text)

                                              cpu.Resume()
                                              isLeftMouseButtonDown = False
                                              isSelectingText = False
                                          End If
                                      End Sub

        AddHandler videoPort.MouseDown, Sub(s As Object, e As MouseEventArgs)
                                            If e.Button = MouseButtons.Left AndAlso isSelectingText Then
                                                If cpu.VideoAdapter.MainMode <> CGAAdapter.MainModes.Text Then
                                                    MsgBox("Text copying is only supported in Text video modes", MsgBoxStyle.Information)
                                                    isSelectingText = False
                                                    Exit Sub
                                                End If

                                                fromColRow = New Point(e.X / videoPort.Width * cpu.VideoAdapter.TextResolution.Width,
                                                                       e.Y / videoPort.Height * cpu.VideoAdapter.TextResolution.Height)
                                                toColRow = fromColRow
                                                isLeftMouseButtonDown = True
                                            End If
                                        End Sub

        AddHandler videoPort.MouseMove, Sub(s As Object, e As MouseEventArgs)
                                            If isLeftMouseButtonDown Then
                                                toColRow = New Point(e.X / videoPort.Width * cpu.VideoAdapter.TextResolution.Width,
                                                                     e.Y / videoPort.Height * cpu.VideoAdapter.TextResolution.Height)
                                            End If
                                        End Sub

        AddHandler cpu.VideoAdapter.PostRender, Sub(sender As Object, e As PaintEventArgs)
                                                    If isLeftMouseButtonDown Then
                                                        Dim fromCol As Integer = Math.Min(fromColRow.X, toColRow.X)
                                                        Dim toCol As Integer = Math.Max(fromColRow.X, toColRow.X)
                                                        Dim fromRow As Integer = Math.Min(fromColRow.Y, toColRow.Y)
                                                        Dim toRow As Integer = Math.Max(fromColRow.Y, toColRow.Y)
                                                        Using sb As New SolidBrush(Color.FromArgb(128, Color.DarkSlateBlue))
                                                            For row As Integer = fromRow To toRow
                                                                For col As Integer = If(row = fromRow, fromCol, 0) To If(row = toRow, toCol, cpu.VideoAdapter.TextResolution.Width) - 1
                                                                    e.Graphics.FillRectangle(sb, cpu.VideoAdapter.ColRowToRectangle(col, row))
                                                                Next
                                                            Next
                                                        End Using
                                                    End If
                                                End Sub

        AddHandler videoPort.MouseEnter, Sub() ContextMenuStripMain.Hide()

        AddHandler videoPort.Click, Sub(s1 As Object, e1 As EventArgs)
                                        If isSelectingText Then Exit Sub
                                        Cursor.Clip = Me.RectangleToScreen(videoPort.Bounds)
                                        CursorVisible = False
                                    End Sub
    End Sub

    Private Sub ShowConsole()
        If fConsole Is Nothing Then
            fConsole = New FormConsole()
            fConsole.Show()
            fConsole.Emulator = cpu
            fConsole.BringToFront()

            AddHandler fConsole.FormClosed, Sub()
                                                fConsole.Dispose()
                                                fConsole = Nothing
                                            End Sub
        End If
    End Sub

    Private Sub ShowDebugger()
        If fDebugger Is Nothing Then
            fDebugger = New FormDebugger()
            fDebugger.Show()
            fDebugger.Emulator = cpu
            fDebugger.BringToFront()

            AddHandler fDebugger.FormClosed, Sub()
                                                 fDebugger.Dispose()
                                                 fDebugger = Nothing
                                             End Sub
        End If
    End Sub

    Private Sub SetZoomFromMenu(sender As Object, e As EventArgs) Handles Zoom25ToolStripMenuItem.Click, Zoom50ToolStripMenuItem.Click, Zoom100ToolStripMenuItem.Click,
                                                                        Zoom150ToolStripMenuItem.Click, Zoom200ToolStripMenuItem.Click, Zoom400ToolStripMenuItem.Click

        Dim zoomText As String = CType(sender, ToolStripMenuItem).Text
        Dim zoomPercentage As Integer = Integer.Parse(zoomText.Replace("%", ""))
        SetZoomLevel(zoomPercentage / 100)
    End Sub

    Private Sub SetSimulationMultiplierFromMenu(sender As Object, e As EventArgs) Handles ToolStripMenuItemSpeed25.Click, ToolStripMenuItemSpeed50.Click,
                                                                                        ToolStripMenuItemSpeed100.Click, ToolStripMenuItemSpeed150.Click,
                                                                                        ToolStripMenuItemSpeed200.Click, ToolStripMenuItemSpeed400.Click,
                                                                                        ToolStripMenuItemSpeed800.Click, ToolStripMenuItemSpeed1000.Click
        Dim speedText As String = CType(sender, ToolStripMenuItem).Text
        Dim speedPercentage As Integer = Integer.Parse(speedText.Replace("%", ""))

        For Each ddi As ToolStripItem In EmulationSpeedToolStripMenuItem.DropDownItems
            If TypeOf ddi Is ToolStripMenuItem Then
                Dim mi = CType(ddi, ToolStripMenuItem)
                If mi.Text.StartsWith("Custom") Then

                Else
                    mi.Checked = (ddi.Text = speedText)
                End If
            End If
        Next

        cpu.SimulationMultiplier = speedPercentage / 100
    End Sub

    Private Sub SetClockFromMenu(sender As Object, e As EventArgs) Handles MHz0477ToolStripMenuItem.Click, MHz0954ToolStripMenuItem.Click,
                                                                    MHz1908ToolStripMenuItem.Click, MHz3816ToolStripMenuItem.Click,
                                                                    MHz4770ToolStripMenuItem4.Click

        Dim speedText As String = CType(sender, ToolStripMenuItem).Text
        Dim speedValue As Double = Double.Parse(speedText.Replace(" MHz", ""))
        SetCPUClockSpeed(speedValue * X8086.MHz)
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub

    Private Sub LoadSettings(showPrompt As Boolean)
        ' Enforce defaults

        If IO.File.Exists("settings.dat") Then
            Dim xml = XDocument.Load("settings.dat")
            ParseSettings(xml.<settings>(0))
        Else
            If showPrompt Then
                If MsgBox($"It looks like this is the first time you run the emulator.{Environment.NewLine}" +
                          $"Use the 'RightCtrl + Home' hotkey to access the emulator settings.{Environment.NewLine}{Environment.NewLine}" +
                          $"Would you like to configure the emulator's floppies and hard drives now?", MsgBoxStyle.Information Or MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
                    RunMediaManager()
                End If
            End If
            'ShowConsole()
        End If
    End Sub

    Private Sub ParseSettings(xml As XElement)
        If cpu IsNot Nothing Then
            cpu.SimulationMultiplier = Double.Parse(xml.<simulationMultiplier>.Value)
            Dim simulationMultiplierText As String = (cpu.SimulationMultiplier * 100).ToString() + "%"
            For Each ddi As ToolStripItem In EmulationSpeedToolStripMenuItem.DropDownItems
                If TypeOf ddi Is ToolStripMenuItem Then
                    Dim mi = CType(ddi, ToolStripMenuItem)
                    If mi.Text.StartsWith("Custom") Then

                    Else
                        mi.Checked = (ddi.Text = simulationMultiplierText)
                    End If
                End If
            Next

            SetCPUClockSpeed(Double.Parse(xml.<clockSpeed>.Value))
            SetZoomLevel(Double.Parse(xml.<videoZoom>.Value))

            If cpu.FloppyContoller IsNot Nothing Then
                For i As Integer = 0 To 512 - 1
                    If cpu.FloppyContoller.DiskImage(i) IsNot Nothing Then cpu.FloppyContoller.DiskImage(i).Close()
                Next

                For Each f In xml.<floppies>.<floppy>
                    Dim index As Integer = Asc(f.<letter>.Value) - 65
                    Dim image As String = f.<image>.Value
                    Dim ro As Boolean = Boolean.Parse(f.<readOnly>.Value)

                    cpu.FloppyContoller.DiskImage(index) = New DiskImage(image, ro)
                Next

                For Each d In xml.<disks>.<disk>
                    Dim index As Integer = Asc(d.<letter>.Value) - 67 + 128
                    Dim image As String = d.<image>.Value
                    Dim ro As Boolean = Boolean.Parse(d.<readOnly>.Value)

                    cpu.FloppyContoller.DiskImage(index) = New DiskImage(image, ro, True)
                Next
            End If

            Try
                If Boolean.Parse(xml.<extras>.<consoleVisible>.Value) Then ShowConsole()
                If Boolean.Parse(xml.<extras>.<debuggerVisible>.Value) Then ShowDebugger()
            Catch
            End Try

            Me.Left = (My.Computer.Screen.WorkingArea.Width - Me.Width) / 2
            Me.Top = (My.Computer.Screen.WorkingArea.Height - Me.Height) / 2
        End If

        Dim b As Boolean
        If Boolean.TryParse(xml.<extras>.<emulateINT13>.Value, b) Then INT13EmulationToolStripMenuItem.Checked = b : int13Emulation = b
        If Boolean.TryParse(xml.<extras>.<vic20>.Value, b) Then VIC20EmulationToolStripMenuItem.Checked = b : v20Emulation = b
    End Sub

    Private Sub SetCPUClockSpeed(value As Double)
        cpu.Clock = value
        Dim clockSpeedText As String = (cpu.Clock / X8086.MHz).ToString("0.00") + " MHz"
        For Each ddi As ToolStripItem In CPUClockToolStripMenuItem.DropDownItems
            If TypeOf ddi Is ToolStripMenuItem Then
                Dim mi = CType(ddi, ToolStripMenuItem)
                If mi.Text.StartsWith("Custom") Then

                Else
                    mi.Checked = (ddi.Text = clockSpeedText)
                End If
            End If
        Next

        SetTitleText()
    End Sub

    Private Sub SetZoomLevel(value As Double)
        cpu.VideoAdapter.Zoom = value
        Dim zoomText As String = (value * 100).ToString() + "%"
        For Each ddi As ToolStripItem In ZoomToolStripMenuItem.DropDownItems
            If TypeOf ddi Is ToolStripMenuItem Then
                Dim mi = CType(ddi, ToolStripMenuItem)
                If mi.Text.StartsWith("Custom") Then

                Else
                    mi.Checked = (ddi.Text = zoomText)
                End If
            End If
        Next

        SetTitleText()
    End Sub

    Private Sub SaveSettings()
        cpuState.SaveSettings("settings.dat",
                              <extras>
                                  <consoleVisible><%= fConsole IsNot Nothing %></consoleVisible>
                                  <debuggerVisible><%= fDebugger IsNot Nothing %></debuggerVisible>
                                  <emulateINT13><%= int13Emulation %></emulateINT13>
                                  <vic20><%= v20Emulation %></vic20>
                              </extras>)
    End Sub

    Private Sub SaveStateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveStateToolStripMenuItem.Click
        cpu.Pause()

        Using dlg As New SaveFileDialog()
            dlg.Title = "Save Emulator State"
            dlg.Filter = "x8086NetEmu State|*.state"
            dlg.AddExtension = True

            If dlg.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
                cpuState.SaveState(dlg.FileName)
            End If
        End Using

        cpu.Resume()
    End Sub

    Private Sub LoadStateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadStateToolStripMenuItem.Click
        Using dlg As New OpenFileDialog()
            dlg.Title = "Load Emulator State"
            dlg.Filter = "x8086NetEmu State|*.state"

            If dlg.ShowDialog(Me) = Windows.Forms.DialogResult.OK Then
                cpu.HardReset()

                Dim xml = XDocument.Load(dlg.FileName)
                ParseSettings(xml.<state>.<settings>(0))
                PowerState(xml.<state>(0))

                cpu.Run(cpu.DebugMode)
            End If
        End Using
    End Sub

    Private Sub PowerState(xml As XElement)
        cpu.Flags.EFlags = xml.<flags>.Value

        cpu.Registers.AX = xml.<registers>.<AX>.Value
        cpu.Registers.BX = xml.<registers>.<BX>.Value
        cpu.Registers.CX = xml.<registers>.<CX>.Value
        cpu.Registers.DX = xml.<registers>.<DX>.Value
        cpu.Registers.CS = xml.<registers>.<CS>.Value
        cpu.Registers.IP = xml.<registers>.<IP>.Value
        cpu.Registers.SS = xml.<registers>.<SS>.Value
        cpu.Registers.SP = xml.<registers>.<SP>.Value
        cpu.Registers.DS = xml.<registers>.<DS>.Value
        cpu.Registers.SI = xml.<registers>.<SI>.Value
        cpu.Registers.ES = xml.<registers>.<ES>.Value
        cpu.Registers.DI = xml.<registers>.<DI>.Value
        cpu.Registers.BP = xml.<registers>.<BP>.Value
        cpu.Registers.ActiveSegmentRegister = [Enum].Parse(GetType(X8086.GPRegisters.RegistersTypes), xml.<registers>.<AS>.Value)

        cpu.Memory = Convert.FromBase64String(xml.<memory>.Value)
        cpu.DebugMode = Boolean.Parse(xml.<debugMode>.Value)

        cpu.VideoAdapter.VideoMode = [Enum].Parse(GetType(CGAAdapter.VideoModes), xml.<videoMode>.Value)
    End Sub

    Private Sub RunChecks()
        cpu = New X8086(True, False)

        Dim skipCount As Integer = 16
        Dim failed As Boolean

        For Each file In (New IO.DirectoryInfo("D:\Users\Xavier\Downloads\80186_tests\").GetFiles("*.bin")).ToList()
            If file.Name = "datatrnf.bin" OrElse file.Name.StartsWith("res_") Then
                Continue For
            End If

            If skipCount > 0 Then
                skipCount -= 1
                Continue For
            End If

            cpu.FPU = Nothing
            cpu.PIC = Nothing
            cpu.DMA = Nothing
            cpu.PIT = Nothing
            cpu.PPI = Nothing
            cpu.Ports.Clear()
            cpu.Adapters.Clear()

            MsgBox($"Test Program: {file.Name}")

            cpu.Run(True)
            cpu.LoadBIN(file.FullName, &HF000, &H0)
            cpu.Registers.CS = &HF000
            cpu.Registers.IP = &H0
            cpu.Flags.EFlags = 0
            ShowDebugger()
            Do
                Application.DoEvents()
            Loop Until cpu.IsHalted

            Dim expectedFileName As String = IO.Path.Combine(file.DirectoryName, "res_" + file.Name)
            If IO.File.Exists(expectedFileName) Then
                failed = False
                Dim expectedData() As Byte = IO.File.ReadAllBytes(expectedFileName)
                For i As Integer = 0 To expectedData.Length - 1
                    'If expectedData(i) <> 0 Then
                    If expectedData(i) <> cpu.RAM8(0, i) Then
                        failed = True
                        MsgBox($"{file.Name} failed at offset {i}{Environment.NewLine}Expected: {expectedData(i):X2}{Environment.NewLine}Found: {cpu.RAM8(0, i):X2}")
                    End If
                    'End If
                Next

                If failed Then
                    MsgBox($"{file.Name} failed to run correctly")
                Else
                    MsgBox($"{file.Name} has executed successfully")
                End If
            End If
        Next
        Exit Sub
    End Sub
End Class


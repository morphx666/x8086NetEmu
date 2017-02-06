Imports x8086NetEmu

Public Class FormEmulator
#If Win32 Then
    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Keys) As Short
    End Function
#End If

    Private cpu As x8086
    Private cpuState As EmulatorState

    Private fMonitor As FormMonitor
    Private fConsole As FormConsole

    Private videoPort As Control

    Private isLeftMouseButtonDown As Boolean
    Private isSelectingText As Boolean
    Private fromColRow As Point
    Private toColRow As Point

    Private v20Emulation As Boolean
    Private int13Emulation As Boolean

    Private Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SaveSettings()
        StopEmulation()
    End Sub

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Color.Black

        ' New settings that are recommend to be turned on
        INT13EmulationToolStripMenuItem.Checked = True
        int13Emulation = True
        VIC20EmulationToolStripMenuItem.Checked = True
        v20Emulation = True

        LoadSettings() ' For pre-emulation settings
        StartEmulation()
        LoadSettings() ' For post-emulation settings

        SetupEventHandlers()

        SetTitleText()
    End Sub

    Private Sub SetupEventHandlers()
        AddHandler MonitorToolStripMenuItem.Click, Sub() ShowMonitor()
        AddHandler ConsoleToolStripMenuItem.Click, Sub() ShowConsole()
        AddHandler SoftResetToolStripMenuItem.Click, Sub() cpu.SoftReset()
        AddHandler HardResetToolStripMenuItem.Click, Sub() cpu.HardReset()
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
            If dlg.ShowDialog(Me) = DialogResult.Yes Then
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
        AddHandler videoPort.MouseEnter, Sub() ContextMenuStripMain.Hide()
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
        If TypeOf cpu.VideoAdapter Is CGAWinForms Then
            Dim cgawf As CGAWinForms = CType(cpu.VideoAdapter, CGAWinForms)
            cgawf.HideHostCursor = False
            cgawf.RenderControl.Cursor = Cursors.IBeam
            cpu.Pause()
            isSelectingText = True
        Else
            MsgBox("Text copying is only supported on CGAWinForms video adapters", MsgBoxStyle.Information)
        End If
    End Sub

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

        Me.Text = String.Format("x8086NetEmu [Menu: {0}]      {1:F2}MHz ● {2}% | Zoom: {3}% | {4:N2} MIPs | {5}",
                                    sysMenIntegercut,
                                    cpu.Clock / x8086.MHz,
                                    cpu.SimulationMultiplier * 100,
                                    cpu.VideoAdapter.Zoom * 100,
                                    cpu.MIPs,
                                    If(cpu.IsHalted, "Halted", If(cpu.DebugMode, "Debugging", If(cpu.IsPaused, "Paused", "Running"))))
    End Sub

    Private Sub StopEmulation()
        If cpu IsNot Nothing Then
            If fMonitor IsNot Nothing Then fMonitor.Close()
            If fConsole IsNot Nothing Then fConsole.Close()

            cpu.Close()
            cpu = Nothing
        End If
    End Sub

    Private Sub StartEmulation()
        StopEmulation()

        cpu = New x8086(v20Emulation, int13Emulation)
        cpuState = New EmulatorState(cpu)

        videoPort = New RenderCtrlGDI()
        Me.Controls.Add(videoPort)

        cpu.Adapters.Add(New FloppyControllerAdapter(cpu))
        cpu.Adapters.Add(New CGAWinForms(cpu, videoPort, Not ConsoleCrayon.RuntimeIsMono))
        'cpu.Adapters.Add(New VGAWinForms(cpu, videoPort, Not ConsoleCrayon.RuntimeIsMono)) ' Not properly supported yet...
        cpu.Adapters.Add(New KeyboardAdapter(cpu))
        'cpu.Adapters.Add(New MouseAdapter(cpu)) ' This breaks many things

        AddSupportForTextCopy()

        ' FIXME: Use BASS to provide cross-platform audio support
#If Win32 Then
        cpu.Adapters.Add(New SpeakerAdpater(cpu))
#End If

        cpu.VideoAdapter.AutoSize()

        x8086.LogToConsole = False

        cpu.Run(False)

        SetupCpuEventHandlers()
    End Sub

    Private Sub AddSupportForTextCopy()
        If Not TypeOf cpu.VideoAdapter Is CGAWinForms Then Exit Sub

        AddHandler videoPort.MouseUp, Sub(s As Object, e As MouseEventArgs)
                                          If e.Button = MouseButtons.Left AndAlso isLeftMouseButtonDown Then
                                              Dim cgawf As CGAWinForms = CType(cpu.VideoAdapter, CGAWinForms)
                                              cgawf.HideHostCursor = True

                                              ' Why is this necessary?
                                              ' Why the Cursor.Hide() at Helpers.vb@428 stops working?
                                              cgawf.RenderControl.Cursor = New Cursor(New IO.MemoryStream(My.Resources.emptyCursor))

                                              Dim c As Integer
                                              Dim text As String = ""
                                              Dim fromCol As Integer = Math.Min(fromColRow.X, toColRow.X)
                                              Dim toCol As Integer = Math.Max(fromColRow.X, toColRow.X)
                                              Dim fromRow As Integer = Math.Min(fromColRow.Y, toColRow.Y)
                                              Dim toRow As Integer = Math.Max(fromColRow.Y, toColRow.Y)
                                              For row As Integer = fromRow To toRow
                                                  For col As Integer = If(row = fromRow, fromCol, 0) To If(row = toRow, toCol, cgawf.TextResolution.Width) - 1
                                                      c = cpu.Memory(cgawf.ColRowToAddress(col, row))
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
                                                Dim cgawf As CGAWinForms = CType(cpu.VideoAdapter, CGAWinForms)
                                                If cgawf.MainMode <> CGAAdapter.MainModes.Text Then
                                                    MsgBox("Text copying is only supported in Text video modes", MsgBoxStyle.Information)
                                                    Exit Sub
                                                End If

                                                fromColRow = New Point(e.X / videoPort.Width * cgawf.TextResolution.Width,
                                                                       e.Y / videoPort.Height * cgawf.TextResolution.Height)
                                                toColRow = fromColRow
                                                isLeftMouseButtonDown = True
                                            End If
                                        End Sub

        AddHandler videoPort.MouseMove, Sub(s As Object, e As MouseEventArgs)
                                            If isLeftMouseButtonDown Then
                                                Dim cgawf As CGAWinForms = CType(cpu.VideoAdapter, CGAWinForms)
                                                toColRow = New Point(e.X / videoPort.Width * cgawf.TextResolution.Width,
                                                                     e.Y / videoPort.Height * cgawf.TextResolution.Height)
                                            End If
                                        End Sub

        AddHandler CType(cpu.VideoAdapter, CGAWinForms).PostRender, Sub(sender As Object, e As PaintEventArgs)
                                                                        If isLeftMouseButtonDown Then
                                                                            Dim cgawf As CGAWinForms = CType(cpu.VideoAdapter, CGAWinForms)
                                                                            Dim fromCol As Integer = Math.Min(fromColRow.X, toColRow.X)
                                                                            Dim toCol As Integer = Math.Max(fromColRow.X, toColRow.X)
                                                                            Dim fromRow As Integer = Math.Min(fromColRow.Y, toColRow.Y)
                                                                            Dim toRow As Integer = Math.Max(fromColRow.Y, toColRow.Y)
                                                                            Using sb As New SolidBrush(Color.FromArgb(128, Color.DarkSlateBlue))
                                                                                For row As Integer = fromRow To toRow
                                                                                    For col As Integer = If(row = fromRow, fromCol, 0) To If(row = toRow, toCol, cgawf.TextResolution.Width) - 1
                                                                                        e.Graphics.FillRectangle(sb, cgawf.ColRowToRectangle(col, row))
                                                                                    Next
                                                                                Next
                                                                            End Using
                                                                        End If
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

    Private Sub ShowMonitor()
        If fMonitor Is Nothing Then
            fMonitor = New FormMonitor()
            fMonitor.Show()
            fMonitor.Emulator = cpu
            fMonitor.BringToFront()

            AddHandler fMonitor.FormClosed, Sub()
                                                fMonitor.Dispose()
                                                fMonitor = Nothing
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
        SetCPUClockSpeed(speedValue * x8086.MHz)
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub

    Private Sub LoadSettings()
        ' Enforce defaults

        If IO.File.Exists("settings.dat") Then
            Dim xml = XDocument.Load("settings.dat")
            ParseSettings(xml.<settings>(0))
        Else
            If MsgBox($"It looks like this is the first time you run the emulator.{Environment.NewLine}" +
                      $"Use the 'RightCtrl + Home' hotkey to access the emulator settings.{Environment.NewLine}{Environment.NewLine}" +
                      $"Would you like to configure the emulator's floppies and hard drives now?", MsgBoxStyle.Information Or MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
                RunMediaManager()
            End If
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
                If Boolean.Parse(xml.<extras>.<monitorVisible>.Value) Then ShowMonitor()
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
        Dim clockSpeedText As String = (cpu.Clock / x8086.MHz).ToString("0.00") + " MHz"
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
                                  <monitorVisible><%= fMonitor IsNot Nothing %></monitorVisible>
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
                cpu.Init()

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
        cpu.Registers.ActiveSegmentRegister = [Enum].Parse(GetType(x8086.GPRegisters.RegistersTypes), xml.<registers>.<AS>.Value)

        cpu.Memory = Convert.FromBase64String(xml.<memory>.Value)
        cpu.DebugMode = Boolean.Parse(xml.<debugMode>.Value)

        cpu.VideoAdapter.VideoMode = [Enum].Parse(GetType(CGAAdapter.VideoModes), xml.<videoMode>.Value)
    End Sub
End Class

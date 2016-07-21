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

    Private Sub frmMain_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        SaveSettings()

        If fMonitor IsNot Nothing Then fMonitor.Close()
        If fConsole IsNot Nothing Then fConsole.Close()

        cpu.Close()
    End Sub

    Private Sub frmMain_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        Me.BackColor = Color.Black

        StartEmulation()

        SetupEventHandlers()
        SetTitleText()
    End Sub

    Private Sub SetupEventHandlers()
        AddHandler MonitorToolStripMenuItem.Click, Sub() ShowMonitor()
        AddHandler ConsoleToolStripMenuItem.Click, Sub() ShowConsole()
        AddHandler SoftResetToolStripMenuItem.Click, Sub() cpu.SoftReset()
        AddHandler HardResetToolStripMenuItem.Click, Sub() cpu.HardReset()
        AddHandler FloppyManagerToolStripMenuItem.Click, Sub()
                                                             Using dlg As New FormDiskManager()
                                                                 dlg.Emulator = cpu
                                                                 dlg.HardDiskMode = False

                                                                 dlg.ShowDialog(Me)
                                                             End Using
                                                         End Sub
        AddHandler HardDiskManagerToolStripMenuItem.Click, Sub()
                                                               Using dlg As New FormDiskManager()
                                                                   dlg.Emulator = cpu
                                                                   dlg.HardDiskMode = True

                                                                   dlg.ShowDialog(Me)
                                                               End Using
                                                           End Sub

        AddHandler cpu.EmulationHalted, Sub()
                                            MsgBox(String.Format("System Halted at {0:X4}:{1:X4}", cpu.Registers.CS, cpu.Registers.IP),
                                                   MsgBoxStyle.Critical, "Emulation Stopped")
                                        End Sub

#If Win32 Then
        AddHandler cpu.VideoAdapter.KeyDown, Sub(s1 As Object, e1 As KeyEventArgs)
                                                 If (e1.KeyData And Keys.Control) = Keys.Control AndAlso Convert.ToBoolean(GetAsyncKeyState(Keys.RControlKey)) Then
                                                     If e1.KeyCode = Keys.Home Then ContextMenuStripMain.Show(Cursor.Position)

                                                     If e1.KeyCode = Keys.Add Then
                                                         Dim zoom = cpu.VideoAdapter.Zoom
                                                         If zoom < 4 Then SetZoomLevel(zoom + 0.25)
                                                     End If

                                                     If e1.KeyCode = Keys.Subtract Then
                                                         Dim zoom = cpu.VideoAdapter.Zoom
                                                         If zoom > 0.25 Then SetZoomLevel(zoom - 0.25)
                                                     End If

                                                     If e1.KeyCode = Keys.NumPad0 Then SetZoomLevel(1)

                                                     'Debug.WriteLine(e1.KeyData)

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

    Private Sub SetTitleText()
        Dim sysMenuShortcut As String

#If Win32 Then
        sysMenuShortcut = "RCtrl + Home"
#Else
        sysMenuShortcut = "Ctrl + MButton"
#End If

        Me.Text = String.Format("x8086NetEmu [System Menu: {0}]       {1:F2}MHz ● {2}% | Zoom: {3}% | {4:N2} MIPs | {5}",
                                    sysMenuShortcut,
                                    cpu.Clock / x8086.MHz,
                                    cpu.SimulationMultiplier * 100,
                                    cpu.VideoAdapter.Zoom * 100,
                                    cpu.MIPs,
                                    If(cpu.IsHalted, "Halted", If(cpu.DebugMode, "Debugging", "Running")))
    End Sub

    Private Sub StartEmulation()
        cpu = New x8086(True)
        cpuState = New EmulatorState(cpu)

        videoPort = New RenderCtrlGDI()
        'videoPort = New RenderCtrlSDL() ' PRE-ALPHA (i.e. DO NOT USE)
        Me.Controls.Add(videoPort)

        cpu.Adapters.Add(New FloppyControllerAdapter(cpu))
        'cpu.Adapters.Add(New FastCGAWinForms.FastCGAWinForms(cpu, videoPort))
        cpu.Adapters.Add(New CGAWinForms(cpu, videoPort, Not ConsoleCrayon.RuntimeIsMono))
        cpu.Adapters.Add(New KeyboardAdapter(cpu))
        'cpu.Adapters.Add(New MouseAdapter(cpu)) ' Not Compatible with MINIX

#If Win32 Then
        cpu.Adapters.Add(New SpeakerAdpater(cpu))
#End If

        ' http://www.allbootdisks.com/
        LoadSettings()

        cpu.VideoAdapter.AutoSize()

        x8086.LogToConsole = False
        cpu.EmulateINT13 = True

        cpu.Run(False)
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
        If IO.File.Exists("settings.dat") Then
            Dim xml = XDocument.Load("settings.dat")

            ParseSettings(xml.<settings>(0))
        End If
    End Sub

    Private Sub ParseSettings(xml As XElement)
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

        Try
            If Boolean.Parse(xml.<extras>.<consoleVisible>.Value) Then ShowConsole()
            If Boolean.Parse(xml.<extras>.<monitorVisible>.Value) Then ShowMonitor()
        Catch
        End Try
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
                                  <consoleVisible><%= (fConsole IsNot Nothing).ToString() %></consoleVisible>
                                  <monitorVisible><%= (fMonitor IsNot Nothing).ToString() %></monitorVisible>
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

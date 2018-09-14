<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormEmulator
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormEmulator))
        Me.ViewToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.DebuggerToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ConsoleToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem4 = New System.Windows.Forms.ToolStripSeparator()
        Me.CopyTextToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.PasteTextToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContextMenuStripMain = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.EmulatorToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CPUClockToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MHz0477ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MHz0954ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MHz1908ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MHz3816ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MHz4770ToolStripMenuItem4 = New System.Windows.Forms.ToolStripMenuItem()
        Me.EmulationSpeedToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed25 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed50 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed100 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed150 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed200 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed400 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed800 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItemSpeed1000 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem5 = New System.Windows.Forms.ToolStripSeparator()
        Me.INT13EmulationToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.VIC20EmulationToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripSeparator()
        Me.SoftResetToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.HardResetToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem2 = New System.Windows.Forms.ToolStripSeparator()
        Me.LoadStateToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SaveStateToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem3 = New System.Windows.Forms.ToolStripSeparator()
        Me.ExitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MediaToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ZoomToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Zoom25ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Zoom50ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Zoom100ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Zoom150ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Zoom200ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Zoom400ToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem6 = New System.Windows.Forms.ToolStripSeparator()
        Me.ZoomFullScreenToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ContextMenuStripMain.SuspendLayout()
        Me.SuspendLayout()
        '
        'ViewToolStripMenuItem
        '
        Me.ViewToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.DebuggerToolStripMenuItem, Me.ConsoleToolStripMenuItem, Me.ToolStripMenuItem4, Me.CopyTextToolStripMenuItem, Me.PasteTextToolStripMenuItem})
        Me.ViewToolStripMenuItem.Image = Global.x8086NetEmuWinForms.My.Resources.Resources.tools_icon
        Me.ViewToolStripMenuItem.Name = "ViewToolStripMenuItem"
        Me.ViewToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.ViewToolStripMenuItem.Text = "Tools"
        '
        'DebuggerToolStripMenuItem
        '
        Me.DebuggerToolStripMenuItem.Name = "DebuggerToolStripMenuItem"
        Me.DebuggerToolStripMenuItem.Size = New System.Drawing.Size(135, 22)
        Me.DebuggerToolStripMenuItem.Text = "Debugger..."
        '
        'ConsoleToolStripMenuItem
        '
        Me.ConsoleToolStripMenuItem.Name = "ConsoleToolStripMenuItem"
        Me.ConsoleToolStripMenuItem.Size = New System.Drawing.Size(135, 22)
        Me.ConsoleToolStripMenuItem.Text = "Console..."
        '
        'ToolStripMenuItem4
        '
        Me.ToolStripMenuItem4.Name = "ToolStripMenuItem4"
        Me.ToolStripMenuItem4.Size = New System.Drawing.Size(132, 6)
        '
        'CopyTextToolStripMenuItem
        '
        Me.CopyTextToolStripMenuItem.Name = "CopyTextToolStripMenuItem"
        Me.CopyTextToolStripMenuItem.Size = New System.Drawing.Size(135, 22)
        Me.CopyTextToolStripMenuItem.Text = "Copy Text"
        '
        'PasteTextToolStripMenuItem
        '
        Me.PasteTextToolStripMenuItem.Name = "PasteTextToolStripMenuItem"
        Me.PasteTextToolStripMenuItem.Size = New System.Drawing.Size(135, 22)
        Me.PasteTextToolStripMenuItem.Text = "Paste Text"
        '
        'ContextMenuStripMain
        '
        Me.ContextMenuStripMain.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.EmulatorToolStripMenuItem, Me.MediaToolStripMenuItem, Me.ZoomToolStripMenuItem, Me.ViewToolStripMenuItem})
        Me.ContextMenuStripMain.Name = "ContextMenuStripMain"
        Me.ContextMenuStripMain.Size = New System.Drawing.Size(181, 114)
        '
        'EmulatorToolStripMenuItem
        '
        Me.EmulatorToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CPUClockToolStripMenuItem, Me.EmulationSpeedToolStripMenuItem, Me.ToolStripMenuItem5, Me.INT13EmulationToolStripMenuItem, Me.VIC20EmulationToolStripMenuItem, Me.ToolStripMenuItem1, Me.SoftResetToolStripMenuItem, Me.HardResetToolStripMenuItem, Me.ToolStripMenuItem2, Me.LoadStateToolStripMenuItem, Me.SaveStateToolStripMenuItem, Me.ToolStripMenuItem3, Me.ExitToolStripMenuItem})
        Me.EmulatorToolStripMenuItem.Image = Global.x8086NetEmuWinForms.My.Resources.Resources.icon
        Me.EmulatorToolStripMenuItem.Name = "EmulatorToolStripMenuItem"
        Me.EmulatorToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.EmulatorToolStripMenuItem.Text = "Emulator"
        '
        'CPUClockToolStripMenuItem
        '
        Me.CPUClockToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MHz0477ToolStripMenuItem, Me.MHz0954ToolStripMenuItem, Me.MHz1908ToolStripMenuItem, Me.MHz3816ToolStripMenuItem, Me.MHz4770ToolStripMenuItem4})
        Me.CPUClockToolStripMenuItem.Name = "CPUClockToolStripMenuItem"
        Me.CPUClockToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.CPUClockToolStripMenuItem.Text = "CPU Clock"
        '
        'MHz0477ToolStripMenuItem
        '
        Me.MHz0477ToolStripMenuItem.Name = "MHz0477ToolStripMenuItem"
        Me.MHz0477ToolStripMenuItem.Size = New System.Drawing.Size(129, 22)
        Me.MHz0477ToolStripMenuItem.Text = "4.77 MHz"
        '
        'MHz0954ToolStripMenuItem
        '
        Me.MHz0954ToolStripMenuItem.Name = "MHz0954ToolStripMenuItem"
        Me.MHz0954ToolStripMenuItem.Size = New System.Drawing.Size(129, 22)
        Me.MHz0954ToolStripMenuItem.Text = "9.54 MHz"
        '
        'MHz1908ToolStripMenuItem
        '
        Me.MHz1908ToolStripMenuItem.Name = "MHz1908ToolStripMenuItem"
        Me.MHz1908ToolStripMenuItem.Size = New System.Drawing.Size(129, 22)
        Me.MHz1908ToolStripMenuItem.Text = "19.08 MHz"
        '
        'MHz3816ToolStripMenuItem
        '
        Me.MHz3816ToolStripMenuItem.Name = "MHz3816ToolStripMenuItem"
        Me.MHz3816ToolStripMenuItem.Size = New System.Drawing.Size(129, 22)
        Me.MHz3816ToolStripMenuItem.Text = "38.16 MHz"
        '
        'MHz4770ToolStripMenuItem4
        '
        Me.MHz4770ToolStripMenuItem4.Name = "MHz4770ToolStripMenuItem4"
        Me.MHz4770ToolStripMenuItem4.Size = New System.Drawing.Size(129, 22)
        Me.MHz4770ToolStripMenuItem4.Text = "47.70 MHz"
        '
        'EmulationSpeedToolStripMenuItem
        '
        Me.EmulationSpeedToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItemSpeed25, Me.ToolStripMenuItemSpeed50, Me.ToolStripMenuItemSpeed100, Me.ToolStripMenuItemSpeed150, Me.ToolStripMenuItemSpeed200, Me.ToolStripMenuItemSpeed400, Me.ToolStripMenuItemSpeed800, Me.ToolStripMenuItemSpeed1000})
        Me.EmulationSpeedToolStripMenuItem.Name = "EmulationSpeedToolStripMenuItem"
        Me.EmulationSpeedToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.EmulationSpeedToolStripMenuItem.Text = "Emulation Speed"
        '
        'ToolStripMenuItemSpeed25
        '
        Me.ToolStripMenuItemSpeed25.Name = "ToolStripMenuItemSpeed25"
        Me.ToolStripMenuItemSpeed25.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed25.Text = "25%"
        '
        'ToolStripMenuItemSpeed50
        '
        Me.ToolStripMenuItemSpeed50.Name = "ToolStripMenuItemSpeed50"
        Me.ToolStripMenuItemSpeed50.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed50.Text = "50%"
        '
        'ToolStripMenuItemSpeed100
        '
        Me.ToolStripMenuItemSpeed100.Checked = True
        Me.ToolStripMenuItemSpeed100.CheckState = System.Windows.Forms.CheckState.Checked
        Me.ToolStripMenuItemSpeed100.Name = "ToolStripMenuItemSpeed100"
        Me.ToolStripMenuItemSpeed100.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed100.Text = "100%"
        '
        'ToolStripMenuItemSpeed150
        '
        Me.ToolStripMenuItemSpeed150.Name = "ToolStripMenuItemSpeed150"
        Me.ToolStripMenuItemSpeed150.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed150.Text = "150%"
        '
        'ToolStripMenuItemSpeed200
        '
        Me.ToolStripMenuItemSpeed200.Name = "ToolStripMenuItemSpeed200"
        Me.ToolStripMenuItemSpeed200.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed200.Text = "200%"
        '
        'ToolStripMenuItemSpeed400
        '
        Me.ToolStripMenuItemSpeed400.Name = "ToolStripMenuItemSpeed400"
        Me.ToolStripMenuItemSpeed400.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed400.Text = "400%"
        '
        'ToolStripMenuItemSpeed800
        '
        Me.ToolStripMenuItemSpeed800.Name = "ToolStripMenuItemSpeed800"
        Me.ToolStripMenuItemSpeed800.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed800.Text = "800%"
        '
        'ToolStripMenuItemSpeed1000
        '
        Me.ToolStripMenuItemSpeed1000.Name = "ToolStripMenuItemSpeed1000"
        Me.ToolStripMenuItemSpeed1000.Size = New System.Drawing.Size(108, 22)
        Me.ToolStripMenuItemSpeed1000.Text = "1000%"
        '
        'ToolStripMenuItem5
        '
        Me.ToolStripMenuItem5.Name = "ToolStripMenuItem5"
        Me.ToolStripMenuItem5.Size = New System.Drawing.Size(223, 6)
        '
        'INT13EmulationToolStripMenuItem
        '
        Me.INT13EmulationToolStripMenuItem.ForeColor = System.Drawing.Color.OrangeRed
        Me.INT13EmulationToolStripMenuItem.Name = "INT13EmulationToolStripMenuItem"
        Me.INT13EmulationToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.INT13EmulationToolStripMenuItem.Text = "Emulate Disk Access (INT 13)"
        '
        'VIC20EmulationToolStripMenuItem
        '
        Me.VIC20EmulationToolStripMenuItem.ForeColor = System.Drawing.Color.OrangeRed
        Me.VIC20EmulationToolStripMenuItem.Name = "VIC20EmulationToolStripMenuItem"
        Me.VIC20EmulationToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.VIC20EmulationToolStripMenuItem.Text = "VIC 20 Emulation"
        '
        'ToolStripMenuItem1
        '
        Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        Me.ToolStripMenuItem1.Size = New System.Drawing.Size(223, 6)
        '
        'SoftResetToolStripMenuItem
        '
        Me.SoftResetToolStripMenuItem.Name = "SoftResetToolStripMenuItem"
        Me.SoftResetToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.SoftResetToolStripMenuItem.Text = "Soft Reset (CTRL+ALT+INS)"
        '
        'HardResetToolStripMenuItem
        '
        Me.HardResetToolStripMenuItem.Name = "HardResetToolStripMenuItem"
        Me.HardResetToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.HardResetToolStripMenuItem.Text = "Hard Reset"
        '
        'ToolStripMenuItem2
        '
        Me.ToolStripMenuItem2.Name = "ToolStripMenuItem2"
        Me.ToolStripMenuItem2.Size = New System.Drawing.Size(223, 6)
        '
        'LoadStateToolStripMenuItem
        '
        Me.LoadStateToolStripMenuItem.Name = "LoadStateToolStripMenuItem"
        Me.LoadStateToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.LoadStateToolStripMenuItem.Text = "Load State..."
        '
        'SaveStateToolStripMenuItem
        '
        Me.SaveStateToolStripMenuItem.Name = "SaveStateToolStripMenuItem"
        Me.SaveStateToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.SaveStateToolStripMenuItem.Text = "Save State..."
        '
        'ToolStripMenuItem3
        '
        Me.ToolStripMenuItem3.Name = "ToolStripMenuItem3"
        Me.ToolStripMenuItem3.Size = New System.Drawing.Size(223, 6)
        '
        'ExitToolStripMenuItem
        '
        Me.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        Me.ExitToolStripMenuItem.Size = New System.Drawing.Size(226, 22)
        Me.ExitToolStripMenuItem.Text = "Exit"
        '
        'MediaToolStripMenuItem
        '
        Me.MediaToolStripMenuItem.Image = Global.x8086NetEmuWinForms.My.Resources.Resources.media_icon
        Me.MediaToolStripMenuItem.Name = "MediaToolStripMenuItem"
        Me.MediaToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.MediaToolStripMenuItem.Text = "Media..."
        '
        'ZoomToolStripMenuItem
        '
        Me.ZoomToolStripMenuItem.AutoToolTip = True
        Me.ZoomToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.Zoom25ToolStripMenuItem, Me.Zoom50ToolStripMenuItem, Me.Zoom100ToolStripMenuItem, Me.Zoom150ToolStripMenuItem, Me.Zoom200ToolStripMenuItem, Me.Zoom400ToolStripMenuItem, Me.ToolStripMenuItem6, Me.ZoomFullScreenToolStripMenuItem})
        Me.ZoomToolStripMenuItem.Image = Global.x8086NetEmuWinForms.My.Resources.Resources.zoom_icon
        Me.ZoomToolStripMenuItem.Name = "ZoomToolStripMenuItem"
        Me.ZoomToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.ZoomToolStripMenuItem.Text = "Zoom"
        '
        'Zoom25ToolStripMenuItem
        '
        Me.Zoom25ToolStripMenuItem.Name = "Zoom25ToolStripMenuItem"
        Me.Zoom25ToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.Zoom25ToolStripMenuItem.Text = "25%"
        '
        'Zoom50ToolStripMenuItem
        '
        Me.Zoom50ToolStripMenuItem.Name = "Zoom50ToolStripMenuItem"
        Me.Zoom50ToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.Zoom50ToolStripMenuItem.Text = "50%"
        '
        'Zoom100ToolStripMenuItem
        '
        Me.Zoom100ToolStripMenuItem.Checked = True
        Me.Zoom100ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked
        Me.Zoom100ToolStripMenuItem.Name = "Zoom100ToolStripMenuItem"
        Me.Zoom100ToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.Zoom100ToolStripMenuItem.Text = "100%"
        '
        'Zoom150ToolStripMenuItem
        '
        Me.Zoom150ToolStripMenuItem.Name = "Zoom150ToolStripMenuItem"
        Me.Zoom150ToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.Zoom150ToolStripMenuItem.Text = "150%"
        '
        'Zoom200ToolStripMenuItem
        '
        Me.Zoom200ToolStripMenuItem.Name = "Zoom200ToolStripMenuItem"
        Me.Zoom200ToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.Zoom200ToolStripMenuItem.Text = "200%"
        '
        'Zoom400ToolStripMenuItem
        '
        Me.Zoom400ToolStripMenuItem.Name = "Zoom400ToolStripMenuItem"
        Me.Zoom400ToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.Zoom400ToolStripMenuItem.Text = "400%"
        '
        'ToolStripMenuItem6
        '
        Me.ToolStripMenuItem6.Name = "ToolStripMenuItem6"
        Me.ToolStripMenuItem6.Size = New System.Drawing.Size(177, 6)
        '
        'ZoomFullScreenToolStripMenuItem
        '
        Me.ZoomFullScreenToolStripMenuItem.Name = "ZoomFullScreenToolStripMenuItem"
        Me.ZoomFullScreenToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.ZoomFullScreenToolStripMenuItem.Text = "Full Screen"
        '
        'FormEmulator
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(432, 367)
        Me.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "FormEmulator"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "x8086.NET"
        Me.ContextMenuStripMain.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ZoomToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Zoom25ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Zoom50ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Zoom100ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Zoom150ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Zoom200ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Zoom400ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ViewToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents DebuggerToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ConsoleToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents MediaToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents EmulatorToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents EmulationSpeedToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed25 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed50 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed100 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed150 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed200 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed400 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem1 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents HardResetToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed800 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItemSpeed1000 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem2 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents LoadStateToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents SaveStateToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem3 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents ExitToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents SoftResetToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents CPUClockToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents MHz0477ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents MHz0954ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents MHz1908ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents MHz3816ToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents MHz4770ToolStripMenuItem4 As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ContextMenuStripMain As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents ToolStripMenuItem4 As ToolStripSeparator
    Friend WithEvents CopyTextToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents PasteTextToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents INT13EmulationToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents VIC20EmulationToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem5 As ToolStripSeparator
    Friend WithEvents ToolStripMenuItem6 As ToolStripSeparator
    Friend WithEvents ZoomFullScreenToolStripMenuItem As ToolStripMenuItem
End Class

Imports System.Threading
Imports x8086NetEmu
Imports System.Text
Imports x8086NetEmuWinForms

Public Class FormDebugger
    Public Enum LastInstructionMode
        Normal
        [Call]
        Interrupt
        Repe
    End Enum

    Private Structure Breakpoint
        Public ReadOnly Segment As Integer
        Public ReadOnly Offset As Integer
        Public ReadOnly Address As UInt32

        Public Sub New(segment As Integer, offset As Integer)
            Me.Segment = segment
            Me.Offset = offset
            Me.Address = X8086.SegmentOffetToAbsolute(segment, offset)
        End Sub
    End Structure

    Public Class State
        Public Registers As X8086.GPRegisters
        Public Flags As X8086.GPFlags
        Public RAM(X8086.ROMStart - 1) As Byte

        Private cpu As X8086
        Private includeRAM As Boolean

        Public Sub New(cpu As X8086, includeRAM As Boolean)
            Me.cpu = cpu

            Registers = cpu.Registers.Clone()
            Flags = cpu.Flags.Clone()

            If includeRAM Then Array.Copy(cpu.Memory, RAM, RAM.Length)
        End Sub

        Public Sub Restore()
            cpu.Registers = Registers
            cpu.Flags = Flags

            'If includeRAM Then Array.Copy(RAM, cpu.Memory, RAM.Length)
        End Sub
    End Class

    Private Class EvaluateResult
        Public Property Value As Integer
        Public Property IsValid As Boolean

        Public Sub New()
            IsValid = True
        End Sub

        Public Sub New(value As Integer, valid As Boolean)
            Me.New()
            Me.Value = value
            Me.IsValid = valid
        End Sub
    End Class

    Private Enum DebugModes
        [Step]
        Run
    End Enum

    Private history(2 ^ 18) As State
    Private historyPointer As Integer

    Private mEmulator As X8086
    Private currentCSIP As String
    Private currentSSSP As String
    Private ignoreEvents As Boolean
    Private syncObject As New Object()
    Private isInit As Boolean
    Private breakIP As Integer = -1
    Private breakCS As Integer = -1
    Private breakPoints As New List(Of Breakpoint)
    Private baseCS As Integer
    Private baseIP As Integer

    Private lastInstruction As Queue(Of LastInstructionMode) = New Queue(Of LastInstructionMode)
    Private isShiftKeyDown As Boolean
    Private instructionDecoded As Boolean
    Private abortF8 As Boolean

    Private debugMode As DebugModes = DebugModes.Step

    Private loopWaiter As AutoResetEvent
    Private threadLoop As Thread

    Private ohpWaiter As AutoResetEvent
    Private ohpThreadLoop As Thread
    Private offsetHistoryDirection As Integer = 0

    Private abortThreads As Boolean

    Private defaultSegmentBackColor As Color = Color.FromArgb(230, 230, 230)

    Private Const numberSufixes As String = "hbo" ' hEX / bINARY / oCTAL (No dragons were invoked here...)
    Private activeInstruction As X8086.Instruction

    Private navigator As Xml.XPath.XPathNavigator = New Xml.XPath.XPathDocument(New IO.StringReader("<r/>")).CreateNavigator()
    Private rex As New RegularExpressions.Regex("([\+\-\*])")
    Private Evaluator As Func(Of String, Double) = Function(exp) CDbl(navigator.Evaluate("number(" + rex.Replace(exp, " ${1} ").Replace("/", " div ").Replace("%", " mod ") + ")"))

    Private segmentTextBoxes As New List(Of TextBox)

    Private ReadOnly selOpColor As Color = Color.FromArgb(65, 66, 96)
    Private ReadOnly bpColor As Color = Color.FromArgb(127, 54, 64)
    Private ReadOnly selOpBpColor As Color = Color.FromArgb(117, 44, 96)
    Private ReadOnly bytesColor As Color = Color.FromArgb(88 + 20, 81 + 20, 64 + 20)
    Private ReadOnly opCodeColor As Color = Color.FromArgb(97, 175, 99)
    Private ReadOnly opCodeParamsColor As Color = Color.FromArgb(35 + 20, 87 + 20, 140 + 20)

#Region "Controls' Event Handlers"
    Private Sub FormDebugger_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitListView(ListViewStack)
        AutoSizeLastColumn(ListViewStack)

        InitListView(ListViewCode)
        AutoSizeLastColumn(ListViewCode)
        ListViewCode.BackColor = Color.FromArgb(34, 40, 42)
        ListViewCode.ForeColor = Color.FromArgb(102 + 60, 80 + 20, 15 + 10)

        loopWaiter = New AutoResetEvent(False)
        ohpWaiter = New AutoResetEvent(False)
        'ohpThreadLoop = New Thread(AddressOf OffsetHistoryLoopSub)
        'ohpThreadLoop.Start()

        TextBoxBreakCS.Text = "0000"
        TextBoxBreakIP.Text = "0000"

        SetupControls(Me)
        SetupCheckBoxes()
        CacheSegmentTextBoxes()

        Dim uiRefreshThread As New Thread(Sub()
                                              Do
                                                  If Not ignoreEvents Then UpdateUI()
                                                  Thread.Sleep(500)
                                              Loop Until abortThreads
                                          End Sub) With {.IsBackground = True}
        uiRefreshThread.Start()

        ' History is disabled until it can be re-written to improve performance and avoid Out Of Memory exceptions
        ButtonForward.Enabled = False
        ButtonBack.Enabled = False
    End Sub

    Private Sub FormDebugger_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        While ignoreEvents
            Application.DoEvents()
        End While

        SyncLock syncObject
            isInit = False
            ignoreEvents = True
            abortThreads = True
            abortF8 = True

            ohpWaiter.Set()
            loopWaiter.Set()

            mEmulator.DebugMode = False
        End SyncLock
    End Sub

    Private Sub FormDebugger_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        isShiftKeyDown = e.Shift

        Select Case e.KeyCode
            Case Keys.F5
                If debugMode = DebugModes.Step Then StartStopRunMode()
            Case Keys.F8
                abortF8 = True
                If debugMode = DebugModes.Run Then debugMode = DebugModes.Step
                StepInto()
        End Select
    End Sub

    Private Sub FormDebugger_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        isShiftKeyDown = e.Shift
    End Sub

    Private Sub ButtonStep_Click(sender As Object, e As EventArgs) Handles ButtonStep.Click
        debugMode = DebugModes.Step
        StepInto()
    End Sub

    Private Sub ButtonRun_Click(sender As Object, e As EventArgs) Handles ButtonRun.Click
        StartStopRunMode()
    End Sub

    Private Sub ListViewCode_DoubleClick(sender As Object, e As EventArgs) Handles ListViewCode.DoubleClick
        If ListViewCode.SelectedItems.Count = 0 Then Exit Sub
        Dim address As String = ListViewCode.SelectedItems(0).Text
        TextBoxCS.Text = address.Split(":")(0)
        TextBoxIP.Text = address.Split(":")(1)
    End Sub

    Private Sub ListViewCode_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles ListViewCode.ItemChecked
        If e.Item.Text = "" OrElse e.Item.SubItems.Count <> 4 Then Exit Sub

        Dim segment As Integer = (Val("&h" + e.Item.Text.Split(":")(0)) And &HFFFF)
        Dim offset As Integer = (Val("&h" + e.Item.Text.Split(":")(1)) And &HFFFF)

        For Each bp In breakPoints
            If bp.Offset = offset AndAlso bp.Segment = segment Then
                breakPoints.Remove(bp)
                Exit For
            End If
        Next

        If e.Item.Checked Then
            breakPoints.Add(New Breakpoint(segment, offset))
            e.Item.BackColor = bpColor
        Else
            e.Item.BackColor = ListViewCode.BackColor
        End If
        e.Item.SubItems(1).BackColor = e.Item.BackColor
        e.Item.SubItems(2).BackColor = e.Item.BackColor
        e.Item.SubItems(3).BackColor = e.Item.BackColor
    End Sub

    Private Sub ListViewCode_ClientSizeChanged(sender As Object, e As EventArgs) Handles ListViewCode.ClientSizeChanged
        AutoSizeLastColumn(ListViewCode)
    End Sub

    Private Sub ButtonRefresh_Click(sender As Object, e As EventArgs) Handles ButtonRefresh.Click
        RefreshCodeListing()
    End Sub

    Private Sub ButtonReboot_Click(sender As Object, e As EventArgs) Handles ButtonReboot.Click
        mEmulator.HardReset()
        historyPointer = -1
        RefreshCodeListing()
    End Sub

    Private Sub ButtonDecIP_Click(sender As Object, e As EventArgs) Handles ButtonDecIP.Click
        mEmulator.Registers.IP -= 1
        RefreshCodeListing()
    End Sub

    Private Sub ButtonBack_MouseDown(sender As Object, e As MouseEventArgs) Handles ButtonBack.MouseDown
        If e.Button = Windows.Forms.MouseButtons.Left Then
            offsetHistoryDirection = -1
        ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
            offsetHistoryDirection = -100
        End If
    End Sub

    Private Sub ButtonBack_MouseUp(sender As Object, e As MouseEventArgs) Handles ButtonBack.MouseUp
        offsetHistoryDirection = 0
    End Sub

    Private Sub ButtonForward_MouseDown(sender As Object, e As MouseEventArgs) Handles ButtonForward.MouseDown
        If e.Button = Windows.Forms.MouseButtons.Left Then
            offsetHistoryDirection = 1
        ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
            offsetHistoryDirection = 100
        End If
    End Sub

    Private Sub ButtonForward_MouseUp(sender As Object, e As MouseEventArgs) Handles ButtonForward.MouseUp
        offsetHistoryDirection = 0
    End Sub
#End Region

    Public Property Emulator As X8086
        Get
            Return mEmulator
        End Get
        Set(value As X8086)
            mEmulator = value
            AddHandler mEmulator.InstructionDecoded, Sub()
                                                         If debugMode = DebugModes.Step Then UpdateUI()
                                                         instructionDecoded = True
                                                         loopWaiter.Set()
                                                     End Sub

            mEmulator.DoReschedule = True
            mEmulator.DebugMode = True

            CheckBoxCF.DataBindings.Add("Checked", mEmulator.Flags, "CF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxPF.DataBindings.Add("Checked", mEmulator.Flags, "PF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxAF.DataBindings.Add("Checked", mEmulator.Flags, "AF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxZF.DataBindings.Add("Checked", mEmulator.Flags, "ZF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxSF.DataBindings.Add("Checked", mEmulator.Flags, "SF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxTF.DataBindings.Add("Checked", mEmulator.Flags, "TF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxIF.DataBindings.Add("Checked", mEmulator.Flags, "IF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxDF.DataBindings.Add("Checked", mEmulator.Flags, "DF", False, DataSourceUpdateMode.OnPropertyChanged)
            CheckBoxOF.DataBindings.Add("Checked", mEmulator.Flags, "OF", False, DataSourceUpdateMode.OnPropertyChanged)

            isInit = True
        End Set
    End Property

    Private Sub UpdateUI()
        If ignoreEvents OrElse Not isInit Then Exit Sub

        SyncLock syncObject
            ignoreEvents = True

            Me.Invoke(Sub()
                          GenCodeAhead()
                          UpdateFlagsAndRegisters()

                          If Now.Second Mod 2 = 0 Then
                              UpdateMemory()
                              SetSegmentTextBoxesState()
                              UpdateStack()
                          End If
                      End Sub)
            ignoreEvents = False
        End SyncLock
    End Sub

    Private Sub SetSegmentTextBoxesState()
        Static asr As String

        Dim newAsr As String = mEmulator.Registers.ActiveSegmentRegister.ToString()
        If newAsr <> asr Then
            asr = newAsr

            For Each tb As TextBox In segmentTextBoxes
                If asr = tb.Name.Substring(7) Then
                    tb.BackColor = Color.LightSkyBlue
                ElseIf tb.Name <> TextBoxMem.Name Then
                    tb.BackColor = defaultSegmentBackColor
                End If
            Next
        End If
    End Sub

    Private Sub UpdateMemory()
        If Not isInit Then Exit Sub
        Static lastRes As String

        Dim address As Integer = X8086.SegmentOffetToAbsolute(EvaluateExpression(TextBoxMemSeg.Text).Value, EvaluateExpression(TextBoxMemOff.Text).Value)

        Dim b As Byte
        Dim res As New StringBuilder()
        Dim mem As New StringBuilder()
        Dim bcr As New StringBuilder()

        For i As Integer = 0 To 16 * Math.Floor(TextBoxMem.Height / TextBoxMem.Font.Height) Step 16
            mem.Clear()
            bcr.Clear().Append("    ")
            For k = 0 To 15
                b = mEmulator.Memory(address + i + k)
                mem.Append(If(k = 8, "- ", "") + $"{b:X2} ")
                If b <= 31 OrElse b > 122 Then
                    bcr.Append(".")
                Else
                    bcr.Append(Convert.ToChar(b))
                End If
            Next
            res.Append(mem)
            res.Append(bcr)
            res.AppendLine()
        Next

        Dim resAsString As String = res.ToString()
        If lastRes <> resAsString Then
            lastRes = resAsString
            TextBoxMem.Text = resAsString
        End If
    End Sub

    Private Function StringToRegister(value As String) As X8086.GPRegisters.RegistersTypes
        Select Case value.ToUpper()
            Case "AL" : Return X8086.GPRegisters.RegistersTypes.AL
            Case "AH" : Return X8086.GPRegisters.RegistersTypes.AH
            Case "AX" : Return X8086.GPRegisters.RegistersTypes.AX

            Case "BL" : Return X8086.GPRegisters.RegistersTypes.BL
            Case "BH" : Return X8086.GPRegisters.RegistersTypes.BH
            Case "BX" : Return X8086.GPRegisters.RegistersTypes.BX

            Case "CL" : Return X8086.GPRegisters.RegistersTypes.CL
            Case "CH" : Return X8086.GPRegisters.RegistersTypes.CH
            Case "CX" : Return X8086.GPRegisters.RegistersTypes.CX

            Case "DL" : Return X8086.GPRegisters.RegistersTypes.DL
            Case "DH" : Return X8086.GPRegisters.RegistersTypes.DH
            Case "DX" : Return X8086.GPRegisters.RegistersTypes.DX

            Case "CS" : Return X8086.GPRegisters.RegistersTypes.CS
            Case "IP" : Return X8086.GPRegisters.RegistersTypes.IP
            Case "SS" : Return X8086.GPRegisters.RegistersTypes.SS
            Case "SP" : Return X8086.GPRegisters.RegistersTypes.SP
            Case "BP" : Return X8086.GPRegisters.RegistersTypes.BP
            Case "SI" : Return X8086.GPRegisters.RegistersTypes.SI
            Case "DI" : Return X8086.GPRegisters.RegistersTypes.DI
            Case "DS" : Return X8086.GPRegisters.RegistersTypes.DS
            Case "ES" : Return X8086.GPRegisters.RegistersTypes.ES

            Case "AS" : Return mEmulator.Registers.Val(mEmulator.Registers.ActiveSegmentRegister)

            Case Else
                Return X8086.GPRegisters.RegistersTypes.NONE
        End Select
    End Function

    Private Function EvaluateExpression(value As String) As EvaluateResult
        If value = "" Then Return New EvaluateResult()
        'value = value.ToUpper()
        Dim result As Integer = 0

        If value.Contains("AS") Then
            value = value.Replace("AS", mEmulator.Registers.Val(mEmulator.Registers.ActiveSegmentRegister).ToString() + "d")
        Else
            For Each reg In [Enum].GetNames(GetType(X8086.GPRegisters.RegistersTypes))
                If value.Contains(reg) Then
                    value = value.Replace(reg, mEmulator.Registers.Val(StringToRegister(reg)).ToString() + "d")
                End If
            Next
        End If

        Dim GetNumber = Function(s As String, p As Integer) As Array
                            Dim i As Integer
                            Dim n As String = ""
                            For i = p - 1 To 0 Step -1
                                If Not Char.IsLetterOrDigit(s(i)) Then Exit For
                                n = s(i) + n
                            Next
                            Dim r As Integer
                            Return If(Binary.TryParse(n, r), {r, i, True}, {r, i, False})
                        End Function

        Dim HasBaseSuffix = Function(s As Char)
                                If Char.IsLetter(s) AndAlso Char.IsLower(s) Then
                                    Return s = "h" OrElse s = "b" OrElse s = "d" OrElse s = "o"
                                Else
                                    Return False
                                End If
                            End Function

        Dim properFormat As Boolean = HasBaseSuffix(value.Last())
        If value.Length = 1 Then
            If Not properFormat Then value += "h"
        ElseIf Not properFormat AndAlso Char.IsLetterOrDigit(value.Last()) Then
            value += "h"
        End If
        value += " "

        Dim isDone As Boolean
        Do
            isDone = True
            For i As Integer = 0 To value.Length - 1
                If Not Char.IsLetterOrDigit(value(i)) AndAlso i > 0 Then
                    If HasBaseSuffix(value(i - 1)) Then
                        Dim data = GetNumber(value, i)
                        If data(2) = True Then
                            value = value.Substring(0, data(1) + 1) + data(0).ToString() + value.Substring(i)
                            isDone = False
                            Exit For
                        End If
                    End If
                End If
            Next
        Loop Until isDone

        Try
            result = Evaluator(value)
            Return New EvaluateResult(result, True)
        Catch
            Return New EvaluateResult(result, False)
        End Try
    End Function

    Private Sub UpdateFlagsAndRegisters()
        With mEmulator
            With .Registers
                If Not TextBoxAH.Focused Then TextBoxAH.Text = $"{ .AH:X2}"
                If Not TextBoxAL.Focused Then TextBoxAL.Text = $"{ .AL:X2}"

                If Not TextBoxBH.Focused Then TextBoxBH.Text = $"{ .BH:X2}"
                If Not TextBoxBL.Focused Then TextBoxBL.Text = $"{ .BL:X2}"

                If Not TextBoxCH.Focused Then TextBoxCH.Text = $"{ .CH:X2}"
                If Not TextBoxCL.Focused Then TextBoxCL.Text = $"{ .CL:X2}"

                If Not TextBoxDH.Focused Then TextBoxDH.Text = $"{ .DH:X2}"
                If Not TextBoxDL.Focused Then TextBoxDL.Text = $"{ .DL:X2}"

                If Not TextBoxCS.Focused Then TextBoxCS.Text = $"{ .CS:X4}"
                If Not TextBoxIP.Focused Then TextBoxIP.Text = $"{ .IP:X4}"

                If Not TextBoxSS.Focused Then TextBoxSS.Text = $"{ .SS:X4}"
                If Not TextBoxSP.Focused Then TextBoxSP.Text = $"{ .SP:X4}"

                If Not TextBoxBP.Focused Then TextBoxBP.Text = $"{ .BP:X4}"
                If Not TextBoxSI.Focused Then TextBoxSI.Text = $"{ .SI:X4}"

                If Not TextBoxDS.Focused Then TextBoxDS.Text = $"{ .DS:X4}"
                If Not TextBoxDI.Focused Then TextBoxDI.Text = $"{ .DI:X4}"

                If Not TextBoxES.Focused Then TextBoxES.Text = $"{ .ES:X4}"
            End With
        End With

        CheckBoxCF.DataBindings(0).ReadValue()
        CheckBoxPF.DataBindings(0).ReadValue()
        CheckBoxAF.DataBindings(0).ReadValue()
        CheckBoxZF.DataBindings(0).ReadValue()
        CheckBoxSF.DataBindings(0).ReadValue()
        CheckBoxTF.DataBindings(0).ReadValue()
        CheckBoxIF.DataBindings(0).ReadValue()
        CheckBoxDF.DataBindings(0).ReadValue()
        CheckBoxOF.DataBindings(0).ReadValue()
    End Sub

    Private Sub UpdateStack()
        Dim index As Integer = 0

        'If ListViewStack.Items.ContainsKey(currentSSSP) Then
        '    With ListViewStack.Items(currentSSSP)
        '        .BackColor = ListViewStack.BackColor
        '        .SubItems(1).BackColor = ListViewStack.BackColor
        '    End With
        'End If

        With mEmulator
            currentSSSP = X8086.SegmentOffetToAbsolute(.Registers.SS, .Registers.SP).ToString("X5")

            Dim offset As Integer = 0
            If (.Registers.SP Mod 2) = 0 Then offset = 1

            Dim startOffset As Integer = Math.Min(Math.Max(.Registers.SP + 0, .Registers.SP + 128), &HFFFF - offset)
            Dim endOffset As Integer = Math.Max(Math.Min(.Registers.SP + 0, .Registers.SP - 128), 0 + offset)

            Dim address As String
            Dim value As Integer
            Dim item As ListViewItem

            For ptr As Integer = startOffset To endOffset Step -2
                address = X8086.SegmentOffetToAbsolute(.Registers.SS, ptr).ToString("X5")
                value = .RAM16(.Registers.SS, ptr,, True)

                If index < ListViewStack.Items.Count Then
                    item = ListViewStack.Items(index)
                Else
                    item = ListViewStack.Items.Add(address, "", 0)
                    item.SubItems.Add("")
                End If
                item.Text = $"{ .Registers.SS:X4}:{ptr:X4}"
                item.SubItems(1).Text = value.ToString("X4")
                If ptr = .Registers.SP Then
                    item.BackColor = Color.DarkSlateBlue
                    item.EnsureVisible()
                Else
                    item.BackColor = ListViewStack.BackColor
                End If

                index += 1
            Next
        End With

        Do While ListViewStack.Items.Count > index
            ListViewStack.Items.RemoveAt(ListViewStack.Items.Count - 1)
        Loop
    End Sub

    Private Sub GenCodeAhead()
        Dim item As ListViewItem
        Dim CS As UInt16 = mEmulator.Registers.CS
        Dim IP As UInt16 = mEmulator.Registers.IP
        Dim insIndex As Integer
        Dim insertedCount As Integer
        Dim newCount As Integer
        Dim sbBytes As New StringBuilder()
        Dim address As UInt32
        Dim addressStr As String
        Dim info As X8086.Instruction
        Dim curIP As String

        If ListViewCode.Items.ContainsKey(currentCSIP) Then
            With ListViewCode.Items(currentCSIP)
                .BackColor = ListViewCode.BackColor
                .SubItems(1).BackColor = ListViewCode.BackColor
                .SubItems(2).BackColor = ListViewCode.BackColor
                .SubItems(3).BackColor = ListViewCode.BackColor
            End With
        End If

        currentCSIP = X8086.SegmentOffetToAbsolute(CS, IP).ToString("X5")
        Do
            address = X8086.SegmentOffetToAbsolute(CS, IP)
            addressStr = address.ToString("X5")

            insIndex = -1
            If ListViewCode.Items.ContainsKey(addressStr) Then
                item = ListViewCode.Items(addressStr)
                insertedCount += 1
            Else
                For Each sItem As ListViewItem In ListViewCode.Items
                    If sItem.Tag > address Then
                        insIndex = sItem.Index
                        Exit For
                    End If
                Next
                If insIndex <> -1 Then
                    item = ListViewCode.Items.Insert(insIndex, addressStr, "", 0)
                    insertedCount += 1
                Else
                    item = ListViewCode.Items.Add(addressStr, "", 0)
                    newCount += 1
                End If
                item.SubItems.Add("")
                item.SubItems.Add("")
                item.SubItems.Add("")
                item.Tag = address
                item.UseItemStyleForSubItems = False

                item.ForeColor = ListViewCode.ForeColor
                item.SubItems(1).ForeColor = bytesColor
                item.SubItems(2).ForeColor = opCodeColor
                item.SubItems(3).ForeColor = opCodeParamsColor
            End If

            info = mEmulator.Decode(CS, IP)
            If Not info.IsValid Then Exit Do

            curIP = IP.ToString("X4")
            IP = (IP + info.Size) Mod &HFFFF

            If item.Text = "" Then
                item.Text = $"{info.CS:X4}:{info.IP:X4}"
                item.SubItems(1).Text = GetBytesString(sbBytes, info.Bytes)
                item.SubItems(2).Text = info.Mnemonic
                If info.Message = "" Then
                    item.SubItems(3).Text = info.Parameter1?.Replace("[", "[" + info.SegmentOverride) + If(info.Parameter2 = "", "", ", " + info.Parameter2?.Replace("[", "[" + info.SegmentOverride))
                Else
                    item.SubItems(3).Text = info.Message
                End If
            End If

            ' TODO: For this to work correctly we need to implement the coloring in the subclassed ListView
            If Not item.Checked AndAlso breakPoints.Any(Function(b) b.Address = address) Then item.Checked = True
            If addressStr = currentCSIP Then
                item.BackColor = If(item.Checked, selOpBpColor, selOpColor)
                item.SubItems(1).BackColor = item.BackColor
                item.SubItems(2).BackColor = item.BackColor
                item.SubItems(3).BackColor = item.BackColor
                item.EnsureVisible() ' TODO: Implement option to enable/disable this
                activeInstruction = info
            ElseIf Not item.Checked AndAlso item.BackColor <> ListViewCode.BackColor Then
                item.SubItems(1).BackColor = item.BackColor
                item.SubItems(2).BackColor = item.BackColor
                item.SubItems(3).BackColor = item.BackColor
            ElseIf item.Checked AndAlso item.BackColor <> bpColor Then
                item.BackColor = bpColor
                item.SubItems(1).BackColor = item.BackColor
                item.SubItems(2).BackColor = item.BackColor
                item.SubItems(3).BackColor = item.BackColor
            End If
        Loop Until (newCount >= 100) OrElse (insertedCount >= 100) OrElse (ListViewCode.Items.Count >= 1000)
    End Sub

    Private Sub StepInto()
        If Not mEmulator.DebugMode Then mEmulator.DebugMode = True
        DoStep()
    End Sub

    Private Sub RefreshCodeListing()
        ListViewStack.Items.Clear()
        ListViewCode.Items.Clear()
    End Sub

    Private Sub StartStopRunMode()
        If Not mEmulator.DebugMode Then mEmulator.DebugMode = True

        If debugMode = DebugModes.Run Then
            debugMode = DebugModes.Step
        Else
            debugMode = DebugModes.Run

            threadLoop = New Thread(AddressOf RunLoop)
            threadLoop.Start()
        End If
    End Sub

    Private Sub DoStep()
        'Try
        '    ' FIXME: This is too slow

        '    If Not mEmulator.IsHalted Then
        '        If historyPointer = history.Length - 1 Then
        '            Array.Copy(history, 1, history, 0, history.Length - 1)
        '        Else
        '            historyPointer += 1
        '        End If
        '        history(historyPointer) = New State(mEmulator, True)
        '    End If
        'Catch ex As Exception
        '    ' TODO: Implement a better solution to the case when this code is executed and historyPointer is set to -1
        'End Try

        If isShiftKeyDown Then
            Tasks.Task.Run(AddressOf RunShiftF8)
        Else
            mEmulator.StepInto()
        End If
    End Sub

    Private Sub RunShiftF8() ' Step Over
        Dim oc As Byte

        Dim processOpCode = Sub(ignoreNormal As Boolean)
                                oc = mEmulator.RAM8(mEmulator.Registers.CS, mEmulator.Registers.IP,, True)

                                Select Case oc
                                    Case &H9A, &HE8 : lastInstruction.Enqueue(LastInstructionMode.Call)
                                    Case &HFF
                                        Dim am As New X8086.AddressingMode()
                                        am.Decode(oc, mEmulator.RAM8(mEmulator.Registers.CS, mEmulator.Registers.IP, 1, True))
                                        If am.Reg = 2 OrElse am.Reg = 3 Then
                                            lastInstruction.Enqueue(LastInstructionMode.Call)
                                        Else
                                            If Not ignoreNormal Then lastInstruction.Enqueue(LastInstructionMode.Normal)
                                        End If
                                    Case &HCC To &HCE : lastInstruction.Enqueue(LastInstructionMode.Interrupt)
                                        ' Not yet supported
                                        'Case &HF2, &HF3 : lastInstruction.Enqueue(LastInstructionMode.Repe)
                                    Case Else : If Not ignoreNormal Then lastInstruction.Enqueue(LastInstructionMode.Normal)
                                End Select
                            End Sub

        Dim kickEmulation = Sub()
                                instructionDecoded = False
                                mEmulator.StepInto()
                                Tasks.Task.Run(Sub()
                                                   While Not instructionDecoded
                                                       Thread.Sleep(1)
                                                   End While
                                               End Sub).Wait()
                            End Sub

        ignoreEvents = True
        processOpCode(False)

        Do
            Select Case lastInstruction.Peek()
                Case LastInstructionMode.Normal
                    lastInstruction.Dequeue()
                    mEmulator.StepInto()
                    Exit Do
                Case LastInstructionMode.Call
                    kickEmulation()
                    oc = mEmulator.RAM8(mEmulator.Registers.CS, mEmulator.Registers.IP,, True)
                    If oc = &HC2 OrElse oc = &HC3 OrElse oc = &HCA OrElse oc = &HCB Then
                        kickEmulation()
                        lastInstruction.Dequeue()
                        If lastInstruction.Count = 0 Then Exit Do
                    End If
                Case LastInstructionMode.Interrupt
                    kickEmulation()
                    oc = mEmulator.RAM8(mEmulator.Registers.CS, mEmulator.Registers.IP,, True)
                    If oc = &HCA OrElse oc = &HCF Then
                        kickEmulation()
                        lastInstruction.Dequeue()
                        If lastInstruction.Count = 0 Then Exit Do
                    End If
            End Select
            processOpCode(True)
        Loop While Not abortF8 AndAlso lastInstruction.Count > 0
        lastInstruction.Clear()
        ignoreEvents = False
    End Sub

    Private Sub RunLoop()
        Dim count As Integer = 0
        Dim maxSteps As Integer = 1000 ' Execute 'maxSteps' instructions before updating the UI
        Dim lastAddress As Integer = -1
        'Dim instructions As New List(Of x8086.Instruction)
        Dim lastREPEMode As X8086.REPLoopModes

        Do
            ignoreEvents = True
            Do
                lastREPEMode = mEmulator.REPELoopMode
                DoStep()
            Loop Until (lastREPEMode = X8086.REPLoopModes.None AndAlso mEmulator.REPELoopMode = X8086.REPLoopModes.None) OrElse
                            abortThreads OrElse
                            mEmulator.IsHalted

            ignoreEvents = False
            loopWaiter.WaitOne()
            If abortThreads Then Exit Do
            ignoreEvents = True

            If mEmulator.IsHalted Then StartStopRunMode()

            SyncLock syncObject
                'Dim instruction = mEmulator.Decode(mEmulator)
                'If Not instructions.Contains(instruction) Then instructions.Add(instruction)

                If breakIP = mEmulator.Registers.IP AndAlso breakCS = mEmulator.Registers.CS Then
                    Beep()
                    StartStopRunMode()
                    ignoreEvents = False
                    Continue Do
                End If

                If breakPoints.Count > 0 Then
                    For Each bp In breakPoints
                        If bp.Offset = mEmulator.Registers.IP AndAlso bp.Segment = mEmulator.Registers.CS Then
                            Beep()
                            StartStopRunMode()
                            ignoreEvents = False
                            Continue Do
                        End If
                    Next
                End If
            End SyncLock

            ignoreEvents = False
        Loop Until abortThreads OrElse debugMode = DebugModes.Step
    End Sub

    Private Sub InitListView(lv As ListView)
        ListViewHelper.EnableDoubleBuffer(lv)

        Dim item As ListViewItem = Nothing
        Select Case lv.Name
            Case ListViewCode.Name
                item = lv.Items.Add("FFFF:FFFF".Replace("F", " "))
                With item
                    .SubItems.Add("FF FF FF FF FF FF".Replace("F", " "))
                    .SubItems.Add("FFFFFF".Replace("F", " "))
                    .SubItems.Add("FFFFFFFFFFFFFFFFFFFF".Replace("F", " "))
                End With
            Case ListViewStack.Name
                item = lv.Items.Add("FFFF FFFF".Replace("F", " "))
                item.SubItems.Add("FFFF".Replace("F", " "))
        End Select

        lv.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
        item.Remove()
    End Sub

    Private Sub AutoSizeLastColumn(lv As ListView)
        Dim w As Integer = lv.ClientSize.Width
        Select Case lv.BorderStyle
            Case BorderStyle.Fixed3D : w -= 4
            Case BorderStyle.FixedSingle : w -= (2 + 16)
        End Select
        For i As Integer = 0 To lv.Columns.Count - 1
            w -= lv.Columns(i).Width
            If w < 1 Then Exit For
        Next

        lv.Columns(lv.Columns.Count - 1).Width += w
    End Sub

    Private Function GetBytesString(r As StringBuilder, b() As Byte) As String
        r.Clear()
        If b IsNot Nothing Then
            For i As Integer = 0 To b.Length - 1
                If CheckBoxBytesOrChars.Checked Then
                    r.Append(Convert.ToChar(b(i)))
                Else
                    r.AppendFormat("{0:X2}", b(i))
                End If
                r.Append(" ")
            Next
        End If
        Return r.ToString().TrimEnd()
    End Function

    Private Sub OffsetHistoryPointer(value As Integer)
        If (historyPointer + value > 0) AndAlso (historyPointer + value < history.Length - 1) Then
            historyPointer += value

            history(historyPointer).Restore()

            RefreshCodeListing()
        End If
    End Sub

    Private Sub OffsetHistoryLoopSub()
        Do
            ohpWaiter.WaitOne(30)
            If abortThreads Then Exit Sub

            If offsetHistoryDirection <> 0 Then Me.Invoke(Sub() OffsetHistoryPointer(offsetHistoryDirection))
        Loop
    End Sub

    Private Sub SetupControls(container As Control)
        For Each c As Control In container.Controls
            If TypeOf c Is TextBox Then
                Dim tb = CType(c, TextBox)
                If tb.Name <> TextBoxMem.Name AndAlso tb.Name <> TextBoxSearch.Name Then
                    AddHandler tb.MouseEnter, Sub() ToolTipValueInfo.SetToolTip(tb, TextBoxValueToHuman(tb))
                    AddHandler tb.KeyUp, Sub() SetItemValue(tb)
                End If
            ElseIf c.Controls.Count > 0 Then
                SetupControls(c)
            End If
        Next
    End Sub

    Public Sub SetupCheckBoxes()
        AddHandler CheckBoxCF.Click, Sub() mEmulator.Flags.CF = If(CheckBoxCF.Checked, 1, 0)
        AddHandler CheckBoxZF.Click, Sub() mEmulator.Flags.ZF = If(CheckBoxZF.Checked, 1, 0)
        AddHandler CheckBoxSF.Click, Sub() mEmulator.Flags.SF = If(CheckBoxSF.Checked, 1, 0)
        AddHandler CheckBoxOF.Click, Sub() mEmulator.Flags.OF = If(CheckBoxOF.Checked, 1, 0)
        AddHandler CheckBoxPF.Click, Sub() mEmulator.Flags.PF = If(CheckBoxPF.Checked, 1, 0)
        AddHandler CheckBoxAF.Click, Sub() mEmulator.Flags.AF = If(CheckBoxAF.Checked, 1, 0)
        AddHandler CheckBoxIF.Click, Sub() mEmulator.Flags.IF = If(CheckBoxIF.Checked, 1, 0)
        AddHandler CheckBoxDF.Click, Sub() mEmulator.Flags.DF = If(CheckBoxDF.Checked, 1, 0)
        AddHandler CheckBoxTF.Click, Sub() mEmulator.Flags.TF = If(CheckBoxTF.Checked, 1, 0)
    End Sub

    Private Sub CacheSegmentTextBoxes()
        segmentTextBoxes.Add(TextBoxCS)
        segmentTextBoxes.Add(TextBoxSS)
        segmentTextBoxes.Add(TextBoxDS)
        segmentTextBoxes.Add(TextBoxES)
    End Sub

    Private Function TextBoxValueToHuman(tb As TextBox) As String
        Dim value As Binary

        Try
            value = Binary.From(EvaluateExpression(tb.Text).Value) And &HFFFF
        Catch
            Return ""
        End Try

        Return String.Format("{1:N0}d{0}{2}h{0}{3}b", Environment.NewLine,
                                 value.ToLong(),
                                 value.ToHex(),
                                 value.ToString())
    End Function

    Private Sub SetItemValue(tb As TextBox)
        Dim evalRes As EvaluateResult = EvaluateExpression(tb.Text)

        Select Case tb.Name
            Case TextBoxBreakCS.Name : breakCS = evalRes.Value
            Case TextBoxBreakIP.Name : breakIP = evalRes.Value

            Case TextBoxMemSeg.Name : UpdateMemory()
            Case TextBoxMemOff.Name : UpdateMemory()

            Case Else
                mEmulator.Registers.Val(StringToRegister(tb.Name.Substring(7, 2))) = evalRes.Value
        End Select

        tb.BackColor = If(evalRes.IsValid, Color.FromKnownColor(KnownColor.Window), Color.Red)
    End Sub

    Private Sub ButtonSearch_Click(sender As Object, e As EventArgs) Handles ButtonSearch.Click
        Dim str As String = TextBoxSearch.Text.ToLower()
        Dim tmp As String = ""
        Dim buffer(str.Length - 1) As Byte
        Dim found As Boolean

        ButtonSearch.Enabled = False
        TextBoxSearch.Enabled = False

        Dim startIndex As Integer = X8086.SegmentOffetToAbsolute(EvaluateExpression(TextBoxMemSeg.Text).Value, EvaluateExpression(TextBoxMemOff.Text).Value)
        Dim endIndex As Integer = X8086.MemSize - 1 - str.Length

        If startIndex <> 0 Then startIndex += 1

        Do
            If CheckBoxTextVideoMemory.Checked Then
                Dim j As Integer
                For i As Integer = startIndex To endIndex
                    found = True

                    For j = 0 To str.Length - 1 Step 2
                        If i + j * 2 >= X8086.MemSize Then
                            found = False
                            Exit For
                        End If

                        If mEmulator.Memory(i + j * 2) <> Asc(str(j)) Then
                            found = False
                            Exit For
                        End If
                    Next

                    If found Then
                        TextBoxMemSeg.Text = X8086.AbsoluteToSegment(i).ToString("X4")
                        TextBoxMemOff.Text = X8086.AbsoluteToOffset(i).ToString("X4")

                        Exit Do
                    End If
                Next
            Else
                For i As Integer = startIndex To endIndex
                    Array.Copy(mEmulator.Memory, i, buffer, 0, str.Length)
                    If ASCIIEncoding.ASCII.GetString(buffer).ToLower() = str Then
                        TextBoxMemSeg.Text = X8086.AbsoluteToSegment(i).ToString("X4")
                        TextBoxMemOff.Text = X8086.AbsoluteToOffset(i).ToString("X4")

                        found = True
                        Exit Do
                    End If
                Next
            End If

            If startIndex = 0 Then
                Exit Do
            Else
                endIndex = startIndex
                startIndex = 0
            End If
        Loop

        TextBoxSearch.BackColor = If(found, Color.LightGreen, Color.LightSalmon)

        UpdateMemory()

        ButtonSearch.Enabled = True
        TextBoxSearch.Enabled = True
    End Sub

    Private Sub ButtonMemBack_Click(sender As Object, e As EventArgs) Handles ButtonMemBack.Click
        Dim address As Integer = X8086.SegmentOffetToAbsolute(EvaluateExpression(TextBoxMemSeg.Text).Value, EvaluateExpression(TextBoxMemOff.Text).Value)
        address -= 256
        TextBoxMemSeg.Text = X8086.AbsoluteToSegment(address).ToString("X4")
        TextBoxMemOff.Text = X8086.AbsoluteToOffset(address).ToString("X4")

        UpdateMemory()
    End Sub

    Private Sub ButtonMemForward_Click(sender As Object, e As EventArgs) Handles ButtonMemForward.Click
        Dim address As Integer = X8086.SegmentOffetToAbsolute(EvaluateExpression(TextBoxMemSeg.Text).Value, EvaluateExpression(TextBoxMemOff.Text).Value)
        address += 256
        TextBoxMemSeg.Text = X8086.AbsoluteToSegment(address).ToString("X4")
        TextBoxMemOff.Text = X8086.AbsoluteToOffset(address).ToString("X4")

        UpdateMemory()
    End Sub

    Private Sub TextBoxSearch_TextChanged(sender As Object, e As EventArgs) Handles TextBoxSearch.TextChanged
        TextBoxSearch.BackColor = Color.FromKnownColor(KnownColor.Window)
    End Sub

    Private Sub CheckBoxBytesOrChars_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxBytesOrChars.CheckedChanged
        ListViewCode.Items.Clear()
    End Sub
End Class
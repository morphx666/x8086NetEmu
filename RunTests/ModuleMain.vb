Imports x8086NetEmu
Imports System.Threading

Module ModuleMain
    Private Const DebugMode As Boolean = False
    Private Const TraceDelay As Integer = 50

    Private cpu As X8086
    Private validData() As Byte = Nothing
    Private testsTotal As Integer = 0
    Private failedTotal As Integer = 0
    Private prefix As String
    Private inst As New List(Of String)

    Sub Main()
        Dim waiter As New AutoResetEvent(False)

        ' X8086.Models.IBMPC_5150 is required as fake86 does not properly handle eflags
        cpu = New X8086(True, False,, X8086.Models.IBMPC_5150) With {.Clock = 47700000}
        AddHandler cpu.EmulationHalted, Sub()
                                            Compare()
                                            Console.WriteLine()
                                            waiter.Set()
                                        End Sub

        X8086.LogToConsole = False
        Console.CursorVisible = False

        For Each f As IO.FileInfo In (New IO.DirectoryInfo(IO.Path.Combine(My.Application.Info.DirectoryPath, "80186_tests"))).GetFiles("*.bin")
            Dim fileName As String = f.Name.Replace(f.Extension, "")
            Dim dataFileName As String = IO.Path.Combine(f.DirectoryName, $"res_{fileName}.bin")

            'If fileName <> "segpr" Then Continue For

            If Not IO.File.Exists(dataFileName) Then Continue For
            validData = IO.File.ReadAllBytes(dataFileName)

            prefix = $"Running: {fileName}"
            Console.Write(prefix)

            If cpu.IsHalted Then cpu.HardReset()
            cpu.LoadBIN(f.FullName, &HF000, &H0)
            cpu.Run(DebugMode, &HF000, &H0)

            If DebugMode Then
                While Not cpu.IsHalted
                    DisplayInstructions()
                    cpu.StepInto()
                    Thread.Sleep(TraceDelay)
                End While
            End If

            waiter.WaitOne()
        Next
        cpu.Close()

        Dim passedTotal As Integer = testsTotal - failedTotal
        Console.ForegroundColor = ConsoleColor.Magenta
        Console.WriteLine($"Score: {passedTotal}/{testsTotal} [{passedTotal / testsTotal * 100:N2}%]")
        Console.ForegroundColor = ConsoleColor.Gray
        Console.WriteLine()

        Console.WriteLine("Press any key to exit")
        Console.ReadKey()
        Console.CursorVisible = True
    End Sub

    Private Sub DisplayInstructions()
        If inst.Count > Console.WindowHeight - 1 Then inst.RemoveAt(0)
        inst.Add(cpu.Decode().ToString())

        Dim c As Integer = Console.CursorLeft
        Dim r As Integer = Console.CursorTop
        Dim cw As Integer = Console.WindowWidth / 2

        For i As Integer = 0 To inst.Count - 1
            Console.SetCursorPosition(cw, i)
            Console.Write(inst(i).PadRight(cw - 1, " "))
        Next

        Console.SetCursorPosition(c, r)
    End Sub

    Private Sub Compare()
        Const p As Integer = 28

        Dim txt As String = ""
        Dim v1 As String
        Dim v2 As String
        Dim invalidData As New List(Of String)
        Dim dataLen As Integer = validData.Length / 2

        testsTotal += dataLen

        For i As Integer = 0 To dataLen - 1 Step 2
            v1 = cpu.RAM16(0, i).ToString("X4")
            v2 = BitConverter.ToInt16(validData, i).ToString("X4")
            If v1 <> v2 Then invalidData.Add($"0000:{i:X4} {v1} <> {v2}")
        Next

        If invalidData.Any() Then
            txt = $" > FAILED [{invalidData.Count}/{dataLen}]"
            Console.WriteLine(txt.PadLeft(p - prefix.Length + txt.Length))
            invalidData.ForEach(Sub(id)
                                    Dim t() As String = id.Split(" "c)
                                    Console.ForegroundColor = ConsoleColor.White
                                    Console.Write($"  {t(0)}")
                                    Console.ForegroundColor = ConsoleColor.Red
                                    Console.Write($" {t(1)}")
                                    Console.ForegroundColor = ConsoleColor.Gray
                                    Console.Write($" {t(2)}")
                                    Console.ForegroundColor = ConsoleColor.Green
                                    Console.WriteLine($" {t(3)}")
                                    Console.ForegroundColor = ConsoleColor.Gray
                                    'Console.WriteLine($"  {id}")
                                End Sub)
            failedTotal += invalidData.Count
        Else
            txt = $" > PASSED [{dataLen}]"
            Console.WriteLine(txt.PadLeft(p - prefix.Length + txt.Length))
        End If
    End Sub
End Module
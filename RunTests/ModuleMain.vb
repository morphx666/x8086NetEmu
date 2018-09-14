Imports x8086NetEmu
Imports System.Threading

Module ModuleMain
    Private cpu As X8086
    Private validData() As Byte = Nothing
    Private testsTotal As Integer = 0
    Private failedTotal As Integer = 0
    Private prefix As String

    Sub Main()
        Dim waiter As New AutoResetEvent(False)

        cpu = New X8086(True, False,, X8086.Models.IBMPC_5150) With {.Clock = 47700000}
        AddHandler cpu.EmulationHalted, Sub()
                                            Compare()
                                            Console.WriteLine()
                                            waiter.Set()
                                        End Sub

        X8086.LogToConsole = False

        For Each f As IO.FileInfo In (New IO.DirectoryInfo(IO.Path.Combine(My.Application.Info.DirectoryPath, "80186_tests"))).GetFiles("*.bin")
            Dim fileName As String = f.Name.Replace(f.Extension, "")
            Dim dataFileName As String = IO.Path.Combine(f.DirectoryName, $"res_{fileName}.bin")

            If fileName <> "div" Then Continue For
            'If fileName = "rep" Then Continue For

            If Not IO.File.Exists(dataFileName) Then Continue For
            validData = IO.File.ReadAllBytes(dataFileName)

            prefix = $"Running: {fileName}"
            Console.Write(prefix)

            If cpu.IsHalted Then cpu.HardReset()
            cpu.LoadBIN(f.FullName, &HF000, &H0)
            cpu.Run(, &HF000, &H0)

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
            If v1 <> v2 Then
                invalidData.Add($"0000:{i:X4} {v1} <> {v2}")
            End If
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
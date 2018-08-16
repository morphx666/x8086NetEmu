Imports x8086NetEmu
Imports System.Threading

Module ModuleMain
    Private cpu As X8086
    Private validData() As Byte = Nothing

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

            'If fileName <> "segpr" Then Continue For
            'If fileName = "segpr" Then Continue For

            If Not IO.File.Exists(dataFileName) Then Continue For
            validData = IO.File.ReadAllBytes(dataFileName)

            Console.Write($"Running: {fileName}")

            If cpu.IsHalted Then cpu.HardReset()
            cpu.LoadBIN(f.FullName, &HF000, &H0)
            cpu.Run(, &HF000, &H0)

            waiter.WaitOne()
        Next
        cpu.Close()

#If DEBUG Then
        Console.WriteLine("Press any key to exit")
        Console.ReadKey()
#End If
    End Sub

    Private Sub Compare()
        Dim v1 As String
        Dim v2 As String
        Dim invalidData As New List(Of String)
        For i As Integer = 0 To validData.Length / 2 - 1 Step 2
            v1 = cpu.RAM16(0, i).ToString("X4")
            v2 = BitConverter.ToInt16(validData, i).ToString("X4")
            If v1 <> v2 Then
                invalidData.Add($"0000:{i:X4} {v1} <> {v2}")
            End If
        Next
        If invalidData.Any() Then
            Console.WriteLine($" > FAILED [{invalidData.Count}]")
            invalidData.ForEach(Sub(id) Console.WriteLine($"  {id}"))
        Else
            Console.WriteLine(" > PASSED")
        End If
    End Sub
End Module
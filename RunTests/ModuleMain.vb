Imports x8086NetEmu
Imports System.Threading
Imports x8086NetEmu.X8086

Module ModuleMain
    Sub Main()
        Dim validData() As Byte = Nothing
        Dim hasErrors As Boolean = False
        Dim waiter As New AutoResetEvent(False)

        X8086.LogToConsole = False

        For Each f As IO.FileInfo In (New IO.DirectoryInfo(IO.Path.Combine(My.Application.Info.DirectoryPath, "80186_tests"))).GetFiles("*.bin")
            Dim fileName As String = f.Name.Replace(f.Extension, "")
            Dim dataFileName As String = IO.Path.Combine(f.DirectoryName, $"res_{fileName}.bin")

            If Not IO.File.Exists(dataFileName) Then Continue For
            validData = IO.File.ReadAllBytes(dataFileName)
            hasErrors = False

            Console.Write($"Running: {fileName}")

            Dim cpu As X8086 = New X8086(True, True)
            AddHandler cpu.EmulationHalted, Sub()
                                                Dim invalidData As New List(Of String)
                                                For i As Integer = 0 To validData.Length - 1
                                                    If cpu.RAM8(0, i) <> validData(i) Then
                                                        invalidData.Add($"0000:{i:X4} {cpu.RAM8(0, i):X2} <> {validData(i):X2}")
                                                    End If
                                                Next
                                                If invalidData.Any() Then
                                                    If Not hasErrors Then Console.WriteLine(": FAILED")
                                                    invalidData.ForEach(Sub(id) Console.WriteLine($"  {id}"))
                                                Else
                                                    Console.WriteLine(": PASSED")
                                                End If
                                                Console.WriteLine()
                                                waiter.Set()
                                            End Sub
            AddHandler cpu.Error, Sub(sender As Object, e As EmulatorErrorEventArgs)
                                      If Not (hasErrors OrElse cpu.IsHalted) Then
                                          hasErrors = True
                                          Console.WriteLine(": FAILED")
                                          Console.WriteLine($"  {cpu.Registers.CS:X4}:{cpu.Registers.IP:X4} -> {e.Message}")
                                          Console.WriteLine()
                                          waiter.Set()
                                      End If
                                  End Sub

            cpu.Registers.CS = &HA000
            cpu.Registers.IP = 0
            cpu.LoadBIN(f.FullName, cpu.Registers.CS, cpu.Registers.IP)
            cpu.Run(False)

            waiter.WaitOne()
            cpu.Close()

            Thread.Sleep(1000)
        Next

#If DEBUG Then
        Console.WriteLine("Press any key to exit")
        Console.ReadKey()
#End If
    End Sub
End Module

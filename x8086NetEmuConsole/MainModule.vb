Imports x8086NetEmu

Module MainModule
    Private cpu As X8086

    Sub Main()
        X8086.LogToConsole = False
        cpu = New X8086(True, True)

        cpu.Adapters.Add(New FloppyControllerAdapter(cpu))
        cpu.Adapters.Add(New CGAConsole(cpu))
        cpu.Adapters.Add(New KeyboardAdapter(cpu))
        'cpu.Adapters.Add(New MouseAdapter(cpu)) ' Not Compatible with MINIX

#If Win32 Then
        cpu.Adapters.Add(New SpeakerAdpater(cpu))
#End If

        LoadSettings()

        cpu.Run()
    End Sub

    Private Sub LoadSettings()
        If IO.File.Exists("settings.dat") Then
            Dim xml = XDocument.Load("settings.dat")

            ParseSettings(xml.<settings>(0))
        End If
    End Sub

    Private Sub ParseSettings(xml As XElement)
        cpu.SimulationMultiplier = Double.Parse(xml.<simulationMultiplier>.Value)

        cpu.Clock = Double.Parse(xml.<clockSpeed>.Value)

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
    End Sub
End Module

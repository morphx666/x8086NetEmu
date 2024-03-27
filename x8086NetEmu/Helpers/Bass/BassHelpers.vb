Imports System.IO
Imports System.Runtime.InteropServices
Imports ManagedBass

Public Class BassHelpers
    Public Shared Function Setup() As Boolean
        Dim result As Boolean = False
        Dim platform As String = x8086NetEmu.Runtime.Platform.ToString().ToLower()
        Dim architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower()

        If platform.StartsWith("arm") Then
            architecture = If(platform.EndsWith("hard"), "hardfp", "softfp")
            platform = "arm"
        End If

        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine("Bass", platform, architecture))

        If x8086NetEmu.Runtime.Platform = x8086NetEmu.Runtime.Platforms.MacOSX Then
            ' CHECK: This is probably wrong
            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            path = IO.Path.GetFullPath(IO.Path.Combine(path, "../MacOS/Bass", platform, architecture))
        End If

        Dim bassLib As FileInfo = Nothing
        Try
            bassLib = New DirectoryInfo(path).GetFiles()(0)
            If bassLib.Exists Then
                Dim fileName As String = IO.Path.GetFullPath(IO.Path.Combine(bassLib.Name))

                If x8086NetEmu.Runtime.Platform = x8086NetEmu.Runtime.Platforms.MacOSX Then
                    ' CHECK: This is probably wrong
                    path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    fileName = IO.Path.GetFullPath(IO.Path.Combine(path, bassLib.Name))
                End If

                If Not File.Exists(fileName) Then File.Copy(bassLib.FullName, fileName, False)
                result = True
            Else
                X8086.Notify($"The BASS library needed for this platform was not found at:{Environment.NewLine}{bassLib.FullName}", X8086.NotificationReasons.Warn)
            End If
        Catch ex As Exception
            X8086.Notify($"Exception copying required BASS library:{Environment.NewLine}{ex}", X8086.NotificationReasons.Err)
        End Try

        X8086.Notify($"Bass Library: {If(bassLib Is Nothing, "Unknown", bassLib.Name)} ({platform} {architecture})", X8086.NotificationReasons.Info)

        If result Then
            X8086.Notify("Initializing Bass", X8086.NotificationReasons.Info)

            Try
                result = Bass.Init(-1, SpeakerAdapter.SampleRate, DeviceInitFlags.Default)
            Catch ex As DllNotFoundException
                Application.Restart()
            End Try
        End If

        Return result
    End Function
End Class
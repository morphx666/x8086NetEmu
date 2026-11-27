Imports System.IO
Imports System.Runtime.InteropServices
Imports ManagedBass

Public Class BassHelpers
    Public Shared Function Setup() As Boolean
        Dim result As Boolean = False
        Dim platform As String = HostRuntime.Platform.ToString().ToLower()
        Dim architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower()

        If platform.StartsWith("arm") Then
            architecture = If(platform.EndsWith("hard"), "hardfp", "softfp")
            platform = "arm"
        End If

        ' Resolve against the executable's folder (not the process working directory, which differs
        ' between Visual Studio, dotnet run, shortcuts, etc.). The native library is copied next to
        ' the executable, which is where the native loader looks for it.
        Dim appPath As String = AppContext.BaseDirectory
        Dim path As String = IO.Path.GetFullPath(IO.Path.Combine(appPath, "Bass", platform, architecture))

        If Not Directory.Exists(path) Then
            X8086.Notify($"Sound disabled: there is no BASS library for {platform} {architecture} (expected in {path})", X8086.NotificationReasons.Warn)
            Return False
        End If

        Dim bassLib As FileInfo = Nothing
        Try
            bassLib = New DirectoryInfo(path).GetFiles()(0)
            If bassLib.Exists Then
                Dim fileName As String = IO.Path.GetFullPath(IO.Path.Combine(appPath, bassLib.Name))
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
                ' BASS stays initialized for the whole process, so a rebuilt machine (hard reset) gets "already initialized"
                result = Bass.Init(-1, SpeakerAdapter.SampleRate, DeviceInitFlags.Default) OrElse Bass.LastError = Errors.Already
                If Not result Then X8086.Notify($"Sound disabled: BASS initialization failed ({Bass.LastError})", X8086.NotificationReasons.Warn)
            Catch ex As Exception When TypeOf ex Is DllNotFoundException OrElse TypeOf ex Is BadImageFormatException OrElse TypeOf ex Is EntryPointNotFoundException
                ' The library exists but can't be loaded (e.g. built for another CPU architecture)
                X8086.Notify($"Sound disabled: unable to load the BASS library ({ex.Message})", X8086.NotificationReasons.Warn)
                result = False
            End Try
        End If

        Return result
    End Function
End Class
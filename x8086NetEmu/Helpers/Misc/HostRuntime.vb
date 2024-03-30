Imports System.IO

Public Class HostRuntime
    Public Enum Platforms
        Windows
        Linux
        MacOSX
        ARMSoft
        ARMHard
        Unknown
    End Enum

    Private Shared mPlatform As Platforms?

    Public Shared ReadOnly Property Platform As Platforms
        Get
            If mPlatform Is Nothing Then DetectPlatform()
            Return If(mPlatform, Platforms.Unknown)
        End Get
    End Property

    Private Shared Sub DetectPlatform()
        Select Case Environment.OSVersion.Platform
            Case PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE, PlatformID.Xbox
                mPlatform = Platforms.Windows
            Case PlatformID.MacOSX
                mPlatform = Platforms.MacOSX
            Case Else

                If Directory.Exists("/Applications") AndAlso Directory.Exists("/System") AndAlso Directory.Exists("/Users") AndAlso Directory.Exists("/Volumes") Then
                    mPlatform = Platforms.MacOSX
                Else
                    mPlatform = Platforms.Linux
                    Dim distro As String = GetLinuxDistro().ToLower()
                    If distro.Contains("raspberrypi") Then mPlatform = If(distro.Contains("armv7l"), Platforms.ARMHard, Platforms.ARMSoft)
                End If
        End Select
    End Sub

    Private Shared Function GetLinuxDistro() As String
        Dim lines As List(Of String) = New List(Of String)()
        Dim si As ProcessStartInfo = New ProcessStartInfo() With {
            .FileName = "uname",
            .Arguments = "-a",
            .CreateNoWindow = True,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .RedirectStandardInput = False
        }
        Dim catProcess As Process = New Process With {
            .StartInfo = si
        }
        AddHandler catProcess.OutputDataReceived, Sub(ByVal s As Object, ByVal e As DataReceivedEventArgs) lines.Add(e.Data)

        Try
            catProcess.Start()
            catProcess.BeginOutputReadLine()
            catProcess.WaitForExit()
            catProcess.Dispose()
            Threading.Thread.Sleep(500)
            Return If(lines.Count > 0, lines(0), "Unknown")
        Catch
            Return Environment.OSVersion.Platform.ToString()
        End Try
    End Function
End Class
Imports System.IO
Imports System.Runtime.InteropServices

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
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            mPlatform = Platforms.Windows
        ElseIf RuntimeInformation.IsOSPlatform(OSPlatform.OSX) Then
            mPlatform = Platforms.MacOSX
        ElseIf RuntimeInformation.IsOSPlatform(OSPlatform.Linux) Then
            mPlatform = Platforms.Linux
            Dim distro As String = GetLinuxDistro().ToLowerInvariant()
            If distro.Contains("raspberrypi") Then mPlatform = If(distro.Contains("armv7l"), Platforms.ARMHard, Platforms.ARMSoft)
        ElseIf Directory.Exists("/Applications") AndAlso Directory.Exists("/System") AndAlso Directory.Exists("/Users") AndAlso Directory.Exists("/Volumes") Then
            mPlatform = Platforms.MacOSX
        Else
            mPlatform = Platforms.Unknown
        End If
    End Sub

    Private Shared Function GetLinuxDistro() As String
        Dim lines As List(Of String) = New List(Of String)()
        Dim si As New Global.System.Diagnostics.ProcessStartInfo() With {
            .FileName = "uname",
            .Arguments = "-a",
            .CreateNoWindow = True,
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .RedirectStandardInput = False
        }
        Dim catProcess As New Global.System.Diagnostics.Process With {
            .StartInfo = si
        }
        AddHandler catProcess.OutputDataReceived, Sub(ByVal s As Object, ByVal e As Global.System.Diagnostics.DataReceivedEventArgs) lines.Add(e.Data)

        Try
            catProcess.Start()
            catProcess.BeginOutputReadLine()
            catProcess.WaitForExit()
            catProcess.Dispose()
            Threading.Thread.Sleep(500)
            Return If(lines.Count > 0, lines(0), "Unknown")
        Catch
            Return RuntimeInformation.OSDescription
        End Try
    End Function
End Class
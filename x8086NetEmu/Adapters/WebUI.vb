Imports System.Net
Imports System.Threading
Imports System.Web

Public Class WebUI
    Private client As Sockets.TcpListener

    Private mBitmap As DirectBitmap
    Private cpu As X8086
    Private ReadOnly syncObj As Object

    Private lastKeyDown As Keys
    Private lastKeyDownTime As Long

    Private lastKeyUp As Keys
    Private lastKeyUpTime As Long

    Public Sub New(cpu As X8086, dBmp As DirectBitmap, syncObj As Object)
        Me.cpu = cpu
        Me.mBitmap = dBmp
        Me.syncObj = syncObj

        CreateClient()

        Tasks.Task.Run(AddressOf ListenerSub)
    End Sub

    Public Property Bitmap As DirectBitmap
        Get
            Return mBitmap
        End Get
        Set(value As DirectBitmap)
            SyncLock syncObj
                mBitmap = value
            End SyncLock
        End Set
    End Property

    Private Sub ListenerSub()
        Do
            Try
                If client?.Pending Then
                    Using tcp As Sockets.TcpClient = client.AcceptTcpClient()
                        Using netStream As Sockets.NetworkStream = tcp.GetStream()
                            Dim buffer(8192 - 1) As Byte
                            Dim data As New List(Of Byte)

                            Do
                                Dim len As Integer = netStream.Read(buffer, 0, buffer.Length)
                                If len > 0 Then data.AddRange(buffer)
                                If len < buffer.Length Then Exit Do
                            Loop

                            ' See '\Projects\SDFWebCuadre\SDFWebCuadre\ModuleMain.vb' for information
                            ' on how to handle binary data, such as images

                            Dim rcvData As String = Text.Encoding.UTF8.GetString(data.ToArray())
                            Dim sndData As Byte() = Nothing
                            Dim resource As String = GetResource(rcvData)
                            Dim cntType As String = "text/html; text/html; charset=UTF-8"
                            Dim params As String = ""
                            If resource.Contains("?") Then
                                params = HttpUtility.UrlDecode(resource.Split("?")(1))
                                resource = resource.Split("?")(0)
                            End If

                            Select Case resource
                                Case "/" : sndData = Text.UTF8Encoding.UTF8.GetBytes(GetUI())
                                Case "/frame" : sndData = GetFrame() : cntType = "image/png"
                                Case "/keyDown"
                                    Dim k As Keys = CType(params.Split("=")(1), Keys)
                                    If k = lastKeyDown AndAlso Now.Ticks - lastKeyDownTime < 3000000 Then Exit Select
                                    lastKeyDown = k
                                    lastKeyDownTime = Now.Ticks
                                    cpu.PPI.PutKeyData(lastKeyDown, False)
                                Case "/keyUp"
                                    Dim k As Keys = CType(params.Split("=")(1), Keys)
                                    If k = lastKeyUp AndAlso Now.Ticks - lastKeyUpTime < 3000000 Then Exit Select
                                    lastKeyUp = k
                                    lastKeyUpTime = Now.Ticks
                                    cpu.PPI.PutKeyData(lastKeyUp, True)
                            End Select

                            If sndData?.Length > 0 Then
                                Dim sb As New Text.StringBuilder()
                                sb.Append("HTTP/1.0 200 OK" + ControlChars.CrLf)
                                sb.Append($"Content-Type: {cntType}{ControlChars.CrLf}")
                                sb.Append($"Content-Length: {sndData.Length}{ControlChars.CrLf}")
                                sb.Append(ControlChars.CrLf)

                                Dim b() As Byte = Text.Encoding.UTF8.GetBytes(sb.ToString())
                                ReDim Preserve b(b.Length + sndData.Length - 1)
                                Array.Copy(sndData, 0, b, sb.ToString().Length, sndData.Length)

                                netStream.Write(b, 0, b.Length)
                            End If

                            netStream.Close()
                        End Using

                        tcp.Close()
                    End Using
                Else
                    Thread.Sleep(100)
                End If
            Catch ex As Exception
                Exit Do
            End Try
        Loop
    End Sub

    Private Function GetUI() As String
        ' FIXME: The zoom compensation is not correctly implemented
        Return $"<!DOCTYPE html>
                <html lang=""en"">
                    <head>
                    <title>x8086NetEmu WebUI</title>
                    <script type=""text/JavaScript"">
                        var host = ""http://""+window.location.hostname+"":8086"";
                        var canvas;
                        var context;
                        var xmlHttp = new XMLHttpRequest();
                        var img = new Image();
                        var lastWidth = 0;
                        var lastHeight = 0;

                        function init() {{
                            canvas = document.getElementById(""x8086"");
                            context = canvas.getContext(""2d"");
                            setInterval(updateFrame, 60);

                            document.onkeydown = function(e) {{
                                e = e || window.event;
                                xmlHttp.open(""GET"", host + ""/keyDown?key="" + e.keyCode, true);
                                xmlHttp.send(null);
                                e.preventDefault();
                            }};

                            document.onkeyup = function(e) {{
                                e = e || window.event;
                                xmlHttp.open(""GET"", host + ""/keyUp?key="" + e.keyCode, true);
                                xmlHttp.send(null);
                                e.preventDefault();
                            }};

                            img.onload = function() {{
                                if((canvas.width != img.width) || (canvas.height = img.height)) {{
                                    canvas.width = img.width * {cpu.VideoAdapter.Zoom / 2};
                                    canvas.height = img.height * {cpu.VideoAdapter.Zoom / 2};
                                    lastWidth = img.width;
                                    lastHeight = img.height;
                                }}
                                context.imageSmoothingEnabled = false;
                                context.drawImage(img, 0, 0, canvas.width, canvas.height);
                            }};
                        }}

                        function updateFrame() {{
                            img.src = host + ""/frame"" + ""?d="" + Date.now();
                        }}
                    </script>

                    <title>x8086 WebUI</title>
                    </head>
                    <body onload=""init()"">
                        <canvas tabindex=""1"" id=""x8086"" width=""640"" height=""480""/>
                    </body>
                </html>"
    End Function

    Private Function GetFrame() As Byte()
        Try
            SyncLock syncObj
                Return CType(Bitmap, Byte())
            End SyncLock
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub CreateClient()
        Close()

        client = New Sockets.TcpListener(IPAddress.Any, 8086)
        client.Start()
    End Sub

    Public Sub Close()
        If client IsNot Nothing Then
            Try
                client.Stop()
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Function GetParams(params As String) As Dictionary(Of String, String)
        Dim data As New Dictionary(Of String, String)

        Dim tokens() As String = params.ToLower().Split("&"c)
        For Each token In tokens
            If token.Contains("="c) Then
                Dim subTokens() As String = token.Split("="c)
                data.Add(subTokens(0), subTokens(1))
            Else
                data.Add(token, "")
            End If
        Next
        Return data
    End Function

    Private Function GetResource(data As String) As String
        Return If(data.StartsWith("GET /"), data.Split(" ")(1), "404")
    End Function
End Class

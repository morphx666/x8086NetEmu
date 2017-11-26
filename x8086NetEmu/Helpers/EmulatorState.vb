Imports System.IO

Public Class EmulatorState
    Private mCPU As X8086

    Public Sub New(cpu As X8086)
        mCPU = cpu
    End Sub

    Public Sub SaveSettings(fileName As String, Optional extras As XElement = Nothing)
        Dim doc As New XDocument(GetSettings())

        If extras IsNot Nothing Then doc.<settings>(0).Add(extras)

        doc.Save(fileName)
    End Sub

    Public Sub SaveState(fileName As String)
        Dim doc As New XDocument()

        doc.Add(<state>
                    <%= GetSettings() %>
                    <flags><%= mCPU.Flags.EFlags %></flags>
                    <%= GetRegisters() %>
                    <%= GetMemory() %>
                    <videoMode><%= If(mCPU.VideoAdapter IsNot Nothing, mCPU.VideoAdapter.VideoMode, "Mode3_Text_Color_80x25") %></videoMode>
                    <debugMode><%= mCPU.DebugMode %></debugMode>
                </state>)

        doc.Save(fileName)
    End Sub

    Private Function GetSettings() As XElement
        Return <settings>
                   <simulationMultiplier><%= mCPU.SimulationMultiplier %></simulationMultiplier>
                   <clockSpeed><%= mCPU.Clock %></clockSpeed>
                   <videoZoom><%= mCPU.VideoAdapter.Zoom %></videoZoom>
                   <%= GetFloppyImages() %>
                   <%= GetDiskImages() %>
               </settings>
    End Function

    Private Function GetFloppyImages() As XElement
        Dim curPath = My.Application.Info.DirectoryPath + "\"
        Dim xml = <floppies></floppies>

        If mCPU.FloppyContoller IsNot Nothing Then
            For i As Integer = 0 To 128 - 1
                If mCPU.FloppyContoller.DiskImage(i) IsNot Nothing Then
                    Dim di = mCPU.FloppyContoller.DiskImage(i)

                    If Not di.IsHardDisk Then
                        xml.Add(<floppy>
                                    <letter><%= Chr(65 + i) %></letter>
                                    <index><%= i %></index>
                                    <image><%= di.FileName.Replace(curPath, "") %></image>
                                    <readOnly><%= di.IsReadOnly.ToString() %></readOnly>
                                </floppy>)
                    End If
                End If
            Next
        End If

        Return xml
    End Function

    Private Function GetDiskImages() As XElement
        Dim curPath = My.Application.Info.DirectoryPath + "\"
        Dim xml = <disks></disks>

        For i As Integer = 128 To 1000 - 1
            If mCPU.FloppyContoller.DiskImage(i) IsNot Nothing Then
                Dim di = mCPU.FloppyContoller.DiskImage(i)

                If di.IsHardDisk Then
                    xml.Add(<disk>
                                <letter><%= Chr(67 + (i - 128)) %></letter>
                                <index><%= i %></index>
                                <image><%= di.FileName.Replace(curPath, "") %></image>
                                <readOnly><%= di.IsReadOnly.ToString() %></readOnly>
                            </disk>)
                End If
            End If
        Next

        Return xml
    End Function

    Private Function GetRegisters() As XElement
        Return <registers>
                   <AX><%= mCPU.Registers.AX %></AX>
                   <BX><%= mCPU.Registers.BX %></BX>
                   <CX><%= mCPU.Registers.CX %></CX>
                   <DX><%= mCPU.Registers.DX %></DX>
                   <CS><%= mCPU.Registers.CS %></CS>
                   <IP><%= mCPU.Registers.IP %></IP>
                   <SS><%= mCPU.Registers.SS %></SS>
                   <SP><%= mCPU.Registers.SP %></SP>
                   <DS><%= mCPU.Registers.DS %></DS>
                   <SI><%= mCPU.Registers.SI %></SI>
                   <ES><%= mCPU.Registers.ES %></ES>
                   <DI><%= mCPU.Registers.DI %></DI>
                   <BP><%= mCPU.Registers.BP %></BP>
                   <AS><%= mCPU.Registers.ActiveSegmentRegister %></AS>
               </registers>
    End Function

    Private Function GetMemory() As XElement
        Return <memory><%= Convert.ToBase64String(mCPU.Memory) %></memory>
    End Function
End Class

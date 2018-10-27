' This code is flugly as hell... but it works.
' It parses the huge "Select Case" in x8086 that processes opcodes (Execute) and converts it into an array of functions,
' which results in a +2x performance improvement

Module ModuleMain
    Sub Main()
        Dim abortMsg As String = "This tool can only be run in DEBUG mode while inside the IDE"
#If DEBUG Then
        If Debugger.IsAttached Then
            Console.WriteLine("Are you sure you want to parse the opcodes' emulation code? [y/N]")
            If Console.ReadKey(True).Key = ConsoleKey.Y Then
                RunParser()
            Else
                Console.WriteLine("Process aborted...")
            End If
        Else
            Console.WriteLine(abortMsg)
        End If
#Else
        Console.WriteLine(abortMsg)
#End If

        Console.ReadKey()
    End Sub

    Private Sub RunParser()
        Dim src As String = IO.File.ReadAllText("..\x8086.vb")
        Dim trg As String = "Partial Public Class X8086
                                Private Delegate Sub ExecOpcode()
                                Private opCodes() As ExecOpcode = {
                                    %1}

                                %2
                            End Class"

        Dim needle As String = "Select Case opCode"
        Dim p1 As Integer = src.IndexOf(needle) + needle.Length
        Dim p2 As Integer
        Dim p3 As Integer
        Dim tmp As String = ""
        Dim tokens() As String = Nothing
        Dim subSkel As String = "Private Sub %1
                                    %2
                                 End Sub"
        Dim subName As String = ""
        Dim subBody As String = ""
        Dim subCalls(&HFF) As String
        Dim startIndex As Integer
        Dim endIndex As Integer

        Dim comment As String = ""

        src = src.Replace(" : ", "' " + vbCrLf)
        Dim eof As Integer = src.IndexOf("If useIPAddrOffset Then", p1)
        needle = "Case &H"

        Dim addSubCall = Sub(addComment As Boolean, v As Integer)
                             subCalls(v) = $"AddressOf {subName},"
                             If addComment AndAlso src.Substring(p2, p3 - p2).Trim() <> "'" Then
                                 comment = $"{vbTab}{src.Substring(p2, p3 - p2)}"
                                 subCalls(v) += comment
                             End If
                         End Sub

        Dim addSubCalls = Sub(fName As String, range As Boolean)
                              Dim subTokens() As String = fName.Split("_"c)

                              If range Then
                                  startIndex = Integer.Parse(subTokens(1).Replace("&H", ""), Globalization.NumberStyles.HexNumber)
                                  endIndex = Integer.Parse(subTokens(2).Replace("&H", ""), Globalization.NumberStyles.HexNumber)

                                  For i As Integer = startIndex To endIndex
                                      addSubCall(False, i)
                                  Next
                              Else
                                  For i As Integer = 1 To subTokens.Length - 1
                                      addSubCall(True, Integer.Parse(subTokens(i).Replace("&H", ""), Globalization.NumberStyles.HexNumber))
                                  Next
                              End If
                          End Sub

        Dim addSubDef = Sub()
                            p2 = src.IndexOf("Case &H", p3)
                            If p2 > eof Then p2 = src.IndexOf("Case Else", p3) - Len("Case Else")
                            tmp = src.Substring(p3, p2 - p3).Trim().Replace("Exit Select", "Exit Sub")
                            subBody += subSkel.Replace("%1", subName + comment).Replace("%2", tmp) + vbCrLf + vbCrLf
                        End Sub

        Dim parseCase = Sub()
                            startIndex = Integer.Parse(tokens(0).Replace("&H", ""), Globalization.NumberStyles.HexNumber)
                            subName += $"_{tokens(0).Replace("&H", "").PadLeft(2, "0")}"
                        End Sub

        Dim parseCaseTo = Sub()
                              startIndex = Integer.Parse(tokens(0).Replace("&H", ""), Globalization.NumberStyles.HexNumber)
                              endIndex = Integer.Parse(tokens(2).Replace("&H", ""), Globalization.NumberStyles.HexNumber)
                              subName = $"_{tokens(0).Replace("&H", "").PadLeft(2, "0")}_{tokens(2).Replace("&H", "").PadLeft(2, "0")}"
                          End Sub

        Dim parseCaseComma = Sub()
                                 Dim subTokens() As String
                                 Dim fName As String = ""

                                 subTokens = tmp.Split(","c)
                                 ReDim tokens(0)
                                 For i As Integer = 0 To subTokens.Length - 1
                                     If subTokens(i).Contains("To") Then
                                         tokens = subTokens(i).Trim().Split(" "c)
                                         If tokens.Length = 4 Then Stop
                                         parseCaseTo()
                                         addSubCalls(subName, True)
                                         addSubDef()
                                     Else
                                         tokens(0) = subTokens(i).Trim()
                                         parseCase()
                                         fName += subName
                                     End If
                                 Next

                                 If fName <> "" Then
                                     addSubCalls(fName, False)
                                     addSubDef()
                                 End If
                             End Sub

        Do
            comment = ""
            subName = ""

            p1 = src.IndexOf(needle, p1) + needle.Length
            p2 = src.IndexOf("'", p1)
            p3 = src.IndexOf(vbCr, p2)

            If p2 > src.IndexOf(vbCr, p1) Then
                p3 = src.IndexOf(vbCr, p1)
                p2 = p3
            End If

            tmp = src.Substring(p1, p2 - p1).Trim()
            tokens = tmp.Split(" ")

            If tokens.Length = 1 Then
                parseCase()
                addSubCall(True, startIndex)
                addSubDef()
            Else
                If tmp.Contains(",") Then
                    parseCaseComma()
                Else
                    parseCaseTo()
                    For i As Integer = startIndex To endIndex
                        addSubCall(i = startIndex, i)
                    Next
                    addSubDef()
                End If
            End If

            p2 = src.IndexOf("Case &H", p3)
            If p2 > eof Then
                For i As Integer = 0 To subCalls.Count - 1
                    If subCalls(i) = "" Then subCalls(i) = "AddressOf OpCodeNotImplemented,"
                Next
                tmp = Join(subCalls.ToArray(), vbCrLf)
                tmp = tmp.Substring(0, tmp.Length - 1)
                trg = trg.Replace("%1", tmp).Replace("%2", subBody)
                IO.File.WriteAllText("..\Helpers\OpCodes.vb", trg)
                Exit Do
            End If

            p1 = p2
        Loop
    End Sub
End Module

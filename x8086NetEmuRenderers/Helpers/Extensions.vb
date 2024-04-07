Imports System.Runtime.CompilerServices
Imports x8086NetEmu.Adapter

Module Extensions

    <Extension()>
    Public Function ToPoint(v As XPoint) As Point
        Return New Point(v.X, v.Y)
    End Function

    <Extension()>
    Public Function ToSize(v As XSize) As Size
        Return New Size(v.Width, v.Height)
    End Function

    <Extension()>
    Public Function ToRectangle(v As XRectangle) As Rectangle
        Return New Rectangle(v.X, v.Y, v.Width, v.Height)
    End Function

    <Extension()>
    Public Function ToColor(v As XColor) As Color
        'Return Color.FromArgb(v.ToArgb())
        Return Color.FromArgb(v.A, v.R, v.G, v.B)
    End Function
End Module

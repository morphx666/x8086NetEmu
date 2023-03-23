Public Interface IIOPortHandler
    ReadOnly Property ValidPortAddress As List(Of UInt32)

    Sub Out(port As UInt16, value As Byte)
    Function [In](port As UInt16) As Byte
    ReadOnly Property Name() As String
    ReadOnly Property Description() As String
    Sub Run()
End Interface

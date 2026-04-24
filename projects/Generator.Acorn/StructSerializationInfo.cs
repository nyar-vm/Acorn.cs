using System.Collections.Generic;

namespace Generator.Acorn;

internal sealed class StructSerializationInfo
{
    public string Name { get; set; } = "";
    public string? Namespace { get; set; }
    public Endianness Endianness { get; set; }
    public List<FieldSerializationInfo> Fields { get; set; } = new List<FieldSerializationInfo>();
    public bool HasBitFields { get; set; }
    public List<OffsetTableInfo> OffsetTables { get; set; } = new List<OffsetTableInfo>();
    public AlgebraicUnionInfo? AlgebraicUnion { get; set; }
}
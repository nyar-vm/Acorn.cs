namespace Generator.Acorn;

internal sealed class FieldSerializationInfo
{
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string DisplayTypeName { get; set; } = "";
    public int Order { get; set; }
    public int Length { get; set; } = -1;
    public string LengthField { get; set; } = "";
    public string ConditionalOn { get; set; } = "";
    public Endianness? Endianness { get; set; }
    public string Encoding { get; set; } = "utf-8";
    public bool IsBitField { get; set; }
    public BitFieldInfo? BitField { get; set; }
    public FieldTypeKind TypeKind { get; set; }
    public bool IsArray { get; set; }
    public string? ElementTypeName { get; set; }
    public bool Optional { get; set; }
    public string? CodecTypeName { get; set; }
}
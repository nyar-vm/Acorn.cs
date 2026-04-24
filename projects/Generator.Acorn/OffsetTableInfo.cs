namespace Generator.Acorn;

internal sealed class OffsetTableInfo
{
    public string FieldName { get; set; } = "";
    public string OffsetField { get; set; } = "";
    public string TargetTypeName { get; set; } = "";
    public string RelativeTo { get; set; } = "start";
}
using System.Collections.Generic;

namespace Generator.Acorn;

internal sealed class AlgebraicUnionInfo
{
    public string DiscriminatorField { get; set; } = "";
    public List<AlgebraicUnionCase> Cases { get; set; } = new();
}
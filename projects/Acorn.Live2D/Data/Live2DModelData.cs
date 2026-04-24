namespace Acorn.Live2D.Data;

/// <summary>
///     Live2D Cubism 模型数据，包含从 moc3 二进制文件解码的完整模型信息。
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组。
///     此数据模型将 SoA 格式重组为面向对象的 AOS 结构，便于上层使用。
/// </remarks>
public sealed class Live2DModelData
{
    /// <summary>
    ///     模型版本（3, 4, 5）。
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    ///     是否为大端序。
    /// </summary>
    public bool IsBigEndian { get; init; }

    /// <summary>
    ///     版本修订号。
    /// </summary>
    public int Revision { get; init; }

    /// <summary>
    ///     模型文件路径。
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    ///     画布信息。
    /// </summary>
    public Live2DCanvasInfo Canvas { get; init; } = new();

    /// <summary>
    ///     参数列表。
    /// </summary>
    public IReadOnlyList<Live2DParameter> Parameters { get; init; } = [];

    /// <summary>
    ///     部件列表。
    /// </summary>
    public IReadOnlyList<Live2DPart> Parts { get; init; } = [];

    /// <summary>
    ///     绘制对象（ArtMesh）列表。
    /// </summary>
    public IReadOnlyList<Live2DDrawable> Drawables { get; init; } = [];

    /// <summary>
    ///     变形器列表。
    /// </summary>
    public IReadOnlyList<Live2DDeformer> Deformers { get; init; } = [];

    /// <summary>
    ///     纹理数量。
    /// </summary>
    public int TextureCount { get; init; }
}

/// <summary>
///     Live2D 画布信息，对应 moc3 段偏移表中 CanvasInfo 段。
/// </summary>
public sealed class Live2DCanvasInfo
{
    /// <summary>
    ///     画布宽度。
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     画布高度。
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    ///     画布中心 X 坐标。
    /// </summary>
    public float CenterX { get; init; }

    /// <summary>
    ///     画布中心 Y 坐标。
    /// </summary>
    public float CenterY { get; init; }

    /// <summary>
    ///     像素密度。
    /// </summary>
    public float PixelsPerUnit { get; init; } = 1.0f;
}

/// <summary>
///     Live2D 参数数据，由 moc3 段偏移表中 ParameterIds/ParameterMinimumValues/ParameterMaximumValues/ParameterDefaultValues 段重组而来。
/// </summary>
public sealed class Live2DParameter
{
    /// <summary>
    ///     参数标识符。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     最小值。
    /// </summary>
    public float MinValue { get; init; }

    /// <summary>
    ///     最大值。
    /// </summary>
    public float MaxValue { get; init; }

    /// <summary>
    ///     默认值。
    /// </summary>
    public float DefaultValue { get; init; }
}

/// <summary>
///     Live2D 部件数据，由 moc3 段偏移表中 PartIds/PartParentPartIndices 段重组而来。
/// </summary>
public sealed class Live2DPart
{
    /// <summary>
    ///     部件标识符。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     父部件索引（-1 表示无父级）。
    /// </summary>
    public int ParentIndex { get; init; } = -1;
}

/// <summary>
///     Live2D 绘制对象（ArtMesh）数据，由 moc3 段偏移表中 DrawableIds/DrawableConstantFlags/DrawableTextureIndices 等段重组而来。
/// </summary>
public sealed class Live2DDrawable
{
    /// <summary>
    ///     绘制对象标识符。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     纹理索引。
    /// </summary>
    public int TextureIndex { get; init; } = -1;

    /// <summary>
    ///     绘制顺序。
    /// </summary>
    public int DrawOrder { get; init; }

    /// <summary>
    ///     渲染顺序。
    /// </summary>
    public int RenderOrder { get; init; }

    /// <summary>
    ///     顶点位置列表（每两个 float 为一个顶点的 X、Y 坐标）。
    /// </summary>
    public IReadOnlyList<float> VertexPositions { get; init; } = [];

    /// <summary>
    ///     顶点 UV 列表（每两个 float 为一个 UV 的 U、V 坐标）。
    /// </summary>
    public IReadOnlyList<float> VertexUvs { get; init; } = [];

    /// <summary>
    ///     三角形索引列表。
    /// </summary>
    public IReadOnlyList<int> Indices { get; init; } = [];

    /// <summary>
    ///     顶点数量。
    /// </summary>
    public int VertexCount { get; init; }

    /// <summary>
    ///     是否翻转 UV 的 Y 轴。
    /// </summary>
    public bool FlipUvY { get; init; }

    /// <summary>
    ///     混合模式（0=Normal, 1=Additive, 2=Multiply）。
    /// </summary>
    public int BlendMode { get; init; }

    /// <summary>
    ///     不透明度。
    /// </summary>
    public float Opacity { get; init; } = 1.0f;

    /// <summary>
    ///     遮罩绘制对象索引列表。
    /// </summary>
    public IReadOnlyList<int> MaskDrawableIndices { get; init; } = [];
}

/// <summary>
///     Live2D 变形器数据，由 moc3 段偏移表中 DeformerIds/DeformerTypes/DeformerParentIndices 等段重组而来（v4+）。
/// </summary>
public sealed class Live2DDeformer
{
    /// <summary>
    ///     变形器标识符。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     变形器类型（0=Rotation, 1=Warp, 2=Combined）。
    /// </summary>
    public int Type { get; init; }

    /// <summary>
    ///     父变形器索引（-1 表示无父级）。
    /// </summary>
    public int ParentIndex { get; init; } = -1;

    /// <summary>
    ///     变形器包围盒 X。
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     变形器包围盒 Y。
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     变形器包围盒宽度。
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     变形器包围盒高度。
    /// </summary>
    public float Height { get; init; }
}

namespace Acorn.Live2D.Data;

/// <summary>
///     Live2D Cubism 二进制格式常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 Live2D Cubism SDK 规范，Acorn 独占二进制编解码职责。
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组，
///     段偏移表（Section Offset Table）记录各段在文件中的偏移量。
/// </remarks>
public static class Live2DConstants
{
    /// <summary>
    ///     moc3 文件魔数（"MOC3"）。
    /// </summary>
    public static ReadOnlySpan<byte> Moc3MagicNumber => new byte[] { 0x4D, 0x4F, 0x43, 0x33 };

    /// <summary>
    ///     moc3 文件头大小（字节）。
    /// </summary>
    /// <remarks>
    ///     头部结构：4 字节魔数 + 1 字节版本 + 1 字节标志 + 2 字节修订号 = 8 字节。
    /// </remarks>
    public const int HeaderSize = 8;

    /// <summary>
    ///     moc3 段偏移表条目大小（字节），每个条目为 i32。
    /// </summary>
    public const int OffsetTableEntrySize = 4;

    #region 段偏移表条目数

    /// <summary>
    ///     moc3 v3 段偏移表条目数。
    /// </summary>
    public const int V3OffsetTableCount = 22;

    /// <summary>
    ///     moc3 v4 段偏移表条目数。
    /// </summary>
    public const int V4OffsetTableCount = 25;

    /// <summary>
    ///     moc3 v5 段偏移表条目数。
    /// </summary>
    public const int V5OffsetTableCount = 28;

    #endregion

    #region 段偏移表索引

    /// <summary>
    ///     获取指定版本的段偏移表条目数。
    /// </summary>
    public static int GetOffsetTableCount(int version)
    {
        return version switch
        {
            >= 5 => V5OffsetTableCount,
            >= 4 => V4OffsetTableCount,
            >= 3 => V3OffsetTableCount,
            _ => V3OffsetTableCount
        };
    }

    /// <summary>
    ///     获取指定版本的段偏移表大小（字节）。
    /// </summary>
    public static int GetOffsetTableSize(int version)
    {
        return GetOffsetTableCount(version) * OffsetTableEntrySize;
    }

    /// <summary>
    ///     获取指定版本的 CountInfo 段在文件中的偏移量。
    /// </summary>
    public static int GetCountInfoOffset(int version)
    {
        return HeaderSize + GetOffsetTableSize(version);
    }

    #endregion

    #region CountInfo 字段偏移

    /// <summary>
    ///     CountInfo 段中参数数量字段的偏移（相对于 CountInfo 段起始位置）。
    /// </summary>
    public const int CountInfoParameterCountOffset = 0;

    /// <summary>
    ///     CountInfo 段中部件数量字段的偏移（相对于 CountInfo 段起始位置）。
    /// </summary>
    public const int CountInfoPartCountOffset = 4;

    /// <summary>
    ///     CountInfo 段中绘制对象数量字段的偏移（相对于 CountInfo 段起始位置）。
    /// </summary>
    public const int CountInfoDrawableCountOffset = 8;

    /// <summary>
    ///     CountInfo 段中变形器数量字段的偏移（相对于 CountInfo 段起始位置，v4+）。
    /// </summary>
    public const int CountInfoDeformerCountOffset = 12;

    /// <summary>
    ///     CountInfo 段中纹理数量字段的偏移（相对于 CountInfo 段起始位置）。
    /// </summary>
    public const int CountInfoTextureCountOffset = 16;

    #endregion
}

/// <summary>
///     moc3 段偏移表索引枚举，定义各段在偏移表中的位置。
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组。
///     段偏移表中的每个条目是一个 i32，表示该段数据在文件中的偏移量（相对于文件起始位置）。
///     偏移量为 0 表示该段不存在。
/// </remarks>
public enum Moc3Section
{
    /// <summary>
    ///     画布信息段（5 个 f32：宽度、高度、中心 X、中心 Y、像素密度）。
    /// </summary>
    CanvasInfo = 0,

    /// <summary>
    ///     计数信息段（各类型元素的数量）。
    /// </summary>
    CountInfo = 1,

    /// <summary>
    ///     参数 ID 段（null 终止字符串数组）。
    /// </summary>
    ParameterIds = 2,

    /// <summary>
    ///     参数最小值段（f32 数组）。
    /// </summary>
    ParameterMinimumValues = 3,

    /// <summary>
    ///     参数最大值段（f32 数组）。
    /// </summary>
    ParameterMaximumValues = 4,

    /// <summary>
    ///     参数默认值段（f32 数组）。
    /// </summary>
    ParameterDefaultValues = 5,

    /// <summary>
    ///     部件 ID 段（null 终止字符串数组）。
    /// </summary>
    PartIds = 6,

    /// <summary>
    ///     部件父部件索引段（i32 数组）。
    /// </summary>
    PartParentPartIndices = 7,

    /// <summary>
    ///     绘制对象 ID 段（null 终止字符串数组）。
    /// </summary>
    DrawableIds = 8,

    /// <summary>
    ///     绘制对象常量标志段（u8 数组）。
    /// </summary>
    DrawableConstantFlags = 9,

    /// <summary>
    ///     绘制对象纹理索引段（i32 数组）。
    /// </summary>
    DrawableTextureIndices = 10,

    /// <summary>
    ///     绘制对象绘制顺序段（i32 数组）。
    /// </summary>
    DrawableDrawOrders = 11,

    /// <summary>
    ///     绘制对象渲染顺序段（i32 数组）。
    /// </summary>
    DrawableRenderOrders = 12,

    /// <summary>
    ///     绘制对象遮罩数量段（i32 数组）。
    /// </summary>
    DrawableMaskCounts = 13,

    /// <summary>
    ///     绘制对象遮罩索引段（i32 二维数组）。
    /// </summary>
    DrawableMasks = 14,

    /// <summary>
    ///     绘制对象顶点数量段（i32 数组）。
    /// </summary>
    DrawableVertexCounts = 15,

    /// <summary>
    ///     绘制对象顶点位置段（f32 二维数组）。
    /// </summary>
    DrawableVertexPositions = 16,

    /// <summary>
    ///     绘制对象顶点 UV 段（f32 二维数组）。
    /// </summary>
    DrawableVertexUvs = 17,

    /// <summary>
    ///     绘制对象三角形索引段（i32 二维数组）。
    /// </summary>
    DrawableIndices = 18,

    /// <summary>
    ///     绘制对象重复标志段（u8 数组，v3.3+）。
    /// </summary>
    DrawableRepeatFlags = 19,

    /// <summary>
    ///     变形器 ID 段（null 终止字符串数组，v4+）。
    /// </summary>
    DeformerIds = 20,

    /// <summary>
    ///     变形器类型段（u8 数组，v4+）。
    /// </summary>
    DeformerTypes = 21,

    /// <summary>
    ///     变形器父索引段（i32 数组，v4+）。
    /// </summary>
    DeformerParentIndices = 22,

    /// <summary>
    ///     变形器包围盒 X 段（f32 数组，v4+）。
    /// </summary>
    DeformerBoundingBoxX = 23,

    /// <summary>
    ///     变形器包围盒 Y 段（f32 数组，v5+）。
    /// </summary>
    DeformerBoundingBoxY = 24,

    /// <summary>
    ///     变形器包围盒宽度段（f32 数组，v5+）。
    /// </summary>
    DeformerBoundingBoxWidth = 25,

    /// <summary>
    ///     变形器包围盒高度段（f32 数组，v5+）。
    /// </summary>
    DeformerBoundingBoxHeight = 26,

    /// <summary>
    ///     变形器旋转段（f32 数组，v5+）。
    /// </summary>
    DeformerRotation = 27
}

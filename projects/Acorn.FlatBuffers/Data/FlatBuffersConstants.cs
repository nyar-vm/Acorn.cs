namespace Acorn.FlatBuffers.Data;

/// <summary>
///     FlatBuffers 格式常量。
/// </summary>
public static class FlatBuffersConstants
{
    /// <summary>
    ///     FlatBuffer 文件标识符大小。
    /// </summary>
    public const int FileIdentifierSize = 4;

    /// <summary>
    ///     vtable 偏移字段大小。
    /// </summary>
    public const int VTableOffsetSize = 2;

    /// <summary>
    ///     vtable 大小字段偏移。
    /// </summary>
    public const int VTableSizeOffset = 0;

    /// <summary>
    ///     vtable 数据偏移字段偏移。
    /// </summary>
    public const int VTableDataOffset = 2;

    /// <summary>
    ///     vtable 字段偏移起始。
    /// </summary>
    public const int VTableFieldStart = 4;
}

/// <summary>
///     FlatBuffers 字段类型。
/// </summary>
public enum FlatBufferFieldType : byte
{
    /// <summary>
    ///     无效类型。
    /// </summary>
    None = 0,

    /// <summary>
    ///     字节。
    /// </summary>
    UByte = 1,

    /// <summary>
    ///     布尔值。
    /// </summary>
    Bool = 2,

    /// <summary>
    ///     字节（有符号）。
    /// </summary>
    Byte = 3,

    /// <summary>
    ///     短整数。
    /// </summary>
    Short = 4,

    /// <summary>
    ///     无符号短整数。
    /// </summary>
    UShort = 5,

    /// <summary>
    ///     整数。
    /// </summary>
    Int = 6,

    /// <summary>
    ///     无符号整数。
    /// </summary>
    UInt = 7,

    /// <summary>
    ///     长整数。
    /// </summary>
    Long = 8,

    /// <summary>
    ///     无符号长整数。
    /// </summary>
    ULong = 9,

    /// <summary>
    ///     浮点数。
    /// </summary>
    Float = 10,

    /// <summary>
    ///     双精度浮点数。
    /// </summary>
    Double = 11
}

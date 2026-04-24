namespace Acorn.MessagePack.Data;

/// <summary>
///     MessagePack 格式标记常量。
/// </summary>
public static class MsgPackConstants
{
    /// <summary>
    ///     正 fixint 最大值。
    /// </summary>
    public const byte PositiveFixIntMax = 0x7F;

    /// <summary>
    ///     fixmap 起始标记。
    /// </summary>
    public const byte FixMapMin = 0x80;

    /// <summary>
    ///     fixmap 结束标记。
    /// </summary>
    public const byte FixMapMax = 0x8F;

    /// <summary>
    ///     fixarray 起始标记。
    /// </summary>
    public const byte FixArrayMin = 0x90;

    /// <summary>
    ///     fixarray 结束标记。
    /// </summary>
    public const byte FixArrayMax = 0x9F;

    /// <summary>
    ///     fixstr 起始标记。
    /// </summary>
    public const byte FixStrMin = 0xA0;

    /// <summary>
    ///     fixstr 结束标记。
    /// </summary>
    public const byte FixStrMax = 0xBF;

    /// <summary>
    ///     nil 标记。
    /// </summary>
    public const byte Nil = 0xC0;

    /// <summary>
    ///     未使用标记。
    /// </summary>
    public const byte Unused = 0xC1;

    /// <summary>
    ///     false 标记。
    /// </summary>
    public const byte False = 0xC2;

    /// <summary>
    ///     true 标记。
    /// </summary>
    public const byte True = 0xC3;

    /// <summary>
    ///     bin8 标记。
    /// </summary>
    public const byte Bin8 = 0xC4;

    /// <summary>
    ///     bin16 标记。
    /// </summary>
    public const byte Bin16 = 0xC5;

    /// <summary>
    ///     bin32 标记。
    /// </summary>
    public const byte Bin32 = 0xC6;

    /// <summary>
    ///     ext8 标记。
    /// </summary>
    public const byte Ext8 = 0xC7;

    /// <summary>
    ///     ext16 标记。
    /// </summary>
    public const byte Ext16 = 0xC8;

    /// <summary>
    ///     ext32 标记。
    /// </summary>
    public const byte Ext32 = 0xC9;

    /// <summary>
    ///     float32 标记。
    /// </summary>
    public const byte Float32 = 0xCA;

    /// <summary>
    ///     float64 标记。
    /// </summary>
    public const byte Float64 = 0xCB;

    /// <summary>
    ///     uint8 标记。
    /// </summary>
    public const byte Uint8 = 0xCC;

    /// <summary>
    ///     uint16 标记。
    /// </summary>
    public const byte Uint16 = 0xCD;

    /// <summary>
    ///     uint32 标记。
    /// </summary>
    public const byte Uint32 = 0xCE;

    /// <summary>
    ///     uint64 标记。
    /// </summary>
    public const byte Uint64 = 0xCF;

    /// <summary>
    ///     int8 标记。
    /// </summary>
    public const byte Int8 = 0xD0;

    /// <summary>
    ///     int16 标记。
    /// </summary>
    public const byte Int16 = 0xD1;

    /// <summary>
    ///     int32 标记。
    /// </summary>
    public const byte Int32 = 0xD2;

    /// <summary>
    ///     int64 标记。
    /// </summary>
    public const byte Int64 = 0xD3;

    /// <summary>
    ///     fixext1 标记。
    /// </summary>
    public const byte FixExt1 = 0xD4;

    /// <summary>
    ///     fixext2 标记。
    /// </summary>
    public const byte FixExt2 = 0xD5;

    /// <summary>
    ///     fixext4 标记。
    /// </summary>
    public const byte FixExt4 = 0xD6;

    /// <summary>
    ///     fixext8 标记。
    /// </summary>
    public const byte FixExt8 = 0xD7;

    /// <summary>
    ///     fixext16 标记。
    /// </summary>
    public const byte FixExt16 = 0xD8;

    /// <summary>
    ///     str8 标记。
    /// </summary>
    public const byte Str8 = 0xD9;

    /// <summary>
    ///     str16 标记。
    /// </summary>
    public const byte Str16 = 0xDA;

    /// <summary>
    ///     str32 标记。
    /// </summary>
    public const byte Str32 = 0xDB;

    /// <summary>
    ///     array16 标记。
    /// </summary>
    public const byte Array16 = 0xDC;

    /// <summary>
    ///     array32 标记。
    /// </summary>
    public const byte Array32 = 0xDD;

    /// <summary>
    ///     map16 标记。
    /// </summary>
    public const byte Map16 = 0xDE;

    /// <summary>
    ///     map32 标记。
    /// </summary>
    public const byte Map32 = 0xDF;

    /// <summary>
    ///     负 fixint 最小值。
    /// </summary>
    public const byte NegativeFixIntMin = 0xE0;
}

/// <summary>
///     MessagePack 值类型。
/// </summary>
public enum MsgPackType : byte
{
    /// <summary>
    ///     整数。
    /// </summary>
    Integer,

    /// <summary>
    ///     无符号整数。
    /// </summary>
    UnsignedInteger,

    /// <summary>
    ///     浮点数。
    /// </summary>
    Float,

    /// <summary>
    ///     字符串。
    /// </summary>
    String,

    /// <summary>
    ///     二进制。
    /// </summary>
    Binary,

    /// <summary>
    ///     数组。
    /// </summary>
    Array,

    /// <summary>
    ///     映射。
    /// </summary>
    Map,

    /// <summary>
    ///     布尔值。
    /// </summary>
    Boolean,

    /// <summary>
    ///     空值。
    /// </summary>
    Nil,

    /// <summary>
    ///     扩展类型。
    /// </summary>
    Extension
}

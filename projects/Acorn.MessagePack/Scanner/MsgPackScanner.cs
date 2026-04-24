using Acorn.Frame;
using Acorn.MessagePack.Data;

namespace Acorn.MessagePack.Scanner;

/// <summary>
///     MessagePack 扫描器。
/// </summary>
public ref struct MsgPackScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="MsgPackScanner" /> 结构的新实例。
    /// </summary>
    public MsgPackScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     数据总长度。
    /// </summary>
    public int Length => _buffer.Length;

    /// <summary>
    ///     扫描 MessagePack 数据头。
    /// </summary>
    public MsgPackScanHeader ScanHeader()
    {
        if (_buffer.IsEnd)
        {
            return new MsgPackScanHeader { RootType = MsgPackType.Nil };
        }

        var b = _buffer.Peek();
        var type = ClassifyType(b);

        return new MsgPackScanHeader { RootType = type, FirstByte = b };
    }

    /// <summary>
    ///     快速判断数据是否可能为 MessagePack 格式。
    /// </summary>
    public bool IsPossibleMsgPack()
    {
        if (_buffer.IsEnd) return false;
        var b = _buffer.Peek();
        return b <= MsgPackConstants.PositiveFixIntMax
               || b >= MsgPackConstants.NegativeFixIntMin
               || b is MsgPackConstants.Nil or MsgPackConstants.False or MsgPackConstants.True
               || b >= MsgPackConstants.FixMapMin;
    }

    private static MsgPackType ClassifyType(byte b)
    {
        if (b <= MsgPackConstants.PositiveFixIntMax) return MsgPackType.Integer;
        if (b >= MsgPackConstants.NegativeFixIntMin) return MsgPackType.Integer;
        if (b >= MsgPackConstants.FixMapMin && b <= MsgPackConstants.FixMapMax) return MsgPackType.Map;
        if (b >= MsgPackConstants.FixArrayMin && b <= MsgPackConstants.FixArrayMax) return MsgPackType.Array;
        if (b >= MsgPackConstants.FixStrMin && b <= MsgPackConstants.FixStrMax) return MsgPackType.String;
        return b switch
        {
            MsgPackConstants.Nil => MsgPackType.Nil,
            MsgPackConstants.False or MsgPackConstants.True => MsgPackType.Boolean,
            MsgPackConstants.Float32 or MsgPackConstants.Float64 => MsgPackType.Float,
            MsgPackConstants.Uint8 or MsgPackConstants.Uint16 or MsgPackConstants.Uint32 or MsgPackConstants.Uint64 => MsgPackType.UnsignedInteger,
            MsgPackConstants.Int8 or MsgPackConstants.Int16 or MsgPackConstants.Int32 or MsgPackConstants.Int64 => MsgPackType.Integer,
            MsgPackConstants.Str8 or MsgPackConstants.Str16 or MsgPackConstants.Str32 => MsgPackType.String,
            MsgPackConstants.Bin8 or MsgPackConstants.Bin16 or MsgPackConstants.Bin32 => MsgPackType.Binary,
            MsgPackConstants.Array16 or MsgPackConstants.Array32 => MsgPackType.Array,
            MsgPackConstants.Map16 or MsgPackConstants.Map32 => MsgPackType.Map,
            _ => MsgPackType.Extension
        };
    }
}

/// <summary>
///     MessagePack 扫描头部信息。
/// </summary>
public sealed class MsgPackScanHeader
{
    /// <summary>
    ///     根值类型。
    /// </summary>
    public MsgPackType RootType { get; init; }

    /// <summary>
    ///     首字节。
    /// </summary>
    public byte FirstByte { get; init; }
}

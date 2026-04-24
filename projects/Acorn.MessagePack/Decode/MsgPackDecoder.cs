using Acorn.Frame;
using Acorn.MessagePack.Data;

namespace Acorn.MessagePack.Decode;

/// <summary>
///     MessagePack 二进制解码器。
/// </summary>
public ref struct MsgPackDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="MsgPackDecoder" /> 结构的新实例。
    /// </summary>
    public MsgPackDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 MessagePack 数据。
    /// </summary>
    public MsgPackData Decode()
    {
        var root = ReadValue();
        return new MsgPackData { Root = root };
    }

    /// <summary>
    ///     仅解码第一个值的类型。
    /// </summary>
    public MsgPackType PeekType()
    {
        if (_buffer.IsEnd) return MsgPackType.Nil;
        var b = _buffer.Peek();
        return ClassifyType(b);
    }

    private MsgPackValue ReadValue()
    {
        if (_buffer.IsEnd)
        {
            return new MsgPackValue { Type = MsgPackType.Nil };
        }

        var b = _buffer.ReadU8();

        if (b <= MsgPackConstants.PositiveFixIntMax)
        {
            return new MsgPackValue { Type = MsgPackType.Integer, RawValue = (long)b };
        }

        if (b >= MsgPackConstants.NegativeFixIntMin)
        {
            return new MsgPackValue { Type = MsgPackType.Integer, RawValue = (long)(sbyte)b };
        }

        if (b >= MsgPackConstants.FixMapMin && b <= MsgPackConstants.FixMapMax)
        {
            return ReadMap(b & 0x0F);
        }

        if (b >= MsgPackConstants.FixArrayMin && b <= MsgPackConstants.FixArrayMax)
        {
            return ReadArray(b & 0x0F);
        }

        if (b >= MsgPackConstants.FixStrMin && b <= MsgPackConstants.FixStrMax)
        {
            return ReadString(b & 0x1F);
        }

        return b switch
        {
            MsgPackConstants.Nil => new MsgPackValue { Type = MsgPackType.Nil },
            MsgPackConstants.False => new MsgPackValue { Type = MsgPackType.Boolean, RawValue = false },
            MsgPackConstants.True => new MsgPackValue { Type = MsgPackType.Boolean, RawValue = true },
            MsgPackConstants.Uint8 => new MsgPackValue { Type = MsgPackType.UnsignedInteger, RawValue = (long)_buffer.ReadU8() },
            MsgPackConstants.Uint16 => new MsgPackValue { Type = MsgPackType.UnsignedInteger, RawValue = (long)_buffer.ReadU16BE() },
            MsgPackConstants.Uint32 => new MsgPackValue { Type = MsgPackType.UnsignedInteger, RawValue = (long)_buffer.ReadU32BE() },
            MsgPackConstants.Uint64 => new MsgPackValue { Type = MsgPackType.UnsignedInteger, RawValue = (long)_buffer.ReadU64BE() },
            MsgPackConstants.Int8 => new MsgPackValue { Type = MsgPackType.Integer, RawValue = (long)_buffer.ReadI8() },
            MsgPackConstants.Int16 => new MsgPackValue { Type = MsgPackType.Integer, RawValue = (long)_buffer.ReadI16BE() },
            MsgPackConstants.Int32 => new MsgPackValue { Type = MsgPackType.Integer, RawValue = (long)_buffer.ReadI32BE() },
            MsgPackConstants.Int64 => new MsgPackValue { Type = MsgPackType.Integer, RawValue = _buffer.ReadI64BE() },
            MsgPackConstants.Float32 => new MsgPackValue { Type = MsgPackType.Float, RawValue = _buffer.ReadF32BE() },
            MsgPackConstants.Float64 => new MsgPackValue { Type = MsgPackType.Float, RawValue = _buffer.ReadF64BE() },
            MsgPackConstants.Str8 => ReadString(_buffer.ReadU8()),
            MsgPackConstants.Str16 => ReadString(_buffer.ReadU16BE()),
            MsgPackConstants.Str32 => ReadString((int)_buffer.ReadU32BE()),
            MsgPackConstants.Bin8 => ReadBinary(_buffer.ReadU8()),
            MsgPackConstants.Bin16 => ReadBinary(_buffer.ReadU16BE()),
            MsgPackConstants.Bin32 => ReadBinary((int)_buffer.ReadU32BE()),
            MsgPackConstants.Array16 => ReadArray(_buffer.ReadU16BE()),
            MsgPackConstants.Array32 => ReadArray((int)_buffer.ReadU32BE()),
            MsgPackConstants.Map16 => ReadMap(_buffer.ReadU16BE()),
            MsgPackConstants.Map32 => ReadMap((int)_buffer.ReadU32BE()),
            MsgPackConstants.FixExt1 => ReadExtension(1),
            MsgPackConstants.FixExt2 => ReadExtension(2),
            MsgPackConstants.FixExt4 => ReadExtension(4),
            MsgPackConstants.FixExt8 => ReadExtension(8),
            MsgPackConstants.FixExt16 => ReadExtension(16),
            MsgPackConstants.Ext8 => ReadExtension(_buffer.ReadU8()),
            MsgPackConstants.Ext16 => ReadExtension(_buffer.ReadU16BE()),
            MsgPackConstants.Ext32 => ReadExtension((int)_buffer.ReadU32BE()),
            _ => new MsgPackValue { Type = MsgPackType.Nil }
        };
    }

    private MsgPackValue ReadString(int length)
    {
        var str = _buffer.ReadString(length);
        return new MsgPackValue { Type = MsgPackType.String, RawValue = str };
    }

    private MsgPackValue ReadBinary(int length)
    {
        var data = _buffer.ReadBytes(length).ToArray();
        return new MsgPackValue { Type = MsgPackType.Binary, RawValue = data };
    }

    private MsgPackValue ReadArray(int count)
    {
        var items = new MsgPackValue[count];

        for (var i = 0; i < count; i++)
        {
            items[i] = ReadValue();
        }

        return new MsgPackValue { Type = MsgPackType.Array, ArrayItems = items };
    }

    private MsgPackValue ReadMap(int count)
    {
        var entries = new MsgPackMapEntry[count];

        for (var i = 0; i < count; i++)
        {
            var key = ReadValue();
            var value = ReadValue();
            entries[i] = new MsgPackMapEntry { Key = key, Value = value };
        }

        return new MsgPackValue { Type = MsgPackType.Map, MapEntries = entries };
    }

    private MsgPackValue ReadExtension(int length)
    {
        var type = (sbyte)_buffer.ReadU8();
        var data = _buffer.ReadBytes(length).ToArray();
        return new MsgPackValue { Type = MsgPackType.Extension, ExtensionType = type, ExtensionData = data };
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

using System.Buffers.Binary;
using Acorn.Frame;
using Acorn.MessagePack.Data;

namespace Acorn.MessagePack.Scanner;

public ref struct MsgPackScanner
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    public MsgPackScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    public int Position => _position;

    public bool IsEnd => _position >= _data.Length;

    public int Length => _data.Length;

    public int RemainingBytes => _data.Length - _position;

    public MsgPackScanHeader ScanHeader()
    {
        if (IsEnd) return new MsgPackScanHeader { RootType = MsgPackType.Nil };
        var b = _data[_position];
        var type = ClassifyType(b);
        return new MsgPackScanHeader { RootType = type, FirstByte = b };
    }

    public bool IsPossibleMsgPack()
    {
        if (IsEnd) return false;
        var b = _data[_position];
        return b <= MsgPackConstants.PositiveFixIntMax
               || b >= MsgPackConstants.NegativeFixIntMin
               || b is MsgPackConstants.Nil or MsgPackConstants.False or MsgPackConstants.True
               || b >= MsgPackConstants.FixMapMin;
    }

    public MsgPackScanStatistics ScanStatistics()
    {
        var stats = new MsgPackScanStatistics();

        while (_position < _data.Length)
        {
            if (_data[_position] == 0) break;

            var b = _data[_position];
            var type = ClassifyType(b);
            CountType(ref stats, type);

            var skip = GetValueByteCount(b);
            if (skip <= 0) break;
            _position += skip;
        }

        return stats;
    }

    private int GetValueByteCount(byte b)
    {
        if (b <= MsgPackConstants.PositiveFixIntMax) return 1;
        if (b >= MsgPackConstants.NegativeFixIntMin) return 1;

        if (b >= MsgPackConstants.FixMapMin && b <= MsgPackConstants.FixMapMax)
        {
            return 1 + 2 * (b & 0x0F);
        }

        if (b >= MsgPackConstants.FixArrayMin && b <= MsgPackConstants.FixArrayMax)
        {
            return 1 + (b & 0x0F);
        }

        if (b >= MsgPackConstants.FixStrMin && b <= MsgPackConstants.FixStrMax)
        {
            return 1 + (b & 0x1F);
        }

        return b switch
        {
            MsgPackConstants.Nil or MsgPackConstants.False or MsgPackConstants.True => 1,
            MsgPackConstants.Float32 => 5,
            MsgPackConstants.Float64 => 9,
            MsgPackConstants.Uint8 => 2,
            MsgPackConstants.Uint16 => 3,
            MsgPackConstants.Uint32 => 5,
            MsgPackConstants.Uint64 => 9,
            MsgPackConstants.Int8 => 2,
            MsgPackConstants.Int16 => 3,
            MsgPackConstants.Int32 => 5,
            MsgPackConstants.Int64 => 9,
            MsgPackConstants.Str8 => 2 + _data[_position + 1],
            MsgPackConstants.Str16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position + 1, 2)),
            MsgPackConstants.Str32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position + 1, 4)),
            MsgPackConstants.Bin8 => 2 + _data[_position + 1],
            MsgPackConstants.Bin16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position + 1, 2)),
            MsgPackConstants.Bin32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position + 1, 4)),
            MsgPackConstants.Array16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position + 1, 2)),
            MsgPackConstants.Array32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position + 1, 4)),
            MsgPackConstants.Map16 => 3 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position + 1, 2)),
            MsgPackConstants.Map32 => 5 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position + 1, 4)),
            MsgPackConstants.FixExt1 => 3,
            MsgPackConstants.FixExt2 => 4,
            MsgPackConstants.FixExt4 => 6,
            MsgPackConstants.FixExt8 => 10,
            MsgPackConstants.FixExt16 => 18,
            MsgPackConstants.Ext8 => 3 + _data[_position + 1],
            MsgPackConstants.Ext16 => 4 + BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position + 1, 2)),
            MsgPackConstants.Ext32 => 6 + (int)BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position + 1, 4)),
            _ => 1
        };
    }

    private static MsgPackType ClassifyType(byte b)
    {
        if (b <= MsgPackConstants.PositiveFixIntMax) return MsgPackType.Integer;
        if (b >= MsgPackConstants.NegativeFixIntMin) return MsgPackType.Integer;
        if (b >= MsgPackConstants.FixMapMin && b <= MsgPackConstants.FixMapMax) return MsgPackType.Map;
        if (b >= MsgPackConstants.FixArrayMin && b <= MsgPackConstants.FixArrayMax) return MsgPackType.Array;
        if (b >= MsgPackConstants.FixStrMin && b <= MsgPackConstants.FixStrMax) return MsgPackType.String;
        if (b >= MsgPackConstants.FixExt1 && b <= MsgPackConstants.FixExt16) return MsgPackType.Extension;
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
            MsgPackConstants.Ext8 or MsgPackConstants.Ext16 or MsgPackConstants.Ext32 => MsgPackType.Extension,
            _ => MsgPackType.Extension
        };
    }

    private static void CountType(ref MsgPackScanStatistics stats, MsgPackType type)
    {
        switch (type)
        {
            case MsgPackType.Nil:
                stats.NilCount++;
                break;
            case MsgPackType.Boolean:
                stats.BoolCount++;
                break;
            case MsgPackType.Integer:
            case MsgPackType.UnsignedInteger:
                stats.IntegerCount++;
                break;
            case MsgPackType.Float:
                stats.FloatCount++;
                break;
            case MsgPackType.String:
                stats.StringCount++;
                break;
            case MsgPackType.Binary:
                stats.BinaryCount++;
                break;
            case MsgPackType.Array:
                stats.ArrayCount++;
                break;
            case MsgPackType.Map:
                stats.MapCount++;
                break;
            case MsgPackType.Extension:
                stats.ExtensionCount++;
                break;
        }
    }
}

public sealed class MsgPackScanHeader
{
    public MsgPackType RootType { get; init; }
    public byte FirstByte { get; init; }
}

public sealed class MsgPackScanStatistics
{
    public int NilCount { get; set; }
    public int BoolCount { get; set; }
    public int IntegerCount { get; set; }
    public int FloatCount { get; set; }
    public int StringCount { get; set; }
    public int BinaryCount { get; set; }
    public int ArrayCount { get; set; }
    public int MapCount { get; set; }
    public int ExtensionCount { get; set; }
}

using Acorn.Frame;
using Acorn.MessagePack.Data;

namespace Acorn.MessagePack.Encode;

public sealed class MsgPackEncoder
{
    public byte[] Encode(MsgPackData data)
    {
        var writer = new ByteBufferWriter(256);
        WriteValue(ref writer, data.Root);
        return writer.ToArray();
    }

    public int Encode(MsgPackData data, Span<byte> buffer)
    {
        var writer = new ByteBufferWriter(buffer.Length + 256);
        WriteValue(ref writer, data.Root);
        var encoded = writer.ToArray();
        encoded.CopyTo(buffer);
        return encoded.Length;
    }

    private static void WriteValue(ref ByteBufferWriter writer, MsgPackValue value)
    {
        switch (value.Type)
        {
            case MsgPackType.Nil:
                writer.WriteU8(MsgPackConstants.Nil);
                break;

            case MsgPackType.Boolean:
                writer.WriteU8((bool)value.RawValue! ? MsgPackConstants.True : MsgPackConstants.False);
                break;

            case MsgPackType.Integer:
                WriteInteger(ref writer, (long)value.RawValue!);
                break;

            case MsgPackType.UnsignedInteger:
                WriteUnsignedInteger(ref writer, (ulong)value.RawValue!);
                break;

            case MsgPackType.Float:
                writer.WriteU8(MsgPackConstants.Float64);
                writer.WriteF64BE((double)value.RawValue!);
                break;

            case MsgPackType.String:
                WriteString(ref writer, (string)value.RawValue!);
                break;

            case MsgPackType.Binary:
                WriteBinary(ref writer, (byte[])value.RawValue!);
                break;

            case MsgPackType.Array:
                WriteArray(ref writer, value.ArrayItems);
                break;

            case MsgPackType.Map:
                WriteMap(ref writer, value.MapEntries);
                break;

            case MsgPackType.Extension:
                WriteExtension(ref writer, value.ExtensionType, value.ExtensionData);
                break;
        }
    }

    private static void WriteInteger(ref ByteBufferWriter writer, long value)
    {
        if (value >= 0 && value <= MsgPackConstants.PositiveFixIntMax)
        {
            writer.WriteU8((byte)value);
            return;
        }

        if (value >= -32 && value < 0)
        {
            writer.WriteU8((byte)(0xE0 + (value + 32)));
            return;
        }

        if (value is >= sbyte.MinValue and <= sbyte.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Int8);
            writer.WriteI8((sbyte)value);
            return;
        }

        if (value is >= short.MinValue and <= short.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Int16);
            writer.WriteI16BE((short)value);
            return;
        }

        if (value is >= int.MinValue and <= int.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Int32);
            writer.WriteI32BE((int)value);
            return;
        }

        writer.WriteU8(MsgPackConstants.Int64);
        writer.WriteI64BE(value);
    }

    private static void WriteUnsignedInteger(ref ByteBufferWriter writer, ulong value)
    {
        if (value <= MsgPackConstants.PositiveFixIntMax)
        {
            writer.WriteU8((byte)value);
            return;
        }

        if (value <= byte.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Uint8);
            writer.WriteU8((byte)value);
            return;
        }

        if (value <= ushort.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Uint16);
            writer.WriteU16BE((ushort)value);
            return;
        }

        if (value <= uint.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Uint32);
            writer.WriteU32BE((uint)value);
            return;
        }

        writer.WriteU8(MsgPackConstants.Uint64);
        writer.WriteU64BE(value);
    }

    private static void WriteString(ref ByteBufferWriter writer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var length = bytes.Length;

        if (length <= 31)
        {
            writer.WriteU8((byte)(MsgPackConstants.FixStrMin | length));
            writer.Write(bytes);
        }
        else if (length <= byte.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Str8);
            writer.WriteU8((byte)length);
            writer.Write(bytes);
        }
        else if (length <= ushort.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Str16);
            writer.WriteU16BE((ushort)length);
            writer.Write(bytes);
        }
        else
        {
            writer.WriteU8(MsgPackConstants.Str32);
            writer.WriteU32BE((uint)length);
            writer.Write(bytes);
        }
    }

    private static void WriteBinary(ref ByteBufferWriter writer, byte[] data)
    {
        var length = data.Length;

        if (length <= byte.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Bin8);
            writer.WriteU8((byte)length);
            writer.Write(data);
        }
        else if (length <= ushort.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Bin16);
            writer.WriteU16BE((ushort)length);
            writer.Write(data);
        }
        else
        {
            writer.WriteU8(MsgPackConstants.Bin32);
            writer.WriteU32BE((uint)length);
            writer.Write(data);
        }
    }

    private static void WriteArray(ref ByteBufferWriter writer, IReadOnlyList<MsgPackValue> items)
    {
        var count = items.Count;

        if (count <= 15)
        {
            writer.WriteU8((byte)(MsgPackConstants.FixArrayMin | count));
        }
        else if (count <= ushort.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Array16);
            writer.WriteU16BE((ushort)count);
        }
        else
        {
            writer.WriteU8(MsgPackConstants.Array32);
            writer.WriteU32BE((uint)count);
        }

        for (var i = 0; i < count; i++)
        {
            WriteValue(ref writer, items[i]);
        }
    }

    private static void WriteMap(ref ByteBufferWriter writer, IReadOnlyList<MsgPackMapEntry> entries)
    {
        var count = entries.Count;

        if (count <= 15)
        {
            writer.WriteU8((byte)(MsgPackConstants.FixMapMin | count));
        }
        else if (count <= ushort.MaxValue)
        {
            writer.WriteU8(MsgPackConstants.Map16);
            writer.WriteU16BE((ushort)count);
        }
        else
        {
            writer.WriteU8(MsgPackConstants.Map32);
            writer.WriteU32BE((uint)count);
        }

        for (var i = 0; i < count; i++)
        {
            WriteValue(ref writer, entries[i].Key);
            WriteValue(ref writer, entries[i].Value);
        }
    }

    private static void WriteExtension(ref ByteBufferWriter writer, sbyte type, byte[] data)
    {
        var length = data.Length;

        switch (length)
        {
            case 1:
                writer.WriteU8(MsgPackConstants.FixExt1);
                break;
            case 2:
                writer.WriteU8(MsgPackConstants.FixExt2);
                break;
            case 4:
                writer.WriteU8(MsgPackConstants.FixExt4);
                break;
            case 8:
                writer.WriteU8(MsgPackConstants.FixExt8);
                break;
            case 16:
                writer.WriteU8(MsgPackConstants.FixExt16);
                break;
            case <= byte.MaxValue:
                writer.WriteU8(MsgPackConstants.Ext8);
                writer.WriteU8((byte)length);
                break;
            case <= ushort.MaxValue:
                writer.WriteU8(MsgPackConstants.Ext16);
                writer.WriteU16BE((ushort)length);
                break;
            default:
                writer.WriteU8(MsgPackConstants.Ext32);
                writer.WriteU32BE((uint)length);
                break;
        }

        writer.WriteU8((byte)type);
        writer.Write(data);
    }
}

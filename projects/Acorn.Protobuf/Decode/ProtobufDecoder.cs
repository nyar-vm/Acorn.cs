using Acorn.Frame;
using Acorn.Protobuf.Data;

namespace Acorn.Protobuf.Decode;

/// <summary>
///     Protobuf 解码器，用于解析 Protobuf 二进制消息。
/// </summary>
public static class ProtobufDecoder
{
    /// <summary>
    ///     解码 Protobuf 消息。
    /// </summary>
    /// <param name="data">要解码的二进制数据。</param>
    /// <returns>解码后的 Protobuf 消息数据。</returns>
    public static ProtobufMessageData DecodeMessage(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        var message = new ProtobufMessageData
        {
            Name = "RootMessage"
        };

        while (!buffer.IsEnd)
        {
            var field = DecodeField(buffer);
            message.Fields.Add(field);
        }

        return message;
    }

    /// <summary>
    ///     解码字段。
    /// </summary>
    /// <param name="buffer">字节缓冲区。</param>
    /// <returns>解码后的字段数据。</returns>
    private static ProtobufFieldData DecodeField(ByteBuffer buffer)
    {
        var tag = buffer.ReadLeb128U32();
        var fieldNumber = (int)(tag >> 3);
        var wireType = (ProtobufConstants.WireType)(tag & 0x07);

        var field = new ProtobufFieldData
        {
            FieldNumber = fieldNumber,
            Type = string.Empty,
            Name = string.Empty
        };

        switch (wireType)
        {
            case ProtobufConstants.WireType.Varint:
                DecodeVarintField(buffer, field);
                break;
            case ProtobufConstants.WireType.Fixed64:
                DecodeFixed64Field(buffer, field);
                break;
            case ProtobufConstants.WireType.LengthDelimited:
                DecodeLengthDelimitedField(buffer, field);
                break;
            case ProtobufConstants.WireType.Fixed32:
                DecodeFixed32Field(buffer, field);
                break;
        }

        return field;
    }

    /// <summary>
    ///     解码 Varint 字段。
    /// </summary>
    /// <param name="buffer">字节缓冲区。</param>
    /// <param name="field">字段数据。</param>
    private static void DecodeVarintField(ByteBuffer buffer, ProtobufFieldData field)
    {
        _ = buffer.ReadLeb128U64();
        field.Type = "varint";
        field.Name = $"field_{field.FieldNumber}";
    }

    /// <summary>
    ///     解码 Fixed64 字段。
    /// </summary>
    /// <param name="buffer">字节缓冲区。</param>
    /// <param name="field">字段数据。</param>
    private static void DecodeFixed64Field(ByteBuffer buffer, ProtobufFieldData field)
    {
        _ = buffer.ReadU64LE();
        field.Type = "fixed64";
        field.Name = $"field_{field.FieldNumber}";
    }

    /// <summary>
    ///     解码 LengthDelimited 字段。
    /// </summary>
    /// <param name="buffer">字节缓冲区。</param>
    /// <param name="field">字段数据。</param>
    private static void DecodeLengthDelimitedField(ByteBuffer buffer, ProtobufFieldData field)
    {
        var length = buffer.ReadLeb128U32();
        buffer.Advance((int)length);
        field.Type = "length_delimited";
        field.Name = $"field_{field.FieldNumber}";
    }

    /// <summary>
    ///     解码 Fixed32 字段。
    /// </summary>
    /// <param name="buffer">字节缓冲区。</param>
    /// <param name="field">字段数据。</param>
    private static void DecodeFixed32Field(ByteBuffer buffer, ProtobufFieldData field)
    {
        _ = buffer.ReadU32LE();
        field.Type = "fixed32";
        field.Name = $"field_{field.FieldNumber}";
    }
}

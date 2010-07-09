using Acorn.Frame;
using Acorn.Protobuf.Data;

namespace Acorn.Protobuf.Encode;

/// <summary>
///     Protobuf 编码器，用于生成 Protobuf 二进制消息。
/// </summary>
public static class ProtobufEncoder
{
    /// <summary>
    ///     编码 Protobuf 消息。
    /// </summary>
    /// <param name="buffer">要写入的目标字节缓冲区。</param>
    /// <param name="message">Protobuf 消息数据。</param>
    public static void EncodeMessage(Span<byte> buffer, ProtobufMessageData message)
    {
        var writer = new ByteBufferWriter(buffer);
        EncodeMessageCore(writer, message);
    }

    /// <summary>
    ///     编码 Protobuf 消息到字节数组。
    /// </summary>
    /// <param name="message">Protobuf 消息数据。</param>
    /// <returns>编码后的字节数组。</returns>
    public static byte[] EncodeMessage(ProtobufMessageData message)
    {
        var temp = new byte[4096];
        var writer = new ByteBufferWriter(temp);
        EncodeMessageCore(writer, message);
        return temp[..writer.Position].ToArray();
    }

    /// <summary>
    ///     编码 Protobuf 消息核心逻辑。
    /// </summary>
    /// <param name="writer">字节缓冲区写入器。</param>
    /// <param name="message">Protobuf 消息数据。</param>
    private static void EncodeMessageCore(ByteBufferWriter writer, ProtobufMessageData message)
    {
        foreach (var field in message.Fields)
        {
            EncodeField(writer, field);
        }

        foreach (var nestedMessage in message.NestedMessages)
        {
            EncodeMessageCore(writer, nestedMessage);
        }
    }

    /// <summary>
    ///     编码字段。
    /// </summary>
    /// <param name="writer">字节缓冲区写入器。</param>
    /// <param name="field">字段数据。</param>
    private static void EncodeField(ByteBufferWriter writer, ProtobufFieldData field)
    {
        var tag = (uint)(field.FieldNumber << 3);

        switch (field.Type)
        {
            case "varint":
                tag |= (uint)ProtobufConstants.WireType.Varint;
                writer.WriteLeb128U32(tag);
                writer.WriteLeb128U64(0);
                break;
            case "fixed64":
                tag |= (uint)ProtobufConstants.WireType.Fixed64;
                writer.WriteLeb128U32(tag);
                writer.WriteU64LE(0);
                break;
            case "length_delimited":
                tag |= (uint)ProtobufConstants.WireType.LengthDelimited;
                writer.WriteLeb128U32(tag);
                writer.WriteLeb128U32(0);
                break;
            case "fixed32":
                tag |= (uint)ProtobufConstants.WireType.Fixed32;
                writer.WriteLeb128U32(tag);
                writer.WriteU32LE(0);
                break;
        }
    }
}

using Acorn.Codec;

namespace Acorn.Protobuf.Codec;

/// <summary>
///     Protobuf 字符串编解码器，基于 <see cref="Leb128UInt32" /> 提供 Protobuf 长度前缀字符串编码。
/// </summary>
public readonly struct ProtobufStringCodec : ICodec<string>
{
    private readonly Leb128UInt32 _varintCodec;

    /// <summary>
    ///     初始化 <see cref="ProtobufStringCodec" /> 结构的新实例。
    /// </summary>
    public ProtobufStringCodec()
    {
        _varintCodec = new Leb128UInt32();
    }

    /// <inheritdoc />
    public int GetSize(string value) => -1;

    /// <inheritdoc />
    public void Encode(string value, Span<byte> destination)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        _varintCodec.Encode((uint)bytes.Length, destination);
        bytes.CopyTo(destination[Leb128EncodedSize((uint)bytes.Length)..]);
    }

    /// <inheritdoc />
    public string Decode(ReadOnlySpan<byte> source)
    {
        var length = (int)_varintCodec.Decode(source);
        var leb128Size = Leb128EncodedSize((uint)length);
        return System.Text.Encoding.UTF8.GetString(source.Slice(leb128Size, length));
    }

    private static int Leb128EncodedSize(uint value)
    {
        var size = 0;
        do
        {
            value >>= 7;
            size++;
        } while (value != 0);

        return size;
    }
}

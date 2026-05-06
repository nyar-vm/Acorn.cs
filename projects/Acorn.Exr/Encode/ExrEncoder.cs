using System.Buffers.Binary;
using System.Text;
using Acorn.Exr.Data;

namespace Acorn.Exr.Encode;

/// <summary>
///     OpenEXR 编码器，将 <see cref="ExrImageData" /> 编码为 EXR 二进制格式。
/// </summary>
public sealed class ExrEncoder
{
    /// <summary>
    ///    将 EXR 图像数据编码为 EXR 头部二进制。
    /// </summary>
    /// <param name="data">EXR 图像数据。</param>
    /// <returns>EXR 头部二进制数据。</returns>
    public byte[] Encode(ExrImageData data)
    {
        var channels = data.Channels;
        var displayWindow = data.DisplayWindow;
        var dataWindow = data.DataWindow;

        // 阻塞构建（避免 MemoryStream 带来的位置不确定性）
        var channelBytes = BuildChannelList(channels);
        var compressionBytes = new byte[] { (byte)data.Compression };
        var dataWindowBytes = BuildBox2i(dataWindow);
        var displayWindowBytes = BuildBox2i(displayWindow);

        var attrs = new (string name, string type, byte[] data)[]
        {
            ("channels", "chlist", channelBytes),
            ("compression", "compression", compressionBytes),
            ("dataWindow", "box2i", dataWindowBytes),
            ("displayWindow", "box2i", displayWindowBytes)
        };

        // 预计算总大小
        var totalSize = 8; // 魔数(4) + 版本(4)

        foreach (var (name, type, attrData) in attrs)
        {
            totalSize += Encoding.ASCII.GetByteCount(name) + 1; // name + null
            totalSize += Encoding.ASCII.GetByteCount(type) + 1; // type + null
            totalSize += 4; // size
            totalSize += attrData.Length; // data
        }

        totalSize += 1; // 终止 null

        var buffer = new byte[totalSize];
        var pos = 0;

        // 魔数
        ExrConstants.MagicNumber.CopyTo(buffer.AsSpan(pos));
        pos += 4;

        // 版本
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), 2);
        pos += 4;

        // 属性
        foreach (var (name, type, attrData) in attrs)
        {
            pos = WriteCString(buffer, pos, name);
            pos = WriteCString(buffer, pos, type);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)attrData.Length);
            pos += 4;
            attrData.CopyTo(buffer.AsSpan(pos));
            pos += attrData.Length;
        }

        // 终止 null
        buffer[pos] = 0;

        return buffer;
    }

    /// <summary>
    ///     写入 C 风格（null 结尾）字符串。
    /// </summary>
    private static int WriteCString(byte[] buffer, int pos, string value)
    {
        var len = Encoding.ASCII.GetBytes(value, buffer.AsSpan(pos));
        pos += len;
        buffer[pos++] = 0;

        return pos;
    }

    /// <summary>
    ///     构建通道列表数据块。
    /// </summary>
    private static byte[] BuildChannelList(IReadOnlyList<ExrChannel> channels)
    {
        var ms = new MemoryStream();

        foreach (var ch in channels)
        {
            var nameBytes = Encoding.ASCII.GetBytes(ch.Name);
            ms.Write(nameBytes);
            ms.WriteByte(0);

            // pixelType (I32LE)
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buf, (int)ch.PixelType);
            ms.Write(buf);

            // pLinear + reserved
            ms.WriteByte(0);
            ms.WriteByte(0);
            ms.WriteByte(0);
            ms.WriteByte(0);

            // xSampling
            BinaryPrimitives.WriteInt32LittleEndian(buf, 1);
            ms.Write(buf);

            // ySampling
            BinaryPrimitives.WriteInt32LittleEndian(buf, 1);
            ms.Write(buf);
        }

        ms.WriteByte(0); // 终止 null

        return ms.ToArray();
    }

    /// <summary>
    ///     构建 Box2i 数据块。
    /// </summary>
    private static byte[] BuildBox2i(ExrBox2i box)
    {
        var data = new byte[16];
        var pos = 0;

        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.XMin);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.YMin);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.XMax);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.YMax);

        return data;
    }
}

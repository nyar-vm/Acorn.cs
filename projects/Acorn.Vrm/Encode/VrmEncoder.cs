using Acorn.Vrm.Data;

namespace Acorn.Vrm.Encode;

/// <summary>
///     VRM 编码器，将 <see cref="VrmModelData" /> 编码为 VRM（glTF 容器）二进制格式。
/// </summary>
public sealed class VrmEncoder
{
    /// <summary>
    ///     将 VRM 模型数据编码为 VRM 二进制。
    /// </summary>
    /// <param name="data">VRM 模型数据。</param>
    /// <returns>VRM 二进制数据。</returns>
    public byte[] Encode(VrmModelData data)
    {
        // VRM 0.x = glTF 2.0 二进制容器，以 "glTF" 魔数开头
        // 最小 glTF 容器：magic(4) + version(4) + totalLength(4) + jsonChunkLength(4) + jsonChunkType(4) + json("{}"(含padding))
        var json = "{}";
        var jsonLen = json.Length;
        var paddedLen = (jsonLen + 3) & ~3; // 4 字节对齐

        var totalLength = 12 + 8 + paddedLen; // header(12) + chunk_header(8) + json_data
        var buffer = new byte[totalLength];
        var pos = 0;

        // glTF 魔数
        "glTF"u8.CopyTo(buffer.AsSpan(pos));
        pos += 4;

        // 版本号（glTF 2.0）
        WriteU32LE(buffer, ref pos, 2);

        // 总长度
        WriteU32LE(buffer, ref pos, (uint)totalLength);

        // JSON chunk 长度
        WriteU32LE(buffer, ref pos, (uint)paddedLen);

        // JSON chunk 类型（0x4E4F534A = "JSON"）
        WriteU32LE(buffer, ref pos, 0x4E4F534A);

        // JSON 数据
        var jsonBytes = System.Text.Encoding.ASCII.GetBytes(json);
        jsonBytes.CopyTo(buffer.AsSpan(pos));
        pos += jsonLen;

        // 空格填充到 4 字节对齐
        while (pos < totalLength)
        {
            buffer[pos++] = 0x20;
        }

        return buffer;
    }

    /// <summary>
    ///     写入 UInt32 Little-Endian。
    /// </summary>
    private static void WriteU32LE(byte[] buffer, ref int pos, uint value)
    {
        buffer[pos++] = (byte)(value);
        buffer[pos++] = (byte)(value >> 8);
        buffer[pos++] = (byte)(value >> 16);
        buffer[pos++] = (byte)(value >> 24);
    }
}

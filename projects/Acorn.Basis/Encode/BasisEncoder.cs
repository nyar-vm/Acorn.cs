using System.Buffers.Binary;
using Acorn.Basis.Data;

namespace Acorn.Basis.Encode;

/// <summary>
///     Basis/KTX2 编码器，将 <see cref="BasisFileData" /> 编码为 KTX2 二进制格式。
/// </summary>
/// <remarks>
///     KTX2 是 Khronos 纹理容器格式，Basis Universal 超级压缩使用此容器。
///     编码器生成 KTX2 格式（魔数 «KTX 20»），不含实际压缩数据。
///     Basis 原生格式（BSS1）暂不支持编码。
/// </remarks>
public sealed class BasisEncoder
{
    /// <summary>
    ///     KTX2 头大小（含魔数）。
    /// </summary>
    private const int Ktx2HeaderSize = 80;

    /// <summary>
    ///     将 Basis 文件数据编码为 KTX2 二进制。
    /// </summary>
    /// <param name="data">Basis 文件数据。</param>
    /// <param name="format">Vulkan 格式（默认 VK_FORMAT_UNDEFINED）。</param>
    /// <returns>KTX2 二进制数据。</returns>
    public byte[] Encode(BasisFileData data, uint format = 0)
    {
        var buffer = new byte[Ktx2HeaderSize];
        var pos = 0;

        // 魔数
        BasisConstants.Ktx2Magic.CopyTo(buffer.AsSpan(pos));
        pos += 12;

        // KTX2 头部字段（共 17 个字段 = 68 字节）
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), format); // vkFormat
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // typeSize
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.Width); // pixelWidth
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.Height); // pixelHeight
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // pixelDepth
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)Math.Max(1, data.ImageCount)); // layerCount
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // faceCount
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)Math.Max(1, data.MipLevels)); // levelCount
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1); // supercompressionScheme (1 = BasisLZ)
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Ktx2HeaderSize); // dfdByteOffset
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 0); // dfdByteLength
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), Ktx2HeaderSize); // kvDataByteOffset
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 0); // kvDataByteLength
        pos += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), Ktx2HeaderSize); // sgdByteOffset
        pos += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0); // sgdByteLength

        return buffer;
    }

    /// <summary>
    ///     编码包含实际 DFD 数据的完整 KTX2。
    /// </summary>
    /// <param name="data">Basis 文件数据。</param>
    /// <param name="dfdData">DFD 数据块。</param>
    /// <param name="levelData">各 mipmap 级别的数据。</param>
    /// <param name="format">Vulkan 格式。</param>
    /// <returns>KTX2 二进制数据。</returns>
    public byte[] EncodeWithData(BasisFileData data, byte[]? dfdData, byte[]?[]? levelData, uint format = 0)
    {
        dfdData ??= [];
        levelData ??= [];

        var dfdOffset = 80L;
        var kvOffset = dfdOffset + dfdData.Length;
        var levelCount = Math.Max(1, data.MipLevels);

        // 计算层级索引区域大小
        long levelsOffset = kvOffset;
        var levelIndexSize = levelCount * 24; // 每级 24 字节（byteOffset:8 + byteLength:8 + uncompressedByteLength:8）

        // 写入头部
        var totalSize = levelsOffset + levelIndexSize;

        foreach (var lvl in levelData)
        {
            if (lvl != null)
            {
                totalSize += lvl.Length;
            }
        }

        var buffer = new byte[totalSize];
        var pos = 0;

        // 魔数
        BasisConstants.Ktx2Magic.CopyTo(buffer.AsSpan(pos));
        pos += 12;

        // 头部
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), format);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.Width);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)data.Height);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)Math.Max(1, data.ImageCount));
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)levelCount);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 1);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)dfdOffset);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)dfdData.Length);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)kvOffset);
        pos += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), 0);
        pos += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)kvOffset);
        pos += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0);
        pos += 8;

        // DFD 数据
        if (dfdData.Length > 0)
        {
            dfdData.CopyTo(buffer.AsSpan(pos));
            pos += dfdData.Length;
        }

        // Level 索引
        var dataCursor = pos + levelIndexSize;

        for (var i = 0; i < levelCount; i++)
        {
            var lvlBytes = i < levelData.Length ? levelData[i] : null;

            if (lvlBytes is { Length: > 0 })
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)dataCursor);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)lvlBytes.Length);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)lvlBytes.Length);
                pos += 8;
                lvlBytes.CopyTo(buffer.AsSpan(dataCursor));
                dataCursor += lvlBytes.Length;
            }
            else
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), (ulong)dataCursor);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0);
                pos += 8;
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(pos), 0);
                pos += 8;
            }
        }

        return buffer;
    }
}

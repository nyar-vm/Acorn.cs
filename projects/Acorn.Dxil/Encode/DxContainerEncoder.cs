using System.Buffers.Binary;
using Acorn.Dxil.Data;
using Acorn.Frame;

namespace Acorn.Dxil.Encode;

/// <summary>
///     DXContainer 编码器，将 C# 数据结构编码为 DirectX DXContainer 二进制格式。
/// </summary>
/// <remarks>
///     DXContainer 是 DirectX 着色器的容器格式，以 "DXBC" 魔数开头，
///     包含多个 Part。编码器生成符合 Microsoft DXContainer 规范的二进制数据。
/// </remarks>
public sealed class DxContainerEncoder
{
    /// <summary>
    ///     将容器数据编码为 DXContainer 二进制格式。
    /// </summary>
    /// <param name="data">容器数据。</param>
    /// <returns>DXContainer 二进制数据。</returns>
    public byte[] Encode(DxContainerData data)
    {
        var partDataList = new List<byte[]>();

        foreach (var part in data.Parts)
        {
            partDataList.Add(EncodePart(part));
        }

        var headerSize = 20;
        var offsetTableSize = 4 * data.Parts.Count;
        var partOffsets = new uint[data.Parts.Count];

        var currentOffset = (uint)(headerSize + offsetTableSize);

        for (var i = 0; i < data.Parts.Count; i++)
        {
            partOffsets[i] = currentOffset;
            currentOffset += (uint)partDataList[i].Length;
        }

        var totalSize = (int)currentOffset;
        var buffer = new byte[totalSize];
        var writer = new ByteBufferWriter(buffer);

        WriteHeader(ref writer, data.Header, (uint)totalSize, (uint)data.Parts.Count);
        WritePartOffsets(ref writer, partOffsets);

        for (var i = 0; i < partDataList.Count; i++)
        {
            writer.Write(partDataList[i]);
        }

        return buffer[..writer.Position];
    }

    /// <summary>
    ///     从 Part 列表编码为 DXContainer 二进制格式。
    /// </summary>
    /// <param name="parts">Part 列表。</param>
    /// <returns>DXContainer 二进制数据。</returns>
    public byte[] EncodeParts(IReadOnlyList<DxContainerPart> parts)
    {
        var data = new DxContainerData
        {
            Header = new DxContainerHeader
            {
                MagicNumber = DxilConstants.ContainerMagicNumber,
                VersionMajor = DxilConstants.ContainerVersionMajor,
                VersionMinor = DxilConstants.ContainerVersionMinor
            },
            Parts = parts
        };

        return Encode(data);
    }

    /// <summary>
    ///     编码单个 Part 为二进制数据（Part 头 + Part 数据）。
    /// </summary>
    /// <param name="part">Part 数据。</param>
    /// <returns>编码后的 Part 二进制数据。</returns>
    public byte[] EncodePart(DxContainerPart part)
    {
        var size = 8 + part.Data.Length;
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteU32LE(part.Header.FourCC);
        writer.WriteU32LE(part.Header.Size);
        writer.Write(part.Data);

        return buffer[..writer.Position];
    }

    private static void WriteHeader(ref ByteBufferWriter writer, DxContainerHeader header,
        uint fileSize, uint partCount)
    {
        writer.WriteU32LE(DxilConstants.ContainerMagicNumber);
        writer.WriteU16LE(header.VersionMajor);
        writer.WriteU16LE(header.VersionMinor);
        writer.WriteU32LE(fileSize);
        writer.WriteU32LE(partCount);
    }

    private static void WritePartOffsets(ref ByteBufferWriter writer, uint[] partOffsets)
    {
        foreach (var offset in partOffsets)
        {
            writer.WriteU32LE(offset);
        }
    }
}

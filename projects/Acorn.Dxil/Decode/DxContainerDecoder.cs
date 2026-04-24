using Acorn.Dxil.Data;
using Acorn.Frame;

namespace Acorn.Dxil.Decode;

/// <summary>
///     DXContainer 解码器，解析 DirectX DXContainer 二进制格式。
/// </summary>
/// <remarks>
///     DXContainer 是 DirectX 着色器的容器格式，以 "DXBC" 魔数开头，
///     包含多个 Part（DXIL 着色器程序、特征标志、哈希、管线状态验证等）。
/// </remarks>
public sealed class DxContainerDecoder
{
    /// <summary>
    ///     从二进制数据解码 DXContainer。
    /// </summary>
    /// <param name="data">DXContainer 二进制数据。</param>
    /// <returns>解码后的容器数据。</returns>
    /// <exception cref="InvalidDataException">数据不是有效的 DXContainer 格式。</exception>
    public DxContainerData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        return DecodeContainer(ref buffer);
    }

    private DxContainerData DecodeContainer(ref ByteBuffer buffer)
    {
        var header = ReadHeader(ref buffer);
        var partOffsets = ReadPartOffsets(ref buffer, header.PartCount);
        var parts = ReadParts(ref buffer, partOffsets);

        return new DxContainerData
        {
            Header = header,
            Parts = parts
        };
    }

    private DxContainerHeader ReadHeader(ref ByteBuffer buffer)
    {
        var magic = buffer.ReadU32LE();

        if (magic != DxilConstants.ContainerMagicNumber)
        {
            throw new InvalidDataException(
                $"不是有效的 DXContainer 文件（魔数不匹配：期望 0x{DxilConstants.ContainerMagicNumber:X8}，实际 0x{magic:X8}）");
        }

        var versionMajor = buffer.ReadU16LE();
        var versionMinor = buffer.ReadU16LE();
        var fileSize = buffer.ReadU32LE();
        var partCount = buffer.ReadU32LE();

        return new DxContainerHeader
        {
            MagicNumber = magic,
            VersionMajor = versionMajor,
            VersionMinor = versionMinor,
            FileSize = fileSize,
            PartCount = partCount
        };
    }

    private static List<uint> ReadPartOffsets(ref ByteBuffer buffer, uint partCount)
    {
        var offsets = new List<uint>((int)partCount);

        for (var i = 0; i < partCount; i++)
        {
            offsets.Add(buffer.ReadU32LE());
        }

        return offsets;
    }

    private static List<DxContainerPart> ReadParts(ref ByteBuffer buffer, List<uint> partOffsets)
    {
        var parts = new List<DxContainerPart>(partOffsets.Count);

        foreach (var offset in partOffsets)
        {
            buffer.Position = (int)offset;
            var part = ReadPart(ref buffer);
            parts.Add(part);
        }

        return parts;
    }

    private static DxContainerPart ReadPart(ref ByteBuffer buffer)
    {
        var fourCC = buffer.ReadU32LE();
        var size = buffer.ReadU32LE();

        var header = new DxContainerPartHeader
        {
            FourCC = fourCC,
            Size = size
        };

        var data = buffer.ReadBytes((int)size).ToArray();

        return new DxContainerPart
        {
            Header = header,
            Data = data
        };
    }
}

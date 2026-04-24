using Acorn.Frame;
using Acorn.Dds.Data;

namespace Acorn.Dds.Scanner;

/// <summary>
///     DDS 文件扫描器，基于 <see cref="ByteBuffer" /> 提供对 DirectDraw Surface 纹理文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     DDS 文件格式由魔数、文件头、可选 DX10 扩展头和纹理数据组成。
///     扫描器只读取文件头信息，不做完整的像素数据解码，以实现快速探查。
/// </remarks>
public ref struct DdsScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="DdsScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 DDS 字节数据。</param>
    public DdsScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前扫描位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     数据总长度。
    /// </summary>
    public int Length => _buffer.Length;

    /// <summary>
    ///     是否已到达数据末尾。
    /// </summary>
    public bool IsEndOfData => _buffer.IsEnd;

    /// <summary>
    ///     扫描 DDS 文件头，提取基本纹理信息。
    /// </summary>
    /// <returns>DDS 文件头信息。</returns>
    public DdsScanHeader ScanHeader()
    {
        if (_buffer.Length < DdsConstants.FullHeaderSize)
        {
            throw new InvalidDataException("DDS 文件数据过短，无法读取文件头");
        }

        if (!_buffer.MatchMagic(DdsConstants.MagicNumber))
        {
            throw new InvalidDataException("DDS 文件魔数不匹配");
        }

        _buffer.ConsumeMagic(DdsConstants.MagicNumber);

        var headerSize = _buffer.ReadU32LE();

        if (headerSize != DdsConstants.HeaderSize)
        {
            throw new InvalidDataException($"DDS 头部大小无效，期望 {DdsConstants.HeaderSize}，实际 {headerSize}");
        }

        var flags = _buffer.ReadU32LE();
        var height = _buffer.ReadU32LE();
        var width = _buffer.ReadU32LE();
        var pitchOrLinearSize = _buffer.ReadU32LE();
        var depth = _buffer.ReadU32LE();
        var mipMapCount = _buffer.ReadU32LE();

        _buffer.Advance(44);

        var pfSize = _buffer.ReadU32LE();
        var pfFlags = _buffer.ReadU32LE();
        var fourCC = _buffer.ReadU32LE();
        var rgbBitCount = _buffer.ReadU32LE();

        return new DdsScanHeader
        {
            Width = (int)width,
            Height = (int)height,
            Depth = (int)depth,
            MipMapCount = (int)mipMapCount,
            FourCC = fourCC,
            RGBBitCount = rgbBitCount,
            IsCompressed = (pfFlags & (uint)DdsPixelFormatFlags.FourCC) != 0
        };
    }

    /// <summary>
    ///     快速判断数据是否为 DDS 格式。
    /// </summary>
    /// <returns>是否为 DDS 格式。</returns>
    public bool IsDds()
    {
        if (_buffer.Length < 4)
        {
            return false;
        }

        return _buffer.MatchMagic(DdsConstants.MagicNumber);
    }
}

/// <summary>
///     DDS 扫描头部信息。
/// </summary>
public sealed class DdsScanHeader
{
    /// <summary>
    ///     纹理宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     纹理深度。
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    ///     Mipmap 级别数量。
    /// </summary>
    public int MipMapCount { get; init; }

    /// <summary>
    ///     FourCC 压缩格式代码。
    /// </summary>
    public uint FourCC { get; init; }

    /// <summary>
    ///     每像素位数。
    /// </summary>
    public uint RGBBitCount { get; init; }

    /// <summary>
    ///     是否为压缩格式。
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    ///     压缩格式名称。
    /// </summary>
    public string CompressionFormat => FourCC switch
    {
        DdsFourCC.DXT1 => "BC1/DXT1",
        DdsFourCC.DXT3 => "BC2/DXT3",
        DdsFourCC.DXT5 => "BC3/DXT5",
        DdsFourCC.ATI1 => "BC4/ATI1",
        DdsFourCC.ATI2 => "BC5/ATI2",
        DdsFourCC.BC6H => "BC6H",
        DdsFourCC.BC7 => "BC7",
        0 => IsCompressed ? "未知压缩" : "未压缩",
        _ => $"0x{FourCC:X8}"
    };
}

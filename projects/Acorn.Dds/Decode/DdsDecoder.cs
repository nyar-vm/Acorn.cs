using System.Text;
using Acorn.Frame;
using Acorn.Dds.Data;

namespace Acorn.Dds.Decode;

/// <summary>
///     DDS 文件解码器，将 DirectDraw Surface 纹理格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     DDS 是 Microsoft DirectDraw 的纹理容器格式，广泛用于 PC 游戏和图形应用。
///     支持 BC1-BC7 块压缩、立方体贴图、体积纹理和 Mipmap 链。
/// </remarks>
public ref struct DdsDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="DdsDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">DDS 二进制数据。</param>
    public DdsDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 DDS 文件。
    /// </summary>
    /// <returns>DDS 纹理数据。</returns>
    public DdsTextureData Decode()
    {
        var magic = _buffer.ReadString(4);

        if (magic != "DDS ")
        {
            throw new InvalidDataException($"DDS 文件签名无效，期望 \"DDS \"，实际 \"{magic}\"");
        }

        var header = ReadHeader();
        var dx10Header = TryReadDx10Header(header);
        var surfaces = ReadSurfaces(header, dx10Header);

        return new DdsTextureData
        {
            Height = header.Height,
            Width = header.Width,
            Depth = header.Depth,
            MipMapCount = header.MipMapCount,
            PixelFormat = header.PixelFormat,
            Dimension = dx10Header?.Dimension ?? InferDimension(header),
            IsCubeMap = (header.Caps2 & 0x200) != 0,
            CubeMapFaceCount = CountCubeMapFaces(header),
            Surfaces = surfaces
        };
    }

    /// <summary>
    ///     仅解码 DDS 文件头信息。
    /// </summary>
    public DdsHeaderInfo DecodeHeader()
    {
        var magic = _buffer.ReadString(4);

        if (magic != "DDS ")
        {
            throw new InvalidDataException($"DDS 文件签名无效，期望 \"DDS \"，实际 \"{magic}\"");
        }

        return ReadHeader();
    }

    #region 私有解析方法

    private DdsHeaderInfo ReadHeader()
    {
        var size = _buffer.ReadU32LE();

        if (size != DdsConstants.HeaderSize)
        {
            throw new InvalidDataException($"DDS 头部大小无效，期望 {DdsConstants.HeaderSize}，实际 {size}");
        }

        var flags = (DdsFlags)_buffer.ReadU32LE();
        var height = (int)_buffer.ReadU32LE();
        var width = (int)_buffer.ReadU32LE();
        var pitchOrLinearSize = _buffer.ReadU32LE();
        var depth = (int)_buffer.ReadU32LE();
        var mipMapCount = (int)_buffer.ReadU32LE();

        _buffer.Advance(44);

        var pixelFormat = ReadPixelFormat();

        var caps1 = _buffer.ReadU32LE();
        var caps2 = _buffer.ReadU32LE();
        var caps3 = _buffer.ReadU32LE();
        var caps4 = _buffer.ReadU32LE();

        _buffer.Advance(4);

        return new DdsHeaderInfo
        {
            Flags = flags,
            Height = height,
            Width = width,
            PitchOrLinearSize = (int)pitchOrLinearSize,
            Depth = depth,
            MipMapCount = mipMapCount,
            PixelFormat = pixelFormat,
            Caps1 = caps1,
            Caps2 = caps2
        };
    }

    private DdsPixelFormatData ReadPixelFormat()
    {
        var size = _buffer.ReadU32LE();

        if (size != DdsConstants.PixelFormatSize)
        {
            throw new InvalidDataException($"DDS 像素格式大小无效，期望 {DdsConstants.PixelFormatSize}，实际 {size}");
        }

        var flags = (DdsPixelFormatFlags)_buffer.ReadU32LE();
        var fourCC = _buffer.ReadU32LE();
        var rgbBitCount = _buffer.ReadU32LE();
        var rBitMask = _buffer.ReadU32LE();
        var gBitMask = _buffer.ReadU32LE();
        var bBitMask = _buffer.ReadU32LE();
        var aBitMask = _buffer.ReadU32LE();

        return new DdsPixelFormatData
        {
            Flags = flags,
            FourCC = fourCC,
            RGBBitCount = rgbBitCount,
            RBitMask = rBitMask,
            GBitMask = gBitMask,
            BBitMask = bBitMask,
            ABitMask = aBitMask
        };
    }

    private DdsDx10Header? TryReadDx10Header(DdsHeaderInfo header)
    {
        if (header.PixelFormat.FourCC != 0x30315844)
        {
            return null;
        }

        var dx10 = new DdsDx10Header
        {
            Format = _buffer.ReadU32LE(),
            Dimension = (DdsResourceDimension)_buffer.ReadU32LE(),
            MiscFlag = _buffer.ReadU32LE(),
            ArraySize = _buffer.ReadU32LE(),
            MiscFlags2 = _buffer.ReadU32LE()
        };

        return dx10;
    }

    private List<DdsSurfaceData> ReadSurfaces(DdsHeaderInfo header, DdsDx10Header? dx10)
    {
        var surfaces = new List<DdsSurfaceData>();
        var surfaceCount = GetSurfaceCount(header, dx10);

        for (var s = 0; s < surfaceCount; s++)
        {
            var mipLevels = new List<DdsMipLevelData>();
            var mipCount = Math.Max(1, header.MipMapCount);

            for (var m = 0; m < mipCount; m++)
            {
                var mipWidth = Math.Max(1, header.Width >> m);
                var mipHeight = Math.Max(1, header.Height >> m);
                var dataSize = ComputeMipDataSize(header.PixelFormat, mipWidth, mipHeight);

                if (_buffer.Remaining < dataSize)
                {
                    break;
                }

                var data = _buffer.ReadBytes(dataSize).ToArray();
                mipLevels.Add(new DdsMipLevelData
                {
                    Width = mipWidth,
                    Height = mipHeight,
                    Data = data
                });
            }

            surfaces.Add(new DdsSurfaceData { MipLevels = mipLevels });
        }

        return surfaces;
    }

    private static int GetSurfaceCount(DdsHeaderInfo header, DdsDx10Header? dx10)
    {
        if (dx10 != null)
        {
            var count = (int)dx10.ArraySize;

            if (dx10.Dimension == DdsResourceDimension.Texture3D)
            {
                count = 1;
            }

            return count;
        }

        if ((header.Caps2 & 0x200) != 0)
        {
            return CountCubeMapFaces(header);
        }

        return 1;
    }

    private static int CountCubeMapFaces(DdsHeaderInfo header)
    {
        if ((header.Caps2 & 0x200) == 0)
        {
            return 1;
        }

        var faces = 0;

        if ((header.Caps2 & 0x400) != 0) faces++;
        if ((header.Caps2 & 0x800) != 0) faces++;
        if ((header.Caps2 & 0x1000) != 0) faces++;
        if ((header.Caps2 & 0x2000) != 0) faces++;
        if ((header.Caps2 & 0x4000) != 0) faces++;
        if ((header.Caps2 & 0x8000) != 0) faces++;

        return Math.Max(faces, 1);
    }

    private static DdsResourceDimension InferDimension(DdsHeaderInfo header)
    {
        if (header.Depth > 0)
        {
            return DdsResourceDimension.Texture3D;
        }

        return DdsResourceDimension.Texture2D;
    }

    private static int ComputeMipDataSize(DdsPixelFormatData format, int width, int height)
    {
        var blockSize = GetBlockSize(format);

        if (blockSize > 0)
        {
            var blocksX = Math.Max(1, (width + 3) / 4);
            var blocksY = Math.Max(1, (height + 3) / 4);
            return blocksX * blocksY * blockSize;
        }

        var bpp = (int)format.RGBBitCount;

        if (bpp == 0)
        {
            bpp = 32;
        }

        return width * height * (bpp / 8);
    }

    private static int GetBlockSize(DdsPixelFormatData format)
    {
        return format.FourCC switch
        {
            DdsFourCC.DXT1 => 8,
            DdsFourCC.DXT3 => 16,
            DdsFourCC.DXT5 => 16,
            DdsFourCC.ATI1 => 8,
            DdsFourCC.ATI2 => 16,
            DdsFourCC.BC6H => 16,
            DdsFourCC.BC7 => 16,
            _ => 0
        };
    }

    #endregion
}

/// <summary>
///     DDS 头部信息（内部使用）。
/// </summary>
internal sealed class DdsHeaderInfo
{
    public DdsFlags Flags { get; init; }
    public int Height { get; init; }
    public int Width { get; init; }
    public int PitchOrLinearSize { get; init; }
    public int Depth { get; init; }
    public int MipMapCount { get; init; }
    public DdsPixelFormatData PixelFormat { get; init; } = new();
    public uint Caps1 { get; init; }
    public uint Caps2 { get; init; }
}

/// <summary>
///     DDS DX10 扩展头部。
/// </summary>
internal sealed class DdsDx10Header
{
    public uint Format { get; init; }
    public DdsResourceDimension Dimension { get; init; }
    public uint MiscFlag { get; init; }
    public uint ArraySize { get; init; }
    public uint MiscFlags2 { get; init; }
}

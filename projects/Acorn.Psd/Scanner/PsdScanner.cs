using System.Buffers.Binary;
using System.Text;
using Acorn.Frame;
using Acorn.Psd.Data;

namespace Acorn.Psd.Scanner;

/// <summary>
///     PSD 文件扫描器，基于 <see cref="ByteBuffer" /> 提供对 Adobe Photoshop PSD 文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     PSD 文件格式由文件头、颜色模式数据、图像资源、图层/蒙版信息、图像数据五部分组成。
///     扫描器只读取文件头和图层信息部分，不做完整的像素数据解码，以实现快速探查。
/// </remarks>
public ref struct PsdScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="PsdScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 PSD 字节数据。</param>
    public PsdScanner(ReadOnlySpan<byte> data)
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
    ///     扫描 PSD 文件头，提取基本图像信息。
    /// </summary>
    /// <returns>PSD 文件头信息。</returns>
    public PsdHeader ScanHeader()
    {
        if (_buffer.Length < 26)
        {
            throw new InvalidDataException("PSD 文件数据过短，无法读取文件头");
        }

        if (!_buffer.MatchMagic(PsdConstants.MagicNumber))
        {
            throw new InvalidDataException("PSD 文件魔数不匹配");
        }

        _buffer.ConsumeMagic(PsdConstants.MagicNumber);
        var version = _buffer.ReadU16BE();

        if (version != PsdConstants.Version)
        {
            throw new InvalidDataException($"不支持的 PSD 版本：{version}");
        }

        _buffer.Advance(6);

        var channels = _buffer.ReadU16BE();
        var height = _buffer.ReadU32BE();
        var width = _buffer.ReadU32BE();
        var depth = _buffer.ReadU16BE();
        var colorMode = _buffer.ReadU16BE();

        return new PsdHeader
        {
            Version = version,
            Channels = channels,
            Height = (int)height,
            Width = (int)width,
            Depth = depth,
            ColorMode = colorMode
        };
    }

    /// <summary>
    ///     扫描 PSD 文件，提取图层名称列表。
    /// </summary>
    /// <returns>图层名称列表。</returns>
    public List<string> ScanLayerNames()
    {
        var names = new List<string>();

        if (_buffer.Length < 26)
        {
            return names;
        }

        var offset = 26;

        if (offset + 4 > _buffer.Length)
        {
            return names;
        }

        var colorModeDataLength = _buffer.ReadI32At(offset, true);
        offset += 4 + colorModeDataLength;

        if (offset + 4 > _buffer.Length)
        {
            return names;
        }

        var imageResourcesLength = _buffer.ReadI32At(offset, true);
        offset += 4 + imageResourcesLength;

        if (offset + 4 > _buffer.Length)
        {
            return names;
        }

        var layerMaskInfoLength = _buffer.ReadI32At(offset, true);
        var layerMaskInfoStart = offset + 4;
        var layerMaskInfoEnd = layerMaskInfoStart + layerMaskInfoLength;

        if (layerMaskInfoLength == 0 || layerMaskInfoEnd > _buffer.Length)
        {
            return names;
        }

        var layerInfoLengthBytes = 4;

        if (layerMaskInfoLength >= PsdConstants.ExtendedLengthMarker - 4)
        {
            layerInfoLengthBytes = 8;
        }

        var layerInfoStart = layerMaskInfoStart;
        var layerInfoLength = layerMaskInfoLength;

        if (layerInfoStart + layerInfoLengthBytes > layerMaskInfoEnd)
        {
            return names;
        }

        var layerCount = 0;

        if (layerInfoLengthBytes == 8)
        {
            layerInfoStart += 8;
            layerInfoLength = (int)BinaryPrimitives.ReadUInt64BigEndian(_buffer.Data.Slice(layerMaskInfoStart, 8));
        }
        else
        {
            layerInfoStart += 4;
            layerInfoLength = _buffer.ReadI32At(layerMaskInfoStart, true);
        }

        if (layerInfoStart + 2 > layerMaskInfoEnd)
        {
            return names;
        }

        layerCount = _buffer.ReadI16BE();
        layerCount = (short)Math.Abs((short)layerCount);
        var layerRecordStart = layerInfoStart + 2;

        for (var i = 0; i < layerCount && layerRecordStart < layerMaskInfoEnd; i++)
        {
            if (layerRecordStart + 16 > layerMaskInfoEnd)
            {
                break;
            }

            layerRecordStart += 16;

            if (layerRecordStart + 2 > layerMaskInfoEnd)
            {
                break;
            }

            var channelCount = _buffer.Data.Slice(layerRecordStart, 2);
            var chCount = BinaryPrimitives.ReadUInt16BigEndian(channelCount);
            layerRecordStart += 2 + chCount * 6;

            if (layerRecordStart + 4 > layerMaskInfoEnd)
            {
                break;
            }

            layerRecordStart += 4;

            if (layerRecordStart + 4 > layerMaskInfoEnd)
            {
                break;
            }

            layerRecordStart += 4;

            if (layerRecordStart + 1 > layerMaskInfoEnd)
            {
                break;
            }

            layerRecordStart += 1;

            if (layerRecordStart + 1 > layerMaskInfoEnd)
            {
                break;
            }

            layerRecordStart += 1;

            if (layerRecordStart + 4 > layerMaskInfoEnd)
            {
                break;
            }

            var extraDataLength = (uint)_buffer.ReadI32At(layerRecordStart, true);
            layerRecordStart += 4;
            var extraDataEnd = layerRecordStart + (int)extraDataLength;

            if (extraDataEnd > layerMaskInfoEnd)
            {
                break;
            }

            if (layerRecordStart + 4 <= extraDataEnd)
            {
                var layerMaskDataLength = (uint)_buffer.ReadI32At(layerRecordStart, true);
                layerRecordStart += 4 + (int)layerMaskDataLength;
            }

            if (layerRecordStart + 4 <= extraDataEnd)
            {
                var blendingRangesLength = (uint)_buffer.ReadI32At(layerRecordStart, true);
                layerRecordStart += 4 + (int)blendingRangesLength;
            }

            if (layerRecordStart + 1 <= extraDataEnd)
            {
                var nameLength = _buffer.ReadU8At(layerRecordStart);
                layerRecordStart++;

                if (layerRecordStart + nameLength <= extraDataEnd)
                {
                    var nameBytes = _buffer.Data.Slice(layerRecordStart, nameLength);
                    var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                    names.Add(name);
                    layerRecordStart += nameLength;
                }
            }

            layerRecordStart = extraDataEnd;
        }

        return names;
    }

    /// <summary>
    ///     扫描 PSD 文件，统计图层数量。
    /// </summary>
    /// <returns>图层数量。</returns>
    public int ScanLayerCount()
    {
        return ScanLayerNames().Count;
    }
}

/// <summary>
///     PSD 文件头信息。
/// </summary>
public sealed class PsdHeader
{
    /// <summary>
    ///     文件版本（始终为 1）。
    /// </summary>
    public ushort Version { get; init; }

    /// <summary>
    ///     通道数量。
    /// </summary>
    public ushort Channels { get; init; }

    /// <summary>
    ///     图像高度（像素）。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     图像宽度（像素）。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     颜色深度（每通道位数）。
    /// </summary>
    public ushort Depth { get; init; }

    /// <summary>
    ///     颜色模式。
    /// </summary>
    public ushort ColorMode { get; init; }

    /// <summary>
    ///     颜色模式名称。
    /// </summary>
    public string ColorModeName => ColorMode switch
    {
        (ushort)PsdColorMode.Bitmap => "位图",
        (ushort)PsdColorMode.Grayscale => "灰度",
        (ushort)PsdColorMode.Indexed => "索引色",
        (ushort)PsdColorMode.Rgb => "RGB",
        (ushort)PsdColorMode.Cmyk => "CMYK",
        (ushort)PsdColorMode.Multichannel => "多通道",
        (ushort)PsdColorMode.Duotone => "双色调",
        (ushort)PsdColorMode.Lab => "Lab",
        _ => $"未知({ColorMode})"
    };
}

using System.Text;
using Acorn.Frame;
using Acorn.Psd.Data;

namespace Acorn.Psd.Decode;

/// <summary>
///     PSD 文件解码器，将 Adobe Photoshop 文档格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     PSD 是 Adobe Photoshop 的原生文件格式，支持图层、通道、蒙版等高级图像编辑功能。
///     解码器解析 PSD 文件头、颜色模式数据、图像资源、图层和蒙版信息以及图像数据。
/// </remarks>
public ref struct PsdDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="PsdDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">PSD 二进制数据。</param>
    public PsdDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 PSD 文件。
    /// </summary>
    /// <returns>PSD 图像数据。</returns>
    public PsdImageData Decode()
    {
        var (width, height, channels, depth, colorMode) = ReadFileHeader();
        SkipColorModeData();
        SkipImageResources();
        var layers = ReadLayerAndMaskInfo();
        var mergedImageData = ReadImageData();

        return new PsdImageData
        {
            Width = (int)width,
            Height = (int)height,
            Channels = channels,
            Depth = depth,
            ColorMode = colorMode,
            Layers = layers,
            MergedImageData = mergedImageData
        };
    }

    /// <summary>
    ///     仅解码 PSD 文件头信息。
    /// </summary>
    /// <returns>包含宽度、高度、通道数、深度和颜色模式的元组。</returns>
    public (int Width, int Height, int Channels, int Depth, int ColorMode) DecodeHeader()
    {
        var (width, height, channels, depth, colorMode) = ReadFileHeader();
        return ((int)width, (int)height, channels, depth, colorMode);
    }

    #region 私有解析方法

    private (uint width, uint height, int channels, int depth, int colorMode) ReadFileHeader()
    {
        var signature = _buffer.ReadString(4);

        if (signature != "8BPS")
        {
            throw new InvalidDataException($"PSD 文件签名无效，期望 \"8BPS\"，实际 \"{signature}\"");
        }

        var version = _buffer.ReadU16BE();

        _buffer.Advance(6);

        var channels = _buffer.ReadU16BE();
        var height = _buffer.ReadU32BE();
        var width = _buffer.ReadU32BE();
        var depth = _buffer.ReadU16BE();
        var colorMode = _buffer.ReadU16BE();

        return (width, height, channels, depth, colorMode);
    }

    private void SkipColorModeData()
    {
        var length = _buffer.ReadU32BE();

        if (length > 0)
        {
            _buffer.Advance((int)length);
        }
    }

    private void SkipImageResources()
    {
        var sectionLength = _buffer.ReadU32BE();

        if (sectionLength > 0)
        {
            _buffer.Advance((int)sectionLength);
        }
    }

    private List<PsdLayer> ReadLayerAndMaskInfo()
    {
        var layers = new List<PsdLayer>();
        var infoLength = _buffer.ReadU32BE();

        if (infoLength == 0)
        {
            return layers;
        }

        var infoEnd = _buffer.Position + (int)infoLength;

        var layerInfoLength = _buffer.ReadU32BE();
        var layerInfoEnd = _buffer.Position + (int)layerInfoLength;

        if (layerInfoLength > 0)
        {
            var layerCount = _buffer.ReadI16BE();

            for (var i = 0; i < Math.Abs(layerCount); i++)
            {
                layers.Add(ReadLayer());
            }

            for (var i = 0; i < layers.Count; i++)
            {
                SkipChannelImageData(layers[i]);
            }
        }

        _buffer.Position = infoEnd;

        return layers;
    }

    private PsdLayer ReadLayer()
    {
        var top = _buffer.ReadI32BE();
        var left = _buffer.ReadI32BE();
        var bottom = _buffer.ReadI32BE();
        var right = _buffer.ReadI32BE();
        var channelCount = _buffer.ReadU16BE();

        for (var i = 0; i < channelCount; i++)
        {
            _buffer.Advance(2);
            var dataLength = _buffer.ReadU32BE();
        }

        var blendModeSignature = _buffer.ReadString(4);

        if (blendModeSignature != "8BIM")
        {
            throw new InvalidDataException($"PSD 图层混合模式签名无效，期望 \"8BIM\"，实际 \"{blendModeSignature}\"");
        }

        var blendMode = _buffer.ReadString(4);
        var opacity = _buffer.ReadU8();
        var clipping = _buffer.ReadU8();
        var flags = _buffer.ReadU8();
        _buffer.Advance(1);

        var isVisible = (flags & 0x02) == 0;

        var extraDataLength = _buffer.ReadU32BE();
        var extraDataEnd = _buffer.Position + (int)extraDataLength;

        var name = ReadPascalString();

        _buffer.Position = extraDataEnd;

        return new PsdLayer
        {
            Name = name,
            Bounds = (top, left, bottom, right),
            ChannelCount = channelCount,
            BlendMode = blendMode,
            Opacity = opacity,
            IsVisible = isVisible
        };
    }

    private void SkipChannelImageData(PsdLayer layer)
    {
        for (var i = 0; i < layer.ChannelCount; i++)
        {
            var compression = _buffer.ReadU16BE();

            var rowSize = (layer.Bounds.Right - layer.Bounds.Left) * (layer.Bounds.Bottom - layer.Bounds.Top);
            var dataLength = compression switch
            {
                0 => rowSize,
                1 => rowSize,
                _ => rowSize
            };

            if (dataLength > 0)
            {
                _buffer.Advance(dataLength);
            }
        }
    }

    private byte[] ReadImageData()
    {
        var compression = _buffer.ReadU16BE();

        if (_buffer.IsEnd)
        {
            return [];
        }

        return _buffer.ReadBytes(_buffer.Remaining).ToArray();
    }

    private string ReadPascalString()
    {
        var length = _buffer.ReadU8();

        if (length == 0)
        {
            if ((_buffer.Position & 1) == 1)
            {
                _buffer.Advance(1);
            }

            return string.Empty;
        }

        var str = _buffer.ReadString(length);

        if ((length + 1) % 2 != 0)
        {
            _buffer.Advance(1);
        }

        return str;
    }

    #endregion
}

using Acorn.Gif.Data;

namespace Acorn.Gif.Scan;

/// <summary>
///     GIF 帧扫描器——快速扫描 GIF 文件元数据，无需解码像素数据
/// </summary>
/// <remarks>
///     扫描逻辑屏幕描述符、帧位置/尺寸/延迟、调色板信息、循环次数等。
///     不执行 LZW 解压，适用于快速获取 GIF 文件结构和元信息。
/// </remarks>
public ref struct GifScanner
{
    private ReadOnlySpan<byte> _data;
    private int _position;

    /// <summary>
    ///     初始化 GIF 扫描器
    /// </summary>
    /// <param name="data">GIF 二进制数据</param>
    public GifScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    ///     扫描 GIF 文件头部信息
    /// </summary>
    /// <returns>GIF 扫描头部数据</returns>
    public GifScanHeader Scan()
    {
        if (_data.Length < GifConstants.SignatureLength + GifConstants.LogicalScreenDescriptorSize)
        {
            return new GifScanHeader { IsValid = false };
        }

        var version = _data.Slice(0, GifConstants.SignatureLength);
        var isValid = version.SequenceEqual(GifConstants.Signature87a) ||
                      version.SequenceEqual(GifConstants.Signature89a);

        if (!isValid)
        {
            return new GifScanHeader { IsValid = false };
        }

        _position = GifConstants.SignatureLength;

        var width = ReadU16();
        var height = ReadU16();
        var packed = _data[_position++];
        var backgroundColorIndex = _data[_position++];
        var pixelAspectRatio = _data[_position++];

        var hasGct = (packed & 0x80) != 0;
        var gctSizeBits = packed & 0x07;
        var gctSize = hasGct ? 3 * (1 << (gctSizeBits + 1)) : 0;
        _position += gctSize;

        var frames = new List<GifScanFrame>();
        var loopCount = -1;

        while (_position < _data.Length)
        {
            var blockType = _data[_position];

            switch (blockType)
            {
                case GifConstants.ImageSeparator:
                    _position++;
                    var frame = ScanImageDescriptor();
                    frames.Add(frame);
                    break;
                case GifConstants.ExtensionIntroducer:
                    _position++;
                    ScanExtension(ref loopCount);
                    break;
                case GifConstants.Trailer:
                    goto done;
                default:
                    _position++;
                    break;
            }
        }

        done:
        return new GifScanHeader
        {
            IsValid = true,
            Width = width,
            Height = height,
            HasGlobalColorTable = hasGct,
            GlobalColorTableSizeBits = gctSizeBits,
            BackgroundColorIndex = backgroundColorIndex,
            PixelAspectRatio = pixelAspectRatio,
            LoopCount = loopCount,
            Frames = frames
        };
    }

    #region 图像描述符扫描

    private GifScanFrame ScanImageDescriptor()
    {
        var left = ReadU16();
        var top = ReadU16();
        var width = ReadU16();
        var height = ReadU16();
        var packed = _data[_position++];

        var hasLct = (packed & 0x80) != 0;
        var interlaced = (packed & 0x40) != 0;
        var lctSizeBits = packed & 0x07;
        var lctSize = hasLct ? 3 * (1 << (lctSizeBits + 1)) : 0;
        _position += lctSize;

        if (_position < _data.Length)
        {
            _position++;
        }

        SkipSubBlocks();

        return new GifScanFrame
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            HasLocalColorTable = hasLct,
            LocalColorTableSizeBits = lctSizeBits,
            Interlaced = interlaced,
            DelayCentiseconds = _scanDelay,
            DisposalMethod = _scanDisposal,
            HasTransparentColor = _scanHasTransparent,
            TransparentColorIndex = _scanTransparentIndex
        };
    }

    #endregion

    #region 扩展块扫描

    private int _scanDelay;
    private GifDisposalMethod _scanDisposal;
    private bool _scanHasTransparent;
    private int _scanTransparentIndex;

    private void ScanExtension(ref int loopCount)
    {
        if (_position >= _data.Length) return;

        var label = _data[_position++];

        switch (label)
        {
            case GifConstants.GraphicControlLabel:
                ScanGraphicControlExtension();
                break;
            case GifConstants.ApplicationExtensionLabel:
                ScanApplicationExtension(ref loopCount);
                break;
            default:
                SkipSubBlocks();
                break;
        }
    }

    private void ScanGraphicControlExtension()
    {
        var blockSize = _data[_position++];
        if (blockSize != 4)
        {
            _position += blockSize;
            if (_position < _data.Length && _data[_position] == 0) _position++;
            return;
        }

        var packed = _data[_position++];
        _scanDelay = ReadU16();
        _scanTransparentIndex = _data[_position++];
        _scanDisposal = (GifDisposalMethod)((packed >> 2) & 0x07);
        _scanHasTransparent = (packed & 0x01) != 0;

        if (_position < _data.Length && _data[_position] == 0) _position++;
    }

    private void ScanApplicationExtension(ref int loopCount)
    {
        var blockSize = _data[_position++];
        if (blockSize != 11)
        {
            _position += blockSize;
            SkipSubBlocks();
            return;
        }

        var appId = _data.Slice(_position, 11);
        _position += 11;

        if (appId.SequenceEqual(GifConstants.NetscapeAppId))
        {
            var subBlockSize = _data[_position++];
            if (subBlockSize >= 3)
            {
                _position++;
                loopCount = ReadU16();
                _position += subBlockSize - 3;
            }
            if (_position < _data.Length && _data[_position] == 0) _position++;
        }
        else
        {
            SkipSubBlocks();
        }
    }

    #endregion

    #region 辅助方法

    private int ReadU16()
    {
        var value = _data[_position] | (_data[_position + 1] << 8);
        _position += 2;
        return value;
    }

    private void SkipSubBlocks()
    {
        while (_position < _data.Length)
        {
            var blockSize = _data[_position++];
            if (blockSize == 0) break;
            _position += blockSize;
        }
    }

    #endregion
}

/// <summary>
///     GIF 扫描头部数据
/// </summary>
public sealed class GifScanHeader
{
    /// <summary>
    ///     是否为有效的 GIF 文件
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    ///     逻辑屏幕宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     逻辑屏幕高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     是否有全局调色板
    /// </summary>
    public bool HasGlobalColorTable { get; init; }

    /// <summary>
    ///     全局调色板大小位数
    /// </summary>
    public int GlobalColorTableSizeBits { get; init; }

    /// <summary>
    ///     背景色索引
    /// </summary>
    public byte BackgroundColorIndex { get; init; }

    /// <summary>
    ///     像素宽高比
    /// </summary>
    public byte PixelAspectRatio { get; init; }

    /// <summary>
    ///     循环次数（0 = 无限循环，-1 = 未指定）
    /// </summary>
    public int LoopCount { get; init; }

    /// <summary>
    ///     帧扫描信息列表
    /// </summary>
    public IReadOnlyList<GifScanFrame> Frames { get; init; } = [];
}

/// <summary>
///     GIF 帧扫描数据
/// </summary>
public sealed class GifScanFrame
{
    /// <summary>
    ///     帧左偏移
    /// </summary>
    public int Left { get; init; }

    /// <summary>
    ///     帧上偏移
    /// </summary>
    public int Top { get; init; }

    /// <summary>
    ///     帧宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     帧高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     是否有局部调色板
    /// </summary>
    public bool HasLocalColorTable { get; init; }

    /// <summary>
    ///     局部调色板大小位数
    /// </summary>
    public int LocalColorTableSizeBits { get; init; }

    /// <summary>
    ///     是否隔行扫描
    /// </summary>
    public bool Interlaced { get; init; }

    /// <summary>
    ///     帧延迟时间（1/100 秒）
    /// </summary>
    public int DelayCentiseconds { get; init; }

    /// <summary>
    ///     帧处置方式
    /// </summary>
    public GifDisposalMethod DisposalMethod { get; init; }

    /// <summary>
    ///     是否有透明色
    /// </summary>
    public bool HasTransparentColor { get; init; }

    /// <summary>
    ///     透明色索引
    /// </summary>
    public int TransparentColorIndex { get; init; }
}

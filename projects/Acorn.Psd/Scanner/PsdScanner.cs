using System.Text;
using Acorn.Frame;
using Acorn.Psd.Data;

namespace Acorn.Psd.Scanner;

/// <summary>
///     PSD 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 Adobe Photoshop PSD 文件的快速元信息扫描。
/// </summary>
public ref struct PsdScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="PsdScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 PSD 字节数据。</param>
    public PsdScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 PSD 文件头，提取基本图像信息。
    /// </summary>
    public PsdScanHeader ScanHeader()
    {
        if (_scanner.Length < PsdConstants.HeaderSize)
        {
            throw new InvalidDataException("PSD 文件数据过短，无法读取文件头");
        }

        if (!_scanner.MatchMagic(PsdConstants.MagicNumber))
        {
            throw new InvalidDataException("PSD 文件魔数不匹配");
        }

        _scanner.ConsumeMagic(PsdConstants.MagicNumber);

        var version = _scanner.Buffer.ReadU16BE();

        _scanner.Advance(6);

        var channels = _scanner.Buffer.ReadU16BE();
        var height = _scanner.Buffer.ReadU32BE();
        var width = _scanner.Buffer.ReadU32BE();
        var depth = _scanner.Buffer.ReadU16BE();
        var colorMode = _scanner.Buffer.ReadU16BE();

        return new PsdScanHeader
        {
            Version = version,
            Channels = channels,
            Height = (int)height,
            Width = (int)width,
            Depth = depth,
            ColorMode = (PsdColorMode)colorMode
        };
    }

    /// <summary>
    ///     扫描 PSD 文件，提取图层名称列表。
    /// </summary>
    public List<string> ScanLayerNames()
    {
        var names = new List<string>();

        if (_scanner.Length < PsdConstants.HeaderSize)
        {
            return names;
        }

        _scanner.ConsumeMagic(PsdConstants.MagicNumber);
        _scanner.Advance(PsdConstants.HeaderSize - 4);

        var version = _scanner.Buffer.ReadU16BE();
        _scanner.Advance(6);

        var colorModeDataLength = _scanner.Buffer.ReadU32BE();
        _scanner.Advance((int)colorModeDataLength);

        var imageResourcesLength = _scanner.Buffer.ReadU32BE();
        _scanner.Advance((int)imageResourcesLength);

        var layerAndMaskInfoLength = _scanner.Buffer.ReadU32BE();
        var layerInfoEnd = _scanner.Position + (int)layerAndMaskInfoLength;

        var layerInfoLength = _scanner.Buffer.ReadU32BE();
        var layerCount = _scanner.Buffer.ReadI16BE();

        if (layerCount < 0)
        {
            layerCount = -layerCount;
        }

        for (var i = 0; i < layerCount; i++)
        {
            _scanner.Advance(16);

            var channelCount = _scanner.Buffer.ReadU16BE();

            for (var j = 0; j < channelCount; j++)
            {
                _scanner.Advance(6);
            }

            _scanner.Advance(12);

            var extraFieldLength = _scanner.Buffer.ReadU32BE();
            var extraFieldEnd = _scanner.Position + (int)extraFieldLength;

            _scanner.Advance(8);

            var nameLength = _scanner.Buffer.ReadU8();

            if (nameLength > 0)
            {
                var name = _scanner.Buffer.ReadString(nameLength);
                names.Add(name);
            }

            _scanner.Position = extraFieldEnd;
        }

        return names;
    }

    /// <summary>
    ///     扫描 PSD 文件，获取图层数量。
    /// </summary>
    public int ScanLayerCount()
    {
        if (_scanner.Length < PsdConstants.HeaderSize)
        {
            return 0;
        }

        _scanner.ConsumeMagic(PsdConstants.MagicNumber);
        _scanner.Advance(PsdConstants.HeaderSize - 4);

        var version = _scanner.Buffer.ReadU16BE();
        _scanner.Advance(6);

        var colorModeDataLength = _scanner.Buffer.ReadU32BE();
        _scanner.Advance((int)colorModeDataLength);

        var imageResourcesLength = _scanner.Buffer.ReadU32BE();
        _scanner.Advance((int)imageResourcesLength);

        var layerAndMaskInfoLength = _scanner.Buffer.ReadU32BE();
        var layerInfoLength = _scanner.Buffer.ReadU32BE();
        var layerCount = _scanner.Buffer.ReadI16BE();

        return layerCount < 0 ? -layerCount : layerCount;
    }
}

/// <summary>
///     PSD 扫描头部信息。
/// </summary>
public sealed class PsdScanHeader
{
    /// <summary>
    ///     PSD 版本号（1 = PSD，2 = PSB）。
    /// </summary>
    public ushort Version { get; init; }

    /// <summary>
    ///     通道数量。
    /// </summary>
    public ushort Channels { get; init; }

    /// <summary>
    ///     图像高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     图像宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     位深度。
    /// </summary>
    public ushort Depth { get; init; }

    /// <summary>
    ///     颜色模式。
    /// </summary>
    public PsdColorMode ColorMode { get; init; }

    /// <summary>
    ///     版本名称。
    /// </summary>
    public string VersionName => Version == 1 ? "PSD" : "PSB";
}

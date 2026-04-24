using Acorn.Frame;
using Acorn.Exr.Data;

namespace Acorn.Exr.Scanner;

/// <summary>
///     EXR 扫描器。
/// </summary>
public ref struct ExrScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="ExrScanner" /> 结构的新实例。
    /// </summary>
    public ExrScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 EXR 文件头。
    /// </summary>
    public ExrScanHeader ScanHeader()
    {
        if (_scanner.Length < ExrConstants.HeaderSize)
        {
            return new ExrScanHeader();
        }

        if (!_scanner.MatchMagic(ExrConstants.MagicNumber))
        {
            return new ExrScanHeader();
        }

        _scanner.ConsumeMagic(ExrConstants.MagicNumber);
        var version = _scanner.Buffer.ReadU32LE();

        return new ExrScanHeader
        {
            Version = version,
            MajorVersion = (int)(version & 0xFF),
            IsMultipart = (version & 0x200) != 0,
            IsSinglePart = (version & 0x200) == 0
        };
    }

    /// <summary>
    ///     是否为 EXR 格式。
    /// </summary>
    public bool IsExr()
    {
        return _scanner.Length >= 4 && _scanner.MatchMagic(ExrConstants.MagicNumber);
    }
}

/// <summary>
///     EXR 扫描头部信息。
/// </summary>
public sealed class ExrScanHeader
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     主版本号。
    /// </summary>
    public int MajorVersion { get; init; }

    /// <summary>
    ///     是否为多部分文件。
    /// </summary>
    public bool IsMultipart { get; init; }

    /// <summary>
    ///     是否为单部分文件。
    /// </summary>
    public bool IsSinglePart { get; init; }
}

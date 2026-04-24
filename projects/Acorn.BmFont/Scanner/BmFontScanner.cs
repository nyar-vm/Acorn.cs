using Acorn.Frame;
using Acorn.BmFont.Data;

namespace Acorn.BmFont.Scanner;

/// <summary>
///     BMFont 文件扫描器，基于 <see cref="ByteBuffer" /> 提供对 BMFont 位图字体文件的快速元信息扫描。
/// </summary>
public ref struct BmFontScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="BmFontScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 BMFont 字节数据。</param>
    public BmFontScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     扫描 BMFont 文件头，提取基本字体信息。
    /// </summary>
    public BmFontScanHeader ScanHeader()
    {
        if (_buffer.Length < 4)
        {
            throw new InvalidDataException("BMFont 文件数据过短，无法读取头部");
        }

        if (!_buffer.MatchMagic(BmFontConstants.BinaryMagic))
        {
            throw new InvalidDataException("BMFont 文件魔数不匹配");
        }

        _buffer.ConsumeMagic(BmFontConstants.BinaryMagic);

        var version = _buffer.ReadU8();

        BmFontScanHeader header = new() { Version = version, IsBinary = true };

        while (!_buffer.IsEnd)
        {
            if (_buffer.Remaining < 5)
            {
                break;
            }

            var blockType = _buffer.ReadU8();
            var blockSize = (int)_buffer.ReadU32LE();

            if (blockType == BmFontConstants.BlockInfo && _buffer.Remaining >= 15)
            {
                header.FontSize = _buffer.ReadI16LE();
                var flags = _buffer.ReadU8();
                header.Bold = (flags & 0x01) != 0;
                header.Italic = (flags & 0x02) != 0;
                header.Unicode = (flags & 0x04) != 0;
                break;
            }

            _buffer.Advance(blockSize);
        }

        return header;
    }

    /// <summary>
    ///     快速判断数据是否为 BMFont 二进制格式。
    /// </summary>
    public bool IsBmFontBinary()
    {
        if (_buffer.Length < 3)
        {
            return false;
        }

        return _buffer.MatchMagic(BmFontConstants.BinaryMagic);
    }
}

/// <summary>
///     BMFont 扫描头部信息。
/// </summary>
public sealed class BmFontScanHeader
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public byte Version { get; init; }

    /// <summary>
    ///     是否为二进制格式。
    /// </summary>
    public bool IsBinary { get; init; }

    /// <summary>
    ///     字体大小。
    /// </summary>
    public short FontSize { get; init; }

    /// <summary>
    ///     是否粗体。
    /// </summary>
    public bool Bold { get; init; }

    /// <summary>
    ///     是否斜体。
    /// </summary>
    public bool Italic { get; init; }

    /// <summary>
    ///     是否 Unicode。
    /// </summary>
    public bool Unicode { get; init; }
}

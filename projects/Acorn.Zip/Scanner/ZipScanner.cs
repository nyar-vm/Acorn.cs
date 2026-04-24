using System.Text;
using Acorn.Frame;
using Acorn.Zip.Data;

namespace Acorn.Zip.Scanner;

/// <summary>
///     ZIP 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 ZIP 归档文件的快速元信息扫描。
/// </summary>
public ref struct ZipScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="ZipScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 ZIP 字节数据。</param>
    public ZipScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     验证 ZIP 文件头。
    /// </summary>
    public bool ValidateHeader()
    {
        if (_scanner.Length < 4)
        {
            return false;
        }

        return _scanner.Buffer.ReadU32At(0) == ZipConstants.LocalFileHeaderMagic;
    }

    /// <summary>
    ///     扫描 ZIP 文件，提取统计信息。
    /// </summary>
    public ZipScanStatistics ScanStatistics()
    {
        var stats = new ZipScanStatistics();
        var entryCount = 0;
        var compressedSize = 0L;
        var uncompressedSize = 0L;
        var entryNames = new List<string>();

        var eocdOffset = FindEndOfCentralDirectory();

        if (eocdOffset < 0)
        {
            return stats;
        }

        _scanner.Position = eocdOffset;

        _scanner.Advance(4);
        _scanner.Advance(4);
        _scanner.Advance(4);

        var centralDirEntryCount = _scanner.Buffer.ReadU16LE();
        var centralDirSize = _scanner.Buffer.ReadU32LE();
        var centralDirOffset = _scanner.Buffer.ReadU32LE();

        _scanner.Position = (int)centralDirOffset;

        for (var i = 0; i < centralDirEntryCount; i++)
        {
            if (_scanner.Position + 46 > _scanner.Length)
            {
                break;
            }

            var sig = _scanner.Buffer.ReadU32LE();

            if (sig != ZipConstants.CentralDirectoryHeaderMagic)
            {
                break;
            }

            _scanner.Advance(6);

            var compressionMethod = _scanner.Buffer.ReadU16LE();
            _scanner.Advance(8);

            var cSize = _scanner.Buffer.ReadU32LE();
            var uSize = _scanner.Buffer.ReadU32LE();
            var nameLength = _scanner.Buffer.ReadU16LE();
            var extraLength = _scanner.Buffer.ReadU16LE();
            var commentLength = _scanner.Buffer.ReadU16LE();

            _scanner.Advance(8);

            var localHeaderOffset = _scanner.Buffer.ReadU32LE();

            var name = _scanner.Buffer.ReadString(nameLength);
            entryNames.Add(name);

            compressedSize += cSize;
            uncompressedSize += uSize;
            entryCount++;

            _scanner.Advance(extraLength + commentLength);
        }

        stats.EntryCount = entryCount;
        stats.CompressedSize = compressedSize;
        stats.UncompressedSize = uncompressedSize;
        stats.EntryNames = entryNames;

        return stats;
    }

    private int FindEndOfCentralDirectory()
    {
        var searchStart = Math.Max(0, _scanner.Length - ZipConstants.MaxEOCDSearchSize);

        for (var pos = _scanner.Length - 22; pos >= searchStart; pos--)
        {
            if (_scanner.Buffer.ReadU32At(pos) == ZipConstants.EndOfCentralDirectoryMagic)
            {
                return pos;
            }
        }

        return -1;
    }
}

/// <summary>
///     ZIP 扫描统计信息。
/// </summary>
public sealed class ZipScanStatistics
{
    /// <summary>
    ///     条目数量。
    /// </summary>
    public int EntryCount { get; set; }

    /// <summary>
    ///     压缩总大小。
    /// </summary>
    public long CompressedSize { get; set; }

    /// <summary>
    ///     未压缩总大小。
    /// </summary>
    public long UncompressedSize { get; set; }

    /// <summary>
    ///     条目名称列表。
    /// </summary>
    public IReadOnlyList<string> EntryNames { get; set; } = [];
}

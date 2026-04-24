using System.Text;
using Acorn.Frame;
using Acorn.Office.Data;

namespace Acorn.Office.Scanner;

/// <summary>
///     XLS 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 Excel 二进制格式（BIFF）文件的快速元信息扫描。
/// </summary>
public ref struct XlsScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="XlsScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 XLS 字节数据。</param>
    public XlsScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     验证 XLS 文件头。
    /// </summary>
    public bool ValidateHeader()
    {
        if (_scanner.Length < XlsConstants.HeaderSize)
        {
            return false;
        }

        return _scanner.MatchMagic(XlsConstants.MagicNumber);
    }

    /// <summary>
    ///     扫描 XLS 文件，提取统计信息。
    /// </summary>
    public XlsScanStatistics ScanStatistics()
    {
        var stats = new XlsScanStatistics();

        if (!ValidateHeader())
        {
            return stats;
        }

        _scanner.ConsumeMagic(XlsConstants.MagicNumber);

        while (!_scanner.IsEnd && _scanner.Position + 4 <= _scanner.Length)
        {
            var recordType = _scanner.Buffer.ReadU16LE();
            var recordSize = _scanner.Buffer.ReadU16LE();

            switch (recordType)
            {
                case XlsRecordType.BOF:
                    if (recordSize >= 2)
                    {
                        var biffVersion = _scanner.Buffer.ReadU16LE();
                        stats.BiffVersion = biffVersion;
                    }

                    _scanner.Advance(recordSize - (recordSize >= 2 ? 2 : 0));
                    break;
                case XlsRecordType.SheetName:
                    stats.SheetCount++;
                    _scanner.Advance(recordSize);
                    break;
                case XlsRecordType.EOF:
                    return stats;
                default:
                    _scanner.Advance(recordSize);
                    break;
            }
        }

        return stats;
    }
}

/// <summary>
///     XLS 扫描统计信息。
/// </summary>
public sealed class XlsScanStatistics
{
    /// <summary>
    ///     BIFF 版本号。
    /// </summary>
    public ushort BiffVersion { get; set; }

    /// <summary>
    ///     工作表数量。
    /// </summary>
    public int SheetCount { get; set; }

    /// <summary>
    ///     BIFF 版本名称。
    /// </summary>
    public string BiffVersionName => BiffVersion switch
    {
        0x0600 => "BIFF8 (Excel 97-2003)",
        0x0500 => "BIFF5 (Excel 5.0/95)",
        0x0400 => "BIFF4 (Excel 4.0)",
        0x0300 => "BIFF3 (Excel 3.0)",
        0x0200 => "BIFF2 (Excel 2.0)",
        _ => $"0x{BiffVersion:X4}"
    };
}

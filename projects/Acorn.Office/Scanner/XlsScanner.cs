using System.Buffers.Binary;
using System.Text;
using Acorn.Frame;
using Acorn.Office.Data;

namespace Acorn.Office.Scanner;

/// <summary>
///     XLS 文件扫描器，基于 <see cref="ByteBuffer" /> 提供对 Excel 97-2003 (.xls) 文件的快速结构扫描。
/// </summary>
public ref struct XlsScanner
{
    private ByteBuffer _buffer;

    public XlsScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public int Length => _buffer.Length;

    public bool IsEndOfData => _buffer.IsEnd;

    /// <summary>
    ///     验证 XLS 文件头。
    /// </summary>
    public bool ValidateHeader()
    {
        return _buffer.MatchMagic(OfficeConstants.Ole2MagicNumber);
    }

    /// <summary>
    ///     扫描 XLS 文件，提取 BIFF 记录统计信息。
    /// </summary>
    public XlsStatistics ScanStatistics()
    {
        var stats = new XlsStatistics();

        if (!ValidateHeader())
        {
            return stats;
        }

        _buffer.Position = 512;

        while (_buffer.Position + 4 <= _buffer.Length)
        {
            var recordType = _buffer.ReadU16LE();
            var recordLength = _buffer.ReadU16LE();

            if (_buffer.Position + recordLength > _buffer.Length)
            {
                break;
            }

            switch (recordType)
            {
                case 0x0009:
                    stats.SheetCount++;
                    break;
                case 0x0018:
                    if (recordLength > 0)
                    {
                        var nameLen = _buffer.ReadU8();
                        if (nameLen > 0 && nameLen < recordLength)
                        {
                            stats.SheetNames.Add(_buffer.ReadString(nameLen));
                        }

                        _buffer.Advance(recordLength - 1 - (nameLen > 0 && nameLen < recordLength ? nameLen : 0));
                    }

                    break;
                case 0x0208:
                    stats.RowCount++;
                    _buffer.Advance(recordLength);
                    break;
                case 0x0203:
                case 0x027E:
                    stats.NumericCellCount++;
                    _buffer.Advance(recordLength);
                    break;
                case 0x0006:
                    stats.FormulaCount++;
                    _buffer.Advance(recordLength);
                    break;
                case 0x00FD:
                case 0x0204:
                    stats.StringCellCount++;
                    _buffer.Advance(recordLength);
                    break;
                default:
                    _buffer.Advance(recordLength);
                    break;
            }
        }

        return stats;
    }
}

/// <summary>
///     XLS 文件统计信息。
/// </summary>
public sealed class XlsStatistics
{
    public int SheetCount { get; set; }
    public int RowCount { get; set; }
    public int NumericCellCount { get; set; }
    public int StringCellCount { get; set; }
    public int FormulaCount { get; set; }
    public List<string> SheetNames { get; set; } = new();
}

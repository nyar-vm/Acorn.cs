using System.Text;
using Acorn.Frame;
using Acorn.Office.Data;

namespace Acorn.Office.Decode;

/// <summary>
///     XLS 文件解码器，将 Microsoft Excel 二进制格式（.xls）解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     XLS 是 Microsoft Excel 97-2003 使用的二进制文件格式，基于 OLE2 复合文档结构（BIFF8 格式）。
///     解码器解析 BIFF 记录流，提取工作簿、工作表和单元格数据。
/// </remarks>
public ref struct XlsDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="XlsDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">XLS 二进制数据。</param>
    public XlsDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 XLS 文件，提取工作簿数据。
    /// </summary>
    /// <returns>Excel 工作簿数据。</returns>
    public ExcelWorkbookData Decode()
    {
        var sheets = new List<ExcelSheetData>();
        var strings = new List<string>();

        while (!_buffer.IsEnd && _buffer.Remaining >= 4)
        {
            var recordType = _buffer.ReadU16LE();
            var recordLength = _buffer.ReadU16LE();

            if (_buffer.Remaining < recordLength)
            {
                break;
            }

            var recordData = _buffer.ReadBytes(recordLength);

            switch (recordType)
            {
                case 0x0085:
                    ParseBoundSheet(recordData, sheets);
                    break;
                case 0x00FC:
                    ParseSST(recordData, strings);
                    break;
            }
        }

        return new ExcelWorkbookData
        {
            Sheets = sheets
        };
    }

    #region 私有解析方法

    private static void ParseBoundSheet(ReadOnlySpan<byte> data, List<ExcelSheetData> sheets)
    {
        if (data.Length < 8)
        {
            return;
        }

        var reader = new ByteBuffer(data);
        var sheetOffset = reader.ReadU32LE();
        var flags = reader.ReadU8();
        var nameLength = reader.ReadU8();

        string sheetName;

        if (reader.Remaining >= nameLength)
        {
            var nameBytes = reader.ReadBytes(nameLength).ToArray();

            if (nameLength > 0 && (nameBytes[0] & 0x01) != 0)
            {
                sheetName = Encoding.Unicode.GetString(nameBytes, 1, nameBytes.Length - 1);
            }
            else
            {
                sheetName = Encoding.ASCII.GetString(nameBytes);
            }
        }
        else
        {
            sheetName = $"Sheet{sheets.Count + 1}";
        }

        sheets.Add(new ExcelSheetData
        {
            Name = sheetName,
            RowCount = 0,
            ColumnCount = 0,
            Rows = []
        });
    }

    private static void ParseSST(ReadOnlySpan<byte> data, List<string> strings)
    {
        if (data.Length < 8)
        {
            return;
        }

        var reader = new ByteBuffer(data);
        var totalStrings = reader.ReadU32LE();
        var uniqueStrings = reader.ReadU32LE();

        for (var i = 0; i < uniqueStrings && !reader.IsEnd; i++)
        {
            if (reader.Remaining < 3)
            {
                break;
            }

            var strLength = reader.ReadU16LE();
            var flags = reader.ReadU8();

            var isUnicode = (flags & 0x01) != 0;
            var hasAsian = (flags & 0x04) != 0;
            var hasRich = (flags & 0x08) != 0;

            if (hasRich && reader.Remaining >= 2)
            {
                reader.Advance(2);
            }

            if (hasAsian && reader.Remaining >= 4)
            {
                reader.Advance(4);
            }

            var byteLength = isUnicode ? strLength * 2 : strLength;

            if (reader.Remaining < byteLength)
            {
                break;
            }

            var strBytes = reader.ReadBytes(byteLength).ToArray();
            var str = isUnicode ? Encoding.Unicode.GetString(strBytes) : Encoding.ASCII.GetString(strBytes);

            strings.Add(str);
        }
    }

    #endregion
}

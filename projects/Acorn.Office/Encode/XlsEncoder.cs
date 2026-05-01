using System.Text;
using Acorn.Frame;
using Acorn.Office.Data;

namespace Acorn.Office.Encode;

/// <summary>
///     XLS 文件编码器，将 C# 数据结构编码为 Microsoft Excel 二进制格式（.xls）。
/// </summary>
/// <remarks>
///     XLS 是 Microsoft Excel 97-2003 使用的二进制文件格式，基于 OLE2 复合文档结构（BIFF8 格式）。
///     编码器生成符合 BIFF8 规范的二进制数据。
/// </remarks>
public sealed class XlsEncoder
{
    /// <summary>
    ///     将 Excel 工作簿数据编码为 XLS 二进制格式。
    /// </summary>
    /// <param name="data">Excel 工作簿数据。</param>
    /// <returns>XLS 二进制数据。</returns>
    public byte[] Encode(ExcelWorkbookData data)
    {
        var writer = new ByteBufferWriter(256);

        WriteBOF(ref writer, OfficeConstants.XlsRecordType.BofTypeWorkbook);
        WriteWriteAccess(ref writer);
        WriteCodePage(ref writer);
        WriteDSF(ref writer);

        foreach (var sheet in data.Sheets)
        {
            WriteBoundSheet(ref writer, sheet);
        }

        WriteEOF(ref writer);

        return writer.ToArray();
    }

    #region 私有编码方法

    private static void WriteBOF(ref ByteBufferWriter writer, ushort biffType)
    {
        writer.WriteU16LE(OfficeConstants.XlsRecordType.Bof8);
        writer.WriteU16LE(16);
        writer.WriteU16LE(OfficeConstants.XlsRecordType.BiffVersion);
        writer.WriteU16LE(biffType);
        writer.WriteU16LE(OfficeConstants.XlsRecordType.BuildYear);
        writer.WriteU16LE(OfficeConstants.XlsRecordType.BuildIdentifier);
        writer.WriteU32LE(0x00000000);
        writer.WriteU32LE(0x00000000);
    }

    private static void WriteWriteAccess(ref ByteBufferWriter writer)
    {
        writer.WriteU16LE(OfficeConstants.XlsRecordType.WriteAccess);
        writer.WriteU16LE(112);

        var userName = Encoding.ASCII.GetBytes("Acorn.Xls");
        writer.Write(userName);

        var padding = 112 - userName.Length;

        for (var i = 0; i < padding; i++)
        {
            writer.WriteU8((byte)' ');
        }
    }

    private static void WriteCodePage(ref ByteBufferWriter writer)
    {
        writer.WriteU16LE(OfficeConstants.XlsRecordType.CodePage);
        writer.WriteU16LE(2);
        writer.WriteU16LE(OfficeConstants.XlsRecordType.CodePageUtf16LE);
    }

    private static void WriteDSF(ref ByteBufferWriter writer)
    {
        writer.WriteU16LE(OfficeConstants.XlsRecordType.Dsf);
        writer.WriteU16LE(2);
        writer.WriteU16LE(0x0000);
    }

    private static void WriteBoundSheet(ref ByteBufferWriter writer, ExcelSheetData sheet)
    {
        var nameBytes = Encoding.Unicode.GetBytes(sheet.Name);
        var recordLength = 4 + 1 + 1 + 1 + 1 + nameBytes.Length;

        writer.WriteU16LE(OfficeConstants.XlsRecordType.BoundSheet);
        writer.WriteU16LE((ushort)recordLength);
        writer.WriteU32LE(0);
        writer.WriteU8(0);
        writer.WriteU8(0);
        writer.WriteU8((byte)sheet.Name.Length);
        writer.WriteU8(OfficeConstants.XlsRecordType.SheetStateVisible);
        writer.Write(nameBytes);
    }

    private static void WriteEOF(ref ByteBufferWriter writer)
    {
        writer.WriteU16LE(OfficeConstants.XlsRecordType.Eof);
        writer.WriteU16LE(0);
    }

    #endregion
}

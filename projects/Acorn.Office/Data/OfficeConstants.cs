namespace Acorn.Office.Data;

/// <summary>
///     Office 97-2003 二进制格式常量。
/// </summary>
public static class OfficeConstants
{
    /// <summary>
    ///     OLE2 Compound Document 魔数。
    /// </summary>
    /// <remarks>
    ///     XLS、DOC、PPT 等 Office 97-2003 文件均使用此魔数。
    /// </remarks>
    public static ReadOnlySpan<byte> Ole2MagicNumber => new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
    
    /// <summary>
    ///     XLS BIFF 记录类型。
    /// </summary>
    public static class XlsRecordType
    {
        /// <summary>
        ///     Beginning of File（BIFF2-7）。
        /// </summary>
        public const ushort Bof = 0x0009;

        /// <summary>
        ///     Beginning of File（BIFF8 Workbook）。
        /// </summary>
        public const ushort Bof8 = 0x0809;

        /// <summary>
        ///     文件类型 — 工作簿全局（BIFF BOFTYPE）。
        /// </summary>
        public const ushort BofTypeWorkbook = 0x0005;
        
        /// <summary>
        ///     End of File。
        /// </summary>
        public const ushort Eof = 0x000A;
        
        /// <summary>
        ///     公式。
        /// </summary>
        public const ushort Formula = 0x0006;
        
        /// <summary>
        ///     标签/名称。
        /// </summary>
        public const ushort Label = 0x0018;
        
        /// <summary>
        ///     数字。
        /// </summary>
        public const ushort Number = 0x0203;
        
        /// <summary>
        ///     字符串标签。
        /// </summary>
        public const ushort LabelSst = 0x00FD;
        
        /// <summary>
        ///     行。
        /// </summary>
        public const ushort Row = 0x0208;
        
        /// <summary>
        ///     RK 值（紧凑数字）。
        /// </summary>
        public const ushort Rk = 0x027E;

        /// <summary>
        ///     工作表绑定记录。
        /// </summary>
        public const ushort BoundSheet = 0x0085;

        /// <summary>
        ///     共享字符串表。
        /// </summary>
        public const ushort Sst = 0x00FC;

        /// <summary>
        ///     工作簿 BOF（BIFF8 Workbook）。
        /// </summary>
        public const ushort BofWorkbook = 0x0809;

        /// <summary>
        ///     BIFF 版本。
        /// </summary>
        public const ushort BiffVersion = 0x0600;

        /// <summary>
        ///     构建年份。
        /// </summary>
        public const ushort BuildYear = 0x09CD;

        /// <summary>
        ///     构建标识符。
        /// </summary>
        public const ushort BuildIdentifier = 0x07C9;

        /// <summary>
        ///     写入访问记录。
        /// </summary>
        public const ushort WriteAccess = 0x005C;

        /// <summary>
        ///     代码页记录。
        /// </summary>
        public const ushort CodePage = 0x0042;

        /// <summary>
        ///     UTF-16LE 代码页值。
        /// </summary>
        public const ushort CodePageUtf16LE = 0x04E4;

        /// <summary>
        ///     双精度存储文件记录。
        /// </summary>
        public const ushort Dsf = 0x0161;

        /// <summary>
        ///     工作表状态 — 可见。
        /// </summary>
        public const byte SheetStateVisible = 0x01;
    }

    /// <summary>
    ///     Word DOC 流类型。
    /// </summary>
    public static class WordStreamType
    {
        /// <summary>
        ///     Grpprl 属性列表。
        /// </summary>
        public const byte Grpprl = 0x01;

        /// <summary>
        ///     PieceTable 片段表。
        /// </summary>
        public const byte PieceTable = 0x02;
    }

    /// <summary>
    ///     Word DOC 格式常量。
    /// </summary>
    public static class WordDoc
    {
        /// <summary>
        ///     Word 二进制文件标识符（wIdent）。
        /// </summary>
        public const ushort FileMagic = 0xA5EC;

        /// <summary>
        ///     CLX 偏移量在文件中的位置。
        /// </summary>
        public const uint ClxOffsetPosition = 0x00A2;
    }
    
    /// <summary>
    ///     PPT 记录类型。
    /// </summary>
    public static class PptRecordType
    {
        /// <summary>
        ///     幻灯片记录。
        /// </summary>
        public const ushort Slide = 0x03EE;
        
        /// <summary>
        ///     文本字符记录。
        /// </summary>
        public const ushort TextCharsAtom = 0x0FBA;
        
        /// <summary>
        ///     文本字节记录。
        /// </summary>
        public const ushort TextBytesAtom = 0x0FBC;
    }
}
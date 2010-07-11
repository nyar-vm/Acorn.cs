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
        ///     Beginning of File。
        /// </summary>
        public const ushort Bof = 0x0009;
        
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
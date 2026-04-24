namespace Acorn.Zip.Data;

/// <summary>
///     ZIP 归档格式常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 PKWARE APPNOTE 规范，Acorn 独占二进制编解码职责。
/// </remarks>
public static class ZipConstants
{
    /// <summary>
    ///     本地文件头魔数（0x04034b50，"PK\x03\x04"）。
    /// </summary>
    public const uint LocalFileHeaderMagic = 0x04034b50;

    /// <summary>
    ///     中央目录文件头魔数（0x02014b50，"PK\x01\x02"）。
    /// </summary>
    public const uint CentralDirectoryHeaderMagic = 0x02014b50;

    /// <summary>
    ///     中央目录结束记录魔数（0x06054b50，"PK\x05\x06"）。
    /// </summary>
    public const uint EndOfCentralDirectoryMagic = 0x06054b50;

    /// <summary>
    ///     EOCD 记录最大搜索范围（64KB）。
    /// </summary>
    public const int MaxEOCDSearchSize = 65536;
}

namespace Acorn.Gif.Data;

/// <summary>
///     GIF 图像格式常量
/// </summary>
public static class GifConstants
{
    /// <summary>
    ///     GIF87a 签名
    /// </summary>
    public static ReadOnlySpan<byte> Signature87a => "GIF87a"u8;

    /// <summary>
    ///     GIF89a 签名
    /// </summary>
    public static ReadOnlySpan<byte> Signature89a => "GIF89a"u8;

    /// <summary>
    ///     签名长度
    /// </summary>
    public const int SignatureLength = 6;

    /// <summary>
    ///     逻辑屏幕描述符大小
    /// </summary>
    public const int LogicalScreenDescriptorSize = 7;

    /// <summary>
    ///     图像描述符大小
    /// </summary>
    public const int ImageDescriptorSize = 10;

    /// <summary>
    ///     最大调色板大小
    /// </summary>
    public const int MaxPaletteSize = 256;

    /// <summary>
    ///     LZW 最小码大小下限
    /// </summary>
    public const int MinLzwCodeSize = 2;

    /// <summary>
    ///     LZW 最大码大小上限
    /// </summary>
    public const int MaxLzwCodeSize = 12;

    /// <summary>
    ///     块终止符
    /// </summary>
    public const byte BlockTerminator = 0x00;

    /// <summary>
    ///     图像分隔符
    /// </summary>
    public const byte ImageSeparator = 0x2C;

    /// <summary>
    ///     扩展引入符
    /// </summary>
    public const byte ExtensionIntroducer = 0x21;

    /// <summary>
    ///     尾部标记
    /// </summary>
    public const byte Trailer = 0x3B;

    /// <summary>
    ///     图形控制扩展标签
    /// </summary>
    public const byte GraphicControlLabel = 0xF9;

    /// <summary>
    ///     应用扩展标签
    /// </summary>
    public const byte ApplicationExtensionLabel = 0xFF;

    /// <summary>
    ///     注释扩展标签
    /// </summary>
    public const byte CommentExtensionLabel = 0xFE;

    /// <summary>
    ///     纯文本扩展标签
    /// </summary>
    public const byte PlainTextLabel = 0x01;

    /// <summary>
    ///     Netscape 应用标识符
    /// </summary>
    public static ReadOnlySpan<byte> NetscapeAppId => "NETSCAPE2.0"u8;

    /// <summary>
    ///     计算调色板大小（字节数）
    /// </summary>
    /// <param name="packedField">逻辑屏幕描述符的打包字段</param>
    /// <returns>调色板字节数</returns>
    public static int GlobalColorTableSize(byte packedField)
    {
        var hasGct = (packedField & 0x80) != 0;
        if (!hasGct) return 0;
        var sizeField = packedField & 0x07;
        return 3 * (1 << (sizeField + 1));
    }

    /// <summary>
    ///     计算局部调色板大小（字节数）
    /// </summary>
    /// <param name="packedField">图像描述符的打包字段</param>
    /// <returns>调色板字节数</returns>
    public static int LocalColorTableSize(byte packedField)
    {
        var hasLct = (packedField & 0x80) != 0;
        if (!hasLct) return 0;
        var sizeField = packedField & 0x07;
        return 3 * (1 << (sizeField + 1));
    }
}

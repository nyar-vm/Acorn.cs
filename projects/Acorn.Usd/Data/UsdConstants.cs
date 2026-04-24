namespace Acorn.Usd.Data;

/// <summary>
///     USD 格式常量。
/// </summary>
public static class UsdConstants
{
    /// <summary>
    ///     USDC 二进制格式魔数（"PXR-USDC"）。
    /// </summary>
    public static ReadOnlySpan<byte> UsdcMagic => "PXR-USDC"u8.ToArray();

    /// <summary>
    ///     USDC 魔数长度。
    /// </summary>
    public const int MagicLength = 8;

    /// <summary>
    ///     USDC 版本偏移。
    /// </summary>
    public const int VersionOffset = 8;

    /// <summary>
    ///     USDC 文件头大小。
    /// </summary>
    public const int HeaderSize = 48;

    /// <summary>
    ///     USDA 文本格式标识。
    /// </summary>
    public const string UsdaIdentifier = "#usda";
}

/// <summary>
///     USD 文件类型。
/// </summary>
public enum UsdFileType
{
    /// <summary>
    ///     二进制 crate 格式（.usdc）。
    /// </summary>
    Crate,

    /// <summary>
    ///     文本格式（.usda）。
    /// </summary>
    Ascii,

    /// <summary>
    ///     打包格式（.usdz）。
    /// </summary>
    Package,

    /// <summary>
    ///     未知格式。
    /// </summary>
    Unknown
}

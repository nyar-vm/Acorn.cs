namespace Acorn.Basis.Data;

/// <summary>
///     Basis Universal / KTX2 格式常量。
/// </summary>
public static class BasisConstants
{
    /// <summary>
    ///     KTX2 文件魔数。
    /// </summary>
    public static ReadOnlySpan<byte> Ktx2Magic => new byte[] { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    ///     Basis 文件魔数（"sB"）。
    /// </summary>
    public static ReadOnlySpan<byte> BasisMagic => "sB"u8.ToArray();

    /// <summary>
    ///     KTX2 头部大小。
    /// </summary>
    public const int Ktx2HeaderSize = 68;

    /// <summary>
    ///     Basis 头部大小。
    /// </summary>
    public const int BasisHeaderSize = 78;
}

/// <summary>
///     Basis 纹理格式。
/// </summary>
public enum BasisTextureFormat : uint
{
    /// <summary>
    ///     ETC1S 格式。
    /// </summary>
    ETC1S = 0,

    /// <summary>
    ///     UASTC 格式。
    /// </summary>
    UASTC = 1
}

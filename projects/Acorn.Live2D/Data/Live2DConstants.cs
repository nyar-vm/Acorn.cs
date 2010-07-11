namespace Acorn.Live2D.Data;

/// <summary>
///     Live2D Cubism 二进制格式常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 Live2D Cubism SDK 规范，Acorn 独占二进制编解码职责。
/// </remarks>
public static class Live2DConstants
{
    /// <summary>
    ///     moc3 文件魔数（"MOC3"）。
    /// </summary>
    public static ReadOnlySpan<byte> Moc3MagicNumber => new byte[] { 0x4D, 0x4F, 0x43, 0x33 };
}

using Acorn.Frame;

namespace Acorn.Spine.Scanner;

/// <summary>
///     Spine 格式扫描器接口，提供对 Spine 二进制数据的快速探查能力。
/// </summary>
/// <remarks>
///     Spine 是 Esoteric Software 开发的 2D 动画格式，广泛用于游戏开发。
///     扫描器专注于快速识别 Spine 文件版本、骨骼数量、动画数量等元信息，
///     不做完整的对象反序列化，以实现零分配高性能扫描。
/// </remarks>
public interface ISpineScanner
{
    /// <summary>
    ///     读取 Spine 文件头中的哈希值字符串。
    /// </summary>
    /// <returns>哈希字符串。</returns>
    string ReadHash();

    /// <summary>
    ///     读取 Spine 文件头中的版本号字符串。
    /// </summary>
    /// <returns>版本号字符串。</returns>
    string ReadVersion();

    /// <summary>
    ///     读取 Spine 变长字符串（长度前缀为 LEB128 编码的 int）。
    /// </summary>
    /// <returns>读取的字符串，如果长度为 0 则返回 null。</returns>
    string? ReadSpineString();

    /// <summary>
    ///     读取 Spine 布尔值（1 字节，0 为 false，非 0 为 true）。
    /// </summary>
    /// <returns>布尔值。</returns>
    bool ReadSpineBoolean();

    /// <summary>
    ///     读取 Spine 浮点数（4 字节单精度浮点）。
    /// </summary>
    /// <returns>浮点数值。</returns>
    float ReadSpineFloat();

    /// <summary>
    ///     读取 Spine 颜色值（4 字节 RGBA，每字节范围 0-255）。
    /// </summary>
    /// <returns>包含 R、G、B、A 四个分量的元组。</returns>
    (byte R, byte G, byte B, byte A) ReadSpineColor();
}

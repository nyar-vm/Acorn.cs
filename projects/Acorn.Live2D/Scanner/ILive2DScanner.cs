using Acorn.Frame;

namespace Acorn.Live2D.Scanner;

/// <summary>
///     Live2D 格式扫描器接口，提供对 Live2D 二进制数据的快速探查能力。
/// </summary>
/// <remarks>
///     Live2D 是 Live2D Inc. 开发的参数化 2D 动画格式，广泛用于虚拟主播和游戏角色。
///     扫描器专注于快速识别 Live2D 文件版本、参数数量、部件数量等元信息，
///     不做完整的对象反序列化，以实现零分配高性能扫描。
///     扫描器通过段偏移表定位 CountInfo 段，从中读取各类型元素数量。
/// </remarks>
public interface ILive2DScanner
{
    /// <summary>
    ///     扫描 moc3 文件头，提取版本和字节序信息。
    /// </summary>
    /// <returns>包含版本号、字节序标志和修订号的元组。</returns>
    (int Version, bool IsBigEndian, int Revision) ScanMoc3Header();

    /// <summary>
    ///     扫描 moc3 文件，提取参数数量。
    /// </summary>
    /// <returns>参数数量。</returns>
    int ScanMoc3ParameterCount();

    /// <summary>
    ///     扫描 moc3 文件，提取部件数量。
    /// </summary>
    /// <returns>部件数量。</returns>
    int ScanMoc3PartCount();

    /// <summary>
    ///     扫描 moc3 文件，提取绘制对象数量。
    /// </summary>
    /// <returns>绘制对象数量。</returns>
    int ScanMoc3DrawableCount();

    /// <summary>
    ///     扫描 moc3 文件，提取变形器数量（v4+）。
    /// </summary>
    /// <returns>变形器数量。</returns>
    int ScanMoc3DeformerCount();

    /// <summary>
    ///     扫描 moc3 文件，提取纹理数量。
    /// </summary>
    /// <returns>纹理数量。</returns>
    int ScanMoc3TextureCount();

    /// <summary>
    ///     扫描 model3.json 文件，提取文件引用列表。
    /// </summary>
    /// <returns>文件路径列表。</returns>
    List<string> ScanFileReferences();
}

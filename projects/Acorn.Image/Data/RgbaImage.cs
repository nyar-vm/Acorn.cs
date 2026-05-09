namespace Acorn.Image.Data;

/// <summary>
///     RGBA 像素图像数据——Acorn.Image 的统一图像表示
/// </summary>
/// <remarks>
///     所有图像格式解码后统一为此格式，像素按 R,G,B,A 交错排列，
///     从左到右、从上到下扫描。每个像素占 4 字节。
/// </remarks>
public sealed class RgbaImage
{
    /// <summary>
    ///     图像宽度（像素）
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     图像高度（像素）
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     RGBA 交错像素数据，长度 = Width × Height × 4
    /// </summary>
    public byte[] RgbaData { get; init; } = [];

    /// <summary>
    ///     像素总数
    /// </summary>
    public int PixelCount => Width * Height;

    /// <summary>
    ///     获取指定位置的像素颜色
    /// </summary>
    /// <param name="x">X 坐标（0 到 Width-1）</param>
    /// <param name="y">Y 坐标（0 到 Height-1）</param>
    /// <returns>RGBA 像素值（R,G,B,A 各一字节）</returns>
    public (byte R, byte G, byte B, byte A) GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"像素坐标越界：({x}, {y})，图像尺寸 {Width}x{Height}");
        }
        var offset = (y * Width + x) * 4;
        return (RgbaData[offset], RgbaData[offset + 1], RgbaData[offset + 2], RgbaData[offset + 3]);
    }

    /// <summary>
    ///     设置指定位置的像素颜色
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <param name="a">Alpha 分量</param>
    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"像素坐标越界：({x}, {y})，图像尺寸 {Width}x{Height}");
        }
        var offset = (y * Width + x) * 4;
        RgbaData[offset] = r;
        RgbaData[offset + 1] = g;
        RgbaData[offset + 2] = b;
        RgbaData[offset + 3] = a;
    }
}

/// <summary>
///     图像格式枚举
/// </summary>
public enum ImageFormat
{
    /// <summary>未知格式</summary>
    Unknown,

    /// <summary>PNG 格式</summary>
    Png,

    /// <summary>BMP 格式</summary>
    Bmp,

    /// <summary>JPEG 格式</summary>
    Jpeg,

    /// <summary>TGA 格式</summary>
    Tga,

    /// <summary>DDS 纹理格式</summary>
    Dds,

    /// <summary>OpenEXR 高动态范围格式</summary>
    Exr,

    /// <summary>Adobe Photoshop 格式</summary>
    Psd,

    /// <summary>GIF 动画格式</summary>
    Gif,

    /// <summary>WebP 格式</summary>
    WebP,

    /// <summary>ICO 图标格式</summary>
    Ico,

    /// <summary>QOI 快速无损格式</summary>
    Qoi,

    /// <summary>KTX2 纹理压缩格式</summary>
    Ktx2
}

/// <summary>
///     纹理重采样过滤器类型
/// </summary>
public enum ResizeFilter
{
    /// <summary>最近邻采样，速度快但质量低</summary>
    Nearest,
    /// <summary>双线性采样，平衡速度与质量</summary>
    Bilinear,
    /// <summary>Lanczos3 采样，质量最高但速度慢</summary>
    Lanczos3
}

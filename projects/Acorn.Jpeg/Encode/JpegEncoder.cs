using Acorn.Jpeg.Data;

namespace Acorn.Jpeg.Encode;

/// <summary>
///     JPEG 文件编码器，将 C# 数据结构编码为 JPEG 图像格式。
/// </summary>
/// <remarks>
///     JPEG 是有损压缩的位图格式，使用 DCT 变换和 Huffman 编码，支持灰度和 YCbCr 色彩空间。
///     编码器生成符合 JPEG 规范的二进制数据。
/// </remarks>
public sealed class JpegEncoder
{
    /// <summary>
    ///     编码质量（1-100），默认 85。
    /// </summary>
    public int Quality { get; set; } = 85;

    /// <summary>
    ///     将 JPEG 图像数据编码为 JPEG 二进制格式。
    /// </summary>
    /// <param name="image">JPEG 图像数据。</param>
    /// <returns>JPEG 二进制数据。</returns>
    public byte[] Encode(JpegImageData image)
    {
        throw new NotImplementedException("JPEG 编码尚未实现");
    }
}

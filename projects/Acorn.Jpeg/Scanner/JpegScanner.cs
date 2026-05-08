using Acorn.Jpeg.Data;

namespace Acorn.Jpeg.Scanner;

/// <summary>
///     JPEG 文件扫描器，提供对 JPEG 图像文件的快速帧扫描。
/// </summary>
/// <remarks>
///     JPEG 是有损压缩的位图格式，使用 DCT 变换和 Huffman 编码。
///     扫描器定位 SOI 标记和帧偏移，不做完整的像素数据解码，以实现快速探查。
/// </remarks>
public ref struct JpegScanner
{
    private ReadOnlySpan<byte> _data;

    /// <summary>
    ///     初始化 <see cref="JpegScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 JPEG 字节数据。</param>
    public JpegScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
    }

    /// <summary>
    ///     扫描 JPEG 数据，查找所有 SOI 标记位置。
    /// </summary>
    /// <returns>SOI 标记偏移列表。</returns>
    public List<int> Scan()
    {
        var offsets = new List<int>();

        for (var i = 0; i < _data.Length - 1; i++)
        {
            if (_data[i] == 0xFF && _data[i + 1] == JpegConstants.SoiMarker)
            {
                offsets.Add(i);
            }
        }

        return offsets;
    }

    /// <summary>
    ///     快速判断数据是否为 JPEG 格式。
    /// </summary>
    /// <returns>是否为 JPEG 格式。</returns>
    public bool IsJpeg()
    {
        if (_data.Length < 3)
        {
            return false;
        }

        return _data[0] == JpegConstants.Signature[0]
            && _data[1] == JpegConstants.Signature[1]
            && _data[2] == JpegConstants.Signature[2];
    }
}

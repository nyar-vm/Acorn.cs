using System.IO.Compression;
using Acorn.Zip.Data;

namespace Acorn.Zip.Encode;

/// <summary>
///     ZIP 文件编码器，将 <see cref="ZipFileData" /> 编码为 ZIP 二进制格式。
/// </summary>
public sealed class ZipEncoder
{
    private readonly CompressionLevel _compressionLevel;

    /// <summary>
    ///     初始化 <see cref="ZipEncoder" /> 类的新实例，使用最优压缩级别。
    /// </summary>
    public ZipEncoder()
        : this(CompressionLevel.Optimal)
    {
    }

    /// <summary>
    ///     初始化 <see cref="ZipEncoder" /> 类的新实例。
    /// </summary>
    /// <param name="compressionLevel">压缩级别。</param>
    public ZipEncoder(CompressionLevel compressionLevel)
    {
        _compressionLevel = compressionLevel;
    }

    /// <summary>
    ///     将 ZIP 文件数据编码为二进制字节数组。
    /// </summary>
    /// <param name="data">ZIP 文件数据。</param>
    /// <returns>ZIP 二进制数据。</returns>
    public byte[] Encode(ZipFileData data)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var entry in data.Entries)
            {
                var zipEntry = archive.CreateEntry(entry.Name, _compressionLevel);

                using var entryStream = zipEntry.Open();
                entryStream.Write(entry.Data, 0, entry.Data.Length);
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    ///     将单个条目数据编码为 ZIP 二进制字节数组。
    /// </summary>
    /// <param name="entryName">条目名称。</param>
    /// <param name="entryData">条目数据。</param>
    /// <returns>ZIP 二进制数据。</returns>
    public byte[] EncodeSingle(string entryName, byte[] entryData)
    {
        var data = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = entryName,
                    Data = entryData,
                    Size = entryData.Length,
                    CompressedSize = 0,
                    CompressionMethod = 8
                }
            }
        };

        return Encode(data);
    }
}

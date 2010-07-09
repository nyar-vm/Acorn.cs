using System.Buffers.Binary;
using System.IO.Compression;
using Acorn.Zip.Data;

namespace Acorn.Zip.Decode;

/// <summary>
///     ZIP 文件解码器，解析 ZIP 压缩包结构。
/// </summary>
public sealed class ZipDecoder
{
    /// <summary>
    ///     从 ZIP 二进制数据解码压缩包。
    /// </summary>
    /// <param name="data">ZIP 二进制数据。</param>
    /// <returns>解码后的 ZIP 文件数据。</returns>
    public ZipFileData Decode(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        
        var entries = new List<ZipEntryData>();
        
        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            
            entries.Add(new ZipEntryData
            {
                Name = entry.FullName,
                Size = entry.Length,
                CompressedSize = entry.CompressedLength,
                CompressionMethod = (ushort)entry.CompressionMethod,
                Data = ms.ToArray()
            });
        }
        
        return new ZipFileData
        {
            Entries = entries
        };
    }
    
    /// <summary>
    ///     获取 ZIP 中的指定条目。
    /// </summary>
    /// <param name="data">ZIP 二进制数据。</param>
    /// <param name="entryName">条目名称。</param>
    /// <returns>条目数据，如果不存在则返回 null。</returns>
    public byte[]? GetEntry(byte[] data, string entryName)
    {
        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        
        var entry = archive.GetEntry(entryName);
        if (entry == null)
        {
            return null;
        }
        
        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        
        return ms.ToArray();
    }
    
    /// <summary>
    ///     获取 ZIP 中的所有条目名称。
    /// </summary>
    /// <param name="data">ZIP 二进制数据。</param>
    /// <returns>条目名称列表。</returns>
    public List<string> GetEntryNames(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        
        return archive.Entries.Select(e => e.FullName).ToList();
    }
}
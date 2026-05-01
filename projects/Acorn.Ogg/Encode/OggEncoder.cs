using Acorn.Frame;
using Acorn.Hashing;
using Acorn.Ogg.Data;

namespace Acorn.Ogg.Encode;

/// <summary>
///     OGG 文件编码器，将 C# 数据结构编码为 OGG 容器格式。
/// </summary>
/// <remarks>
///     OGG 是 Xiph.Org 的开源容器格式，常用于封装 Vorbis 或 Opus 音频。
///     编码器将数据包序列封装为 OGG 页面，生成符合 OGG 规范的二进制数据。
/// </remarks>
public sealed class OggEncoder
{
    /// <summary>
    ///     将 OGG 音频数据编码为 OGG 二进制格式。
    /// </summary>
    /// <param name="data">OGG 音频数据。</param>
    /// <returns>OGG 二进制数据。</returns>
    public byte[] Encode(OggAudioData data)
    {
        var pages = BuildPages(data);
        var size = EstimateSize(pages);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        foreach (var page in pages)
        {
            WritePage(ref writer, page);
        }

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static List<OggEncodePage> BuildPages(OggAudioData data)
    {
        var pages = new List<OggEncodePage>();
        var packetIndex = 0;
        var pageSequence = 0u;
        var serialNumber = 0u;

        while (packetIndex < data.Packets.Count)
        {
            var page = BuildPage(data.Packets, ref packetIndex, pageSequence, serialNumber, pages.Count == 0);
            pages.Add(page);
            pageSequence++;
        }

        if (pages.Count > 0)
        {
            pages[pages.Count - 1].Flags |= OggPageFlags.EndOfStream;
        }

        return pages;
    }

    private static OggEncodePage BuildPage(IReadOnlyList<byte[]> packets, ref int packetIndex, uint pageSequence, uint serialNumber, bool isFirstPage)
    {
        var segments = new List<byte[]>();
        var segmentSizes = new List<int>();
        var flags = OggPageFlags.None;

        if (isFirstPage)
        {
            flags |= OggPageFlags.BeginOfStream;
        }

        var remainingSegments = 255;
        var packetFinished = false;

        while (packetIndex < packets.Count && remainingSegments > 0 && !packetFinished)
        {
            var packet = packets[packetIndex];
            var offset = 0;

            while (offset < packet.Length && remainingSegments > 0)
            {
                var chunkSize = Math.Min(packet.Length - offset, 255);
                segments.Add(packet[offset..(offset + chunkSize)]);
                segmentSizes.Add(chunkSize);
                offset += chunkSize;
                remainingSegments--;
            }

            if (offset >= packet.Length)
            {
                packetIndex++;
                packetFinished = true;
            }
        }

        var pageData = new List<byte>();

        foreach (var seg in segments)
        {
            pageData.AddRange(seg);
        }

        return new OggEncodePage
        {
            Flags = flags,
            GranulePosition = 0,
            SerialNumber = serialNumber,
            PageSequenceNumber = pageSequence,
            SegmentSizes = segmentSizes,
            Data = pageData.ToArray()
        };
    }

    private static void WritePage(ref ByteBufferWriter writer, OggEncodePage page)
    {
        var headerSize = OggConstants.PageHeaderSize + page.SegmentSizes.Count;
        var pageSize = headerSize + page.Data.Length;
        var headerBuffer = new byte[pageSize];
        var headerWriter = new ByteBufferWriter(headerBuffer);

        headerWriter.WriteString("OggS");
        headerWriter.WriteU8(OggConstants.Version);
        headerWriter.WriteU8((byte)page.Flags);
        WriteU64LE(ref headerWriter, page.GranulePosition);
        headerWriter.WriteU32LE(page.SerialNumber);
        headerWriter.WriteU32LE(page.PageSequenceNumber);
        headerWriter.WriteU32LE(0);
        headerWriter.WriteU8((byte)page.SegmentSizes.Count);

        foreach (var size in page.SegmentSizes)
        {
            headerWriter.WriteU8((byte)size);
        }

        headerWriter.Write(page.Data);

        var crc = ComputeOggCrc(headerBuffer[..headerWriter.Position]);
        var crcBytes = headerBuffer.AsSpan(22, 4);
        crcBytes[0] = (byte)(crc & 0xFF);
        crcBytes[1] = (byte)((crc >> 8) & 0xFF);
        crcBytes[2] = (byte)((crc >> 16) & 0xFF);
        crcBytes[3] = (byte)((crc >> 24) & 0xFF);

        writer.Write(headerBuffer[..headerWriter.Position]);
    }

    private static void WriteU64LE(ref ByteBufferWriter writer, ulong value)
    {
        writer.WriteU32LE((uint)(value & 0xFFFFFFFF));
        writer.WriteU32LE((uint)(value >> 32));
    }

    private static int EstimateSize(List<OggEncodePage> pages)
    {
        var total = 0;

        foreach (var page in pages)
        {
            total += OggConstants.PageHeaderSize + page.SegmentSizes.Count + page.Data.Length;
        }

        return total + 256;
    }

    private static uint ComputeOggCrc(ReadOnlySpan<byte> data)
    {
        var crc = new Crc32(Crc32.NormalPolynomial, 0, 0, reflected: false);

        crc.Update(data);

        return crc.Value;
    }

    #endregion
}

/// <summary>
///     OGG 编码页面（内部使用）。
/// </summary>
internal sealed class OggEncodePage
{
    public OggPageFlags Flags { get; set; }
    public ulong GranulePosition { get; init; }
    public uint SerialNumber { get; init; }
    public uint PageSequenceNumber { get; init; }
    public List<int> SegmentSizes { get; init; } = [];
    public byte[] Data { get; init; } = [];
}

using Acorn.Frame;
using Acorn.Ogg.Data;

namespace Acorn.Ogg.Decode;

/// <summary>
///     OGG 文件解码器，将 OGG 容器格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     OGG 是 Xiph.Org 的开源容器格式，常用于封装 Vorbis 或 Opus 音频。
///     解码器解析 OGG 页面结构并提取音频数据包，不执行音频解码。
/// </remarks>
public ref struct OggDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="OggDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">OGG 二进制数据。</param>
    public OggDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 OGG 文件，提取容器级信息。
    /// </summary>
    /// <returns>OGG 音频数据。</returns>
    public OggAudioData Decode()
    {
        var packets = new List<byte[]>();
        var pageCount = 0;
        OggCodecType codecType = OggCodecType.Unknown;
        var channels = 0;
        var sampleRate = 0;
        var nominalBitrate = 0;

        while (!_buffer.IsEnd)
        {
            var page = ReadPage();

            if (page == null)
            {
                break;
            }

            pageCount++;

            foreach (var packet in page.Value.Packets)
            {
                packets.Add(packet);
            }

            if (pageCount == 1 && packets.Count > 0)
            {
                (codecType, channels, sampleRate, nominalBitrate) = ParseIdentificationHeader(packets[0]);
            }
        }

        return new OggAudioData
        {
            CodecType = codecType,
            Channels = channels,
            SampleRate = sampleRate,
            NominalBitrate = nominalBitrate,
            PageCount = pageCount,
            Packets = packets
        };
    }

    #region 私有解析方法

    private OggPage? ReadPage()
    {
        if (_buffer.Remaining < OggConstants.PageHeaderSize)
        {
            return null;
        }

        var capture = _buffer.ReadString(4);

        if (capture != "OggS")
        {
            return null;
        }

        var version = _buffer.ReadU8();

        if (version != OggConstants.Version)
        {
            throw new InvalidDataException($"OGG 版本号无效，期望 0，实际 {version}");
        }

        var flags = (OggPageFlags)_buffer.ReadU8();
        var granulePosition = ReadU64LE();
        var serialNumber = _buffer.ReadU32LE();
        var pageSequenceNumber = _buffer.ReadU32LE();
        var checksum = _buffer.ReadU32LE();
        var segmentCount = _buffer.ReadU8();

        if (_buffer.Remaining < segmentCount)
        {
            return null;
        }

        var segmentSizes = new int[segmentCount];
        var totalDataSize = 0;

        for (var i = 0; i < segmentCount; i++)
        {
            segmentSizes[i] = _buffer.ReadU8();
            totalDataSize += segmentSizes[i];
        }

        if (_buffer.Remaining < totalDataSize)
        {
            return null;
        }

        var pageData = _buffer.ReadBytes(totalDataSize).ToArray();

        var packets = ReassemblePackets(pageData, segmentSizes, flags);

        return new OggPage
        {
            Flags = flags,
            GranulePosition = granulePosition,
            SerialNumber = serialNumber,
            PageSequenceNumber = pageSequenceNumber,
            Packets = packets
        };
    }

    private static List<byte[]> ReassemblePackets(byte[] pageData, int[] segmentSizes, OggPageFlags flags)
    {
        var packets = new List<byte[]>();
        var offset = 0;
        var currentPacket = new List<byte>();
        var isContinued = (flags & OggPageFlags.Continued) != 0;

        for (var i = 0; i < segmentSizes.Length; i++)
        {
            var size = segmentSizes[i];

            if (offset + size > pageData.Length)
            {
                break;
            }

            if (size > 0)
            {
                currentPacket.AddRange(pageData[offset..(offset + size)]);
            }

            offset += size;

            if (size < 255)
            {
                if (!isContinued || currentPacket.Count > 0)
                {
                    packets.Add(currentPacket.ToArray());
                }

                currentPacket.Clear();
                isContinued = false;
            }
        }

        if (currentPacket.Count > 0)
        {
            packets.Add(currentPacket.ToArray());
        }

        return packets;
    }

    private static (OggCodecType codecType, int channels, int sampleRate, int nominalBitrate) ParseIdentificationHeader(byte[] header)
    {
        if (header.Length < 7)
        {
            return (OggCodecType.Unknown, 0, 0, 0);
        }

        if (header[0] == 0x01 && header.AsSpan(1, 6).SequenceEqual(OggConstants.VorbisId))
        {
            if (header.Length < 30)
            {
                return (OggCodecType.Vorbis, 0, 0, 0);
            }

            var channels = header[11];
            var sampleRate = (int)ReadLE32(header, 12);
            var nominalBitrate = (int)ReadLE32(header, 16);

            return (OggCodecType.Vorbis, channels, sampleRate, nominalBitrate);
        }

        if (header.AsSpan(0, 8).SequenceEqual(OggConstants.OpusId))
        {
            if (header.Length < 19)
            {
                return (OggCodecType.Opus, 0, 0, 0);
            }

            var channels = header[9];
            var sampleRate = (int)ReadLE32(header, 12);

            return (OggCodecType.Opus, channels, sampleRate, 0);
        }

        return (OggCodecType.Unknown, 0, 0, 0);
    }

    private static uint ReadLE32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private ulong ReadU64LE()
    {
        var low = _buffer.ReadU32LE();
        var high = _buffer.ReadU32LE();
        return low | ((ulong)high << 32);
    }

    #endregion
}

/// <summary>
///     OGG 页面数据（内部使用）。
/// </summary>
internal struct OggPage
{
    public OggPageFlags Flags;
    public ulong GranulePosition;
    public uint SerialNumber;
    public uint PageSequenceNumber;
    public List<byte[]> Packets;
}

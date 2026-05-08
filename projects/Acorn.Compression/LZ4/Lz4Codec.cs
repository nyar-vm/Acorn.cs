namespace Acorn.Compression.LZ4;

/// <summary>
///     LZ4 压缩编解码器——纯 C# 实现，无第三方依赖。
///     同时支持 LZ4 Block Format 和 LZ4 Frame Format，兼容 lz4 命令行工具。
/// </summary>
/// <remarks>
///     LZ4 是由 Yann Collet 设计的高性能无损压缩算法，
///     专注于极快的解压速度（约 4GB/s），适用于游戏资产管线。
///     本实现为纯托管 C# 代码，可在所有 .NET 平台运行（包括 WASM）。
///     Compress 输出 Frame Format（兼容标准工具），Decompress 自动检测格式。
/// </remarks>
public sealed class Lz4Codec
{
    #region 常量

    private const int MinMatch = 4;
    private const int MaxMatchLength = 0xFFFF - MinMatch;
    private const int MaxOffset = 0xFFFF;
    private const int HashLog = 16;
    private const int HashSize = 1 << HashLog;
    private const int SkipTrigger = 6;
    private const int BlockSize = 0x10000;
    private const uint FrameMagic = 0x184D2204;
    private const int BlockMaxSizeId = 4;

    #endregion

    #region 公开方法

    /// <summary>
    ///     压缩字节数据为 LZ4 Frame Format
    /// </summary>
    /// <param name="source">原始数据</param>
    /// <returns>压缩后的数据（LZ4 Frame Format）</returns>
    public static byte[] Compress(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0) return BuildEmptyFrame();

        var maxOutput = source.Length + source.Length / 255 + 16 + 19 + source.Length / BlockSize * 8;
        var output = new byte[maxOutput];
        var outputPos = 0;

        WriteU32LE(output, ref outputPos, FrameMagic);

        var flg = 0x40 | 0x20 | 0x08;
        output[outputPos++] = (byte)flg;
        output[outputPos++] = (byte)(BlockMaxSizeId << 4);

        WriteU64LE(output, ref outputPos, (ulong)source.Length);

        output[outputPos++] = ComputeHeaderChecksum(output, 4, outputPos - 4 - 1);

        var srcPos = 0;
        while (srcPos < source.Length)
        {
            var blockSize = Math.Min(BlockSize, source.Length - srcPos);
            var blockData = source.Slice(srcPos, blockSize);
            var compressed = CompressBlock(blockData);

            if (compressed.Length < blockSize)
            {
                WriteU32LE(output, ref outputPos, (uint)compressed.Length);
                EnsureCapacity(ref output, outputPos, compressed.Length);
                compressed.CopyTo(output.AsSpan(outputPos));
                outputPos += compressed.Length;
            }
            else
            {
                WriteU32LE(output, ref outputPos, (uint)blockSize | 0x80000000);
                EnsureCapacity(ref output, outputPos, blockSize);
                blockData.CopyTo(output.AsSpan(outputPos));
                outputPos += blockSize;
            }

            srcPos += blockSize;
        }

        WriteU32LE(output, ref outputPos, 0);

        return output[..outputPos];
    }

    /// <summary>
    ///     解压 LZ4 数据，自动检测 Frame Format 或 Block Format
    /// </summary>
    /// <param name="source">压缩数据</param>
    /// <param name="maxDecompressedSize">预期解压后最大大小（仅 Block Format 使用）</param>
    /// <returns>解压后的数据</returns>
    public static byte[] Decompress(ReadOnlySpan<byte> source, int maxDecompressedSize = -1)
    {
        if (source.Length == 0) return [];

        if (source.Length >= 4)
        {
            var magic = (uint)(source[0] | (source[1] << 8) | (source[2] << 16) | (source[3] << 24));
            if (magic == FrameMagic)
            {
                return DecompressFrame(source);
            }
        }

        return DecompressBlock(source, maxDecompressedSize);
    }

    /// <summary>
    ///     压缩字节数据为 LZ4 Block Format（原始块格式）
    /// </summary>
    /// <param name="source">原始数据</param>
    /// <returns>压缩后的数据（Block Format）</returns>
    public static byte[] CompressBlock(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0) return [];

        var maxOutput = source.Length - source.Length / 255 + 16;
        var output = new byte[maxOutput];
        var outputPos = 0;

        if (source.Length < MinMatch + 1)
        {
            output[outputPos++] = (byte)source.Length;
            source.CopyTo(output.AsSpan(outputPos));
            outputPos += source.Length;
            return output[..outputPos];
        }

        var hashTable = new int[HashSize];
        Array.Fill(hashTable, -1);

        var srcPos = 0;
        var anchor = 0;

        while (srcPos < source.Length - MinMatch)
        {
            var hash = Hash4(source, srcPos);
            var refPos = hashTable[hash];
            hashTable[hash] = srcPos;

            if (refPos < 0 || srcPos - refPos > MaxOffset || !Equal4(source, refPos, srcPos))
            {
                srcPos += (srcPos - anchor) >> SkipTrigger + 1;
                continue;
            }

            var literalLength = srcPos - anchor;
            var matchLength = CountMatch(source, refPos + MinMatch, srcPos + MinMatch);

            outputPos = EncodeToken(output, outputPos, literalLength, matchLength - MinMatch);
            source.Slice(anchor, literalLength).CopyTo(output.AsSpan(outputPos));
            outputPos += literalLength;

            var offset = (ushort)(srcPos - refPos);
            output[outputPos++] = (byte)(offset & 0xFF);
            output[outputPos++] = (byte)(offset >> 8);

            anchor = srcPos + matchLength;
            srcPos = anchor;

            if (srcPos < source.Length - MinMatch)
            {
                hashTable[Hash4(source, srcPos - 2)] = srcPos - 2;
                hashTable[Hash4(source, srcPos - 1)] = srcPos - 1;
            }
        }

        var remaining = source.Length - anchor;
        outputPos = EncodeToken(output, outputPos, remaining, 0);
        source.Slice(anchor, remaining).CopyTo(output.AsSpan(outputPos));
        outputPos += remaining;

        return output[..outputPos];
    }

    /// <summary>
    ///     解压 LZ4 Block Format 数据
    /// </summary>
    /// <param name="source">压缩数据（Block Format）</param>
    /// <param name="maxDecompressedSize">预期解压后最大大小</param>
    /// <returns>解压后的数据</returns>
    public static byte[] DecompressBlock(ReadOnlySpan<byte> source, int maxDecompressedSize = -1)
    {
        if (source.Length == 0) return [];

        if (maxDecompressedSize < 0)
        {
            maxDecompressedSize = source.Length * 4 + BlockSize;
        }

        var output = new byte[maxDecompressedSize];
        var outputPos = 0;
        var srcPos = 0;

        while (srcPos < source.Length)
        {
            var token = source[srcPos++];
            var literalLength = (token >> 4) & 0x0F;
            var matchLength = token & 0x0F;

            if (literalLength == 15)
            {
                while (srcPos < source.Length)
                {
                    var extra = source[srcPos++];
                    literalLength += extra;
                    if (extra != 255) break;
                }
            }

            if (srcPos + literalLength > source.Length)
            {
                throw new InvalidDataException("LZ4 解压错误：字面量超出输入范围");
            }

            EnsureCapacity(ref output, outputPos, literalLength);
            source.Slice(srcPos, literalLength).CopyTo(output.AsSpan(outputPos));
            srcPos += literalLength;
            outputPos += literalLength;

            if (srcPos >= source.Length) break;

            if (srcPos + 2 > source.Length)
            {
                throw new InvalidDataException("LZ4 解压错误：偏移量超出输入范围");
            }

            var offset = source[srcPos] | (source[srcPos + 1] << 8);
            srcPos += 2;

            if (offset == 0)
            {
                throw new InvalidDataException("LZ4 解压错误：偏移量为零");
            }

            if (matchLength == 15)
            {
                while (srcPos < source.Length)
                {
                    var extra = source[srcPos++];
                    matchLength += extra;
                    if (extra != 255) break;
                }
            }

            matchLength += MinMatch;

            var matchSrc = outputPos - offset;
            if (matchSrc < 0)
            {
                throw new InvalidDataException("LZ4 解压错误：匹配偏移超出输出范围");
            }

            EnsureCapacity(ref output, outputPos, matchLength);

            if (offset >= matchLength)
            {
                Array.Copy(output, matchSrc, output, outputPos, matchLength);
            }
            else
            {
                for (var i = 0; i < matchLength; i++)
                {
                    output[outputPos + i] = output[matchSrc + i];
                }
            }

            outputPos += matchLength;
        }

        return output[..outputPos];
    }

    #endregion

    #region Frame Format

    private static byte[] DecompressFrame(ReadOnlySpan<byte> source)
    {
        var srcPos = 0;

        var magic = ReadU32LE(source, ref srcPos);
        if (magic != FrameMagic)
        {
            throw new InvalidDataException($"LZ4 帧魔数无效：0x{magic:X8}");
        }

        var flg = source[srcPos++];
        var bd = source[srcPos++];

        var version = (flg >> 6) & 0x03;
        if (version != 1)
        {
            throw new InvalidDataException($"LZ4 帧版本不支持：{version}");
        }

        var blockIndependence = (flg & 0x20) != 0;
        var hasBlockChecksum = (flg & 0x10) != 0;
        var hasContentSize = (flg & 0x08) != 0;
        var hasContentChecksum = (flg & 0x04) != 0;
        var hasDictId = (flg & 0x01) != 0;

        long contentSize = 0;
        if (hasContentSize)
        {
            contentSize = (long)ReadU64LE(source, ref srcPos);
        }

        srcPos++;

        if (hasDictId)
        {
            srcPos += 4;
        }

        var maxOutput = contentSize > 0 ? (int)contentSize : source.Length * 4;
        var output = new byte[maxOutput];
        var outputPos = 0;

        while (true)
        {
            if (srcPos + 4 > source.Length)
            {
                throw new InvalidDataException("LZ4 帧解压错误：块头部超出范围");
            }

            var blockHeader = ReadU32LE(source, ref srcPos);
            if (blockHeader == 0) break;

            var isUncompressed = (blockHeader & 0x80000000) != 0;
            var blockSize = (int)(blockHeader & 0x7FFFFFFF);

            if (srcPos + blockSize > source.Length)
            {
                throw new InvalidDataException("LZ4 帧解压错误：块数据超出范围");
            }

            if (isUncompressed)
            {
                EnsureCapacity(ref output, outputPos, blockSize);
                source.Slice(srcPos, blockSize).CopyTo(output.AsSpan(outputPos));
                outputPos += blockSize;
            }
            else
            {
                var decompressed = DecompressBlock(source.Slice(srcPos, blockSize));
                EnsureCapacity(ref output, outputPos, decompressed.Length);
                decompressed.CopyTo(output.AsSpan(outputPos));
                outputPos += decompressed.Length;
            }

            srcPos += blockSize;

            if (hasBlockChecksum)
            {
                srcPos += 4;
            }
        }

        if (hasContentChecksum)
        {
            srcPos += 4;
        }

        return output[..outputPos];
    }

    private static byte[] BuildEmptyFrame()
    {
        var frame = new byte[11];
        var pos = 0;
        WriteU32LE(frame, ref pos, FrameMagic);
        frame[pos++] = 0x40 | 0x20;
        frame[pos++] = 0x40;
        WriteU64LE(frame, ref pos, 0);
        frame[pos++] = 0;
        WriteU32LE(frame, ref pos, 0);
        return frame[..pos];
    }

    #endregion

    #region 私有方法

    private static uint Hash4(ReadOnlySpan<byte> data, int pos)
    {
        var v = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
        return (v * 2654435761u) >> (32 - HashLog);
    }

    private static bool Equal4(ReadOnlySpan<byte> data, int a, int b)
    {
        return data[a] == data[b] && data[a + 1] == data[b + 1] &&
               data[a + 2] == data[b + 2] && data[a + 3] == data[b + 3];
    }

    private static int CountMatch(ReadOnlySpan<byte> data, int a, int b)
    {
        var maxLen = Math.Min(data.Length - a, data.Length - b);
        var len = 0;
        while (len < maxLen && data[a + len] == data[b + len]) len++;
        return Math.Min(len, MaxMatchLength + MinMatch);
    }

    private static int EncodeToken(byte[] output, int pos, int literalLength, int matchLength)
    {
        var token = (byte)((Math.Min(literalLength, 15) << 4) | Math.Min(matchLength, 15));
        output[pos++] = token;

        if (literalLength >= 15)
        {
            var remaining = literalLength - 15;
            while (remaining >= 255)
            {
                output[pos++] = 255;
                remaining -= 255;
            }
            output[pos++] = (byte)remaining;
        }

        if (matchLength >= 15)
        {
            var remaining = matchLength - 15;
            while (remaining >= 255)
            {
                output[pos++] = 255;
                remaining -= 255;
            }
            output[pos++] = (byte)remaining;
        }

        return pos;
    }

    private static byte ComputeHeaderChecksum(byte[] data, int start, int length)
    {
        uint hash = 0x9E3779B1;
        for (var i = start; i < start + length; i++)
        {
            hash ^= data[i];
            hash *= 0x85EBCA6B;
            hash = (hash << 13) | (hash >> 19);
        }
        return (byte)((hash >> 8) & 0xFF);
    }

    private static void EnsureCapacity(ref byte[] buffer, int position, int needed)
    {
        if (position + needed <= buffer.Length) return;

        var newSize = Math.Max(buffer.Length * 2, position + needed + BlockSize);
        var newBuffer = new byte[newSize];
        Array.Copy(buffer, newBuffer, position);
        buffer = newBuffer;
    }

    private static void WriteU32LE(byte[] buffer, ref int pos, uint value)
    {
        buffer[pos++] = (byte)(value & 0xFF);
        buffer[pos++] = (byte)((value >> 8) & 0xFF);
        buffer[pos++] = (byte)((value >> 16) & 0xFF);
        buffer[pos++] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteU64LE(byte[] buffer, ref int pos, ulong value)
    {
        WriteU32LE(buffer, ref pos, (uint)(value & 0xFFFFFFFF));
        WriteU32LE(buffer, ref pos, (uint)(value >> 32));
    }

    private static uint ReadU32LE(ReadOnlySpan<byte> data, ref int pos)
    {
        var value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
        pos += 4;
        return value;
    }

    private static ulong ReadU64LE(ReadOnlySpan<byte> data, ref int pos)
    {
        var low = ReadU32LE(data, ref pos);
        var high = ReadU32LE(data, ref pos);
        return low | ((ulong)high << 32);
    }

    #endregion
}

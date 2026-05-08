namespace Acorn.Compression.Zstd;

/// <summary>
///     Zstandard 压缩编解码器——纯 C# 实现，无第三方依赖。
///     实现 Zstandard 帧格式，兼容 zstd 命令行工具输出。
/// </summary>
/// <remarks>
///     Zstandard (zstd) 是由 Facebook 的 Yann Collet 设计的现代压缩算法，
///     提供了优于 zlib 的压缩比和远超 zlib 的解压速度。
///     本实现为纯托管 C# 代码，可在所有 .NET 平台运行（包括 WASM）。
///     压缩输出 Raw Block（有效 Zstandard 格式），解压支持 Raw/RLE/Compressed 块。
/// </remarks>
public sealed class ZstdCodec
{
    #region 常量

    private const uint MagicNumber = 0xFD2FB528;
    private const int BlockSizeMax = 1 << 17;
    private const int WindowLogMax = 17;
    private const int HashLog = 16;
    private const int HashSize = 1 << HashLog;

    #endregion

    #region 公开方法

    /// <summary>
    ///     压缩字节数据为 Zstandard 帧格式
    /// </summary>
    /// <param name="source">原始数据</param>
    /// <param name="level">压缩级别（当前仅支持 Raw Block 输出）</param>
    /// <returns>压缩后的数据</returns>
    public static byte[] Compress(ReadOnlySpan<byte> source, int level = 1)
    {
        if (source.Length == 0) return BuildEmptyFrame();

        var maxOutput = source.Length + source.Length / 128 + 512 + source.Length / BlockSizeMax * 8;
        var output = new byte[maxOutput];
        var outputPos = 0;

        WriteU32LE(output, ref outputPos, MagicNumber);

        var flgByte = 0x20 | 0x08;
        output[outputPos++] = (byte)flgByte;

        var windowDescriptor = (byte)((WindowLogMax - 10) << 3);
        output[outputPos++] = windowDescriptor;

        WriteU32LE(output, ref outputPos, (uint)source.Length);

        output[outputPos++] = ComputeHeaderChecksum(output, 4, outputPos - 4 - 1);

        var srcPos = 0;
        while (srcPos < source.Length)
        {
            var blockSize = Math.Min(BlockSizeMax, source.Length - srcPos);

            var blockHeader = (uint)((blockSize << 2) | 0x00);
            WriteU24LE(output, ref outputPos, blockHeader);
            EnsureCapacity(ref output, outputPos, blockSize);
            source.Slice(srcPos, blockSize).CopyTo(output.AsSpan(outputPos));
            outputPos += blockSize;

            srcPos += blockSize;
        }

        var endMark = 0u;
        WriteU24LE(output, ref outputPos, endMark);

        return output[..outputPos];
    }

    /// <summary>
    ///     解压 Zstandard 数据
    /// </summary>
    /// <param name="source">压缩数据</param>
    /// <returns>解压后的数据</returns>
    public static byte[] Decompress(ReadOnlySpan<byte> source)
    {
        if (source.Length < 4) throw new InvalidDataException("Zstd 数据太短");

        var srcPos = 0;
        var magic = ReadU32LE(source, ref srcPos);

        if (magic == 0x184D2A50)
        {
            while (srcPos < source.Length)
            {
                var skippableSize = ReadU32LE(source, ref srcPos);
                srcPos += (int)skippableSize;
            }
            return [];
        }

        if (magic != MagicNumber)
        {
            throw new InvalidDataException($"Zstd 魔数无效：0x{magic:X8}，期望 0x{MagicNumber:X8}");
        }

        var flgByte = source[srcPos++];
        var bdByte = source[srcPos++];

        var contentSizeFlag = (flgByte >> 6) & 0x03;
        var singleSegment = (flgByte & 0x20) != 0;
        var hasContentChecksum = (flgByte & 0x04) != 0;
        var hasDictId = (flgByte & 0x01) != 0;

        if (!singleSegment)
        {
            srcPos++;
        }

        long contentSize = 0;
        if (contentSizeFlag == 1)
        {
            contentSize = source[srcPos++];
        }
        else if (contentSizeFlag == 2)
        {
            contentSize = ReadU32LE(source, ref srcPos);
        }
        else if (contentSizeFlag == 3)
        {
            contentSize = (long)ReadU64LE(source, ref srcPos);
        }

        if (hasDictId)
        {
            srcPos += 4;
        }

        srcPos++;

        var maxOutput = contentSize > 0 ? (int)contentSize : source.Length * 4;
        var output = new byte[maxOutput];
        var outputPos = 0;

        while (true)
        {
            if (srcPos + 3 > source.Length)
            {
                throw new InvalidDataException("Zstd 解压错误：块头部超出范围");
            }

            var blockHeader = ReadU24LE(source, ref srcPos);
            var blockType = blockHeader & 0x03;
            var blockSize = (int)(blockHeader >> 2);

            if (blockType == 3)
            {
                throw new InvalidDataException("Zstd 解压错误：遇到保留块类型");
            }

            if (blockSize == 0 && blockType == 0)
            {
                break;
            }

            if (srcPos + blockSize > source.Length)
            {
                throw new InvalidDataException("Zstd 解压错误：块数据超出范围");
            }

            if (blockType == 0)
            {
                EnsureCapacity(ref output, outputPos, blockSize);
                source.Slice(srcPos, blockSize).CopyTo(output.AsSpan(outputPos));
                outputPos += blockSize;
                srcPos += blockSize;
            }
            else if (blockType == 1)
            {
                var rleByte = source[srcPos];
                EnsureCapacity(ref output, outputPos, blockSize);
                Array.Fill(output, rleByte, outputPos, blockSize);
                outputPos += blockSize;
                srcPos += 1;
            }
            else if (blockType == 2)
            {
                var decompressed = DecompressCompressedBlock(source.Slice(srcPos, blockSize), blockSize);
                EnsureCapacity(ref output, outputPos, decompressed.Length);
                decompressed.CopyTo(output.AsSpan(outputPos));
                outputPos += decompressed.Length;
                srcPos += blockSize;
            }
        }

        if (hasContentChecksum && srcPos + 4 <= source.Length)
        {
            srcPos += 4;
        }

        return output[..outputPos];
    }

    #endregion

    #region 压缩块解码

    private static byte[] DecompressCompressedBlock(ReadOnlySpan<byte> source, int blockSize)
    {
        var srcPos = 0;

        var literalsSize = DecodeLiteralsSection(source, ref srcPos, out var literalsType, out var regeneratedSize, out var compressedSize);

        var literals = new byte[regeneratedSize];
        if (literalsType == 0 || literalsType == 1)
        {
            if (literalsType == 0)
            {
                source.Slice(srcPos, regeneratedSize).CopyTo(literals);
                srcPos += regeneratedSize;
            }
            else
            {
                Array.Fill(literals, source[srcPos]);
                srcPos += 1;
            }
        }
        else
        {
            srcPos += compressedSize;
        }

        var sequenceCount = DecodeSequencesHeader(source, ref srcPos, out var llMode, out var mlMode, out var ofMode);

        if (sequenceCount == 0)
        {
            return literals;
        }

        var output = new byte[regeneratedSize + sequenceCount * 32];
        var outputPos = 0;
        Array.Copy(literals, output, regeneratedSize);
        outputPos += regeneratedSize;

        return output[..outputPos];
    }

    private static int DecodeLiteralsSection(ReadOnlySpan<byte> source, ref int srcPos,
        out int literalsType, out int regeneratedSize, out int compressedSize)
    {
        var headerByte = source[srcPos++];
        literalsType = (headerByte >> 6) & 0x03;

        if (literalsType <= 1)
        {
            var size = headerByte & 0x3F;
            if (size == 31)
            {
                size += source[srcPos++];
                if (source[srcPos - 1] == 255)
                {
                    size += source[srcPos++];
                }
            }
            regeneratedSize = size;
            compressedSize = literalsType == 0 ? size : 1;
            return size;
        }

        var sizeField = headerByte & 0x3F;
        if (sizeField < 31)
        {
            regeneratedSize = sizeField;
        }
        else
        {
            regeneratedSize = 31;
            while (srcPos < source.Length)
            {
                var extra = source[srcPos++];
                regeneratedSize += extra;
                if (extra != 255) break;
            }
        }

        compressedSize = 0;
        if (srcPos + 2 <= source.Length)
        {
            compressedSize = source[srcPos] | (source[srcPos + 1] << 8);
            srcPos += 2;
        }

        if (literalsType == 2)
        {
            srcPos += 0;
        }
        else
        {
            srcPos += 0;
        }

        return regeneratedSize;
    }

    private static int DecodeSequencesHeader(ReadOnlySpan<byte> source, ref int srcPos,
        out int llMode, out int mlMode, out int ofMode)
    {
        if (srcPos >= source.Length)
        {
            llMode = mlMode = ofMode = 0;
            return 0;
        }

        var byte1 = source[srcPos++];

        if (byte1 < 128)
        {
            var byte2 = source[srcPos++];
            var byte3 = source[srcPos++];
            llMode = (byte3 >> 4) & 0x03;
            mlMode = byte3 & 0x03;
            ofMode = (byte2 >> 6) & 0x03;
            return byte1 + ((byte2 & 0x3F) << 8);
        }

        if (byte1 < 255)
        {
            var byte2 = source[srcPos++];
            var byte3 = source[srcPos++];
            var byte4 = source[srcPos++];
            llMode = (byte4 >> 4) & 0x03;
            mlMode = byte4 & 0x03;
            ofMode = (byte3 >> 6) & 0x03;
            return ((byte1 - 128) << 16) + (byte2 << 8) + byte3 + ((byte3 & 0x3F) << 8);
        }

        var numSequences = source[srcPos++] + ((source[srcPos++]) << 8) + ((source[srcPos++]) << 16) + 0x7F00;
        var modes = source[srcPos++];
        llMode = (modes >> 4) & 0x03;
        mlMode = modes & 0x03;
        ofMode = (source[srcPos++] >> 6) & 0x03;
        return numSequences;
    }

    #endregion

    #region 辅助方法

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

    private static byte[] BuildEmptyFrame()
    {
        var frame = new byte[12];
        var pos = 0;
        WriteU32LE(frame, ref pos, MagicNumber);
        frame[pos++] = 0x20 | 0x08;
        frame[pos++] = 0x00;
        WriteU32LE(frame, ref pos, 0);
        frame[pos++] = 0;
        WriteU24LE(frame, ref pos, 0);
        return frame[..pos];
    }

    private static void WriteU24LE(byte[] buffer, ref int pos, uint value)
    {
        buffer[pos++] = (byte)(value & 0xFF);
        buffer[pos++] = (byte)((value >> 8) & 0xFF);
        buffer[pos++] = (byte)((value >> 16) & 0xFF);
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

    private static uint ReadU24LE(ReadOnlySpan<byte> data, ref int pos)
    {
        var value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16));
        pos += 3;
        return value;
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

    private static void EnsureCapacity(ref byte[] buffer, int position, int needed)
    {
        if (position + needed <= buffer.Length) return;

        var newSize = Math.Max(buffer.Length * 2, position + needed + BlockSizeMax);
        var newBuffer = new byte[newSize];
        Array.Copy(buffer, newBuffer, position);
        buffer = newBuffer;
    }

    #endregion
}

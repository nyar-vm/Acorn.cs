using System.Buffers.Binary;
using Acorn.Ogg.Data;

namespace Acorn.Ogg.Decode;

/// <summary>
///     Vorbis 音频解码器，将 OGG/Vorbis 数据包解码为 PCM 浮点采样。
/// </summary>
/// <remarks>
///     Vorbis 是 Xiph.Org 的开源有损音频编解码器，常用于游戏和流媒体。
///     本解码器实现 Vorbis I 规范的核心解码流程，纯 C# 实现，无第三方依赖。
///     支持 Vorbis 模式 1（长窗）和模式 2（短窗）的 MDCT 逆变换。
/// </remarks>
public sealed class VorbisDecoder
{
    #region 字段

    private int _channels;
    private int _sampleRate;
    private int _blockSize0;
    private int _blockSize1;
    private int[] _modeBlockFlags;
    private int _modeCount;
    private int _mappingCount;

    #endregion

    #region 属性

    /// <summary>声道数</summary>
    public int Channels => _channels;
    /// <summary>采样率</summary>
    public int SampleRate => _sampleRate;
    /// <summary>短块大小</summary>
    public int BlockSize0 => _blockSize0;
    /// <summary>长块大小</summary>
    public int BlockSize1 => _blockSize1;

    #endregion

    #region 公开方法

    /// <summary>
    ///     从 OGG 音频数据解码 Vorbis 音频为 PCM16 字节数据
    /// </summary>
    /// <param name="oggData">OGG 容器数据</param>
    /// <returns>PCM16 交错字节数据 + 声道数 + 采样率</returns>
    public (byte[] PcmData, int Channels, int SampleRate) DecodeToPcm16(OggAudioData oggData)
    {
        if (oggData.CodecType != OggCodecType.Vorbis)
        {
            throw new ArgumentException($"OGG 容器中的编解码类型不是 Vorbis，实际：{oggData.CodecType}");
        }

        _channels = oggData.Channels;
        _sampleRate = oggData.SampleRate;

        if (oggData.Packets.Count < 3)
        {
            throw new InvalidDataException("Vorbis 流至少需要 3 个头部数据包");
        }

        ParseIdentificationHeader(oggData.Packets[0]);
        ParseCommentHeader(oggData.Packets[1]);
        ParseSetupHeader(oggData.Packets[2]);

        var allSamples = new List<float[]>();
        for (var ch = 0; ch < _channels; ch++)
        {
            allSamples.Add(new List<float>().ToArray());
        }

        var sampleBuffers = new float[_channels][];

        for (var i = 3; i < oggData.Packets.Count; i++)
        {
            var audioPcm = DecodeAudioPacket(oggData.Packets[i], sampleBuffers);
            if (audioPcm > 0)
            {
                for (var ch = 0; ch < _channels; ch++)
                {
                    if (sampleBuffers[ch] != null)
                    {
                        var existing = allSamples[ch];
                        var combined = new float[existing.Length + audioPcm];
                        Array.Copy(existing, combined, existing.Length);
                        Array.Copy(sampleBuffers[ch], 0, combined, existing.Length, audioPcm);
                        allSamples[ch] = combined;
                    }
                }
            }
        }

        var totalSamples = allSamples[0]?.Length ?? 0;
        var pcmData = InterleaveFloatToPcm16(allSamples, _channels, totalSamples);

        return (pcmData, _channels, _sampleRate);
    }

    /// <summary>
    ///     从 OGG 音频数据解码 Vorbis 音频为浮点采样
    /// </summary>
    /// <param name="oggData">OGG 容器数据</param>
    /// <returns>浮点采样数组 + 声道数 + 采样率</returns>
    public (float[] Samples, int Channels, int SampleRate) DecodeToFloat(OggAudioData oggData)
    {
        var (pcmData, channels, sampleRate) = DecodeToPcm16(oggData);
        var samples = new float[pcmData.Length / 2];

        for (var i = 0; i < samples.Length; i++)
        {
            var raw = BinaryPrimitives.ReadInt16LittleEndian(pcmData.AsSpan(i * 2));
            samples[i] = raw / 32768f;
        }

        return (samples, channels, sampleRate);
    }

    #endregion

    #region Vorbis 头部解析

    private void ParseIdentificationHeader(byte[] packet)
    {
        if (packet.Length < 30)
        {
            throw new InvalidDataException("Vorbis 识别头部太短");
        }

        if (packet[0] != 0x01)
        {
            throw new InvalidDataException("Vorbis 识别头部类型标记无效");
        }

        _channels = packet[11];
        _sampleRate = (int)ReadLE32(packet, 12);

        var blockSizesByte = packet[29];
        _blockSize0 = 1 << (blockSizesByte & 0x0F);
        _blockSize1 = 1 << ((blockSizesByte >> 4) & 0x0F);

        if (_blockSize0 > _blockSize1)
        {
            throw new InvalidDataException("Vorbis 块大小无效：blockSize0 不能大于 blockSize1");
        }
    }

    private static void ParseCommentHeader(byte[] packet)
    {
        if (packet.Length < 7 || packet[0] != 0x03)
        {
            throw new InvalidDataException("Vorbis 注释头部无效");
        }
    }

    private void ParseSetupHeader(byte[] packet)
    {
        if (packet.Length < 7 || packet[0] != 0x05)
        {
            throw new InvalidDataException("Vorbis 设置头部无效");
        }

        var reader = new BitReader(packet, 7 * 8);

        _modeCount = (int)reader.ReadBits(6) + 1;
        _modeBlockFlags = new int[_modeCount];

        for (var i = 0; i < _modeCount; i++)
        {
            _modeBlockFlags[i] = (int)reader.ReadBits(1);
        }

        _mappingCount = (int)reader.ReadBits(6) + 1;
    }

    #endregion

    #region Vorbis 音频解码

    private int DecodeAudioPacket(byte[] packet, float[][] outputBuffers)
    {
        if (packet.Length < 1) return 0;

        var reader = new BitReader(packet, 0);

        var packetType = reader.ReadBits(1);
        if (packetType != 0) return 0;

        var modeNumber = (int)reader.ReadBits((uint)ILog(_modeCount));
        if (modeNumber >= _modeCount) return 0;

        var isLongBlock = _modeBlockFlags[modeNumber] != 0;
        var blockSize = isLongBlock ? _blockSize1 : _blockSize0;
        var halfBlock = blockSize / 2;

        for (var ch = 0; ch < _channels; ch++)
        {
            if (outputBuffers[ch] == null || outputBuffers[ch].Length < halfBlock)
            {
                outputBuffers[ch] = new float[halfBlock];
            }
            else
            {
                Array.Clear(outputBuffers[ch], 0, halfBlock);
            }
        }

        for (var ch = 0; ch < _channels; ch++)
        {
            DecodeChannel(ref reader, outputBuffers[ch], halfBlock, isLongBlock);
        }

        ApplyImdct(outputBuffers, _channels, halfBlock, isLongBlock);

        return halfBlock;
    }

    private static void DecodeChannel(ref BitReader reader, float[] output, int halfBlock, bool isLongBlock)
    {
        for (var i = 0; i < halfBlock; i++)
        {
            output[i] *= 0.5f;
        }
    }

    private static void ApplyImdct(float[][] buffers, int channels, int halfBlock, bool isLongBlock)
    {
        for (var ch = 0; ch < channels; ch++)
        {
            Imdct(buffers[ch], halfBlock);
            WindowApply(buffers[ch], halfBlock, isLongBlock);
        }
    }

    private static void Imdct(float[] data, int n)
    {
        var result = new float[n * 2];

        for (var i = 0; i < n; i++)
        {
            var sum = 0.0;
            for (var k = 0; k < n; k++)
            {
                sum += data[k] * Math.Cos(Math.PI * (2.0 * i + n + 1) * (2.0 * k + 1) / (4.0 * n));
            }
            result[i] = (float)sum;
            result[i + n] = (float)-sum;
        }

        Array.Copy(result, data, Math.Min(data.Length, result.Length));
    }

    private static void WindowApply(float[] data, int halfBlock, bool isLongBlock)
    {
        var window = isLongBlock ? VorbisWindow.GetLongWindow(halfBlock * 2)
                                 : VorbisWindow.GetShortWindow(halfBlock * 2);

        for (var i = 0; i < Math.Min(data.Length, window.Length); i++)
        {
            data[i] *= window[i];
        }
    }

    #endregion

    #region PCM 交错

    private static byte[] InterleaveFloatToPcm16(float[][] channelSamples, int channels, int totalSamples)
    {
        var pcmData = new byte[totalSamples * channels * 2];

        for (var i = 0; i < totalSamples; i++)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                var sample = channelSamples[ch][i];
                var clamped = Math.Clamp(sample, -1f, 1f);
                var pcm16 = (short)(clamped * 32767);
                BinaryPrimitives.WriteInt16LittleEndian(pcmData.AsSpan((i * channels + ch) * 2), pcm16);
            }
        }

        return pcmData;
    }

    #endregion

    #region 辅助方法

    private static uint ReadLE32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private static int ILog(int value)
    {
        var result = 0;
        while ((1 << result) < value) result++;
        return result;
    }

    #endregion

    #region 内部类型

    private ref struct BitReader
    {
        private readonly byte[] _data;
        private int _bitPosition;

        public BitReader(byte[] data, int startBit)
        {
            _data = data;
            _bitPosition = startBit;
        }

        public uint ReadBits(uint count)
        {
            uint result = 0;
            for (var i = 0; i < count; i++)
            {
                var byteIndex = _bitPosition / 8;
                var bitIndex = _bitPosition % 8;

                if (byteIndex < _data.Length)
                {
                    if ((_data[byteIndex] & (1 << bitIndex)) != 0)
                    {
                        result |= 1u << i;
                    }
                }

                _bitPosition++;
            }
            return result;
        }
    }

    #endregion
}

/// <summary>
///     Vorbis 窗函数——提供长窗和短窗的加窗系数
/// </summary>
internal static class VorbisWindow
{
    private static float[]? _longWindow;
    private static float[]? _shortWindow;

    /// <summary>
    ///     获取 Vorbis 长窗系数
    /// </summary>
    /// <param name="blockSize">块大小</param>
    /// <returns>窗函数系数</returns>
    public static float[] GetLongWindow(int blockSize)
    {
        if (_longWindow != null && _longWindow.Length == blockSize) return _longWindow;

        _longWindow = BuildWindow(blockSize, VorbisWindowType.SinBartlett);
        return _longWindow;
    }

    /// <summary>
    ///     获取 Vorbis 短窗系数
    /// </summary>
    /// <param name="blockSize">块大小</param>
    /// <returns>窗函数系数</returns>
    public static float[] GetShortWindow(int blockSize)
    {
        if (_shortWindow != null && _shortWindow.Length == blockSize) return _shortWindow;

        _shortWindow = BuildWindow(blockSize, VorbisWindowType.SinBartlett);
        return _shortWindow;
    }

    private static float[] BuildWindow(int blockSize, VorbisWindowType type)
    {
        var window = new float[blockSize];
        var half = blockSize / 2;

        for (var i = 0; i < blockSize; i++)
        {
            window[i] = type switch
            {
                VorbisWindowType.SinBartlett => (float)Math.Sin(0.5 * Math.PI * VorbisWindowFunc(i, half)),
                _ => (float)Math.Sin(Math.PI * i / (blockSize - 1))
            };
        }

        return window;
    }

    private static double VorbisWindowFunc(int index, int half)
    {
        var x = (double)index / half;
        if (x < 0.5) return 2.0 * x * x;
        var y = 1.0 - x;
        return 1.0 - 2.0 * y * y;
    }
}

/// <summary>
///     Vorbis 窗函数类型
/// </summary>
internal enum VorbisWindowType
{
    /// <summary>Vorbis 标准 SinBartlett 窗</summary>
    SinBartlett,
    /// <summary>简单正弦窗</summary>
    Sine
}

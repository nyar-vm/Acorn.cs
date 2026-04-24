namespace Acorn.Wav.Data;

/// <summary>
///     WAV 音频格式常量。
/// </summary>
public static class WavConstants
{
    /// <summary>
    ///     RIFF 魔数。
    /// </summary>
    public static ReadOnlySpan<byte> RiffMagic => new byte[] { 0x52, 0x49, 0x46, 0x46 };

    /// <summary>
    ///     WAVE 魔数。
    /// </summary>
    public static ReadOnlySpan<byte> WaveMagic => new byte[] { 0x57, 0x41, 0x56, 0x45 };

    /// <summary>
    ///     fmt 子块 ID。
    /// </summary>
    public static ReadOnlySpan<byte> FmtChunkId => new byte[] { 0x66, 0x6D, 0x74, 0x20 };

    /// <summary>
    ///     data 子块 ID。
    /// </summary>
    public static ReadOnlySpan<byte> DataChunkId => new byte[] { 0x64, 0x61, 0x74, 0x61 };

    /// <summary>
    ///     RIFF 字符串。
    /// </summary>
    public const string RiffTag = "RIFF";

    /// <summary>
    ///     WAVE 字符串。
    /// </summary>
    public const string WaveTag = "WAVE";
}

/// <summary>
///     WAV 音频格式标签。
/// </summary>
public enum WavFormatTag : ushort
{
    /// <summary>
    ///     PCM（无压缩）。
    /// </summary>
    Pcm = 1,

    /// <summary>
    ///     IEEE 浮点。
    /// </summary>
    IeeeFloat = 3,

    /// <summary>
    ///     A-Law。
    /// </summary>
    ALaw = 6,

    /// <summary>
    ///     μ-Law。
    /// </summary>
    MuLaw = 7,

    /// <summary>
    ///     ADPCM。
    /// </summary>
    Adpcm = 2,

    /// <summary>
    ///     IMA ADPCM。
    /// </summary>
    ImaAdpcm = 0x0011,

    /// <summary>
    ///     WAVE_FORMAT_EXTENSIBLE。
    /// </summary>
    Extensible = 0xFFFE
}

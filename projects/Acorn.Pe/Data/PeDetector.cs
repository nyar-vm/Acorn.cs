using Acorn;

namespace Acorn.Pe.Data;

/// <summary>
///     PE 格式检测器。基于魔数 'M' 'Z' 检测二进制数据是否为 PE 格式。
/// </summary>
public readonly struct PeDetector : IDetector
{
    /// <inheritdoc />
    public bool Detect(ReadOnlySpan<byte> header)
    {
        return header.Length >= 2
            && header[0] == (byte)'M'
            && header[1] == (byte)'Z';
    }
}

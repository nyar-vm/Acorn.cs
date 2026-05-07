using Acorn;

namespace Acorn.ELF.Data;

/// <summary>
///     ELF 格式检测器。基于魔数 0x7F 'E' 'L' 'F' 检测二进制数据是否为 ELF 格式。
/// </summary>
public readonly struct ElfDetector : IDetector
{
    /// <inheritdoc />
    public bool Detect(ReadOnlySpan<byte> header)
    {
        return header.Length >= 4
            && header[0] == 0x7F
            && header[1] == (byte)'E'
            && header[2] == (byte)'L'
            && header[3] == (byte)'F';
    }
}

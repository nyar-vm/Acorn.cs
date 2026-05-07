using Acorn;

namespace Acorn.Wasm.Data;

/// <summary>
///     WebAssembly 格式检测器。基于魔数 \0 'a' 's' 'm' 检测二进制数据是否为 WASM 格式。
/// </summary>
public readonly struct WasmDetector : IDetector
{
    /// <inheritdoc />
    public bool Detect(ReadOnlySpan<byte> header)
    {
        return header.Length >= 4
            && header[0] == 0x00
            && header[1] == (byte)'a'
            && header[2] == (byte)'s'
            && header[3] == (byte)'m';
    }
}

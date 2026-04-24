using Acorn.Frame;
using Acorn.FlatBuffers.Data;

namespace Acorn.FlatBuffers.Scanner;

/// <summary>
///     FlatBuffers 扫描器。
/// </summary>
public ref struct FlatBuffersScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="FlatBuffersScanner" /> 结构的新实例。
    /// </summary>
    public FlatBuffersScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 FlatBuffer 头部。
    /// </summary>
    public FlatBuffersScanHeader ScanHeader()
    {
        if (_scanner.Length < 8)
        {
            return new FlatBuffersScanHeader();
        }

        var rootOffset = _scanner.Buffer.ReadU32LE();
        var fileId = _scanner.Buffer.ReadString(4);

        return new FlatBuffersScanHeader
        {
            RootOffset = rootOffset,
            FileIdentifier = fileId
        };
    }
}

/// <summary>
///     FlatBuffers 扫描头部信息。
/// </summary>
public sealed class FlatBuffersScanHeader
{
    /// <summary>
    ///     根表偏移。
    /// </summary>
    public uint RootOffset { get; init; }

    /// <summary>
    ///     文件标识符。
    /// </summary>
    public string FileIdentifier { get; init; } = string.Empty;
}

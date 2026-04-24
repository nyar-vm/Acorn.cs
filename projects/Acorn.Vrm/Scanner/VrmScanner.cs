using Acorn.Frame;
using Acorn.Vrm.Data;

namespace Acorn.Vrm.Scanner;

/// <summary>
///     VRM 扫描器。
/// </summary>
public ref struct VrmScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="VrmScanner" /> 结构的新实例。
    /// </summary>
    public VrmScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 VRM 文件头。
    /// </summary>
    public VrmScanHeader ScanHeader()
    {
        if (_scanner.Length < 4)
        {
            return new VrmScanHeader();
        }

        var magic = _scanner.Buffer.ReadString(4);

        if (magic != "glTF")
        {
            return new VrmScanHeader();
        }

        return new VrmScanHeader { IsGltf = true };
    }

    /// <summary>
    ///     是否可能为 VRM 文件（基于 glTF 魔数）。
    /// </summary>
    public bool IsPossibleVrm()
    {
        if (_scanner.Length < 4) return false;
        var magic = _scanner.Buffer.ReadString(4);
        _scanner.Position = 0;
        return magic == "glTF";
    }
}

/// <summary>
///     VRM 扫描头部信息。
/// </summary>
public sealed class VrmScanHeader
{
    /// <summary>
    ///     是否为 glTF 容器。
    /// </summary>
    public bool IsGltf { get; init; }
}

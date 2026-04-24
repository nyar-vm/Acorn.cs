using Acorn.Frame;
using Acorn.Fbx.Data;

namespace Acorn.Fbx.Scanner;

/// <summary>
///     FBX 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 Autodesk FBX 文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     FBX 文件格式由文件头和节点层级结构组成。
///     扫描器只读取文件头和顶层节点信息，不做完整解码，以实现快速探查。
/// </remarks>
public ref struct FbxScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="FbxScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 FBX 字节数据。</param>
    public FbxScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 FBX 文件头，提取版本信息。
    /// </summary>
    /// <returns>FBX 文件头信息。</returns>
    public FbxScanHeader ScanHeader()
    {
        if (_scanner.Length < FbxConstants.HeaderSize)
        {
            throw new InvalidDataException("FBX 文件数据过短，无法读取文件头");
        }

        var magic = _scanner.Buffer.ReadString(FbxConstants.MagicLength);

        if (!magic.StartsWith("Kaydara FBX Binary"))
        {
            throw new InvalidDataException("FBX 文件签名不匹配");
        }

        var version = (int)_scanner.Buffer.ReadU32LE();

        return new FbxScanHeader
        {
            Version = version,
            MajorVersion = version / 1000,
            IsBinary = true
        };
    }

    /// <summary>
    ///     快速判断数据是否为 FBX 二进制格式。
    /// </summary>
    public bool IsFbxBinary()
    {
        if (_scanner.Length < 19)
        {
            return false;
        }

        var header = _scanner.Data[..19];
        return header.SequenceEqual("Kaydara FBX Binary"u8);
    }
}

/// <summary>
///     FBX 扫描头部信息。
/// </summary>
public sealed class FbxScanHeader
{
    /// <summary>
    ///     FBX 版本号。
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    ///     主版本号。
    /// </summary>
    public int MajorVersion { get; init; }

    /// <summary>
    ///     是否为二进制格式。
    /// </summary>
    public bool IsBinary { get; init; }

    /// <summary>
    ///     版本名称。
    /// </summary>
    public string VersionName => Version switch
    {
        FbxVersions.V61 => "FBX 6.1",
        FbxVersions.V70 => "FBX 7.0",
        FbxVersions.V71 => "FBX 7.1",
        FbxVersions.V72 => "FBX 7.2",
        FbxVersions.V73 => "FBX 7.3",
        FbxVersions.V74 => "FBX 7.4",
        FbxVersions.V75 => "FBX 7.5",
        FbxVersions.V77 => "FBX 7.7",
        _ => $"FBX {Version / 1000}.{Version % 1000 / 100}"
    };
}

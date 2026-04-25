using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Scanner;

/// <summary>
///     Gnosis 字节码模块扫描器，基于 <see cref="SpanScanner" /> 提供对 .gnosis 字节码模块的快速元信息扫描。
/// </summary>
/// <remarks>
///     .gnosis 文件是 Gnosis VM 的字节码模块格式，基于 Game 方言特化。
///     扫描器只读取头部元信息，不做完整的指令解码，以实现快速探查。
///     扫描器需要跳过常量池等变长区域才能正确读取后续符号计数。
/// </remarks>
public ref struct GnosisScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="GnosisScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 .gnosis 字节数据。</param>
    public GnosisScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 .gnosis 模块头部，提取基本模块信息。
    /// </summary>
    /// <returns>Gnosis 模块头部信息。</returns>
    public GnosisScanHeader ScanHeader()
    {
        if (_scanner.Length < GnosisConstants.MinHeaderSize)
        {
            throw new InvalidDataException(".gnosis 文件数据过短，无法读取头部");
        }

        if (!_scanner.MatchMagic(GnosisConstants.MagicNumber))
        {
            throw new InvalidDataException($".gnosis 文件魔数不匹配，期望 GNOS(0x474E4F53)");
        }

        _scanner.ConsumeMagic(GnosisConstants.MagicNumber);

        var version = _scanner.Buffer.ReadU16LE();

        if (_scanner.Buffer.Remaining < 2)
        {
            return new GnosisScanHeader { Version = version };
        }

        var nameLength = _scanner.Buffer.ReadU16LE();
        var moduleName = nameLength > 0 && _scanner.Buffer.Remaining >= nameLength
            ? _scanner.Buffer.ReadString(nameLength)
            : string.Empty;

        var header = new GnosisScanHeader
        {
            Version = version,
            ModuleName = moduleName
        };

        if (_scanner.Buffer.Remaining < 4)
        {
            return header;
        }

        header.ConstantCount = _scanner.Buffer.ReadI32LE();
        SkipConstants(header.ConstantCount);

        if (_scanner.Buffer.Remaining >= 2)
        {
            header.ImportedSymbolCount = _scanner.Buffer.ReadU16LE();
            SkipLeb128StringList(header.ImportedSymbolCount);
        }

        if (_scanner.Buffer.Remaining >= 2)
        {
            header.ExportedSymbolCount = _scanner.Buffer.ReadU16LE();
            SkipLeb128StringList(header.ExportedSymbolCount);
        }

        if (_scanner.Buffer.Remaining >= 2)
        {
            header.DependencyCount = _scanner.Buffer.ReadU16LE();
        }

        return header;
    }

    /// <summary>
    ///     快速判断数据是否为 .gnosis 格式。
    /// </summary>
    public bool IsGnosis()
    {
        if (_scanner.Length < 4)
        {
            return false;
        }

        return _scanner.MatchMagic(GnosisConstants.MagicNumber);
    }

    #region 私有跳过方法

    private void SkipConstants(int count)
    {
        for (var i = 0; i < count && !_scanner.Buffer.IsEnd; i++)
        {
            var tag = _scanner.Buffer.ReadU8();

            switch ((GnosisConstantTag)tag)
            {
                case GnosisConstantTag.String:
                    _scanner.Buffer.ReadLeb128String();
                    break;

                case GnosisConstantTag.Int:
                    _scanner.Buffer.Advance(4);
                    break;

                case GnosisConstantTag.Float:
                    _scanner.Buffer.Advance(4);
                    break;

                default:
                    _scanner.Buffer.Advance(4);
                    break;
            }
        }
    }

    private void SkipLeb128StringList(int count)
    {
        for (var i = 0; i < count && !_scanner.Buffer.IsEnd; i++)
        {
            _scanner.Buffer.ReadLeb128String();
        }
    }

    #endregion
}

/// <summary>
///     Gnosis 扫描头部信息。
/// </summary>
public sealed class GnosisScanHeader
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public ushort Version { get; set; }

    /// <summary>
    ///     模块名称。
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    ///     常量池条目数量。
    /// </summary>
    public int ConstantCount { get; set; }

    /// <summary>
    ///     导入符号数量。
    /// </summary>
    public int ImportedSymbolCount { get; set; }

    /// <summary>
    ///     导出符号数量。
    /// </summary>
    public int ExportedSymbolCount { get; set; }

    /// <summary>
    ///     依赖模块数量。
    /// </summary>
    public int DependencyCount { get; set; }
}

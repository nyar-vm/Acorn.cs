using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Scanner;

/// <summary>
///     Gnosis 字节码模块扫描器，基于 <see cref="SpanScanner" /> 提供对 .gnosis 字节码模块的快速元信息扫描。
/// </summary>
/// <remarks>
///     .gnosis 模块有两种二进制格式：GGBC（ScriptCompiler 输出）和 GNOS（GnosisBackend 输出）。
///     扫描器读取头部和元信息，不做完整的指令解码，以实现快速探查。
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

        var magic = _scanner.Buffer.ReadU32LE();
        var version = _scanner.Buffer.ReadU16LE();

        GnosisModuleFormat format;

        if (magic == GnosisConstants.GgbcMagicValue)
        {
            format = GnosisModuleFormat.Ggbc;
        }
        else if (magic == GnosisConstants.GnosMagicValue)
        {
            format = GnosisModuleFormat.Gnos;
        }
        else
        {
            throw new InvalidDataException($".gnosis 文件魔数无效，期望 GGBC(0x47474243) 或 GNOS(0x474E4F53)，实际 0x{magic:X8}");
        }

        var header = new GnosisScanHeader
        {
            Format = format,
            Version = version
        };

        if (format == GnosisModuleFormat.Ggbc)
        {
            ScanGgbcHeader(ref header);
        }
        else
        {
            ScanGnosHeader(ref header);
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

        return _scanner.MatchMagic(GnosisConstants.GgbcMagic) || _scanner.MatchMagic(GnosisConstants.GnosMagic);
    }

    #region 私有扫描方法

    private void ScanGgbcHeader(ref GnosisScanHeader header)
    {
        if (_scanner.Buffer.Remaining < 2)
        {
            return;
        }

        var nameLength = _scanner.Buffer.ReadU16LE();
        header.ModuleName = nameLength > 0 && _scanner.Buffer.Remaining >= nameLength
            ? _scanner.Buffer.ReadString(nameLength)
            : string.Empty;

        if (_scanner.Buffer.Remaining >= 4)
        {
            header.ConstantCount = _scanner.Buffer.ReadI32LE();
        }

        SkipConstants(header.ConstantCount);

        if (_scanner.Buffer.Remaining >= 2)
        {
            header.ImportedSymbolCount = _scanner.Buffer.ReadU16LE();
        }

        SkipStrings();

        if (_scanner.Buffer.Remaining >= 2)
        {
            header.ExportedSymbolCount = _scanner.Buffer.ReadU16LE();
        }

        SkipStrings();

        if (_scanner.Buffer.Remaining >= 2)
        {
            header.DependencyCount = _scanner.Buffer.ReadU16LE();
        }
    }

    private void ScanGnosHeader(ref GnosisScanHeader header)
    {
        if (_scanner.Buffer.Remaining < 4)
        {
            return;
        }

        var nameLength = _scanner.Buffer.ReadI32LE();
        header.ModuleName = nameLength > 0 && _scanner.Buffer.Remaining >= nameLength
            ? _scanner.Buffer.ReadString(nameLength)
            : string.Empty;

        if (_scanner.Buffer.Remaining >= 4)
        {
            header.ConstantCount = _scanner.Buffer.ReadI32LE();
        }

        if (_scanner.Buffer.Remaining >= 4)
        {
            header.FunctionCount = _scanner.Buffer.ReadI32LE();
        }
    }

    private void SkipConstants(int count)
    {
        for (var i = 0; i < count && !_scanner.Buffer.IsEnd; i++)
        {
            var tag = _scanner.Buffer.ReadU8();

            switch ((GnosisConstantTag)tag)
            {
                case GnosisConstantTag.String:
                    if (_scanner.Buffer.Remaining >= 4)
                    {
                        var len = _scanner.Buffer.ReadI32LE();
                        _scanner.Advance(len);
                    }

                    break;

                case GnosisConstantTag.Int:
                    _scanner.Advance(4);
                    break;

                case GnosisConstantTag.Float:
                    _scanner.Advance(8);
                    break;
            }
        }
    }

    private void SkipStrings()
    {
        while (_scanner.Buffer.Remaining > 0)
        {
            var pos = _scanner.Buffer.Position;
            var b = _scanner.Buffer.ReadU8();

            if (b == 0)
            {
                break;
            }

            while (!_scanner.Buffer.IsEnd && _scanner.Buffer.ReadU8() != 0) { }
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
    ///     模块格式类型。
    /// </summary>
    public GnosisModuleFormat Format { get; set; }

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

    /// <summary>
    ///     函数数量（GNOS 格式）。
    /// </summary>
    public int FunctionCount { get; set; }

    /// <summary>
    ///     格式名称。
    /// </summary>
    public string FormatName => Format switch
    {
        GnosisModuleFormat.Ggbc => "GGBC",
        GnosisModuleFormat.Gnos => "GNOS",
        _ => "未知"
    };
}

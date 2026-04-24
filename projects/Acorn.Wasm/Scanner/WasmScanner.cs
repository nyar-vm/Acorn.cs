using System.Text;
using Acorn.Frame;
using Acorn.Wasm.Data;

namespace Acorn.Wasm.Scanner;

/// <summary>
///     WebAssembly 二进制格式扫描器，基于 <see cref="SpanScanner" /> 提供对 WASM 模块的快速元信息扫描。
/// </summary>
public ref struct WasmScanner : IWasmScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="WasmScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 WASM 字节数据。</param>
    public WasmScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <inheritdoc />
    public uint ReadVersion()
    {
        return _scanner.Buffer.ReadU32LE();
    }

    /// <inheritdoc />
    public string ReadName()
    {
        var length = (int)_scanner.Buffer.ReadLeb128U32();
        return _scanner.Buffer.ReadString(length);
    }

    /// <inheritdoc />
    public uint ReadLeb128UInt32()
    {
        return _scanner.Buffer.ReadLeb128U32();
    }

    /// <summary>
    ///     扫描 WASM 文件头，提取版本信息。
    /// </summary>
    public WasmScanHeader ScanHeader()
    {
        if (_scanner.Length < 8)
        {
            throw new InvalidDataException("WASM 文件数据过短，无法读取文件头");
        }

        if (!_scanner.MatchMagic(WasmConstants.MagicNumber))
        {
            throw new InvalidDataException("WASM 文件魔数不匹配");
        }

        _scanner.ConsumeMagic(WasmConstants.MagicNumber);
        var version = ReadVersion();

        return new WasmScanHeader
        {
            Version = version
        };
    }

    /// <summary>
    ///     扫描 WASM 模块，提取统计信息。
    /// </summary>
    public WasmStatistics ScanStatistics()
    {
        var header = ScanHeader();

        var stats = new WasmStatistics
        {
            Version = header.Version
        };

        while (!_scanner.IsEnd)
        {
            var sectionId = _scanner.Buffer.ReadU8();

            if (sectionId > 12)
            {
                break;
            }

            var sectionSize = (int)_scanner.Buffer.ReadLeb128U32();
            var sectionEnd = _scanner.Position + sectionSize;

            switch ((WasmSectionId)sectionId)
            {
                case WasmSectionId.Type:
                    stats.TypeSectionSize = sectionSize;
                    break;
                case WasmSectionId.Import:
                    stats.ImportCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Function:
                    stats.FunctionCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Table:
                    stats.TableCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Memory:
                    stats.MemoryCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Global:
                    stats.GlobalCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Export:
                    stats.ExportCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Start:
                    stats.HasStartFunction = true;
                    break;
                case WasmSectionId.Element:
                    stats.ElementCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Code:
                    stats.CodeCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case WasmSectionId.Data:
                    stats.DataCount = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
                case (WasmSectionId)12:
                    stats.DataCountSection = (int)_scanner.Buffer.ReadLeb128U32();
                    break;
            }

            _scanner.Position = sectionEnd;
        }

        return stats;
    }

    /// <summary>
    ///     扫描 WASM 模块，提取导出名称列表。
    /// </summary>
    public List<(WasmExternalKind Kind, string Name)> ScanExports()
    {
        var exports = new List<(WasmExternalKind, string)>();
        var header = ScanHeader();

        while (!_scanner.IsEnd)
        {
            var sectionId = _scanner.Buffer.ReadU8();

            if (sectionId > 12)
            {
                break;
            }

            var sectionSize = (int)_scanner.Buffer.ReadLeb128U32();
            var sectionEnd = _scanner.Position + sectionSize;

            if ((WasmSectionId)sectionId == WasmSectionId.Export)
            {
                var count = (int)_scanner.Buffer.ReadLeb128U32();

                for (var i = 0; i < count; i++)
                {
                    var name = ReadName();
                    var kind = (WasmExternalKind)_scanner.Buffer.ReadU8();
                    _scanner.Buffer.ReadLeb128U32();
                    exports.Add((kind, name));
                }

                break;
            }

            _scanner.Position = sectionEnd;
        }

        return exports;
    }
}

/// <summary>
///     WASM 扫描头部信息。
/// </summary>
public sealed class WasmScanHeader
{
    /// <summary>
    ///     WASM 版本号。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     版本名称。
    /// </summary>
    public string VersionName => Version switch
    {
        1 => "MVP",
        2 => "Feature Test",
        _ => $"0x{Version:X8}"
    };
}

/// <summary>
///     WASM 模块统计信息。
/// </summary>
public sealed class WasmStatistics
{
    /// <summary>
    ///     WASM 版本号。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     类型段大小。
    /// </summary>
    public int TypeSectionSize { get; set; }

    /// <summary>
    ///     导入数量。
    /// </summary>
    public int ImportCount { get; set; }

    /// <summary>
    ///     函数数量。
    /// </summary>
    public int FunctionCount { get; set; }

    /// <summary>
    ///     表数量。
    /// </summary>
    public int TableCount { get; set; }

    /// <summary>
    ///     内存数量。
    /// </summary>
    public int MemoryCount { get; set; }

    /// <summary>
    ///     全局变量数量。
    /// </summary>
    public int GlobalCount { get; set; }

    /// <summary>
    ///     导出数量。
    /// </summary>
    public int ExportCount { get; set; }

    /// <summary>
    ///     是否有起始函数。
    /// </summary>
    public bool HasStartFunction { get; set; }

    /// <summary>
    ///     元素段数量。
    /// </summary>
    public int ElementCount { get; set; }

    /// <summary>
    ///     代码段数量。
    /// </summary>
    public int CodeCount { get; set; }

    /// <summary>
    ///     数据段数量。
    /// </summary>
    public int DataCount { get; set; }

    /// <summary>
    ///     DataCount 段值。
    /// </summary>
    public int DataCountSection { get; set; }
}

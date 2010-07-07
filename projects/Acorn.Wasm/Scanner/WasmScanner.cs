using System.Text;
using Acorn.Frame;
using Acorn.Wasm.Data;

namespace Acorn.Wasm.Scanner;

/// <summary>
///     WebAssembly 二进制扫描器，基于 <see cref="ByteBuffer" /> 提供零分配的快速元信息扫描。
/// </summary>
/// <remarks>
///     扫描器解析 Wasm 模块的结构，提取版本、段数量、函数数量等元信息，
///     不做完整的指令解码，以实现零分配高性能扫描。
/// </remarks>
public ref struct WasmScanner
{
    private static ReadOnlySpan<byte> WasmMagic => WasmConstants.MagicNumber;

    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="WasmScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 Wasm 字节数据。</param>
    public WasmScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前扫描位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     数据总长度。
    /// </summary>
    public int Length => _buffer.Length;

    /// <summary>
    ///     是否已到达数据末尾。
    /// </summary>
    public bool IsEndOfData => _buffer.IsEnd;

    /// <summary>
    ///     扫描 Wasm 模块头，验证魔数并提取版本信息。
    /// </summary>
    /// <returns>Wasm 版本号。</returns>
    public uint ScanHeader()
    {
        if (_buffer.Length < 8)
        {
            throw new InvalidDataException("Wasm 文件数据过短，无法读取头信息");
        }

        if (!_buffer.ConsumeMagic(WasmMagic))
        {
            throw new InvalidDataException("无效的 Wasm 魔数");
        }

        return _buffer.ReadU32LE();
    }

    /// <summary>
    ///     扫描 Wasm 模块，提取完整的统计信息。
    /// </summary>
    /// <returns>Wasm 模块统计信息。</returns>
    public WasmStatistics ScanStatistics()
    {
        var version = ScanHeader();
        var sectionCounts = new Dictionary<WasmSectionId, int>();
        var typeCount = 0u;
        var functionCount = 0u;
        var importCount = 0u;
        var exportCount = 0u;
        var tableCount = 0u;
        var memoryCount = 0u;
        var globalCount = 0u;
        var elementCount = 0u;
        var codeCount = 0u;
        var dataCount = 0u;
        var customSectionNames = new List<string>();

        while (!_buffer.IsEnd)
        {
            var sectionId = _buffer.ReadU8();
            var sectionSize = _buffer.ReadLeb128U32();
            var sectionEnd = _buffer.Position + (int)sectionSize;

            var wasmSectionId = (WasmSectionId)sectionId;
            if (!sectionCounts.ContainsKey(wasmSectionId))
            {
                sectionCounts[wasmSectionId] = 0;
            }

            sectionCounts[wasmSectionId]++;

            switch (wasmSectionId)
            {
                case WasmSectionId.Type:
                    typeCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Function:
                    functionCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Import:
                    importCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Export:
                    exportCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Table:
                    tableCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Memory:
                    memoryCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Global:
                    globalCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Element:
                    elementCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Code:
                    codeCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Data:
                    dataCount = _buffer.ReadLeb128U32();
                    break;

                case WasmSectionId.Custom:
                    var nameLength = _buffer.ReadLeb128U32();
                    var name = _buffer.ReadString((int)nameLength);
                    customSectionNames.Add(name);
                    break;
            }

            _buffer.Position = sectionEnd;
        }

        return new WasmStatistics
        {
            Version = version,
            SectionCounts = sectionCounts,
            TypeCount = typeCount,
            FunctionCount = functionCount,
            ImportCount = importCount,
            ExportCount = exportCount,
            TableCount = tableCount,
            MemoryCount = memoryCount,
            GlobalCount = globalCount,
            ElementCount = elementCount,
            CodeCount = codeCount,
            DataCount = dataCount,
            CustomSectionNames = customSectionNames
        };
    }

    /// <summary>
    ///     扫描 Wasm 模块，提取所有导出项的名称和类型。
    /// </summary>
    /// <returns>导出项列表。</returns>
    public List<WasmExportInfo> ScanExports()
    {
        ScanHeader();
        var exports = new List<WasmExportInfo>();

        while (!_buffer.IsEnd)
        {
            var sectionId = _buffer.ReadU8();
            var sectionSize = _buffer.ReadLeb128U32();
            var sectionEnd = _buffer.Position + (int)sectionSize;

            if ((WasmSectionId)sectionId == WasmSectionId.Export)
            {
                var exportCount = _buffer.ReadLeb128U32();
                for (var i = 0; i < exportCount; i++)
                {
                    var nameLength = _buffer.ReadLeb128U32();
                    var name = _buffer.ReadString((int)nameLength);
                    var kind = _buffer.ReadU8();
                    var index = _buffer.ReadLeb128U32();

                    exports.Add(new WasmExportInfo
                    {
                        Name = name,
                        Kind = (WasmExternalKind)kind,
                        Index = index
                    });
                }
            }

            _buffer.Position = sectionEnd;
        }

        return exports;
    }
}

/// <summary>
///     Wasm 模块统计信息。
/// </summary>
public sealed class WasmStatistics
{
    /// <summary>
    ///     Wasm 版本号。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     各段的数量统计。
    /// </summary>
    public Dictionary<WasmSectionId, int> SectionCounts { get; init; } = new();

    /// <summary>
    ///     类型数量。
    /// </summary>
    public uint TypeCount { get; init; }

    /// <summary>
    ///     函数数量。
    /// </summary>
    public uint FunctionCount { get; init; }

    /// <summary>
    ///     导入数量。
    /// </summary>
    public uint ImportCount { get; init; }

    /// <summary>
    ///     导出数量。
    /// </summary>
    public uint ExportCount { get; init; }

    /// <summary>
    ///     表数量。
    /// </summary>
    public uint TableCount { get; init; }

    /// <summary>
    ///     内存数量。
    /// </summary>
    public uint MemoryCount { get; init; }

    /// <summary>
    ///     全局变量数量。
    /// </summary>
    public uint GlobalCount { get; init; }

    /// <summary>
    ///     元素段数量。
    /// </summary>
    public uint ElementCount { get; init; }

    /// <summary>
    ///     代码段数量。
    /// </summary>
    public uint CodeCount { get; init; }

    /// <summary>
    ///     数据段数量。
    /// </summary>
    public uint DataCount { get; init; }

    /// <summary>
    ///     自定义段名称列表。
    /// </summary>
    public List<string> CustomSectionNames { get; init; } = new();
}

/// <summary>
///     Wasm 导出项信息。
/// </summary>
public sealed class WasmExportInfo
{
    /// <summary>
    ///     导出名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     导出种类。
    /// </summary>
    public WasmExternalKind Kind { get; init; }

    /// <summary>
    ///     导出项索引。
    /// </summary>
    public uint Index { get; init; }
}

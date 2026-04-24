using System.Text;
using Acorn.Frame;
using Acorn.Spirv.Data;

namespace Acorn.Spirv.Scanner;

/// <summary>
///     SPIR-V 格式扫描器的默认实现，基于 <see cref="SpanScanner" /> 提供零分配的快速数据扫描。
/// </summary>
/// <remarks>
///     SPIR-V 是 Khronos 定义的着色器二进制中间语言，用于 Vulkan、OpenCL 等图形和计算 API。
///     扫描器只读取文件头和关键指令，不做完整的指令解码，以实现快速探查。
/// </remarks>
public ref struct SpirvScanner : ISpirvScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="SpirvScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 SPIR-V 二进制数据。</param>
    public SpirvScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <inheritdoc />
    public uint ReadSpirvWord()
    {
        return _scanner.Buffer.ReadU32LE();
    }

    /// <inheritdoc />
    public (ushort Opcode, ushort WordCount) ReadInstructionHeader()
    {
        var word = ReadSpirvWord();
        var opcode = (ushort)(word & 0xFFFF);
        var wordCount = (ushort)(word >> 16);
        return (opcode, wordCount);
    }

    /// <inheritdoc />
    public string ReadSpirvString()
    {
        var bytes = new List<byte>();
        var startWordPosition = _scanner.Position;

        while (_scanner.Position + 4 <= _scanner.Length)
        {
            var word = ReadSpirvWord();

            for (var i = 0; i < 4; i++)
            {
                var b = (byte)((word >> (i * 8)) & 0xFF);
                bytes.Add(b);

                if (b == 0)
                {
                    var consumedWords = (_scanner.Position - startWordPosition) / 4;
                    var alignedWords = (bytes.Count + 3) / 4;

                    if (alignedWords > consumedWords)
                    {
                        var extraWords = alignedWords - consumedWords;
                        _scanner.Advance(extraWords * 4);
                    }

                    var charCount = bytes.Count - 1;

                    for (var j = 0; j < bytes.Count; j++)
                    {
                        if (bytes[j] == 0)
                        {
                            charCount = j;
                            break;
                        }
                    }

                    return Encoding.UTF8.GetString(bytes.ToArray(), 0, charCount);
                }
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    ///     扫描 SPIR-V 文件头，提取基本模块信息。
    /// </summary>
    /// <returns>SPIR-V 文件头信息。</returns>
    public SpirvHeader ScanHeader()
    {
        if (_scanner.Length < 20)
        {
            throw new InvalidDataException("SPIR-V 文件数据过短，无法读取文件头");
        }

        var magicNumber = _scanner.Buffer.ReadU32At(0);

        if (magicNumber != SpirvConstants.MagicNumber)
        {
            throw new InvalidDataException($"SPIR-V 文件魔数不匹配，期望 0x07230203，实际 0x{magicNumber:X8}");
        }

        var version = _scanner.Buffer.ReadU32At(4);
        var generatorMagic = _scanner.Buffer.ReadU32At(8);
        var bound = _scanner.Buffer.ReadU32At(12);
        var schema = _scanner.Buffer.ReadU32At(16);

        return new SpirvHeader
        {
            Version = version,
            GeneratorMagic = generatorMagic,
            Bound = bound,
            Schema = schema
        };
    }

    /// <summary>
    ///     扫描 SPIR-V 模块，提取统计信息。
    /// </summary>
    /// <returns>SPIR-V 统计信息。</returns>
    public SpirvStatistics ScanStatistics()
    {
        var header = ScanHeader();

        var entryPointCount = 0;
        var capabilityCount = 0;
        var decorationCount = 0;
        var typeCount = 0;
        var nameCount = 0;
        var instructionCount = 0;
        var entryPoints = new List<(SpirvExecutionModel ExecutionModel, string Name)>();
        var capabilities = new List<SpirvCapability>();

        var offset = 20;

        while (offset + 4 <= _scanner.Length)
        {
            var firstWord = _scanner.Buffer.ReadI32At(offset, false) & 0xFFFFFFFF;
            var wordCount = (ushort)(firstWord >> 16);
            var opcode = (SpirvOpCode)(firstWord & 0xFFFF);

            if (wordCount == 0)
            {
                break;
            }

            instructionCount++;

            if (opcode == SpirvOpCode.OpEntryPoint)
            {
                entryPointCount++;

                if (offset + 12 <= _scanner.Length && wordCount >= 3)
                {
                    var executionModel = (SpirvExecutionModel)_scanner.Buffer.ReadU32At(offset + 4);
                    var nameStart = offset + 12;
                    var name = _scanner.Buffer.ReadStringAt(nameStart);
                    entryPoints.Add((executionModel, name));
                }
            }
            else if (opcode == SpirvOpCode.OpCapability)
            {
                capabilityCount++;

                if (offset + 8 <= _scanner.Length)
                {
                    var capability = (SpirvCapability)_scanner.Buffer.ReadU32At(offset + 4);
                    capabilities.Add(capability);
                }
            }
            else if (opcode is SpirvOpCode.OpDecorate or SpirvOpCode.OpMemberDecorate)
            {
                decorationCount++;
            }
            else if (opcode is >= SpirvOpCode.OpTypeVoid and <= SpirvOpCode.OpTypeForwardPointer
                     or SpirvOpCode.OpTypePipe
                     or SpirvOpCode.OpTypeAccelerationStructureKHR
                     or SpirvOpCode.OpTypeCooperativeMatrixNV)
            {
                typeCount++;
            }
            else if (opcode == SpirvOpCode.OpName)
            {
                nameCount++;
            }

            offset += wordCount * 4;
        }

        return new SpirvStatistics
        {
            Version = header.Version,
            GeneratorMagic = header.GeneratorMagic,
            Bound = header.Bound,
            InstructionCount = instructionCount,
            EntryPointCount = entryPointCount,
            CapabilityCount = capabilityCount,
            DecorationCount = decorationCount,
            TypeCount = typeCount,
            NameCount = nameCount,
            EntryPoints = entryPoints,
            Capabilities = capabilities
        };
    }

    /// <summary>
    ///     扫描 SPIR-V 模块，提取入口点名称列表。
    /// </summary>
    /// <returns>入口点名称列表。</returns>
    public List<string> ScanEntryPointNames()
    {
        var names = new List<string>();
        var offset = 20;

        while (offset + 4 <= _scanner.Length)
        {
            var firstWord = _scanner.Buffer.ReadI32At(offset, false) & 0xFFFFFFFF;
            var wordCount = (ushort)(firstWord >> 16);
            var opcode = (SpirvOpCode)(firstWord & 0xFFFF);

            if (wordCount == 0)
            {
                break;
            }

            if (opcode == SpirvOpCode.OpEntryPoint && wordCount >= 3)
            {
                var nameStart = offset + 12;
                var name = _scanner.Buffer.ReadStringAt(nameStart);
                names.Add(name);
            }

            offset += wordCount * 4;
        }

        return names;
    }
}

/// <summary>
///     SPIR-V 文件头信息。
/// </summary>
public sealed class SpirvHeader
{
    /// <summary>
    ///     SPIR-V 版本号。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     生成器魔数。
    /// </summary>
    public uint GeneratorMagic { get; init; }

    /// <summary>
    ///     ID 绑定值。
    /// </summary>
    public uint Bound { get; init; }

    /// <summary>
    ///     保留字。
    /// </summary>
    public uint Schema { get; init; }

    /// <summary>
    ///     版本号字符串表示（如 "1.0"、"1.3"、"1.5"）。
    /// </summary>
    public string VersionString
    {
        get
        {
            var major = (Version >> 16) & 0xFF;
            var minor = (Version >> 8) & 0xFF;
            return $"{major}.{minor}";
        }
    }
}

/// <summary>
///     SPIR-V 模块统计信息。
/// </summary>
public sealed class SpirvStatistics
{
    /// <summary>
    ///     SPIR-V 版本号。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     生成器魔数。
    /// </summary>
    public uint GeneratorMagic { get; init; }

    /// <summary>
    ///     ID 绑定值。
    /// </summary>
    public uint Bound { get; init; }

    /// <summary>
    ///     指令总数。
    /// </summary>
    public int InstructionCount { get; init; }

    /// <summary>
    ///     入口点数量。
    /// </summary>
    public int EntryPointCount { get; init; }

    /// <summary>
    ///     能力声明数量。
    /// </summary>
    public int CapabilityCount { get; init; }

    /// <summary>
    ///     装饰指令数量。
    /// </summary>
    public int DecorationCount { get; init; }

    /// <summary>
    ///     类型指令数量。
    /// </summary>
    public int TypeCount { get; init; }

    /// <summary>
    ///     名称指令数量。
    /// </summary>
    public int NameCount { get; init; }

    /// <summary>
    ///     入口点列表（执行模型和名称）。
    /// </summary>
    public IReadOnlyList<(SpirvExecutionModel ExecutionModel, string Name)> EntryPoints { get; init; } = [];

    /// <summary>
    ///     能力声明列表。
    /// </summary>
    public IReadOnlyList<SpirvCapability> Capabilities { get; init; } = [];

    /// <summary>
    ///     版本号字符串表示。
    /// </summary>
    public string VersionString
    {
        get
        {
            var major = (Version >> 16) & 0xFF;
            var minor = (Version >> 8) & 0xFF;
            return $"{major}.{minor}";
        }
    }
}

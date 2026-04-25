using System.Text;
using Acorn.Frame;
using Acorn.Spirv.Data;

namespace Acorn.Spirv.Decode;

/// <summary>
///     SPIR-V 模块解码器，将 Khronus SPIR-V 二进制中间语言格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     <para>
///     SPIR-V 是 Khronos 定义的着色器二进制中间语言，用于 Vulkan、OpenCL 等图形和计算 API。
///     解码器解析完整的 SPIR-V 模块结构，提取文件头、指令流、入口点和装饰信息。
///     </para>
///     <para>
///     调用 <see cref="DecodeAll" /> 可一次解析并缓存所有数据，后续通过属性访问入口点、装饰、名称和类型信息，
///     避免重复解析。单独调用 <see cref="DecodeEntryPoints" /> 等方法会每次重新解析，适用于仅需部分数据的场景。
///     </para>
/// </remarks>
public ref struct SpirvDecoder
{
    private ByteBuffer _buffer;

    private SpirvModuleData? _cachedModule;

    /// <summary>
    ///     初始化 <see cref="SpirvDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">SPIR-V 二进制数据。</param>
    public SpirvDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取最近一次 <see cref="DecodeAll" /> 调用缓存的模块数据，未调用前为 null。
    /// </summary>
    public SpirvModuleData? CachedModule => _cachedModule;

    /// <summary>
    ///     一次解析 SPIR-V 模块的所有数据并缓存，后续可通过 <see cref="CachedModule" /> 访问。
    /// </summary>
    /// <returns>解码后的模块数据。</returns>
    public SpirvModuleData DecodeAll()
    {
        var header = ReadHeader();
        var instructions = ReadInstructions(header.Bound);

        var entryPoints = new List<SpirvEntryPoint>();
        var decorations = new List<SpirvDecorationInfo>();
        var names = new List<SpirvName>();
        var types = new List<SpirvTypeInfo>();

        foreach (var instruction in instructions)
        {
            if (instruction.Opcode == SpirvOpCode.OpEntryPoint)
            {
                entryPoints.Add(ParseEntryPoint(instruction));
            }
            else if (instruction.Opcode == SpirvOpCode.OpDecorate)
            {
                decorations.Add(ParseDecorate(instruction));
            }
            else if (instruction.Opcode == SpirvOpCode.OpMemberDecorate)
            {
                decorations.Add(ParseMemberDecorate(instruction));
            }
            else if (instruction.Opcode == SpirvOpCode.OpName)
            {
                names.Add(ParseName(instruction));
            }
            else if (IsTypeInstruction(instruction.Opcode))
            {
                types.Add(ParseTypeInfo(instruction));
            }
        }

        _cachedModule = new SpirvModuleData
        {
            MagicNumber = header.MagicNumber,
            Version = header.Version,
            GeneratorMagic = header.GeneratorMagic,
            Bound = header.Bound,
            Schema = header.Schema,
            Instructions = instructions,
            EntryPoints = entryPoints,
            Decorations = decorations,
            Names = names,
            Types = types
        };

        return _cachedModule;
    }

    /// <summary>
    ///     从 SPIR-V 二进制数据解码模块数据。
    /// </summary>
    /// <returns>解码后的模块数据。</returns>
    public SpirvModuleData Decode()
    {
        var header = ReadHeader();
        var instructions = ReadInstructions(header.Bound);

        return new SpirvModuleData
        {
            MagicNumber = header.MagicNumber,
            Version = header.Version,
            GeneratorMagic = header.GeneratorMagic,
            Bound = header.Bound,
            Schema = header.Schema,
            Instructions = instructions
        };
    }

    /// <summary>
    ///     从 SPIR-V 二进制数据中提取入口点信息。
    /// </summary>
    /// <remarks>
    ///     如果已调用 <see cref="DecodeAll" />，则直接返回缓存数据，避免重复解析。
    /// </remarks>
    /// <returns>入口点信息列表。</returns>
    public IReadOnlyList<SpirvEntryPoint> DecodeEntryPoints()
    {
        if (_cachedModule != null)
        {
            return _cachedModule.EntryPoints;
        }

        var header = ReadHeader();
        var instructions = ReadInstructions(header.Bound);

        var entryPoints = new List<SpirvEntryPoint>();

        foreach (var instruction in instructions)
        {
            if (instruction.Opcode == SpirvOpCode.OpEntryPoint)
            {
                entryPoints.Add(ParseEntryPoint(instruction));
            }
        }

        return entryPoints;
    }

    /// <summary>
    ///     从 SPIR-V 二进制数据中提取装饰信息。
    /// </summary>
    /// <remarks>
    ///     如果已调用 <see cref="DecodeAll" />，则直接返回缓存数据，避免重复解析。
    /// </remarks>
    /// <returns>装饰信息列表。</returns>
    public IReadOnlyList<SpirvDecorationInfo> DecodeDecorations()
    {
        if (_cachedModule != null)
        {
            return _cachedModule.Decorations;
        }

        var header = ReadHeader();
        var instructions = ReadInstructions(header.Bound);

        var decorations = new List<SpirvDecorationInfo>();

        foreach (var instruction in instructions)
        {
            if (instruction.Opcode == SpirvOpCode.OpDecorate)
            {
                decorations.Add(ParseDecorate(instruction));
            }
            else if (instruction.Opcode == SpirvOpCode.OpMemberDecorate)
            {
                decorations.Add(ParseMemberDecorate(instruction));
            }
        }

        return decorations;
    }

    /// <summary>
    ///     从 SPIR-V 二进制数据中提取名称信息。
    /// </summary>
    /// <remarks>
    ///     如果已调用 <see cref="DecodeAll" />，则直接返回缓存数据，避免重复解析。
    /// </remarks>
    /// <returns>名称信息列表。</returns>
    public IReadOnlyList<SpirvName> DecodeNames()
    {
        if (_cachedModule != null)
        {
            return _cachedModule.Names;
        }

        var header = ReadHeader();
        var instructions = ReadInstructions(header.Bound);

        var names = new List<SpirvName>();

        foreach (var instruction in instructions)
        {
            if (instruction.Opcode == SpirvOpCode.OpName)
            {
                names.Add(ParseName(instruction));
            }
        }

        return names;
    }

    /// <summary>
    ///     从 SPIR-V 二进制数据中提取类型信息。
    /// </summary>
    /// <remarks>
    ///     如果已调用 <see cref="DecodeAll" />，则直接返回缓存数据，避免重复解析。
    /// </remarks>
    /// <returns>类型信息列表。</returns>
    public IReadOnlyList<SpirvTypeInfo> DecodeTypes()
    {
        if (_cachedModule != null)
        {
            return _cachedModule.Types;
        }

        var header = ReadHeader();
        var instructions = ReadInstructions(header.Bound);

        var types = new List<SpirvTypeInfo>();

        foreach (var instruction in instructions)
        {
            if (IsTypeInstruction(instruction.Opcode))
            {
                types.Add(ParseTypeInfo(instruction));
            }
        }

        return types;
    }

    #region 私有解析方法

    private SpirvHeaderInfo ReadHeader()
    {
        if (_buffer.Length < 20)
        {
            throw new InvalidDataException("SPIR-V 文件数据过短，无法读取文件头");
        }

        var magicNumber = _buffer.ReadU32LE();

        if (magicNumber != SpirvConstants.MagicNumber)
        {
            throw new InvalidDataException($"SPIR-V 文件魔数不匹配，期望 0x07230203，实际 0x{magicNumber:X8}");
        }

        var version = _buffer.ReadU32LE();
        var generatorMagic = _buffer.ReadU32LE();
        var bound = _buffer.ReadU32LE();
        var schema = _buffer.ReadU32LE();

        return new SpirvHeaderInfo
        {
            MagicNumber = magicNumber,
            Version = version,
            GeneratorMagic = generatorMagic,
            Bound = bound,
            Schema = schema
        };
    }

    private List<SpirvInstruction> ReadInstructions(uint bound)
    {
        var instructions = new List<SpirvInstruction>();

        while (!_buffer.IsEnd && _buffer.Remaining >= 4)
        {
            var firstWord = _buffer.ReadU32LE();
            var wordCount = (ushort)(firstWord >> 16);
            var opcode = (SpirvOpCode)(firstWord & 0xFFFF);

            if (wordCount == 0)
            {
                throw new InvalidDataException("SPIR-V 指令字数不能为 0");
            }

            var operandCount = wordCount - 1;
            var operands = new uint[operandCount];

            for (var i = 0; i < operandCount; i++)
            {
                if (_buffer.Remaining < 4)
                {
                    throw new InvalidDataException("SPIR-V 指令操作数超出数据范围");
                }

                operands[i] = _buffer.ReadU32LE();
            }

            instructions.Add(new SpirvInstruction
            {
                Opcode = opcode,
                WordCount = wordCount,
                Operands = operands
            });
        }

        return instructions;
    }

    private static SpirvEntryPoint ParseEntryPoint(SpirvInstruction instruction)
    {
        if (instruction.Operands.Count < 3)
        {
            throw new InvalidDataException("OpEntryPoint 指令操作数不足");
        }

        var executionModel = (SpirvExecutionModel)instruction.Operands[0];
        var entryPointId = instruction.Operands[1];
        var name = ReadStringFromOperands(instruction.Operands, 2, out var nameEndIndex);

        var interfaceIds = new List<uint>();

        for (var i = nameEndIndex; i < instruction.Operands.Count; i++)
        {
            interfaceIds.Add(instruction.Operands[i]);
        }

        return new SpirvEntryPoint
        {
            ExecutionModel = executionModel,
            EntryPointId = entryPointId,
            Name = name,
            InterfaceIds = interfaceIds
        };
    }

    private static SpirvDecorationInfo ParseDecorate(SpirvInstruction instruction)
    {
        if (instruction.Operands.Count < 2)
        {
            throw new InvalidDataException("OpDecorate 指令操作数不足");
        }

        var targetId = instruction.Operands[0];
        var decoration = (SpirvDecoration)instruction.Operands[1];
        var extraOperands = instruction.Operands.Skip(2).ToArray();

        return new SpirvDecorationInfo
        {
            TargetId = targetId,
            Decoration = decoration,
            ExtraOperands = extraOperands
        };
    }

    private static SpirvDecorationInfo ParseMemberDecorate(SpirvInstruction instruction)
    {
        if (instruction.Operands.Count < 3)
        {
            throw new InvalidDataException("OpMemberDecorate 指令操作数不足");
        }

        var targetId = instruction.Operands[0];
        var decoration = (SpirvDecoration)instruction.Operands[2];
        var extraOperands = instruction.Operands.Skip(3).ToArray();

        return new SpirvDecorationInfo
        {
            TargetId = targetId,
            Decoration = decoration,
            ExtraOperands = extraOperands
        };
    }

    private static SpirvName ParseName(SpirvInstruction instruction)
    {
        if (instruction.Operands.Count < 2)
        {
            throw new InvalidDataException("OpName 指令操作数不足");
        }

        var targetId = instruction.Operands[0];
        var name = ReadStringFromOperands(instruction.Operands, 1, out _);

        return new SpirvName
        {
            TargetId = targetId,
            Name = name
        };
    }

    private static SpirvTypeInfo ParseTypeInfo(SpirvInstruction instruction)
    {
        var resultId = instruction.Operands.Count > 0 ? instruction.Operands[0] : 0;
        var operands = instruction.Operands.Skip(1).ToArray();

        return new SpirvTypeInfo
        {
            ResultId = resultId,
            Opcode = instruction.Opcode,
            Operands = operands
        };
    }

    private static string ReadStringFromOperands(IReadOnlyList<uint> operands, int startIndex, out int endIndex)
    {
        var bytes = new List<byte>();

        var i = startIndex;

        while (i < operands.Count)
        {
            var word = operands[i];
            bytes.Add((byte)(word & 0xFF));

            if ((word & 0xFF) == 0)
            {
                break;
            }

            bytes.Add((byte)((word >> 8) & 0xFF));

            if ((word >> 8 & 0xFF) == 0)
            {
                break;
            }

            bytes.Add((byte)((word >> 16) & 0xFF));

            if ((word >> 16 & 0xFF) == 0)
            {
                break;
            }

            bytes.Add((byte)((word >> 24) & 0xFF));

            if ((word >> 24 & 0xFF) == 0)
            {
                break;
            }

            i++;
        }

        endIndex = i + 1;

        var charCount = bytes.Count;

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

    private static bool IsTypeInstruction(SpirvOpCode opcode)
    {
        return opcode is >= SpirvOpCode.OpTypeVoid and <= SpirvOpCode.OpTypeForwardPointer
               or SpirvOpCode.OpTypePipe
               or SpirvOpCode.OpTypeAccelerationStructureKHR
               or SpirvOpCode.OpTypeCooperativeMatrixNV
               or SpirvOpCode.OpTypeVmeImageINTEL
               or SpirvOpCode.OpTypeAvcImePayloadINTEL
               or SpirvOpCode.OpTypeAvcRefPayloadINTEL
               or SpirvOpCode.OpTypeAvcSicPayloadINTEL
               or SpirvOpCode.OpTypeAvcMcePayloadINTEL
               or SpirvOpCode.OpTypeAvcMceResultINTEL
               or SpirvOpCode.OpTypeAvcImeResultINTEL
               or SpirvOpCode.OpTypeAvcImeResultSingleReferenceStreamoutINTEL
               or SpirvOpCode.OpTypeAvcImeResultDualReferenceStreamoutINTEL
               or SpirvOpCode.OpTypeAvcImeSingleReferenceStreaminINTEL
               or SpirvOpCode.OpTypeAvcImeDualReferenceStreaminINTEL
               or SpirvOpCode.OpTypeAvcRefResultINTEL
               or SpirvOpCode.OpTypeAvcSicResultINTEL;
    }

    private sealed class SpirvHeaderInfo
    {
        public uint MagicNumber { get; init; }
        public uint Version { get; init; }
        public uint GeneratorMagic { get; init; }
        public uint Bound { get; init; }
        public uint Schema { get; init; }
    }

    #endregion
}

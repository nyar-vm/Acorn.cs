using Acorn.Frame;
using Acorn.Wasm.Data;

namespace Acorn.Wasm.Decode;

/// <summary>
///     WebAssembly 二进制解码器，将 Wasm 二进制格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     WebAssembly 二进制格式使用小端序和 LEB128 变长整数编码。
///     解码器按照 Wasm MVP（版本 1）规范将模块数据解析为 C# 数据结构。
/// </remarks>
public static class WasmDecoder
{
    /// <summary>
    ///     解码完整的 Wasm 模块。
    /// </summary>
    /// <param name="data">要解码的二进制数据。</param>
    /// <returns>解码后的 Wasm 模块数据。</returns>
    public static WasmModuleData DecodeModule(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        var version = ReadAndValidateHeader(ref buffer);

        var types = new List<WasmFunctionType>();
        var imports = new List<WasmImport>();
        var functionTypeIndices = new List<uint>();
        var tables = new List<WasmTable>();
        var memories = new List<WasmMemory>();
        var globals = new List<WasmGlobal>();
        var exports = new List<WasmExport>();
        uint? startFunctionIndex = null;
        var elements = new List<WasmElement>();
        var codes = new List<WasmCode>();
        var dataSegments = new List<WasmData>();
        var customSections = new List<WasmCustomSection>();

        while (!buffer.IsEnd)
        {
            var sectionId = buffer.ReadU8();
            var sectionSize = buffer.ReadLeb128U32();
            var sectionEnd = buffer.Position + (int)sectionSize;

            switch (sectionId)
            {
                case 0:
                    customSections.Add(ReadCustomSection(ref buffer, sectionSize));
                    break;
                case 1:
                    types = ReadTypeSection(ref buffer);
                    break;
                case 2:
                    imports = ReadImportSection(ref buffer);
                    break;
                case 3:
                    functionTypeIndices = ReadFunctionSection(ref buffer);
                    break;
                case 4:
                    tables = ReadTableSection(ref buffer);
                    break;
                case 5:
                    memories = ReadMemorySection(ref buffer);
                    break;
                case 6:
                    globals = ReadGlobalSection(ref buffer);
                    break;
                case 7:
                    exports = ReadExportSection(ref buffer);
                    break;
                case 8:
                    startFunctionIndex = ReadStartSection(ref buffer);
                    break;
                case 9:
                    elements = ReadElementSection(ref buffer);
                    break;
                case 10:
                    codes = ReadCodeSection(ref buffer);
                    break;
                case 11:
                    dataSegments = ReadDataSection(ref buffer);
                    break;
                default:
                    buffer.Position = sectionEnd;
                    break;
            }

            if (buffer.Position < sectionEnd)
            {
                buffer.Position = sectionEnd;
            }
        }

        return new WasmModuleData
        {
            Version = version,
            Types = types,
            Imports = imports,
            FunctionTypeIndices = functionTypeIndices,
            Tables = tables,
            Memories = memories,
            Globals = globals,
            Exports = exports,
            StartFunctionIndex = startFunctionIndex,
            Elements = elements,
            Codes = codes,
            DataSegments = dataSegments,
            CustomSections = customSections
        };
    }

    /// <summary>
    ///     读取并验证 Wasm 文件头。
    /// </summary>
    /// <param name="buffer">字节缓冲区。</param>
    /// <returns>Wasm 版本号。</returns>
    public static uint ReadAndValidateHeader(ref ByteBuffer buffer)
    {
        if (!WasmHeader.TryRead(ref buffer, out var header))
        {
            throw new InvalidDataException("Wasm 文件头部读取失败");
        }

        if (!header.Magic.AsSpan().SequenceEqual(WasmConstants.MagicNumber))
        {
            throw new InvalidDataException("Wasm 文件魔数不匹配，期望 \\0asm");
        }

        if (header.Version != WasmConstants.Version)
        {
            throw new InvalidDataException($"不支持的 Wasm 版本：{header.Version}，仅支持版本 {WasmConstants.Version}");
        }

        return header.Version;
    }

    #region 段读取方法

    private static WasmCustomSection ReadCustomSection(ref ByteBuffer buffer, uint sectionSize)
    {
        var posBeforeName = buffer.Position;
        var name = buffer.ReadLeb128String();
        var nameBytesRead = buffer.Position - posBeforeName;
        var remainingSize = (int)sectionSize - nameBytesRead;
        var data = buffer.ReadBytes(remainingSize).ToArray();

        return new WasmCustomSection
        {
            Name = name,
            Data = data
        };
    }

    private static List<WasmFunctionType> ReadTypeSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var types = new List<WasmFunctionType>((int)count);

        for (var i = 0; i < count; i++)
        {
            types.Add(ReadFunctionType(ref buffer));
        }

        return types;
    }

    private static List<WasmImport> ReadImportSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var imports = new List<WasmImport>((int)count);

        for (var i = 0; i < count; i++)
        {
            imports.Add(ReadImport(ref buffer));
        }

        return imports;
    }

    private static List<uint> ReadFunctionSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var indices = new List<uint>((int)count);

        for (var i = 0; i < count; i++)
        {
            indices.Add(buffer.ReadLeb128U32());
        }

        return indices;
    }

    private static List<WasmTable> ReadTableSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var tables = new List<WasmTable>((int)count);

        for (var i = 0; i < count; i++)
        {
            tables.Add(ReadTable(ref buffer));
        }

        return tables;
    }

    private static List<WasmMemory> ReadMemorySection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var memories = new List<WasmMemory>((int)count);

        for (var i = 0; i < count; i++)
        {
            memories.Add(ReadMemory(ref buffer));
        }

        return memories;
    }

    private static List<WasmGlobal> ReadGlobalSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var globals = new List<WasmGlobal>((int)count);

        for (var i = 0; i < count; i++)
        {
            globals.Add(ReadGlobal(ref buffer));
        }

        return globals;
    }

    private static List<WasmExport> ReadExportSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var exports = new List<WasmExport>((int)count);

        for (var i = 0; i < count; i++)
        {
            exports.Add(ReadExport(ref buffer));
        }

        return exports;
    }

    private static uint ReadStartSection(ref ByteBuffer buffer)
    {
        return buffer.ReadLeb128U32();
    }

    private static List<WasmElement> ReadElementSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var elements = new List<WasmElement>((int)count);

        for (var i = 0; i < count; i++)
        {
            elements.Add(ReadElement(ref buffer));
        }

        return elements;
    }

    private static List<WasmCode> ReadCodeSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var codes = new List<WasmCode>((int)count);

        for (var i = 0; i < count; i++)
        {
            codes.Add(ReadCode(ref buffer));
        }

        return codes;
    }

    private static List<WasmData> ReadDataSection(ref ByteBuffer buffer)
    {
        var count = buffer.ReadLeb128U32();
        var dataSegments = new List<WasmData>((int)count);

        for (var i = 0; i < count; i++)
        {
            dataSegments.Add(ReadDataSegment(ref buffer));
        }

        return dataSegments;
    }

    #endregion

    #region 类型读取方法

    private static WasmFunctionType ReadFunctionType(ref ByteBuffer buffer)
    {
        var form = buffer.ReadU8();

        if (form != WasmConstants.FunctionTypeForm)
        {
            throw new InvalidDataException($"无效的函数类型标记：0x{form:X2}，期望 0x{WasmConstants.FunctionTypeForm:X2}");
        }

        var paramCount = buffer.ReadLeb128U32();
        var parameters = new List<WasmValueType>((int)paramCount);

        for (var i = 0; i < paramCount; i++)
        {
            parameters.Add(ReadValueType(ref buffer));
        }

        var resultCount = buffer.ReadLeb128U32();
        var results = new List<WasmValueType>((int)resultCount);

        for (var i = 0; i < resultCount; i++)
        {
            results.Add(ReadValueType(ref buffer));
        }

        return new WasmFunctionType
        {
            Parameters = parameters,
            Results = results
        };
    }

    private static WasmValueType ReadValueType(ref ByteBuffer buffer)
    {
        var code = buffer.ReadU8();

        return code switch
        {
            0x7F => WasmValueType.Int32,
            0x7E => WasmValueType.Int64,
            0x7D => WasmValueType.Float32,
            0x7C => WasmValueType.Float64,
            0x70 => WasmValueType.FuncRef,
            0x6F => WasmValueType.ExternRef,
            _ => throw new InvalidDataException($"未知的值类型：0x{code:X2}")
        };
    }

    private static WasmLimits ReadLimits(ref ByteBuffer buffer)
    {
        var flags = buffer.ReadU8();
        var minimum = buffer.ReadLeb128U32();
        uint? maximum = null;

        if (flags == 1)
        {
            maximum = buffer.ReadLeb128U32();
        }

        return new WasmLimits
        {
            Minimum = minimum,
            Maximum = maximum
        };
    }

    private static WasmTableType ReadTableType(ref ByteBuffer buffer)
    {
        var elementType = ReadValueType(ref buffer);
        var limits = ReadLimits(ref buffer);

        return new WasmTableType
        {
            ElementType = elementType,
            Limits = limits
        };
    }

    private static WasmMemoryType ReadMemoryType(ref ByteBuffer buffer)
    {
        var limits = ReadLimits(ref buffer);

        return new WasmMemoryType
        {
            Limits = limits
        };
    }

    private static WasmGlobalType ReadGlobalType(ref ByteBuffer buffer)
    {
        var valueType = ReadValueType(ref buffer);
        var mutable = buffer.ReadU8() == WasmConstants.GlobalMutable;

        return new WasmGlobalType
        {
            ValueType = valueType,
            Mutable = mutable
        };
    }

    private static WasmImport ReadImport(ref ByteBuffer buffer)
    {
        var module = buffer.ReadLeb128String();
        var field = buffer.ReadLeb128String();
        var descriptor = ReadImportDescriptor(ref buffer);

        return new WasmImport
        {
            Module = module,
            Field = field,
            Descriptor = descriptor
        };
    }

    private static WasmImportDescriptor ReadImportDescriptor(ref ByteBuffer buffer)
    {
        var kind = (WasmExternalKind)buffer.ReadU8();

        switch (kind)
        {
            case WasmExternalKind.Function:
                return new WasmImportDescriptor
                {
                    Kind = kind,
                    FunctionTypeIndex = buffer.ReadLeb128U32()
                };
            case WasmExternalKind.Table:
                return new WasmImportDescriptor
                {
                    Kind = kind,
                    TableType = ReadTableType(ref buffer)
                };
            case WasmExternalKind.Memory:
                return new WasmImportDescriptor
                {
                    Kind = kind,
                    MemoryType = ReadMemoryType(ref buffer)
                };
            case WasmExternalKind.Global:
                return new WasmImportDescriptor
                {
                    Kind = kind,
                    GlobalType = ReadGlobalType(ref buffer)
                };
            default:
                throw new InvalidDataException($"未知的导入种类：{kind}");
        }
    }

    private static WasmTable ReadTable(ref ByteBuffer buffer)
    {
        return new WasmTable
        {
            Type = ReadTableType(ref buffer)
        };
    }

    private static WasmMemory ReadMemory(ref ByteBuffer buffer)
    {
        return new WasmMemory
        {
            Type = ReadMemoryType(ref buffer)
        };
    }

    private static WasmGlobal ReadGlobal(ref ByteBuffer buffer)
    {
        var type = ReadGlobalType(ref buffer);
        var initExpression = ReadInitExpression(ref buffer);

        return new WasmGlobal
        {
            Type = type,
            InitExpression = initExpression
        };
    }

    private static WasmExport ReadExport(ref ByteBuffer buffer)
    {
        var name = buffer.ReadLeb128String();
        var kind = (WasmExternalKind)buffer.ReadU8();
        var index = buffer.ReadLeb128U32();

        return new WasmExport
        {
            Name = name,
            Kind = kind,
            Index = index
        };
    }

    private static WasmElement ReadElement(ref ByteBuffer buffer)
    {
        var flags = buffer.ReadLeb128U32();
        uint tableIndex = 0;
        byte[]? offsetExpression = null;

        switch (flags)
        {
            case 0:
                tableIndex = 0;
                offsetExpression = ReadInitExpression(ref buffer);
                break;
            case 1:
                offsetExpression = null;
                break;
            case 2:
                tableIndex = buffer.ReadLeb128U32();
                offsetExpression = ReadInitExpression(ref buffer);
                break;
            default:
                throw new InvalidDataException($"不支持的元素段标志：{flags}");
        }

        var count = buffer.ReadLeb128U32();
        var initValues = new List<uint>((int)count);

        for (var i = 0; i < count; i++)
        {
            initValues.Add(buffer.ReadLeb128U32());
        }

        return new WasmElement
        {
            TableIndex = tableIndex,
            OffsetExpression = offsetExpression ?? [],
            InitValues = initValues
        };
    }

    private static WasmCode ReadCode(ref ByteBuffer buffer)
    {
        var bodySize = buffer.ReadLeb128U32();
        var bodyStart = buffer.Position;

        var localCount = buffer.ReadLeb128U32();
        var locals = new List<WasmLocal>((int)localCount);

        for (var i = 0; i < localCount; i++)
        {
            var count = buffer.ReadLeb128U32();
            var type = ReadValueType(ref buffer);
            locals.Add(new WasmLocal { Count = count, Type = type });
        }

        var remainingSize = (int)(bodyStart + bodySize - buffer.Position);
        var body = buffer.ReadBytes(remainingSize).ToArray();

        return new WasmCode
        {
            Locals = locals,
            Body = body
        };
    }

    private static WasmData ReadDataSegment(ref ByteBuffer buffer)
    {
        var flags = buffer.ReadLeb128U32();
        uint memoryIndex = 0;
        byte[]? offsetExpression = null;

        switch (flags)
        {
            case 0:
                memoryIndex = 0;
                offsetExpression = ReadInitExpression(ref buffer);
                break;
            case 1:
                offsetExpression = null;
                break;
            case 2:
                memoryIndex = buffer.ReadLeb128U32();
                offsetExpression = ReadInitExpression(ref buffer);
                break;
            default:
                throw new InvalidDataException($"不支持的数据段标志：{flags}");
        }

        var dataSize = buffer.ReadLeb128U32();
        var initializer = buffer.ReadBytes((int)dataSize).ToArray();

        return new WasmData
        {
            MemoryIndex = memoryIndex,
            OffsetExpression = offsetExpression ?? [],
            Initializer = initializer
        };
    }

    #endregion

    #region 辅助方法

    private static byte[] ReadInitExpression(ref ByteBuffer buffer)
    {
        using var ms = new MemoryStream();

        while (true)
        {
            var opcode = buffer.ReadU8();
            ms.WriteByte(opcode);

            if (opcode == (byte)WasmInitOpCode.End)
            {
                break;
            }

            switch ((WasmInitOpCode)opcode)
            {
                case WasmInitOpCode.I32Const:
                    var i32Value = buffer.ReadLeb128I32();
                    var i32Writer = new ByteBufferWriter(16);
                    i32Writer.WriteLeb128I32(i32Value);
                    ms.Write(i32Writer.WrittenData.ToArray());
                    break;
                case WasmInitOpCode.I64Const:
                    var i64Value = buffer.ReadLeb128I64();
                    var i64Writer = new ByteBufferWriter(16);
                    i64Writer.WriteLeb128I64(i64Value);
                    ms.Write(i64Writer.WrittenData.ToArray());
                    break;
                case WasmInitOpCode.F32Const:
                    var f32Bytes = buffer.ReadBytes(4).ToArray();
                    ms.Write(f32Bytes);
                    break;
                case WasmInitOpCode.F64Const:
                    var f64Bytes = buffer.ReadBytes(8).ToArray();
                    ms.Write(f64Bytes);
                    break;
                case WasmInitOpCode.GlobalGet:
                    var globalIdx = buffer.ReadLeb128U32();
                    var globalIdxWriter = new ByteBufferWriter(16);
                    globalIdxWriter.WriteLeb128U32(globalIdx);
                    ms.Write(globalIdxWriter.WrittenData.ToArray());
                    break;
                default:
                    throw new InvalidDataException($"初始化表达式中不支持的操作码：0x{opcode:X2}");
            }
        }

        return ms.ToArray();
    }

    #endregion
}

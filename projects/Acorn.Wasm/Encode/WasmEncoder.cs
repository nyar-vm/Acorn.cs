using System.Text;
using Acorn.Frame;
using Acorn.Wasm.Data;

namespace Acorn.Wasm.Encode;

/// <summary>
///     WebAssembly 二进制编码器，将 C# 数据结构编码为 Wasm 二进制格式。
/// </summary>
/// <remarks>
///     WebAssembly 二进制格式使用小端序和 LEB128 变长整数编码。
///     编码器按照 Wasm MVP（版本 1）规范将模块数据写入二进制缓冲区。
/// </remarks>
public static class WasmEncoder
{
    /// <summary>
    ///     将完整的 Wasm 模块编码写入缓冲区。
    /// </summary>
    /// <param name="buffer">要写入的目标字节缓冲区。</param>
    /// <param name="module">要编码的 Wasm 模块数据。</param>
    /// <returns>已写入的字节数。</returns>
    public static int EncodeModule(Span<byte> buffer, WasmModuleData module)
    {
        var writer = new ByteBufferWriter(buffer.Length);
        WriteHeader(writer, module.Version);

        foreach (var customSection in module.CustomSections)
        {
            WriteCustomSection(writer, customSection);
        }

        if (module.Types.Count > 0)
        {
            WriteTypeSection(writer, module.Types);
        }

        if (module.Imports.Count > 0)
        {
            WriteImportSection(writer, module.Imports);
        }

        if (module.FunctionTypeIndices.Count > 0)
        {
            WriteFunctionSection(writer, module.FunctionTypeIndices);
        }

        if (module.Tables.Count > 0)
        {
            WriteTableSection(writer, module.Tables);
        }

        if (module.Memories.Count > 0)
        {
            WriteMemorySection(writer, module.Memories);
        }

        if (module.Globals.Count > 0)
        {
            WriteGlobalSection(writer, module.Globals);
        }

        if (module.Exports.Count > 0)
        {
            WriteExportSection(writer, module.Exports);
        }

        if (module.StartFunctionIndex.HasValue)
        {
            WriteStartSection(writer, module.StartFunctionIndex.Value);
        }

        if (module.Elements.Count > 0)
        {
            WriteElementSection(writer, module.Elements);
        }

        if (module.Codes.Count > 0)
        {
            WriteCodeSection(writer, module.Codes);
        }

        if (module.DataSegments.Count > 0)
        {
            WriteDataSection(writer, module.DataSegments);
        }

        var data = writer.WrittenData;
        data.CopyTo(buffer);
        return data.Length;
    }

    /// <summary>
    ///     将完整的 Wasm 模块编码为字节数组。
    /// </summary>
    /// <param name="module">要编码的 Wasm 模块数据。</param>
    /// <returns>编码后的字节数组。</returns>
    public static byte[] EncodeModule(WasmModuleData module)
    {
        var writer = new ByteBufferWriter(1024 * 1024);
        WriteHeader(writer, module.Version);

        foreach (var customSection in module.CustomSections)
        {
            WriteCustomSection(writer, customSection);
        }

        if (module.Types.Count > 0)
        {
            WriteTypeSection(writer, module.Types);
        }

        if (module.Imports.Count > 0)
        {
            WriteImportSection(writer, module.Imports);
        }

        if (module.FunctionTypeIndices.Count > 0)
        {
            WriteFunctionSection(writer, module.FunctionTypeIndices);
        }

        if (module.Tables.Count > 0)
        {
            WriteTableSection(writer, module.Tables);
        }

        if (module.Memories.Count > 0)
        {
            WriteMemorySection(writer, module.Memories);
        }

        if (module.Globals.Count > 0)
        {
            WriteGlobalSection(writer, module.Globals);
        }

        if (module.Exports.Count > 0)
        {
            WriteExportSection(writer, module.Exports);
        }

        if (module.StartFunctionIndex.HasValue)
        {
            WriteStartSection(writer, module.StartFunctionIndex.Value);
        }

        if (module.Elements.Count > 0)
        {
            WriteElementSection(writer, module.Elements);
        }

        if (module.Codes.Count > 0)
        {
            WriteCodeSection(writer, module.Codes);
        }

        if (module.DataSegments.Count > 0)
        {
            WriteDataSection(writer, module.DataSegments);
        }

        return writer.ToArray();
    }

    /// <summary>
    ///     写入 Wasm 文件头（魔数和版本号）。
    /// </summary>
    /// <param name="writer">字节缓冲区写入器。</param>
    /// <param name="version">版本号，默认为 1。</param>
    public static void WriteHeader(ByteBufferWriter writer, uint version = WasmConstants.Version)
    {
        writer.Write(WasmConstants.MagicNumber);
        writer.WriteU32LE(version);
    }

    #region 段写入方法

    private static void WriteSectionHeader(ByteBufferWriter writer, byte sectionId, uint sectionSize)
    {
        writer.WriteU8(sectionId);
        writer.WriteLeb128U32(sectionSize);
    }

    private static void WriteCustomSection(ByteBufferWriter writer, WasmCustomSection section)
    {
        var sectionData = BuildSectionData(w => WriteNameTo(w, section.Name), w => w.Write(section.Data));
        WriteSectionHeader(writer, (byte)WasmSectionId.Custom, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteTypeSection(ByteBufferWriter writer, IReadOnlyList<WasmFunctionType> types)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)types.Count);
            foreach (var type in types)
            {
                WriteFunctionTypeTo(w, type);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Type, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteImportSection(ByteBufferWriter writer, IReadOnlyList<WasmImport> imports)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)imports.Count);
            foreach (var import in imports)
            {
                WriteImportTo(w, import);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Import, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteFunctionSection(ByteBufferWriter writer, IReadOnlyList<uint> functionTypeIndices)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)functionTypeIndices.Count);
            foreach (var index in functionTypeIndices)
            {
                w.WriteLeb128U32(index);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Function, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteTableSection(ByteBufferWriter writer, IReadOnlyList<WasmTable> tables)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)tables.Count);
            foreach (var table in tables)
            {
                WriteTableTypeTo(w, table.Type);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Table, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteMemorySection(ByteBufferWriter writer, IReadOnlyList<WasmMemory> memories)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)memories.Count);
            foreach (var memory in memories)
            {
                WriteLimitsTo(w, memory.Type.Limits);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Memory, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteGlobalSection(ByteBufferWriter writer, IReadOnlyList<WasmGlobal> globals)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)globals.Count);
            foreach (var global in globals)
            {
                WriteGlobalTypeTo(w, global.Type);
                w.Write(global.InitExpression);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Global, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteExportSection(ByteBufferWriter writer, IReadOnlyList<WasmExport> exports)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)exports.Count);
            foreach (var export in exports)
            {
                WriteNameTo(w, export.Name);
                w.WriteU8((byte)export.Kind);
                w.WriteLeb128U32(export.Index);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Export, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteStartSection(ByteBufferWriter writer, uint startFunctionIndex)
    {
        var sectionData = BuildSectionData(w => w.WriteLeb128U32(startFunctionIndex));
        WriteSectionHeader(writer, (byte)WasmSectionId.Start, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteElementSection(ByteBufferWriter writer, IReadOnlyList<WasmElement> elements)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)elements.Count);
            foreach (var element in elements)
            {
                WriteElementTo(w, element);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Element, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteCodeSection(ByteBufferWriter writer, IReadOnlyList<WasmCode> codes)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)codes.Count);
            foreach (var code in codes)
            {
                WriteCodeTo(w, code);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Code, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    private static void WriteDataSection(ByteBufferWriter writer, IReadOnlyList<WasmData> dataSegments)
    {
        var sectionData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)dataSegments.Count);
            foreach (var data in dataSegments)
            {
                WriteDataSegmentTo(w, data);
            }
        });
        WriteSectionHeader(writer, (byte)WasmSectionId.Data, (uint)sectionData.Length);
        writer.Write(sectionData);
    }

    #endregion

    #region 类型写入方法

    private static void WriteFunctionTypeTo(ByteBufferWriter writer, WasmFunctionType type)
    {
        writer.WriteU8(WasmConstants.FunctionTypeForm);
        writer.WriteLeb128U32((uint)type.Parameters.Count);

        foreach (var param in type.Parameters)
        {
            WriteValueTypeTo(writer, param);
        }

        writer.WriteLeb128U32((uint)type.Results.Count);

        foreach (var result in type.Results)
        {
            WriteValueTypeTo(writer, result);
        }
    }

    private static void WriteValueTypeTo(ByteBufferWriter writer, WasmValueType valueType)
    {
        writer.WriteU8((byte)valueType);
    }

    private static void WriteLimitsTo(ByteBufferWriter writer, WasmLimits limits)
    {
        if (limits.Maximum.HasValue)
        {
            writer.WriteU8(WasmConstants.LimitsHasMinMax);
            writer.WriteLeb128U32(limits.Minimum);
            writer.WriteLeb128U32(limits.Maximum.Value);
        }
        else
        {
            writer.WriteU8(WasmConstants.LimitsHasOnlyMin);
            writer.WriteLeb128U32(limits.Minimum);
        }
    }

    private static void WriteTableTypeTo(ByteBufferWriter writer, WasmTableType tableType)
    {
        WriteValueTypeTo(writer, tableType.ElementType);
        WriteLimitsTo(writer, tableType.Limits);
    }

    private static void WriteGlobalTypeTo(ByteBufferWriter writer, WasmGlobalType globalType)
    {
        WriteValueTypeTo(writer, globalType.ValueType);
        writer.WriteU8(globalType.Mutable ? WasmConstants.GlobalMutable : WasmConstants.GlobalImmutable);
    }

    private static void WriteImportTo(ByteBufferWriter writer, WasmImport import)
    {
        WriteNameTo(writer, import.Module);
        WriteNameTo(writer, import.Field);
        WriteImportDescriptorTo(writer, import.Descriptor);
    }

    private static void WriteImportDescriptorTo(ByteBufferWriter writer, WasmImportDescriptor descriptor)
    {
        writer.WriteU8((byte)descriptor.Kind);

        switch (descriptor.Kind)
        {
            case WasmExternalKind.Function:
                writer.WriteLeb128U32(descriptor.FunctionTypeIndex);
                break;
            case WasmExternalKind.Table:
                WriteTableTypeTo(writer, descriptor.TableType!);
                break;
            case WasmExternalKind.Memory:
                WriteLimitsTo(writer, descriptor.MemoryType!.Limits);
                break;
            case WasmExternalKind.Global:
                WriteGlobalTypeTo(writer, descriptor.GlobalType!);
                break;
        }
    }

    private static void WriteElementTo(ByteBufferWriter writer, WasmElement element)
    {
        writer.WriteLeb128U32(element.TableIndex);
        writer.Write(element.OffsetExpression);
        writer.WriteLeb128U32((uint)element.InitValues.Count);

        foreach (var value in element.InitValues)
        {
            writer.WriteLeb128U32(value);
        }
    }

    private static void WriteCodeTo(ByteBufferWriter writer, WasmCode code)
    {
        var bodyData = BuildSectionData(w =>
        {
            w.WriteLeb128U32((uint)code.Locals.Count);

            foreach (var local in code.Locals)
            {
                w.WriteLeb128U32(local.Count);
                WriteValueTypeTo(w, local.Type);
            }

            w.Write(code.Body);
        });

        writer.WriteLeb128U32((uint)bodyData.Length);
        writer.Write(bodyData);
    }

    private static void WriteDataSegmentTo(ByteBufferWriter writer, WasmData data)
    {
        writer.WriteLeb128U32(data.MemoryIndex);
        writer.Write(data.OffsetExpression);
        writer.WriteLeb128U32((uint)data.Initializer.Length);
        writer.Write(data.Initializer);
    }

    #endregion

    #region 辅助方法

    private static void WriteNameTo(ByteBufferWriter writer, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        writer.WriteLeb128U32((uint)bytes.Length);
        writer.Write(bytes);
    }

    private static byte[] BuildSectionData(params Action<ByteBufferWriter>[] writers)
    {
        return BuildSectionData(w => { foreach (var write in writers) write(w); });
    }

    private static byte[] BuildSectionData(Action<ByteBufferWriter> writeContent)
    {
        var tempWriter = new ByteBufferWriter(1024 * 1024);
        writeContent(tempWriter);
        return tempWriter.ToArray();
    }

    #endregion
}

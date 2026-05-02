using System.IO;
using System.Text;
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
    public static int EncodeModule(Span<byte> buffer, WasmModuleData module)
    {
        var bytes = EncodeModule(module);
        bytes.CopyTo(buffer);
        return bytes.Length;
    }

    /// <summary>
    ///     将完整的 Wasm 模块编码为字节数组。
    /// </summary>
    public static byte[] EncodeModule(WasmModuleData module)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        #region Header

        writer.Write(WasmConstants.MagicNumber);
        writer.WriteLE(module.Version);

        #endregion

        #region Type Section

        if (module.Types.Count > 0 || module.GcSubTypes is { Count: > 0 })
        {
            var funcTypeCount = (uint)module.Types.Count;
            var hasGcType = module.GcSubTypes is { Count: > 0 };
            var totalTypeCount = funcTypeCount + (hasGcType ? 1u : 0u);

            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128(totalTypeCount);

                for (var idx = 0; idx < funcTypeCount; idx++)
                {
                    var type = module.Types[idx];
                    w.Write((byte)WasmConstants.FunctionTypeForm);
                    w.WriteLEB128((uint)type.Parameters.Count);
                    foreach (var param in type.Parameters)
                    {
                        w.Write((byte)param);
                    }

                    w.WriteLEB128((uint)type.Results.Count);
                    foreach (var result in type.Results)
                    {
                        w.Write((byte)result);
                    }
                }

                if (hasGcType)
                {
                    w.Write(WasmConstants.RecTypeForm);
                    w.WriteLEB128((uint)module.GcSubTypes!.Count);
                    foreach (var subType in module.GcSubTypes)
                    {
                        EncodeSubType(w, subType);
                    }
                }
            });
            writer.Write((byte)WasmSectionId.Type);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Import Section

        if (module.Imports.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.Imports.Count);
                foreach (var import in module.Imports)
                {
                    WriteName(w, import.Module);
                    WriteName(w, import.Field);
                    w.Write((byte)import.Descriptor.Kind);

                    switch (import.Descriptor.Kind)
                    {
                        case WasmExternalKind.Function:
                            w.WriteLEB128(import.Descriptor.FunctionTypeIndex);
                            break;
                        case WasmExternalKind.Table:
                            w.Write((byte)import.Descriptor.TableType!.ElementType);
                            WriteLimits(w, import.Descriptor.TableType.Limits);
                            break;
                        case WasmExternalKind.Memory:
                            WriteLimits(w, import.Descriptor.MemoryType!.Limits);
                            break;
                        case WasmExternalKind.Global:
                            w.Write((byte)import.Descriptor.GlobalType!.ValueType);
                            w.Write(import.Descriptor.GlobalType.Mutable ? WasmConstants.GlobalMutable : WasmConstants.GlobalImmutable);
                            break;
                    }
                }
            });
            writer.Write((byte)WasmSectionId.Import);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Function Section

        if (module.FunctionTypeIndices.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.FunctionTypeIndices.Count);
                foreach (var index in module.FunctionTypeIndices)
                {
                    w.WriteLEB128(index);
                }
            });
            writer.Write((byte)WasmSectionId.Function);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Table Section

        if (module.Tables.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.Tables.Count);
                foreach (var table in module.Tables)
                {
                    w.Write((byte)table.Type.ElementType);
                    WriteLimits(w, table.Type.Limits);
                }
            });
            writer.Write((byte)WasmSectionId.Table);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Memory Section

        if (module.Memories.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.Memories.Count);
                foreach (var memory in module.Memories)
                {
                    WriteLimits(w, memory.Type.Limits);
                }
            });
            writer.Write((byte)WasmSectionId.Memory);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Global Section

        if (module.Globals.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.Globals.Count);
                foreach (var global in module.Globals)
                {
                    w.Write((byte)global.Type.ValueType);
                    w.Write(global.Type.Mutable ? WasmConstants.GlobalMutable : WasmConstants.GlobalImmutable);
                    w.Write(global.InitExpression);
                }
            });
            writer.Write((byte)WasmSectionId.Global);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Export Section

        if (module.Exports.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.Exports.Count);
                foreach (var export in module.Exports)
                {
                    WriteName(w, export.Name);
                    w.Write((byte)export.Kind);
                    w.WriteLEB128(export.Index);
                }
            });
            writer.Write((byte)WasmSectionId.Export);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Start Section

        if (module.StartFunctionIndex.HasValue)
        {
            var sectionData = BuildBytes(w => w.WriteLEB128(module.StartFunctionIndex.Value));
            writer.Write((byte)WasmSectionId.Start);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Element Section

        if (module.Elements.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.Elements.Count);
                foreach (var element in module.Elements)
                {
                    w.WriteLEB128(element.TableIndex);
                    w.Write(element.OffsetExpression);
                    w.WriteLEB128((uint)element.InitValues.Count);
                    foreach (var value in element.InitValues)
                    {
                        w.WriteLEB128(value);
                    }
                }
            });
            writer.Write((byte)WasmSectionId.Element);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Code Section

        if (module.Codes.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.Codes.Count);
                foreach (var code in module.Codes)
                {
                    var bodyData = BuildBytes(bw =>
                    {
                        bw.WriteLEB128((uint)code.Locals.Count);
                        foreach (var local in code.Locals)
                        {
                            bw.WriteLEB128(local.Count);
                            bw.Write((byte)local.Type);
                        }

                        bw.Write(code.Body);
                    });
                    w.WriteLEB128((uint)bodyData.Length);
                    w.Write(bodyData);
                }
            });
            writer.Write((byte)WasmSectionId.Code);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Data Section

        if (module.DataSegments.Count > 0)
        {
            var sectionData = BuildBytes(w =>
            {
                w.WriteLEB128((uint)module.DataSegments.Count);
                foreach (var data in module.DataSegments)
                {
                    w.WriteLEB128(data.MemoryIndex);
                    w.Write(data.OffsetExpression);
                    w.WriteLEB128((uint)data.Initializer.Length);
                    w.Write(data.Initializer);
                }
            });
            writer.Write((byte)WasmSectionId.Data);
            writer.WriteLEB128((uint)sectionData.Length);
            writer.Write(sectionData);
        }

        #endregion

        #region Custom Sections

        if (module.CustomSections.Count > 0)
        {
            foreach (var custom in module.CustomSections)
            {
                var sectionData = BuildBytes(w =>
                {
                    WriteName(w, custom.Name);
                    w.Write(custom.Data);
                });
                writer.Write((byte)WasmSectionId.Custom);
                writer.WriteLEB128((uint)sectionData.Length);
                writer.Write(sectionData);
            }
        }

        #endregion

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    ///     写入 Wasm 文件头（魔数和版本号）。
    /// </summary>
    public static void WriteHeader(System.IO.BinaryWriter writer, uint version = WasmConstants.Version)
    {
        writer.Write(WasmConstants.MagicNumber);
        writer.WriteLE(version);
    }

    private static void WriteName(BinaryWriter writer, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        writer.WriteLEB128((uint)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteLimits(BinaryWriter writer, WasmLimits limits)
    {
        if (limits.Maximum.HasValue)
        {
            writer.Write((byte)WasmConstants.LimitsHasMinMax);
            writer.WriteLEB128(limits.Minimum);
            writer.WriteLEB128(limits.Maximum.Value);
        }
        else
        {
            writer.Write((byte)WasmConstants.LimitsHasOnlyMin);
            writer.WriteLEB128(limits.Minimum);
        }
    }

    private static byte[] BuildBytes(Action<BinaryWriter> writeContent)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        writeContent(w);
        w.Flush();
        return ms.ToArray();
    }

    #region GC 类型编码方法

    /// <summary>
    ///     编码 WASM GC 子类型。
    /// </summary>
    private static void EncodeSubType(BinaryWriter writer, WasmSubType subType)
    {
        writer.Write(WasmConstants.SubTypeForm);
        writer.Write(subType.Final ? WasmConstants.FinalType : WasmConstants.NonFinalType);

        if (subType.SuperTypeIndex.HasValue)
        {
            writer.WriteLEB128(subType.SuperTypeIndex.Value);
        }

        EncodeCompositeType(writer, subType.Type);
    }

    /// <summary>
    ///     编码 WASM GC 复合类型。
    /// </summary>
    private static void EncodeCompositeType(BinaryWriter writer, WasmCompositeType type)
    {
        switch (type.Kind)
        {
            case WasmCompositeTypeKind.Struct:
                writer.Write(WasmConstants.StructTypeForm);
                writer.WriteLEB128((uint)(type.Fields?.Count ?? 0));
                if (type.Fields is { } fields)
                {
                    foreach (var field in fields)
                    {
                        EncodeStorageType(writer, field.StorageType);
                        writer.Write(field.Mutable ? WasmConstants.FieldMutable : WasmConstants.FieldImmutable);
                    }
                }

                break;

            case WasmCompositeTypeKind.Array:
                writer.Write(WasmConstants.ArrayTypeForm);
                if (type.ElementType is { } elemType)
                {
                    EncodeStorageType(writer, elemType);
                }
                else
                {
                    writer.Write((byte)WasmValueType.Int32);
                }

                writer.Write(WasmConstants.FieldMutable);
                break;

            case WasmCompositeTypeKind.Rec:
                writer.Write(WasmConstants.RecTypeForm);
                writer.WriteLEB128((uint)(type.SubTypes?.Count ?? 0));
                if (type.SubTypes is { } subTypes)
                {
                    foreach (var sub in subTypes)
                    {
                        EncodeSubType(writer, sub);
                    }
                }

                break;
        }
    }

    /// <summary>
    ///     编码 WASM GC 存储类型。
    /// </summary>
    private static void EncodeStorageType(BinaryWriter writer, WasmStorageType storageType)
    {
        if (storageType.PackedType.HasValue)
        {
            writer.Write((byte)storageType.PackedType.Value);
        }
        else
        {
            writer.Write((byte)storageType.ValueType);
        }
    }

    #endregion

    #region Component Model 编码

    /// <summary>
    ///     将 WASM 组件数据编码为字节数组（Component Model 二进制格式）。
    ///     当前为骨架实现，将在后续阶段完善完整的组件编码。
    /// </summary>
    public static byte[] EncodeComponent(WasmComponentData component)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(WasmConstants.MagicNumber);
        writer.WriteLE(WasmConstants.ComponentVersion);

        writer.Flush();
        return stream.ToArray();
    }

    #endregion

    #region BinaryWriter 扩展方法

    private static void WriteLE(this BinaryWriter writer, uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        writer.Write(bytes);
    }

    private static void WriteLEB128(this BinaryWriter writer, uint value)
    {
        do
        {
            var byteVal = value & 0x7F;
            value >>= 7;
            if (value != 0)
            {
                byteVal |= 0x80;
            }

            writer.Write((byte)byteVal);
        } while (value != 0);
    }

    private static void WriteLEB128(this BinaryWriter writer, int value)
    {
        WriteLEB128(writer, (uint)value);
    }

    #endregion
}

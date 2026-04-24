using System.Text;
using Acorn.Frame;
using Acorn.Nyar.Data;

namespace Acorn.Nyar.Encode;

/// <summary>
///     Nyar 字节码模块编码器，将 C# 数据结构编码为 .nyarc 字节码格式。
/// </summary>
/// <remarks>
///     .nyarc 是 NyarVM 的字节码模块格式，采用分段式二进制布局。
///     编码器将模块数据序列化为符合 NyarVM 规范的二进制数据。
///     二进制布局：[Header 16B] → [Section Headers N*9B] → [Name Section] → [Section Data...]
/// </remarks>
public sealed class NyarEncoder
{
    /// <summary>
    ///     将 Nyar 模块数据编码为 .nyarc 二进制格式。
    /// </summary>
    /// <param name="data">Nyar 模块数据。</param>
    /// <returns>.nyarc 二进制数据。</returns>
    public byte[] Encode(NyarModuleData data)
    {
        var sections = BuildSections(data);
        var size = EstimateSize(data, sections);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteHeader(ref writer, data, sections.Count);
        WriteSectionHeaders(ref writer, sections);
        WriteNameSection(ref writer, data.Name);

        foreach (var section in sections)
        {
            writer.Write(section.Data);
        }

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static List<NyarSection> BuildSections(NyarModuleData data)
    {
        var sections = new List<NyarSection>();

        if (data.Constants.Count > 0)
        {
            sections.Add(BuildConstantsSection(data.Constants));
        }

        if (data.Functions.Count > 0)
        {
            sections.Add(BuildFunctionsSection(data.Functions));
        }

        if (data.Imports.Count > 0)
        {
            sections.Add(BuildImportsSection(data.Imports));
        }

        if (data.Exports.Count > 0)
        {
            sections.Add(BuildExportsSection(data.Exports));
        }

        return sections;
    }

    private static NyarSection BuildConstantsSection(IReadOnlyList<NyarConstant> constants)
    {
        var size = 4;

        foreach (var constant in constants)
        {
            size += 1;

            switch (constant.Kind)
            {
                case NyarConstantKind.Int32:
                    size += 4;
                    break;
                case NyarConstantKind.Float64:
                    size += 8;
                    break;
                case NyarConstantKind.Bool:
                    size += 1;
                    break;
                case NyarConstantKind.Null:
                    break;
                case NyarConstantKind.String:
                    size += 4 + Encoding.UTF8.GetByteCount((string?)constant.Value ?? "");
                    break;
                case NyarConstantKind.BigInt:
                    size += 4 + ((byte[]?)constant.Value ?? []).Length;
                    break;
            }
        }

        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteI32LE(constants.Count);

        foreach (var constant in constants)
        {
            writer.WriteU8((byte)constant.Kind);

            switch (constant.Kind)
            {
                case NyarConstantKind.Int32:
                    writer.WriteI32LE(constant.Value is int i ? i : 0);
                    break;
                case NyarConstantKind.Float64:
                    writer.WriteF64LE(constant.Value is double d ? d : 0.0);
                    break;
                case NyarConstantKind.Bool:
                    writer.WriteU8(constant.Value is bool b && b ? (byte)1 : (byte)0);
                    break;
                case NyarConstantKind.Null:
                    break;
                case NyarConstantKind.String:
                    var strBytes = Encoding.UTF8.GetBytes((string?)constant.Value ?? "");
                    writer.WriteI32LE(strBytes.Length);
                    writer.Write(strBytes);
                    break;
                case NyarConstantKind.BigInt:
                    var bigIntBytes = (byte[]?)constant.Value ?? [];
                    writer.WriteI32LE(bigIntBytes.Length);
                    writer.Write(bigIntBytes);
                    break;
            }
        }

        return new NyarSection
        {
            Kind = NyarSectionKind.Constants,
            Data = buffer[..writer.Position].ToArray()
        };
    }

    private static NyarSection BuildFunctionsSection(IReadOnlyList<NyarFunction> functions)
    {
        var size = 4 + functions.Count * (4 + 4 + 4 + 4);

        foreach (var func in functions)
        {
            size += Encoding.UTF8.GetByteCount(func.Name);
        }

        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteI32LE(functions.Count);

        foreach (var func in functions)
        {
            WriteBinaryWriterString(ref writer, func.Name);
            writer.WriteI32LE(func.Arity);
            writer.WriteI32LE(func.LocalCount);
            writer.WriteI32LE(func.CodeLength);
        }

        return new NyarSection
        {
            Kind = NyarSectionKind.Functions,
            Data = buffer[..writer.Position].ToArray()
        };
    }

    private static NyarSection BuildImportsSection(IReadOnlyList<NyarImport> imports)
    {
        var size = 4;

        foreach (var import in imports)
        {
            size += 1 + 4 + Encoding.UTF8.GetByteCount(import.ModuleName) + 4 + Encoding.UTF8.GetByteCount(import.SymbolName);
        }

        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteI32LE(imports.Count);

        foreach (var import in imports)
        {
            writer.WriteU8((byte)import.Kind);
            WriteBinaryWriterString(ref writer, import.ModuleName);
            WriteBinaryWriterString(ref writer, import.SymbolName);
        }

        return new NyarSection
        {
            Kind = NyarSectionKind.Imports,
            Data = buffer[..writer.Position].ToArray()
        };
    }

    private static NyarSection BuildExportsSection(IReadOnlyList<NyarExport> exports)
    {
        var size = 4;

        foreach (var export in exports)
        {
            size += 1 + 4 + Encoding.UTF8.GetByteCount(export.SymbolName);
        }

        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteI32LE(exports.Count);

        foreach (var export in exports)
        {
            writer.WriteU8((byte)export.Kind);
            WriteBinaryWriterString(ref writer, export.SymbolName);
        }

        return new NyarSection
        {
            Kind = NyarSectionKind.Exports,
            Data = buffer[..writer.Position].ToArray()
        };
    }

    private static void WriteHeader(ref ByteBufferWriter writer, NyarModuleData data, int sectionCount)
    {
        writer.WriteU32LE(NyarConstants.MagicValue);
        writer.WriteU32LE(data.Version);
        writer.WriteI32LE(sectionCount);

        var nameOffset = NyarConstants.HeaderSize + sectionCount * NyarConstants.SectionHeaderSize;
        writer.WriteI32LE(nameOffset);
    }

    private static void WriteSectionHeaders(ref ByteBufferWriter writer, List<NyarSection> sections)
    {
        var dataStart = NyarConstants.HeaderSize + sections.Count * NyarConstants.SectionHeaderSize;

        var nameBytes = Encoding.UTF8.GetBytes(sections.Count > 0 ? "" : "");
        dataStart += 4;

        var currentOffset = dataStart;

        foreach (var section in sections)
        {
            currentOffset += section.Data.Length;
        }

        currentOffset = dataStart;

        for (var i = 0; i < sections.Count; i++)
        {
            writer.WriteU8((byte)sections[i].Kind);
            writer.WriteI32LE(currentOffset);
            writer.WriteI32LE(sections[i].Data.Length);
            currentOffset += sections[i].Data.Length;
        }
    }

    private static void WriteNameSection(ref ByteBufferWriter writer, string name)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        writer.WriteI32LE(nameBytes.Length);
        writer.Write(nameBytes);
    }

    private static void WriteBinaryWriterString(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.WriteI32LE(bytes.Length);
        writer.Write(bytes);
    }

    private static int EstimateSize(NyarModuleData data, List<NyarSection> sections)
    {
        var size = NyarConstants.HeaderSize;
        size += sections.Count * NyarConstants.SectionHeaderSize;
        size += 4 + Encoding.UTF8.GetByteCount(data.Name);

        foreach (var section in sections)
        {
            size += section.Data.Length;
        }

        return size + 256;
    }

    #endregion
}

/// <summary>
///     Nyar 段数据（内部使用）。
/// </summary>
internal sealed class NyarSection
{
    public NyarSectionKind Kind { get; init; }
    public byte[] Data { get; init; } = [];
}

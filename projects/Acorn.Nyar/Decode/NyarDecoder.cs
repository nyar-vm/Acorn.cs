using System.Numerics;
using System.Text;
using Acorn.Frame;
using Acorn.Nyar.Data;

namespace Acorn.Nyar.Decode;

public sealed class NyarDecoder
{
    public NyarModuleData Decode(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        return DecodeFromBuffer(ref buffer);
    }

    private NyarModuleData DecodeFromBuffer(ref ByteBuffer buffer)
    {
        var header = ReadHeader(ref buffer);
        if (!header.IsValid)
        {
            throw new InvalidNyarDataException(
                $"无效的 .nyar 文件头：magic=0x{header.Magic:X8}, version={header.Version}");
        }

        var sections = ReadSectionHeaders(ref buffer, header.SectionCount);
        var moduleName = ReadModuleName(ref buffer, header.NameOffset);

        var constants = new List<NyarConstant>();
        var functions = new List<NyarFunction>();
        var imports = new List<NyarImport>();
        var exports = new List<NyarExport>();

        foreach (var section in sections)
        {
            buffer.Position = section.Offset;
            DecodeSection(ref buffer, section.Kind, constants, functions, imports, exports);
        }

        return new NyarModuleData
        {
            Name = moduleName,
            Version = header.Version,
            Constants = constants,
            Functions = functions,
            Imports = imports,
            Exports = exports
        };
    }

    #region 头部读取

    private static NyarFileHeader ReadHeader(ref ByteBuffer buffer)
    {
        return new NyarFileHeader
        {
            Magic = buffer.ReadU32BE(),
            Version = buffer.ReadU32LE(),
            SectionCount = buffer.ReadI32LE(),
            NameOffset = buffer.ReadI32LE()
        };
    }

    private static List<NyarSectionHeader> ReadSectionHeaders(ref ByteBuffer buffer, int count)
    {
        var sections = new List<NyarSectionHeader>(count);
        for (var i = 0; i < count; i++)
        {
            sections.Add(new NyarSectionHeader
            {
                Kind = (NyarSectionKind)buffer.ReadU8(),
                Offset = buffer.ReadI32LE(),
                Size = buffer.ReadI32LE()
            });
        }

        return sections;
    }

    private static string ReadModuleName(ref ByteBuffer buffer, int nameOffset)
    {
        if (nameOffset <= 0)
        {
            return "<unknown>";
        }

        var savedPosition = buffer.Position;
        buffer.Position = nameOffset;
        var nameLength = buffer.ReadI32LE();
        var name = buffer.ReadString(nameLength);
        buffer.Position = savedPosition;
        return name;
    }

    #endregion

    #region 段解码

    private static void DecodeSection(ref ByteBuffer buffer, NyarSectionKind kind,
        List<NyarConstant> constants, List<NyarFunction> functions,
        List<NyarImport> imports, List<NyarExport> exports)
    {
        switch (kind)
        {
            case NyarSectionKind.Constants:
                DecodeConstants(ref buffer, constants);
                break;
            case NyarSectionKind.Functions:
                DecodeFunctions(ref buffer, functions);
                break;
            case NyarSectionKind.Imports:
                DecodeImports(ref buffer, imports);
                break;
            case NyarSectionKind.Exports:
                DecodeExports(ref buffer, exports);
                break;
        }
    }

    private static void DecodeConstants(ref ByteBuffer buffer, List<NyarConstant> constants)
    {
        var count = buffer.ReadI32LE();
        constants.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var kind = (NyarConstantKind)buffer.ReadU8();
            constants.Add(DecodeConstant(ref buffer, kind));
        }
    }

    private static NyarConstant DecodeConstant(ref ByteBuffer buffer, NyarConstantKind kind)
    {
        return kind switch
        {
            NyarConstantKind.Int32 => new NyarConstant { Kind = kind, Value = buffer.ReadI32LE() },
            NyarConstantKind.Float64 => new NyarConstant { Kind = kind, Value = buffer.ReadF64LE() },
            NyarConstantKind.Bool => new NyarConstant { Kind = kind, Value = buffer.ReadU8() != 0 },
            NyarConstantKind.Null => new NyarConstant { Kind = kind, Value = null },
            NyarConstantKind.String => new NyarConstant { Kind = kind, Value = ReadLengthPrefixedString(ref buffer) },
            NyarConstantKind.BigInt => new NyarConstant { Kind = kind, Value = DecodeBigIntBytes(ref buffer) },
            _ => throw new InvalidNyarDataException($"未知的常量类型：{kind}")
        };
    }

    private static byte[] DecodeBigIntBytes(ref ByteBuffer buffer)
    {
        var byteCount = buffer.ReadI32LE();
        var bytes = buffer.ReadBytes(byteCount);
        return bytes.ToArray();
    }

    private static void DecodeFunctions(ref ByteBuffer buffer, List<NyarFunction> functions)
    {
        var count = buffer.ReadI32LE();
        functions.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var name = ReadLengthPrefixedString(ref buffer);
            var arity = buffer.ReadI32LE();
            var localCount = buffer.ReadI32LE();
            var codeOffset = buffer.ReadI32LE();
            var codeLength = buffer.ReadI32LE();
            functions.Add(new NyarFunction
            {
                Name = name, Arity = arity, LocalCount = localCount,
                CodeOffset = codeOffset, CodeLength = codeLength
            });
        }
    }

    private static void DecodeImports(ref ByteBuffer buffer, List<NyarImport> imports)
    {
        var count = buffer.ReadI32LE();
        imports.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var kind = (NyarImportKind)buffer.ReadU8();
            var moduleName = ReadLengthPrefixedString(ref buffer);
            var symbolName = ReadLengthPrefixedString(ref buffer);
            imports.Add(new NyarImport { Kind = kind, ModuleName = moduleName, SymbolName = symbolName });
        }
    }

    private static void DecodeExports(ref ByteBuffer buffer, List<NyarExport> exports)
    {
        var count = buffer.ReadI32LE();
        exports.Capacity = count;
        for (var i = 0; i < count; i++)
        {
            var kind = (NyarExportKind)buffer.ReadU8();
            var symbolName = ReadLengthPrefixedString(ref buffer);
            var functionIndex = buffer.ReadI32LE();
            exports.Add(new NyarExport { Kind = kind, SymbolName = symbolName, FunctionIndex = functionIndex });
        }
    }

    private static string ReadLengthPrefixedString(ref ByteBuffer buffer)
    {
        var length = buffer.ReadI32LE();
        return buffer.ReadString(length);
    }

    #endregion

    #region 内部结构

    private sealed class NyarFileHeader
    {
        public uint Magic;
        public uint Version;
        public int SectionCount;
        public int NameOffset;

        public bool IsValid => Magic == NyarConstants.MagicValue;
    }

    private sealed class NyarSectionHeader
    {
        public NyarSectionKind Kind;
        public int Offset;
        public int Size;
    }

    #endregion
}

public sealed class InvalidNyarDataException : Exception
{
    public InvalidNyarDataException(string message) : base(message)
    {
    }

    public InvalidNyarDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

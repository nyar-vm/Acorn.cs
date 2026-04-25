using System.Text;
using Acorn.Frame;
using Acorn.Nyar.Data;

namespace Acorn.Nyar.Decode;

/// <summary>
///     Nyar 字节码模块解码器，将 .nyar 二进制格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     .nyar 是 NyarVM 的字节码模块格式，采用分段式二进制布局。
///     解码器完整解析模块结构，包括头部、段表、常量池、函数表、导入表和导出表。
///     二进制布局：[Header 16B] → [Section Headers N*9B] → [Name Section] → [Section Data...]
/// </remarks>
public ref struct NyarDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="NyarDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">.nyar 二进制数据。</param>
    public NyarDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 .nyar 模块。
    /// </summary>
    /// <returns>Nyar 模块数据。</returns>
    public NyarModuleData Decode()
    {
        if (_buffer.Remaining < NyarConstants.HeaderSize)
        {
            throw new InvalidDataException($".nyar 文件数据过短，期望至少 {NyarConstants.HeaderSize} 字节");
        }

        var magic = _buffer.ReadU32BE();

        if (magic != NyarConstants.MagicValue)
        {
            throw new InvalidDataException($".nyar 文件魔数无效，期望 NYAR(0x{NyarConstants.MagicValue:X8})，实际 0x{magic:X8}");
        }

        var version = _buffer.ReadU32LE();
        var sectionCount = _buffer.ReadI32LE();
        var nameOffset = _buffer.ReadI32LE();

        var sectionHeaders = ReadSectionHeaders(sectionCount);
        var moduleName = ReadModuleName(nameOffset);

        var constants = new List<NyarConstant>();
        var functions = new List<NyarFunction>();
        var imports = new List<NyarImport>();
        var exports = new List<NyarExport>();

        foreach (var header in sectionHeaders)
        {
            _buffer.Position = header.Offset;
            DecodeSection(header.Kind, constants, functions, imports, exports);
        }

        return new NyarModuleData
        {
            Version = version,
            Name = moduleName,
            Constants = constants,
            Functions = functions,
            Imports = imports,
            Exports = exports
        };
    }

    /// <summary>
    ///     仅解码 .nyar 模块头部信息。
    /// </summary>
    public (uint Version, string ModuleName) DecodeHeader()
    {
        if (_buffer.Remaining < NyarConstants.HeaderSize)
        {
            throw new InvalidDataException($".nyar 文件数据过短，期望至少 {NyarConstants.HeaderSize} 字节");
        }

        var magic = _buffer.ReadU32BE();

        if (magic != NyarConstants.MagicValue)
        {
            throw new InvalidDataException($".nyar 文件魔数无效，期望 NYAR(0x{NyarConstants.MagicValue:X8})，实际 0x{magic:X8}");
        }

        var version = _buffer.ReadU32LE();
        var sectionCount = _buffer.ReadI32LE();
        var nameOffset = _buffer.ReadI32LE();

        var moduleName = ReadModuleName(nameOffset);

        return (version, moduleName);
    }

    #region 私有解码方法

    private List<NyarSectionHeader> ReadSectionHeaders(int count)
    {
        var headers = new List<NyarSectionHeader>(count);

        for (var i = 0; i < count; i++)
        {
            if (_buffer.Remaining < NyarConstants.SectionHeaderSize)
            {
                throw new InvalidDataException($".nyar 段头数据不足，期望 {NyarConstants.SectionHeaderSize} 字节");
            }

            headers.Add(new NyarSectionHeader
            {
                Kind = (NyarSectionKind)_buffer.ReadU8(),
                Offset = _buffer.ReadI32LE(),
                Size = _buffer.ReadI32LE()
            });
        }

        return headers;
    }

    private string ReadModuleName(int nameOffset)
    {
        if (nameOffset <= 0 || nameOffset >= _buffer.Length)
        {
            return string.Empty;
        }

        var savedPosition = _buffer.Position;
        _buffer.Position = nameOffset;

        if (_buffer.Remaining < 4)
        {
            _buffer.Position = savedPosition;
            return string.Empty;
        }

        var nameLength = _buffer.ReadI32LE();

        if (nameLength <= 0 || _buffer.Remaining < nameLength)
        {
            _buffer.Position = savedPosition;
            return string.Empty;
        }

        var name = _buffer.ReadString(nameLength);
        _buffer.Position = savedPosition;
        return name;
    }

    private void DecodeSection(
        NyarSectionKind kind,
        List<NyarConstant> constants,
        List<NyarFunction> functions,
        List<NyarImport> imports,
        List<NyarExport> exports)
    {
        switch (kind)
        {
            case NyarSectionKind.Constants:
                DecodeConstants(constants);
                break;
            case NyarSectionKind.Functions:
                DecodeFunctions(functions);
                break;
            case NyarSectionKind.Imports:
                DecodeImports(imports);
                break;
            case NyarSectionKind.Exports:
                DecodeExports(exports);
                break;
        }
    }

    private void DecodeConstants(List<NyarConstant> constants)
    {
        if (_buffer.Remaining < 4)
        {
            throw new InvalidDataException(".nyar 常量池段数据不足，无法读取条目数量");
        }

        var count = _buffer.ReadI32LE();

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            if (_buffer.Remaining < 1)
            {
                throw new InvalidDataException($".nyar 常量池第 {i} 个条目数据不足，无法读取类型标签");
            }

            var kind = (NyarConstantKind)_buffer.ReadU8();
            var constant = DecodeConstant(kind);
            constants.Add(constant);
        }
    }

    private NyarConstant DecodeConstant(NyarConstantKind kind)
    {
        object? value = kind switch
        {
            NyarConstantKind.Int32 => _buffer.Remaining >= 4 ? _buffer.ReadI32LE() : 0,
            NyarConstantKind.Float64 => _buffer.Remaining >= 8 ? _buffer.ReadF64LE() : 0.0,
            NyarConstantKind.Bool => _buffer.Remaining >= 1 ? _buffer.ReadU8() != 0 : false,
            NyarConstantKind.Null => null,
            NyarConstantKind.String => ReadLengthPrefixedString(),
            NyarConstantKind.BigInt => ReadBigIntBytes(),
            _ => throw new InvalidDataException($".nyar 未知常量类型：0x{(byte)kind:X2}")
        };

        return new NyarConstant
        {
            Kind = kind,
            Value = value
        };
    }

    private string ReadLengthPrefixedString()
    {
        if (_buffer.Remaining < 4)
        {
            throw new InvalidDataException(".nyar 字符串长度前缀数据不足");
        }

        var length = _buffer.ReadI32LE();

        if (length < 0)
        {
            throw new InvalidDataException($".nyar 字符串长度为负数：{length}");
        }

        if (_buffer.Remaining < length)
        {
            throw new InvalidDataException($".nyar 字符串数据不足，期望 {length} 字节");
        }

        return _buffer.ReadString(length);
    }

    private byte[] ReadBigIntBytes()
    {
        if (_buffer.Remaining < 4)
        {
            throw new InvalidDataException(".nyar 大整数长度前缀数据不足");
        }

        var length = _buffer.ReadI32LE();

        if (length < 0)
        {
            throw new InvalidDataException($".nyar 大整数长度为负数：{length}");
        }

        if (_buffer.Remaining < length)
        {
            throw new InvalidDataException($".nyar 大整数数据不足，期望 {length} 字节");
        }

        return _buffer.ReadBytes(length).ToArray();
    }

    private void DecodeFunctions(List<NyarFunction> functions)
    {
        if (_buffer.Remaining < 4)
        {
            throw new InvalidDataException(".nyar 函数表段数据不足，无法读取条目数量");
        }

        var count = _buffer.ReadI32LE();

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            var name = ReadLengthPrefixedString();

            if (_buffer.Remaining < 12)
            {
                throw new InvalidDataException($".nyar 函数表第 {i} 个条目数据不足");
            }

            var arity = _buffer.ReadI32LE();
            var localCount = _buffer.ReadI32LE();
            var codeLength = _buffer.ReadI32LE();

            functions.Add(new NyarFunction
            {
                Name = name,
                Arity = arity,
                LocalCount = localCount,
                CodeLength = codeLength
            });
        }
    }

    private void DecodeImports(List<NyarImport> imports)
    {
        if (_buffer.Remaining < 4)
        {
            throw new InvalidDataException(".nyar 导入表段数据不足，无法读取条目数量");
        }

        var count = _buffer.ReadI32LE();

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            if (_buffer.Remaining < 1)
            {
                throw new InvalidDataException($".nyar 导入表第 {i} 个条目数据不足，无法读取类型标签");
            }

            var kind = (NyarImportKind)_buffer.ReadU8();
            var moduleName = ReadLengthPrefixedString();
            var symbolName = ReadLengthPrefixedString();

            imports.Add(new NyarImport
            {
                Kind = kind,
                ModuleName = moduleName,
                SymbolName = symbolName
            });
        }
    }

    private void DecodeExports(List<NyarExport> exports)
    {
        if (_buffer.Remaining < 4)
        {
            throw new InvalidDataException(".nyar 导出表段数据不足，无法读取条目数量");
        }

        var count = _buffer.ReadI32LE();

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            if (_buffer.Remaining < 1)
            {
                throw new InvalidDataException($".nyar 导出表第 {i} 个条目数据不足，无法读取类型标签");
            }

            var kind = (NyarExportKind)_buffer.ReadU8();
            var symbolName = ReadLengthPrefixedString();

            exports.Add(new NyarExport
            {
                Kind = kind,
                SymbolName = symbolName
            });
        }
    }

    #endregion
}

/// <summary>
///     Nyar 段头信息（解码内部使用）。
/// </summary>
internal struct NyarSectionHeader
{
    public NyarSectionKind Kind;
    public int Offset;
    public int Size;
}

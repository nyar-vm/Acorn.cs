using System.Text;
using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Decode;

/// <summary>
///     Gnosis 字节码模块解码器，将 .gnosis 字节码格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     .gnosis 模块有两种二进制格式：GGBC（ScriptCompiler 输出）和 GNOS（GnosisBackend 输出）。
///     解码器完整解析模块结构，包括常量池、符号表和指令字节码。
/// </remarks>
public ref struct GnosisDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="GnosisDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">.gnosis 二进制数据。</param>
    public GnosisDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 .gnosis 模块。
    /// </summary>
    /// <returns>Gnosis 模块数据。</returns>
    public GnosisModuleData Decode()
    {
        if (_buffer.Remaining < GnosisConstants.MinHeaderSize)
        {
            throw new InvalidDataException(".gnosis 文件数据过短");
        }

        var magic = _buffer.ReadU32LE();
        var version = _buffer.ReadU16LE();

        if (magic == GnosisConstants.GgbcMagicValue)
        {
            return DecodeGgbc(version);
        }

        if (magic == GnosisConstants.GnosMagicValue)
        {
            return DecodeGnos(version);
        }

        throw new InvalidDataException($".gnosis 文件魔数无效，期望 GGBC(0x47474243) 或 GNOS(0x474E4F53)，实际 0x{magic:X8}");
    }

    /// <summary>
    ///     仅解码 .gnosis 模块头部信息。
    /// </summary>
    public (GnosisModuleFormat Format, ushort Version, string ModuleName) DecodeHeader()
    {
        if (_buffer.Remaining < GnosisConstants.MinHeaderSize)
        {
            throw new InvalidDataException(".gnosis 文件数据过短");
        }

        var magic = _buffer.ReadU32LE();
        var version = _buffer.ReadU16LE();

        GnosisModuleFormat format;

        if (magic == GnosisConstants.GgbcMagicValue)
        {
            format = GnosisModuleFormat.Ggbc;
        }
        else if (magic == GnosisConstants.GnosMagicValue)
        {
            format = GnosisModuleFormat.Gnos;
        }
        else
        {
            throw new InvalidDataException($".gnosis 文件魔数无效");
        }

        string moduleName;

        if (format == GnosisModuleFormat.Ggbc)
        {
            var nameLength = _buffer.ReadU16LE();
            moduleName = nameLength > 0 ? _buffer.ReadString(nameLength) : string.Empty;
        }
        else
        {
            var nameLength = _buffer.ReadI32LE();
            moduleName = nameLength > 0 ? _buffer.ReadString(nameLength) : string.Empty;
        }

        return (format, version, moduleName);
    }

    #region 私有解码方法

    private GnosisModuleData DecodeGgbc(ushort version)
    {
        var nameLength = _buffer.ReadU16LE();
        var moduleName = nameLength > 0 ? _buffer.ReadString(nameLength) : string.Empty;

        var constantCount = _buffer.ReadI32LE();
        var constants = ReadConstants(constantCount);

        var importedSymbolCount = _buffer.ReadU16LE();
        var importedSymbols = ReadSymbolList(importedSymbolCount);

        var exportedSymbolCount = _buffer.ReadU16LE();
        var exportedSymbols = ReadSymbolList(exportedSymbolCount);

        var dependencyCount = _buffer.ReadU16LE();
        var dependencies = ReadSymbolList(dependencyCount);

        var instructionLength = _buffer.ReadI32LE();
        var instructions = instructionLength > 0 ? _buffer.ReadBytes(instructionLength).ToArray() : [];

        return new GnosisModuleData
        {
            Format = GnosisModuleFormat.Ggbc,
            Version = version,
            ModuleName = moduleName,
            Constants = constants,
            ImportedSymbols = importedSymbols,
            ExportedSymbols = exportedSymbols,
            Dependencies = dependencies,
            Instructions = instructions
        };
    }

    private GnosisModuleData DecodeGnos(ushort version)
    {
        var nameLength = _buffer.ReadI32LE();
        var moduleName = nameLength > 0 ? _buffer.ReadString(nameLength) : string.Empty;

        var constantCount = _buffer.ReadI32LE();
        var constants = ReadConstants(constantCount);

        var functionCount = _buffer.ReadI32LE();
        var functions = ReadFunctions(functionCount);

        return new GnosisModuleData
        {
            Format = GnosisModuleFormat.Gnos,
            Version = version,
            ModuleName = moduleName,
            Constants = constants,
            Functions = functions
        };
    }

    private List<GnosisConstant> ReadConstants(int count)
    {
        var constants = new List<GnosisConstant>(count);

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            var tag = _buffer.ReadU8();

            object? value = (GnosisConstantTag)tag switch
            {
                GnosisConstantTag.String => ReadLengthPrefixedString(),
                GnosisConstantTag.Int => _buffer.ReadI32LE(),
                GnosisConstantTag.Float => _buffer.ReadF64LE(),
                _ => null
            };

            constants.Add(new GnosisConstant
            {
                Tag = (GnosisConstantTag)tag,
                Value = value
            });
        }

        return constants;
    }

    private string ReadLengthPrefixedString()
    {
        var length = _buffer.ReadI32LE();
        return length > 0 ? _buffer.ReadString(length) : string.Empty;
    }

    private List<string> ReadSymbolList(int count)
    {
        var symbols = new List<string>(count);

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            symbols.Add(ReadBinaryWriterString());
        }

        return symbols;
    }

    private string ReadBinaryWriterString()
    {
        var length = _buffer.ReadI32LE();

        if (length <= 0)
        {
            return string.Empty;
        }

        var bytes = _buffer.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private List<GnosisFunction> ReadFunctions(int count)
    {
        var functions = new List<GnosisFunction>(count);

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            var name = ReadBinaryWriterString();
            var parameterCount = _buffer.ReadI32LE();
            var codeLength = _buffer.ReadI32LE();
            var code = codeLength > 0 ? _buffer.ReadBytes(codeLength).ToArray() : [];

            functions.Add(new GnosisFunction
            {
                Name = name,
                ParameterCount = parameterCount,
                CodeLength = codeLength,
                Code = code
            });
        }

        return functions;
    }

    #endregion
}

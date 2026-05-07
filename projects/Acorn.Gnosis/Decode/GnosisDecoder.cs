using System.Text;
using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Decode;

/// <summary>
///     Gnosis 字节码模块解码器，将 .gnosis 字节码格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     .gnosis 文件是 Gnosis VM 的字节码模块格式，基于 Game 方言特化。
///     解码器完整解析模块结构，包括常量池、符号表和指令字节码。
///     字符串编码兼容 .NET BinaryWriter.Write(string) 的 LEB128 长度前缀格式。
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

        if (magic != GnosisConstants.MagicValue)
        {
            throw new InvalidDataException($".gnosis 文件魔数无效，期望 GNOS(0x474E4F53)，实际 0x{magic:X8}");
        }

        var version = _buffer.ReadU16LE();

        var nameLength = _buffer.ReadU16LE();
        var moduleName = nameLength > 0 ? _buffer.ReadString(nameLength) : string.Empty;

        var constantCount = _buffer.ReadI32LE();
        var constants = ReadConstants(constantCount);

        var importedSymbolCount = _buffer.ReadU16LE();
        var importedSymbols = ReadLeb128StringList(importedSymbolCount);

        var exportedSymbolCount = _buffer.ReadU16LE();
        var exportedSymbols = ReadLeb128StringList(exportedSymbolCount);

        var dependencyCount = _buffer.ReadU16LE();
        var dependencies = ReadLeb128StringList(dependencyCount);

        var instructionLength = _buffer.ReadI32LE();
        var instructions = instructionLength > 0 ? _buffer.ReadBytes(instructionLength).ToArray() : [];

        return new GnosisModuleData
        {
            Version = version,
            ModuleName = moduleName,
            Constants = constants,
            ImportedSymbols = importedSymbols,
            ExportedSymbols = exportedSymbols,
            Dependencies = dependencies,
            Instructions = instructions
        };
    }

    /// <summary>
    ///     仅解码 .gnosis 模块头部信息。
    /// </summary>
    public (ushort Version, string ModuleName) DecodeHeader()
    {
        if (_buffer.Remaining < GnosisConstants.MinHeaderSize)
        {
            throw new InvalidDataException(".gnosis 文件数据过短");
        }

        var magic = _buffer.ReadU32LE();

        if (magic != GnosisConstants.MagicValue)
        {
            throw new InvalidDataException(".gnosis 文件魔数无效");
        }

        var version = _buffer.ReadU16LE();
        var nameLength = _buffer.ReadU16LE();
        var moduleName = nameLength > 0 ? _buffer.ReadString(nameLength) : string.Empty;

        return (version, moduleName);
    }

    #region 私有解码方法

    private List<GnosisConstant> ReadConstants(int count)
    {
        var constants = new List<GnosisConstant>(count);

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            var tag = _buffer.ReadU8();

            object? value = (GnosisConstantTag)tag switch
            {
                GnosisConstantTag.String => _buffer.ReadLeb128String(),
                GnosisConstantTag.Int => _buffer.ReadI64LE(),
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

    private List<string> ReadLeb128StringList(int count)
    {
        var symbols = new List<string>(count);

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            symbols.Add(_buffer.ReadLeb128String());
        }

        return symbols;
    }

    #endregion
}

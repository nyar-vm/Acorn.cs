using System.Text;
using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Encode;

/// <summary>
///     Gnosis 字节码模块编码器，将 C# 数据结构编码为 .gnosis 字节码格式。
/// </summary>
/// <remarks>
///     .gnosis 模块有两种二进制格式：GGBC（ScriptCompiler 输出）和 GNOS（GnosisBackend 输出）。
///     编码器根据模块数据的 Format 属性选择对应的编码方式。
/// </remarks>
public sealed class GnosisEncoder
{
    /// <summary>
    ///     将 Gnosis 模块数据编码为 .gnosis 二进制格式。
    /// </summary>
    /// <param name="data">Gnosis 模块数据。</param>
    /// <returns>.gnosis 二进制数据。</returns>
    public byte[] Encode(GnosisModuleData data)
    {
        var size = EstimateSize(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        if (data.Format == GnosisModuleFormat.Ggbc)
        {
            EncodeGgbc(ref writer, data);
        }
        else
        {
            EncodeGnos(ref writer, data);
        }

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void EncodeGgbc(ref ByteBufferWriter writer, GnosisModuleData data)
    {
        writer.WriteU32LE(GnosisConstants.GgbcMagicValue);
        writer.WriteU16LE(data.Version);

        WriteModuleNameU16(ref writer, data.ModuleName);
        WriteConstants(ref writer, data.Constants);
        WriteSymbolListU16(ref writer, data.ImportedSymbols);
        WriteSymbolListU16(ref writer, data.ExportedSymbols);
        WriteSymbolListU16(ref writer, data.Dependencies);
        WriteInstructions(ref writer, data.Instructions);
    }

    private static void EncodeGnos(ref ByteBufferWriter writer, GnosisModuleData data)
    {
        writer.WriteU32LE(GnosisConstants.GnosMagicValue);
        writer.WriteU16LE(data.Version);

        WriteModuleNameI32(ref writer, data.ModuleName);
        WriteConstants(ref writer, data.Constants);
        WriteFunctions(ref writer, data.Functions);
    }

    private static void WriteModuleNameU16(ref ByteBufferWriter writer, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        writer.WriteU16LE((ushort)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteModuleNameI32(ref ByteBufferWriter writer, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        writer.WriteI32LE(bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteConstants(ref ByteBufferWriter writer, IReadOnlyList<GnosisConstant> constants)
    {
        writer.WriteI32LE(constants.Count);

        foreach (var constant in constants)
        {
            writer.WriteU8((byte)constant.Tag);

            switch (constant.Tag)
            {
                case GnosisConstantTag.String:
                    WriteBinaryWriterString(ref writer, (string?)constant.Value ?? string.Empty);
                    break;

                case GnosisConstantTag.Int:
                    writer.WriteI32LE(constant.Value is int i ? i : 0);
                    break;

                case GnosisConstantTag.Float:
                    writer.WriteF64LE(constant.Value is double d ? d : 0.0);
                    break;
            }
        }
    }

    private static void WriteSymbolListU16(ref ByteBufferWriter writer, IReadOnlyList<string> symbols)
    {
        writer.WriteU16LE((ushort)symbols.Count);

        foreach (var symbol in symbols)
        {
            WriteBinaryWriterString(ref writer, symbol);
        }
    }

    private static void WriteBinaryWriterString(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.WriteI32LE(bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteInstructions(ref ByteBufferWriter writer, byte[] instructions)
    {
        writer.WriteI32LE(instructions.Length);
        writer.Write(instructions);
    }

    private static void WriteFunctions(ref ByteBufferWriter writer, IReadOnlyList<GnosisFunction> functions)
    {
        writer.WriteI32LE(functions.Count);

        foreach (var function in functions)
        {
            WriteBinaryWriterString(ref writer, function.Name);
            writer.WriteI32LE(function.ParameterCount);
            writer.WriteI32LE(function.Code.Length);
            writer.Write(function.Code);
        }
    }

    private static int EstimateSize(GnosisModuleData data)
    {
        var size = GnosisConstants.MinHeaderSize;

        size += 2 + Encoding.UTF8.GetByteCount(data.ModuleName);
        size += 4;

        foreach (var constant in data.Constants)
        {
            size += 1;

            switch (constant.Tag)
            {
                case GnosisConstantTag.String:
                    size += 4 + Encoding.UTF8.GetByteCount((string?)constant.Value ?? "");
                    break;
                case GnosisConstantTag.Int:
                    size += 4;
                    break;
                case GnosisConstantTag.Float:
                    size += 8;
                    break;
            }
        }

        size += 2;

        foreach (var s in data.ImportedSymbols)
        {
            size += 4 + Encoding.UTF8.GetByteCount(s);
        }

        size += 2;

        foreach (var s in data.ExportedSymbols)
        {
            size += 4 + Encoding.UTF8.GetByteCount(s);
        }

        size += 2;

        foreach (var s in data.Dependencies)
        {
            size += 4 + Encoding.UTF8.GetByteCount(s);
        }

        size += 4 + data.Instructions.Length;

        size += 4;

        foreach (var f in data.Functions)
        {
            size += 4 + Encoding.UTF8.GetByteCount(f.Name) + 4 + 4 + f.Code.Length;
        }

        return size + 256;
    }

    #endregion
}

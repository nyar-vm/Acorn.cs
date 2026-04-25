using System.Text;
using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Encode;

/// <summary>
///     Gnosis 字节码模块编码器，将 C# 数据结构编码为 .gnosis 字节码格式。
/// </summary>
/// <remarks>
///     .gnosis 文件是 Gnosis VM 的字节码模块格式，基于 Game 方言特化。
///     编码器生成与 Gnosis.Toolchain.BytecodeGenerator 兼容的二进制数据。
///     字符串编码使用 LEB128 长度前缀格式（兼容 .NET BinaryWriter.Write(string)）。
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

        writer.WriteU32LE(GnosisConstants.MagicValue);
        writer.WriteU16LE(data.Version);

        WriteModuleName(ref writer, data.ModuleName);
        WriteConstants(ref writer, data.Constants);
        WriteSymbolList(ref writer, data.ImportedSymbols);
        WriteSymbolList(ref writer, data.ExportedSymbols);
        WriteSymbolList(ref writer, data.Dependencies);
        WriteInstructions(ref writer, data.Instructions);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteModuleName(ref ByteBufferWriter writer, string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        writer.WriteU16LE((ushort)bytes.Length);
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
                    writer.WriteLeb128String((string?)constant.Value ?? string.Empty);
                    break;

                case GnosisConstantTag.Int:
                    writer.WriteI32LE(constant.Value is int i ? i : 0);
                    break;

                case GnosisConstantTag.Float:
                    writer.WriteF32LE(constant.Value is float f ? f : 0.0f);
                    break;
            }
        }
    }

    private static void WriteSymbolList(ref ByteBufferWriter writer, IReadOnlyList<string> symbols)
    {
        writer.WriteU16LE((ushort)symbols.Count);

        foreach (var symbol in symbols)
        {
            writer.WriteLeb128String(symbol);
        }
    }

    private static void WriteInstructions(ref ByteBufferWriter writer, byte[] instructions)
    {
        writer.WriteI32LE(instructions.Length);
        writer.Write(instructions);
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
                    size += 5 + Encoding.UTF8.GetByteCount((string?)constant.Value ?? "");
                    break;
                case GnosisConstantTag.Int:
                    size += 4;
                    break;
                case GnosisConstantTag.Float:
                    size += 4;
                    break;
            }
        }

        size += 2;

        foreach (var s in data.ImportedSymbols)
        {
            size += 5 + Encoding.UTF8.GetByteCount(s);
        }

        size += 2;

        foreach (var s in data.ExportedSymbols)
        {
            size += 5 + Encoding.UTF8.GetByteCount(s);
        }

        size += 2;

        foreach (var s in data.Dependencies)
        {
            size += 5 + Encoding.UTF8.GetByteCount(s);
        }

        size += 4 + data.Instructions.Length;

        return size + 256;
    }

    #endregion
}

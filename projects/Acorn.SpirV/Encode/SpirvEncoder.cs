using System.Buffers.Binary;
using System.Text;
using Acorn.Frame;
using Acorn.Spirv.Data;

namespace Acorn.Spirv.Encode;

/// <summary>
///     SPIR-V 模块编码器，将 C# 数据结构编码为 Khronos SPIR-V 二进制中间语言格式。
/// </summary>
/// <remarks>
///     SPIR-V 是 Khronos 定义的着色器二进制中间语言，用于 Vulkan、OpenCL 等图形和计算 API。
///     编码器生成符合 Khronos SPIR-V 规范的二进制数据。
/// </remarks>
public sealed class SpirvEncoder
{
    /// <summary>
    ///     将模块数据编码为 SPIR-V 二进制格式。
    /// </summary>
    /// <param name="data">模块数据。</param>
    /// <returns>SPIR-V 二进制数据。</returns>
    public byte[] Encode(SpirvModuleData data)
    {
        var size = 20 + CalculateInstructionsSize(data.Instructions);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteHeader(ref writer, data);
        WriteInstructions(ref writer, data.Instructions);

        return buffer[..writer.Position];
    }

    /// <summary>
    ///     将指令列表编码为 SPIR-V 二进制格式（不包含文件头）。
    /// </summary>
    /// <param name="instructions">指令列表。</param>
    /// <returns>SPIR-V 指令流二进制数据。</returns>
    public byte[] EncodeInstructions(IReadOnlyList<SpirvInstruction> instructions)
    {
        var size = CalculateInstructionsSize(instructions);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteInstructions(ref writer, instructions);

        return buffer[..writer.Position];
    }

    /// <summary>
    ///     编码一条 SPIR-V 指令。
    /// </summary>
    /// <param name="opcode">操作码。</param>
    /// <param name="operands">操作数字列表。</param>
    /// <returns>编码后的指令二进制数据。</returns>
    public byte[] EncodeInstruction(SpirvOpCode opcode, IReadOnlyList<uint> operands)
    {
        var wordCount = (ushort)(1 + operands.Count);
        var buffer = new byte[wordCount * 4];
        var writer = new ByteBufferWriter(buffer);

        var firstWord = (uint)(wordCount << 16) | (ushort)opcode;
        writer.WriteU32LE(firstWord);

        foreach (var operand in operands)
        {
            writer.WriteU32LE(operand);
        }

        return buffer[..writer.Position];
    }

    /// <summary>
    ///     编码字符串为 SPIR-V 字序列（以 null 终止并填充到 4 字节对齐）。
    /// </summary>
    /// <param name="value">字符串值。</param>
    /// <returns>编码后的字列表。</returns>
    public IReadOnlyList<uint> EncodeString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var paddedLength = (bytes.Length + 1 + 3) & ~3;
        var paddedBytes = new byte[paddedLength];

        Array.Copy(bytes, paddedBytes, bytes.Length);

        for (var i = bytes.Length; i < paddedLength; i++)
        {
            paddedBytes[i] = 0;
        }

        var words = new uint[paddedLength / 4];

        for (var i = 0; i < words.Length; i++)
        {
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(paddedBytes.AsSpan(i * 4, 4));
        }

        return words;
    }

    #region 私有编码方法

    private static void WriteHeader(ref ByteBufferWriter writer, SpirvModuleData data)
    {
        writer.WriteU32LE(data.MagicNumber);
        writer.WriteU32LE(data.Version);
        writer.WriteU32LE(data.GeneratorMagic);
        writer.WriteU32LE(data.Bound);
        writer.WriteU32LE(data.Schema);
    }

    private static void WriteInstructions(ref ByteBufferWriter writer, IReadOnlyList<SpirvInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            WriteInstruction(ref writer, instruction);
        }
    }

    private static void WriteInstruction(ref ByteBufferWriter writer, SpirvInstruction instruction)
    {
        var wordCount = (ushort)(1 + instruction.Operands.Count);
        var firstWord = (uint)(wordCount << 16) | (ushort)instruction.Opcode;

        writer.WriteU32LE(firstWord);

        foreach (var operand in instruction.Operands)
        {
            writer.WriteU32LE(operand);
        }
    }

    private static int CalculateInstructionsSize(IReadOnlyList<SpirvInstruction> instructions)
    {
        var size = 0;

        foreach (var instruction in instructions)
        {
            size += (1 + instruction.Operands.Count) * 4;
        }

        return size;
    }

    #endregion
}

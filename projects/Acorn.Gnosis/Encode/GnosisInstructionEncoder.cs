using System.Runtime.CompilerServices;
using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Encode;

/// <summary>
///     Gnosis VM 指令编码器，将结构化指令编码为扁平字节码流。
/// </summary>
/// <remarks>
///     编码器将 GnosisInstruction 列表编码为与 Gnosis.Runtime.VM.VMInterpreter 兼容的字节码格式。
///     操作数格式与 BytecodeModuleAdapter.WriteOperand 的写入方式一致。
/// </remarks>
public sealed class GnosisInstructionEncoder
{
    /// <summary>
    ///     将指令列表编码为字节码流。
    /// </summary>
    /// <param name="instructions">指令列表。</param>
    /// <returns>字节码数据。</returns>
    public byte[] Encode(IReadOnlyList<GnosisInstruction> instructions)
    {
        var size = EstimateSize(instructions);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        foreach (var instruction in instructions)
        {
            writer.WriteU8((byte)instruction.OpCode);
            WriteOperand(ref writer, instruction.OpCode, instruction.RawOperand);
        }

        return buffer[..writer.Position];
    }

    /// <summary>
    ///     将单条指令编码为字节码。
    /// </summary>
    /// <param name="instruction">指令。</param>
    /// <returns>字节码数据。</returns>
    public byte[] EncodeOne(GnosisInstruction instruction)
    {
        var operandSize = GnosisOpCodeInfo.GetOperandSize(instruction.OpCode);
        var buffer = new byte[1 + operandSize];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteU8((byte)instruction.OpCode);
        WriteOperand(ref writer, instruction.OpCode, instruction.RawOperand);

        return buffer[..writer.Position];
    }

    #region 私有方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteOperand(ref ByteBufferWriter writer, GnosisOpCode opCode, long rawOperand)
    {
        var format = GnosisOpCodeInfo.GetOperandFormat(opCode);

        switch (format)
        {
            case GnosisOperandFormat.Int8:
                writer.WriteU8((byte)rawOperand);
                break;
            case GnosisOperandFormat.Int16:
                writer.WriteI16LE((short)rawOperand);
                break;
            case GnosisOperandFormat.Int32:
                writer.WriteI32LE((int)rawOperand);
                break;
            case GnosisOperandFormat.Int64:
                writer.WriteI64LE(rawOperand);
                break;
            case GnosisOperandFormat.Float32:
                writer.WriteI32LE((int)rawOperand);
                break;
            case GnosisOperandFormat.Float64:
                writer.WriteI64LE(rawOperand);
                break;
        }
    }

    private static int EstimateSize(IReadOnlyList<GnosisInstruction> instructions)
    {
        var size = 0;

        foreach (var instruction in instructions)
        {
            size += 1 + GnosisOpCodeInfo.GetOperandSize(instruction.OpCode);
        }

        return size + 64;
    }

    #endregion
}

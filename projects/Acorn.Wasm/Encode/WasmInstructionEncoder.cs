using System;
using System.Collections.Generic;
using Acorn.Frame;
using Acorn.Wasm.Data;

namespace Acorn.Wasm.Encode;

/// <summary>
///     WasmInstruction 编码器，将结构化指令编码为 WASM 字节码。
/// </summary>
public static class WasmInstructionEncoder
{
    /// <summary>
    ///     编码单个指令，写入 ByteBufferWriter。
    /// </summary>
    /// <returns>编码的字节数。</returns>
    public static int Encode(ref ByteBufferWriter writer, WasmInstruction instruction)
    {
        var startPos = writer.Position;
        var opcodeValue = (ushort)instruction.Opcode;

        if (opcodeValue > 0xFF)
        {
            var prefix = (byte)(opcodeValue >> 8);
            var suffix = (byte)(opcodeValue & 0xFF);
            writer.WriteU8(prefix);
            writer.WriteU8(suffix);
        }
        else
        {
            writer.WriteU8((byte)opcodeValue);
        }

        if (instruction.Operands is { } operands)
        {
            foreach (var operand in operands)
            {
                EncodeOperand(ref writer, operand);
            }
        }

        return writer.Position - startPos;
    }

    /// <summary>
    ///     编码指令列表并返回 BodySize（用于 WasmCode.MaxLength）。
    /// </summary>
    /// <returns>BodySize（指令列表的总字节数）。</returns>
    public static uint EncodeList(ref ByteBufferWriter writer, IReadOnlyList<WasmInstruction> instructions)
    {
        var startPos = writer.Position;
        foreach (var instruction in instructions)
        {
            Encode(ref writer, instruction);
        }
        return (uint)(writer.Position - startPos);
    }

    private static void EncodeOperand(ref ByteBufferWriter writer, WasmImmediate operand)
    {
        switch (operand)
        {
            case WasmI32Imm i32:
                writer.WriteLeb128I32(i32.Value);
                break;

            case WasmI64Imm i64:
                writer.WriteLeb128I64(i64.Value);
                break;

            case WasmF32Imm f32:
                writer.WriteF32LE(f32.Value);
                break;

            case WasmF64Imm f64:
                writer.WriteF64LE(f64.Value);
                break;

            case WasmTypeIndexImm typeIdx:
                writer.WriteLeb128U32(typeIdx.Index);
                break;

            case WasmFuncIndexImm funcIdx:
                writer.WriteLeb128U32(funcIdx.Index);
                break;

            case WasmFieldIndexImm fieldIdx:
                writer.WriteLeb128U32(fieldIdx.Index);
                break;

            case WasmHeapTypeImm heapIdx:
                writer.WriteLeb128U32(heapIdx.Index);
                break;

            case WasmLabelIndexImm labelIdx:
                writer.WriteLeb128U32(labelIdx.Index);
                break;

            case WasmLocalIndexImm localIdx:
                writer.WriteLeb128U32(localIdx.Index);
                break;

            case WasmGlobalIndexImm globalIdx:
                writer.WriteLeb128U32(globalIdx.Index);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operand), operand.GetType().Name, "未知的立即数类型");
        }
    }
}

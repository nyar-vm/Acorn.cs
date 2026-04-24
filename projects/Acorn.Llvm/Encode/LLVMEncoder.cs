using System.Buffers.Binary;
using Acorn.Frame;
using Acorn.LLVM.Data;

namespace Acorn.LLVM.Encode;

/// <summary>
///     LLVM 位码编码器，将 LLVM 位码数据结构编码为 LLVM Bitcode 二进制格式。
/// </summary>
/// <remarks>
///     本编码器生成 LLVM 3.7 兼容的 Bitcode 格式，用于 DXIL 着色器程序。
///     LLVM Bitcode 使用基于位流的变长编码，包含块和记录两种结构。
///     编码器遵循 Acorn 架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class LLVMEncoder
{
    /// <summary>
    ///     LLVM Bitcode 魔数（"BC" + 0xC0 + 0xDE）。
    /// </summary>
    public const uint BitcodeMagic = 0xDEC04242u;

    /// <summary>
    ///     将 LLVM 位码数据编码为 Bitcode 二进制格式。
    /// </summary>
    /// <param name="data">LLVM 位码数据。</param>
    /// <returns>LLVM Bitcode 二进制数据。</returns>
    public byte[] Encode(LLVMBitcodeData data)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(new byte[] { 0x42, 0x43, 0xC0, 0xDE });

        WriteU16LE(writer, data.Magic.Version);

        WriteBlocks(writer, data.TopLevelBlocks);

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    ///     将块列表编码为 Bitcode 二进制格式。
    /// </summary>
    /// <param name="blocks">块列表。</param>
    /// <returns>Bitcode 二进制数据（不含魔数）。</returns>
    public byte[] EncodeBlocks(IReadOnlyList<LLVMBlockData> blocks)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        WriteBlocks(writer, blocks);

        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteBlocks(BinaryWriter writer, IReadOnlyList<LLVMBlockData> blocks)
    {
        foreach (var block in blocks)
        {
            WriteBlock(writer, block);
        }
    }

    private static void WriteBlock(BinaryWriter writer, LLVMBlockData block)
    {
        WriteLeb128U32(writer, block.BlockID);

        using var blockStream = new MemoryStream();
        var blockWriter = new BinaryWriter(blockStream);

        foreach (var subBlock in block.SubBlocks)
        {
            WriteLeb128U32(blockWriter, 1u);
            WriteBlock(blockWriter, subBlock);
        }

        foreach (var record in block.Records)
        {
            WriteRecord(blockWriter, record);
        }

        WriteLeb128U32(blockWriter, 2u);

        blockWriter.Flush();
        var blockData = blockStream.ToArray();

        WriteLeb128U32(writer, (uint)blockData.Length);
        writer.Write(blockData);
    }

    private static void WriteRecord(BinaryWriter writer, LLVMRecordData record)
    {
        WriteLeb128U32(writer, record.Code);
        WriteLeb128U32(writer, (uint)record.Operands.Count);

        foreach (var operand in record.Operands)
        {
            WriteLeb128U64(writer, operand);
        }
    }

    private static void WriteU16LE(BinaryWriter writer, ushort value)
    {
        var bytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteLeb128U32(BinaryWriter writer, uint value)
    {
        do
        {
            var byteVal = value & 0x7Fu;
            value >>= 7;

            if (value != 0)
            {
                byteVal |= 0x80u;
            }

            writer.Write((byte)byteVal);
        } while (value != 0);
    }

    private static void WriteLeb128U64(BinaryWriter writer, ulong value)
    {
        do
        {
            var byteVal = value & 0x7FUL;
            value >>= 7;

            if (value != 0)
            {
                byteVal |= 0x80u;
            }

            writer.Write((byte)byteVal);
        } while (value != 0);
    }
}

using Acorn.Frame;
using Acorn.LLVM.Data;

namespace Acorn.LLVM.Decode;

/// <summary>
///     LLVM 位码文件解码器，解析 LLVM 位码（.bc）格式。
/// </summary>
public sealed class LLVMDecoder
{
    /// <summary>
    ///     从 LLVM 位码二进制数据解码文件。
    /// </summary>
    /// <param name="data">LLVM 位码二进制数据。</param>
    /// <returns>解码后的 LLVM 位码文件数据。</returns>
    public LLVMBitcodeData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return DecodeFile(ref buffer);
    }

    private LLVMBitcodeData DecodeFile(ref ByteBuffer buffer)
    {
        var magicBytes = buffer.ReadBytes(4).ToArray();

        if (magicBytes[0] != 0x42 || magicBytes[1] != 0x43 || magicBytes[2] != 0xC0 || magicBytes[3] != 0xDE)
        {
            throw new InvalidDataException("不是有效的 LLVM 位码文件（魔数不匹配）");
        }

        var magic = ReadMagic(ref buffer);
        var blocks = ReadBlocks(ref buffer);

        return new LLVMBitcodeData
        {
            Magic = magic,
            TopLevelBlocks = blocks
        };
    }

    /// <summary>
    ///     读取魔数信息。
    /// </summary>
    private LLVMMagicData ReadMagic(ref ByteBuffer buffer)
    {
        var version = buffer.ReadU16LE();

        return new LLVMMagicData
        {
            Magic = 0x42C0DE00u | version,
            Version = version
        };
    }

    /// <summary>
    ///     读取块列表。
    /// </summary>
    private List<LLVMBlockData> ReadBlocks(ref ByteBuffer buffer)
    {
        var blocks = new List<LLVMBlockData>();

        while (!buffer.IsEnd)
        {
            var block = ReadBlock(ref buffer);
            if (block != null)
            {
                blocks.Add(block);
            }
            else
            {
                break;
            }
        }

        return blocks;
    }

    /// <summary>
    ///     读取单个块。
    /// </summary>
    private LLVMBlockData? ReadBlock(ref ByteBuffer buffer)
    {
        if (buffer.Remaining < 8)
        {
            return null;
        }

        var blockId = buffer.ReadLeb128U32();
        var blockSize = buffer.ReadLeb128U32();

        if (blockSize == 0 || buffer.Position + blockSize > buffer.Length)
        {
            return null;
        }

        var blockEnd = buffer.Position + (int)blockSize;
        var records = new List<LLVMRecordData>();
        var subBlocks = new List<LLVMBlockData>();

        while (buffer.Position < blockEnd)
        {
            var code = buffer.ReadLeb128U32();

            if (code == 0)
            {
                break;
            }

            if (code == 1)
            {
                var subBlock = ReadBlock(ref buffer);
                if (subBlock != null)
                {
                    subBlocks.Add(subBlock);
                }

                continue;
            }

            if (code == 2)
            {
                buffer.Position = Math.Min(blockEnd, buffer.Length);
                break;
            }

            var record = ReadRecord(ref buffer, code);
            records.Add(record);
        }

        buffer.Position = Math.Min(blockEnd, buffer.Length);

        return new LLVMBlockData
        {
            BlockID = blockId,
            BlockSize = blockSize,
            Records = records,
            SubBlocks = subBlocks
        };
    }

    /// <summary>
    ///     读取记录。
    /// </summary>
    private LLVMRecordData ReadRecord(ref ByteBuffer buffer, uint code)
    {
        var operandCount = buffer.ReadLeb128U32();
        var operands = new List<ulong>();

        for (var i = 0; i < operandCount; i++)
        {
            operands.Add(buffer.ReadLeb128U64());
        }

        return new LLVMRecordData
        {
            Code = code,
            Operands = operands
        };
    }
}

using Acorn.Clr.Data;
using Acorn.Pe.Data;
using System;
using System.IO;
using System.Text;

namespace Acorn.Clr.Encode;

/// <summary>
///     CLR 模块编码器
/// </summary>
public sealed class ClrEncoder
{
    /// <summary>
    ///     编码 CLR 模块为字节数组
    /// </summary>
    public byte[] Encode(ClrModuleData module)
    {
        // 简化实现：直接编码 CLR 元数据和 MSIL 指令
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // 写入 CLR 目录表
        writer.Write(EncodeClrDirectory(module.ClrDirectory));

        // 写入元数据
        writer.Write(EncodeMetadata(module.Metadata));

        // 写入方法指令
        foreach (var method in module.Methods)
        {
            writer.Write(EncodeInstructions(method.Instructions));
        }

        return ms.ToArray();
    }

    /// <summary>
    ///     编码 CLR 目录表
    /// </summary>
    private byte[] EncodeClrDirectory(ClrDirectoryData directory)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(directory.Characteristics);
        writer.Write(directory.MajorVersion);
        writer.Write(directory.MinorVersion);
        writer.Write(directory.MetadataRva);
        writer.Write(directory.MetadataSize);
        writer.Write(directory.Flags);
        writer.Write(directory.EntryPointRva);
        writer.Write(directory.ResourcesRva);
        writer.Write(directory.ResourcesSize);
        writer.Write(directory.StrongNameSignatureRva);
        writer.Write(directory.StrongNameSignatureSize);
        writer.Write(directory.CodeManagerTableRva);
        writer.Write(directory.CodeManagerTableSize);
        writer.Write(directory.VTableFixupsRva);
        writer.Write(directory.VTableFixupsSize);
        writer.Write(directory.ExportAddressTableJumpsRva);
        writer.Write(directory.ExportAddressTableJumpsSize);
        writer.Write(directory.ManagedNativeHeaderRva);
        writer.Write(directory.ManagedNativeHeaderSize);

        return ms.ToArray();
    }

    /// <summary>
    ///     编码元数据
    /// </summary>
    private byte[] EncodeMetadata(ClrMetadata metadata)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // 编码元数据头
        writer.Write(metadata.Header.Magic);
        writer.Write(metadata.Header.MajorVersion);
        writer.Write(metadata.Header.MinorVersion);
        writer.Write(metadata.Header.Reserved);
        writer.Write(metadata.Header.VersionStringLength);
        writer.Write(Encoding.UTF8.GetBytes(metadata.Header.VersionString));
        writer.Write(metadata.Header.Flags);
        writer.Write(metadata.Header.Streams);

        // 编码表流、字符串堆、Blob 堆等
        // 简化实现

        return ms.ToArray();
    }

    /// <summary>
    ///     编码 MSIL 指令
    /// </summary>
    private byte[] EncodeInstructions(IReadOnlyList<ClrInstruction> instructions)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        foreach (var instr in instructions)
        {
            writer.Write((byte)instr.Opcode);
            EncodeOperand(writer, instr.Operand);
        }

        return ms.ToArray();
    }

    /// <summary>
    ///     编码操作数
    /// </summary>
    private void EncodeOperand(BinaryWriter writer, ClrOperand? operand)
    {
        if (operand is null)
        {
            return;
        }

        switch (operand.Kind)
        {
            case ClrOperandKind.Int32:
                writer.Write(((ClrInt32Operand)operand).Value);
                break;
            case ClrOperandKind.Int64:
                writer.Write(((ClrInt64Operand)operand).Value);
                break;
            case ClrOperandKind.Float32:
                writer.Write(((ClrFloat32Operand)operand).Value);
                break;
            case ClrOperandKind.Float64:
                writer.Write(((ClrFloat64Operand)operand).Value);
                break;
            case ClrOperandKind.String:
                var str = ((ClrStringOperand)operand).Value;
                var strBytes = Encoding.UTF8.GetBytes(str);
                writer.Write(strBytes.Length);
                writer.Write(strBytes);
                break;
            case ClrOperandKind.BranchTarget:
                writer.Write(((ClrBranchTargetOperand)operand).Offset);
                break;
            case ClrOperandKind.SwitchTargets:
                var offsets = ((ClrSwitchTargetsOperand)operand).Offsets;
                writer.Write(offsets.Count);
                foreach (var offset in offsets)
                {
                    writer.Write(offset);
                }
                break;
            case ClrOperandKind.LocalIndex:
                writer.Write(((ClrLocalIndexOperand)operand).Index);
                break;
            case ClrOperandKind.ArgumentIndex:
                writer.Write(((ClrArgumentIndexOperand)operand).Index);
                break;
            case ClrOperandKind.Token:
                writer.Write(((ClrTokenOperand)operand).Value);
                break;
        }
    }
}

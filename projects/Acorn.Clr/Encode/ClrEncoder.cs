using Acorn.Clr.Data;
using Acorn.Frame;
using System.Text;

namespace Acorn.Clr.Encode;

/// <summary>
///     CLR 模块编码器，将 ClrModuleData 编码为 .NET 程序集（PE + CLR 元数据 + MSIL）。
/// </summary>
public sealed class ClrEncoder
{
    /// <summary>
    ///     将 CLR 模块数据编码为字节数组。
    /// </summary>
    public byte[] Encode(ClrModuleData module)
    {
        var peBuilder = new PeBuilder(module);
        return peBuilder.Build();
    }

    /// <summary>
    ///     仅编码 MSIL 指令序列为字节数组。
    /// </summary>
    public static byte[] EncodeInstructions(IReadOnlyList<ClrInstruction> instructions)
    {
        var size = EstimateInstructionSize(instructions);
        var writer = new ByteBufferWriter(size);

        foreach (var instr in instructions)
        {
            WriteInstruction(ref writer, instr);
        }

        return writer.ToArray();
    }

    /// <summary>
    ///     编码方法体（Fat 头 + MSIL + 异常处理表）。
    /// </summary>
    public static byte[] EncodeMethodBody(IReadOnlyList<ClrInstruction> instructions, ushort maxStack = 8, uint localVarSigTok = 0, IReadOnlyList<ClrExceptionHandler>? exceptionHandlers = null)
    {
        var codeBytes = EncodeInstructions(instructions);
        var codeSize = codeBytes.Length;
        var hasEh = exceptionHandlers is { Count: > 0 };

        if (codeSize < 64 && !hasEh && localVarSigTok == 0 && maxStack <= 8)
        {
            var tinyWriter = new ByteBufferWriter(1 + codeSize);
            tinyWriter.WriteU8((byte)((codeSize << 2) | ClrConstants.MethodHeaderTinyFlag));
            tinyWriter.Write(codeBytes);

            return tinyWriter.ToArray();
        }

        var headerFlags = (ushort)(ClrConstants.MethodHeaderFatFlag | 0x0300);

        if (hasEh)
        {
            headerFlags |= ClrConstants.MethodHeaderMoreSects;
        }

        var ehBytes = hasEh ? EncodeExceptionHandlers(exceptionHandlers!) : [];

        var fatWriter = new ByteBufferWriter(12 + codeSize + ehBytes.Length);
        fatWriter.WriteU16LE(headerFlags);
        fatWriter.WriteU16LE(maxStack);
        fatWriter.WriteU32LE((uint)codeSize);
        fatWriter.WriteU32LE(localVarSigTok);
        fatWriter.Write(codeBytes);

        if (ehBytes.Length > 0)
        {
            var padding = (4 - (codeSize % 4)) % 4;

            for (var i = 0; i < padding; i++)
            {
                fatWriter.WriteU8(0);
            }

            fatWriter.Write(ehBytes);
        }

        return fatWriter.ToArray();
    }

    #region MSIL 指令编码

    private static int EstimateInstructionSize(IReadOnlyList<ClrInstruction> instructions)
    {
        var size = 0;

        foreach (var instr in instructions)
        {
            size += GetOpcodeSize(instr.Opcode) + GetOperandSize(instr.Opcode, instr.Operand);
        }

        return size;
    }

    private static int GetOpcodeSize(ClrOpcode opcode)
    {
        return (ushort)opcode >= 0xFE00 ? 2 : 1;
    }

    private static int GetOperandSize(ClrOpcode opcode, ClrOperand? operand)
    {
        return opcode switch
        {
            ClrOpcode.Ldarg_S or ClrOpcode.Ldarga_S or ClrOpcode.Starg_S or ClrOpcode.Ldloc_S or ClrOpcode.Ldloca_S or ClrOpcode.Stloc_S or ClrOpcode.Ldc_I4_S or ClrOpcode.Unaligned => 1,
            ClrOpcode.Ldarg or ClrOpcode.Ldarga or ClrOpcode.Starg or ClrOpcode.Ldloc or ClrOpcode.Ldloca or ClrOpcode.Stloc => 2,
            ClrOpcode.Ldc_I4 => 4,
            ClrOpcode.Ldc_I8 => 8,
            ClrOpcode.Ldc_R4 => 4,
            ClrOpcode.Ldc_R8 => 8,
            ClrOpcode.Br_S or ClrOpcode.Brfalse_S or ClrOpcode.Brtrue_S or ClrOpcode.Beq_S or ClrOpcode.Bne_Un_S or ClrOpcode.Blt_S or ClrOpcode.Ble_S or ClrOpcode.Bgt_S or ClrOpcode.Bge_S or ClrOpcode.Blt_Un_S or ClrOpcode.Ble_Un_S or ClrOpcode.Bgt_Un_S or ClrOpcode.Bge_Un_S or ClrOpcode.Leave_S => 1,
            ClrOpcode.Br or ClrOpcode.Brfalse or ClrOpcode.Brtrue or ClrOpcode.Beq or ClrOpcode.Bne_Un or ClrOpcode.Blt or ClrOpcode.Ble or ClrOpcode.Bgt or ClrOpcode.Bge or ClrOpcode.Blt_Un or ClrOpcode.Ble_Un or ClrOpcode.Bgt_Un or ClrOpcode.Bge_Un or ClrOpcode.Leave => 4,
            ClrOpcode.Switch => operand is ClrSwitchTargetsOperand sw ? 4 + sw.Offsets.Count * 4 : 4,
            ClrOpcode.Call or ClrOpcode.Callvirt or ClrOpcode.Newobj or ClrOpcode.Ldstr or ClrOpcode.Ldftn or ClrOpcode.Ldvirtftn or ClrOpcode.Castclass or ClrOpcode.Isinst or ClrOpcode.Unbox or ClrOpcode.Unbox_Any or ClrOpcode.Box or ClrOpcode.Newarr or ClrOpcode.Ldelema or ClrOpcode.Initobj or ClrOpcode.Constrained or ClrOpcode.Jmp or ClrOpcode.Calli or ClrOpcode.Ldobj or ClrOpcode.Stobj or ClrOpcode.Ldfld or ClrOpcode.Ldflda or ClrOpcode.Stfld or ClrOpcode.Ldsfld or ClrOpcode.Ldsflda or ClrOpcode.Stsfld or ClrOpcode.Sizeof or ClrOpcode.Ldelem_Any or ClrOpcode.Stelem_Any or ClrOpcode.Cpobj or ClrOpcode.Mkrefany or ClrOpcode.Refanyval or ClrOpcode.Ldtoken => 4,
            _ => 0
        };
    }

    private static void WriteInstruction(ref ByteBufferWriter writer, ClrInstruction instr)
    {
        WriteOpcode(ref writer, instr.Opcode);
        WriteOperand(ref writer, instr.Opcode, instr.Operand);
    }

    private static void WriteOpcode(ref ByteBufferWriter writer, ClrOpcode opcode)
    {
        var value = (ushort)opcode;

        if (value >= 0xFE00)
        {
            writer.WriteU8(0xFE);
            writer.WriteU8((byte)(value & 0xFF));
        }
        else
        {
            writer.WriteU8((byte)value);
        }
    }

    private static void WriteOperand(ref ByteBufferWriter writer, ClrOpcode opcode, ClrOperand? operand)
    {
        switch (opcode)
        {
            case ClrOpcode.Ldarg_S:
            case ClrOpcode.Ldarga_S:
            case ClrOpcode.Starg_S:
                writer.WriteU8((byte)((ClrArgumentIndexOperand?)operand!).Index);
                break;
            case ClrOpcode.Ldloc_S:
            case ClrOpcode.Ldloca_S:
            case ClrOpcode.Stloc_S:
                writer.WriteU8((byte)((ClrLocalIndexOperand?)operand!).Index);
                break;
            case ClrOpcode.Ldarg:
            case ClrOpcode.Ldarga:
            case ClrOpcode.Starg:
                writer.WriteU16LE((ushort)((ClrArgumentIndexOperand?)operand!).Index);
                break;
            case ClrOpcode.Ldloc:
            case ClrOpcode.Ldloca:
            case ClrOpcode.Stloc:
                writer.WriteU16LE((ushort)((ClrLocalIndexOperand?)operand!).Index);
                break;
            case ClrOpcode.Ldc_I4_S:
                writer.WriteI8(((ClrInt8Operand?)operand!).Value);
                break;
            case ClrOpcode.Ldc_I4:
                writer.WriteI32LE(((ClrInt32Operand?)operand!).Value);
                break;
            case ClrOpcode.Ldc_I8:
                writer.WriteI64LE(((ClrInt64Operand?)operand!).Value);
                break;
            case ClrOpcode.Ldc_R4:
                writer.WriteF32LE(((ClrFloat32Operand?)operand!).Value);
                break;
            case ClrOpcode.Ldc_R8:
                writer.WriteF64LE(((ClrFloat64Operand?)operand!).Value);
                break;
            case ClrOpcode.Br_S:
            case ClrOpcode.Brfalse_S:
            case ClrOpcode.Brtrue_S:
            case ClrOpcode.Beq_S:
            case ClrOpcode.Bne_Un_S:
            case ClrOpcode.Blt_S:
            case ClrOpcode.Ble_S:
            case ClrOpcode.Bgt_S:
            case ClrOpcode.Bge_S:
            case ClrOpcode.Blt_Un_S:
            case ClrOpcode.Ble_Un_S:
            case ClrOpcode.Bgt_Un_S:
            case ClrOpcode.Bge_Un_S:
            case ClrOpcode.Leave_S:
            {
                var target = (ClrBranchTarget8Operand?)operand!;
                writer.WriteI8((sbyte)(target.Offset - (int)(writer.Position + 1)));
                break;
            }
            case ClrOpcode.Br:
            case ClrOpcode.Brfalse:
            case ClrOpcode.Brtrue:
            case ClrOpcode.Beq:
            case ClrOpcode.Bne_Un:
            case ClrOpcode.Blt:
            case ClrOpcode.Ble:
            case ClrOpcode.Bgt:
            case ClrOpcode.Bge:
            case ClrOpcode.Blt_Un:
            case ClrOpcode.Ble_Un:
            case ClrOpcode.Bgt_Un:
            case ClrOpcode.Bge_Un:
            case ClrOpcode.Leave:
            {
                var target = (ClrBranchTarget32Operand?)operand!;
                writer.WriteI32LE(target.Offset - (int)(writer.Position + 4));
                break;
            }
            case ClrOpcode.Switch:
            {
                var sw = (ClrSwitchTargetsOperand?)operand!;
                writer.WriteU32LE((uint)sw.Offsets.Count);
                var baseOffset = writer.Position + sw.Offsets.Count * 4;

                foreach (var target in sw.Offsets)
                {
                    writer.WriteI32LE(target - (int)baseOffset);
                }

                break;
            }
            case ClrOpcode.Call:
            case ClrOpcode.Callvirt:
            case ClrOpcode.Newobj:
            case ClrOpcode.Ldstr:
            case ClrOpcode.Ldftn:
            case ClrOpcode.Ldvirtftn:
            case ClrOpcode.Castclass:
            case ClrOpcode.Isinst:
            case ClrOpcode.Unbox:
            case ClrOpcode.Unbox_Any:
            case ClrOpcode.Box:
            case ClrOpcode.Newarr:
            case ClrOpcode.Ldelema:
            case ClrOpcode.Initobj:
            case ClrOpcode.Constrained:
            case ClrOpcode.Jmp:
            case ClrOpcode.Calli:
            case ClrOpcode.Ldobj:
            case ClrOpcode.Stobj:
            case ClrOpcode.Ldfld:
            case ClrOpcode.Ldflda:
            case ClrOpcode.Stfld:
            case ClrOpcode.Ldsfld:
            case ClrOpcode.Ldsflda:
            case ClrOpcode.Stsfld:
            case ClrOpcode.Sizeof:
            case ClrOpcode.Ldelem_Any:
            case ClrOpcode.Stelem_Any:
            case ClrOpcode.Cpobj:
            case ClrOpcode.Mkrefany:
            case ClrOpcode.Refanyval:
            case ClrOpcode.Ldtoken:
                writer.WriteU32LE(((ClrTokenOperand?)operand!).Value);
                break;
            case ClrOpcode.Unaligned:
                writer.WriteU8((byte)((ClrInt8Operand?)operand!).Value);
                break;
        }
    }

    #endregion

    #region 异常处理表编码

    /// <summary>
    ///     编码异常处理表（Fat 格式）。
    /// </summary>
    private static byte[] EncodeExceptionHandlers(IReadOnlyList<ClrExceptionHandler> handlers)
    {
        var dataSize = 4 + handlers.Count * 24;
        var writer = new ByteBufferWriter(dataSize + 4);

        writer.WriteU8(ClrConstants.ExceptionHandlerTableFlag | ClrConstants.ExceptionHandlerFatFlag);
        writer.WriteU8((byte)(dataSize >> 16));
        writer.WriteU8((byte)(dataSize >> 8));
        writer.WriteU8((byte)dataSize);

        foreach (var eh in handlers)
        {
            writer.WriteU32LE((uint)eh.HandlerKind);
            writer.WriteU32LE(eh.TryStart);
            writer.WriteU32LE(eh.TryLength);
            writer.WriteU32LE(eh.HandlerStart);
            writer.WriteU32LE(eh.HandlerLength);
            writer.WriteU32LE(eh.ClassTokenOrFilterOffset);
        }

        return writer.ToArray();
    }

    #endregion

    #region PE 构建器

    /// <summary>
    ///     PE 文件构建器，组装完整的 .NET 程序集。
    /// </summary>
    private sealed class PeBuilder
    {
        private readonly ClrModuleData _module;

        public PeBuilder(ClrModuleData module)
        {
            _module = module;
        }

        public byte[] Build()
        {
            var metadataBytes = BuildMetadata();
            var ilSectionBytes = BuildILSection();

            var textSectionData = new ByteBufferWriter(0x2000);
            var clrDirectoryOffset = 0x80;
            var metadataOffset = clrDirectoryOffset + ClrConstants.ClrDirectorySize;

            textSectionData.Write(ilSectionBytes);

            while (textSectionData.Position < metadataOffset)
            {
                textSectionData.WriteU8(0);
            }

            textSectionData.Write(metadataBytes);

            var textSectionSize = (uint)((textSectionData.Position + 0x1FF) & ~0x1FF);
            var peHeaderSize = 0x200;
            var textSectionRva = (uint)peHeaderSize;

            var writer = new ByteBufferWriter(peHeaderSize + (int)textSectionSize);

            WriteDosHeader(ref writer);
            WritePEHeader(ref writer, textSectionRva, textSectionSize);
            WriteOptionalHeader(ref writer, textSectionRva, textSectionSize, (uint)(clrDirectoryOffset + textSectionRva), (uint)metadataBytes.Length);
            WriteSectionHeader(ref writer, textSectionRva, textSectionSize);

            while (writer.Position < peHeaderSize)
            {
                writer.WriteU8(0);
            }

            writer.Write(textSectionData.ToArray());

            return writer.ToArray();
        }

        private byte[] BuildMetadata()
        {
            var headerSize = 16;
            var versionString = Encoding.UTF8.GetBytes(_module.Version ?? "v4.0.30319");
            var versionLength = (4 + versionString.Length + 3) & ~3;

            var stringHeap = BuildStringHeap();
            var blobHeap = BuildBlobHeap();
            var guidHeap = BuildGuidHeap();
            var userStringHeap = BuildUserStringHeap();
            var tableStream = BuildTableStream();

            var streamCount = 5;
            var streamHeaderSize = 0;

            var streamOffsets = new int[streamCount];
            var currentOffset = headerSize + versionLength + 4;

            var streamNames = new[] { ClrConstants.TableStreamName, ClrConstants.StringsStreamName, ClrConstants.BlobStreamName, ClrConstants.GuidStreamName, ClrConstants.UserStringStreamName };
            var streamData = new[] { tableStream, stringHeap, blobHeap, guidHeap, userStringHeap };

            for (var i = 0; i < streamCount; i++)
            {
                streamOffsets[i] = currentOffset;
                var nameBytes = Encoding.UTF8.GetBytes(streamNames[i] + "\0");
                var namePadded = (nameBytes.Length + 3) & ~3;
                streamHeaderSize += 8 + namePadded;
                currentOffset += streamData[i].Length;
            }

            var totalSize = headerSize + versionLength + 4 + streamHeaderSize;

            foreach (var sd in streamData)
            {
                totalSize += sd.Length;
            }

            var writer = new ByteBufferWriter(totalSize);

            writer.WriteU32LE(ClrConstants.MetadataSignature);
            writer.WriteU16LE(1);
            writer.WriteU16LE(1);
            writer.WriteU32LE(0);
            writer.WriteU32LE((uint)versionLength);
            writer.Write(versionString);

            var versionPad = versionLength - versionString.Length;

            for (var i = 0; i < versionPad; i++)
            {
                writer.WriteU8(0);
            }

            writer.WriteU16LE(0);
            writer.WriteU16LE((ushort)streamCount);

            for (var i = 0; i < streamCount; i++)
            {
                writer.WriteU32LE((uint)streamOffsets[i]);
                writer.WriteU32LE((uint)streamData[i].Length);
                var nameBytes = Encoding.UTF8.GetBytes(streamNames[i] + "\0");
                writer.Write(nameBytes);
                var namePad = ((nameBytes.Length + 3) & ~3) - nameBytes.Length;

                for (var p = 0; p < namePad; p++)
                {
                    writer.WriteU8(0);
                }
            }

            foreach (var sd in streamData)
            {
                writer.Write(sd);
            }

            return writer.ToArray();
        }

        private byte[] BuildStringHeap()
        {
            var strings = new HashSet<string> { _module.ModuleName ?? string.Empty, "<Module>" };

            foreach (var type in _module.Types)
            {
                strings.Add(type.Name);
                strings.Add(type.Namespace);
            }

            foreach (var method in _module.Methods)
            {
                strings.Add(method.Name);
            }

            foreach (var field in _module.Fields)
            {
                strings.Add(field.Name);
            }

            strings.Remove(string.Empty);

            var writer = new ByteBufferWriter(1 + strings.Sum(s => Encoding.UTF8.GetByteCount(s) + 1));
            writer.WriteU8(0);

            foreach (var s in strings)
            {
                writer.Write(Encoding.UTF8.GetBytes(s));
                writer.WriteU8(0);
            }

            return writer.ToArray();
        }

        private byte[] BuildBlobHeap()
        {
            return [0];
        }

        private byte[] BuildGuidHeap()
        {
            var guid = _module.Metadata.GuidHeap.Data;

            if (guid.Length > 0)
            {
                return guid;
            }

            return new byte[16];
        }

        private byte[] BuildUserStringHeap()
        {
            return [0];
        }

        private byte[] BuildTableStream()
        {
            var writer = new ByteBufferWriter(256);
            writer.WriteU32LE(0);
            writer.WriteU8(2);
            writer.WriteU8(0);
            writer.WriteU8(0);
            writer.WriteU8(0);
            writer.WriteU64LE(0);
            writer.WriteU64LE(0);

            return writer.ToArray();
        }

        private byte[] BuildILSection()
        {
            var writer = new ByteBufferWriter(0x80);

            foreach (var method in _module.Methods)
            {
                if (method.Instructions.Count == 0)
                {
                    continue;
                }

                var bodyBytes = EncodeMethodBody(method.Instructions, method.MaxStack, method.LocalVarSigTok, method.ExceptionHandlers);
                writer.Write(bodyBytes);

                var pad = (4 - (bodyBytes.Length % 4)) % 4;

                for (var i = 0; i < pad; i++)
                {
                    writer.WriteU8(0);
                }
            }

            return writer.ToArray();
        }

        private static void WriteDosHeader(ref ByteBufferWriter writer)
        {
            writer.WriteU16LE(0x5A4D);
            writer.WriteU16LE(0x0090);
            writer.WriteU16LE(0x0003);
            writer.WriteU16LE(0x0000);
            writer.WriteU16LE(0x0004);
            writer.WriteU16LE(0x0000);
            writer.WriteU16LE(0xFFFF);
            writer.WriteU16LE(0x0000);
            writer.WriteU16LE(0x00B8);
            writer.WriteU16LE(0x0000);
            writer.WriteU16LE(0x0000);
            writer.WriteU16LE(0x0000);
            writer.WriteU32LE(0x00000000);
            writer.WriteU32LE(0x00000000);

            for (var i = 0; i < 10; i++)
            {
                writer.WriteU16LE(0x0000);
            }

            writer.WriteU32LE(0x00000080);
        }

        private static void WritePEHeader(ref ByteBufferWriter writer, uint textSectionRva, uint textSectionSize)
        {
            writer.WriteU32LE(0x00004550);
            writer.WriteU16LE(0x014C);
            writer.WriteU16LE(1);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU16LE(0x00E0);
            writer.WriteU16LE(0x0102);
        }

        private static void WriteOptionalHeader(ref ByteBufferWriter writer, uint textSectionRva, uint textSectionSize, uint clrRva, uint clrSize)
        {
            writer.WriteU16LE(0x010B);
            writer.WriteU8(8);
            writer.WriteU8(0);
            writer.WriteU32LE(textSectionSize);
            writer.WriteU32LE(0x2000);
            writer.WriteU32LE(0);
            writer.WriteU32LE(textSectionRva + 0x2000);
            writer.WriteU32LE(textSectionRva);
            writer.WriteU32LE(textSectionRva + 0x1000);
            writer.WriteU32LE(0x00400000);
            writer.WriteU32LE(0x2000);
            writer.WriteU32LE(0x200);
            writer.WriteU16LE(4);
            writer.WriteU16LE(0);
            writer.WriteU16LE(0);
            writer.WriteU16LE(0);
            writer.WriteU16LE(4);
            writer.WriteU16LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(textSectionRva + textSectionSize);
            writer.WriteU32LE(0x200);
            writer.WriteU32LE(0);
            writer.WriteU16LE(3);
            writer.WriteU16LE(0x8540);
            writer.WriteU32LE(0x00100000);
            writer.WriteU32LE(0x1000);
            writer.WriteU32LE(0x00100000);
            writer.WriteU32LE(0x1000);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0x10);
            writer.WriteU32LE(16);

            for (var i = 0; i < 14; i++)
            {
                writer.WriteU32LE(0);
                writer.WriteU32LE(0);
            }

            writer.WriteU32LE(clrRva);
            writer.WriteU32LE(clrSize);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
        }

        private static void WriteSectionHeader(ref ByteBufferWriter writer, uint rva, uint rawSize)
        {
            var name = Encoding.UTF8.GetBytes(".text\0\0\0");
            writer.Write(name);
            writer.WriteU32LE(rawSize);
            writer.WriteU32LE(rva);
            writer.WriteU32LE(rawSize);
            writer.WriteU32LE(0x200);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU16LE(0);
            writer.WriteU16LE(0);
            writer.WriteU32LE(0x60000020);
        }
    }

    #endregion
}

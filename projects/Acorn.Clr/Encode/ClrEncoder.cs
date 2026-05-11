using Acorn.Clr.Data;
using Acorn.Frame;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
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
        return ManagedPeBuilderAdapter.Build(module);
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

        // Fat 方法头的 Size 位位于高 4 bit（以 DWORD 计），3 表示 12 字节头。
        var headerFlags = (ushort)(ClrConstants.MethodHeaderFatFlag | 0x3000);

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
        return (ushort)opcode >= ClrConstants.TwoByteOpcodeBase ? 2 : 1;
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

        if (value >= ClrConstants.TwoByteOpcodeBase)
        {
            writer.WriteU8(ClrConstants.TwoByteOpcodePrefix);
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

    #region ManagedPE 生成

    /// <summary>
    ///     基于 BCL 的 ManagedPEBuilder 生成可执行 CLR 程序集，避免手写 PE 细节差异。
    /// </summary>
    private static class ManagedPeBuilderAdapter
    {
        public static byte[] Build(ClrModuleData module)
        {
            var metadata = new MetadataBuilder();
            var ilBuilder = new BlobBuilder();
            var methodBodyEncoder = new MethodBodyStreamEncoder(ilBuilder);

            var moduleFileName = string.IsNullOrWhiteSpace(module.ModuleName) ? "Module.exe" : module.ModuleName;
            var assemblyName = Path.GetFileNameWithoutExtension(moduleFileName);
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = "Module";
            }

            var mvid = ResolveMvid(module);
            metadata.AddModule(
                0,
                metadata.GetOrAddString(moduleFileName),
                metadata.GetOrAddGuid(mvid),
                default,
                default);

            metadata.AddAssembly(
                name: metadata.GetOrAddString(assemblyName),
                version: ParseAssemblyVersion(module.Version),
                culture: default,
                publicKey: default,
                flags: default,
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var externalMemberRefTokenMap = EmitExternalRefs(metadata, module.ExternalMethodRefs);

            var methods = module.Types.SelectMany(t => t.Methods).ToList();
            var firstParameterHandle = MetadataTokens.ParameterHandle(1);
            foreach (var method in methods)
            {
                var methodBody = EncodeInstructions(method.Instructions);
                var codeBuilder = new BlobBuilder();
                codeBuilder.WriteBytes(methodBody);
                var instructionEncoder = new InstructionEncoder(codeBuilder);
                var bodyOffset = methodBodyEncoder.AddMethodBody(
                    instructionEncoder,
                    method.MaxStack,
                    default,
                    default);

                var signature = method.Signature.Length == 0 ? [0x00, 0x00, 0x01] : method.Signature;
                metadata.AddMethodDefinition(
                    attributes: (MethodAttributes)method.Flags,
                    implAttributes: MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    name: metadata.GetOrAddString(method.Name),
                    signature: metadata.GetOrAddBlob(signature),
                    bodyOffset: bodyOffset,
                    parameterList: firstParameterHandle);
            }

            var firstFieldHandle = MetadataTokens.FieldDefinitionHandle(1);
            var methodStart = 1;
            foreach (var type in module.Types)
            {
                metadata.AddTypeDefinition(
                    attributes: (TypeAttributes)type.Flags,
                    @namespace: metadata.GetOrAddString(type.Namespace ?? string.Empty),
                    name: metadata.GetOrAddString(type.Name),
                    baseType: default,
                    fieldList: firstFieldHandle,
                    methodList: MetadataTokens.MethodDefinitionHandle(methodStart));

                methodStart += type.Methods.Count;
            }

            var entryPointHandle = ResolveEntryPointHandle(module, methods, externalMemberRefTokenMap);

            var peBuilder = new ManagedPEBuilder(
                new PEHeaderBuilder(
                    imageCharacteristics: entryPointHandle.IsNil ? Characteristics.Dll : Characteristics.ExecutableImage,
                    subsystem: Subsystem.WindowsCui,
                    dllCharacteristics: DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware),
                new MetadataRootBuilder(metadata),
                ilBuilder,
                mappedFieldData: null,
                managedResources: null,
                strongNameSignatureSize: 0,
                entryPoint: entryPointHandle,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: null);

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            return peBlob.ToArray();
        }

        private static Guid ResolveMvid(ClrModuleData module)
        {
            var raw = module.Metadata.GuidHeap.Data;
            if (raw.Length >= 16)
            {
                return new Guid(raw.AsSpan(0, 16));
            }

            return Guid.NewGuid();
        }

        private static Version ParseAssemblyVersion(string? version)
        {
            return Version.TryParse(version, out var parsed)
                ? new Version(
                    Math.Max(0, parsed.Major),
                    Math.Max(0, parsed.Minor),
                    Math.Max(0, parsed.Build),
                    Math.Max(0, parsed.Revision))
                : new Version(1, 0, 0, 0);
        }

        /// <summary>
        ///     为所有外部方法引用生成 AssemblyRef → TypeRef → MemberRef 元数据条目。
        ///     返回 MemberRef token 值到 MemberReferenceHandle 的映射，用于入口点解析。
        /// </summary>
        private static Dictionary<uint, MemberReferenceHandle> EmitExternalRefs(
            MetadataBuilder metadata,
            IReadOnlyList<ClrExternalMethodRef> externalRefs)
        {
            var memberRefTokenMap = new Dictionary<uint, MemberReferenceHandle>();
            var assemblyRefHandles = new Dictionary<string, AssemblyReferenceHandle>(StringComparer.Ordinal);
            var typeRefHandles = new Dictionary<(string, string, string), TypeReferenceHandle>();

            foreach (var extRef in externalRefs)
            {
                if (!assemblyRefHandles.TryGetValue(extRef.AssemblyName, out var assemblyRefHandle))
                {
                    assemblyRefHandle = metadata.AddAssemblyReference(
                        name: metadata.GetOrAddString(extRef.AssemblyName),
                        version: new Version(0, 0, 0, 0),
                        culture: default,
                        publicKeyOrToken: default,
                        flags: default,
                        hashValue: default);
                    assemblyRefHandles[extRef.AssemblyName] = assemblyRefHandle;
                }

                var typeKey = (extRef.AssemblyName, extRef.TypeNamespace, extRef.TypeName);
                if (!typeRefHandles.TryGetValue(typeKey, out var typeRefHandle))
                {
                    typeRefHandle = metadata.AddTypeReference(
                        resolutionScope: assemblyRefHandle,
                        @namespace: metadata.GetOrAddString(extRef.TypeNamespace),
                        name: metadata.GetOrAddString(extRef.TypeName));
                    typeRefHandles[typeKey] = typeRefHandle;
                }

                var signature = extRef.MethodSignature.Length == 0 ? (new byte[] { 0x00, 0x00, 0x01 }) : extRef.MethodSignature;
                var memberRefHandle = metadata.AddMemberReference(
                    parent: typeRefHandle,
                    name: metadata.GetOrAddString(extRef.MethodName),
                    signature: metadata.GetOrAddBlob(signature));

                var token = (uint)MetadataTokens.GetToken(memberRefHandle);
                memberRefTokenMap[token] = memberRefHandle;
            }

            return memberRefTokenMap;
        }

        /// <summary>
        ///     从 EntryPoint 令牌解析入口点 MethodDefinitionHandle（或 MemberReferenceHandle）。
        ///     令牌高字节标识表类型：0x06 = MethodDef，0x0A = MemberRef。
        /// </summary>
        private static MethodDefinitionHandle ResolveEntryPointHandle(
            ClrModuleData module,
            List<ClrMethodDef> methods,
            IReadOnlyDictionary<uint, MemberReferenceHandle> memberRefTokenMap)
        {
            var entryPoint = module.ClrDirectory.EntryPoint;
            if (entryPoint == 0)
            {
                return default;
            }

            var tableType = entryPoint >> 24;
            var rowNumber = (int)(entryPoint & 0x00FFFFFF);

            if (tableType == 0x06 && rowNumber > 0 && rowNumber <= methods.Count)
            {
                return MetadataTokens.MethodDefinitionHandle(rowNumber);
            }

            return default;
        }
    }

    #endregion

    #region PE 构建器

    /// <summary>
    ///     PE 文件构建器，组装完整的 .NET 程序集。
    /// </summary>
    private sealed class PeBuilder
    {
        private readonly ClrModuleData _module;
        private Dictionary<string, uint> _stringIndices = [];
        private List<uint> _methodRvas = [];
        private readonly List<byte[]> _methodSignatures = [];
        private readonly Dictionary<byte[], uint> _signatureBlobIndexMap = new(ByteArrayComparer.Instance);

        public PeBuilder(ClrModuleData module)
        {
            _module = module;
        }

        public byte[] Build()
        {
            var stringHeap = BuildStringHeap();
            var blobHeap = BuildBlobHeap();
            var guidHeap = BuildGuidHeap();
            var userStringHeap = BuildUserStringHeap();

            // `.text` 的虚拟地址必须与 SectionAlignment 对齐。
            // OptionalHeader 中 SectionAlignment 固定为 0x2000，因此 RVA 不能使用 0x200。
            var textSectionRva = 0x2000;
            var ilSectionBytes = BuildILSection(textSectionRva);

            var tableStream = BuildTableStream();
            var metadataBytes = BuildMetadata(stringHeap, blobHeap, guidHeap, userStringHeap, tableStream);

            var textSectionData = new ByteBufferWriter(0x2000);

            textSectionData.Write(ilSectionBytes);
            var clrDirectoryOffset = AlignUp((uint)textSectionData.Position, 4u);
            while ((uint)textSectionData.Position < clrDirectoryOffset)
            {
                textSectionData.WriteU8(0);
            }

            var metadataOffset = AlignUp(clrDirectoryOffset + ClrConstants.ClrDirectorySize, 4u);

            var directoryFlags = _module.ClrDirectory.Flags != 0
                ? _module.ClrDirectory.Flags
                : (uint)ClrDirectoryFlags.ILOnly;
            var entryPoint = _module.ClrDirectory.EntryPoint;
            WriteClrDirectory(
                ref textSectionData,
                (uint)(metadataOffset + textSectionRva),
                (uint)metadataBytes.Length,
                directoryFlags,
                entryPoint);

            textSectionData.Write(metadataBytes);

            var textSectionSize = (uint)((textSectionData.Position + 0x1FF) & ~0x1FF);
            var peHeaderSize = 0x200;

            var writer = new ByteBufferWriter(peHeaderSize + (int)textSectionSize);

            WriteDosHeader(ref writer);
            WritePEHeader(ref writer, (uint)textSectionRva, textSectionSize);
            WriteOptionalHeader(ref writer, (uint)textSectionRva, textSectionSize, (uint)(clrDirectoryOffset + textSectionRva), ClrConstants.ClrDirectorySize);
            WriteSectionHeader(ref writer, (uint)textSectionRva, textSectionSize);

            while (writer.Position < peHeaderSize)
            {
                writer.WriteU8(0);
            }

            writer.Write(textSectionData.ToArray());

            while (writer.Position < peHeaderSize + (int)textSectionSize)
            {
                writer.WriteU8(0);
            }

            return writer.ToArray();
        }

        private static uint AlignUp(uint value, uint alignment)
        {
            if (alignment == 0)
            {
                return value;
            }

            var remainder = value % alignment;
            return remainder == 0 ? value : value + (alignment - remainder);
        }

        private byte[] BuildMetadata(byte[] stringHeap, byte[] blobHeap, byte[] guidHeap, byte[] userStringHeap, byte[] tableStream)
        {
            var headerSize = 16;
            var versionString = Encoding.UTF8.GetBytes(_module.Version ?? "v4.0.30319");
            var versionLength = (4 + versionString.Length + 3) & ~3;

            var streamCount = 5;
            var streamHeaderSize = 0;

            var streamNames = new[] { ClrConstants.TableStreamName, ClrConstants.StringsStreamName, ClrConstants.BlobStreamName, ClrConstants.GuidStreamName, ClrConstants.UserStringStreamName };
            var streamData = new[] { tableStream, stringHeap, blobHeap, guidHeap, userStringHeap };

            for (var i = 0; i < streamCount; i++)
            {
                var nameBytes = Encoding.UTF8.GetBytes(streamNames[i] + "\0");
                var namePadded = (nameBytes.Length + 3) & ~3;
                streamHeaderSize += 8 + namePadded;
            }

            var streamOffsets = new int[streamCount];
            var currentOffset = headerSize + versionLength + 4 + streamHeaderSize;

            for (var i = 0; i < streamCount; i++)
            {
                streamOffsets[i] = currentOffset;
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
            var strings = new List<string>();
            _stringIndices = new Dictionary<string, uint>();
            _stringIndices[string.Empty] = 0;

            void AddString(string s)
            {
                if (string.IsNullOrEmpty(s) || _stringIndices.ContainsKey(s))
                {
                    return;
                }

                _stringIndices[s] = 0;
                strings.Add(s);
            }

            AddString(_module.ModuleName);
            AddString(GetAssemblySimpleName());

            foreach (var type in _module.Types)
            {
                AddString(type.Name);
                AddString(type.Namespace);

                foreach (var field in type.Fields)
                {
                    AddString(field.Name);
                }

                foreach (var method in type.Methods)
                {
                    AddString(method.Name);
                }
            }

            foreach (var method in _module.Methods)
            {
                AddString(method.Name);
            }

            foreach (var field in _module.Fields)
            {
                AddString(field.Name);
            }

            var writer = new ByteBufferWriter(1 + strings.Sum(s => Encoding.UTF8.GetByteCount(s) + 1));
            writer.WriteU8(0);

            foreach (var s in strings)
            {
                _stringIndices[s] = (uint)writer.Position;
                writer.Write(Encoding.UTF8.GetBytes(s));
                writer.WriteU8(0);
            }

            return writer.ToArray();
        }

        private uint GetStringIndex(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }

            return _stringIndices.TryGetValue(s, out var idx) ? idx : 0;
        }

        private byte[] BuildBlobHeap()
        {
            _methodSignatures.Clear();
            _methodSignatures.AddRange(_module.Methods.Select(m => m.Signature.Length == 0 ? [0x00, 0x00, 0x01] : m.Signature));

            foreach (var type in _module.Types)
            {
                _methodSignatures.AddRange(type.Methods.Select(m => m.Signature.Length == 0 ? [0x00, 0x00, 0x01] : m.Signature));
            }

            var writer = new ByteBufferWriter(256);
            writer.WriteU8(0);
            _signatureBlobIndexMap.Clear();

            foreach (var signature in _methodSignatures)
            {
                if (_signatureBlobIndexMap.ContainsKey(signature))
                {
                    continue;
                }

                var index = (uint)writer.Position;
                WriteCompressedUnsigned(ref writer, (uint)signature.Length);
                writer.Write(signature);
                _signatureBlobIndexMap[signature] = index;
            }

            return writer.ToArray();
        }

        private byte[] BuildGuidHeap()
        {
            var guid = _module.Metadata.GuidHeap.Data;

            if (guid.Length > 0)
            {
                return guid;
            }

            return Guid.NewGuid().ToByteArray();
        }

        private byte[] BuildUserStringHeap()
        {
            return [0];
        }

        private byte[] BuildTableStream()
        {
            var allMethods = new List<ClrMethodDef>();
            allMethods.AddRange(_module.Methods);

            foreach (var type in _module.Types)
            {
                allMethods.AddRange(type.Methods);
            }

            var allFields = new List<ClrFieldDef>();
            allFields.AddRange(_module.Types.SelectMany(t => t.Fields));

            var moduleRowCount = 1u;
            var typeDefRowCount = (uint)_module.Types.Count;
            var methodDefRowCount = (uint)allMethods.Count;
            var fieldRowCount = (uint)allFields.Count;
            var paramRowCount = 0u;
            var assemblyRowCount = 1u;

            var validTables = 0UL;
            validTables |= 1UL << (int)ClrTableKind.Module;
            validTables |= 1UL << (int)ClrTableKind.TypeDef;
            validTables |= 1UL << (int)ClrTableKind.Assembly;

            if (fieldRowCount > 0)
            {
                validTables |= 1UL << (int)ClrTableKind.Field;
            }

            if (methodDefRowCount > 0)
            {
                validTables |= 1UL << (int)ClrTableKind.MethodDef;
                validTables |= 1UL << (int)ClrTableKind.Param;
            }

            var heapSizes = (byte)0;
            var stringIndexSize = 2;
            var guidIndexSize = 2;
            var blobIndexSize = 2;

            var rowCounts = new List<uint>();

            if ((validTables & (1UL << (int)ClrTableKind.Module)) != 0)
            {
                rowCounts.Add(moduleRowCount);
            }

            if ((validTables & (1UL << (int)ClrTableKind.TypeDef)) != 0)
            {
                rowCounts.Add(typeDefRowCount);
            }

            if ((validTables & (1UL << (int)ClrTableKind.Field)) != 0)
            {
                rowCounts.Add(fieldRowCount);
            }

            if ((validTables & (1UL << (int)ClrTableKind.MethodDef)) != 0)
            {
                rowCounts.Add(methodDefRowCount);
            }

            if ((validTables & (1UL << (int)ClrTableKind.Param)) != 0)
            {
                rowCounts.Add(paramRowCount);
            }

            if ((validTables & (1UL << (int)ClrTableKind.Assembly)) != 0)
            {
                rowCounts.Add(assemblyRowCount);
            }

            var typeDefOrRefIndexSize = 2;
            var fieldTableIndexSize = fieldRowCount <= 0xFFFF ? 2 : 4;
            var methodDefTableIndexSize = methodDefRowCount <= 0xFFFF ? 2 : 4;
            var paramTableIndexSize = 2;

            var moduleRowSize = 2 + stringIndexSize + guidIndexSize * 3;
            var typeDefRowSize = 4 + stringIndexSize * 2 + typeDefOrRefIndexSize + fieldTableIndexSize + methodDefTableIndexSize;
            var fieldRowSize = 2 + stringIndexSize + blobIndexSize;
            var methodDefRowSize = 4 + 2 + 2 + stringIndexSize + blobIndexSize + paramTableIndexSize;
            var assemblyRowSize = 4 + 2 + 2 + 2 + 2 + 4 + blobIndexSize + stringIndexSize + stringIndexSize;

            var totalRowSize = moduleRowCount * (uint)moduleRowSize
                               + typeDefRowCount * (uint)typeDefRowSize
                               + fieldRowCount * (uint)fieldRowSize
                               + methodDefRowCount * (uint)methodDefRowSize
                               + assemblyRowCount * (uint)assemblyRowSize;

            var headerSize = 24 + rowCounts.Count * 4;
            var writer = new ByteBufferWriter(headerSize + (int)totalRowSize);

            writer.WriteU32LE(0);
            writer.WriteU8(2);
            writer.WriteU8(0);
            writer.WriteU8(heapSizes);
            writer.WriteU8(0);
            writer.WriteU64LE(validTables);
            // Sorted bitmask 仅在确实满足对应表排序约束时设置；这里保守写 0，避免声明错误排序。
            writer.WriteU64LE(0);

            foreach (var rc in rowCounts)
            {
                writer.WriteU32LE(rc);
            }

            writer.WriteU16LE(0);
            WriteIndex(ref writer, GetStringIndex(_module.ModuleName), stringIndexSize);
            WriteIndex(ref writer, 1, guidIndexSize);
            WriteIndex(ref writer, 0, guidIndexSize);
            WriteIndex(ref writer, 0, guidIndexSize);

            var methodIndex = 1 + _module.Methods.Count;
            var fieldIndex = 1;

            foreach (var type in _module.Types)
            {
                writer.WriteU32LE((uint)type.Flags);
                WriteIndex(ref writer, GetStringIndex(type.Name), stringIndexSize);
                WriteIndex(ref writer, GetStringIndex(type.Namespace), stringIndexSize);
                WriteIndex(ref writer, 0, typeDefOrRefIndexSize);
                WriteIndex(ref writer, (uint)fieldIndex, fieldTableIndexSize);
                WriteIndex(ref writer, (uint)methodIndex, methodDefTableIndexSize);

                fieldIndex += type.Fields.Count;
                methodIndex += type.Methods.Count;
            }

            foreach (var type in _module.Types)
            {
                foreach (var field in type.Fields)
                {
                    writer.WriteU16LE((ushort)field.Flags);
                    WriteIndex(ref writer, GetStringIndex(field.Name), stringIndexSize);
                    WriteIndex(ref writer, 0, blobIndexSize);
                }
            }

            var methodIdx = 0;

            foreach (var method in _module.Methods)
            {
                var rva = methodIdx < _methodRvas.Count ? _methodRvas[methodIdx] : 0u;
                methodIdx++;
                writer.WriteU32LE(rva);
                writer.WriteU16LE(0);
                writer.WriteU16LE((ushort)method.Flags);
                WriteIndex(ref writer, GetStringIndex(method.Name), stringIndexSize);
                WriteIndex(ref writer, GetMethodSignatureIndex(method), blobIndexSize);
                WriteIndex(ref writer, 1, paramTableIndexSize);
            }

            foreach (var type in _module.Types)
            {
                foreach (var method in type.Methods)
                {
                    var rva = methodIdx < _methodRvas.Count ? _methodRvas[methodIdx] : 0u;
                    methodIdx++;
                    writer.WriteU32LE(rva);
                    writer.WriteU16LE(0);
                    writer.WriteU16LE((ushort)method.Flags);
                    WriteIndex(ref writer, GetStringIndex(method.Name), stringIndexSize);
                    WriteIndex(ref writer, GetMethodSignatureIndex(method), blobIndexSize);
                    WriteIndex(ref writer, 1, paramTableIndexSize);
                }
            }

            var (majorVersion, minorVersion, buildNumber, revisionNumber) = ParseVersion(_module.Version);
            writer.WriteU32LE(0x00008004);
            writer.WriteU16LE(majorVersion);
            writer.WriteU16LE(minorVersion);
            writer.WriteU16LE(buildNumber);
            writer.WriteU16LE(revisionNumber);
            writer.WriteU32LE(0);
            WriteIndex(ref writer, 0, blobIndexSize);
            WriteIndex(ref writer, GetStringIndex(GetAssemblySimpleName()), stringIndexSize);
            WriteIndex(ref writer, 0, stringIndexSize);

            return writer.ToArray();
        }

        private string GetAssemblySimpleName()
        {
            if (string.IsNullOrWhiteSpace(_module.ModuleName))
            {
                return "Module";
            }

            return Path.GetFileNameWithoutExtension(_module.ModuleName);
        }

        private static (ushort Major, ushort Minor, ushort Build, ushort Revision) ParseVersion(string? version)
        {
            if (Version.TryParse(version, out var parsed))
            {
                return (
                    (ushort)Math.Clamp(parsed.Major, 0, ushort.MaxValue),
                    (ushort)Math.Clamp(parsed.Minor, 0, ushort.MaxValue),
                    (ushort)Math.Clamp(parsed.Build < 0 ? 0 : parsed.Build, 0, ushort.MaxValue),
                    (ushort)Math.Clamp(parsed.Revision < 0 ? 0 : parsed.Revision, 0, ushort.MaxValue)
                );
            }

            return (1, 0, 0, 0);
        }

        private uint GetMethodSignatureIndex(ClrMethodDef method)
        {
            var signature = method.Signature.Length == 0 ? [0x00, 0x00, 0x01] : method.Signature;
            if (_signatureBlobIndexMap.TryGetValue(signature, out var index))
            {
                return index;
            }

            return 0;
        }

        private static void WriteCompressedUnsigned(ref ByteBufferWriter writer, uint value)
        {
            if (value <= 0x7F)
            {
                writer.WriteU8((byte)value);
                return;
            }

            if (value <= 0x3FFF)
            {
                writer.WriteU8((byte)((value >> 8) | 0x80));
                writer.WriteU8((byte)(value & 0xFF));
                return;
            }

            writer.WriteU8((byte)((value >> 24) | 0xC0));
            writer.WriteU8((byte)((value >> 16) & 0xFF));
            writer.WriteU8((byte)((value >> 8) & 0xFF));
            writer.WriteU8((byte)(value & 0xFF));
        }

        private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public static readonly ByteArrayComparer Instance = new();

            public bool Equals(byte[]? x, byte[]? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null || x.Length != y.Length)
                {
                    return false;
                }

                return x.AsSpan().SequenceEqual(y);
            }

            public int GetHashCode(byte[] obj)
            {
                var hash = new HashCode();
                foreach (var b in obj)
                {
                    hash.Add(b);
                }

                return hash.ToHashCode();
            }
        }

        private static void WriteIndex(ref ByteBufferWriter writer, uint value, int size)
        {
            if (size == 2)
            {
                writer.WriteU16LE((ushort)value);
            }
            else
            {
                writer.WriteU32LE(value);
            }
        }

        private byte[] BuildILSection(int textSectionRva)
        {
            var writer = new ByteBufferWriter(0x80);
            _methodRvas = [];

            var allMethods = new List<ClrMethodDef>();
            allMethods.AddRange(_module.Methods);

            foreach (var type in _module.Types)
            {
                allMethods.AddRange(type.Methods);
            }

            foreach (var method in allMethods)
            {
                if (method.Instructions.Count == 0)
                {
                    _methodRvas.Add(0);
                    continue;
                }

                var rva = (uint)(writer.Position + textSectionRva);
                _methodRvas.Add(rva);

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

            for (var i = 0; i < 14; i++)
            {
                writer.WriteU16LE(0x0000);
            }

            writer.WriteU32LE(0x00000040);
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
            writer.WriteU16LE(0x0022);
        }

        private static void WriteOptionalHeader(ref ByteBufferWriter writer, uint textSectionRva, uint textSectionSize, uint clrRva, uint clrSize)
        {
            writer.WriteU16LE(0x010B);
            writer.WriteU8(8);
            writer.WriteU8(0);
            writer.WriteU32LE(textSectionSize);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            // ILOnly 程序集由 CLR Header 的 EntryPoint token 决定入口，不写原生 RVA 入口。
            writer.WriteU32LE(0);
            writer.WriteU32LE(textSectionRva);
            writer.WriteU32LE(textSectionRva);
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

        private static void WriteClrDirectory(
            ref ByteBufferWriter writer,
            uint metadataRva,
            uint metadataSize,
            uint flags,
            uint entryPoint)
        {
            writer.WriteU32LE(ClrConstants.ClrDirectorySize);
            writer.WriteU16LE(2);
            writer.WriteU16LE(5);
            writer.WriteU32LE(metadataRva);
            writer.WriteU32LE(metadataSize);
            writer.WriteU32LE(flags);
            writer.WriteU32LE(entryPoint);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
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

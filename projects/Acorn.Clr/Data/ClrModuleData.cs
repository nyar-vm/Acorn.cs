using Acorn.Pe.Data;
using System;
using System.Text;

namespace Acorn.Clr.Data;

/// <summary>
///     CLR 模块数据（.NET 程序集）。
/// </summary>
public sealed class ClrModuleData
{
    /// <summary>
    ///     基础 PE 文件数据。
    /// </summary>
    public PeFileData PeFile { get; init; } = new();

    /// <summary>
    ///     CLR 目录表（PE 可选头中的 .NET 元数据入口点）。
    /// </summary>
    public ClrDirectoryData ClrDirectory { get; init; } = new();

    /// <summary>
    ///     元数据。
    /// </summary>
    public ClrMetadata Metadata { get; init; } = new();

    /// <summary>
    ///     方法列表（从 MethodDef 表解析）。
    /// </summary>
    public IReadOnlyList<ClrMethodDef> Methods { get; init; } = [];

    /// <summary>
    ///     类型列表（从 TypeDef 表解析）。
    /// </summary>
    public IReadOnlyList<ClrTypeDef> Types { get; init; } = [];

    /// <summary>
    ///     字段列表（从 Field 表解析）。
    /// </summary>
    public IReadOnlyList<ClrFieldDefRow> Fields { get; init; } = [];

    /// <summary>
    ///     属性列表（从 Property 表解析）。
    /// </summary>
    public IReadOnlyList<ClrPropertyDefRow> Properties { get; init; } = [];

    /// <summary>
    ///     事件列表（从 Event 表解析）。
    /// </summary>
    public IReadOnlyList<ClrEventDefRow> Events { get; init; } = [];

    /// <summary>
    ///     模块名。
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    ///     版本号。
    /// </summary>
    public string Version { get; init; } = string.Empty;
}

/// <summary>
///     CLR 目录表（PE 可选头中的 .NET 元数据入口点，72 字节）。
/// </summary>
public sealed class ClrDirectoryData
{
    /// <summary>
    ///     头部大小（字节）。
    /// </summary>
    public uint Cb { get; init; }

    /// <summary>
    ///     主运行时版本号。
    /// </summary>
    public ushort MajorRuntimeVersion { get; init; }

    /// <summary>
    ///     次运行时版本号。
    /// </summary>
    public ushort MinorRuntimeVersion { get; init; }

    /// <summary>
    ///     元数据 RVA。
    /// </summary>
    public uint MetadataRva { get; init; }

    /// <summary>
    ///     元数据大小。
    /// </summary>
    public uint MetadataSize { get; init; }

    /// <summary>
    ///     标志（<see cref="ClrDirectoryFlags" />）。
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     入口点 RVA 或令牌。
    /// </summary>
    public uint EntryPoint { get; init; }

    /// <summary>
    ///     资源 RVA。
    /// </summary>
    public uint ResourcesRva { get; init; }

    /// <summary>
    ///     资源大小。
    /// </summary>
    public uint ResourcesSize { get; init; }

    /// <summary>
    ///     强名称签名 RVA。
    /// </summary>
    public uint StrongNameSignatureRva { get; init; }

    /// <summary>
    ///     强名称签名大小。
    /// </summary>
    public uint StrongNameSignatureSize { get; init; }

    /// <summary>
    ///     代码管理器表 RVA。
    /// </summary>
    public uint CodeManagerTableRva { get; init; }

    /// <summary>
    ///     代码管理器表大小。
    /// </summary>
    public uint CodeManagerTableSize { get; init; }

    /// <summary>
    ///     VTable 固定部分映射 RVA。
    /// </summary>
    public uint VTableFixupsRva { get; init; }

    /// <summary>
    ///     VTable 固定部分映射大小。
    /// </summary>
    public uint VTableFixupsSize { get; init; }

    /// <summary>
    ///     导出地址表 RVA。
    /// </summary>
    public uint ExportAddressTableJumpsRva { get; init; }

    /// <summary>
    ///     导出地址表大小。
    /// </summary>
    public uint ExportAddressTableJumpsSize { get; init; }

    /// <summary>
    ///     托管原生头 RVA。
    /// </summary>
    public uint ManagedNativeHeaderRva { get; init; }

    /// <summary>
    ///     托管原生头大小。
    /// </summary>
    public uint ManagedNativeHeaderSize { get; init; }

    /// <summary>
    ///     是否为纯 IL 程序集。
    /// </summary>
    public bool IsILOnly => (Flags & (uint)ClrDirectoryFlags.ILOnly) != 0;

    /// <summary>
    ///     入口点是否为元数据令牌（而非 RVA）。
    /// </summary>
    public bool IsEntryPointToken => (Flags & (uint)ClrDirectoryFlags.NativeEntryPoint) == 0;
}

/// <summary>
///     CLR 元数据。
/// </summary>
public sealed class ClrMetadata
{
    /// <summary>
    ///     元数据头。
    /// </summary>
    public ClrMetadataHeader Header { get; init; } = new();

    /// <summary>
    ///     流头列表。
    /// </summary>
    public IReadOnlyList<ClrStreamHeader> StreamHeaders { get; init; } = [];

    /// <summary>
    ///     表流（#~ 或 #-）。
    /// </summary>
    public ClrTableStream? TableStream { get; init; }

    /// <summary>
    ///     字符串堆（#Strings）。
    /// </summary>
    public ClrStringHeap StringHeap { get; init; } = new();

    /// <summary>
    ///     Blob 堆（#Blob）。
    /// </summary>
    public ClrBlobHeap BlobHeap { get; init; } = new();

    /// <summary>
    ///     GUID 堆（#GUID）。
    /// </summary>
    public ClrGuidHeap GuidHeap { get; init; } = new();

    /// <summary>
    ///     用户字符串堆（#US）。
    /// </summary>
    public ClrUserStringHeap UserStringHeap { get; init; } = new();
}

/// <summary>
///     元数据头。
/// </summary>
public sealed class ClrMetadataHeader
{
    /// <summary>
    ///     签名（应为 <see cref="ClrConstants.MetadataSignature" /> = 0x424A5342 "BSJB"）。
    /// </summary>
    public uint Signature { get; init; }

    /// <summary>
    ///     主版本号。
    /// </summary>
    public ushort MajorVersion { get; init; }

    /// <summary>
    ///     次版本号。
    /// </summary>
    public ushort MinorVersion { get; init; }

    /// <summary>
    ///     保留字段。
    /// </summary>
    public uint Reserved { get; init; }

    /// <summary>
    ///     版本字符串长度（包含尾部填充）。
    /// </summary>
    public uint VersionStringLength { get; init; }

    /// <summary>
    ///     版本字符串。
    /// </summary>
    public string VersionString { get; init; } = string.Empty;

    /// <summary>
    ///     标志。
    /// </summary>
    public ushort Flags { get; init; }

    /// <summary>
    ///     流数量。
    /// </summary>
    public ushort Streams { get; init; }
}

/// <summary>
///     元数据流头。
/// </summary>
public sealed class ClrStreamHeader
{
    /// <summary>
    ///     流数据偏移量（相对于元数据根）。
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    ///     流数据大小（字节）。
    /// </summary>
    public uint Size { get; init; }

    /// <summary>
    ///     流名称（如 "#~"、"#Strings"、"#Blob"、"#GUID"、"#US"）。
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     表流（#~ 或 #-）。
/// </summary>
public sealed class ClrTableStream
{
    /// <summary>
    ///     表头部。
    /// </summary>
    public ClrTableHeader Header { get; init; } = new();

    /// <summary>
    ///     各表行数据，按 <see cref="ClrTableKind" /> 索引。
    /// </summary>
    public IReadOnlyList<ClrTableData> Tables { get; init; } = [];
}

/// <summary>
///     表流头部。
/// </summary>
public sealed class ClrTableHeader
{
    /// <summary>
    ///     预留字节。
    /// </summary>
    public uint Reserved { get; init; }

    /// <summary>
    ///     主版本号。
    /// </summary>
    public byte MajorVersion { get; init; }

    /// <summary>
    ///     次版本号。
    /// </summary>
    public byte MinorVersion { get; init; }

    /// <summary>
    ///     堆偏移大小标志（<see cref="ClrHeapSizeFlags" />）。
    /// </summary>
    public byte HeapSizes { get; init; }

    /// <summary>
    ///     有效表位掩码（哪些表存在）。
    /// </summary>
    public ulong ValidTables { get; init; }

    /// <summary>
    ///     已排序表位掩码。
    /// </summary>
    public ulong SortedTables { get; init; }

    /// <summary>
    ///     各表行计数（仅包含 ValidTables 中标记为存在的表）。
    /// </summary>
    public IReadOnlyList<uint> RowCounts { get; init; } = [];

    /// <summary>
    ///     #Strings 堆索引大小（2 或 4 字节）。
    /// </summary>
    public int StringIndexSize => (HeapSizes & (byte)ClrHeapSizeFlags.StringHeapLarge) != 0 ? 4 : 2;

    /// <summary>
    ///     #GUID 堆索引大小（2 或 4 字节）。
    /// </summary>
    public int GuidIndexSize => (HeapSizes & (byte)ClrHeapSizeFlags.GuidHeapLarge) != 0 ? 4 : 2;

    /// <summary>
    ///     #Blob 堆索引大小（2 或 4 字节）。
    /// </summary>
    public int BlobIndexSize => (HeapSizes & (byte)ClrHeapSizeFlags.BlobHeapLarge) != 0 ? 4 : 2;
}

/// <summary>
///     单个元数据表的原始数据。
/// </summary>
public sealed class ClrTableData
{
    /// <summary>
    ///     表类型。
    /// </summary>
    public ClrTableKind Kind { get; init; }

    /// <summary>
    ///     行数。
    /// </summary>
    public uint RowCount { get; init; }

    /// <summary>
    ///     原始行数据（每行为字节数组，由具体表类型解析）。
    /// </summary>
    public IReadOnlyList<byte[]> RawRows { get; init; } = [];
}

/// <summary>
///     元数据表类型（ECMA-335 §22）。
/// </summary>
public enum ClrTableKind : byte
{
    Module = 0,
    TypeRef = 1,
    TypeDef = 2,
    FieldPtr = 3,
    Field = 4,
    MethodPtr = 5,
    MethodDef = 6,
    ParamPtr = 7,
    Param = 8,
    InterfaceImpl = 9,
    MemberRef = 10,
    Constant = 11,
    CustomAttribute = 12,
    FieldMarshal = 13,
    DeclSecurity = 14,
    ClassLayout = 15,
    FieldLayout = 16,
    StandAloneSig = 17,
    EventMap = 18,
    EventPtr = 19,
    Event = 20,
    PropertyMap = 21,
    PropertyPtr = 22,
    Property = 23,
    MethodSemantics = 24,
    MethodImpl = 25,
    ModuleRef = 26,
    TypeSpec = 27,
    ImplMap = 28,
    FieldRVA = 29,
    EncLog = 30,
    EncMap = 31,
    Assembly = 32,
    AssemblyProcessor = 33,
    AssemblyOS = 34,
    AssemblyRef = 35,
    AssemblyRefProcessor = 36,
    AssemblyRefOS = 37,
    File = 38,
    ExportedType = 39,
    ManifestResource = 40,
    NestedClass = 41,
    GenericParam = 42,
    MethodSpec = 43,
    GenericParamConstraint = 44
}

#region 元数据表行类型

/// <summary>
///     Module 表行（ECMA-335 §22.30）。
/// </summary>
public sealed class ClrModuleRow
{
    public ushort Generation { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid Mvid { get; init; }
    public Guid EncId { get; init; }
    public Guid EncBaseId { get; init; }
}

/// <summary>
///     TypeDef 表行（ECMA-335 §22.37）。
/// </summary>
public sealed class ClrTypeDefRow
{
    public ClrTypeAttributes Flags { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public uint ExtendsIndex { get; init; }
    public int FieldListStart { get; init; }
    public int MethodListStart { get; init; }
}

/// <summary>
///     MethodDef 表行（ECMA-335 §22.26）。
/// </summary>
public sealed class ClrMethodDefRow
{
    public uint Rva { get; init; }
    public ushort ImplFlags { get; init; }
    public ClrMethodAttributes Flags { get; init; }
    public string Name { get; init; } = string.Empty;
    public uint SignatureIndex { get; init; }
    public int ParamListStart { get; init; }
}

/// <summary>
///     Field 表行（ECMA-335 §22.15）。
/// </summary>
public sealed class ClrFieldDefRow
{
    public ClrFieldAttributes Flags { get; init; }
    public string Name { get; init; } = string.Empty;
    public uint SignatureIndex { get; init; }
}

/// <summary>
///     Param 表行（ECMA-335 §22.33）。
/// </summary>
public sealed class ClrParamRow
{
    public ushort Flags { get; init; }
    public ushort Sequence { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     TypeRef 表行（ECMA-335 §22.38）。
/// </summary>
public sealed class ClrTypeRefRow
{
    public uint ResolutionScopeIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
}

/// <summary>
///     MemberRef 表行（ECMA-335 §22.25）。
/// </summary>
public sealed class ClrMemberRefRow
{
    public uint ClassIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public uint SignatureIndex { get; init; }
}

/// <summary>
///     InterfaceImpl 表行（ECMA-335 §22.23）。
/// </summary>
public sealed class ClrInterfaceImplRow
{
    public uint ClassIndex { get; init; }
    public uint InterfaceIndex { get; init; }
}

/// <summary>
///     Property 表行（ECMA-335 §22.34）。
/// </summary>
public sealed class ClrPropertyDefRow
{
    public ushort Flags { get; init; }
    public string Name { get; init; } = string.Empty;
    public uint SignatureIndex { get; init; }
}

/// <summary>
///     Event 表行（ECMA-335 §22.13）。
/// </summary>
public sealed class ClrEventDefRow
{
    public ushort EventFlags { get; init; }
    public string Name { get; init; } = string.Empty;
    public uint EventTypeIndex { get; init; }
}

/// <summary>
///     Assembly 表行（ECMA-335 §22.2）。
/// </summary>
public sealed class ClrAssemblyRow
{
    public uint HashAlgId { get; init; }
    public ushort MajorVersion { get; init; }
    public ushort MinorVersion { get; init; }
    public ushort BuildNumber { get; init; }
    public ushort RevisionNumber { get; init; }
    public uint Flags { get; init; }
    public uint PublicKeyIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Culture { get; init; } = string.Empty;
}

/// <summary>
///     AssemblyRef 表行（ECMA-335 §22.5）。
/// </summary>
public sealed class ClrAssemblyRefRow
{
    public ushort MajorVersion { get; init; }
    public ushort MinorVersion { get; init; }
    public ushort BuildNumber { get; init; }
    public ushort RevisionNumber { get; init; }
    public uint Flags { get; init; }
    public uint PublicKeyOrTokenIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Culture { get; init; } = string.Empty;
    public uint HashValueIndex { get; init; }
}

/// <summary>
///     NestedClass 表行（ECMA-335 §22.32）。
/// </summary>
public sealed class ClrNestedClassRow
{
    public uint NestedClassIndex { get; init; }
    public uint EnclosingClassIndex { get; init; }
}

/// <summary>
///     CustomAttribute 表行（ECMA-335 §22.10）。
/// </summary>
public sealed class ClrCustomAttributeRow
{
    public uint ParentIndex { get; init; }
    public uint TypeIndex { get; init; }
    public uint ValueIndex { get; init; }
}

/// <summary>
///     StandAloneSig 表行（ECMA-335 §22.36）。
/// </summary>
public sealed class ClrStandAloneSigRow
{
    public uint SignatureIndex { get; init; }
}

/// <summary>
///     TypeSpec 表行（ECMA-335 §22.39）。
/// </summary>
public sealed class ClrTypeSpecRow
{
    public uint SignatureIndex { get; init; }
}

/// <summary>
///     MethodSpec 表行（ECMA-335 §22.27）。
/// </summary>
public sealed class ClrMethodSpecRow
{
    public uint MethodIndex { get; init; }
    public uint InstantiationIndex { get; init; }
}

/// <summary>
///     GenericParam 表行（ECMA-335 §22.20）。
/// </summary>
public sealed class ClrGenericParamRow
{
    public ushort Number { get; init; }
    public ushort Flags { get; init; }
    public uint OwnerIndex { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     GenericParamConstraint 表行（ECMA-335 §22.21）。
/// </summary>
public sealed class ClrGenericParamConstraintRow
{
    public uint OwnerIndex { get; init; }
    public uint ConstraintIndex { get; init; }
}

/// <summary>
///     ManifestResource 表行（ECMA-335 §22.24）。
/// </summary>
public sealed class ClrManifestResourceRow
{
    public uint Offset { get; init; }
    public uint Flags { get; init; }
    public uint ImplementationIndex { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     File 表行（ECMA-335 §22.16）。
/// </summary>
public sealed class ClrFileRow
{
    public uint Flags { get; init; }
    public string Name { get; init; } = string.Empty;
    public uint HashValueIndex { get; init; }
}

/// <summary>
///     ExportedType 表行（ECMA-335 §22.14）。
/// </summary>
public sealed class ClrExportedTypeRow
{
    public uint Flags { get; init; }
    public uint TypeDefId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public uint ImplementationIndex { get; init; }
}

#endregion

#region 堆类型

/// <summary>
///     字符串堆（#Strings）。
/// </summary>
public sealed class ClrStringHeap
{
    /// <summary>
    ///     原始数据。
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     按偏移量读取以 null 结尾的 UTF-8 字符串。
    /// </summary>
    public string ReadString(uint offset)
    {
        if (offset == 0 || offset >= Data.Length)
        {
            return string.Empty;
        }

        var start = (int)offset;
        var end = start;

        while (end < Data.Length && Data[end] != 0)
        {
            end++;
        }

        return Encoding.UTF8.GetString(Data, start, end - start);
    }
}

/// <summary>
///     Blob 堆（#Blob）。
/// </summary>
public sealed class ClrBlobHeap
{
    /// <summary>
    ///     原始数据。
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     按偏移量读取 Blob（先读取压缩长度前缀，再读取数据）。
    /// </summary>
    public ReadOnlySpan<byte> ReadBlob(uint offset)
    {
        if (offset == 0 || offset >= Data.Length)
        {
            return [];
        }

        var pos = (int)offset;
        var length = DecodeBlobLength(Data, ref pos);

        return Data.AsSpan(pos, length);
    }

    /// <summary>
    ///     解码 Blob 堆的压缩长度前缀（ECMA-335 §23.2.4）。
    /// </summary>
    internal static int DecodeBlobLength(byte[] data, ref int pos)
    {
        var first = data[pos];

        if ((first & 0x80) == 0)
        {
            pos++;
            return first;
        }

        if ((first & 0xC0) == 0x80)
        {
            pos += 2;
            return ((first & 0x3F) << 8) | data[pos - 1];
        }

        if ((first & 0xE0) == 0xC0)
        {
            pos += 4;
            return ((first & 0x1F) << 24) | (data[pos - 3] << 16) | (data[pos - 2] << 8) | data[pos - 1];
        }

        pos++;
        return 0;
    }
}

/// <summary>
///     GUID 堆（#GUID）。
/// </summary>
public sealed class ClrGuidHeap
{
    /// <summary>
    ///     原始数据。
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     按索引读取 GUID（索引从 1 开始，每个 GUID 16 字节）。
    /// </summary>
    public Guid ReadGuid(uint index)
    {
        if (index == 0 || Data.Length < 16)
        {
            return Guid.Empty;
        }

        var offset = (int)(index - 1) * 16;

        if (offset + 16 > Data.Length)
        {
            return Guid.Empty;
        }

        return new Guid(Data.AsSpan(offset, 16));
    }
}

/// <summary>
///     用户字符串堆（#US）。
/// </summary>
public sealed class ClrUserStringHeap
{
    /// <summary>
    ///     原始数据。
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     按偏移量读取用户字符串（先读取压缩长度前缀，再读取 UTF-16LE 数据 + 尾部标志字节）。
    /// </summary>
    public string ReadUserString(uint offset)
    {
        if (offset == 0 || offset >= Data.Length)
        {
            return string.Empty;
        }

        var pos = (int)offset;
        var length = ClrBlobHeap.DecodeBlobLength(Data, ref pos);

        if (length == 0 || pos + length > Data.Length)
        {
            return string.Empty;
        }

        var byteCount = length - 1;

        if (byteCount <= 0)
        {
            return string.Empty;
        }

        return Encoding.Unicode.GetString(Data, pos, byteCount);
    }
}

#endregion

#region 高级类型（解析后的便捷视图）

/// <summary>
///     方法定义（解析后的高级视图）。
/// </summary>
public sealed class ClrMethodDef
{
    /// <summary>
    ///     方法名。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     方法签名（不含 Blob 压缩长度前缀），例如 `00 00 08` 表示 `int32 ()`。
    /// </summary>
    public byte[] Signature { get; init; } = [];

    /// <summary>
    ///     访问标志。
    /// </summary>
    public ClrMethodAttributes Flags { get; init; }

    /// <summary>
    ///     方法 RVA。
    /// </summary>
    public uint Rva { get; init; }

    /// <summary>
    ///     代码大小。
    /// </summary>
    public uint CodeSize { get; init; }

    /// <summary>
    ///     局部变量签名令牌。
    /// </summary>
    public uint LocalVarSigTok { get; init; }

    /// <summary>
    ///     最大栈深度。
    /// </summary>
    public ushort MaxStack { get; init; }

    /// <summary>
    ///     MSIL 指令列表。
    /// </summary>
    public IReadOnlyList<ClrInstruction> Instructions { get; init; } = [];

    /// <summary>
    ///     异常处理表。
    /// </summary>
    public IReadOnlyList<ClrExceptionHandler> ExceptionHandlers { get; init; } = [];
}

/// <summary>
///     类型定义（解析后的高级视图）。
/// </summary>
public sealed class ClrTypeDef
{
    /// <summary>
    ///     类型名。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     命名空间。
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    ///     类型标志。
    /// </summary>
    public ClrTypeAttributes Flags { get; init; }

    /// <summary>
    ///     父类索引（TypeDef 或 TypeRef 编码索引）。
    /// </summary>
    public uint ExtendsIndex { get; init; }

    /// <summary>
    ///     字段列表。
    /// </summary>
    public IReadOnlyList<ClrFieldDef> Fields { get; init; } = [];

    /// <summary>
    ///     方法列表。
    /// </summary>
    public IReadOnlyList<ClrMethodDef> Methods { get; init; } = [];

    /// <summary>
    ///     属性列表。
    /// </summary>
    public IReadOnlyList<ClrPropertyDef> Properties { get; init; } = [];

    /// <summary>
    ///     事件列表。
    /// </summary>
    public IReadOnlyList<ClrEventDef> Events { get; init; } = [];
}

/// <summary>
///     字段定义（解析后的高级视图）。
/// </summary>
public sealed class ClrFieldDef
{
    /// <summary>
    ///     字段名。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     字段标志。
    /// </summary>
    public ClrFieldAttributes Flags { get; init; }

    /// <summary>
    ///     签名 Blob 偏移。
    /// </summary>
    public uint SignatureIndex { get; init; }
}

/// <summary>
///     属性定义（解析后的高级视图）。
/// </summary>
public sealed class ClrPropertyDef
{
    /// <summary>
    ///     属性名。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性标志。
    /// </summary>
    public ushort Flags { get; init; }

    /// <summary>
    ///     签名 Blob 偏移。
    /// </summary>
    public uint SignatureIndex { get; init; }
}

/// <summary>
///     事件定义（解析后的高级视图）。
/// </summary>
public sealed class ClrEventDef
{
    /// <summary>
    ///     事件名。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     事件标志。
    /// </summary>
    public ushort EventFlags { get; init; }

    /// <summary>
    ///     事件类型索引。
    /// </summary>
    public uint EventTypeIndex { get; init; }
}

#endregion

#region MSIL 指令

/// <summary>
///     MSIL 指令。
/// </summary>
public sealed class ClrInstruction
{
    /// <summary>
    ///     指令偏移量。
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    ///     操作码。
    /// </summary>
    public ClrOpcode Opcode { get; init; }

    /// <summary>
    ///     操作数。
    /// </summary>
    public ClrOperand? Operand { get; init; }
}

/// <summary>
///     MSIL 操作码（ECMA-335 标准定义）。
/// </summary>
/// <remarks>
///     单字节操作码范围 0x00-0xFE，双字节操作码以 0xFE 为前缀，
///     编码为 0xFE00-0xFEFF。
/// </remarks>
public enum ClrOpcode : ushort
{
    Nop = 0x00,
    Break = 0x01,
    Ldarg_0 = 0x02,
    Ldarg_1 = 0x03,
    Ldarg_2 = 0x04,
    Ldarg_3 = 0x05,
    Ldloc_0 = 0x06,
    Ldloc_1 = 0x07,
    Ldloc_2 = 0x08,
    Ldloc_3 = 0x09,
    Stloc_0 = 0x0A,
    Stloc_1 = 0x0B,
    Stloc_2 = 0x0C,
    Stloc_3 = 0x0D,
    Ldarg_S = 0x0E,
    Ldarga_S = 0x0F,
    Starg_S = 0x10,
    Ldloc_S = 0x11,
    Ldloca_S = 0x12,
    Stloc_S = 0x13,
    Ldnull = 0x14,
    Ldc_I4_M1 = 0x15,
    Ldc_I4_0 = 0x16,
    Ldc_I4_1 = 0x17,
    Ldc_I4_2 = 0x18,
    Ldc_I4_3 = 0x19,
    Ldc_I4_4 = 0x1A,
    Ldc_I4_5 = 0x1B,
    Ldc_I4_6 = 0x1C,
    Ldc_I4_7 = 0x1D,
    Ldc_I4_8 = 0x1E,
    Ldc_I4_S = 0x1F,
    Ldc_I4 = 0x20,
    Ldc_I8 = 0x21,
    Ldc_R4 = 0x22,
    Ldc_R8 = 0x23,
    Dup = 0x25,
    Pop = 0x26,
    Jmp = 0x27,
    Call = 0x28,
    Calli = 0x29,
    Ret = 0x2A,
    Br_S = 0x2B,
    Brfalse_S = 0x2C,
    Brtrue_S = 0x2D,
    Beq_S = 0x2E,
    Bne_Un_S = 0x2F,
    Blt_S = 0x30,
    Ble_S = 0x31,
    Bgt_S = 0x32,
    Bge_S = 0x33,
    Blt_Un_S = 0x34,
    Ble_Un_S = 0x35,
    Bgt_Un_S = 0x36,
    Bge_Un_S = 0x37,
    Br = 0x38,
    Brfalse = 0x39,
    Brtrue = 0x3A,
    Beq = 0x3B,
    Bne_Un = 0x3C,
    Blt = 0x3D,
    Ble = 0x3E,
    Bgt = 0x3F,
    Bge = 0x40,
    Blt_Un = 0x41,
    Ble_Un = 0x42,
    Bgt_Un = 0x43,
    Bge_Un = 0x44,
    Switch = 0x45,
    Ldind_I1 = 0x46,
    Ldind_U1 = 0x47,
    Ldind_I2 = 0x48,
    Ldind_U2 = 0x49,
    Ldind_I4 = 0x4A,
    Ldind_U4 = 0x4B,
    Ldind_I8 = 0x4C,
    Ldind_I = 0x4D,
    Ldind_R4 = 0x4E,
    Ldind_R8 = 0x4F,
    Ldind_Ref = 0x50,
    Stind_Ref = 0x51,
    Stind_I1 = 0x52,
    Stind_I2 = 0x53,
    Stind_I4 = 0x54,
    Stind_I8 = 0x55,
    Stind_R4 = 0x56,
    Stind_R8 = 0x57,
    Add = 0x58,
    Sub = 0x59,
    Mul = 0x5A,
    Div = 0x5B,
    Div_Un = 0x5C,
    Rem = 0x5D,
    Rem_Un = 0x5E,
    And = 0x5F,
    Or = 0x60,
    Xor = 0x61,
    Shl = 0x62,
    Shr = 0x63,
    Shr_Un = 0x64,
    Neg = 0x65,
    Not = 0x66,
    Conv_I1 = 0x67,
    Conv_I2 = 0x68,
    Conv_I4 = 0x69,
    Conv_I8 = 0x6A,
    Conv_R4 = 0x6B,
    Conv_R8 = 0x6C,
    Conv_U4 = 0x6D,
    Conv_U8 = 0x6E,
    Callvirt = 0x6F,
    Cpobj = 0x70,
    Ldobj = 0x71,
    Ldstr = 0x72,
    Newobj = 0x73,
    Castclass = 0x74,
    Isinst = 0x75,
    Conv_R_Un = 0x76,
    Unbox = 0x79,
    Throw = 0x7A,
    Ldfld = 0x7B,
    Ldflda = 0x7C,
    Stfld = 0x7D,
    Ldsfld = 0x7E,
    Ldsflda = 0x7F,
    Stsfld = 0x80,
    Stobj = 0x81,
    Conv_Ovf_I1_Un = 0x82,
    Conv_Ovf_I2_Un = 0x83,
    Conv_Ovf_I4_Un = 0x84,
    Conv_Ovf_I8_Un = 0x85,
    Conv_Ovf_U1_Un = 0x86,
    Conv_Ovf_U2_Un = 0x87,
    Conv_Ovf_U4_Un = 0x88,
    Conv_Ovf_U8_Un = 0x89,
    Conv_Ovf_I_Un = 0x8A,
    Conv_Ovf_U_Un = 0x8B,
    Box = 0x8C,
    Newarr = 0x8D,
    Ldlen = 0x8E,
    Ldelema = 0x8F,
    Ldelem_I1 = 0x90,
    Ldelem_U1 = 0x91,
    Ldelem_I2 = 0x92,
    Ldelem_U2 = 0x93,
    Ldelem_I4 = 0x94,
    Ldelem_U4 = 0x95,
    Ldelem_I8 = 0x96,
    Ldelem_I = 0x97,
    Ldelem_R4 = 0x98,
    Ldelem_R8 = 0x99,
    Ldelem_Ref = 0x9A,
    Stelem_I = 0x9B,
    Stelem_I1 = 0x9C,
    Stelem_I2 = 0x9D,
    Stelem_I4 = 0x9E,
    Stelem_I8 = 0x9F,
    Stelem_R4 = 0xA0,
    Stelem_R8 = 0xA1,
    Stelem_Ref = 0xA2,
    Ldelem_Any = 0xA3,
    Stelem_Any = 0xA4,
    Unbox_Any = 0xA5,
    Conv_Ovf_I1 = 0xB3,
    Conv_Ovf_U1 = 0xB4,
    Conv_Ovf_I2 = 0xB5,
    Conv_Ovf_U2 = 0xB6,
    Conv_Ovf_I4 = 0xB7,
    Conv_Ovf_U4 = 0xB8,
    Conv_Ovf_I8 = 0xB9,
    Conv_Ovf_U8 = 0xBA,
    Conv_Ovf_I = 0xC2,
    Conv_Ovf_U = 0xC3,
    Leave = 0xDD,
    Leave_S = 0xDE,
    Stind_I = 0xDF,
    Conv_U = 0xE0,

    Arglist = 0xFE00,
    Ceq = 0xFE01,
    Cgt = 0xFE02,
    Cgt_Un = 0xFE03,
    Clt = 0xFE04,
    Clt_Un = 0xFE05,
    Ldftn = 0xFE06,
    Ldvirtftn = 0xFE07,
    Ldarg = 0xFE09,
    Ldarga = 0xFE0A,
    Starg = 0xFE0B,
    Ldloc = 0xFE0C,
    Ldloca = 0xFE0D,
    Stloc = 0xFE0E,
    Localloc = 0xFE0F,
    Endfilter = 0xFE11,
    Unaligned = 0xFE12,
    Volatile = 0xFE13,
    Tail = 0xFE14,
    Initobj = 0xFE15,
    Constrained = 0xFE16,
    Cpblk = 0xFE17,
    Initblk = 0xFE18,
    Rethrow = 0xFE1A,
    Sizeof = 0xFE1C,
    Refanytype = 0xFE1D,
    Readonly = 0xFE1E,
    Mkrefany = 0xC6,
    Refanyval = 0xC7,
    Ldtoken = 0xD0
}

/// <summary>
///     MSIL 操作数。
/// </summary>
public abstract class ClrOperand
{
    /// <summary>
    ///     操作数类型。
    /// </summary>
    public abstract ClrOperandKind Kind { get; }
}

/// <summary>
///     操作数类型。
/// </summary>
public enum ClrOperandKind
{
    None,
    Int8,
    Int16,
    Int32,
    Int64,
    Float32,
    Float64,
    String,
    Token,
    BranchTarget8,
    BranchTarget32,
    SwitchTargets,
    LocalIndex,
    ArgumentIndex
}

/// <summary>
///     8 位整数操作数。
/// </summary>
public sealed class ClrInt8Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Int8;
    public sbyte Value { get; init; }
}

/// <summary>
///     16 位整数操作数。
/// </summary>
public sealed class ClrInt16Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Int16;
    public short Value { get; init; }
}

/// <summary>
///     32 位整数操作数。
/// </summary>
public sealed class ClrInt32Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Int32;
    public int Value { get; init; }
}

/// <summary>
///     64 位整数操作数。
/// </summary>
public sealed class ClrInt64Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Int64;
    public long Value { get; init; }
}

/// <summary>
///     32 位浮点操作数。
/// </summary>
public sealed class ClrFloat32Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Float32;
    public float Value { get; init; }
}

/// <summary>
///     64 位浮点操作数。
/// </summary>
public sealed class ClrFloat64Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Float64;
    public double Value { get; init; }
}

/// <summary>
///     元数据令牌操作数。
/// </summary>
public sealed class ClrTokenOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Token;
    public uint Value { get; init; }
}

/// <summary>
///     8 位分支目标操作数。
/// </summary>
public sealed class ClrBranchTarget8Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.BranchTarget8;
    public int Offset { get; init; }
}

/// <summary>
///     32 位分支目标操作数。
/// </summary>
public sealed class ClrBranchTarget32Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.BranchTarget32;
    public int Offset { get; init; }
}

/// <summary>
///     开关目标操作数。
/// </summary>
public sealed class ClrSwitchTargetsOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.SwitchTargets;
    public IReadOnlyList<int> Offsets { get; init; } = [];
}

/// <summary>
///     局部变量索引操作数。
/// </summary>
public sealed class ClrLocalIndexOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.LocalIndex;
    public uint Index { get; init; }
}

/// <summary>
///     参数索引操作数。
/// </summary>
public sealed class ClrArgumentIndexOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.ArgumentIndex;
    public uint Index { get; init; }
}

#endregion

/// <summary>
///     异常处理子句。
/// </summary>
public sealed class ClrExceptionHandler
{
    /// <summary>
    ///     异常处理类型标志。
    /// </summary>
    public ClrExceptionHandlerKind HandlerKind { get; init; }

    /// <summary>
    ///     尝试块开始偏移。
    /// </summary>
    public uint TryStart { get; init; }

    /// <summary>
    ///     尝试块长度。
    /// </summary>
    public uint TryLength { get; init; }

    /// <summary>
    ///     处理块开始偏移。
    /// </summary>
    public uint HandlerStart { get; init; }

    /// <summary>
    ///     处理块长度。
    /// </summary>
    public uint HandlerLength { get; init; }

    /// <summary>
    ///     异常类型令牌（Catch 类型）或 Filter 偏移。
    /// </summary>
    public uint ClassTokenOrFilterOffset { get; init; }
}

/// <summary>
///     异常处理类型（ECMA-335 §25.4.6）。
/// </summary>
public enum ClrExceptionHandlerKind : uint
{
    Catch = 0x0000,
    Filter = 0x0001,
    Finally = 0x0002,
    Fault = 0x0004
}

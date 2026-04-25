using Acorn.Pe.Data;
using System;
using System.Text;

namespace Acorn.Clr.Data;

/// <summary>
///     CLR 模块数据（.NET 程序集）
/// </summary>
public sealed class ClrModuleData
{
    /// <summary>
    ///     基础 PE 文件数据
    /// </summary>
    public PeFileData PeFile { get; init; }

    /// <summary>
    ///     CLR 目录表（元数据入口点）
    /// </summary>
    public ClrDirectoryData ClrDirectory { get; init; }

    /// <summary>
    ///     元数据
    /// </summary>
    public ClrMetadata Metadata { get; init; }

    /// <summary>
    ///     方法列表
    /// </summary>
    public IReadOnlyList<ClrMethod> Methods { get; init; }

    /// <summary>
    ///     类型列表
    /// </summary>
    public IReadOnlyList<ClrType> Types { get; init; }

    /// <summary>
    ///     字段列表
    /// </summary>
    public IReadOnlyList<ClrField> Fields { get; init; }

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<ClrProperty> Properties { get; init; }

    /// <summary>
    ///     事件列表
    /// </summary>
    public IReadOnlyList<ClrEvent> Events { get; init; }

    /// <summary>
    ///     模块名
    /// </summary>
    public string ModuleName { get; init; }

    /// <summary>
    ///     版本号
    /// </summary>
    public string Version { get; init; }
}

/// <summary>
///     CLR 目录表（PE 可选头中的 .NET 元数据入口点）
/// </summary>
public sealed class ClrDirectoryData
{
    /// <summary>
    ///     特征标志
    /// </summary>
    public uint Characteristics { get; init; }

    /// <summary>
    ///     主版本号
    /// </summary>
    public ushort MajorVersion { get; init; }

    /// <summary>
    ///     次版本号
    /// </summary>
    public ushort MinorVersion { get; init; }

    /// <summary>
    ///     元数据 RVA
    /// </summary>
    public uint MetadataRva { get; init; }

    /// <summary>
    ///     元数据大小
    /// </summary>
    public uint MetadataSize { get; init; }

    /// <summary>
    ///     标志
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     入口点 RVA（如果有）
    /// </summary>
    public uint EntryPointRva { get; init; }

    /// <summary>
    ///     资源 RVA
    /// </summary>
    public uint ResourcesRva { get; init; }

    /// <summary>
    ///     资源大小
    /// </summary>
    public uint ResourcesSize { get; init; }

    /// <summary>
    ///     强名称签名 RVA
    /// </summary>
    public uint StrongNameSignatureRva { get; init; }

    /// <summary>
    ///     强名称签名大小
    /// </summary>
    public uint StrongNameSignatureSize { get; init; }

    /// <summary>
    ///     代码管理器表 RVA
    /// </summary>
    public uint CodeManagerTableRva { get; init; }

    /// <summary>
    ///     代码管理器表大小
    /// </summary>
    public uint CodeManagerTableSize { get; init; }

    /// <summary>
    ///     VTable 固定部分映射 RVA
    /// </summary>
    public uint VTableFixupsRva { get; init; }

    /// <summary>
    ///     VTable 固定部分映射大小
    /// </summary>
    public uint VTableFixupsSize { get; init; }

    /// <summary>
    ///     导出地址表 RVA
    /// </summary>
    public uint ExportAddressTableJumpsRva { get; init; }

    /// <summary>
    ///     导出地址表大小
    /// </summary>
    public uint ExportAddressTableJumpsSize { get; init; }

    /// <summary>
    ///     托管原生头 RVA
    /// </summary>
    public uint ManagedNativeHeaderRva { get; init; }

    /// <summary>
    ///     托管原生头大小
    /// </summary>
    public uint ManagedNativeHeaderSize { get; init; }
}

/// <summary>
///     CLR 元数据
/// </summary>
public sealed class ClrMetadata
{
    /// <summary>
    ///     元数据头
    /// </summary>
    public ClrMetadataHeader Header { get; init; }

    /// <summary>
    ///     表流（#~ 或 #Schema）
    /// </summary>
    public ClrTableStream TableStream { get; init; }

    /// <summary>
    ///     字符串堆（#Strings）
    /// </summary>
    public ClrStringHeap StringHeap { get; init; }

    /// <summary>
    ///     Blob 堆（#Blob）
    /// </summary>
    public ClrBlobHeap BlobHeap { get; init; }

    /// <summary>
    ///     GUID 堆（#GUID）
    /// </summary>
    public ClrGuidHeap GuidHeap { get; init; }

    /// <summary>
    ///     用户字符串堆（#US）
    /// </summary>
    public ClrUserStringHeap UserStringHeap { get; init; }
}

/// <summary>
///     元数据头
/// </summary>
public sealed class ClrMetadataHeader
{
    /// <summary>
    ///     签名（"Magic"）
    /// </summary>
    public uint Magic { get; init; }

    /// <summary>
    ///     主版本号
    /// </summary>
    public ushort MajorVersion { get; init; }

    /// <summary>
    ///     次版本号
    /// </summary>
    public ushort MinorVersion { get; init; }

    /// <summary>
    ///     保留字段
    /// </summary>
    public uint Reserved { get; init; }

    /// <summary>
    ///     主版本字符串长度
    /// </summary>
    public uint VersionStringLength { get; init; }

    /// <summary>
    ///     版本字符串
    /// </summary>
    public string VersionString { get; init; }

    /// <summary>
    ///     位掩码（表存在性）
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     流数量
    /// </summary>
    public uint Streams { get; init; }
}

/// <summary>
///     表流
/// </summary>
public sealed class ClrTableStream
{
    /// <summary>
    ///     表头部
    /// </summary>
    public ClrTableHeader Header { get; init; }

    /// <summary>
    ///     表数据
    /// </summary>
    public IReadOnlyList<ClrTable> Tables { get; init; }
}

/// <summary>
///     表头部
/// </summary>
public sealed class ClrTableHeader
{
    /// <summary>
    ///     预留字节
    /// </summary>
    public byte Reserved1 { get; init; }

    /// <summary>
    ///     主要表计数
    /// </summary>
    public byte MajorVersion { get; init; }

    /// <summary>
    ///     次要表计数
    /// </summary>
    public byte MinorVersion { get; init; }

    /// <summary>
    ///     堆偏移大小（2 或 4）
    /// </summary>
    public byte HeapOffsetSize { get; init; }

    /// <summary>
    ///     表行计数
    /// </summary>
    public IReadOnlyList<uint> RowCounts { get; init; }
}

/// <summary>
///     表基类
/// </summary>
public abstract class ClrTable
{
    /// <summary>
    ///     表类型
    /// </summary>
    public abstract ClrTableKind Kind { get; }

    /// <summary>
    ///     行数据
    /// </summary>
    public abstract IReadOnlyList<ClrTableRow> Rows { get; }
}

/// <summary>
///     表类型
/// </summary>
public enum ClrTableKind
{
    Module = 0,
    TypeRef = 1,
    TypeDef = 2,
    Field = 4,
    MethodDef = 6,
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
    Event = 20,
    PropertyMap = 21,
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

/// <summary>
///     表行基类
/// </summary>
public abstract class ClrTableRow
{
    /// <summary>
    ///     行索引
    /// </summary>
    public uint Index { get; init; }
}

/// <summary>
///     字符串堆
/// </summary>
public sealed class ClrStringHeap
{
    /// <summary>
    ///     数据
    /// </summary>
    public byte[] Data { get; init; }

    /// <summary>
    ///     按偏移量读取字符串
    /// </summary>
    public string ReadString(uint offset) => Encoding.UTF8.GetString(Data, (int)offset, Data.Length - (int)offset);
}

/// <summary>
///     Blob 堆
/// </summary>
public sealed class ClrBlobHeap
{
    /// <summary>
    ///     数据
    /// </summary>
    public byte[] Data { get; init; }

    /// <summary>
    ///     按偏移量读取 Blob
    /// </summary>
    public byte[] ReadBlob(uint offset) => Data[(int)offset..];
}

/// <summary>
///     GUID 堆
/// </summary>
public sealed class ClrGuidHeap
{
    /// <summary>
    ///     数据
    /// </summary>
    public byte[] Data { get; init; }

    /// <summary>
    ///     按索引读取 GUID
    /// </summary>
    public Guid ReadGuid(uint index) => new(Data.Skip((int)(index * 16)).Take(16).ToArray());
}

/// <summary>
///     用户字符串堆
/// </summary>
public sealed class ClrUserStringHeap
{
    /// <summary>
    ///     数据
    /// </summary>
    public byte[] Data { get; init; }

    /// <summary>
    ///     按偏移量读取用户字符串
    /// </summary>
    public string ReadUserString(uint offset) => Encoding.Unicode.GetString(Data, (int)offset, Data.Length - (int)offset);
}

/// <summary>
///     方法定义
/// </summary>
public sealed class ClrMethod
{
    /// <summary>
    ///     方法名
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     签名
    /// </summary>
    public string Signature { get; init; }

    /// <summary>
    ///     访问标志
    /// </summary>
    public uint AccessFlags { get; init; }

    /// <summary>
    ///     RVA
    /// </summary>
    public uint Rva { get; init; }

    /// <summary>
    ///     代码大小
    /// </summary>
    public uint CodeSize { get; init; }

    /// <summary>
    ///     局部变量大小
    /// </summary>
    public uint LocalVarSigTok { get; init; }

    /// <summary>
    ///     MSIL 指令
    /// </summary>
    public IReadOnlyList<ClrInstruction> Instructions { get; init; }

    /// <summary>
    ///     异常表
    /// </summary>
    public IReadOnlyList<ClrExceptionHandler> ExceptionHandlers { get; init; }
}

/// <summary>
///     类型定义
/// </summary>
public sealed class ClrType
{
    /// <summary>
    ///     类型名
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     命名空间
    /// </summary>
    public string Namespace { get; init; }

    /// <summary>
    ///     访问标志
    /// </summary>
    public uint AccessFlags { get; init; }

    /// <summary>
    ///     父类
    /// </summary>
    public ClrType? BaseType { get; init; }

    /// <summary>
    ///     接口列表
    /// </summary>
    public IReadOnlyList<ClrType> Interfaces { get; init; }

    /// <summary>
    ///     字段列表
    /// </summary>
    public IReadOnlyList<ClrField> Fields { get; init; }

    /// <summary>
    ///     方法列表
    /// </summary>
    public IReadOnlyList<ClrMethod> Methods { get; init; }

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<ClrProperty> Properties { get; init; }

    /// <summary>
    ///     事件列表
    /// </summary>
    public IReadOnlyList<ClrEvent> Events { get; init; }
}

/// <summary>
///     字段定义
/// </summary>
public sealed class ClrField
{
    /// <summary>
    ///     字段名
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     签名
    /// </summary>
    public string Signature { get; init; }

    /// <summary>
    ///     访问标志
    /// </summary>
    public uint AccessFlags { get; init; }
}

/// <summary>
///     属性定义
/// </summary>
public sealed class ClrProperty
{
    /// <summary>
    ///     属性名
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     签名
    /// </summary>
    public string Signature { get; init; }

    /// <summary>
    ///     访问标志
    /// </summary>
    public uint AccessFlags { get; init; }

    /// <summary>
    ///     Getter 方法
    /// </summary>
    public ClrMethod? GetMethod { get; init; }

    /// <summary>
    ///     Setter 方法
    /// </summary>
    public ClrMethod? SetMethod { get; init; }
}

/// <summary>
///     事件定义
/// </summary>
public sealed class ClrEvent
{
    /// <summary>
    ///     事件名
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     事件类型
    /// </summary>
    public ClrType EventType { get; init; }

    /// <summary>
    ///     访问标志
    /// </summary>
    public uint AccessFlags { get; init; }

    /// <summary>
    ///     Add 方法
    /// </summary>
    public ClrMethod? AddMethod { get; init; }

    /// <summary>
    ///     Remove 方法
    /// </summary>
    public ClrMethod? RemoveMethod { get; init; }

    /// <summary>
    ///     Raise 方法
    /// </summary>
    public ClrMethod? RaiseMethod { get; init; }
}

/// <summary>
///     MSIL 指令
/// </summary>
public sealed class ClrInstruction
{
    /// <summary>
    ///     偏移量
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    ///     操作码
    /// </summary>
    public ClrOpcode Opcode { get; init; }

    /// <summary>
    ///     操作数
    /// </summary>
    public ClrOperand? Operand { get; init; }
}

/// <summary>
///     MSIL 操作码
/// </summary>
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
    Ldind_R4 = 0x4D,
    Ldind_R8 = 0x4E,
    Ldind_Ref = 0x4F,
    Stind_Ref = 0x50,
    Stind_I1 = 0x51,
    Stind_I2 = 0x52,
    Stind_I4 = 0x53,
    Stind_I8 = 0x54,
    Stind_R4 = 0x55,
    Stind_R8 = 0x56,
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
    Ldftn = 0x81,
    Ldvirtftn = 0x83,
    Ldarg = 0x89,
    Ldarga = 0x8A,
    Starg = 0x8B,
    Ldloc = 0x8C,
    Ldloca = 0x8D,
    Stloc = 0x8E,
    Localloc = 0x8F,
    Endfinally = 0xDC,
    Leave = 0xDD,
    Leave_S = 0xDE,
    Stind_I = 0xFE01,
    Ldind_I = 0xFE02,
    Conv_U1 = 0xFE03,
    Conv_U2 = 0xFE04,
    Conv_I = 0xFE05,
    Conv_Ovf_I1_Un = 0xFE06,
    Conv_Ovf_I2_Un = 0xFE07,
    Conv_Ovf_I4_Un = 0xFE08,
    Conv_Ovf_I8_Un = 0xFE09,
    Conv_Ovf_U1_Un = 0xFE0A,
    Conv_Ovf_U2_Un = 0xFE0B,
    Conv_Ovf_U4_Un = 0xFE0C,
    Conv_Ovf_U8_Un = 0xFE0D,
    Conv_Ovf_I_Un = 0xFE0E,
    Conv_Ovf_U_Un = 0xFE0F,
    Box = 0xFE10,
    Newarr = 0xFE14,
    Ldlen = 0xFE15,
    Ldelema = 0xFE16,
    Ldelem_I1 = 0xFE17,
    Ldelem_U1 = 0xFE18,
    Ldelem_I2 = 0xFE19,
    Ldelem_U2 = 0xFE1A,
    Ldelem_I4 = 0xFE1B,
    Ldelem_U4 = 0xFE1C,
    Ldelem_I8 = 0xFE1D,
    Ldelem_R4 = 0xFE1E,
    Ldelem_R8 = 0xFE1F,
    Ldelem_Ref = 0xFE20,
    Stelem_I = 0xFE21,
    Stelem_I1 = 0xFE22,
    Stelem_I2 = 0xFE23,
    Stelem_I4 = 0xFE24,
    Stelem_I8 = 0xFE25,
    Stelem_R4 = 0xFE26,
    Stelem_R8 = 0xFE27,
    Stelem_Ref = 0xFE28,
    Ldelem_I = 0xFE29,
    Conv_R8_Un = 0xFE2A,
    Conv_R4_Un = 0xFE2B,
    Conv_I8_Un = 0xFE2C,
    Conv_I4_Un = 0xFE2D,
    Conv_I2_Un = 0xFE2E,
    Conv_I1_Un = 0xFE2F,
    Calli_Vftable = 0xFE30,
    Jmp_Vftable = 0xFE31,
    Ldvirtftn_Vftable = 0xFE32,
    Callvirt_Vftable = 0xFE33,
    Calli_RegIndirect = 0xFE34,
    Jmp_RegIndirect = 0xFE35,
    Ldvirtftn_RegIndirect = 0xFE36,
    Callvirt_RegIndirect = 0xFE37,
    Initobj = 0xFE38,
    Constrained = 0xFE39,
    Cpblk = 0xFE3A,
    Initblk = 0xFE3B,
    Ldtoken = 0xFE3C,
    Ldarg_0_Virtual = 0xFE3D,
    Ldarg_1_Virtual = 0xFE3E,
    Ldarg_2_Virtual = 0xFE3F,
    Ldarg_3_Virtual = 0xFE40,
    Ldarg_S_Virtual = 0xFE41,
    Ldarga_S_Virtual = 0xFE42,
    Starg_S_Virtual = 0xFE43,
    Ldloc_S_Virtual = 0xFE44,
    Ldloca_S_Virtual = 0xFE45,
    Stloc_S_Virtual = 0xFE46,
    Ldarg_Virtual = 0xFE47,
    Ldarga_Virtual = 0xFE48,
    Starg_Virtual = 0xFE49,
    Ldloc_Virtual = 0xFE4A,
    Ldloca_Virtual = 0xFE4B,
    Stloc_Virtual = 0xFE4C,
    Unbox_Any = 0xFE4D,
    Refanyval = 0xFE4F,
    Ckfinite = 0xFE50,
    Mkrefany = 0xFE51,
    Ldtoken_Method = 0xFE52,
    Ldtoken_Field = 0xFE53,
    Ldtoken_Type = 0xFE54,
    Ldtoken_MemberRef = 0xFE55,
    Ldtoken_MemberDef = 0xFE56,
    Ldtoken_UserString = 0xFE57,
    Ldtoken_MethodSpec = 0xFE58,
    Ldtoken_TypeSpec = 0xFE59,
    Ldtoken_Token = 0xFE5A,
    Throw_Unchecked = 0xFE5B,
    ReThrow = 0xFE5C,
    Sizeof = 0xFE5D,
    Refanytype = 0xFE5E,
    Box_Any = 0xFE5F,
    Unbox_Any_Unchecked = 0xFE60,
    Calli_Unmanaged = 0xFE61,
    Calli_Unmanaged_Vftable = 0xFE62,
    Calli_Unmanaged_RegIndirect = 0xFE63,
    Jmp_Unmanaged = 0xFE64,
    Jmp_Unmanaged_Vftable = 0xFE65,
    Jmp_Unmanaged_RegIndirect = 0xFE66,
    Ldftn_Unmanaged = 0xFE67,
    Ldvirtftn_Unmanaged = 0xFE68,
    Ldvirtftn_Unmanaged_Vftable = 0xFE69,
    Ldvirtftn_Unmanaged_RegIndirect = 0xFE6A,
    Callvirt_Unmanaged = 0xFE6B,
    Callvirt_Unmanaged_Vftable = 0xFE6C,
    Callvirt_Unmanaged_RegIndirect = 0xFE6D,
    Constrained_Unmanaged = 0xFE6E,
    Initobj_Unmanaged = 0xFE6F,
    Cpblk_Unmanaged = 0xFE70,
    Initblk_Unmanaged = 0xFE71,
    Ldtoken_Unmanaged = 0xFE72,
    Ldtoken_Method_Unmanaged = 0xFE73,
    Ldtoken_Field_Unmanaged = 0xFE74,
    Ldtoken_Type_Unmanaged = 0xFE75,
    Ldtoken_MemberRef_Unmanaged = 0xFE76,
    Ldtoken_MemberDef_Unmanaged = 0xFE77,
    Ldtoken_UserString_Unmanaged = 0xFE78,
    Ldtoken_MethodSpec_Unmanaged = 0xFE79,
    Ldtoken_TypeSpec_Unmanaged = 0xFE7A,
    Ldtoken_Token_Unmanaged = 0xFE7B,
    Throw_Unchecked_Unmanaged = 0xFE7C,
    ReThrow_Unmanaged = 0xFE7D,
    Sizeof_Unmanaged = 0xFE7E,
    Refanytype_Unmanaged = 0xFE7F,
    Box_Any_Unmanaged = 0xFE80,
    Unbox_Any_Unchecked_Unmanaged = 0xFE81
}

/// <summary>
///     MSIL 操作数
/// </summary>
public abstract class ClrOperand
{
    /// <summary>
    ///     操作数类型
    /// </summary>
    public abstract ClrOperandKind Kind { get; }
}

/// <summary>
///     操作数类型
/// </summary>
public enum ClrOperandKind
{
    None,
    Int32,
    Int64,
    Float32,
    Float64,
    String,
    Type,
    Method,
    Field,
    Token,
    BranchTarget,
    SwitchTargets,
    LocalIndex,
    ArgumentIndex
}

/// <summary>
///     32 位整数操作数
/// </summary>
public sealed class ClrInt32Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Int32;
    public int Value { get; init; }
}

/// <summary>
///     64 位整数操作数
/// </summary>
public sealed class ClrInt64Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Int64;
    public long Value { get; init; }
}

/// <summary>
///     32 位浮点操作数
/// </summary>
public sealed class ClrFloat32Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Float32;
    public float Value { get; init; }
}

/// <summary>
///     64 位浮点操作数
/// </summary>
public sealed class ClrFloat64Operand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Float64;
    public double Value { get; init; }
}

/// <summary>
///     字符串操作数
/// </summary>
public sealed class ClrStringOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.String;
    public string Value { get; init; }
}

/// <summary>
///     类型操作数
/// </summary>
public sealed class ClrTypeOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Type;
    public ClrType Value { get; init; }
}

/// <summary>
///     方法操作数
/// </summary>
public sealed class ClrMethodOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Method;
    public ClrMethod Value { get; init; }
}

/// <summary>
///     字段操作数
/// </summary>
public sealed class ClrFieldOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Field;
    public ClrField Value { get; init; }
}

/// <summary>
///     元数据令牌操作数
/// </summary>
public sealed class ClrTokenOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.Token;
    public uint Value { get; init; }
}

/// <summary>
///     分支目标操作数
/// </summary>
public sealed class ClrBranchTargetOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.BranchTarget;
    public uint Offset { get; init; }
}

/// <summary>
///     开关目标操作数
/// </summary>
public sealed class ClrSwitchTargetsOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.SwitchTargets;
    public IReadOnlyList<uint> Offsets { get; init; }
}

/// <summary>
///     局部变量索引操作数
/// </summary>
public sealed class ClrLocalIndexOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.LocalIndex;
    public uint Index { get; init; }
}

/// <summary>
///     参数索引操作数
/// </summary>
public sealed class ClrArgumentIndexOperand : ClrOperand
{
    public override ClrOperandKind Kind => ClrOperandKind.ArgumentIndex;
    public uint Index { get; init; }
}

/// <summary>
///     异常处理
/// </summary>
public sealed class ClrExceptionHandler
{
    /// <summary>
    ///     尝试块开始偏移
    /// </summary>
    public uint TryStart { get; init; }

    /// <summary>
    ///     尝试块长度
    /// </summary>
    public uint TryLength { get; init; }

    /// <summary>
    ///     处理块开始偏移
    /// </summary>
    public uint HandlerStart { get; init; }

    /// <summary>
    ///     处理块长度
    /// </summary>
    public uint HandlerLength { get; init; }

    /// <summary>
    ///     异常类型令牌
    /// </summary>
    public uint CatchTypeToken { get; init; }
}

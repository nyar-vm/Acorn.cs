namespace Acorn.Clr.Data;

/// <summary>
///     CLR 二进制格式常量（ECMA-335 标准）。
/// </summary>
public static class ClrConstants
{
    /// <summary>
    ///     CLR 元数据签名（"BSJB" = 0x424A5342）。
    /// </summary>
    public const uint MetadataSignature = 0x424A5342;

    /// <summary>
    ///     CLR 目录头大小（72 字节）。
    /// </summary>
    public const int ClrDirectorySize = 72;

    /// <summary>
    ///     CLR 元数据头最小大小。
    /// </summary>
    public const int MetadataHeaderMinSize = 16;

    /// <summary>
    ///     方法头 Tiny 格式标志（第 0-1 位 = 0x02）。
    /// </summary>
    public const byte MethodHeaderTinyFlag = 0x02;

    /// <summary>
    ///     方法头 Fat 格式标志（第 0-1 位 = 0x03）。
    /// </summary>
    public const byte MethodHeaderFatFlag = 0x03;

    /// <summary>
    ///     方法头格式掩码。
    /// </summary>
    public const byte MethodHeaderFormatMask = 0x03;

    /// <summary>
    ///     Fat 方法头 MoreSects 标志。
    /// </summary>
    public const byte MethodHeaderMoreSects = 0x08;

    /// <summary>
    ///     异常处理表 EHTable 标志。
    /// </summary>
    public const byte ExceptionHandlerTableFlag = 0x01;

    /// <summary>
    ///     异常处理表 Fat 格式标志。
    /// </summary>
    public const byte ExceptionHandlerFatFlag = 0x40;

    /// <summary>
    ///     元数据表最大数量。
    /// </summary>
    public const int TableCount = 64;

    /// <summary>
    ///     #Strings 流名称。
    /// </summary>
    public const string StringsStreamName = "#Strings";

    /// <summary>
    ///     #Blob 流名称。
    /// </summary>
    public const string BlobStreamName = "#Blob";

    /// <summary>
    ///     #GUID 流名称。
    /// </summary>
    public const string GuidStreamName = "#GUID";

    /// <summary>
    ///     #US 流名称（用户字符串堆）。
    /// </summary>
    public const string UserStringStreamName = "#US";

    /// <summary>
    ///     #~ 流名称（表流）。
    /// </summary>
    public const string TableStreamName = "#~";

    /// <summary>
    ///     #- 流名称（未优化的表流）。
    /// </summary>
    public const string UnoptimizedTableStreamName = "#-";
}

/// <summary>
///     CLR 目录标志位。
/// </summary>
public enum ClrDirectoryFlags : uint
{
    /// <summary>
    ///     纯 IL 代码（无本地入口点）。
    /// </summary>
    ILOnly = 0x00000001,

    /// <summary>
    ///     需要 32 位运行时。
    /// </summary>
    Requires32Bit = 0x00000002,

    /// <summary>
    ///     强名称签名。
    /// </summary>
    StrongNameSigned = 0x00000008,

    /// <summary>
    ///     原生入口点。
    /// </summary>
    NativeEntryPoint = 0x00000010,

    /// <summary>
    ///     可追踪调试信息。
    /// </summary>
    TrackDebugData = 0x00010000
}

/// <summary>
///     元数据表堆偏移大小标志（HeapSizes 字段的位掩码）。
/// </summary>
public enum ClrHeapSizeFlags : byte
{
    /// <summary>
    ///     #Strings 堆索引使用 4 字节（否则 2 字节）。
    /// </summary>
    StringHeapLarge = 0x01,

    /// <summary>
    ///     #GUID 堆索引使用 4 字节（否则 2 字节）。
    /// </summary>
    GuidHeapLarge = 0x02,

    /// <summary>
    ///     #Blob 堆索引使用 4 字节（否则 2 字节）。
    /// </summary>
    BlobHeapLarge = 0x04
}

/// <summary>
///     方法访问标志（MethodAttributes，ECMA-335 §23.1.10）。
/// </summary>
[Flags]
public enum ClrMethodAttributes : ushort
{
    MemberAccessMask = 0x0007,
    Private = 0x0001,
    FamANDAssem = 0x0002,
    Assembly = 0x0003,
    Family = 0x0004,
    FamORAssem = 0x0005,
    Public = 0x0006,
    Static = 0x0010,
    Final = 0x0020,
    Virtual = 0x0040,
    HideBySig = 0x0080,
    VtableLayoutMask = 0x0100,
    ReuseSlot = 0x0000,
    NewSlot = 0x0100,
    Abstract = 0x0400,
    SpecialName = 0x0800,
    PInvokeImpl = 0x2000,
    UnmanagedExport = 0x0008,
    RTSpecialName = 0x1000,
    HasSecurity = 0x4000,
    RequireSecObject = 0x8000
}

/// <summary>
///     类型访问标志（TypeAttributes，ECMA-335 §23.1.14）。
/// </summary>
[Flags]
public enum ClrTypeAttributes : uint
{
    VisibilityMask = 0x00000007,
    NotPublic = 0x00000000,
    Public = 0x00000001,
    NestedPublic = 0x00000002,
    NestedPrivate = 0x00000003,
    NestedFamily = 0x00000004,
    NestedAssembly = 0x00000005,
    NestedFamANDAssem = 0x00000006,
    NestedFamORAssem = 0x00000007,
    SequentialLayout = 0x00000008,
    ExplicitLayout = 0x00000010,
    Interface = 0x00000020,
    Abstract = 0x00000080,
    Sealed = 0x00000100,
    SpecialName = 0x00000400,
    RTSpecialName = 0x00000800,
    Import = 0x00001000,
    Serializable = 0x00002000,
    StringFormatMask = 0x00030000,
    AnsiClass = 0x00000000,
    UnicodeClass = 0x00010000,
    AutoClass = 0x00020000,
    CustomFormatClass = 0x00030000,
    BeforeFieldInit = 0x00100000,
    HasSecurity = 0x00040000
}

/// <summary>
///     字段访问标志（FieldAttributes，ECMA-335 §23.1.5）。
/// </summary>
[Flags]
public enum ClrFieldAttributes : ushort
{
    FieldAccessMask = 0x0007,
    Private = 0x0001,
    FamANDAssem = 0x0002,
    Assembly = 0x0003,
    Family = 0x0004,
    FamORAssem = 0x0005,
    Public = 0x0006,
    Static = 0x0010,
    InitOnly = 0x0020,
    Literal = 0x0040,
    NotSerialized = 0x0080,
    SpecialName = 0x0200,
    PInvokeImpl = 0x2000,
    RTSpecialName = 0x0400,
    HasFieldMarshal = 0x1000,
    HasDefault = 0x8000,
    HasFieldRVA = 0x0100
}

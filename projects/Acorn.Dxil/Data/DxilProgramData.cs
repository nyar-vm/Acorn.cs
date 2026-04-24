using Acorn;
using Acorn.Attributes;
using Acorn.Codec;

namespace Acorn.Dxil.Data;

/// <summary>
///     DXIL 程序头数据（24 字节）。
/// </summary>
/// <remarks>
///     DXIL 程序头位于 DXIL Part 数据的起始位置，
///     包含着色器模型类型、DXIL 版本和 LLVM Bitcode 的偏移与大小。
/// </remarks>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct DxilProgramHeader
{
    /// <summary>
    ///     DXIL 主版本号。
    /// </summary>
    [Field(Order = 0)]
    public byte MajorVersion;

    /// <summary>
    ///     DXIL 次版本号。
    /// </summary>
    [Field(Order = 1)]
    public byte MinorVersion;

    /// <summary>
    ///     着色器模型类型（<see cref="DxilShaderModelKind" /> 枚举值）。
    /// </summary>
    [Field(Order = 2)]
    public byte ShaderModelKindRaw;

    /// <summary>
    ///     着色器模型类型。
    /// </summary>
    public DxilShaderModelKind ShaderModelKind
    {
        get => (DxilShaderModelKind)ShaderModelKindRaw;
        init => ShaderModelKindRaw = (byte)value;
    }

    /// <summary>
    ///     对齐填充字节。
    /// </summary>
    [Field(Order = 3)]
    public byte Padding;

    /// <summary>
    ///     DXIL 数据总大小（字节，含 ProgramHeader）。
    /// </summary>
    [Field(Order = 4)]
    public uint Size;

    /// <summary>
    ///     LLVM Bitcode 相对于 DXIL Part 数据起始位置的偏移。
    /// </summary>
    [Field(Order = 5)]
    public uint BitcodeOffset;

    /// <summary>
    ///     LLVM Bitcode 大小（字节）。
    /// </summary>
    [Field(Order = 6)]
    public uint BitcodeSize;
}

/// <summary>
///     DXIL 着色器特征标志。
/// </summary>
[Flags]
public enum DxilShaderFlags : ulong
{
    None = 0,

    /// <summary>
    ///     禁用着色器优化。
    /// </summary>
    DisableOptimizations = 1 << 0,

    /// <summary>
    ///     禁用数学重构。
    /// </summary>
    DisableMathRefactoring = 1 << 1,

    /// <summary>
    ///     着色器使用双精度浮点。
    /// </summary>
    UsesDoubles = 1 << 2,

    /// <summary>
    ///     强制早期深度模板测试。
    /// </summary>
    ForceEarlyDepthStencil = 1 << 3,

    /// <summary>
    ///     启用原始和结构化缓冲区。
    /// </summary>
    EnableRawAndStructuredBuffers = 1 << 4,

    /// <summary>
    ///     着色器使用半精度。
    /// </summary>
    UsesMinPrecision = 1 << 5,

    /// <summary>
    ///     着色器使用双精度扩展内联函数。
    /// </summary>
    UsesDoubleExtensions = 1 << 6,

    /// <summary>
    ///     着色器使用 MSAD。
    /// </summary>
    UsesMSAD = 1 << 7,

    /// <summary>
    ///     所有资源必须在着色器执行期间绑定。
    /// </summary>
    AllResourcesBound = 1 << 8,

    /// <summary>
    ///     着色器使用 Wave 操作内联函数。
    /// </summary>
    UsesWaveOps = 1 << 19,

    /// <summary>
    ///     着色器使用 int64 指令。
    /// </summary>
    UsesInt64 = 1 << 20
}

/// <summary>
///     DXIL 着色器哈希数据（20 字节）。
/// </summary>
public sealed class DxilShaderHash
{
    /// <summary>
    ///     哈希标志（0 = 包含源码信息，1 = 不包含）。
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     SHA-1 哈希值（20 字节）。
    /// </summary>
    public byte[] Digest { get; init; } = new byte[20];
}

/// <summary>
///     DXIL 着色器签名元素数据。
/// </summary>
public sealed class DxilSignatureElement
{
    /// <summary>
    ///     元素唯一 ID。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     语义名称。
    /// </summary>
    public string SemanticName { get; init; } = string.Empty;

    /// <summary>
    ///     组件类型。
    /// </summary>
    public DxilComponentType ComponentType { get; init; }

    /// <summary>
    ///     语义种类。
    /// </summary>
    public byte SemanticKind { get; init; }

    /// <summary>
    ///     语义索引列表。
    /// </summary>
    public IReadOnlyList<int> SemanticIndices { get; init; } = [];

    /// <summary>
    ///     插值模式。
    /// </summary>
    public DxilInterpolationMode InterpolationMode { get; init; }

    /// <summary>
    ///     行数。
    /// </summary>
    public int Rows { get; init; }

    /// <summary>
    ///     列数。
    /// </summary>
    public byte Columns { get; init; }

    /// <summary>
    ///     起始行。
    /// </summary>
    public int StartRow { get; init; }

    /// <summary>
    ///     起始列。
    /// </summary>
    public byte StartColumn { get; init; }
}

/// <summary>
///     DXIL 着色器签名数据。
/// </summary>
public sealed class DxilSignature
{
    /// <summary>
    ///     签名元素列表。
    /// </summary>
    public IReadOnlyList<DxilSignatureElement> Elements { get; init; } = [];
}

/// <summary>
///     DXIL 资源记录数据。
/// </summary>
public sealed class DxilResourceRecord
{
    /// <summary>
    ///     资源唯一 ID。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     资源名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     资源类。
    /// </summary>
    public DxilResourceClass ResourceClass { get; init; }

    /// <summary>
    ///     资源种类。
    /// </summary>
    public DxilResourceKind ResourceKind { get; init; }

    /// <summary>
    ///     绑定空间。
    /// </summary>
    public int SpaceId { get; init; }

    /// <summary>
    ///     绑定下界。
    /// </summary>
    public int LowerBound { get; init; }

    /// <summary>
    ///     绑定范围大小。
    /// </summary>
    public int RangeSize { get; init; }
}

/// <summary>
///     DXIL 入口点数据。
/// </summary>
public sealed class DxilEntryPoint
{
    /// <summary>
    ///     入口点名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     着色器模型类型。
    /// </summary>
    public DxilShaderModelKind ShaderModel { get; init; }

    /// <summary>
    ///     输入签名。
    /// </summary>
    public DxilSignature? InputSignature { get; init; }

    /// <summary>
    ///     输出签名。
    /// </summary>
    public DxilSignature? OutputSignature { get; init; }

    /// <summary>
    ///     补丁常量签名。
    /// </summary>
    public DxilSignature? PatchConstantSignature { get; init; }

    /// <summary>
    ///     SRV 资源列表。
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> SRVs { get; init; } = [];

    /// <summary>
    ///     UAV 资源列表。
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> UAVs { get; init; } = [];

    /// <summary>
    ///     CBV 资源列表。
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> CBVs { get; init; } = [];

    /// <summary>
    ///     采样器列表。
    /// </summary>
    public IReadOnlyList<DxilResourceRecord> Samplers { get; init; } = [];
}

/// <summary>
///     DXIL 程序完整数据。
/// </summary>
public sealed class DxilProgramData
{
    /// <summary>
    ///     程序头。
    /// </summary>
    public DxilProgramHeader Header { get; init; }

    /// <summary>
    ///     着色器特征标志。
    /// </summary>
    public DxilShaderFlags ShaderFlags { get; init; }

    /// <summary>
    ///     入口点列表。
    /// </summary>
    public IReadOnlyList<DxilEntryPoint> EntryPoints { get; init; } = [];

    /// <summary>
    ///     LLVM Bitcode 原始数据。
    /// </summary>
    public byte[] BitcodeData { get; init; } = [];
}

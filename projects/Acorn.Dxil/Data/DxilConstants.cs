namespace Acorn.Dxil.Data;

/// <summary>
///     DXIL/DXContainer 格式常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 Microsoft DXIL 规范和 DXContainer 格式规范，
///     Acorn 独占二进制编解码职责。
/// </remarks>
public static class DxilConstants
{
    /// <summary>
    ///     DXContainer 魔数（"DXBC" 小端序 = 0x44434247 → 实际字节序为 44 58 42 43）。
    /// </summary>
    /// <remarks>
    ///     注意：DXBC 的实际字节序为 D X B C（0x44 0x58 0x42 0x43），
    ///     小端序读取后为 0x43425844。此处使用小端序读取后的 uint 值。
    /// </remarks>
    public const uint ContainerMagicNumber = 0x43425844u;

    /// <summary>
    ///     DXContainer 格式主版本号。
    /// </summary>
    public const ushort ContainerVersionMajor = 1;

    /// <summary>
    ///     DXContainer 格式次版本号。
    /// </summary>
    public const ushort ContainerVersionMinor = 0;

    /// <summary>
    ///     DXIL 1.0 主版本号。
    /// </summary>
    public const byte DxilVersion10Major = 1;

    /// <summary>
    ///     DXIL 1.0 次版本号。
    /// </summary>
    public const byte DxilVersion10Minor = 0;

    /// <summary>
    ///     DXIL 1.1 主版本号。
    /// </summary>
    public const byte DxilVersion11Major = 1;

    /// <summary>
    ///     DXIL 1.1 次版本号。
    /// </summary>
    public const byte DxilVersion11Minor = 1;

    /// <summary>
    ///     DXIL 1.2 主版本号。
    /// </summary>
    public const byte DxilVersion12Major = 1;

    /// <summary>
    ///     DXIL 1.2 次版本号。
    /// </summary>
    public const byte DxilVersion12Minor = 2;

    /// <summary>
    ///     DXIL Program Header 大小（24 字节）。
    /// </summary>
    public const int ProgramHeaderSize = 24;

    /// <summary>
    ///     Gnosis 生成器标识。
    /// </summary>
    public const uint GeneratorMagicNumber = 0x00470000;
}

/// <summary>
///     DXContainer Part FourCC 标识枚举。
/// </summary>
public enum DxilPartFourCC : uint
{
    /// <summary>
    ///     着色器特征标志。
    /// </summary>
    FeatureInfo = 0x30494653u,

    /// <summary>
    ///     着色器哈希。
    /// </summary>
    Hash = 0x48534148u,

    /// <summary>
    ///     DXIL 着色器程序。
    /// </summary>
    Dxil = 0x4C495844u,

    /// <summary>
    ///     调试信息 DXIL。
    /// </summary>
    DebugInfoDxil = 0x42444C49u,

    /// <summary>
    ///     管线状态验证。
    /// </summary>
    PipelineStateValidation = 0x30565350u,

    /// <summary>
    ///     运行时数据。
    /// </summary>
    RuntimeData = 0x54414452u,

    /// <summary>
    ///     着色器统计信息。
    /// </summary>
    Statistics = 0x54415453u,

    /// <summary>
    ///     着色器调试名称。
    /// </summary>
    DebugName = 0x4E4D4424u,

    /// <summary>
    ///     根签名。
    /// </summary>
    RootSignature = 0x54534F52u,

    /// <summary>
    ///     DXIL 1.x 着色器程序。
    /// </summary>
    Dxil1 = 0x314C4958u
}

/// <summary>
///     DXIL 着色器模型类型枚举。
/// </summary>
public enum DxilShaderModelKind : byte
{
    /// <summary>
    ///     顶点着色器。
    /// </summary>
    Vertex = 0,

    /// <summary>
    ///     像素着色器。
    /// </summary>
    Pixel = 1,

    /// <summary>
    ///     几何着色器。
    /// </summary>
    Geometry = 2,

    /// <summary>
    ///     外壳着色器。
    /// </summary>
    Hull = 3,

    /// <summary>
    ///     域着色器。
    /// </summary>
    Domain = 4,

    /// <summary>
    ///     计算着色器。
    /// </summary>
    Compute = 5,

    /// <summary>
    ///     库。
    /// </summary>
    Library = 6,

    /// <summary>
    ///     光线生成着色器。
    /// </summary>
    RayGeneration = 7,

    /// <summary>
    ///     相交着色器。
    /// </summary>
    Intersection = 8,

    /// <summary>
    ///     任意命中着色器。
    /// </summary>
    AnyHit = 9,

    /// <summary>
    ///     最近命中着色器。
    /// </summary>
    ClosestHit = 10,

    /// <summary>
    ///     未命中着色器。
    /// </summary>
    Miss = 11,

    /// <summary>
    ///     可调用着色器。
    /// </summary>
    Callable = 12,

    /// <summary>
    ///     网格着色器。
    /// </summary>
    Mesh = 13,

    /// <summary>
    ///     放大着色器。
    /// </summary>
    Amplification = 14
}

/// <summary>
///     DXIL 操作码枚举（CoreOps）。
/// </summary>
/// <remarks>
///     操作码值来自 Microsoft DXIL 规范，用于 dx.op.* 外部函数调用的第一个参数。
/// </remarks>
public enum DxilOpCode : int
{
    TempRegLoad = 0,
    TempRegStore = 1,
    MinPrecXRegLoad = 2,
    MinPrecXRegStore = 3,
    LoadInput = 4,
    StoreOutput = 5,
    FAbs = 6,
    Saturate = 7,
    IsNaN = 8,
    IsInf = 9,
    IsFinite = 10,
    IsNormal = 11,
    Cos = 12,
    Sin = 13,
    Tan = 14,
    Acos = 15,
    Asin = 16,
    Atan = 17,
    Hcos = 18,
    Hsin = 19,
    Htan = 20,
    Exp = 21,
    Frc = 22,
    Log = 23,
    Sqrt = 24,
    Rsqrt = 25,
    Round_ne = 26,
    Round_ni = 27,
    Round_pi = 28,
    Round_z = 29,
    Bfrev = 30,
    Countbits = 31,
    FirstbitLo = 32,
    FirstbitHi = 33,
    FirstbitSHi = 34,
    FMax = 35,
    FMin = 36,
    IMax = 37,
    IMin = 38,
    UMax = 39,
    UMin = 40,
    IMul = 41,
    UMul = 42,
    UDiv = 43,
    UAddc = 44,
    USubb = 45,
    FMad = 46,
    Fma = 47,
    IMad = 48,
    UMad = 49,
    Msad = 50,
    Ibfe = 51,
    Ubfe = 52,
    Bfi = 53,
    Dot2 = 54,
    Dot3 = 55,
    Dot4 = 56,
    CreateHandle = 57,
    CBufferLoad = 58,
    CBufferLoadLegacy = 59,
    Sample = 60,
    SampleBias = 61,
    SampleLevel = 62,
    SampleGrad = 63,
    SampleCmp = 64,
    SampleCmpLevelZero = 65,
    TextureLoad = 66,
    TextureStore = 67,
    BufferLoad = 68,
    BufferStore = 69,
    BufferUpdateCounter = 70,
    CheckAccessFullyMapped = 71,
    GetDimensions = 72,
    TextureGather = 73,
    TextureGatherCmp = 74,
    Texture2DMSGetSamplePosition = 75,
    RenderTargetGetSamplePosition = 76,
    RenderTargetGetSampleCount = 77,
    AtomicBinOp = 78,
    AtomicCompareExchange = 79,
    Barrier = 80,
    CalculateLOD = 81,
    Discard = 82,
    DerivCoarseX = 83,
    DerivCoarseY = 84,
    DerivFineX = 85,
    DerivFineY = 86,
    EvalSnapped = 87,
    EvalSampleIndex = 88,
    EvalCentroid = 89,
    SampleIndex = 90,
    Coverage = 91,
    InnerCoverage = 92,
    ThreadId = 93,
    GroupId = 94,
    ThreadIdInGroup = 95,
    FlattenedThreadIdInGroup = 96,
    EmitStream = 97,
    CutStream = 98,
    EmitThenCutStream = 99,
    GSInstanceID = 100,
    MakeDouble = 101,
    SplitDouble = 102,
    LoadOutputControlPoint = 103,
    LoadPatchConstant = 104,
    DomainLocation = 105,
    StorePatchConstant = 106,
    OutputControlPointID = 107,
    PrimitiveID = 108,
    CycleCounterLegacy = 109,
    WaveIsFirstLane = 110,
    WaveGetLaneIndex = 111,
    WaveGetLaneCount = 112,
    WaveAnyTrue = 113,
    WaveAllTrue = 114,
    WaveActiveAllEqual = 115,
    WaveActiveBallot = 116,
    WaveReadLaneAt = 117,
    WaveReadLaneFirst = 118,
    WaveActiveOp = 119,
    WaveActiveBit = 120,
    WavePrefixOp = 121,
    QuadReadLaneAt = 122,
    QuadOp = 123,
    BitcastI16toF16 = 124,
    BitcastF16toI16 = 125,
    BitcastI32toF32 = 126,
    BitcastF32toI32 = 127,
    BitcastI64toF64 = 128,
    BitcastF64toI64 = 129,
    LegacyF32ToF16 = 130,
    LegacyF16ToF32 = 131,
    LegacyDoubleToFloat = 132,
    LegacyDoubleToSInt32 = 133,
    LegacyDoubleToUInt32 = 134,
    WaveAllBitCount = 135,
    WavePrefixBitCount = 136,
    AttributeAtVertex = 137,
    ViewID = 138,
    RawBufferLoad = 139,
    RawBufferStore = 140,
    InstanceID = 141,
    InstanceIndex = 142,
    HitKind = 143,
    RayFlags = 144,
    DispatchRaysIndex = 145,
    DispatchRaysDimensions = 146,
    WorldRayOrigin = 147,
    WorldRayDirection = 148,
    ObjectRayOrigin = 149,
    ObjectRayDirection = 150,
    ObjectToWorld = 151,
    WorldToObject = 152,
    RayTMin = 153,
    RayTCurrent = 154,
    IgnoreHit = 155,
    AcceptHitAndEndSearch = 156,
    TraceRay = 157,
    ReportHit = 158,
    CallShader = 159,
    CreateHandleForLib = 160,
    PrimitiveIndex = 161,
    Dot2AddHalf = 162,
    Dot4AddI8Packed = 163,
    Dot4AddU8Packed = 164,
    WaveMatch = 165,
    WaveMultiPrefixOp = 166,
    WaveMultiPrefixBitCount = 167,
    SetMeshOutputCounts = 168,
    EmitIndices = 169,
    GetMeshPayload = 170,
    StoreVertexOutput = 171,
    StorePrimitiveOutput = 172,
    DispatchMesh = 173,
    WriteSamplerFeedback = 174,
    WriteSamplerFeedbackBias = 175,
    WriteSamplerFeedbackLevel = 176,
    WriteSamplerFeedbackGrad = 177,
    AllocateRayQuery = 178,
    RayQuery_TraceRayInline = 179,
    RayQuery_Proceed = 180,
    RayQuery_Abort = 181,
    RayQuery_CommitNonOpaqueTriangleHit = 182,
    RayQuery_CommitProceduralPrimitiveHit = 183,
    RayQuery_CommittedStatus = 184,
    RayQuery_CandidateType = 185,
    BarrierByMemoryType = 244,
    BarrierByMemoryHandle = 245,
    AnnotateHandle = 216,
    CreateHandleFromBinding = 217,
    CreateHandleFromHeap = 218,
    IsHelperLane = 221,
    SampleCmpLevel = 224,
    RawBufferVectorLoad = 303,
    RawBufferVectorStore = 304,
    FDot = 311
}

/// <summary>
///     DXIL 组件类型枚举。
/// </summary>
public enum DxilComponentType : byte
{
    Invalid = 0,
    I1 = 1,
    I16 = 2,
    U16 = 3,
    I32 = 4,
    U32 = 5,
    I64 = 6,
    U64 = 7,
    F16 = 8,
    F32 = 9,
    F64 = 10,
    SNormF16 = 11,
    UNormF16 = 12,
    SNormF32 = 13,
    UNormF32 = 14,
    SNormF64 = 15,
    UNormF64 = 16
}

/// <summary>
///     DXIL 资源类型枚举。
/// </summary>
public enum DxilResourceKind : byte
{
    Invalid = 0,
    Texture1D = 1,
    Texture2D = 2,
    Texture2DMS = 3,
    Texture3D = 4,
    TextureCube = 5,
    Texture1DArray = 6,
    Texture2DArray = 7,
    Texture2DMSArray = 8,
    TextureCubeArray = 9,
    TypedBuffer = 10,
    RawBuffer = 11,
    StructuredBuffer = 12,
    CBuffer = 13,
    Sampler = 14,
    TBuffer = 15,
    RTAccelerationStructure = 16,
    FeedbackTexture2D = 17,
    FeedbackTexture2DArray = 18
}

/// <summary>
///     DXIL 资源类枚举。
/// </summary>
public enum DxilResourceClass : byte
{
    SRV = 0,
    UAV = 1,
    CBV = 2,
    Sampler = 3
}

/// <summary>
///     DXIL 插值模式枚举。
/// </summary>
public enum DxilInterpolationMode : byte
{
    Undefined = 0,
    Constant = 1,
    Linear = 2,
    LinearCentroid = 3,
    LinearNoperspective = 4,
    LinearNoperspectiveCentroid = 5,
    LinearSample = 6,
    LinearNoperspectiveSample = 7,
    NoInterpolation = 9
}

/// <summary>
///     DXIL 地址空间枚举。
/// </summary>
public enum DxilAddressSpace : uint
{
    Default = 0,
    DeviceMemory = 1,
    CBuffer = 2,
    GroupShared = 3,
    Generic = 4,
    NodeRecord = 6
}

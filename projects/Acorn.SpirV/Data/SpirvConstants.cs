namespace Acorn.Spirv.Data;

/// <summary>
///     SPIR-V 格式常量，包含魔数、版本号等标量常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 Khronos SPIR-V 规范，Acorn 独占二进制编解码职责。
///     枚举类常量（Capability、ExecutionModel 等）已迁移为独立的 enum 类型。
/// </remarks>
public static class SpirvConstants
{
    /// <summary>
    ///     SPIR-V 魔数（0x07230203）。
    /// </summary>
    public const uint MagicNumber = 0x07230203;

    /// <summary>
    ///     SPIR-V 1.0 版本号。
    /// </summary>
    public const uint Version10 = 0x00010000;

    /// <summary>
    ///     SPIR-V 1.1 版本号。
    /// </summary>
    public const uint Version11 = 0x00010100;

    /// <summary>
    ///     SPIR-V 1.2 版本号。
    /// </summary>
    public const uint Version12 = 0x00010200;

    /// <summary>
    ///     SPIR-V 1.3 版本号。
    /// </summary>
    public const uint Version13 = 0x00010300;

    /// <summary>
    ///     SPIR-V 1.4 版本号。
    /// </summary>
    public const uint Version14 = 0x00010400;

    /// <summary>
    ///     SPIR-V 1.5 版本号。
    /// </summary>
    public const uint Version15 = 0x00010500;

    /// <summary>
    ///     Gnosis 生成器魔数。
    /// </summary>
    public const uint GeneratorMagicNumber = 0x00470000;

    /// <summary>
    ///     默认 Schema 值。
    /// </summary>
    public const uint Schema = 0;
}

/// <summary>
///     SPIR-V Capability 枚举。
/// </summary>
public enum SpirvCapability : uint
{
    Matrix = 0,
    Shader = 1,
    Geometry = 2,
    Tessellation = 3,
    Addresses = 4,
    Linkage = 5,
    Kernel = 6,
    Vector16 = 7,
    Float16Buffer = 8,
    Float16 = 9,
    Float64 = 10,
    Int64 = 11,
    Int64Atomics = 12,
    ImageBasic = 13,
    ImageReadWrite = 14,
    ImageMipmap = 15,
    Pipes = 17,
    Groups = 18,
    DeviceEnqueue = 19,
    LiteralSampler = 20,
    AtomicStorage = 21,
    Int16 = 22,
    TessellationPointSize = 23,
    GeometryPointSize = 24,
    ImageGatherExtended = 25,
    StorageImageMultisample = 27,
    UniformBufferArrayDynamicIndexing = 28,
    SampledImageArrayDynamicIndexing = 29,
    StorageBufferArrayDynamicIndexing = 30,
    StorageImageArrayDynamicIndexing = 31,
    ClipDistance = 32,
    CullDistance = 33,
    ImageCubeArray = 34,
    SampleRateShading = 35,
    ImageRect = 36,
    SampledRect = 37,
    GenericPointer = 38,
    Int8 = 39,
    InputAttachment = 40,
    SparseResidency = 41,
    MinLod = 42,
    Sampled1D = 43,
    Image1D = 44,
    SampledCubeArray = 45,
    SampledBuffer = 46,
    ImageBuffer = 47,
    ImageMSArray = 48,
    StorageImageExtendedFormats = 49,
    ImageQuery = 50,
    DerivativeControl = 51,
    InterpolationFunction = 52,
    TransformFeedback = 53,
    GeometryStreams = 54,
    StorageImageReadWithoutFormat = 55,
    StorageImageWriteWithoutFormat = 56,
    MultiViewport = 57,
    SubgroupDispatch = 58,
    NamedBarrier = 59,
    MeshShadingNV = 60,
    /// <summary>
    ///     GLSL 样式的按组件插值。
    /// </summary>
    FragmentBarycentricKHR = 4484,
    /// <summary>
    ///     物理存储缓冲区 64 位地址。
    /// </summary>
    PhysicalStorageBufferAddresses = 5347,
    /// <summary>
    ///     协同矩阵运算。
    /// </summary>
    CooperativeMatrixKHR = 5366,
    /// <summary>
    ///     Mesh Shading 扩展。
    /// </summary>
    MeshShadingEXT = 5368,
    SubgroupBallotKHR = 4423,
    DrawParameters = 4427,
    SubgroupVoteKHR = 4431,
    StorageBuffer16BitAccess = 4433,
    StoragePushConstant16 = 4435,
    StorageInputOutput16 = 4436,
    DeviceGroup = 4437,
    MultiView = 4439,
    VariablePointersStorageBuffer = 4441,
    VariablePointers = 4442,
    FragmentDensityEXT = 4444,
    ShaderNonUniformEXT = 4446,
    RuntimeDescriptorArrayEXT = 4447,
    RayTracingKHR = 4479,
    RayQueryKHR = 4472,
    VulkanMemoryModel = 4434,
    /// <summary>
    ///     着色器时钟（用于性能测量）。
    /// </summary>
    ShaderClockKHR = 5068,
    /// <summary>
    ///     片段着色器交错执行。
    /// </summary>
    FragmentShaderInterlockEXT = 5363,
    /// <summary>
    ///     按片段着色率。
    /// </summary>
    FragmentShadingRateKHR = 5408
}

/// <summary>
///     SPIR-V ExecutionModel 枚举。
/// </summary>
public enum SpirvExecutionModel : uint
{
    Vertex = 0,
    TessellationControl = 1,
    TessellationEvaluation = 2,
    Geometry = 3,
    Fragment = 4,
    GLCompute = 5,
    Kernel = 6,
    RayGenerationKHR = 5313,
    IntersectionKHR = 5314,
    AnyHitKHR = 5315,
    ClosestHitKHR = 5316,
    MissKHR = 5317
}

/// <summary>
///     SPIR-V ExecutionMode 枚举。
/// </summary>
public enum SpirvExecutionMode : uint
{
    Invocations = 0,
    SpacingEqual = 1,
    SpacingFractionalEven = 2,
    SpacingFractionalOdd = 3,
    VertexOrderCw = 4,
    VertexOrderCcw = 5,
    PixelCenterInteger = 6,
    OriginUpperLeft = 7,
    OriginLowerLeft = 8,
    EarlyFragmentTests = 9,
    PointMode = 10,
    Xfb = 11,
    DepthReplacing = 12,
    DepthGreater = 14,
    DepthLess = 15,
    DepthUnchanged = 16,
    LocalSize = 17,
    LocalSizeHint = 18,
    InputPoints = 19,
    InputLines = 20,
    InputLinesAdjacency = 21,
    Triangles = 22,
    InputTrianglesAdjacency = 23,
    Quads = 24,
    Isolines = 25,
    OutputVertices = 26,
    OutputPoints = 27,
    OutputLineStrip = 28,
    OutputTriangleStrip = 29,
    /// <summary>
    ///     VecTypeHint 整数类型提示。
    /// </summary>
    VecTypeHint = 30,
    /// <summary>
    ///     连续线输出。
    /// </summary>
    ContractionOff = 31,
    /// <summary>
    ///     后段深度覆盖。
    /// </summary>
    PostDepthCoverage = 4446,
    DenormPreserve = 4459,
    DenormFlushToZero = 4460,
    SignedZeroInfNanPreserve = 4461,
    RoundingModeRTE = 4462,
    RoundingModeRTZ = 4463,
    StencilRefReplacingEXT = 5101,
    OutputLinesEXT = 5195,
    OutputPrimitivesEXT = 5196,
    LocalSizeId = 5197,
    LocalSizeHintId = 5198,
    SubgroupUniformControlFlowKHR = 5021,
    SubgroupSize = 5027,
    SubgroupsPerWorkgroup = 5028,
    SubgroupsPerWorkgroupId = 5029
}

/// <summary>
///     SPIR-V StorageClass 枚举。
/// </summary>
public enum SpirvStorageClass : uint
{
    UniformConstant = 0,
    Input = 1,
    Uniform = 2,
    Output = 3,
    Workgroup = 4,
    CrossWorkgroup = 5,
    Private = 6,
    Function = 7,
    Generic = 8,
    PushConstant = 9,
    AtomicCounter = 10,
    Image = 11,
    StorageBuffer = 12,
    RayPayloadKHR = 33,
    HitAttributeKHR = 34,
    IncomingRayPayloadKHR = 35,
    ShaderRecordBufferKHR = 36
}

/// <summary>
///     SPIR-V Decoration 枚举。
/// </summary>
public enum SpirvDecoration : uint
{
    Block = 2,
    BufferBlock = 3,
    RowMajor = 4,
    ColMajor = 5,
    ArrayStride = 6,
    MatrixStride = 7,
    GLSLShared = 8,
    GLSLPacked = 9,
    CPacked = 10,
    BuiltIn = 11,
    NoPerspective = 13,
    Flat = 14,
    Patch = 15,
    Centroid = 16,
    Sample = 17,
    Invariant = 18,
    Restrict = 19,
    Aliased = 20,
    Volatile = 21,
    Constant = 22,
    Coherent = 23,
    NonWritable = 24,
    NonReadable = 25,
    Uniform = 26,
    UniformId = 27,
    SaturatedConversion = 28,
    Stream = 29,
    Location = 30,
    Component = 31,
    Index = 32,
    Binding = 33,
    DescriptorSet = 34,
    Offset = 35,
    InputAttachmentIndex = 40,
    SpecId = 41,
    NonUniformEXT = 5300,
    PerVertexKHR = 5285,
    PerPrimitiveNV = 5271,
    PerViewNV = 5270,
    PerTaskNV = 5273,
    OverrideCoverageNV = 5248,
    PassthroughNV = 5250,
    ViewportRelativeNV = 5252,
    FullyCoveredEXT = 5258,
    FPFastMathMode = 6085,
    LinkageAttributes = 6086,
    UserSemantic = 6082,
    CounterBuffer = 5634
}

/// <summary>
///     SPIR-V BuiltIn 枚举。
/// </summary>
public enum SpirvBuiltIn : uint
{
    Position = 0,
    PointSize = 1,
    ClipDistance = 3,
    CullDistance = 4,
    VertexId = 5,
    InstanceId = 6,
    PrimitiveId = 7,
    InvocationId = 8,
    Layer = 9,
    ViewportIndex = 10,
    TessLevelOuter = 11,
    TessLevelInner = 12,
    TessCoord = 13,
    PatchVertices = 14,
    FragCoord = 15,
    PointCoord = 16,
    FrontFacing = 17,
    SampleId = 18,
    SamplePosition = 19,
    SampleMask = 20,
    FragDepth = 22,
    HelperInvocation = 23,
    NumWorkgroups = 24,
    WorkgroupSize = 25,
    WorkgroupId = 26,
    LocalInvocationId = 27,
    GlobalInvocationId = 28,
    LocalInvocationIndex = 29,
    WorkDim = 30,
    GlobalSize = 31,
    EnqueuedWorkgroupSize = 32,
    GlobalOffset = 33,
    GlobalLinearId = 34,
    SubgroupSize = 36,
    SubgroupMaxSize = 37,
    NumSubgroups = 38,
    NumEnqueuedSubgroups = 39,
    SubgroupId = 40,
    SubgroupLocalInvocationId = 41,
    VertexIndex = 42,
    InstanceIndex = 43,
    LaunchIdKHR = 5319,
    LaunchSizeKHR = 5320,
    WorldRayOriginKHR = 5321,
    WorldRayDirectionKHR = 5322,
    ObjectRayOriginKHR = 5323,
    ObjectRayDirectionKHR = 5324,
    RayTminKHR = 5325,
    RayTmaxKHR = 5326,
    InstanceCustomIndexKHR = 5327,
    ObjectToWorldKHR = 5330,
    WorldToObjectKHR = 5331,
    HitTKHR = 5332,
    HitKindKHR = 5333,
    SubgroupEqMaskKHR = 4416,
    SubgroupGeMaskKHR = 4417,
    SubgroupGtMaskKHR = 4418,
    SubgroupLeMaskKHR = 4419,
    SubgroupLtMaskKHR = 4420,
    DeviceIndex = 4438,
    ViewIndex = 4440,
    FragStencilRefEXT = 5014,
    FullyCoveredEXT = 5257,
    PrimitiveShadingRateKHR = 5328,
    ShadingRateKHR = 5329,
    BaryCoordNoPerspAMD = 4992
}

/// <summary>
///     SPIR-V AddressingModel 枚举。
/// </summary>
public enum SpirvAddressingModel : uint
{
    Logical = 0,
    Physical32 = 1,
    Physical64 = 2
}

/// <summary>
///     SPIR-V MemoryModel 枚举。
/// </summary>
public enum SpirvMemoryModel : uint
{
    Simple = 0,
    GLSL450 = 1,
    OpenCL = 2,
    Vulkan = 3
}

/// <summary>
///     SPIR-V ImageDim 枚举。
/// </summary>
public enum SpirvImageDim : uint
{
    Dim1D = 0,
    Dim2D = 1,
    Dim3D = 2,
    DimCube = 3,
    DimRect = 4,
    DimBuffer = 5,
    DimSubpassData = 6
}

/// <summary>
///     GLSL.std.450 扩展指令集枚举。
/// </summary>
public enum SpirvGLSLstd450 : uint
{
    Round = 1,
    RoundEven = 2,
    Trunc = 3,
    FAbs = 4,
    SAbs = 5,
    FSign = 6,
    SSign = 7,
    Floor = 8,
    Ceil = 9,
    Fract = 10,
    Radians = 11,
    Degrees = 12,
    Sin = 13,
    Cos = 14,
    Tan = 15,
    Asin = 16,
    Acos = 17,
    Atan = 18,
    Sinh = 19,
    Cosh = 20,
    Tanh = 21,
    Asinh = 22,
    Acosh = 23,
    Atanh = 24,
    Atan2 = 25,
    Pow = 26,
    Exp = 27,
    Log = 28,
    Exp2 = 29,
    Log2 = 30,
    Sqrt = 31,
    InverseSqrt = 32,
    Determinant = 33,
    MatrixInverse = 34,
    Modf = 35,
    ModfStruct = 36,
    FMin = 37,
    UMin = 38,
    SMin = 39,
    FMax = 40,
    UMax = 41,
    SMax = 42,
    FClamp = 43,
    UClamp = 44,
    SClamp = 45,
    FMix = 46,
    IMix = 47,
    Step = 48,
    SmoothStep = 49,
    Fma = 50,
    Frexp = 52,
    Ldexp = 53,
    PackSnorm4x8 = 54,
    PackUnorm4x8 = 55,
    PackSnorm2x16 = 56,
    PackUnorm2x16 = 57,
    PackHalf2x16 = 58,
    PackDouble2x32 = 59,
    UnpackSnorm2x16 = 60,
    UnpackUnorm2x16 = 61,
    UnpackHalf2x16 = 62,
    UnpackSnorm4x8 = 63,
    UnpackUnorm4x8 = 64,
    UnpackDouble2x32 = 65,
    Length = 66,
    Distance = 67,
    Cross = 68,
    Normalize = 69,
    FaceForward = 70,
    Reflect = 71,
    Refract = 72,
    FindILsb = 73,
    FindSMsb = 74,
    FindUMsb = 75,
    InterpolateAtCentroid = 76,
    InterpolateAtSample = 77,
    InterpolateAtOffset = 78,
    NMin = 79,
    NMax = 80,
    NClamp = 81
}

/// <summary>
///     SPIR-V SourceLanguage 枚举。
/// </summary>
public enum SpirvSourceLanguage : uint
{
    Unknown = 0,
    ESSL = 1,
    GLSL = 2,
    OpenCL_C = 3,
    OpenCL_CPP = 4,
    HLSL = 5
}

/// <summary>
///     SPIR-V 操作码枚举。
/// </summary>
public enum SpirvOpCode : ushort
{
    OpNop = 0,
    OpUndef = 1,
    OpSource = 3,
    OpSourceExtension = 4,
    OpName = 5,
    OpMemberName = 6,
    OpString = 7,
    OpLine = 8,
    OpExtension = 10,
    OpExtInstImport = 11,
    OpExtInst = 12,
    OpMemoryModel = 14,
    OpEntryPoint = 15,
    OpExecutionMode = 16,
    OpCapability = 17,
    OpTypeVoid = 19,
    OpTypeBool = 20,
    OpTypeInt = 21,
    OpTypeFloat = 22,
    OpTypeVector = 23,
    OpTypeMatrix = 24,
    OpTypeImage = 25,
    OpTypeSampler = 26,
    OpTypeSampledImage = 27,
    OpTypeArray = 28,
    OpTypeRuntimeArray = 29,
    OpTypeStruct = 30,
    OpTypePointer = 31,
    OpTypeFunction = 33,
    OpTypeForwardPointer = 39,
    OpConstantTrue = 41,
    OpConstantFalse = 42,
    OpConstant = 43,
    OpSpecConstant = 44,
    OpSpecConstantOp = 52,
    OpFunction = 54,
    OpFunctionParameter = 55,
    OpFunctionEnd = 56,
    OpFunctionCall = 57,
    OpVariable = 59,
    OpLoad = 61,
    OpStore = 62,
    OpCopyMemory = 63,
    OpAccessChain = 65,
    OpDecorate = 71,
    OpMemberDecorate = 72,
    OpCompositeConstruct = 80,
    OpCompositeExtract = 81,
    OpVectorShuffle = 79,
    OpSampledImage = 86,
    OpImageSampleImplicitLod = 87,
    OpImageSampleExplicitLod = 88,
    OpImageSampleDrefImplicitLod = 89,
    OpImageSampleDrefExplicitLod = 90,
    OpImageSampleProjImplicitLod = 91,
    OpImageSampleProjExplicitLod = 92,
    OpImageFetch = 95,
    OpImageGather = 96,
    OpImageDrefGather = 97,
    OpImageRead = 98,
    OpImageWrite = 99,
    OpImageTexelPointer = 100,
    OpImageQuerySize = 101,
    OpImageQuerySizeLod = 102,
    OpImageQueryLod = 103,
    OpImageQueryLevels = 104,
    OpImageQuerySamples = 105,
    OpConvertFToU = 109,
    OpConvertFToS = 110,
    OpConvertUToF = 111,
    OpConvertSToF = 112,
    OpUConvert = 113,
    OpSConvert = 114,
    OpFConvert = 115,
    OpSNegate = 126,
    OpFNegate = 127,
    OpIAdd = 128,
    OpISub = 129,
    OpIMul = 130,
    OpSDiv = 131,
    OpUDiv = 132,
    OpFAdd = 136,
    OpFSub = 137,
    OpFMul = 138,
    OpFDiv = 139,
    OpDot = 148,
    OpMatrixTimesVector = 145,
    OpMatrixTimesMatrix = 146,
    OpIEqual = 162,
    OpINotEqual = 163,
    OpULessThan = 166,
    OpUGreaterThan = 167,
    OpSGreaterThan = 168,
    OpSGreaterThanEqual = 169,
    OpSLessThan = 170,
    OpSLessThanEqual = 171,
    OpULessThanEqual = 172,
    OpUGreaterThanEqual = 173,
    OpFOrdEqual = 176,
    OpFOrdNotEqual = 177,
    OpFOrdLessThan = 178,
    OpFOrdGreaterThan = 179,
    OpFOrdLessThanEqual = 180,
    OpFOrdGreaterThanEqual = 181,
    OpLogicalAnd = 182,
    OpLogicalOr = 183,
    OpLogicalNot = 184,
    OpSelect = 185,
    OpPhi = 195,
    OpControlBarrier = 224,
    OpAtomicIAdd = 234,
    OpAtomicExchange = 235,
    OpAtomicCompareExchange = 237,
    OpSelectionMerge = 247,
    OpLoopMerge = 246,
    OpLabel = 248,
    OpBranch = 249,
    OpBranchConditional = 250,
    OpSwitch = 251,
    OpKill = 252,
    OpReturn = 253,
    OpReturnValue = 254,
    OpCopyObject = 83,
    OpTranspose = 84,
    OpPtrAccessChain = 66,
    OpInBoundsPtrAccessChain = 69,
    OpPtrDiff = 70,
    OpAll = 196,
    OpAny = 197,
    OpIsInf = 207,
    OpIsNan = 208,
    OpLessOrGreater = 210,
    OpOrdered = 211,
    OpUnordered = 212,
    OpSignBitSet = 213,
    OpBitCount = 202,
    OpBitReverse = 203,
    OpBitFieldInsert = 209,
    OpBitFieldSExtract = 214,
    OpBitFieldUExtract = 215,
    OpShiftRightLogical = 194,
    OpMatrixTimesScalar = 149,
    OpVectorTimesMatrix = 150,
    OpOuterProduct = 151,
    OpAtomicLoad = 227,
    OpAtomicStore = 228,
    OpAtomicIIncrement = 232,
    OpAtomicIDecrement = 233,
    OpAtomicUMin = 236,
    OpAtomicUMax = 237,
    OpAtomicAnd = 238,
    OpAtomicOr = 239,
    OpAtomicXor = 240,
    OpAtomicFlagClear = 229,
    OpAtomicFlagTestAndSet = 230,
    OpConstantSampler = 5114,
    OpConstantComposite = 44,
    OpSpecConstantComposite = 45,
    OpSpecConstantTrue = 48,
    OpSpecConstantFalse = 49,
    OpSizeOf = 321,
    OpTypeCooperativeMatrixKHR = 5368,
    OpGroupAsyncCopy = 259,
    OpGenericCastToPtr = 117,
    OpPtrCastToGeneric = 118,
    OpTypePipe = 4181,
    OpTypeAccelerationStructureKHR = 5341,
    OpRayQueryInitializeKHR = 5345,
    OpRayQueryProceedKHR = 5346,
    OpRayQueryGetIntersectionTypeKHR = 5349,
    OpTypeCooperativeMatrixNV = 5358,
    OpTypeVmeImageINTEL = 5611,
    OpTypeAvcImePayloadINTEL = 5612,
    OpTypeAvcRefPayloadINTEL = 5613,
    OpTypeAvcSicPayloadINTEL = 5614,
    OpTypeAvcMcePayloadINTEL = 5615,
    OpTypeAvcMceResultINTEL = 5616,
    OpTypeAvcImeResultINTEL = 5617,
    OpTypeAvcImeResultSingleReferenceStreamoutINTEL = 5618,
    OpTypeAvcImeResultDualReferenceStreamoutINTEL = 5619,
    OpTypeAvcImeSingleReferenceStreaminINTEL = 5620,
    OpTypeAvcImeDualReferenceStreaminINTEL = 5621,
    OpTypeAvcRefResultINTEL = 5622,
    OpTypeAvcSicResultINTEL = 5623
}

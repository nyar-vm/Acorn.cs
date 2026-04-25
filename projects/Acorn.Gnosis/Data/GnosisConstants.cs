namespace Acorn.Gnosis.Data;

/// <summary>
///     Gnosis 字节码模块格式常量。
/// </summary>
/// <remarks>
///     .gnosis 文件是 Gnosis VM 的字节码模块格式，基于 Game 方言特化。
///     完整规范请参阅 Gnosis.cs/documentation/technical/gnosis-bytecode-format.md
/// </remarks>
public static class GnosisConstants
{
    /// <summary>
    ///     GNOS 格式魔数。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x47, 0x4E, 0x4F, 0x53 };

    /// <summary>
    ///     GNOS 魔数值。
    /// </summary>
    public const uint MagicValue = 0x474E4F53;

    /// <summary>
    ///     当前版本号。
    /// </summary>
    public const ushort CurrentVersion = 1;

    /// <summary>
    ///     头部最小大小（Magic 4 + Version 2）。
    /// </summary>
    public const int MinHeaderSize = 6;
}

/// <summary>
///     常量池条目类型标签。
/// </summary>
public enum GnosisConstantTag : byte
{
    /// <summary>
    ///     字符串常量。
    /// </summary>
    String = 0x01,

    /// <summary>
    ///     整数常量。
    /// </summary>
    Int = 0x02,

    /// <summary>
    ///     浮点数常量（f32，4 字节小端序）。
    /// </summary>
    Float = 0x03
}

/// <summary>
///     Gnosis VM 操作码，单字节编码，按功能分区。
/// </summary>
/// <remarks>
///     操作码高位字节标识类别：0x0x 控制、0x1x 常量推送、0x2x 栈操作、
///     0x3x-0x4x 算术/比较/逻辑/控制流、0x5x 调用、0x6x 变量存取、
///     0x7x 对象操作、0x8x ECS（Game 方言特化）、0x9x 字符串、
///     0xAx 数组、0xBx 闭包、0xCx 类型检查、0xDx 协程。
/// </remarks>
public enum GnosisOpCode : byte
{
    #region 控制 (0x00-0x01)

    Halt = 0x00,
    Nop = 0x01,

    #endregion

    #region 常量推送 (0x10-0x18)

    PushInt8 = 0x10,
    PushInt16 = 0x11,
    PushInt32 = 0x12,
    PushInt64 = 0x13,
    PushFloat32 = 0x14,
    PushFloat64 = 0x15,
    PushTrue = 0x16,
    PushFalse = 0x17,
    PushNull = 0x18,

    #endregion

    #region 栈操作 (0x20-0x21)

    Pop = 0x20,
    Dup = 0x21,

    #endregion

    #region 算术 (0x30-0x39)

    AddInt = 0x30,
    SubInt = 0x31,
    MulInt = 0x32,
    DivInt = 0x33,
    AddFloat = 0x34,
    SubFloat = 0x35,
    MulFloat = 0x36,
    DivFloat = 0x37,
    NegInt = 0x38,
    NegFloat = 0x39,

    #endregion

    #region 整数比较 (0x3A-0x3F)

    EqualInt = 0x3A,
    NotEqualInt = 0x3B,
    LessInt = 0x3C,
    GreaterInt = 0x3D,
    LessEqualInt = 0x3E,
    GreaterEqualInt = 0x3F,

    #endregion

    #region 控制流 (0x40-0x42)

    Jump = 0x40,
    JumpIfTrue = 0x41,
    JumpIfFalse = 0x42,

    #endregion

    #region 浮点比较 (0x43-0x48)

    EqualFloat = 0x43,
    NotEqualFloat = 0x44,
    LessFloat = 0x45,
    GreaterFloat = 0x46,
    LessEqualFloat = 0x47,
    GreaterEqualFloat = 0x48,

    #endregion

    #region 逻辑 (0x49-0x4B)

    And = 0x49,
    Or = 0x4A,
    Not = 0x4B,

    #endregion

    #region 调用 (0x50-0x53)

    Call = 0x50,
    CallNative = 0x51,
    Return = 0x52,
    CallModule = 0x53,

    #endregion

    #region 变量访问 (0x60-0x65)

    LoadLocal = 0x60,
    StoreLocal = 0x61,
    LoadGlobal = 0x62,
    StoreGlobal = 0x63,
    LoadField = 0x64,
    StoreField = 0x65,

    #endregion

    #region 对象 (0x70-0x72)

    NewObject = 0x70,
    GetField = 0x71,
    SetField = 0x72,

    #endregion

    #region ECS — Game 方言特化 (0x80-0x8E)

    SpawnEntity = 0x80,
    DestroyEntity = 0x81,
    AddComponent = 0x82,
    GetComponent = 0x83,
    RemoveComponent = 0x84,
    QueryAll = 0x85,
    QueryAny = 0x86,
    DefineComponent = 0x87,
    DefineSystem = 0x88,
    SetComponent = 0x89,
    HasComponent = 0x8A,
    QueryWith = 0x8B,
    QueryWithout = 0x8C,
    SystemSchedule = 0x8D,
    WorldUpdate = 0x8E,

    #endregion

    #region 字符串 (0x90-0x93)

    PushString = 0x90,
    ConcatString = 0x91,
    StringLength = 0x92,
    StringGetChar = 0x93,

    #endregion

    #region 数组 (0xA0-0xA3)

    NewArray = 0xA0,
    ArrayGet = 0xA1,
    ArraySet = 0xA2,
    ArrayLength = 0xA3,

    #endregion

    #region 闭包 (0xB0-0xB2)

    MakeClosure = 0xB0,
    GetUpvalue = 0xB1,
    SetUpvalue = 0xB2,

    #endregion

    #region 类型检查 (0xC0-0xC2)

    IsNull = 0xC0,
    IsType = 0xC1,
    TypeOf = 0xC2,

    #endregion

    #region 协程 (0xD0-0xD1)

    Yield = 0xD0,
    Resume = 0xD1

    #endregion
}

/// <summary>
///     Gnosis VM 操作码操作数格式。
/// </summary>
public enum GnosisOperandFormat : byte
{
    /// <summary>
    ///     无操作数。
    /// </summary>
    None,

    /// <summary>
    ///     1 字节整数操作数。
    /// </summary>
    Int8,

    /// <summary>
    ///     2 字节整数操作数（小端序）。
    /// </summary>
    Int16,

    /// <summary>
    ///     4 字节整数操作数（小端序）。
    /// </summary>
    Int32,

    /// <summary>
    ///     8 字节整数操作数（小端序）。
    /// </summary>
    Int64,

    /// <summary>
    ///     4 字节浮点操作数（小端序）。
    /// </summary>
    Float32,

    /// <summary>
    ///     8 字节浮点操作数（小端序）。
    /// </summary>
    Float64
}

/// <summary>
///     Gnosis VM 指令类别。
/// </summary>
public enum GnosisInstructionCategory : byte
{
    Control,
    Constant,
    Stack,
    Arithmetic,
    Comparison,
    Logic,
    ControlFlow,
    Call,
    Variable,
    Object,
    Ecs,
    String,
    Array,
    Closure,
    TypeCheck,
    Coroutine
}

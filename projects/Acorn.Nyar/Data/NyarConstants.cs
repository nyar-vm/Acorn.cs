namespace Acorn.Nyar.Data;

/// <summary>
///     Nyar 字节码模块格式常量。
/// </summary>
public static class NyarConstants
{
    /// <summary>
    ///     .nyar 文件魔数（"NYAR" 大端序）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x4E, 0x59, 0x41, 0x52 };

    /// <summary>
    ///     魔数值。
    /// </summary>
    public const uint MagicValue = 0x4E594152;

    /// <summary>
    ///     当前版本号。
    /// </summary>
    public const uint CurrentVersion = 1;

    /// <summary>
    ///     头部大小（16 字节）。
    /// </summary>
    public const int HeaderSize = 16;

    /// <summary>
    ///     段头大小（9 字节：Kind 1 + Offset 4 + Size 4）。
    /// </summary>
    public const int SectionHeaderSize = 9;
}

/// <summary>
///     Nyar 段类型。
/// </summary>
public enum NyarSectionKind : byte
{
    /// <summary>
    ///     常量池段。
    /// </summary>
    Constants = 0x01,

    /// <summary>
    ///     函数表段。
    /// </summary>
    Functions = 0x02,

    /// <summary>
    ///     代码段。
    /// </summary>
    Code = 0x03,

    /// <summary>
    ///     导入表段。
    /// </summary>
    Imports = 0x04,

    /// <summary>
    ///     导出表段。
    /// </summary>
    Exports = 0x05,

    /// <summary>
    ///     调试信息段。
    /// </summary>
    DebugInfo = 0x10,

    /// <summary>
    ///     源码映射段。
    /// </summary>
    SourceMap = 0x11
}

/// <summary>
///     常量池条目类型。
/// </summary>
public enum NyarConstantKind : byte
{
    /// <summary>
    ///     32 位整数。
    /// </summary>
    Int32 = 0x01,

    /// <summary>
    ///     64 位浮点数。
    /// </summary>
    Float64 = 0x02,

    /// <summary>
    ///     布尔值。
    /// </summary>
    Bool = 0x03,

    /// <summary>
    ///     空值。
    /// </summary>
    Null = 0x04,

    /// <summary>
    ///     字符串。
    /// </summary>
    String = 0x05,

    /// <summary>
    ///     大整数。
    /// </summary>
    BigInt = 0x06
}

/// <summary>
///     导入类型。
/// </summary>
public enum NyarImportKind : byte
{
    /// <summary>
    ///     函数导入。
    /// </summary>
    Function = 0,

    /// <summary>
    ///     全局变量导入。
    /// </summary>
    Global = 1,

    /// <summary>
    ///     模块导入。
    /// </summary>
    Module = 2
}

/// <summary>
///     导出类型。
/// </summary>
public enum NyarExportKind : byte
{
    /// <summary>
    ///     函数导出。
    /// </summary>
    Function = 0,

    /// <summary>
    ///     全局变量导出。
    /// </summary>
    Global = 1
}

/// <summary>
///     Nyar 字节码操作码。
/// </summary>
public enum NyarOpcode : byte
{
    #region 控制流

    Nop = 0x00,
    Jump = 0x01,
    JumpIfTrue = 0x02,
    JumpIfFalse = 0x03,
    Call = 0x04,
    Return = 0x05,
    TailCall = 0x06,
    Throw = 0x07,
    Catch = 0x08,
    Yield = 0x0A,
    Resume = 0x0B,
    EffectHandle = 0x0C,

    #endregion

    #region 栈操作

    Const = 0x10,
    Pop = 0x11,
    Dup = 0x12,
    Swap = 0x13,

    #endregion

    #region 局部变量

    LoadLocal = 0x20,
    StoreLocal = 0x21,
    LoadArg = 0x22,
    LoadGlobal = 0x23,
    StoreGlobal = 0x24,

    #endregion

    #region i32 操作

    I32Add = 0x30,
    I32Sub = 0x31,
    I32Mul = 0x32,
    I32DivS = 0x33,
    I32DivU = 0x34,
    I32RemS = 0x35,
    I32RemU = 0x36,
    I32Neg = 0x37,
    I32And = 0x38,
    I32Or = 0x39,
    I32Xor = 0x3A,
    I32Shl = 0x3B,
    I32ShrS = 0x3C,
    I32ShrU = 0x3D,
    I32Not = 0x3E,

    #endregion

    #region i32 比较

    I32Eq = 0x40,
    I32Ne = 0x41,
    I32LtS = 0x42,
    I32LtU = 0x43,
    I32LeS = 0x44,
    I32LeU = 0x45,
    I32GtS = 0x46,
    I32GtU = 0x47,
    I32GeS = 0x48,
    I32GeU = 0x49,

    #endregion

    #region i64 操作

    I64Add = 0x50,
    I64Sub = 0x51,
    I64Mul = 0x52,
    I64DivS = 0x53,
    I64DivU = 0x54,
    I64Neg = 0x55,

    #endregion

    #region f32 操作

    F32Add = 0x60,
    F32Sub = 0x61,
    F32Mul = 0x62,
    F32Div = 0x63,
    F32Neg = 0x64,

    #endregion

    #region f64 操作

    F64Add = 0x70,
    F64Sub = 0x71,
    F64Mul = 0x72,
    F64Div = 0x73,
    F64Neg = 0x74,

    #endregion

    #region 类型转换

    I32ExtendI64S = 0x80,
    I32ExtendI64U = 0x81,
    I64TruncI32S = 0x82,
    I64TruncI32U = 0x83,
    I32ToF32S = 0x84,
    I32ToF64S = 0x85,

    #endregion

    #region 内存操作

    Alloc = 0x90,
    Free = 0x91,
    I32Load = 0x92,
    I32Store = 0x93,
    I64Load = 0x94,
    I64Store = 0x95,

    #endregion

    #region 对象操作

    NewObject = 0xA0,
    GetField = 0xA1,
    SetField = 0xA2,
    GetIndex = 0xA3,
    SetIndex = 0xA4,
    Length = 0xA5,
    NewClosure = 0xA6,
    GetUpvalue = 0xA7,
    SetUpvalue = 0xA8,

    #endregion

    #region 字符串操作

    StringConcat = 0xB0,
    StringLenBytes = 0xB1,
    StringLenChars = 0xB2,
    StringSubstr = 0xB3,

    #endregion

    #region BigInt 操作

    BigIntAdd = 0xC0,
    BigIntSub = 0xC1,
    BigIntMul = 0xC2,

    #endregion

    #region 内置函数分派

    BuiltinCall = 0xE0,

    #endregion

    Exit = 0xD2,
    GetTime = 0xD3,
    Sleep = 0xD4,
    MathSin = 0xD8,
    MathCos = 0xD9,
    MathSqrt = 0xDA,
    MathAbs = 0xDB,
    MathRand = 0xDC
}

namespace Acorn.Wasm.Data;

/// <summary>
///     WebAssembly 操作码枚举。
///     覆盖 WASM Core MVP、GC 提案前缀指令和 SIMD 前缀指令。
///     多字节前缀指令（0xFB/0xFC/0xFD/0xFE）用 ushort 高字节表达前缀。
/// </summary>
public enum WasmOpcode : ushort
{
    #region 控制流指令

    /// <summary>不可达指令，触发陷阱。</summary>
    Unreachable = 0x00,

    /// <summary>空操作。</summary>
    Nop = 0x01,

    /// <summary>块结构开始，可选块类型和返回值。</summary>
    Block = 0x02,

    /// <summary>循环结构开始，可选块类型和返回值。</summary>
    Loop = 0x03,

    /// <summary>条件分支开始，可选块类型和返回值。</summary>
    If = 0x04,

    /// <summary>条件分支 else 子句。</summary>
    Else = 0x05,

    /// <summary>块结束。</summary>
    End = 0x0B,

    /// <summary>无条件跳转到指定标签。</summary>
    Br = 0x0C,

    /// <summary>条件跳转到指定标签。</summary>
    BrIf = 0x0D,

    /// <summary>跳转表，根据索引跳转到不同标签。</summary>
    BrTable = 0x0E,

    /// <summary>函数返回。</summary>
    Return = 0x0F,

    /// <summary>调用函数。</summary>
    Call = 0x10,

    /// <summary>间接函数调用。</summary>
    CallIndirect = 0x11,

    /// <summary>丢弃栈顶值。</summary>
    Drop = 0x1A,

    /// <summary>条件选择。</summary>
    Select = 0x1B,

    /// <summary>带类型条件选择。</summary>
    SelectTyped = 0x1C,

    #endregion

    #region 局部变量指令

    /// <summary>获取局部变量。</summary>
    LocalGet = 0x20,

    /// <summary>设置局部变量。</summary>
    LocalSet = 0x21,

    /// <summary>设置局部变量并保留栈顶值。</summary>
    LocalTee = 0x22,

    /// <summary>获取全局变量。</summary>
    GlobalGet = 0x23,

    /// <summary>设置全局变量。</summary>
    GlobalSet = 0x24,

    #endregion

    #region 内存指令

    /// <summary>加载 32 位整数。</summary>
    I32Load = 0x28,

    /// <summary>加载 64 位整数。</summary>
    I64Load = 0x29,

    /// <summary>加载 32 位浮点数。</summary>
    F32Load = 0x2A,

    /// <summary>加载 64 位浮点数。</summary>
    F64Load = 0x2B,

    /// <summary>加载 8 位有符号整数并扩展为 32 位。</summary>
    I32Load8S = 0x2C,

    /// <summary>加载 8 位无符号整数并扩展为 32 位。</summary>
    I32Load8U = 0x2D,

    /// <summary>加载 16 位有符号整数并扩展为 32 位。</summary>
    I32Load16S = 0x2E,

    /// <summary>加载 16 位无符号整数并扩展为 32 位。</summary>
    I32Load16U = 0x2F,

    /// <summary>加载 8 位有符号整数并扩展为 64 位。</summary>
    I64Load8S = 0x30,

    /// <summary>加载 8 位无符号整数并扩展为 64 位。</summary>
    I64Load8U = 0x31,

    /// <summary>加载 16 位有符号整数并扩展为 64 位。</summary>
    I64Load16S = 0x32,

    /// <summary>加载 16 位无符号整数并扩展为 64 位。</summary>
    I64Load16U = 0x33,

    /// <summary>加载 32 位有符号整数并扩展为 64 位。</summary>
    I64Load32S = 0x34,

    /// <summary>加载 32 位无符号整数并扩展为 64 位。</summary>
    I64Load32U = 0x35,

    /// <summary>存储 32 位整数。</summary>
    I32Store = 0x36,

    /// <summary>存储 64 位整数。</summary>
    I64Store = 0x37,

    /// <summary>存储 32 位浮点数。</summary>
    F32Store = 0x38,

    /// <summary>存储 64 位浮点数。</summary>
    F64Store = 0x39,

    /// <summary>存储 32 位整数的低 8 位。</summary>
    I32Store8 = 0x3A,

    /// <summary>存储 32 位整数的低 16 位。</summary>
    I32Store16 = 0x3B,

    /// <summary>存储 64 位整数的低 8 位。</summary>
    I64Store8 = 0x3C,

    /// <summary>存储 64 位整数的低 16 位。</summary>
    I64Store16 = 0x3D,

    /// <summary>存储 64 位整数的低 32 位。</summary>
    I64Store32 = 0x3E,

    /// <summary>当前内存大小（页数）。</summary>
    MemorySize = 0x3F,

    /// <summary>增长内存（页数）。</summary>
    MemoryGrow = 0x40,

    #endregion

    #region 常量指令

    /// <summary>32 位整数常量。</summary>
    I32Const = 0x41,

    /// <summary>64 位整数常量。</summary>
    I64Const = 0x42,

    /// <summary>32 位浮点数常量。</summary>
    F32Const = 0x43,

    /// <summary>64 位浮点数常量。</summary>
    F64Const = 0x44,

    #endregion

    #region 比较指令

    /// <summary>32 位整数相等比较。</summary>
    I32Eq = 0x46,

    /// <summary>32 位整数不等比较。</summary>
    I32Ne = 0x47,

    /// <summary>32 位有符号整数小于比较。</summary>
    I32LtS = 0x48,

    /// <summary>32 位无符号整数小于比较。</summary>
    I32LtU = 0x49,

    /// <summary>32 位有符号整数大于比较。</summary>
    I32GtS = 0x4A,

    /// <summary>32 位无符号整数大于比较。</summary>
    I32GtU = 0x4B,

    /// <summary>32 位有符号整数小于等于比较。</summary>
    I32LeS = 0x4C,

    /// <summary>32 位无符号整数小于等于比较。</summary>
    I32LeU = 0x4D,

    /// <summary>32 位有符号整数大于等于比较。</summary>
    I32GeS = 0x4E,

    /// <summary>32 位无符号整数大于等于比较。</summary>
    I32GeU = 0x4F,

    /// <summary>64 位整数相等比较。</summary>
    I64Eq = 0x51,

    /// <summary>64 位整数不等比较。</summary>
    I64Ne = 0x52,

    /// <summary>64 位有符号整数小于比较。</summary>
    I64LtS = 0x53,

    /// <summary>64 位无符号整数小于比较。</summary>
    I64LtU = 0x54,

    /// <summary>64 位有符号整数大于比较。</summary>
    I64GtS = 0x55,

    /// <summary>64 位无符号整数大于比较。</summary>
    I64GtU = 0x56,

    /// <summary>64 位有符号整数小于等于比较。</summary>
    I64LeS = 0x57,

    /// <summary>64 位无符号整数小于等于比较。</summary>
    I64LeU = 0x58,

    /// <summary>64 位有符号整数大于等于比较。</summary>
    I64GeS = 0x59,

    /// <summary>64 位无符号整数大于等于比较。</summary>
    I64GeU = 0x5A,

    /// <summary>32 位浮点数相等比较。</summary>
    F32Eq = 0x5B,

    /// <summary>32 位浮点数不等比较。</summary>
    F32Ne = 0x5C,

    /// <summary>32 位浮点数小于比较。</summary>
    F32Lt = 0x5D,

    /// <summary>32 位浮点数大于比较。</summary>
    F32Gt = 0x5E,

    /// <summary>32 位浮点数小于等于比较。</summary>
    F32Le = 0x5F,

    /// <summary>32 位浮点数大于等于比较。</summary>
    F32Ge = 0x60,

    /// <summary>64 位浮点数相等比较。</summary>
    F64Eq = 0x61,

    /// <summary>64 位浮点数不等比较。</summary>
    F64Ne = 0x62,

    /// <summary>64 位浮点数小于比较。</summary>
    F64Lt = 0x63,

    /// <summary>64 位浮点数大于比较。</summary>
    F64Gt = 0x64,

    /// <summary>64 位浮点数小于等于比较。</summary>
    F64Le = 0x65,

    /// <summary>64 位浮点数大于等于比较。</summary>
    F64Ge = 0x66,

    #endregion

    #region 算术指令

    /// <summary>32 位整数加法。</summary>
    I32Add = 0x6A,

    /// <summary>32 位整数减法。</summary>
    I32Sub = 0x6B,

    /// <summary>32 位整数乘法。</summary>
    I32Mul = 0x6C,

    /// <summary>32 位有符号整数除法。</summary>
    I32DivS = 0x6D,

    /// <summary>32 位无符号整数除法。</summary>
    I32DivU = 0x6E,

    /// <summary>32 位有符号整数取余。</summary>
    I32RemS = 0x6F,

    /// <summary>32 位无符号整数取余。</summary>
    I32RemU = 0x70,

    /// <summary>32 位整数按位与。</summary>
    I32And = 0x71,

    /// <summary>32 位整数按位或。</summary>
    I32Or = 0x72,

    /// <summary>32 位整数按位异或。</summary>
    I32Xor = 0x73,

    /// <summary>32 位整数左移。</summary>
    I32Shl = 0x74,

    /// <summary>32 位有符号整数右移。</summary>
    I32ShrS = 0x75,

    /// <summary>32 位无符号整数右移。</summary>
    I32ShrU = 0x76,

    /// <summary>32 位整数循环左移。</summary>
    I32Rotl = 0x77,

    /// <summary>32 位整数循环右移。</summary>
    I32Rotr = 0x78,

    /// <summary>64 位整数加法。</summary>
    I64Add = 0x7C,

    /// <summary>64 位整数减法。</summary>
    I64Sub = 0x7D,

    /// <summary>64 位整数乘法。</summary>
    I64Mul = 0x7E,

    /// <summary>64 位有符号整数除法。</summary>
    I64DivS = 0x7F,

    /// <summary>64 位无符号整数除法。</summary>
    I64DivU = 0x80,

    /// <summary>64 位有符号整数取余。</summary>
    I64RemS = 0x81,

    /// <summary>64 位无符号整数取余。</summary>
    I64RemU = 0x82,

    /// <summary>64 位整数按位与。</summary>
    I64And = 0x83,

    /// <summary>64 位整数按位或。</summary>
    I64Or = 0x84,

    /// <summary>64 位整数按位异或。</summary>
    I64Xor = 0x85,

    /// <summary>64 位整数左移。</summary>
    I64Shl = 0x86,

    /// <summary>64 位有符号整数右移。</summary>
    I64ShrS = 0x87,

    /// <summary>64 位无符号整数右移。</summary>
    I64ShrU = 0x88,

    /// <summary>64 位整数循环左移。</summary>
    I64Rotl = 0x89,

    /// <summary>64 位整数循环右移。</summary>
    I64Rotr = 0x8A,

    /// <summary>32 位浮点数加法。</summary>
    F32Add = 0x92,

    /// <summary>32 位浮点数减法。</summary>
    F32Sub = 0x93,

    /// <summary>32 位浮点数乘法。</summary>
    F32Mul = 0x94,

    /// <summary>32 位浮点数除法。</summary>
    F32Div = 0x95,

    /// <summary>32 位浮点数最小值。</summary>
    F32Min = 0x96,

    /// <summary>32 位浮点数最大值。</summary>
    F32Max = 0x97,

    /// <summary>32 位浮点数取负。</summary>
    F32Neg = 0x8C,

    /// <summary>32 位浮点数平方根。</summary>
    F32Sqrt = 0x91,

    /// <summary>64 位浮点数加法。</summary>
    F64Add = 0xA0,

    /// <summary>64 位浮点数减法。</summary>
    F64Sub = 0xA1,

    /// <summary>64 位浮点数乘法。</summary>
    F64Mul = 0xA2,

    /// <summary>64 位浮点数除法。</summary>
    F64Div = 0xA3,

    /// <summary>64 位浮点数最小值。</summary>
    F64Min = 0xA4,

    /// <summary>64 位浮点数最大值。</summary>
    F64Max = 0xA5,

    /// <summary>64 位浮点数取负。</summary>
    F64Neg = 0x9A,

    /// <summary>64 位浮点数平方根。</summary>
    F64Sqrt = 0x9F,

    /// <summary>32 位整数截断 32 位浮点数。</summary>
    I32TruncF32S = 0xA8,

    /// <summary>32 位无符号整数截断 32 位浮点数。</summary>
    I32TruncF32U = 0xA9,

    /// <summary>32 位有符号整数截断 64 位浮点数。</summary>
    I32TruncF64S = 0xAA,

    /// <summary>32 位无符号整数截断 64 位浮点数。</summary>
    I32TruncF64U = 0xAB,

    /// <summary>64 位有符号整数截断 32 位浮点数。</summary>
    I64TruncF32S = 0xAE,

    /// <summary>64 位无符号整数截断 32 位浮点数。</summary>
    I64TruncF32U = 0xAF,

    /// <summary>64 位有符号整数截断 64 位浮点数。</summary>
    I64TruncF64S = 0xB0,

    /// <summary>64 位无符号整数截断 64 位浮点数。</summary>
    I64TruncF64U = 0xB1,

    /// <summary>32 位有符号整数转换为 32 位浮点数。</summary>
    F32ConvertI32S = 0xB2,

    /// <summary>32 位无符号整数转换为 32 位浮点数。</summary>
    F32ConvertI32U = 0xB3,

    /// <summary>32 位有符号整数转换为 64 位浮点数。</summary>
    F64ConvertI32S = 0xB7,

    /// <summary>32 位无符号整数转换为 64 位浮点数。</summary>
    F64ConvertI32U = 0xB8,

    /// <summary>64 位有符号整数转换为 32 位浮点数。</summary>
    F32ConvertI64S = 0xB4,

    /// <summary>64 位无符号整数转换为 32 位浮点数。</summary>
    F32ConvertI64U = 0xB5,

    /// <summary>64 位有符号整数转换为 64 位浮点数。</summary>
    F64ConvertI64S = 0xB9,

    /// <summary>64 位无符号整数转换为 64 位浮点数。</summary>
    F64ConvertI64U = 0xBA,

    /// <summary>32 位整数扩展为 64 位有符号整数。</summary>
    I64ExtendI32S = 0xAC,

    /// <summary>32 位整数扩展为 64 位无符号整数。</summary>
    I64ExtendI32U = 0xAD,

    /// <summary>64 位整数包装为 32 位整数。</summary>
    I32WrapI64 = 0xA7,

    /// <summary>32 位浮点数提升为 64 位浮点数。</summary>
    F64PromoteF32 = 0xBB,

    /// <summary>64 位浮点数降级为 32 位浮点数。</summary>
    F32DemoteF64 = 0xB6,

    #endregion

    #region 引用指令

    /// <summary>空引用。</summary>
    RefNull = 0xD0,

    /// <summary>引用空值判断。</summary>
    RefIsNull = 0xD1,

    /// <summary>函数引用。</summary>
    RefFunc = 0xD2,

    /// <summary>引用相等比较。</summary>
    RefEq = 0xD3,

    /// <summary>断言引用非空。</summary>
    RefAsNonNull = 0xD4,

    #endregion

    #region 表指令

    /// <summary>获取表元素。</summary>
    TableGet = 0x25,

    /// <summary>设置表元素。</summary>
    TableSet = 0x26,

    /// <summary>表大小。</summary>
    TableSize = 0xFC10,

    /// <summary>增长表。</summary>
    TableGrow = 0xFC0F,

    /// <summary>填充表。</summary>
    TableFill = 0xFC11,

    /// <summary>表复制。</summary>
    TableCopy = 0xFC0E,

    /// <summary>表初始化。</summary>
    TableInit = 0xFC0C,

    /// <summary>删除表元素。</summary>
    TableElemDrop = 0xFC0D,

    #endregion

    #region 内存批量指令

    /// <summary>内存复制。</summary>
    MemoryCopy = 0xFC0A,

    /// <summary>内存填充。</summary>
    MemoryFill = 0xFC0B,

    /// <summary>内存初始化。</summary>
    MemoryInit = 0xFC08,

    /// <summary>丢弃数据段。</summary>
    DataDrop = 0xFC09,

    #endregion

    #region GC 结构体指令（前缀 0xFB）

    /// <summary>创建结构体实例。</summary>
    StructNew = 0xFB00,

    /// <summary>以默认值创建结构体实例。</summary>
    StructNewDefault = 0xFB01,

    /// <summary>读取结构体字段。</summary>
    StructGet = 0xFB02,

    /// <summary>有符号读取结构体打包字段。</summary>
    StructGetS = 0xFB03,

    /// <summary>无符号读取结构体打包字段。</summary>
    StructGetU = 0xFB04,

    /// <summary>写入结构体字段。</summary>
    StructSet = 0xFB05,

    #endregion

    #region GC 数组指令（前缀 0xFB）

    /// <summary>创建数组实例。</summary>
    ArrayNew = 0xFB06,

    /// <summary>以默认值创建数组实例。</summary>
    ArrayNewDefault = 0xFB07,

    /// <summary>以固定大小创建数组实例。</summary>
    ArrayNewFixed = 0xFB08,

    /// <summary>从数据段创建数组实例。</summary>
    ArrayNewData = 0xFB09,

    /// <summary>从元素段创建数组实例。</summary>
    ArrayNewElem = 0xFB0A,

    /// <summary>读取数组元素。</summary>
    ArrayGet = 0xFB0B,

    /// <summary>有符号读取数组打包元素。</summary>
    ArrayGetS = 0xFB0C,

    /// <summary>无符号读取数组打包元素。</summary>
    ArrayGetU = 0xFB0D,

    /// <summary>写入数组元素。</summary>
    ArraySet = 0xFB0E,

    /// <summary>获取数组长度。</summary>
    ArrayLen = 0xFB0F,

    /// <summary>填充数组。</summary>
    ArrayFill = 0xFB10,

    /// <summary>复制数组。</summary>
    ArrayCopy = 0xFB11,

    #endregion

    #region GC 整数引用指令（前缀 0xFB）

    /// <summary>创建 i31 引用。</summary>
    I31New = 0xFB20,

    /// <summary>有符号读取 i31 引用。</summary>
    I31GetS = 0xFB21,

    /// <summary>无符号读取 i31 引用。</summary>
    I31GetU = 0xFB22,

    #endregion

    #region GC 转换指令（前缀 0xFB）

    /// <summary>测试引用类型。</summary>
    RefTest = 0xFB40,

    /// <summary>强制转换引用类型。</summary>
    RefCast = 0xFB41,

    /// <summary>分支判断引用转换。</summary>
    BrOnCast = 0xFB42,

    /// <summary>分支判断引用转换失败。</summary>
    BrOnCastFail = 0xFB43,

    /// <summary>外部引用转为任意引用。</summary>
    ExternInternalize = 0xFB50,

    /// <summary>任意引用转为外部引用。</summary>
    ExternExternalize = 0xFB51,

    #endregion

    #region 带返回值的调用（前缀 0xFC）

    /// <summary>带返回值的函数调用。</summary>
    CallRef = 0xFC00,

    /// <summary>带返回值的间接函数调用。</summary>
    ReturnCallRef = 0xFC01,

    /// <summary>返回调用函数。</summary>
    ReturnCall = 0x12,

    /// <summary>带返回值的返回间接调用。</summary>
    ReturnCallIndirect = 0x13,

    #endregion

    #region Canonical ABI 指令（Component Model，前缀 0xFE）

    /// <summary>内存复制 8 位。</summary>
    MemoryAtomicNotify = 0xFE00,

    /// <summary>内存等待 32 位。</summary>
    MemoryAtomicWait32 = 0xFE01,

    /// <summary>内存等待 64 位。</summary>
    MemoryAtomicWait64 = 0xFE02,

    /// <summary>原子栅栏。</summary>
    AtomicFence = 0xFE03,

    #endregion
}

using Acorn;
using Acorn.Attributes;
using Acorn.Codec;

namespace Acorn.Wasm.Data;

/// <summary>
///     WebAssembly 二进制格式常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 WebAssembly MVP 规范，Acorn 独占二进制编解码职责。
/// </remarks>
public static class WasmConstants
{
    /// <summary>
    ///     Wasm 魔数（\0asm）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x00, 0x61, 0x73, 0x6D };

    /// <summary>
    ///     Wasm MVP 版本号。
    /// </summary>
    public const uint Version = 1;

    /// <summary>
    ///     函数类型标记字节。
    /// </summary>
    public const byte FunctionTypeForm = 0x60;

    /// <summary>
    ///     Limits 无上限标志。
    /// </summary>
    public const byte LimitsHasOnlyMin = 0x00;

    /// <summary>
    ///     Limits 有上限标志。
    /// </summary>
    public const byte LimitsHasMinMax = 0x01;

    /// <summary>
    ///     全局变量不可变标志。
    /// </summary>
    public const byte GlobalImmutable = 0x00;

    /// <summary>
    ///     全局变量可变标志。
    /// </summary>
    public const byte GlobalMutable = 0x01;

    /// <summary>
    ///     递归类型组标记字节。
    /// </summary>
    public const byte RecTypeForm = 0x4E;

    /// <summary>
    ///     子类型标记字节。
    /// </summary>
    public const byte SubTypeForm = 0x50;

    /// <summary>
    ///     复合类型结构体标记字节。
    /// </summary>
    public const byte StructTypeForm = 0x5F;

    /// <summary>
    ///     复合类型数组标记字节。
    /// </summary>
    public const byte ArrayTypeForm = 0x5E;

    /// <summary>
    ///     最终类型标记字节（不可被继承）。
    /// </summary>
    public const byte FinalType = 0x00;

    /// <summary>
    ///     非最终类型标记字节（可被继承）。
    /// </summary>
    public const byte NonFinalType = 0x01;

    /// <summary>
    ///     字段不可变标记字节。
    /// </summary>
    public const byte FieldImmutable = 0x00;

    /// <summary>
    ///     字段可变标记字节。
    /// </summary>
    public const byte FieldMutable = 0x01;

    /// <summary>
    ///     Component Model 版本号。
    /// </summary>
    public const uint ComponentVersion = 0x0A;

    /// <summary>
    ///     JSON 填充字节（空格）。
    /// </summary>
    public const byte PaddingByte = 0x20;
}

/// <summary>
///     WebAssembly 段 ID 枚举。
/// </summary>
public enum WasmSectionId : byte
{
    /// <summary>
    ///     自定义段。
    /// </summary>
    Custom = 0,

    /// <summary>
    ///     类型段。
    /// </summary>
    Type = 1,

    /// <summary>
    ///     导入段。
    /// </summary>
    Import = 2,

    /// <summary>
    ///     函数段。
    /// </summary>
    Function = 3,

    /// <summary>
    ///     表段。
    /// </summary>
    Table = 4,

    /// <summary>
    ///     内存段。
    /// </summary>
    Memory = 5,

    /// <summary>
    ///     全局段。
    /// </summary>
    Global = 6,

    /// <summary>
    ///     导出段。
    /// </summary>
    Export = 7,

    /// <summary>
    ///     起始段。
    /// </summary>
    Start = 8,

    /// <summary>
    ///     元素段。
    /// </summary>
    Element = 9,

    /// <summary>
    ///     代码段。
    /// </summary>
    Code = 10,

    /// <summary>
    ///     数据段。
    /// </summary>
    Data = 11,

    /// <summary>
    ///     数据计数段（WebAssembly 2.0+）。
    /// </summary>
    DataCount = 12
}

/// <summary>
///     WebAssembly 初始化表达式操作码枚举。
/// </summary>
public enum WasmInitOpCode : byte
{
    /// <summary>
    ///     i32.const 指令。
    /// </summary>
    I32Const = 0x41,

    /// <summary>
    ///     i64.const 指令。
    /// </summary>
    I64Const = 0x42,

    /// <summary>
    ///     f32.const 指令。
    /// </summary>
    F32Const = 0x43,

    /// <summary>
    ///     f64.const 指令。
    /// </summary>
    F64Const = 0x44,

    /// <summary>
    ///     global.get 指令。
    /// </summary>
    GlobalGet = 0x23,

    /// <summary>
    ///     end 指令。
    /// </summary>
    End = 0x0B
}

/// <summary>
///     Wasm 文件头部（8 字节）。
/// </summary>
[BinarySerializable]
public partial struct WasmHeader
{
    [Field(Order = 0, Length = 4)]
    public FixedBytes4 Magic;

    [Field(Order = 1)]
    public uint Version;
}

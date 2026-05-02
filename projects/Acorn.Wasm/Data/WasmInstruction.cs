namespace Acorn.Wasm.Data;

/// <summary>
///     WebAssembly 结构化指令，封装操作码和操作数列表。
///     替代直接在 ByteBufferWriter 中写裸字节。
/// </summary>
public sealed class WasmInstruction
{
    /// <summary>
    ///     WASM 操作码。
    /// </summary>
    public WasmOpcode Opcode { get; init; }

    /// <summary>
    ///     立即数操作数列表，无立即数时为 null。
    /// </summary>
    public IReadOnlyList<WasmImmediate>? Operands { get; init; }
}

/// <summary>
///     WASM 指令立即数操作数抽象基类。
/// </summary>
public abstract record WasmImmediate;

/// <summary>
///     32 位有符号整数立即数。
/// </summary>
public sealed record WasmI32Imm(int Value) : WasmImmediate;

/// <summary>
///     64 位有符号整数立即数。
/// </summary>
public sealed record WasmI64Imm(long Value) : WasmImmediate;

/// <summary>
///     32 位浮点数立即数。
/// </summary>
public sealed record WasmF32Imm(float Value) : WasmImmediate;

/// <summary>
///     64 位浮点数立即数。
/// </summary>
public sealed record WasmF64Imm(double Value) : WasmImmediate;

/// <summary>
///     类型索引立即数（用于 struct.new, array.get 等 GC 指令）。
/// </summary>
public sealed record WasmTypeIndexImm(uint Index) : WasmImmediate;

/// <summary>
///     函数索引立即数（用于 call 指令）。
/// </summary>
public sealed record WasmFuncIndexImm(uint Index) : WasmImmediate;

/// <summary>
///     字段索引立即数（用于 struct.get/set 指令）。
/// </summary>
public sealed record WasmFieldIndexImm(uint Index) : WasmImmediate;

/// <summary>
///     堆类型索引立即数（用于 ref.cast, ref.test 等）。
/// </summary>
public sealed record WasmHeapTypeImm(uint Index) : WasmImmediate;

/// <summary>
///     标签索引立即数（用于 br/br_if/br_table）。
/// </summary>
public sealed record WasmLabelIndexImm(uint Index) : WasmImmediate;

/// <summary>
///     局部变量索引立即数（用于 local.get/set/tee）。
/// </summary>
public sealed record WasmLocalIndexImm(uint Index) : WasmImmediate;

/// <summary>
///     全局变量索引立即数（用于 global.get/set）。
/// </summary>
public sealed record WasmGlobalIndexImm(uint Index) : WasmImmediate;

using System;
using System.Collections.Generic;

namespace Acorn.Jvm.Scanner;

/// <summary>
///     JVM 字节码分析结果，包含最大栈深度计算与控制流图
/// </summary>
public sealed class JvmCodeAnalysis
{
    /// <summary>
    ///     声明的最大栈深度
    /// </summary>
    public ushort DeclaredMaxStack { get; init; }

    /// <summary>
    ///     计算出的实际最大栈深度
    /// </summary>
    public int ComputedMaxStack { get; init; }

    /// <summary>
    ///     声明的最大局部变量数
    /// </summary>
    public ushort DeclaredMaxLocals { get; init; }

    /// <summary>
    ///     基本块列表
    /// </summary>
    public IReadOnlyList<JvmBasicBlock> BasicBlocks { get; init; }

    /// <summary>
    ///     异常处理器列表
    /// </summary>
    public IReadOnlyList<JvmExceptionHandler> ExceptionHandlers { get; init; }

    /// <summary>
    ///     指令总数
    /// </summary>
    public int InstructionCount { get; init; }
}

/// <summary>
///     JVM 基本块，包含连续的指令序列与入边/出边
/// </summary>
public sealed class JvmBasicBlock
{
    /// <summary>
    ///     基本块在字节码中的起始偏移
    /// </summary>
    public int StartOffset { get; init; }

    /// <summary>
    ///     基本块在字节码中的结束偏移（不含）
    /// </summary>
    public int EndOffset { get; init; }

    /// <summary>
    ///     进入基本块时的栈深度
    /// </summary>
    public int EntryStackDepth { get; set; }

    /// <summary>
    ///     基本块内的栈深度最大值（相对于入口的增量）
    /// </summary>
    public int MaxStackDepthDelta { get; set; }

    /// <summary>
    ///     后继基本块索引列表
    /// </summary>
    public IReadOnlyList<int> Successors { get; set; }

    /// <summary>
    ///     前驱基本块索引列表
    /// </summary>
    public IReadOnlyList<int> Predecessors { get; set; }

    /// <summary>
    ///     是否为异常处理器入口
    /// </summary>
    public bool IsExceptionHandlerEntry { get; init; }

    /// <summary>
    ///     是否为方法入口基本块
    /// </summary>
    public bool IsEntry { get; init; }
}

/// <summary>
///     异常处理器（对应 JVM 异常表中的一条记录）
/// </summary>
public sealed class JvmExceptionHandler
{
    /// <summary>
    ///     异常处理器覆盖的起始 PC
    /// </summary>
    public ushort StartPc { get; init; }

    /// <summary>
    ///     异常处理器覆盖的结束 PC
    /// </summary>
    public ushort EndPc { get; init; }

    /// <summary>
    ///     异常处理器入口 PC
    /// </summary>
    public ushort HandlerPc { get; init; }

    /// <summary>
    ///     捕获的异常类型常量池索引（0 表示 finally）
    /// </summary>
    public ushort CatchType { get; init; }

    /// <summary>
    ///     对应的处理器基本块索引
    /// </summary>
    public int HandlerBlockIndex { get; init; }
}

/// <summary>
///     解码后的单条 JVM 指令信息
/// </summary>
public sealed class JvmDecodedInstruction
{
    /// <summary>
    ///     指令在字节码中的偏移
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    ///     操作码
    /// </summary>
    public byte Opcode { get; init; }

    /// <summary>
    ///     操作数（0-4 字节的原始数值，对于分支指令为分支偏移量）
    /// </summary>
    public int Operand { get; init; }

    /// <summary>
    ///     指令总字节数（含操作码）
    /// </summary>
    public int Size { get; init; }

    /// <summary>
    ///     tableswitch / lookupswitch 的所有分支目标偏移（以当前指令偏移为基准）
    /// </summary>
    public IReadOnlyList<int>? SwitchTargets { get; init; }
}

using System.Runtime.CompilerServices;
using Acorn.Frame;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Decode;

/// <summary>
///     Gnosis VM 指令解码器，将扁平字节码流解码为结构化指令列表。
/// </summary>
/// <remarks>
///     解码器逐条解析字节码流中的指令，根据操作码读取对应宽度的操作数。
///     操作数格式与 Gnosis.Runtime.VM.VMInterpreter 的读取方式一致。
/// </remarks>
public ref struct GnosisInstructionDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="GnosisInstructionDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="instructions">指令字节码数据。</param>
    public GnosisInstructionDecoder(ReadOnlySpan<byte> instructions)
    {
        _buffer = new ByteBuffer(instructions);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Position;
    }

    /// <summary>
    ///     获取剩余未读取的字节数。
    /// </summary>
    public int Remaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Remaining;
    }

    /// <summary>
    ///     获取一个值，该值指示是否已到达数据末尾。
    /// </summary>
    public bool IsEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.IsEnd;
    }

    /// <summary>
    ///     解码全部指令。
    /// </summary>
    /// <returns>指令列表。</returns>
    public List<GnosisInstruction> DecodeAll()
    {
        var instructions = new List<GnosisInstruction>();

        while (!_buffer.IsEnd)
        {
            var instruction = DecodeNext();

            if (instruction is not null)
            {
                instructions.Add(instruction);
            }
        }

        return instructions;
    }

    /// <summary>
    ///     解码下一条指令。返回 null 表示遇到无效操作码。
    /// </summary>
    public GnosisInstruction? DecodeNext()
    {
        if (_buffer.IsEnd)
        {
            return null;
        }

        var offset = _buffer.Position;
        var opCodeByte = _buffer.ReadU8();

        if (!Enum.IsDefined(typeof(GnosisOpCode), opCodeByte))
        {
            return new GnosisInstruction
            {
                Offset = offset,
                OpCode = (GnosisOpCode)opCodeByte,
                RawOperand = 0
            };
        }

        var opCode = (GnosisOpCode)opCodeByte;
        var operandFormat = GnosisOpCodeInfo.GetOperandFormat(opCode);
        var rawOperand = ReadOperand(operandFormat);

        return new GnosisInstruction
        {
            Offset = offset,
            OpCode = opCode,
            RawOperand = rawOperand
        };
    }

    /// <summary>
    ///     跳过下一条指令（不创建对象，仅前进位置）。
    /// </summary>
    public bool SkipNext()
    {
        if (_buffer.IsEnd)
        {
            return false;
        }

        var opCodeByte = _buffer.ReadU8();

        if (!Enum.IsDefined(typeof(GnosisOpCode), opCodeByte))
        {
            return false;
        }

        var opCode = (GnosisOpCode)opCodeByte;
        var operandSize = GnosisOpCodeInfo.GetOperandSize(opCode);

        if (_buffer.Remaining < operandSize)
        {
            return false;
        }

        _buffer.Advance(operandSize);
        return true;
    }

    /// <summary>
    ///     统计指令总数（不创建指令对象）。
    /// </summary>
    public int CountInstructions()
    {
        var savedPosition = _buffer.Position;
        var count = 0;

        while (SkipNext())
        {
            count++;
        }

        _buffer.Position = savedPosition;
        return count;
    }

    #region 私有方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long ReadOperand(GnosisOperandFormat format)
    {
        return format switch
        {
            GnosisOperandFormat.Int8 => _buffer.ReadU8(),
            GnosisOperandFormat.Int16 => _buffer.ReadI16LE(),
            GnosisOperandFormat.Int32 => _buffer.ReadI32LE(),
            GnosisOperandFormat.Int64 => _buffer.ReadI64LE(),
            GnosisOperandFormat.Float32 => _buffer.ReadI32LE(),
            GnosisOperandFormat.Float64 => _buffer.ReadI64LE(),
            _ => 0
        };
    }

    #endregion
}

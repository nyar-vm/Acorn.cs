using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Acorn.Codec;

namespace Acorn.Frame;

/// <summary>
///     零拷贝内存缓冲区，基于 <see cref="ReadOnlySpan{T}" /> 提供零分配的二进制数据读取能力。
/// </summary>
/// <remarks>
///     <para>
///     ByteBuffer 是 Acorn 扫描层的基础设施，所有格式扫描器应基于 ByteBuffer 构建。
///     所有读取方法使用 <see cref="ReadOnlySpan{T}" /> 和 <see cref="BinaryPrimitives" />，
///     避免任何堆分配和流包装开销。
///     </para>
///     <para>
///     热路径方法标记 <see cref="MethodImplOptions.AggressiveInlining" /> 以确保 JIT 内联。
///     </para>
/// </remarks>
public ref struct ByteBuffer
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;
    private Endianness _endianness;

    /// <summary>
    ///     初始化 <see cref="ByteBuffer" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要读取的字节数据。</param>
    /// <param name="endianness">字节序，默认为小端序。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer(ReadOnlySpan<byte> data, Endianness endianness = Endianness.LittleEndian)
    {
        _data = data;
        _position = 0;
        _endianness = endianness;
    }

    /// <summary>
    ///     获取或设置当前读取位置。
    /// </summary>
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _position = value;
    }

    /// <summary>
    ///     获取数据总长度。
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Length;
    }

    /// <summary>
    ///     获取剩余未读取的字节数。
    /// </summary>
    public int Remaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Length - _position;
    }

    /// <summary>
    ///     获取一个值，该值指示是否已到达数据末尾。
    /// </summary>
    public bool IsEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position >= _data.Length;
    }

    /// <summary>
    ///     获取或设置字节序。通用读取方法（如 <see cref="ReadU16" />、<see cref="ReadU32" /> 等）
    ///     根据此属性选择小端序或大端序。默认为小端序。
    /// </summary>
    public Endianness Endianness
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _endianness;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _endianness = value;
    }

    /// <summary>
    ///     获取底层只读字节 Span。
    /// </summary>
    public ReadOnlySpan<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data;
    }

    /// <summary>
    ///     获取从当前位置到末尾的只读字节 Span。
    /// </summary>
    public ReadOnlySpan<byte> RemainingSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Slice(_position);
    }

    #region 前进与查看

    /// <summary>
    ///     向前移动指定字节数，不返回任何数据。
    /// </summary>
    /// <param name="count">要跳过的字节数。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "前进字节数不能为负数");
        }

        var newPosition = _position + count;

        if (newPosition > _data.Length)
        {
            throw new InvalidOperationException("读取位置超出数据范围");
        }

        _position = newPosition;
    }

    /// <summary>
    ///     查看接下来的若干字节但不移动位置。
    /// </summary>
    /// <param name="count">要查看的字节数。</param>
    /// <returns>指定长度的只读字节 Span。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> Peek(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "查看字节数不能为负数");
        }

        if (_position + count > _data.Length)
        {
            throw new InvalidOperationException("查看范围超出数据边界");
        }

        return _data.Slice(_position, count);
    }

    /// <summary>
    ///     读取指定数量的字节并前进位置。
    /// </summary>
    /// <param name="count">要读取的字节数。</param>
    /// <returns>指定长度的只读字节 Span。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "读取字节数不能为负数");
        }

        if (_position + count > _data.Length)
        {
            throw new InvalidOperationException("读取范围超出数据边界");
        }

        var result = _data.Slice(_position, count);
        _position += count;
        return result;
    }

    #endregion

    #region 魔数匹配

    /// <summary>
    ///     尝试匹配魔数（Magic Number）。
    /// </summary>
    /// <param name="magic">期望的魔数字节序列。</param>
    /// <returns>如果匹配成功则返回 true，否则返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MatchMagic(ReadOnlySpan<byte> magic)
    {
        if (magic.IsEmpty)
        {
            return true;
        }

        if (_position + magic.Length > _data.Length)
        {
            return false;
        }

        return _data.Slice(_position, magic.Length).SequenceEqual(magic);
    }

    /// <summary>
    ///     尝试匹配魔数并自动前进位置。
    /// </summary>
    /// <param name="magic">期望的魔数字节序列。</param>
    /// <returns>如果匹配成功则返回 true 并前进位置，否则返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConsumeMagic(ReadOnlySpan<byte> magic)
    {
        if (!MatchMagic(magic))
        {
            return false;
        }

        _position += magic.Length;
        return true;
    }

    #endregion

    #region 无符号整数（小端序）

    /// <summary>
    ///     读取一个无符号 8 位整数并前进 1 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadU8()
    {
        if (_position >= _data.Length)
        {
            throw new InvalidOperationException("已到达数据末尾");
        }

        return _data[_position++];
    }

    /// <summary>
    ///     以小端序读取一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadU16LE()
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position));
        _position += 2;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadU32LE()
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadU64LE()
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }

    #endregion

    #region 无符号整数（大端序）

    /// <summary>
    ///     以大端序读取一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadU16BE()
    {
        var value = BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position));
        _position += 2;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadU32BE()
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadU64BE()
    {
        var value = BinaryPrimitives.ReadUInt64BigEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }

    #endregion

    #region 有符号整数（小端序）

    /// <summary>
    ///     读取一个有符号 8 位整数并前进 1 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadI8()
    {
        return (sbyte)ReadU8();
    }

    /// <summary>
    ///     以小端序读取一个有符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadI16LE()
    {
        var value = BinaryPrimitives.ReadInt16LittleEndian(_data.Slice(_position));
        _position += 2;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadI32LE()
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadI64LE()
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }

    #endregion

    #region 有符号整数（大端序）

    /// <summary>
    ///     以大端序读取一个有符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadI16BE()
    {
        var value = BinaryPrimitives.ReadInt16BigEndian(_data.Slice(_position));
        _position += 2;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadI32BE()
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadI64BE()
    {
        var value = BinaryPrimitives.ReadInt64BigEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }

    #endregion

    #region 浮点数

    /// <summary>
    ///     以小端序读取一个 16 位半精度浮点数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half ReadF16LE()
    {
        var bits = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position));
        _position += 2;
        return BitConverter.UInt16BitsToHalf(bits);
    }

    /// <summary>
    ///     以大端序读取一个 16 位半精度浮点数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half ReadF16BE()
    {
        var bits = BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position));
        _position += 2;
        return BitConverter.UInt16BitsToHalf(bits);
    }

    /// <summary>
    ///     以小端序读取一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadF32LE()
    {
        var value = BinaryPrimitives.ReadSingleLittleEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadF32BE()
    {
        var value = BinaryPrimitives.ReadSingleBigEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }

    /// <summary>
    ///     以小端序读取一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadF64LE()
    {
        var value = BinaryPrimitives.ReadDoubleLittleEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }

    /// <summary>
    ///     以大端序读取一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadF64BE()
    {
        var value = BinaryPrimitives.ReadDoubleBigEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }

    #endregion

    #region 通用字节序读取

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadU16()
    {
        return _endianness == Endianness.LittleEndian ? ReadU16LE() : ReadU16BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadU32()
    {
        return _endianness == Endianness.LittleEndian ? ReadU32LE() : ReadU32BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadU64()
    {
        return _endianness == Endianness.LittleEndian ? ReadU64LE() : ReadU64BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个有符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadI16()
    {
        return _endianness == Endianness.LittleEndian ? ReadI16LE() : ReadI16BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadI32()
    {
        return _endianness == Endianness.LittleEndian ? ReadI32LE() : ReadI32BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadI64()
    {
        return _endianness == Endianness.LittleEndian ? ReadI64LE() : ReadI64BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个 16 位半精度浮点数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half ReadF16()
    {
        return _endianness == Endianness.LittleEndian ? ReadF16LE() : ReadF16BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadF32()
    {
        return _endianness == Endianness.LittleEndian ? ReadF32LE() : ReadF32BE();
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序读取一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadF64()
    {
        return _endianness == Endianness.LittleEndian ? ReadF64LE() : ReadF64BE();
    }

    #endregion

    #region LEB128 变长整数

    /// <summary>
    ///     读取 LEB128 编码的无符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadLeb128U32()
    {
        if (_position >= _data.Length)
        {
            throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
        }

        var b = _data[_position];

        if ((b & 0x80) == 0)
        {
            _position++;
            return b;
        }

        uint result = 0;
        var shift = 0;

        while (true)
        {
            if (_position >= _data.Length)
            {
                throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
            }

            b = _data[_position++];
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                break;
            }

            shift += 7;

            if (shift >= 32)
            {
                throw new InvalidDataException("LEB128 编码的整数过大");
            }
        }

        return result;
    }

    /// <summary>
    ///     读取 LEB128 编码的无符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadLeb128U64()
    {
        if (_position >= _data.Length)
        {
            throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
        }

        var b = _data[_position];

        if ((b & 0x80) == 0)
        {
            _position++;
            return b;
        }

        ulong result = 0;
        var shift = 0;

        while (true)
        {
            if (_position >= _data.Length)
            {
                throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
            }

            b = _data[_position++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                break;
            }

            shift += 7;

            if (shift >= 64)
            {
                throw new InvalidDataException("LEB128 编码的整数过大");
            }
        }

        return result;
    }

    /// <summary>
    ///     读取 LEB128 编码的有符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadLeb128I32()
    {
        if (_position >= _data.Length)
        {
            throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
        }

        var b = _data[_position];

        if ((b & 0x80) == 0)
        {
            _position++;

            if (b < 0x40)
            {
                return b;
            }

            return b - 0x100;
        }

        int result = 0;
        var shift = 0;

        do
        {
            if (_position >= _data.Length)
            {
                throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
            }

            b = _data[_position++];
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (shift < 32 && (b & 0x40) != 0)
        {
            result |= ~0 << shift;
        }

        return result;
    }

    /// <summary>
    ///     读取 ZigZag + LEB128 编码的有符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadZigZagLeb128I32()
    {
        var raw = ReadLeb128U32();
        return (int)((raw >> 1) ^ (0u - (raw & 1)));
    }

    /// <summary>
    ///     读取 LEB128 编码的有符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLeb128I64()
    {
        if (_position >= _data.Length)
        {
            throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
        }

        var b = _data[_position];

        if ((b & 0x80) == 0)
        {
            _position++;

            if (b < 0x40)
            {
                return b;
            }

            return b - 0x100;
        }

        long result = 0;
        var shift = 0;

        do
        {
            if (_position >= _data.Length)
            {
                throw new InvalidOperationException("已到达数据末尾，LEB128 编码不完整");
            }

            b = _data[_position++];
            result |= (long)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        if (shift < 64 && (b & 0x40) != 0)
        {
            result |= ~0L << shift;
        }

        return result;
    }

    /// <summary>
    ///     读取 ZigZag + LEB128 编码的有符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadZigZagLeb128I64()
    {
        var raw = ReadLeb128U64();
        return (long)((raw >> 1) ^ (0UL - (raw & 1)));
    }

    /// <summary>
    ///     尝试从指定位置读取 LEB128 编码的有符号 32 位整数，不移动位置。
    /// </summary>
    /// <param name="start">起始位置。</param>
    /// <param name="value">解码后的值。</param>
    /// <param name="consumed">消耗的字节数。</param>
    /// <returns>如果成功解码则返回 true。</returns>
    public static bool TryDecodeLeb128I32(ReadOnlySpan<byte> data, out int value, out int consumed)
    {
        value = 0;
        consumed = 0;
        int result = 0;
        var shift = 0;
        byte b;

        while (consumed < data.Length)
        {
            b = data[consumed];
            consumed++;
            result |= (b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0)
            {
                if (shift < 32 && (b & 0x40) != 0)
                {
                    result |= ~0 << shift;
                }

                value = result;
                return true;
            }

            if (shift >= 32)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读取 LEB128 编码的无符号 32 位整数，不移动位置。
    /// </summary>
    /// <param name="data">数据源。</param>
    /// <param name="value">解码后的值。</param>
    /// <param name="consumed">消耗的字节数。</param>
    /// <returns>如果成功解码则返回 true。</returns>
    public static bool TryDecodeLeb128U32(ReadOnlySpan<byte> data, out uint value, out int consumed)
    {
        value = 0;
        consumed = 0;
        uint result = 0;
        var shift = 0;

        while (consumed < data.Length)
        {
            var b = data[consumed];
            consumed++;
            result |= (uint)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                value = result;
                return true;
            }

            shift += 7;

            if (shift >= 32)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读取 LEB128 编码的无符号 64 位整数，不移动位置。
    /// </summary>
    /// <param name="data">数据源。</param>
    /// <param name="value">解码后的值。</param>
    /// <param name="consumed">消耗的字节数。</param>
    /// <returns>如果成功解码则返回 true。</returns>
    public static bool TryDecodeLeb128U64(ReadOnlySpan<byte> data, out ulong value, out int consumed)
    {
        value = 0;
        consumed = 0;
        ulong result = 0;
        var shift = 0;

        while (consumed < data.Length)
        {
            var b = data[consumed];
            consumed++;
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                value = result;
                return true;
            }

            shift += 7;

            if (shift >= 64)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读取 LEB128 编码的有符号 64 位整数，不移动位置。
    /// </summary>
    /// <param name="data">数据源。</param>
    /// <param name="value">解码后的值。</param>
    /// <param name="consumed">消耗的字节数。</param>
    /// <returns>如果成功解码则返回 true。</returns>
    public static bool TryDecodeLeb128I64(ReadOnlySpan<byte> data, out long value, out int consumed)
    {
        value = 0;
        consumed = 0;
        long result = 0;
        var shift = 0;
        byte b;

        while (consumed < data.Length)
        {
            b = data[consumed];
            consumed++;
            result |= (long)(b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0)
            {
                if (shift < 64 && (b & 0x40) != 0)
                {
                    result |= ~0L << shift;
                }

                value = result;
                return true;
            }

            if (shift >= 64)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    ///     尝试从指定位置读取 ZigZag + LEB128 编码的有符号 32 位整数，不移动位置。
    /// </summary>
    /// <param name="data">数据源。</param>
    /// <param name="value">解码后的值。</param>
    /// <param name="consumed">消耗的字节数。</param>
    /// <returns>如果成功解码则返回 true。</returns>
    public static bool TryDecodeZigZagLeb128I32(ReadOnlySpan<byte> data, out int value, out int consumed)
    {
        if (!TryDecodeLeb128U32(data, out var raw, out consumed))
        {
            value = 0;
            return false;
        }

        value = (int)((raw >> 1) ^ (0u - (raw & 1)));
        return true;
    }

    /// <summary>
    ///     尝试从指定位置读取 ZigZag + LEB128 编码的有符号 64 位整数，不移动位置。
    /// </summary>
    /// <param name="data">数据源。</param>
    /// <param name="value">解码后的值。</param>
    /// <param name="consumed">消耗的字节数。</param>
    /// <returns>如果成功解码则返回 true。</returns>
    public static bool TryDecodeZigZagLeb128I64(ReadOnlySpan<byte> data, out long value, out int consumed)
    {
        if (!TryDecodeLeb128U64(data, out var raw, out consumed))
        {
            value = 0;
            return false;
        }

        value = (long)((raw >> 1) ^ (0UL - (raw & 1)));
        return true;
    }

    #endregion

    #region 字符串

    /// <summary>
    ///     读取指定字节长度的 UTF-8 字符串并前进相应字节数。
    /// </summary>
    /// <param name="byteLength">字符串的字节长度。</param>
    /// <returns>解码后的字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(int byteLength)
    {
        if (byteLength == 0)
        {
            return string.Empty;
        }

        var span = ReadBytes(byteLength);
        return Encoding.UTF8.GetString(span);
    }

    /// <summary>
    ///     读取 LEB128 长度前缀的 UTF-8 字符串并前进相应字节数。
    /// </summary>
    /// <returns>解码后的字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLeb128String()
    {
        var length = (int)ReadLeb128U32();
        return ReadString(length);
    }

    /// <summary>
    ///     读取以 null 终止的 UTF-8 字符串并前进相应字节数（含终止符）。
    /// </summary>
    /// <returns>解码后的字符串。</returns>
    public string ReadNullTerminatedString()
    {
        var start = _position;

        while (_position < _data.Length && _data[_position] != 0)
        {
            _position++;
        }

        var length = _position - start;

        if (_position < _data.Length)
        {
            _position++;
        }

        return length == 0 ? string.Empty : Encoding.UTF8.GetString(_data.Slice(start, length));
    }

    #endregion

    #region 指定位置读取

    /// <summary>
    ///     在指定位置读取一个无符号 8 位整数，不移动位置。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadU8At(int offset)
    {
        if (offset < 0 || offset >= _data.Length)
        {
            return 0;
        }

        return _data[offset];
    }

    /// <summary>
    ///     在指定位置以指定字节序读取一个有符号 32 位整数，不移动位置。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadI32At(int offset, bool bigEndian)
    {
        if (offset < 0 || offset + 4 > _data.Length)
        {
            return 0;
        }

        return bigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(_data.Slice(offset, 4))
            : BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(offset, 4));
    }

    /// <summary>
    ///     在指定位置以小端序读取一个无符号 32 位整数，不移动位置。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadU32At(int offset)
    {
        if (offset < 0 || offset + 4 > _data.Length)
        {
            return 0;
        }

        return BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(offset, 4));
    }

    /// <summary>
    ///     在指定位置以指定字节序读取一个 32 位浮点数，不移动位置。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadF32At(int offset, bool bigEndian)
    {
        if (offset < 0 || offset + 4 > _data.Length)
        {
            return 0f;
        }

        return bigEndian
            ? BinaryPrimitives.ReadSingleBigEndian(_data.Slice(offset, 4))
            : BinaryPrimitives.ReadSingleLittleEndian(_data.Slice(offset, 4));
    }

    /// <summary>
    ///     在指定位置读取以 null 终止的 UTF-8 字符串，不移动位置。
    /// </summary>
    public string ReadStringAt(int offset)
    {
        if (offset < 0 || offset >= _data.Length)
        {
            return string.Empty;
        }

        var end = offset;

        while (end < _data.Length && _data[end] != 0)
        {
            end++;
        }

        return end == offset ? string.Empty : Encoding.UTF8.GetString(_data.Slice(offset, end - offset));
    }

    #endregion

    #region 泛型编解码器读取

    /// <summary>
    ///     使用指定编解码器从当前位置读取值并前进相应字节数。
    /// </summary>
    /// <typeparam name="T">读取的值类型。</typeparam>
    /// <typeparam name="TCodec">编解码器类型。</typeparam>
    /// <param name="codec">用于解码的编解码器实例。</param>
    /// <returns>解码后的值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T, TCodec>(TCodec codec) where TCodec : ICodec<T>
    {
        var size = codec.GetSize(default!);
        var value = codec.Decode(_data.Slice(_position));
        if (size > 0)
        {
            _position += size;
        }
        return value;
    }

    #endregion

    #region Unsafe 快速读取路径

    /// <summary>
    ///     不检查边界地读取一个无符号 8 位整数并前进 1 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte UnsafeReadU8()
    {
        return _data[_position++];
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个无符号 16 位整数并前进 2 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort UnsafeReadU16LE()
    {
        var value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(_data.Slice(_position)));
        _position += 2;

        if (BitConverter.IsLittleEndian)
        {
            return value;
        }

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个无符号 16 位整数并前进 2 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort UnsafeReadU16BE()
    {
        var value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(_data.Slice(_position)));
        _position += 2;

        if (!BitConverter.IsLittleEndian)
        {
            return value;
        }

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个无符号 32 位整数并前进 4 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint UnsafeReadU32LE()
    {
        var value = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(_data.Slice(_position)));
        _position += 4;

        if (BitConverter.IsLittleEndian)
        {
            return value;
        }

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个无符号 32 位整数并前进 4 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint UnsafeReadU32BE()
    {
        var value = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(_data.Slice(_position)));
        _position += 4;

        if (!BitConverter.IsLittleEndian)
        {
            return value;
        }

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个无符号 64 位整数并前进 8 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong UnsafeReadU64LE()
    {
        var value = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(_data.Slice(_position)));
        _position += 8;

        if (BitConverter.IsLittleEndian)
        {
            return value;
        }

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个无符号 64 位整数并前进 8 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong UnsafeReadU64BE()
    {
        var value = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(_data.Slice(_position)));
        _position += 8;

        if (!BitConverter.IsLittleEndian)
        {
            return value;
        }

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个有符号 32 位整数并前进 4 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int UnsafeReadI32LE()
    {
        return (int)UnsafeReadU32LE();
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个有符号 32 位整数并前进 4 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int UnsafeReadI32BE()
    {
        return (int)UnsafeReadU32BE();
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个有符号 64 位整数并前进 8 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long UnsafeReadI64LE()
    {
        return (long)UnsafeReadU64LE();
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个有符号 64 位整数并前进 8 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long UnsafeReadI64BE()
    {
        return (long)UnsafeReadU64BE();
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个 32 位浮点数并前进 4 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float UnsafeReadF32LE()
    {
        return BitConverter.UInt32BitsToSingle(UnsafeReadU32LE());
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个 32 位浮点数并前进 4 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float UnsafeReadF32BE()
    {
        return BitConverter.UInt32BitsToSingle(UnsafeReadU32BE());
    }

    /// <summary>
    ///     不检查边界地以小端序读取一个 64 位浮点数并前进 8 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double UnsafeReadF64LE()
    {
        return BitConverter.UInt64BitsToDouble(UnsafeReadU64LE());
    }

    /// <summary>
    ///     不检查边界地以大端序读取一个 64 位浮点数并前进 8 字节。
    ///     调用方必须保证缓冲区有足够数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double UnsafeReadF64BE()
    {
        return BitConverter.UInt64BitsToDouble(UnsafeReadU64BE());
    }

    #endregion
}

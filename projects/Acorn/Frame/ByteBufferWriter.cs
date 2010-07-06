using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Acorn.Frame;

/// <summary>
///     零拷贝内存写入缓冲区，基于 <see cref="Span{T}" /> 提供零分配的二进制数据写入能力。
/// </summary>
/// <remarks>
///     <para>
///     ByteBufferWriter 是 ByteBuffer 的对称写入组件，所有写入方法使用 <see cref="Span{T}" /> 和
///     <see cref="BinaryPrimitives" />，避免任何堆分配和流包装开销。
///     </para>
///     <para>
///     热路径方法标记 <see cref="MethodImplOptions.AggressiveInlining" /> 以确保 JIT 内联。
///     </para>
/// </remarks>
public ref struct ByteBufferWriter
{
    private readonly Span<byte> _buffer;
    private int _position;

    /// <summary>
    ///     初始化 <see cref="ByteBufferWriter" /> 结构的新实例。
    /// </summary>
    /// <param name="buffer">要写入的目标字节缓冲区。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBufferWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    /// <summary>
    ///     获取当前写入位置。
    /// </summary>
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
    }

    /// <summary>
    ///     获取缓冲区总长度。
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Length;
    }

    /// <summary>
    ///     获取剩余可写入的字节数。
    /// </summary>
    public int Remaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Length - _position;
    }

    #region 基础写入

    /// <summary>
    ///     获取从当前位置开始的写入 Span。
    /// </summary>
    /// <param name="sizeHint">期望的写入大小，0 或负数表示剩余全部空间。</param>
    /// <returns>可写入的字节 Span。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var size = sizeHint <= 0 ? _buffer.Length - _position : sizeHint;
        if (_position + size > _buffer.Length)
        {
            throw new InvalidOperationException("写入空间不足");
        }
        return _buffer.Slice(_position, size);
    }

    /// <summary>
    ///     向前移动指定字节数的写入位置。
    /// </summary>
    /// <param name="bytes">要前进的字节数。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "前进字节数不能为负数");
        }
        var newPosition = _position + bytes;
        if (newPosition > _buffer.Length)
        {
            throw new InvalidOperationException("写入位置超出缓冲区范围");
        }
        _position = newPosition;
    }

    /// <summary>
    ///     将字节数据写入缓冲区并前进相应字节数。
    /// </summary>
    /// <param name="data">要写入的字节数据。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> data)
    {
        if (_position + data.Length > _buffer.Length)
        {
            throw new InvalidOperationException("写入数据超出缓冲区范围");
        }
        data.CopyTo(_buffer.Slice(_position));
        _position += data.Length;
    }

    #endregion

    #region 无符号整数写入

    /// <summary>
    ///     写入一个无符号 8 位整数并前进 1 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU8(byte value)
    {
        if (_position >= _buffer.Length)
        {
            throw new InvalidOperationException("写入位置超出缓冲区范围");
        }
        _buffer[_position++] = value;
    }

    /// <summary>
    ///     以小端序写入一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU16LE(ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以大端序写入一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU16BE(ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.Slice(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以小端序写入一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU32LE(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以大端序写入一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU32BE(uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.Slice(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以小端序写入一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU64LE(ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.Slice(_position), value);
        _position += 8;
    }

    /// <summary>
    ///     以大端序写入一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU64BE(ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(_buffer.Slice(_position), value);
        _position += 8;
    }

    #endregion

    #region 有符号整数写入

    /// <summary>
    ///     写入一个有符号 8 位整数并前进 1 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI8(sbyte value)
    {
        WriteU8((byte)value);
    }

    /// <summary>
    ///     以小端序写入一个有符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI16LE(short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(_buffer.Slice(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以大端序写入一个有符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI16BE(short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(_buffer.Slice(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以小端序写入一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI32LE(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以大端序写入一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI32BE(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(_buffer.Slice(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以小端序写入一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI64LE(long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(_position), value);
        _position += 8;
    }

    /// <summary>
    ///     以大端序写入一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI64BE(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(_buffer.Slice(_position), value);
        _position += 8;
    }

    #endregion

    #region 浮点数写入

    /// <summary>
    ///     以小端序写入一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF32LE(float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.Slice(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以大端序写入一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF32BE(float value)
    {
        BinaryPrimitives.WriteSingleBigEndian(_buffer.Slice(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以小端序写入一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF64LE(double value)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.Slice(_position), value);
        _position += 8;
    }

    /// <summary>
    ///     以大端序写入一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF64BE(double value)
    {
        BinaryPrimitives.WriteDoubleBigEndian(_buffer.Slice(_position), value);
        _position += 8;
    }

    #endregion

    #region LEB128 写入

    /// <summary>
    ///     以 LEB128 编码写入一个无符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLeb128U32(uint value)
    {
        while (true)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) b |= 0x80;
            _buffer[_position++] = b;
            if (value == 0) break;
        }
    }

    /// <summary>
    ///     以 LEB128 编码写入一个无符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLeb128U64(ulong value)
    {
        while (true)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) b |= 0x80;
            _buffer[_position++] = b;
            if (value == 0) break;
        }
    }

    /// <summary>
    ///     以 LEB128 编码写入一个有符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLeb128I32(int value)
    {
        var uvalue = (uint)(value < 0 ? (~(uint)(-value) << 1) | 1 : (uint)value << 1);
        while (true)
        {
            var b = (byte)(uvalue & 0x7F);
            uvalue >>= 7;
            if (uvalue != 0) b |= 0x80;
            _buffer[_position++] = b;
            if (uvalue == 0) break;
        }
    }

    /// <summary>
    ///     以 ZigZag + LEB128 编码写入一个有符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteZigZagLeb128I32(int value)
    {
        var zigzag = (uint)((value << 1) ^ (value >> 31));
        WriteLeb128U32(zigzag);
    }

    /// <summary>
    ///     以 ZigZag + LEB128 编码写入一个有符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteZigZagLeb128I64(long value)
    {
        var zigzag = (ulong)((value << 1) ^ (value >> 63));
        WriteLeb128U64(zigzag);
    }

    #endregion

    #region 字符串写入

    /// <summary>
    ///     写入原始字节形式的字符串数据。
    /// </summary>
    /// <param name="data">UTF-8 编码的字节数据。</param>
    public void WriteString(ReadOnlySpan<byte> data)
    {
        Write(data);
    }

    /// <summary>
    ///     将字符串以 UTF-8 编码写入缓冲区。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Write(bytes);
    }

    /// <summary>
    ///     写入以 null 终止的 UTF-8 字符串。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void WriteNullTerminatedString(string value)
    {
        WriteString(value);
        WriteU8(0);
    }

    /// <summary>
    ///     写入 LEB128 长度前缀的 UTF-8 字符串。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void WriteLeb128String(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteLeb128U32((uint)bytes.Length);
        Write(bytes);
    }

    #endregion
}

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using Acorn.Codec;

namespace Acorn.Frame;

/// <summary>
///     内存写入缓冲区，基于 <see cref="Span{T}" /> 提供零分配的二进制数据写入能力。
/// </summary>
/// <remarks>
///     <para>
///     ByteBufferWriter 是 ByteBuffer 的对称写入组件，所有写入方法使用 <see cref="Span{T}" /> 和
///     <see cref="BinaryPrimitives" />，避免任何堆分配和流包装开销。
///     </para>
///     <para>
///     当写入超出缓冲区容量时，自动扩容为原来的 2 倍，确保编码器无需预计算精确大小。
///     </para>
///     <para>
///     热路径方法标记 <see cref="MethodImplOptions.AggressiveInlining" /> 以确保 JIT 内联。
///     </para>
/// </remarks>
public ref struct ByteBufferWriter
{
    private byte[] _buffer;
    private int _position;
    private Endianness _endianness;

    /// <summary>
    ///     初始化 <see cref="ByteBufferWriter" /> 结构的新实例。
    /// </summary>
    /// <param name="buffer">要写入的目标字节缓冲区。</param>
    /// <param name="endianness">字节序，默认为小端序。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBufferWriter(Span<byte> buffer, Endianness endianness = Endianness.LittleEndian)
    {
        _buffer = buffer.ToArray();
        _position = 0;
        _endianness = endianness;
    }

    /// <summary>
    ///     初始化 <see cref="ByteBufferWriter" /> 结构的新实例。
    /// </summary>
    /// <param name="capacity">初始容量（字节）。</param>
    /// <param name="endianness">字节序，默认为小端序。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBufferWriter(int capacity, Endianness endianness = Endianness.LittleEndian)
    {
        _buffer = new byte[capacity];
        _position = 0;
        _endianness = endianness;
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

    /// <summary>
    ///     获取或设置字节序。通用写入方法（如 <see cref="WriteU16" />、<see cref="WriteU32" /> 等）
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
    ///     获取已写入数据的只读视图。
    /// </summary>
    public ReadOnlySpan<byte> WrittenData => new(_buffer, 0, _position);

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
        EnsureCapacity(size);
        return _buffer.AsSpan(_position, size);
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

        _position += bytes;
    }

    /// <summary>
    ///     将字节数据写入缓冲区并前进相应字节数。
    /// </summary>
    /// <param name="data">要写入的字节数据。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.AsSpan(_position));
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
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }

    /// <summary>
    ///     以小端序写入一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU16LE(ushort value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以大端序写入一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU16BE(ushort value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以小端序写入一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU32LE(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以大端序写入一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU32BE(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以小端序写入一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU64LE(ulong value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    /// <summary>
    ///     以大端序写入一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU64BE(ulong value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(_position), value);
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
        EnsureCapacity(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以大端序写入一个有符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI16BE(short value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(_position), value);
        _position += 2;
    }

    /// <summary>
    ///     以小端序写入一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI32LE(int value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以大端序写入一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI32BE(int value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以小端序写入一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI64LE(long value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    /// <summary>
    ///     以大端序写入一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI64BE(long value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteInt64BigEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    #endregion

    #region 浮点数写入

    /// <summary>
    ///     以小端序写入一个 16 位半精度浮点数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF16LE(Half value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_position), BitConverter.HalfToUInt16Bits(value));
        _position += 2;
    }

    /// <summary>
    ///     以大端序写入一个 16 位半精度浮点数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF16BE(Half value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), BitConverter.HalfToUInt16Bits(value));
        _position += 2;
    }

    /// <summary>
    ///     以小端序写入一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF32LE(float value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以大端序写入一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF32BE(float value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteSingleBigEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }

    /// <summary>
    ///     以小端序写入一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF64LE(double value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    /// <summary>
    ///     以大端序写入一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF64BE(double value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteDoubleBigEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }

    #endregion

    #region 通用字节序写入

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个无符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU16(ushort value)
    {
        if (_endianness == Endianness.LittleEndian) WriteU16LE(value); else WriteU16BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个无符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU32(uint value)
    {
        if (_endianness == Endianness.LittleEndian) WriteU32LE(value); else WriteU32BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个无符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteU64(ulong value)
    {
        if (_endianness == Endianness.LittleEndian) WriteU64LE(value); else WriteU64BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个有符号 16 位整数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI16(short value)
    {
        if (_endianness == Endianness.LittleEndian) WriteI16LE(value); else WriteI16BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个有符号 32 位整数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI32(int value)
    {
        if (_endianness == Endianness.LittleEndian) WriteI32LE(value); else WriteI32BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个有符号 64 位整数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteI64(long value)
    {
        if (_endianness == Endianness.LittleEndian) WriteI64LE(value); else WriteI64BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个 16 位半精度浮点数并前进 2 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF16(Half value)
    {
        if (_endianness == Endianness.LittleEndian) WriteF16LE(value); else WriteF16BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个 32 位浮点数并前进 4 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF32(float value)
    {
        if (_endianness == Endianness.LittleEndian) WriteF32LE(value); else WriteF32BE(value);
    }

    /// <summary>
    ///     以 <see cref="Endianness" /> 属性指定的字节序写入一个 64 位浮点数并前进 8 字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteF64(double value)
    {
        if (_endianness == Endianness.LittleEndian) WriteF64LE(value); else WriteF64BE(value);
    }

    #endregion

    #region LEB128 写入

    /// <summary>
    ///     直接写入一个字节到缓冲区指定位置，返回更新后的位置索引。不做容量检查，由调用方保证足够空间。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteU8Internal(byte[] buffer, int position, byte value)
    {
        buffer[position] = value;
        return position + 1;
    }

    /// <summary>
    ///     以 LEB128 编码写入一个无符号 32 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLeb128U32(uint value)
    {
        if (value < 0x80)
        {
            _position = WriteU8Internal(_buffer, _position, (byte)value);
            return;
        }

        EnsureCapacity(5);

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
        if (value < 0x80)
        {
            _position = WriteU8Internal(_buffer, _position, (byte)value);
            return;
        }

        EnsureCapacity(10);

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
        if (value >= 0 && value < 0x40)
        {
            _position = WriteU8Internal(_buffer, _position, (byte)value);
            return;
        }

        EnsureCapacity(5);
        var more = true;

        while (more)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;

            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
            {
                more = false;
            }
            else
            {
                b |= 0x80;
            }

            _buffer[_position++] = b;
        }
    }

    /// <summary>
    ///     以 LEB128 编码写入一个有符号 64 位整数并前进相应字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLeb128I64(long value)
    {
        if (value >= 0 && value < 0x40)
        {
            _position = WriteU8Internal(_buffer, _position, (byte)value);
            return;
        }

        EnsureCapacity(10);
        var more = true;

        while (more)
        {
            var b = (byte)(value & 0x7F);
            value >>= 7;

            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
            {
                more = false;
            }
            else
            {
                b |= 0x80;
            }

            _buffer[_position++] = b;
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
    ///     将字符串以 UTF-8 编码写入缓冲区（零分配）。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void WriteString(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(maxByteCount);
        var written = Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position));
        _position += written;
    }

    /// <summary>
    ///     写入以 null 终止的 UTF-8 字符串（零分配）。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void WriteNullTerminatedString(string value)
    {
        WriteString(value);
        WriteU8(0);
    }

    /// <summary>
    ///     写入 LEB128 长度前缀的 UTF-8 字符串（零分配）。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void WriteLeb128String(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(maxByteCount + 5);

        var leb128Start = _position;
        WriteLeb128U32(0);
        var written = Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position));
        _position += written;

        var savedPosition = _position;
        _position = leb128Start;
        WriteLeb128U32((uint)written);
        _position = savedPosition;
    }

    #endregion

    #region 输出方法

    /// <summary>
    ///     将已写入的数据复制到新数组。
    /// </summary>
    /// <returns>包含已写入数据的字节数组。</returns>
    public byte[] ToArray()
    {
        return WrittenData.ToArray();
    }

    #endregion

    #region 私有方法

    /// <summary>
    ///     确保缓冲区有足够的剩余容量，不足时自动扩容。
    /// </summary>
    /// <param name="needed">需要的额外字节数。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int needed)
    {
        if (_position + needed <= _buffer.Length)
        {
            return;
        }

        var newCapacity = Math.Max(_buffer.Length * 2, _position + needed);
        var newBuffer = new byte[newCapacity];
        Array.Copy(_buffer, newBuffer, _position);
        _buffer = newBuffer;
    }

    #endregion
}

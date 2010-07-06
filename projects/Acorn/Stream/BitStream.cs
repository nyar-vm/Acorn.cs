using System.Runtime.CompilerServices;

namespace Acorn.Stream;

/// <summary>
///     比特流结构体，支持按位读写数据，最小化网络传输数据量。
/// </summary>
/// <remarks>
///     比特流适用于需要将数据压缩到比特级别的场景，例如网络同步、紧凑存储等。
///     写入时自动扩容，读取时按比特位解析。
/// </remarks>
public ref struct BitStream
{
    #region 字段

    private byte[] _buffer;
    private int _bitPosition;
    private int _bitLength;

    #endregion

    #region 属性

    /// <summary>
    ///     获取底层缓冲区的只读视图。
    /// </summary>
    public ReadOnlySpan<byte> Buffer => new(_buffer, 0, ByteLength);

    /// <summary>
    ///     获取当前比特位置。
    /// </summary>
    public int BitPosition => _bitPosition;

    /// <summary>
    ///     获取比特总长度。
    /// </summary>
    public int BitLength => _bitLength;

    /// <summary>
    ///     获取字节总长度（向上取整）。
    /// </summary>
    public int ByteLength => (_bitLength + 7) >> 3;

    /// <summary>
    ///     获取剩余可读比特数。
    /// </summary>
    public int RemainingBits => _bitLength - _bitPosition;

    /// <summary>
    ///     获取是否已到达末尾。
    /// </summary>
    public bool IsEnd => _bitPosition >= _bitLength;

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化写入用比特流。
    /// </summary>
    /// <param name="capacity">初始容量（字节）。</param>
    public BitStream(int capacity)
    {
        _buffer = new byte[capacity];
        _bitPosition = 0;
        _bitLength = 0;
    }

    /// <summary>
    ///     初始化读取用比特流。
    /// </summary>
    /// <param name="data">要读取的数据。</param>
    public BitStream(ReadOnlySpan<byte> data)
    {
        _buffer = new byte[data.Length];
        data.CopyTo(_buffer);
        _bitPosition = 0;
        _bitLength = data.Length * 8;
    }

    #endregion

    #region 写入方法

    /// <summary>
    ///     写入单个比特。
    /// </summary>
    /// <param name="value">比特值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBit(bool value)
    {
        EnsureWriteCapacity(1);

        if (value)
        {
            var byteIndex = _bitPosition >> 3;
            var bitIndex = _bitPosition & 7;
            _buffer[byteIndex] |= (byte)(1 << bitIndex);
        }

        _bitPosition++;
        _bitLength = Math.Max(_bitLength, _bitPosition);
    }

    /// <summary>
    ///     写入指定比特数的无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    /// <param name="bits">比特数（1-32）。</param>
    public void WriteBits(uint value, int bits)
    {
        if (bits is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(bits), $"比特数必须在 1-32 之间，当前值：{bits}");
        }

        EnsureWriteCapacity(bits);

        for (var i = 0; i < bits; i++)
        {
            var bitValue = (value >> i) & 1;
            var byteIndex = _bitPosition >> 3;
            var bitIndex = _bitPosition & 7;

            if (bitValue != 0)
            {
                _buffer[byteIndex] |= (byte)(1 << bitIndex);
            }
            else
            {
                _buffer[byteIndex] &= (byte)~(1 << bitIndex);
            }

            _bitPosition++;
        }

        _bitLength = Math.Max(_bitLength, _bitPosition);
    }

    /// <summary>
    ///     写入字节。
    /// </summary>
    /// <param name="value">字节值。</param>
    public void WriteByte(byte value)
    {
        WriteBits(value, 8);
    }

    /// <summary>
    ///     写入 16 位无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteUInt16(ushort value)
    {
        WriteBits(value, 16);
    }

    /// <summary>
    ///     写入 32 位无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteUInt32(uint value)
    {
        WriteBits(value, 32);
    }

    /// <summary>
    ///     写入 64 位无符号整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteUInt64(ulong value)
    {
        WriteBits((uint)(value & 0xFFFFFFFF), 32);
        WriteBits((uint)(value >> 32), 32);
    }

    /// <summary>
    ///     写入有符号 32 位整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteInt32(int value)
    {
        WriteBits((uint)value, 32);
    }

    /// <summary>
    ///     写入有符号 64 位整数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteInt64(long value)
    {
        WriteUInt64((ulong)value);
    }

    /// <summary>
    ///     写入 32 位浮点数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteFloat(float value)
    {
        var bits = BitConverter.SingleToUInt32Bits(value);
        WriteBits(bits, 32);
    }

    /// <summary>
    ///     写入 64 位浮点数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    public void WriteDouble(double value)
    {
        var bits = BitConverter.DoubleToUInt64Bits(value);
        WriteUInt64(bits);
    }

    /// <summary>
    ///     写入布尔值（1 比特）。
    /// </summary>
    /// <param name="value">布尔值。</param>
    public void WriteBool(bool value)
    {
        WriteBit(value);
    }

    /// <summary>
    ///     写入指定范围内的整数，使用最小比特数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    /// <param name="min">最小值（含）。</param>
    /// <param name="max">最大值（含）。</param>
    public void WriteRangedInt(int value, int min, int max)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"值 {value} 不在范围 [{min}, {max}] 内");
        }

        var range = (uint)(max - min);
        var bits = BitsRequired(range);
        WriteBits((uint)(value - min), bits);
    }

    /// <summary>
    ///     写入指定范围内的浮点数，量化为指定比特数。
    /// </summary>
    /// <param name="value">要写入的值。</param>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    /// <param name="bits">量化比特数。</param>
    public void WriteRangedFloat(float value, float min, float max, int bits)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"值 {value} 不在范围 [{min}, {max}] 内");
        }

        var normalized = (value - min) / (max - min);
        var maxQuantized = (1u << bits) - 1;
        var quantized = (uint)(normalized * maxQuantized + 0.5f);
        WriteBits(quantized, bits);
    }

    /// <summary>
    ///     写入字节数组。
    /// </summary>
    /// <param name="data">要写入的数据。</param>
    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        WriteBits((uint)data.Length, 16);

        foreach (var b in data)
        {
            WriteByte(b);
        }
    }

    /// <summary>
    ///     写入字符串（UTF-8 编码）。
    /// </summary>
    /// <param name="value">要写入的字符串。</param>
    public void WriteString(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteBytes(bytes);
    }

    #endregion

    #region 读取方法

    /// <summary>
    ///     读取单个比特。
    /// </summary>
    /// <returns>比特值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBit()
    {
        if (_bitPosition >= _bitLength)
        {
            throw new InvalidOperationException("已到达比特流末尾，无法读取更多数据");
        }

        var byteIndex = _bitPosition >> 3;
        var bitIndex = _bitPosition & 7;
        var bit = (_buffer[byteIndex] >> bitIndex) & 1;
        _bitPosition++;

        return bit != 0;
    }

    /// <summary>
    ///     读取指定比特数的无符号整数。
    /// </summary>
    /// <param name="bits">比特数（1-32）。</param>
    /// <returns>读取的无符号整数。</returns>
    public uint ReadBits(int bits)
    {
        if (bits is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(bits), $"比特数必须在 1-32 之间，当前值：{bits}");
        }

        if (_bitPosition + bits > _bitLength)
        {
            throw new InvalidOperationException($"比特流剩余 {_bitLength - _bitPosition} 比特，不足以读取 {bits} 比特");
        }

        uint value = 0;

        for (var i = 0; i < bits; i++)
        {
            var byteIndex = _bitPosition >> 3;
            var bitIndex = _bitPosition & 7;
            var bit = (_buffer[byteIndex] >> bitIndex) & 1;
            value |= (uint)(bit << i);
            _bitPosition++;
        }

        return value;
    }

    /// <summary>
    ///     读取字节。
    /// </summary>
    /// <returns>读取的字节值。</returns>
    public byte ReadByte()
    {
        return (byte)ReadBits(8);
    }

    /// <summary>
    ///     读取 16 位无符号整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public ushort ReadUInt16()
    {
        return (ushort)ReadBits(16);
    }

    /// <summary>
    ///     读取 32 位无符号整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public uint ReadUInt32()
    {
        return ReadBits(32);
    }

    /// <summary>
    ///     读取 64 位无符号整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public ulong ReadUInt64()
    {
        var low = ReadBits(32);
        var high = ReadBits(32);
        return ((ulong)high << 32) | low;
    }

    /// <summary>
    ///     读取有符号 32 位整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public int ReadInt32()
    {
        return (int)ReadBits(32);
    }

    /// <summary>
    ///     读取有符号 64 位整数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public long ReadInt64()
    {
        return (long)ReadUInt64();
    }

    /// <summary>
    ///     读取 32 位浮点数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public float ReadFloat()
    {
        var bits = ReadBits(32);
        return BitConverter.UInt32BitsToSingle(bits);
    }

    /// <summary>
    ///     读取 64 位浮点数。
    /// </summary>
    /// <returns>读取的值。</returns>
    public double ReadDouble()
    {
        var bits = ReadUInt64();
        return BitConverter.UInt64BitsToDouble(bits);
    }

    /// <summary>
    ///     读取布尔值。
    /// </summary>
    /// <returns>读取的布尔值。</returns>
    public bool ReadBool()
    {
        return ReadBit();
    }

    /// <summary>
    ///     读取指定范围内的整数。
    /// </summary>
    /// <param name="min">最小值（含）。</param>
    /// <param name="max">最大值（含）。</param>
    /// <returns>读取的值。</returns>
    public int ReadRangedInt(int min, int max)
    {
        var range = (uint)(max - min);
        var bits = BitsRequired(range);
        var raw = ReadBits(bits);
        return (int)raw + min;
    }

    /// <summary>
    ///     读取指定范围内的量化浮点数。
    /// </summary>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    /// <param name="bits">量化比特数。</param>
    /// <returns>读取的浮点值。</returns>
    public float ReadRangedFloat(float min, float max, int bits)
    {
        var maxQuantized = (1u << bits) - 1;
        var quantized = ReadBits(bits);
        var normalized = (float)quantized / maxQuantized;
        return min + normalized * (max - min);
    }

    /// <summary>
    ///     读取字节数组。
    /// </summary>
    /// <returns>读取的字节数组。</returns>
    public byte[] ReadBytes()
    {
        var length = (int)ReadBits(16);
        var data = new byte[length];

        for (var i = 0; i < length; i++)
        {
            data[i] = ReadByte();
        }

        return data;
    }

    /// <summary>
    ///     读取字符串（UTF-8 编码）。
    /// </summary>
    /// <returns>读取的字符串。</returns>
    public string ReadString()
    {
        var bytes = ReadBytes();
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     重置读取位置到起始。
    /// </summary>
    public void Reset()
    {
        _bitPosition = 0;
    }

    /// <summary>
    ///     将比特流数据复制到新数组。
    /// </summary>
    /// <returns>包含比特流数据的字节数组。</returns>
    public byte[] ToArray()
    {
        var result = new byte[ByteLength];
        Array.Copy(_buffer, result, ByteLength);
        return result;
    }

    /// <summary>
    ///     对齐到下一个字节边界。
    /// </summary>
    public void AlignToByte()
    {
        var remainder = _bitPosition & 7;

        if (remainder != 0)
        {
            _bitPosition += 8 - remainder;
            _bitLength = Math.Max(_bitLength, _bitPosition);
        }
    }

    /// <summary>
    ///     计算表示指定值所需的最小比特数。
    /// </summary>
    /// <param name="value">要表示的最大值。</param>
    /// <returns>所需比特数。</returns>
    public static int BitsRequired(uint value)
    {
        if (value == 0)
        {
            return 1;
        }

        var bits = 0;

        while (value > 0)
        {
            value >>= 1;
            bits++;
        }

        return bits;
    }

    #endregion

    #region 私有方法

    /// <summary>
    ///     确保写入容量足够。
    /// </summary>
    /// <param name="bitsToWrite">需要写入的比特数。</param>
    private void EnsureWriteCapacity(int bitsToWrite)
    {
        var requiredBytes = (_bitPosition + bitsToWrite + 7) >> 3;

        if (requiredBytes <= _buffer.Length)
        {
            return;
        }

        var newCapacity = Math.Max(_buffer.Length * 2, requiredBytes);
        var newBuffer = new byte[newCapacity];
        Array.Copy(_buffer, newBuffer, _buffer.Length);
        _buffer = newBuffer;
    }

    #endregion
}

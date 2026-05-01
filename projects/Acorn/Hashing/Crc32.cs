namespace Acorn.Hashing;

/// <summary>
///     CRC-32 校验和计算器，支持反射和非反射两种算法。
/// </summary>
/// <remarks>
///     反射算法（Reflected=True）：多项式 0xEDB88320，用于 PNG、gzip、zlib 等格式。
///     非反射算法（Reflected=False）：多项式 0x04C11DB7，用于 OGG、MPEG-2 等格式。
/// </remarks>
public sealed class Crc32
{
    /// <summary>
    ///     反射算法使用的 CRC-32 多项式（0xEDB88320）。
    /// </summary>
    public const uint ReflectedPolynomial = 0xEDB88320;

    /// <summary>
    ///     非反射算法使用的 CRC-32 多项式（0x04C11DB7）。
    /// </summary>
    public const uint NormalPolynomial = 0x04C11DB7;

    private readonly uint[] _table;
    private readonly uint _initialValue;
    private readonly uint _finalXor;
    private readonly bool _reflected;

    private uint _crc;

    /// <summary>
    ///     使用默认参数初始化 <see cref="Crc32" /> 的新实例（反射算法，初始值 0xFFFFFFFF，输出异或 0xFFFFFFFF，用于 PNG/zlib/gzip）。
    /// </summary>
    public Crc32()
        : this(ReflectedPolynomial, 0xFFFFFFFF, 0xFFFFFFFF, reflected: true)
    {
    }

    /// <summary>
    ///     使用指定参数初始化 <see cref="Crc32" /> 的新实例。
    /// </summary>
    /// <param name="polynomial">CRC 多项式。</param>
    /// <param name="initialValue">CRC 初始值。</param>
    /// <param name="finalXor">输出前与 CRC 值异或的掩码。</param>
    /// <param name="reflected">是否使用反射算法。</param>
    public Crc32(uint polynomial, uint initialValue, uint finalXor, bool reflected)
    {
        _table = BuildTable(polynomial, reflected);
        _initialValue = initialValue;
        _finalXor = finalXor;
        _reflected = reflected;
        _crc = initialValue;
    }

    /// <summary>
    ///     获取当前 CRC-32 值。
    /// </summary>
    public uint Value => _crc ^ _finalXor;

    /// <summary>
    ///     重置 CRC 计算器到初始状态。
    /// </summary>
    public void Reset()
    {
        _crc = _initialValue;
    }

    /// <summary>
    ///     使用字节数据更新 CRC 值。
    /// </summary>
    /// <param name="data">输入数据。</param>
    public void Update(ReadOnlySpan<byte> data)
    {
        if (_reflected)
        {
            foreach (var b in data)
            {
                _crc = _table[(_crc ^ b) & 0xFF] ^ (_crc >> 8);
            }
        }
        else
        {
            foreach (var b in data)
            {
                _crc = (_crc << 8) ^ _table[(_crc >> 24) ^ b];
            }
        }
    }

    /// <summary>
    ///     使用字节数组更新 CRC 值。
    /// </summary>
    /// <param name="data">输入数据。</param>
    public void Update(byte[] data)
    {
        Update(data.AsSpan());
    }

    /// <summary>
    ///     计算数据的 CRC-32 校验和（使用默认反射算法参数）。
    /// </summary>
    /// <param name="data">输入数据。</param>
    /// <returns>CRC-32 校验和。</returns>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var crc = new Crc32();

        crc.Update(data);

        return crc.Value;
    }

    /// <summary>
    ///     计算数据的 CRC-32 校验和（使用默认反射算法参数）。
    /// </summary>
    /// <param name="data">输入数据。</param>
    /// <returns>CRC-32 校验和。</returns>
    public static uint Compute(byte[] data)
    {
        return Compute(data.AsSpan());
    }

    /// <summary>
    ///     构建 CRC 查找表。
    /// </summary>
    private static uint[] BuildTable(uint polynomial, bool reflected)
    {
        var table = new uint[256];

        for (var i = 0u; i < 256; i++)
        {
            if (reflected)
            {
                var crc = i;

                for (var j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                    {
                        crc = (crc >> 1) ^ polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }

                table[i] = crc;
            }
            else
            {
                var r = i << 24;

                for (var j = 0; j < 8; j++)
                {
                    if ((r & 0x80000000) != 0)
                    {
                        r = (r << 1) ^ polynomial;
                    }
                    else
                    {
                        r <<= 1;
                    }
                }

                table[i] = r;
            }
        }

        return table;
    }
}

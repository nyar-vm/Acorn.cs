using Acorn.Frame;
using Acorn.Basis.Data;

namespace Acorn.Basis.Scanner;

/// <summary>
///     Basis/KTX2 扫描器。
/// </summary>
public ref struct BasisScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="BasisScanner" /> 结构的新实例。
    /// </summary>
    public BasisScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     扫描头部信息。
    /// </summary>
    public BasisScanHeader ScanHeader()
    {
        if (_buffer.Length < 12)
        {
            return new BasisScanHeader();
        }

        if (_buffer.MatchMagic(BasisConstants.Ktx2Magic))
        {
            return new BasisScanHeader { Format = "KTX2" };
        }

        if (_buffer.Length >= 2 && _buffer.Data[..2].SequenceEqual(BasisConstants.BasisMagic))
        {
            return new BasisScanHeader { Format = "Basis" };
        }

        return new BasisScanHeader();
    }

    /// <summary>
    ///     是否为 Basis/KTX2 格式。
    /// </summary>
    public bool IsBasisOrKtx2()
    {
        if (_buffer.Length >= 12 && _buffer.MatchMagic(BasisConstants.Ktx2Magic)) return true;
        if (_buffer.Length >= 2 && _buffer.Data[..2].SequenceEqual(BasisConstants.BasisMagic)) return true;
        return false;
    }
}

/// <summary>
///     Basis 扫描头部信息。
/// </summary>
public sealed class BasisScanHeader
{
    /// <summary>
    ///     格式名称。
    /// </summary>
    public string Format { get; init; } = "未知";
}

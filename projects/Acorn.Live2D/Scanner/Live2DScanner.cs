using System.Text;
using System.Text.Json;
using Acorn.Frame;
using Acorn.Live2D.Data;

namespace Acorn.Live2D.Scanner;

/// <summary>
///     Live2D Cubism 模型扫描器，基于 <see cref="ByteBuffer" /> 提供对模型 JSON / moc3 二进制文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     扫描器通过流式 JSON 解析或二进制头读取快速提取版本、文件引用、参数数量等元信息，
///     避免完整反序列化带来的内存分配。
///     moc3 文件头格式：4 字节魔数 "MOC3" + 1 字节版本 + 1 字节字节序标志 + 2 字节修订号。
/// </remarks>
public ref struct Live2DScanner : ILive2DScanner
{
    private ByteBuffer _buffer;

    public Live2DScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public int Length => _buffer.Length;

    public bool IsEndOfData => _buffer.IsEnd;

    public ReadOnlySpan<byte> Remaining => _buffer.RemainingSpan;

    public void Advance(int count)
    {
        _buffer.Advance(count);
    }

    public ReadOnlySpan<byte> Peek(int count)
    {
        return _buffer.Peek(count);
    }

    public ReadOnlySpan<byte> Read(int count)
    {
        return _buffer.ReadBytes(count);
    }

    public bool MatchMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.MatchMagic(magic);
    }

    public bool ConsumeMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.ConsumeMagic(magic);
    }

    /// <summary>
    ///     扫描 model3.json 文件，提取文件引用列表。
    /// </summary>
    public List<string> ScanFileReferences()
    {
        var references = new List<string>();
        var content = Encoding.UTF8.GetString(_buffer.Data);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (!root.TryGetProperty("FileReferences", out var fileRefs)) return references;

        if (fileRefs.TryGetProperty("Moc", out var moc))
        {
            references.Add(moc.GetString()!);
        }

        if (fileRefs.TryGetProperty("Textures", out var textures))
        {
            foreach (var texture in textures.EnumerateArray())
            {
                references.Add(texture.GetString()!);
            }
        }

        if (fileRefs.TryGetProperty("Physics", out var physics))
        {
            references.Add(physics.GetString()!);
        }

        if (fileRefs.TryGetProperty("Motions", out var motions))
        {
            foreach (var motion in motions.EnumerateArray())
            {
                if (motion.TryGetProperty("File", out var motionFile))
                {
                    references.Add(motionFile.GetString()!);
                }
            }
        }

        if (fileRefs.TryGetProperty("Expressions", out var expressions))
        {
            foreach (var expression in expressions.EnumerateArray())
            {
                if (expression.TryGetProperty("File", out var expressionFile))
                {
                    references.Add(expressionFile.GetString()!);
                }
            }
        }

        return references;
    }

    /// <summary>
    ///     扫描 moc3 文件头，提取版本和字节序信息。
    /// </summary>
    /// <remarks>
    ///     moc3 文件头格式：4 字节魔数 + 1 字节版本号 + 1 字节字节序标志 + 2 字节修订号。
    ///     字节序标志：0 = 小端序，非 0 = 大端序。
    /// </remarks>
    public (int Version, bool IsBigEndian, int Revision) ScanMoc3Header()
    {
        if (_buffer.Length < 8)
        {
            throw new InvalidDataException("moc3 文件数据过短，无法读取文件头");
        }

        if (!_buffer.MatchMagic(Live2DConstants.Moc3MagicNumber))
        {
            throw new InvalidDataException("moc3 文件魔数不匹配");
        }

        _buffer.ConsumeMagic(Live2DConstants.Moc3MagicNumber);
        var version = _buffer.ReadU8();
        var isBigEndian = _buffer.ReadU8() != 0;
        var revision = _buffer.ReadI16LE();

        return (version, isBigEndian, revision);
    }

    /// <summary>
    ///     扫描 moc3 文件，从偏移表提取参数数量。
    /// </summary>
    public int ScanMoc3ParameterCount()
    {
        if (_buffer.Length < 12)
        {
            return 0;
        }

        var (_, isBigEndian, _) = ScanMoc3Header();
        var version = _buffer.ReadU8At(4);

        var parameterCountOffset = GetCountInfoOffset(version) + 4;

        if (parameterCountOffset + 4 > _buffer.Length)
        {
            return 0;
        }

        return _buffer.ReadI32At(parameterCountOffset, isBigEndian);
    }

    /// <summary>
    ///     扫描 moc3 文件，从偏移表提取部件数量。
    /// </summary>
    public int ScanMoc3PartCount()
    {
        if (_buffer.Length < 16)
        {
            return 0;
        }

        var (_, isBigEndian, _) = ScanMoc3Header();
        var version = _buffer.ReadU8At(4);

        var partCountOffset = GetCountInfoOffset(version) + 8;

        if (partCountOffset + 4 > _buffer.Length)
        {
            return 0;
        }

        return _buffer.ReadI32At(partCountOffset, isBigEndian);
    }

    /// <summary>
    ///     扫描 moc3 文件，从偏移表提取绘制对象数量。
    /// </summary>
    public int ScanMoc3DrawableCount()
    {
        if (_buffer.Length < 20)
        {
            return 0;
        }

        var (_, isBigEndian, _) = ScanMoc3Header();
        var version = _buffer.ReadU8At(4);

        var drawableCountOffset = GetCountInfoOffset(version) + 12;

        if (drawableCountOffset + 4 > _buffer.Length)
        {
            return 0;
        }

        return _buffer.ReadI32At(drawableCountOffset, isBigEndian);
    }

    private static int GetCountInfoOffset(int version)
    {
        var offsetTableSize = version switch
        {
            3 => 88,
            4 => 93,
            5 => 100,
            _ => 100
        };

        return 8 + offsetTableSize;
    }
}

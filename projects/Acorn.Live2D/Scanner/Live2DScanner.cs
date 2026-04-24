using System.IO;
using System.Text;
using System.Text.Json;
using Acorn.Frame;
using Acorn.Live2D.Data;

namespace Acorn.Live2D.Scanner;

/// <summary>
///     Live2D Cubism 模型扫描器，基于 <see cref="SpanScanner" /> 提供对模型 JSON / moc3 二进制文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     扫描器通过流式 JSON 解析或二进制头读取快速提取版本、文件引用、参数数量等元信息，
///     避免完整反序列化带来的内存分配。
///     moc3 文件头格式：4 字节魔数 "MOC3" + 1 字节版本 + 1 字节字节序标志 + 2 字节修订号。
///     段偏移表紧跟文件头，通过 <see cref="Live2DConstants" /> 计算各段偏移。
/// </remarks>
public ref struct Live2DScanner : ILive2DScanner
{
    private SpanScanner _scanner;

    public Live2DScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 model3.json 文件，提取文件引用列表。
    /// </summary>
    public List<string> ScanFileReferences()
    {
        var references = new List<string>();
        var content = Encoding.UTF8.GetString(_scanner.Data);

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
        if (_scanner.Length < Live2DConstants.HeaderSize)
        {
            throw new InvalidDataException("moc3 文件数据过短，无法读取文件头");
        }

        if (!_scanner.MatchMagic(Live2DConstants.Moc3MagicNumber))
        {
            throw new InvalidDataException("moc3 文件魔数不匹配");
        }

        _scanner.ConsumeMagic(Live2DConstants.Moc3MagicNumber);
        var version = _scanner.Buffer.ReadU8();
        var isBigEndian = _scanner.Buffer.ReadU8() != 0;
        var revision = _scanner.Buffer.ReadI16LE();

        return (version, isBigEndian, revision);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取参数数量。
    /// </summary>
    /// <remarks>
    ///     通过段偏移表中 CountInfo 段的偏移量定位计数表，
    ///     然后从计数表中读取参数数量（偏移 0，i32）。
    /// </remarks>
    public int ScanMoc3ParameterCount()
    {
        return ScanCountInfoField(Live2DConstants.CountInfoParameterCountOffset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取部件数量。
    /// </summary>
    /// <remarks>
    ///     通过段偏移表中 CountInfo 段的偏移量定位计数表，
    ///     然后从计数表中读取部件数量（偏移 4，i32）。
    /// </remarks>
    public int ScanMoc3PartCount()
    {
        return ScanCountInfoField(Live2DConstants.CountInfoPartCountOffset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取绘制对象数量。
    /// </summary>
    /// <remarks>
    ///     通过段偏移表中 CountInfo 段的偏移量定位计数表，
    ///     然后从计数表中读取绘制对象数量（偏移 8，i32）。
    /// </remarks>
    public int ScanMoc3DrawableCount()
    {
        return ScanCountInfoField(Live2DConstants.CountInfoDrawableCountOffset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取变形器数量（v4+）。
    /// </summary>
    public int ScanMoc3DeformerCount()
    {
        return ScanCountInfoField(Live2DConstants.CountInfoDeformerCountOffset);
    }

    /// <summary>
    ///     扫描 moc3 文件，从段偏移表定位 CountInfo 段并提取纹理数量。
    /// </summary>
    public int ScanMoc3TextureCount()
    {
        return ScanCountInfoField(Live2DConstants.CountInfoTextureCountOffset);
    }

    private int ScanCountInfoField(int fieldOffset)
    {
        if (_scanner.Length < Live2DConstants.HeaderSize + Live2DConstants.OffsetTableEntrySize)
        {
            return 0;
        }

        var (_, isBigEndian, _) = ScanMoc3Header();
        var version = _scanner.Buffer.ReadU8At(4);

        var countInfoSectionOffset = ReadCountInfoSectionOffset(version, isBigEndian);
        if (countInfoSectionOffset == 0)
        {
            return 0;
        }

        var fieldPosition = countInfoSectionOffset + fieldOffset;
        if (fieldPosition + 4 > _scanner.Length)
        {
            return 0;
        }

        return _scanner.Buffer.ReadI32At(fieldPosition, isBigEndian);
    }

    private int ReadCountInfoSectionOffset(int version, bool isBigEndian)
    {
        var countInfoEntryPosition = Live2DConstants.HeaderSize + (int)Moc3Section.CountInfo * Live2DConstants.OffsetTableEntrySize;

        if (countInfoEntryPosition + 4 > _scanner.Length)
        {
            return 0;
        }

        return _scanner.Buffer.ReadI32At(countInfoEntryPosition, isBigEndian);
    }
}

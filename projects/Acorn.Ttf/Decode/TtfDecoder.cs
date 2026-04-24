using Acorn.Frame;
using Acorn.Ttf.Data;

namespace Acorn.Ttf.Decode;

/// <summary>
///     TrueType/OpenType 字体文件解码器，将字体格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     TrueType 是 Apple 和 Microsoft 的字体格式，OpenType 是其扩展版本。
///     解码器解析字体偏移表和表记录，提取基本元信息，不执行完整字形渲染。
/// </remarks>
public ref struct TtfDecoder
{
    private ByteBuffer _buffer;
    private TtfFontType _fontType;

    /// <summary>
    ///     初始化 <see cref="TtfDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">TTF 二进制数据。</param>
    public TtfDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
        _fontType = TtfFontType.Unknown;
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 TrueType/OpenType 字体文件。
    /// </summary>
    /// <returns>字体数据。</returns>
    public TtfFontData Decode()
    {
        var (fontType, tableCount, tables) = ReadOffsetTable();

        if (tableCount == 0)
        {
            return new TtfFontData { FontType = fontType, TableCount = 0, Tables = [] };
        }

        var headTable = FindTable(tables, TtfTableNames.Head);
        var nameTable = FindTable(tables, TtfTableNames.Name);

        var headInfo = headTable != null ? ReadHeadTable(headTable) : null;
        var nameInfo = nameTable != null ? ReadNameTable(nameTable) : null;

        return new TtfFontData
        {
            FontType = fontType,
            TableCount = tableCount,
            Tables = tables,
            HeadInfo = headInfo,
            NameInfo = nameInfo
        };
    }

    /// <summary>
    ///     仅解码字体头部信息。
    /// </summary>
    public (TtfFontType FontType, ushort TableCount) DecodeHeader()
    {
        var (fontType, tableCount, _) = ReadOffsetTable();
        return (fontType, tableCount);
    }

    #region 私有解析方法

    private (TtfFontType, ushort, List<TtfTableRecord>) ReadOffsetTable()
    {
        if (_buffer.Remaining < TtfConstants.OffsetTableSize)
        {
            throw new InvalidDataException("TTF 文件数据过短，无法读取偏移表");
        }

        var sfVersion = _buffer.ReadU32BE();
        TtfFontType fontType;

        if (sfVersion == TtfConstants.TrueTypeMagic)
        {
            fontType = TtfFontType.TrueType;
        }
        else if (_buffer.Position >= 4 && sfVersion == 0x4F54544F)
        {
            fontType = TtfFontType.Cff;
        }
        else if (sfVersion == 0x74746366)
        {
            fontType = TtfFontType.Collection;
        }
        else
        {
            throw new InvalidDataException($"TTF 版本标识无效：0x{sfVersion:X8}");
        }

        _fontType = fontType;

        if (fontType == TtfFontType.Collection)
        {
            return (fontType, 0, []);
        }

        var tableCount = _buffer.ReadU16BE();

        _buffer.Advance(6);

        var tables = new List<TtfTableRecord>(tableCount);

        for (var i = 0; i < tableCount; i++)
        {
            tables.Add(ReadTableRecord());
        }

        return (fontType, tableCount, tables);
    }

    private TtfTableRecord ReadTableRecord()
    {
        var tagBytes = _buffer.ReadBytes(4).ToArray();
        var tag = System.Text.Encoding.ASCII.GetString(tagBytes);
        var checksum = _buffer.ReadU32BE();
        var offset = _buffer.ReadU32BE();
        var length = _buffer.ReadU32BE();

        return new TtfTableRecord
        {
            Tag = tag,
            Checksum = checksum,
            Offset = offset,
            Length = length
        };
    }

    private static TtfTableRecord? FindTable(IReadOnlyList<TtfTableRecord> tables, string tag)
    {
        foreach (var table in tables)
        {
            if (table.Tag == tag)
            {
                return table;
            }
        }

        return null;
    }

    private TtfHeadInfo? ReadHeadTable(TtfTableRecord record)
    {
        if ((int)(record.Offset + 54) > _buffer.Length)
        {
            return null;
        }

        var savedPos = _buffer.Position;
        _buffer.Position = (int)record.Offset;

        var version = _buffer.ReadU32BE();
        var fontRevision = _buffer.ReadU32BE();
        _buffer.Advance(8);
        var unitsPerEm = _buffer.ReadU16BE();
        _buffer.Advance(8);
        var created = _buffer.ReadI64BE();
        var modified = _buffer.ReadI64BE();
        _buffer.Advance(12);
        var xMin = _buffer.ReadI16BE();
        var yMin = _buffer.ReadI16BE();
        var xMax = _buffer.ReadI16BE();
        var yMax = _buffer.ReadI16BE();

        _buffer.Position = savedPos;

        return new TtfHeadInfo
        {
            Version = version,
            UnitsPerEm = unitsPerEm,
            Created = created,
            Modified = modified,
            XMin = xMin,
            YMin = yMin,
            XMax = xMax,
            YMax = yMax
        };
    }

    private TtfNameInfo? ReadNameTable(TtfTableRecord record)
    {
        if ((int)(record.Offset + 6) > _buffer.Length)
        {
            return null;
        }

        var savedPos = _buffer.Position;
        _buffer.Position = (int)record.Offset;

        var format = _buffer.ReadU16BE();
        var count = _buffer.ReadU16BE();
        var stringOffset = _buffer.ReadU16BE();

        var familyName = string.Empty;
        var subFamilyName = string.Empty;
        var fullName = string.Empty;
        var versionStr = string.Empty;

        for (var i = 0; i < count && !_buffer.IsEnd; i++)
        {
            var platformID = _buffer.ReadU16BE();
            var encodingID = _buffer.ReadU16BE();
            var languageID = _buffer.ReadU16BE();
            var nameID = _buffer.ReadU16BE();
            var length = _buffer.ReadU16BE();
            var offset = _buffer.ReadU16BE();

            if (platformID != 3 || encodingID != 1)
            {
                continue;
            }

            var strStart = (int)(record.Offset + stringOffset + offset);

            if (strStart + length <= _buffer.Length)
            {
                var saved = _buffer.Position;
                _buffer.Position = strStart;
                var str = _buffer.ReadString(length / 2).TrimEnd('\0');
                _buffer.Position = saved;

                switch (nameID)
                {
                    case 1:
                        familyName = string.IsNullOrEmpty(familyName) ? str : familyName;
                        break;
                    case 2:
                        subFamilyName = string.IsNullOrEmpty(subFamilyName) ? str : subFamilyName;
                        break;
                    case 4:
                        fullName = string.IsNullOrEmpty(fullName) ? str : fullName;
                        break;
                    case 5:
                        versionStr = string.IsNullOrEmpty(versionStr) ? str : versionStr;
                        break;
                }
            }
        }

        _buffer.Position = savedPos;

        return new TtfNameInfo
        {
            FamilyName = familyName,
            SubFamilyName = subFamilyName,
            FullName = fullName,
            Version = versionStr
        };
    }

    #endregion
}

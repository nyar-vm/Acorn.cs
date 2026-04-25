using Acorn.Frame;
using Acorn.Ttf.Data;
using System.Text;

namespace Acorn.Ttf.Encode;

/// <summary>
///     TrueType/OpenType 字体文件编码器，将 TtfFontData 编码为 TTF 二进制格式。
///     TTF 格式始终使用大端序。
/// </summary>
public sealed class TtfEncoder
{
    /// <summary>
    ///     编码 TTF 字体数据为字节数组。
    /// </summary>
    /// <param name="data">TTF 字体数据。</param>
    /// <returns>编码后的字节数组。</returns>
    public byte[] Encode(TtfFontData data)
    {
        if (data.FontType == TtfFontType.Collection)
        {
            throw new NotSupportedException("暂不支持 TrueType 集合字体编码");
        }

        var tables = data.Tables;
        var sortedTables = tables.OrderBy(t => t.Tag).ToList();
        var tableCount = (ushort)sortedTables.Count;

        var headerSize = TtfConstants.OffsetTableSize + tableCount * TtfConstants.TableRecordSize;

        var dataOffset = (uint)headerSize;
        var tableOffsets = new uint[tableCount];

        for (var i = 0; i < sortedTables.Count; i++)
        {
            tableOffsets[i] = dataOffset;
            var paddedLength = PadTo4(sortedTables[i].Data.Length);
            dataOffset += (uint)paddedLength;
        }

        var totalSize = (int)dataOffset;
        var writer = new ByteBufferWriter(totalSize);

        WriteOffsetTable(ref writer, data.FontType, tableCount);
        WriteTableRecords(ref writer, sortedTables, tableOffsets);
        WriteTableData(ref writer, sortedTables);

        return writer.ToArray();
    }

    #region 偏移表

    private static void WriteOffsetTable(ref ByteBufferWriter writer, TtfFontType fontType, ushort tableCount)
    {
        switch (fontType)
        {
            case TtfFontType.TrueType:
                writer.WriteU32BE(TtfConstants.TrueTypeMagic);
                break;
            case TtfFontType.Cff:
                writer.Write("OTTO"u8);
                break;
            default:
                writer.WriteU32BE(TtfConstants.TrueTypeMagic);
                break;
        }

        writer.WriteU16BE(tableCount);

        var (searchRange, entrySelector, rangeShift) = CalculateSearchFields(tableCount);
        writer.WriteU16BE(searchRange);
        writer.WriteU16BE(entrySelector);
        writer.WriteU16BE(rangeShift);
    }

    #endregion

    #region 表记录

    private static void WriteTableRecords(ref ByteBufferWriter writer, List<TtfTableRecord> tables, uint[] offsets)
    {
        for (var i = 0; i < tables.Count; i++)
        {
            var table = tables[i];
            var tagBytes = Encoding.ASCII.GetBytes(table.Tag);

            if (tagBytes.Length < 4)
            {
                var padded = new byte[4];
                Array.Copy(tagBytes, padded, tagBytes.Length);
                writer.Write(padded);
            }
            else
            {
                writer.Write(tagBytes.AsSpan(0, 4));
            }

            var checksum = table.Data.Length > 0 ? CalculateChecksum(table.Data) : table.Checksum;
            writer.WriteU32BE(checksum);
            writer.WriteU32BE(offsets[i]);
            writer.WriteU32BE((uint)table.Data.Length);
        }
    }

    #endregion

    #region 表数据

    private static void WriteTableData(ref ByteBufferWriter writer, List<TtfTableRecord> tables)
    {
        foreach (var table in tables)
        {
            if (table.Data.Length == 0)
            {
                continue;
            }

            writer.Write(table.Data);

            var padding = PadTo4(table.Data.Length) - table.Data.Length;
            for (var i = 0; i < padding; i++)
            {
                writer.WriteU8(0);
            }
        }
    }

    #endregion

    #region 校验和计算

    private static uint CalculateChecksum(byte[] data)
    {
        uint sum = 0;
        var paddedLength = PadTo4(data.Length);

        for (var i = 0; i < paddedLength; i += 4)
        {
            uint word = 0;
            word |= (uint)(i < data.Length ? data[i] : 0) << 24;
            word |= (uint)(i + 1 < data.Length ? data[i + 1] : 0) << 16;
            word |= (uint)(i + 2 < data.Length ? data[i + 2] : 0) << 8;
            word |= (uint)(i + 3 < data.Length ? data[i + 3] : 0);
            sum += word;
        }

        return sum;
    }

    #endregion

    #region 辅助方法

    private static int PadTo4(int length)
    {
        return (length + 3) & ~3;
    }

    private static (ushort SearchRange, ushort EntrySelector, ushort RangeShift) CalculateSearchFields(ushort tableCount)
    {
        if (tableCount == 0)
        {
            return (0, 0, 0);
        }

        var log2 = 0;
        var power = 1;

        while (power * 2 <= tableCount)
        {
            power *= 2;
            log2++;
        }

        var searchRange = (ushort)(power * 16);
        var entrySelector = (ushort)log2;
        var rangeShift = (ushort)(tableCount * 16 - searchRange);

        return (searchRange, entrySelector, rangeShift);
    }

    #endregion
}

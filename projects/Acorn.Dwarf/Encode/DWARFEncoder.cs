using Acorn.DWARF.Data;
using Acorn.Frame;

namespace Acorn.DWARF.Encode;

/// <summary>
///     DWARF 文件编码器，将调试信息结构体序列化为 DWARF 二进制数据。
/// </summary>
public sealed class DWARFEncoder
{
    /// <summary>
    ///     将 DWARF 文件数据编码为二进制数据。
    /// </summary>
    /// <param name="fileData">DWARF 文件数据。</param>
    /// <returns>DWARF 二进制数据。</returns>
    public byte[] Encode(DWARFFileData fileData)
    {
        var writer = new ByteBufferWriter(4096);

        foreach (var cu in fileData.CompilationUnits)
        {
            WriteCompilationUnit(ref writer, cu);
        }

        if (fileData.LineNumberTables.Count > 0)
        {
            writer.WriteU32LE(0);

            foreach (var lineTable in fileData.LineNumberTables)
            {
                WriteLineNumberTable(ref writer, lineTable);
            }
        }

        return writer.WrittenData.ToArray();
    }

    #region 编译单元

    /// <summary>
    ///     写入编译单元。
    /// </summary>
    private static void WriteCompilationUnit(ref ByteBufferWriter writer, DWARFCompilationUnitData cu)
    {
        var bodyWriter = new ByteBufferWriter(1024);
        WriteCompilationUnitBody(ref bodyWriter, cu);
        var bodySpan = bodyWriter.WrittenData;

        var unitLength = (uint)bodySpan.Length;

        writer.WriteU32LE(unitLength);
        writer.Write(bodySpan);
    }

    /// <summary>
    ///     写入编译单元主体（不含 unitLength 字段）。
    /// </summary>
    private static void WriteCompilationUnitBody(ref ByteBufferWriter writer, DWARFCompilationUnitData cu)
    {
        writer.WriteU16LE(cu.Version);
        writer.WriteU32LE(cu.DebugInfoOffset);
        writer.WriteU8(cu.AddressSize);
        writer.WriteU8(cu.SegmentSelectorSize);

        foreach (var entry in cu.Entries)
        {
            WriteEntry(ref writer, entry);
        }

        writer.WriteLeb128U64(0);
    }

    #endregion

    #region 条目与属性

    /// <summary>
    ///     写入条目。
    /// </summary>
    private static void WriteEntry(ref ByteBufferWriter writer, DWARFEntryData entry)
    {
        writer.WriteLeb128U64(entry.AbbreviationCode);
        writer.WriteLeb128U64(entry.Tag);
        writer.WriteU8((byte)(entry.HasChildren ? 1 : 0));

        foreach (var attr in entry.Attributes)
        {
            WriteAttribute(ref writer, attr);
        }

        writer.WriteLeb128U64(0);
        writer.WriteLeb128U64(0);
    }

    /// <summary>
    ///     写入属性。
    /// </summary>
    private static void WriteAttribute(ref ByteBufferWriter writer, DWARFAttributeData attr)
    {
        writer.WriteLeb128U64(attr.Name);
        writer.WriteLeb128U64(attr.Form);
        WriteAttributeValue(ref writer, attr.Form, attr.Value);
    }

    /// <summary>
    ///     根据属性形式写入属性值。
    /// </summary>
    private static void WriteAttributeValue(ref ByteBufferWriter writer, uint form, object? value)
    {
        switch (form)
        {
            case 0x01:
                break;

            case 0x03:
                writer.WriteU16LE(ConvertToU16(value));
                break;

            case 0x04:
                writer.WriteU32LE(ConvertToU32(value));
                break;

            case 0x05:
                writer.WriteU8((byte)(IsTrue(value) ? 1 : 0));
                break;

            case 0x06:
                writer.WriteU8(ConvertToU8(value));
                break;

            case 0x07:
                writer.WriteU8(ConvertToU8(value));
                break;

            case 0x08:
                writer.WriteLeb128U64(ConvertToU64(value));
                break;

            case 0x09:
                writer.WriteU16LE(ConvertToU16(value));
                break;

            case 0x0A:
                writer.WriteU32LE(ConvertToU32(value));
                break;

            case 0x0B:
                writer.WriteNullTerminatedString(ConvertToString(value));
                break;

            case 0x0C:
                writer.WriteU8(ConvertToU8(value));
                break;

            case 0x0D:
                writer.WriteU16LE(ConvertToU16(value));
                break;

            case 0x0E:
                writer.WriteU32LE(ConvertToU32(value));
                break;

            case 0x0F:
                writer.WriteU64LE(ConvertToU64(value));
                break;

            case 0x10:
                writer.WriteLeb128U64(ConvertToU64(value));
                break;

            case 0x11:
                writer.WriteNullTerminatedString(ConvertToString(value));
                break;
        }
    }

    #endregion

    #region 行号表

    /// <summary>
    ///     写入行号表。
    /// </summary>
    private static void WriteLineNumberTable(ref ByteBufferWriter writer, DWARFLineNumberTableData table)
    {
        var bodyWriter = new ByteBufferWriter(512);
        WriteLineNumberTableBody(ref bodyWriter, table);
        var bodySpan = bodyWriter.WrittenData;

        var unitLength = (uint)bodySpan.Length;

        writer.WriteU32LE(unitLength);
        writer.Write(bodySpan);
    }

    /// <summary>
    ///     写入行号表主体（不含 unitLength 字段）。
    /// </summary>
    private static void WriteLineNumberTableBody(ref ByteBufferWriter writer, DWARFLineNumberTableData table)
    {
        writer.WriteU16LE(table.Version);
        writer.WriteU8(table.AddressSize);
        writer.WriteU8(table.SegmentSelectorSize);
        writer.WriteU32LE(table.HeaderLength);
        writer.WriteU8(table.MinimumInstructionLength);
        writer.WriteU8(table.MaximumOperationsPerInstruction);
        writer.WriteU8(table.DefaultIsStatement);
        writer.WriteI8(table.LineBase);
        writer.WriteU8(table.LineRange);
        writer.WriteU8(table.OpcodeBase);

        foreach (var length in table.StandardOpcodeLengths)
        {
            writer.WriteU8(length);
        }

        foreach (var fileName in table.FileNames)
        {
            writer.WriteNullTerminatedString(fileName);
        }
    }

    #endregion

    #region 值转换辅助方法

    /// <summary>
    ///     将对象值转换为 ushort。
    /// </summary>
    private static ushort ConvertToU16(object? value)
    {
        return value switch
        {
            null => 0,
            ushort v => v,
            short v => (ushort)v,
            int v => (ushort)v,
            uint v => (ushort)v,
            long v => (ushort)v,
            ulong v => (ushort)v,
            string s => ushort.TryParse(s, out var r) ? r : (ushort)0,
            _ => (ushort)Convert.ChangeType(value, typeof(ushort))
        };
    }

    /// <summary>
    ///     将对象值转换为 uint。
    /// </summary>
    private static uint ConvertToU32(object? value)
    {
        return value switch
        {
            null => 0,
            uint v => v,
            int v => (uint)v,
            ushort v => v,
            short v => (uint)v,
            long v => (uint)v,
            ulong v => (uint)v,
            string s => uint.TryParse(s, out var r) ? r : 0,
            _ => (uint)Convert.ChangeType(value, typeof(uint))
        };
    }

    /// <summary>
    ///     将对象值转换为 ulong。
    /// </summary>
    private static ulong ConvertToU64(object? value)
    {
        return value switch
        {
            null => 0,
            ulong v => v,
            long v => (ulong)v,
            uint v => v,
            int v => (ulong)v,
            ushort v => v,
            short v => (ulong)v,
            byte v => v,
            sbyte v => (ulong)v,
            string s => ulong.TryParse(s, out var r) ? r : 0,
            _ => (ulong)Convert.ChangeType(value, typeof(ulong))
        };
    }

    /// <summary>
    ///     将对象值转换为 byte。
    /// </summary>
    private static byte ConvertToU8(object? value)
    {
        return value switch
        {
            null => 0,
            byte v => v,
            sbyte v => (byte)v,
            ushort v => (byte)v,
            short v => (byte)v,
            int v => (byte)v,
            uint v => (byte)v,
            long v => (byte)v,
            ulong v => (byte)v,
            bool v => v ? (byte)1 : (byte)0,
            string s => byte.TryParse(s, out var r) ? r : (byte)0,
            _ => (byte)Convert.ChangeType(value, typeof(byte))
        };
    }

    /// <summary>
    ///     判断对象值是否为 true。
    /// </summary>
    private static bool IsTrue(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            byte b => b != 0,
            sbyte b => b != 0,
            ushort v => v != 0,
            short v => v != 0,
            int v => v != 0,
            uint v => v != 0,
            long v => v != 0,
            ulong v => v != 0,
            string s => bool.TryParse(s, out var r) && r,
            _ => false
        };
    }

    /// <summary>
    ///     将对象值转换为字符串。
    /// </summary>
    private static string ConvertToString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            _ => value.ToString() ?? string.Empty
        };
    }

    #endregion
}

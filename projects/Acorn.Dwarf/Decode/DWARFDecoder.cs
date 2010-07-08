using Acorn.DWARF.Data;
using Acorn.Frame;

namespace Acorn.DWARF.Decode;

/// <summary>
///     DWARF 文件解码器，解析调试信息格式。
/// </summary>
public sealed class DWARFDecoder
{
    /// <summary>
    ///     从 DWARF 二进制数据解码调试信息。
    /// </summary>
    /// <param name="data">DWARF 二进制数据。</param>
    /// <returns>解码后的 DWARF 文件数据。</returns>
    public DWARFFileData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return DecodeFile(ref buffer);
    }

    private DWARFFileData DecodeFile(ref ByteBuffer buffer)
    {
        var compilationUnits = new List<DWARFCompilationUnitData>();
        var lineNumberTables = new List<DWARFLineNumberTableData>();

        while (!buffer.IsEnd)
        {
            var unit = ReadCompilationUnit(ref buffer);
            if (unit != null)
            {
                compilationUnits.Add(unit);
            }
            else
            {
                break;
            }
        }

        return new DWARFFileData
        {
            CompilationUnits = compilationUnits,
            LineNumberTables = lineNumberTables
        };
    }

    /// <summary>
    ///     读取编译单元。
    /// </summary>
    private DWARFCompilationUnitData? ReadCompilationUnit(ref ByteBuffer buffer)
    {
        if (buffer.Remaining < 4)
        {
            return null;
        }

        var unitLength = buffer.ReadU32LE();
        if (unitLength == 0xFFFFFFFF)
        {
            unitLength = (uint)buffer.ReadU64LE();
        }

        if (unitLength == 0 || buffer.Position + unitLength > buffer.Length)
        {
            return null;
        }

        var version = buffer.ReadU16LE();
        var debugInfoOffset = buffer.ReadU32LE();
        var addressSize = buffer.ReadU8();
        var segmentSelectorSize = buffer.ReadU8();

        var entries = new List<DWARFEntryData>();
        var endPosition = buffer.Position + (int)unitLength - 11;

        while (buffer.Position < endPosition)
        {
            var abbrevCode = buffer.ReadLeb128U64();
            if (abbrevCode == 0)
            {
                break;
            }

            var tag = buffer.ReadLeb128U64();
            var hasChildren = buffer.ReadU8() != 0;

            var attributes = new List<DWARFAttributeData>();

            while (true)
            {
                var attrName = buffer.ReadLeb128U64();
                var attrForm = buffer.ReadLeb128U64();

                if (attrName == 0 && attrForm == 0)
                {
                    break;
                }

                var value = ReadAttributeValue(ref buffer, attrForm);
                attributes.Add(new DWARFAttributeData
                {
                    Name = (uint)attrName,
                    Form = (uint)attrForm,
                    Value = value
                });
            }

            entries.Add(new DWARFEntryData
            {
                AbbreviationCode = abbrevCode,
                Tag = (uint)tag,
                HasChildren = hasChildren,
                Attributes = attributes
            });
        }

        return new DWARFCompilationUnitData
        {
            UnitLength = unitLength,
            Version = version,
            DebugInfoOffset = debugInfoOffset,
            AddressSize = addressSize,
            SegmentSelectorSize = segmentSelectorSize,
            Entries = entries
        };
    }

    /// <summary>
    ///     读取属性值。
    /// </summary>
    private object? ReadAttributeValue(ref ByteBuffer buffer, ulong form)
    {
        return form switch
        {
            0x01 => null,
            0x03 => buffer.ReadU16LE(),
            0x04 => buffer.ReadU32LE(),
            0x05 => buffer.ReadU8() == 1,
            0x06 => buffer.ReadU8(),
            0x07 => buffer.ReadU8(),
            0x08 => buffer.ReadLeb128U64(),
            0x09 => buffer.ReadU16LE(),
            0x0A => buffer.ReadU32LE(),
            0x0B => buffer.ReadNullTerminatedString(),
            0x0C => buffer.ReadU8(),
            0x0D => buffer.ReadU16LE(),
            0x0E => buffer.ReadU32LE(),
            0x0F => buffer.ReadU64LE(),
            0x10 => buffer.ReadLeb128U64(),
            0x11 => buffer.ReadNullTerminatedString(),
            _ => null
        };
    }
}

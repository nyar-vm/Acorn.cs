using Acorn.Frame;
using Acorn.MachO.Data;
using System.Text;

namespace Acorn.MachO.Encode;

/// <summary>
///     Mach-O 文件编码器，将 MachOFileData 编码为 Mach-O 二进制格式。
///     支持 32 位/64 位双模式和小端/大端双字节序。
///     支持节区内容编码和 LC_SEGMENT/LC_SEGMENT_64 结构化编码。
/// </summary>
public sealed class MachOEncoder
{
    private const uint LC_SEGMENT = 0x1;
    private const uint LC_SEGMENT_64 = 0x19;

    /// <summary>
    ///     编码 Mach-O 文件数据为字节数组。
    /// </summary>
    public byte[] Encode(MachOFileData data)
    {
        var header = data.Header;
        var is64 = header.Is64Bit;
        var isLE = header.IsLittleEndian;

        var size = EstimateSize(data);
        var writer = new ByteBufferWriter(size);

        WriteMachOHeader(ref writer, header, is64, isLE);
        WriteLoadCommands(ref writer, data, is64, isLE);
        WriteSectionContents(ref writer, data, isLE);

        return writer.ToArray();
    }

    #region Mach-O 头

    private static void WriteMachOHeader(ref ByteBufferWriter writer, MachOHeaderData header, bool is64, bool isLE)
    {
        WriteU32(ref writer, header.Magic, isLE);
        WriteI32(ref writer, header.CPUType, isLE);
        WriteI32(ref writer, header.CPUSubtype, isLE);
        WriteU32(ref writer, header.FileType, isLE);
        WriteU32(ref writer, header.NumberOfLoadCommands, isLE);
        WriteU32(ref writer, header.SizeOfLoadCommands, isLE);
        WriteU32(ref writer, header.Flags, isLE);

        if (is64)
        {
            WriteU32(ref writer, header.Reserved, isLE);
        }
    }

    #endregion

    #region 加载命令

    private static void WriteLoadCommands(ref ByteBufferWriter writer, MachOFileData data, bool is64, bool isLE)
    {
        foreach (var cmd in data.LoadCommands)
        {
            WriteU32(ref writer, cmd.Command, isLE);
            WriteU32(ref writer, cmd.Size, isLE);

            if (cmd.Command == LC_SEGMENT_64 && is64)
            {
                WriteSegment64Command(ref writer, cmd, data.Sections, isLE);
            }
            else if (cmd.Command == LC_SEGMENT && !is64)
            {
                WriteSegmentCommand(ref writer, cmd, data.Sections, isLE);
            }
            else
            {
                writer.Write(cmd.Data);
            }
        }
    }

    private static void WriteSegment64Command(ref ByteBufferWriter writer, MachOLoadCommandData cmd,
        IReadOnlyList<MachOSectionData> sections, bool isLE)
    {
        var segmentName = ReadString(cmd.Data, 0, 16);
        WritePaddedName(ref writer, segmentName, 16);

        if (cmd.Data.Length >= 80)
        {
            WriteU64(ref writer, ReadU64BE(cmd.Data, 16), isLE);
            WriteU64(ref writer, ReadU64BE(cmd.Data, 24), isLE);
            WriteU64(ref writer, ReadU64BE(cmd.Data, 32), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 40), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 44), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 48), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 52), isLE);

            var numberOfSections = ReadU32BE(cmd.Data, 56);
            WriteU32(ref writer, numberOfSections, isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 60), isLE);

            var sectionStart = 64;
            for (var i = 0; i < numberOfSections && sectionStart + 80 <= cmd.Data.Length; i++)
            {
                WriteSection64(ref writer, sections, i, isLE);
                sectionStart += 80;
            }
        }
        else
        {
            writer.Write(cmd.Data.AsSpan(16));
        }
    }

    private static void WriteSegmentCommand(ref ByteBufferWriter writer, MachOLoadCommandData cmd,
        IReadOnlyList<MachOSectionData> sections, bool isLE)
    {
        var segmentName = ReadString(cmd.Data, 0, 16);
        WritePaddedName(ref writer, segmentName, 16);

        if (cmd.Data.Length >= 56)
        {
            WriteU32(ref writer, ReadU32BE(cmd.Data, 16), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 20), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 24), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 28), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 32), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 36), isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 40), isLE);

            var numberOfSections = ReadU32BE(cmd.Data, 44);
            WriteU32(ref writer, numberOfSections, isLE);
            WriteU32(ref writer, ReadU32BE(cmd.Data, 48), isLE);

            var sectionStart = 52;
            for (var i = 0; i < numberOfSections && sectionStart + 68 <= cmd.Data.Length; i++)
            {
                WriteSection32(ref writer, sections, i, isLE);
                sectionStart += 68;
            }
        }
        else
        {
            writer.Write(cmd.Data.AsSpan(16));
        }
    }

    private static void WriteSection64(ref ByteBufferWriter writer, IReadOnlyList<MachOSectionData> sections, int index, bool isLE)
    {
        if (index < sections.Count)
        {
            var section = sections[index];
            WritePaddedName(ref writer, section.SectionName, 16);
            WritePaddedName(ref writer, section.SegmentName, 16);
            WriteU64(ref writer, section.Address, isLE);
            WriteU64(ref writer, section.Size, isLE);
            WriteU32(ref writer, section.Offset, isLE);
            WriteU32(ref writer, section.Alignment, isLE);
            WriteU32(ref writer, section.RelocationsOffset, isLE);
            WriteU32(ref writer, section.NumberOfRelocations, isLE);
            WriteU32(ref writer, section.Flags, isLE);
            WriteU32(ref writer, 0, isLE);
            WriteU32(ref writer, 0, isLE);
        }
        else
        {
            WritePadding(ref writer, 80);
        }
    }

    private static void WriteSection32(ref ByteBufferWriter writer, IReadOnlyList<MachOSectionData> sections, int index, bool isLE)
    {
        if (index < sections.Count)
        {
            var section = sections[index];
            WritePaddedName(ref writer, section.SectionName, 16);
            WritePaddedName(ref writer, section.SegmentName, 16);
            WriteU32(ref writer, (uint)section.Address, isLE);
            WriteU32(ref writer, (uint)section.Size, isLE);
            WriteU32(ref writer, section.Offset, isLE);
            WriteU32(ref writer, section.Alignment, isLE);
            WriteU32(ref writer, section.RelocationsOffset, isLE);
            WriteU32(ref writer, section.NumberOfRelocations, isLE);
            WriteU32(ref writer, section.Flags, isLE);
            WriteU32(ref writer, 0, isLE);
            WriteU32(ref writer, 0, isLE);
        }
        else
        {
            WritePadding(ref writer, 68);
        }
    }

    #endregion

    #region 节区内容

    private static void WriteSectionContents(ref ByteBufferWriter writer, MachOFileData data, bool isLE)
    {
        foreach (var section in data.Sections)
        {
            if (section.Content.Length == 0) continue;

            var targetOffset = (int)section.Offset;
            var currentPos = writer.Position;

            if (currentPos < targetOffset)
            {
                WritePadding(ref writer, targetOffset - currentPos);
            }

            writer.Write(section.Content);

            var alignment = 1 << (int)section.Alignment;
            if (alignment > 1)
            {
                var aligned = (writer.Position + alignment - 1) & ~(alignment - 1);
                if (aligned > writer.Position)
                {
                    WritePadding(ref writer, aligned - writer.Position);
                }
            }
        }
    }

    #endregion

    #region 辅助方法

    private static void WritePaddedName(ref ByteBufferWriter writer, string name, int totalLength)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var writeLength = Math.Min(nameBytes.Length, totalLength);
        writer.Write(nameBytes.AsSpan(0, writeLength));
        for (var i = writeLength; i < totalLength; i++)
        {
            writer.WriteU8(0);
        }
    }

    private static void WritePadding(ref ByteBufferWriter writer, int count)
    {
        for (var i = 0; i < count; i++)
        {
            writer.WriteU8(0);
        }
    }

    private static string ReadString(byte[] data, int offset, int length)
    {
        var end = Math.Min(offset + length, data.Length);
        var span = data.AsSpan(offset, end - offset);
        var nullIndex = span.IndexOf((byte)0);
        if (nullIndex >= 0)
        {
            span = span.Slice(0, nullIndex);
        }

        return Encoding.UTF8.GetString(span);
    }

    private static uint ReadU32BE(byte[] data, int offset)
    {
        if (offset + 4 > data.Length) return 0;
        return (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
    }

    private static ulong ReadU64BE(byte[] data, int offset)
    {
        if (offset + 8 > data.Length) return 0;
        return ((ulong)ReadU32BE(data, offset) << 32) | ReadU32BE(data, offset + 4);
    }

    private static void WriteU16(ref ByteBufferWriter writer, ushort value, bool isLE)
    {
        if (isLE) writer.WriteU16LE(value);
        else writer.WriteU16BE(value);
    }

    private static void WriteU32(ref ByteBufferWriter writer, uint value, bool isLE)
    {
        if (isLE) writer.WriteU32LE(value);
        else writer.WriteU32BE(value);
    }

    private static void WriteU64(ref ByteBufferWriter writer, ulong value, bool isLE)
    {
        if (isLE) writer.WriteU64LE(value);
        else writer.WriteU64BE(value);
    }

    private static void WriteI32(ref ByteBufferWriter writer, int value, bool isLE)
    {
        if (isLE) writer.WriteI32LE(value);
        else writer.WriteI32BE(value);
    }

    #endregion

    #region 大小预估

    private static int EstimateSize(MachOFileData data)
    {
        var headerSize = data.Header.Is64Bit ? 32 : 28;

        var loadCommandsSize = 0;
        foreach (var cmd in data.LoadCommands)
        {
            loadCommandsSize += (int)cmd.Size;
        }

        var sectionContentSize = 0;
        foreach (var section in data.Sections)
        {
            sectionContentSize += section.Content.Length;
            var alignment = 1 << (int)section.Alignment;
            if (alignment > 1)
            {
                sectionContentSize = (sectionContentSize + alignment - 1) & ~(alignment - 1);
            }
        }

        return headerSize + loadCommandsSize + sectionContentSize + 4096;
    }

    #endregion
}

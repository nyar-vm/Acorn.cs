using Acorn.Frame;
using Acorn.ELF.Data;

namespace Acorn.ELF.Encode;

/// <summary>
///     ELF 文件编码器，将 ELFFileData 编码为 ELF 二进制格式。
///     支持 32 位/64 位双模式和小端/大端双字节序。
///     支持节区内容编码和字符串表自动构建。
/// </summary>
public sealed class ElfEncoder
{
    /// <summary>
    ///     编码 ELF 文件数据为字节数组
    /// </summary>
    public byte[] Encode(ELFFileData data)
    {
        var header = data.Header;
        var is64 = header.Is64Bit;
        var isLE = header.IsLittleEndian;

        var layout = ComputeLayout(data, is64);

        var size = layout.TotalSize;
        var writer = new ByteBufferWriter(size);

        WriteElfHeader(ref writer, header, is64, isLE, layout);

        WriteProgramHeaders(ref writer, data.ProgramHeaders, is64, isLE);

        foreach (var section in data.SectionHeaders)
        {
            if (section.Content.Length > 0 && layout.SectionOffsets.TryGetValue(section.Name, out var offset))
            {
                var currentPos = writer.Position;
                if (currentPos < offset)
                {
                    WritePadding(ref writer, offset - currentPos);
                }

                writer.Write(section.Content);

                var aligned = AlignUp(writer.Position, (int)section.Alignment);
                if (aligned > writer.Position)
                {
                    WritePadding(ref writer, aligned - writer.Position);
                }
            }
        }

        if (layout.StringTableOffset > 0)
        {
            var currentPos = writer.Position;
            if (currentPos < layout.StringTableOffset)
            {
                WritePadding(ref writer, layout.StringTableOffset - currentPos);
            }

            WriteStringTable(ref writer, layout.StringTable);
        }

        WriteSectionHeaders(ref writer, data.SectionHeaders, is64, isLE, layout);

        return writer.ToArray();
    }

    #region 布局计算

    /// <summary>
    ///     ELF 文件布局信息
    /// </summary>
    private sealed class ElfLayout
    {
        public int TotalSize;
        public int StringTableOffset;
        public int SectionHeadersOffset;
        public Dictionary<string, int> SectionOffsets = [];
        public List<string> StringTable = [];
    }

    private static ElfLayout ComputeLayout(ELFFileData data, bool is64)
    {
        var layout = new ElfLayout();

        var headerSize = is64 ? 64 : 52;
        var phSize = is64 ? 56 : 32;
        var shSize = is64 ? 64 : 40;

        var currentOffset = headerSize;
        currentOffset += data.ProgramHeaders.Count * phSize;

        layout.StringTable = BuildStringTable(data.SectionHeaders);

        foreach (var section in data.SectionHeaders)
        {
            if (section.Content.Length > 0)
            {
                var alignment = Math.Max((int)section.Alignment, 1);
                currentOffset = AlignUp(currentOffset, alignment);
                layout.SectionOffsets[section.Name] = currentOffset;
                currentOffset += section.Content.Length;
            }
        }

        layout.StringTableOffset = currentOffset;
        layout.StringTable.Add("\0");
        currentOffset += layout.StringTable.Sum(s => s.Length);

        currentOffset = AlignUp(currentOffset, 8);
        layout.SectionHeadersOffset = currentOffset;
        currentOffset += data.SectionHeaders.Count * shSize;

        layout.TotalSize = currentOffset;

        return layout;
    }

    private static List<string> BuildStringTable(IReadOnlyList<ELFSectionHeaderData> sections)
    {
        var strings = new List<string> { "\0" };

        foreach (var section in sections)
        {
            if (!string.IsNullOrEmpty(section.Name))
            {
                strings.Add(section.Name + "\0");
            }
        }

        return strings;
    }

    #endregion

    #region ELF 头

    private static void WriteElfHeader(ref ByteBufferWriter writer, ELFHeaderData header, bool is64, bool isLE, ElfLayout layout)
    {
        writer.Write(header.Magic);

        writer.WriteU8(header.Class);
        writer.WriteU8(header.DataEncoding);
        writer.WriteU8(header.Version);
        writer.WriteU8(header.OSABI);
        writer.WriteU8(header.ABIVersion);

        for (var i = 0; i < 7; i++)
        {
            writer.WriteU8(0);
        }

        WriteU16(ref writer, header.Type, isLE);
        WriteU16(ref writer, header.Machine, isLE);
        WriteU32(ref writer, header.ObjectVersion, isLE);

        if (is64)
        {
            WriteU64(ref writer, header.EntryPoint, isLE);
            WriteU64(ref writer, (ulong)header.ProgramHeaderOffset, isLE);
            WriteU64(ref writer, (ulong)layout.SectionHeadersOffset, isLE);
        }
        else
        {
            WriteU32(ref writer, (uint)header.EntryPoint, isLE);
            WriteU32(ref writer, (uint)header.ProgramHeaderOffset, isLE);
            WriteU32(ref writer, (uint)layout.SectionHeadersOffset, isLE);
        }

        WriteU32(ref writer, header.Flags, isLE);
        WriteU16(ref writer, header.ELFHeaderSize, isLE);
        WriteU16(ref writer, header.ProgramHeaderSize, isLE);
        WriteU16(ref writer, header.ProgramHeaderCount, isLE);
        WriteU16(ref writer, header.SectionHeaderSize, isLE);
        WriteU16(ref writer, header.SectionHeaderCount, isLE);
        WriteU16(ref writer, header.StringTableIndex, isLE);
    }

    #endregion

    #region 程序头

    private static void WriteProgramHeaders(ref ByteBufferWriter writer, IReadOnlyList<ELFProgramHeaderData> headers, bool is64, bool isLE)
    {
        foreach (var ph in headers)
        {
            WriteProgramHeader(ref writer, ph, is64, isLE);
        }
    }

    private static void WriteProgramHeader(ref ByteBufferWriter writer, ELFProgramHeaderData ph, bool is64, bool isLE)
    {
        WriteU32(ref writer, ph.Type, isLE);

        if (is64)
        {
            WriteU32(ref writer, ph.Flags, isLE);
            WriteU64(ref writer, ph.Offset, isLE);
            WriteU64(ref writer, ph.VirtualAddress, isLE);
            WriteU64(ref writer, ph.PhysicalAddress, isLE);
            WriteU64(ref writer, ph.FileSize, isLE);
            WriteU64(ref writer, ph.MemorySize, isLE);
            WriteU64(ref writer, ph.Alignment, isLE);
        }
        else
        {
            WriteU32(ref writer, (uint)ph.Offset, isLE);
            WriteU32(ref writer, (uint)ph.VirtualAddress, isLE);
            WriteU32(ref writer, (uint)ph.PhysicalAddress, isLE);
            WriteU32(ref writer, (uint)ph.FileSize, isLE);
            WriteU32(ref writer, (uint)ph.MemorySize, isLE);
            WriteU32(ref writer, ph.Flags, isLE);
            WriteU32(ref writer, (uint)ph.Alignment, isLE);
        }
    }

    #endregion

    #region 节区头

    private static void WriteSectionHeaders(ref ByteBufferWriter writer, IReadOnlyList<ELFSectionHeaderData> headers, bool is64, bool isLE, ElfLayout layout)
    {
        var nameOffset = 1;

        foreach (var sh in headers)
        {
            var nameIdx = sh.NameIndex;
            if (nameIdx == 0 && !string.IsNullOrEmpty(sh.Name))
            {
                nameIdx = (uint)nameOffset;
                nameOffset += sh.Name.Length + 1;
            }

            WriteSectionHeader(ref writer, sh, nameIdx, is64, isLE, layout);
        }
    }

    private static void WriteSectionHeader(ref ByteBufferWriter writer, ELFSectionHeaderData sh, uint nameIndex, bool is64, bool isLE, ElfLayout layout)
    {
        WriteU32(ref writer, nameIndex, isLE);
        WriteU32(ref writer, sh.Type, isLE);

        var offset = sh.Offset;
        var size = sh.Size;
        if (sh.Content.Length > 0 && layout.SectionOffsets.TryGetValue(sh.Name, out var computedOffset))
        {
            offset = (ulong)computedOffset;
            size = (ulong)sh.Content.Length;
        }

        if (is64)
        {
            WriteU64(ref writer, sh.Flags, isLE);
            WriteU64(ref writer, sh.Address, isLE);
            WriteU64(ref writer, offset, isLE);
            WriteU64(ref writer, size, isLE);
            WriteU32(ref writer, sh.Link, isLE);
            WriteU32(ref writer, sh.Info, isLE);
            WriteU64(ref writer, sh.Alignment, isLE);
            WriteU64(ref writer, sh.EntrySize, isLE);
        }
        else
        {
            WriteU32(ref writer, (uint)sh.Flags, isLE);
            WriteU32(ref writer, (uint)sh.Address, isLE);
            WriteU32(ref writer, (uint)offset, isLE);
            WriteU32(ref writer, (uint)size, isLE);
            WriteU32(ref writer, sh.Link, isLE);
            WriteU32(ref writer, sh.Info, isLE);
            WriteU32(ref writer, (uint)sh.Alignment, isLE);
            WriteU32(ref writer, (uint)sh.EntrySize, isLE);
        }
    }

    #endregion

    #region 字符串表

    private static void WriteStringTable(ref ByteBufferWriter writer, List<string> strings)
    {
        foreach (var s in strings)
        {
            if (s == "\0")
            {
                writer.WriteU8(0);
            }
            else
            {
                writer.WriteNullTerminatedString(s.TrimEnd('\0'));
            }
        }
    }

    #endregion

    #region 辅助方法

    private static void WritePadding(ref ByteBufferWriter writer, int count)
    {
        for (var i = 0; i < count; i++)
        {
            writer.WriteU8(0);
        }
    }

    private static int AlignUp(int offset, int alignment)
    {
        return (offset + alignment - 1) & ~(alignment - 1);
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

    #endregion
}

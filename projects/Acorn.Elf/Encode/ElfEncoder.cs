using Acorn.Frame;
using Acorn.ELF.Data;

namespace Acorn.ELF.Encode;

/// <summary>
///     ELF 文件编码器，将 ELFFileData 编码为 ELF 二进制格式。
///     支持 32 位/64 位双模式和小端/大端双字节序。
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

        var size = EstimateSize(data);
        var writer = new ByteBufferWriter(size);

        WriteElfHeader(ref writer, header, is64, isLE);
        WriteProgramHeaders(ref writer, data.ProgramHeaders, is64, isLE);
        WriteSectionHeaders(ref writer, data.SectionHeaders, is64, isLE);

        return writer.ToArray();
    }

    #region ELF 头

    private static void WriteElfHeader(ref ByteBufferWriter writer, ELFHeaderData header, bool is64, bool isLE)
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
            WriteU64(ref writer, header.ProgramHeaderOffset, isLE);
            WriteU64(ref writer, header.SectionHeaderOffset, isLE);
        }
        else
        {
            WriteU32(ref writer, (uint)header.EntryPoint, isLE);
            WriteU32(ref writer, (uint)header.ProgramHeaderOffset, isLE);
            WriteU32(ref writer, (uint)header.SectionHeaderOffset, isLE);
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

    private static void WriteSectionHeaders(ref ByteBufferWriter writer, IReadOnlyList<ELFSectionHeaderData> headers, bool is64, bool isLE)
    {
        foreach (var sh in headers)
        {
            WriteSectionHeader(ref writer, sh, is64, isLE);
        }
    }

    private static void WriteSectionHeader(ref ByteBufferWriter writer, ELFSectionHeaderData sh, bool is64, bool isLE)
    {
        WriteU32(ref writer, sh.NameIndex, isLE);
        WriteU32(ref writer, sh.Type, isLE);

        if (is64)
        {
            WriteU64(ref writer, sh.Flags, isLE);
            WriteU64(ref writer, sh.Address, isLE);
            WriteU64(ref writer, sh.Offset, isLE);
            WriteU64(ref writer, sh.Size, isLE);
            WriteU32(ref writer, sh.Link, isLE);
            WriteU32(ref writer, sh.Info, isLE);
            WriteU64(ref writer, sh.Alignment, isLE);
            WriteU64(ref writer, sh.EntrySize, isLE);
        }
        else
        {
            WriteU32(ref writer, (uint)sh.Flags, isLE);
            WriteU32(ref writer, (uint)sh.Address, isLE);
            WriteU32(ref writer, (uint)sh.Offset, isLE);
            WriteU32(ref writer, (uint)sh.Size, isLE);
            WriteU32(ref writer, sh.Link, isLE);
            WriteU32(ref writer, sh.Info, isLE);
            WriteU32(ref writer, (uint)sh.Alignment, isLE);
            WriteU32(ref writer, (uint)sh.EntrySize, isLE);
        }
    }

    #endregion

    #region 字节序辅助

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

    #region 大小预估

    private static int EstimateSize(ELFFileData data)
    {
        var is64 = data.Header.Is64Bit;
        var headerSize = is64 ? 64 : 52;
        var phSize = is64 ? 56 : 32;
        var shSize = is64 ? 64 : 40;

        return headerSize
               + data.ProgramHeaders.Count * phSize
               + data.SectionHeaders.Count * shSize
               + 4096;
    }

    #endregion
}

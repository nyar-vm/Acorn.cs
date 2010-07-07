using Acorn.ELF.Data;
using Acorn.Frame;

namespace Acorn.ELF.Decode;

/// <summary>
///     ELF 文件解码器，解析 Linux 可执行文件（.elf, .so）格式。
/// </summary>
public sealed class ELFDecoder
{
    /// <summary>
    ///     从 ELF 二进制数据解码文件。
    /// </summary>
    /// <param name="data">ELF 二进制数据。</param>
    /// <returns>解码后的 ELF 文件数据。</returns>
    public ELFFileData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return DecodeFile(ref buffer);
    }

    private ELFFileData DecodeFile(ref ByteBuffer buffer)
    {
        var header = ReadELFHeader(ref buffer);
        var sectionHeaders = ReadSectionHeaders(ref buffer, header);
        var programHeaders = ReadProgramHeaders(ref buffer, header);

        return new ELFFileData
        {
            Header = header,
            SectionHeaders = sectionHeaders,
            ProgramHeaders = programHeaders
        };
    }

    /// <summary>
    ///     读取 ELF 头。
    /// </summary>
    private ELFHeaderData ReadELFHeader(ref ByteBuffer buffer)
    {
        var magic = buffer.ReadBytes(4).ToArray();

        if (magic[0] != 0x7F || magic[1] != 0x45 || magic[2] != 0x4C || magic[3] != 0x46)
        {
            throw new InvalidDataException("不是有效的 ELF 文件（魔数不匹配）");
        }

        var elfClass = buffer.ReadU8();
        var dataEncoding = buffer.ReadU8();
        var version = buffer.ReadU8();
        var osAbi = buffer.ReadU8();
        var abiVersion = buffer.ReadU8();

        buffer.Advance(7);

        var type = ReadUInt16(ref buffer, dataEncoding);
        var machine = ReadUInt16(ref buffer, dataEncoding);
        var objectVersion = ReadUInt32(ref buffer, dataEncoding);

        ulong entryPoint;
        ulong programHeaderOffset;
        ulong sectionHeaderOffset;

        if (elfClass == 1)
        {
            entryPoint = ReadUInt32(ref buffer, dataEncoding);
            programHeaderOffset = ReadUInt32(ref buffer, dataEncoding);
            sectionHeaderOffset = ReadUInt32(ref buffer, dataEncoding);
        }
        else
        {
            entryPoint = ReadUInt64(ref buffer, dataEncoding);
            programHeaderOffset = ReadUInt64(ref buffer, dataEncoding);
            sectionHeaderOffset = ReadUInt64(ref buffer, dataEncoding);
        }

        var flags = ReadUInt32(ref buffer, dataEncoding);
        var elfHeaderSize = ReadUInt16(ref buffer, dataEncoding);
        var programHeaderSize = ReadUInt16(ref buffer, dataEncoding);
        var programHeaderCount = ReadUInt16(ref buffer, dataEncoding);
        var sectionHeaderSize = ReadUInt16(ref buffer, dataEncoding);
        var sectionHeaderCount = ReadUInt16(ref buffer, dataEncoding);
        var stringTableIndex = ReadUInt16(ref buffer, dataEncoding);

        return new ELFHeaderData
        {
            Magic = magic,
            Class = elfClass,
            DataEncoding = dataEncoding,
            Version = version,
            OSABI = osAbi,
            ABIVersion = abiVersion,
            Type = type,
            Machine = machine,
            ObjectVersion = objectVersion,
            EntryPoint = entryPoint,
            ProgramHeaderOffset = programHeaderOffset,
            SectionHeaderOffset = sectionHeaderOffset,
            Flags = flags,
            ELFHeaderSize = elfHeaderSize,
            ProgramHeaderSize = programHeaderSize,
            ProgramHeaderCount = programHeaderCount,
            SectionHeaderSize = sectionHeaderSize,
            SectionHeaderCount = sectionHeaderCount,
            StringTableIndex = stringTableIndex
        };
    }

    /// <summary>
    ///     读取节区头。
    /// </summary>
    private List<ELFSectionHeaderData> ReadSectionHeaders(ref ByteBuffer buffer, ELFHeaderData header)
    {
        var sections = new List<ELFSectionHeaderData>();

        buffer.Position = (int)header.SectionHeaderOffset;

        for (var i = 0; i < header.SectionHeaderCount; i++)
        {
            var nameIndex = ReadUInt32(ref buffer, header.DataEncoding);
            var type = ReadUInt32(ref buffer, header.DataEncoding);

            ulong flags;
            ulong address;
            ulong offset;
            ulong size;

            if (header.Is64Bit)
            {
                flags = ReadUInt64(ref buffer, header.DataEncoding);
                address = ReadUInt64(ref buffer, header.DataEncoding);
                offset = ReadUInt64(ref buffer, header.DataEncoding);
                size = ReadUInt64(ref buffer, header.DataEncoding);
            }
            else
            {
                flags = ReadUInt32(ref buffer, header.DataEncoding);
                address = ReadUInt32(ref buffer, header.DataEncoding);
                offset = ReadUInt32(ref buffer, header.DataEncoding);
                size = ReadUInt32(ref buffer, header.DataEncoding);
            }

            var link = ReadUInt32(ref buffer, header.DataEncoding);
            var info = ReadUInt32(ref buffer, header.DataEncoding);

            ulong alignment;
            ulong entrySize;

            if (header.Is64Bit)
            {
                alignment = ReadUInt64(ref buffer, header.DataEncoding);
                entrySize = ReadUInt64(ref buffer, header.DataEncoding);
            }
            else
            {
                alignment = ReadUInt32(ref buffer, header.DataEncoding);
                entrySize = ReadUInt32(ref buffer, header.DataEncoding);
            }

            sections.Add(new ELFSectionHeaderData
            {
                NameIndex = nameIndex,
                Type = type,
                Flags = flags,
                Address = address,
                Offset = offset,
                Size = size,
                Link = link,
                Info = info,
                Alignment = alignment,
                EntrySize = entrySize
            });
        }

        return sections;
    }

    /// <summary>
    ///     读取程序头。
    /// </summary>
    private List<ELFProgramHeaderData> ReadProgramHeaders(ref ByteBuffer buffer, ELFHeaderData header)
    {
        var programs = new List<ELFProgramHeaderData>();

        buffer.Position = (int)header.ProgramHeaderOffset;

        for (var i = 0; i < header.ProgramHeaderCount; i++)
        {
            var type = ReadUInt32(ref buffer, header.DataEncoding);

            uint flags;
            ulong offset;
            ulong virtualAddress;
            ulong physicalAddress;
            ulong fileSize;
            ulong memorySize;
            ulong alignment;

            if (header.Is64Bit)
            {
                flags = ReadUInt32(ref buffer, header.DataEncoding);
                offset = ReadUInt64(ref buffer, header.DataEncoding);
                virtualAddress = ReadUInt64(ref buffer, header.DataEncoding);
                physicalAddress = ReadUInt64(ref buffer, header.DataEncoding);
                fileSize = ReadUInt64(ref buffer, header.DataEncoding);
                memorySize = ReadUInt64(ref buffer, header.DataEncoding);
                alignment = ReadUInt64(ref buffer, header.DataEncoding);
            }
            else
            {
                offset = ReadUInt32(ref buffer, header.DataEncoding);
                virtualAddress = ReadUInt32(ref buffer, header.DataEncoding);
                physicalAddress = ReadUInt32(ref buffer, header.DataEncoding);
                fileSize = ReadUInt32(ref buffer, header.DataEncoding);
                memorySize = ReadUInt32(ref buffer, header.DataEncoding);
                flags = ReadUInt32(ref buffer, header.DataEncoding);
                alignment = ReadUInt32(ref buffer, header.DataEncoding);
            }

            programs.Add(new ELFProgramHeaderData
            {
                Type = type,
                Flags = flags,
                Offset = offset,
                VirtualAddress = virtualAddress,
                PhysicalAddress = physicalAddress,
                FileSize = fileSize,
                MemorySize = memorySize,
                Alignment = alignment
            });
        }

        return programs;
    }

    /// <summary>
    ///     读取 UInt16，根据编码选择字节序。
    /// </summary>
    private static ushort ReadUInt16(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == 1 ? buffer.ReadU16LE() : buffer.ReadU16BE();
    }

    /// <summary>
    ///     读取 UInt32，根据编码选择字节序。
    /// </summary>
    private static uint ReadUInt32(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == 1 ? buffer.ReadU32LE() : buffer.ReadU32BE();
    }

    /// <summary>
    ///     读取 UInt64，根据编码选择字节序。
    /// </summary>
    private static ulong ReadUInt64(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == 1 ? buffer.ReadU64LE() : buffer.ReadU64BE();
    }
}

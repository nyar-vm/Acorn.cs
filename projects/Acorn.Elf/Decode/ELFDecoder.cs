using System.Text;
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
        var symbolTable = ReadSymbolTable(ref buffer, sectionHeaders, header);
        ReadSectionContents(ref buffer, sectionHeaders);

        return new ELFFileData
        {
            Header = header,
            SectionHeaders = sectionHeaders,
            ProgramHeaders = programHeaders,
            SymbolTable = symbolTable
        };
    }

    /// <summary>
    ///     读取 ELF 头。
    /// </summary>
    private ELFHeaderData ReadELFHeader(ref ByteBuffer buffer)
    {
        var magic = buffer.ReadBytes(4).ToArray();

        if (magic[0] != ElfConstants.Magic[0] || magic[1] != ElfConstants.Magic[1] || magic[2] != ElfConstants.Magic[2] || magic[3] != ElfConstants.Magic[3])
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
        return dataEncoding == ElfConstants.DataEncodingLittleEndian ? buffer.ReadU16LE() : buffer.ReadU16BE();
    }

    /// <summary>
    ///     读取 UInt32，根据编码选择字节序。
    /// </summary>
    private static uint ReadUInt32(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == ElfConstants.DataEncodingLittleEndian ? buffer.ReadU32LE() : buffer.ReadU32BE();
    }

    /// <summary>
    ///     读取 UInt64，根据编码选择字节序。
    /// </summary>
    private static ulong ReadUInt64(ref ByteBuffer buffer, byte dataEncoding)
    {
        return dataEncoding == ElfConstants.DataEncodingLittleEndian ? buffer.ReadU64LE() : buffer.ReadU64BE();
    }

    /// <summary>
    ///     读取符号表。
    /// </summary>
    /// <param name="buffer">数据缓冲区。</param>
    /// <param name="sectionHeaders">节区头列表。</param>
    /// <param name="header">ELF 头数据。</param>
    /// <returns>符号表数据，若无符号表则返回 null。</returns>
    private ELFSymbolTableData? ReadSymbolTable(ref ByteBuffer buffer, IReadOnlyList<ELFSectionHeaderData> sectionHeaders, ELFHeaderData header)
    {
        ELFSectionHeaderData? symtabSection = null;

        for (var i = 0; i < sectionHeaders.Count; i++)
        {
            if (sectionHeaders[i].Type == 2)
            {
                symtabSection = sectionHeaders[i];
                break;
            }
        }

        if (symtabSection == null)
        {
            return null;
        }

        var strtabIndex = (int)symtabSection.Link;

        if (strtabIndex < 0 || strtabIndex >= sectionHeaders.Count)
        {
            return null;
        }

        var strtabSection = sectionHeaders[strtabIndex];

        buffer.Position = (int)strtabSection.Offset;
        var strTabBytes = buffer.ReadBytes((int)strtabSection.Size).ToArray();

        buffer.Position = (int)symtabSection.Offset;
        var entrySize = header.Is64Bit ? ElfConstants.SymbolEntrySize64 : ElfConstants.SymbolEntrySize32;
        var symbolCount = (int)(symtabSection.Size / (ulong)entrySize);
        var symbols = new List<ELFSymbolData>(symbolCount);

        for (var i = 0; i < symbolCount; i++)
        {
            uint nameIndex;
            byte info;
            byte other;
            ushort sectionIndex;
            ulong value;
            ulong size;

            if (header.Is64Bit)
            {
                nameIndex = ReadUInt32(ref buffer, header.DataEncoding);
                info = buffer.ReadU8();
                other = buffer.ReadU8();
                sectionIndex = ReadUInt16(ref buffer, header.DataEncoding);
                value = ReadUInt64(ref buffer, header.DataEncoding);
                size = ReadUInt64(ref buffer, header.DataEncoding);
            }
            else
            {
                nameIndex = ReadUInt32(ref buffer, header.DataEncoding);
                value = ReadUInt32(ref buffer, header.DataEncoding);
                size = ReadUInt32(ref buffer, header.DataEncoding);
                info = buffer.ReadU8();
                other = buffer.ReadU8();
                sectionIndex = ReadUInt16(ref buffer, header.DataEncoding);
            }

            var name = ReadStringFromTable(strTabBytes, nameIndex);

            symbols.Add(new ELFSymbolData
            {
                NameIndex = nameIndex,
                Info = info,
                Other = other,
                SectionIndex = sectionIndex,
                Value = value,
                Size = size,
                Name = name
            });
        }

        return new ELFSymbolTableData
        {
            Symbols = symbols
        };
    }

    /// <summary>
    ///     从字符串表数据中读取以 null 结尾的字符串。
    /// </summary>
    /// <param name="strTabData">字符串表原始数据。</param>
    /// <param name="nameIndex">字符串起始索引。</param>
    /// <returns>解码后的字符串。</returns>
    private static string ReadStringFromTable(ReadOnlySpan<byte> strTabData, uint nameIndex)
    {
        var index = (int)nameIndex;

        if (index < 0 || index >= strTabData.Length)
        {
            return string.Empty;
        }

        var end = index;

        while (end < strTabData.Length && strTabData[end] != 0)
        {
            end++;
        }

        if (end == index)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(strTabData.Slice(index, end - index));
    }

    /// <summary>
    ///     读取所有节区的原始内容。
    /// </summary>
    /// <param name="buffer">数据缓冲区。</param>
    /// <param name="sectionHeaders">节区头列表。</param>
    private void ReadSectionContents(ref ByteBuffer buffer, List<ELFSectionHeaderData> sectionHeaders)
    {
        for (var i = 0; i < sectionHeaders.Count; i++)
        {
            var section = sectionHeaders[i];

            if (section.Offset == 0 || section.Size == 0)
            {
                continue;
            }

            buffer.Position = (int)section.Offset;
            section.Content = buffer.ReadBytes((int)section.Size).ToArray();
        }
    }
}

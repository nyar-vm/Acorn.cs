using System.Buffers.Binary;
using Acorn.Codec;
using Acorn.Coff.Data;
using Acorn.Frame;

namespace Acorn.Coff.Decode;

/// <summary>
///     COFF 文件解码器，解析 Windows 目标文件（.obj）格式。
/// </summary>
public sealed class CoffDecoder
{
    /// <summary>
    ///     从 COFF 二进制数据解码目标文件。
    /// </summary>
    /// <param name="data">COFF 二进制数据。</param>
    /// <returns>解码后的 COFF 文件数据。</returns>
    public CoffFileData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return DecodeFile(ref buffer);
    }

    private CoffFileData DecodeFile(ref ByteBuffer buffer)
    {
        var header = ReadCOFFHeader(ref buffer);
        var sections = ReadSectionHeaders(ref buffer, header);
        var symbols = ReadSymbolTable(ref buffer, header);
        var relocations = ReadRelocations(ref buffer, sections);

        return new CoffFileData
        {
            Header = header,
            Sections = sections,
            Symbols = symbols,
            Relocations = relocations
        };
    }

    /// <summary>
    ///     读取 COFF 头。
    /// </summary>
    private CoffHeaderData ReadCOFFHeader(ref ByteBuffer buffer)
    {
        var machine = buffer.ReadU16LE();
        var numberOfSections = buffer.ReadU16LE();
        var timeDateStamp = buffer.ReadU32LE();
        var pointerToSymbolTable = buffer.ReadU32LE();
        var numberOfSymbols = buffer.ReadU32LE();
        var sizeOfOptionalHeader = buffer.ReadU16LE();
        var characteristics = buffer.ReadU16LE();

        return new CoffHeaderData
        {
            Machine = machine,
            NumberOfSections = numberOfSections,
            TimeDateStamp = timeDateStamp,
            PointerToSymbolTable = pointerToSymbolTable,
            NumberOfSymbols = numberOfSymbols,
            SizeOfOptionalHeader = sizeOfOptionalHeader,
            Characteristics = characteristics
        };
    }

    /// <summary>
    ///     读取节区头。
    /// </summary>
    private List<CoffSectionHeaderData> ReadSectionHeaders(ref ByteBuffer buffer, CoffHeaderData header)
    {
        var sections = new List<CoffSectionHeaderData>();

        for (var i = 0; i < header.NumberOfSections; i++)
        {
            var nameBytes = FixedBytes8.FromSpan(buffer.ReadBytes(8));

            var physicalAddress = buffer.ReadU32LE();
            var virtualAddress = buffer.ReadU32LE();
            var sizeOfRawData = buffer.ReadU32LE();
            var pointerToRawData = buffer.ReadU32LE();
            var pointerToRelocations = buffer.ReadU32LE();
            var pointerToLinenumbers = buffer.ReadU32LE();
            var numberOfRelocations = buffer.ReadU16LE();
            var numberOfLinenumbers = buffer.ReadU16LE();
            var characteristics = buffer.ReadU32LE();

            sections.Add(new CoffSectionHeaderData
            {
                NameBytes = nameBytes,
                PhysicalAddress = physicalAddress,
                VirtualAddress = virtualAddress,
                SizeOfRawData = sizeOfRawData,
                PointerToRawData = pointerToRawData,
                PointerToRelocations = pointerToRelocations,
                PointerToLinenumbers = pointerToLinenumbers,
                NumberOfRelocations = numberOfRelocations,
                NumberOfLinenumbers = numberOfLinenumbers,
                Characteristics = characteristics
            });
        }

        return sections;
    }

    /// <summary>
    ///     读取符号表。
    /// </summary>
    private List<CoffSymbolData> ReadSymbolTable(ref ByteBuffer buffer, CoffHeaderData header)
    {
        var symbols = new List<CoffSymbolData>();

        if (header.PointerToSymbolTable == 0 || header.NumberOfSymbols == 0)
        {
            return symbols;
        }

        buffer.Position = (int)header.PointerToSymbolTable;

        for (var i = 0; i < header.NumberOfSymbols; i++)
        {
            var nameBytes = buffer.ReadBytes(8).ToArray();
            var value = buffer.ReadU32LE();
            var sectionNumber = buffer.ReadI16LE();
            var type = buffer.ReadU16LE();
            var storageClass = buffer.ReadU8();
            var numberOfAuxSymbols = buffer.ReadU8();

            var name = DecodeSymbolName(nameBytes, ref buffer, header);

            symbols.Add(new CoffSymbolData
            {
                Name = name,
                Value = value,
                SectionNumber = sectionNumber,
                Type = type,
                StorageClass = storageClass,
                NumberOfAuxSymbols = numberOfAuxSymbols
            });

            i += numberOfAuxSymbols;
            buffer.Advance(numberOfAuxSymbols * 18);
        }

        return symbols;
    }

    /// <summary>
    ///     解码符号名称。
    /// </summary>
    private static string DecodeSymbolName(byte[] nameBytes, ref ByteBuffer buffer, CoffHeaderData header)
    {
        if (nameBytes[0] == 0 && nameBytes[1] == 0 && nameBytes[2] == 0 && nameBytes[3] == 0)
        {
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(nameBytes.AsSpan(4));

            var stringTableOffset = (int)(header.PointerToSymbolTable + header.NumberOfSymbols * 18);
            var nameOffset = stringTableOffset + (int)offset;

            if (nameOffset < buffer.Length)
            {
                return buffer.ReadStringAt(nameOffset);
            }
        }

        return System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
    }

    /// <summary>
    ///     读取重定位。
    /// </summary>
    private List<CoffRelocationData> ReadRelocations(ref ByteBuffer buffer, List<CoffSectionHeaderData> sections)
    {
        var relocations = new List<CoffRelocationData>();

        foreach (var section in sections)
        {
            if (section.PointerToRelocations == 0 || section.NumberOfRelocations == 0)
            {
                continue;
            }

            buffer.Position = (int)section.PointerToRelocations;

            for (var i = 0; i < section.NumberOfRelocations; i++)
            {
                var virtualAddress = buffer.ReadU32LE();
                var symbolTableIndex = buffer.ReadU32LE();
                var type = buffer.ReadU16LE();

                relocations.Add(new CoffRelocationData
                {
                    VirtualAddress = virtualAddress,
                    SymbolTableIndex = symbolTableIndex,
                    Type = type
                });
            }
        }

        return relocations;
    }
}

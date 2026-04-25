using Acorn.Codec;
using Acorn.Frame;
using Acorn.Pe.Data;

namespace Acorn.Pe.Decode;

/// <summary>
///     PE 文件解码器，解析 Windows 可执行文件（.exe, .dll）格式。
/// </summary>
public sealed class PeDecoder
{
    /// <summary>
    ///     从 PE 二进制数据解码文件。
    /// </summary>
    /// <param name="data">PE 二进制数据。</param>
    /// <returns>解码后的 PE 文件数据。</returns>
    public PeFileData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return DecodeFile(ref buffer);
    }

    private PeFileData DecodeFile(ref ByteBuffer buffer)
    {
        var dosHeader = ReadDosHeader(ref buffer);

        buffer.Position = (int)dosHeader.PeHeaderOffset;

        var peHeader = ReadPEHeader(ref buffer);
        var optionalHeader = ReadOptionalHeader(ref buffer);
        var sections = ReadSections(ref buffer, peHeader.NumberOfSections);

        return new PeFileData
        {
            Header = peHeader,
            OptionalHeader = optionalHeader,
            Sections = sections
        };
    }

    /// <summary>
    ///     读取 DOS 头。
    /// </summary>
    private PeHeaderData ReadDosHeader(ref ByteBuffer buffer)
    {
        var dosMagic = buffer.ReadU16LE();

        if (dosMagic != 0x5A4D)
        {
            throw new InvalidDataException("不是有效的 PE 文件（DOS 头魔数不匹配）");
        }

        buffer.Advance(58);

        var peHeaderOffset = buffer.ReadU32LE();

        return new PeHeaderData
        {
            DosMagic = dosMagic,
            PeHeaderOffset = peHeaderOffset
        };
    }

    /// <summary>
    ///     读取 PE 头。
    /// </summary>
    private PeHeaderData ReadPEHeader(ref ByteBuffer buffer)
    {
        var peMagic = buffer.ReadU32LE();

        if (peMagic != 0x00004550)
        {
            throw new InvalidDataException("不是有效的 PE 文件（PE 头魔数不匹配）");
        }

        var machine = buffer.ReadU16LE();
        var numberOfSections = buffer.ReadU16LE();
        var timeDateStamp = buffer.ReadU32LE();
        var pointerToSymbolTable = buffer.ReadU32LE();
        var numberOfSymbols = buffer.ReadU32LE();
        var sizeOfOptionalHeader = buffer.ReadU16LE();
        var characteristics = buffer.ReadU16LE();

        return new PeHeaderData
        {
            PeMagic = peMagic,
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
    ///     读取可选头。
    /// </summary>
    private PeOptionalHeaderData ReadOptionalHeader(ref ByteBuffer buffer)
    {
        var magic = buffer.ReadU16LE();

        var majorLinkerVersion = buffer.ReadU8();
        var minorLinkerVersion = buffer.ReadU8();
        var sizeOfCode = buffer.ReadU32LE();
        var sizeOfInitializedData = buffer.ReadU32LE();
        var sizeOfUninitializedData = buffer.ReadU32LE();
        var addressOfEntryPoint = buffer.ReadU32LE();
        var baseOfCode = buffer.ReadU32LE();

        uint baseOfData = 0;
        ulong imageBase;

        if (magic == 0x10B)
        {
            baseOfData = buffer.ReadU32LE();
            imageBase = buffer.ReadU32LE();
        }
        else if (magic == 0x20B)
        {
            imageBase = buffer.ReadU64LE();
        }
        else
        {
            throw new InvalidDataException($"不支持的可选头魔术数字: 0x{magic:X4}");
        }

        var sectionAlignment = buffer.ReadU32LE();
        var fileAlignment = buffer.ReadU32LE();
        var majorOperatingSystemVersion = buffer.ReadU16LE();
        var minorOperatingSystemVersion = buffer.ReadU16LE();
        var majorImageVersion = buffer.ReadU16LE();
        var minorImageVersion = buffer.ReadU16LE();
        var majorSubsystemVersion = buffer.ReadU16LE();
        var minorSubsystemVersion = buffer.ReadU16LE();
        var win32VersionValue = buffer.ReadU32LE();
        var sizeOfImage = buffer.ReadU32LE();
        var sizeOfHeaders = buffer.ReadU32LE();
        var checkSum = buffer.ReadU32LE();
        var subsystem = buffer.ReadU16LE();
        var dllCharacteristics = buffer.ReadU16LE();

        ulong sizeOfStackReserve;
        ulong sizeOfStackCommit;
        ulong sizeOfHeapReserve;
        ulong sizeOfHeapCommit;

        if (magic == 0x10B)
        {
            sizeOfStackReserve = buffer.ReadU32LE();
            sizeOfStackCommit = buffer.ReadU32LE();
            sizeOfHeapReserve = buffer.ReadU32LE();
            sizeOfHeapCommit = buffer.ReadU32LE();
        }
        else
        {
            sizeOfStackReserve = buffer.ReadU64LE();
            sizeOfStackCommit = buffer.ReadU64LE();
            sizeOfHeapReserve = buffer.ReadU64LE();
            sizeOfHeapCommit = buffer.ReadU64LE();
        }

        var loaderFlags = buffer.ReadU32LE();
        var numberOfRvaAndSizes = buffer.ReadU32LE();

        var dataDirectories = ReadDataDirectories(ref buffer, (int)Math.Min(numberOfRvaAndSizes, 16));

        return new PeOptionalHeaderData
        {
            Magic = magic,
            MajorLinkerVersion = majorLinkerVersion,
            MinorLinkerVersion = minorLinkerVersion,
            SizeOfCode = sizeOfCode,
            SizeOfInitializedData = sizeOfInitializedData,
            SizeOfUninitializedData = sizeOfUninitializedData,
            AddressOfEntryPoint = addressOfEntryPoint,
            BaseOfCode = baseOfCode,
            BaseOfData = baseOfData,
            ImageBase = imageBase,
            SectionAlignment = sectionAlignment,
            FileAlignment = fileAlignment,
            MajorOperatingSystemVersion = majorOperatingSystemVersion,
            MinorOperatingSystemVersion = minorOperatingSystemVersion,
            MajorImageVersion = majorImageVersion,
            MinorImageVersion = minorImageVersion,
            MajorSubsystemVersion = majorSubsystemVersion,
            MinorSubsystemVersion = minorSubsystemVersion,
            Win32VersionValue = win32VersionValue,
            SizeOfImage = sizeOfImage,
            SizeOfHeaders = sizeOfHeaders,
            CheckSum = checkSum,
            Subsystem = subsystem,
            DllCharacteristics = dllCharacteristics,
            SizeOfStackReserve = sizeOfStackReserve,
            SizeOfStackCommit = sizeOfStackCommit,
            SizeOfHeapReserve = sizeOfHeapReserve,
            SizeOfHeapCommit = sizeOfHeapCommit,
            LoaderFlags = loaderFlags,
            NumberOfRvaAndSizes = numberOfRvaAndSizes,
            DataDirectories = dataDirectories
        };
    }

    /// <summary>
    ///     读取数据目录。
    /// </summary>
    private List<PeDataDirectoryEntry> ReadDataDirectories(ref ByteBuffer buffer, int count)
    {
        var directories = new List<PeDataDirectoryEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var rva = buffer.ReadU32LE();
            var size = buffer.ReadU32LE();

            directories.Add(new PeDataDirectoryEntry { Rva = rva, Size = size });
        }

        return directories;
    }

    /// <summary>
    ///     读取节区表。
    /// </summary>
    private List<PeSectionData> ReadSections(ref ByteBuffer buffer, int numberOfSections)
    {
        var sections = new List<PeSectionData>();

        for (var i = 0; i < numberOfSections; i++)
        {
            var nameBytes = FixedBytes8.FromSpan(buffer.ReadBytes(8));

            var virtualSize = buffer.ReadU32LE();
            var virtualAddress = buffer.ReadU32LE();
            var sizeOfRawData = buffer.ReadU32LE();
            var pointerToRawData = buffer.ReadU32LE();
            var pointerToRelocations = buffer.ReadU32LE();
            var pointerToLinenumbers = buffer.ReadU32LE();
            var numberOfRelocations = buffer.ReadU16LE();
            var numberOfLinenumbers = buffer.ReadU16LE();
            var characteristics = buffer.ReadU32LE();

            sections.Add(new PeSectionData
            {
                NameBytes = nameBytes,
                VirtualSize = virtualSize,
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
}

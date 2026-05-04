using System.Text;
using Acorn.Codec;
using Acorn.Frame;
using Acorn.Pe.Data;

namespace Acorn.Pe.Decode;

/// <summary>
///     PE 文件解码器，解析 Windows 可执行文件（.exe, .dll）格式。
///     支持 DOS 头、PE 头、可选头、节区表、节区内容、导入表和重定位表解码。
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

        var mergedHeader = new PeHeaderData
        {
            DosMagic = dosHeader.DosMagic,
            PeHeaderOffset = dosHeader.PeHeaderOffset,
            PeMagic = peHeader.PeMagic,
            Machine = peHeader.Machine,
            NumberOfSections = peHeader.NumberOfSections,
            TimeDateStamp = peHeader.TimeDateStamp,
            PointerToSymbolTable = peHeader.PointerToSymbolTable,
            NumberOfSymbols = peHeader.NumberOfSymbols,
            SizeOfOptionalHeader = peHeader.SizeOfOptionalHeader,
            Characteristics = peHeader.Characteristics
        };

        var result = new PeFileData
        {
            Header = mergedHeader,
            OptionalHeader = optionalHeader,
            Sections = sections
        };

        ReadSectionContents(ref buffer, result);
        result.Imports = ReadImportTable(ref buffer, result);
        result.Relocations = ReadRelocations(ref buffer, result);

        return result;
    }

    /// <summary>
    ///     读取 DOS 头。
    /// </summary>
    private PeHeaderData ReadDosHeader(ref ByteBuffer buffer)
    {
        var dosMagic = buffer.ReadU16LE();

        if (dosMagic != PeConstants.DosMagic)
        {
            throw new InvalidDataException("不是有效的 PE 文件（DOS 头魔数不匹配）");
        }

        buffer.Advance(PeConstants.PeOffsetPosition - 2);

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

        if (peMagic != PeConstants.PeMagic)
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

        if (magic == PeConstants.OptionalMagicPE32)
        {
            baseOfData = buffer.ReadU32LE();
            imageBase = buffer.ReadU32LE();
        }
        else if (magic == PeConstants.OptionalMagicPE32Plus)
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

        if (magic == PeConstants.OptionalMagicPE32)
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

    /// <summary>
    ///     读取所有节区的原始内容。
    /// </summary>
    private void ReadSectionContents(ref ByteBuffer buffer, PeFileData data)
    {
        for (var i = 0; i < data.Sections.Count; i++)
        {
            var section = data.Sections[i];

            if (section.PointerToRawData == 0 || section.SizeOfRawData == 0)
            {
                continue;
            }

            var offset = (int)section.PointerToRawData;
            var rawSize = (int)section.SizeOfRawData;
            var contentSize = (int)Math.Min(section.VirtualSize, section.SizeOfRawData);

            if (contentSize == 0)
            {
                continue;
            }

            if (offset + contentSize > buffer.Length)
            {
                continue;
            }

            buffer.Position = offset;
            var content = buffer.ReadBytes(contentSize).ToArray();

            if (content.Length > 0)
            {
                data.SectionContents[i] = content;
            }
        }
    }

    /// <summary>
    ///     读取导入表。
    /// </summary>
    private List<PeImportDescriptor> ReadImportTable(ref ByteBuffer buffer, PeFileData data)
    {
        var imports = new List<PeImportDescriptor>();
        var importDir = data.GetDataDirectory(PeDataDirectoryIndex.ImportTable);

        if (importDir.IsEmpty)
        {
            return imports;
        }

        var is64 = data.Is64Bit;
        var thunkSize = is64 ? PeConstants.ImportThunkSize64 : PeConstants.ImportThunkSize32;
        var ordinalFlag = is64 ? PeConstants.ImportOrdinalFlag64 : PeConstants.ImportOrdinalFlag32;
        var descOffset = data.RvaToOffset(importDir.Rva);

        buffer.Position = descOffset;

        while (true)
        {
            var originalFirstThunk = buffer.ReadU32LE();
            var timeDateStamp = buffer.ReadU32LE();
            var forwarderChain = buffer.ReadU32LE();
            var nameRva = buffer.ReadU32LE();
            var firstThunk = buffer.ReadU32LE();

            if (originalFirstThunk == 0 && firstThunk == 0)
            {
                break;
            }

            var nameOffset = data.RvaToOffset(nameRva);
            var savedPos = buffer.Position;

            buffer.Position = nameOffset;
            var dllName = ReadNullTerminatedAscii(ref buffer);
            buffer.Position = savedPos;

            var iltOffset = data.RvaToOffset(originalFirstThunk);
            buffer.Position = iltOffset;

            var thunks = new List<PeImportThunk>();

            while (true)
            {
                ulong thunkValue;

                if (is64)
                {
                    thunkValue = buffer.ReadU64LE();
                }
                else
                {
                    thunkValue = buffer.ReadU32LE();
                }

                if (thunkValue == 0)
                {
                    break;
                }

                var isOrdinal = (thunkValue & ordinalFlag) != 0;
                ushort ordinal = 0;
                var funcName = "";

                if (isOrdinal)
                {
                    ordinal = (ushort)(thunkValue & 0xFFFF);
                }
                else
                {
                    var hintNameOffset = data.RvaToOffset((uint)thunkValue);
                    savedPos = buffer.Position;

                    buffer.Position = hintNameOffset;
                    buffer.ReadU16LE();
                    funcName = ReadNullTerminatedAscii(ref buffer);
                    buffer.Position = savedPos;
                }

                thunks.Add(new PeImportThunk
                {
                    Value = thunkValue,
                    IsOrdinal = isOrdinal,
                    Ordinal = ordinal,
                    Name = funcName
                });
            }

            imports.Add(new PeImportDescriptor
            {
                OriginalFirstThunk = originalFirstThunk,
                TimeDateStamp = timeDateStamp,
                ForwarderChain = forwarderChain,
                NameRva = nameRva,
                FirstThunk = firstThunk,
                Name = dllName,
                Thunks = thunks
            });

            buffer.Position = descOffset + imports.Count * PeConstants.ImportDescriptorSize;
        }

        return imports;
    }

    /// <summary>
    ///     读取重定位表。
    /// </summary>
    private List<PeBaseRelocationBlock> ReadRelocations(ref ByteBuffer buffer, PeFileData data)
    {
        var relocs = new List<PeBaseRelocationBlock>();
        var relocDir = data.GetDataDirectory(PeDataDirectoryIndex.BaseRelocationTable);

        if (relocDir.IsEmpty)
        {
            return relocs;
        }

        var offset = data.RvaToOffset(relocDir.Rva);
        var endOffset = Math.Min(offset + (int)relocDir.Size, buffer.Length);

        buffer.Position = offset;

        while (buffer.Position < endOffset)
        {
            var virtualAddress = buffer.ReadU32LE();
            var sizeOfBlock = buffer.ReadU32LE();

            if (sizeOfBlock == 0)
            {
                break;
            }

            var entryCount = ((int)sizeOfBlock - PeConstants.RelocationBlockHeaderSize) / PeConstants.RelocationEntrySize;
            var entries = new List<PeBaseRelocationEntry>();

            for (var i = 0; i < entryCount; i++)
            {
                var encoded = buffer.ReadU16LE();
                var type = (byte)(encoded >> 12);
                var relOffset = (ushort)(encoded & 0xFFF);

                entries.Add(new PeBaseRelocationEntry { Type = type, Offset = relOffset });
            }

            relocs.Add(new PeBaseRelocationBlock
            {
                VirtualAddress = virtualAddress,
                Entries = entries
            });
        }

        return relocs;
    }

    /// <summary>
    ///     从缓冲区当前位置读取 null 终止的 ASCII 字符串。
    /// </summary>
    private static string ReadNullTerminatedAscii(ref ByteBuffer buffer)
    {
        var bytes = new List<byte>();

        while (true)
        {
            var b = buffer.ReadU8();

            if (b == 0)
            {
                break;
            }

            bytes.Add(b);
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}

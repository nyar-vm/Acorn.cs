using Acorn.Codec;
using Acorn.Pe.Data;
using Acorn.Pe.Decode;
using Acorn.Pe.Encode;

namespace Acorn.Native.Tests;

public sealed class PeRoundTripTests
{
    #region 最小 PE32 往返

    [Fact]
    public void Encode_Decode_MinimalPe32()
    {
        var original = CreateMinimalPeFile(is64: false);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        AssertHeaderEqual(original.Header, decoded.Header);
        AssertOptionalHeaderEqual(original.OptionalHeader, decoded.OptionalHeader);
        Assert.Equal(original.Sections.Count, decoded.Sections.Count);
    }

    #endregion

    #region 最小 PE64 往返

    [Fact]
    public void Encode_Decode_MinimalPe64()
    {
        var original = CreateMinimalPeFile(is64: true);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        AssertHeaderEqual(original.Header, decoded.Header);
        AssertOptionalHeaderEqual(original.OptionalHeader, decoded.OptionalHeader);
        Assert.Equal(original.Sections.Count, decoded.Sections.Count);
    }

    #endregion

    #region 含节区内容的往返

    [Fact]
    public void Encode_Decode_WithSectionContent()
    {
        var textContent = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 };
        var dataContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        var original = CreatePeFileWithSections(
            is64: true,
            textContent,
            dataContent);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        AssertHeaderEqual(original.Header, decoded.Header);

        for (var i = 0; i < original.Sections.Count; i++)
        {
            var orig = original.Sections[i];
            var dec = decoded.Sections[i];

            Assert.Equal(orig.VirtualAddress, dec.VirtualAddress);
            Assert.Equal(orig.SizeOfRawData, dec.SizeOfRawData);
            Assert.Equal(orig.Characteristics, dec.Characteristics);
        }

        Assert.Equal(textContent, decoded.SectionContents[0]);
        Assert.Equal(dataContent, decoded.SectionContents[1]);
    }

    [Fact]
    public void Encode_Decode_WithSectionContent_32Bit()
    {
        var textContent = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0xC3 };
        var dataContent = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var original = CreatePeFileWithSections(
            is64: false,
            textContent,
            dataContent);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(textContent, decoded.SectionContents[0]);
        Assert.Equal(dataContent, decoded.SectionContents[1]);
    }

    #endregion

    #region 含导入表的往返

    [Fact]
    public void Encode_Decode_WithImports()
    {
        var textContent = new byte[] { 0xE8, 0x00, 0x00, 0x00, 0x00, 0xC3 };

        var original = new PeFileData
        {
            Header = CreatePeHeader(is64: true, sectionCount: 3),
            OptionalHeader = CreateOptionalHeader(is64: true, importRva: 0x3000, importSize: 128, relocationRva: 0, relocationSize: 0, sectionCount: 3),
            Sections =
            [
                CreateSection(".text", 0x1000, 0x200, 0x200, 0x200, PeConstants.SectionCharacteristicsCode | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".rdata", 0x2000, 0x200, 0x200, 0x400, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".idata", 0x3000, 0x200, 0x200, 0x600, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable)
            ],
            Imports =
            [
                new PeImportDescriptor
                {
                    Name = "KERNEL32.dll",
                    Thunks =
                    [
                        new PeImportThunk { IsOrdinal = false, Name = "GetStdHandle" },
                        new PeImportThunk { IsOrdinal = false, Name = "WriteFile" },
                        new PeImportThunk { IsOrdinal = false, Name = "ExitProcess" }
                    ]
                },
                new PeImportDescriptor
                {
                    Name = "USER32.dll",
                    Thunks =
                    [
                        new PeImportThunk { IsOrdinal = false, Name = "MessageBoxA" }
                    ]
                }
            ]
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Imports.Count, decoded.Imports.Count);

        for (var d = 0; d < original.Imports.Count; d++)
        {
            var origDll = original.Imports[d];
            var decDll = decoded.Imports[d];

            Assert.Equal(origDll.Name, decDll.Name);
            Assert.Equal(origDll.Thunks.Count, decDll.Thunks.Count);

            for (var t = 0; t < origDll.Thunks.Count; t++)
            {
                Assert.Equal(origDll.Thunks[t].Name, decDll.Thunks[t].Name);
                Assert.Equal(origDll.Thunks[t].IsOrdinal, decDll.Thunks[t].IsOrdinal);
            }
        }
    }

    [Fact]
    public void Encode_Decode_WithImports_32Bit()
    {
        var textContent = new byte[] { 0xE8, 0x00, 0x00, 0x00, 0x00, 0xC3 };

        var original = new PeFileData
        {
            Header = CreatePeHeader(is64: false, sectionCount: 3),
            OptionalHeader = CreateOptionalHeader(is64: false, importRva: 0x3000, importSize: 128, relocationRva: 0, relocationSize: 0, sectionCount: 3),
            Sections =
            [
                CreateSection(".text", 0x1000, 0x200, 0x200, 0x200, PeConstants.SectionCharacteristicsCode | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".rdata", 0x2000, 0x200, 0x200, 0x400, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".idata", 0x3000, 0x200, 0x200, 0x600, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable)
            ],
            Imports =
            [
                new PeImportDescriptor
                {
                    Name = "KERNEL32.dll",
                    Thunks =
                    [
                        new PeImportThunk { IsOrdinal = true, Ordinal = 42 },
                        new PeImportThunk { IsOrdinal = false, Name = "ExitProcess" }
                    ]
                }
            ]
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Imports.Count, decoded.Imports.Count);
        Assert.Equal("KERNEL32.dll", decoded.Imports[0].Name);
        Assert.Equal(2, decoded.Imports[0].Thunks.Count);
        Assert.True(decoded.Imports[0].Thunks[0].IsOrdinal);
        Assert.Equal((ushort)42, decoded.Imports[0].Thunks[0].Ordinal);
        Assert.Equal("ExitProcess", decoded.Imports[0].Thunks[1].Name);
    }

    #endregion

    #region 含重定位表的往返

    [Fact]
    public void Encode_Decode_WithRelocations()
    {
        var original = new PeFileData
        {
            Header = CreatePeHeader(is64: true, sectionCount: 3),
            OptionalHeader = CreateOptionalHeader(is64: true, importRva: 0, importSize: 0, relocationRva: 0x3000, relocationSize: 128, sectionCount: 3),
            Sections =
            [
                CreateSection(".text", 0x1000, 0x200, 0x200, 0x200, PeConstants.SectionCharacteristicsCode | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".rdata", 0x2000, 0x200, 0x200, 0x400, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".reloc", 0x3000, 0x200, 0x200, 0x600, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable)
            ],
            Relocations =
            [
                new PeBaseRelocationBlock
                {
                    VirtualAddress = 0x1000,
                    Entries =
                    [
                        new PeBaseRelocationEntry { Type = PeConstants.RelocationTypeDir64, Offset = 0x20 },
                        new PeBaseRelocationEntry { Type = PeConstants.RelocationTypeDir64, Offset = 0x28 },
                        new PeBaseRelocationEntry { Type = PeConstants.RelocationTypeDir64, Offset = 0x30 }
                    ]
                },
                new PeBaseRelocationBlock
                {
                    VirtualAddress = 0x2000,
                    Entries =
                    [
                        new PeBaseRelocationEntry { Type = PeConstants.RelocationTypeDir64, Offset = 0x50 }
                    ]
                }
            ]
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Relocations.Count, decoded.Relocations.Count);

        for (var b = 0; b < original.Relocations.Count; b++)
        {
            var origBlock = original.Relocations[b];
            var decBlock = decoded.Relocations[b];

            Assert.Equal(origBlock.VirtualAddress, decBlock.VirtualAddress);
            Assert.Equal(origBlock.Entries.Count, decBlock.Entries.Count);

            for (var e = 0; e < origBlock.Entries.Count; e++)
            {
                Assert.Equal(origBlock.Entries[e].Type, decBlock.Entries[e].Type);
                Assert.Equal(origBlock.Entries[e].Offset, decBlock.Entries[e].Offset);
            }
        }
    }

    [Fact]
    public void Encode_Decode_WithRelocations_32Bit()
    {
        var original = new PeFileData
        {
            Header = CreatePeHeader(is64: false, sectionCount: 3),
            OptionalHeader = CreateOptionalHeader(is64: false, importRva: 0, importSize: 0, relocationRva: 0x3000, relocationSize: 64, sectionCount: 3),
            Sections =
            [
                CreateSection(".text", 0x1000, 0x200, 0x200, 0x200, PeConstants.SectionCharacteristicsCode | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".rdata", 0x2000, 0x200, 0x200, 0x400, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".reloc", 0x3000, 0x200, 0x200, 0x600, PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable)
            ],
            Relocations =
            [
                new PeBaseRelocationBlock
                {
                    VirtualAddress = 0x1000,
                    Entries =
                    [
                        new PeBaseRelocationEntry { Type = PeConstants.RelocationTypeHighLow, Offset = 0x14 },
                        new PeBaseRelocationEntry { Type = PeConstants.RelocationTypeHighLow, Offset = 0x2C }
                    ]
                }
            ]
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.Relocations);
        Assert.Equal((uint)0x1000, decoded.Relocations[0].VirtualAddress);
        Assert.Equal(2, decoded.Relocations[0].Entries.Count);
        Assert.Equal(PeConstants.RelocationTypeHighLow, decoded.Relocations[0].Entries[0].Type);
    }

    #endregion

    #region 多架构往返

    [Theory]
    [InlineData(0x014C, false)]
    [InlineData(0x8664, true)]
    [InlineData(0x01C0, false)]
    [InlineData(0xAA64, true)]
    [InlineData(0x01C4, false)]
    public void Encode_Decode_MultiArchitecture(ushort machine, bool is64)
    {
        var original = CreateMinimalPeFile(is64, machine);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PeDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(machine, decoded.Header.Machine);
        Assert.Equal(is64, decoded.Is64Bit);
    }

    #endregion

    #region 辅助方法

    private static PeFileData CreateMinimalPeFile(bool is64, ushort machine = 0)
    {
        return new PeFileData
        {
            Header = CreatePeHeader(is64, 0, machine),
            OptionalHeader = CreateOptionalHeader(is64, 0, 0, 0, 0, 0),
            Sections = []
        };
    }

    private static PeFileData CreatePeFileWithSections(bool is64, byte[] textContent, byte[] dataContent)
    {
        var result = new PeFileData
        {
            Header = CreatePeHeader(is64, 2),
            OptionalHeader = CreateOptionalHeader(is64, 0, 0, 0, 0, 2),
            Sections =
            [
                CreateSection(".text", 0x1000, (uint)textContent.Length, 0x200, 0x200,
                    PeConstants.SectionCharacteristicsCode | PeConstants.SectionCharacteristicsReadable),
                CreateSection(".data", 0x2000, (uint)dataContent.Length, 0x200, 0x400,
                    PeConstants.SectionCharacteristicsInitializedData | PeConstants.SectionCharacteristicsReadable | PeConstants.SectionCharacteristicsWritable)
            ]
        };

        result.SectionContents[0] = textContent;
        result.SectionContents[1] = dataContent;

        return result;
    }

    private static PeHeaderData CreatePeHeader(bool is64, int sectionCount, ushort machine = 0)
    {
        if (machine == 0)
        {
            machine = is64 ? (ushort)0x8664 : (ushort)0x014C;
        }

        return new PeHeaderData
        {
            DosMagic = PeConstants.DosMagic,
            PeHeaderOffset = 0x40,
            PeMagic = PeConstants.PeMagic,
            Machine = machine,
            NumberOfSections = (ushort)sectionCount,
            TimeDateStamp = 0,
            PointerToSymbolTable = 0,
            NumberOfSymbols = 0,
            SizeOfOptionalHeader = is64 ? (ushort)240 : (ushort)224,
            Characteristics = is64 ? (ushort)0x002F : (ushort)0x010F
        };
    }

    private static PeOptionalHeaderData CreateOptionalHeader(bool is64, uint importRva, uint importSize, uint relocationRva, uint relocationSize, int sectionCount)
    {
        var dataDirs = new List<PeDataDirectoryEntry>();

        for (var i = 0; i < 16; i++)
        {
            dataDirs.Add(new PeDataDirectoryEntry { Rva = 0, Size = 0 });
        }

        if (importRva != 0)
        {
            dataDirs[(int)PeDataDirectoryIndex.ImportTable] = new PeDataDirectoryEntry { Rva = importRva, Size = importSize };
        }

        if (relocationRva != 0)
        {
            dataDirs[(int)PeDataDirectoryIndex.BaseRelocationTable] = new PeDataDirectoryEntry { Rva = relocationRva, Size = relocationSize };
        }

        return new PeOptionalHeaderData
        {
            Magic = is64 ? PeConstants.OptionalMagicPE32Plus : PeConstants.OptionalMagicPE32,
            MajorLinkerVersion = 14,
            MinorLinkerVersion = 0,
            SizeOfCode = 0x200,
            SizeOfInitializedData = 0x400,
            SizeOfUninitializedData = 0,
            AddressOfEntryPoint = sectionCount > 0 ? 0x1000u : 0u,
            BaseOfCode = 0x1000,
            BaseOfData = is64 ? 0u : 0x2000u,
            ImageBase = is64 ? 0x140000000ul : 0x00400000ul,
            SectionAlignment = 0x1000,
            FileAlignment = 0x200,
            MajorOperatingSystemVersion = 6,
            MinorOperatingSystemVersion = 0,
            MajorImageVersion = 0,
            MinorImageVersion = 0,
            MajorSubsystemVersion = 6,
            MinorSubsystemVersion = 0,
            Win32VersionValue = 0,
            SizeOfImage = sectionCount > 0 ? (uint)((sectionCount + 1) * 0x1000) : 0x1000,
            SizeOfHeaders = 0x200,
            CheckSum = 0,
            Subsystem = 3,
            DllCharacteristics = is64 ? (ushort)0x0160 : (ushort)0x8140,
            SizeOfStackReserve = is64 ? 0x100000ul : 0x100000ul,
            SizeOfStackCommit = is64 ? 0x1000ul : 0x1000ul,
            SizeOfHeapReserve = is64 ? 0x100000ul : 0x100000ul,
            SizeOfHeapCommit = is64 ? 0x1000ul : 0x1000ul,
            LoaderFlags = 0,
            NumberOfRvaAndSizes = 16,
            DataDirectories = dataDirs
        };
    }

    private static PeSectionData CreateSection(string name, uint virtualAddress, uint virtualSize, uint rawSize, uint pointerToRawData, uint characteristics)
    {
        var nameBytes = new byte[8];
        var nameStr = System.Text.Encoding.ASCII.GetBytes(name);

        for (var i = 0; i < Math.Min(nameStr.Length, 8); i++)
        {
            nameBytes[i] = nameStr[i];
        }

        return new PeSectionData
        {
            NameBytes = Acorn.Codec.FixedBytes8.FromSpan(nameBytes),
            VirtualSize = virtualSize,
            VirtualAddress = virtualAddress,
            SizeOfRawData = rawSize,
            PointerToRawData = pointerToRawData,
            Characteristics = characteristics
        };
    }

    private static void AssertHeaderEqual(PeHeaderData expected, PeHeaderData actual)
    {
        Assert.Equal(expected.DosMagic, actual.DosMagic);
        Assert.Equal(expected.PeHeaderOffset, actual.PeHeaderOffset);
        Assert.Equal(expected.PeMagic, actual.PeMagic);
        Assert.Equal(expected.Machine, actual.Machine);
        Assert.Equal(expected.NumberOfSections, actual.NumberOfSections);
        Assert.Equal(expected.SizeOfOptionalHeader, actual.SizeOfOptionalHeader);
        Assert.Equal(expected.Characteristics, actual.Characteristics);
    }

    private static void AssertOptionalHeaderEqual(PeOptionalHeaderData expected, PeOptionalHeaderData actual)
    {
        Assert.Equal(expected.Magic, actual.Magic);
        Assert.Equal(expected.ImageBase, actual.ImageBase);
        Assert.Equal(expected.SectionAlignment, actual.SectionAlignment);
        Assert.Equal(expected.FileAlignment, actual.FileAlignment);
        Assert.Equal(expected.SizeOfHeaders, actual.SizeOfHeaders);
        Assert.Equal(expected.Subsystem, actual.Subsystem);
    }

    #endregion
}

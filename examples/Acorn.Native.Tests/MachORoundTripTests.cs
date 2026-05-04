using Acorn.MachO.Data;
using Acorn.MachO.Decode;
using Acorn.MachO.Encode;
using System.Text;

namespace Acorn.Native.Tests;

public sealed class MachORoundTripTests
{
    #region 最小往返

    [Fact]
    public void EncodeDecode_Minimal64Bit_Roundtrip()
    {
        var data = CreateMinimalMachO(true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded);
        Assert.Equal(data.Header.Magic, decoded.Header.Magic);
        Assert.Equal(data.Header.CPUType, decoded.Header.CPUType);
        Assert.Equal(data.Header.CPUSubtype, decoded.Header.CPUSubtype);
        Assert.Equal(data.Header.FileType, decoded.Header.FileType);
        Assert.Equal(data.Header.Flags, decoded.Header.Flags);
        Assert.Equal(data.Header.NumberOfLoadCommands, decoded.Header.NumberOfLoadCommands);
        Assert.True(decoded.Header.Is64Bit);
        Assert.True(decoded.Header.IsLittleEndian);
    }

    [Fact]
    public void EncodeDecode_Minimal32Bit_Roundtrip()
    {
        var data = CreateMinimalMachO(false);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded);
        Assert.Equal(data.Header.Magic, decoded.Header.Magic);
        Assert.Equal(data.Header.CPUType, decoded.Header.CPUType);
        Assert.False(decoded.Header.Is64Bit);
    }

    #endregion

    #region 带节区内容往返

    [Fact]
    public void EncodeDecode_WithTextSection_Roundtrip()
    {
        var textContent = new byte[] { 0x55, 0x48, 0x89, 0xE5, 0xC3 };
        var data = CreateMachOWithTextSection(textContent);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.Sections);
        Assert.Equal("__text", decoded.Sections[0].SectionName);
        Assert.Equal("__TEXT", decoded.Sections[0].SegmentName);
        Assert.Equal(textContent, decoded.Sections[0].Content);
        Assert.Equal((ulong)textContent.Length, decoded.Sections[0].Size);
    }

    [Fact]
    public void EncodeDecode_WithDataSection_Roundtrip()
    {
        var dataContent = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE };
        var data = CreateMachOWithDataSection(dataContent);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.Sections);
        Assert.Equal("__data", decoded.Sections[0].SectionName);
        Assert.Equal("__DATA", decoded.Sections[0].SegmentName);
        Assert.Equal(dataContent, decoded.Sections[0].Content);
    }

    [Fact]
    public void EncodeDecode_WithMultipleSections_Roundtrip()
    {
        var textContent = new byte[] { 0xC3 };
        var dataContent = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };

        var data = CreateMachOWithMultipleSections(textContent, dataContent);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(2, decoded.Sections.Count);
        Assert.Equal("__text", decoded.Sections[0].SectionName);
        Assert.Equal(textContent, decoded.Sections[0].Content);
        Assert.Equal("__data", decoded.Sections[1].SectionName);
        Assert.Equal(dataContent, decoded.Sections[1].Content);
    }

    [Fact]
    public void EncodeDecode_LargeSectionContent_Roundtrip()
    {
        var largeContent = new byte[4096];
        for (var i = 0; i < largeContent.Length; i++)
        {
            largeContent[i] = (byte)(i % 256);
        }

        var data = CreateMachOWithTextSection(largeContent);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.Sections);
        Assert.Equal(largeContent, decoded.Sections[0].Content);
    }

    #endregion

    #region 不同架构往返

    [Fact]
    public void EncodeDecode_X86_64_Roundtrip()
    {
        var data = CreateMinimalMachO(true, cpuType: 0x01000007);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(0x01000007, decoded.Header.CPUType);
        Assert.True(decoded.Header.Is64Bit);
    }

    [Fact]
    public void EncodeDecode_ARM64_Roundtrip()
    {
        var data = CreateMinimalMachO(true, cpuType: 0x0100000C);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(0x0100000C, decoded.Header.CPUType);
        Assert.True(decoded.Header.Is64Bit);
    }

    [Fact]
    public void EncodeDecode_ARM_Roundtrip()
    {
        var data = CreateMinimalMachO(false, cpuType: 12);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(12, decoded.Header.CPUType);
        Assert.False(decoded.Header.Is64Bit);
    }

    #endregion

    #region 文件类型往返

    [Theory]
    [InlineData(1u, "MH_OBJECT")]
    [InlineData(2u, "MH_EXECUTE")]
    [InlineData(6u, "MH_DYLIB")]
    [InlineData(8u, "MH_BUNDLE")]
    public void EncodeDecode_FileType_Roundtrip(uint fileType, string name)
    {
        var data = CreateMinimalMachO(true, fileType: fileType);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(fileType, decoded.Header.FileType);
    }

    #endregion

    #region 标志位往返

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(0x200085u)]
    public void EncodeDecode_Flags_Roundtrip(uint flags)
    {
        var data = CreateMinimalMachO(true, flags: flags);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(flags, decoded.Header.Flags);
    }

    #endregion

    #region 预留字段往返

    [Fact]
    public void EncodeDecode_ReservedField_Roundtrip()
    {
        var data = new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = 0xFEEDFACF,
                CPUType = 0x01000007,
                CPUSubtype = 3,
                FileType = 2,
                NumberOfLoadCommands = 0,
                SizeOfLoadCommands = 0,
                Flags = 0,
                Reserved = 42,
                IsLittleEndian = true
            },
            LoadCommands = [],
            Sections = []
        };

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(42u, decoded.Header.Reserved);
    }

    #endregion

    #region 大端序往返

    [Fact]
    public void EncodeDecode_BigEndian64Bit_Roundtrip()
    {
        var data = new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = 0xFEEDFACF,
                CPUType = 0x01000007,
                CPUSubtype = 3,
                FileType = 2,
                NumberOfLoadCommands = 0,
                SizeOfLoadCommands = 0,
                Flags = 1,
                Reserved = 0,
                IsLittleEndian = false
            },
            LoadCommands = [],
            Sections = []
        };

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(data.Header.Magic, decoded.Header.Magic);
        Assert.Equal(data.Header.CPUType, decoded.Header.CPUType);
        Assert.False(decoded.Header.IsLittleEndian);
    }

    [Fact]
    public void EncodeDecode_BigEndian32Bit_Roundtrip()
    {
        var data = new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = 0xFEEDFACE,
                CPUType = 7,
                CPUSubtype = 3,
                FileType = 2,
                NumberOfLoadCommands = 0,
                SizeOfLoadCommands = 0,
                Flags = 1,
                Reserved = 0,
                IsLittleEndian = false
            },
            LoadCommands = [],
            Sections = []
        };

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new MachODecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(data.Header.Magic, decoded.Header.Magic);
        Assert.False(decoded.Header.IsLittleEndian);
    }

    #endregion

    #region 辅助方法

    private static MachOFileData CreateMinimalMachO(
        bool is64, int cpuType = 0, uint fileType = 2, uint flags = 0)
    {
        var actualCpuType = cpuType != 0 ? cpuType : (is64 ? 0x01000007 : 7);

        return new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = is64 ? 0xFEEDFACF : 0xFEEDFACE,
                CPUType = actualCpuType,
                CPUSubtype = 3,
                FileType = fileType,
                NumberOfLoadCommands = 0,
                SizeOfLoadCommands = 0,
                Flags = flags,
                Reserved = 0,
                IsLittleEndian = true
            },
            LoadCommands = [],
            Sections = []
        };
    }

    private static MachOFileData CreateMachOWithTextSection(byte[] content)
    {
        return CreateMachOWithSection(content, "__text", "__TEXT", 0x80000400);
    }

    private static MachOFileData CreateMachOWithDataSection(byte[] content)
    {
        return CreateMachOWithSection(content, "__data", "__DATA", 0);
    }

    private static MachOFileData CreateMachOWithSection(
        byte[] content, string sectionName, string segmentName, uint sectionFlags, int alignment = 4)
    {
        var headerSize = 32;
        var sectionSize = 80;
        var segmentCommandSize = 72 + sectionSize;
        var sectionOffset = (uint)(headerSize + segmentCommandSize);

        var segmentData = new byte[segmentCommandSize - 8];
        Encoding.UTF8.GetBytes($"{segmentName}\0").AsSpan().CopyTo(segmentData);

        return new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = 0xFEEDFACF,
                CPUType = 0x01000007,
                CPUSubtype = 3,
                FileType = 2,
                NumberOfLoadCommands = 1,
                SizeOfLoadCommands = (uint)segmentCommandSize,
                Flags = 0,
                Reserved = 0,
                IsLittleEndian = true
            },
            LoadCommands =
            [
                new MachOLoadCommandData
                {
                    Command = 0x19,
                    Size = (uint)segmentCommandSize,
                    Data = segmentData
                }
            ],
            Sections =
            [
                new MachOSectionData
                {
                    SectionName = sectionName,
                    SegmentName = segmentName,
                    Address = 0,
                    Size = (ulong)content.Length,
                    Offset = sectionOffset,
                    Alignment = (uint)alignment,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = sectionFlags,
                    Content = content
                }
            ]
        };
    }

    private static MachOFileData CreateMachOWithMultipleSections(byte[] textContent, byte[] dataContent)
    {
        var headerSize = 32;
        var sectionSize = 80;
        var segmentCommandSize = 72 + sectionSize * 2;
        var textOffset = (uint)(headerSize + segmentCommandSize);
        var dataOffset = textOffset + (uint)textContent.Length;
        dataOffset = (dataOffset + 15) & ~15u;

        var segmentData = new byte[segmentCommandSize - 8];
        Encoding.UTF8.GetBytes("__TEXT\0").AsSpan().CopyTo(segmentData);

        return new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = 0xFEEDFACF,
                CPUType = 0x01000007,
                CPUSubtype = 3,
                FileType = 2,
                NumberOfLoadCommands = 1,
                SizeOfLoadCommands = (uint)segmentCommandSize,
                Flags = 0,
                Reserved = 0,
                IsLittleEndian = true
            },
            LoadCommands =
            [
                new MachOLoadCommandData
                {
                    Command = 0x19,
                    Size = (uint)segmentCommandSize,
                    Data = segmentData
                }
            ],
            Sections =
            [
                new MachOSectionData
                {
                    SectionName = "__text",
                    SegmentName = "__TEXT",
                    Address = 0,
                    Size = (ulong)textContent.Length,
                    Offset = textOffset,
                    Alignment = 4,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = 0x80000400,
                    Content = textContent
                },
                new MachOSectionData
                {
                    SectionName = "__data",
                    SegmentName = "__DATA",
                    Address = (ulong)textContent.Length,
                    Size = (ulong)dataContent.Length,
                    Offset = dataOffset,
                    Alignment = 3,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = 0,
                    Content = dataContent
                }
            ]
        };
    }

    #endregion
}

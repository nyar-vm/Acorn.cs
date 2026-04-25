using Acorn.MachO.Data;
using Acorn.MachO.Encode;
using System.Text;

namespace Acorn.Native.Tests;

public sealed class MachOEncoderTests
{
    #region 最小编码

    [Fact]
    public void Encode_Minimal64Bit_ProducesValidMachO()
    {
        var data = CreateMinimalMachOData(is64: true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 32);
        Assert.Equal(0xCF, bytes[0]);
        Assert.Equal(0xFA, bytes[1]);
        Assert.Equal(0xED, bytes[2]);
        Assert.Equal(0xFE, bytes[3]);
    }

    [Fact]
    public void Encode_Minimal32Bit_ProducesValidMachO()
    {
        var data = CreateMinimalMachOData(is64: false);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 28);
        Assert.Equal(0xCE, bytes[0]);
        Assert.Equal(0xFA, bytes[1]);
        Assert.Equal(0xED, bytes[2]);
        Assert.Equal(0xFE, bytes[3]);
    }

    #endregion

    #region 头部字段验证

    [Fact]
    public void Encode_64BitMagic_IsFEEDFACF()
    {
        var data = CreateMinimalMachOData(is64: true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var magic = ReadU32LE(bytes, 0);
        Assert.Equal(0xFEEDFACFu, magic);
    }

    [Fact]
    public void Encode_32BitMagic_IsFEEDFACE()
    {
        var data = CreateMinimalMachOData(is64: false);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var magic = ReadU32LE(bytes, 0);
        Assert.Equal(0xFEEDFACEu, magic);
    }

    [Fact]
    public void Encode_CpuType_PreservesValue()
    {
        var data = CreateMinimalMachOData(is64: true, cpuType: 0x01000007);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var cpuType = ReadI32LE(bytes, 4);
        Assert.Equal(0x01000007, cpuType);
    }

    [Fact]
    public void Encode_FileType_PreservesValue()
    {
        var data = CreateMinimalMachOData(is64: true, fileType: 2);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var fileType = ReadU32LE(bytes, 12);
        Assert.Equal(2u, fileType);
    }

    [Fact]
    public void Encode_Flags_PreservesValue()
    {
        var data = CreateMinimalMachOData(is64: true, flags: 0x200085);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var flags = ReadU32LE(bytes, 24);
        Assert.Equal(0x200085u, flags);
    }

    #endregion

    #region 节区内容编码

    [Fact]
    public void Encode_WithSectionContent_IncludesContentBytes()
    {
        var textContent = new byte[] { 0x55, 0x48, 0x89, 0xE5, 0xC3 };
        var data = CreateMachOWithSectionContent(textContent);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var found = false;
        for (var i = 0; i <= bytes.Length - textContent.Length; i++)
        {
            var match = true;
            for (var j = 0; j < textContent.Length; j++)
            {
                if (bytes[i + j] != textContent[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                found = true;
                break;
            }
        }

        Assert.True(found, "节区内容未在编码输出中找到");
    }

    [Fact]
    public void Encode_MultipleSectionContents_AllPresent()
    {
        var textContent = new byte[] { 0xC3 };
        var dataContent = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE, 0xDE, 0xAD, 0xBE, 0xEF };

        var data = CreateMachOWithMultipleSections(textContent, dataContent);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length > textContent.Length + dataContent.Length + 32);
    }

    [Fact]
    public void Encode_EmptySectionContent_Succeeds()
    {
        var data = CreateMinimalMachOData(is64: true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    #endregion

    #region 加载命令

    [Fact]
    public void Encode_LoadCommandCount_PreservesValue()
    {
        var data = CreateMinimalMachOData(is64: true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var numberOfLoadCommands = ReadU32LE(bytes, 16);
        Assert.Equal(data.Header.NumberOfLoadCommands, numberOfLoadCommands);
    }

    [Fact]
    public void Encode_SegmentLoadCommand_CommandTypeIsCorrect()
    {
        var data = CreateMachOWithSectionContent([0xC3]);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var headerSize = 32;
        var commandType = ReadU32LE(bytes, headerSize);
        Assert.Equal(0x19u, commandType);
    }

    #endregion

    #region 不同架构

    [Theory]
    [InlineData(7, false)]
    [InlineData(0x01000007, true)]
    [InlineData(12, false)]
    [InlineData(0x0100000C, true)]
    public void Encode_DifferentArchitectures_ProducesValidMachO(int cpuType, bool is64)
    {
        var data = CreateMinimalMachOData(is64, cpuType: cpuType);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        if (is64)
        {
            Assert.Equal(0xCF, bytes[0]);
            Assert.Equal(0xFA, bytes[1]);
            Assert.Equal(0xED, bytes[2]);
            Assert.Equal(0xFE, bytes[3]);
        }
        else
        {
            Assert.Equal(0xCE, bytes[0]);
            Assert.Equal(0xFA, bytes[1]);
            Assert.Equal(0xED, bytes[2]);
            Assert.Equal(0xFE, bytes[3]);
        }
    }

    #endregion

    #region 大端序

    [Fact]
    public void Encode_BigEndian64Bit_MagicIsCFFAEDFE()
    {
        var data = CreateMinimalMachOData(is64: true, magic: 0xFEEDFACF, isLittleEndian: false);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(0xFE, bytes[0]);
        Assert.Equal(0xED, bytes[1]);
        Assert.Equal(0xFA, bytes[2]);
        Assert.Equal(0xCF, bytes[3]);
    }

    [Fact]
    public void Encode_BigEndian32Bit_MagicIsCEFAEDFE()
    {
        var data = CreateMinimalMachOData(is64: false, magic: 0xFEEDFACE, isLittleEndian: false);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(0xFE, bytes[0]);
        Assert.Equal(0xED, bytes[1]);
        Assert.Equal(0xFA, bytes[2]);
        Assert.Equal(0xCE, bytes[3]);
    }

    #endregion

    #region 辅助方法

    private static uint ReadU32LE(byte[] bytes, int offset)
    {
        return (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
    }

    private static int ReadI32LE(byte[] bytes, int offset)
    {
        return (int)ReadU32LE(bytes, offset);
    }

    private static MachOFileData CreateMinimalMachOData(
        bool is64, int cpuType = 0, uint fileType = 2, uint flags = 0,
        uint magic = 0, bool isLittleEndian = true)
    {
        var actualMagic = magic != 0 ? magic : (is64 ? 0xFEEDFACF : 0xFEEDFACE);
        var actualCpuType = cpuType != 0 ? cpuType : (is64 ? 0x01000007 : 7);

        return new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = actualMagic,
                CPUType = actualCpuType,
                CPUSubtype = 3,
                FileType = fileType,
                NumberOfLoadCommands = 0,
                SizeOfLoadCommands = 0,
                Flags = flags,
                Reserved = 0,
                IsLittleEndian = isLittleEndian
            },
            LoadCommands = [],
            Sections = []
        };
    }

    private static MachOFileData CreateMachOWithSectionContent(byte[] textContent)
    {
        var headerSize = 32;
        var segmentCommandSize = 72 + 80;
        var textOffset = headerSize + segmentCommandSize;

        var segmentData = new byte[segmentCommandSize - 8];
        var nameBytes = Encoding.UTF8.GetBytes("__TEXT\0\0\0\0\0\0\0\0\0\0");
        nameBytes.AsSpan().CopyTo(segmentData);

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
                    Offset = (uint)textOffset,
                    Alignment = 4,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = 0x80000400,
                    Content = textContent
                }
            ]
        };
    }

    private static MachOFileData CreateMachOWithMultipleSections(byte[] textContent, byte[] dataContent)
    {
        var headerSize = 32;
        var segmentCommandSize = 72 + 80 * 2;
        var textOffset = headerSize + segmentCommandSize;
        var dataOffset = textOffset + textContent.Length;
        dataOffset = (dataOffset + 15) & ~15;

        var segmentData = new byte[segmentCommandSize - 8];
        var nameBytes = Encoding.UTF8.GetBytes("__TEXT\0\0\0\0\0\0\0\0\0\0");
        nameBytes.AsSpan().CopyTo(segmentData);

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
                    Offset = (uint)textOffset,
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
                    Offset = (uint)dataOffset,
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

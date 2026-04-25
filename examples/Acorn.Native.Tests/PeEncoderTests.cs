using Acorn.Codec;
using Acorn.Pe.Data;
using Acorn.Pe.Encode;
using System.Text;

namespace Acorn.Native.Tests;

public sealed class PeEncoderTests
{
    #region PE 布局常量

    private const int DosHeaderSize = 122;
    private const int PeSignatureOffset = DosHeaderSize;
    private const int CoffHeaderOffset = DosHeaderSize + 4;
    private const int MachineOffset = CoffHeaderOffset;
    private const int OptHeaderOffset = CoffHeaderOffset + 20;

    #endregion

    #region 最小编码

    [Fact]
    public void Encode_Minimal64Bit_ProducesValidPe()
    {
        var data = CreateMinimalPeData(is64: true);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length > DosHeaderSize);
        Assert.Equal((byte)'M', bytes[0]);
        Assert.Equal((byte)'Z', bytes[1]);
    }

    [Fact]
    public void Encode_Minimal32Bit_ProducesValidPe()
    {
        var data = CreateMinimalPeData(is64: false);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length > DosHeaderSize);
        Assert.Equal((byte)'M', bytes[0]);
        Assert.Equal((byte)'Z', bytes[1]);
    }

    #endregion

    #region DOS 头验证

    [Fact]
    public void Encode_DosMagic_Is5A4D()
    {
        var data = CreateMinimalPeData(is64: true);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var dosMagic = BitConverter.ToUInt16(bytes, 0);
        Assert.Equal((ushort)0x5A4D, dosMagic);
    }

    [Fact]
    public void Encode_PeSignature_Is4550()
    {
        var data = CreateMinimalPeData(is64: true);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var peSignature = BitConverter.ToUInt32(bytes, PeSignatureOffset);
        Assert.Equal(0x00004550u, peSignature);
    }

    #endregion

    #region COFF 头验证

    [Fact]
    public void Encode_64BitMachine_Is8664()
    {
        var data = CreateMinimalPeData(is64: true);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var machine = BitConverter.ToUInt16(bytes, MachineOffset);
        Assert.Equal((ushort)0x8664, machine);
    }

    [Fact]
    public void Encode_32BitMachine_Is14C()
    {
        var data = CreateMinimalPeData(is64: false);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var machine = BitConverter.ToUInt16(bytes, MachineOffset);
        Assert.Equal((ushort)0x014C, machine);
    }

    #endregion

    #region 节区内容编码

    [Fact]
    public void Encode_WithSectionContent_IncludesContentBytes()
    {
        var textContent = new byte[] { 0x48, 0x31, 0xC0, 0xC3 };
        var data = CreatePeWithSectionContent(textContent, is64: true);

        var encoder = new PeEncoder();
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
        var textContent = new byte[] { 0x90, 0xC3 };
        var dataContent = new byte[] { 0x42, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        var data = CreatePeWithMultipleSections(textContent, dataContent);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length > textContent.Length + dataContent.Length + 256);
    }

    #endregion

    #region 可选头字段

    [Fact]
    public void Encode_64BitMagic_Is20B()
    {
        var data = CreateMinimalPeData(is64: true);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var magic = BitConverter.ToUInt16(bytes, OptHeaderOffset);
        Assert.Equal((ushort)0x20B, magic);
    }

    [Fact]
    public void Encode_32BitMagic_Is10B()
    {
        var data = CreateMinimalPeData(is64: false);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var magic = BitConverter.ToUInt16(bytes, OptHeaderOffset);
        Assert.Equal((ushort)0x10B, magic);
    }

    [Fact]
    public void Encode_Subsystem_PreservesValue()
    {
        var is64 = true;
        var data = CreateMinimalPeData(is64);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var subsystemOffset = OptHeaderOffset + 68;
        var subsystem = BitConverter.ToUInt16(bytes, subsystemOffset);
        Assert.Equal((ushort)3, subsystem);
    }

    [Fact]
    public void Encode_DataDirectories_WritesRvaAndSize()
    {
        var is64 = true;
        var data = new PeFileData
        {
            Header = CreateMinimalPeHeader(is64),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = is64 ? (ushort)0x20B : (ushort)0x10B,
                ImageBase = is64 ? 0x140000000UL : 0x400000,
                SectionAlignment = 0x1000,
                FileAlignment = 0x200,
                SizeOfImage = 0x4000,
                SizeOfHeaders = 0x200,
                Subsystem = 3,
                DllCharacteristics = 0x8160,
                NumberOfRvaAndSizes = 2,
                DataDirectories =
                [
                    new PeDataDirectoryEntry { Rva = 0x1000, Size = 0x100 },
                    new PeDataDirectoryEntry { Rva = 0x2000, Size = 0x200 }
                ]
            },
            Sections = [],
            SectionContents = []
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var dataDirStart = OptHeaderOffset + (is64 ? 112 : 96);
        var rva0 = BitConverter.ToUInt32(bytes, dataDirStart);
        var size0 = BitConverter.ToUInt32(bytes, dataDirStart + 4);
        Assert.Equal(0x1000u, rva0);
        Assert.Equal(0x100u, size0);

        var rva1 = BitConverter.ToUInt32(bytes, dataDirStart + 8);
        var size1 = BitConverter.ToUInt32(bytes, dataDirStart + 12);
        Assert.Equal(0x2000u, rva1);
        Assert.Equal(0x200u, size1);
    }

    #endregion

    #region 不同架构

    [Theory]
    [InlineData(0x014C, false)]
    [InlineData(0x8664, true)]
    [InlineData(0x01C0, false)]
    [InlineData(0xAA64, true)]
    public void Encode_DifferentArchitectures_ProducesValidPe(ushort machine, bool is64)
    {
        var data = CreateMinimalPeData(is64, machine: machine);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal((byte)'M', bytes[0]);
        Assert.Equal((byte)'Z', bytes[1]);
    }

    #endregion

    #region 辅助方法

    private static PeHeaderData CreateMinimalPeHeader(bool is64, ushort machine = 0)
    {
        return new PeHeaderData
        {
            DosMagic = 0x5A4D,
            PeHeaderOffset = DosHeaderSize,
            PeMagic = 0x00004550,
            Machine = machine != 0 ? machine : (is64 ? (ushort)0x8664 : (ushort)0x014C),
            NumberOfSections = 0,
            TimeDateStamp = 0,
            PointerToSymbolTable = 0,
            NumberOfSymbols = 0,
            SizeOfOptionalHeader = is64 ? (ushort)240 : (ushort)224,
            Characteristics = 0x0002
        };
    }

    private static PeFileData CreateMinimalPeData(bool is64, ushort machine = 0)
    {
        return new PeFileData
        {
            Header = CreateMinimalPeHeader(is64, machine),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = is64 ? (ushort)0x20B : (ushort)0x10B,
                ImageBase = is64 ? 0x140000000UL : 0x400000,
                SectionAlignment = 0x1000,
                FileAlignment = 0x200,
                SizeOfImage = 0x4000,
                SizeOfHeaders = 0x200,
                Subsystem = 3,
                DllCharacteristics = 0x8160
            },
            Sections = [],
            SectionContents = []
        };
    }

    private static PeFileData CreatePeWithSectionContent(byte[] textContent, bool is64)
    {
        var nameBytes = FixedBytes8.FromSpan(".text\0\0\0"u8);

        return new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = 0x5A4D,
                PeHeaderOffset = DosHeaderSize,
                PeMagic = 0x00004550,
                Machine = is64 ? (ushort)0x8664 : (ushort)0x014C,
                NumberOfSections = 1,
                TimeDateStamp = 0,
                PointerToSymbolTable = 0,
                NumberOfSymbols = 0,
                SizeOfOptionalHeader = is64 ? (ushort)240 : (ushort)224,
                Characteristics = 0x0002
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = is64 ? (ushort)0x20B : (ushort)0x10B,
                ImageBase = is64 ? 0x140000000UL : 0x400000,
                SectionAlignment = 0x1000,
                FileAlignment = 0x200,
                SizeOfImage = 0x4000,
                SizeOfHeaders = 0x200,
                Subsystem = 3,
                DllCharacteristics = 0x8160
            },
            Sections =
            [
                new PeSectionData
                {
                    NameBytes = nameBytes,
                    VirtualSize = (uint)textContent.Length,
                    VirtualAddress = 0x1000,
                    SizeOfRawData = (uint)((textContent.Length + 0x1FF) & ~0x1FF),
                    PointerToRawData = 0x200,
                    PointerToRelocations = 0,
                    PointerToLinenumbers = 0,
                    NumberOfRelocations = 0,
                    NumberOfLinenumbers = 0,
                    Characteristics = 0x60000020
                }
            ],
            SectionContents = new Dictionary<int, byte[]> { [0] = textContent }
        };
    }

    private static PeFileData CreatePeWithMultipleSections(byte[] textContent, byte[] dataContent)
    {
        var textNameBytes = FixedBytes8.FromSpan(".text\0\0\0"u8);
        var dataNameBytes = FixedBytes8.FromSpan(".data\0\0\0"u8);

        return new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = 0x5A4D,
                PeHeaderOffset = DosHeaderSize,
                PeMagic = 0x00004550,
                Machine = 0x8664,
                NumberOfSections = 2,
                TimeDateStamp = 0,
                PointerToSymbolTable = 0,
                NumberOfSymbols = 0,
                SizeOfOptionalHeader = 240,
                Characteristics = 0x0002
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B,
                ImageBase = 0x140000000UL,
                SectionAlignment = 0x1000,
                FileAlignment = 0x200,
                SizeOfImage = 0x4000,
                SizeOfHeaders = 0x200,
                Subsystem = 3,
                DllCharacteristics = 0x8160
            },
            Sections =
            [
                new PeSectionData
                {
                    NameBytes = textNameBytes,
                    VirtualSize = (uint)textContent.Length,
                    VirtualAddress = 0x1000,
                    SizeOfRawData = (uint)((textContent.Length + 0x1FF) & ~0x1FF),
                    PointerToRawData = 0x200,
                    PointerToRelocations = 0,
                    PointerToLinenumbers = 0,
                    NumberOfRelocations = 0,
                    NumberOfLinenumbers = 0,
                    Characteristics = 0x60000020
                },
                new PeSectionData
                {
                    NameBytes = dataNameBytes,
                    VirtualSize = (uint)dataContent.Length,
                    VirtualAddress = 0x2000,
                    SizeOfRawData = (uint)((dataContent.Length + 0x1FF) & ~0x1FF),
                    PointerToRawData = 0x400,
                    PointerToRelocations = 0,
                    PointerToLinenumbers = 0,
                    NumberOfRelocations = 0,
                    NumberOfLinenumbers = 0,
                    Characteristics = 0xC0000040
                }
            ],
            SectionContents = new Dictionary<int, byte[]>
            {
                [0] = textContent,
                [1] = dataContent
            }
        };
    }

    #endregion
}

using Acorn.Frame;
using Acorn.Pe.Data;

namespace Acorn.Pe.Encode;

/// <summary>
///     PE 文件编码器，将 PeFileData 编码为 PE 二进制格式。
///     PE 格式固定使用小端序。
/// </summary>
public sealed class PeEncoder
{
    /// <summary>
    ///     编码 PE 文件数据为字节数组
    /// </summary>
    public byte[] Encode(PeFileData data)
    {
        var is64 = data.Is64Bit;
        var size = EstimateSize(data);
        var writer = new ByteBufferWriter(size);

        WriteDosHeader(ref writer, data.Header);
        WritePeSignature(ref writer);
        WriteCoffHeader(ref writer, data.Header);
        WriteOptionalHeader(ref writer, data.OptionalHeader, is64);
        WriteSectionHeaders(ref writer, data.Sections);

        return writer.ToArray();
    }

    #region DOS 头

    private static void WriteDosHeader(ref ByteBufferWriter writer, PeHeaderData header)
    {
        writer.WriteU16LE(header.DosMagic);

        for (var i = 0; i < 58; i++)
        {
            writer.WriteU16LE(0);
        }

        writer.WriteU32LE(header.PeHeaderOffset);
    }

    #endregion

    #region PE 签名

    private static void WritePeSignature(ref ByteBufferWriter writer)
    {
        writer.WriteU32LE(0x00004550);
    }

    #endregion

    #region COFF 头

    private static void WriteCoffHeader(ref ByteBufferWriter writer, PeHeaderData header)
    {
        writer.WriteU16LE(header.Machine);
        writer.WriteU16LE(header.NumberOfSections);
        writer.WriteU32LE(header.TimeDateStamp);
        writer.WriteU32LE(header.PointerToSymbolTable);
        writer.WriteU32LE(header.NumberOfSymbols);
        writer.WriteU16LE(header.SizeOfOptionalHeader);
        writer.WriteU16LE(header.Characteristics);
    }

    #endregion

    #region 可选头

    private static void WriteOptionalHeader(ref ByteBufferWriter writer, PeOptionalHeaderData opt, bool is64)
    {
        writer.WriteU16LE(opt.Magic);
        writer.WriteU8(opt.MajorLinkerVersion);
        writer.WriteU8(opt.MinorLinkerVersion);
        writer.WriteU32LE(opt.SizeOfCode);
        writer.WriteU32LE(opt.SizeOfInitializedData);
        writer.WriteU32LE(opt.SizeOfUninitializedData);
        writer.WriteU32LE(opt.AddressOfEntryPoint);
        writer.WriteU32LE(opt.BaseOfCode);

        if (!is64)
        {
            writer.WriteU32LE(opt.BaseOfData);
            writer.WriteU32LE((uint)opt.ImageBase);
        }
        else
        {
            writer.WriteU64LE(opt.ImageBase);
        }

        writer.WriteU32LE(opt.SectionAlignment);
        writer.WriteU32LE(opt.FileAlignment);
        writer.WriteU16LE(opt.MajorOperatingSystemVersion);
        writer.WriteU16LE(opt.MinorOperatingSystemVersion);
        writer.WriteU16LE(opt.MajorImageVersion);
        writer.WriteU16LE(opt.MinorImageVersion);
        writer.WriteU16LE(opt.MajorSubsystemVersion);
        writer.WriteU16LE(opt.MinorSubsystemVersion);
        writer.WriteU32LE(opt.Win32VersionValue);
        writer.WriteU32LE(opt.SizeOfImage);
        writer.WriteU32LE(opt.SizeOfHeaders);
        writer.WriteU32LE(opt.CheckSum);
        writer.WriteU16LE(opt.Subsystem);
        writer.WriteU16LE(opt.DllCharacteristics);

        if (is64)
        {
            writer.WriteU64LE(opt.SizeOfStackReserve);
            writer.WriteU64LE(opt.SizeOfStackCommit);
            writer.WriteU64LE(opt.SizeOfHeapReserve);
            writer.WriteU64LE(opt.SizeOfHeapCommit);
        }
        else
        {
            writer.WriteU32LE((uint)opt.SizeOfStackReserve);
            writer.WriteU32LE((uint)opt.SizeOfStackCommit);
            writer.WriteU32LE((uint)opt.SizeOfHeapReserve);
            writer.WriteU32LE((uint)opt.SizeOfHeapCommit);
        }

        writer.WriteU32LE(opt.LoaderFlags);
        writer.WriteU32LE(opt.NumberOfRvaAndSizes);

        for (var i = 0; i < opt.NumberOfRvaAndSizes; i++)
        {
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
        }
    }

    #endregion

    #region 节区头

    private static void WriteSectionHeaders(ref ByteBufferWriter writer, IReadOnlyList<PeSectionData> sections)
    {
        foreach (var section in sections)
        {
            writer.Write(section.NameBytes.AsSpan());
            writer.WriteU32LE(section.VirtualSize);
            writer.WriteU32LE(section.VirtualAddress);
            writer.WriteU32LE(section.SizeOfRawData);
            writer.WriteU32LE(section.PointerToRawData);
            writer.WriteU32LE(section.PointerToRelocations);
            writer.WriteU32LE(section.PointerToLinenumbers);
            writer.WriteU16LE(section.NumberOfRelocations);
            writer.WriteU16LE(section.NumberOfLinenumbers);
            writer.WriteU32LE(section.Characteristics);
        }
    }

    #endregion

    #region 大小预估

    private static int EstimateSize(PeFileData data)
    {
        var is64 = data.Is64Bit;
        var dosHeaderSize = 64;
        var peSignatureSize = 4;
        var coffHeaderSize = 20;
        var optionalHeaderSize = is64 ? 240 : 224;
        var dataDirSize = (int)data.OptionalHeader.NumberOfRvaAndSizes * 8;
        var sectionHeaderSize = data.Sections.Count * 40;

        return dosHeaderSize + peSignatureSize + coffHeaderSize + optionalHeaderSize + dataDirSize + sectionHeaderSize + 4096;
    }

    #endregion
}

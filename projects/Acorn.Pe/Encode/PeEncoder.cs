using Acorn.Frame;
using Acorn.Pe.Data;

namespace Acorn.Pe.Encode;

/// <summary>
///     PE 文件编码器，将 PeFileData 编码为 PE 二进制格式。
///     PE 格式固定使用小端序。
///     支持节区内容编码和数据目录正确写入。
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

        WriteSectionContents(ref writer, data);

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
            if (i < opt.DataDirectories.Count)
            {
                writer.WriteU32LE(opt.DataDirectories[i].Rva);
                writer.WriteU32LE(opt.DataDirectories[i].Size);
            }
            else
            {
                writer.WriteU32LE(0);
                writer.WriteU32LE(0);
            }
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

    #region 节区内容

    private static void WriteSectionContents(ref ByteBufferWriter writer, PeFileData data)
    {
        for (var i = 0; i < data.Sections.Count; i++)
        {
            if (!data.SectionContents.TryGetValue(i, out var content)) continue;
            if (content.Length == 0) continue;

            var section = data.Sections[i];
            var targetOffset = (int)section.PointerToRawData;

            var currentPos = writer.Position;
            if (currentPos < targetOffset)
            {
                WritePadding(ref writer, targetOffset - currentPos);
            }

            writer.Write(content);

            var fileAlignment = data.OptionalHeader.FileAlignment;
            if (fileAlignment > 0)
            {
                var aligned = (writer.Position + (int)fileAlignment - 1) & ~((int)fileAlignment - 1);
                if (aligned > writer.Position)
                {
                    WritePadding(ref writer, aligned - writer.Position);
                }
            }
        }
    }

    #endregion

    #region 辅助方法

    private static void WritePadding(ref ByteBufferWriter writer, int count)
    {
        for (var i = 0; i < count; i++)
        {
            writer.WriteU8(0);
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

        var sectionContentSize = 0;
        foreach (var (index, content) in data.SectionContents)
        {
            sectionContentSize += content.Length;
            var fileAlignment = (int)data.OptionalHeader.FileAlignment;
            if (fileAlignment > 0)
            {
                sectionContentSize = (sectionContentSize + fileAlignment - 1) & ~(fileAlignment - 1);
            }
        }

        return dosHeaderSize + peSignatureSize + coffHeaderSize + optionalHeaderSize + dataDirSize + sectionHeaderSize + sectionContentSize + 4096;
    }

    #endregion
}

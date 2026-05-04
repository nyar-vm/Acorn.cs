using System.Text;
using Acorn.Frame;
using Acorn.Pe.Data;

namespace Acorn.Pe.Encode;

/// <summary>
///     PE 文件编码器，将 PeFileData 编码为 PE 二进制格式。
///     PE 格式固定使用小端序。
///     支持节区内容、导入表和重定位表编码。
/// </summary>
public sealed class PeEncoder
{
    /// <summary>
    ///     编码 PE 文件数据为字节数组。
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
        WriteImportTable(ref writer, data);
        WriteRelocationTable(ref writer, data);

        return writer.ToArray();
    }

    #region DOS 头

    private static void WriteDosHeader(ref ByteBufferWriter writer, PeHeaderData header)
    {
        writer.WriteU16LE(header.DosMagic);

        for (var i = 0; i < 29; i++)
        {
            writer.WriteU16LE(0);
        }

        writer.WriteU32LE(header.PeHeaderOffset);

        WriteToOffset(ref writer, (int)header.PeHeaderOffset);
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
            if (!data.SectionContents.TryGetValue(i, out var content))
            {
                continue;
            }

            if (content.Length == 0)
            {
                continue;
            }

            var section = data.Sections[i];
            var targetOffset = (int)section.PointerToRawData;

            WriteToOffset(ref writer, targetOffset);
            writer.Write(content);
        }
    }

    #endregion

    #region 导入表

    private static void WriteImportTable(ref ByteBufferWriter writer, PeFileData data)
    {
        if (data.Imports.Count == 0)
        {
            return;
        }

        var is64 = data.Is64Bit;
        var importDir = data.GetDataDirectory(PeDataDirectoryIndex.ImportTable);

        if (importDir.IsEmpty)
        {
            return;
        }

        var importFileOffset = data.RvaToOffset(importDir.Rva);
        WriteToOffset(ref writer, importFileOffset);

        var thunkSize = is64 ? PeConstants.ImportThunkSize64 : PeConstants.ImportThunkSize32;
        var ordinalFlag = is64 ? PeConstants.ImportOrdinalFlag64 : PeConstants.ImportOrdinalFlag32;

        var descriptorStartRva = importDir.Rva;
        var descriptorEndRva = descriptorStartRva + (uint)((data.Imports.Count + 1) * PeConstants.ImportDescriptorSize);
        uint currentRva = descriptorEndRva;

        var dllLayouts = new List<(uint iltRva, uint iatRva, uint nameRva, List<uint> hintNameRvas)>();

        foreach (var dll in data.Imports)
        {
            var iltRva = currentRva;
            var iltSize = (uint)((dll.Thunks.Count + 1) * thunkSize);
            currentRva += iltSize;

            var iatRva = currentRva;
            currentRva += iltSize;

            var nameRva = currentRva;
            currentRva += (uint)(Encoding.ASCII.GetByteCount(dll.Name) + 1);

            var hintNameRvas = new List<uint>();

            foreach (var thunk in dll.Thunks)
            {
                if (!thunk.IsOrdinal)
                {
                    hintNameRvas.Add(currentRva);
                    currentRva += (uint)(2 + Encoding.ASCII.GetByteCount(thunk.Name) + 1);
                }
            }

            dllLayouts.Add((iltRva, iatRva, nameRva, hintNameRvas));
        }

        for (var d = 0; d < data.Imports.Count; d++)
        {
            var layout = dllLayouts[d];
            writer.WriteU32LE(layout.iltRva);
            writer.WriteU32LE(0);
            writer.WriteU32LE(0);
            writer.WriteU32LE(layout.nameRva);
            writer.WriteU32LE(layout.iatRva);
        }

        for (var i = 0; i < PeConstants.ImportDescriptorSize; i++)
        {
            writer.WriteU8(0);
        }

        for (var d = 0; d < data.Imports.Count; d++)
        {
            var dll = data.Imports[d];
            var layout = dllLayouts[d];
            var hintIdx = 0;

            WriteThunkArray(dll.Thunks, layout.hintNameRvas, ref hintIdx, is64, ordinalFlag, ref writer);

            hintIdx = 0;

            WriteThunkArray(dll.Thunks, layout.hintNameRvas, ref hintIdx, is64, ordinalFlag, ref writer);

            hintIdx = 0;

            writer.Write(Encoding.ASCII.GetBytes(dll.Name));
            writer.WriteU8(0);

            foreach (var thunk in dll.Thunks)
            {
                if (!thunk.IsOrdinal)
                {
                    writer.WriteU16LE(0);
                    writer.Write(Encoding.ASCII.GetBytes(thunk.Name));
                    writer.WriteU8(0);
                }
            }
        }
    }

    private static void WriteThunkArray(IReadOnlyList<PeImportThunk> thunks, List<uint> hintNameRvas, ref int hintIdx, bool is64, ulong ordinalFlag, ref ByteBufferWriter writer)
    {
        foreach (var thunk in thunks)
        {
            if (thunk.IsOrdinal)
            {
                if (is64)
                {
                    writer.WriteU64LE(ordinalFlag | thunk.Ordinal);
                }
                else
                {
                    writer.WriteU32LE((uint)(ordinalFlag | thunk.Ordinal));
                }
            }
            else
            {
                var hintNameRva = hintNameRvas[hintIdx++];

                if (is64)
                {
                    writer.WriteU64LE(hintNameRva);
                }
                else
                {
                    writer.WriteU32LE(hintNameRva);
                }
            }
        }

        if (is64)
        {
            writer.WriteU64LE(0);
        }
        else
        {
            writer.WriteU32LE(0);
        }
    }

    #endregion

    #region 重定位表

    private static void WriteRelocationTable(ref ByteBufferWriter writer, PeFileData data)
    {
        if (data.Relocations.Count == 0)
        {
            return;
        }

        var relocDir = data.GetDataDirectory(PeDataDirectoryIndex.BaseRelocationTable);

        if (relocDir.IsEmpty)
        {
            return;
        }

        var relocFileOffset = data.RvaToOffset(relocDir.Rva);
        WriteToOffset(ref writer, relocFileOffset);

        foreach (var block in data.Relocations)
        {
            var blockSize = (uint)(PeConstants.RelocationBlockHeaderSize + block.Entries.Count * PeConstants.RelocationEntrySize);

            writer.WriteU32LE(block.VirtualAddress);
            writer.WriteU32LE(blockSize);

            foreach (var entry in block.Entries)
            {
                var encoded = (ushort)(((uint)entry.Type << 12) | (entry.Offset & 0xFFF));
                writer.WriteU16LE(encoded);
            }
        }
    }

    #endregion

    #region 辅助方法

    private static void WriteToOffset(ref ByteBufferWriter writer, int targetOffset)
    {
        while (writer.Position < targetOffset)
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

        foreach (var content in data.SectionContents.Values)
        {
            sectionContentSize += content.Length;
        }

        var importSize = EstimateImportSize(data);
        var relocationSize = EstimateRelocationSize(data);

        return dosHeaderSize + peSignatureSize + coffHeaderSize + optionalHeaderSize + dataDirSize + sectionHeaderSize + sectionContentSize + importSize + relocationSize + 4096;
    }

    private static int EstimateImportSize(PeFileData data)
    {
        if (data.Imports.Count == 0)
        {
            return 0;
        }

        var is64 = data.Is64Bit;
        var thunkSize = is64 ? PeConstants.ImportThunkSize64 : PeConstants.ImportThunkSize32;
        var total = 0;

        foreach (var dll in data.Imports)
        {
            total += (dll.Thunks.Count + 1) * thunkSize * 2;
            total += dll.Name.Length + 1;

            foreach (var thunk in dll.Thunks)
            {
                if (!thunk.IsOrdinal)
                {
                    total += 2 + thunk.Name.Length + 1;
                }
            }
        }

        total += (data.Imports.Count + 1) * PeConstants.ImportDescriptorSize;
        total += 256;

        return total;
    }

    private static int EstimateRelocationSize(PeFileData data)
    {
        if (data.Relocations.Count == 0)
        {
            return 0;
        }

        var total = 0;

        foreach (var block in data.Relocations)
        {
            var blockSize = PeConstants.RelocationBlockHeaderSize + block.Entries.Count * PeConstants.RelocationEntrySize;
            blockSize = (blockSize + 3) & ~3;
            total += blockSize;
        }

        total += 256;

        return total;
    }

    #endregion
}

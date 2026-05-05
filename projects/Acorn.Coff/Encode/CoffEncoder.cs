using System.Buffers.Binary;
using System.Text;
using Acorn.Coff.Data;

namespace Acorn.Coff.Encode;

/// <summary>
///     COFF 文件编码器，将 <see cref="CoffFileData" /> 编码为 COFF 二进制格式。
/// </summary>
/// <remarks>
///     COFF 布局：Header(20) + SectionHeaders(40×N) + Relocations + SymbolTable + StringTable。
///     当前版本不编码原始节区数据（模型中不含此字段）。
/// </remarks>
public sealed class CoffEncoder
{
    private const int SymbolEntrySize = 18;
    private const int SectionHeaderSize = 40;
    private const int RelocationEntrySize = 10;
    private const int CoffHeaderSize = 20;

    /// <summary>
    ///     将 COFF 文件数据编码为二进制字节数组。
    /// </summary>
    /// <param name="data">COFF 文件数据。</param>
    /// <returns>COFF 二进制数据。</returns>
    public byte[] Encode(CoffFileData data)
    {
        var sections = data.Sections;
        var symbols = data.Symbols;
        var relocations = data.Relocations;

        var sectionCount = sections.Count;
        var symbolCount = symbols.Count;
        var relocCount = relocations.Count;

        var totalAuxSymbols = 0;

        foreach (var sym in symbols)
        {
            totalAuxSymbols += sym.NumberOfAuxSymbols;
        }

        // 计算字符串表大小
        var stringTableSize = 4; // 4 字节长度头

        foreach (var sym in symbols)
        {
            var nameLen = Encoding.UTF8.GetByteCount(sym.Name);

            if (nameLen > 8)
            {
                stringTableSize += nameLen + 1; // 含 null 结尾
            }
        }

        // 计算布局
        var relocOffset = CoffHeaderSize + sectionCount * SectionHeaderSize;
        var symbolTableOffset = relocOffset + relocCount * RelocationEntrySize;
        var stringTableOffset = symbolTableOffset + (symbolCount + totalAuxSymbols) * SymbolEntrySize;
        var totalSize = stringTableOffset + stringTableSize;

        var buffer = new byte[totalSize];
        var pos = 0;

        // 1. 写入 Header
        WriteHeader(ref buffer, pos, data.Header, (uint)symbolCount, (uint)symbolTableOffset);
        pos += CoffHeaderSize;

        // 2. 写入 Section Headers
        for (var i = 0; i < sectionCount; i++)
        {
            var section = sections[i];
            var sectionRelocCount = i == 0 ? (ushort)relocCount : (ushort)0;

            WriteSectionHeader(ref buffer, pos, section, (uint)relocOffset, sectionRelocCount);
            pos += SectionHeaderSize;
        }

        // 3. 写入 Relocations
        foreach (var reloc in relocations)
        {
            WriteRelocation(ref buffer, relocOffset, reloc);
            relocOffset += RelocationEntrySize;
        }

        // 4. 写入 Symbol Table
        var symPos = symbolTableOffset;
        var strTableCursor = 4; // 跳过 4 字节长度头

        foreach (var sym in symbols)
        {
            var nameBytes = Encoding.UTF8.GetBytes(sym.Name);

            if (nameBytes.Length <= 8)
            {
                WriteShortNameSymbol(ref buffer, symPos, nameBytes, sym);
            }
            else
            {
                WriteLongNameSymbol(ref buffer, symPos, sym, strTableCursor);
                strTableCursor += nameBytes.Length + 1;
            }

            symPos += SymbolEntrySize;

            for (var a = 0; a < sym.NumberOfAuxSymbols; a++)
            {
                Array.Fill(buffer, (byte)0, symPos, SymbolEntrySize);
                symPos += SymbolEntrySize;
            }
        }

        // 5. 写入 String Table
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(stringTableOffset), stringTableSize);
        pos = stringTableOffset + 4;

        foreach (var sym in symbols)
        {
            var nameBytes = Encoding.UTF8.GetBytes(sym.Name);

            if (nameBytes.Length > 8)
            {
                nameBytes.CopyTo(buffer.AsSpan(pos));
                pos += nameBytes.Length;
                buffer[pos++] = 0;
            }
        }

        return buffer;
    }

    #region 写入辅助方法

    private static void WriteHeader(ref byte[] buffer, int offset, CoffHeaderData header, uint symbolCount, uint symbolTableOffset)
    {
        var span = buffer.AsSpan(offset);
        BinaryPrimitives.WriteUInt16LittleEndian(span, header.Machine);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2), header.NumberOfSections);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4), header.TimeDateStamp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), symbolTableOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12), symbolCount);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(16), header.SizeOfOptionalHeader);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(18), header.Characteristics);
    }

    private static void WriteSectionHeader(ref byte[] buffer, int offset, CoffSectionHeaderData section,
        uint relocationOffset, ushort relocationCount)
    {
        var span = buffer.AsSpan(offset);
        section.NameBytes.AsSpan().CopyTo(span.Slice(0, 8));
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), section.PhysicalAddress);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12), section.VirtualAddress);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16), section.SizeOfRawData);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(20), section.PointerToRawData);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(24), relocationOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(28), section.PointerToLinenumbers);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(32), relocationCount);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(34), section.NumberOfLinenumbers);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(36), section.Characteristics);
    }

    private static void WriteRelocation(ref byte[] buffer, int offset, CoffRelocationData reloc)
    {
        var span = buffer.AsSpan(offset);
        BinaryPrimitives.WriteUInt32LittleEndian(span, reloc.VirtualAddress);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4), reloc.SymbolTableIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(8), reloc.Type);
    }

    private static void WriteShortNameSymbol(ref byte[] buffer, int offset, byte[] nameBytes, CoffSymbolData symbol)
    {
        var span = buffer.AsSpan(offset);
        nameBytes.CopyTo(span.Slice(0, Math.Min(nameBytes.Length, 8)));

        if (nameBytes.Length < 8)
        {
            span.Slice(nameBytes.Length, 8 - nameBytes.Length).Fill(0);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), symbol.Value);
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(12), symbol.SectionNumber);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(14), symbol.Type);
        span[16] = symbol.StorageClass;
        span[17] = symbol.NumberOfAuxSymbols;
    }

    private static void WriteLongNameSymbol(ref byte[] buffer, int offset, CoffSymbolData symbol, int strTableOffset)
    {
        var span = buffer.AsSpan(offset);
        span.Slice(0, 4).Fill(0);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4), strTableOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), symbol.Value);
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(12), symbol.SectionNumber);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(14), symbol.Type);
        span[16] = symbol.StorageClass;
        span[17] = symbol.NumberOfAuxSymbols;
    }

    #endregion
}

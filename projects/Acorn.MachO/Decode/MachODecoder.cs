using System.Buffers.Binary;
using Acorn.Frame;
using Acorn.MachO.Data;

namespace Acorn.MachO.Decode;

/// <summary>
///     Mach-O 文件解码器，解析 macOS/iOS 可执行文件（.macho, .dylib）格式。
/// </summary>
public sealed class MachODecoder
{
    /// <summary>
    ///     从 Mach-O 二进制数据解码文件。
    /// </summary>
    /// <param name="data">Mach-O 二进制数据。</param>
    /// <returns>解码后的 Mach-O 文件数据。</returns>
    public MachOFileData Decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return DecodeFile(ref buffer);
    }

    private MachOFileData DecodeFile(ref ByteBuffer buffer)
    {
        var header = ReadMachOHeader(ref buffer, out var isLittleEndian);
        var loadCommands = ReadLoadCommands(ref buffer, header, isLittleEndian);
        var sections = ReadSections(header, loadCommands, isLittleEndian);

        return new MachOFileData
        {
            Header = header,
            LoadCommands = loadCommands,
            Sections = sections
        };
    }

    /// <summary>
    ///     读取 Mach-O 头。
    /// </summary>
    private MachOHeaderData ReadMachOHeader(ref ByteBuffer buffer, out bool isLittleEndian)
    {
        var rawMagic = buffer.ReadU32LE();

        if (rawMagic == 0xFEEDFACE || rawMagic == 0xFEEDFACF)
        {
            isLittleEndian = true;
        }
        else if (rawMagic == 0xCEFAEDFE || rawMagic == 0xCFFAEDFE)
        {
            isLittleEndian = false;
        }
        else
        {
            throw new InvalidDataException("不是有效的 Mach-O 文件（魔数不匹配）");
        }

        var magic = isLittleEndian ? rawMagic : BinaryPrimitives.ReverseEndianness(rawMagic);

        var cpuType = ReadInt32(ref buffer, isLittleEndian);
        var cpuSubtype = ReadInt32(ref buffer, isLittleEndian);
        var fileType = ReadUInt32(ref buffer, isLittleEndian);
        var numberOfLoadCommands = ReadUInt32(ref buffer, isLittleEndian);
        var sizeOfLoadCommands = ReadUInt32(ref buffer, isLittleEndian);
        var flags = ReadUInt32(ref buffer, isLittleEndian);

        uint reserved = 0;
        if (magic == 0xFEEDFACF)
        {
            reserved = ReadUInt32(ref buffer, isLittleEndian);
        }

        return new MachOHeaderData
        {
            Magic = magic,
            CPUType = cpuType,
            CPUSubtype = cpuSubtype,
            FileType = fileType,
            NumberOfLoadCommands = numberOfLoadCommands,
            SizeOfLoadCommands = sizeOfLoadCommands,
            Flags = flags,
            Reserved = reserved,
            IsLittleEndian = isLittleEndian
        };
    }

    /// <summary>
    ///     读取加载命令。
    /// </summary>
    private List<MachOLoadCommandData> ReadLoadCommands(ref ByteBuffer buffer, MachOHeaderData header, bool isLittleEndian)
    {
        var commands = new List<MachOLoadCommandData>();

        for (var i = 0; i < header.NumberOfLoadCommands; i++)
        {
            var cmd = ReadUInt32(ref buffer, isLittleEndian);
            var cmdSize = ReadUInt32(ref buffer, isLittleEndian);

            var dataSize = (int)cmdSize - 8;
            var data = dataSize > 0 ? buffer.ReadBytes(dataSize).ToArray() : [];

            commands.Add(new MachOLoadCommandData
            {
                Command = cmd,
                Size = cmdSize,
                Data = data
            });
        }

        return commands;
    }

    /// <summary>
    ///     读取节区。
    /// </summary>
    private List<MachOSectionData> ReadSections(MachOHeaderData header, List<MachOLoadCommandData> loadCommands, bool isLittleEndian)
    {
        var sections = new List<MachOSectionData>();

        foreach (var cmd in loadCommands)
        {
            if (cmd.Command == 0x01 || cmd.Command == 0x19)
            {
                var sectionBuffer = new ByteBuffer(cmd.Data);

                sectionBuffer.Advance(16);

                if (header.Is64Bit)
                {
                    sectionBuffer.Advance(32);
                }
                else
                {
                    sectionBuffer.Advance(24);
                }

                var nsects = ReadUInt32(ref sectionBuffer, isLittleEndian);

                for (var i = 0; i < nsects; i++)
                {
                    var sectionName = ReadString(ref sectionBuffer, 16);
                    var segmentName = ReadString(ref sectionBuffer, 16);

                    ulong address;
                    ulong size;
                    uint offset;
                    uint alignment;
                    uint relocationsOffset;
                    uint numberOfRelocations;
                    uint flags;

                    if (header.Is64Bit)
                    {
                        address = ReadUInt64(ref sectionBuffer, isLittleEndian);
                        size = ReadUInt64(ref sectionBuffer, isLittleEndian);
                        offset = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        alignment = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        relocationsOffset = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        numberOfRelocations = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        flags = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        sectionBuffer.Advance(12);
                    }
                    else
                    {
                        address = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        size = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        offset = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        alignment = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        relocationsOffset = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        numberOfRelocations = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        flags = ReadUInt32(ref sectionBuffer, isLittleEndian);
                        sectionBuffer.Advance(8);
                    }

                    sections.Add(new MachOSectionData
                    {
                        SectionName = sectionName,
                        SegmentName = segmentName,
                        Address = address,
                        Size = size,
                        Offset = offset,
                        Alignment = alignment,
                        RelocationsOffset = relocationsOffset,
                        NumberOfRelocations = numberOfRelocations,
                        Flags = flags
                    });
                }
            }
        }

        return sections;
    }

    /// <summary>
    ///     读取固定长度字符串。
    /// </summary>
    private static string ReadString(ref ByteBuffer buffer, int length)
    {
        return buffer.ReadString(length).TrimEnd('\0');
    }

    /// <summary>
    ///     读取 Int32，根据字节序转换。
    /// </summary>
    private static int ReadInt32(ref ByteBuffer buffer, bool isLittleEndian)
    {
        return isLittleEndian ? buffer.ReadI32LE() : buffer.ReadI32BE();
    }

    /// <summary>
    ///     读取 UInt32，根据字节序转换。
    /// </summary>
    private static uint ReadUInt32(ref ByteBuffer buffer, bool isLittleEndian)
    {
        return isLittleEndian ? buffer.ReadU32LE() : buffer.ReadU32BE();
    }

    /// <summary>
    ///     读取 UInt64，根据字节序转换。
    /// </summary>
    private static ulong ReadUInt64(ref ByteBuffer buffer, bool isLittleEndian)
    {
        return isLittleEndian ? buffer.ReadU64LE() : buffer.ReadU64BE();
    }
}

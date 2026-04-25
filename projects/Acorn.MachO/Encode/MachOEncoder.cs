using Acorn.Frame;
using Acorn.MachO.Data;
using System.Text;

namespace Acorn.MachO.Encode;

/// <summary>
///     Mach-O 文件编码器，将 MachOFileData 编码为 Mach-O 二进制格式。
///     支持 32 位/64 位双模式和小端/大端双字节序。
/// </summary>
public sealed class MachOEncoder
{
    /// <summary>
    ///     编码 Mach-O 文件数据为字节数组。
    /// </summary>
    /// <param name="data">Mach-O 文件数据。</param>
    /// <returns>编码后的字节数组。</returns>
    public byte[] Encode(MachOFileData data)
    {
        var header = data.Header;
        var is64 = header.Is64Bit;
        var isLE = header.IsLittleEndian;

        var size = EstimateSize(data);
        var writer = new ByteBufferWriter(size);

        WriteMachOHeader(ref writer, header, is64, isLE);
        WriteLoadCommands(ref writer, data.LoadCommands, isLE);

        return writer.ToArray();
    }

    #region Mach-O 头

    private static void WriteMachOHeader(ref ByteBufferWriter writer, MachOHeaderData header, bool is64, bool isLE)
    {
        WriteU32(ref writer, header.Magic, isLE);
        WriteI32(ref writer, header.CPUType, isLE);
        WriteI32(ref writer, header.CPUSubtype, isLE);
        WriteU32(ref writer, header.FileType, isLE);
        WriteU32(ref writer, header.NumberOfLoadCommands, isLE);
        WriteU32(ref writer, header.SizeOfLoadCommands, isLE);
        WriteU32(ref writer, header.Flags, isLE);

        if (is64)
        {
            WriteU32(ref writer, header.Reserved, isLE);
        }
    }

    #endregion

    #region 加载命令

    private static void WriteLoadCommands(ref ByteBufferWriter writer, IReadOnlyList<MachOLoadCommandData> commands, bool isLE)
    {
        foreach (var cmd in commands)
        {
            WriteU32(ref writer, cmd.Command, isLE);
            WriteU32(ref writer, cmd.Size, isLE);
            writer.Write(cmd.Data);
        }
    }

    #endregion

    #region 字节序辅助

    private static void WriteU16(ref ByteBufferWriter writer, ushort value, bool isLE)
    {
        if (isLE) writer.WriteU16LE(value);
        else writer.WriteU16BE(value);
    }

    private static void WriteU32(ref ByteBufferWriter writer, uint value, bool isLE)
    {
        if (isLE) writer.WriteU32LE(value);
        else writer.WriteU32BE(value);
    }

    private static void WriteU64(ref ByteBufferWriter writer, ulong value, bool isLE)
    {
        if (isLE) writer.WriteU64LE(value);
        else writer.WriteU64BE(value);
    }

    private static void WriteI32(ref ByteBufferWriter writer, int value, bool isLE)
    {
        if (isLE) writer.WriteI32LE(value);
        else writer.WriteI32BE(value);
    }

    #endregion

    #region 大小预估

    private static int EstimateSize(MachOFileData data)
    {
        var headerSize = data.Header.Is64Bit ? 32 : 28;

        var loadCommandsSize = 0;
        foreach (var cmd in data.LoadCommands)
        {
            loadCommandsSize += (int)cmd.Size;
        }

        return headerSize + loadCommandsSize + 4096;
    }

    #endregion
}

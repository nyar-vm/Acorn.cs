using System.Text;
using Acorn;
using Acorn.Attributes;
using Acorn.Codec;

namespace Acorn.Dxil.Data;

/// <summary>
///     DXContainer 文件头数据。
/// </summary>
/// <remarks>
///     DXContainer 文件头由魔数、版本号、文件大小和 Part 数量组成。
///     魔数为 "DXBC"（小端序读取后为 0x43425844）。
/// </remarks>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct DxContainerHeader
{
    /// <summary>
    ///     魔数（"DXBC" 小端序 = 0x43425844）。
    /// </summary>
    [Field(Order = 0)]
    public uint MagicNumber;

    /// <summary>
    ///     容器格式主版本号。
    /// </summary>
    [Field(Order = 1)]
    public ushort VersionMajor;

    /// <summary>
    ///     容器格式次版本号。
    /// </summary>
    [Field(Order = 2)]
    public ushort VersionMinor;

    /// <summary>
    ///     文件总大小（字节）。
    /// </summary>
    [Field(Order = 3)]
    public uint FileSize;

    /// <summary>
    ///     Part 数量。
    /// </summary>
    [Field(Order = 4)]
    public uint PartCount;
}

/// <summary>
///     DXContainer Part 头数据。
/// </summary>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct DxContainerPartHeader
{
    /// <summary>
    ///     Part 类型标识（4 字符 ASCII FourCC）。
    /// </summary>
    [Field(Order = 0)]
    public uint FourCC;

    /// <summary>
    ///     Part 数据大小（字节，不含 Part 头）。
    /// </summary>
    [Field(Order = 1)]
    public uint Size;

    /// <summary>
    ///     获取 FourCC 的 ASCII 字符串表示。
    /// </summary>
    public readonly string FourCCString
    {
        get
        {
            var bytes = new byte[4];
            bytes[0] = (byte)(FourCC & 0xFF);
            bytes[1] = (byte)((FourCC >> 8) & 0xFF);
            bytes[2] = (byte)((FourCC >> 16) & 0xFF);
            bytes[3] = (byte)((FourCC >> 24) & 0xFF);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}

/// <summary>
///     DXContainer Part 数据，包含 Part 头和原始数据。
/// </summary>
public sealed class DxContainerPart
{
    /// <summary>
    ///     Part 头。
    /// </summary>
    public DxContainerPartHeader Header { get; init; }

    /// <summary>
    ///     Part 原始数据。
    /// </summary>
    public byte[] Data { get; init; } = [];
}

/// <summary>
///     DXContainer 完整数据。
/// </summary>
public sealed class DxContainerData
{
    /// <summary>
    ///     容器文件头。
    /// </summary>
    public DxContainerHeader Header { get; init; }

    /// <summary>
    ///     Part 列表。
    /// </summary>
    public IReadOnlyList<DxContainerPart> Parts { get; init; } = [];
}

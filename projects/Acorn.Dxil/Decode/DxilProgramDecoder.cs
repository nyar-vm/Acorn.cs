using Acorn.Dxil.Data;
using Acorn.Frame;

namespace Acorn.Dxil.Decode;

/// <summary>
///     DXIL 程序解码器，解析 DXIL Part 内的着色器程序数据。
/// </summary>
/// <remarks>
///     DXIL 程序由 ProgramHeader（24 字节）和 LLVM Bitcode 数据组成。
///     LLVM Bitcode 的详细解码由 Acorn.Llvm 提供。
/// </remarks>
public sealed class DxilProgramDecoder
{
    /// <summary>
    ///     从 DXIL Part 数据解码着色器程序。
    /// </summary>
    /// <param name="partData">DXIL Part 的原始数据。</param>
    /// <returns>解码后的 DXIL 程序数据。</returns>
    /// <exception cref="InvalidDataException">数据不是有效的 DXIL 程序格式。</exception>
    public DxilProgramData Decode(ReadOnlySpan<byte> partData)
    {
        var buffer = new ByteBuffer(partData);
        return DecodeProgram(ref buffer);
    }

    private DxilProgramData DecodeProgram(ref ByteBuffer buffer)
    {
        var header = ReadProgramHeader(ref buffer);

        if (buffer.Position + header.BitcodeSize > buffer.Length)
        {
            throw new InvalidDataException(
                $"DXIL Bitcode 数据不完整：期望 {header.BitcodeSize} 字节，剩余 {buffer.Length - buffer.Position} 字节");
        }

        buffer.Position = (int)header.BitcodeOffset;
        var bitcodeData = buffer.ReadBytes((int)header.BitcodeSize).ToArray();

        return new DxilProgramData
        {
            Header = header,
            BitcodeData = bitcodeData
        };
    }

    private static DxilProgramHeader ReadProgramHeader(ref ByteBuffer buffer)
    {
        if (buffer.Remaining < DxilConstants.ProgramHeaderSize)
        {
            throw new InvalidDataException(
                $"DXIL Part 数据过短：期望至少 {DxilConstants.ProgramHeaderSize} 字节，实际 {buffer.Remaining} 字节");
        }

        var majorVersion = buffer.ReadU8();
        var minorVersion = buffer.ReadU8();
        var shaderModelKindRaw = buffer.ReadU8();
        var padding = buffer.ReadU8();
        var size = buffer.ReadU32LE();
        var bitcodeOffset = buffer.ReadU32LE();
        var bitcodeSize = buffer.ReadU32LE();

        return new DxilProgramHeader
        {
            MajorVersion = majorVersion,
            MinorVersion = minorVersion,
            ShaderModelKindRaw = shaderModelKindRaw,
            Padding = padding,
            Size = size,
            BitcodeOffset = bitcodeOffset,
            BitcodeSize = bitcodeSize
        };
    }
}

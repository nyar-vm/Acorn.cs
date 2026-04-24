using Acorn.Dxil.Data;
using Acorn.Frame;

namespace Acorn.Dxil.Encode;

/// <summary>
///     DXIL 程序编码器，将着色器程序数据编码为 DXIL Part 二进制格式。
/// </summary>
/// <remarks>
///     DXIL 程序由 ProgramHeader（24 字节）和 LLVM Bitcode 数据组成。
///     LLVM Bitcode 的编码由 Acorn.Llvm 的编码器提供。
/// </remarks>
public sealed class DxilProgramEncoder
{
    /// <summary>
    ///     将 DXIL 程序数据编码为 DXIL Part 数据。
    /// </summary>
    /// <param name="program">DXIL 程序数据。</param>
    /// <returns>DXIL Part 数据（ProgramHeader + Bitcode）。</returns>
    public byte[] Encode(DxilProgramData program)
    {
        var bitcodeOffset = (uint)DxilConstants.ProgramHeaderSize;
        var totalSize = bitcodeOffset + (uint)program.BitcodeData.Length;

        var buffer = new byte[totalSize];
        var writer = new ByteBufferWriter(buffer);

        WriteProgramHeader(ref writer, program.Header, totalSize, bitcodeOffset,
            (uint)program.BitcodeData.Length);
        writer.Write(program.BitcodeData);

        return buffer[..writer.Position];
    }

    /// <summary>
    ///     从着色器模型和 LLVM Bitcode 创建并编码 DXIL 程序。
    /// </summary>
    /// <param name="shaderModel">着色器模型类型。</param>
    /// <param name="dxilMajorVersion">DXIL 主版本号。</param>
    /// <param name="dxilMinorVersion">DXIL 次版本号。</param>
    /// <param name="bitcodeData">LLVM Bitcode 数据。</param>
    /// <returns>DXIL Part 数据。</returns>
    public byte[] EncodeFromBitcode(DxilShaderModelKind shaderModel,
        byte dxilMajorVersion, byte dxilMinorVersion, byte[] bitcodeData)
    {
        var header = new DxilProgramHeader
        {
            MajorVersion = dxilMajorVersion,
            MinorVersion = dxilMinorVersion,
            ShaderModelKind = shaderModel
        };

        var program = new DxilProgramData
        {
            Header = header,
            BitcodeData = bitcodeData
        };

        return Encode(program);
    }

    private static void WriteProgramHeader(ref ByteBufferWriter writer,
        DxilProgramHeader header, uint totalSize, uint bitcodeOffset, uint bitcodeSize)
    {
        writer.WriteU8(header.MajorVersion);
        writer.WriteU8(header.MinorVersion);
        writer.WriteU8(header.ShaderModelKindRaw);
        writer.WriteU8(header.Padding);
        writer.WriteU32LE(totalSize);
        writer.WriteU32LE(bitcodeOffset);
        writer.WriteU32LE(bitcodeSize);
    }
}

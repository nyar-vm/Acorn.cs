using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Acorn.Codec;

#region 半精度浮点数编解码器

/// <summary>
///     小端序 16 位半精度浮点数（IEEE 754 binary16）编解码器。
/// </summary>
/// <remarks>
///     半精度浮点数由 1 位符号、5 位指数、10 位尾数组成，
///     广泛用于 GPU 着色器（SpirV/HLSL）、深度学习（SafeTensors/F16）、glTF 等格式。
/// </remarks>
public readonly struct F16LE : ICodec<Half>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(Half value) => 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(Half value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt16LittleEndian(destination, BitConverter.HalfToUInt16Bits(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half Decode(ReadOnlySpan<byte> source) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(source));
}

/// <summary>
///     大端序 16 位半精度浮点数（IEEE 754 binary16）编解码器。
/// </summary>
/// <remarks>
///     半精度浮点数由 1 位符号、5 位指数、10 位尾数组成，
///     广泛用于 GPU 着色器（SpirV/HLSL）、深度学习（SafeTensors/F16）、glTF 等格式。
/// </remarks>
public readonly struct F16BE : ICodec<Half>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(Half value) => 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(Half value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt16BigEndian(destination, BitConverter.HalfToUInt16Bits(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Half Decode(ReadOnlySpan<byte> source) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16BigEndian(source));
}

#endregion

#region 浮点数编解码器

/// <summary>
///     小端序 32 位浮点数编解码器。
/// </summary>
public readonly struct F32LE : ICodec<float>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(float value) => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(float value, Span<byte> destination) =>
        BinaryPrimitives.WriteSingleLittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadSingleLittleEndian(source);
}

/// <summary>
///     大端序 32 位浮点数编解码器。
/// </summary>
public readonly struct F32BE : ICodec<float>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(float value) => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(float value, Span<byte> destination) =>
        BinaryPrimitives.WriteSingleBigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadSingleBigEndian(source);
}

/// <summary>
///     小端序 64 位浮点数编解码器。
/// </summary>
public readonly struct F64LE : ICodec<double>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(double value) => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(double value, Span<byte> destination) =>
        BinaryPrimitives.WriteDoubleLittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadDoubleLittleEndian(source);
}

/// <summary>
///     大端序 64 位浮点数编解码器。
/// </summary>
public readonly struct F64BE : ICodec<double>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(double value) => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(double value, Span<byte> destination) =>
        BinaryPrimitives.WriteDoubleBigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadDoubleBigEndian(source);
}

#endregion

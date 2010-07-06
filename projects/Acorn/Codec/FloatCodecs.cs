using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Acorn.Codec;

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

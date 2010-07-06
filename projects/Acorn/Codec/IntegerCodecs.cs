using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Acorn.Codec;

#region 无符号整数编解码器

/// <summary>
///     8 位无符号整数编解码器。
/// </summary>
public readonly struct U8 : ICodec<byte>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(byte value) => 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(byte value, Span<byte> destination) => destination[0] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Decode(ReadOnlySpan<byte> source) => source[0];
}

/// <summary>
///     8 位有符号整数编解码器。
/// </summary>
public readonly struct I8 : ICodec<sbyte>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(sbyte value) => 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(sbyte value, Span<byte> destination) => destination[0] = (byte)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte Decode(ReadOnlySpan<byte> source) => (sbyte)source[0];
}

/// <summary>
///     小端序 16 位无符号整数编解码器。
/// </summary>
public readonly struct U16LE : ICodec<ushort>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(ushort value) => 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(ushort value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt16LittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadUInt16LittleEndian(source);
}

/// <summary>
///     大端序 16 位无符号整数编解码器。
/// </summary>
public readonly struct U16BE : ICodec<ushort>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(ushort value) => 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(ushort value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt16BigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadUInt16BigEndian(source);
}

/// <summary>
///     小端序 16 位有符号整数编解码器。
/// </summary>
public readonly struct I16LE : ICodec<short>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(short value) => 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(short value, Span<byte> destination) =>
        BinaryPrimitives.WriteInt16LittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt16LittleEndian(source);
}

/// <summary>
///     大端序 16 位有符号整数编解码器。
/// </summary>
public readonly struct I16BE : ICodec<short>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(short value) => 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(short value, Span<byte> destination) =>
        BinaryPrimitives.WriteInt16BigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt16BigEndian(source);
}

/// <summary>
///     小端序 32 位无符号整数编解码器。
/// </summary>
public readonly struct U32LE : ICodec<uint>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(uint value) => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(uint value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32LittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadUInt32LittleEndian(source);
}

/// <summary>
///     大端序 32 位无符号整数编解码器。
/// </summary>
public readonly struct U32BE : ICodec<uint>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(uint value) => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(uint value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadUInt32BigEndian(source);
}

/// <summary>
///     小端序 32 位有符号整数编解码器。
/// </summary>
public readonly struct I32LE : ICodec<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(int value) => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(int value, Span<byte> destination) =>
        BinaryPrimitives.WriteInt32LittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt32LittleEndian(source);
}

/// <summary>
///     大端序 32 位有符号整数编解码器。
/// </summary>
public readonly struct I32BE : ICodec<int>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(int value) => 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(int value, Span<byte> destination) =>
        BinaryPrimitives.WriteInt32BigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt32BigEndian(source);
}

/// <summary>
///     小端序 64 位无符号整数编解码器。
/// </summary>
public readonly struct U64LE : ICodec<ulong>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(ulong value) => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(ulong value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt64LittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadUInt64LittleEndian(source);
}

/// <summary>
///     大端序 64 位无符号整数编解码器。
/// </summary>
public readonly struct U64BE : ICodec<ulong>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(ulong value) => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(ulong value, Span<byte> destination) =>
        BinaryPrimitives.WriteUInt64BigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadUInt64BigEndian(source);
}

/// <summary>
///     小端序 64 位有符号整数编解码器。
/// </summary>
public readonly struct I64LE : ICodec<long>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(long value) => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(long value, Span<byte> destination) =>
        BinaryPrimitives.WriteInt64LittleEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt64LittleEndian(source);
}

/// <summary>
///     大端序 64 位有符号整数编解码器。
/// </summary>
public readonly struct I64BE : ICodec<long>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSize(long value) => 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(long value, Span<byte> destination) =>
        BinaryPrimitives.WriteInt64BigEndian(destination, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decode(ReadOnlySpan<byte> source) =>
        BinaryPrimitives.ReadInt64BigEndian(source);
}

#endregion

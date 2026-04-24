using System.Text;
using Acorn.Frame;
using Acorn.Fbx.Data;

namespace Acorn.Fbx.Encode;

/// <summary>
///     FBX 二进制文件编码器，将 C# 数据结构编码为 Autodesk FBX 格式。
/// </summary>
/// <remarks>
///     FBX 是 Autodesk 的 3D 模型/动画交换格式，游戏行业事实标准。
///     编码器生成符合 FBX 二进制规范的二进制数据。
/// </remarks>
public sealed class FbxEncoder
{
    /// <summary>
    ///     将 FBX 文件数据编码为 FBX 二进制格式。
    /// </summary>
    /// <param name="data">FBX 文件数据。</param>
    /// <returns>FBX 二进制数据。</returns>
    public byte[] Encode(FbxFileData data)
    {
        var size = EstimateSize(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteFileHeader(ref writer, data);

        foreach (var child in data.Root.Children)
        {
            WriteNode(ref writer, child, data.Version);
        }

        WriteNullRecord(ref writer);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteFileHeader(ref ByteBufferWriter writer, FbxFileData data)
    {
        writer.Write(FbxConstants.BinaryMagic);

        var paddingNeeded = FbxConstants.MagicLength - FbxConstants.BinaryMagic.Length;

        for (var i = 0; i < paddingNeeded; i++)
        {
            writer.WriteU8(0);
        }

        writer.WriteU32LE((uint)data.Version);
    }

    private static void WriteNode(ref ByteBufferWriter writer, FbxNode node, int version)
    {
        var use64Bit = version >= FbxVersions.V75;

        var nameBytes = Encoding.ASCII.GetBytes(node.Name);
        var propertyListData = BuildPropertyList(node.Properties);
        var propertyListLength = propertyListData.Length;

        var headerSize = use64Bit ? 8 + 8 + 8 + 1 : 4 + 4 + 4 + 1;
        var nodeSize = headerSize + nameBytes.Length + propertyListLength;

        foreach (var child in node.Children)
        {
            nodeSize += ComputeNodeSize(child, version);
        }

        nodeSize += 13;

        var endOffset = writer.Position + nodeSize;

        if (use64Bit)
        {
            writer.WriteU64LE((ulong)endOffset);
            writer.WriteU64LE((ulong)node.Properties.Count);
            writer.WriteU64LE((ulong)propertyListLength);
        }
        else
        {
            writer.WriteU32LE((uint)endOffset);
            writer.WriteU32LE((uint)node.Properties.Count);
            writer.WriteU32LE((uint)propertyListLength);
        }

        writer.WriteU8((byte)nameBytes.Length);
        writer.Write(nameBytes);
        writer.Write(propertyListData);

        foreach (var child in node.Children)
        {
            WriteNode(ref writer, child, version);
        }

        WriteNullRecord(ref writer);
    }

    private static void WriteNullRecord(ref ByteBufferWriter writer)
    {
        writer.Write(FbxConstants.NullRecord);
    }

    private static byte[] BuildPropertyList(IReadOnlyList<FbxProperty> properties)
    {
        var size = 0;

        foreach (var prop in properties)
        {
            size += EstimatePropertySize(prop);
        }

        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        foreach (var prop in properties)
        {
            WriteProperty(ref writer, prop);
        }

        return buffer[..writer.Position].ToArray();
    }

    private static void WriteProperty(ref ByteBufferWriter writer, FbxProperty property)
    {
        writer.WriteU8(property.TypeCode);

        switch (property.TypeCode)
        {
            case FbxConstants.PropertyType.Boolean:
                writer.WriteU8((byte)((bool)property.Value! ? 1 : 0));
                break;

            case FbxConstants.PropertyType.Int8:
                writer.WriteU8((byte)(short)property.Value!);
                break;

            case FbxConstants.PropertyType.Int16:
                writer.WriteI16LE((short)property.Value!);
                break;

            case FbxConstants.PropertyType.Int32:
                writer.WriteI32LE((int)property.Value!);
                break;

            case FbxConstants.PropertyType.Int64:
                writer.WriteI64LE((long)property.Value!);
                break;

            case FbxConstants.PropertyType.Float32:
                writer.WriteF32LE((float)property.Value!);
                break;

            case FbxConstants.PropertyType.Float64:
                writer.WriteF64LE((double)property.Value!);
                break;

            case FbxConstants.PropertyType.String:
                WriteStringProperty(ref writer, (string)property.Value!);
                break;

            case FbxConstants.PropertyType.RawBuffer:
                WriteRawProperty(ref writer, (byte[])property.Value!);
                break;

            default:
                WriteArrayProperty(ref writer, property);
                break;
        }
    }

    private static void WriteStringProperty(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        writer.WriteU32LE((uint)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteRawProperty(ref ByteBufferWriter writer, byte[] value)
    {
        writer.WriteU32LE((uint)value.Length);
        writer.Write(value);
    }

    private static void WriteArrayProperty(ref ByteBufferWriter writer, FbxProperty property)
    {
        var value = property.Value;

        switch (value)
        {
            case int[] intArray:
                writer.WriteU32LE((uint)intArray.Length);
                writer.WriteU32LE(0);
                writer.WriteU32LE((uint)(intArray.Length * 4));

                foreach (var v in intArray)
                {
                    writer.WriteI32LE(v);
                }

                break;

            case long[] longArray:
                writer.WriteU32LE((uint)longArray.Length);
                writer.WriteU32LE(0);
                writer.WriteU32LE((uint)(longArray.Length * 8));

                foreach (var v in longArray)
                {
                    writer.WriteI64LE(v);
                }

                break;

            case float[] floatArray:
                writer.WriteU32LE((uint)floatArray.Length);
                writer.WriteU32LE(0);
                writer.WriteU32LE((uint)(floatArray.Length * 4));

                foreach (var v in floatArray)
                {
                    writer.WriteF32LE(v);
                }

                break;

            case double[] doubleArray:
                writer.WriteU32LE((uint)doubleArray.Length);
                writer.WriteU32LE(0);
                writer.WriteU32LE((uint)(doubleArray.Length * 8));

                foreach (var v in doubleArray)
                {
                    writer.WriteF64LE(v);
                }

                break;

            case bool[] boolArray:
                writer.WriteU32LE((uint)boolArray.Length);
                writer.WriteU32LE(0);
                writer.WriteU32LE((uint)boolArray.Length);

                foreach (var v in boolArray)
                {
                    writer.WriteU8((byte)(v ? 1 : 0));
                }

                break;

            case byte[] rawData:
                writer.WriteU32LE(0);
                writer.WriteU32LE(0);
                writer.WriteU32LE((uint)rawData.Length);
                writer.Write(rawData);
                break;

            default:
                writer.WriteU32LE(0);
                writer.WriteU32LE(0);
                writer.WriteU32LE(0);
                break;
        }
    }

    private static int ComputeNodeSize(FbxNode node, int version)
    {
        var use64Bit = version >= FbxVersions.V75;
        var nameBytes = Encoding.ASCII.GetBytes(node.Name);
        var propertyListLength = EstimatePropertyListSize(node.Properties);

        var headerSize = use64Bit ? 8 + 8 + 8 + 1 : 4 + 4 + 4 + 1;
        var size = headerSize + nameBytes.Length + propertyListLength;

        foreach (var child in node.Children)
        {
            size += ComputeNodeSize(child, version);
        }

        size += 13;

        return size;
    }

    private static int EstimatePropertyListSize(IReadOnlyList<FbxProperty> properties)
    {
        var size = 0;

        foreach (var prop in properties)
        {
            size += EstimatePropertySize(prop);
        }

        return size;
    }

    private static int EstimatePropertySize(FbxProperty property)
    {
        return property.TypeCode switch
        {
            FbxConstants.PropertyType.Boolean => 2,
            FbxConstants.PropertyType.Int8 => 2,
            FbxConstants.PropertyType.Int16 => 3,
            FbxConstants.PropertyType.Int32 => 5,
            FbxConstants.PropertyType.Int64 => 9,
            FbxConstants.PropertyType.Float32 => 5,
            FbxConstants.PropertyType.Float64 => 9,
            FbxConstants.PropertyType.String => 5 + ((string?)property.Value ?? "").Length,
            FbxConstants.PropertyType.RawBuffer => 5 + ((byte[]?)property.Value ?? []).Length,
            _ => EstimateArrayPropertySize(property)
        };
    }

    private static int EstimateArrayPropertySize(FbxProperty property)
    {
        var baseSize = 1 + 4 + 4 + 4;

        return property.Value switch
        {
            int[] arr => baseSize + arr.Length * 4,
            long[] arr => baseSize + arr.Length * 8,
            float[] arr => baseSize + arr.Length * 4,
            double[] arr => baseSize + arr.Length * 8,
            bool[] arr => baseSize + arr.Length,
            byte[] arr => baseSize + arr.Length,
            _ => baseSize
        };
    }

    private static int EstimateSize(FbxFileData data)
    {
        var size = FbxConstants.HeaderSize;

        foreach (var child in data.Root.Children)
        {
            size += ComputeNodeSize(child, data.Version);
        }

        size += 13;

        return size + 4096;
    }

    #endregion
}

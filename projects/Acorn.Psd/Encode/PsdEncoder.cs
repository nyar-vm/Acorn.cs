using System.Text;
using Acorn.Frame;
using Acorn.Psd.Data;

namespace Acorn.Psd.Encode;

/// <summary>
///     PSD 文件编码器，将 C# 数据结构编码为 Adobe Photoshop 文档格式。
/// </summary>
/// <remarks>
///     PSD 是 Adobe Photoshop 的原生文件格式，支持图层、通道、蒙版等高级图像编辑功能。
///     编码器生成符合 Adobe PSD 规范的二进制数据。
/// </remarks>
public sealed class PsdEncoder
{
    /// <summary>
    ///     将 PSD 图像数据编码为 PSD 二进制格式。
    /// </summary>
    /// <param name="data">PSD 图像数据。</param>
    /// <returns>PSD 二进制数据。</returns>
    public byte[] Encode(PsdImageData data)
    {
        var size = EstimateSize(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteFileHeader(ref writer, data);
        WriteColorModeData(ref writer);
        WriteImageResources(ref writer);
        WriteLayerAndMaskInfo(ref writer, data.Layers);
        WriteImageData(ref writer, data);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteFileHeader(ref ByteBufferWriter writer, PsdImageData data)
    {
        writer.WriteString("8BPS");
        writer.WriteU16BE(1);
        writer.Write(new byte[6]);
        writer.WriteU16BE((ushort)data.Channels);
        writer.WriteU32BE((uint)data.Height);
        writer.WriteU32BE((uint)data.Width);
        writer.WriteU16BE((ushort)data.Depth);
        writer.WriteU16BE((ushort)data.ColorMode);
    }

    private static void WriteColorModeData(ref ByteBufferWriter writer)
    {
        writer.WriteU32BE(0);
    }

    private static void WriteImageResources(ref ByteBufferWriter writer)
    {
        writer.WriteU32BE(0);
    }

    private static void WriteLayerAndMaskInfo(ref ByteBufferWriter writer, IReadOnlyList<PsdLayer> layers)
    {
        if (layers.Count == 0)
        {
            writer.WriteU32BE(0);
            return;
        }

        var layerInfoData = EncodeLayerInfo(layers);

        writer.WriteU32BE((uint)(4 + layerInfoData.Length));
        writer.WriteU32BE((uint)layerInfoData.Length);
        writer.Write(layerInfoData);
    }

    private static byte[] EncodeLayerInfo(IReadOnlyList<PsdLayer> layers)
    {
        var size = EstimateLayerInfoSize(layers);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        writer.WriteI16BE((short)layers.Count);

        foreach (var layer in layers)
        {
            WriteLayer(ref writer, layer);
        }

        return buffer[..writer.Position];
    }

    private static void WriteLayer(ref ByteBufferWriter writer, PsdLayer layer)
    {
        writer.WriteI32BE(layer.Bounds.Top);
        writer.WriteI32BE(layer.Bounds.Left);
        writer.WriteI32BE(layer.Bounds.Bottom);
        writer.WriteI32BE(layer.Bounds.Right);
        writer.WriteU16BE((ushort)layer.ChannelCount);

        for (var i = 0; i < layer.ChannelCount; i++)
        {
            writer.WriteI16BE((short)i);
            writer.WriteU32BE(0);
        }

        writer.WriteString("8BIM");
        writer.WriteString(layer.BlendMode);
        writer.WriteU8(layer.Opacity);
        writer.WriteU8(0);
        writer.WriteU8((byte)(layer.IsVisible ? 0 : 2));
        writer.WriteU8(0);

        var nameBytes = Encoding.ASCII.GetBytes(layer.Name);
        var extraDataLength = 4 + 1 + nameBytes.Length + ((nameBytes.Length + 1) % 2 != 0 ? 1 : 0);
        writer.WriteU32BE((uint)extraDataLength);

        writer.WriteU32BE(0);
        writer.WriteU8((byte)nameBytes.Length);
        writer.Write(nameBytes);

        if ((nameBytes.Length + 1) % 2 != 0)
        {
            writer.WriteU8(0);
        }
    }

    private static void WriteImageData(ref ByteBufferWriter writer, PsdImageData data)
    {
        writer.WriteU16BE(0);

        if (data.MergedImageData != null)
        {
            writer.Write(data.MergedImageData);
        }
    }

    private static int EstimateSize(PsdImageData data)
    {
        return 26 + 8 + 8 + EstimateLayerAndMaskInfoSize(data.Layers) + (data.MergedImageData?.Length ?? 0) + 2 + 1024;
    }

    private static int EstimateLayerAndMaskInfoSize(IReadOnlyList<PsdLayer> layers)
    {
        if (layers.Count == 0)
        {
            return 4;
        }

        return 8 + EstimateLayerInfoSize(layers);
    }

    private static int EstimateLayerInfoSize(IReadOnlyList<PsdLayer> layers)
    {
        var size = 2;

        foreach (var layer in layers)
        {
            size += 18 + layer.ChannelCount * 6 + 8 + 4 + 1 + layer.Name.Length + 2;
        }

        return size;
    }

    #endregion
}

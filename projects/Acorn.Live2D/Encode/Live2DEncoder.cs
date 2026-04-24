using System.Text;
using Acorn.Frame;
using Acorn.Live2D.Data;

namespace Acorn.Live2D.Encode;

/// <summary>
///     Live2D moc3 文件编码器，将 C# 数据结构编码为 Live2D Cubism moc3 二进制格式。
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组。
///     编码器按以下顺序写入数据：
///     1. 文件头（8 字节：魔数 + 版本 + 标志 + 修订号）
///     2. 段偏移表（i32 数组，每个条目指向对应段在文件中的偏移量）
///     3. 各数据段（CanvasInfo、CountInfo、ParameterIds、...）
/// </remarks>
public sealed class Live2DEncoder
{
    /// <summary>
    ///     将 moc3 模型数据编码为 moc3 二进制格式。
    /// </summary>
    /// <param name="data">moc3 模型数据。</param>
    /// <returns>moc3 二进制数据。</returns>
    public byte[] EncodeMoc3(Live2DModelData data)
    {
        var size = EstimateMoc3Size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        var offsetCount = Live2DConstants.GetOffsetTableCount(data.Version);
        var offsets = new int[offsetCount];

        WriteMoc3Header(ref writer, data);
        WriteOffsetTablePlaceholder(ref writer, offsetCount);
        WriteDataSections(ref writer, data, offsets);
        PatchOffsetTable(ref writer, offsets, data.IsBigEndian);

        return writer.WrittenData.ToArray();
    }

    #region 头部编码

    private static void WriteMoc3Header(ref ByteBufferWriter writer, Live2DModelData data)
    {
        writer.WriteString("MOC3");
        writer.WriteU8((byte)data.Version);
        writer.WriteU8((byte)(data.IsBigEndian ? 1 : 0));
        writer.WriteI16LE((short)data.Revision);
    }

    #endregion

    #region 段偏移表

    private static void WriteOffsetTablePlaceholder(ref ByteBufferWriter writer, int offsetCount)
    {
        for (var i = 0; i < offsetCount; i++)
        {
            writer.WriteI32LE(0);
        }
    }

    private static void PatchOffsetTable(ref ByteBufferWriter writer, int[] offsets, bool isBigEndian)
    {
        var offsetTablePosition = Live2DConstants.HeaderSize;

        for (var i = 0; i < offsets.Length; i++)
        {
            var position = offsetTablePosition + i * Live2DConstants.OffsetTableEntrySize;

            if (isBigEndian)
            {
                WriteI32At(ref writer, position, offsets[i], true);
            }
            else
            {
                WriteI32At(ref writer, position, offsets[i], false);
            }
        }
    }

    private static void WriteI32At(ref ByteBufferWriter writer, int position, int value, bool bigEndian)
    {
        var span = writer.GetSpan();
        if (bigEndian)
        {
            span[position] = (byte)(value >> 24);
            span[position + 1] = (byte)(value >> 16);
            span[position + 2] = (byte)(value >> 8);
            span[position + 3] = (byte)value;
        }
        else
        {
            span[position] = (byte)value;
            span[position + 1] = (byte)(value >> 8);
            span[position + 2] = (byte)(value >> 16);
            span[position + 3] = (byte)(value >> 24);
        }
    }

    #endregion

    #region 数据段编码

    private static void WriteDataSections(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        WriteCanvasInfoSection(ref writer, data, offsets);
        WriteCountInfoSection(ref writer, data, offsets);
        WriteParameterIdsSection(ref writer, data, offsets);
        WriteParameterMinimumValuesSection(ref writer, data, offsets);
        WriteParameterMaximumValuesSection(ref writer, data, offsets);
        WriteParameterDefaultValuesSection(ref writer, data, offsets);
        WritePartIdsSection(ref writer, data, offsets);
        WritePartParentPartIndicesSection(ref writer, data, offsets);
        WriteDrawableIdsSection(ref writer, data, offsets);
        WriteDrawableConstantFlagsSection(ref writer, data, offsets);
        WriteDrawableTextureIndicesSection(ref writer, data, offsets);
        WriteDrawableDrawOrdersSection(ref writer, data, offsets);
        WriteDrawableRenderOrdersSection(ref writer, data, offsets);
        WriteDrawableMaskCountsSection(ref writer, data, offsets);
        WriteDrawableMasksSection(ref writer, data, offsets);
        WriteDrawableVertexCountsSection(ref writer, data, offsets);
        WriteDrawableVertexPositionsSection(ref writer, data, offsets);
        WriteDrawableVertexUvsSection(ref writer, data, offsets);
        WriteDrawableIndicesSection(ref writer, data, offsets);

        if (data.Version >= 3)
        {
            WriteDrawableRepeatFlagsSection(ref writer, data, offsets);
        }

        if (data.Version >= 4)
        {
            WriteDeformerIdsSection(ref writer, data, offsets);
            WriteDeformerTypesSection(ref writer, data, offsets);
            WriteDeformerParentIndicesSection(ref writer, data, offsets);
            WriteDeformerBoundingBoxXSection(ref writer, data, offsets);
        }

        if (data.Version >= 5)
        {
            WriteDeformerBoundingBoxYSection(ref writer, data, offsets);
            WriteDeformerBoundingBoxWidthSection(ref writer, data, offsets);
            WriteDeformerBoundingBoxHeightSection(ref writer, data, offsets);
            WriteDeformerRotationSection(ref writer, data, offsets);
        }
    }

    private static void WriteCanvasInfoSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.CanvasInfo, offsets);
        WriteF32(ref writer, data.Canvas.Width, data.IsBigEndian);
        WriteF32(ref writer, data.Canvas.Height, data.IsBigEndian);
        WriteF32(ref writer, data.Canvas.CenterX, data.IsBigEndian);
        WriteF32(ref writer, data.Canvas.CenterY, data.IsBigEndian);
        WriteF32(ref writer, data.Canvas.PixelsPerUnit, data.IsBigEndian);
    }

    private static void WriteCountInfoSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.CountInfo, offsets);
        WriteI32(ref writer, data.Parameters.Count, data.IsBigEndian);
        WriteI32(ref writer, data.Parts.Count, data.IsBigEndian);
        WriteI32(ref writer, data.Drawables.Count, data.IsBigEndian);
        WriteI32(ref writer, data.Deformers.Count, data.IsBigEndian);
        WriteI32(ref writer, data.TextureCount, data.IsBigEndian);
    }

    private static void WriteParameterIdsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.ParameterIds, offsets);
        foreach (var parameter in data.Parameters)
        {
            writer.WriteNullTerminatedString(parameter.Id);
        }
    }

    private static void WriteParameterMinimumValuesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.ParameterMinimumValues, offsets);
        foreach (var parameter in data.Parameters)
        {
            WriteF32(ref writer, parameter.MinValue, data.IsBigEndian);
        }
    }

    private static void WriteParameterMaximumValuesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.ParameterMaximumValues, offsets);
        foreach (var parameter in data.Parameters)
        {
            WriteF32(ref writer, parameter.MaxValue, data.IsBigEndian);
        }
    }

    private static void WriteParameterDefaultValuesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.ParameterDefaultValues, offsets);
        foreach (var parameter in data.Parameters)
        {
            WriteF32(ref writer, parameter.DefaultValue, data.IsBigEndian);
        }
    }

    private static void WritePartIdsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.PartIds, offsets);
        foreach (var part in data.Parts)
        {
            writer.WriteNullTerminatedString(part.Id);
        }
    }

    private static void WritePartParentPartIndicesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.PartParentPartIndices, offsets);
        foreach (var part in data.Parts)
        {
            WriteI32(ref writer, part.ParentIndex, data.IsBigEndian);
        }
    }

    private static void WriteDrawableIdsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableIds, offsets);
        foreach (var drawable in data.Drawables)
        {
            writer.WriteNullTerminatedString(drawable.Id);
        }
    }

    private static void WriteDrawableConstantFlagsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableConstantFlags, offsets);
        foreach (var drawable in data.Drawables)
        {
            var flags = ComputeDrawableConstantFlags(drawable);
            writer.WriteU8(flags);
        }
    }

    private static void WriteDrawableTextureIndicesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableTextureIndices, offsets);
        foreach (var drawable in data.Drawables)
        {
            WriteI32(ref writer, drawable.TextureIndex, data.IsBigEndian);
        }
    }

    private static void WriteDrawableDrawOrdersSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableDrawOrders, offsets);
        foreach (var drawable in data.Drawables)
        {
            WriteI32(ref writer, drawable.DrawOrder, data.IsBigEndian);
        }
    }

    private static void WriteDrawableRenderOrdersSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableRenderOrders, offsets);
        foreach (var drawable in data.Drawables)
        {
            WriteI32(ref writer, drawable.RenderOrder, data.IsBigEndian);
        }
    }

    private static void WriteDrawableMaskCountsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableMaskCounts, offsets);
        foreach (var drawable in data.Drawables)
        {
            WriteI32(ref writer, drawable.MaskDrawableIndices.Count, data.IsBigEndian);
        }
    }

    private static void WriteDrawableMasksSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableMasks, offsets);
        foreach (var drawable in data.Drawables)
        {
            foreach (var maskIndex in drawable.MaskDrawableIndices)
            {
                WriteI32(ref writer, maskIndex, data.IsBigEndian);
            }
        }
    }

    private static void WriteDrawableVertexCountsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableVertexCounts, offsets);
        foreach (var drawable in data.Drawables)
        {
            WriteI32(ref writer, drawable.VertexCount, data.IsBigEndian);
        }
    }

    private static void WriteDrawableVertexPositionsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableVertexPositions, offsets);
        foreach (var drawable in data.Drawables)
        {
            for (var i = 0; i < drawable.VertexPositions.Count; i++)
            {
                WriteF32(ref writer, drawable.VertexPositions[i], data.IsBigEndian);
            }
        }
    }

    private static void WriteDrawableVertexUvsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableVertexUvs, offsets);
        foreach (var drawable in data.Drawables)
        {
            for (var i = 0; i < drawable.VertexUvs.Count; i++)
            {
                WriteF32(ref writer, drawable.VertexUvs[i], data.IsBigEndian);
            }
        }
    }

    private static void WriteDrawableIndicesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableIndices, offsets);
        foreach (var drawable in data.Drawables)
        {
            foreach (var index in drawable.Indices)
            {
                WriteI32(ref writer, index, data.IsBigEndian);
            }
        }
    }

    private static void WriteDrawableRepeatFlagsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DrawableRepeatFlags, offsets);
        foreach (var _ in data.Drawables)
        {
            writer.WriteU8(0);
        }
    }

    private static void WriteDeformerIdsSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerIds, offsets);
        foreach (var deformer in data.Deformers)
        {
            writer.WriteNullTerminatedString(deformer.Id);
        }
    }

    private static void WriteDeformerTypesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerTypes, offsets);
        foreach (var deformer in data.Deformers)
        {
            writer.WriteU8((byte)deformer.Type);
        }
    }

    private static void WriteDeformerParentIndicesSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerParentIndices, offsets);
        foreach (var deformer in data.Deformers)
        {
            WriteI32(ref writer, deformer.ParentIndex, data.IsBigEndian);
        }
    }

    private static void WriteDeformerBoundingBoxXSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerBoundingBoxX, offsets);
        foreach (var deformer in data.Deformers)
        {
            WriteF32(ref writer, deformer.X, data.IsBigEndian);
        }
    }

    private static void WriteDeformerBoundingBoxYSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerBoundingBoxY, offsets);
        foreach (var deformer in data.Deformers)
        {
            WriteF32(ref writer, deformer.Y, data.IsBigEndian);
        }
    }

    private static void WriteDeformerBoundingBoxWidthSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerBoundingBoxWidth, offsets);
        foreach (var deformer in data.Deformers)
        {
            WriteF32(ref writer, deformer.Width, data.IsBigEndian);
        }
    }

    private static void WriteDeformerBoundingBoxHeightSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerBoundingBoxHeight, offsets);
        foreach (var deformer in data.Deformers)
        {
            WriteF32(ref writer, deformer.Height, data.IsBigEndian);
        }
    }

    private static void WriteDeformerRotationSection(ref ByteBufferWriter writer, Live2DModelData data, int[] offsets)
    {
        RecordOffset(ref writer, (int)Moc3Section.DeformerRotation, offsets);
        foreach (var _ in data.Deformers)
        {
            WriteF32(ref writer, 0.0f, data.IsBigEndian);
        }
    }

    #endregion

    #region 辅助方法

    private static void RecordOffset(ref ByteBufferWriter writer, int sectionIndex, int[] offsets)
    {
        if (sectionIndex < offsets.Length)
        {
            offsets[sectionIndex] = writer.Position;
        }
    }

    private static void WriteI32(ref ByteBufferWriter writer, int value, bool isBigEndian)
    {
        if (isBigEndian)
        {
            writer.WriteI32BE(value);
        }
        else
        {
            writer.WriteI32LE(value);
        }
    }

    private static void WriteF32(ref ByteBufferWriter writer, float value, bool isBigEndian)
    {
        if (isBigEndian)
        {
            writer.WriteF32BE(value);
        }
        else
        {
            writer.WriteF32LE(value);
        }
    }

    private static byte ComputeDrawableConstantFlags(Live2DDrawable drawable)
    {
        byte flags = 0;

        if (drawable.BlendMode == 1)
        {
            flags |= 0x01;
        }
        else if (drawable.BlendMode == 2)
        {
            flags |= 0x02;
        }

        if (drawable.FlipUvY)
        {
            flags |= 0x04;
        }

        return flags;
    }

    private static int EstimateMoc3Size(Live2DModelData data)
    {
        var size = Live2DConstants.HeaderSize;
        size += Live2DConstants.GetOffsetTableSize(data.Version);

        size += 5 * 4;
        size += 5 * 4;

        size += EstimateStringArraySize(data.Parameters, p => p.Id);
        size += data.Parameters.Count * 4;
        size += data.Parameters.Count * 4;
        size += data.Parameters.Count * 4;

        size += EstimateStringArraySize(data.Parts, p => p.Id);
        size += data.Parts.Count * 4;

        size += EstimateStringArraySize(data.Drawables, d => d.Id);
        size += data.Drawables.Count;
        size += data.Drawables.Count * 4;
        size += data.Drawables.Count * 4;
        size += data.Drawables.Count * 4;
        size += data.Drawables.Count * 4;
        size += data.Drawables.Count * 4;
        size += data.Drawables.Count * 4;

        foreach (var drawable in data.Drawables)
        {
            size += drawable.VertexPositions.Count * 4;
            size += drawable.VertexUvs.Count * 4;
            size += drawable.Indices.Count * 4;
        }

        if (data.Version >= 3)
        {
            size += data.Drawables.Count;
        }

        if (data.Version >= 4)
        {
            size += EstimateStringArraySize(data.Deformers, d => d.Id);
            size += data.Deformers.Count;
            size += data.Deformers.Count * 4;
            size += data.Deformers.Count * 4;
        }

        if (data.Version >= 5)
        {
            size += data.Deformers.Count * 4;
            size += data.Deformers.Count * 4;
            size += data.Deformers.Count * 4;
            size += data.Deformers.Count * 4;
        }

        size += 4096;

        return size;
    }

    private static int EstimateStringArraySize<T>(IReadOnlyList<T> items, Func<T, string> idSelector)
    {
        var size = 0;
        foreach (var item in items)
        {
            size += Encoding.UTF8.GetByteCount(idSelector(item)) + 1;
        }

        return size;
    }

    #endregion
}

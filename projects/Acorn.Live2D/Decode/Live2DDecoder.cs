using System.IO;
using Acorn.Frame;
using Acorn.Live2D.Data;

namespace Acorn.Live2D.Decode;

/// <summary>
///     Live2D moc3 文件解码器，从 moc3 二进制格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     moc3 格式采用 Structure of Arrays 范式，每个数据字段存储为独立的连续数组。
///     解码器通过段偏移表定位各段数据，然后按 SoA 方式读取各字段。
/// </remarks>
public ref struct Live2DDecoder
{
    private ByteBuffer _buffer;

    public Live2DDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     从 moc3 二进制数据解码完整的模型数据。
    /// </summary>
    /// <returns>解码后的 moc3 模型数据。</returns>
    public Live2DModelData DecodeMoc3()
    {
        var (version, isBigEndian, revision) = DecodeHeader();

        var offsetCount = Live2DConstants.GetOffsetTableCount(version);
        var offsets = ReadOffsetTable(offsetCount, isBigEndian);

        var parameterCount = ReadCountInfo(offsets, (int)Moc3Section.CountInfo, Live2DConstants.CountInfoParameterCountOffset, isBigEndian);
        var partCount = ReadCountInfo(offsets, (int)Moc3Section.CountInfo, Live2DConstants.CountInfoPartCountOffset, isBigEndian);
        var drawableCount = ReadCountInfo(offsets, (int)Moc3Section.CountInfo, Live2DConstants.CountInfoDrawableCountOffset, isBigEndian);
        var deformerCount = version >= 4
            ? ReadCountInfo(offsets, (int)Moc3Section.CountInfo, Live2DConstants.CountInfoDeformerCountOffset, isBigEndian)
            : 0;
        var textureCount = ReadCountInfo(offsets, (int)Moc3Section.CountInfo, Live2DConstants.CountInfoTextureCountOffset, isBigEndian);

        var canvas = DecodeCanvasInfo(offsets, isBigEndian);
        var parameters = DecodeParameters(offsets, parameterCount, isBigEndian);
        var parts = DecodeParts(offsets, partCount, isBigEndian);
        var drawables = DecodeDrawables(offsets, drawableCount, isBigEndian);
        var deformers = version >= 4
            ? DecodeDeformers(offsets, deformerCount, isBigEndian)
            : new List<Live2DDeformer>();

        return new Live2DModelData
        {
            Version = version,
            IsBigEndian = isBigEndian,
            Revision = revision,
            Canvas = canvas,
            Parameters = parameters,
            Parts = parts,
            Drawables = drawables,
            Deformers = deformers,
            TextureCount = textureCount
        };
    }

    #region 头部解码

    /// <summary>
    ///     解码 moc3 文件头，返回版本、字节序和修订号。
    /// </summary>
    public (int Version, bool IsBigEndian, int Revision) DecodeHeader()
    {
        var signature = _buffer.ReadString(4);
        if (signature != "MOC3")
        {
            throw new InvalidDataException($"moc3 文件签名无效，期望 \"MOC3\"，实际 \"{signature}\"");
        }

        var version = _buffer.ReadU8();
        var isBigEndian = _buffer.ReadU8() != 0;
        var revision = _buffer.ReadI16LE();

        return (version, isBigEndian, revision);
    }

    #endregion

    #region 段偏移表

    private int[] ReadOffsetTable(int offsetCount, bool isBigEndian)
    {
        _buffer.Position = Live2DConstants.HeaderSize;

        var offsets = new int[offsetCount];
        for (var i = 0; i < offsetCount; i++)
        {
            offsets[i] = ReadI32(isBigEndian);
        }

        return offsets;
    }

    private int ReadCountInfo(int[] offsets, int countInfoSectionIndex, int fieldOffset, bool isBigEndian)
    {
        var sectionOffset = offsets[countInfoSectionIndex];
        if (sectionOffset == 0)
        {
            return 0;
        }

        _buffer.Position = sectionOffset + fieldOffset;
        return ReadI32(isBigEndian);
    }

    #endregion

    #region CanvasInfo 解码

    private Live2DCanvasInfo DecodeCanvasInfo(int[] offsets, bool isBigEndian)
    {
        var offset = offsets[(int)Moc3Section.CanvasInfo];
        if (offset == 0)
        {
            return new Live2DCanvasInfo();
        }

        _buffer.Position = offset;
        return new Live2DCanvasInfo
        {
            Width = ReadF32(isBigEndian),
            Height = ReadF32(isBigEndian),
            CenterX = ReadF32(isBigEndian),
            CenterY = ReadF32(isBigEndian),
            PixelsPerUnit = ReadF32(isBigEndian)
        };
    }

    #endregion

    #region Parameter 解码

    private List<Live2DParameter> DecodeParameters(int[] offsets, int count, bool isBigEndian)
    {
        var ids = ReadStringArray(offsets, (int)Moc3Section.ParameterIds, count);
        var minValues = ReadF32Array(offsets, (int)Moc3Section.ParameterMinimumValues, count, isBigEndian);
        var maxValues = ReadF32Array(offsets, (int)Moc3Section.ParameterMaximumValues, count, isBigEndian);
        var defaultValues = ReadF32Array(offsets, (int)Moc3Section.ParameterDefaultValues, count, isBigEndian);

        var parameters = new List<Live2DParameter>(count);
        for (var i = 0; i < count; i++)
        {
            parameters.Add(new Live2DParameter
            {
                Id = ids[i],
                MinValue = minValues[i],
                MaxValue = maxValues[i],
                DefaultValue = defaultValues[i]
            });
        }

        return parameters;
    }

    #endregion

    #region Part 解码

    private List<Live2DPart> DecodeParts(int[] offsets, int count, bool isBigEndian)
    {
        var ids = ReadStringArray(offsets, (int)Moc3Section.PartIds, count);
        var parentIndices = ReadI32Array(offsets, (int)Moc3Section.PartParentPartIndices, count, isBigEndian);

        var parts = new List<Live2DPart>(count);
        for (var i = 0; i < count; i++)
        {
            parts.Add(new Live2DPart
            {
                Id = ids[i],
                ParentIndex = parentIndices[i]
            });
        }

        return parts;
    }

    #endregion

    #region Drawable 解码

    private List<Live2DDrawable> DecodeDrawables(int[] offsets, int count, bool isBigEndian)
    {
        var ids = ReadStringArray(offsets, (int)Moc3Section.DrawableIds, count);
        var textureIndices = ReadI32Array(offsets, (int)Moc3Section.DrawableTextureIndices, count, isBigEndian);
        var drawOrders = ReadI32Array(offsets, (int)Moc3Section.DrawableDrawOrders, count, isBigEndian);
        var renderOrders = ReadI32Array(offsets, (int)Moc3Section.DrawableRenderOrders, count, isBigEndian);
        var maskCounts = ReadI32Array(offsets, (int)Moc3Section.DrawableMaskCounts, count, isBigEndian);
        var vertexCounts = ReadI32Array(offsets, (int)Moc3Section.DrawableVertexCounts, count, isBigEndian);

        var maskOffset = offsets[(int)Moc3Section.DrawableMasks];
        var vertexPositionsOffset = offsets[(int)Moc3Section.DrawableVertexPositions];
        var vertexUvsOffset = offsets[(int)Moc3Section.DrawableVertexUvs];
        var indicesOffset = offsets[(int)Moc3Section.DrawableIndices];

        var drawables = new List<Live2DDrawable>(count);
        var maskPos = maskOffset;
        var vertexPosPos = vertexPositionsOffset;
        var vertexUvPos = vertexUvsOffset;
        var indicesPos = indicesOffset;

        for (var i = 0; i < count; i++)
        {
            var maskDrawableIndices = ReadMaskIndices(ref maskPos, maskCounts[i], isBigEndian);
            var vertexPositions = ReadF32ArrayAt(ref vertexPosPos, vertexCounts[i] * 2, isBigEndian);
            var vertexUvs = ReadF32ArrayAt(ref vertexUvPos, vertexCounts[i] * 2, isBigEndian);

            var indexCount = EstimateIndexCount(vertexCounts[i]);
            var indices = ReadI32ArrayAt(ref indicesPos, indexCount, isBigEndian);

            drawables.Add(new Live2DDrawable
            {
                Id = ids[i],
                TextureIndex = textureIndices[i],
                DrawOrder = drawOrders[i],
                RenderOrder = renderOrders[i],
                VertexPositions = vertexPositions.ToList(),
                VertexUvs = vertexUvs.ToList(),
                Indices = indices.ToList(),
                VertexCount = vertexCounts[i],
                MaskDrawableIndices = maskDrawableIndices
            });
        }

        return drawables;
    }

    private List<int> ReadMaskIndices(ref int position, int maskCount, bool isBigEndian)
    {
        if (position == 0 || maskCount == 0)
        {
            return new List<int>();
        }

        _buffer.Position = position;
        var result = new List<int>(maskCount);
        for (var i = 0; i < maskCount; i++)
        {
            result.Add(ReadI32(isBigEndian));
        }

        position = _buffer.Position;
        return result;
    }

    private static int EstimateIndexCount(int vertexCount)
    {
        return vertexCount > 2 ? (vertexCount - 2) * 3 : 0;
    }

    #endregion

    #region Deformer 解码

    private List<Live2DDeformer> DecodeDeformers(int[] offsets, int count, bool isBigEndian)
    {
        var ids = ReadStringArray(offsets, (int)Moc3Section.DeformerIds, count);
        var types = ReadU8Array(offsets, (int)Moc3Section.DeformerTypes, count);
        var parentIndices = ReadI32Array(offsets, (int)Moc3Section.DeformerParentIndices, count, isBigEndian);
        var boundingBoxX = ReadF32Array(offsets, (int)Moc3Section.DeformerBoundingBoxX, count, isBigEndian);

        var boundingBoxY = offsets.Length > (int)Moc3Section.DeformerBoundingBoxY && offsets[(int)Moc3Section.DeformerBoundingBoxY] != 0
            ? ReadF32Array(offsets, (int)Moc3Section.DeformerBoundingBoxY, count, isBigEndian)
            : new float[count];
        var boundingBoxWidth = offsets.Length > (int)Moc3Section.DeformerBoundingBoxWidth && offsets[(int)Moc3Section.DeformerBoundingBoxWidth] != 0
            ? ReadF32Array(offsets, (int)Moc3Section.DeformerBoundingBoxWidth, count, isBigEndian)
            : new float[count];
        var boundingBoxHeight = offsets.Length > (int)Moc3Section.DeformerBoundingBoxHeight && offsets[(int)Moc3Section.DeformerBoundingBoxHeight] != 0
            ? ReadF32Array(offsets, (int)Moc3Section.DeformerBoundingBoxHeight, count, isBigEndian)
            : new float[count];

        var deformers = new List<Live2DDeformer>(count);
        for (var i = 0; i < count; i++)
        {
            deformers.Add(new Live2DDeformer
            {
                Id = ids[i],
                Type = types[i],
                ParentIndex = parentIndices[i],
                X = boundingBoxX[i],
                Y = boundingBoxY[i],
                Width = boundingBoxWidth[i],
                Height = boundingBoxHeight[i]
            });
        }

        return deformers;
    }

    #endregion

    #region 通用数组读取

    private string[] ReadStringArray(int[] offsets, int sectionIndex, int count)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0)
        {
            return new string[count];
        }

        _buffer.Position = offset;
        var result = new string[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = _buffer.ReadNullTerminatedString();
        }

        return result;
    }

    private float[] ReadF32Array(int[] offsets, int sectionIndex, int count, bool isBigEndian)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0)
        {
            return new float[count];
        }

        _buffer.Position = offset;
        return ReadF32ArrayAt(offset, count, isBigEndian);
    }

    private float[] ReadF32ArrayAt(int offset, int count, bool isBigEndian)
    {
        _buffer.Position = offset;
        var result = new float[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = ReadF32(isBigEndian);
        }

        return result;
    }

    private float[] ReadF32ArrayAt(ref int position, int count, bool isBigEndian)
    {
        if (position == 0 || count == 0)
        {
            return new float[count];
        }

        _buffer.Position = position;
        var result = new float[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = ReadF32(isBigEndian);
        }

        position = _buffer.Position;
        return result;
    }

    private int[] ReadI32Array(int[] offsets, int sectionIndex, int count, bool isBigEndian)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0)
        {
            return new int[count];
        }

        _buffer.Position = offset;
        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = ReadI32(isBigEndian);
        }

        return result;
    }

    private int[] ReadI32ArrayAt(ref int position, int count, bool isBigEndian)
    {
        if (position == 0 || count == 0)
        {
            return new int[count];
        }

        _buffer.Position = position;
        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = ReadI32(isBigEndian);
        }

        position = _buffer.Position;
        return result;
    }

    private byte[] ReadU8Array(int[] offsets, int sectionIndex, int count)
    {
        var offset = offsets[sectionIndex];
        if (offset == 0 || count == 0)
        {
            return new byte[count];
        }

        _buffer.Position = offset;
        var result = new byte[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = _buffer.ReadU8();
        }

        return result;
    }

    #endregion

    #region 字节序感知读取

    private int ReadI32(bool isBigEndian)
    {
        return isBigEndian ? _buffer.ReadI32BE() : _buffer.ReadI32LE();
    }

    private float ReadF32(bool isBigEndian)
    {
        return isBigEndian ? _buffer.ReadF32BE() : _buffer.ReadF32LE();
    }

    #endregion
}

using Acorn.Gif.Data;

namespace Acorn.Gif.Encode;

/// <summary>
///     GIF 动画编码器——纯 C# 实现，无第三方依赖
/// </summary>
/// <remarks>
///     实现了 GIF89a 格式的动画编码，使用中值切分量化算法将 RGBA 像素转换为 256 色调色板，
///     LZW 压缩像素索引数据。支持多帧动画、透明色和帧延迟控制。
/// </remarks>
public sealed class GifEncoder
{
    #region 公开方法

    /// <summary>
    ///     编码 GIF 动画
    /// </summary>
    /// <param name="frames">帧列表</param>
    /// <param name="repeatCount">循环次数（0 = 无限循环）</param>
    /// <returns>GIF 二进制数据</returns>
    public byte[] Encode(IReadOnlyList<GifFrame> frames, int repeatCount = 0)
    {
        if (frames.Count == 0) return [];

        var output = new List<byte>();
        var width = frames[0].Width;
        var height = frames[0].Height;

        WriteHeader(output, width, height);

        if (repeatCount != 1)
        {
            WriteNetscapeExtension(output, repeatCount);
        }

        for (var i = 0; i < frames.Count; i++)
        {
            WriteFrame(output, frames[i], width, height, i == 0);
        }

        output.Add(0x3B);

        return output.ToArray();
    }

    #endregion

    #region GIF 头部

    private static void WriteHeader(List<byte> output, int width, int height)
    {
        output.AddRange("GIF89a"u8);

        WriteU16LE(output, (ushort)width);
        WriteU16LE(output, (ushort)height);

        output.Add(0x70);
        output.Add(0);
        output.Add(0);
    }

    #endregion

    #region 扩展块

    private static void WriteNetscapeExtension(List<byte> output, int repeatCount)
    {
        output.Add(0x21);
        output.Add(0xFF);
        output.Add(0x0B);
        output.AddRange("NETSCAPE2.0"u8);
        output.Add(0x03);
        output.Add(0x01);
        WriteU16LE(output, (ushort)repeatCount);
        output.Add(0x00);
    }

    private static void WriteGraphicControlExtension(List<byte> output, GifFrame frame, int transparentIndex)
    {
        output.Add(0x21);
        output.Add(0xF9);
        output.Add(0x04);

        var packed = (byte)((int)frame.DisposalMethod << 2);
        if (transparentIndex >= 0)
        {
            packed |= 0x01;
        }
        output.Add(packed);

        WriteU16LE(output, (ushort)frame.DelayCentiseconds);
        output.Add((byte)(transparentIndex >= 0 ? transparentIndex : 0));
        output.Add(0x00);
    }

    #endregion

    #region 帧编码

    private static void WriteFrame(List<byte> output, GifFrame frame, int canvasWidth, int canvasHeight, bool isFirst)
    {
        var (palette, indices, transparentIndex) = Quantize(frame.RgbaData, frame.Width, frame.Height);

        WriteGraphicControlExtension(output, frame, transparentIndex);

        output.Add(0x2C);

        WriteU16LE(output, 0);
        WriteU16LE(output, 0);
        WriteU16LE(output, (ushort)frame.Width);
        WriteU16LE(output, (ushort)frame.Height);

        output.Add(0x80);

        for (var i = 0; i < 256; i++)
        {
            if (i < palette.Length)
            {
                output.Add(palette[i].R);
                output.Add(palette[i].G);
                output.Add(palette[i].B);
            }
            else
            {
                output.Add(0);
                output.Add(0);
                output.Add(0);
            }
        }

        var minCodeSize = 8;
        output.Add((byte)minCodeSize);

        var compressed = LzwCompress(indices, minCodeSize);
        var pos = 0;
        while (pos < compressed.Count)
        {
            var blockSize = Math.Min(255, compressed.Count - pos);
            output.Add((byte)blockSize);
            for (var i = 0; i < blockSize; i++)
            {
                output.Add(compressed[pos + i]);
            }
            pos += blockSize;
        }

        output.Add(0x00);
    }

    #endregion

    #region 中值切分量化

    private static ((byte R, byte G, byte B)[] Palette, byte[] Indices, int TransparentIndex) Quantize(
        byte[] rgbaData, int width, int height)
    {
        var transparentIndex = -1;
        var hasTransparency = false;

        for (var i = 3; i < rgbaData.Length; i += 4)
        {
            if (rgbaData[i] < 128)
            {
                hasTransparency = true;
                break;
            }
        }

        var maxColors = hasTransparency ? 255 : 256;
        var colorList = new List<(byte R, byte G, byte B, int Index)>();
        var colorMap = new Dictionary<int, int>();

        for (var i = 0; i < rgbaData.Length; i += 4)
        {
            if (rgbaData[i + 3] < 128) continue;

            var colorKey = (rgbaData[i] << 16) | (rgbaData[i + 1] << 8) | rgbaData[i + 2];
            if (!colorMap.ContainsKey(colorKey))
            {
                colorMap[colorKey] = colorList.Count;
                colorList.Add((rgbaData[i], rgbaData[i + 1], rgbaData[i + 2], colorList.Count));
            }
        }

        var palette = new (byte R, byte G, byte B)[256];
        var indexMap = new int[colorMap.Count];

        if (colorList.Count <= maxColors)
        {
            for (var i = 0; i < colorList.Count; i++)
            {
                palette[i] = (colorList[i].R, colorList[i].G, colorList[i].B);
                indexMap[i] = i;
            }

            if (hasTransparency)
            {
                transparentIndex = colorList.Count;
                palette[transparentIndex] = (0, 0, 0);
            }
        }
        else
        {
            var boxes = MedianCut(colorList, maxColors);
            var paletteIdx = 0;

            foreach (var box in boxes)
            {
                var avgR = (byte)(box.Sum(c => c.R) / box.Count);
                var avgG = (byte)(box.Sum(c => c.G) / box.Count);
                var avgB = (byte)(box.Sum(c => c.B) / box.Count);

                palette[paletteIdx] = (avgR, avgG, avgB);

                foreach (var color in box)
                {
                    indexMap[color.Index] = paletteIdx;
                }

                paletteIdx++;
            }

            if (hasTransparency)
            {
                transparentIndex = paletteIdx;
                palette[transparentIndex] = (0, 0, 0);
            }
        }

        var indices = new byte[width * height];
        for (var i = 0; i < width * height; i++)
        {
            var pixelOffset = i * 4;
            if (rgbaData[pixelOffset + 3] < 128)
            {
                indices[i] = (byte)(transparentIndex >= 0 ? transparentIndex : 0);
            }
            else
            {
                var colorKey = (rgbaData[pixelOffset] << 16) | (rgbaData[pixelOffset + 1] << 8) | rgbaData[pixelOffset + 2];
                var colorIdx = colorMap[colorKey];
                indices[i] = (byte)indexMap[colorIdx];
            }
        }

        return (palette, indices, transparentIndex);
    }

    private static List<List<(byte R, byte G, byte B, int Index)>> MedianCut(
        List<(byte R, byte G, byte B, int Index)> colors, int targetCount)
    {
        var boxes = new List<List<(byte R, byte G, byte B, int Index)>> { colors };

        while (boxes.Count < targetCount)
        {
            var maxBoxIdx = -1;
            var maxRange = -1;

            for (var i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].Count <= 1) continue;

                var rRange = boxes[i].Max(c => c.R) - boxes[i].Min(c => c.R);
                var gRange = boxes[i].Max(c => c.G) - boxes[i].Min(c => c.G);
                var bRange = boxes[i].Max(c => c.B) - boxes[i].Min(c => c.B);
                var range = Math.Max(rRange, Math.Max(gRange, bRange));

                if (range > maxRange)
                {
                    maxRange = range;
                    maxBoxIdx = i;
                }
            }

            if (maxBoxIdx < 0) break;

            var box = boxes[maxBoxIdx];
            var rRange2 = box.Max(c => c.R) - box.Min(c => c.R);
            var gRange2 = box.Max(c => c.G) - box.Min(c => c.G);
            var bRange2 = box.Max(c => c.B) - box.Min(c => c.B);

            if (rRange2 >= gRange2 && rRange2 >= bRange2)
            {
                box.Sort((a, b) => a.R.CompareTo(b.R));
            }
            else if (gRange2 >= bRange2)
            {
                box.Sort((a, b) => a.G.CompareTo(b.G));
            }
            else
            {
                box.Sort((a, b) => a.B.CompareTo(b.B));
            }

            var mid = box.Count / 2;
            boxes[maxBoxIdx] = box.Take(mid).ToList();
            boxes.Insert(maxBoxIdx + 1, box.Skip(mid).ToList());
        }

        return boxes;
    }

    #endregion

    #region LZW 压缩

    private static List<byte> LzwCompress(byte[] indices, int minCodeSize)
    {
        var clearCode = 1 << minCodeSize;
        var eoiCode = clearCode + 1;
        var codeSize = minCodeSize + 1;
        var nextCode = eoiCode + 1;
        var maxCode = 1 << codeSize;

        var output = new List<byte>();
        var bitBuffer = 0u;
        var bitCount = 0;

        var dictionary = new Dictionary<int, int>();
        for (var i = 0; i < clearCode; i++)
        {
            dictionary[i] = i;
        }

        WriteBits(output, clearCode, codeSize, ref bitBuffer, ref bitCount);

        if (indices.Length == 0)
        {
            WriteBits(output, eoiCode, codeSize, ref bitBuffer, ref bitCount);
            FlushBits(output, ref bitBuffer, ref bitCount);
            return output;
        }

        var current = (int)indices[0];

        for (var i = 1; i < indices.Length; i++)
        {
            var next = indices[i];
            var key = (current << 8) | next;

            if (dictionary.TryGetValue(key, out var code))
            {
                current = code;
            }
            else
            {
                WriteBits(output, current, codeSize, ref bitBuffer, ref bitCount);

                if (nextCode < 4096)
                {
                    dictionary[key] = nextCode;
                    nextCode++;

                    if (nextCode > maxCode && codeSize < 12)
                    {
                        codeSize++;
                        maxCode = 1 << codeSize;
                    }
                }
                else
                {
                    WriteBits(output, clearCode, codeSize, ref bitBuffer, ref bitCount);
                    dictionary.Clear();
                    for (var j = 0; j < clearCode; j++)
                    {
                        dictionary[j] = j;
                    }
                    nextCode = eoiCode + 1;
                    codeSize = minCodeSize + 1;
                    maxCode = 1 << codeSize;
                }

                current = next;
            }
        }

        WriteBits(output, current, codeSize, ref bitBuffer, ref bitCount);
        WriteBits(output, eoiCode, codeSize, ref bitBuffer, ref bitCount);
        FlushBits(output, ref bitBuffer, ref bitCount);

        return output;
    }

    private static void WriteBits(List<byte> output, int code, int codeSize, ref uint bitBuffer, ref int bitCount)
    {
        bitBuffer |= (uint)(code << bitCount);
        bitCount += codeSize;

        while (bitCount >= 8)
        {
            output.Add((byte)(bitBuffer & 0xFF));
            bitBuffer >>= 8;
            bitCount -= 8;
        }
    }

    private static void FlushBits(List<byte> output, ref uint bitBuffer, ref int bitCount)
    {
        if (bitCount > 0)
        {
            output.Add((byte)(bitBuffer & 0xFF));
            bitBuffer = 0;
            bitCount = 0;
        }
    }

    #endregion

    #region 辅助方法

    private static void WriteU16LE(List<byte> output, ushort value)
    {
        output.Add((byte)(value & 0xFF));
        output.Add((byte)((value >> 8) & 0xFF));
    }

    #endregion
}

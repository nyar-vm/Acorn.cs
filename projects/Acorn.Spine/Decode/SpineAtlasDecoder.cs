using System.Globalization;
using System.Text;
using Acorn.Spine.Data;

namespace Acorn.Spine.Decode;

/// <summary>
///     Spine Atlas 解码器，将 Spine Atlas 文本格式解码为 <see cref="SpineAtlasData" />。
/// </summary>
/// <remarks>
///     Spine Atlas 格式是纯文本格式，包含页面定义和区域定义。
///     解析器逐行读取，识别页面和区域边界。
/// </remarks>
public sealed class SpineAtlasDecoder
{
    /// <summary>
    ///     从文本字符串解码 Atlas 数据。
    /// </summary>
    /// <param name="content">Atlas 文本内容。</param>
    /// <returns>解码后的 Atlas 数据。</returns>
    public SpineAtlasData Decode(string content)
    {
        var pages = new List<SpineAtlasPage>();
        var regions = new List<SpineAtlasRegion>();

        using var reader = new StringReader(content);
        var line = reader.ReadLine();
        var currentPageIndex = -1;

        while (line != null)
        {
            line = line.Trim();

            if (string.IsNullOrEmpty(line))
            {
                line = reader.ReadLine();
                continue;
            }

            if (!line.Contains(':'))
            {
                if (currentPageIndex >= 0)
                {
                    regions.Add(ParseRegion(line, reader, currentPageIndex));
                }
            }
            else if (line.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
            {
                currentPageIndex++;
                pages.Add(ParsePage(line, reader));
            }

            line = reader.ReadLine();
        }

        return new SpineAtlasData
        {
            Pages = pages,
            Regions = regions
        };
    }

    private static SpineAtlasPage ParsePage(string firstLine, StringReader reader)
    {
        var textureFilePath = string.Empty;
        var width = 0;
        var height = 0;
        var format = string.Empty;
        var filterMin = string.Empty;
        var filterMag = string.Empty;
        var wrapS = "clampToEdge";
        var wrapT = "clampToEdge";

        var parts = firstLine.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length >= 2)
        {
            var sizeParts = parts[1].Split(',', StringSplitOptions.TrimEntries);

            if (sizeParts.Length >= 2)
            {
                int.TryParse(sizeParts[0], out width);
                int.TryParse(sizeParts[1], out height);
            }
        }

        var line = reader.ReadLine();

        while (line != null && !string.IsNullOrWhiteSpace(line) && line.Contains(':'))
        {
            line = line.Trim();
            var kv = line.Split(':', StringSplitOptions.TrimEntries);

            if (kv.Length >= 2)
            {
                var key = kv[0].ToLowerInvariant();
                var value = kv[1];

                switch (key)
                {
                    case "format":
                        format = value;
                        break;

                    case "filter":
                        var filters = value.Split(',', StringSplitOptions.TrimEntries);
                        filterMin = filters.Length > 0 ? filters[0] : value;
                        filterMag = filters.Length > 1 ? filters[1] : filterMin;
                        break;

                    case "repeat":
                        var repeatValue = value.ToLowerInvariant();
                        wrapS = repeatValue switch
                        {
                            "x" => "repeat",
                            "xy" => "repeat",
                            _ => "clampToEdge"
                        };
                        wrapT = repeatValue switch
                        {
                            "y" => "repeat",
                            "xy" => "repeat",
                            _ => "clampToEdge"
                        };
                        break;
                }
            }

            line = reader.ReadLine();
        }

        return new SpineAtlasPage
        {
            TextureFilePath = textureFilePath,
            Width = width,
            Height = height,
            Format = format,
            FilterMin = filterMin,
            FilterMag = filterMag,
            WrapS = wrapS,
            WrapT = wrapT
        };
    }

    private static SpineAtlasRegion ParseRegion(string name, StringReader reader, int pageIndex)
    {
        var x = 0;
        var y = 0;
        var width = 0;
        var height = 0;
        var offsetX = 0;
        var offsetY = 0;
        var originalWidth = 0;
        var originalHeight = 0;
        var isRotated = false;
        var isSplit = false;
        int[]? splits = null;
        int[]? pads = null;

        var line = reader.ReadLine();

        while (line != null && !string.IsNullOrWhiteSpace(line) && line.Contains(':'))
        {
            line = line.Trim();
            var kv = line.Split(':', StringSplitOptions.TrimEntries);

            if (kv.Length >= 2)
            {
                var key = kv[0].ToLowerInvariant().Trim();
                var value = kv[1].Trim();

                switch (key)
                {
                    case "bounds":
                    case "xy":
                        var boundsParts = value.Split(',', StringSplitOptions.TrimEntries);

                        if (boundsParts.Length >= 2)
                        {
                            int.TryParse(boundsParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x);
                            int.TryParse(boundsParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y);
                        }

                        if (boundsParts.Length >= 4)
                        {
                            int.TryParse(boundsParts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out width);
                            int.TryParse(boundsParts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out height);
                        }

                        break;

                    case "size":
                        var sizeParts = value.Split(',', StringSplitOptions.TrimEntries);

                        if (sizeParts.Length >= 2)
                        {
                            int.TryParse(sizeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out width);
                            int.TryParse(sizeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out height);
                        }

                        break;

                    case "offset":
                        var offsetParts = value.Split(',', StringSplitOptions.TrimEntries);

                        if (offsetParts.Length >= 2)
                        {
                            int.TryParse(offsetParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out offsetX);
                            int.TryParse(offsetParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out offsetY);
                        }

                        break;

                    case "orig":
                    case "original":
                        var origParts = value.Split(',', StringSplitOptions.TrimEntries);

                        if (origParts.Length >= 2)
                        {
                            int.TryParse(origParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out originalWidth);
                            int.TryParse(origParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out originalHeight);
                        }

                        break;

                    case "rotate":
                        isRotated = value.Equals("90", StringComparison.OrdinalIgnoreCase)
                                    || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        break;

                    case "split":
                        isSplit = true;
                        splits = ParseIntArray(value);
                        break;

                    case "pad":
                        pads = ParseIntArray(value);
                        break;
                }
            }

            line = reader.ReadLine();
        }

        return new SpineAtlasRegion
        {
            Name = name,
            PageIndex = pageIndex,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            OffsetX = offsetX,
            OffsetY = offsetY,
            OriginalWidth = originalWidth,
            OriginalHeight = originalHeight,
            IsRotated = isRotated,
            IsSplit = isSplit,
            Splits = splits,
            Pads = pads
        };
    }

    private static int[] ParseIntArray(string value)
    {
        return value.Split(',', StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0)
            .ToArray();
    }
}

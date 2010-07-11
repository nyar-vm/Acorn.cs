using System.Globalization;
using System.Text;
using Acorn.Spine.Data;

namespace Acorn.Spine.Encode;

/// <summary>
///     Spine Atlas 编码器，将 <see cref="SpineAtlasData" /> 编码为 Spine Atlas 文本格式。
/// </summary>
/// <remarks>
///     Spine Atlas 格式是纯文本格式，包含页面定义和区域定义。
///     每个页面以 size 属性开头，后跟区域定义。
/// </remarks>
public sealed class SpineAtlasEncoder
{
    private readonly StringBuilder _builder = new();

    /// <summary>
    ///     将 Atlas 数据编码为文本字符串。
    /// </summary>
    /// <param name="data">Atlas 数据。</param>
    /// <returns>编码后的 Atlas 文本。</returns>
    public string Encode(SpineAtlasData data)
    {
        _builder.Clear();

        for (var pageIndex = 0; pageIndex < data.Pages.Count; pageIndex++)
        {
            var page = data.Pages[pageIndex];
            EncodePage(page);

            var regions = data.Regions.Where(r => r.PageIndex == pageIndex).ToList();

            foreach (var region in regions)
            {
                EncodeRegion(region);
            }
        }

        return _builder.ToString();
    }

    private void EncodePage(SpineAtlasPage page)
    {
        _builder.AppendLine(page.TextureFilePath);
        _builder.AppendLine($"size: {page.Width}, {page.Height}");

        if (!string.IsNullOrEmpty(page.Format))
        {
            _builder.AppendLine($"format: {page.Format}");
        }

        if (!string.IsNullOrEmpty(page.FilterMin) || !string.IsNullOrEmpty(page.FilterMag))
        {
            var filterMin = page.FilterMin;
            var filterMag = page.FilterMag;

            if (string.IsNullOrEmpty(filterMin))
            {
                filterMin = filterMag;
            }

            if (string.IsNullOrEmpty(filterMag))
            {
                filterMag = filterMin;
            }

            _builder.AppendLine($"filter: {filterMin}, {filterMag}");
        }

        var wrapS = page.WrapS.ToLowerInvariant();
        var wrapT = page.WrapT.ToLowerInvariant();

        if (wrapS == "repeat" && wrapT == "repeat")
        {
            _builder.AppendLine("repeat: xy");
        }
        else if (wrapS == "repeat")
        {
            _builder.AppendLine("repeat: x");
        }
        else if (wrapT == "repeat")
        {
            _builder.AppendLine("repeat: y");
        }

        _builder.AppendLine();
    }

    private void EncodeRegion(SpineAtlasRegion region)
    {
        _builder.AppendLine(region.Name);
        _builder.AppendLine($"  bounds: {region.X}, {region.Y}, {region.Width}, {region.Height}");

        if (region.OffsetX != 0 || region.OffsetY != 0)
        {
            _builder.AppendLine($"  offset: {region.OffsetX}, {region.OffsetY}");
        }

        if (region.OriginalWidth != 0 || region.OriginalHeight != 0)
        {
            _builder.AppendLine($"  orig: {region.OriginalWidth}, {region.OriginalHeight}");
        }

        if (region.IsRotated)
        {
            _builder.AppendLine("  rotate: 90");
        }

        if (region.IsSplit && region.Splits != null)
        {
            _builder.AppendLine($"  split: {string.Join(", ", region.Splits)}");
        }

        if (region.Pads != null)
        {
            _builder.AppendLine($"  pad: {string.Join(", ", region.Pads)}");
        }

        _builder.AppendLine();
    }
}

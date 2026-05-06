using System.Buffers.Binary;
using System.Text;
using Acorn.BmFont.Data;

namespace Acorn.BmFont.Encode;

/// <summary>
///     BMFont 编码器，将 <see cref="BmFontData" /> 编码为 BMFont 二进制格式。
/// </summary>
/// <remarks>
///     BMFont 是 AngelCode 的位图字体格式，广泛用于游戏 UI 渲染。
///     编码器输出 BMFont 二进制格式（.fnt），文本格式由 Oak.BmFont 处理。
/// </remarks>
public sealed class BmFontEncoder
{
    /// <summary>
    ///     将 BMFont 数据编码为二进制字节数组。
    /// </summary>
    /// <param name="data">BMFont 数据。</param>
    /// <returns>BMFont 二进制数据。</returns>
    public byte[] Encode(BmFontData data)
    {
        var blocks = new List<byte[]>();

        // Info 块
        blocks.Add(EncodeInfoBlock(data.Info));

        // Common 块
        blocks.Add(EncodeCommonBlock(data.Common));

        // Pages 块
        if (data.Pages.Count > 0)
        {
            blocks.Add(EncodePagesBlock(data.Pages, data.Common.Pages));
        }

        // Chars 块
        if (data.Chars.Count > 0)
        {
            blocks.Add(EncodeCharsBlock(data.Chars));
        }

        // Kerning 块
        if (data.KerningPairs.Count > 0)
        {
            blocks.Add(EncodeKerningBlock(data.KerningPairs));
        }

        // 计算总大小并组装
        var totalSize = 4; // BMF + version

        foreach (var block in blocks)
        {
            totalSize += block.Length;
        }

        var buffer = new byte[totalSize];
        var pos = 0;

        buffer[pos++] = (byte)'B';
        buffer[pos++] = (byte)'M';
        buffer[pos++] = (byte)'F';
        buffer[pos++] = BmFontConstants.BinaryVersion;

        foreach (var block in blocks)
        {
            block.CopyTo(buffer.AsSpan(pos));
            pos += block.Length;
        }

        return buffer;
    }

    #region 块编码

    private static byte[] EncodeInfoBlock(BmFontInfo info)
    {
        var nameBytes = Encoding.UTF8.GetBytes(info.FontName);
        var dataSize = 13 + nameBytes.Length + 1; // 13 字节固定头 + 名称 + null
        var block = new byte[5 + dataSize]; // 类型(1) + 大小(4) + 数据
        var pos = 0;

        block[pos++] = BmFontConstants.BlockInfo;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.Size);
        pos += 2;

        byte flags = 0;

        if (info.Bold)
        {
            flags |= 0x01;
        }

        if (info.Italic)
        {
            flags |= 0x02;
        }

        if (info.Unicode)
        {
            flags |= 0x04;
        }

        block[pos++] = flags;
        block[pos++] = info.BitDepth;
        block[pos++] = info.CharSet;
        pos += 2; // stretchH 填充

        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.SpacingH);
        pos += 2;
        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.SpacingV);
        pos += 2;
        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.LineHeight);
        pos += 2;

        nameBytes.CopyTo(block.AsSpan(pos));
        pos += nameBytes.Length;
        block[pos] = 0; // null 结尾

        return block;
    }

    private static byte[] EncodeCommonBlock(BmFontCommon common)
    {
        const int dataSize = 15;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.BlockCommon;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), dataSize);
        pos += 4;

        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.LineHeight);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.Base);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.ScaleW);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.ScaleH);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.Pages);
        pos += 2;

        byte flags = 0;

        if (common.AlphaChannel)
        {
            flags |= 0x01;
        }

        if (common.RedChannel)
        {
            flags |= 0x02;
        }

        if (common.GreenChannel)
        {
            flags |= 0x04;
        }

        if (common.BlueChannel)
        {
            flags |= 0x08;
        }

        if (common.Packed)
        {
            flags |= 0x10;
        }

        block[pos++] = flags;
        pos += 3; // 保留填充

        return block;
    }

    private static byte[] EncodePagesBlock(IReadOnlyList<string> pages, ushort pageCount)
    {
        if (pages.Count == 0)
        {
            return [];
        }

        // 所有页面名称等长填充
        var maxLen = 0;

        foreach (var page in pages)
        {
            var len = Encoding.UTF8.GetByteCount(page);

            if (len > maxLen)
            {
                maxLen = len;
            }
        }

        var paddedLen = maxLen + 1;
        var dataSize = pageCount * paddedLen;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.BlockPages;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        for (var i = 0; i < pageCount && i < pages.Count; i++)
        {
            var nameBytes = Encoding.UTF8.GetBytes(pages[i]);
            nameBytes.CopyTo(block.AsSpan(pos));
            pos += paddedLen; // 直接跳到下一个页面名称位置
        }

        return block;
    }

    private static byte[] EncodeCharsBlock(IReadOnlyList<BmFontChar> chars)
    {
        const int charSize = 20;
        var dataSize = chars.Count * charSize;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.BlockChars;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        foreach (var c in chars)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), c.Id);
            pos += 4;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.X);
            pos += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.Y);
            pos += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.Width);
            pos += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.Height);
            pos += 2;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), c.XOffset);
            pos += 2;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), c.YOffset);
            pos += 2;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), c.XAdvance);
            pos += 2;
            block[pos++] = c.Page;
            block[pos++] = (byte)c.Channel;
        }

        return block;
    }

    private static byte[] EncodeKerningBlock(IReadOnlyList<BmFontKerningPair> pairs)
    {
        const int pairSize = 10;
        var dataSize = pairs.Count * pairSize;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.BlockKerningPairs;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        foreach (var p in pairs)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), p.First);
            pos += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), p.Second);
            pos += 4;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), p.Amount);
            pos += 2;
        }

        return block;
    }

    #endregion
}

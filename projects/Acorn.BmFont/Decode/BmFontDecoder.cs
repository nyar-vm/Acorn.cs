using System.Text;
using Acorn.Frame;
using Acorn.BmFont.Data;

namespace Acorn.BmFont.Decode;

/// <summary>
///     BMFont 二进制文件解码器，将 AngelCode BMFont 格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     BMFont 是 AngelCode 的位图字体格式，广泛用于游戏 UI 渲染。
///     解码器支持 BMFont 二进制格式（.fnt），文本格式由 Oak.BmFont 处理。
/// </remarks>
public ref struct BmFontDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="BmFontDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">BMFont 二进制数据。</param>
    public BmFontDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 BMFont 二进制文件。
    /// </summary>
    /// <returns>BMFont 数据。</returns>
    public BmFontData Decode()
    {
        var magic = _buffer.ReadString(3);

        if (magic != "BMF")
        {
            throw new InvalidDataException($"BMFont 文件签名无效，期望 \"BMF\"，实际 \"{magic}\"");
        }

        var version = _buffer.ReadU8();

        if (version != BmFontConstants.BinaryVersion)
        {
            throw new InvalidDataException($"BMFont 版本号无效，期望 {BmFontConstants.BinaryVersion}，实际 {version}");
        }

        BmFontInfo? info = null;
        BmFontCommon? common = null;
        var pages = new List<string>();
        var chars = new List<BmFontChar>();
        var kerningPairs = new List<BmFontKerningPair>();

        while (!_buffer.IsEnd)
        {
            if (_buffer.Remaining < 5)
            {
                break;
            }

            var blockType = _buffer.ReadU8();
            var blockSize = (int)_buffer.ReadU32LE();

            if (_buffer.Remaining < blockSize)
            {
                break;
            }

            switch (blockType)
            {
                case BmFontConstants.BlockInfo:
                    info = ReadInfoBlock(blockSize);
                    break;
                case BmFontConstants.BlockCommon:
                    common = ReadCommonBlock();
                    break;
                case BmFontConstants.BlockPages:
                    pages = ReadPagesBlock(blockSize, common?.Pages ?? 1);
                    break;
                case BmFontConstants.BlockChars:
                    chars = ReadCharsBlock(blockSize);
                    break;
                case BmFontConstants.BlockKerningPairs:
                    kerningPairs = ReadKerningBlock(blockSize);
                    break;
                default:
                    _buffer.Advance(blockSize);
                    break;
            }
        }

        return new BmFontData
        {
            Info = info ?? new BmFontInfo(),
            Common = common ?? new BmFontCommon(),
            Pages = pages,
            Chars = chars,
            KerningPairs = kerningPairs
        };
    }

    #region 私有解析方法

    private BmFontInfo ReadInfoBlock(int blockSize)
    {
        var size = _buffer.ReadI16LE();
        var flags = _buffer.ReadU8();
        var bitDepth = _buffer.ReadU8();
        var charSet = _buffer.ReadU8();

        _buffer.Advance(2);

        var spacingH = _buffer.ReadI16LE();
        var spacingV = _buffer.ReadI16LE();
        var lineHeight = _buffer.ReadI16LE();

        var nameLength = blockSize - 15;
        var fontName = nameLength > 0 ? _buffer.ReadString(nameLength).TrimEnd('\0') : string.Empty;

        return new BmFontInfo
        {
            Size = size,
            Bold = (flags & 0x01) != 0,
            Italic = (flags & 0x02) != 0,
            Unicode = (flags & 0x04) != 0,
            BitDepth = bitDepth,
            CharSet = charSet,
            SpacingH = spacingH,
            SpacingV = spacingV,
            LineHeight = lineHeight,
            FontName = fontName
        };
    }

    private BmFontCommon ReadCommonBlock()
    {
        var lineHeight = _buffer.ReadU16LE();
        var base_ = _buffer.ReadU16LE();
        var scaleW = _buffer.ReadU16LE();
        var scaleH = _buffer.ReadU16LE();
        var pages = _buffer.ReadU16LE();
        var flags = _buffer.ReadU8();

        _buffer.Advance(3);

        return new BmFontCommon
        {
            LineHeight = lineHeight,
            Base = base_,
            ScaleW = scaleW,
            ScaleH = scaleH,
            Pages = pages,
            AlphaChannel = (flags & 0x01) != 0,
            RedChannel = (flags & 0x02) != 0,
            GreenChannel = (flags & 0x04) != 0,
            BlueChannel = (flags & 0x08) != 0,
            Packed = (flags & 0x10) != 0
        };
    }

    private List<string> ReadPagesBlock(int blockSize, ushort pageCount)
    {
        var pages = new List<string>();
        var pageNameLength = pageCount > 0 ? blockSize / pageCount : 0;

        for (var i = 0; i < pageCount; i++)
        {
            var name = _buffer.ReadString(pageNameLength).TrimEnd('\0');
            pages.Add(name);
        }

        return pages;
    }

    private List<BmFontChar> ReadCharsBlock(int blockSize)
    {
        var charSize = 20;
        var count = blockSize / charSize;
        var chars = new List<BmFontChar>(count);

        for (var i = 0; i < count; i++)
        {
            var id = _buffer.ReadU32LE();
            var x = _buffer.ReadU16LE();
            var y = _buffer.ReadU16LE();
            var width = _buffer.ReadU16LE();
            var height = _buffer.ReadU16LE();
            var xOffset = _buffer.ReadI16LE();
            var yOffset = _buffer.ReadI16LE();
            var xAdvance = _buffer.ReadI16LE();
            var page = _buffer.ReadU8();
            var channel = _buffer.ReadU8();

            chars.Add(new BmFontChar
            {
                Id = id,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                XOffset = xOffset,
                YOffset = yOffset,
                XAdvance = xAdvance,
                Page = page,
                Channel = (BmFontChannel)channel
            });
        }

        return chars;
    }

    private List<BmFontKerningPair> ReadKerningBlock(int blockSize)
    {
        var pairSize = 10;
        var count = blockSize / pairSize;
        var pairs = new List<BmFontKerningPair>(count);

        for (var i = 0; i < count; i++)
        {
            var first = _buffer.ReadU32LE();
            var second = _buffer.ReadU32LE();
            var amount = _buffer.ReadI16LE();

            pairs.Add(new BmFontKerningPair
            {
                First = first,
                Second = second,
                Amount = amount
            });
        }

        return pairs;
    }

    #endregion
}

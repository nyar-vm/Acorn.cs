using Acorn.Gif.Data;

namespace Acorn.Gif.Decode;

/// <summary>
///     GIF 图像解码器——纯 C# 实现，无第三方依赖
/// </summary>
/// <remarks>
///     支持 GIF87a 和 GIF89a 格式解码，包括多帧动画、
///     全局/局部调色板、LZW 解压、隔行扫描、透明色、
///     图形控制扩展、Netscape 循环扩展。
/// </remarks>
public ref struct GifDecoder
{
    private ReadOnlySpan<byte> _data;
    private int _position;

    private int _width;
    private int _height;
    private byte[] _globalColorTable;
    private byte _backgroundColorIndex;
    private byte _pixelAspectRatio;
    private int _loopCount;

    private int _delayCentiseconds;
    private GifDisposalMethod _disposalMethod;
    private bool _hasTransparentColor;
    private int _transparentColorIndex;

    /// <summary>
    ///     初始化 GIF 解码器
    /// </summary>
    /// <param name="data">GIF 二进制数据</param>
    public GifDecoder(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
        _globalColorTable = [];
    }

    /// <summary>
    ///     解码 GIF 图像为结构化数据
    /// </summary>
    /// <returns>GIF 图像数据</returns>
    public GifImageData Decode()
    {
        ParseHeader();
        ParseLogicalScreenDescriptor();

        var frames = new List<GifImageFrame>();

        while (_position < _data.Length)
        {
            var blockType = _data[_position];

            switch (blockType)
            {
                case GifConstants.ImageSeparator:
                    _position++;
                    var frame = ParseImageDescriptor();
                    frames.Add(frame);
                    break;
                case GifConstants.ExtensionIntroducer:
                    _position++;
                    ParseExtension();
                    break;
                case GifConstants.Trailer:
                    goto done;
                default:
                    _position++;
                    break;
            }
        }

        done:
        return new GifImageData
        {
            Width = _width,
            Height = _height,
            GlobalColorTable = _globalColorTable,
            BackgroundColorIndex = _backgroundColorIndex,
            PixelAspectRatio = _pixelAspectRatio,
            LoopCount = _loopCount,
            Frames = frames
        };
    }

    /// <summary>
    ///     解码 GIF 图像为 RGBA 帧列表
    /// </summary>
    /// <returns>RGBA 帧列表</returns>
    public List<GifFrame> DecodeToRgba()
    {
        var gifData = Decode();
        var frames = new List<GifFrame>();
        var canvas = new byte[_width * _height * 4];
        var prevCanvas = new byte[_width * _height * 4];

        foreach (var frame in gifData.Frames)
        {
            var palette = frame.LocalColorTable.Length > 0
                ? frame.LocalColorTable
                : gifData.GlobalColorTable;

            if (frame.DisposalMethod == GifDisposalMethod.RestoreToPrevious)
            {
                Array.Copy(canvas, prevCanvas, canvas.Length);
            }

            CompositeFrame(canvas, frame, palette, gifData);

            var rgbaData = new byte[_width * _height * 4];
            Array.Copy(canvas, rgbaData, canvas.Length);

            frames.Add(new GifFrame
            {
                Width = _width,
                Height = _height,
                DelayCentiseconds = frame.DelayCentiseconds,
                DisposalMethod = frame.DisposalMethod,
                RgbaData = rgbaData
            });

            switch (frame.DisposalMethod)
            {
                case GifDisposalMethod.RestoreToBackground:
                    ClearRegion(canvas, frame.Left, frame.Top, frame.Width, frame.Height);
                    break;
                case GifDisposalMethod.RestoreToPrevious:
                    Array.Copy(prevCanvas, canvas, canvas.Length);
                    break;
            }
        }

        return frames;
    }

    #region 头部解析

    private void ParseHeader()
    {
        if (_position + GifConstants.SignatureLength > _data.Length)
        {
            throw new InvalidDataException("GIF 数据过短，无法读取签名");
        }

        var sig = _data.Slice(_position, GifConstants.SignatureLength);
        _position += GifConstants.SignatureLength;

        if (!sig.SequenceEqual(GifConstants.Signature87a) && !sig.SequenceEqual(GifConstants.Signature89a))
        {
            throw new InvalidDataException("无效的 GIF 签名");
        }
    }

    private void ParseLogicalScreenDescriptor()
    {
        _width = ReadU16();
        _height = ReadU16();
        var packed = _data[_position++];
        _backgroundColorIndex = _data[_position++];
        _pixelAspectRatio = _data[_position++];

        var hasGct = (packed & 0x80) != 0;
        if (hasGct)
        {
            var gctSize = 3 * (1 << ((packed & 0x07) + 1));
            _globalColorTable = new byte[gctSize];
            _data.Slice(_position, gctSize).CopyTo(_globalColorTable);
            _position += gctSize;
        }
    }

    #endregion

    #region 图像描述符解析

    private GifImageFrame ParseImageDescriptor()
    {
        var left = ReadU16();
        var top = ReadU16();
        var width = ReadU16();
        var height = ReadU16();
        var packed = _data[_position++];

        var hasLct = (packed & 0x80) != 0;
        var interlaced = (packed & 0x40) != 0;

        byte[] localColorTable = [];
        if (hasLct)
        {
            var lctSize = 3 * (1 << ((packed & 0x07) + 1));
            localColorTable = new byte[lctSize];
            _data.Slice(_position, lctSize).CopyTo(localColorTable);
            _position += lctSize;
        }

        var minCodeSize = _data[_position++];
        var compressedData = ReadSubBlocks();
        var indices = LzwDecompress(compressedData, minCodeSize, width * height);

        if (interlaced)
        {
            indices = Deinterlace(indices, width, height);
        }

        var frame = new GifImageFrame
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            LocalColorTable = localColorTable,
            DelayCentiseconds = _delayCentiseconds,
            DisposalMethod = _disposalMethod,
            HasTransparentColor = _hasTransparentColor,
            TransparentColorIndex = _transparentColorIndex,
            Interlaced = interlaced,
            Indices = indices
        };

        _delayCentiseconds = 0;
        _disposalMethod = GifDisposalMethod.None;
        _hasTransparentColor = false;
        _transparentColorIndex = 0;

        return frame;
    }

    #endregion

    #region 扩展块解析

    private void ParseExtension()
    {
        if (_position >= _data.Length) return;

        var label = _data[_position++];

        switch (label)
        {
            case GifConstants.GraphicControlLabel:
                ParseGraphicControlExtension();
                break;
            case GifConstants.ApplicationExtensionLabel:
                ParseApplicationExtension();
                break;
            default:
                SkipSubBlocks();
                break;
        }
    }

    private void ParseGraphicControlExtension()
    {
        var blockSize = _data[_position++];
        if (blockSize != 4)
        {
            _position += blockSize;
            return;
        }

        var packed = _data[_position++];
        _delayCentiseconds = ReadU16();
        _transparentColorIndex = _data[_position++];

        _disposalMethod = (GifDisposalMethod)((packed >> 2) & 0x07);
        _hasTransparentColor = (packed & 0x01) != 0;

        _position++;
    }

    private void ParseApplicationExtension()
    {
        var blockSize = _data[_position++];
        if (blockSize != 11)
        {
            _position += blockSize;
            SkipSubBlocks();
            return;
        }

        var appId = _data.Slice(_position, 11);
        _position += 11;

        if (appId.SequenceEqual(GifConstants.NetscapeAppId))
        {
            var subBlockSize = _data[_position++];
            if (subBlockSize >= 3)
            {
                _position++;
                _loopCount = ReadU16();
                _position += subBlockSize - 3;
            }
            _position++;
        }
        else
        {
            SkipSubBlocks();
        }
    }

    #endregion

    #region LZW 解压

    private static byte[] LzwDecompress(byte[] compressed, int minCodeSize, int totalPixels)
    {
        var clearCode = 1 << minCodeSize;
        var eoiCode = clearCode + 1;
        var codeSize = minCodeSize + 1;
        var nextCode = eoiCode + 1;
        var maxCode = 1 << codeSize;

        var output = new List<byte>(totalPixels);

        var prefixTable = new int[4096];
        var suffixTable = new byte[4096];
        var lengthTable = new int[4096];

        for (var i = 0; i < clearCode; i++)
        {
            prefixTable[i] = -1;
            suffixTable[i] = (byte)i;
            lengthTable[i] = 1;
        }

        var bitReader = new GifLzwBitReader(compressed);

        var code = bitReader.ReadCode(codeSize);
        if (code != clearCode)
        {
            if (code < clearCode)
            {
                output.Add((byte)code);
            }
        }

        code = bitReader.ReadCode(codeSize);
        var prevCode = code;

        while (code != eoiCode && output.Count < totalPixels)
        {
            if (code == clearCode)
            {
                codeSize = minCodeSize + 1;
                nextCode = eoiCode + 1;
                maxCode = 1 << codeSize;

                code = bitReader.ReadCode(codeSize);
                if (code == eoiCode) break;

                if (code < clearCode)
                {
                    output.Add((byte)code);
                }

                prevCode = code;
                code = bitReader.ReadCode(codeSize);
                continue;
            }

            if (code < nextCode)
            {
                var stack = new List<byte>();
                var c = code;
                while (c >= 0)
                {
                    stack.Add(suffixTable[c]);
                    c = prefixTable[c];
                }

                for (var i = stack.Count - 1; i >= 0; i--)
                {
                    output.Add(stack[i]);
                }
            }
            else
            {
                var stack = new List<byte>();
                var c = prevCode;
                while (c >= 0)
                {
                    stack.Add(suffixTable[c]);
                    c = prefixTable[c];
                }

                var firstByte = stack[^1];
                for (var i = stack.Count - 1; i >= 0; i--)
                {
                    output.Add(stack[i]);
                }
                output.Add(firstByte);
            }

            if (nextCode < 4096)
            {
                prefixTable[nextCode] = prevCode;

                var tempCode = code < nextCode ? code : prevCode;
                while (prefixTable[tempCode] >= 0)
                {
                    tempCode = prefixTable[tempCode];
                }
                suffixTable[nextCode] = suffixTable[tempCode];
                lengthTable[nextCode] = lengthTable[prevCode] + 1;

                nextCode++;
                if (nextCode > maxCode && codeSize < 12)
                {
                    codeSize++;
                    maxCode = 1 << codeSize;
                }
            }

            prevCode = code;
            code = bitReader.ReadCode(codeSize);
        }

        var result = new byte[totalPixels];
        var copyLen = Math.Min(output.Count, totalPixels);
        for (var i = 0; i < copyLen; i++)
        {
            result[i] = output[i];
        }
        return result;
    }

    #endregion

    #region 隔行扫描

    private static byte[] Deinterlace(byte[] indices, int width, int height)
    {
        var result = new byte[indices.Length];
        var passes = new (int Start, int Step)[]
        {
            (0, 8),
            (4, 8),
            (2, 4),
            (1, 2)
        };

        var srcRow = 0;
        foreach (var (start, step) in passes)
        {
            for (var y = start; y < height; y += step)
            {
                var srcOffset = srcRow * width;
                var dstOffset = y * width;
                if (srcOffset + width <= indices.Length)
                {
                    Array.Copy(indices, srcOffset, result, dstOffset, width);
                }
                srcRow++;
            }
        }

        return result;
    }

    #endregion

    #region 帧合成

    private static void CompositeFrame(byte[] canvas, GifImageFrame frame, byte[] palette, GifImageData gifData)
    {
        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                var canvasX = frame.Left + x;
                var canvasY = frame.Top + y;

                if (canvasX >= gifData.Width || canvasY >= gifData.Height) continue;

                var srcIdx = y * frame.Width + x;
                if (srcIdx >= frame.Indices.Length) continue;

                var colorIndex = frame.Indices[srcIdx];

                if (frame.HasTransparentColor && colorIndex == frame.TransparentColorIndex) continue;

                var dstIdx = (canvasY * gifData.Width + canvasX) * 4;

                var palOffset = colorIndex * 3;
                if (palOffset + 2 < palette.Length)
                {
                    canvas[dstIdx] = palette[palOffset];
                    canvas[dstIdx + 1] = palette[palOffset + 1];
                    canvas[dstIdx + 2] = palette[palOffset + 2];
                    canvas[dstIdx + 3] = 255;
                }
            }
        }
    }

    private static void ClearRegion(byte[] canvas, int left, int top, int width, int height)
    {
        for (var y = top; y < top + height; y++)
        {
            for (var x = left; x < left + width; x++)
            {
                var idx = (y * width + x) * 4;
                if (idx + 3 < canvas.Length)
                {
                    canvas[idx] = 0;
                    canvas[idx + 1] = 0;
                    canvas[idx + 2] = 0;
                    canvas[idx + 3] = 0;
                }
            }
        }
    }

    #endregion

    #region 辅助方法

    private int ReadU16()
    {
        var value = _data[_position] | (_data[_position + 1] << 8);
        _position += 2;
        return value;
    }

    private byte[] ReadSubBlocks()
    {
        var blocks = new List<byte>();

        while (_position < _data.Length)
        {
            var blockSize = _data[_position++];
            if (blockSize == 0) break;

            if (_position + blockSize > _data.Length) break;

            for (var i = 0; i < blockSize; i++)
            {
                blocks.Add(_data[_position++]);
            }
        }

        return blocks.ToArray();
    }

    private void SkipSubBlocks()
    {
        while (_position < _data.Length)
        {
            var blockSize = _data[_position++];
            if (blockSize == 0) break;
            _position += blockSize;
        }
    }

    #endregion
}

internal ref struct GifLzwBitReader
{
    private readonly byte[] _data;
    private int _bytePos;
    private int _bitPos;

    public GifLzwBitReader(byte[] data)
    {
        _data = data;
        _bytePos = 0;
        _bitPos = 0;
    }

    public int ReadCode(int codeSize)
    {
        var code = 0;
        for (var i = 0; i < codeSize; i++)
        {
            if (_bytePos >= _data.Length) return 0;

            var bit = (_data[_bytePos] >> _bitPos) & 1;
            code |= bit << i;

            _bitPos++;
            if (_bitPos >= 8)
            {
                _bitPos = 0;
                _bytePos++;
            }
        }
        return code;
    }
}

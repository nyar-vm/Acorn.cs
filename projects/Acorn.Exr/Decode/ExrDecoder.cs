using System.Buffers.Binary;
using System.Text;
using Acorn.Frame;
using Acorn.Exr.Data;

namespace Acorn.Exr.Decode;

/// <summary>
///     OpenEXR 解码器。
/// </summary>
public ref struct ExrDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="ExrDecoder" /> 结构的新实例。
    /// </summary>
    public ExrDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 EXR 文件。
    /// </summary>
    public ExrImageData Decode()
    {
        if (!_buffer.MatchMagic(ExrConstants.MagicNumber))
        {
            throw new InvalidDataException("EXR 魔数不匹配");
        }

        _buffer.ConsumeMagic(ExrConstants.MagicNumber);

        var version = _buffer.ReadU32LE();

        var channels = new List<ExrChannel>();
        ExrCompression compression = ExrCompression.None;
        ExrBox2i displayWindow = new();
        ExrBox2i dataWindow = new();

        while (!_buffer.IsEnd)
        {
            var attrName = ReadNullTerminatedString();

            if (string.IsNullOrEmpty(attrName)) break;

            var attrType = ReadNullTerminatedString();
            var attrSize = (int)_buffer.ReadU32LE();
            var attrEnd = _buffer.Position + attrSize;

            if (attrName == "channels" && attrType == "chlist")
            {
                channels = ReadChannelList(attrSize);
            }
            else if (attrName == "compression" && attrType == "compression")
            {
                compression = (ExrCompression)_buffer.ReadU8();
            }
            else if (attrName == "dataWindow" && attrType == "box2i")
            {
                dataWindow = ReadBox2i();
            }
            else if (attrName == "displayWindow" && attrType == "box2i")
            {
                displayWindow = ReadBox2i();
            }

            _buffer.Position = attrEnd;
        }

        return new ExrImageData
        {
            Width = displayWindow.XMax - displayWindow.XMin + 1,
            Height = displayWindow.YMax - displayWindow.YMin + 1,
            Channels = channels,
            Compression = compression,
            DisplayWindow = displayWindow,
            DataWindow = dataWindow
        };
    }

    private string ReadNullTerminatedString()
    {
        var start = _buffer.Position;

        while (_position < _buffer.Length && _buffer.Data[_position] != 0)
        {
            _position++;
        }

        var str = Encoding.ASCII.GetString(_buffer.Data[start.._position]);

        if (_position < _buffer.Length)
        {
            _position++;
        }

        return str;
    }

    private int _position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    private List<ExrChannel> ReadChannelList(int size)
    {
        var channels = new List<ExrChannel>();
        var end = _buffer.Position + size;

        while (_buffer.Position < end - 1)
        {
            var name = ReadNullTerminatedString();

            if (string.IsNullOrEmpty(name)) break;

            var pixelType = (ExrPixelType)_buffer.ReadI32LE();
            _buffer.Advance(12);

            channels.Add(new ExrChannel { Name = name, PixelType = pixelType });
        }

        return channels;
    }

    private ExrBox2i ReadBox2i()
    {
        var xMin = _buffer.ReadI32LE();
        var yMin = _buffer.ReadI32LE();
        var xMax = _buffer.ReadI32LE();
        var yMax = _buffer.ReadI32LE();

        return new ExrBox2i { XMin = xMin, YMin = yMin, XMax = xMax, YMax = yMax };
    }
}

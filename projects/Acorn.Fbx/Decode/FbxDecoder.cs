using System.Text;
using Acorn.Frame;
using Acorn.Fbx.Data;

namespace Acorn.Fbx.Decode;

/// <summary>
///     FBX 二进制文件解码器，将 Autodesk FBX 格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     FBX 是 Autodesk 的 3D 模型/动画交换格式，游戏行业事实标准。
///     解码器解析 FBX 二进制格式的节点层级结构。
/// </remarks>
public ref struct FbxDecoder
{
    private ByteBuffer _buffer;
    private int _version;

    /// <summary>
    ///     初始化 <see cref="FbxDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">FBX 二进制数据。</param>
    public FbxDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
        _version = 0;
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 FBX 二进制文件。
    /// </summary>
    /// <returns>FBX 文件数据。</returns>
    public FbxFileData Decode()
    {
        ReadFileHeader();

        var rootChildren = new List<FbxNode>();

        while (!_buffer.IsEnd)
        {
            var node = ReadNode();

            if (node == null)
            {
                break;
            }

            rootChildren.Add(node);
        }

        return new FbxFileData
        {
            Version = _version,
            Root = new FbxNode { Name = "Root", Children = rootChildren }
        };
    }

    /// <summary>
    ///     仅解码 FBX 文件头信息。
    /// </summary>
    public int DecodeHeader()
    {
        ReadFileHeader();
        return _version;
    }

    #region 私有解析方法

    private void ReadFileHeader()
    {
        if (_buffer.Remaining < FbxConstants.HeaderSize)
        {
            throw new InvalidDataException("FBX 文件数据过短，无法读取文件头");
        }

        var magic = _buffer.ReadString(FbxConstants.MagicLength);

        if (!magic.StartsWith("Kaydara FBX Binary"))
        {
            throw new InvalidDataException($"FBX 文件签名无效，期望 \"Kaydara FBX Binary\"，实际 \"{magic[..Math.Min(19, magic.Length)]}\"");
        }

        _version = (int)_buffer.ReadU32LE();
    }

    private FbxNode? ReadNode()
    {
        if (_buffer.Remaining < 4)
        {
            return null;
        }

        var endOffset = (int)_buffer.ReadU32LE();

        if (endOffset == 0)
        {
            return null;
        }

        if (endOffset > _buffer.Length)
        {
            return null;
        }

        var propertyCount = (int)_buffer.ReadU32LE();
        var propertyListLength = (int)_buffer.ReadU32LE();
        var nameLength = _buffer.ReadU8();

        if (nameLength == 0)
        {
            _buffer.Position = endOffset;
            return null;
        }

        var name = _buffer.ReadString(nameLength);
        var properties = ReadProperties(propertyCount, propertyListLength);

        var children = new List<FbxNode>();
        var childStart = _buffer.Position;

        while (_buffer.Position < endOffset - 13)
        {
            var child = ReadNode();

            if (child != null)
            {
                children.Add(child);
            }
            else
            {
                break;
            }
        }

        _buffer.Position = endOffset;

        return new FbxNode
        {
            Name = name,
            Properties = properties,
            Children = children
        };
    }

    private List<FbxProperty> ReadProperties(int count, int totalLength)
    {
        var properties = new List<FbxProperty>(count);
        var endPos = _buffer.Position + totalLength;

        for (var i = 0; i < count && _buffer.Position < endPos; i++)
        {
            properties.Add(ReadProperty());
        }

        return properties;
    }

    private FbxProperty ReadProperty()
    {
        var typeCode = _buffer.ReadU8();

        object? value = typeCode switch
        {
            FbxConstants.PropertyType.Boolean => _buffer.ReadU8() != 0,
            FbxConstants.PropertyType.Int8 => (short)_buffer.ReadU8(),
            FbxConstants.PropertyType.Int16 => _buffer.ReadI16LE(),
            FbxConstants.PropertyType.Int32 => _buffer.ReadI32LE(),
            FbxConstants.PropertyType.Int64 => _buffer.ReadI64LE(),
            FbxConstants.PropertyType.Float32 => _buffer.ReadF32LE(),
            FbxConstants.PropertyType.Float64 => _buffer.ReadF64LE(),
            FbxConstants.PropertyType.String => ReadStringProperty(),
            FbxConstants.PropertyType.RawBuffer => ReadRawProperty(),
            _ => ReadArrayProperty(typeCode)
        };

        return new FbxProperty { TypeCode = typeCode, Value = value };
    }

    private string ReadStringProperty()
    {
        var length = (int)_buffer.ReadU32LE();
        return _buffer.ReadString(length);
    }

    private byte[] ReadRawProperty()
    {
        var length = (int)_buffer.ReadU32LE();
        return _buffer.ReadBytes(length).ToArray();
    }

    private object ReadArrayProperty(byte typeCode)
    {
        var arrayType = (char)typeCode;

        if (arrayType != 'i' && arrayType != 'l' && arrayType != 'f' && arrayType != 'd' && arrayType != 'b')
        {
            return Array.Empty<byte>();
        }

        var count = (int)_buffer.ReadU32LE();
        var encoding = _buffer.ReadU32LE();
        var compressedLength = (int)_buffer.ReadU32LE();

        if (encoding == 0)
        {
            return arrayType switch
            {
                'i' => ReadInt32Array(count),
                'l' => ReadInt64Array(count),
                'f' => ReadFloat32Array(count),
                'd' => ReadFloat64Array(count),
                'b' => ReadBoolArray(count),
                _ => Array.Empty<byte>()
            };
        }

        var data = _buffer.ReadBytes(compressedLength).ToArray();

        return arrayType switch
        {
            'i' => data,
            'l' => data,
            'f' => data,
            'd' => data,
            _ => data
        };
    }

    private int[] ReadInt32Array(int count)
    {
        var result = new int[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = _buffer.ReadI32LE();
        }

        return result;
    }

    private long[] ReadInt64Array(int count)
    {
        var result = new long[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = _buffer.ReadI64LE();
        }

        return result;
    }

    private float[] ReadFloat32Array(int count)
    {
        var result = new float[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = _buffer.ReadF32LE();
        }

        return result;
    }

    private double[] ReadFloat64Array(int count)
    {
        var result = new double[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = _buffer.ReadF64LE();
        }

        return result;
    }

    private bool[] ReadBoolArray(int count)
    {
        var result = new bool[count];

        for (var i = 0; i < count; i++)
        {
            result[i] = _buffer.ReadU8() != 0;
        }

        return result;
    }

    #endregion
}

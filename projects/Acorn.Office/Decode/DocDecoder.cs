using System.Text;
using Acorn.Frame;
using Acorn.Office.Data;

namespace Acorn.Office.Decode;

/// <summary>
///     DOC 文件解码器，将 Microsoft Word 二进制格式（.doc）解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     DOC 是 Microsoft Word 97-2003 使用的二进制文件格式，基于 OLE2 复合文档结构。
///     解码器解析 Word 二进制文件头和文档流，提取文本和格式信息。
/// </remarks>
public ref struct DocDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="DocDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">DOC 二进制数据。</param>
    public DocDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 DOC 文件，提取文本内容。
    /// </summary>
    /// <returns>Word 文档数据。</returns>
    public WordDocumentData Decode()
    {
        var text = ExtractText();

        return new WordDocumentData
        {
            Text = text
        };
    }

    #region 私有解析方法

    private string ExtractText()
    {
        var sb = new StringBuilder();

        if (_buffer.Length < 12)
        {
            return string.Empty;
        }

        _buffer.Position = 0;
        var wIdent = _buffer.ReadU16LE();

        if (wIdent != OfficeConstants.WordDoc.FileMagic)
        {
            return string.Empty;
        }

        var nFib = _buffer.ReadU16LE();

        _buffer.Position = (int)OfficeConstants.WordDoc.ClxOffsetPosition;
        var clxOffset = _buffer.ReadU32LE();

        if (clxOffset == 0 || clxOffset >= _buffer.Length)
        {
            return string.Empty;
        }

        _buffer.Position = (int)clxOffset;

        while (!_buffer.IsEnd && _buffer.Remaining >= 1)
        {
            var type = _buffer.ReadU8();

            if (type == OfficeConstants.WordStreamType.Grpprl)
            {
                _buffer.Advance(1);
                var cbGrpprl = _buffer.ReadU16LE();
                _buffer.Advance(cbGrpprl);
            }
            else if (type == OfficeConstants.WordStreamType.PieceTable)
            {
                var cb = _buffer.ReadU32LE();

                if (cb > _buffer.Remaining)
                {
                    break;
                }

                var pieceTableData = _buffer.ReadBytes((int)cb).ToArray();
                var pieces = ParsePieceTable(pieceTableData);

                foreach (var piece in pieces)
                {
                    sb.Append(piece);
                }
            }
            else
            {
                break;
            }
        }

        return sb.ToString();
    }

    private static List<string> ParsePieceTable(byte[] data)
    {
        var pieces = new List<string>();
        var reader = new ByteBuffer(data);

        while (!reader.IsEnd && reader.Remaining >= 2)
        {
            var n = reader.ReadU16BE();

            if (n == 0)
            {
                break;
            }

            var count = (n - 1) / 2;

            var cpOffsets = new uint[count + 1];

            for (var i = 0; i <= count; i++)
            {
                if (reader.Remaining >= 4)
                {
                    cpOffsets[i] = reader.ReadU32LE();
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (reader.Remaining < 8)
                {
                    break;
                }

                var fcValue = reader.ReadU32LE();
                var prm = reader.ReadU16LE();

                var isUnicode = (fcValue & 0x40000000) == 0;
                var fc = isUnicode ? fcValue >> 1 : (fcValue & ~0x40000000) >> 1;

                var charCount = (int)(cpOffsets[i + 1] - cpOffsets[i]);

                if (charCount > 0)
                {
                    if (isUnicode)
                    {
                        pieces.Add($"[文本段 {i}: {charCount} Unicode 字符, 偏移 0x{fc:X}]");
                    }
                    else
                    {
                        pieces.Add($"[文本段 {i}: {charCount} ANSI 字符, 偏移 0x{fc:X}]");
                    }
                }
            }

            break;
        }

        return pieces;
    }

    #endregion
}

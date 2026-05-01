using System.Text;
using Acorn.Frame;
using Acorn.Office.Data;

namespace Acorn.Office.Decode;

/// <summary>
///     PPT 文件解码器，将 Microsoft PowerPoint 二进制格式（.ppt）解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     PPT 是 Microsoft PowerPoint 97-2003 使用的二进制文件格式，基于 OLE2 复合文档结构。
///     解码器解析 PowerPoint 文档流，提取幻灯片、文本和元信息。
/// </remarks>
public ref struct PptDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="PptDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">PPT 二进制数据。</param>
    public PptDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 PPT 文件，提取演示文稿数据。
    /// </summary>
    /// <returns>PPT 演示文稿数据。</returns>
    public PowerPointData Decode()
    {
        var slideTexts = new List<string>();

        while (!_buffer.IsEnd && _buffer.Remaining >= 8)
        {
            var recordType = _buffer.ReadU16LE();
            var recordVersion = (byte)(recordType & 0x000F);
            var recordInstance = (ushort)((recordType >> 4) & 0x0FFF);
            recordType = _buffer.ReadU16LE();
            var recordLength = _buffer.ReadU32LE();

            if (_buffer.Remaining < recordLength)
            {
                break;
            }

            var recordData = _buffer.ReadBytes((int)recordLength);

            if (recordType == OfficeConstants.PptRecordType.TextCharsAtom)
            {
                var text = ParseTextRecord(recordData);

                if (!string.IsNullOrEmpty(text))
                {
                    slideTexts.Add(text);
                }
            }
        }

        return new PowerPointData
        {
            Slides = slideTexts
        };
    }

    #region 私有解析方法

    private static string ParseTextRecord(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
        {
            return string.Empty;
        }

        var reader = new ByteBuffer(data);
        var textLength = reader.ReadI32LE();

        if (textLength <= 0 || reader.Remaining < textLength * 2)
        {
            return string.Empty;
        }

        var textBytes = reader.ReadBytes(textLength * 2).ToArray();
        return Encoding.Unicode.GetString(textBytes);
    }

    #endregion
}

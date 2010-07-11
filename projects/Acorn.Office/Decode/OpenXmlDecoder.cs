using Acorn.Office.Data;
using Acorn.Zip.Decode;

namespace Acorn.Office.Decode;

/// <summary>
///     Open XML 格式解码器，解析 Office Open XML 格式（.xlsx, .docx, .pptx）。
/// </summary>
/// <remarks>
///     Open XML 格式本质上是 ZIP 压缩包，内部包含 XML 文件。
///     本解码器使用 Acorn.Zip 解析 ZIP 结构，提取其中的 XML 内容。
/// </remarks>
public sealed class OpenXmlDecoder
{
    private readonly ZipDecoder _zipDecoder;
    
    /// <summary>
    ///     初始化 <see cref="OpenXmlDecoder" /> 类的新实例。
    /// </summary>
    public OpenXmlDecoder()
    {
        _zipDecoder = new ZipDecoder();
    }
    
    /// <summary>
    ///     从 Open XML 二进制数据解码工作簿（.xlsx）。
    /// </summary>
    /// <param name="data">Open XML 二进制数据。</param>
    /// <returns>解码后的工作簿数据。</returns>
    public ExcelWorkbookData DecodeExcel(byte[] data)
    {
        var zipFile = _zipDecoder.Decode(data);
        
        var sheets = new List<ExcelSheetData>();
        
        // 查找工作簿文件
        var workbookEntry = zipFile.Entries.FirstOrDefault(e => e.Name == "xl/workbook.xml");
        if (workbookEntry != null)
        {
            // 提取工作表名称
            var sheetNames = ExtractSheetNames(workbookEntry.Data);
            
            foreach (var sheetName in sheetNames)
            {
                // 查找对应的工作表文件
                var sheetPath = $"xl/worksheets/{sheetName}.xml";
                var sheetEntry = zipFile.Entries.FirstOrDefault(e => e.Name == sheetPath);
                
                if (sheetEntry != null)
                {
                    var rows = ExtractSheetData(sheetEntry.Data);
                    sheets.Add(new ExcelSheetData
                    {
                        Name = sheetName,
                        RowCount = rows.Count,
                        ColumnCount = rows.Count > 0 ? rows.Max(r => r.Cells.Count) : 0,
                        Rows = rows
                    });
                }
            }
        }
        
        return new ExcelWorkbookData
        {
            Sheets = sheets
        };
    }
    
    /// <summary>
    ///     从 Open XML 二进制数据解码文档（.docx）。
    /// </summary>
    /// <param name="data">Open XML 二进制数据。</param>
    /// <returns>解码后的文档数据。</returns>
    public WordDocumentData DecodeWord(byte[] data)
    {
        var zipFile = _zipDecoder.Decode(data);
        
        // 查找文档内容文件
        var documentEntry = zipFile.Entries.FirstOrDefault(e => e.Name == "word/document.xml");
        if (documentEntry != null)
        {
            var text = ExtractTextFromXml(documentEntry.Data);
            var paragraphs = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            
            return new WordDocumentData
            {
                Text = text,
                Paragraphs = paragraphs
            };
        }
        
        return new WordDocumentData { Text = string.Empty, Paragraphs = [] };
    }
    
    /// <summary>
    ///     从 Open XML 二进制数据解码演示文稿（.pptx）。
    /// </summary>
    /// <param name="data">Open XML 二进制数据。</param>
    /// <returns>解码后的演示文稿数据。</returns>
    public PowerPointData DecodePowerPoint(byte[] data)
    {
        var zipFile = _zipDecoder.Decode(data);
        
        var slides = new List<string>();
        
        // 查找所有幻灯片文件
        var slideEntries = zipFile.Entries.Where(e => e.Name.StartsWith("ppt/slides/slide") && e.Name.EndsWith(".xml"));
        
        foreach (var slideEntry in slideEntries)
        {
            var slideText = ExtractTextFromXml(slideEntry.Data);
            if (!string.IsNullOrEmpty(slideText))
            {
                slides.Add(slideText);
            }
        }
        
        return new PowerPointData { Slides = slides };
    }
    
    /// <summary>
    ///     提取工作表名称。
    /// </summary>
    private List<string> ExtractSheetNames(byte[] xmlData)
    {
        // 简化实现，实际应该解析 XML
        var xml = System.Text.Encoding.UTF8.GetString(xmlData);
        var names = new List<string>();
        
        // 简单的字符串匹配提取 sheet 名称
        var sheetTag = "sheet name=\"";
        var index = 0;
        while ((index = xml.IndexOf(sheetTag, index)) != -1)
        {
            index += sheetTag.Length;
            var endIndex = xml.IndexOf("\"", index);
            if (endIndex != -1)
            {
                names.Add(xml.Substring(index, endIndex - index));
            }
        }
        
        return names;
    }
    
    /// <summary>
    ///     提取工作表数据。
    /// </summary>
    private List<ExcelRowData> ExtractSheetData(byte[] xmlData)
    {
        // 简化实现，实际应该解析 XML
        var xml = System.Text.Encoding.UTF8.GetString(xmlData);
        var rows = new List<ExcelRowData>();
        
        // 简单的字符串匹配提取行数据
        var rowTag = "<row";
        var cellTag = "<c";
        var valueTag = "<v>";
        
        var rowIndex = 0;
        var index = 0;
        while ((index = xml.IndexOf(rowTag, index)) != -1)
        {
            index += rowTag.Length;
            var endIndex = xml.IndexOf("</row>", index);
            if (endIndex == -1) break;
            
            var rowXml = xml.Substring(index, endIndex - index);
            var cells = new List<ExcelCellData>();
            var cellIndex = 0;
            var cellPos = 0;
            
            while ((cellPos = rowXml.IndexOf(cellTag, cellPos)) != -1)
            {
                cellPos += cellTag.Length;
                var valuePos = rowXml.IndexOf(valueTag, cellPos);
                if (valuePos != -1)
                {
                    valuePos += valueTag.Length;
                    var valueEnd = rowXml.IndexOf("</v>", valuePos);
                    if (valueEnd != -1)
                    {
                        var value = rowXml.Substring(valuePos, valueEnd - valuePos);
                        cells.Add(new ExcelCellData
                        {
                            Row = rowIndex,
                            Column = cellIndex,
                            Value = value,
                            CellReference = $"{GetColumnName(cellIndex)}{rowIndex + 1}"
                        });
                        cellIndex++;
                    }
                }
            }
            
            rows.Add(new ExcelRowData
            {
                RowIndex = rowIndex,
                Cells = cells
            });
            rowIndex++;
        }
        
        return rows;
    }
    
    /// <summary>
    ///     从 XML 数据提取文本内容。
    /// </summary>
    private string ExtractTextFromXml(byte[] xmlData)
    {
        // 简化实现，实际应该解析 XML
        var xml = System.Text.Encoding.UTF8.GetString(xmlData);
        var text = new System.Text.StringBuilder();
        
        // 简单的字符串匹配提取文本
        var textTag = "<w:t>";
        var index = 0;
        while ((index = xml.IndexOf(textTag, index)) != -1)
        {
            index += textTag.Length;
            var endIndex = xml.IndexOf("</w:t>", index);
            if (endIndex != -1)
            {
                text.Append(xml.Substring(index, endIndex - index));
                text.Append(" ");
            }
        }
        
        return text.ToString().Trim();
    }
    
    /// <summary>
    ///     获取列名称（如 A, B, C...）。
    /// </summary>
    private string GetColumnName(int index)
    {
        var name = new System.Text.StringBuilder();
        var c = index;
        do
        {
            name.Insert(0, (char)('A' + c % 26));
            c = c / 26 - 1;
        } while (c >= 0);
        return name.ToString();
    }
}
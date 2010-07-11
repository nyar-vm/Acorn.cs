# Acorn.Office

Acorn Office 格式库，提供 Office 97-2003 格式（.xls, .doc, .ppt）的扫描、编码和解码功能。

## 功能特性

- **XLS 文件解码**：支持解析 Excel 97-2003 (.xls) 格式
- **XLS 文件编码**：支持生成 Excel 97-2003 (.xls) 格式
- **DOC 文件解码**：支持解析 Word 97-2003 (.doc) 格式
- **PPT 文件解码**：支持解析 PowerPoint 97-2003 (.ppt) 格式
- **Open XML 文件解码**：支持解析 Office Open XML (.xlsx, .docx, .pptx) 格式
- **XLS 文件扫描**：快速扫描 .xls 文件结构
- **轻量级实现**：不依赖第三方库，纯 C# 实现
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口
- **借助 Acorn.Zip**：Open XML 格式通过 Acorn.Zip 解析 ZIP 结构

## 安装

```bash
dotnet add package Acorn.Office
```

## 使用示例

### 解码 XLS 文件

```csharp
using Acorn.Office.Decode;

var data = File.ReadAllBytes("example.xls");

var decoder = new XlsDecoder();
var workbook = decoder.Decode(data);

Console.WriteLine($"Workbook contains {workbook.Sheets.Count} sheets:");
foreach (var sheet in workbook.Sheets)
{
    Console.WriteLine($"- {sheet.Name}: {sheet.RowCount} rows, {sheet.ColumnCount} columns");
}
```

### 编码 XLS 文件

```csharp
using Acorn.Office.Encode;
using Acorn.Office.Data;

var workbook = new ExcelWorkbookData
{
    Sheets = new List<ExcelSheetData>
    {
        new ExcelSheetData
        {
            Name = "Sheet1",
            RowCount = 2,
            ColumnCount = 2,
            Rows = new List<ExcelRowData>
            {
                new ExcelRowData
                {
                    RowIndex = 0,
                    Cells = new List<ExcelCellData>
                    {
                        new ExcelCellData { Column = 0, Row = 0, Value = "Name", CellReference = "A1" },
                        new ExcelCellData { Column = 1, Row = 0, Value = "Age", CellReference = "B1" }
                    }
                },
                new ExcelRowData
                {
                    RowIndex = 1,
                    Cells = new List<ExcelCellData>
                    {
                        new ExcelCellData { Column = 0, Row = 1, Value = "John", CellReference = "A2" },
                        new ExcelCellData { Column = 1, Row = 1, Value = "30", CellReference = "B2" }
                    }
                }
            }
        }
    }
};

var encoder = new XlsEncoder();
var data = encoder.Encode(workbook);

File.WriteAllBytes("output.xls", data);
Console.WriteLine("XLS file created successfully!");
```

### 解码 DOC 文件

```csharp
using Acorn.Office.Decode;

var data = File.ReadAllBytes("example.doc");

var decoder = new DocDecoder();
var document = decoder.Decode(data);

Console.WriteLine($"Document text: {document.Text}");
Console.WriteLine($"Paragraphs: {document.Paragraphs.Count}");
```

### 解码 PPT 文件

```csharp
using Acorn.Office.Decode;

var data = File.ReadAllBytes("example.ppt");

var decoder = new PptDecoder();
var presentation = decoder.Decode(data);

Console.WriteLine($"Slides: {presentation.SlideCount}");
foreach (var slide in presentation.Slides)
{
    Console.WriteLine($"- {slide}");
}
```

### 解码 Open XML 文件（.xlsx, .docx, .pptx）

```csharp
using Acorn.Office.Decode;

// 解码 .xlsx 文件
var xlsxData = File.ReadAllBytes("example.xlsx");
var openXmlDecoder = new OpenXmlDecoder();
var workbook = openXmlDecoder.DecodeExcel(xlsxData);

Console.WriteLine($"Workbook contains {workbook.Sheets.Count} sheets:");
foreach (var sheet in workbook.Sheets)
{
    Console.WriteLine($"- {sheet.Name}: {sheet.RowCount} rows, {sheet.ColumnCount} columns");
}

// 解码 .docx 文件
var docxData = File.ReadAllBytes("example.docx");
var document = openXmlDecoder.DecodeWord(docxData);
Console.WriteLine($"Document text: {document.Text}");

// 解码 .pptx 文件
var pptxData = File.ReadAllBytes("example.pptx");
var presentation = openXmlDecoder.DecodePowerPoint(pptxData);
Console.WriteLine($"Slides: {presentation.SlideCount}");
```

### 扫描 XLS 文件

```csharp
using Acorn.Office.Scanner;

var data = File.ReadAllBytes("example.xls");

var scanResult = XlsScanner.Scan(data);
Console.WriteLine(scanResult);
```

## 项目结构

- `Acorn.Office/`
  - `Data/` - 数据结构
    - `OfficeConstants.cs` - Office 格式常量定义
    - `OfficeData.cs` - Office 数据结构（Excel, Word, PowerPoint）
  - `Decode/` - 解码器
    - `XlsDecoder.cs` - Excel 97-2003 文件解码器
    - `DocDecoder.cs` - Word 97-2003 文件解码器
    - `PptDecoder.cs` - PowerPoint 97-2003 文件解码器
    - `OpenXmlDecoder.cs` - Office Open XML 文件解码器（借助 Acorn.Zip）
  - `Encode/` - 编码器
    - `XlsEncoder.cs` - Excel 97-2003 文件编码器
  - `Scanner/` - 扫描器
    - `XlsScanner.cs` - Excel 文件扫描器

## 支持的格式

- **Excel (.xls)**：Excel 97-2003 格式
- **Word (.doc)**：Word 97-2003 格式
- **PowerPoint (.ppt)**：PowerPoint 97-2003 格式
- **Excel (.xlsx)**：Office Open XML 格式（借助 Acorn.Zip）
- **Word (.docx)**：Office Open XML 格式（借助 Acorn.Zip）
- **PowerPoint (.pptx)**：Office Open XML 格式（借助 Acorn.Zip）

## 注意事项

- Office 97-2003 格式（.xls, .doc, .ppt）使用原生二进制解析
- Office Open XML 格式（.xlsx, .docx, .pptx）借助 Acorn.Zip 解析 ZIP 结构
- Open XML 格式的 XML 内容处理可结合 Oak.Xml 库使用

## 依赖

- .NET 11.0+
- Acorn.Core

## 许可证

MPL-2.0

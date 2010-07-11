namespace Acorn.Office.Data;

/// <summary>
///     Excel 单元格数据。
/// </summary>
public sealed class ExcelCellData
{
    /// <summary>
    ///     列索引（从 0 开始）。
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    ///     行索引（从 0 开始）。
    /// </summary>
    public int Row { get; init; }

    /// <summary>
    ///     单元格值。
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    ///     单元格引用（例如 "A1"）。
    /// </summary>
    public string? CellReference { get; init; }
}

/// <summary>
///     Excel 行数据。
/// </summary>
public sealed class ExcelRowData
{
    /// <summary>
    ///     行索引（从 0 开始）。
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    ///     该行包含的单元格列表。
    /// </summary>
    public IReadOnlyList<ExcelCellData> Cells { get; init; } = [];
}

/// <summary>
///     Excel 工作表数据。
/// </summary>
public sealed class ExcelSheetData
{
    /// <summary>
    ///     工作表名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     行数。
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    ///     列数。
    /// </summary>
    public int ColumnCount { get; init; }

    /// <summary>
    ///     工作表包含的行列表。
    /// </summary>
    public IReadOnlyList<ExcelRowData> Rows { get; init; } = [];
}

/// <summary>
///     Excel 工作簿数据。
/// </summary>
public sealed class ExcelWorkbookData
{
    /// <summary>
    ///     工作簿包含的工作表列表。
    /// </summary>
    public IReadOnlyList<ExcelSheetData> Sheets { get; init; } = [];
}

/// <summary>
///     Word 文档数据。
/// </summary>
public sealed class WordDocumentData
{
    /// <summary>
    ///     文档文本内容。
    /// </summary>
    public string Text { get; init; } = string.Empty;
    
    /// <summary>
    ///     段落列表。
    /// </summary>
    public IReadOnlyList<string> Paragraphs { get; init; } = [];
}

/// <summary>
///     PowerPoint 演示文稿数据。
/// </summary>
public sealed class PowerPointData
{
    /// <summary>
    ///     幻灯片文本内容列表。
    /// </summary>
    public IReadOnlyList<string> Slides { get; init; } = [];
    
    /// <summary>
    ///     幻灯片数量。
    /// </summary>
    public int SlideCount => Slides.Count;
}
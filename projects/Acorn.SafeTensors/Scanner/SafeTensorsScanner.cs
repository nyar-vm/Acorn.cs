using System.Text;
using System.Text.Json;
using Acorn.Frame;
using Acorn.SafeTensors.Data;

namespace Acorn.SafeTensors.Scanner;

/// <summary>
///     SafeTensors 格式扫描器，基于 <see cref="SpanScanner" /> 提供零分配的快速 SafeTensors 文件探查。
/// </summary>
/// <remarks>
///     SafeTensors 格式：8 字节头长度（小端序 uint64）+ JSON 头 + 二进制张量数据。
///     扫描器只读取 JSON 头，不加载张量数据，实现快速元数据探查。
/// </remarks>
public ref struct SafeTensorsScanner
{
    private SpanScanner _scanner;

    public SafeTensorsScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 SafeTensors 文件头，提取统计信息。
    /// </summary>
    /// <returns>SafeTensors 统计信息。</returns>
    public SafeTensorsStatistics ScanStatistics()
    {
        if (_scanner.Length < 8)
        {
            throw new InvalidDataException("SafeTensors 文件数据过短，无法读取头长度");
        }

        var headerLength = _scanner.Buffer.ReadU64LE();

        if (8 + (long)headerLength > _scanner.Length)
        {
            throw new InvalidDataException("SafeTensors 文件头超出数据范围");
        }

        var headerJson = _scanner.Buffer.ReadString((int)headerLength);

        var tensorCount = 0;
        var totalParameters = 0L;
        var tensorNames = new List<string>();
        var dtypes = new HashSet<string>();

        using var doc = JsonDocument.Parse(headerJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name == "__metadata__")
            {
                continue;
            }

            tensorCount++;
            tensorNames.Add(property.Name);

            if (property.Value.TryGetProperty("dtype", out var dtypeEl))
            {
                dtypes.Add(dtypeEl.GetString() ?? "F32");
            }

            if (property.Value.TryGetProperty("shape", out var shapeEl))
            {
                var paramCount = 1L;

                foreach (var dim in shapeEl.EnumerateArray())
                {
                    paramCount *= dim.GetInt64();
                }

                totalParameters += paramCount;
            }
        }

        return new SafeTensorsStatistics
        {
            HeaderSize = headerLength,
            DataSize = (ulong)_scanner.Length - 8 - headerLength,
            TensorCount = tensorCount,
            TotalParameters = totalParameters,
            TensorNames = tensorNames,
            DTypes = dtypes.ToList()
        };
    }

    /// <summary>
    ///     扫描 SafeTensors 文件头，提取张量名称列表。
    /// </summary>
    /// <returns>张量名称列表。</returns>
    public List<string> ScanTensorNames()
    {
        if (_scanner.Length < 8)
        {
            throw new InvalidDataException("SafeTensors 文件数据过短");
        }

        var headerLength = _scanner.Buffer.ReadU64LE();
        var headerJson = _scanner.Buffer.ReadString((int)headerLength);

        var names = new List<string>();

        using var doc = JsonDocument.Parse(headerJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name != "__metadata__")
            {
                names.Add(property.Name);
            }
        }

        return names;
    }
}

/// <summary>
///     SafeTensors 文件统计信息。
/// </summary>
public sealed class SafeTensorsStatistics
{
    /// <summary>
    ///     JSON 头大小（字节）。
    /// </summary>
    public ulong HeaderSize { get; init; }

    /// <summary>
    ///     数据区大小（字节）。
    /// </summary>
    public ulong DataSize { get; init; }

    /// <summary>
    ///     张量数量。
    /// </summary>
    public int TensorCount { get; init; }

    /// <summary>
    ///     总参数量。
    /// </summary>
    public long TotalParameters { get; init; }

    /// <summary>
    ///     张量名称列表。
    /// </summary>
    public IReadOnlyList<string> TensorNames { get; init; } = [];

    /// <summary>
    ///     使用的数据类型列表。
    /// </summary>
    public IReadOnlyList<string> DTypes { get; init; } = [];
}

using System.Text;
using System.Text.Json;
using Acorn.Frame;
using Acorn.Gltf.Data;

namespace Acorn.Gltf.Scanner;

/// <summary>
///     GLTF / GLB 模型扫描器，基于 <see cref="SpanScanner" /> 提供对 GLTF JSON 和 GLB 二进制文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     GLTF 使用 JSON 格式描述场景，GLB 是 GLTF 的二进制容器格式。
///     扫描器支持两种格式，快速提取版本、场景统计、缓冲区大小等元信息。
/// </remarks>
public ref struct GltfScanner
{
    private static ReadOnlySpan<byte> GlbMagic => GltfConstants.GlbMagicNumber;

    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="GltfScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 GLTF/GLB 字节数据。</param>
    public GltfScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     判断数据是否为 GLB 二进制格式。
    /// </summary>
    /// <returns>如果是 GLB 格式则返回 true。</returns>
    public bool IsGlbFormat()
    {
        return _scanner.MatchMagic(GlbMagic);
    }

    /// <summary>
    ///     扫描 GLB 文件头，提取版本和长度信息。
    /// </summary>
    /// <returns>包含版本、文件长度和 JSON 块长度的元组。</returns>
    public (uint Version, uint TotalLength, uint JsonChunkLength, uint JsonChunkType) ScanGlbHeader()
    {
        if (!IsGlbFormat())
        {
            throw new InvalidDataException("数据不是有效的 GLB 格式");
        }

        _scanner.ConsumeMagic(GlbMagic);
        var version = _scanner.Buffer.ReadU32LE();
        var totalLength = _scanner.Buffer.ReadU32LE();
        var jsonChunkLength = _scanner.Buffer.ReadU32LE();
        var jsonChunkType = _scanner.Buffer.ReadU32LE();

        return (version, totalLength, jsonChunkLength, jsonChunkType);
    }

    /// <summary>
    ///     扫描 GLTF/GLB 文件，提取场景统计信息。
    /// </summary>
    /// <returns>场景统计信息。</returns>
    public GltfStatistics ScanStatistics()
    {
        string jsonContent;

        if (IsGlbFormat())
        {
            var (_, _, jsonChunkLength, _) = ScanGlbHeader();
            var jsonBytes = _scanner.Buffer.ReadBytes((int)jsonChunkLength);
            jsonContent = Encoding.UTF8.GetString(jsonBytes);
        }
        else
        {
            jsonContent = Encoding.UTF8.GetString(_scanner.Data);
        }

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        var asset = root.GetProperty("asset");
        var version = asset.GetProperty("version").GetString() ?? "2.0";

        var sceneCount = root.TryGetProperty("scenes", out var scenes) ? scenes.GetArrayLength() : 0;
        var nodeCount = root.TryGetProperty("nodes", out var nodes) ? nodes.GetArrayLength() : 0;
        var meshCount = root.TryGetProperty("meshes", out var meshes) ? meshes.GetArrayLength() : 0;
        var materialCount = root.TryGetProperty("materials", out var materials) ? materials.GetArrayLength() : 0;
        var textureCount = root.TryGetProperty("textures", out var textures) ? textures.GetArrayLength() : 0;
        var imageCount = root.TryGetProperty("images", out var images) ? images.GetArrayLength() : 0;
        var animationCount = root.TryGetProperty("animations", out var animations) ? animations.GetArrayLength() : 0;
        var skinCount = root.TryGetProperty("skins", out var skins) ? skins.GetArrayLength() : 0;

        var totalBufferSize = 0L;

        if (root.TryGetProperty("buffers", out var buffers))
        {
            foreach (var buffer in buffers.EnumerateArray())
            {
                if (buffer.TryGetProperty("byteLength", out var byteLength))
                {
                    totalBufferSize += byteLength.GetInt64();
                }
            }
        }

        var hasBinaryChunk = false;

        if (IsGlbFormat())
        {
            var (_, _, jsonChunkLength, _) = ScanGlbHeader();
            hasBinaryChunk = _scanner.Length > 20 + (int)jsonChunkLength;
        }

        return new GltfStatistics
        {
            Version = version,
            SceneCount = sceneCount,
            NodeCount = nodeCount,
            MeshCount = meshCount,
            MaterialCount = materialCount,
            TextureCount = textureCount,
            ImageCount = imageCount,
            AnimationCount = animationCount,
            SkinCount = skinCount,
            TotalBufferSize = totalBufferSize,
            HasBinaryChunk = hasBinaryChunk
        };
    }

    /// <summary>
    ///     扫描 GLTF/GLB 文件，提取外部资源引用列表。
    /// </summary>
    /// <returns>外部资源 URI 列表。</returns>
    public List<string> ScanExternalResources()
    {
        var resources = new List<string>();
        string jsonContent;

        if (IsGlbFormat())
        {
            var (_, _, jsonChunkLength, _) = ScanGlbHeader();
            var jsonBytes = _scanner.Buffer.ReadBytes((int)jsonChunkLength);
            jsonContent = Encoding.UTF8.GetString(jsonBytes);
        }
        else
        {
            jsonContent = Encoding.UTF8.GetString(_scanner.Data);
        }

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        if (root.TryGetProperty("buffers", out var buffers))
        {
            foreach (var buffer in buffers.EnumerateArray())
            {
                if (buffer.TryGetProperty("uri", out var uri) && !IsDataUri(uri.GetString()!))
                {
                    resources.Add(uri.GetString()!);
                }
            }
        }

        if (root.TryGetProperty("images", out var images))
        {
            foreach (var image in images.EnumerateArray())
            {
                if (image.TryGetProperty("uri", out var uri) && !IsDataUri(uri.GetString()!))
                {
                    resources.Add(uri.GetString()!);
                }
            }
        }

        return resources;
    }

    private static bool IsDataUri(string uri)
    {
        return uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
///     GLTF 文件统计信息。
/// </summary>
public sealed class GltfStatistics
{
    /// <summary>
    ///     GLTF 版本。
    /// </summary>
    public string Version { get; init; } = "2.0";

    /// <summary>
    ///     场景数量。
    /// </summary>
    public int SceneCount { get; init; }

    /// <summary>
    ///     节点数量。
    /// </summary>
    public int NodeCount { get; init; }

    /// <summary>
    ///     网格数量。
    /// </summary>
    public int MeshCount { get; init; }

    /// <summary>
    ///     材质数量。
    /// </summary>
    public int MaterialCount { get; init; }

    /// <summary>
    ///     纹理数量。
    /// </summary>
    public int TextureCount { get; init; }

    /// <summary>
    ///     图像数量。
    /// </summary>
    public int ImageCount { get; init; }

    /// <summary>
    ///     动画数量。
    /// </summary>
    public int AnimationCount { get; init; }

    /// <summary>
    ///     蒙皮数量。
    /// </summary>
    public int SkinCount { get; init; }

    /// <summary>
    ///     缓冲区总大小（字节）。
    /// </summary>
    public long TotalBufferSize { get; init; }

    /// <summary>
    ///     是否包含二进制数据块（仅 GLB）。
    /// </summary>
    public bool HasBinaryChunk { get; init; }
}

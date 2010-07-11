using System.Text;
using System.Text.Json;
using Acorn.Frame;
using Acorn.Gltf.Data;

namespace Acorn.Gltf.Decode;

/// <summary>
///     GLTF / GLB 模型解码器，将 GLTF JSON 或 GLB 二进制格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     GLTF 使用 JSON 格式描述场景，GLB 是 GLTF 的二进制容器格式。
///     解码器支持两种输入格式，解析为统一的 C# 数据结构。
/// </remarks>
public sealed class GltfDecoder
{
    /// <summary>
    ///     从 GLTF JSON 字符串解码模型数据。
    /// </summary>
    /// <param name="jsonContent">JSON 内容。</param>
    /// <returns>解码后的模型数据。</returns>
    public GltfModelData DecodeJson(string jsonContent)
    {
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        return new GltfModelData
        {
            Asset = ParseAsset(root.GetProperty("asset")),
            Scene = root.TryGetProperty("scene", out var scene) ? scene.GetInt32() : null,
            Scenes = ParseArray(root, "scenes", ParseScene),
            Nodes = ParseArray(root, "nodes", ParseNode),
            Meshes = ParseArray(root, "meshes", ParseMesh),
            Buffers = ParseArray(root, "buffers", ParseBuffer),
            BufferViews = ParseArray(root, "bufferViews", ParseBufferView),
            Accessors = ParseArray(root, "accessors", ParseAccessor),
            Materials = ParseArray(root, "materials", ParseMaterial),
            Textures = ParseArray(root, "textures", ParseTexture),
            Images = ParseArray(root, "images", ParseImage),
            Samplers = ParseArray(root, "samplers", ParseSampler),
            Skins = ParseArray(root, "skins", ParseSkin),
            Animations = ParseArray(root, "animations", ParseAnimation)
        };
    }

    /// <summary>
    ///     从 GLB 二进制数据解码模型数据。
    /// </summary>
    /// <param name="data">GLB 二进制数据。</param>
    /// <returns>解码后的模型数据。</returns>
    public GltfModelData DecodeGlb(ReadOnlySpan<byte> data)
    {
        if (data.Length < 20)
        {
            throw new InvalidDataException("GLB 文件数据过短");
        }

        var buffer = new ByteBuffer(data);

        if (!GlbHeader.TryRead(ref buffer, out var header))
        {
            throw new InvalidDataException("GLB 文件头部读取失败");
        }

        if (!header.Magic.AsSpan().SequenceEqual(GltfConstants.GlbMagicNumber))
        {
            throw new InvalidDataException("GLB 文件魔数不匹配");
        }

        if (!GlbChunkHeader.TryRead(ref buffer, out var jsonChunkHeader))
        {
            throw new InvalidDataException("GLB 文件 JSON 块头部读取失败");
        }

        if (jsonChunkHeader.Type != GltfConstants.ChunkTypeJson)
        {
            throw new InvalidDataException("GLB 文件第一个块必须是 JSON 类型");
        }

        var jsonBytes = buffer.ReadBytes((int)jsonChunkHeader.Length);
        var jsonContent = Encoding.UTF8.GetString(jsonBytes);

        return DecodeJson(jsonContent);
    }

    #region 私有解析方法

    private static IReadOnlyList<T> ParseArray<T>(JsonElement root, string propertyName, Func<JsonElement, T> parser)
    {
        if (!root.TryGetProperty(propertyName, out var array))
        {
            return [];
        }

        var result = new List<T>(array.GetArrayLength());

        foreach (var element in array.EnumerateArray())
        {
            result.Add(parser(element));
        }

        return result;
    }

    private static GltfAsset ParseAsset(JsonElement element)
    {
        return new GltfAsset
        {
            Version = element.GetProperty("version").GetString() ?? "2.0",
            Generator = element.TryGetProperty("generator", out var generator) ? generator.GetString() : null,
            Copyright = element.TryGetProperty("copyright", out var copyright) ? copyright.GetString() : null
        };
    }

    private static GltfScene ParseScene(JsonElement element)
    {
        return new GltfScene
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Nodes = ParseIntArray(element, "nodes")
        };
    }

    private static GltfNode ParseNode(JsonElement element)
    {
        return new GltfNode
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Children = ParseIntArray(element, "children"),
            Mesh = element.TryGetProperty("mesh", out var mesh) ? mesh.GetInt32() : null,
            Skin = element.TryGetProperty("skin", out var skin) ? skin.GetInt32() : null,
            Matrix = ParseFloatArray(element, "matrix"),
            Translation = ParseFloatArray(element, "translation"),
            Rotation = ParseFloatArray(element, "rotation"),
            Scale = ParseFloatArray(element, "scale")
        };
    }

    private static GltfMesh ParseMesh(JsonElement element)
    {
        return new GltfMesh
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Primitives = ParseArray(element, "primitives", ParsePrimitive),
            Weights = ParseFloatArray(element, "weights")
        };
    }

    private static GltfPrimitive ParsePrimitive(JsonElement element)
    {
        var attributes = new Dictionary<string, int>();

        if (element.TryGetProperty("attributes", out var attrs))
        {
            foreach (var attr in attrs.EnumerateObject())
            {
                attributes[attr.Name] = attr.Value.GetInt32();
            }
        }

        return new GltfPrimitive
        {
            Attributes = attributes,
            Indices = element.TryGetProperty("indices", out var indices) ? indices.GetInt32() : null,
            Material = element.TryGetProperty("material", out var material) ? material.GetInt32() : null,
            Mode = element.TryGetProperty("mode", out var mode) ? mode.GetInt32() : 4
        };
    }

    private static GltfBuffer ParseBuffer(JsonElement element)
    {
        return new GltfBuffer
        {
            ByteLength = element.GetProperty("byteLength").GetInt32(),
            Uri = element.TryGetProperty("uri", out var uri) ? uri.GetString() : null,
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfBufferView ParseBufferView(JsonElement element)
    {
        return new GltfBufferView
        {
            Buffer = element.GetProperty("buffer").GetInt32(),
            ByteOffset = element.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0,
            ByteLength = element.GetProperty("byteLength").GetInt32(),
            ByteStride = element.TryGetProperty("byteStride", out var stride) ? stride.GetInt32() : null,
            Target = element.TryGetProperty("target", out var target) ? target.GetInt32() : null,
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfAccessor ParseAccessor(JsonElement element)
    {
        return new GltfAccessor
        {
            BufferView = element.GetProperty("bufferView").GetInt32(),
            ByteOffset = element.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0,
            ComponentType = element.GetProperty("componentType").GetInt32(),
            Count = element.GetProperty("count").GetInt32(),
            Type = element.GetProperty("type").GetString() ?? string.Empty,
            Min = ParseFloatArray(element, "min"),
            Max = ParseFloatArray(element, "max"),
            Normalized = element.TryGetProperty("normalized", out var normalized) && normalized.GetBoolean(),
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfMaterial ParseMaterial(JsonElement element)
    {
        return new GltfMaterial
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            PbrMetallicRoughness = element.TryGetProperty("pbrMetallicRoughness", out var pbr)
                ? ParsePbrMetallicRoughness(pbr)
                : null,
            NormalTexture = element.TryGetProperty("normalTexture", out var normal)
                ? ParseNormalTextureInfo(normal)
                : null,
            OcclusionTexture = element.TryGetProperty("occlusionTexture", out var occlusion)
                ? ParseOcclusionTextureInfo(occlusion)
                : null,
            EmissiveTexture = element.TryGetProperty("emissiveTexture", out var emissive)
                ? ParseTextureInfo(emissive)
                : null,
            EmissiveFactor = ParseFloatArray(element, "emissiveFactor"),
            AlphaMode = element.TryGetProperty("alphaMode", out var alphaMode)
                ? alphaMode.GetString() ?? "OPAQUE"
                : "OPAQUE",
            AlphaCutoff = element.TryGetProperty("alphaCutoff", out var alphaCutoff)
                ? alphaCutoff.GetSingle()
                : 0.5f,
            DoubleSided = element.TryGetProperty("doubleSided", out var doubleSided) && doubleSided.GetBoolean()
        };
    }

    private static GltfPbrMetallicRoughness ParsePbrMetallicRoughness(JsonElement element)
    {
        return new GltfPbrMetallicRoughness
        {
            BaseColorFactor = ParseFloatArray(element, "baseColorFactor") ?? [1.0f, 1.0f, 1.0f, 1.0f],
            BaseColorTexture = element.TryGetProperty("baseColorTexture", out var baseColor)
                ? ParseTextureInfo(baseColor)
                : null,
            MetallicFactor = element.TryGetProperty("metallicFactor", out var metallic)
                ? metallic.GetSingle()
                : 1.0f,
            RoughnessFactor = element.TryGetProperty("roughnessFactor", out var roughness)
                ? roughness.GetSingle()
                : 1.0f,
            MetallicRoughnessTexture = element.TryGetProperty("metallicRoughnessTexture", out var metallicRoughness)
                ? ParseTextureInfo(metallicRoughness)
                : null
        };
    }

    private static GltfTextureInfo ParseTextureInfo(JsonElement element)
    {
        return new GltfTextureInfo
        {
            Index = element.GetProperty("index").GetInt32(),
            TexCoord = element.TryGetProperty("texCoord", out var texCoord) ? texCoord.GetInt32() : 0
        };
    }

    private static GltfNormalTextureInfo ParseNormalTextureInfo(JsonElement element)
    {
        return new GltfNormalTextureInfo
        {
            Index = element.GetProperty("index").GetInt32(),
            TexCoord = element.TryGetProperty("texCoord", out var texCoord) ? texCoord.GetInt32() : 0,
            Scale = element.TryGetProperty("scale", out var scale) ? scale.GetSingle() : 1.0f
        };
    }

    private static GltfOcclusionTextureInfo ParseOcclusionTextureInfo(JsonElement element)
    {
        return new GltfOcclusionTextureInfo
        {
            Index = element.GetProperty("index").GetInt32(),
            TexCoord = element.TryGetProperty("texCoord", out var texCoord) ? texCoord.GetInt32() : 0,
            Strength = element.TryGetProperty("strength", out var strength) ? strength.GetSingle() : 1.0f
        };
    }

    private static GltfTexture ParseTexture(JsonElement element)
    {
        return new GltfTexture
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Sampler = element.TryGetProperty("sampler", out var sampler) ? sampler.GetInt32() : null,
            Source = element.TryGetProperty("source", out var source) ? source.GetInt32() : null
        };
    }

    private static GltfImage ParseImage(JsonElement element)
    {
        return new GltfImage
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            MimeType = element.TryGetProperty("mimeType", out var mimeType) ? mimeType.GetString() : null,
            Uri = element.TryGetProperty("uri", out var uri) ? uri.GetString() : null,
            BufferView = element.TryGetProperty("bufferView", out var bufferView) ? bufferView.GetInt32() : null
        };
    }

    private static GltfSampler ParseSampler(JsonElement element)
    {
        return new GltfSampler
        {
            MagFilter = element.TryGetProperty("magFilter", out var magFilter) ? magFilter.GetInt32() : 9729,
            MinFilter = element.TryGetProperty("minFilter", out var minFilter) ? minFilter.GetInt32() : 9987,
            WrapS = element.TryGetProperty("wrapS", out var wrapS) ? wrapS.GetInt32() : 10497,
            WrapT = element.TryGetProperty("wrapT", out var wrapT) ? wrapT.GetInt32() : 10497,
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfSkin ParseSkin(JsonElement element)
    {
        return new GltfSkin
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            InverseBindMatrices = element.TryGetProperty("inverseBindMatrices", out var ibm)
                ? ibm.GetInt32()
                : null,
            Joints = ParseIntArray(element, "joints"),
            Skeleton = element.TryGetProperty("skeleton", out var skeleton) ? skeleton.GetInt32() : null
        };
    }

    private static GltfAnimation ParseAnimation(JsonElement element)
    {
        return new GltfAnimation
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Channels = ParseArray(element, "channels", ParseAnimationChannel),
            Samplers = ParseArray(element, "samplers", ParseAnimationSampler)
        };
    }

    private static GltfAnimationChannel ParseAnimationChannel(JsonElement element)
    {
        return new GltfAnimationChannel
        {
            Sampler = element.GetProperty("sampler").GetInt32(),
            Target = ParseAnimationChannelTarget(element.GetProperty("target"))
        };
    }

    private static GltfAnimationChannelTarget ParseAnimationChannelTarget(JsonElement element)
    {
        return new GltfAnimationChannelTarget
        {
            Node = element.TryGetProperty("node", out var node) ? node.GetInt32() : null,
            Path = element.GetProperty("path").GetString() ?? string.Empty
        };
    }

    private static GltfAnimationSampler ParseAnimationSampler(JsonElement element)
    {
        return new GltfAnimationSampler
        {
            Input = element.GetProperty("input").GetInt32(),
            Output = element.GetProperty("output").GetInt32(),
            Interpolation = element.TryGetProperty("interpolation", out var interpolation)
                ? interpolation.GetString() ?? "LINEAR"
                : "LINEAR"
        };
    }

    private static IReadOnlyList<int> ParseIntArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array))
        {
            return [];
        }

        var result = new List<int>(array.GetArrayLength());

        foreach (var item in array.EnumerateArray())
        {
            result.Add(item.GetInt32());
        }

        return result;
    }

    private static IReadOnlyList<float>? ParseFloatArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array))
        {
            return null;
        }

        var result = new List<float>(array.GetArrayLength());

        foreach (var item in array.EnumerateArray())
        {
            result.Add(item.GetSingle());
        }

        return result;
    }

    #endregion
}

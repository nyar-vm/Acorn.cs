using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Acorn.Codec;
using Acorn.Frame;
using Acorn.Gltf.Data;

namespace Acorn.Gltf.Encode;

/// <summary>
///     GLTF / GLB 模型编码器，将 C# 数据结构编码为 GLTF JSON 或 GLB 二进制格式。
/// </summary>
/// <remarks>
///     GLTF 使用 JSON 格式描述场景，GLB 是 GLTF 的二进制容器格式。
///     编码器支持两种输出格式，生成符合 Khronos GLTF 2.0 规范的数据。
/// </remarks>
public sealed class GltfEncoder
{
    /// <summary>
    ///     将模型数据编码为 GLTF JSON 字符串。
    /// </summary>
    /// <param name="data">模型数据。</param>
    /// <returns>JSON 字符串。</returns>
    public string EncodeJson(GltfModelData data)
    {
        var root = new JsonObject();

        root["asset"] = EncodeAsset(data.Asset);

        if (data.Scene.HasValue)
        {
            root["scene"] = data.Scene.Value;
        }

        if (data.Scenes.Count > 0)
        {
            root["scenes"] = new JsonArray(data.Scenes.Select(EncodeScene).ToArray());
        }

        if (data.Nodes.Count > 0)
        {
            root["nodes"] = new JsonArray(data.Nodes.Select(EncodeNode).ToArray());
        }

        if (data.Meshes.Count > 0)
        {
            root["meshes"] = new JsonArray(data.Meshes.Select(EncodeMesh).ToArray());
        }

        if (data.Buffers.Count > 0)
        {
            root["buffers"] = new JsonArray(data.Buffers.Select(EncodeBuffer).ToArray());
        }

        if (data.BufferViews.Count > 0)
        {
            root["bufferViews"] = new JsonArray(data.BufferViews.Select(EncodeBufferView).ToArray());
        }

        if (data.Accessors.Count > 0)
        {
            root["accessors"] = new JsonArray(data.Accessors.Select(EncodeAccessor).ToArray());
        }

        if (data.Materials.Count > 0)
        {
            root["materials"] = new JsonArray(data.Materials.Select(EncodeMaterial).ToArray());
        }

        if (data.Textures.Count > 0)
        {
            root["textures"] = new JsonArray(data.Textures.Select(EncodeTexture).ToArray());
        }

        if (data.Images.Count > 0)
        {
            root["images"] = new JsonArray(data.Images.Select(EncodeImage).ToArray());
        }

        if (data.Samplers.Count > 0)
        {
            root["samplers"] = new JsonArray(data.Samplers.Select(EncodeSampler).ToArray());
        }

        if (data.Skins.Count > 0)
        {
            root["skins"] = new JsonArray(data.Skins.Select(EncodeSkin).ToArray());
        }

        if (data.Animations.Count > 0)
        {
            root["animations"] = new JsonArray(data.Animations.Select(EncodeAnimation).ToArray());
        }

        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    ///     将模型数据编码为 GLB 二进制格式。
    /// </summary>
    /// <param name="data">模型数据。</param>
    /// <param name="binaryData">二进制缓冲区数据。</param>
    /// <returns>GLB 二进制数据。</returns>
    public byte[] EncodeGlb(GltfModelData data, byte[]? binaryData = null)
    {
        var json = EncodeJson(data);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var jsonPadding = (4 - (jsonBytes.Length % 4)) % 4;
        var binaryPadding = binaryData != null ? (4 - (binaryData.Length % 4)) % 4 : 0;

        var jsonChunkLength = jsonBytes.Length + jsonPadding;
        var binaryChunkLength = binaryData != null ? binaryData.Length + binaryPadding : 0;

        var totalLength = 12 + 8 + jsonChunkLength;

        if (binaryData != null)
        {
            totalLength += 8 + binaryChunkLength;
        }

        var output = new byte[totalLength];
        var writer = new ByteBufferWriter(output);

        var header = new GlbHeader
        {
            Magic = FixedBytes4.FromSpan(GltfConstants.GlbMagicNumber),
            Version = GltfConstants.GlbVersion,
            Length = (uint)totalLength
        };
        header.WriteTo(ref writer);

        var jsonChunkHeader = new GlbChunkHeader
        {
            Length = (uint)jsonChunkLength,
            Type = GltfConstants.ChunkTypeJson
        };
        jsonChunkHeader.WriteTo(ref writer);
        writer.Write(jsonBytes);

        for (var i = 0; i < jsonPadding; i++)
        {
            writer.WriteU8((byte)GltfConstants.PaddingByte);
        }

        if (binaryData != null)
        {
            var binChunkHeader = new GlbChunkHeader
            {
                Length = (uint)binaryChunkLength,
                Type = GltfConstants.ChunkTypeBin
            };
            binChunkHeader.WriteTo(ref writer);
            writer.Write(binaryData);

            for (var i = 0; i < binaryPadding; i++)
            {
                writer.WriteU8(0);
            }
        }

        return output;
    }

    #region 私有编码方法

    private static JsonObject EncodeAsset(GltfAsset asset)
    {
        var obj = new JsonObject
        {
            ["version"] = asset.Version
        };

        if (asset.Generator != null)
        {
            obj["generator"] = asset.Generator;
        }

        if (asset.Copyright != null)
        {
            obj["copyright"] = asset.Copyright;
        }

        return obj;
    }

    private static JsonObject EncodeScene(GltfScene scene)
    {
        var obj = new JsonObject();

        if (scene.Name != null)
        {
            obj["name"] = scene.Name;
        }

        if (scene.Nodes.Count > 0)
        {
            obj["nodes"] = new JsonArray(scene.Nodes.Select(n => (JsonNode)n).ToArray());
        }

        return obj;
    }

    private static JsonObject EncodeNode(GltfNode node)
    {
        var obj = new JsonObject();

        if (node.Name != null)
        {
            obj["name"] = node.Name;
        }

        if (node.Children.Count > 0)
        {
            obj["children"] = new JsonArray(node.Children.Select(n => (JsonNode)n).ToArray());
        }

        if (node.Mesh.HasValue)
        {
            obj["mesh"] = node.Mesh.Value;
        }

        if (node.Skin.HasValue)
        {
            obj["skin"] = node.Skin.Value;
        }

        if (node.Matrix != null)
        {
            obj["matrix"] = new JsonArray(node.Matrix.Select(n => (JsonNode)n).ToArray());
        }

        if (node.Translation != null)
        {
            obj["translation"] = new JsonArray(node.Translation.Select(n => (JsonNode)n).ToArray());
        }

        if (node.Rotation != null)
        {
            obj["rotation"] = new JsonArray(node.Rotation.Select(n => (JsonNode)n).ToArray());
        }

        if (node.Scale != null)
        {
            obj["scale"] = new JsonArray(node.Scale.Select(n => (JsonNode)n).ToArray());
        }

        return obj;
    }

    private static JsonObject EncodeMesh(GltfMesh mesh)
    {
        var obj = new JsonObject();

        if (mesh.Name != null)
        {
            obj["name"] = mesh.Name;
        }

        obj["primitives"] = new JsonArray(mesh.Primitives.Select(EncodePrimitive).ToArray());

        if (mesh.Weights != null)
        {
            obj["weights"] = new JsonArray(mesh.Weights.Select(n => (JsonNode)n).ToArray());
        }

        return obj;
    }

    private static JsonObject EncodePrimitive(GltfPrimitive primitive)
    {
        var obj = new JsonObject
        {
            ["attributes"] = new JsonObject(primitive.Attributes.Select(a =>
                new KeyValuePair<string, JsonNode?>(a.Key, a.Value)))
        };

        if (primitive.Indices.HasValue)
        {
            obj["indices"] = primitive.Indices.Value;
        }

        if (primitive.Material.HasValue)
        {
            obj["material"] = primitive.Material.Value;
        }

        if (primitive.Mode != 4)
        {
            obj["mode"] = primitive.Mode;
        }

        return obj;
    }

    private static JsonObject EncodeBuffer(GltfBuffer buffer)
    {
        var obj = new JsonObject
        {
            ["byteLength"] = buffer.ByteLength
        };

        if (buffer.Uri != null)
        {
            obj["uri"] = buffer.Uri;
        }

        if (buffer.Name != null)
        {
            obj["name"] = buffer.Name;
        }

        return obj;
    }

    private static JsonObject EncodeBufferView(GltfBufferView bufferView)
    {
        var obj = new JsonObject
        {
            ["buffer"] = bufferView.Buffer,
            ["byteOffset"] = bufferView.ByteOffset,
            ["byteLength"] = bufferView.ByteLength
        };

        if (bufferView.ByteStride.HasValue)
        {
            obj["byteStride"] = bufferView.ByteStride.Value;
        }

        if (bufferView.Target.HasValue)
        {
            obj["target"] = bufferView.Target.Value;
        }

        if (bufferView.Name != null)
        {
            obj["name"] = bufferView.Name;
        }

        return obj;
    }

    private static JsonObject EncodeAccessor(GltfAccessor accessor)
    {
        var obj = new JsonObject
        {
            ["bufferView"] = accessor.BufferView,
            ["componentType"] = accessor.ComponentType,
            ["count"] = accessor.Count,
            ["type"] = accessor.Type
        };

        if (accessor.ByteOffset != 0)
        {
            obj["byteOffset"] = accessor.ByteOffset;
        }

        if (accessor.Min != null)
        {
            obj["min"] = new JsonArray(accessor.Min.Select(n => (JsonNode)n).ToArray());
        }

        if (accessor.Max != null)
        {
            obj["max"] = new JsonArray(accessor.Max.Select(n => (JsonNode)n).ToArray());
        }

        if (accessor.Normalized)
        {
            obj["normalized"] = true;
        }

        if (accessor.Name != null)
        {
            obj["name"] = accessor.Name;
        }

        return obj;
    }

    private static JsonObject EncodeMaterial(GltfMaterial material)
    {
        var obj = new JsonObject();

        if (material.Name != null)
        {
            obj["name"] = material.Name;
        }

        if (material.PbrMetallicRoughness != null)
        {
            obj["pbrMetallicRoughness"] = EncodePbrMetallicRoughness(material.PbrMetallicRoughness);
        }

        if (material.NormalTexture != null)
        {
            obj["normalTexture"] = EncodeNormalTextureInfo(material.NormalTexture);
        }

        if (material.OcclusionTexture != null)
        {
            obj["occlusionTexture"] = EncodeOcclusionTextureInfo(material.OcclusionTexture);
        }

        if (material.EmissiveTexture != null)
        {
            obj["emissiveTexture"] = EncodeTextureInfo(material.EmissiveTexture);
        }

        if (material.EmissiveFactor != null)
        {
            obj["emissiveFactor"] = new JsonArray(material.EmissiveFactor.Select(n => (JsonNode)n).ToArray());
        }

        if (material.AlphaMode != "OPAQUE")
        {
            obj["alphaMode"] = material.AlphaMode;
        }

        if (Math.Abs(material.AlphaCutoff - 0.5f) > float.Epsilon)
        {
            obj["alphaCutoff"] = material.AlphaCutoff;
        }

        if (material.DoubleSided)
        {
            obj["doubleSided"] = true;
        }

        return obj;
    }

    private static JsonObject EncodePbrMetallicRoughness(GltfPbrMetallicRoughness pbr)
    {
        var obj = new JsonObject();

        if (pbr.BaseColorFactor != null && (pbr.BaseColorFactor.Count != 4 ||
                                            pbr.BaseColorFactor[0] != 1.0f ||
                                            pbr.BaseColorFactor[1] != 1.0f ||
                                            pbr.BaseColorFactor[2] != 1.0f ||
                                            pbr.BaseColorFactor[3] != 1.0f))
        {
            obj["baseColorFactor"] = new JsonArray(pbr.BaseColorFactor.Select(n => (JsonNode)n).ToArray());
        }

        if (pbr.BaseColorTexture != null)
        {
            obj["baseColorTexture"] = EncodeTextureInfo(pbr.BaseColorTexture);
        }

        if (Math.Abs(pbr.MetallicFactor - 1.0f) > float.Epsilon)
        {
            obj["metallicFactor"] = pbr.MetallicFactor;
        }

        if (Math.Abs(pbr.RoughnessFactor - 1.0f) > float.Epsilon)
        {
            obj["roughnessFactor"] = pbr.RoughnessFactor;
        }

        if (pbr.MetallicRoughnessTexture != null)
        {
            obj["metallicRoughnessTexture"] = EncodeTextureInfo(pbr.MetallicRoughnessTexture);
        }

        return obj;
    }

    private static JsonObject EncodeTextureInfo(GltfTextureInfo textureInfo)
    {
        var obj = new JsonObject
        {
            ["index"] = textureInfo.Index
        };

        if (textureInfo.TexCoord != 0)
        {
            obj["texCoord"] = textureInfo.TexCoord;
        }

        return obj;
    }

    private static JsonObject EncodeNormalTextureInfo(GltfNormalTextureInfo textureInfo)
    {
        var obj = EncodeTextureInfo(textureInfo);

        if (Math.Abs(textureInfo.Scale - 1.0f) > float.Epsilon)
        {
            obj["scale"] = textureInfo.Scale;
        }

        return obj;
    }

    private static JsonObject EncodeOcclusionTextureInfo(GltfOcclusionTextureInfo textureInfo)
    {
        var obj = EncodeTextureInfo(textureInfo);

        if (Math.Abs(textureInfo.Strength - 1.0f) > float.Epsilon)
        {
            obj["strength"] = textureInfo.Strength;
        }

        return obj;
    }

    private static JsonObject EncodeTexture(GltfTexture texture)
    {
        var obj = new JsonObject();

        if (texture.Name != null)
        {
            obj["name"] = texture.Name;
        }

        if (texture.Sampler.HasValue)
        {
            obj["sampler"] = texture.Sampler.Value;
        }

        if (texture.Source.HasValue)
        {
            obj["source"] = texture.Source.Value;
        }

        return obj;
    }

    private static JsonObject EncodeImage(GltfImage image)
    {
        var obj = new JsonObject();

        if (image.Name != null)
        {
            obj["name"] = image.Name;
        }

        if (image.Uri != null)
        {
            obj["uri"] = image.Uri;
        }

        if (image.MimeType != null)
        {
            obj["mimeType"] = image.MimeType;
        }

        if (image.BufferView.HasValue)
        {
            obj["bufferView"] = image.BufferView.Value;
        }

        return obj;
    }

    private static JsonObject EncodeSampler(GltfSampler sampler)
    {
        var obj = new JsonObject();

        if (sampler.MagFilter != 9729)
        {
            obj["magFilter"] = sampler.MagFilter;
        }

        if (sampler.MinFilter != 9987)
        {
            obj["minFilter"] = sampler.MinFilter;
        }

        if (sampler.WrapS != 10497)
        {
            obj["wrapS"] = sampler.WrapS;
        }

        if (sampler.WrapT != 10497)
        {
            obj["wrapT"] = sampler.WrapT;
        }

        if (sampler.Name != null)
        {
            obj["name"] = sampler.Name;
        }

        return obj;
    }

    private static JsonObject EncodeSkin(GltfSkin skin)
    {
        var obj = new JsonObject();

        if (skin.Name != null)
        {
            obj["name"] = skin.Name;
        }

        if (skin.InverseBindMatrices.HasValue)
        {
            obj["inverseBindMatrices"] = skin.InverseBindMatrices.Value;
        }

        obj["joints"] = new JsonArray(skin.Joints.Select(n => (JsonNode)n).ToArray());

        if (skin.Skeleton.HasValue)
        {
            obj["skeleton"] = skin.Skeleton.Value;
        }

        return obj;
    }

    private static JsonObject EncodeAnimation(GltfAnimation animation)
    {
        var obj = new JsonObject();

        if (animation.Name != null)
        {
            obj["name"] = animation.Name;
        }

        obj["channels"] = new JsonArray(animation.Channels.Select(EncodeAnimationChannel).ToArray());
        obj["samplers"] = new JsonArray(animation.Samplers.Select(EncodeAnimationSampler).ToArray());

        return obj;
    }

    private static JsonObject EncodeAnimationChannel(GltfAnimationChannel channel)
    {
        return new JsonObject
        {
            ["sampler"] = channel.Sampler,
            ["target"] = EncodeAnimationChannelTarget(channel.Target)
        };
    }

    private static JsonObject EncodeAnimationChannelTarget(GltfAnimationChannelTarget target)
    {
        var obj = new JsonObject
        {
            ["path"] = target.Path
        };

        if (target.Node.HasValue)
        {
            obj["node"] = target.Node.Value;
        }

        return obj;
    }

    private static JsonObject EncodeAnimationSampler(GltfAnimationSampler sampler)
    {
        var obj = new JsonObject
        {
            ["input"] = sampler.Input,
            ["output"] = sampler.Output
        };

        if (sampler.Interpolation != "LINEAR")
        {
            obj["interpolation"] = sampler.Interpolation;
        }

        return obj;
    }

    #endregion
}

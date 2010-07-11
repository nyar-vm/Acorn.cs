namespace Acorn.Gltf.Data;

/// <summary>
///     GLTF 模型数据。
/// </summary>
public sealed class GltfModelData
{
    /// <summary>
    ///     资产信息。
    /// </summary>
    public GltfAsset Asset { get; init; } = new();

    /// <summary>
    ///     场景索引。
    /// </summary>
    public int? Scene { get; init; }

    /// <summary>
    ///     场景列表。
    /// </summary>
    public IReadOnlyList<GltfScene> Scenes { get; init; } = [];

    /// <summary>
    ///     节点列表。
    /// </summary>
    public IReadOnlyList<GltfNode> Nodes { get; init; } = [];

    /// <summary>
    ///     网格列表。
    /// </summary>
    public IReadOnlyList<GltfMesh> Meshes { get; init; } = [];

    /// <summary>
    ///     缓冲区列表。
    /// </summary>
    public IReadOnlyList<GltfBuffer> Buffers { get; init; } = [];

    /// <summary>
    ///     缓冲区视图列表。
    /// </summary>
    public IReadOnlyList<GltfBufferView> BufferViews { get; init; } = [];

    /// <summary>
    ///     访问器列表。
    /// </summary>
    public IReadOnlyList<GltfAccessor> Accessors { get; init; } = [];

    /// <summary>
    ///     材质列表。
    /// </summary>
    public IReadOnlyList<GltfMaterial> Materials { get; init; } = [];

    /// <summary>
    ///     纹理列表。
    /// </summary>
    public IReadOnlyList<GltfTexture> Textures { get; init; } = [];

    /// <summary>
    ///     图像列表。
    /// </summary>
    public IReadOnlyList<GltfImage> Images { get; init; } = [];

    /// <summary>
    ///     采样器列表。
    /// </summary>
    public IReadOnlyList<GltfSampler> Samplers { get; init; } = [];

    /// <summary>
    ///     蒙皮列表。
    /// </summary>
    public IReadOnlyList<GltfSkin> Skins { get; init; } = [];

    /// <summary>
    ///     动画列表。
    /// </summary>
    public IReadOnlyList<GltfAnimation> Animations { get; init; } = [];
}

/// <summary>
///     GLTF 资产信息。
/// </summary>
public sealed class GltfAsset
{
    /// <summary>
    ///     版本。
    /// </summary>
    public string Version { get; init; } = "2.0";

    /// <summary>
    ///     生成器名称。
    /// </summary>
    public string? Generator { get; init; }

    /// <summary>
    ///     版权信息。
    /// </summary>
    public string? Copyright { get; init; }
}

/// <summary>
///     GLTF 场景。
/// </summary>
public sealed class GltfScene
{
    /// <summary>
    ///     场景名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     根节点索引列表。
    /// </summary>
    public IReadOnlyList<int> Nodes { get; init; } = [];
}

/// <summary>
///     GLTF 节点。
/// </summary>
public sealed class GltfNode
{
    /// <summary>
    ///     节点名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     子节点索引列表。
    /// </summary>
    public IReadOnlyList<int> Children { get; init; } = [];

    /// <summary>
    ///     网格索引。
    /// </summary>
    public int? Mesh { get; init; }

    /// <summary>
    ///     蒙皮索引。
    /// </summary>
    public int? Skin { get; init; }

    /// <summary>
    ///     局部变换矩阵（16 个 float）。
    /// </summary>
    public IReadOnlyList<float>? Matrix { get; init; }

    /// <summary>
    ///     位移向量（3 个 float）。
    /// </summary>
    public IReadOnlyList<float>? Translation { get; init; }

    /// <summary>
    ///     旋转四元数（4 个 float）。
    /// </summary>
    public IReadOnlyList<float>? Rotation { get; init; }

    /// <summary>
    ///     缩放向量（3 个 float）。
    /// </summary>
    public IReadOnlyList<float>? Scale { get; init; }
}

/// <summary>
///     GLTF 网格。
/// </summary>
public sealed class GltfMesh
{
    /// <summary>
    ///     网格名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     图元列表。
    /// </summary>
    public IReadOnlyList<GltfPrimitive> Primitives { get; init; } = [];

    /// <summary>
    ///     权重列表（变形目标使用）。
    /// </summary>
    public IReadOnlyList<float>? Weights { get; init; }
}

/// <summary>
///     GLTF 图元。
/// </summary>
public sealed class GltfPrimitive
{
    /// <summary>
    ///     属性访问器字典（POSITION、NORMAL、TEXCOORD_0 等）。
    /// </summary>
    public IReadOnlyDictionary<string, int> Attributes { get; init; } = new Dictionary<string, int>();

    /// <summary>
    ///     索引访问器索引。
    /// </summary>
    public int? Indices { get; init; }

    /// <summary>
    ///     材质索引。
    /// </summary>
    public int? Material { get; init; }

    /// <summary>
    ///     图元拓扑模式（0=点, 1=线, 2=线环, 3=线串, 4=三角形, 5=三角扇, 6=三角带）。
    /// </summary>
    public int Mode { get; init; } = 4;
}

/// <summary>
///     GLTF 缓冲区。
/// </summary>
public sealed class GltfBuffer
{
    /// <summary>
    ///     缓冲区字节长度。
    /// </summary>
    public int ByteLength { get; init; }

    /// <summary>
    ///     数据 URI（base64 编码）或文件路径。
    /// </summary>
    public string? Uri { get; init; }

    /// <summary>
    ///     缓冲区名称。
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
///     GLTF 缓冲区视图。
/// </summary>
public sealed class GltfBufferView
{
    /// <summary>
    ///     所属缓冲区索引。
    /// </summary>
    public int Buffer { get; init; }

    /// <summary>
    ///     字节偏移量。
    /// </summary>
    public int ByteOffset { get; init; }

    /// <summary>
    ///     字节长度。
    /// </summary>
    public int ByteLength { get; init; }

    /// <summary>
    ///     字节步长（顶点属性之间的字节距离）。
    /// </summary>
    public int? ByteStride { get; init; }

    /// <summary>
    ///     视图目标（34962=ARRAY_BUFFER, 34963=ELEMENT_ARRAY_BUFFER）。
    /// </summary>
    public int? Target { get; init; }

    /// <summary>
    ///     视图名称。
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
///     GLTF 访问器。
/// </summary>
public sealed class GltfAccessor
{
    /// <summary>
    ///     所属缓冲区视图索引。
    /// </summary>
    public int BufferView { get; init; }

    /// <summary>
    ///     字节偏移量。
    /// </summary>
    public int ByteOffset { get; init; }

    /// <summary>
    ///     组件数据类型（5120=BYTE, 5121=UNSIGNED_BYTE, 5122=SHORT, 5123=UNSIGNED_SHORT, 5125=UNSIGNED_INT, 5126=FLOAT）。
    /// </summary>
    public int ComponentType { get; init; }

    /// <summary>
    ///     元素数量。
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    ///     元素类型（SCALAR、VEC2、VEC3、VEC4、MAT2、MAT3、MAT4）。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    ///     最小值。
    /// </summary>
    public IReadOnlyList<float>? Min { get; init; }

    /// <summary>
    ///     最大值。
    /// </summary>
    public IReadOnlyList<float>? Max { get; init; }

    /// <summary>
    ///     是否归一化。
    /// </summary>
    public bool Normalized { get; init; }

    /// <summary>
    ///     访问器名称。
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
///     GLTF 材质。
/// </summary>
public sealed class GltfMaterial
{
    /// <summary>
    ///     材质名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     PBR 金属度/粗糙度工作流参数。
    /// </summary>
    public GltfPbrMetallicRoughness? PbrMetallicRoughness { get; init; }

    /// <summary>
    ///     法线纹理。
    /// </summary>
    public GltfNormalTextureInfo? NormalTexture { get; init; }

    /// <summary>
    ///     遮挡纹理。
    /// </summary>
    public GltfOcclusionTextureInfo? OcclusionTexture { get; init; }

    /// <summary>
    ///     自发光纹理。
    /// </summary>
    public GltfTextureInfo? EmissiveTexture { get; init; }

    /// <summary>
    ///     自发光颜色（RGB）。
    /// </summary>
    public IReadOnlyList<float>? EmissiveFactor { get; init; }

    /// <summary>
    ///     Alpha 渲染模式（OPAQUE、MASK、BLEND）。
    /// </summary>
    public string AlphaMode { get; init; } = "OPAQUE";

    /// <summary>
    ///     Alpha 裁剪阈值。
    /// </summary>
    public float AlphaCutoff { get; init; } = 0.5f;

    /// <summary>
    ///     是否双面渲染。
    /// </summary>
    public bool DoubleSided { get; init; }
}

/// <summary>
///     GLTF PBR 金属度/粗糙度参数。
/// </summary>
public sealed class GltfPbrMetallicRoughness
{
    /// <summary>
    ///     基础颜色因子（RGBA）。
    /// </summary>
    public IReadOnlyList<float> BaseColorFactor { get; init; } = [1.0f, 1.0f, 1.0f, 1.0f];

    /// <summary>
    ///     基础颜色纹理。
    /// </summary>
    public GltfTextureInfo? BaseColorTexture { get; init; }

    /// <summary>
    ///     金属度因子。
    /// </summary>
    public float MetallicFactor { get; init; } = 1.0f;

    /// <summary>
    ///     粗糙度因子。
    /// </summary>
    public float RoughnessFactor { get; init; } = 1.0f;

    /// <summary>
    ///     金属度/粗糙度纹理。
    /// </summary>
    public GltfTextureInfo? MetallicRoughnessTexture { get; init; }
}

/// <summary>
///     GLTF 纹理信息。
/// </summary>
public class GltfTextureInfo
{
    /// <summary>
    ///     纹理索引。
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    ///     纹理坐标集索引。
    /// </summary>
    public int TexCoord { get; init; }
}

/// <summary>
///     GLTF 法线纹理信息。
/// </summary>
public sealed class GltfNormalTextureInfo : GltfTextureInfo
{
    /// <summary>
    ///     法线缩放因子。
    /// </summary>
    public float Scale { get; init; } = 1.0f;
}

/// <summary>
///     GLTF 遮挡纹理信息。
/// </summary>
public sealed class GltfOcclusionTextureInfo : GltfTextureInfo
{
    /// <summary>
    ///     遮挡强度。
    /// </summary>
    public float Strength { get; init; } = 1.0f;
}

/// <summary>
///     GLTF 纹理。
/// </summary>
public sealed class GltfTexture
{
    /// <summary>
    ///     纹理名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     采样器索引。
    /// </summary>
    public int? Sampler { get; init; }

    /// <summary>
    ///     图像索引。
    /// </summary>
    public int? Source { get; init; }
}

/// <summary>
///     GLTF 图像。
/// </summary>
public sealed class GltfImage
{
    /// <summary>
    ///     图像名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     MIME 类型。
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    ///     数据 URI 或文件路径。
    /// </summary>
    public string? Uri { get; init; }

    /// <summary>
    ///     缓冲区视图索引（用于嵌入二进制数据）。
    /// </summary>
    public int? BufferView { get; init; }
}

/// <summary>
///     GLTF 采样器。
/// </summary>
public sealed class GltfSampler
{
    /// <summary>
    ///     放大过滤器（9728=NEAREST, 9729=LINEAR）。
    /// </summary>
    public int MagFilter { get; init; } = 9729;

    /// <summary>
    ///     缩小过滤器（9728=NEAREST, 9729=LINEAR, 9984=NEAREST_MIPMAP_NEAREST, 9985=LINEAR_MIPMAP_NEAREST, 9986=NEAREST_MIPMAP_LINEAR, 9987=LINEAR_MIPMAP_LINEAR）。
    /// </summary>
    public int MinFilter { get; init; } = 9987;

    /// <summary>
    ///     水平环绕模式（33071=CLAMP_TO_EDGE, 33648=MIRRORED_REPEAT, 10497=REPEAT）。
    /// </summary>
    public int WrapS { get; init; } = 10497;

    /// <summary>
    ///     垂直环绕模式（33071=CLAMP_TO_EDGE, 33648=MIRRORED_REPEAT, 10497=REPEAT）。
    /// </summary>
    public int WrapT { get; init; } = 10497;

    /// <summary>
    ///     采样器名称。
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
///     GLTF 蒙皮。
/// </summary>
public sealed class GltfSkin
{
    /// <summary>
    ///     蒙皮名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     逆绑定矩阵访问器索引。
    /// </summary>
    public int? InverseBindMatrices { get; init; }

    /// <summary>
    ///     骨骼节点索引列表。
    /// </summary>
    public IReadOnlyList<int> Joints { get; init; } = [];

    /// <summary>
    ///     根骨骼节点索引。
    /// </summary>
    public int? Skeleton { get; init; }
}

/// <summary>
///     GLTF 动画。
/// </summary>
public sealed class GltfAnimation
{
    /// <summary>
    ///     动画名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     动画通道列表。
    /// </summary>
    public IReadOnlyList<GltfAnimationChannel> Channels { get; init; } = [];

    /// <summary>
    ///     动画采样器列表。
    /// </summary>
    public IReadOnlyList<GltfAnimationSampler> Samplers { get; init; } = [];
}

/// <summary>
///     GLTF 动画通道。
/// </summary>
public sealed class GltfAnimationChannel
{
    /// <summary>
    ///     采样器索引。
    /// </summary>
    public int Sampler { get; init; }

    /// <summary>
    ///     目标节点和路径。
    /// </summary>
    public GltfAnimationChannelTarget Target { get; init; } = new();
}

/// <summary>
///     GLTF 动画通道目标。
/// </summary>
public sealed class GltfAnimationChannelTarget
{
    /// <summary>
    ///     目标节点索引。
    /// </summary>
    public int? Node { get; init; }

    /// <summary>
    ///     目标路径（translation、rotation、scale、weights）。
    /// </summary>
    public string Path { get; init; } = string.Empty;
}

/// <summary>
///     GLTF 动画采样器。
/// </summary>
public sealed class GltfAnimationSampler
{
    /// <summary>
    ///     输入访问器索引（时间戳）。
    /// </summary>
    public int Input { get; init; }

    /// <summary>
    ///     输出访问器索引（值）。
    /// </summary>
    public int Output { get; init; }

    /// <summary>
    ///     插值方式（LINEAR、STEP、CUBICSPLINE）。
    /// </summary>
    public string Interpolation { get; init; } = "LINEAR";
}

using Acorn;
using Acorn.Attributes;
using Acorn.Codec;

namespace Acorn.Gltf.Data;

/// <summary>
///     GLTF / GLB 二进制格式常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 Khronos GLTF 2.0 规范，Acorn 独占二进制编解码职责。
/// </remarks>
public static class GltfConstants
{
    /// <summary>
    ///     GLB 文件魔数（"glTF"）。
    /// </summary>
    public static ReadOnlySpan<byte> GlbMagicNumber => new byte[] { 0x67, 0x6C, 0x54, 0x46 };

    /// <summary>
    ///     GLB 版本号（2）。
    /// </summary>
    public const uint GlbVersion = 2;

    /// <summary>
    ///     GLB JSON 块类型标识（"JSON" 的 ASCII 小端序）。
    /// </summary>
    public const uint ChunkTypeJson = 0x4E4F534A;

    /// <summary>
    ///     GLB BIN 块类型标识（"BIN\0" 的 ASCII 小端序）。
    /// </summary>
    public const uint ChunkTypeBin = 0x004E4942;

    /// <summary>
    ///     GLB JSON 块填充字节（空格）。
    /// </summary>
    public const byte PaddingByte = 0x20;
}

/// <summary>
///     GLTF 纹理过滤模式枚举。
/// </summary>
public enum GltfFilterMode : int
{
    /// <summary>
    ///     最近邻采样。
    /// </summary>
    Nearest = 9728,

    /// <summary>
    ///     线性采样（默认）。
    /// </summary>
    Linear = 9729,

    /// <summary>
    ///     最近邻 mipmap 最近邻采样。
    /// </summary>
    NearestMipmapNearest = 9984,

    /// <summary>
    ///     线性 mipmap 最近邻采样。
    /// </summary>
    LinearMipmapNearest = 9985,

    /// <summary>
    ///     最近邻 mipmap 线性采样。
    /// </summary>
    NearestMipmapLinear = 9986,

    /// <summary>
    ///     线性 mipmap 线性采样（默认）。
    /// </summary>
    LinearMipmapLinear = 9987
}

/// <summary>
///     GLTF 纹理环绕模式枚举。
/// </summary>
public enum GltfWrapMode : int
{
    /// <summary>
    ///     钳制到边缘（默认）。
    /// </summary>
    ClampToEdge = 33071,

    /// <summary>
    ///     镜像重复。
    /// </summary>
    MirroredRepeat = 33648,

    /// <summary>
    ///     重复（默认）。
    /// </summary>
    Repeat = 10497
}

/// <summary>
///     GLTF 图元渲染模式枚举。
/// </summary>
public enum GltfPrimitiveMode : int
{
    /// <summary>
    ///     点列表。
    /// </summary>
    Points = 0,

    /// <summary>
    ///     线列表。
    /// </summary>
    Lines = 1,

    /// <summary>
    ///     线环。
    /// </summary>
    LineLoop = 2,

    /// <summary>
    ///     线带。
    /// </summary>
    LineStrip = 3,

    /// <summary>
    ///     三角形列表（默认）。
    /// </summary>
    Triangles = 4,

    /// <summary>
    ///     三角形带。
    /// </summary>
    TriangleStrip = 5,

    /// <summary>
    ///     三角形扇。
    /// </summary>
    TriangleFan = 6
}

/// <summary>
///     GLB 文件头部（12 字节）。
/// </summary>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct GlbHeader
{
    [Field(Order = 0, Length = 4)]
    public FixedBytes4 Magic;

    [Field(Order = 1)]
    public uint Version;

    [Field(Order = 2)]
    public uint Length;
}

/// <summary>
///     GLB 块头部（8 字节）。
/// </summary>
[BinarySerializable]
public partial struct GlbChunkHeader
{
    [Field(Order = 0)]
    public uint Length;

    [Field(Order = 1)]
    public uint Type;
}

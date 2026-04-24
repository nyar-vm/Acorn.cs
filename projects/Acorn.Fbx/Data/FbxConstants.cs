namespace Acorn.Fbx.Data;

/// <summary>
///     Autodesk FBX 二进制格式常量。
/// </summary>
public static class FbxConstants
{
    /// <summary>
    ///     FBX 二进制文件魔数前缀（"Kaydara FBX Binary"）。
    /// </summary>
    public static ReadOnlySpan<byte> BinaryMagic => "Kaydara FBX Binary\u0020\u000A\u0000\u001A\u0000"u8;

    /// <summary>
    ///     FBX 二进制魔数字符串长度。
    /// </summary>
    public const int MagicLength = 23;

    /// <summary>
    ///     FBX 头部版本偏移。
    /// </summary>
    public const int VersionOffset = 23;

    /// <summary>
    ///     FBX 头部总大小。
    /// </summary>
    public const int HeaderSize = 27;

    /// <summary>
    ///     FBX 记录结束标记。
    /// </summary>
    public static ReadOnlySpan<byte> NullRecord => new byte[13];

    /// <summary>
    ///     FBX 属性类型代码。
    /// </summary>
    public static class PropertyType
    {
        /// <summary>
        ///     16 位布尔值。
        /// </summary>
        public const byte Boolean = (byte)'C';

        /// <summary>
        ///     8 位整数。
        /// </summary>
        public const byte Int8 = (byte)'Y';

        /// <summary>
        ///     16 位整数。
        /// </summary>
        public const byte Int16 = (byte)'h';

        /// <summary>
        ///     32 位整数。
        /// </summary>
        public const byte Int32 = (byte)'i';

        /// <summary>
        ///     64 位整数。
        /// </summary>
        public const byte Int64 = (byte)'l';

        /// <summary>
        ///     32 位浮点数。
        /// </summary>
        public const byte Float32 = (byte)'f';

        /// <summary>
        ///     64 位浮点数。
        /// </summary>
        public const byte Float64 = (byte)'d';

        /// <summary>
        ///     字符串。
        /// </summary>
        public const byte String = (byte)'S';

        /// <summary>
        ///     原始字节缓冲区。
        /// </summary>
        public const byte RawBuffer = (byte)'R';

        /// <summary>
        ///     数组类型标记。
        /// </summary>
        public const byte ArrayMarker = (byte)'[';
    }
}

/// <summary>
///     FBX 文件版本信息。
/// </summary>
public static class FbxVersions
{
    /// <summary>
    ///     FBX 6.1。
    /// </summary>
    public const int V61 = 6100;

    /// <summary>
    ///     FBX 7.0。
    /// </summary>
    public const int V70 = 7000;

    /// <summary>
    ///     FBX 7.1。
    /// </summary>
    public const int V71 = 7100;

    /// <summary>
    ///     FBX 7.2。
    /// </summary>
    public const int V72 = 7200;

    /// <summary>
    ///     FBX 7.3。
    /// </summary>
    public const int V73 = 7300;

    /// <summary>
    ///     FBX 7.4。
    /// </summary>
    public const int V74 = 7400;

    /// <summary>
    ///     FBX 7.5。
    /// </summary>
    public const int V75 = 7500;

    /// <summary>
    ///     FBX 7.7。
    /// </summary>
    public const int V77 = 7700;
}

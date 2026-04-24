namespace Acorn.Vrm.Data;

/// <summary>
///     VRM 格式常量。
/// </summary>
public static class VrmConstants
{
    /// <summary>
    ///     VRM 扩展名称。
    /// </summary>
    public const string ExtensionName = "VRM";

    /// <summary>
    ///     VRM 0.x 版本扩展名称。
    /// </summary>
    public const string Vrm0Extension = "VRM";

    /// <summary>
    ///     VRM 1.0 版本扩展名称。
    /// </summary>
    public const string Vrm1Extension = "VRMC_vrm";

    /// <summary>
    ///     VRM 1.0 材质扩展名称。
    /// </summary>
    public const string Vrm1MaterialExtension = "VRMC_materials_mtoon";

    /// <summary>
    ///     VRM 1.0 约束扩展名称。
    /// </summary>
    public const string Vrm1ConstraintExtension = "VRMC_node_constraint";

    /// <summary>
    ///     VRM 1.0 弹簧骨骼扩展名称。
    /// </summary>
    public const string Vrm1SpringBoneExtension = "VRMC_springBone";
}

/// <summary>
///     VRM 版本。
/// </summary>
public enum VrmVersion
{
    /// <summary>
    ///     未知版本。
    /// </summary>
    Unknown,

    /// <summary>
    ///     VRM 0.x。
    /// </summary>
    Vrm0,

    /// <summary>
    ///     VRM 1.0。
    /// </summary>
    Vrm1
}

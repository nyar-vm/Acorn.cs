namespace Acorn.Pe.Data;

/// <summary>
///     指定 PE 文件的目标机器架构。
/// </summary>
public enum MachineType : ushort
{
    /// <summary>
    ///     未知目标机器。
    /// </summary>
    Unknown = 0x0000,

    /// <summary>
    ///     Intel 386 或兼容处理器。
    /// </summary>
    I386 = 0x014c,

    /// <summary>
    ///     Intel Itanium 处理器家族。
    /// </summary>
    IA64 = 0x0200,

    /// <summary>
    ///     AMD64（x64）处理器。
    /// </summary>
    AMD64 = 0x8664,

    /// <summary>
    ///     ARM 处理器。
    /// </summary>
    ARM = 0x01c0,

    /// <summary>
    ///     ARM Thumb 处理器。
    /// </summary>
    ARMThumb = 0x01c2,

    /// <summary>
    ///     ARM64（AArch64）处理器。
    /// </summary>
    ARM64 = 0xaa64,

    /// <summary>
    ///     Mitsubishi M32R 处理器。
    /// </summary>
    M32R = 0x9041,

    /// <summary>
    ///     MIPS16 处理器。
    /// </summary>
    MIPS16 = 0x0266,

    /// <summary>
    ///     MIPS FPU 处理器。
    /// </summary>
    MIPSFPU = 0x0366,

    /// <summary>
    ///     MIPS FPU16 处理器。
    /// </summary>
    MIPSFPU16 = 0x0466,

    /// <summary>
    ///     Power PC 小端序。
    /// </summary>
    PowerPC = 0x01f0,

    /// <summary>
    ///     Power PC 浮点支持。
    /// </summary>
    PowerPCFP = 0x01f1,

    /// <summary>
    ///     Hitachi SH3 处理器。
    /// </summary>
    SH3 = 0x01a2,

    /// <summary>
    ///     Hitachi SH3 DSP 处理器。
    /// </summary>
    SH3DSP = 0x01a3,

    /// <summary>
    ///     Hitachi SH4 处理器。
    /// </summary>
    SH4 = 0x01a6,

    /// <summary>
    ///     Hitachi SH5 处理器。
    /// </summary>
    SH5 = 0x01a8,

    /// <summary>
    ///     ARM Thumb-2 小端序。
    /// </summary>
    ARMNT = 0x01c4
}

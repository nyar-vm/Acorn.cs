using System;

namespace Acorn.Pe.Data;

/// <summary>
/// 定义 PE 文件的特征标志，指示文件的各种属性。
/// </summary>
[Flags]
public enum FileCharacteristics : ushort
{
    /// <summary>
    /// 未定义任何特征。
    /// </summary>
    None = 0x0000,

    /// <summary>
    /// 重定位信息已从文件中剥离，文件必须加载到其首选基址。
    /// </summary>
    RelocsStripped = 0x0001,

    /// <summary>
    /// 文件是可执行的（可以直接运行）。
    /// </summary>
    ExecutableImage = 0x0002,

    /// <summary>
    /// COFF 行号信息已从文件中剥离。
    /// </summary>
    LineNumsStripped = 0x0004,

    /// <summary>
    /// COFF 符号表条目已从文件中剥离。
    /// </summary>
    LocalSymsStripped = 0x0008,

    /// <summary>
    /// 积极修剪工作集。
    /// </summary>
    AggressiveWsTrim = 0x0010,

    /// <summary>
    /// 应用程序可以处理大于 2 GB 的地址。
    /// </summary>
    LargeAddressAware = 0x0020,

    /// <summary>
    /// 小端字节序（已弃用）。
    /// </summary>
    BytesReversedLo = 0x0080,

    /// <summary>
    /// 文件在 32 位机器上运行。
    /// </summary>
    Machine32Bit = 0x0100,

    /// <summary>
    /// 调试信息已从文件中剥离。
    /// </summary>
    DebugStripped = 0x0200,

    /// <summary>
    /// 文件设计为在可移动介质上运行。
    /// </summary>
    RemovableRunFromSwap = 0x0400,

    /// <summary>
    /// 文件设计为从网络运行。
    /// </summary>
    NetRunFromSwap = 0x0800,

    /// <summary>
    /// 文件是系统文件。
    /// </summary>
    System = 0x1000,

    /// <summary>
    /// 文件是动态链接库（DLL）。
    /// </summary>
    Dll = 0x2000,

    /// <summary>
    /// 文件仅在单处理器机器上运行。
    /// </summary>
    UpSystemOnly = 0x4000,

    /// <summary>
    /// 大端字节序（已弃用）。
    /// </summary>
    BytesReversedHi = 0x8000
}

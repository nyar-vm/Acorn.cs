using Acorn.Frame;

namespace Acorn.Spirv.Scanner;

/// <summary>
///     SPIR-V 格式扫描器接口，提供对 SPIR-V 着色器二进制数据的快速探查能力。
/// </summary>
/// <remarks>
///     SPIR-V 是 Khronos 定义的着色器二进制中间语言，用于 Vulkan、OpenCL 等图形和计算 API。
///     扫描器专注于快速识别 SPIR-V 版本、入口点、能力声明等元信息，
///     不做完整的指令反序列化，以实现零分配高性能扫描。
/// </remarks>
public interface ISpirvScanner
{
    /// <summary>
    ///     读取 SPIR-V 小端序 32 位无符号整数并前进 4 字节。
    /// </summary>
    /// <returns>无符号 32 位整数值。</returns>
    uint ReadSpirvWord();

    /// <summary>
    ///     读取 SPIR-V 指令头（操作码和字数）并前进 4 字节。
    /// </summary>
    /// <returns>包含操作码和字数的元组。</returns>
    (ushort Opcode, ushort WordCount) ReadInstructionHeader();

    /// <summary>
    ///     读取 SPIR-V 字序列中的字符串（null 终止，4 字节对齐）。
    /// </summary>
    /// <returns>解码后的字符串。</returns>
    string ReadSpirvString();
}

namespace Acorn.Wasm.Scanner;

/// <summary>
///     Wasm 格式扫描器接口，提供对 WebAssembly 二进制数据的快速探查能力。
/// </summary>
/// <remarks>
///     WebAssembly 是一种可移植、体积小、加载快的二进制指令格式。
///     扫描器专注于快速识别 Wasm 文件版本、段信息、导入导出数量等元信息，
///     不做完整的对象反序列化，以实现零分配高性能扫描。
/// </remarks>
public interface IWasmScanner
{
    /// <summary>
    ///     读取 Wasm 文件头中的版本号。
    /// </summary>
    /// <returns>版本号。</returns>
    uint ReadVersion();

    /// <summary>
    ///     读取 Wasm 变长名称（LEB128 长度前缀 + UTF-8 字节）。
    /// </summary>
    /// <returns>名称字符串。</returns>
    string ReadName();

    /// <summary>
    ///     读取 LEB128 编码的无符号 32 位整数。
    /// </summary>
    /// <returns>无符号 32 位整数值。</returns>
    uint ReadLeb128UInt32();
}

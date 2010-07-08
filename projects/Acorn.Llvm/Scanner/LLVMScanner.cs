using System.Text;
using Acorn.LLVM.Data;
using Acorn.LLVM.Decode;

namespace Acorn.LLVM.Scanner;

/// <summary>
///     LLVM 位码文件扫描器，提供对 LLVM 位码的快速结构扫描。
/// </summary>
public class LLVMScanner
{
    /// <summary>
    ///     扫描 LLVM 位码文件，提取结构信息。
    /// </summary>
    /// <param name="data">LLVM 位码二进制数据。</param>
    /// <returns>扫描结果。</returns>
    public static string Scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("LLVM Bitcode File Scan Result:");
        result.AppendLine("=============================");

        if (data.Length < 4)
        {
            result.AppendLine("❌ 文件数据过短");
            return result.ToString();
        }

        if (data[0] != 0x42 || data[1] != 0x43 || data[2] != 0xC0 || data[3] != 0xDE)
        {
            result.AppendLine("❌ 不是有效的 LLVM 位码文件");
            return result.ToString();
        }

        result.AppendLine("✅ 有效的 LLVM 位码文件");
        result.AppendLine();

        try
        {
            var decoder = new LLVMDecoder();
            var bitcodeFile = decoder.Decode(data);

            result.AppendLine($"📋 版本: {bitcodeFile.Magic.Version}");
            result.AppendLine($"📋 包装格式: {(bitcodeFile.Magic.IsWrapped ? "是" : "否")}");
            result.AppendLine();

            result.AppendLine($"📦 顶层块数量: {bitcodeFile.TopLevelBlocks.Count}");
            if (bitcodeFile.TopLevelBlocks.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("块列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var block in bitcodeFile.TopLevelBlocks)
                {
                    PrintBlock(result, block, 0);
                }
            }
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错误: {ex.Message}");
        }

        return result.ToString();
    }

    /// <summary>
    ///     打印块信息。
    /// </summary>
    private static void PrintBlock(StringBuilder result, LLVMBlockData block, int indent)
    {
        var prefix = new string(' ', indent * 2);
        result.AppendLine($"{prefix}📦 块 ID: {block.BlockID} (大小: {block.BlockSize} bits)");
        result.AppendLine($"{prefix}   记录数量: {block.Records.Count}, 子块数量: {block.SubBlocks.Count}");

        if (block.Records.Count > 0)
        {
            foreach (var record in block.Records.Take(5))
            {
                result.AppendLine($"{prefix}   📄 记录代码: {record.Code}, 操作数: {record.Operands.Count}");
            }

            if (block.Records.Count > 5)
            {
                result.AppendLine($"{prefix}   ... 等 {block.Records.Count - 5} 个记录");
            }
        }

        foreach (var subBlock in block.SubBlocks)
        {
            PrintBlock(result, subBlock, indent + 1);
        }
    }
}

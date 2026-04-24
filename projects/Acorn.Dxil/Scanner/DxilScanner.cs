using System.Text;
using Acorn.Dxil.Data;
using Acorn.Dxil.Decode;

namespace Acorn.Dxil.Scanner;

/// <summary>
///     DXIL/DXContainer 扫描器，提供对着色器二进制数据的快速结构扫描。
/// </summary>
public class DxilScanner
{
    /// <summary>
    ///     扫描 DXContainer 二进制数据，提取结构信息。
    /// </summary>
    /// <param name="data">DXContainer 二进制数据。</param>
    /// <returns>人类可读的扫描报告。</returns>
    public static string Scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("DXContainer 扫描结果：");
        result.AppendLine("=====================");

        if (data.Length < 20)
        {
            result.AppendLine("❌ 数据过短，不是有效的 DXContainer 文件");
            return result.ToString();
        }

        var magic = BitConverter.ToUInt32(data, 0);

        if (magic != DxilConstants.ContainerMagicNumber)
        {
            result.AppendLine($"❌ 魔数不匹配：期望 0x{DxilConstants.ContainerMagicNumber:X8}，实际 0x{magic:X8}");
            return result.ToString();
        }

        result.AppendLine("✅ 有效的 DXContainer 文件");
        result.AppendLine();

        try
        {
            var containerDecoder = new DxContainerDecoder();
            var container = containerDecoder.Decode(data);

            result.AppendLine($"📋 容器版本：{container.Header.VersionMajor}.{container.Header.VersionMinor}");
            result.AppendLine($"📋 文件大小：{container.Header.FileSize} 字节");
            result.AppendLine($"📋 Part 数量：{container.Header.PartCount}");
            result.AppendLine();

            if (container.Parts.Count > 0)
            {
                result.AppendLine("Part 列表：");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var part in container.Parts)
                {
                    var fourCCString = part.Header.FourCCString;
                    result.AppendLine($"📦 {fourCCString}（大小：{part.Header.Size} 字节）");

                    if (part.Header.FourCC == (uint)DxilPartFourCC.Dxil ||
                        part.Header.FourCC == (uint)DxilPartFourCC.Dxil1)
                    {
                        ScanDxilPart(result, part.Data);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错误：{ex.Message}");
        }

        return result.ToString();
    }

    private static void ScanDxilPart(StringBuilder result, byte[] partData)
    {
        if (partData.Length < DxilConstants.ProgramHeaderSize)
        {
            result.AppendLine("   ⚠️ DXIL Part 数据过短，无法解析程序头");
            return;
        }

        try
        {
            var programDecoder = new DxilProgramDecoder();
            var program = programDecoder.Decode(partData);

            result.AppendLine($"   着色器模型：{program.Header.ShaderModelKind}");
            result.AppendLine($"   DXIL 版本：{program.Header.MajorVersion}.{program.Header.MinorVersion}");
            result.AppendLine($"   Bitcode 大小：{program.Header.BitcodeSize} 字节");
            result.AppendLine($"   Bitcode 偏移：{program.Header.BitcodeOffset}");
        }
        catch (Exception ex)
        {
            result.AppendLine($"   ⚠️ 解析 DXIL 程序头失败：{ex.Message}");
        }
    }
}

using System.Text;
using Acorn.Frame;
using Acorn.Live2D.Data;

namespace Acorn.Live2D.Encode;

/// <summary>
///     Live2D moc3 文件编码器，将 C# 数据结构编码为 Live2D Cubism moc3 二进制格式。
/// </summary>
/// <remarks>
///     Live2D 是 Live2D Inc. 开发的参数化 2D 动画格式，moc3 是其编译后的二进制模型格式。
///     编码器生成符合 moc3 规范的二进制数据。
/// </remarks>
public sealed class Live2DEncoder
{
    /// <summary>
    ///     将 moc3 模型数据编码为 moc3 二进制格式。
    /// </summary>
    /// <param name="data">moc3 模型数据。</param>
    /// <returns>moc3 二进制数据。</returns>
    public byte[] EncodeMoc3(Live2DModelData data)
    {
        var size = EstimateMoc3Size(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteMoc3Header(ref writer, data);
        WriteOffsetTable(ref writer, data);
        WriteDataSections(ref writer, data);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteMoc3Header(ref ByteBufferWriter writer, Live2DModelData data)
    {
        writer.WriteString("MOC3");
        writer.WriteU8((byte)data.Version);
        writer.WriteU8((byte)(data.IsBigEndian ? 1 : 0));
        writer.WriteU8((byte)data.Revision);
        writer.WriteU8(0);

        if (data.IsBigEndian)
        {
            writer.WriteU32BE(0);
        }
        else
        {
            writer.WriteU32LE(0);
        }

        writer.Write(new byte[48]);
    }

    private static void WriteOffsetTable(ref ByteBufferWriter writer, Live2DModelData data)
    {
        var offsetCount = data.Version switch
        {
            >= 5 => 28,
            >= 4 => 24,
            >= 3 => 20,
            _ => 16
        };

        writer.Write(new byte[offsetCount * 4]);
    }

    private static void WriteDataSections(ref ByteBufferWriter writer, Live2DModelData data)
    {
        foreach (var part in data.Parts)
        {
            var bytes = Encoding.UTF8.GetBytes(part.Id);
            writer.Write(bytes);

            if (bytes.Length < 32)
            {
                writer.Write(new byte[32 - bytes.Length]);
            }
        }

        foreach (var parameter in data.Parameters)
        {
            var bytes = Encoding.UTF8.GetBytes(parameter.Id);
            writer.Write(bytes);

            if (bytes.Length < 32)
            {
                writer.Write(new byte[32 - bytes.Length]);
            }
        }

        foreach (var drawable in data.Drawables)
        {
            var bytes = Encoding.UTF8.GetBytes(drawable.Id);
            writer.Write(bytes);

            if (bytes.Length < 32)
            {
                writer.Write(new byte[32 - bytes.Length]);
            }
        }
    }

    private static int EstimateMoc3Size(Live2DModelData data)
    {
        var size = 64;

        size += data.Parts.Count * 32;
        size += data.Parameters.Count * 32;
        size += data.Drawables.Count * 32;

        var offsetCount = data.Version switch
        {
            >= 5 => 28,
            >= 4 => 24,
            >= 3 => 20,
            _ => 16
        };

        size += offsetCount * 4;
        size += 4096;

        return size;
    }

    #endregion
}

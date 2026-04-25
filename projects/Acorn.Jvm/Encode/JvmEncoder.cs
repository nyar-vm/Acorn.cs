using System;
using System.IO;
using System.Text;

namespace Acorn.Jvm.Encode;

/// <summary>
///     JVM ClassFile 编码器
/// </summary>
public sealed class JvmEncoder
{
    /// <summary>
    ///     编码 ClassFile 为字节数组
    /// </summary>
    public byte[] Encode(JvmClassFileData classFile)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // 写入魔数
        writer.Write(classFile.Magic);

        // 写入版本号
        writer.Write(classFile.MinorVersion);
        writer.Write(classFile.MajorVersion);

        // 写入常量池
        writer.Write((ushort)classFile.ConstantPool.Count + 1); // 常量池索引从 1 开始
        foreach (var constant in classFile.ConstantPool)
        {
            EncodeConstant(writer, constant);
        }

        // 写入访问标志
        writer.Write(classFile.AccessFlags);

        // 写入类索引
        writer.Write(classFile.ThisClass);
        writer.Write(classFile.SuperClass);

        // 写入接口
        writer.Write((ushort)classFile.Interfaces.Count);
        foreach (var iface in classFile.Interfaces)
        {
            writer.Write(iface);
        }

        // 写入字段
        writer.Write((ushort)classFile.Fields.Count);
        foreach (var field in classFile.Fields)
        {
            EncodeFieldInfo(writer, field);
        }

        // 写入方法
        writer.Write((ushort)classFile.Methods.Count);
        foreach (var method in classFile.Methods)
        {
            EncodeMethodInfo(writer, method);
        }

        // 写入属性
        writer.Write((ushort)classFile.Attributes.Count);
        foreach (var attribute in classFile.Attributes)
        {
            EncodeAttributeInfo(writer, attribute);
        }

        return ms.ToArray();
    }

    /// <summary>
    ///     编码常量池项
    /// </summary>
    private void EncodeConstant(BinaryWriter writer, JvmConstant constant)
    {
        writer.Write((byte)constant.Kind);
        switch (constant)
        {
            case JvmConstantUtf8 utf8:
                var bytes = Encoding.UTF8.GetBytes(utf8.Value);
                writer.Write((ushort)bytes.Length);
                writer.Write(bytes);
                break;
            case JvmConstantInteger integer:
                writer.Write(integer.Value);
                break;
            case JvmConstantFloat single:
                writer.Write(single.Value);
                break;
            case JvmConstantLong l:
                writer.Write(l.Value);
                break;
            case JvmConstantDouble d:
                writer.Write(d.Value);
                break;
            case JvmConstantClass cls:
                writer.Write(cls.NameIndex);
                break;
            case JvmConstantString str:
                writer.Write(str.StringIndex);
                break;
            case JvmConstantFieldref fieldref:
                writer.Write(fieldref.ClassIndex);
                writer.Write(fieldref.NameAndTypeIndex);
                break;
            case JvmConstantMethodref methodref:
                writer.Write(methodref.ClassIndex);
                writer.Write(methodref.NameAndTypeIndex);
                break;
            case JvmConstantInterfaceMethodref imethodref:
                writer.Write(imethodref.ClassIndex);
                writer.Write(imethodref.NameAndTypeIndex);
                break;
            case JvmConstantNameAndType nat:
                writer.Write(nat.NameIndex);
                writer.Write(nat.DescriptorIndex);
                break;
            case JvmConstantMethodHandle mh:
                writer.Write(mh.ReferenceKind);
                writer.Write(mh.ReferenceIndex);
                break;
            case JvmConstantMethodType mt:
                writer.Write(mt.DescriptorIndex);
                break;
            case JvmConstantInvokeDynamic id:
                writer.Write(id.BootstrapMethodAttrIndex);
                writer.Write(id.NameAndTypeIndex);
                break;
        }
    }

    /// <summary>
    ///     编码字段信息
    /// </summary>
    private void EncodeFieldInfo(BinaryWriter writer, JvmFieldInfo field)
    {
        writer.Write(field.AccessFlags);
        writer.Write(field.NameIndex);
        writer.Write(field.DescriptorIndex);
        writer.Write((ushort)field.Attributes.Count);
        foreach (var attribute in field.Attributes)
        {
            EncodeAttributeInfo(writer, attribute);
        }
    }

    /// <summary>
    ///     编码方法信息
    /// </summary>
    private void EncodeMethodInfo(BinaryWriter writer, JvmMethodInfo method)
    {
        writer.Write(method.AccessFlags);
        writer.Write(method.NameIndex);
        writer.Write(method.DescriptorIndex);
        writer.Write((ushort)method.Attributes.Count);
        foreach (var attribute in method.Attributes)
        {
            EncodeAttributeInfo(writer, attribute);
        }
    }

    /// <summary>
    ///     编码属性信息
    /// </summary>
    private void EncodeAttributeInfo(BinaryWriter writer, JvmAttributeInfo attribute)
    {
        writer.Write(attribute.AttributeNameIndex);
        writer.Write(attribute.AttributeLength);
        // 简化实现，只写入长度，不写入具体内容
    }
}

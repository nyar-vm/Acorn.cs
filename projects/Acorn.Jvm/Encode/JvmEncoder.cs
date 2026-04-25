using System.Text;
using Acorn.Frame;
using Acorn.Jvm.Data;

namespace Acorn.Jvm.Encode;

/// <summary>
///     JVM ClassFile 编码器，将 JvmClassFileData 编码为 .class 二进制格式。
///     JVM ClassFile 格式要求大端序（Big-Endian），本编码器使用 Acorn.Frame.ByteBufferWriter 的大端序写入方法。
/// </summary>
public sealed class JvmEncoder
{
    /// <summary>
    ///     编码 ClassFile 为字节数组
    /// </summary>
    public byte[] Encode(JvmClassFileData classFile)
    {
        var size = EstimateSize(classFile);
        var writer = new ByteBufferWriter(size);

        WriteHeader(ref writer, classFile);
        WriteConstantPool(ref writer, classFile.ConstantPool);
        WriteAccessFlags(ref writer, classFile.AccessFlags);
        WriteClassIndices(ref writer, classFile);
        WriteInterfaces(ref writer, classFile.Interfaces);
        WriteFields(ref writer, classFile.Fields);
        WriteMethods(ref writer, classFile.Methods);
        WriteAttributes(ref writer, classFile.Attributes);

        return writer.ToArray();
    }

    #region 头部

    private static void WriteHeader(ref ByteBufferWriter writer, JvmClassFileData classFile)
    {
        writer.WriteU32BE(classFile.Magic);
        writer.WriteU16BE(classFile.MinorVersion);
        writer.WriteU16BE(classFile.MajorVersion);
    }

    #endregion

    #region 常量池

    private static void WriteConstantPool(ref ByteBufferWriter writer, IReadOnlyList<JvmConstant> constantPool)
    {
        writer.WriteU16BE((ushort)(constantPool.Count + 1));

        foreach (var constant in constantPool)
        {
            EncodeConstant(ref writer, constant);
        }
    }

    private static void EncodeConstant(ref ByteBufferWriter writer, JvmConstant constant)
    {
        writer.WriteU8((byte)constant.Kind);

        switch (constant)
        {
            case JvmConstantUtf8 utf8:
                var bytes = Encoding.UTF8.GetBytes(utf8.Value);
                writer.WriteU16BE((ushort)bytes.Length);
                writer.Write(bytes);
                break;
            case JvmConstantInteger integer:
                writer.WriteI32BE(integer.Value);
                break;
            case JvmConstantFloat single:
                writer.WriteF32BE(single.Value);
                break;
            case JvmConstantLong l:
                writer.WriteI64BE(l.Value);
                break;
            case JvmConstantDouble d:
                writer.WriteF64BE(d.Value);
                break;
            case JvmConstantClass cls:
                writer.WriteU16BE(cls.NameIndex);
                break;
            case JvmConstantString str:
                writer.WriteU16BE(str.StringIndex);
                break;
            case JvmConstantFieldref fieldref:
                writer.WriteU16BE(fieldref.ClassIndex);
                writer.WriteU16BE(fieldref.NameAndTypeIndex);
                break;
            case JvmConstantMethodref methodref:
                writer.WriteU16BE(methodref.ClassIndex);
                writer.WriteU16BE(methodref.NameAndTypeIndex);
                break;
            case JvmConstantInterfaceMethodref imethodref:
                writer.WriteU16BE(imethodref.ClassIndex);
                writer.WriteU16BE(imethodref.NameAndTypeIndex);
                break;
            case JvmConstantNameAndType nat:
                writer.WriteU16BE(nat.NameIndex);
                writer.WriteU16BE(nat.DescriptorIndex);
                break;
            case JvmConstantMethodHandle mh:
                writer.WriteU8(mh.ReferenceKind);
                writer.WriteU16BE(mh.ReferenceIndex);
                break;
            case JvmConstantMethodType mt:
                writer.WriteU16BE(mt.DescriptorIndex);
                break;
            case JvmConstantInvokeDynamic id:
                writer.WriteU16BE(id.BootstrapMethodAttrIndex);
                writer.WriteU16BE(id.NameAndTypeIndex);
                break;
        }
    }

    #endregion

    #region 访问标志和类索引

    private static void WriteAccessFlags(ref ByteBufferWriter writer, ushort accessFlags)
    {
        writer.WriteU16BE(accessFlags);
    }

    private static void WriteClassIndices(ref ByteBufferWriter writer, JvmClassFileData classFile)
    {
        writer.WriteU16BE(classFile.ThisClass);
        writer.WriteU16BE(classFile.SuperClass);
    }

    #endregion

    #region 接口

    private static void WriteInterfaces(ref ByteBufferWriter writer, IReadOnlyList<ushort> interfaces)
    {
        writer.WriteU16BE((ushort)interfaces.Count);

        foreach (var iface in interfaces)
        {
            writer.WriteU16BE(iface);
        }
    }

    #endregion

    #region 字段

    private static void WriteFields(ref ByteBufferWriter writer, IReadOnlyList<JvmFieldInfo> fields)
    {
        writer.WriteU16BE((ushort)fields.Count);

        foreach (var field in fields)
        {
            WriteFieldInfo(ref writer, field);
        }
    }

    private static void WriteFieldInfo(ref ByteBufferWriter writer, JvmFieldInfo field)
    {
        writer.WriteU16BE(field.AccessFlags);
        writer.WriteU16BE(field.NameIndex);
        writer.WriteU16BE(field.DescriptorIndex);
        WriteAttributes(ref writer, field.Attributes);
    }

    #endregion

    #region 方法

    private static void WriteMethods(ref ByteBufferWriter writer, IReadOnlyList<JvmMethodInfo> methods)
    {
        writer.WriteU16BE((ushort)methods.Count);

        foreach (var method in methods)
        {
            WriteMethodInfo(ref writer, method);
        }
    }

    private static void WriteMethodInfo(ref ByteBufferWriter writer, JvmMethodInfo method)
    {
        writer.WriteU16BE(method.AccessFlags);
        writer.WriteU16BE(method.NameIndex);
        writer.WriteU16BE(method.DescriptorIndex);
        WriteAttributes(ref writer, method.Attributes);
    }

    #endregion

    #region 属性

    private static void WriteAttributes(ref ByteBufferWriter writer, IReadOnlyList<JvmAttributeInfo> attributes)
    {
        writer.WriteU16BE((ushort)attributes.Count);

        foreach (var attribute in attributes)
        {
            WriteAttributeInfo(ref writer, attribute);
        }
    }

    private static void WriteAttributeInfo(ref ByteBufferWriter writer, JvmAttributeInfo attribute)
    {
        writer.WriteU16BE(attribute.AttributeNameIndex);

        switch (attribute)
        {
            case JvmCodeAttribute code:
                WriteCodeAttribute(ref writer, code);
                break;
            case JvmConstantValueAttribute cv:
                writer.WriteU32BE(2);
                writer.WriteU16BE(cv.ConstantValueIndex);
                break;
            case JvmSourceFileAttribute sf:
                writer.WriteU32BE(2);
                writer.WriteU16BE(sf.SourceFileIndex);
                break;
            case JvmLineNumberTableAttribute lnt:
                WriteLineNumberTableAttribute(ref writer, lnt);
                break;
            case JvmLocalVariableTableAttribute lvt:
                WriteLocalVariableTableAttribute(ref writer, lvt);
                break;
            case JvmInnerClassesAttribute ic:
                WriteInnerClassesAttribute(ref writer, ic);
                break;
            case JvmBootstrapMethodsAttribute bm:
                WriteBootstrapMethodsAttribute(ref writer, bm);
                break;
            default:
                writer.WriteU32BE(attribute.AttributeLength);
                break;
        }
    }

    private static void WriteCodeAttribute(ref ByteBufferWriter writer, JvmCodeAttribute code)
    {
        var codeAttrWriter = new ByteBufferWriter(code.Code.Length + 128);

        codeAttrWriter.WriteU16BE(code.MaxStack);
        codeAttrWriter.WriteU16BE(code.MaxLocals);
        codeAttrWriter.WriteI32BE(code.Code.Length);
        codeAttrWriter.Write(code.Code);

        codeAttrWriter.WriteU16BE((ushort)code.ExceptionTable.Count);

        foreach (var entry in code.ExceptionTable)
        {
            codeAttrWriter.WriteU16BE(entry.StartPc);
            codeAttrWriter.WriteU16BE(entry.EndPc);
            codeAttrWriter.WriteU16BE(entry.HandlerPc);
            codeAttrWriter.WriteU16BE(entry.CatchType);
        }

        WriteAttributes(ref codeAttrWriter, code.Attributes);

        var codeAttrBytes = codeAttrWriter.ToArray();
        writer.WriteU32BE((uint)codeAttrBytes.Length);
        writer.Write(codeAttrBytes);
    }

    private static void WriteLineNumberTableAttribute(ref ByteBufferWriter writer, JvmLineNumberTableAttribute lnt)
    {
        var attrSize = 2 + lnt.LineNumberTable.Count * 4;
        writer.WriteU32BE((uint)attrSize);
        writer.WriteU16BE((ushort)lnt.LineNumberTable.Count);

        foreach (var entry in lnt.LineNumberTable)
        {
            writer.WriteU16BE(entry.StartPc);
            writer.WriteU16BE(entry.LineNumber);
        }
    }

    private static void WriteLocalVariableTableAttribute(ref ByteBufferWriter writer, JvmLocalVariableTableAttribute lvt)
    {
        var attrSize = 2 + lvt.LocalVariableTable.Count * 10;
        writer.WriteU32BE((uint)attrSize);
        writer.WriteU16BE((ushort)lvt.LocalVariableTable.Count);

        foreach (var entry in lvt.LocalVariableTable)
        {
            writer.WriteU16BE(entry.StartPc);
            writer.WriteU16BE(entry.Length);
            writer.WriteU16BE(entry.NameIndex);
            writer.WriteU16BE(entry.DescriptorIndex);
            writer.WriteU16BE(entry.Index);
        }
    }

    private static void WriteInnerClassesAttribute(ref ByteBufferWriter writer, JvmInnerClassesAttribute ic)
    {
        var attrSize = 2 + ic.Classes.Count * 8;
        writer.WriteU32BE((uint)attrSize);
        writer.WriteU16BE((ushort)ic.Classes.Count);

        foreach (var entry in ic.Classes)
        {
            writer.WriteU16BE(entry.InnerClassInfoIndex);
            writer.WriteU16BE(entry.OuterClassInfoIndex);
            writer.WriteU16BE(entry.InnerNameIndex);
            writer.WriteU16BE(entry.InnerClassAccessFlags);
        }
    }

    private static void WriteBootstrapMethodsAttribute(ref ByteBufferWriter writer, JvmBootstrapMethodsAttribute bm)
    {
        var attrSize = 2;

        foreach (var method in bm.BootstrapMethods)
        {
            attrSize += 4 + method.BootstrapArguments.Count * 2;
        }

        writer.WriteU32BE((uint)attrSize);
        writer.WriteU16BE((ushort)bm.BootstrapMethods.Count);

        foreach (var method in bm.BootstrapMethods)
        {
            writer.WriteU16BE(method.BootstrapMethodRef);
            writer.WriteU16BE((ushort)method.BootstrapArguments.Count);

            foreach (var arg in method.BootstrapArguments)
            {
                writer.WriteU16BE(arg);
            }
        }
    }

    #endregion

    #region 大小预估

    private static int EstimateSize(JvmClassFileData classFile)
    {
        var size = 10;

        size += 2;

        foreach (var constant in classFile.ConstantPool)
        {
            size += EstimateConstantSize(constant);
        }

        size += 2 + 4;
        size += 2 + classFile.Interfaces.Count * 2;
        size += 2;

        foreach (var field in classFile.Fields)
        {
            size += 8 + EstimateAttributesSize(field.Attributes);
        }

        size += 2;

        foreach (var method in classFile.Methods)
        {
            size += 8 + EstimateAttributesSize(method.Attributes);
        }

        size += 2 + EstimateAttributesSize(classFile.Attributes);

        return size;
    }

    private static int EstimateConstantSize(JvmConstant constant)
    {
        return constant switch
        {
            JvmConstantUtf8 utf8 => 3 + Encoding.UTF8.GetByteCount(utf8.Value),
            JvmConstantInteger => 5,
            JvmConstantFloat => 5,
            JvmConstantLong => 9,
            JvmConstantDouble => 9,
            JvmConstantClass => 3,
            JvmConstantString => 3,
            JvmConstantFieldref => 5,
            JvmConstantMethodref => 5,
            JvmConstantInterfaceMethodref => 5,
            JvmConstantNameAndType => 5,
            JvmConstantMethodHandle => 4,
            JvmConstantMethodType => 3,
            JvmConstantInvokeDynamic => 5,
            _ => 3
        };
    }

    private static int EstimateAttributesSize(IReadOnlyList<JvmAttributeInfo> attributes)
    {
        var size = 2;

        foreach (var attribute in attributes)
        {
            size += 6 + (int)attribute.AttributeLength;
        }

        return size;
    }

    #endregion
}

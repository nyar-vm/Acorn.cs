using System;
using System.Text;

namespace Acorn.Jvm.Data;

/// <summary>
///     JVM ClassFile 数据
/// </summary>
public sealed class JvmClassFileData
{
    /// <summary>
    ///     魔数（0xCAFEBABE）
    /// </summary>
    public uint Magic { get; init; }

    /// <summary>
    ///     主次版本号
    /// </summary>
    public ushort MinorVersion { get; init; }
    public ushort MajorVersion { get; init; }

    /// <summary>
    ///     常量池
    /// </summary>
    public IReadOnlyList<JvmConstant> ConstantPool { get; init; }

    /// <summary>
    ///     访问标志
    /// </summary>
    public ushort AccessFlags { get; init; }

    /// <summary>
    ///     本类索引
    /// </summary>
    public ushort ThisClass { get; init; }

    /// <summary>
    ///     父类索引
    /// </summary>
    public ushort SuperClass { get; init; }

    /// <summary>
    ///     接口索引表
    /// </summary>
    public IReadOnlyList<ushort> Interfaces { get; init; }

    /// <summary>
    ///     字段表
    /// </summary>
    public IReadOnlyList<JvmFieldInfo> Fields { get; init; }

    /// <summary>
    ///     方法表
    /// </summary>
    public IReadOnlyList<JvmMethodInfo> Methods { get; init; }

    /// <summary>
    ///     属性表
    /// </summary>
    public IReadOnlyList<JvmAttributeInfo> Attributes { get; init; }
}

/// <summary>
///     常量池项基类
/// </summary>
public abstract class JvmConstant
{
    /// <summary>
    ///     常量类型
    /// </summary>
    public abstract JvmConstantKind Kind { get; }
}

/// <summary>
///     常量类型
/// </summary>
public enum JvmConstantKind : byte
{
    Utf8 = 1,
    Integer = 3,
    Float = 4,
    Long = 5,
    Double = 6,
    Class = 7,
    String = 8,
    Fieldref = 9,
    Methodref = 10,
    InterfaceMethodref = 11,
    NameAndType = 12,
    MethodHandle = 15,
    MethodType = 16,
    InvokeDynamic = 18
}

/// <summary>
///     UTF-8 常量
/// </summary>
public sealed class JvmConstantUtf8 : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Utf8;
    public string Value { get; init; }
}

/// <summary>
///     整数常量
/// </summary>
public sealed class JvmConstantInteger : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Integer;
    public int Value { get; init; }
}

/// <summary>
///     浮点数常量
/// </summary>
public sealed class JvmConstantFloat : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Float;
    public float Value { get; init; }
}

/// <summary>
///     长整数常量
/// </summary>
public sealed class JvmConstantLong : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Long;
    public long Value { get; init; }
}

/// <summary>
///     双精度浮点数常量
/// </summary>
public sealed class JvmConstantDouble : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Double;
    public double Value { get; init; }
}

/// <summary>
///     类常量
/// </summary>
public sealed class JvmConstantClass : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Class;
    public ushort NameIndex { get; init; }
}

/// <summary>
///     字符串常量
/// </summary>
public sealed class JvmConstantString : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.String;
    public ushort StringIndex { get; init; }
}

/// <summary>
///     字段引用常量
/// </summary>
public sealed class JvmConstantFieldref : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Fieldref;
    public ushort ClassIndex { get; init; }
    public ushort NameAndTypeIndex { get; init; }
}

/// <summary>
///     方法引用常量
/// </summary>
public sealed class JvmConstantMethodref : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Methodref;
    public ushort ClassIndex { get; init; }
    public ushort NameAndTypeIndex { get; init; }
}

/// <summary>
///     接口方法引用常量
/// </summary>
public sealed class JvmConstantInterfaceMethodref : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.InterfaceMethodref;
    public ushort ClassIndex { get; init; }
    public ushort NameAndTypeIndex { get; init; }
}

/// <summary>
///     名称和类型常量
/// </summary>
public sealed class JvmConstantNameAndType : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.NameAndType;
    public ushort NameIndex { get; init; }
    public ushort DescriptorIndex { get; init; }
}

/// <summary>
///     方法句柄常量
/// </summary>
public sealed class JvmConstantMethodHandle : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.MethodHandle;
    public byte ReferenceKind { get; init; }
    public ushort ReferenceIndex { get; init; }
}

/// <summary>
///     方法类型常量
/// </summary>
public sealed class JvmConstantMethodType : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.MethodType;
    public ushort DescriptorIndex { get; init; }
}

/// <summary>
///     动态调用常量
/// </summary>
public sealed class JvmConstantInvokeDynamic : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.InvokeDynamic;
    public ushort BootstrapMethodAttrIndex { get; init; }
    public ushort NameAndTypeIndex { get; init; }
}

/// <summary>
///     字段信息
/// </summary>
public sealed class JvmFieldInfo
{
    public ushort AccessFlags { get; init; }
    public ushort NameIndex { get; init; }
    public ushort DescriptorIndex { get; init; }
    public IReadOnlyList<JvmAttributeInfo> Attributes { get; init; }
}

/// <summary>
///     方法信息
/// </summary>
public sealed class JvmMethodInfo
{
    public ushort AccessFlags { get; init; }
    public ushort NameIndex { get; init; }
    public ushort DescriptorIndex { get; init; }
    public IReadOnlyList<JvmAttributeInfo> Attributes { get; init; }
}

/// <summary>
///     属性信息基类
/// </summary>
public abstract class JvmAttributeInfo
{
    public ushort AttributeNameIndex { get; init; }
    public uint AttributeLength { get; init; }
}

/// <summary>
///     代码属性
/// </summary>
public sealed class JvmCodeAttribute : JvmAttributeInfo
{
    public ushort MaxStack { get; init; }
    public ushort MaxLocals { get; init; }
    public uint CodeLength { get; init; }
    public byte[] Code { get; init; }
    public ushort ExceptionTableLength { get; init; }
    public IReadOnlyList<JvmExceptionTableEntry> ExceptionTable { get; init; }
    public ushort AttributesCount { get; init; }
    public IReadOnlyList<JvmAttributeInfo> Attributes { get; init; }
}

/// <summary>
///     异常表项
/// </summary>
public sealed class JvmExceptionTableEntry
{
    public ushort StartPc { get; init; }
    public ushort EndPc { get; init; }
    public ushort HandlerPc { get; init; }
    public ushort CatchType { get; init; }
}

/// <summary>
///     行号表属性
/// </summary>
public sealed class JvmLineNumberTableAttribute : JvmAttributeInfo
{
    public ushort LineNumberTableLength { get; init; }
    public IReadOnlyList<JvmLineNumberEntry> LineNumberTable { get; init; }
}

/// <summary>
///     行号表项
/// </summary>
public sealed class JvmLineNumberEntry
{
    public ushort StartPc { get; init; }
    public ushort LineNumber { get; init; }
}

/// <summary>
///     局部变量表属性
/// </summary>
public sealed class JvmLocalVariableTableAttribute : JvmAttributeInfo
{
    public ushort LocalVariableTableLength { get; init; }
    public IReadOnlyList<JvmLocalVariableEntry> LocalVariableTable { get; init; }
}

/// <summary>
///     局部变量表项
/// </summary>
public sealed class JvmLocalVariableEntry
{
    public ushort StartPc { get; init; }
    public ushort Length { get; init; }
    public ushort NameIndex { get; init; }
    public ushort DescriptorIndex { get; init; }
    public ushort Index { get; init; }
}

/// <summary>
///     常量值属性
/// </summary>
public sealed class JvmConstantValueAttribute : JvmAttributeInfo
{
    public ushort ConstantValueIndex { get; init; }
}

/// <summary>
///     源文件属性
/// </summary>
public sealed class JvmSourceFileAttribute : JvmAttributeInfo
{
    public ushort SourceFileIndex { get; init; }
}

/// <summary>
///     内部类属性
/// </summary>
public sealed class JvmInnerClassesAttribute : JvmAttributeInfo
{
    public ushort NumberOfClasses { get; init; }
    public IReadOnlyList<JvmInnerClassInfo> Classes { get; init; }
}

/// <summary>
///     内部类信息
/// </summary>
public sealed class JvmInnerClassInfo
{
    public ushort InnerClassInfoIndex { get; init; }
    public ushort OuterClassInfoIndex { get; init; }
    public ushort InnerNameIndex { get; init; }
    public ushort InnerClassAccessFlags { get; init; }
}

/// <summary>
///     引导方法表属性
/// </summary>
public sealed class JvmBootstrapMethodsAttribute : JvmAttributeInfo
{
    public ushort NumBootstrapMethods { get; init; }
    public IReadOnlyList<JvmBootstrapMethod> BootstrapMethods { get; init; }
}

/// <summary>
///     引导方法
/// </summary>
public sealed class JvmBootstrapMethod
{
    public ushort BootstrapMethodRef { get; init; }
    public ushort NumBootstrapArguments { get; init; }
    public IReadOnlyList<ushort> BootstrapArguments { get; init; }
}

/// <summary>
///     JVM 操作码
/// </summary>
public enum JvmOpcode : byte
{
    Nop = 0,
    AconstNull = 1,
    IconstM1 = 2,
    Iconst0 = 3,
    Iconst1 = 4,
    Iconst2 = 5,
    Iconst3 = 6,
    Iconst4 = 7,
    Iconst5 = 8,
    Lconst0 = 9,
    Lconst1 = 10,
    Fconst0 = 11,
    Fconst1 = 12,
    Fconst2 = 13,
    Dconst0 = 14,
    Dconst1 = 15,
    Bipush = 16,
    Sipush = 17,
    Ldc = 18,
    LdcW = 19,
    Ldc2W = 20,
    Iload = 21,
    Lload = 22,
    Fload = 23,
    Dload = 24,
    Aload = 25,
    Iload0 = 26,
    Iload1 = 27,
    Iload2 = 28,
    Iload3 = 29,
    Lload0 = 30,
    Lload1 = 31,
    Lload2 = 32,
    Lload3 = 33,
    Fload0 = 34,
    Fload1 = 35,
    Fload2 = 36,
    Fload3 = 37,
    Dload0 = 38,
    Dload1 = 39,
    Dload2 = 40,
    Dload3 = 41,
    Aload0 = 42,
    Aload1 = 43,
    Aload2 = 44,
    Aload3 = 45,
    Iaload = 46,
    Laload = 47,
    Faload = 48,
    Daload = 49,
    Aaload = 50,
    Baload = 51,
    Caload = 52,
    Saload = 53,
    Istore = 54,
    Lstore = 55,
    Fstore = 56,
    Dstore = 57,
    Astore = 58,
    Istore0 = 59,
    Istore1 = 60,
    Istore2 = 61,
    Istore3 = 62,
    Lstore0 = 63,
    Lstore1 = 64,
    Lstore2 = 65,
    Lstore3 = 66,
    Fstore0 = 67,
    Fstore1 = 68,
    Fstore2 = 69,
    Fstore3 = 70,
    Dstore0 = 71,
    Dstore1 = 72,
    Dstore2 = 73,
    Dstore3 = 74,
    Astore0 = 75,
    Astore1 = 76,
    Astore2 = 77,
    Astore3 = 78,
    Iastore = 79,
    Lastore = 80,
    Fastore = 81,
    Dastore = 82,
    Aastore = 83,
    Bastore = 84,
    Castore = 85,
    Sastore = 86,
    Pop = 87,
    Pop2 = 88,
    Dup = 89,
    DupX1 = 90,
    DupX2 = 91,
    Dup2 = 92,
    Dup2X1 = 93,
    Dup2X2 = 94,
    Swap = 95,
    Iadd = 96,
    Ladd = 97,
    Fadd = 98,
    Dadd = 99,
    Isub = 100,
    Lsub = 101,
    Fsub = 102,
    Dsub = 103,
    Imul = 104,
    Lmul = 105,
    Fmul = 106,
    Dmul = 107,
    Idiv = 108,
    Ldiv = 109,
    Fdiv = 110,
    Ddiv = 111,
    Irem = 112,
    Lrem = 113,
    Frem = 114,
    Drem = 115,
    Ineg = 116,
    Lneg = 117,
    Fneg = 118,
    Dneg = 119,
    Ishl = 120,
    Lshl = 121,
    Ishr = 122,
    Lshr = 123,
    Iushr = 124,
    Lushr = 125,
    Iand = 126,
    Land = 127,
    Ior = 128,
    Lor = 129,
    Ixor = 130,
    Lxor = 131,
    Iinc = 132,
    I2l = 133,
    I2f = 134,
    I2d = 135,
    L2i = 136,
    L2f = 137,
    L2d = 138,
    F2i = 139,
    F2l = 140,
    F2d = 141,
    D2i = 142,
    D2l = 143,
    D2f = 144,
    I2b = 145,
    I2c = 146,
    I2s = 147,
    Lcmp = 148,
    Fcmpl = 149,
    Fcmpg = 150,
    Dcmpl = 151,
    Dcmpg = 152,
    Ifeq = 153,
    Ifne = 154,
    Iflt = 155,
    Ifge = 156,
    Ifgt = 157,
    Ifle = 158,
    Iflt = 159,
    Ificmpeq = 160,
    Ificmpne = 161,
    Ificmplt = 162,
    Ificmpge = 163,
    Ificmpgt = 164,
    Ificmple = 165,
    Ifacmpeq = 166,
    Ifacmpne = 167,
    Goto = 168,
    Jsr = 169,
    Ret = 170,
    Tableswitch = 171,
    Lookupswitch = 172,
    Ireturn = 176,
    Lreturn = 177,
    Freturn = 178,
    Dreturn = 179,
    Areturn = 180,
    Return = 181,
    Getstatic = 178,
    Putstatic = 179,
    Getfield = 180,
    Putfield = 181,
    Invokevirtual = 182,
    Invokespecial = 183,
    Invokestatic = 184,
    Invokeinterface = 185,
    Invokedynamic = 186,
    New = 187,
    Newarray = 188,
    Anewarray = 189,
    Arraylength = 190,
    Athrow = 191,
    Checkcast = 192,
    Instanceof = 193,
    Monitorenter = 194,
    Monitorexit = 195,
    Wide = 196,
    Multianewarray = 197,
    Ifnull = 198,
    Ifnonnull = 199,
    GotoW = 200,
    JsrW = 201
}

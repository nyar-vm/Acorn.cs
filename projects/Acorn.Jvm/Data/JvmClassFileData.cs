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
    Dynamic = 17,
    InvokeDynamic = 18,
    Module = 19,
    Package = 20
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
    Ificmpeq = 159,
    Ificmpne = 160,
    Ificmplt = 161,
    Ificmpge = 162,
    Ificmpgt = 163,
    Ificmple = 164,
    Ifacmpeq = 165,
    Ifacmpne = 166,
    Goto = 167,
    Jsr = 168,
    Ret = 169,
    Tableswitch = 170,
    Lookupswitch = 171,
    Ireturn = 172,
    Lreturn = 173,
    Freturn = 174,
    Dreturn = 175,
    Areturn = 176,
    Return = 177,
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
    JsrW = 201,

    /// <summary>
    ///     调试器断点保留指令
    /// </summary>
    Breakpoint = 202,

    /// <summary>
    ///     实现相关指令 1
    /// </summary>
    Impdep1 = 254,

    /// <summary>
    ///     实现相关指令 2
    /// </summary>
    Impdep2 = 255
}

/// <summary>
///     JVM 类/字段/方法访问标志
/// </summary>
[Flags]
public enum JvmAccessFlags : ushort
{
    Public = 0x0001,
    Private = 0x0002,
    Protected = 0x0004,
    Static = 0x0008,
    Final = 0x0010,
    Super = 0x0020,
    Synchronized = 0x0020,
    Volatile = 0x0040,
    Bridge = 0x0040,
    Transient = 0x0080,
    Varargs = 0x0080,
    Native = 0x0100,
    Interface = 0x0200,
    Abstract = 0x0400,
    Strict = 0x0800,
    Synthetic = 0x1000,
    Annotation = 0x2000,
    Enum = 0x4000,
    Module = 0x8000
}

/// <summary>
///     CONSTANT_Dynamic 常量（JVM 11+）
/// </summary>
public sealed class JvmConstantDynamic : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Dynamic;
    public ushort BootstrapMethodAttrIndex { get; init; }
    public ushort NameAndTypeIndex { get; init; }
}

/// <summary>
///     CONSTANT_Module 常量（JVM 9+）
/// </summary>
public sealed class JvmConstantModule : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Module;
    public ushort NameIndex { get; init; }
}

/// <summary>
///     CONSTANT_Package 常量（JVM 9+）
/// </summary>
public sealed class JvmConstantPackage : JvmConstant
{
    public override JvmConstantKind Kind => JvmConstantKind.Package;
    public ushort NameIndex { get; init; }
}

/// <summary>
///     方法句柄引用类型
/// </summary>
public enum JvmMethodHandleKind : byte
{
    GetField = 1,
    GetStatic = 2,
    PutField = 3,
    PutStatic = 4,
    InvokeVirtual = 5,
    InvokeStatic = 6,
    InvokeSpecial = 7,
    NewInvokeSpecial = 8,
    InvokeInterface = 9
}

/// <summary>
///     StackMapTable 属性（字节码验证必需）
/// </summary>
public sealed class JvmStackMapTableAttribute : JvmAttributeInfo
{
    public ushort NumberOfEntries { get; init; }
    public IReadOnlyList<JvmStackMapFrame> Entries { get; init; }
}

/// <summary>
///     StackMapFrame 基类
/// </summary>
public abstract class JvmStackMapFrame
{
    /// <summary>
    ///     帧类型（0-255）
    /// </summary>
    public byte FrameType { get; init; }
}

/// <summary>
///     same_frame（frame_type 0-63）
/// </summary>
public sealed class JvmSameFrame : JvmStackMapFrame { }

/// <summary>
///     same_locals_1_stack_item_frame（frame_type 64-127）
/// </summary>
public sealed class JvmSameLocals1StackItemFrame : JvmStackMapFrame
{
    public IReadOnlyList<JvmVerificationTypeInfo> Stack { get; init; }
}

/// <summary>
///     chop_frame（frame_type 248-250）
/// </summary>
public sealed class JvmChopFrame : JvmStackMapFrame
{
    public ushort OffsetDelta { get; init; }
}

/// <summary>
///     same_frame_extended（frame_type 251）
/// </summary>
public sealed class JvmSameFrameExtended : JvmStackMapFrame
{
    public ushort OffsetDelta { get; init; }
}

/// <summary>
///     append_frame（frame_type 252-254）
/// </summary>
public sealed class JvmAppendFrame : JvmStackMapFrame
{
    public ushort OffsetDelta { get; init; }
    public IReadOnlyList<JvmVerificationTypeInfo> Locals { get; init; }
}

/// <summary>
///     full_frame（frame_type 255）
/// </summary>
public sealed class JvmFullFrame : JvmStackMapFrame
{
    public ushort OffsetDelta { get; init; }
    public ushort NumberOfLocals { get; init; }
    public IReadOnlyList<JvmVerificationTypeInfo> Locals { get; init; }
    public ushort NumberOfStackItems { get; init; }
    public IReadOnlyList<JvmVerificationTypeInfo> Stack { get; init; }
}

/// <summary>
///     校验类型信息
/// </summary>
public sealed class JvmVerificationTypeInfo
{
    public byte Tag { get; init; }
    public ushort? CpoolIndex { get; init; }
    public ushort? Offset { get; init; }
}

/// <summary>
///     异常属性
/// </summary>
public sealed class JvmExceptionsAttribute : JvmAttributeInfo
{
    public ushort NumberOfExceptions { get; init; }
    public IReadOnlyList<ushort> ExceptionIndexTable { get; init; }
}

/// <summary>
///     签名属性（泛型签名）
/// </summary>
public sealed class JvmSignatureAttribute : JvmAttributeInfo
{
    public ushort SignatureIndex { get; init; }
}

/// <summary>
///     合成属性（编译器生成标记）
/// </summary>
public sealed class JvmSyntheticAttribute : JvmAttributeInfo { }

/// <summary>
///     废弃属性
/// </summary>
public sealed class JvmDeprecatedAttribute : JvmAttributeInfo { }

/// <summary>
///     外围方法属性
/// </summary>
public sealed class JvmEnclosingMethodAttribute : JvmAttributeInfo
{
    public ushort ClassIndex { get; init; }
    public ushort MethodIndex { get; init; }
}

/// <summary>
///     源文件调试扩展属性
/// </summary>
public sealed class JvmSourceDebugExtensionAttribute : JvmAttributeInfo
{
    public byte[] DebugExtension { get; init; }
}

/// <summary>
///     方法参数属性
/// </summary>
public sealed class JvmMethodParametersAttribute : JvmAttributeInfo
{
    public byte ParameterCount { get; init; }
    public IReadOnlyList<JvmMethodParameterInfo> Parameters { get; init; }
}

/// <summary>
///     方法参数信息
/// </summary>
public sealed class JvmMethodParameterInfo
{
    public ushort NameIndex { get; init; }
    public ushort AccessFlags { get; init; }
}

/// <summary>
///     模块属性（JVM 9+）
/// </summary>
public sealed class JvmModuleAttribute : JvmAttributeInfo
{
    public ushort ModuleNameIndex { get; init; }
    public ushort ModuleFlags { get; init; }
    public ushort ModuleVersionIndex { get; init; }
    public IReadOnlyList<JvmModuleRequire> Requires { get; init; }
    public IReadOnlyList<JvmModuleExport> Exports { get; init; }
    public IReadOnlyList<JvmModuleOpen> Opens { get; init; }
    public IReadOnlyList<ushort> UsesIndex { get; init; }
    public IReadOnlyList<JvmModuleProvide> Provides { get; init; }
}

/// <summary>
///     模块 requires 项
/// </summary>
public sealed class JvmModuleRequire
{
    public ushort RequiresIndex { get; init; }
    public ushort RequiresFlags { get; init; }
    public ushort? RequiresVersionIndex { get; init; }
}

/// <summary>
///     模块 exports 项
/// </summary>
public sealed class JvmModuleExport
{
    public ushort ExportsIndex { get; init; }
    public ushort ExportsFlags { get; init; }
    public IReadOnlyList<ushort> ExportsToIndex { get; init; }
}

/// <summary>
///     模块 opens 项
/// </summary>
public sealed class JvmModuleOpen
{
    public ushort OpensIndex { get; init; }
    public ushort OpensFlags { get; init; }
    public IReadOnlyList<ushort> OpensToIndex { get; init; }
}

/// <summary>
///     模块 provides 项
/// </summary>
public sealed class JvmModuleProvide
{
    public ushort ProvidesIndex { get; init; }
    public IReadOnlyList<ushort> ProvidesWithIndex { get; init; }
}

/// <summary>
///     巢主属性（JVM 11+）
/// </summary>
public sealed class JvmNestHostAttribute : JvmAttributeInfo
{
    public ushort HostClassIndex { get; init; }
}

/// <summary>
///     巢成员属性（JVM 11+）
/// </summary>
public sealed class JvmNestMembersAttribute : JvmAttributeInfo
{
    public ushort NumberOfClasses { get; init; }
    public IReadOnlyList<ushort> ClassIndexes { get; init; }
}

/// <summary>
///     记录属性（JVM 16+）
/// </summary>
public sealed class JvmRecordAttribute : JvmAttributeInfo
{
    public ushort ComponentsCount { get; init; }
    public IReadOnlyList<JvmRecordComponentInfo> Components { get; init; }
}

/// <summary>
///     记录组件信息
/// </summary>
public sealed class JvmRecordComponentInfo
{
    public ushort NameIndex { get; init; }
    public ushort DescriptorIndex { get; init; }
    public IReadOnlyList<JvmAttributeInfo> Attributes { get; init; }
}

/// <summary>
///     允许的子类属性（JVM 17+）
/// </summary>
public sealed class JvmPermittedSubclassesAttribute : JvmAttributeInfo
{
    public ushort NumberOfClasses { get; init; }
    public IReadOnlyList<ushort> ClassIndexes { get; init; }
}

/// <summary>
///     运行时可见注解属性
/// </summary>
public sealed class JvmRuntimeVisibleAnnotationsAttribute : JvmAttributeInfo
{
    public ushort NumAnnotations { get; init; }
    public IReadOnlyList<JvmAnnotation> Annotations { get; init; }
}

/// <summary>
///     运行时不可见注解属性
/// </summary>
public sealed class JvmRuntimeInvisibleAnnotationsAttribute : JvmAttributeInfo
{
    public ushort NumAnnotations { get; init; }
    public IReadOnlyList<JvmAnnotation> Annotations { get; init; }
}

/// <summary>
///     运行时可见参数注解属性
/// </summary>
public sealed class JvmRuntimeVisibleParameterAnnotationsAttribute : JvmAttributeInfo
{
    public byte NumParameters { get; init; }
    public IReadOnlyList<JvmParameterAnnotations> ParameterAnnotations { get; init; }
}

/// <summary>
///     运行时不可见参数注解属性
/// </summary>
public sealed class JvmRuntimeInvisibleParameterAnnotationsAttribute : JvmAttributeInfo
{
    public byte NumParameters { get; init; }
    public IReadOnlyList<JvmParameterAnnotations> ParameterAnnotations { get; init; }
}

/// <summary>
///     参数注解列表
/// </summary>
public sealed class JvmParameterAnnotations
{
    public ushort NumAnnotations { get; init; }
    public IReadOnlyList<JvmAnnotation> Annotations { get; init; }
}

/// <summary>
///     Java 注解
/// </summary>
public sealed class JvmAnnotation
{
    public ushort TypeIndex { get; init; }
    public ushort NumElementValuePairs { get; init; }
    public IReadOnlyList<JvmElementValuePair> ElementValuePairs { get; init; }
}

/// <summary>
///     注解元素-值对
/// </summary>
public sealed class JvmElementValuePair
{
    public ushort ElementNameIndex { get; init; }
    public JvmElementValue Value { get; init; }
}

/// <summary>
///     注解元素值
/// </summary>
public sealed class JvmElementValue
{
    public byte Tag { get; init; }
    public ushort? ConstValueIndex { get; init; }
    public ushort? TypeNameIndex { get; init; }
    public ushort? ClassInfoIndex { get; init; }
    public JvmAnnotation? AnnotationValue { get; init; }
    public ushort? ArrayNumValues { get; init; }
    public IReadOnlyList<JvmElementValue>? ArrayValues { get; init; }
    public ushort? EnumConstNameIndex { get; init; }
}

/// <summary>
///     注解默认值属性
/// </summary>
public sealed class JvmAnnotationDefaultAttribute : JvmAttributeInfo
{
    public JvmElementValue DefaultValue { get; init; }
}

/// <summary>
///     局部变量类型表属性（泛型方法调试信息）
/// </summary>
public sealed class JvmLocalVariableTypeTableAttribute : JvmAttributeInfo
{
    public ushort LocalVariableTypeTableLength { get; init; }
    public IReadOnlyList<JvmLocalVariableTypeEntry> LocalVariableTypeTable { get; init; }
}

/// <summary>
///     局部变量类型表项
/// </summary>
public sealed class JvmLocalVariableTypeEntry
{
    public ushort StartPc { get; init; }
    public ushort Length { get; init; }
    public ushort NameIndex { get; init; }
    public ushort SignatureIndex { get; init; }
    public ushort Index { get; init; }
}

/// <summary>
///     原始二进制属性（用于保留未知属性格式的原始数据）
/// </summary>
public sealed class JvmRawAttribute : JvmAttributeInfo
{
    /// <summary>
    ///     属性名称（从常量池解析）
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     属性的原始字节数据
    /// </summary>
    public byte[] RawData { get; init; }
}

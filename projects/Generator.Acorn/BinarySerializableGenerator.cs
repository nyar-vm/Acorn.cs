using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.Acorn;

/// <summary>
///     为标记 [BinarySerializable] 的 partial struct 生成 TryRead/WriteTo 方法的增量源生成器。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class BinarySerializableGenerator : IIncrementalGenerator
{
    private const string BinarySerializableAttributeName = "BinarySerializable";
    private const string BinarySerializableAttributeFullName = "Acorn.Attributes.BinarySerializableAttribute";
    private const string FieldAttributeFullName = "Acorn.Attributes.FieldAttribute";
    private const string BitFieldAttributeFullName = "Acorn.Attributes.BitFieldAttribute";
    private const string OffsetTableAttributeFullName = "Acorn.Attributes.OffsetTableAttribute";
    private const string AlgebraicUnionAttributeFullName = "Acorn.Attributes.AlgebraicUnionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var structDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BinarySerializableAttributeFullName,
                static (node, _) => node is StructDeclarationSyntax,
                static (context, _) => GetStructInfo(context))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(
            structDeclarations,
            static (productionContext, structInfo) =>
            {
                if (structInfo is null) return;
                var source = GenerateStructCode(structInfo);
                productionContext.AddSource(
                    $"{structInfo.Namespace}.{structInfo.Name}.g.cs",
                    source);
            });
    }

    #region 结构体信息提取

    private static StructSerializationInfo? GetStructInfo(GeneratorAttributeSyntaxContext context)
    {
        var structSyntax = (StructDeclarationSyntax)context.TargetNode;
        var structSymbol = (INamedTypeSymbol)context.TargetSymbol;

        if (!structSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return null;
        }

        var attribute = structSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == BinarySerializableAttributeFullName);

        if (attribute is null)
        {
            return null;
        }

        var endianness = Endianness.LittleEndian;
        if (attribute.NamedArguments.Length > 0)
        {
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Key == "Endianness" && arg.Value.Value is int e)
                {
                    endianness = (Endianness)e;
                }
            }
        }

        var fields = new List<FieldSerializationInfo>();
        var offsetTables = new List<OffsetTableInfo>();

        foreach (var member in structSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol || fieldSymbol.IsStatic || fieldSymbol.IsImplicitlyDeclared)
            {
                continue;
            }

            var offsetTableAttr = fieldSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OffsetTableAttributeFullName);

            if (offsetTableAttr is not null)
            {
                var offsetTableInfo = ExtractOffsetTableInfo(fieldSymbol, offsetTableAttr);
                if (offsetTableInfo is not null)
                {
                    offsetTables.Add(offsetTableInfo);
                }
                continue;
            }

            var fieldAttr = fieldSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == FieldAttributeFullName);

            if (fieldAttr is null)
            {
                continue;
            }

            var fieldInfo = ExtractFieldInfo(fieldSymbol, fieldAttr, structSymbol);
            if (fieldInfo is not null)
            {
                fields.Add(fieldInfo);
            }
        }

        fields = fields.OrderBy(f => f.Order).ToList();

        var algebraicUnion = ExtractAlgebraicUnionInfo(structSymbol);

        return new StructSerializationInfo
        {
            Name = structSymbol.Name,
            Namespace = structSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : structSymbol.ContainingNamespace.ToDisplayString(),
            Endianness = endianness,
            Fields = fields,
            HasBitFields = fields.Any(f => f.IsBitField),
            OffsetTables = offsetTables,
            AlgebraicUnion = algebraicUnion
        };
    }

    private static FieldSerializationInfo? ExtractFieldInfo(IFieldSymbol fieldSymbol, AttributeData fieldAttr, INamedTypeSymbol structSymbol)
    {
        var order = 0;
        var length = -1;
        var lengthField = "";
        var conditionalOn = "";
        var fieldEndianness = (Endianness?)null;
        var encoding = "utf-8";
        var optional = false;
        string? codecTypeName = null;

        foreach (var arg in fieldAttr.NamedArguments)
        {
            switch (arg.Key)
            {
                case "Order" when arg.Value.Value is int o:
                    order = o;
                    break;
                case "Length" when arg.Value.Value is int l:
                    length = l;
                    break;
                case "LengthField" when arg.Value.Value is string lf:
                    lengthField = lf;
                    break;
                case "ConditionalOn" when arg.Value.Value is string co:
                    conditionalOn = co;
                    break;
                case "Endianness" when arg.Value.Value is int e:
                    fieldEndianness = (Endianness)e;
                    break;
                case "Encoding" when arg.Value.Value is string enc:
                    encoding = enc;
                    break;
                case "Optional" when arg.Value.Value is bool opt:
                    optional = opt;
                    break;
                case "Codec" when arg.Value.Value is ITypeSymbol codecType:
                    codecTypeName = codecType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    break;
            }
        }

        var bitFieldAttr = fieldSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == BitFieldAttributeFullName);

        BitFieldInfo? bitFieldInfo = null;
        if (bitFieldAttr is not null)
        {
            var bitOffset = 0;
            var bitCount = 0;

            foreach (var arg in bitFieldAttr.NamedArguments)
            {
                switch (arg.Key)
                {
                    case "BitOffset" when arg.Value.Value is int bo:
                        bitOffset = bo;
                        break;
                    case "BitCount" when arg.Value.Value is int bc:
                        bitCount = bc;
                        break;
                }
            }

            bitFieldInfo = new BitFieldInfo(bitOffset, bitCount);
        }

        var fieldType = fieldSymbol.Type;
        var typeKind = GetTypeKind(fieldType);

        return new FieldSerializationInfo
        {
            Name = fieldSymbol.Name,
            TypeName = fieldType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            DisplayTypeName = fieldType.ToDisplayString(),
            Order = order,
            Length = length,
            LengthField = lengthField,
            ConditionalOn = conditionalOn,
            Endianness = fieldEndianness,
            Encoding = encoding,
            IsBitField = bitFieldInfo is not null,
            BitField = bitFieldInfo,
            TypeKind = typeKind,
            IsArray = fieldType.TypeKind == TypeKind.Array,
            ElementTypeName = fieldType.TypeKind == TypeKind.Array
                ? ((IArrayTypeSymbol)fieldType).ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : null,
            Optional = optional,
            CodecTypeName = codecTypeName
        };
    }

    private static OffsetTableInfo? ExtractOffsetTableInfo(IFieldSymbol fieldSymbol, AttributeData attr)
    {
        var offsetField = "";
        var targetTypeName = "";
        var relativeTo = "start";

        foreach (var arg in attr.NamedArguments)
        {
            switch (arg.Key)
            {
                case "OffsetField" when arg.Value.Value is string of:
                    offsetField = of;
                    break;
                case "TargetType" when arg.Value.Value is ITypeSymbol tt:
                    targetTypeName = tt.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    break;
                case "RelativeTo" when arg.Value.Value is string rt:
                    relativeTo = rt;
                    break;
            }
        }

        return new OffsetTableInfo
        {
            FieldName = fieldSymbol.Name,
            OffsetField = offsetField,
            TargetTypeName = targetTypeName,
            RelativeTo = relativeTo
        };
    }

    private static AlgebraicUnionInfo? ExtractAlgebraicUnionInfo(INamedTypeSymbol structSymbol)
    {
        var unionAttr = structSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AlgebraicUnionAttributeFullName);

        if (unionAttr is null)
        {
            return null;
        }

        var discriminatorField = "";
        var cases = new List<AlgebraicUnionCase>();

        foreach (var arg in unionAttr.NamedArguments)
        {
            if (arg.Key == "DiscriminatorField" && arg.Value.Value is string df)
            {
                discriminatorField = df;
            }
            else if (arg.Key == "Cases" && arg.Value.Values.Length > 0)
            {
                foreach (var caseValue in arg.Value.Values)
                {
                    if (caseValue.Value is ITypeSymbol caseType)
                    {
                        var caseTypeName = caseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var caseFieldName = FindFieldForCaseType(structSymbol, caseType);
                        if (caseFieldName is not null)
                        {
                            cases.Add(new AlgebraicUnionCase
                            {
                                TypeName = caseTypeName,
                                FieldName = caseFieldName
                            });
                        }
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(discriminatorField) || cases.Count == 0)
        {
            return null;
        }

        return new AlgebraicUnionInfo
        {
            DiscriminatorField = discriminatorField,
            Cases = cases
        };
    }

    private static string? FindFieldForCaseType(INamedTypeSymbol structSymbol, ITypeSymbol caseType)
    {
        var caseTypeName = caseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        foreach (var member in structSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol || fieldSymbol.IsStatic || fieldSymbol.IsImplicitlyDeclared)
            {
                continue;
            }

            var fieldTypeName = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (fieldTypeName == caseTypeName)
            {
                return fieldSymbol.Name;
            }
        }

        return null;
    }

    private static FieldTypeKind GetTypeKind(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();

        return typeName switch
        {
            "byte" or "System.Byte" => FieldTypeKind.Byte,
            "sbyte" or "System.SByte" => FieldTypeKind.SByte,
            "ushort" or "System.UInt16" => FieldTypeKind.UInt16,
            "short" or "System.Int16" => FieldTypeKind.Int16,
            "uint" or "System.UInt32" => FieldTypeKind.UInt32,
            "int" or "System.Int32" => FieldTypeKind.Int32,
            "ulong" or "System.UInt64" => FieldTypeKind.UInt64,
            "long" or "System.Int64" => FieldTypeKind.Int64,
            "float" or "System.Single" => FieldTypeKind.Float,
            "double" or "System.Double" => FieldTypeKind.Double,
            "bool" or "System.Boolean" => FieldTypeKind.Boolean,
            "string" or "System.String" => FieldTypeKind.String,
            _ when type.Name.StartsWith("FixedBytes") => FieldTypeKind.FixedBytes,
            _ => FieldTypeKind.Custom
        };
    }

    #endregion

    #region 代码生成

    private static string GenerateStructCode(StructSerializationInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// 由 Acorn.CodeGenerator 自动生成，请勿手动修改");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using Acorn;");
        sb.AppendLine("using Acorn.Frame;");
        sb.AppendLine("using Acorn.Codec;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        sb.AppendLine($"public partial struct {info.Name}");
        sb.AppendLine("{");

        #region TryRead

        GenerateTryReadMethod(sb, info);

        #endregion

        sb.AppendLine();

        #region WriteTo

        GenerateWriteToMethod(sb, info);

        #endregion

        sb.AppendLine();

        #region GetSize

        GenerateGetSizeMethod(sb, info);

        #endregion

        if (info.HasBitFields)
        {
            sb.AppendLine();

            #region BitField 属性

            GenerateBitFieldProperties(sb, info);

            #endregion
        }

        if (info.AlgebraicUnion is not null)
        {
            sb.AppendLine();

            #region 代数联合 Match/Switch

            GenerateAlgebraicUnionMethods(sb, info);

            #endregion
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    #region TryRead 生成

    private static void GenerateTryReadMethod(StringBuilder sb, StructSerializationInfo info)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    ///     尝试从字节缓冲区读取当前结构体。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"buffer\">源字节缓冲区。</param>");
        sb.AppendLine("    /// <param name=\"value\">读取成功后的结构体值。</param>");
        sb.AppendLine("    /// <returns>如果读取成功则返回 true，否则返回 false。</returns>");
        sb.AppendLine($"    public static bool TryRead(ref ByteBuffer buffer, out {info.Name} value)");
        sb.AppendLine("    {");
        sb.AppendLine($"        value = new {info.Name}();");
        sb.AppendLine();

        foreach (var field in info.Fields)
        {
            if (field.IsBitField && field.BitField is not null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(field.ConditionalOn))
            {
                sb.AppendLine($"        if (value.{field.ConditionalOn})");
                sb.AppendLine("        {");
                GenerateFieldRead(sb, field, info, 12);
                sb.AppendLine("        }");
            }
            else
            {
                GenerateFieldRead(sb, field, info, 8);
            }
        }

        if (info.OffsetTables.Count > 0)
        {
            sb.AppendLine();

            foreach (var ot in info.OffsetTables)
            {
                GenerateOffsetTableRead(sb, ot, info, 8);
            }
        }

        sb.AppendLine();
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
    }

    private static void GenerateFieldRead(StringBuilder sb, FieldSerializationInfo field, StructSerializationInfo info, int indent)
    {
        var indentStr = new string(' ', indent);
        var endianness = field.Endianness ?? info.Endianness;
        var endianSuffix = endianness == Endianness.BigEndian ? "BE" : "LE";

        if (field.CodecTypeName is not null)
        {
            GenerateCodecFieldRead(sb, field, indent);
            return;
        }

        if (field.Optional)
        {
            GenerateOptionalFieldRead(sb, field, info, indent);
            return;
        }

        if (field.IsArray && !string.IsNullOrEmpty(field.LengthField))
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = new {field.ElementTypeName}[value.{field.LengthField}];");
            sb.AppendLine($"{indentStr}for (var i = 0; i < value.{field.LengthField}; i++)");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateSingleValueRead(sb, field, endianSuffix, indent + 4, $"value.{field.Name}[i]");
            sb.AppendLine($"{indentStr}" + "}");
        }
        else if (field.IsArray && field.Length > 0)
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = new {field.ElementTypeName}[{field.Length}];");
            sb.AppendLine($"{indentStr}for (var i = 0; i < {field.Length}; i++)");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateSingleValueRead(sb, field, endianSuffix, indent + 4, $"value.{field.Name}[i]");
            sb.AppendLine($"{indentStr}" + "}");
        }
        else if (field.TypeKind == FieldTypeKind.String && field.Length > 0)
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = buffer.ReadString({field.Length});");
        }
        else if (field.TypeKind == FieldTypeKind.String && !string.IsNullOrEmpty(field.LengthField))
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = buffer.ReadString((int)value.{field.LengthField});");
        }
        else if (field.TypeKind == FieldTypeKind.FixedBytes)
        {
            var fixedLength = GetFixedBytesLength(field.DisplayTypeName);
            sb.AppendLine($"{indentStr}value.{field.Name} = {field.DisplayTypeName}.FromSpan(buffer.ReadBytes({fixedLength}));");
        }
        else
        {
            GenerateSingleValueRead(sb, field, endianSuffix, indent, $"value.{field.Name}");
        }
    }

    private static void GenerateOptionalFieldRead(StringBuilder sb, FieldSerializationInfo field, StructSerializationInfo info, int indent)
    {
        var indentStr = new string(' ', indent);
        var endianness = field.Endianness ?? info.Endianness;
        var endianSuffix = endianness == Endianness.BigEndian ? "BE" : "LE";

        var minBytes = GetFixedFieldSize(field);

        if (minBytes > 0)
        {
            sb.AppendLine($"{indentStr}if (buffer.Remaining >= {minBytes})");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateFieldReadCore(sb, field, endianSuffix, indent + 4);
            sb.AppendLine($"{indentStr}" + "}");
            sb.AppendLine($"{indentStr}else");
            sb.AppendLine($"{indentStr}" + "{");
            sb.AppendLine($"{indentStr}    value.{field.Name} = default;");
            sb.AppendLine($"{indentStr}" + "}");
        }
        else
        {
            sb.AppendLine($"{indentStr}if (!buffer.IsEnd)");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateFieldReadCore(sb, field, endianSuffix, indent + 4);
            sb.AppendLine($"{indentStr}" + "}");
            sb.AppendLine($"{indentStr}else");
            sb.AppendLine($"{indentStr}" + "{");
            sb.AppendLine($"{indentStr}    value.{field.Name} = default;");
            sb.AppendLine($"{indentStr}" + "}");
        }
    }

    private static void GenerateFieldReadCore(StringBuilder sb, FieldSerializationInfo field, string endianSuffix, int indent)
    {
        var indentStr = new string(' ', indent);

        if (field.IsArray && !string.IsNullOrEmpty(field.LengthField))
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = new {field.ElementTypeName}[value.{field.LengthField}];");
            sb.AppendLine($"{indentStr}for (var i = 0; i < value.{field.LengthField}; i++)");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateSingleValueRead(sb, field, endianSuffix, indent + 4, $"value.{field.Name}[i]");
            sb.AppendLine($"{indentStr}" + "}");
        }
        else if (field.IsArray && field.Length > 0)
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = new {field.ElementTypeName}[{field.Length}];");
            sb.AppendLine($"{indentStr}for (var i = 0; i < {field.Length}; i++)");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateSingleValueRead(sb, field, endianSuffix, indent + 4, $"value.{field.Name}[i]");
            sb.AppendLine($"{indentStr}" + "}");
        }
        else if (field.TypeKind == FieldTypeKind.String && field.Length > 0)
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = buffer.ReadString({field.Length});");
        }
        else if (field.TypeKind == FieldTypeKind.String && !string.IsNullOrEmpty(field.LengthField))
        {
            sb.AppendLine($"{indentStr}value.{field.Name} = buffer.ReadString((int)value.{field.LengthField});");
        }
        else if (field.TypeKind == FieldTypeKind.FixedBytes)
        {
            var fixedLength = GetFixedBytesLength(field.DisplayTypeName);
            sb.AppendLine($"{indentStr}value.{field.Name} = {field.DisplayTypeName}.FromSpan(buffer.ReadBytes({fixedLength}));");
        }
        else
        {
            GenerateSingleValueRead(sb, field, endianSuffix, indent, $"value.{field.Name}");
        }
    }

    private static void GenerateCodecFieldRead(StringBuilder sb, FieldSerializationInfo field, int indent)
    {
        var indentStr = new string(' ', indent);

        sb.AppendLine($"{indentStr}value.{field.Name} = buffer.Read<{field.TypeName}, {field.CodecTypeName}>(new {field.CodecTypeName}());");
    }

    private static void GenerateSingleValueRead(StringBuilder sb, FieldSerializationInfo field, string endianSuffix, int indent, string target)
    {
        var indentStr = new string(' ', indent);

        var readMethod = field.TypeKind switch
        {
            FieldTypeKind.Byte => "ReadU8()",
            FieldTypeKind.SByte => "ReadI8()",
            FieldTypeKind.UInt16 => $"ReadU16{endianSuffix}()",
            FieldTypeKind.Int16 => $"ReadI16{endianSuffix}()",
            FieldTypeKind.UInt32 => $"ReadU32{endianSuffix}()",
            FieldTypeKind.Int32 => $"ReadI32{endianSuffix}()",
            FieldTypeKind.UInt64 => $"ReadU64{endianSuffix}()",
            FieldTypeKind.Int64 => $"ReadI64{endianSuffix}()",
            FieldTypeKind.Float => $"ReadF32{endianSuffix}()",
            FieldTypeKind.Double => $"ReadF64{endianSuffix}()",
            FieldTypeKind.Boolean => "ReadU8() != 0",
            _ => null
        };

        if (readMethod is not null)
        {
            sb.AppendLine($"{indentStr}{target} = buffer.{readMethod};");
        }
        else if (field.TypeKind == FieldTypeKind.Custom)
        {
            sb.AppendLine($"{indentStr}if (!{field.DisplayTypeName}.TryRead(ref buffer, out var {field.Name}Value))");
            sb.AppendLine($"{indentStr}" + "{");
            sb.AppendLine($"{indentStr}    return false;");
            sb.AppendLine($"{indentStr}" + "}");
            sb.AppendLine($"{indentStr}{target} = {field.Name}Value;");
        }
    }

    private static void GenerateOffsetTableRead(StringBuilder sb, OffsetTableInfo ot, StructSerializationInfo info, int indent)
    {
        var indentStr = new string(' ', indent);

        sb.AppendLine($"{indentStr}var {ot.FieldName}SavedPos = buffer.Position;");
        sb.AppendLine($"{indentStr}buffer.Position = (int)value.{ot.OffsetField};");

        if (!string.IsNullOrEmpty(ot.TargetTypeName))
        {
            sb.AppendLine($"{indentStr}if (!{ot.TargetTypeName}.TryRead(ref buffer, out value.{ot.FieldName}))");
            sb.AppendLine($"{indentStr}" + "{");
            sb.AppendLine($"{indentStr}    return false;");
            sb.AppendLine($"{indentStr}" + "}");
        }

        sb.AppendLine($"{indentStr}buffer.Position = {ot.FieldName}SavedPos;");
    }

    #endregion

    #region WriteTo 生成

    private static void GenerateWriteToMethod(StringBuilder sb, StructSerializationInfo info)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    ///     将当前结构体写入字节缓冲区。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"writer\">目标字节缓冲区写入器。</param>");
        sb.AppendLine("    public void WriteTo(ref ByteBufferWriter writer)");
        sb.AppendLine("    {");

        foreach (var field in info.Fields)
        {
            if (field.IsBitField && field.BitField is not null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(field.ConditionalOn))
            {
                sb.AppendLine($"        if ({field.ConditionalOn})");
                sb.AppendLine("        {");
                GenerateFieldWrite(sb, field, info, 12);
                sb.AppendLine("        }");
            }
            else
            {
                GenerateFieldWrite(sb, field, info, 8);
            }
        }

        if (info.OffsetTables.Count > 0)
        {
            sb.AppendLine();

            foreach (var ot in info.OffsetTables)
            {
                GenerateOffsetTableWrite(sb, ot, info, 8);
            }
        }

        sb.AppendLine("    }");
    }

    private static void GenerateFieldWrite(StringBuilder sb, FieldSerializationInfo field, StructSerializationInfo info, int indent)
    {
        var indentStr = new string(' ', indent);
        var endianness = field.Endianness ?? info.Endianness;
        var endianSuffix = endianness == Endianness.BigEndian ? "BE" : "LE";

        if (field.CodecTypeName is not null)
        {
            GenerateCodecFieldWrite(sb, field, indent);
            return;
        }

        if (field.IsArray && !string.IsNullOrEmpty(field.LengthField))
        {
            sb.AppendLine($"{indentStr}for (var i = 0; i < {field.LengthField}; i++)");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateSingleValueWrite(sb, field, endianSuffix, indent + 4, $"{field.Name}[i]");
            sb.AppendLine($"{indentStr}" + "}");
        }
        else if (field.IsArray && field.Length > 0)
        {
            sb.AppendLine($"{indentStr}for (var i = 0; i < {field.Length}; i++)");
            sb.AppendLine($"{indentStr}" + "{");
            GenerateSingleValueWrite(sb, field, endianSuffix, indent + 4, $"{field.Name}[i]");
            sb.AppendLine($"{indentStr}" + "}");
        }
        else if (field.TypeKind == FieldTypeKind.String && field.Length > 0)
        {
            sb.AppendLine($"{indentStr}var {field.Name}Bytes = System.Text.Encoding.UTF8.GetBytes({field.Name} ?? string.Empty);");
            sb.AppendLine($"{indentStr}var {field.Name}ToWrite = {field.Name}Bytes.Length > {field.Length} ? {field.Name}Bytes.AsSpan(0, {field.Length}) : {field.Name}Bytes;");
            sb.AppendLine($"{indentStr}writer.Write({field.Name}ToWrite);");
            sb.AppendLine($"{indentStr}var {field.Name}Padding = {field.Length} - {field.Name}ToWrite.Length;");
            sb.AppendLine($"{indentStr}for (var i = 0; i < {field.Name}Padding; i++) writer.WriteU8(0);");
        }
        else if (field.TypeKind == FieldTypeKind.String && !string.IsNullOrEmpty(field.LengthField))
        {
            sb.AppendLine($"{indentStr}var {field.Name}Bytes = System.Text.Encoding.UTF8.GetBytes({field.Name} ?? string.Empty);");
            sb.AppendLine($"{indentStr}writer.Write({field.Name}Bytes);");
        }
        else if (field.TypeKind == FieldTypeKind.FixedBytes)
        {
            sb.AppendLine($"{indentStr}writer.Write({field.Name}.AsSpan());");
        }
        else
        {
            GenerateSingleValueWrite(sb, field, endianSuffix, indent, field.Name);
        }
    }

    private static void GenerateCodecFieldWrite(StringBuilder sb, FieldSerializationInfo field, int indent)
    {
        var indentStr = new string(' ', indent);

        sb.AppendLine($"{indentStr}var {field.Name}Codec = new {field.CodecTypeName}();");
        sb.AppendLine($"{indentStr}var {field.Name}CodecSize = {field.Name}Codec.GetSize({field.Name});");
        sb.AppendLine($"{indentStr}if ({field.Name}CodecSize > 0)");
        sb.AppendLine($"{indentStr}" + "{");
        sb.AppendLine($"{indentStr}    {field.Name}Codec.Encode({field.Name}, writer.GetSpan({field.Name}CodecSize));");
        sb.AppendLine($"{indentStr}    writer.Advance({field.Name}CodecSize);");
        sb.AppendLine($"{indentStr}" + "}");
        sb.AppendLine($"{indentStr}else");
        sb.AppendLine($"{indentStr}" + "{");
        sb.AppendLine($"{indentStr}    var {field.Name}Span = writer.GetSpan(10);");
        sb.AppendLine($"{indentStr}    {field.Name}Codec.Encode({field.Name}, {field.Name}Span);");
        sb.AppendLine($"{indentStr}    var {field.Name}Encoded = 1;");
        sb.AppendLine($"{indentStr}    while ({field.Name}Span[{field.Name}Encoded - 1] >= 0x80) {field.Name}Encoded++;");
        sb.AppendLine($"{indentStr}    writer.Advance({field.Name}Encoded);");
        sb.AppendLine($"{indentStr}" + "}");
    }

    private static void GenerateSingleValueWrite(StringBuilder sb, FieldSerializationInfo field, string endianSuffix, int indent, string source)
    {
        var indentStr = new string(' ', indent);

        var writeMethod = field.TypeKind switch
        {
            FieldTypeKind.Byte => $"WriteU8({source})",
            FieldTypeKind.SByte => $"WriteI8({source})",
            FieldTypeKind.UInt16 => $"WriteU16{endianSuffix}({source})",
            FieldTypeKind.Int16 => $"WriteI16{endianSuffix}({source})",
            FieldTypeKind.UInt32 => $"WriteU32{endianSuffix}({source})",
            FieldTypeKind.Int32 => $"WriteI32{endianSuffix}({source})",
            FieldTypeKind.UInt64 => $"WriteU64{endianSuffix}({source})",
            FieldTypeKind.Int64 => $"WriteI64{endianSuffix}({source})",
            FieldTypeKind.Float => $"WriteF32{endianSuffix}({source})",
            FieldTypeKind.Double => $"WriteF64{endianSuffix}({source})",
            FieldTypeKind.Boolean => $"WriteU8((byte)({source} ? 1 : 0))",
            _ => null
        };

        if (writeMethod is not null)
        {
            sb.AppendLine($"{indentStr}writer.{writeMethod};");
        }
        else if (field.TypeKind == FieldTypeKind.Custom)
        {
            sb.AppendLine($"{indentStr}{source}.WriteTo(ref writer);");
        }
    }

    private static void GenerateOffsetTableWrite(StringBuilder sb, OffsetTableInfo ot, StructSerializationInfo info, int indent)
    {
        var indentStr = new string(' ', indent);

        sb.AppendLine($"{indentStr}{ot.FieldName}.WriteTo(ref writer);");
    }

    #endregion

    #region GetSize 生成

    private static void GenerateGetSizeMethod(StringBuilder sb, StructSerializationInfo info)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    ///     获取当前结构体的序列化大小。固定大小返回正数，包含变长字段返回 -1。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <returns>序列化大小，或 -1 表示变长。</returns>");
        sb.AppendLine("    public int GetSize()");
        sb.AppendLine("    {");

        var hasVariableLength = info.Fields.Any(f =>
            (!string.IsNullOrEmpty(f.LengthField) && f.IsArray) ||
            (f.TypeKind == FieldTypeKind.String && string.IsNullOrEmpty(f.LengthField)));

        if (hasVariableLength)
        {
            sb.AppendLine("        return -1;");
        }
        else
        {
            sb.AppendLine("        var size = 0;");

            foreach (var field in info.Fields)
            {
                if (field.IsBitField)
                {
                    continue;
                }

                var fieldSize = GetFixedFieldSize(field);
                if (fieldSize > 0)
                {
                    if (field.IsArray && field.Length > 0)
                    {
                        sb.AppendLine($"        size += {fieldSize * field.Length};");
                    }
                    else
                    {
                        sb.AppendLine($"        size += {fieldSize};");
                    }
                }
                else if (field.IsArray && !string.IsNullOrEmpty(field.LengthField))
                {
                    sb.AppendLine($"        for (var i = 0; i < {field.LengthField}; i++)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            size += {field.Name}[i].GetSize();");
                    sb.AppendLine("        }");
                }
                else if (field.TypeKind == FieldTypeKind.Custom)
                {
                    sb.AppendLine($"        size += {field.Name}.GetSize();");
                }
                else if (field.TypeKind == FieldTypeKind.FixedBytes)
                {
                    var fixedLength = GetFixedBytesLength(field.DisplayTypeName);
                    sb.AppendLine($"        size += {fixedLength};");
                }
            }

            sb.AppendLine();
            sb.AppendLine("        return size;");
        }

        sb.AppendLine("    }");
    }

    #endregion

    #region BitField 属性生成

    private static void GenerateBitFieldProperties(StringBuilder sb, StructSerializationInfo info)
    {
        var bitFields = info.Fields.Where(f => f.IsBitField && f.BitField is not null).ToList();
        if (bitFields.Count == 0) return;

        var nonBitFields = info.Fields.Where(f => !f.IsBitField).OrderBy(f => f.Order).ToList();

        foreach (var bf in bitFields)
        {
            var hostField = nonBitFields.LastOrDefault(f => f.Order < bf.Order);
            if (hostField is null) continue;

            var bitInfo = bf.BitField!;
            var hostCastType = GetBitFieldCastType(hostField.TypeKind);
            if (hostCastType is null) continue;

            var bitOffset = bitInfo.BitOffset;
            var bitCount = bitInfo.BitCount;
            var mask = (1 << bitCount) - 1;
            var isBool = bitCount == 1;

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    ///     位域属性 {bf.Name}，位于 {hostField.Name} 的第 {bitOffset} 位起 {bitCount} 位。");
            sb.AppendLine("    /// </summary>");

            if (isBool)
            {
                if (bitOffset == 0)
                {
                    sb.AppendLine($"    public bool {bf.Name}");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        get => ({hostField.Name} & {mask}) != 0;");
                    sb.AppendLine($"        set => {hostField.Name} = ({hostCastType})(({hostField.Name} & ~({mask})) | ((value ? 1 : 0) & {mask}));");
                    sb.AppendLine("    }");
                }
                else
                {
                    sb.AppendLine($"    public bool {bf.Name}");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        get => (({hostField.Name} >> {bitOffset}) & {mask}) != 0;");
                    sb.AppendLine($"        set => {hostField.Name} = ({hostCastType})(({hostField.Name} & ~({mask} << {bitOffset})) | (((value ? 1 : 0) & {mask}) << {bitOffset}));");
                    sb.AppendLine("    }");
                }
            }
            else
            {
                if (bitOffset == 0)
                {
                    sb.AppendLine($"    public {hostCastType} {bf.Name}");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        get => ({hostCastType})({hostField.Name} & {mask});");
                    sb.AppendLine($"        set => {hostField.Name} = ({hostCastType})(({hostField.Name} & ~{mask}) | (value & {mask}));");
                    sb.AppendLine("    }");
                }
                else
                {
                    sb.AppendLine($"    public {hostCastType} {bf.Name}");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        get => ({hostCastType})(({hostField.Name} >> {bitOffset}) & {mask});");
                    sb.AppendLine($"        set => {hostField.Name} = ({hostCastType})(({hostField.Name} & ~({mask} << {bitOffset})) | ((value & {mask}) << {bitOffset}));");
                    sb.AppendLine("    }");
                }
            }

            sb.AppendLine();
        }
    }

    private static string? GetBitFieldCastType(FieldTypeKind typeKind)
    {
        return typeKind switch
        {
            FieldTypeKind.Byte => "byte",
            FieldTypeKind.SByte => "sbyte",
            FieldTypeKind.UInt16 => "ushort",
            FieldTypeKind.Int16 => "short",
            FieldTypeKind.UInt32 => "uint",
            FieldTypeKind.Int32 => "int",
            FieldTypeKind.UInt64 => "ulong",
            FieldTypeKind.Int64 => "long",
            _ => null
        };
    }

    #endregion

    #region 代数联合 Match/Switch 生成

    private static void GenerateAlgebraicUnionMethods(StringBuilder sb, StructSerializationInfo info)
    {
        var union = info.AlgebraicUnion!;
        var discriminatorField = union.DiscriminatorField;

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    ///     对代数联合执行模式匹配，返回结果值。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <typeparam name=\"TResult\">匹配返回类型。</typeparam>");

        var typeParams = string.Join(", ", union.Cases.Select((c, i) => $"Func<{c.TypeName}, TResult>"));
        var paramList = string.Join(", ", union.Cases.Select((c, i) => $"case{i + 1}"));

        sb.AppendLine($"    public TResult Match<TResult>({typeParams})");
        sb.AppendLine("    {");
        sb.AppendLine($"        return {discriminatorField} switch");
        sb.AppendLine("        {");

        for (var i = 0; i < union.Cases.Count; i++)
        {
            var c = union.Cases[i];
            var comma = i < union.Cases.Count - 1 ? "," : "";
            sb.AppendLine($"            var _ when {c.FieldName} is not null => case{i + 1}({c.FieldName}){comma}");
        }

        sb.AppendLine("            _ => throw new InvalidOperationException(\"未匹配到任何代数联合分支\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");

        sb.AppendLine();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    ///     对代数联合执行模式匹配，执行副作用操作。");
        sb.AppendLine("    /// </summary>");

        var actionParams = string.Join(", ", union.Cases.Select((c, i) => $"Action<{c.TypeName}>"));
        var actionParamList = string.Join(", ", union.Cases.Select((c, i) => $"case{i + 1}"));

        sb.AppendLine($"    public void Switch({actionParams})");
        sb.AppendLine("    {");
        sb.AppendLine($"        switch ({discriminatorField})");
        sb.AppendLine("        {");

        for (var i = 0; i < union.Cases.Count; i++)
        {
            var c = union.Cases[i];
            sb.AppendLine($"            case var _ when {c.FieldName} is not null:");
            sb.AppendLine($"                case{i + 1}({c.FieldName});");
            sb.AppendLine("                break;");
        }

        sb.AppendLine("            default:");
        sb.AppendLine("                throw new InvalidOperationException(\"未匹配到任何代数联合分支\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    #endregion

    #region 辅助方法

    private static int GetFixedFieldSize(FieldSerializationInfo field)
    {
        return field.TypeKind switch
        {
            FieldTypeKind.Byte or FieldTypeKind.SByte or FieldTypeKind.Boolean => 1,
            FieldTypeKind.UInt16 or FieldTypeKind.Int16 => 2,
            FieldTypeKind.UInt32 or FieldTypeKind.Int32 or FieldTypeKind.Float => 4,
            FieldTypeKind.UInt64 or FieldTypeKind.Int64 or FieldTypeKind.Double => 8,
            _ => 0
        };
    }

    private static int GetFixedBytesLength(string typeName)
    {
        return typeName switch
        {
            "FixedBytes4" => 4,
            "FixedBytes8" => 8,
            "FixedBytes16" => 16,
            "FixedBytes32" => 32,
            "FixedBytes56" => 56,
            "FixedBytes64" => 64,
            _ => 0
        };
    }

    #endregion

    #endregion
}


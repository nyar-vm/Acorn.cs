using Acorn.Clr.Data;
using Acorn.Frame;
using Acorn.Pe.Data;
using Acorn.Pe.Decode;
using System.Text;

namespace Acorn.Clr.Decode;

/// <summary>
///     CLR 模块解码器，解析 .NET 程序集（PE + CLR 元数据 + MSIL）。
/// </summary>
public sealed class ClrDecoder
{
    private readonly PeDecoder _peDecoder = new();

    /// <summary>
    ///     从字节数组解码 CLR 模块。
    /// </summary>
    public ClrModuleData Decode(ReadOnlySpan<byte> data)
    {
        var peFile = _peDecoder.Decode(data);

        var clrDirectory = DecodeClrDirectory(data, peFile);
        var metadata = DecodeMetadata(data, peFile, clrDirectory);

        var moduleRow = ResolveModuleRow(metadata);
        var typeDefRows = ResolveTypeDefRows(metadata);
        var methodDefRows = ResolveMethodDefRows(metadata);
        var fieldDefRows = ResolveFieldDefRows(metadata);
        var propertyDefRows = ResolvePropertyDefRows(metadata);
        var eventDefRows = ResolveEventDefRows(metadata);

        var methods = DecodeMethodBodies(data, peFile, methodDefRows, metadata);

        return new ClrModuleData
        {
            PeFile = peFile,
            ClrDirectory = clrDirectory,
            Metadata = metadata,
            Methods = methods,
            Types = BuildTypeDefs(typeDefRows, methodDefRows, fieldDefRows, propertyDefRows, eventDefRows, methods, metadata),
            Fields = fieldDefRows,
            Properties = propertyDefRows,
            Events = eventDefRows,
            ModuleName = moduleRow?.Name ?? string.Empty,
            Version = metadata.Header.VersionString
        };
    }

    #region CLR 目录解码

    /// <summary>
    ///     解码 CLR 目录表（PE 可选头数据目录索引 14）。
    /// </summary>
    private ClrDirectoryData DecodeClrDirectory(ReadOnlySpan<byte> data, PeFileData peFile)
    {
        var clrDir = peFile.GetDataDirectory(PeDataDirectoryIndex.ClrRuntimeHeader);

        if (clrDir.IsEmpty)
        {
            throw new InvalidDataException("PE 文件不包含 CLR 目录（不是 .NET 程序集）");
        }

        var offset = peFile.RvaToOffset(clrDir.Rva);

        if (offset < 0 || offset + ClrConstants.ClrDirectorySize > data.Length)
        {
            throw new InvalidDataException("CLR 目录数据超出文件范围");
        }

        var buffer = new ByteBuffer(data[offset..]);

        return new ClrDirectoryData
        {
            Cb = buffer.ReadU32LE(),
            MajorRuntimeVersion = buffer.ReadU16LE(),
            MinorRuntimeVersion = buffer.ReadU16LE(),
            MetadataRva = buffer.ReadU32LE(),
            MetadataSize = buffer.ReadU32LE(),
            Flags = buffer.ReadU32LE(),
            EntryPoint = buffer.ReadU32LE(),
            ResourcesRva = buffer.ReadU32LE(),
            ResourcesSize = buffer.ReadU32LE(),
            StrongNameSignatureRva = buffer.ReadU32LE(),
            StrongNameSignatureSize = buffer.ReadU32LE(),
            CodeManagerTableRva = buffer.ReadU32LE(),
            CodeManagerTableSize = buffer.ReadU32LE(),
            VTableFixupsRva = buffer.ReadU32LE(),
            VTableFixupsSize = buffer.ReadU32LE(),
            ExportAddressTableJumpsRva = buffer.ReadU32LE(),
            ExportAddressTableJumpsSize = buffer.ReadU32LE(),
            ManagedNativeHeaderRva = buffer.ReadU32LE(),
            ManagedNativeHeaderSize = buffer.ReadU32LE()
        };
    }

    #endregion

    #region 元数据解码

    /// <summary>
    ///     解码 CLR 元数据（元数据头 + 流头 + 流数据）。
    /// </summary>
    private ClrMetadata DecodeMetadata(ReadOnlySpan<byte> data, PeFileData peFile, ClrDirectoryData clrDirectory)
    {
        var metadataOffset = peFile.RvaToOffset(clrDirectory.MetadataRva);

        if (metadataOffset < 0 || metadataOffset + ClrConstants.MetadataHeaderMinSize > data.Length)
        {
            throw new InvalidDataException("元数据 RVA 无效或超出文件范围");
        }

        var buffer = new ByteBuffer(data[metadataOffset..]);

        var header = ReadMetadataHeader(ref buffer);
        var streamHeaders = ReadStreamHeaders(ref buffer, header.Streams);

        ClrTableStream? tableStream = null;
        var stringHeap = new ClrStringHeap();
        var blobHeap = new ClrBlobHeap();
        var guidHeap = new ClrGuidHeap();
        var userStringHeap = new ClrUserStringHeap();

        foreach (var sh in streamHeaders)
        {
            var streamOffset = (int)sh.Offset;
            var streamEnd = streamOffset + (int)sh.Size;

            if (streamOffset < 0 || streamEnd > data.Length - metadataOffset)
            {
                continue;
            }

            var streamData = data.Slice(metadataOffset + streamOffset, (int)sh.Size);

            switch (sh.Name)
            {
                case ClrConstants.TableStreamName:
                case ClrConstants.UnoptimizedTableStreamName:
                    tableStream = DecodeTableStream(streamData);
                    break;
                case ClrConstants.StringsStreamName:
                    stringHeap = new ClrStringHeap { Data = streamData.ToArray() };
                    break;
                case ClrConstants.BlobStreamName:
                    blobHeap = new ClrBlobHeap { Data = streamData.ToArray() };
                    break;
                case ClrConstants.GuidStreamName:
                    guidHeap = new ClrGuidHeap { Data = streamData.ToArray() };
                    break;
                case ClrConstants.UserStringStreamName:
                    userStringHeap = new ClrUserStringHeap { Data = streamData.ToArray() };
                    break;
            }
        }

        return new ClrMetadata
        {
            Header = header,
            StreamHeaders = streamHeaders,
            TableStream = tableStream,
            StringHeap = stringHeap,
            BlobHeap = blobHeap,
            GuidHeap = guidHeap,
            UserStringHeap = userStringHeap
        };
    }

    /// <summary>
    ///     读取元数据头。
    /// </summary>
    private ClrMetadataHeader ReadMetadataHeader(ref ByteBuffer buffer)
    {
        var signature = buffer.ReadU32LE();

        if (signature != ClrConstants.MetadataSignature)
        {
            throw new InvalidDataException($"元数据签名不匹配：期望 0x{ClrConstants.MetadataSignature:X8}，实际 0x{signature:X8}");
        }

        var majorVersion = buffer.ReadU16LE();
        var minorVersion = buffer.ReadU16LE();
        var reserved = buffer.ReadU32LE();
        var versionStringLength = buffer.ReadU32LE();

        var versionBytes = buffer.ReadBytes((int)versionStringLength);
        var versionString = Encoding.UTF8.GetString(versionBytes).TrimEnd('\0');

        var flags = buffer.ReadU16LE();
        var streams = buffer.ReadU16LE();

        return new ClrMetadataHeader
        {
            Signature = signature,
            MajorVersion = majorVersion,
            MinorVersion = minorVersion,
            Reserved = reserved,
            VersionStringLength = versionStringLength,
            VersionString = versionString,
            Flags = flags,
            Streams = streams
        };
    }

    /// <summary>
    ///     读取流头列表。
    /// </summary>
    private List<ClrStreamHeader> ReadStreamHeaders(ref ByteBuffer buffer, int count)
    {
        var headers = new List<ClrStreamHeader>(count);

        for (var i = 0; i < count; i++)
        {
            var offset = buffer.ReadU32LE();
            var size = buffer.ReadU32LE();
            var name = ReadAlignedStreamName(ref buffer);

            headers.Add(new ClrStreamHeader { Offset = offset, Size = size, Name = name });
        }

        return headers;
    }

    /// <summary>
    ///     读取流名称（null 终止，4 字节对齐）。
    /// </summary>
    private static string ReadAlignedStreamName(ref ByteBuffer buffer)
    {
        var start = buffer.Position;
        var name = buffer.ReadNullTerminatedString();
        var bytesRead = buffer.Position - start;
        var padding = (4 - (bytesRead % 4)) % 4;
        buffer.Advance(padding);

        return name;
    }

    #endregion

    #region 表流解码

    /// <summary>
    ///     解码表流（#~ 或 #-）。
    /// </summary>
    private ClrTableStream DecodeTableStream(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        var reserved = buffer.ReadU32LE();
        var majorVersion = buffer.ReadU8();
        var minorVersion = buffer.ReadU8();
        var heapSizes = buffer.ReadU8();
        buffer.ReadU8();

        var validTables = buffer.ReadU64LE();
        var sortedTables = buffer.ReadU64LE();

        var rowCountMask = validTables;
        var rowCounts = new List<uint>();

        for (var i = 0; i < 64; i++)
        {
            if ((rowCountMask & (1UL << i)) != 0)
            {
                rowCounts.Add(buffer.ReadU32LE());
            }
        }

        var header = new ClrTableHeader
        {
            Reserved = reserved,
            MajorVersion = majorVersion,
            MinorVersion = minorVersion,
            HeapSizes = heapSizes,
            ValidTables = validTables,
            SortedTables = sortedTables,
            RowCounts = rowCounts
        };

        var tables = DecodeTables(ref buffer, header);

        return new ClrTableStream
        {
            Header = header,
            Tables = tables
        };
    }

    /// <summary>
    ///     解码所有元数据表。
    /// </summary>
    private List<ClrTableData> DecodeTables(ref ByteBuffer buffer, ClrTableHeader header)
    {
        var tables = new List<ClrTableData>();
        var rowIndex = 0;

        for (var i = 0; i < 64; i++)
        {
            if ((header.ValidTables & (1UL << i)) == 0)
            {
                continue;
            }

            var kind = (ClrTableKind)i;
            var rowCount = header.RowCounts[rowIndex++];
            var rowSize = GetRowSize(kind, header);

            var rawRows = new List<byte[]>((int)rowCount);

            for (var r = 0; r < rowCount; r++)
            {
                if (buffer.Remaining < rowSize)
                {
                    break;
                }

                rawRows.Add(buffer.ReadBytes(rowSize).ToArray());
            }

            tables.Add(new ClrTableData
            {
                Kind = kind,
                RowCount = rowCount,
                RawRows = rawRows
            });
        }

        return tables;
    }

    /// <summary>
    ///     计算指定表的单行大小（字节）。
    /// </summary>
    private int GetRowSize(ClrTableKind kind, ClrTableHeader header)
    {
        return kind switch
        {
            ClrTableKind.Module => 2 + header.StringIndexSize + header.GuidIndexSize + header.GuidIndexSize + header.GuidIndexSize,
            ClrTableKind.TypeRef => GetCodedIndexSize(ClrCodedIndex.ResolutionScope, header) + header.StringIndexSize + header.StringIndexSize,
            ClrTableKind.TypeDef => 4 + header.StringIndexSize + header.StringIndexSize + GetCodedIndexSize(ClrCodedIndex.TypeDefOrRef, header) + GetTableIndexSize(ClrTableKind.Field, header) + GetTableIndexSize(ClrTableKind.MethodDef, header),
            ClrTableKind.Field => 2 + header.StringIndexSize + header.BlobIndexSize,
            ClrTableKind.MethodDef => 4 + 2 + 2 + header.StringIndexSize + header.BlobIndexSize + GetTableIndexSize(ClrTableKind.Param, header),
            ClrTableKind.Param => 2 + 2 + header.StringIndexSize,
            ClrTableKind.InterfaceImpl => GetTableIndexSize(ClrTableKind.TypeDef, header) + GetCodedIndexSize(ClrCodedIndex.TypeDefOrRef, header),
            ClrTableKind.MemberRef => GetCodedIndexSize(ClrCodedIndex.MemberRefParent, header) + header.StringIndexSize + header.BlobIndexSize,
            ClrTableKind.Constant => 2 + GetCodedIndexSize(ClrCodedIndex.HasConstant, header) + header.BlobIndexSize,
            ClrTableKind.CustomAttribute => GetCodedIndexSize(ClrCodedIndex.HasCustomAttribute, header) + GetCodedIndexSize(ClrCodedIndex.CustomAttributeType, header) + header.BlobIndexSize,
            ClrTableKind.FieldMarshal => GetCodedIndexSize(ClrCodedIndex.HasFieldMarshal, header) + header.BlobIndexSize,
            ClrTableKind.DeclSecurity => 2 + GetCodedIndexSize(ClrCodedIndex.HasDeclSecurity, header) + header.BlobIndexSize,
            ClrTableKind.ClassLayout => 2 + 4 + GetTableIndexSize(ClrTableKind.TypeDef, header),
            ClrTableKind.FieldLayout => 4 + GetTableIndexSize(ClrTableKind.Field, header),
            ClrTableKind.StandAloneSig => header.BlobIndexSize,
            ClrTableKind.EventMap => GetTableIndexSize(ClrTableKind.TypeDef, header) + GetTableIndexSize(ClrTableKind.Event, header),
            ClrTableKind.Event => 2 + header.StringIndexSize + GetCodedIndexSize(ClrCodedIndex.TypeDefOrRef, header),
            ClrTableKind.PropertyMap => GetTableIndexSize(ClrTableKind.TypeDef, header) + GetTableIndexSize(ClrTableKind.Property, header),
            ClrTableKind.Property => 2 + header.StringIndexSize + header.BlobIndexSize,
            ClrTableKind.MethodSemantics => 2 + GetTableIndexSize(ClrTableKind.MethodDef, header) + GetCodedIndexSize(ClrCodedIndex.HasSemantics, header),
            ClrTableKind.MethodImpl => GetTableIndexSize(ClrTableKind.TypeDef, header) + GetCodedIndexSize(ClrCodedIndex.MethodDefOrRef, header) + GetCodedIndexSize(ClrCodedIndex.MethodDefOrRef, header),
            ClrTableKind.ModuleRef => header.StringIndexSize,
            ClrTableKind.TypeSpec => header.BlobIndexSize,
            ClrTableKind.ImplMap => 2 + GetCodedIndexSize(ClrCodedIndex.MemberForwarded, header) + header.StringIndexSize + GetTableIndexSize(ClrTableKind.ModuleRef, header),
            ClrTableKind.FieldRVA => 4 + GetTableIndexSize(ClrTableKind.Field, header),
            ClrTableKind.Assembly => 4 + 2 + 2 + 2 + 2 + 4 + header.BlobIndexSize + header.StringIndexSize + header.StringIndexSize,
            ClrTableKind.AssemblyRef => 2 + 2 + 2 + 2 + 4 + header.BlobIndexSize + header.StringIndexSize + header.StringIndexSize + header.BlobIndexSize,
            ClrTableKind.File => 4 + header.StringIndexSize + header.BlobIndexSize,
            ClrTableKind.ExportedType => 4 + 4 + header.StringIndexSize + header.StringIndexSize + GetCodedIndexSize(ClrCodedIndex.Implementation, header),
            ClrTableKind.ManifestResource => 4 + 4 + GetCodedIndexSize(ClrCodedIndex.Implementation, header) + header.StringIndexSize,
            ClrTableKind.NestedClass => GetTableIndexSize(ClrTableKind.TypeDef, header) + GetTableIndexSize(ClrTableKind.TypeDef, header),
            ClrTableKind.GenericParam => 2 + 2 + GetCodedIndexSize(ClrCodedIndex.TypeOrMethodDef, header) + header.StringIndexSize,
            ClrTableKind.MethodSpec => GetCodedIndexSize(ClrCodedIndex.MethodDefOrRef, header) + header.BlobIndexSize,
            ClrTableKind.GenericParamConstraint => GetTableIndexSize(ClrTableKind.GenericParam, header) + GetCodedIndexSize(ClrCodedIndex.TypeDefOrRef, header),
            _ => 0
        };
    }

    /// <summary>
    ///     获取编码索引大小（2 或 4 字节）。
    /// </summary>
    private int GetCodedIndexSize(ClrCodedIndex codedIndex, ClrTableHeader header)
    {
        var maxRows = GetMaxRowsForCodedIndex(codedIndex, header);
        var tagBits = GetTagBitsForCodedIndex(codedIndex);

        return (maxRows << tagBits) <= 0xFFFF ? 2 : 4;
    }

    /// <summary>
    ///     获取表索引大小（2 或 4 字节）。
    /// </summary>
    private int GetTableIndexSize(ClrTableKind table, ClrTableHeader header)
    {
        var rowCount = GetRowCount(table, header);

        return rowCount <= 0xFFFF ? 2 : 4;
    }

    /// <summary>
    ///     获取指定表的行数。
    /// </summary>
    private uint GetRowCount(ClrTableKind table, ClrTableHeader header)
    {
        var rowIndex = 0;

        for (var i = 0; i < 64; i++)
        {
            if ((header.ValidTables & (1UL << i)) == 0)
            {
                continue;
            }

            if (i == (int)table)
            {
                return header.RowCounts[rowIndex];
            }

            rowIndex++;
        }

        return 0;
    }

    /// <summary>
    ///     获取编码索引涉及的最大行数。
    /// </summary>
    private uint GetMaxRowsForCodedIndex(ClrCodedIndex codedIndex, ClrTableHeader header)
    {
        var tables = GetTablesForCodedIndex(codedIndex);
        var max = 0u;

        foreach (var t in tables)
        {
            var count = GetRowCount(t, header);

            if (count > max)
            {
                max = count;
            }
        }

        return max;
    }

    /// <summary>
    ///     获取编码索引的标签位数。
    /// </summary>
    private static int GetTagBitsForCodedIndex(ClrCodedIndex codedIndex)
    {
        var tables = GetTablesForCodedIndex(codedIndex);
        var count = tables.Length;

        if (count <= 1)
        {
            return 0;
        }

        var bits = 0;

        while ((1 << bits) < count)
        {
            bits++;
        }

        return bits;
    }

    /// <summary>
    ///     获取编码索引涉及的表列表。
    /// </summary>
    private static ClrTableKind[] GetTablesForCodedIndex(ClrCodedIndex codedIndex)
    {
        return codedIndex switch
        {
            ClrCodedIndex.TypeDefOrRef => [ClrTableKind.TypeDef, ClrTableKind.TypeRef, ClrTableKind.TypeSpec],
            ClrCodedIndex.HasConstant => [ClrTableKind.Field, ClrTableKind.Param, ClrTableKind.Property],
            ClrCodedIndex.HasCustomAttribute => [ClrTableKind.MethodDef, ClrTableKind.Field, ClrTableKind.TypeRef, ClrTableKind.TypeDef, ClrTableKind.Param, ClrTableKind.InterfaceImpl, ClrTableKind.MemberRef, ClrTableKind.Module, ClrTableKind.DeclSecurity, ClrTableKind.Property, ClrTableKind.Event, ClrTableKind.StandAloneSig, ClrTableKind.ModuleRef, ClrTableKind.TypeSpec, ClrTableKind.Assembly, ClrTableKind.AssemblyRef, ClrTableKind.File, ClrTableKind.ExportedType, ClrTableKind.ManifestResource, ClrTableKind.GenericParam, ClrTableKind.GenericParamConstraint, ClrTableKind.MethodSpec],
            ClrCodedIndex.HasFieldMarshal => [ClrTableKind.Field, ClrTableKind.Param],
            ClrCodedIndex.HasDeclSecurity => [ClrTableKind.TypeDef, ClrTableKind.MethodDef, ClrTableKind.Assembly],
            ClrCodedIndex.MemberRefParent => [ClrTableKind.TypeDef, ClrTableKind.TypeRef, ClrTableKind.ModuleRef, ClrTableKind.MethodDef, ClrTableKind.TypeSpec],
            ClrCodedIndex.HasSemantics => [ClrTableKind.Event, ClrTableKind.Property],
            ClrCodedIndex.MethodDefOrRef => [ClrTableKind.MethodDef, ClrTableKind.MemberRef],
            ClrCodedIndex.MemberForwarded => [ClrTableKind.Field, ClrTableKind.MethodDef],
            ClrCodedIndex.Implementation => [ClrTableKind.File, ClrTableKind.AssemblyRef, ClrTableKind.ExportedType],
            ClrCodedIndex.CustomAttributeType => [ClrTableKind.MethodDef, ClrTableKind.MemberRef, ClrTableKind.MethodDef],
            ClrCodedIndex.ResolutionScope => [ClrTableKind.Module, ClrTableKind.ModuleRef, ClrTableKind.AssemblyRef, ClrTableKind.TypeRef],
            ClrCodedIndex.TypeOrMethodDef => [ClrTableKind.TypeDef, ClrTableKind.MethodDef],
            _ => []
        };
    }

    #endregion

    #region 表行解析

    /// <summary>
    ///     从原始行数据解析 Module 表行。
    /// </summary>
    private ClrModuleRow? ResolveModuleRow(ClrMetadata metadata)
    {
        var tableData = FindTable(metadata, ClrTableKind.Module);

        if (tableData == null || tableData.RawRows.Count == 0)
        {
            return null;
        }

        var header = metadata.TableStream!.Header;
        var row = tableData.RawRows[0];
        var buf = new ByteBuffer(row);

        return new ClrModuleRow
        {
            Generation = buf.ReadU16LE(),
            Name = metadata.StringHeap.ReadString(ReadIndex(ref buf, header.StringIndexSize)),
            Mvid = metadata.GuidHeap.ReadGuid(ReadIndex(ref buf, header.GuidIndexSize)),
            EncId = metadata.GuidHeap.ReadGuid(ReadIndex(ref buf, header.GuidIndexSize)),
            EncBaseId = metadata.GuidHeap.ReadGuid(ReadIndex(ref buf, header.GuidIndexSize))
        };
    }

    /// <summary>
    ///     解析 TypeDef 表行。
    /// </summary>
    private List<ClrTypeDefRow> ResolveTypeDefRows(ClrMetadata metadata)
    {
        var tableData = FindTable(metadata, ClrTableKind.TypeDef);
        var header = metadata.TableStream?.Header;
        var result = new List<ClrTypeDefRow>();

        if (tableData == null || header == null)
        {
            return result;
        }

        foreach (var row in tableData.RawRows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrTypeDefRow
            {
                Flags = (ClrTypeAttributes)buf.ReadU32LE(),
                Name = metadata.StringHeap.ReadString(ReadIndex(ref buf, header.StringIndexSize)),
                Namespace = metadata.StringHeap.ReadString(ReadIndex(ref buf, header.StringIndexSize)),
                ExtendsIndex = (uint)ReadCodedIndex(ref buf, ClrCodedIndex.TypeDefOrRef, header),
                FieldListStart = (int)ReadTableIndex(ref buf, ClrTableKind.Field, header),
                MethodListStart = (int)ReadTableIndex(ref buf, ClrTableKind.MethodDef, header)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 MethodDef 表行。
    /// </summary>
    private List<ClrMethodDefRow> ResolveMethodDefRows(ClrMetadata metadata)
    {
        var tableData = FindTable(metadata, ClrTableKind.MethodDef);
        var header = metadata.TableStream?.Header;
        var result = new List<ClrMethodDefRow>();

        if (tableData == null || header == null)
        {
            return result;
        }

        foreach (var row in tableData.RawRows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrMethodDefRow
            {
                Rva = buf.ReadU32LE(),
                ImplFlags = buf.ReadU16LE(),
                Flags = (ClrMethodAttributes)buf.ReadU16LE(),
                Name = metadata.StringHeap.ReadString(ReadIndex(ref buf, header.StringIndexSize)),
                SignatureIndex = ReadIndex(ref buf, header.BlobIndexSize),
                ParamListStart = (int)ReadTableIndex(ref buf, ClrTableKind.Param, header)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 Field 表行。
    /// </summary>
    private List<ClrFieldDefRow> ResolveFieldDefRows(ClrMetadata metadata)
    {
        var tableData = FindTable(metadata, ClrTableKind.Field);
        var header = metadata.TableStream?.Header;
        var result = new List<ClrFieldDefRow>();

        if (tableData == null || header == null)
        {
            return result;
        }

        foreach (var row in tableData.RawRows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrFieldDefRow
            {
                Flags = (ClrFieldAttributes)buf.ReadU16LE(),
                Name = metadata.StringHeap.ReadString(ReadIndex(ref buf, header.StringIndexSize)),
                SignatureIndex = ReadIndex(ref buf, header.BlobIndexSize)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 Property 表行。
    /// </summary>
    private List<ClrPropertyDefRow> ResolvePropertyDefRows(ClrMetadata metadata)
    {
        var tableData = FindTable(metadata, ClrTableKind.Property);
        var header = metadata.TableStream?.Header;
        var result = new List<ClrPropertyDefRow>();

        if (tableData == null || header == null)
        {
            return result;
        }

        foreach (var row in tableData.RawRows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrPropertyDefRow
            {
                Flags = buf.ReadU16LE(),
                Name = metadata.StringHeap.ReadString(ReadIndex(ref buf, header.StringIndexSize)),
                SignatureIndex = ReadIndex(ref buf, header.BlobIndexSize)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 Event 表行。
    /// </summary>
    private List<ClrEventDefRow> ResolveEventDefRows(ClrMetadata metadata)
    {
        var tableData = FindTable(metadata, ClrTableKind.Event);
        var header = metadata.TableStream?.Header;
        var result = new List<ClrEventDefRow>();

        if (tableData == null || header == null)
        {
            return result;
        }

        foreach (var row in tableData.RawRows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrEventDefRow
            {
                EventFlags = buf.ReadU16LE(),
                Name = metadata.StringHeap.ReadString(ReadIndex(ref buf, header.StringIndexSize)),
                EventTypeIndex = (uint)ReadCodedIndex(ref buf, ClrCodedIndex.TypeDefOrRef, header)
            });
        }

        return result;
    }

    #endregion

    #region MSIL 方法体解码

    /// <summary>
    ///     解码方法体（MSIL 指令 + 异常处理表）。
    /// </summary>
    private List<ClrMethodDef> DecodeMethodBodies(ReadOnlySpan<byte> data, PeFileData peFile, List<ClrMethodDefRow> methodRows, ClrMetadata metadata)
    {
        var methods = new List<ClrMethodDef>(methodRows.Count);

        foreach (var row in methodRows)
        {
            if (row.Rva == 0)
            {
                methods.Add(new ClrMethodDef
                {
                    Name = row.Name,
                    Flags = row.Flags,
                    Rva = 0,
                    CodeSize = 0,
                    MaxStack = 0,
                    LocalVarSigTok = 0
                });

                continue;
            }

            var offset = peFile.RvaToOffset(row.Rva);

            if (offset < 0 || offset >= data.Length)
            {
                methods.Add(new ClrMethodDef
                {
                    Name = row.Name,
                    Flags = row.Flags,
                    Rva = row.Rva
                });

                continue;
            }

            var methodBody = DecodeMethodBody(data[offset..]);
            var instructions = DecodeInstructions(methodBody.Code);

            methods.Add(new ClrMethodDef
            {
                Name = row.Name,
                Flags = row.Flags,
                Rva = row.Rva,
                CodeSize = methodBody.CodeSize,
                MaxStack = methodBody.MaxStack,
                LocalVarSigTok = methodBody.LocalVarSigTok,
                Instructions = instructions,
                ExceptionHandlers = methodBody.ExceptionHandlers
            });
        }

        return methods;
    }

    /// <summary>
    ///     解码方法头和代码体。
    /// </summary>
    private MethodBodyData DecodeMethodBody(ReadOnlySpan<byte> data)
    {
        if (data.Length < 1)
        {
            return new MethodBodyData();
        }

        var firstByte = data[0];
        var format = (byte)(firstByte & ClrConstants.MethodHeaderFormatMask);

        if (format == ClrConstants.MethodHeaderTinyFlag)
        {
            var codeSize = (byte)(firstByte >> 2);
            var code = codeSize > 0 && codeSize < data.Length ? data[1..(1 + codeSize)] : [];

            return new MethodBodyData
            {
                CodeSize = codeSize,
                MaxStack = 8,
                LocalVarSigTok = 0,
                Code = code.ToArray(),
                ExceptionHandlers = []
            };
        }

        if (format == ClrConstants.MethodHeaderFatFlag)
        {
            var buffer = new ByteBuffer(data);
            var headerWord = buffer.ReadU16LE();
            var maxStack = buffer.ReadU16LE();
            var codeSize = buffer.ReadU32LE();
            var localVarSigTok = buffer.ReadU32LE();

            var hasMoreSects = (headerWord & ClrConstants.MethodHeaderMoreSects) != 0;
            var code = codeSize > 0 && 12 + codeSize <= data.Length ? data[12..(12 + (int)codeSize)] : [];

            var exceptionHandlers = new List<ClrExceptionHandler>();

            if (hasMoreSects)
            {
                var sectOffset = 12 + (int)codeSize;
                AlignTo4(ref sectOffset);

                while (sectOffset + 4 <= data.Length)
                {
                    var sectData = data[sectOffset..];
                    var sectKind = sectData[0];
                    var isFat = (sectKind & ClrConstants.ExceptionHandlerFatFlag) != 0;
                    var moreSects = (sectKind & 0x80) != 0;

                    if ((sectKind & ClrConstants.ExceptionHandlerTableFlag) == 0)
                    {
                        break;
                    }

                    if (isFat)
                    {
                        ReadFatExceptionHandlers(sectData, exceptionHandlers);
                    }
                    else
                    {
                        ReadSmallExceptionHandlers(sectData, exceptionHandlers);
                    }

                    if (!moreSects)
                    {
                        break;
                    }

                    if (isFat)
                    {
                        var dataSize = (int)((sectData[1] << 16) | (sectData[2] << 8) | sectData[3]);
                        sectOffset += dataSize;
                    }
                    else
                    {
                        var dataSize = sectData[1];
                        sectOffset += dataSize;
                    }

                    AlignTo4(ref sectOffset);
                }
            }

            return new MethodBodyData
            {
                CodeSize = codeSize,
                MaxStack = maxStack,
                LocalVarSigTok = localVarSigTok,
                Code = code.ToArray(),
                ExceptionHandlers = exceptionHandlers
            };
        }

        return new MethodBodyData();
    }

    /// <summary>
    ///     读取 Small 异常处理表。
    /// </summary>
    private static void ReadSmallExceptionHandlers(ReadOnlySpan<byte> sectData, List<ClrExceptionHandler> handlers)
    {
        var dataSize = sectData[1];
        var clauseSize = 12;
        var clauseCount = (dataSize - 4) / clauseSize;

        var buffer = new ByteBuffer(sectData[4..]);

        for (var i = 0; i < clauseCount; i++)
        {
            var kind = (ClrExceptionHandlerKind)buffer.ReadU32LE();
            var tryOffset = buffer.ReadU16LE();
            var tryLength = buffer.ReadU8();
            var handlerOffset = buffer.ReadU16LE();
            var handlerLength = buffer.ReadU8();
            var classTokenOrFilter = buffer.ReadU32LE();

            handlers.Add(new ClrExceptionHandler
            {
                HandlerKind = kind,
                TryStart = tryOffset,
                TryLength = tryLength,
                HandlerStart = handlerOffset,
                HandlerLength = handlerLength,
                ClassTokenOrFilterOffset = classTokenOrFilter
            });
        }
    }

    /// <summary>
    ///     读取 Fat 异常处理表。
    /// </summary>
    private static void ReadFatExceptionHandlers(ReadOnlySpan<byte> sectData, List<ClrExceptionHandler> handlers)
    {
        var dataSize = (sectData[1] << 16) | (sectData[2] << 8) | sectData[3];
        var clauseSize = 24;
        var clauseCount = (dataSize - 4) / clauseSize;

        var buffer = new ByteBuffer(sectData[4..]);

        for (var i = 0; i < clauseCount; i++)
        {
            var kind = (ClrExceptionHandlerKind)buffer.ReadU32LE();
            var tryOffset = buffer.ReadU32LE();
            var tryLength = buffer.ReadU32LE();
            var handlerOffset = buffer.ReadU32LE();
            var handlerLength = buffer.ReadU32LE();
            var classTokenOrFilter = buffer.ReadU32LE();

            handlers.Add(new ClrExceptionHandler
            {
                HandlerKind = kind,
                TryStart = tryOffset,
                TryLength = tryLength,
                HandlerStart = handlerOffset,
                HandlerLength = handlerLength,
                ClassTokenOrFilterOffset = classTokenOrFilter
            });
        }
    }

    /// <summary>
    ///     解码 MSIL 指令。
    /// </summary>
    private List<ClrInstruction> DecodeInstructions(byte[] code)
    {
        var instructions = new List<ClrInstruction>();
        var buffer = new ByteBuffer(code);
        var offset = 0u;

        while (!buffer.IsEnd)
        {
            var instrOffset = offset;
            var firstByte = buffer.ReadU8();
            ClrOpcode opcode;

            if (firstByte == ClrConstants.TwoByteOpcodePrefix)
            {
                var secondByte = buffer.ReadU8();
                opcode = (ClrOpcode)(ClrConstants.TwoByteOpcodeBase | secondByte);
                offset += 2;
            }
            else
            {
                opcode = (ClrOpcode)firstByte;
                offset += 1;
            }

            var operand = DecodeOperand(ref buffer, opcode, ref offset);

            instructions.Add(new ClrInstruction
            {
                Offset = instrOffset,
                Opcode = opcode,
                Operand = operand
            });
        }

        return instructions;
    }

    /// <summary>
    ///     解码操作数。
    /// </summary>
    private ClrOperand? DecodeOperand(ref ByteBuffer buffer, ClrOpcode opcode, ref uint offset)
    {
        switch (opcode)
        {
            case ClrOpcode.Nop:
            case ClrOpcode.Break:
            case ClrOpcode.Ldarg_0:
            case ClrOpcode.Ldarg_1:
            case ClrOpcode.Ldarg_2:
            case ClrOpcode.Ldarg_3:
            case ClrOpcode.Ldloc_0:
            case ClrOpcode.Ldloc_1:
            case ClrOpcode.Ldloc_2:
            case ClrOpcode.Ldloc_3:
            case ClrOpcode.Stloc_0:
            case ClrOpcode.Stloc_1:
            case ClrOpcode.Stloc_2:
            case ClrOpcode.Stloc_3:
            case ClrOpcode.Ldnull:
            case ClrOpcode.Ldc_I4_0:
            case ClrOpcode.Ldc_I4_1:
            case ClrOpcode.Ldc_I4_2:
            case ClrOpcode.Ldc_I4_3:
            case ClrOpcode.Ldc_I4_4:
            case ClrOpcode.Ldc_I4_5:
            case ClrOpcode.Ldc_I4_6:
            case ClrOpcode.Ldc_I4_7:
            case ClrOpcode.Ldc_I4_8:
            case ClrOpcode.Ldc_I4_M1:
            case ClrOpcode.Dup:
            case ClrOpcode.Pop:
            case ClrOpcode.Ret:
            case ClrOpcode.Add:
            case ClrOpcode.Sub:
            case ClrOpcode.Mul:
            case ClrOpcode.Div:
            case ClrOpcode.Div_Un:
            case ClrOpcode.Rem:
            case ClrOpcode.Rem_Un:
            case ClrOpcode.And:
            case ClrOpcode.Or:
            case ClrOpcode.Xor:
            case ClrOpcode.Shl:
            case ClrOpcode.Shr:
            case ClrOpcode.Shr_Un:
            case ClrOpcode.Neg:
            case ClrOpcode.Not:
            case ClrOpcode.Conv_I1:
            case ClrOpcode.Conv_I2:
            case ClrOpcode.Conv_I4:
            case ClrOpcode.Conv_I8:
            case ClrOpcode.Conv_R4:
            case ClrOpcode.Conv_R8:
            case ClrOpcode.Conv_U4:
            case ClrOpcode.Conv_U8:
            case ClrOpcode.Conv_R_Un:
            case ClrOpcode.Conv_U:
            case ClrOpcode.Conv_Ovf_I1:
            case ClrOpcode.Conv_Ovf_U1:
            case ClrOpcode.Conv_Ovf_I2:
            case ClrOpcode.Conv_Ovf_U2:
            case ClrOpcode.Conv_Ovf_I4:
            case ClrOpcode.Conv_Ovf_U4:
            case ClrOpcode.Conv_Ovf_I8:
            case ClrOpcode.Conv_Ovf_U8:
            case ClrOpcode.Conv_Ovf_I:
            case ClrOpcode.Conv_Ovf_U:
            case ClrOpcode.Conv_Ovf_I1_Un:
            case ClrOpcode.Conv_Ovf_I2_Un:
            case ClrOpcode.Conv_Ovf_I4_Un:
            case ClrOpcode.Conv_Ovf_I8_Un:
            case ClrOpcode.Conv_Ovf_U1_Un:
            case ClrOpcode.Conv_Ovf_U2_Un:
            case ClrOpcode.Conv_Ovf_U4_Un:
            case ClrOpcode.Conv_Ovf_U8_Un:
            case ClrOpcode.Conv_Ovf_I_Un:
            case ClrOpcode.Conv_Ovf_U_Un:
            case ClrOpcode.Ldind_I1:
            case ClrOpcode.Ldind_U1:
            case ClrOpcode.Ldind_I2:
            case ClrOpcode.Ldind_U2:
            case ClrOpcode.Ldind_I4:
            case ClrOpcode.Ldind_U4:
            case ClrOpcode.Ldind_I8:
            case ClrOpcode.Ldind_I:
            case ClrOpcode.Ldind_R4:
            case ClrOpcode.Ldind_R8:
            case ClrOpcode.Ldind_Ref:
            case ClrOpcode.Stind_Ref:
            case ClrOpcode.Stind_I1:
            case ClrOpcode.Stind_I2:
            case ClrOpcode.Stind_I4:
            case ClrOpcode.Stind_I8:
            case ClrOpcode.Stind_R4:
            case ClrOpcode.Stind_R8:
            case ClrOpcode.Stind_I:
            case ClrOpcode.Throw:
            case ClrOpcode.Endfilter:
            case ClrOpcode.Rethrow:
            case ClrOpcode.Cpblk:
            case ClrOpcode.Initblk:
            case ClrOpcode.Ldlen:
            case ClrOpcode.Arglist:
            case ClrOpcode.Ceq:
            case ClrOpcode.Cgt:
            case ClrOpcode.Cgt_Un:
            case ClrOpcode.Clt:
            case ClrOpcode.Clt_Un:
            case ClrOpcode.Localloc:
            case ClrOpcode.Readonly:
            case ClrOpcode.Volatile:
            case ClrOpcode.Tail:
                return null;

            case ClrOpcode.Ldarg_S:
            case ClrOpcode.Ldarga_S:
            case ClrOpcode.Starg_S:
            case ClrOpcode.Ldloc_S:
            case ClrOpcode.Ldloca_S:
            case ClrOpcode.Stloc_S:
            {
                var val = buffer.ReadU8();
                offset += 1;
                return opcode is ClrOpcode.Ldarg_S or ClrOpcode.Ldarga_S or ClrOpcode.Starg_S
                    ? new ClrArgumentIndexOperand { Index = val }
                    : new ClrLocalIndexOperand { Index = val };
            }

            case ClrOpcode.Ldarg:
            case ClrOpcode.Ldarga:
            case ClrOpcode.Starg:
            {
                var val = buffer.ReadU16LE();
                offset += 2;
                return opcode == ClrOpcode.Starg
                    ? new ClrArgumentIndexOperand { Index = val }
                    : new ClrArgumentIndexOperand { Index = val };
            }

            case ClrOpcode.Ldloc:
            case ClrOpcode.Ldloca:
            case ClrOpcode.Stloc:
            {
                var val = buffer.ReadU16LE();
                offset += 2;
                return new ClrLocalIndexOperand { Index = val };
            }

            case ClrOpcode.Ldc_I4_S:
            {
                var val = buffer.ReadI8();
                offset += 1;
                return new ClrInt8Operand { Value = val };
            }

            case ClrOpcode.Ldc_I4:
            {
                var val = buffer.ReadI32LE();
                offset += 4;
                return new ClrInt32Operand { Value = val };
            }

            case ClrOpcode.Ldc_I8:
            {
                var val = buffer.ReadI64LE();
                offset += 8;
                return new ClrInt64Operand { Value = val };
            }

            case ClrOpcode.Ldc_R4:
            {
                var val = buffer.ReadF32LE();
                offset += 4;
                return new ClrFloat32Operand { Value = val };
            }

            case ClrOpcode.Ldc_R8:
            {
                var val = buffer.ReadF64LE();
                offset += 8;
                return new ClrFloat64Operand { Value = val };
            }

            case ClrOpcode.Br_S:
            case ClrOpcode.Brfalse_S:
            case ClrOpcode.Brtrue_S:
            case ClrOpcode.Beq_S:
            case ClrOpcode.Bne_Un_S:
            case ClrOpcode.Blt_S:
            case ClrOpcode.Ble_S:
            case ClrOpcode.Bgt_S:
            case ClrOpcode.Bge_S:
            case ClrOpcode.Blt_Un_S:
            case ClrOpcode.Ble_Un_S:
            case ClrOpcode.Bgt_Un_S:
            case ClrOpcode.Bge_Un_S:
            case ClrOpcode.Leave_S:
            {
                var delta = buffer.ReadI8();
                offset += 1;
                return new ClrBranchTarget8Operand { Offset = (int)offset + delta };
            }

            case ClrOpcode.Br:
            case ClrOpcode.Brfalse:
            case ClrOpcode.Brtrue:
            case ClrOpcode.Beq:
            case ClrOpcode.Bne_Un:
            case ClrOpcode.Blt:
            case ClrOpcode.Ble:
            case ClrOpcode.Bgt:
            case ClrOpcode.Bge:
            case ClrOpcode.Blt_Un:
            case ClrOpcode.Ble_Un:
            case ClrOpcode.Bgt_Un:
            case ClrOpcode.Bge_Un:
            case ClrOpcode.Leave:
            {
                var delta = buffer.ReadI32LE();
                offset += 4;
                return new ClrBranchTarget32Operand { Offset = (int)offset + delta };
            }

            case ClrOpcode.Switch:
            {
                var count = buffer.ReadU32LE();
                offset += 4;
                var targets = new int[count];

                for (var i = 0; i < count; i++)
                {
                    targets[i] = (int)offset + (int)count * 4 + buffer.ReadI32LE();
                    offset += 4;
                }

                return new ClrSwitchTargetsOperand { Offsets = targets };
            }

            case ClrOpcode.Call:
            case ClrOpcode.Callvirt:
            case ClrOpcode.Newobj:
            case ClrOpcode.Ldstr:
            case ClrOpcode.Ldftn:
            case ClrOpcode.Ldvirtftn:
            case ClrOpcode.Ldtoken:
            case ClrOpcode.Castclass:
            case ClrOpcode.Isinst:
            case ClrOpcode.Unbox:
            case ClrOpcode.Unbox_Any:
            case ClrOpcode.Box:
            case ClrOpcode.Newarr:
            case ClrOpcode.Ldelema:
            case ClrOpcode.Ldelem_Any:
            case ClrOpcode.Stelem_Any:
            case ClrOpcode.Initobj:
            case ClrOpcode.Constrained:
            case ClrOpcode.Jmp:
            case ClrOpcode.Calli:
            case ClrOpcode.Cpobj:
            case ClrOpcode.Ldobj:
            case ClrOpcode.Stobj:
            case ClrOpcode.Ldfld:
            case ClrOpcode.Ldflda:
            case ClrOpcode.Stfld:
            case ClrOpcode.Ldsfld:
            case ClrOpcode.Ldsflda:
            case ClrOpcode.Stsfld:
            case ClrOpcode.Sizeof:
            case ClrOpcode.Mkrefany:
            case ClrOpcode.Refanyval:
            case ClrOpcode.Refanytype:
            {
                var token = buffer.ReadU32LE();
                offset += 4;
                return new ClrTokenOperand { Value = token };
            }

            case ClrOpcode.Unaligned:
            {
                var val = buffer.ReadU8();
                offset += 1;
                return new ClrInt8Operand { Value = (sbyte)val };
            }

            default:
                return null;
        }
    }

    #endregion

    #region 类型构建

    /// <summary>
    ///     从 TypeDef 行构建高级类型视图。
    /// </summary>
    private List<ClrTypeDef> BuildTypeDefs(
        List<ClrTypeDefRow> typeDefRows,
        List<ClrMethodDefRow> methodDefRows,
        List<ClrFieldDefRow> fieldDefRows,
        List<ClrPropertyDefRow> propertyDefRows,
        List<ClrEventDefRow> eventDefRows,
        List<ClrMethodDef> methods,
        ClrMetadata metadata)
    {
        var types = new List<ClrTypeDef>(typeDefRows.Count);

        for (var i = 0; i < typeDefRows.Count; i++)
        {
            var row = typeDefRows[i];
            var nextFieldStart = i + 1 < typeDefRows.Count ? typeDefRows[i + 1].FieldListStart : fieldDefRows.Count + 1;
            var nextMethodStart = i + 1 < typeDefRows.Count ? typeDefRows[i + 1].MethodListStart : methodDefRows.Count + 1;

            var typeFields = fieldDefRows.Skip(row.FieldListStart - 1).Take(nextFieldStart - row.FieldListStart)
                .Select(f => new ClrFieldDef { Name = f.Name, Flags = f.Flags, SignatureIndex = f.SignatureIndex }).ToList();
            var typeMethods = methods.Skip(row.MethodListStart - 1).Take(nextMethodStart - row.MethodListStart).ToList();

            types.Add(new ClrTypeDef
            {
                Name = row.Name,
                Namespace = row.Namespace,
                Flags = row.Flags,
                ExtendsIndex = row.ExtendsIndex,
                Fields = typeFields,
                Methods = typeMethods
            });
        }

        return types;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     在元数据中查找指定类型的表。
    /// </summary>
    private static ClrTableData? FindTable(ClrMetadata metadata, ClrTableKind kind)
    {
        if (metadata.TableStream == null)
        {
            return null;
        }

        foreach (var table in metadata.TableStream.Tables)
        {
            if (table.Kind == kind)
            {
                return table;
            }
        }

        return null;
    }

    /// <summary>
    ///     读取堆索引（2 或 4 字节）。
    /// </summary>
    private static uint ReadIndex(ref ByteBuffer buffer, int size)
    {
        return size == 4 ? buffer.ReadU32LE() : buffer.ReadU16LE();
    }

    /// <summary>
    ///     读取表索引（2 或 4 字节）。
    /// </summary>
    private uint ReadTableIndex(ref ByteBuffer buffer, ClrTableKind table, ClrTableHeader header)
    {
        return GetTableIndexSize(table, header) == 4 ? buffer.ReadU32LE() : buffer.ReadU16LE();
    }

    /// <summary>
    ///     读取编码索引（2 或 4 字节）。
    /// </summary>
    private uint ReadCodedIndex(ref ByteBuffer buffer, ClrCodedIndex codedIndex, ClrTableHeader header)
    {
        return GetCodedIndexSize(codedIndex, header) == 4 ? buffer.ReadU32LE() : buffer.ReadU16LE();
    }

    /// <summary>
    ///     将偏移量对齐到 4 字节边界。
    /// </summary>
    private static void AlignTo4(ref int offset)
    {
        offset = (offset + 3) & ~3;
    }

    #endregion

    #region 内部类型

    /// <summary>
    ///     CLR 编码索引类型（ECMA-335 §23.2.8）。
    /// </summary>
    private enum ClrCodedIndex
    {
        TypeDefOrRef,
        HasConstant,
        HasCustomAttribute,
        HasFieldMarshal,
        HasDeclSecurity,
        MemberRefParent,
        HasSemantics,
        MethodDefOrRef,
        MemberForwarded,
        Implementation,
        CustomAttributeType,
        ResolutionScope,
        TypeOrMethodDef
    }

    /// <summary>
    ///     方法体解码中间数据。
    /// </summary>
    private sealed class MethodBodyData
    {
        public uint CodeSize { get; init; }
        public ushort MaxStack { get; init; }
        public uint LocalVarSigTok { get; init; }
        public byte[] Code { get; init; } = [];
        public IReadOnlyList<ClrExceptionHandler> ExceptionHandlers { get; init; } = [];
    }

    #endregion
}

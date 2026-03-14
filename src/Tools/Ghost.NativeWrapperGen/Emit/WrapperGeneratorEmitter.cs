using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Model;
using Ghost.NativeWrapperGen.Parsing;
using Ghost.NativeWrapperGen.Transform;

namespace Ghost.NativeWrapperGen.Emit;

public sealed class WrapperGeneratorEmitter
{
    public IEnumerable<GeneratedFile> Emit(NativeLibrary library, WrapperConfig config)
    {
        var naming = new NamingConventions(config);
        var resolver = new PublicTypeResolver(library, config, naming);
        var ownedTypes = config.OwnedTypes.ToDictionary(static o => o.NativeType, StringComparer.Ordinal);
        var marshalledTypes = config.MarshalledTypes.ToDictionary(static m => m.NativeType, StringComparer.Ordinal);
        var partialTypes = new HashSet<string>(config.PartialTypes, StringComparer.Ordinal);
        var manualMethods = config.StaticMethods.ToDictionary(static m => m.NativeFunction, StringComparer.Ordinal);

        yield return EmitHelpers(config);

        foreach (var @struct in library.Structs.Where(static s => !s.Name.StartsWith("_", StringComparison.Ordinal)).OrderBy(static s => s.Name, StringComparer.Ordinal))
        {
            if (@struct.IsList)
            {
                if (@struct.IsPointerList && @struct.ListElementType is not null && resolver.HasWrapper(@struct.ListElementType))
                {
                    yield return EmitPointerList(config, naming, @struct);
                }
                else if (string.Equals(@struct.ListElementType, "void", StringComparison.Ordinal))
                {
                    yield return EmitVoidList(config, naming, @struct);
                }

                continue;
            }

            ownedTypes.TryGetValue(@struct.Name, out var owned);
            marshalledTypes.TryGetValue(@struct.Name, out var marshalled);
            var isPartialType = partialTypes.Contains(@struct.Name);

            if (marshalled is not null)
            {
                yield return EmitMarshalledWrapper(library, config, naming, resolver, @struct, marshalled, owned);
            }
            else
            {
                yield return EmitWrapper(library, config, naming, resolver, @struct, owned, isPartialType, manualMethods);
            }
        }

        yield return EmitAutoStaticApi(library, config, naming, resolver, manualMethods);
    }

    // ─── Helpers file ────────────────────────────────────────────────────────

    private static GeneratedFile EmitHelpers(WrapperConfig config)
    {
        var writer = new CodeWriter();
        writer.WriteLine("using System.Text;");
        writer.WriteLine();
        writer.WriteLine($"namespace {config.WrapperNamespace};");
        writer.WriteLine();
        writer.WriteLine("internal static unsafe class NativeWrapperHelpers");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            foreach (var stringType in config.SpecialTypes.Strings)
            {
                EmitStringHelpers(writer, stringType);
                writer.WriteLine();
            }

            foreach (var blobType in config.SpecialTypes.Blobs)
            {
                EmitBlobHelpers(writer, blobType);
                writer.WriteLine();
            }

            writer.WriteLine("public static void ThrowIfOutOfRange(int index, int count)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("if ((uint)index >= (uint)count)");
                writer.WriteLine("{");
                using (writer.IndentScope())
                {
                    writer.WriteLine("throw new ArgumentOutOfRangeException(nameof(index));");
                }
                writer.WriteLine("}");
            }
            writer.WriteLine("}");
        }
        writer.WriteLine("}");

        return new GeneratedFile
        {
            FileName = "NativeWrapperHelpers.nativegen.cs",
            Content = writer.ToString(),
        };
    }

    private static void EmitStringHelpers(CodeWriter writer, StringTypeConfig config)
    {
        writer.WriteLine($"public static ReadOnlySpan<byte> AsByteSpan({config.Type} value)");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            writer.WriteLine($"if (value.{config.DataField} == null || value.{config.LengthField} == 0)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("return ReadOnlySpan<byte>.Empty;");
            }
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine($"return new ReadOnlySpan<byte>((byte*)value.{config.DataField}, checked((int)value.{config.LengthField}) * {Math.Max(1, config.CharSize / 8)});");
        }
        writer.WriteLine("}");
        writer.WriteLine();
        writer.WriteLine($"public static string GetString({config.Type} value)");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            writer.WriteLine("var bytes = AsByteSpan(value);");
            writer.WriteLine("if (bytes.IsEmpty)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("return string.Empty;");
            }
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine(config.Encoding.ToLowerInvariant() switch
            {
                "utf16" => "return Encoding.Unicode.GetString(bytes);",
                "utf32" => "return Encoding.UTF32.GetString(bytes);",
                _ => "return Encoding.UTF8.GetString(bytes);",
            });
        }
        writer.WriteLine("}");
    }

    private static void EmitBlobHelpers(CodeWriter writer, BlobTypeConfig config)
    {
        writer.WriteLine($"public static ReadOnlySpan<{config.ElementType}> AsSpan({config.Type} value)");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            writer.WriteLine($"if (value.{config.DataField} == null || value.{config.LengthField} == 0)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine($"return ReadOnlySpan<{config.ElementType}>.Empty;");
            }
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine($"return new ReadOnlySpan<{config.ElementType}>(value.{config.DataField}, checked((int)value.{config.LengthField}));");
        }
        writer.WriteLine("}");
    }

    // ─── Marshalled type wrapper (heap-pointer struct) ────────────────────────

    private static GeneratedFile EmitMarshalledWrapper(NativeLibrary library, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, NativeStruct @struct, MarshalledTypeConfig marshalled, OwnedTypeConfig? owned)
    {
        var writer = new CodeWriter();
        writer.WriteLine($"namespace {config.WrapperNamespace};");
        writer.WriteLine();

        var wrapperName = naming.GetWrapperTypeName(@struct.Name);
        var wrapperKind = GetWrapperKind(config, @struct.Name, owned);
        var marshalledPropsByNative = marshalled.MarshalledProperties.ToDictionary(static p => p.Native, StringComparer.Ordinal);

        writer.WriteLine($"public unsafe partial {wrapperKind} {wrapperName} : System.IDisposable");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            // Pointer + alloc flag
            writer.WriteLine($"private {@struct.Name}* _ptr;");
            writer.WriteLine("private bool _csAlloc;");
            writer.WriteLine();

            // Default constructor — alloc on heap, zero-fill
            writer.WriteLine($"public {wrapperName}()");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine($"_ptr = ({@struct.Name}*)System.Runtime.InteropServices.NativeMemory.AllocZeroed((nuint)sizeof({@struct.Name}));");
                writer.WriteLine("_csAlloc = true;");
            }
            writer.WriteLine("}");
            writer.WriteLine();

            // Internal constructor from existing pointer (e.g. native API returned it)
            writer.WriteLine($"internal {wrapperName}({@struct.Name}* ptr)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("_ptr = ptr;");
                writer.WriteLine("_csAlloc = false;");
            }
            writer.WriteLine("}");
            writer.WriteLine();

            writer.WriteLine("public bool IsNull => _ptr == null;");
            writer.WriteLine();

            // Partial Dispose stub — hand-written impl frees cstrings + conditionally frees _ptr
            writer.WriteLine("public partial void Dispose();");
            writer.WriteLine();

            // Emit properties for each member
            foreach (var member in @struct.Members.Where(static m =>
                m.Name != "Anonymous"
                && !m.Name.StartsWith("_", StringComparison.Ordinal)
                && !m.TypeName.StartsWith("_", StringComparison.Ordinal)
                && !m.TypeName.Contains("<", StringComparison.Ordinal)
                && !m.TypeName.Contains("ref ", StringComparison.Ordinal)))
            {
                EmitMarshalledMember(writer, config, naming, resolver, wrapperName, member, marshalledPropsByNative);
            }

            writer.WriteLine($"internal {@struct.Name}* GetUnsafePtr() => _ptr;");
        }
        writer.WriteLine("}");

        return new GeneratedFile
        {
            FileName = $"{wrapperName}.nativegen.cs",
            Content = writer.ToString(),
        };
    }

    private static void EmitMarshalledMember(CodeWriter writer, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, string wrapperName, NativeMember member, Dictionary<string, MarshalledPropertyConfig> marshalledProps)
    {
        var propertyName = GetSafePropertyName(wrapperName, naming.GetPropertyName(member.Name));

        // Marshalled property → emit partial property stub + backing field (hand-written impl manages cstring lifetime)
        if (marshalledProps.TryGetValue(member.Name, out var marshalledProp))
        {
            var fieldName = "_" + char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
            writer.WriteLine($"private {marshalledProp.Type} {fieldName};");
            writer.WriteLine($"public partial {marshalledProp.Type} {propertyName} {{ get; set; }}");
            writer.WriteLine();
            return;
        }

        var pointerDepth = BindingParser.GetPointerDepth(member.TypeName);

        // Skip function pointer and deep pointer fields
        if (pointerDepth > 1)
        {
            return;
        }

        // String special type → read-only helpers via pointer dereference
        var stringType = config.SpecialTypes.Strings.FirstOrDefault(s => s.Type == member.TypeName);
        if (stringType is not null)
        {
            if (stringType.EmitRawSpanProperty)
            {
                writer.WriteLine($"public ReadOnlySpan<byte> {propertyName}Bytes => NativeWrapperHelpers.AsByteSpan(_ptr->{member.Name});");
            }
            if (stringType.EmitStringProperty)
            {
                writer.WriteLine($"public string {propertyName} => NativeWrapperHelpers.GetString(_ptr->{member.Name});");
            }
            writer.WriteLine();
            return;
        }

        // Blob special type → read-only span via pointer dereference
        var blobType = config.SpecialTypes.Blobs.FirstOrDefault(b => b.Type == member.TypeName);
        if (blobType is not null)
        {
            writer.WriteLine($"public ReadOnlySpan<{blobType.ElementType}> {propertyName} => NativeWrapperHelpers.AsSpan(_ptr->{member.Name});");
            writer.WriteLine();
            return;
        }

        // Pointer field (depth == 1) — expose raw pointer as read/write
        if (pointerDepth == 1)
        {
            writer.WriteLine($"public {member.TypeName} {propertyName} {{ get => _ptr->{member.Name}; set => _ptr->{member.Name} = value; }}");
            writer.WriteLine();
            return;
        }

        // Plain value field — direct read/write through pointer
        writer.WriteLine($"public {resolver.GetPublicType(member.TypeName)} {propertyName} {{ get => _ptr->{member.Name}; set => _ptr->{member.Name} = value; }}");
        writer.WriteLine();
    }

    // ─── Pointer-based wrapper (read-only view) ───────────────────────────────

    private static GeneratedFile EmitWrapper(NativeLibrary library, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, NativeStruct @struct, OwnedTypeConfig? owned, bool isPartialType, Dictionary<string, StaticMethodConfig> manualMethods)
    {
        var writer = new CodeWriter();
        writer.WriteLine($"namespace {config.WrapperNamespace};");
        writer.WriteLine();

        var wrapperName = naming.GetWrapperTypeName(@struct.Name);
        var wrapperKind = GetWrapperKind(config, @struct.Name, owned);
        var implementsIDisposable = wrapperKind == "class" && !string.IsNullOrWhiteSpace(owned?.FreeFunction);
        var partialKeyword = isPartialType ? "partial " : string.Empty;

        writer.WriteLine($"public unsafe {partialKeyword}{GetWrapperDeclaration(wrapperName, wrapperKind, implementsIDisposable)}");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            writer.WriteLine(GetPointerFieldDeclaration(@struct.Name, wrapperKind));
            writer.WriteLine();
            writer.WriteLine($"internal {wrapperName}({@struct.Name}* ptr)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("_ptr = ptr;");
            }
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine("public bool IsNull => _ptr == null;");
            writer.WriteLine();

            if (!string.IsNullOrWhiteSpace(owned?.FreeFunction))
            {
                writer.WriteLine("public void Dispose()");
                writer.WriteLine("{");
                using (writer.IndentScope())
                {
                    writer.WriteLine("if (_ptr != null)");
                    writer.WriteLine("{");
                    using (writer.IndentScope())
                    {
                        writer.WriteLine($"Api.{owned.FreeFunction}(_ptr);");
                        writer.WriteLine("_ptr = null;");
                    }
                    writer.WriteLine("}");
                }
                writer.WriteLine("}");
                writer.WriteLine();
            }

            // Emit instance methods auto-routed to this wrapper type
            foreach (var func in library.Functions.Where(f => f.IsDllImport))
            {
                var routing = ResolveAutoTarget(library, config, naming, func, manualMethods);
                if (routing.TargetType != wrapperName)
                {
                    continue;
                }

                if (manualMethods.TryGetValue(func.Name, out var manual))
                {
                    EmitStaticMethod(writer, library, config, naming, resolver, manual, routing.Kind == RoutingKind.InstanceMethod ? wrapperName : null);
                }
                else
                {
                    EmitAutoMethod(writer, library, config, naming, resolver, func, routing.Kind == RoutingKind.InstanceMethod ? wrapperName : null);
                }
                writer.WriteLine();
            }

            foreach (var member in @struct.Members.Where(static m => m.Name != "Anonymous" && !m.Name.StartsWith("_", StringComparison.Ordinal)))
            {
                EmitMember(writer, library, config, naming, resolver, wrapperName, member);
            }

            writer.WriteLine($"internal {@struct.Name}* GetUnsafePtr() => _ptr;");
        }
        writer.WriteLine("}");

        return new GeneratedFile
        {
            FileName = $"{wrapperName}.nativegen.cs",
            Content = writer.ToString(),
        };
    }

    // ─── Member emission (pointer-based) ─────────────────────────────────────

    private static void EmitMember(CodeWriter writer, NativeLibrary library, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, string wrapperName, NativeMember member)
    {
        if (member.TypeName.StartsWith("_", StringComparison.Ordinal))
        {
            return;
        }

        if (member.TypeName.Contains("<", StringComparison.Ordinal) || member.TypeName.Contains("ref ", StringComparison.Ordinal))
        {
            return;
        }

        var propertyName = GetSafePropertyName(wrapperName, naming.GetPropertyName(member.Name));
        var pointerDepth = BindingParser.GetPointerDepth(member.TypeName);
        var baseType = BindingParser.TrimPointers(member.TypeName);

        var stringType = config.SpecialTypes.Strings.FirstOrDefault(s => s.Type == member.TypeName);
        if (stringType is not null)
        {
            if (stringType.EmitRawSpanProperty)
            {
                writer.WriteLine($"public ReadOnlySpan<byte> {propertyName}Bytes => NativeWrapperHelpers.AsByteSpan(_ptr->{member.Name});");
            }

            if (stringType.EmitStringProperty)
            {
                writer.WriteLine($"public string {propertyName} => NativeWrapperHelpers.GetString(_ptr->{member.Name});");
            }

            writer.WriteLine();
            return;
        }

        var blobType = config.SpecialTypes.Blobs.FirstOrDefault(b => b.Type == member.TypeName);
        if (blobType is not null)
        {
            writer.WriteLine($"public ReadOnlySpan<{blobType.ElementType}> {propertyName} => NativeWrapperHelpers.AsSpan(_ptr->{member.Name});");
            writer.WriteLine();
            return;
        }

        if (library.StructsByName.TryGetValue(baseType, out var listStruct) && listStruct.IsList)
        {
            EmitListMember(writer, naming, resolver, member, listStruct, propertyName);
            return;
        }

        if (pointerDepth == 1 && resolver.HasWrapper(baseType))
        {
            var wrapperType = resolver.GetPublicType(member.TypeName);
            writer.WriteLine($"public bool Has{propertyName} => _ptr->{member.Name} != null;");
            writer.WriteLine($"public {wrapperType} {propertyName} => _ptr->{member.Name} != null ? new(_ptr->{member.Name}) : throw new InvalidOperationException(\"{propertyName} is null.\");");
            writer.WriteLine();
            return;
        }

        if (pointerDepth > 0)
        {
            writer.WriteLine($"public {member.TypeName} {propertyName} => _ptr->{member.Name};");
            writer.WriteLine();
            return;
        }

        if (resolver.HasWrapper(baseType))
        {
            var wrapperType = naming.GetWrapperTypeName(baseType);
            writer.WriteLine($"public {wrapperType} {propertyName} => new(({baseType}*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->{member.Name}));");
            writer.WriteLine();
            return;
        }

        writer.WriteLine($"public {resolver.GetPublicType(member.TypeName)} {propertyName} => _ptr->{member.Name};");
        writer.WriteLine();
    }

    private static void EmitListMember(CodeWriter writer, NamingConventions naming, PublicTypeResolver resolver, NativeMember member, NativeStruct listStruct, string propertyName)
    {
        if (listStruct.ListElementType is null)
        {
            writer.WriteLine($"public {member.TypeName} {propertyName} => _ptr->{member.Name};");
            writer.WriteLine();
            return;
        }

        if (listStruct.IsPointerList && resolver.HasWrapper(listStruct.ListElementType))
        {
            var listWrapperName = naming.GetWrapperTypeName(listStruct.Name);
            writer.WriteLine($"public {listWrapperName} {propertyName} => new(_ptr->{member.Name}.data, _ptr->{member.Name}.count);");
            writer.WriteLine();
            return;
        }

        if (string.Equals(listStruct.ListElementType, "void", StringComparison.Ordinal))
        {
            var listWrapperName = naming.GetWrapperTypeName(listStruct.Name);
            writer.WriteLine($"public {listWrapperName} {propertyName} => new(_ptr->{member.Name}.data, _ptr->{member.Name}.count);");
            writer.WriteLine();
            return;
        }

        var elementType = resolver.GetPublicType(listStruct.ListElementType);
        writer.WriteLine($"public ReadOnlySpan<{elementType}> {propertyName} => _ptr->{member.Name}.data == null ? ReadOnlySpan<{elementType}>.Empty : new ReadOnlySpan<{elementType}>(_ptr->{member.Name}.data, checked((int)_ptr->{member.Name}.count));");
        writer.WriteLine();
    }

    // ─── List wrappers ────────────────────────────────────────────────────────

    private static GeneratedFile EmitPointerList(WrapperConfig config, NamingConventions naming, NativeStruct listStruct)
    {
        var writer = new CodeWriter();
        var wrapperName = naming.GetWrapperTypeName(listStruct.Name);
        var elementType = listStruct.ListElementType!;
        var elementWrapperName = naming.GetWrapperTypeName(elementType);

        writer.WriteLine($"namespace {config.WrapperNamespace};");
        writer.WriteLine();
        writer.WriteLine($"public unsafe readonly ref struct {wrapperName}");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            writer.WriteLine($"private readonly {elementType}** _data;");
            writer.WriteLine("public int Count { get; }");
            writer.WriteLine();
            writer.WriteLine($"internal {wrapperName}({elementType}** data, nuint count)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("_data = data;");
                writer.WriteLine("Count = checked((int)count);");
            }
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine($"public {elementWrapperName} this[int index]");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("get");
                writer.WriteLine("{");
                using (writer.IndentScope())
                {
                    writer.WriteLine("NativeWrapperHelpers.ThrowIfOutOfRange(index, Count);");
                    writer.WriteLine("return new(_data[index]);");
                }
                writer.WriteLine("}");
            }
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine("public Enumerator GetEnumerator() => new(_data, Count);");
            writer.WriteLine();
            writer.WriteLine("public unsafe ref struct Enumerator");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine($"private readonly {elementType}** _data;");
                writer.WriteLine("private readonly int _count;");
                writer.WriteLine("private int _index;");
                writer.WriteLine();
                writer.WriteLine($"internal Enumerator({elementType}** data, int count)");
                writer.WriteLine("{");
                using (writer.IndentScope())
                {
                    writer.WriteLine("_data = data;");
                    writer.WriteLine("_count = count;");
                    writer.WriteLine("_index = -1;");
                }
                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine($"public {elementWrapperName} Current => new(_data[_index]);");
                writer.WriteLine();
                writer.WriteLine("public bool MoveNext()");
                writer.WriteLine("{");
                using (writer.IndentScope())
                {
                    writer.WriteLine("var next = _index + 1;");
                    writer.WriteLine("if (next >= _count)");
                    writer.WriteLine("{");
                    using (writer.IndentScope())
                    {
                        writer.WriteLine("return false;");
                    }
                    writer.WriteLine("}");
                    writer.WriteLine();
                    writer.WriteLine("_index = next;");
                    writer.WriteLine("return true;");
                }
                writer.WriteLine("}");
            }
            writer.WriteLine("}");
        }
        writer.WriteLine("}");

        return new GeneratedFile
        {
            FileName = $"{wrapperName}.nativegen.cs",
            Content = writer.ToString(),
        };
    }

    private static GeneratedFile EmitVoidList(WrapperConfig config, NamingConventions naming, NativeStruct listStruct)
    {
        var writer = new CodeWriter();
        var wrapperName = naming.GetWrapperTypeName(listStruct.Name);

        writer.WriteLine($"namespace {config.WrapperNamespace};");
        writer.WriteLine();
        writer.WriteLine($"public unsafe readonly ref struct {wrapperName}");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            writer.WriteLine("private readonly void* _data;");
            writer.WriteLine("public int Count { get; }");
            writer.WriteLine();
            writer.WriteLine($"internal {wrapperName}(void* data, nuint count)");
            writer.WriteLine("{");
            using (writer.IndentScope())
            {
                writer.WriteLine("_data = data;");
                writer.WriteLine("Count = checked((int)count);");
            }
            writer.WriteLine("}");
            writer.WriteLine();
            writer.WriteLine("public void* Data => _data;");
        }
        writer.WriteLine("}");

        return new GeneratedFile
        {
            FileName = $"{wrapperName}.nativegen.cs",
            Content = writer.ToString(),
        };
    }

    // ─── Auto-dispatch static API ─────────────────────────────────────────────

    private static GeneratedFile EmitAutoStaticApi(NativeLibrary library, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, Dictionary<string, StaticMethodConfig> manualMethods)
    {
        var staticTypeName = config.StaticApiClassName ?? (config.LibraryName + "Global");
        var writer = new CodeWriter();

        writer.WriteLine($"namespace {config.WrapperNamespace};");
        writer.WriteLine();
        writer.WriteLine($"public static unsafe class {staticTypeName}");
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
        foreach (var func in library.Functions.Where(static f => f.IsDllImport).OrderBy(static f => f.Name, StringComparer.Ordinal))
        {
            var routing = ResolveAutoTarget(library, config, naming, func, manualMethods);
            if (routing.TargetType != staticTypeName)
            {
                continue;
            }

            if (manualMethods.TryGetValue(func.Name, out var manual))
            {
                EmitStaticMethod(writer, library, config, naming, resolver, manual, null);
            }
            else
            {
                EmitAutoMethod(writer, library, config, naming, resolver, func, null);
            }
            writer.WriteLine();
        }
        }
        writer.WriteLine("}");

        return new GeneratedFile
        {
            FileName = $"{staticTypeName}.nativegen.cs",
            Content = writer.ToString(),
        };
    }

    // ─── Auto routing ─────────────────────────────────────────────────────────

    private enum RoutingKind { GlobalStatic, InstanceMethod, StaticOnType }

    private sealed record RoutingResult(string TargetType, RoutingKind Kind);

    /// <summary>
    /// Classifies a native function into one of three routing categories:
    /// a) TKnown* func(somethingElse...) → return-type wrapper class (static factory)
    /// b) TKnown1 func(TKnown0*, ...) → first-param wrapper type (instance method)
    /// c) everything else → global static class
    /// Manual configs that set StaticType override the auto-routing.
    /// Manual configs with selfPointer adapter force route (b).
    /// </summary>
    private static RoutingResult ResolveAutoTarget(NativeLibrary library, WrapperConfig config, NamingConventions naming, NativeFunction func, Dictionary<string, StaticMethodConfig> manualMethods)
    {
        var staticTypeName = config.StaticApiClassName ?? (config.LibraryName + "Global");

        // Manual override: StaticType wins
        if (manualMethods.TryGetValue(func.Name, out var manual) && !string.IsNullOrWhiteSpace(manual.StaticType))
        {
            return new(manual.StaticType, RoutingKind.StaticOnType);
        }

        // Manual override: selfPointer wins (route to that type as instance method)
        if (manualMethods.TryGetValue(func.Name, out manual))
        {
            var selfParam = manual.Parameters.FirstOrDefault(static p => p.Adapter == "selfPointer");
            if (selfParam is not null)
            {
                var nativeParam = func.Parameters.FirstOrDefault(p => string.Equals(p.Name, selfParam.Native, StringComparison.Ordinal));
                if (nativeParam is not null)
                {
                    var baseType = BindingParser.TrimPointers(nativeParam.TypeName);
                    if (library.StructsByName.ContainsKey(baseType))
                    {
                        return new(naming.GetWrapperTypeName(baseType), RoutingKind.InstanceMethod);
                    }
                }
            }
        }

        // Rule (b): first param is T* where T is a known wrapper type → instance on T
        if (func.Parameters.Count > 0)
        {
            var firstParam = func.Parameters[0];
            var firstBase = BindingParser.TrimPointers(firstParam.TypeName);
            if (BindingParser.GetPointerDepth(firstParam.TypeName) == 1 && library.StructsByName.ContainsKey(firstBase))
            {
                return new(naming.GetWrapperTypeName(firstBase), RoutingKind.InstanceMethod);
            }
        }

        // Rule (a): return type is T* where T is a known wrapper type → static on T
        var returnPointerDepth = BindingParser.GetPointerDepth(func.ReturnType);
        if (returnPointerDepth == 1)
        {
            var returnBase = BindingParser.TrimPointers(func.ReturnType);
            if (library.StructsByName.ContainsKey(returnBase))
            {
                return new(naming.GetWrapperTypeName(returnBase), RoutingKind.StaticOnType);
            }
        }

        // Rule (c): global static
        return new(staticTypeName, RoutingKind.GlobalStatic);
    }

    // ─── Auto method emission ─────────────────────────────────────────────────

    private static void EmitAutoMethod(CodeWriter writer, NativeLibrary library, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, NativeFunction func, string? instanceTypeName)
    {
        var isInstanceMethod = instanceTypeName is not null;

        // Build parameter list, skipping the first param if it's the self pointer
        var parameters = new List<string>();
        var argumentExpressions = new List<string>();
        var marshalledTypes = config.MarshalledTypes.ToDictionary(static m => m.NativeType, StringComparer.Ordinal);
        var skipFirst = isInstanceMethod;

        foreach (var param in func.Parameters)
        {
            if (skipFirst)
            {
                skipFirst = false;
                argumentExpressions.Add("_ptr");
                continue;
            }

            var publicName = NamingConventions.ToPascalCase(param.Name);
            publicName = char.ToLowerInvariant(publicName[0]) + publicName[1..];

            var pointerDepth = BindingParser.GetPointerDepth(param.TypeName);
            var baseType = BindingParser.TrimPointers(param.TypeName);

            if (pointerDepth == 1 && marshalledTypes.TryGetValue(baseType, out _))
            {
                // Pass marshalled wrapper by pointer directly — no copy needed
                var wrapperType = naming.GetWrapperTypeName(baseType);
                parameters.Add($"{wrapperType} {publicName}");
                argumentExpressions.Add($"{publicName}.GetUnsafePtr()");
            }
            else if (pointerDepth == 1 && library.StructsByName.ContainsKey(baseType))
            {
                var wrapperType = naming.GetWrapperTypeName(baseType);
                parameters.Add($"{wrapperType} {publicName}");
                argumentExpressions.Add($"{publicName}.GetUnsafePtr()");
            }
            else
            {
                parameters.Add($"{param.TypeName} {publicName}");
                argumentExpressions.Add(publicName);
            }
        }

        var (returnType, returnConversion) = ResolveReturnConversion(config, resolver, naming, func.ReturnType);
        var methodName = naming.GetWrapperTypeName(func.Name);
        var staticKeyword = isInstanceMethod ? string.Empty : "static ";
        var signatureLine = $"public {staticKeyword}{returnType} {methodName}({string.Join(", ", parameters)})";

        writer.WriteLine(signatureLine);
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            var callExpression = $"Api.{func.Name}({string.Join(", ", argumentExpressions)})";
            var returnsWrappedType = BindingParser.GetPointerDepth(func.ReturnType) == 1 && resolver.HasWrapper(BindingParser.TrimPointers(func.ReturnType));
            var returnsVoid = string.Equals(func.ReturnType, "void", StringComparison.Ordinal);

            if (returnsWrappedType)
            {
                writer.WriteLine($"return new({callExpression});");
            }
            else if (returnsVoid)
            {
                writer.WriteLine($"{callExpression};");
            }
            else if (returnConversion is not null)
            {
                writer.WriteLine($"return {string.Format(returnConversion, callExpression)};");
            }
            else
            {
                writer.WriteLine($"return {callExpression};");
            }
        }
        writer.WriteLine("}");
    }

    // ─── Manual method emission ───────────────────────────────────────────────

    private static void EmitStaticMethod(CodeWriter writer, NativeLibrary library, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, StaticMethodConfig methodConfig, string? wrapperName)
    {
        if (!library.FunctionsByName.TryGetValue(methodConfig.NativeFunction, out var nativeFunction))
        {
            return;
        }

        var signature = BuildMethodSignature(library, config, naming, resolver, methodConfig, nativeFunction, wrapperName);
        writer.WriteLine(signature.SignatureLine);
        writer.WriteLine("{");
        using (writer.IndentScope())
        {
            foreach (var line in signature.BodyLines)
            {
                writer.WriteLine(line);
            }
        }
        writer.WriteLine("}");
    }

    private static GeneratedMethod BuildMethodSignature(NativeLibrary library, WrapperConfig config, NamingConventions naming, PublicTypeResolver resolver, StaticMethodConfig methodConfig, NativeFunction nativeFunction, string? wrapperName)
    {
        var parameters = new List<string>();
        var preCallLines = new List<string>();
        var argumentExpressions = new List<string>();
        string? pendingSpanSource = null;
        string? failureVariable = null;
        var isInstanceMethod = false;

        foreach (var parameter in nativeFunction.Parameters)
        {
            var configParameter = methodConfig.Parameters.FirstOrDefault(p => p.Native == parameter.Name);
            if (configParameter is null)
            {
                // Implicit selfPointer: first unconfigured T* param where T is a known struct
                if (!isInstanceMethod && argumentExpressions.Count == 0)
                {
                    var pd = BindingParser.GetPointerDepth(parameter.TypeName);
                    var bt = BindingParser.TrimPointers(parameter.TypeName);
                    if (pd == 1 && library.StructsByName.ContainsKey(bt))
                    {
                        isInstanceMethod = true;
                        argumentExpressions.Add("_ptr");
                        continue;
                    }
                }

                argumentExpressions.Add(parameter.Name);
                parameters.Add($"{parameter.TypeName} {parameter.Name}");
                continue;
            }

            var publicName = configParameter.PublicName ?? NamingConventions.ToPascalCase(parameter.Name);
            publicName = char.ToLowerInvariant(publicName[0]) + publicName[1..];

            switch (configParameter.Adapter)
            {
                case "selfPointer":
                    isInstanceMethod = true;
                    argumentExpressions.Add("_ptr");
                    break;
                case "utf8Path":
                    parameters.Add($"ReadOnlySpan<byte> {publicName}");
                    preCallLines.Add($"fixed (byte* {publicName}Ptr = {publicName})");
                    argumentExpressions.Add($"(sbyte*){publicName}Ptr");
                    pendingSpanSource = publicName;
                    break;
                case "utf8Length":
                    argumentExpressions.Add($"(nuint){configParameter.Source ?? pendingSpanSource}.Length");
                    break;
                case "byteSpan":
                    parameters.Add($"ReadOnlySpan<byte> {publicName}");
                    preCallLines.Add($"fixed (byte* {publicName}Ptr = {publicName})");
                    argumentExpressions.Add($"{publicName}Ptr");
                    pendingSpanSource = publicName;
                    break;
                case "byteSpanLength":
                    argumentExpressions.Add($"(nuint){configParameter.Source ?? pendingSpanSource}.Length");
                    break;
                case "inValue":
                    {
                        var typeName = configParameter.Type ?? parameter.TypeName.TrimEnd('*').Trim();
                        var defaultValue = configParameter.OptionalDefault ? " = default" : string.Empty;
                        parameters.Add($"in {typeName} {publicName}{defaultValue}");
                        preCallLines.Add($"var {publicName}Local = {publicName};");
                        argumentExpressions.Add($"&{publicName}Local");
                        break;
                    }
                case "errorOut":
                    {
                        var typeName = configParameter.Type ?? parameter.TypeName.TrimEnd('*').Trim();
                        failureVariable = publicName;
                        preCallLines.Add($"{typeName} {publicName} = default;");
                        argumentExpressions.Add($"&{publicName}");
                        break;
                    }
                case "getPtr":
                    {
                        var typeName = configParameter.Type ?? parameter.TypeName.TrimEnd('*').Trim();
                        parameters.Add($"{typeName} {publicName}");
                        argumentExpressions.Add($"{publicName}.GetUnsafePtr()");
                        break;
                    }
                default:
                    parameters.Add($"{configParameter.Type ?? parameter.TypeName} {publicName}");
                    argumentExpressions.Add(publicName);
                    break;
            }
        }

        var (returnType, returnConversion) = ResolveReturnConversion(config, resolver, naming, nativeFunction.ReturnType);
        var methodName = methodConfig.MethodName ?? naming.GetWrapperTypeName(methodConfig.NativeFunction);
        var staticKeyword = isInstanceMethod ? string.Empty : "static ";
        var signatureLine = $"public {staticKeyword}{returnType} {methodName}({string.Join(", ", parameters)})";

        var body = new List<string>();
        var fixedLines = preCallLines.Where(static l => l.StartsWith("fixed ", StringComparison.Ordinal)).ToList();
        var normalLines = preCallLines.Where(static l => !l.StartsWith("fixed ", StringComparison.Ordinal)).ToList();
        body.AddRange(normalLines);

        if (fixedLines.Count > 0)
        {
            foreach (var fixedLine in fixedLines)
            {
                body.Add(fixedLine);
            }

            body.Add("{");
        }

        var callExpression = $"Api.{nativeFunction.Name}({string.Join(", ", argumentExpressions)})";
        var returnsWrappedType = BindingParser.GetPointerDepth(nativeFunction.ReturnType) == 1 && resolver.HasWrapper(BindingParser.TrimPointers(nativeFunction.ReturnType));

        if (returnsWrappedType)
        {
            body.Add($"var value = {callExpression};");
            if (methodConfig.ThrowOnNullReturn)
            {
                body.Add("if (value == null)");
                body.Add("{");
                if (!string.IsNullOrWhiteSpace(failureVariable) && !string.IsNullOrWhiteSpace(methodConfig.FailureMessageMember))
                {
                    body.Add($"    throw new InvalidOperationException(NativeWrapperHelpers.GetString({failureVariable}.{methodConfig.FailureMessageMember}));");
                }
                else
                {
                    body.Add("    throw new InvalidOperationException(\"Native call failed.\");");
                }
                body.Add("}");
            }

            body.Add("return new(value);");
        }
        else if (string.Equals(nativeFunction.ReturnType, "void", StringComparison.Ordinal))
        {
            body.Add($"{callExpression};");
        }
        else if (returnConversion is not null)
        {
            body.Add($"return {string.Format(returnConversion, callExpression)};");
        }
        else
        {
            body.Add($"return {callExpression};");
        }

        if (fixedLines.Count > 0)
        {
            body.Add("}");
        }

        return new GeneratedMethod(signatureLine, body);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns (publicReturnType, conversionWrapper) where conversionWrapper is
    /// a format string like "NativeWrapperHelpers.GetString({0})" if the native
    /// return type is a special string/blob that needs conversion, or null otherwise.
    /// </summary>
    private static (string PublicType, string? ConversionFormat) ResolveReturnConversion(
        WrapperConfig config, PublicTypeResolver resolver, NamingConventions naming, string nativeReturnType)
    {
        // Bare void
        if (string.Equals(nativeReturnType, "void", StringComparison.Ordinal))
        {
            return ("void", null);
        }

        // Special string types (e.g. ufbx_string → string via GetString)
        var stringType = config.SpecialTypes.Strings.FirstOrDefault(s =>
            string.Equals(s.Type, nativeReturnType, StringComparison.Ordinal));
        if (stringType is not null)
        {
            return ("string", "NativeWrapperHelpers.GetString({0})");
        }

        // Special blob types (e.g. ufbx_blob → ReadOnlySpan<byte> via AsSpan)
        var blobType = config.SpecialTypes.Blobs.FirstOrDefault(b =>
            string.Equals(b.Type, nativeReturnType, StringComparison.Ordinal));
        if (blobType is not null)
        {
            return ($"ReadOnlySpan<{blobType.ElementType}>", "NativeWrapperHelpers.AsSpan({0})");
        }

        var publicType = ResolveGeneratedReturnType(resolver, naming, nativeReturnType);
        return (publicType, null);
    }

    private static string ResolveGeneratedReturnType(PublicTypeResolver resolver, NamingConventions naming, string nativeReturnType)
    {
        // Bare void — never convert to void*
        if (string.Equals(nativeReturnType, "void", StringComparison.Ordinal))
        {
            return "void";
        }

        var pointerDepth = BindingParser.GetPointerDepth(nativeReturnType);
        if (pointerDepth == 1)
        {
            var baseType = BindingParser.TrimPointers(nativeReturnType);
            if (resolver.HasWrapper(baseType))
            {
                return naming.GetWrapperTypeName(baseType);
            }
        }

        return resolver.GetPublicType(nativeReturnType);
    }

    private static string GetWrapperKind(WrapperConfig config, string nativeTypeName, OwnedTypeConfig? owned)
    {
        if (owned?.WrapperKind is not null)
        {
            return owned.WrapperKind;
        }

        if (config.Wrappers.Kinds.TryGetValue(nativeTypeName, out var kind))
        {
            return kind;
        }

        return owned is null ? config.Wrappers.DefaultKind : config.Wrappers.DefaultOwnedKind;
    }

    private static string GetWrapperDeclaration(string wrapperName, string wrapperKind, bool implementsIDisposable = false)
    {
        var baseDeclaration = wrapperKind switch
        {
            "class" => $"class {wrapperName}",
            "ref struct" => $"ref struct {wrapperName}",
            "readonly ref struct" => $"readonly ref struct {wrapperName}",
            _ => $"struct {wrapperName}",
        };

        if (implementsIDisposable && wrapperKind == "class")
        {
            return $"{baseDeclaration} : IDisposable";
        }

        return baseDeclaration;
    }

    private static string GetPointerFieldDeclaration(string nativeTypeName, string wrapperKind)
    {
        return wrapperKind switch
        {
            "class" => $"private {nativeTypeName}* _ptr;",
            "ref struct" => $"private {nativeTypeName}* _ptr;",
            "readonly ref struct" => $"private readonly {nativeTypeName}* _ptr;",
            _ => $"private {nativeTypeName}* _ptr;",
        };
    }

    private static string GetSafePropertyName(string wrapperName, string propertyName)
    {
        if (string.Equals(wrapperName, propertyName, StringComparison.Ordinal))
        {
            return propertyName + "Value";
        }

        return propertyName;
    }

    private sealed record GeneratedMethod(string SignatureLine, IReadOnlyList<string> BodyLines);
}

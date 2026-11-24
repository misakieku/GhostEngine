using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Emit;
using Ghost.NativeWrapperGen.Model;
using Ghost.NativeWrapperGen.Transform;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.CompilerServices;

namespace Ghost.UnitTest.Tools;

[TestClass]
public class NativeWrapperGenTests
{
    [TestMethod]
    public void NegatedConditions_EvaluateAccuratelyWithoutShortCircuiting()
    {
        var library = CreateLibrary(
            structs: ["MyStruct"],
            functions:
            [
                new NativeFunction
                {
                    Name = "my_self_void",
                    ReturnType = "void",
                    IsDllImport = true,
                    Parameters = [new NativeParameter { Name = "s", TypeName = "MyStruct*" }],
                },
                new NativeFunction
                {
                    Name = "my_other_int",
                    ReturnType = "int",
                    IsDllImport = true,
                    Parameters = [new NativeParameter { Name = "x", TypeName = "int" }],
                },
                new NativeFunction
                {
                    Name = "my_other_void",
                    ReturnType = "void",
                    IsDllImport = true,
                    Parameters = [new NativeParameter { Name = "x", TypeName = "int" }],
                },
            ]);

        var config = new WrapperConfig
        {
            LibraryName = "test",
            NativeNamespace = "TestNs",
            OutputNamespace = "TestNs",
            NativeTypePrefix = "my_",
            Actions =
            [
                new ActionConfig
                {
                    Filter = "EXTERN_API",
                    Conditions = ["!SELF_PTR", "VOID_RETURN"],
                    TargetType = "TestApi",
                    Apply =
                    [
                        new ActionApplyConfig
                        {
                            Type = "STATIC_METHOD",
                            Opts = new ApplyOptsConfig
                            {
                                Name = new NameOptsConfig { Remove = ["PREFIX"], Style = "PascalCase" },
                            },
                        },
                    ],
                },
            ],
        };

        var files = new WrapperGeneratorEmitter().Emit(library, config).ToList();
        Assert.HasCount(1, files);
        var content = files[0].Content;
        Assert.Contains("OtherVoid(", content);
        Assert.DoesNotContain("SelfVoid(", content);
        Assert.DoesNotContain("OtherInt(", content);
    }

    [TestMethod]
    public void EmptyCondition_DoesNotThrowAndDoesNotMatch()
    {
        var library = CreateLibrary(
            structs: [],
            functions:
            [
                new NativeFunction
                {
                    Name = "my_fn",
                    ReturnType = "void",
                    IsDllImport = true,
                    Parameters = [],
                },
            ]);

        var config = new WrapperConfig
        {
            LibraryName = "test",
            NativeNamespace = "TestNs",
            OutputNamespace = "TestNs",
            NativeTypePrefix = "my_",
            Actions =
            [
                new ActionConfig
                {
                    Filter = "EXTERN_API",
                    Conditions = [""],
                    TargetType = "TestApi",
                    Apply = [new ActionApplyConfig { Type = "STATIC_METHOD" }],
                },
            ],
        };

        var files = new WrapperGeneratorEmitter().Emit(library, config).ToList();
        Assert.HasCount(0, files);
    }

    [TestMethod]
    public void DerivesFrom_SkipsSelfParamNonNumericSiblingsVoidPtrAndSupportsFallthrough()
    {
        var library = CreateLibrary(
            structs: ["MyMesh"],
            functions:
            [
                // Self param must not consume num_mesh; only items + num_items should remap.
                new NativeFunction
                {
                    Name = "my_mesh_set",
                    ReturnType = "void",
                    IsDllImport = true,
                    Parameters =
                    [
                        new NativeParameter { Name = "mesh", TypeName = "MyMesh*" },
                        new NativeParameter { Name = "num_mesh", TypeName = "nuint" },
                        new NativeParameter { Name = "items", TypeName = "int*" },
                        new NativeParameter { Name = "num_items", TypeName = "nuint" },
                    ],
                },
                // Must skip int* outlen and match int len; also tests length-before-pointer and void* guard.
                new NativeFunction
                {
                    Name = "my_decode",
                    ReturnType = "void",
                    IsDllImport = true,
                    Parameters =
                    [
                        new NativeParameter { Name = "buffer", TypeName = "byte*" },
                        new NativeParameter { Name = "outlen", TypeName = "int*" },
                        new NativeParameter { Name = "len", TypeName = "int" },
                        new NativeParameter { Name = "num_raw", TypeName = "nuint" },
                        new NativeParameter { Name = "raw", TypeName = "void*" },
                    ],
                },
                // First sbyte* remap requires _len (absent); should fall through to second sbyte* remap.
                new NativeFunction
                {
                    Name = "my_open",
                    ReturnType = "void",
                    IsDllImport = true,
                    Parameters =
                    [
                        new NativeParameter { Name = "name", TypeName = "sbyte*" },
                    ],
                },
            ]);

        var config = new WrapperConfig
        {
            LibraryName = "test",
            NativeNamespace = "TestNs",
            OutputNamespace = "TestNs",
            NativeTypePrefix = "my_",
            Remaps =
            [
                new RemapConfig
                {
                    Src = "sbyte*",
                    Dst = "ReadOnlySpan<byte>",
                    Scope = ["parameter"],
                    Filter = [".*name"],
                    DerivesFrom = new DerivesFromConfig { ParamSuffix = "_len", Expr = "(nuint)$arg.Length" },
                    Adapter = new AdapterConfig
                    {
                        ConvertBack = new ConvertBackConfig { WrapCall = "fixed (byte* p$arg = $arg) { $CALL }", PassAs = "(sbyte*)p$arg" },
                    },
                },
                new RemapConfig
                {
                    Src = "sbyte*",
                    Dst = "ReadOnlySpan<byte>",
                    Scope = ["parameter"],
                    Filter = [".*name"],
                    Adapter = new AdapterConfig
                    {
                        ConvertBack = new ConvertBackConfig { WrapCall = "fixed (byte* p$arg = $arg) { $CALL }", PassAs = "(sbyte*)p$arg" },
                    },
                },
                new RemapConfig
                {
                    Src = "$TYPE*",
                    Dst = "ReadOnlySpan<$TYPE>",
                    Scope = ["parameter"],
                    Filter = [".*buffer"],
                    DerivesFrom = new DerivesFromConfig { RegexName = ".*len", Expr = "$arg.Length" },
                    Adapter = new AdapterConfig
                    {
                        ConvertBack = new ConvertBackConfig { WrapCall = "fixed ($TYPE* p$arg = $arg) { $CALL }", PassAs = "p$arg" },
                    },
                },
                new RemapConfig
                {
                    Src = "$TYPE*",
                    Dst = "ReadOnlySpan<$TYPE>",
                    Scope = ["parameter"],
                    DerivesFrom = new DerivesFromConfig { ParamPrefix = "num_", Expr = "(nuint)$arg.Length" },
                    Adapter = new AdapterConfig
                    {
                        ConvertBack = new ConvertBackConfig { WrapCall = "fixed ($TYPE* p$arg = $arg) { $CALL }", PassAs = "p$arg" },
                    },
                },
            ],
            Actions =
            [
                new ActionConfig
                {
                    Filter = "EXTERN_API",
                    Conditions = ["SELF_PTR"],
                    TargetType = "FIRST_PARAM_TYPE",
                    Apply =
                    [
                        new ActionApplyConfig
                        {
                            Type = "INSTANCE_METHOD",
                            Opts = new ApplyOptsConfig
                            {
                                RemoveFirstParam = true,
                                Name = new NameOptsConfig { Remove = ["PREFIX", "$TBare"], Style = "PascalCase" },
                            },
                        },
                    ],
                },
                new ActionConfig
                {
                    Filter = "EXTERN_API",
                    TargetType = "TestApi",
                    Apply =
                    [
                        new ActionApplyConfig
                        {
                            Type = "STATIC_METHOD",
                            Opts = new ApplyOptsConfig
                            {
                                Name = new NameOptsConfig { Remove = ["PREFIX"], Style = "PascalCase" },
                            },
                        },
                    ],
                },
            ],
        };

        var files = new WrapperGeneratorEmitter().Emit(library, config).ToDictionary(static f => f.FileName, static f => f.Content);
        var meshCode = files["MyMesh.nativegen.cs"];
        Assert.Contains("public void MeshSet(nuint num_mesh, ReadOnlySpan<int> items)", meshCode);
        Assert.Contains("System.Runtime.CompilerServices.Unsafe.AsPointer(ref this)", meshCode);
        Assert.DoesNotContain("mesh.Length", meshCode);

        var apiCode = files["TestApi.nativegen.cs"];
        Assert.Contains("public static void Decode(ReadOnlySpan<byte> buffer, int* outlen, nuint num_raw, void* raw)", apiCode);
        Assert.DoesNotContain("ReadOnlySpan<void>", apiCode);
        Assert.Contains("public static void Open(ReadOnlySpan<byte> name)", apiCode);
    }

    [TestMethod]
    public void GeneratedCode_WithKeywordParamsDelegateCrefInheritanceOnlyAndReturnRemap_CompilesCleanly()
    {
        var library = CreateLibrary(
            structs: ["MyObj"],
            functions:
            [
                new NativeFunction
                {
                    Name = "my_obj_use",
                    ReturnType = "int",
                    IsDllImport = true,
                    Parameters =
                    [
                        new NativeParameter { Name = "self", TypeName = "MyObj*" },
                        new NativeParameter { Name = "@params", TypeName = "sbyte*" },
                        new NativeParameter { Name = "params_len", TypeName = "nuint" },
                        new NativeParameter { Name = "cb", TypeName = "delegate* unmanaged[Cdecl]<void*, int, void>" },
                    ],
                },
                new NativeFunction
                {
                    Name = "my_get_bytes",
                    ReturnType = "byte*",
                    IsDllImport = true,
                    Parameters =
                    [
                        new NativeParameter { Name = "x", TypeName = "int" },
                    ],
                },
                new NativeFunction
                {
                    Name = "my_marker",
                    ReturnType = "void",
                    IsDllImport = true,
                    Parameters = [],
                },
            ]);

        var config = new WrapperConfig
        {
            LibraryName = "test",
            NativeNamespace = "TestNs",
            OutputNamespace = "TestNs",
            NativeTypePrefix = "my_",
            Remaps =
            [
                new RemapConfig
                {
                    Src = "sbyte*",
                    Dst = "ReadOnlySpan<byte>",
                    Scope = ["parameter"],
                    DerivesFrom = new DerivesFromConfig { ParamSuffix = "_len", Expr = "(nuint)$arg.Length" },
                    Adapter = new AdapterConfig
                    {
                        ConvertBack = new ConvertBackConfig { WrapCall = "fixed (byte* p$arg = $arg) { $CALL", PassAs = "(sbyte*)p$arg" },
                    },
                },
                new RemapConfig
                {
                    Src = "byte*",
                    Dst = "nint",
                    Scope = ["return"],
                    Adapter = new AdapterConfig { ConvertTo = "(nint)$result" },
                },
            ],
            Actions =
            [
                new ActionConfig
                {
                    Filter = "EXTERN_API",
                    Conditions = ["SELF_PTR"],
                    TargetType = "FIRST_PARAM_TYPE",
                    Apply =
                    [
                        new ActionApplyConfig
                        {
                            Type = "INSTANCE_METHOD",
                            Opts = new ApplyOptsConfig
                            {
                                RemoveFirstParam = false,
                                Name = new NameOptsConfig { Set = "Use" },
                            },
                        },
                    ],
                },
                new ActionConfig
                {
                    Filter = "EXTERN_API",
                    Conditions = ["NAME_CONDITION(^my_marker$)"],
                    TargetType = "EmptyInherited",
                    Apply =
                    [
                        new ActionApplyConfig
                        {
                            Type = "INHERITANCE",
                            Opts = new ApplyOptsConfig { BaseType = ["System.IDisposable"] },
                        },
                    ],
                },
                new ActionConfig
                {
                    Filter = "EXTERN_API",
                    TargetType = "TestApi",
                    Apply =
                    [
                        new ActionApplyConfig
                        {
                            Type = "STATIC_METHOD",
                            Opts = new ApplyOptsConfig
                            {
                                Name = new NameOptsConfig { Remove = ["PREFIX"], Style = "PascalCase" },
                            },
                        },
                    ],
                },
            ],
        };

        var files = new WrapperGeneratorEmitter().Emit(library, config).ToDictionary(static f => f.FileName, static f => f.Content);
        Assert.IsTrue(files.ContainsKey("EmptyInherited.nativegen.cs"));
        Assert.Contains(": System.IDisposable", files["EmptyInherited.nativegen.cs"]);

        var objCode = files["MyObj.nativegen.cs"];
        Assert.Contains("delegate* unmanaged[Cdecl]{void*, int, void}", objCode);
        Assert.Contains("public int Use(MyObj* self, ReadOnlySpan<byte> @params, delegate* unmanaged[Cdecl]<void*, int, void> cb)", objCode);
        Assert.Contains("fixed (byte* pparams = @params)", objCode);
        Assert.Contains("(nuint)@params.Length", objCode);

        var apiCode = files["TestApi.nativegen.cs"];
        Assert.Contains("public static nint GetBytes(int x)", apiCode);
        Assert.Contains("var __result = Api.my_get_bytes(x);", apiCode);
        Assert.Contains("return (nint)__result;", apiCode);

        const string stubApi = """
            global using System;
            namespace TestNs;
            public unsafe partial struct MyObj { }
            public unsafe partial struct EmptyInherited : System.IDisposable { public void Dispose() { } }
            public unsafe partial struct TestApi { }
            public static unsafe class Api
            {
                public static int my_obj_use(MyObj* self, sbyte* @params, nuint params_len, delegate* unmanaged[Cdecl]<void*, int, void> cb) => 0;
                public static byte* my_get_bytes(int x) => null;
                public static void my_marker() { }
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Diagnose);
        var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(stubApi, parseOptions) };
        trees.AddRange(files.Values.Select(code => CSharpSyntaxTree.ParseText(code, parseOptions)));

        var refs = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Unsafe).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ReadOnlySpan<byte>).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create(
            "GeneratedVerify",
            trees,
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        var diagnostics = compilation.GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error || d.Id == "CS1570")
            .ToList();
        Assert.IsEmpty(diagnostics, string.Join(Environment.NewLine, diagnostics));
    }

    [TestMethod]
    public void NamingConventions_TrailingUnderscoreInPascalCase_DoesNotThrow()
    {
        var config = new WrapperConfig
        {
            LibraryName = "test",
            NativeNamespace = "TestNs",
            OutputNamespace = "TestNs",
            NativeTypePrefix = "my_",
        };

        var naming = new NamingConventions(config);
        var result = naming.GetName("_foo_bar_", new NameOptsConfig { Style = "PascalCase" }, "TestApi");
        Assert.AreEqual("FooBar", result);
    }

    private static NativeLibrary CreateLibrary(IReadOnlyList<string> structs, IReadOnlyList<NativeFunction> functions)
    {
        var nativeStructs = structs.Select(static name => new NativeStruct
        {
            Name = name,
            Namespace = "TestNs",
            Members = [],
            IsList = false,
            IsPointerList = false,
        }).ToArray();

        return new NativeLibrary
        {
            NativeNamespace = "TestNs",
            Structs = nativeStructs,
            Enums = [],
            Functions = functions,
            StructsByName = nativeStructs.ToDictionary(static s => s.Name, StringComparer.Ordinal),
            FunctionsByName = functions.ToDictionary(static f => f.Name, StringComparer.Ordinal),
        };
    }
}

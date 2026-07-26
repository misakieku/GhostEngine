using Ghost.Core;
using Ghost.DSL.ShaderCompiler;
using Ghost.MicroTest.Core;

namespace Ghost.MicroTest;

internal class DslCompilerTest : ITest
{
    public void Setup()
    {
    }

    public void Run()
    {
        var code = File.ReadAllText("F:\\csharp\\GhostEngine\\src\\Runtime\\Ghost.Engine\\Assets\\EngineResources\\Shaders\\Blit.gshdr");
        var sytax = DSLShaderCompiler.ParseGraphicsShaderSyntax(code).GetValueOrThrow();
        var semantics = DSLShaderCompiler.GetShaderSemantics(sytax).GetValueOrThrow();
        var descriptor = DSLShaderCompiler.ResolveShader(semantics, new DSL.Models.ShaderReflectionData(), new Dictionary<string, string>()).GetValueOrThrow();
    }

    public void Cleanup()
    {
    }
}

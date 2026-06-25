using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Ghost.Generator;

[Generator]
internal class EntryPointGenerator : IIncrementalGenerator
{
    private class MethodData
    {
        public string className;
        public string methodName;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var initializeSymbols = context.SyntaxProvider
            .ForAttributeWithMetadataName(
            "Ghost.Engine.RuntimeInitializeAttribute",
                (n, ct) => n is MethodDeclarationSyntax,
                (ctx, ct) =>
                {
                    var methodSymbol = (IMethodSymbol)ctx.TargetSymbol;

                    if (!methodSymbol.IsStatic || (methodSymbol.Parameters.Length != 1 && methodSymbol.Parameters[0].Name != "EngineCore"))
                    {
                        return null;
                    }

                    var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var methodName = methodSymbol.Name;
                    return new MethodData { className = className, methodName = methodName };
                })
            .Where(data => data != null)
            .Collect();

        var shutdownSymbols = context.SyntaxProvider
            .ForAttributeWithMetadataName(
            "Ghost.Engine.RuntimeShutdownAttribute",
                (n, ct) => n is MethodDeclarationSyntax,
                (ctx, ct) =>
                {
                    var methodSymbol = (IMethodSymbol)ctx.TargetSymbol;

                    if (!methodSymbol.IsStatic || (methodSymbol.Parameters.Length != 1 && methodSymbol.Parameters[0].Name != "EngineCore"))
                    {
                        return null;
                    }

                    var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var methodName = methodSymbol.Name;
                    return new MethodData { className = className, methodName = methodName };
                })
            .Where(data => data != null)
            .Collect();

        context.RegisterSourceOutput(initializeSymbols.Combine(shutdownSymbols), GenerateEntryPoint);
    }

    private void GenerateEntryPoint(SourceProductionContext context, (ImmutableArray<MethodData> Left, ImmutableArray<MethodData> Right) tuple)
    {
        var initializeSb = new StringBuilder();
        var shutdownSb = new StringBuilder();

        var initializeArray = tuple.Left;
        var shutdownArray = tuple.Right;

        foreach (var info in initializeArray)
        {
            initializeSb.AppendLine($"            {info.className}.{info.methodName}(engineCore);");
        }

        foreach (var info in shutdownArray)
        {
            shutdownSb.AppendLine($"                {info.className}.{info.methodName}(engineCore);");
        }

        var entrySource = @$"
internal class Program
{{
    private static void Main(string[] args)
    {{
        global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Initialize(global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManagerDesc.Default);

        if (!global::SDL.SDL3.SDL_Init(global::SDL.SDL_InitFlags.SDL_INIT_VIDEO))
        {{
            global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Dispose();
            var errorMessage = global::SDL.SDL3.SDL_GetError();
            throw new global::System.Exception($""Failed to initialize SDL{{errorMessage}}"");
        }}

        try
        {{
            using var engineCore = new global::Ghost.Engine.EngineCore(new global::Ghost.Engine.Streaming.RuntimeContentProvider());

{initializeSb}
            try
            {{
                var windowDesc = new global::Ghost.Engine.WindowDesc
                {{
                    Width = 800,
                    Height = 600,
                    Title = ""Ghost Engine""
                }};

                using var window = new global::Ghost.Engine.EngineWindow(engineCore.RenderEngine.SwapChainManager, windowDesc);

                engineCore.Start();

                while (window.IsRunning)
                {{
                    window.PollEvents();

                    engineCore.Tick();
                }}
            }}
            finally
            {{
{shutdownSb}
            }}
        }}
        finally
        {{
            global::SDL.SDL3.SDL_Quit();
            global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Dispose();
        }}
    }}
}}";

        context.AddSource("EntryPoint.g.cs", entrySource);
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Ghost.Generator;

[Generator]
internal class EntryPointGenerator : IIncrementalGenerator
{
    private class MethodData
    {
        public string className;
        public string methodName;
        public IMethodSymbol methodSymbol;
    }

    private class ErrorMethodData : MethodData
    {
        public string errorMessage;
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

                    if (!methodSymbol.IsStatic || !methodSymbol.ReturnsVoid || (methodSymbol.Parameters.Length != 1 && methodSymbol.Parameters[0].Name != "EngineCore"))
                    {
                        return new ErrorMethodData { methodSymbol = methodSymbol, errorMessage = @"Invalid method signature. The methods with <see cref=""Ghost.Engine.RuntimeInitializeAttribute""/> must return void and have only one parameter with type <see cref=""Ghost.Engine.EngineCore""/>" };
                    }

                    var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var methodName = methodSymbol.Name;
                    return new MethodData { className = className, methodName = methodName, methodSymbol = methodSymbol };
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

                    if (!methodSymbol.IsStatic || !methodSymbol.ReturnsVoid || (methodSymbol.Parameters.Length != 1 && methodSymbol.Parameters[0].Name != "EngineCore"))
                    {
                        return new ErrorMethodData { methodSymbol = methodSymbol, errorMessage = @"Invalid method signature. The methods with <see cref=""Ghost.Engine.RuntimeShutdownAttribute""/> must return void and have only one parameter with type <see cref=""Ghost.Engine.EngineCore""/>" };
                    }

                    var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var methodName = methodSymbol.Name;
                    return new MethodData { className = className, methodName = methodName, methodSymbol = methodSymbol };
                })
            .Where(data => data != null)
            .Collect();

        var configSymbols = context.SyntaxProvider
            .ForAttributeWithMetadataName("Ghost.Engine.RuntimeConfigurationAttribute",
            (n, ct) => n is MethodDeclarationSyntax,
            (ctx, ct) =>
            {
                var methodSymbol = (IMethodSymbol)ctx.TargetSymbol;

                if (!methodSymbol.IsStatic || (methodSymbol.Parameters.Length != 0 && methodSymbol.ReturnType.Name != "EngineDesc"))
                {
                    return new ErrorMethodData { methodSymbol = methodSymbol, errorMessage = @"Invalid method signature. The methods with <see cref=""Ghost.Engine.RuntimeConfigurationAttribute""/> must return <see cref=""Ghost.Engine.EngineDesc""/> and have no parameter" };
                }

                var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var methodName = methodSymbol.Name;
                return new MethodData { className = className, methodName = methodName, methodSymbol = methodSymbol };
            })
            .Where(data => data != null)
            .Collect();

        context.RegisterSourceOutput(initializeSymbols.Combine(shutdownSymbols).Combine(configSymbols), GenerateEntryPoint);
    }

    private void GenerateEntryPoint(SourceProductionContext context, ((ImmutableArray<MethodData> Init, ImmutableArray<MethodData> Shutdown) Left, ImmutableArray<MethodData> Config) tuple)
    {
        var initializeSb = new StringBuilder();
        var shutdownSb = new StringBuilder();

        var initializeArray = tuple.Left.Init;
        var shutdownArray = tuple.Left.Shutdown;
        var configArray = tuple.Config;

        var foundConfig = false;
        var configCode = "global::Ghost.Engine.EngineDesc.GetDefault()";
        foreach (var info in configArray)
        {
            if (info is ErrorMethodData error)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "GHOST001",
                    "Invalid method signature",
                    error.errorMessage,
                    "Ghost.Generator",
                    DiagnosticSeverity.Error,
                    true), info.methodSymbol.Locations.FirstOrDefault()));
            }
            else
            {
                if (foundConfig)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        "GHOST002",
                        "Multiple configuration methods found",
                        "Only one method with <see cref=\"Ghost.Engine.RuntimeConfigurationAttribute\"/> is allowed.",
                        "Ghost.Generator",
                        DiagnosticSeverity.Error,
                        true), info.methodSymbol.Locations.FirstOrDefault()));
                }

                foundConfig = true;
                configCode = $"{info.className}.{info.methodName}()";
            }
        }

        foreach (var info in initializeArray)
        {
            if (info is ErrorMethodData error)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "GHOST001",
                    "Invalid method signature",
                    error.errorMessage,
                    "Ghost.Generator",
                    DiagnosticSeverity.Error,
                    true), info.methodSymbol.Locations.FirstOrDefault()));
            }

            initializeSb.AppendLine($"            {info.className}.{info.methodName}(engineCore);");
        }

        foreach (var info in shutdownArray)
        {
            if (info is ErrorMethodData error)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "GHOST001",
                    "Invalid method signature",
                    error.errorMessage,
                    "Ghost.Generator",
                    DiagnosticSeverity.Error,
                    true), info.methodSymbol.Locations.FirstOrDefault()));
            }

            shutdownSb.AppendLine($"                {info.className}.{info.methodName}(engineCore);");
        }

        var entrySource = @$"
internal class Program
{{
    private static void Main(string[] args)
    {{
        var engineDesc = {configCode};

        global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Initialize(engineDesc.AllocationManagerDesc);

        if (!global::SDL.SDL3.SDL_Init(global::SDL.SDL_InitFlags.SDL_INIT_VIDEO))
        {{
            global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Dispose();
            var errorMessage = global::SDL.SDL3.SDL_GetError();
            throw new global::System.Exception($""Failed to initialize SDL{{errorMessage}}"");
        }}

        try
        {{
            using var engineCore = new global::Ghost.Engine.EngineCore(engineDesc.JobSchedulerDesc, engineDesc.RenderDesc, engineDesc.ContentProvider);

{initializeSb}
            try
            {{
                using var window = new global::Ghost.Engine.EngineWindow(engineCore.RenderEngine.SwapChainManager, engineDesc.WindowDesc);

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
        // TODO: Log the exception
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
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
        public string ClassName
        {
            get;
        }

        public string MethodName
        {
            get;
        }

        public IMethodSymbol MethodSymbol
        {
            get;
        }


        public MethodData(string className, string methodName, IMethodSymbol methodSymbol)
        {
            ClassName = className;
            MethodName = methodName;
            MethodSymbol = methodSymbol;
        }
    }

    private class ErrorMethodData : MethodData
    {
        public string ErrorMessage
        {
            get;
        }

        public ErrorMethodData(IMethodSymbol methodSymbol, string errorMessage)
            : base(methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), methodSymbol.Name, methodSymbol)
        {
            this.ErrorMessage = errorMessage;
        }
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
                        return new ErrorMethodData(methodSymbol, @"Invalid method signature. The methods with <see cref=""Ghost.Engine.RuntimeInitializeAttribute""/> must return void and have only one parameter with type <see cref=""Ghost.Engine.EngineCore""/>");
                    }

                    var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var methodName = methodSymbol.Name;
                    return new MethodData(className, methodName, methodSymbol);
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
                        return new ErrorMethodData(methodSymbol, @"Invalid method signature. The methods with <see cref=""Ghost.Engine.RuntimeShutdownAttribute""/> must return void and have only one parameter with type <see cref=""Ghost.Engine.EngineCore""/>");
                    }

                    var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var methodName = methodSymbol.Name;
                    return new MethodData(className, methodName, methodSymbol);
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
                    return new ErrorMethodData(methodSymbol, @"Invalid method signature. The methods with <see cref=""Ghost.Engine.RuntimeConfigurationAttribute""/> must return <see cref=""Ghost.Engine.EngineDesc""/> and have no parameter");
                }

                var className = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var methodName = methodSymbol.Name;
                return new MethodData(className, methodName, methodSymbol);
            })
            .Where(data => data != null)
            .Collect();

        context.RegisterSourceOutput(initializeSymbols.Combine(shutdownSymbols).Combine(configSymbols), GenerateEntryPoint);
    }

    private void GenerateEntryPoint(SourceProductionContext context, ((ImmutableArray<MethodData> Init, ImmutableArray<MethodData> Shutdown) Left, ImmutableArray<MethodData> Config) tuple)
    {
        var initializeArray = tuple.Left.Init;
        var shutdownArray = tuple.Left.Shutdown;
        var configArray = tuple.Config;

        if (initializeArray.IsEmpty && shutdownArray.IsEmpty && configArray.IsEmpty)
        {
            return;
        }

        var configCall = GetConfigCall(context, configArray);
        var initCall = GetInitCall(context, initializeArray);
        var shutdownCall = GetShutdownCall(context, shutdownArray);

        var entrySource = @$"
internal class Program
{{
    private static void Main(string[] args)
    {{
        var engineDesc = {configCall};
        global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Initialize(engineDesc.AllocationManagerDesc);

        if (!global::SDL.SDL3.SDL_Init(global::SDL.SDL_InitFlags.SDL_INIT_VIDEO))
        {{
            global::Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Dispose();
            var errorMessage = global::SDL.SDL3.SDL_GetError();
            throw new global::System.Exception($""Failed to initialize SDL. {{errorMessage}}"");
        }}

        try
        {{
            using var engineCore = new global::Ghost.Engine.EngineCore(engineDesc.JobSchedulerDesc, engineDesc.RenderDescFactory(), engineDesc.ContentProviderFactory());

{initCall}
            try
            {{
                using var window = new global::Ghost.Engine.EngineWindow(engineCore.RenderEngine, engineDesc.WindowDesc);

                engineCore.Start();

                while (window.IsRunning)
                {{
                    window.PollEvents();

                    engineCore.Tick();
                }}

                engineCore.Stop();
            }}
            finally
            {{
{shutdownCall}
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

    private static string GetShutdownCall(SourceProductionContext context, ImmutableArray<MethodData> shutdownArray)
    {
        var sb = new StringBuilder();
        foreach (var info in shutdownArray)
        {
            if (info is ErrorMethodData error)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "GHOST001",
                    "Invalid method signature",
                    error.ErrorMessage,
                    "Ghost.Generator",
                    DiagnosticSeverity.Error,
                    true), info.MethodSymbol.Locations.FirstOrDefault()));
            }

            sb.AppendLine($"                {info.ClassName}.{info.MethodName}(engineCore);");
        }

        return sb.ToString();
    }

    private static string GetInitCall(SourceProductionContext context, ImmutableArray<MethodData> initializeArray)
    {
        var sb = new StringBuilder();

        foreach (var info in initializeArray)
        {
            if (info is ErrorMethodData error)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    "GHOST001",
                    "Invalid method signature",
                    error.ErrorMessage,
                    "Ghost.Generator",
                    DiagnosticSeverity.Error,
                    true), info.MethodSymbol.Locations.FirstOrDefault()));
            }

            sb.AppendLine($"            {info.ClassName}.{info.MethodName}(engineCore);");
        }

        return sb.ToString();
    }

    private static string GetConfigCall(SourceProductionContext context, ImmutableArray<MethodData> configArray)
    {
        if (configArray.IsEmpty)
        {

            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                "GHOST003",
                "Engine configureation not found.",
                "Need exactly one method marked with RuntimeConfigurationAttribute",
                "Ghost.Generator",
                DiagnosticSeverity.Error,
                true), null));

            return string.Empty;
        }
        else
        {
            var foundConfig = false;
            var configCode = string.Empty;

            foreach (var info in configArray)
            {
                if (info is ErrorMethodData error)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        "GHOST001",
                        "Invalid method signature",
                        error.ErrorMessage,
                        "Ghost.Generator",
                        DiagnosticSeverity.Error,
                        true), info.MethodSymbol.Locations.FirstOrDefault()));
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
                            true), info.MethodSymbol.Locations.FirstOrDefault()));
                    }
                    else
                    {
                        foundConfig = true;
                        configCode = $"{info.ClassName}.{info.MethodName}()";
                    }
                }
            }

            return configCode;
        }
    }
}

using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Editor.Core.Services;

[EditorInjection(EditorInjectionAttribute.ServiceLifetime.Singleton, typeof(IShaderCompilationBridge))]
internal sealed class EditorShaderCompilerBridge : IShaderCompilationBridge, IDisposable
{
    private readonly IAssetRegistry _assetRegistry;
    private readonly IShaderCompiler _compiler;
    private readonly ConcurrentDictionary<ulong, Guid> _shaderIdToAssetId = new();
    private readonly IServiceProvider _serviceProvider;

    public event Action<Key64<ShaderVariant>, ulong>? OnShaderVariantCompiled;

    public EditorShaderCompilerBridge(IAssetRegistry assetRegistry, IServiceProvider serviceProvider)
    {
        _assetRegistry = assetRegistry;
        _serviceProvider = serviceProvider;
        _compiler = new DXCShaderCompiler();
        
        _assetRegistry.OnAssetImported += OnAssetImported;
    }

    private void OnAssetImported(object? sender, Guid guid)
    {
        var path = _assetRegistry.GetAssetPath(guid);
        if (path != null && (path.EndsWith(".gshdr") || path.EndsWith(".gcomp")))
        {
            var result = _assetRegistry.LoadAssetAsync(guid).AsTask().Result;
            if (result.IsSuccess)
            {
                ulong nameHash = 0;
                if (result.Value is GraphicsShaderAsset graphicsAsset)
                {
                    nameHash = RHIUtility.GetShaderID(graphicsAsset.Descriptor.Name);
                }
                else if (result.Value is ComputeShaderAsset computeAsset)
                {
                    nameHash = RHIUtility.GetShaderID(computeAsset.Descriptor.Name);
                }

                if (nameHash != 0)
                {
                    _shaderIdToAssetId[nameHash] = guid;
                    
                    var engineCore = _serviceProvider.GetService<EngineCore>();
                    if (engineCore != null)
                    {
                        var shaderLibrary = engineCore.RenderSystem.ShaderLibrary;
                        var pipelineLibrary = engineCore.RenderSystem.GraphicsEngine.PipelineLibrary;
                        shaderLibrary.InvalidateShaderCache(nameHash, pipelineLibrary);
                    }
                }
            }
        }
    }

    public void RequestCompilation(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey)
    {
        Task.Run(async () =>
        {
            if (!_shaderIdToAssetId.TryGetValue(shaderId, out var guid))
            {
                var catalog = _assetRegistry.GetAssetCatalog();
                foreach (var (assetGuid, path) in catalog.EnumerateAll())
                {
                    if (path.EndsWith(".gshdr") || path.EndsWith(".gcomp"))
                    {
                        var result = await _assetRegistry.LoadAssetAsync(assetGuid);
                        if (result.IsSuccess)
                        {
                            ulong nameHash = 0;
                            if (result.Value is GraphicsShaderAsset graphicsAsset)
                            {
                                nameHash = RHIUtility.GetShaderID(graphicsAsset.Descriptor.Name);
                            }
                            else if (result.Value is ComputeShaderAsset computeAsset)
                            {
                                nameHash = RHIUtility.GetShaderID(computeAsset.Descriptor.Name);
                            }
                            if (nameHash != 0)
                            {
                                _shaderIdToAssetId[nameHash] = assetGuid;
                            }
                        }
                    }
                }
            }

            if (_shaderIdToAssetId.TryGetValue(shaderId, out var assetId))
            {
                var assetResult = await _assetRegistry.LoadAssetAsync(assetId);
                if (assetResult.IsSuccess)
                {
                    if (assetResult.Value is GraphicsShaderAsset graphicsAsset)
                    {
                        var pass = graphicsAsset.Descriptor.Passes[passIndex];
                        await CompileGraphicsPassAsync(shaderId, passIndex, variantKey, pass);
                    }
                    else if (assetResult.Value is ComputeShaderAsset computeAsset)
                    {
                        var code = computeAsset.Descriptor.ShaderCodes[passIndex];
                        await CompileComputePassAsync(shaderId, passIndex, variantKey, code);
                    }
                }
            }
        });
    }

    private unsafe Task CompileGraphicsPassAsync(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey, PassDescriptor pass)
    {
        // For simplicity, just compile the pixel shader. A real implementation would compile
        // all stages (Mesh/Amp/Vertex/Pixel) defined in the pass descriptor.
        var config = new ShaderCompilationConfig
        {
            shaderCode = pass.pixelShaderCode.code,
            entryPoint = pass.pixelShaderCode.entryPoint,
            stage = ShaderStage.PixelShader,
            defines = pass.defines,
            model = ShaderModel.SM_6_6
        };

        var compileResult = _compiler.Compile(in config, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        if (compileResult.IsSuccess)
        {
            var engineCore = _serviceProvider.GetService<EngineCore>();
            if (engineCore != null)
            {
                using var bytecodeArray = compileResult.Value;
                
                var byteCode = new ShaderByteCode
                {
                    pCode = (byte*)bytecodeArray.GetUnsafePtr(),
                    size = (ulong)bytecodeArray.Length
                };

                // Assume 1 stage for now. In reality, we'd pass an array of ShaderByteCode for all stages.
                var byteCodes = new Span<ShaderByteCode>(ref byteCode);

                engineCore.RenderSystem.ShaderLibrary.CacheCompiledResult(shaderId, passIndex, variantKey, byteCodes);
                
                // Get the generated hash to fire the event
                var dataSpan = new ReadOnlySpan<byte>(byteCode.pCode, (int)byteCode.size);
                var hash = System.IO.Hashing.XxHash64.HashToUInt64(dataSpan);
                OnShaderVariantCompiled?.Invoke(variantKey, hash);
            }
        }
        else
        {
            Ghost.Core.Logger.Error($"Failed to compile graphics shader {shaderId}: {compileResult.Message}");
        }

        return Task.CompletedTask;
    }

    private unsafe Task CompileComputePassAsync(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey, ShaderCode code)
    {
        var config = new ShaderCompilationConfig
        {
            shaderCode = code.code,
            entryPoint = code.entryPoint,
            stage = ShaderStage.ComputeShader,
            defines = Array.Empty<string>(),
            model = ShaderModel.SM_6_6
        };

        var compileResult = _compiler.Compile(in config, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        if (compileResult.IsSuccess)
        {
            var engineCore = _serviceProvider.GetService<EngineCore>();
            if (engineCore != null)
            {
                using var bytecodeArray = compileResult.Value;
                
                var byteCode = new ShaderByteCode
                {
                    pCode = (byte*)bytecodeArray.GetUnsafePtr(),
                    size = (ulong)bytecodeArray.Length
                };

                var byteCodes = new Span<ShaderByteCode>(ref byteCode);

                engineCore.RenderSystem.ShaderLibrary.CacheCompiledResult(shaderId, passIndex, variantKey, byteCodes);
                
                var dataSpan = new ReadOnlySpan<byte>(byteCode.pCode, (int)byteCode.size);
                var hash = System.IO.Hashing.XxHash64.HashToUInt64(dataSpan);
                OnShaderVariantCompiled?.Invoke(variantKey, hash);
            }
        }
        else
        {
            Ghost.Core.Logger.Error($"Failed to compile compute shader {shaderId}: {compileResult.Message}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _assetRegistry.OnAssetImported -= OnAssetImported;
    }
}

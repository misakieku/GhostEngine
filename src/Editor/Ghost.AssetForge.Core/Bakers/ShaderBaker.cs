using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ghost.AssetForge.Core.Contracts;
using Ghost.Core.Utilities;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.DSL.Composition;
using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.ShaderParser.Syntax;
using Ghost.DSL.Symbols;
using Ghost.DSL.Syntax.Symbols;
using Ghost.DXC;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
namespace Ghost.AssetForge.Core.Bakers;

public partial class ShaderBakeSettings : ObservableObject, IBakeSettings
{
    [ObservableProperty]
    private CompilerOptimizeLevel _optimizeLevel = CompilerOptimizeLevel.O3;

    [ObservableProperty]
    private CompilerOption _options = CompilerOption.None;
}

[AssetBaker(Extensions = [".gshdr", ".gcomp"], Type = AssetType.Shader, SettingsType = typeof(ShaderBakeSettings))]
internal partial class ShaderBaker : IAssetBaker, IDisposable
{
    private readonly DXCShaderCompiler _compiler = new DXCShaderCompiler();
    private readonly SemaphoreSlim _compileLock = new(1, 1);

    private static async Task<ulong> WriteShaderEntries(
        Stream stream,
        long variantDataOffset,
        IEnumerable<KeyValuePair<ulong, ulong>>? bindings,
        CancellationToken cancellationToken,
        params (ShaderStage stage, UnsafeArray<byte> bytecode)[] entries)
    {
        if (bindings != null)
        {
            foreach (var kvp in bindings)
            {
                stream.Write(new ShaderContentHeader.BindingRecord
                {
                    interfaceId = kvp.Key,
                    implementationId = kvp.Value
                });
            }
        }

        var baseByteCodeOffset = (stream.Position - variantDataOffset) + (entries.Length * Unsafe.SizeOf<ShaderContentHeader.EntryPointHeader>());

        for (var i = 0; i < entries.Length; i++)
        {
            var (stage, bytecode) = entries[i];
            var byteCodeOffset = baseByteCodeOffset;
            for (var j = 0; j < i; j++)
            {
                byteCodeOffset += entries[j].bytecode.Length;
            }

            var entryPointHeader = new ShaderContentHeader.EntryPointHeader
            {
                stage = stage,
                byteCodeSize = bytecode.Length,
                byteCodeOffset = byteCodeOffset
            };

            stream.Write(entryPointHeader);
        }

        ulong contentHash = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        for (var i = 0; i < entries.Length; i++)
        {
            var bytecode = entries[i].bytecode;
            if (!bytecode.IsCreated)
            {
                continue;
            }

            using var memoryManager = NativeMemoryManager<byte>.FromUnsafeCollection(in bytecode);
            var mem = memoryManager.Memory;
            var span = mem.Span;
            for (int k = 0; k < span.Length; k++)
            {
                contentHash ^= span[k];
                contentHash *= prime;
            }
            await stream.WriteAsync(mem, cancellationToken).ConfigureAwait(false);
        }

        return contentHash;
    }

    private static async Task<ulong> WriteCachedShaderEntries(
        Stream stream,
        long variantDataOffset,
        IEnumerable<KeyValuePair<ulong, ulong>>? bindings,
        CancellationToken cancellationToken,
        params (ShaderStage stage, byte[] bytecode)[] entries)
    {
        if (bindings != null)
        {
            foreach (var kvp in bindings)
            {
                stream.Write(new ShaderContentHeader.BindingRecord
                {
                    interfaceId = kvp.Key,
                    implementationId = kvp.Value
                });
            }
        }

        var baseByteCodeOffset = (stream.Position - variantDataOffset) + (entries.Length * Unsafe.SizeOf<ShaderContentHeader.EntryPointHeader>());

        for (var i = 0; i < entries.Length; i++)
        {
            var (stage, bytecode) = entries[i];
            var byteCodeOffset = baseByteCodeOffset;
            for (var j = 0; j < i; j++)
            {
                byteCodeOffset += entries[j].bytecode.Length;
            }

            var entryPointHeader = new ShaderContentHeader.EntryPointHeader
            {
                stage = stage,
                byteCodeSize = bytecode.Length,
                byteCodeOffset = byteCodeOffset
            };

            stream.Write(entryPointHeader);
        }

        ulong contentHash = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        for (var i = 0; i < entries.Length; i++)
        {
            var bytecode = entries[i].bytecode;
            for (int k = 0; k < bytecode.Length; k++)
            {
                contentHash ^= bytecode[k];
                contentHash *= prime;
            }
            await stream.WriteAsync(bytecode.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        return contentHash;
    }

    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        // DXCShaderCompiler is a native handle and is not thread-safe. Serialize
        // concurrent shader bakes through the lock; textures keep running in parallel.
        await _compileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await BakeAssetCoreAsync(src, dst, settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _compileLock.Release();
        }
    }

    private async Task BakeAssetCoreAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        var bakeSettings = (ShaderBakeSettings)settings;
        var ext = Path.GetExtension(src).ToLowerInvariant();

        var configTemplate = new ShaderCompilationConfig
        {
            optimizeLevel = bakeSettings.OptimizeLevel,
            options = bakeSettings.Options,
            includeDirectories = ctx.AssetDirectories,
        };

        var codeBytes = await File.ReadAllBytesAsync(src, cancellationToken).ConfigureAwait(false);
        var codeStr = Encoding.UTF8.GetString(codeBytes);

        if (string.Equals(ext, ".gshdr", StringComparison.Ordinal))
        {
            // 1. Ingest into Workspace if not pre-populated
            var workspace = ctx.ShaderWorkspace;
            if (workspace == null)
            {
                workspace = new ShaderWorkspace();
                var doc = DSLShaderCompiler.ParseDSLDocument(codeStr).GetValueOrThrow();
                workspace.IndexDocument(src, doc);
                workspace.ResolveAndValidate().ThrowIfFailed();
            }

            // 2. Identify Target Shader in Workspace
            var shaderSymbol = workspace.Shaders.Values.FirstOrDefault(s => s.SourceFile == src)
                            ?? workspace.Shaders.Values.FirstOrDefault()
                            ?? throw new InvalidOperationException($"No shader declaration found in '{src}'.");

            // 3. Resolve Pass Specializations and Composition Matrix
            var composition = workspace.ResolveShaderComposition(shaderSymbol).GetValueOrThrow();

            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Graphics,
                passCount = (uint)composition.Passes.Count,
                shaderId = shaderSymbol.Id,
                schemaId = shaderSymbol.PropertySchema?.SchemaId ?? 0,
                propertyBufferSize = shaderSymbol.PropertySchema?.TotalSize ?? 0,
                reserved = 0
            };

            var assetStartOffset = dst.Position;
            dst.Write(header);

            // Reflection property data
            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(shaderSymbol.QualifiedName, new DSL.Models.ShaderReflectionData());

            for (var passIdx = 0; passIdx < composition.Passes.Count; passIdx++)
            {
                var passSet = composition.Passes[passIdx];
                var entryCount = passSet.Syntax.ShaderEntries.Count > 0 ? passSet.Syntax.ShaderEntries.Count : 1;

                var passHeader = new ShaderContentHeader.PassHeader
                {
                    passId = SymbolId.Compute($"{shaderSymbol.QualifiedName}.{passSet.PassName}"),
                    entryPointCount = (uint)entryCount,
                    variantCount = (uint)passSet.Specializations.Count,
                    isTemplateShared = passSet.IsTemplateShared ? 1u : 0u,
                    templatePassId = passSet.TemplatePassId ?? 0ul
                };
                dst.Write(passHeader);

                var variantEntriesOffset = dst.Position;
                var variantEntries = new ShaderContentHeader.VariantEntry[passSet.Specializations.Count];
                for (var i = 0; i < passSet.Specializations.Count; i++)
                {
                    dst.Write(variantEntries[i]); // Placeholder
                }

                // Check Template Pass Bytecode Cache for shared passes (e.g. DepthOnly)
                if (passSet.IsTemplateShared && passSet.TemplatePassId.HasValue && ctx.SharedPassBytecodeCache != null)
                {
                    if (ctx.SharedPassBytecodeCache.TryGetValue(passSet.TemplatePassId.Value, out var cachedEntries))
                    {
                        var variantDataStart = dst.Position;
                        var contentHash = await WriteCachedShaderEntries(dst, variantDataStart, null, cancellationToken, cachedEntries).ConfigureAwait(false);

                        variantEntries[0] = new ShaderContentHeader.VariantEntry
                        {
                            variantKey = 0,
                            programContentHash = contentHash,
                            dataOffset = variantDataStart - assetStartOffset,
                            dataSize = dst.Position - variantDataStart,
                            bindingCount = 0,
                            reserved = 0
                        };

                        var endOfCachedPass = dst.Position;
                        dst.Position = variantEntriesOffset;
                        dst.Write(variantEntries[0]);
                        dst.Position = endOfCachedPass;
                        continue;
                    }
                }

                var cachedEntriesToSave = new List<(ShaderStage stage, byte[] bytecode)>();
                var propertiesHlsl = shaderSymbol.PropertySchema?.GenerateHlslStruct("MaterialProperties") ?? reflectionData.Code;

                for (var i = 0; i < passSet.Specializations.Count; i++)
                {
                    var spec = passSet.Specializations[i];
                    var variantDataStart = dst.Position;

                    var compiledEntries = new List<(ShaderStage stage, UnsafeArray<byte> bytecode)>();
                    ulong contentHash = 0;

                    try
                    {
                        if (passSet.Syntax.ShaderEntries.Count > 0)
                        {
                            foreach (var entry in passSet.Syntax.ShaderEntries)
                            {
                                var stage = ParseShaderStage(entry.EntryType);
                                var hlslResult = HLSLCodeGenerator.GeneratePassHLSL(
                                    passSet.Syntax,
                                    spec,
                                    shaderSymbol.PayloadBody,
                                    propertiesHlsl,
                                    ctx.ShaderMetadata.VirtualShader,
                                    entry.ShaderPath,
                                    ctx.AssetDirectories);

                                var hlslCode = hlslResult.GetValueOrThrow();

                                var config = configTemplate with
                                {
                                    stage = stage,
                                    model = ShaderModel.SM_6_6,
                                    defines = spec.CompilerDefines.ToArray(),
                                    entryPoint = entry.EntryPoint,
                                    shaderCode = hlslCode,
                                };

                                var bytecode = _compiler.Compile(in config, AllocationHandle.Persistent).GetValueOrThrow();
                                compiledEntries.Add((stage, bytecode));

                                if (passSet.IsTemplateShared && i == 0)
                                {
                                    var copy = new byte[bytecode.Length];
                                    using var mem = NativeMemoryManager<byte>.FromUnsafeCollection(in bytecode);
                                    mem.Memory.Span.CopyTo(copy);
                                    cachedEntriesToSave.Add((stage, copy));
                                }
                            }
                        }
                        else
                        {
                            // Inline HLSL fallback
                            var hlslResult = HLSLCodeGenerator.GeneratePassHLSL(
                                passSet.Syntax,
                                spec,
                                shaderSymbol.PayloadBody,
                                propertiesHlsl,
                                ctx.ShaderMetadata.VirtualShader,
                                null,
                                ctx.AssetDirectories);

                            var hlslCode = hlslResult.GetValueOrThrow();

                            var config = configTemplate with
                            {
                                stage = ShaderStage.PixelShader,
                                model = ShaderModel.SM_6_6,
                                defines = spec.CompilerDefines.ToArray(),
                                entryPoint = "MainPS",
                                shaderCode = hlslCode,
                            };

                            var bytecode = _compiler.Compile(in config, AllocationHandle.Persistent).GetValueOrThrow();
                            compiledEntries.Add((ShaderStage.PixelShader, bytecode));

                            if (passSet.IsTemplateShared && i == 0)
                            {
                                var copy = new byte[bytecode.Length];
                                using var mem = NativeMemoryManager<byte>.FromUnsafeCollection(in bytecode);
                                mem.Memory.Span.CopyTo(copy);
                                cachedEntriesToSave.Add((ShaderStage.PixelShader, copy));
                            }
                        }

                        contentHash = await WriteShaderEntries(dst, variantDataStart, spec.Bindings, cancellationToken, compiledEntries.ToArray()).ConfigureAwait(false);
                    }
                    finally
                    {
                        foreach (var (_, bc) in compiledEntries)
                        {
                            bc.Dispose();
                        }
                    }

                    variantEntries[i] = new ShaderContentHeader.VariantEntry
                    {
                        variantKey = spec.CompositionKey,
                        programContentHash = contentHash,
                        dataOffset = variantDataStart - assetStartOffset,
                        dataSize = dst.Position - variantDataStart,
                        bindingCount = (uint)spec.Bindings.Count,
                        reserved = 0
                    };
                }

                if (passSet.IsTemplateShared && passSet.TemplatePassId.HasValue && ctx.SharedPassBytecodeCache != null && cachedEntriesToSave.Count > 0)
                {
                    ctx.SharedPassBytecodeCache[passSet.TemplatePassId.Value] = cachedEntriesToSave.ToArray();
                }

                var endOfPass = dst.Position;
                dst.Position = variantEntriesOffset;
                foreach (var entry in variantEntries)
                {
                    dst.Write(entry);
                }
                dst.Position = endOfPass;
            }
        }
        else if (string.Equals(ext, ".gcomp", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseComputeShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShaderMetadata.VirtualShader).GetValueOrThrow();

            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Compute,
                passCount = 1,
                shaderId = SymbolId.Compute(semantics.name),
                schemaId = 0,
                propertyBufferSize = descriptor.PropertyBufferSize,
                reserved = 0
            };

            var assetStartOffset = dst.Position;
            dst.Write(header);

            var passHeader = new ShaderContentHeader.PassHeader
            {
                passId = SymbolId.Compute($"{semantics.name}.Main"),
                entryPointCount = (uint)descriptor.ShaderCodes.Length,
                variantCount = 1,
                isTemplateShared = 0,
                templatePassId = 0
            };
            dst.Write(passHeader);

            var variantEntriesOffset = dst.Position;
            var variantEntry = new ShaderContentHeader.VariantEntry();
            dst.Write(variantEntry); // Placeholder

            var variantDataStart = dst.Position;
            var byteCodes = new UnsafeArray<byte>[descriptor.ShaderCodes.Length];

            try
            {
                for (var j = 0; j < descriptor.ShaderCodes.Length; j++)
                {
                    var shaderCode = descriptor.ShaderCodes[j];
                    var config = configTemplate with
                    {
                        stage = ShaderStage.ComputeShader,
                        model = descriptor.ShaderModel,
                        defines = descriptor.Defines.ToArray(),
                        entryPoint = shaderCode.entryPoint,
                        shaderCode = shaderCode.code,
                    };

                    byteCodes[j] = _compiler.Compile(in config, AllocationHandle.Persistent).GetValueOrThrow();
                }

                var entries = byteCodes.Select((bc, index) => (ShaderStage.ComputeShader, bc)).ToArray();
                var contentHash = await WriteShaderEntries(dst, variantDataStart, null, cancellationToken, entries).ConfigureAwait(false);

                variantEntry = new ShaderContentHeader.VariantEntry
                {
                    variantKey = 0,
                    programContentHash = contentHash,
                    dataOffset = variantDataStart - assetStartOffset,
                    dataSize = dst.Position - variantDataStart,
                    bindingCount = 0,
                    reserved = 0
                };
            }
            finally
            {
                foreach (var code in byteCodes)
                {
                    code.Dispose();
                }
            }

            var endOfPass = dst.Position;
            dst.Position = variantEntriesOffset;
            dst.Write(variantEntry);
            dst.Position = endOfPass;
        }
        else
        {
            throw new NotSupportedException($"Unsupported shader file extension: {ext}");
        }
    }

    private static ShaderStage ParseShaderStage(string entryType) =>
        entryType.ToLowerInvariant() switch
        {
            "mesh" or "ms" or "meshshader" => ShaderStage.MeshShader,
            "amplification" or "as" or "task" or "amplificationshader" => ShaderStage.AmplificationShader,
            "pixel" or "ps" or "pixelshader" => ShaderStage.PixelShader,
            "compute" or "cs" or "computeshader" => ShaderStage.ComputeShader,
            _ => ShaderStage.PixelShader,
        };

    public void Dispose()
    {
        _compiler.Dispose();
        _compileLock.Dispose();
    }
}

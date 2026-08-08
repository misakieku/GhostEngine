using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.DSL.ShaderCompiler;
using Ghost.DXC;
using Ghost.AssetForge.Core.Services;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.AssetForge.Core.Bakers;

public partial class ShaderBakeSettings : ObservableObject, IBakeSettings
{
    [ObservableProperty]
    public partial CompilerOptimizeLevel OptimizeLevel
    {
        get; set;
    } = CompilerOptimizeLevel.O3;

    [ObservableProperty]
    public partial CompilerOption Options
    {
        get; set;
    } = CompilerOption.None;
}

[AssetBaker(Extensions = [".gshdr", ".gcomp"], Type = AssetType.Shader, SettingsType = typeof(ShaderBakeSettings))]
internal partial class ShaderBaker : IAssetBaker, IDisposable
{
    private readonly DXCShaderCompiler _compiler = new DXCShaderCompiler();
    private readonly SemaphoreSlim _compileLock = new(1, 1);

    private static async Task WriteShaderEntries(Stream stream, long variantDataOffset, CancellationToken cancellationToken, params (ShaderStage stage, UnsafeArray<byte> bytecode)[] entries)
    {
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

        for (var i = 0; i < entries.Length; i++)
        {
            var bytecode = entries[i].bytecode;
            if (!bytecode.IsCreated)
            {
                continue;
            }

            using var memory = NativeMemoryManager<byte>.FromUnsafeCollection(in bytecode);
            await stream.WriteAsync(memory.Memory, cancellationToken).ConfigureAwait(false);
        }
    }

    private static ulong ComputeVariantKey(List<string> activeKeywords, List<string> allKeywords)
    {
        uint[] data = new uint[4];
        foreach (var active in activeKeywords)
        {
            var localIndex = allKeywords.IndexOf(active);
            if (localIndex < 0) continue;
            var index = localIndex / 32;
            var bit = localIndex % 32;
            data[index] |= (uint)(1 << bit);
        }

        var hash = 14695981039346656037ul; // FNV Offset basis

        for (var i = 0; i < 4; i++)
        {
            hash ^= data[i];
            hash *= 1099511628211ul; // FNV prime
        }

        return hash;
    }

    private static List<List<string>> GenerateVariantCombinations(KeywordsGroup[] groups)
    {
        var combinations = new List<List<string>>();
        var current = new string[groups.Length];
        
        void Backtrack(int groupIndex)
        {
            if (groupIndex == groups.Length)
            {
                combinations.Add(current.Where(k => !string.IsNullOrEmpty(k)).ToList());
                return;
            }

            var group = groups[groupIndex];
            if (group.keywords == null || group.keywords.Count == 0)
            {
                Backtrack(groupIndex + 1);
            }
            else
            {
                foreach (var kw in group.keywords)
                {
                    current[groupIndex] = kw;
                    Backtrack(groupIndex + 1);
                }
            }
        }

        Backtrack(0);
        
        if (combinations.Count == 0)
        {
            combinations.Add(new List<string>());
        }

        return combinations;
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
        if (settings is not ShaderBakeSettings shaderSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected ShaderBakeSettings.", nameof(settings));
        }

        var codeStr = await File.ReadAllTextAsync(src, cancellationToken).ConfigureAwait(false);
        var ext = Path.GetExtension(src);

        var configTemplate = new ShaderCompilationConfig
        {
            optimizeLevel = shaderSettings.OptimizeLevel,
            options = shaderSettings.Options,
            includeDirectories = ctx.AssetDirectories.ToArray(),
        };

        if (string.Equals(ext, ".gshdr", StringComparison.Ordinal))
        {
            var syntax = DSLShaderCompiler.ParseGraphicsShaderSyntax(codeStr).GetValueOrThrow();
            var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

            var reflectionData = ctx.ShaderMetadata.ReflectionDatas.GetValueOrDefault(semantics.name, new DSL.Models.ShaderReflectionData());
            var descriptor = DSLShaderCompiler.ResolveShader(semantics, reflectionData, ctx.ShaderMetadata.VirtualShader).GetValueOrThrow();

            var header = new ShaderContentHeader
            {
                shaderType = ShaderType.Graphics,
                passCount = (uint)descriptor.Passes.Length,
            };

            var assetStartOffset = dst.Position;
            dst.Write(header);

            // Calculate all unique keywords across all passes for string table
            var allKeywords = new List<string>();
            var stringTableBytes = new List<byte>();
            
            // Build string table
            var passGroupOffsets = new List<List<uint>>(); // Pass index -> Group Index -> string table offset
            foreach (var pass in descriptor.Passes)
            {
                var groupOffsets = new List<uint>();
                foreach (var group in pass.keywords)
                {
                    groupOffsets.Add((uint)stringTableBytes.Count);
                    if (group.keywords != null)
                    {
                        foreach (var kw in group.keywords)
                        {
                            if (!allKeywords.Contains(kw)) allKeywords.Add(kw);
                            stringTableBytes.AddRange(Encoding.UTF8.GetBytes(kw));
                            stringTableBytes.Add(0); // Null terminator
                        }
                    }
                }
                passGroupOffsets.Add(groupOffsets);
            }

            var stringTableOffset = (uint)(dst.Position - assetStartOffset);
            var stringTableSize = (uint)stringTableBytes.Count;
            
            // Update header
            var currentPos = dst.Position;
            dst.Position = assetStartOffset;
            header.keywordStringTableOffset = stringTableOffset;
            header.keywordStringTableSize = stringTableSize;
            dst.Write(header);
            dst.Position = currentPos;

            // Write String Table
            if (stringTableSize > 0)
            {
                dst.Write(BitConverter.GetBytes(stringTableSize));
                dst.Write(stringTableBytes.ToArray());
            }
            else
            {
                dst.Write(BitConverter.GetBytes((uint)0));
            }

            for (var passIdx = 0; passIdx < descriptor.Passes.Length; passIdx++)
            {
                var pass = descriptor.Passes[passIdx];
                var combinations = GenerateVariantCombinations(pass.keywords);
                var groupOffsets = passGroupOffsets[passIdx];

                var passHeader = new ShaderContentHeader.PassHeader
                {
                    entryPointCount = 3, // Amplification, Mesh, Pixel
                    variantCount = (uint)combinations.Count,
                    keywordGroupCount = (uint)pass.keywords.Length
                };
                dst.Write(passHeader);

                foreach (var t in groupOffsets.Select((offset, idx) => new ShaderContentHeader.KeywordGroupDescriptor
                {
                    stringTableOffset = offset,
                    keywordCount = (uint)(pass.keywords[idx].keywords?.Count ?? 0)
                }))
                {
                    dst.Write(t);
                }

                var variantEntriesOffset = dst.Position;
                var variantEntries = new ShaderContentHeader.VariantEntry[combinations.Count];
                for (var i = 0; i < combinations.Count; i++)
                {
                    dst.Write(variantEntries[i]); // Placeholder
                }

                for (var i = 0; i < combinations.Count; i++)
                {
                    var activeKeywords = combinations[i];
                    var variantDataStart = dst.Position;
                    
                    var variantDefines = pass.defines.ToList();
                    variantDefines.AddRange(activeKeywords);

                    var config = configTemplate with
                    {
                        stage = ShaderStage.AmplificationShader,
                        model = descriptor.ShaderModel,
                        defines = variantDefines.ToArray(),
                        entryPoint = pass.amplificationShaderCode.entryPoint,
                        shaderCode = pass.amplificationShaderCode.code,
                    };

                    if (!pass.meshShaderCode.IsCreated || !pass.pixelShaderCode.IsCreated)
                    {
                        throw new InvalidOperationException("Shader pass is missing required shader stages. Both mesh and pixel shaders must be present.");
                    }

                    using var asByteCode = pass.amplificationShaderCode.IsCreated ?
                        _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow()
                        : default;

                    config.stage = ShaderStage.MeshShader;
                    config.entryPoint = pass.meshShaderCode.entryPoint;
                    config.shaderCode = pass.meshShaderCode.code;

                    using var msByteCode = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();

                    config.stage = ShaderStage.PixelShader;
                    config.entryPoint = pass.pixelShaderCode.entryPoint;
                    config.shaderCode = pass.pixelShaderCode.code;

                    using var psByteCode = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();

                    await WriteShaderEntries(dst, variantDataStart, cancellationToken,
                        (ShaderStage.AmplificationShader, asByteCode),
                        (ShaderStage.MeshShader, msByteCode),
                        (ShaderStage.PixelShader, psByteCode));

                    variantEntries[i] = new ShaderContentHeader.VariantEntry
                    {
                        variantKey = ComputeVariantKey(activeKeywords, allKeywords),
                        dataOffset = variantDataStart - assetStartOffset,
                        dataSize = dst.Position - variantDataStart
                    };
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
                passCount = 1, // Compute shaders have a single pass
            };

            var assetStartOffset = dst.Position;
            dst.Write(header);

            // Compute unique keywords
            var allKeywords = new List<string>();
            var stringTableBytes = new List<byte>();
            var groupOffsets = new List<uint>();
            foreach (var group in semantics.keywords)
            {
                groupOffsets.Add((uint)stringTableBytes.Count);
                if (group.keywords != null)
                {
                    foreach (var kw in group.keywords)
                    {
                        if (!allKeywords.Contains(kw)) allKeywords.Add(kw);
                        stringTableBytes.AddRange(Encoding.UTF8.GetBytes(kw));
                        stringTableBytes.Add(0); // Null terminator
                    }
                }
            }

            var stringTableOffset = (uint)(dst.Position - assetStartOffset);
            var stringTableSize = (uint)stringTableBytes.Count;
            
            // Update header
            var currentPos = dst.Position;
            dst.Position = assetStartOffset;
            header.keywordStringTableOffset = stringTableOffset;
            header.keywordStringTableSize = stringTableSize;
            dst.Write(header);
            dst.Position = currentPos;

            if (stringTableSize > 0)
            {
                dst.Write(BitConverter.GetBytes(stringTableSize));
                dst.Write(stringTableBytes.ToArray());
            }
            else
            {
                dst.Write(BitConverter.GetBytes((uint)0));
            }

            var combinations = GenerateVariantCombinations(semantics.keywords.ToArray());

            var passHeader = new ShaderContentHeader.PassHeader
            {
                entryPointCount = (uint)descriptor.ShaderCodes.Length,
                variantCount = (uint)combinations.Count,
                keywordGroupCount = (uint)semantics.keywords.Count
            };
            dst.Write(passHeader);

            foreach (var t in groupOffsets.Select((offset, idx) => new ShaderContentHeader.KeywordGroupDescriptor
            {
                stringTableOffset = offset,
                keywordCount = (uint)(semantics.keywords[idx].keywords?.Count ?? 0)
            }))
            {
                dst.Write(t);
            }

            var variantEntriesOffset = dst.Position;
            var variantEntries = new ShaderContentHeader.VariantEntry[combinations.Count];
            for (var i = 0; i < combinations.Count; i++)
            {
                dst.Write(variantEntries[i]); // Placeholder
            }

            for (var i = 0; i < combinations.Count; i++)
            {
                var activeKeywords = combinations[i];
                var variantDataStart = dst.Position;
                
                var variantDefines = descriptor.Defines.ToList();
                variantDefines.AddRange(activeKeywords);

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
                            defines = variantDefines.ToArray(),
                            entryPoint = shaderCode.entryPoint,
                            shaderCode = shaderCode.code,
                        };

                        byteCodes[j] = _compiler.Compile(in config, AllocationHandle.TLSF).GetValueOrThrow();
                    }

                    var entries = byteCodes.Select((bc, index) => (ShaderStage.ComputeShader, bc)).ToArray();
                    await WriteShaderEntries(dst, variantDataStart, cancellationToken, entries);
                }
                finally
                {
                    foreach (var code in byteCodes)
                    {
                        code.Dispose();
                    }
                }

                variantEntries[i] = new ShaderContentHeader.VariantEntry
                {
                    variantKey = ComputeVariantKey(activeKeywords, allKeywords),
                    dataOffset = variantDataStart - assetStartOffset,
                    dataSize = dst.Position - variantDataStart
                };
            }

            var endOfPass = dst.Position;
            dst.Position = variantEntriesOffset;
            foreach (var entry in variantEntries)
            {
                dst.Write(entry);
            }
            dst.Position = endOfPass;
        }
        else
        {
            throw new NotSupportedException($"Unsupported shader file extension: {ext}");
        }
    }

    public void Dispose()
    {
        _compiler.Dispose();
        GC.SuppressFinalize(this);
    }
}

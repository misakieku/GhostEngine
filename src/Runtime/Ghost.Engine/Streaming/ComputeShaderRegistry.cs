using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Services;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

public enum ComputeShaderState : byte
{
    Invalid = 0,
    MetadataReady = 1,
    BytecodeReady = 2,
}

public struct ComputeShaderRecord
{
    public Guid AssetId { get; internal set; }
    public Handle<ComputeShader> Shader { get; internal set; }
    public ulong ShaderId { get; internal set; }
    public ulong LayoutHash { get; internal set; }
    public uint PropertyBufferSize { get; internal set; }
    public int EntryPointCount { get; internal set; }
    public uint Generation { get; internal set; }
    public ComputeShaderState State { get; internal set; }
}

public sealed class ComputeShaderRegistry : IDisposable
{
    private readonly ResourceManager _resourceManager;
    private readonly Dictionary<Guid, int> _assetToShader;
    private readonly ComputeShaderRecord[] _shaders;
    private readonly int[] _states;
    private readonly uint[] _generations;
    private bool _disposed;

    internal ComputeShaderRegistry(ResourceManager resourceManager, IReadOnlyList<ShaderCatalogEntry> catalog)
    {
        _resourceManager = resourceManager;
        var computeCount = 0;
        for (var i = 0; i < catalog.Count; i++)
        {
            if (catalog[i].ShaderType == ShaderType.Compute)
            {
                computeCount++;
            }
        }

        _assetToShader = new Dictionary<Guid, int>(computeCount);
        _shaders = new ComputeShaderRecord[computeCount];
        _states = new int[computeCount];
        _generations = new uint[computeCount];

        var shaderIndex = 0;
        try
        {
            for (var catalogIndex = 0; catalogIndex < catalog.Count; catalogIndex++)
            {
                var entry = catalog[catalogIndex];
                if (entry.ShaderType != ShaderType.Compute)
                {
                    continue;
                }

                if (entry.Passes.Length != 1 || entry.Passes[0].EntryPointCount == 0 || entry.Passes[0].EntryPointCount > 8)
                {
                    throw new InvalidDataException($"Compute shader '{entry.Name}' must have one to eight entry points.");
                }

                if (!_assetToShader.TryAdd(entry.AssetId, shaderIndex))
                {
                    throw new InvalidDataException($"Shader catalog contains duplicate asset ID {entry.AssetId}.");
                }

                var descriptor = new ComputeShaderDescriptor
                {
                    Name = entry.Name,
                    PropertyBufferSize = entry.PropertyBufferSize,
                    ShaderModel = entry.ShaderModel,
                    ShaderCodes = new ShaderCode[entry.Passes[0].EntryPointCount],
                    Defines = Array.Empty<string>(),
                };
                var handle = resourceManager.CreateComputeShader(descriptor);
                if (handle.IsInvalid)
                {
                    throw new InvalidOperationException($"Failed to register compute shader metadata for '{entry.Name}'.");
                }

                _shaders[shaderIndex] = new ComputeShaderRecord
                {
                    AssetId = entry.AssetId,
                    Shader = handle,
                    ShaderId = entry.ShaderId,
                    LayoutHash = entry.LayoutHash,
                    PropertyBufferSize = entry.PropertyBufferSize,
                    EntryPointCount = (int)entry.Passes[0].EntryPointCount,
                    State = ComputeShaderState.MetadataReady,
                };
                _states[shaderIndex] = (int)ComputeShaderState.MetadataReady;
                shaderIndex++;
            }
        }
        catch
        {
            for (var i = 0; i < shaderIndex; i++)
            {
                resourceManager.ReleaseComputeShader(_shaders[i].Shader);
            }

            throw;
        }
    }

    public int Count => _shaders.Length;

    public ref readonly ComputeShaderRecord GetShader(int index)
    {
        if ((uint)index >= (uint)_shaders.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return ref _shaders[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComputeShaderState GetState(int index)
    {
        if ((uint)index >= (uint)_shaders.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return (ComputeShaderState)Volatile.Read(ref _states[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetGeneration(int index)
    {
        if ((uint)index >= (uint)_shaders.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Volatile.Read(ref _generations[index]);
    }

    internal bool TryGetShaderHandle(Guid assetId, out Handle<ComputeShader> handle)
    {
        if (_assetToShader.TryGetValue(assetId, out var index))
        {
            handle = _shaders[index].Shader;
            return true;
        }

        handle = Handle<ComputeShader>.Invalid;
        return false;
    }

    internal bool TryGetShaderIndex(Guid assetId, out int index)
    {
        return _assetToShader.TryGetValue(assetId, out index);
    }

    internal void PublishBytecodeReady(Guid assetId)
    {
        if (!_assetToShader.TryGetValue(assetId, out var index))
        {
            throw new InvalidDataException($"Compute shader asset {assetId} is missing from the runtime shader catalog.");
        }

        var generation = Volatile.Read(ref _generations[index]) + 1;
        Volatile.Write(ref _generations[index], generation);
        Volatile.Write(ref _states[index], (int)ComputeShaderState.BytecodeReady);
    }
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        for (var i = 0; i < _shaders.Length; i++)
        {
            _resourceManager.ReleaseComputeShader(_shaders[i].Shader);
        }

        GC.SuppressFinalize(this);
    }
}

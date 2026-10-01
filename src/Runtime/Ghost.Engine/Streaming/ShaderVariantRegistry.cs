using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

/// <summary>
/// Dense runtime index for a graphics shader variant.
/// </summary>
public readonly struct ShaderVariantIndex : IEquatable<ShaderVariantIndex>
{
    public static ShaderVariantIndex Invalid => new ShaderVariantIndex(-1);

    public int Value { get; }
    public bool IsValid => Value >= 0;

    public ShaderVariantIndex(int value)
    {
        Value = value;
    }

    public bool Equals(ShaderVariantIndex other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ShaderVariantIndex other && Equals(other);
    public override int GetHashCode() => Value;
    public static bool operator ==(ShaderVariantIndex left, ShaderVariantIndex right) => left.Equals(right);
    public static bool operator !=(ShaderVariantIndex left, ShaderVariantIndex right) => !left.Equals(right);
}

/// <summary>
/// Publication state of one graphics shader variant.
/// </summary>
public enum ShaderVariantState : byte
{
    Invalid = 0,
    MetadataReady = 1,
    BytecodeReady = 2,
}

/// <summary>
/// Runtime metadata for one graphics shader variant.
/// </summary>
public struct ShaderVariantRecord
{
    public ShaderVariantIndex Index { get; internal set; }
    public Guid AssetId { get; internal set; }
    public Handle<Shader> Shader { get; internal set; }
    public ulong ShaderId { get; internal set; }
    public ulong FamilyId { get; internal set; }
    public ulong LayoutHash { get; internal set; }
    public uint PropertyBufferSize { get; internal set; }
    public int PassCount { get; internal set; }
    public uint SupportedPasses { get; internal set; }
    public uint ShadingModelId { get; internal set; }
    internal ShaderModel ShaderModel { get; set; }
    internal ShaderCatalogPass[] Passes { get; set; }
}

/// <summary>
/// Owns metadata-complete graphics shader handles and dense per-semantic variant rosters.
/// Constructed before runtime initialization so materials can be created before bytecode streaming completes.
/// </summary>
public sealed class ShaderVariantRegistry : IShaderVariantSource, IDisposable
{
    private readonly ResourceManager _resourceManager;
    private readonly Dictionary<Guid, int> _assetToVariant;
    private readonly Dictionary<ulong, int> _shaderToVariant;
    private readonly ShaderVariantRecord[] _variants;
    private readonly ShaderVariantIndex[][] _semanticVariants;
    private readonly ShaderVariantDispatchInfo[][] _dispatchVariants;
    private readonly int[][] _dispatchRepresentatives;
    private readonly int[][] _variantToDispatchSlot;
    private readonly int[] _states;
    private readonly uint[] _generations;
    private bool _disposed;

    internal ShaderVariantRegistry(ResourceManager resourceManager, IReadOnlyList<ShaderCatalogEntry> catalog)
    {
        _resourceManager = resourceManager;

        var graphicsCount = 0;
        for (var i = 0; i < catalog.Count; i++)
        {
            if (catalog[i].ShaderType == ShaderType.Graphics)
            {
                graphicsCount++;
            }
        }

        _assetToVariant = new Dictionary<Guid, int>(graphicsCount);
        _shaderToVariant = new Dictionary<ulong, int>(graphicsCount);
        _variants = new ShaderVariantRecord[graphicsCount];
        _states = new int[graphicsCount];
        _generations = new uint[graphicsCount];

        var semanticLists = new List<ShaderVariantIndex>[(int)PassSemantic.Count];
        for (var i = 0; i < semanticLists.Length; i++)
        {
            semanticLists[i] = new List<ShaderVariantIndex>();
        }

        var variantIndex = 0;
        try
        {
            for (var catalogIndex = 0; catalogIndex < catalog.Count; catalogIndex++)
            {
                var entry = catalog[catalogIndex];
                if (entry.ShaderType != ShaderType.Graphics)
                {
                    continue;
                }

                if (!_assetToVariant.TryAdd(entry.AssetId, variantIndex))
                {
                    throw new InvalidDataException($"Shader catalog contains duplicate asset ID {entry.AssetId}.");
                }

                if (!_shaderToVariant.TryAdd(entry.ShaderId, variantIndex))
                {
                    throw new InvalidDataException($"Shader catalog contains duplicate shader ID 0x{entry.ShaderId:X16}.");
                }

                var index = new ShaderVariantIndex(variantIndex);
                var supportedPasses = 0u;
                for (var passIndex = 0; passIndex < entry.Passes.Length; passIndex++)
                {
                    var semantic = entry.Passes[passIndex].Semantic;
                    if ((uint)semantic >= (uint)PassSemantic.Count)
                    {
                        throw new InvalidDataException($"Shader '{entry.Name}' contains invalid pass semantic {semantic}.");
                    }

                    supportedPasses |= 1u << (int)semantic;
                }

                var descriptor = CreateDescriptor(entry, (uint)variantIndex);
                var shaderHandle = resourceManager.CreateShader(descriptor);
                if (shaderHandle.IsInvalid)
                {
                    throw new InvalidOperationException($"Failed to register shader metadata for '{entry.Name}'.");
                }

                var shadingModelId = 0u;
                for (var passIndex = 0; passIndex < entry.Passes.Length; passIndex++)
                {
                    var pass = entry.Passes[passIndex];
                    if (pass.Semantic == PassSemantic.DeferredLighting)
                    {
                        shadingModelId = pass.ShadingModelId;
                    }

                    semanticLists[(int)pass.Semantic].Add(index);
                }

                _variants[variantIndex] = new ShaderVariantRecord
                {
                    Index = index,
                    AssetId = entry.AssetId,
                    Shader = shaderHandle,
                    FamilyId = entry.FamilyId,
                    ShaderId = entry.ShaderId,
                    LayoutHash = entry.LayoutHash,
                    PropertyBufferSize = entry.PropertyBufferSize,
                    PassCount = entry.Passes.Length,
                    SupportedPasses = supportedPasses,
                    ShaderModel = entry.ShaderModel,
                    Passes = entry.Passes,
                    ShadingModelId = shadingModelId,
                };
                _states[variantIndex] = (int)ShaderVariantState.MetadataReady;

                variantIndex++;
            }
        }
        catch
        {
            for (var i = 0; i < variantIndex; i++)
            {
                resourceManager.ReleaseShader(_variants[i].Shader);
            }

            throw;
        }

        _semanticVariants = new ShaderVariantIndex[semanticLists.Length][];
        for (var i = 0; i < semanticLists.Length; i++)
        {
            _semanticVariants[i] = semanticLists[i].ToArray();
        }

        _dispatchVariants = new ShaderVariantDispatchInfo[_semanticVariants.Length][];
        _dispatchRepresentatives = new int[_semanticVariants.Length][];
        _variantToDispatchSlot = new int[_semanticVariants.Length][];

        for (var semanticIndex = 0; semanticIndex < _semanticVariants.Length; semanticIndex++)
        {
            var semantic = (PassSemantic)semanticIndex;
            var semanticVariants = _semanticVariants[semanticIndex];

            _variantToDispatchSlot[semanticIndex] = new int[graphicsCount];
            Array.Fill(_variantToDispatchSlot[semanticIndex], -1);

            var uniqueSignatures = new List<PassCodeSignature>();
            var uniqueDispatch = new List<ShaderVariantDispatchInfo>();
            var representatives = new List<int>();

            for (var i = 0; i < semanticVariants.Length; i++)
            {
                var index = semanticVariants[i];
                var pass = FindPass(in _variants[index.Value], semantic);
                if (pass == null)
                {
                    continue;
                }

                var sig = new PassCodeSignature(pass);
                var existingSlot = -1;
                for (var slot = 0; slot < uniqueSignatures.Count; slot++)
                {
                    if (uniqueSignatures[slot].Equals(sig))
                    {
                        existingSlot = slot;
                        break;
                    }
                }

                if (existingSlot >= 0)
                {
                    _variantToDispatchSlot[semanticIndex][index.Value] = existingSlot;
                }
                else
                {
                    var slot = uniqueSignatures.Count;
                    uniqueSignatures.Add(sig);
                    representatives.Add(index.Value);
                    _variantToDispatchSlot[semanticIndex][index.Value] = slot;

                    var denseIndex = (semantic == PassSemantic.Visibility) ? slot : index.Value;
                    uniqueDispatch.Add(new ShaderVariantDispatchInfo(denseIndex, _variants[index.Value].Shader));
                }
            }

            _dispatchVariants[semanticIndex] = uniqueDispatch.ToArray();
            _dispatchRepresentatives[semanticIndex] = representatives.ToArray();
        }
    }

    /// <summary>
    /// Number of registered graphics shader variants.
    /// </summary>
    public int Count => _variants.Length;

    /// <summary>
    /// Resolves a shader asset ID to its dense runtime variant index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetVariantIndex(Guid assetId, out ShaderVariantIndex index)
    {
        if (_assetToVariant.TryGetValue(assetId, out var value))
        {
            index = new ShaderVariantIndex(value);
            return true;
        }

        index = ShaderVariantIndex.Invalid;
        return false;
    }

    /// <summary>
    /// Resolves a persistent shader ID to its dense runtime variant index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetVariantIndex(ulong shaderId, out ShaderVariantIndex index)
    {
        if (_shaderToVariant.TryGetValue(shaderId, out var value))
        {
            index = new ShaderVariantIndex(value);
            return true;
        }

        index = ShaderVariantIndex.Invalid;
        return false;
    }

    /// <summary>
    /// Returns metadata for a valid dense variant index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ShaderVariantRecord GetVariant(ShaderVariantIndex index)
    {
        if ((uint)index.Value >= (uint)_variants.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return ref _variants[index.Value];
    }

    /// <summary>
    /// Returns the dense graphics shader variants implementing the requested semantic.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ShaderVariantIndex> GetVariants(PassSemantic semantic)
    {
        if ((uint)semantic >= (uint)_semanticVariants.Length)
        {
            return ReadOnlySpan<ShaderVariantIndex>.Empty;
        }

        return _semanticVariants[(int)semantic];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ShaderVariantDispatchInfo> GetDispatchVariants(PassSemantic semantic)
    {
        if ((uint)semantic >= (uint)_dispatchVariants.Length)
        {
            return ReadOnlySpan<ShaderVariantDispatchInfo>.Empty;
        }

        return _dispatchVariants[(int)semantic];
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBytecodeReady(int denseIndex)
    {
        return (uint)denseIndex < (uint)_states.Length &&
               (ShaderVariantState)Volatile.Read(ref _states[denseIndex]) == ShaderVariantState.BytecodeReady;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBytecodeReady(ShaderVariantIndex index)
    {
        return IsBytecodeReady(index.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateIndex(ShaderVariantIndex index)
    {
        if ((uint)index.Value >= (uint)_variants.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <summary>
    /// Returns the atomically published readiness state for a variant.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderVariantState GetState(ShaderVariantIndex index)
    {
        ValidateIndex(index);
        return (ShaderVariantState)Volatile.Read(ref _states[index.Value]);
    }

    /// <summary>
    /// Returns the atomically published bytecode generation for a variant.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetGeneration(ShaderVariantIndex index)
    {
        ValidateIndex(index);
        return Volatile.Read(ref _generations[index.Value]);
    }

    internal void PublishBytecodeReady(Guid assetId)
    {
        if (!_assetToVariant.TryGetValue(assetId, out var index))
        {
            throw new InvalidDataException($"Shader asset {assetId} is missing from the runtime shader catalog.");
        }

        var generation = Volatile.Read(ref _generations[index]) + 1;
        Volatile.Write(ref _generations[index], generation);
        Volatile.Write(ref _states[index], (int)ShaderVariantState.BytecodeReady);

        UpdateDispatchRepresentatives(index);
    }

    private void UpdateDispatchRepresentatives(int readyVariantIndex)
    {
        ref readonly var record = ref _variants[readyVariantIndex];
        var passes = record.Passes;
        if (passes == null)
        {
            return;
        }

        for (var passIndex = 0; passIndex < passes.Length; passIndex++)
        {
            var semanticIndex = (int)passes[passIndex].Semantic;
            if ((uint)semanticIndex >= (uint)_dispatchVariants.Length)
            {
                continue;
            }

            var slot = _variantToDispatchSlot[semanticIndex][readyVariantIndex];
            if (slot < 0)
            {
                continue;
            }

            var currentRep = _dispatchRepresentatives[semanticIndex][slot];
            if (!IsBytecodeReady(currentRep))
            {
                _dispatchRepresentatives[semanticIndex][slot] = readyVariantIndex;
                var semantic = (PassSemantic)semanticIndex;
                var denseIndex = (semantic == PassSemantic.Visibility) ? slot : readyVariantIndex;
                _dispatchVariants[semanticIndex][slot] = new ShaderVariantDispatchInfo(denseIndex, record.Shader);
            }
        }
    }

    private static ShaderCatalogPass? FindPass(in ShaderVariantRecord record, PassSemantic semantic)
    {
        var passes = record.Passes;
        if (passes == null)
        {
            return null;
        }

        for (var i = 0; i < passes.Length; i++)
        {
            if (passes[i].Semantic == semantic)
            {
                return passes[i];
            }
        }

        return null;
    }

    private readonly struct PassCodeSignature : IEquatable<PassCodeSignature>
    {
        private readonly ulong _pipelineKey;
        private readonly ShaderStageMask _stageMask;
        private readonly uint _shadingModelId;
        private readonly ulong[] _bytecodeHashes;
        private readonly uint _fallbackKey;

        public PassCodeSignature(ShaderCatalogPass pass)
        {
            _pipelineKey = pass.LocalPipeline.GetHashCode64();
            _stageMask = pass.StageMask;
            _shadingModelId = pass.Semantic == PassSemantic.DeferredLighting ? pass.ShadingModelId : 0u;
            _bytecodeHashes = pass.BytecodeHashes ?? Array.Empty<ulong>();

            if (_bytecodeHashes.Length == 0 || pass.Semantic == PassSemantic.DeferredTexturing)
            {
                _fallbackKey = (pass.Semantic == PassSemantic.DeferredLighting && pass.ShadingModelId != 0)
                    ? pass.ShadingModelId
                    : (uint)pass.PassId;
            }
            else
            {
                _fallbackKey = 0;
            }
        }

        public bool Equals(PassCodeSignature other)
        {
            if (_pipelineKey != other._pipelineKey ||
                _stageMask != other._stageMask ||
                _shadingModelId != other._shadingModelId)
            {
                return false;
            }

            if (_fallbackKey != 0 || other._fallbackKey != 0)
            {
                return _fallbackKey == other._fallbackKey;
            }

            if (_bytecodeHashes.Length != other._bytecodeHashes.Length)
            {
                return false;
            }

            for (var i = 0; i < _bytecodeHashes.Length; i++)
            {
                if (_bytecodeHashes[i] != other._bytecodeHashes[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is PassCodeSignature other && Equals(other);

        public override int GetHashCode()
        {
            var h = HashCode.Combine(_pipelineKey, (int)_stageMask, _shadingModelId, _fallbackKey);
            for (var i = 0; i < _bytecodeHashes.Length; i++)
            {
                h = HashCode.Combine(h, _bytecodeHashes[i]);
            }
            return h;
        }
    }

    internal bool TryGetShaderHandle(Guid assetId, out Handle<Shader> handle)
    {
        if (_assetToVariant.TryGetValue(assetId, out var index))
        {
            handle = _variants[index].Shader;
            return true;
        }

        handle = Handle<Shader>.Invalid;
        return false;
    }

    private static GraphicsShaderDescriptor CreateDescriptor(ShaderCatalogEntry entry, uint variantIndex)
    {
        var passes = new PassDescriptor[entry.Passes.Length];
        for (var i = 0; i < passes.Length; i++)
        {
            var pass = entry.Passes[i];
            passes[i] = new PassDescriptor
            {
                name = pass.Name,
                semantic = pass.Semantic,
                stageMask = pass.StageMask,
                localPipeline = pass.LocalPipeline,
                defines = Array.Empty<string>(),
            };
        }

        var descriptor = new GraphicsShaderDescriptor
        {
            Name = entry.Name,
            PropertyBufferSize = entry.PropertyBufferSize,
            ShaderModel = entry.ShaderModel,
            Passes = passes,
            VariantIndex = variantIndex,
        };

        for (var i = 0; i < passes.Length; i++)
        {
            descriptor.Passes[i].shader = descriptor;
        }

        return descriptor;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (var i = 0; i < _variants.Length; i++)
        {
            _resourceManager.ReleaseShader(_variants[i].Shader);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

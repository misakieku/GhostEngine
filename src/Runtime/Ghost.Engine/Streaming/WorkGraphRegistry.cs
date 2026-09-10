using Ghost.Core;
using Ghost.Core.Graphics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

public enum WorkGraphState : byte
{
    Invalid = 0,
    MetadataReady = 1,
    BytecodeReady = 2,
}

public struct WorkGraphRecord
{
    public Guid AssetId { get; internal set; }
    public string Name { get; internal set; }
    public ulong ShaderId { get; internal set; }
    public ulong LayoutHash { get; internal set; }
    public uint PropertyBufferSize { get; internal set; }
    internal ulong FamilyId { get; set; }
    internal ShaderModel ShaderModel { get; set; }
    internal ShaderCatalogPass[] Passes { get; set; }
    public uint Generation { get; internal set; }
    public WorkGraphState State { get; internal set; }
}

public sealed class WorkGraphRegistry : IDisposable
{
    private readonly Dictionary<Guid, int> _assetToGraph;
    private readonly WorkGraphRecord[] _graphs;
    private readonly int[] _states;
    private readonly uint[] _generations;
    private bool _disposed;

    internal WorkGraphRegistry(IReadOnlyList<ShaderCatalogEntry> catalog)
    {
        var count = 0;
        for (var i = 0; i < catalog.Count; i++)
        {
            if (catalog[i].ShaderType == ShaderType.WorkGraph)
            {
                count++;
            }
        }

        _assetToGraph = new Dictionary<Guid, int>(count);
        _graphs = new WorkGraphRecord[count];
        _states = new int[count];
        _generations = new uint[count];

        var index = 0;
        for (var catalogIndex = 0; catalogIndex < catalog.Count; catalogIndex++)
        {
            var entry = catalog[catalogIndex];
            if (entry.ShaderType != ShaderType.WorkGraph)
            {
                continue;
            }

            if (!_assetToGraph.TryAdd(entry.AssetId, index))
            {
                throw new InvalidDataException($"Shader catalog contains duplicate asset ID {entry.AssetId}.");
            }

            _graphs[index] = new WorkGraphRecord
            {
                AssetId = entry.AssetId,
                Name = entry.Name,
                ShaderId = entry.ShaderId,
                LayoutHash = entry.LayoutHash,
                PropertyBufferSize = entry.PropertyBufferSize,
                FamilyId = entry.FamilyId,
                ShaderModel = entry.ShaderModel,
                Passes = entry.Passes,
                State = WorkGraphState.MetadataReady,
            };
            _states[index] = (int)WorkGraphState.MetadataReady;
            index++;
        }
    }

    public int Count => _graphs.Length;

    public ref readonly WorkGraphRecord GetGraph(int index)
    {
        if ((uint)index >= (uint)_graphs.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return ref _graphs[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WorkGraphState GetState(int index)
    {
        if ((uint)index >= (uint)_graphs.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return (WorkGraphState)Volatile.Read(ref _states[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetGeneration(int index)
    {
        if ((uint)index >= (uint)_graphs.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Volatile.Read(ref _generations[index]);
    }

    internal bool TryGetGraphIndex(Guid assetId, out int index)
    {
        return _assetToGraph.TryGetValue(assetId, out index);
    }

    internal void PublishBytecodeReady(Guid assetId)
    {
        if (!_assetToGraph.TryGetValue(assetId, out var index))
        {
            throw new InvalidDataException($"Work graph asset {assetId} is missing from the runtime shader catalog.");
        }

        var generation = Volatile.Read(ref _generations[index]) + 1;
        Volatile.Write(ref _generations[index], generation);
        Volatile.Write(ref _states[index], (int)WorkGraphState.BytecodeReady);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

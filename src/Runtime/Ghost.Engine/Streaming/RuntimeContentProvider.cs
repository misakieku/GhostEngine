using Ghost.Core;
using K4os.Compression.LZ4.Streams;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using ZstdSharp;

namespace Ghost.Engine.Streaming;

public class RuntimeContentProvider : IContentProvider, IDisposable
{
    private readonly Manifest _manifest;
    private readonly Dictionary<Guid, AssetInfo> _guidToInfo;
    private readonly ConcurrentDictionary<string, MemoryMappedFile> _packFiles;
    private readonly string _packDirectory;
    private readonly object _validatedPacksLock = new();
    private readonly HashSet<string> _validatedPacks = new(StringComparer.Ordinal);

    public RuntimeContentProvider(string manifestPath)
    {
        _manifest = Manifest.LoadFromDiskAsync(manifestPath).GetAwaiter().GetResult();
        _guidToInfo = new Dictionary<Guid, AssetInfo>(_manifest.Assets.Count);
        _packFiles = new ConcurrentDictionary<string, MemoryMappedFile>(StringComparer.Ordinal);
        _packDirectory = Path.GetDirectoryName(manifestPath) ?? string.Empty;

        foreach (var (_, location) in _manifest.Assets)
        {
            _guidToInfo[location.AssetId] = location;
        }
    }

    public Guid VirtualPathToGuid(string path)
    {
        return _manifest.Assets.GetValueOrDefault(path).AssetId;
    }

    public AssetType GetAssetType(Guid guid)
    {
        return _guidToInfo.GetValueOrDefault(guid).AssetType;
    }

    public Guid[] GetDependencies(Guid guid)
    {
        // TODO: The manifest does not currently record per-asset dependency IDs (DependencyIds),
        // so dependencies cannot be resolved at runtime. Once the packing step writes them into
        // each AssetInfo entry, return them here so AssetManager can pre-schedule dependencies.
        return Array.Empty<Guid>();
    }

    public bool HasAsset(Guid guid)
    {
        return _guidToInfo.ContainsKey(guid);
    }

    public Result<AssetReadData> OpenReadAsync(Guid guid, CancellationToken token = default)
    {
        if (!_guidToInfo.TryGetValue(guid, out var info))
        {
            return Result.Failure($"Asset with GUID {guid} not found in the manifest.");
        }

        var packPath = ResolvePackPath(info.PackFileName);
        if (!ValidatePackHeader(packPath))
        {
            return Result.Failure($"Pack file '{info.PackFileName}' is missing or has an invalid header (bad magic or unsupported format version).");
        }

        var packFile = GetOrOpenPackFile(packPath);
        if (packFile is null)
        {
            return Result.Failure($"Pack file '{info.PackFileName}' could not be opened.");
        }

        if (info.Size <= 0)
        {
            return Result.Failure($"Asset with GUID {guid} has an invalid size ({info.Size}) in the manifest.");
        }

        // A memory-mapped view over exactly the packed payload slice. Multiple concurrent readers
        // may create views over the same mapped file safely.
        var viewStream = packFile.CreateViewStream(info.Offset, info.Size, MemoryMappedFileAccess.Read);

        var decompressedStream = _manifest.CompressionMethod switch
        {
            CompressionMethod.None => viewStream,
            CompressionMethod.Zstd => new DecompressionStream(viewStream, leaveOpen: false),
            CompressionMethod.LZ4 => (Stream)LZ4Stream.Decode(viewStream, leaveOpen: false),
            _ => throw new NotSupportedException($"Unsupported compression method: {_manifest.CompressionMethod}")
        };

        return new AssetReadData
        {
            assetId = info.AssetId,
            assetType = info.AssetType,
            stream = decompressedStream,
        };
    }

    /// <summary>
    /// Resolves a pack file path from the manifest against the directory that contains the
    /// manifest itself, so that relative names like "pack_0000.pack" work regardless of the
    /// process working directory.
    /// </summary>
    private string ResolvePackPath(string packFileName)
    {
        return Path.IsPathRooted(packFileName)
            ? packFileName
            : Path.Combine(_packDirectory, packFileName);
    }

    /// <summary>
    /// Lazily opens (and caches) a memory-mapped handle for a pack file.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="MemoryMappedFile"/> is shared by all concurrent reads; each
    /// <see cref="OpenReadAsync"/> call creates its own view stream over the mapped region.
    /// A failed open is cached as <c>null</c> so the error is not retried on every access.
    /// </remarks>
    private MemoryMappedFile? GetOrOpenPackFile(string packPath)
    {
        return _packFiles.GetOrAdd(packPath, static path =>
        {
            try
            {
                return MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Releases the cached memory-mapped pack file handles.
    /// </summary>
    public void Dispose()
    {
        foreach (var packFile in _packFiles.Values)
        {
            packFile?.Dispose();
        }

        _packFiles.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Validates the <see cref="PackFileHeader"/> of a pack file once per file.
    /// </summary>
    /// <remarks>
    /// Each pack file is validated the first time it is opened; a <see cref="HashSet{T}"/>
    /// of already-validated file names avoids re-reading the header on every asset open.
    /// Only successfully validated files are recorded, so a missing/corrupt pack is
    /// re-checked (and re-reported) on subsequent opens.
    /// </remarks>
    private bool ValidatePackHeader(string packFileName)
    {
        bool needsValidation;
        lock (_validatedPacksLock)
        {
            needsValidation = !_validatedPacks.Contains(packFileName);
        }

        if (!needsValidation)
        {
            return true;
        }

        bool isValid;
        try
        {
            using var fs = new FileStream(packFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (!PackFileHeader.TryReadFrom(fs, out var header)
                || header.magic != PackFileHeader.MAGIC
                || header.version != PackFileHeader.VERSION)
            {
                Logger.Error($"Pack file '{packFileName}' has an invalid header (magic 0x{header.magic:X8}, version {header.version}); it was not produced by this pipeline version.");
                isValid = false;
            }
            else
            {
                isValid = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to validate pack file '{packFileName}': {ex.Message}");
            isValid = false;
        }

        lock (_validatedPacksLock)
        {
            if (isValid)
            {
                _validatedPacks.Add(packFileName);
            }
        }

        return isValid;
    }
}

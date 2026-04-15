using System.Threading.Channels;
using Ghost.Core;
using Ghost.Editor.Core.AssetHandler;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ghost.Editor.Core.Services;

internal enum ImportReason
{
    NewAsset,
    SourceChanged,
    SettingsChanged,
    HandlerUpgraded,
    ManualReimport,
    Startup,
}

internal readonly record struct ImportJob(
    Guid AssetGuid,
    string SourcePath,
    string MetaPath,
    ImportReason Reason
);

internal sealed class ImportCoordinator : IDisposable
{
    private readonly Channel<ImportJob> _importChannel;
    private readonly AssetCatalog _catalog;
    private readonly AssetHandlerRegistry _handlers;
    private readonly string _assetsRoot;
    private readonly string _libraryRoot;
    private readonly CancellationTokenSource _cts;
    private readonly Task[] _workers;

    // In a real implementation, this event would be used to notify the UI/Rest of engine
    // For now we just focus on the core logic
    // public event EventHandler<AssetChangedEventArgs>? OnAssetChanged;

    public ImportCoordinator(AssetCatalog catalog, AssetHandlerRegistry handlers, string assetsRoot, string libraryRoot, int workerCount = 2)
    {
        _catalog = catalog;
        _handlers = handlers;
        _assetsRoot = assetsRoot;
        _libraryRoot = libraryRoot;
        _cts = new CancellationTokenSource();

        _importChannel = Channel.CreateBounded<ImportJob>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
        });

        _workers = new Task[workerCount];
        for (var i = 0; i < workerCount; i++)
        {
            _workers[i] = Task.Run(() => WorkerLoop(_cts.Token));
        }
    }

    public ValueTask EnqueueAsync(ImportJob job, CancellationToken token = default)
    {
        return _importChannel.Writer.WriteAsync(job, token);
    }

    public async ValueTask EnqueueDirtyAssetsAsync(CancellationToken token = default)
    {
        foreach (var (guid, sourcePath) in _catalog.GetDirtyAssets())
        {
            var metaPath = AssetMetaIO.GetMetaPath(Path.Combine(_assetsRoot, sourcePath));
            await EnqueueAsync(new ImportJob(guid, sourcePath, metaPath, ImportReason.Startup), token);
        }
    }

    private async Task WorkerLoop(CancellationToken token)
    {
        await foreach (var job in _importChannel.Reader.ReadAllAsync(token))
        {
            try
            {
                await ProcessImportAsync(job, token);
            }
            catch (Exception ex)
            {
                _catalog.MarkFailed(job.AssetGuid, ex.Message);
            }
        }
    }

    private async ValueTask ProcessImportAsync(ImportJob job, CancellationToken token)
    {
        var fullSourcePath = Path.Combine(_assetsRoot, job.SourcePath);
        var meta = await AssetMetaIO.ReadAsync(job.MetaPath, token);
        if (meta is null)
        {
            _catalog.MarkFailed(job.AssetGuid, "Missing .gmeta file");
            return;
        }

        var handler = meta.HandlerTypeId.HasValue
            ? _handlers.GetByTypeId(meta.HandlerTypeId.Value)
            : _handlers.GetByExtension(Path.GetExtension(job.SourcePath));

        var contentHash = await ComputeFileHashAsync(fullSourcePath, token);
        var settingsHash = ComputeSettingsHash(meta.Settings);

        // Check if we can skip (if not a manual reimport)
        if (job.Reason != ImportReason.ManualReimport &&
            meta.ContentHash == contentHash &&
            meta.SettingsHash == settingsHash &&
            meta.HandlerVersion == _handlers.GetVersionByTypeId(meta.HandlerTypeId ?? Guid.Empty))
        {
            _catalog.MarkImported(job.AssetGuid, contentHash, settingsHash);
            return;
        }

        var importResult = Result.Success();
        if (handler is IImportableAssetHandler importable)
        {
            // TODO: This should be handled by EditorApplication.
            var importsDir = Path.Combine(_libraryRoot, "Imports");
            Directory.CreateDirectory(importsDir);
            var targetPath = Path.Combine(importsDir, $"{job.AssetGuid:N}.imported");

            await using var sourceStream = new FileStream(fullSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);

            importResult = await importable.ImportAsync(sourceStream, targetStream, job.AssetGuid, meta.Settings, token);
        }

        if (importResult.IsSuccess)
        {
            meta.ContentHash = contentHash;
            meta.SettingsHash = settingsHash;
            meta.HandlerVersion = _handlers.GetVersionByTypeId(meta.HandlerTypeId ?? Guid.Empty);
            meta.LastImportedUtc = DateTime.UtcNow;

            await AssetMetaIO.WriteAsync(job.MetaPath, meta, token);
            _catalog.MarkImported(job.AssetGuid, contentHash, settingsHash);
        }
        else
        {
            _catalog.MarkFailed(job.AssetGuid, importResult.Message ?? "Unknown import error");
        }
    }

    private static async ValueTask<string> ComputeFileHashAsync(string filePath, CancellationToken token)
    {
        if (!File.Exists(filePath))
        {
            return "";
        }

        using var sha = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await sha.ComputeHashAsync(stream, token);
        return Convert.ToHexString(hash);
    }

    private static string ComputeSettingsHash(IAssetSettings? settings)
    {
        if (settings is null)
        {
            return "";
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(settings);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(json);
        return Convert.ToHexString(hash);
    }

    public void Dispose()
    {
        _importChannel.Writer.TryComplete();
        _cts.Cancel();
        _cts.Dispose();
    }
}

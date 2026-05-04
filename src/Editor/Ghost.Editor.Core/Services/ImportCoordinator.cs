using Ghost.Core;
using Ghost.Editor.Core.Assets;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

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

internal sealed partial class ImportCoordinator : IDisposable
{
    public const string IMPORTED_EXTENSION_NAME = "Imported";
    public const string IMPORTED_EXTENSION = ".imported";

    private readonly Channel<ImportJob> _importChannel;
    private readonly AssetCatalog _catalog;
    private readonly CancellationTokenSource _cts;
    private readonly Task[] _workers;

    public event EventHandler<Guid>? OnImportCompleted;

    public ImportCoordinator(AssetCatalog catalog, int workerCount = 2)
    {
        _catalog = catalog;
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
                Logger.Error(ex);
            }
        }
    }

    public static string GetImportedAssetPath(Guid assetGuid)
    {
        var fileName = $"{assetGuid:N}{IMPORTED_EXTENSION}";
        var folderName = fileName.Substring(0, 2);

        var importsFolder = Path.Combine(EditorApplication.LibraryImportsFolderPath, folderName);
        var finalPath = Path.Combine(importsFolder, fileName);
        Directory.CreateDirectory(importsFolder);

        return finalPath;
    }

    private async ValueTask ProcessImportAsync(ImportJob job, CancellationToken token)
    {
        var meta = await AssetMetaIO.ReadAsync(job.MetaPath, token);
        if (meta is null)
        {
            Logger.Error("Missing .gmeta file");
            return;
        }

        var handler = meta.HandlerTypeId.HasValue
            ? AssetHandlerRegistry.GetByAssetTypeId(meta.HandlerTypeId.Value)
            : AssetHandlerRegistry.GetByExtension(Path.GetExtension(job.SourcePath));

        var contentHash = await ComputeFileHashAsync(job.SourcePath, token);
        var settingsHash = ComputeSettingsHash(meta.Settings);

        // Check if we can skip (if not a manual reimport)
        if (job.Reason != ImportReason.ManualReimport &&
            meta.ContentHash == contentHash &&
            meta.SettingsHash == settingsHash &&
            meta.HandlerVersion == AssetHandlerRegistry.GetVersionByAssetTypeId(meta.HandlerTypeId ?? Guid.Empty))
        {
            return;
        }

        var importResult = Result.Success();
        ImportedSubAsset[] subAssets = Array.Empty<ImportedSubAsset>();
        if (handler is IImportableAssetHandler importable)
        {
            var targetPath = GetImportedAssetPath(job.AssetGuid);
            if (importable is ISubAssetImportableAssetHandler subAssetImportable)
            {
                var subAssetResult = await subAssetImportable.ImportWithSubAssetsAsync(job.SourcePath, targetPath, job.AssetGuid, meta.Settings, token);
                importResult = subAssetResult;
                if (subAssetResult.IsSuccess)
                {
                    subAssets = subAssetResult.Value;
                }
            }
            else
            {
                importResult = await importable.ImportAsync(job.SourcePath, targetPath, job.AssetGuid, meta.Settings, token);
            }
        }

        if (importResult.IsSuccess)
        {
            meta.ContentHash = contentHash;
            meta.SettingsHash = settingsHash;
            meta.HandlerVersion = AssetHandlerRegistry.GetVersionByAssetTypeId(meta.HandlerTypeId ?? Guid.Empty);
            meta.LastImportedUtc = DateTime.UtcNow;

            await AssetMetaIO.WriteAsync(job.MetaPath, meta, token);

            if (subAssets.Length > 0)
            {
                var dependencies = new Guid[subAssets.Length];
                for (var i = 0; i < subAssets.Length; i++)
                {
                    var subAsset = subAssets[i];
                    dependencies[i] = subAsset.Guid;

                    var subMeta = new AssetMeta
                    {
                        Guid = subAsset.Guid,
                        HandlerTypeId = subAsset.HandlerTypeId,
                        HandlerVersion = meta.HandlerVersion,
                        ContentHash = contentHash,
                        SettingsHash = settingsHash,
                        LastImportedUtc = meta.LastImportedUtc,
                    };

                    _catalog.UpsertSubAsset(job.AssetGuid, subMeta, subAsset.VirtualSourcePath, subAsset.Kind, subAsset.DisplayName, subAsset.StablePath);
                }

                _catalog.RemoveSubAssetsExcept(job.AssetGuid, dependencies);
                _catalog.SetDependencies(job.AssetGuid, dependencies);
            }
            else if (handler is ISubAssetImportableAssetHandler)
            {
                _catalog.RemoveSubAssetsExcept(job.AssetGuid, ReadOnlySpan<Guid>.Empty);
                _catalog.SetDependencies(job.AssetGuid, ReadOnlySpan<Guid>.Empty);
            }

            OnImportCompleted?.Invoke(null, job.AssetGuid);
        }
        else
        {
            Logger.Error(importResult.Message ?? "Unknown import error");
        }
    }

    private static async ValueTask<string> ComputeFileHashAsync(string filePath, CancellationToken token)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        var hasher = new XxHash128();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await hasher.AppendAsync(stream, token);

        Span<byte> hash = stackalloc byte[16];
        hasher.GetCurrentHash(hash);

        return Convert.ToHexString(hash);
    }

    private static string ComputeSettingsHash(IAssetSettings? settings)
    {
        if (settings is null)
        {
            return string.Empty;
        }

        var hash = XxHash128.HashToUInt128(JsonSerializer.SerializeToUtf8Bytes(settings, settings.GetType()));
        Span<byte> bytes = stackalloc byte[16];
        Unsafe.WriteUnaligned(ref bytes[0], hash);

        return Convert.ToHexString(bytes);
    }

    public void Dispose()
    {
        _importChannel.Writer.TryComplete();
        _cts.Cancel();
        _cts.Dispose();
    }
}

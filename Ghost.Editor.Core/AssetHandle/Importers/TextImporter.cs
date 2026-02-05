using Ghost.Core;
using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.AssetHandle.Importers;

/// <summary>
/// Example importer settings for text assets.
/// </summary>
internal class TextImporterSettings : ImporterSettings
{
    public string Encoding
    {
        get;
        set;
    } = "UTF-8";

    public bool TrimWhitespace
    {
        get;
        set;
    } = false;
}

/// <summary>
/// Example importer for text files (.txt, .md).
/// This is a simple test importer to demonstrate the asset import system.
/// </summary>
[AssetImporter(".txt", ".md")]
internal class TextImporter : AssetImporter<TextImporterSettings>
{
    public override async ValueTask<Result> ImportAsync(string assetPath, AssetMeta meta, IAssetService assetService, CancellationToken token = default)
    {
        var settings = GetSettings(meta);

        // Text files typically don't have dependencies
        // If they did, you would extract them from the content here
        var dependencies = new List<Guid>();

        // Validate dependencies
        var depResult = await ValidateDependenciesAsync(dependencies, assetService, token);
        if (depResult.IsFailure)
        {
            return depResult;
        }

        try
        {
            // Read the file
            var content = await File.ReadAllTextAsync(assetPath, token);

            if (settings.TrimWhitespace)
            {
                content = content.Trim();
            }

            // TODO: Process the text content
            // For example:
            // - Convert to a specific format
            // - Extract metadata
            // - Generate assets
            // - Save to output folder

            // For now, just report success
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to import text asset: {ex.Message}");
        }
    }
}

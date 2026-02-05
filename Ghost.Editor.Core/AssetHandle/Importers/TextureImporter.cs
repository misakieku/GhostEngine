using Ghost.Core;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle.Importers;

/// <summary>
/// Importer settings for texture assets.
/// </summary>
internal class TextureImporterSettings : ImporterSettings
{
    /// <summary>
    /// Whether to generate mipmaps for the texture.
    /// </summary>
    public bool GenerateMipmaps
    {
        get;
        set;
    } = true;

    /// <summary>
    /// Whether the texture uses sRGB color space.
    /// </summary>
    public bool SRGB
    {
        get;
        set;
    } = true;

    /// <summary>
    /// Maximum texture size. Images larger than this will be downscaled.
    /// </summary>
    public uint MaxSize
    {
        get;
        set;
    } = 2048;

    /// <summary>
    /// Texture compression format.
    /// Options: "None", "BC1", "BC3", "BC7"
    /// </summary>
    public string CompressionFormat
    {
        get;
        set;
    } = "None";

    /// <summary>
    /// Texture filter mode.
    /// Options: "Point", "Bilinear", "Trilinear"
    /// </summary>
    public string FilterMode
    {
        get;
        set;
    } = "Bilinear";

    /// <summary>
    /// Texture wrap mode.
    /// Options: "Repeat", "Clamp", "Mirror"
    /// </summary>
    public string WrapMode
    {
        get;
        set;
    } = "Repeat";
}

/// <summary>
/// Importer for texture files (.png, .jpg, .jpeg, .dds, .tga, .bmp).
/// Processes image files and converts them into engine-ready texture assets.
/// </summary>
[AssetImporter(".png", ".jpg", ".jpeg", ".dds", ".tga", ".bmp")]
internal class TextureImporter : AssetImporter<TextureImporterSettings>
{
    public override async ValueTask<Result> ImportAsync(string assetPath, AssetMeta meta, CancellationToken token = default)
    {
        var settings = GetSettings(meta);

        // Textures typically don't reference other assets as dependencies
        // If they did (e.g., normal maps referencing base textures), extract here
        var dependencies = new List<Guid>();

        // Validate dependencies
        var depResult = await ValidateDependenciesAsync(dependencies, token);
        if (depResult.IsFailure)
        {
            return depResult;
        }

        try
        {
            // Check if file exists
            if (!File.Exists(assetPath))
            {
                return Result.Failure($"Source texture file not found: {assetPath}");
            }

            // Get image dimensions (simplified - in real implementation would use image library)
            var (width, height) = GetImageDimensions(assetPath);

            if (width == 0 || height == 0)
            {
                return Result.Failure("Failed to read image dimensions");
            }

            // Apply max size constraint
            if (width > settings.MaxSize || height > settings.MaxSize)
            {
                var scale = Math.Min(settings.MaxSize / (float)width, settings.MaxSize / (float)height);
                width = (uint)(width * scale);
                height = (uint)(height * scale);
            }

            // Calculate mipmap count
            uint mipLevels = 1;
            if (settings.GenerateMipmaps)
            {
                mipLevels = CalculateMipLevels(width, height);
            }

            // Determine format
            var format = settings.CompressionFormat == "None" ? "RGBA8" : settings.CompressionFormat;

            // Create texture asset
            var textureAsset = new TextureAsset(meta.Guid, Path.GetFileNameWithoutExtension(assetPath))
            {
                Width = width,
                Height = height,
                MipLevels = mipLevels,
                Format = format,
                IsSRGB = settings.SRGB,
                SourcePath = assetPath
            };

            // Save the imported asset data
            var saveResult = AssetService.SaveImportedAsset(meta.Guid, textureAsset);
            if (saveResult.IsFailure)
            {
                return Result.Failure($"Failed to save texture asset: {saveResult.Message}");
            }

            // In a real implementation, you would:
            // 1. Load the image using a library like ImageSharp or StbImageSharp
            // 2. Resize if needed
            // 3. Generate mipmaps
            // 4. Compress if needed
            // 5. Save the processed texture data to the ImportedAssets folder
            // 6. Update the hash in database

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to import texture: {ex.Message}");
        }
    }

    /// <summary>
    /// Get image dimensions from file.
    /// Simplified implementation - in production, use an image library.
    /// </summary>
    private static (uint width, uint height) GetImageDimensions(string imagePath)
    {
        // This is a placeholder implementation
        // In a real implementation, you would use a library like:
        // - ImageSharp
        // - StbImageSharp
        // - DirectXTex (for DDS files)

        var extension = Path.GetExtension(imagePath).ToLowerInvariant();

        if (extension == ".dds")
        {
            // For DDS files, read the header
            // DDS header format: https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-header
            return ReadDDSHeader(imagePath);
        }
        else
        {
            // For PNG/JPG/etc, we would use an image library
            // For now, return placeholder values
            return (1024, 1024);
        }
    }

    /// <summary>
    /// Read DDS file header to get dimensions.
    /// </summary>
    private static (uint width, uint height) ReadDDSHeader(string ddsPath)
    {
        try
        {
            using var stream = File.OpenRead(ddsPath);
            using var reader = new BinaryReader(stream);

            // Read magic number (should be "DDS ")
            var magic = reader.ReadUInt32();
            if (magic != 0x20534444) // "DDS " in little-endian
            {
                return (0, 0);
            }

            // Read header size (should be 124)
            var headerSize = reader.ReadUInt32();
            if (headerSize != 124)
            {
                return (0, 0);
            }

            // Skip flags
            reader.ReadUInt32();

            // Read height and width
            var height = reader.ReadUInt32();
            var width = reader.ReadUInt32();

            return (width, height);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// Export a texture asset from memory to disk.
    /// </summary>
    public override async ValueTask<Result> ExportAsync<T>(string assetPath, T assetData, AssetMeta meta, CancellationToken token = default)
    {
        if (assetData is not TextureAsset textureAsset)
        {
            return Result.Failure($"Asset data is not a TextureAsset, got {typeof(T).Name}");
        }

        try
        {
            // In a real implementation, you would:
            // 1. Convert the texture data to the appropriate format
            // 2. Write the image file (PNG, DDS, etc.)
            // 3. Save metadata

            // For now, just save metadata as JSON
            var json = JsonSerializer.Serialize(textureAsset, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(assetPath, json, token);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to export texture: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate number of mipmap levels for a given texture size.
    /// </summary>
    private static uint CalculateMipLevels(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return 0;
        }

        uint count = 1;
        while (width > 1 || height > 1)
        {
            width >>= 1;
            height >>= 1;
            count++;
        }

        return count;
    }
}

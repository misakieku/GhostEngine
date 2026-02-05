using Ghost.Editor.Core.AssetHandle;
using Ghost.Data.Services;
using Ghost.Core;

namespace Ghost.UnitTest;

/// <summary>
/// Comprehensive integration tests for AssetService.
/// Tests database operations, file system watchers, searching, importing, and race conditions.
/// </summary>
[TestClass]
[DoNotParallelize] // AssetService is a singleton, tests must run sequentially
public class AssetDatabaseIntegrationTest
{
    private string _tempPath = string.Empty;
    private string _testProjectDir = string.Empty;
    private string _testAssetsDir = string.Empty;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public async Task Setup()
    {
        // Create temporary test project structure
        _tempPath = Path.GetTempPath();
        _testProjectDir = Path.Combine(_tempPath, "GhostAssetDBIntegration_" + Guid.NewGuid().ToString());
        _testAssetsDir = Path.Combine(_testProjectDir, ProjectService.ASSETS_FOLDER);

        Directory.CreateDirectory(_testProjectDir);
        Directory.CreateDirectory(_testAssetsDir);
        Directory.CreateDirectory(Path.Combine(_testProjectDir, ProjectService.CACHE_FOLDER));
        Directory.CreateDirectory(Path.Combine(_testProjectDir, ProjectService.CONFIG_FOLDER));

        Console.WriteLine($"Test project directory: {_testProjectDir}");
        Console.WriteLine($"Test assets directory: {_testAssetsDir}");

        // Create a minimal project file with required metadata
        var projectPath = Path.Combine(_testProjectDir, "TestProject.gproj");

        // Create a proper ProjectMetadata instance
        var metadata = new Ghost.Data.Models.ProjectMetadata("TestProject", new Version(1, 0, 0));

        await using var fileStream = File.Create(projectPath);
        await System.Text.Json.JsonSerializer.SerializeAsync(fileStream, metadata, Ghost.Data.JsonContext.Default.ProjectMetadata, TestContext.CancellationToken);
        await fileStream.FlushAsync(TestContext.CancellationToken);
        fileStream.Close();

        // Set CurrentProject directly
        var projectMetadataInfo = new Data.Models.ProjectMetadataInfo(projectPath, metadata);
        ProjectService.CurrentProject = projectMetadataInfo;

        // Init AssetService
        await AssetService.Initialize(TestContext.CancellationToken);

        // Give the file system watcher time to start
        await Task.Delay(100, TestContext.CancellationToken);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Shutdown AssetService to release file watchers
        try
        {
            AssetService.Shutdown();
        }
        catch
        {
            // Ignore shutdown errors
        }

        // Clean up test directory
        if (Directory.Exists(_tempPath))
        {
            try
            {
                // Add delay to allow file handles to be released
                Thread.Sleep(100);
                Directory.Delete(_tempPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Helper to wait for file system events to be processed.
    /// </summary>
    private async Task WaitForFileSystemEvents(int delayMs = 300)
    {
        await Task.Delay(delayMs, TestContext.CancellationToken);
        AssetService.FlushPendingCommands();

        // Give a bit more time after flush for any final processing
        await Task.Delay(50, TestContext.CancellationToken);
    }

    private static void CheckInternalErrors()
    {
        if (Logger.Logs.Count > 0)
        {
            foreach (var log in Logger.Logs)
            {
                if (log.Level == LogLevel.Error)
                {
                    Assert.Fail($"Internal error logged: {log.Message}");
                }
            }
        }
    }

    [TestMethod]
    public async Task TestAutoMetaGeneration_WhenFileCreated()
    {
        // Create a test file directly in the file system
        var testFile = Path.Combine(_testAssetsDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World", TestContext.CancellationToken);

        // Wait for file system watcher to react and process commands
        await WaitForFileSystemEvents();

        // Check if meta file was auto-generated
        var metaFile = testFile + ".gmeta";
        Assert.IsTrue(File.Exists(metaFile), "Meta file should be auto-generated");

        // Verify meta file content
        var metaContent = await File.ReadAllTextAsync(metaFile, TestContext.CancellationToken);
        Assert.Contains("Guid", metaContent, "Meta file should contain GUID");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestFindAssetsByName_WithWildcards()
    {
        // Create test files
        await File.WriteAllTextAsync(Path.Combine(_testAssetsDir, "player.txt"), "data", TestContext.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_testAssetsDir, "player1.txt"), "data", TestContext.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_testAssetsDir, "player2.txt"), "data", TestContext.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_testAssetsDir, "enemy.txt"), "data", TestContext.CancellationToken);

        // Wait for database to update
        await WaitForFileSystemEvents();

        // Test wildcard search: player*
        var results = await AssetService.FindAssetsByNameAsync("player*", TestContext.CancellationToken);
        Assert.HasCount(3, results, "Should find 3 files matching 'player*'");

        // Test single character wildcard: player?
        results = await AssetService.FindAssetsByNameAsync("player?.txt", TestContext.CancellationToken);
        Assert.HasCount(2, results, "Should find 2 files matching 'player?.txt'");

        // Test exact match
        results = await AssetService.FindAssetsByNameAsync("enemy.txt", TestContext.CancellationToken);
        Assert.HasCount(1, results, "Should find 1 file matching 'enemy.txt'");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestFileRename_ViaFileSystem()
    {
        // Create a file
        var originalPath = Path.Combine(_testAssetsDir, "original.txt");
        await File.WriteAllTextAsync(originalPath, "data", TestContext.CancellationToken);
        await WaitForFileSystemEvents();

        // Get the GUID before rename
        var guidResult = AssetService.PathToGuid(originalPath);
        Assert.IsTrue(guidResult.IsSuccess, "Should be able to get GUID before rename");
        var guid = guidResult.Value;

        // Rename via file system
        var newPath = Path.Combine(_testAssetsDir, "renamed.txt");
        File.Move(originalPath, newPath);
        await WaitForFileSystemEvents();

        // Check if meta file was also moved
        var newMetaPath = newPath + ".gmeta";
        Assert.IsTrue(File.Exists(newMetaPath), "Meta file should be moved with the asset");

        // Verify GUID is preserved
        var newGuidResult = AssetService.PathToGuid(newPath);
        Assert.IsTrue(newGuidResult.IsSuccess, "Should be able to get GUID after rename");
        Assert.AreEqual(guid, newGuidResult.Value, "GUID should be preserved after rename");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestFileDelete_ViaFileSystem()
    {
        // Create a file
        var filePath = Path.Combine(_testAssetsDir, "todelete.txt");
        await File.WriteAllTextAsync(filePath, "data", TestContext.CancellationToken);
        await WaitForFileSystemEvents();

        var guidResult = AssetService.PathToGuid(filePath);
        Assert.IsTrue(guidResult.IsSuccess);
        var guid = guidResult.Value;

        // Delete via file system
        File.Delete(filePath);
        await WaitForFileSystemEvents();

        await Task.Delay(1000, TestContext.CancellationToken);
        // Meta file should also be deleted
        var metaPath = filePath + ".gmeta";
        Assert.IsFalse(File.Exists(metaPath), "Meta file should be deleted with asset");

        // Asset should be removed from database
        var pathResult = AssetService.GuidToPath(guid);
        Assert.IsTrue(pathResult.IsFailure, "Asset should be removed from database");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestFileCreate_ViaAPI()
    {
        var filePath = Path.Combine(_testAssetsDir, "apiCreated.txt");

        // Create via API
        var result = await AssetService.CreateAssetAsync(filePath, TestContext.CancellationToken);
        Assert.IsTrue(result.IsSuccess, "Should create asset successfully");

        // File and meta should exist
        Assert.IsTrue(File.Exists(filePath), "Asset file should exist");
        Assert.IsTrue(File.Exists(filePath + ".gmeta"), "Meta file should exist");

        // Should be in database
        var guidResult = AssetService.PathToGuid(filePath);
        Assert.IsTrue(guidResult.IsSuccess, "Asset should be in database");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestFileMove_ViaAPI()
    {
        // Create initial file
        var sourcePath = Path.Combine(_testAssetsDir, "source.txt");
        await File.WriteAllTextAsync(sourcePath, "data", TestContext.CancellationToken);
        await WaitForFileSystemEvents();

        var guid = AssetService.PathToGuid(sourcePath).Value;

        // Create subdirectory
        var subDir = Path.Combine(_testAssetsDir, "SubFolder");
        Directory.CreateDirectory(subDir);

        var destPath = Path.Combine(subDir, "source.txt");

        // Move via API
        var result = await AssetService.MoveAssetAsync(sourcePath, destPath, TestContext.CancellationToken);
        Assert.IsTrue(result.IsSuccess, $"Should move asset successfully. Error: {result.Message}");

        // Old file should not exist
        Assert.IsFalse(File.Exists(sourcePath), "Source file should not exist");
        Assert.IsFalse(File.Exists(sourcePath + ".gmeta"), "Source meta should not exist");

        // New file should exist
        Assert.IsTrue(File.Exists(destPath), "Destination file should exist");
        Assert.IsTrue(File.Exists(destPath + ".gmeta"), "Destination meta should exist");

        // GUID should be preserved
        var newGuid = AssetService.PathToGuid(destPath).Value;
        Assert.AreEqual(guid, newGuid, "GUID should be preserved");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestFileCopy_ViaAPI()
    {
        // Create initial file
        var sourcePath = Path.Combine(_testAssetsDir, "tocopy.txt");
        await File.WriteAllTextAsync(sourcePath, "data", TestContext.CancellationToken);
        await WaitForFileSystemEvents();

        var sourceGuid = AssetService.PathToGuid(sourcePath).Value;
        var destPath = Path.Combine(_testAssetsDir, "copied.txt");

        // Copy via API
        var result = await AssetService.CopyAssetAsync(sourcePath, destPath, TestContext.CancellationToken);
        Assert.IsTrue(result.IsSuccess, "Should copy asset successfully");

        // Both files should exist
        Assert.IsTrue(File.Exists(sourcePath), "Source file should still exist");
        Assert.IsTrue(File.Exists(destPath), "Destination file should exist");

        // Both should have different GUIDs
        var destGuid = AssetService.PathToGuid(destPath).Value;
        Assert.AreNotEqual(sourceGuid, destGuid, "Copied asset should have different GUID");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestFileDelete_ViaAPI()
    {
        // Create initial file
        var filePath = Path.Combine(_testAssetsDir, "todelete2.txt");
        await File.WriteAllTextAsync(filePath, "data", TestContext.CancellationToken);
        await WaitForFileSystemEvents();

        var guid = AssetService.PathToGuid(filePath).Value;

        // Delete via API
        var result = await AssetService.DeleteAssetAsync(filePath, TestContext.CancellationToken);
        Assert.IsTrue(result.IsSuccess, "Should delete asset successfully");

        // File and meta should not exist
        Assert.IsFalse(File.Exists(filePath), "File should be deleted");
        Assert.IsFalse(File.Exists(filePath + ".gmeta"), "Meta should be deleted");

        // Should be removed from database
        var pathResult = AssetService.GuidToPath(guid);
        Assert.IsTrue(pathResult.IsFailure, "Asset should be removed from database");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestRaceCondition_MultipleFileCreations()
    {
        // Create multiple files simultaneously to test debouncing
        var tasks = new List<Task>();
        var fileNames = new List<string>();

        for (int i = 0; i < 10; i++)
        {
            var fileName = $"race{i}.txt";
            fileNames.Add(fileName);
            var filePath = Path.Combine(_testAssetsDir, fileName);

            tasks.Add(Task.Run(async () =>
            {
                await File.WriteAllTextAsync(filePath, $"data{i}", TestContext.CancellationToken);
            }, TestContext.CancellationToken));
        }

        await Task.WhenAll(tasks);
        await WaitForFileSystemEvents(500); // Wait for all file system events

        // All files should have exactly one meta file
        foreach (var fileName in fileNames)
        {
            var filePath = Path.Combine(_testAssetsDir, fileName);
            var metaPath = filePath + ".gmeta";

            Assert.IsTrue(File.Exists(metaPath), $"Meta file should exist for {fileName}");

            // Read meta and verify it's valid JSON
            var metaContent = await File.ReadAllTextAsync(metaPath, TestContext.CancellationToken);
            Assert.Contains("Guid", metaContent, $"Meta file should be valid for {fileName}");
        }

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestTagSearching()
    {
        // Create files and add tags
        var file1 = Path.Combine(_testAssetsDir, "tagged1.txt");
        var file2 = Path.Combine(_testAssetsDir, "tagged2.txt");
        var file3 = Path.Combine(_testAssetsDir, "untagged.txt");

        await File.WriteAllTextAsync(file1, "data", TestContext.CancellationToken);
        await File.WriteAllTextAsync(file2, "data", TestContext.CancellationToken);
        await File.WriteAllTextAsync(file3, "data", TestContext.CancellationToken);
        await WaitForFileSystemEvents();

        var guid1 = AssetService.PathToGuid(file1).Value;
        var guid2 = AssetService.PathToGuid(file2).Value;

        // Add tags
        await AssetService.SetAssetTagsAsync(guid1, new List<string> { "Test", "Player" }, TestContext.CancellationToken);
        await AssetService.SetAssetTagsAsync(guid2, new List<string> { "Test", "Enemy" }, TestContext.CancellationToken);

        // Search by tag
        var testAssets = await AssetService.FindAssetsByTagAsync("Test", TestContext.CancellationToken);
        Assert.HasCount(2, testAssets, "Should find 2 assets with 'Test' tag");

        var playerAssets = await AssetService.FindAssetsByTagAsync("Player", TestContext.CancellationToken);
        Assert.HasCount(1, playerAssets, "Should find 1 asset with 'Player' tag");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task TestRefreshAsync_DoesNotDuplicateMetadata()
    {
        // Create a file
        var filePath = Path.Combine(_testAssetsDir, "refresh.txt");
        await File.WriteAllTextAsync(filePath, "data", TestContext.CancellationToken);
        await WaitForFileSystemEvents();

        var guid1 = AssetService.PathToGuid(filePath).Value;

        // Call RefreshAsync multiple times
        await AssetService.RefreshAsync(TestContext.CancellationToken);
        await AssetService.RefreshAsync(TestContext.CancellationToken);
        await AssetService.RefreshAsync(TestContext.CancellationToken);

        // GUID should remain the same
        var guid2 = AssetService.PathToGuid(filePath).Value;
        Assert.AreEqual(guid1, guid2, "GUID should not change after refresh");

        // Only one meta file should exist
        var metaFiles = Directory.GetFiles(_testAssetsDir, "refresh.txt.gmeta");
        Assert.HasCount(1, metaFiles, "Should have exactly one meta file");

        CheckInternalErrors();
    }

    [TestMethod]
    public async Task ThreadSafetyTest()
    {
        try
        {
            var testFile = Path.Combine(_testAssetsDir, "test.txt");
            await File.WriteAllTextAsync(testFile, "Hello World", TestContext.CancellationToken);
            await AssetService.RefreshAsync(TestContext.CancellationToken); // This will cause race conditions if not handle properly because both AssetService and FileSystemWatcher are involved
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }

        CheckInternalErrors();
    }
}

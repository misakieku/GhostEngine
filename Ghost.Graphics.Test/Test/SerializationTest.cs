using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;
using System.Text.Json;

namespace Ghost.UnitTest.Test;

internal class SerializationTest : ITest
{
    private const string _TEST_FILE_PATH = "C:/Users/Misaki/Downloads/testScene.ghostscene";

    public void Run()
    {
        var testWorld = World.Create();
        var testScene = new WorldNode(testWorld, "Test Scene");

        var entity1 = SceneGraphHelpers.CreateEntityNode(testScene, "entity 1");
        var entity2 = SceneGraphHelpers.CreateEntityNode(testScene, "entity 2");
        var entity3 = SceneGraphHelpers.CreateEntityNode(testScene, "entity 3");
        var entity4 = SceneGraphHelpers.CreateEntityNode(testScene, "entity 4");
        var entity5 = SceneGraphHelpers.CreateEntityNode(testScene, "entity 5");

        testWorld.SystemStorage.AddSystem<TestSystem>();

        SceneGraphHelpers.AttachChild(testScene, entity1, entity2);
        SceneGraphHelpers.AttachChild(testScene, entity1, entity3);
        SceneGraphHelpers.AttachChild(testScene, entity2, entity4);

        testScene.AddChild(entity1);
        testScene.AddChild(entity5);

        var createStream = new FileStream(_TEST_FILE_PATH, FileMode.Create, FileAccess.Write, FileShare.None);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            IgnoreReadOnlyProperties = true,
        };

        JsonSerializer.Serialize(createStream, testScene, options);

        createStream.Dispose();
        testWorld.Dispose();

        var readStream = new FileStream(_TEST_FILE_PATH, FileMode.Open, FileAccess.Read, FileShare.Read);

        var deserializedScene = JsonSerializer.Deserialize<WorldNode>(readStream, options) ?? throw new Exception("Deserialization failed.");
        deserializedScene.LoadAsync();
    }
}
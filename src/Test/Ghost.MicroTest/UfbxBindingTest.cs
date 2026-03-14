using Ghost.Test.Core;
using Ghost.Ufbx;

namespace Ghost.MicroTest;

internal class UfbxBindingTest : ITest
{
    private static ReadOnlySpan<byte> TestFilePath => "F:/c/Third Parties/ufbx/data/blender_340_z_up_7400_binary.fbx"u8;

    public void Setup()
    {
    }

    public void Run()
    {
        // Smoke-test LoadOpts heap-pointer shape (construct, set, read back, dispose)
        using var opts = new LoadOpts();
        opts.IgnoreAnimation = true;
        opts.IgnoreEmbedded = true;

        // Load scene using the safe high-level wrapper (no unsafe, no fixed blocks)
        using var scene = Scene.LoadFile(TestFilePath, opts);

        // Enumerate nodes using the wrapper's NodeList (ref struct, no allocation)
        for (var i = 0; i < scene.Nodes.Count; i++)
        {
            var node = scene.Nodes[i];
            if (node.IsRoot)
            {
                continue;
            }

            // node.Name is a string property — no manual ToString() needed
            Console.WriteLine($"Object: {node.Name}");

            if (node.HasMesh)
            {
                Console.WriteLine($"-> mesh with {node.Mesh.NumFaces} faces");
            }
        }

        // Find a node by name using the new instance method (no unsafe, no fixed)
        var rootNode = scene.FindNode("RootNode"u8);
        if (!rootNode.IsNull)
        {
            Console.WriteLine($"Found root node: {rootNode.Name}");
        }

        // Find a material by name
        var material = scene.FindMaterial("Material"u8);
        if (!material.IsNull)
        {
            Console.WriteLine($"Found material: {material.Name}");
            // Find a prop on the material's props using the instance method
            var prop = material.Props.FindProp("DiffuseColor"u8);
            if (!prop.IsNull)
            {
                Console.WriteLine($"  DiffuseColor prop type: {prop.Type}");
            }
        }

        // Find an anim stack
        var animStack = scene.FindAnimStack("Take 001"u8);
        if (!animStack.IsNull)
        {
            Console.WriteLine($"Found anim stack: {animStack.Name}");
        }

        Console.WriteLine("Done.");
    }

    public void Cleanup()
    {
    }
}

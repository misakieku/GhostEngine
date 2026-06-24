using Ghost.MicroTest.Core;
using Ghost.Ufbx;

namespace Ghost.MicroTest;

internal unsafe class UfbxBindingTest : ITest
{
    public void Setup()
    {
    }

    public void Run()
    {
        var load_Opts = new ufbx_load_opts();
        var error = new ufbx_error();

        var pScene = ufbx_scene.LoadFileLen("F:/c/Third Parties/ufbx/data/blender_340_z_up_7400_binary.fbx"u8, &load_Opts, &error);

        if (pScene == null)
        {
            Console.WriteLine(error.description.ToString());
        }

        for (var i = 0u; i < pScene->nodes.count; i++)
        {
            var node = pScene->nodes.data[i];
            if (node->is_root)
            {
                continue;
            }

            Console.WriteLine($"Object: {node->name}");

            if (node->mesh != null)
            {
                Console.WriteLine($"-> mesh with {node->mesh->num_faces} faces");
                Console.WriteLine($"-> mesh with positions: {node->local_transform.translation}");
            }

            for (var j = 0u; j < node->materials.count; j++)
            {
                var mat = node->materials.data[j];
                Console.WriteLine("-> material: " + mat->name);
                Console.WriteLine("   -> shader type: " + mat->shader_type);
                Console.WriteLine("   -> texture count: " + mat->textures.count);
            }
        }

        pScene->Dispose();
        Console.WriteLine("Done.");
    }

    public void Cleanup()
    {
    }
}

using Ghost.Test.Core;
using Ghost.Ufbx;
using System.Diagnostics;
using System.Text;
using static Ghost.Ufbx.Api;

namespace Ghost.MicroTest;

internal unsafe class UfbxBindingTest : ITest
{
    private static ReadOnlySpan<byte> TestFilePath => "F:/c/Third Parties/ufbx/data/blender_340_z_up_7400_binary.fbx"u8;

    public void Setup()
    {
    }

    public void Run()
    {
        ufbx_load_opts opts = default;
        ufbx_error error;
        ufbx_scene* pScene;

        var path = TestFilePath;
        fixed (byte* p = path)
        {
            pScene = ufbx_load_file((sbyte*)p, &opts, &error);
        }

        if (pScene == null)
        {
            Debug.Fail($"Failed to load scene: {Encoding.UTF8.GetString((byte*)error.description.data, (int)error.description.length)}");
        }

        for (nuint i = 0; i < pScene->nodes.count; i++)
        {
            var node = pScene->nodes.data[i];
            if (node->is_root)
            {
                continue;
            }

            Console.WriteLine($"Object: {Encoding.UTF8.GetString((byte*)node->name.data, (int)node->name.length)}");
            if (node->mesh != null)
            {
                Console.WriteLine($"-> mesh with {node->mesh->faces.count} faces");
            }
        }

        ufbx_free_scene(pScene);
    }

    public void Cleanup()
    {
    }
}

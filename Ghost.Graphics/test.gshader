shader "MyShader/Standard"
{
    properties
    {
        float4 color = float4(1, 1, 1, 1);
        texture2d texture1 = texture2d(black);
        texture2d texture2 = texture2d(white);
        texture2d texture3 = texture2d(grey);
        texture2d texture4 = texture2d(normal);
    }

    pipeline
    {
        ztest = less_equal;
        zwrite = on;
        cull = back;
        blend = opaque;
        color_mask = 0;
    }

    pass "Forward"
    {
        ms("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "MSMain");
        ps("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "PSMain");

        includes
        {
            "F:/csharp/GhostEngine/Ghost.Shader/BuiltIn/Common.hlsl";
        }
    }
}
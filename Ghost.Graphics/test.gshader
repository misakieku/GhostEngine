shader "MyShader/Standard"
{
    fallback("Ghost/Standard"); // This is a test comment.

    // Another comment.
    properties
    {
        global uint test;
        global texture2d global_texture;
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

    /*
        This is a
        multi-line comment.
    */

    pass "Forward"
    {
        vs("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "VSMain");
        ps("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "PSMain");

        includes
        {
            "F:/csharp/GhostEngine/Ghost.Shader/BuiltIn/Common.hlsl";
        }
    }

    pass "DepthOnly"
    {
        properties
        {
            float testProp = float(0.5);
        }

        vs("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "VSMain");
        ps("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "PSMain");

        includes
        {
            "F:/csharp/GhostEngine/Ghost.Shader/BuiltIn/Common.hlsl";
        }
    }
}
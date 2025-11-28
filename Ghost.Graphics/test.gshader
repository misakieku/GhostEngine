shader "MyShader/Standard"
{
    properties
    {
        float4 color = float4(1, 1, 1, 1);
        tex2d texture1 = tex2d(black);
        tex2d texture2 = tex2d(white);
        tex2d texture3 = tex2d(grey);
        tex2d texture4 = tex2d(normal);
    }

    pass "Forward"
    {
        pipeline
        {
            ztest = disable;
            zwrite = off;
            cull = off;
            blend = opaque;
            color_mask = 15;
        }

        ms("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "MSMain");
        ps("F:/csharp/GhostEngine/Ghost.Graphics/RenderPasses/ShaderCode.hlsl", "PSMain");
    }
}

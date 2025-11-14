shader "MyShader/Standard"
{
    properties
    {
        float4 color = float4(1, 1, 1, 1);
        tex2d_b texture1 = tex2d_b(black);
        tex2d_b texture2 = tex2d_b(white);
        tex2d_b texture3 = tex2d_b(grey);
        tex2d_b texture4 = tex2d_b(normal);
        uint vertexBufferIndex;
        uint indexBufferIndex;
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
    }
}
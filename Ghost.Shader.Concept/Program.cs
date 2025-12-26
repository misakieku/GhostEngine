using System.Diagnostics;

namespace Ghost.Shader.Concept;

/// <summary>
/// Mock implementations for demonstration
/// </summary>
internal class MockShaderCompiler : IShaderCompiler
{
    public IntPtr CompileVariant(ShaderVariantKey key, in KeywordSet keywords)
    {
        // Simulate compilation delay
        Thread.Sleep(10);
        return new IntPtr(key.GetHashCode());
    }
}

internal class MockPipelineLibrary : IPipelineLibrary
{
    public IntPtr GetOrCreatePipeline(in GraphicsPipelineKey key)
    {
        // Simulate PSO creation
        Thread.Sleep(5);
        return new IntPtr(key.GetHashCode());
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Ghost Shader Concept - High Performance Material System ===\n");

        // Initialize system
        var registry = ShaderKeywordRegistry.Instance;
        var compiler = new MockShaderCompiler();
        var pipelineLib = new MockPipelineLibrary();
        var batchRenderer = new MaterialBatchRenderer(compiler, pipelineLib);
        var materialPool = new MaterialPool();

        Console.WriteLine("1. Creating Keywords...");
        var globalHDR = registry.GetOrRegister("HDR", KeywordScope.Global);
        var globalShadows = registry.GetOrRegister("SHADOWS_ENABLED", KeywordScope.Global);
        var localAlphaTest = registry.GetOrRegister("ALPHA_TEST", KeywordScope.Local);
        var localNormalMap = registry.GetOrRegister("NORMAL_MAP", KeywordScope.Local);
        var localMetallic = registry.GetOrRegister("METALLIC_WORKFLOW", KeywordScope.Local);

        // Set global keywords
        GlobalKeywordState.Instance.EnableKeyword(globalHDR);
        GlobalKeywordState.Instance.EnableKeyword(globalShadows);
        Console.WriteLine($"   - Global Keywords: HDR, SHADOWS_ENABLED");
        Console.WriteLine($"   - Local Keywords: ALPHA_TEST, NORMAL_MAP, METALLIC_WORKFLOW\n");

        // Create shader program with multiple passes
        Console.WriteLine("2. Creating Shader Program with Multi-Pass...");
        var shaderProgram = new ShaderProgramBuilder()
            .WithName("StandardPBR")
            .AddPass("ForwardBase", RenderState.Default)
            .AddPass("ShadowCaster", new RenderState
            {
                CullMode = CullMode.Back,
                FillMode = FillMode.Solid,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareFunc = CompareFunction.LessEqual,
                ColorWriteMask = ColorWriteMask.None,
                Topology = PrimitiveTopology.TriangleList
            })
            .AddPass("DepthPrepass", new RenderState
            {
                CullMode = CullMode.Back,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                ColorWriteMask = ColorWriteMask.None,
                Topology = PrimitiveTopology.TriangleList
            })
            .DeclareKeywords(localAlphaTest, localNormalMap, localMetallic)
            .Build();

        Console.WriteLine($"   - Shader: {shaderProgram.Name} (ID: {shaderProgram.Id})");
        Console.WriteLine($"   - Passes: {shaderProgram.Passes.Length}");
        foreach (var pass in shaderProgram.Passes)
        {
            Console.WriteLine($"     * {pass.Name} (ID: {pass.PassId})");
        }
        Console.WriteLine();

        // Create materials with different configurations
        Console.WriteLine("3. Creating Material Instances...");
        var materials = new List<Material>();

        // Material 1: Basic opaque
        var mat1 = new Material(shaderProgram);
        mat1.SetVector4("_Color", 1.0f, 0.0f, 0.0f, 1.0f);
        mat1.SetFloat("_Metallic", 0.5f);
        mat1.SetFloat("_Roughness", 0.3f);
        mat1.EnableKeyword(localMetallic);
        materials.Add(mat1);
        Console.WriteLine($"   - Material 1: Red Metallic (Keywords: METALLIC_WORKFLOW)");

        // Material 2: Alpha tested
        var mat2 = new Material(shaderProgram);
        mat2.SetVector4("_Color", 0.0f, 1.0f, 0.0f, 0.5f);
        mat2.SetFloat("_Cutoff", 0.5f);
        mat2.EnableKeyword(localAlphaTest);
        materials.Add(mat2);
        Console.WriteLine($"   - Material 2: Green Alpha Test (Keywords: ALPHA_TEST)");

        // Material 3: Full featured
        var mat3 = new Material(shaderProgram);
        mat3.SetVector4("_Color", 0.0f, 0.0f, 1.0f, 1.0f);
        mat3.SetFloat("_Metallic", 1.0f);
        mat3.SetFloat("_Roughness", 0.1f);
        mat3.EnableKeyword(localMetallic);
        mat3.EnableKeyword(localNormalMap);
        materials.Add(mat3);
        Console.WriteLine($"   - Material 3: Blue Metallic + Normal Map (Keywords: METALLIC_WORKFLOW, NORMAL_MAP)");

        // Material 4: Override blend state for transparent pass
        var mat4 = new Material(shaderProgram);
        mat4.SetVector4("_Color", 1.0f, 1.0f, 0.0f, 0.7f);
        var transparentState = RenderState.Default;
        transparentState.BlendEnable = true;
        transparentState.SrcBlend = BlendFactor.SrcAlpha;
        transparentState.DestBlend = BlendFactor.InvSrcAlpha;
        mat4.SetPassRenderState(0, transparentState);
        materials.Add(mat4);
        Console.WriteLine($"   - Material 4: Yellow Transparent (Per-pass blend override)\n");

        // Demonstrate material property updates
        Console.WriteLine("4. Testing Material Property Updates...");
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            mat1.SetFloat("_Metallic", (float)Math.Sin(i * 0.01));
            mat1.SetVector4("_Color", 1.0f, 0.5f, 0.5f, 1.0f);
        }
        sw.Stop();
        Console.WriteLine($"   - 10,000 property updates: {sw.ElapsedMilliseconds}ms ({10000.0 / sw.ElapsedMilliseconds:F2} updates/ms)\n");

        // Demonstrate keyword toggling
        Console.WriteLine("5. Testing Keyword Toggle Performance...");
        sw.Restart();
        for (int i = 0; i < 10000; i++)
        {
            if (i % 2 == 0)
                mat3.EnableKeyword(localAlphaTest);
            else
                mat3.DisableKeyword(localAlphaTest);
        }
        sw.Stop();
        Console.WriteLine($"   - 10,000 keyword toggles: {sw.ElapsedMilliseconds}ms ({10000.0 / sw.ElapsedMilliseconds:F2} toggles/ms)\n");

        // Create draw commands
        Console.WriteLine("6. Creating Draw Commands...");
        var drawCommands = new List<DrawCommand>();
        var random = new Random(42);

        for (int i = 0; i < 1000; i++)
        {
            drawCommands.Add(new DrawCommand
            {
                Material = materials[random.Next(materials.Count)],
                PassIndex = random.Next(3), // Random pass
                VertexBuffer = new IntPtr(i * 1000),
                IndexBuffer = new IntPtr(i * 1000 + 500),
                IndexCount = 36,
                InstanceCount = 1,
                InstanceData = IntPtr.Zero
            });
        }
        Console.WriteLine($"   - Created {drawCommands.Count} draw commands\n");

        // Batch rendering
        Console.WriteLine("7. Batching Draw Calls...");
        sw.Restart();
        var batches = batchRenderer.BatchDrawCalls(drawCommands.ToArray());
        sw.Stop();
        Console.WriteLine($"   - Batched into {batches.Length} unique PSO states");
        Console.WriteLine($"   - Batching time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"   - Average batch size: {drawCommands.Count / (float)batches.Length:F2} draws/batch\n");

        // Show batch details
        Console.WriteLine("8. Batch Details:");
        int batchNum = 1;
        foreach (var batch in batches.Take(5))
        {
            Console.WriteLine($"   Batch {batchNum++}:");
            Console.WriteLine($"     - PSO Hash: 0x{batch.PipelineKey.GetHashCode():X8}");
            Console.WriteLine($"     - Draw calls: {batch.DrawCommands.Length}");
            Console.WriteLine($"     - Shader variant: 0x{batch.PipelineKey.VariantKey.KeywordHash:X16}");
        }
        if (batches.Length > 5)
            Console.WriteLine($"   ... and {batches.Length - 5} more batches\n");

        // Demonstrate variant warmup
        Console.WriteLine("9. Shader Variant Warmup (Async)...");
        var warmupConfigs = new KeywordSet[8];
        for (int i = 0; i < warmupConfigs.Length; i++)
        {
            warmupConfigs[i] = new KeywordSet();
            if ((i & 1) != 0) warmupConfigs[i].Enable(localAlphaTest);
            if ((i & 2) != 0) warmupConfigs[i].Enable(localNormalMap);
            if ((i & 4) != 0) warmupConfigs[i].Enable(localMetallic);
        }

        sw.Restart();
        var warmupTask = batchRenderer.WarmupVariantsAsync(shaderProgram, warmupConfigs);
        warmupTask.Wait();
        sw.Stop();
        Console.WriteLine($"   - Pre-compiled {warmupConfigs.Length} variants in {sw.ElapsedMilliseconds}ms\n");

        // Material cloning
        Console.WriteLine("10. Material Cloning...");
        var clonedMat = mat3.Clone();
        Console.WriteLine($"    - Cloned material 3");
        Console.WriteLine($"    - Original metallic: {(mat3.TryGetFloat("_Metallic", out var m1) ? m1 : 0)}");
        Console.WriteLine($"    - Clone metallic: {(clonedMat.TryGetFloat("_Metallic", out var m2) ? m2 : 0)}");
        clonedMat.SetFloat("_Metallic", 0.0f);
        Console.WriteLine($"    - After clone modification:");
        Console.WriteLine($"      Original: {(mat3.TryGetFloat("_Metallic", out var m3) ? m3 : 0)}");
        Console.WriteLine($"      Clone: {(clonedMat.TryGetFloat("_Metallic", out var m4) ? m4 : 0)}\n");

        // Material pooling
        Console.WriteLine("11. Material Pooling...");
        sw.Restart();
        for (int i = 0; i < 1000; i++)
        {
            var pooledMat = materialPool.Rent(shaderProgram);
            pooledMat.SetFloat("_Test", i);
            materialPool.Return(pooledMat);
        }
        sw.Stop();
        Console.WriteLine($"    - 1000 rent/return cycles: {sw.ElapsedMilliseconds}ms\n");

        // Cleanup
        Console.WriteLine("12. Cleanup...");
        foreach (var mat in materials)
        {
            mat.Dispose();
        }
        clonedMat.Dispose();
        materialPool.Clear();
        Console.WriteLine("    - All materials disposed\n");

        Console.WriteLine("=== Demo Complete ===");
        Console.WriteLine("\nKey Features Demonstrated:");
        Console.WriteLine("  ✓ Multi-pass shader support");
        Console.WriteLine("  ✓ Global and local keyword system");
        Console.WriteLine("  ✓ Shader variant compilation");
        Console.WriteLine("  ✓ Per-pass pipeline state overrides");
        Console.WriteLine("  ✓ Fast material property updates");
        Console.WriteLine("  ✓ Efficient draw call batching");
        Console.WriteLine("  ✓ Async variant warmup");
        Console.WriteLine("  ✓ Material cloning and pooling");
        Console.WriteLine("  ✓ Cache-friendly data structures");
    }
}

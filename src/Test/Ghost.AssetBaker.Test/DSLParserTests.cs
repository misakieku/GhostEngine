using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.ShaderParser;
using Ghost.DSL.ShaderParser.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLParserTests
{
    [TestMethod]
    public void Parse_ModuleWithInterfacesAndImplementations_Succeeds()
    {
        var dsl = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface shader IBSDF
    {
        static float3 Evaluate(in BSDFContext context, in SurfaceData surface, inout Payload payload);
    }

    export interface pipeline IShadow
    {
        static float EvaluateShadow(in ShadowContext context, inout Payload payload);
    }

    export closed interface shader IInternalEncoding
    {
        static uint Encode(SurfaceData surface);
    }
}
";
        var result = DSLShaderCompiler.ParseDSLDocument(dsl);
        Assert.IsTrue(result.IsSuccess, result.Message);

        var doc = result.Value;
        Assert.AreEqual(1, doc.Modules.Count);

        var module = doc.Modules[0];
        Assert.AreEqual("Ghost.Rendering.Interfaces", module.Name);
        Assert.AreEqual(3, module.Interfaces.Count);

        var ibsdf = module.Interfaces[0];
        Assert.AreEqual("IBSDF", ibsdf.Name);
        Assert.AreEqual(InterfaceScope.Shader, ibsdf.Scope);
        Assert.IsTrue(ibsdf.IsExported);
        Assert.IsFalse(ibsdf.IsClosed);
        StringAssert.Contains(ibsdf.Body, "Evaluate");

        var ishadow = module.Interfaces[1];
        Assert.AreEqual("IShadow", ishadow.Name);
        Assert.AreEqual(InterfaceScope.Pipeline, ishadow.Scope);
        Assert.IsTrue(ishadow.IsExported);
        Assert.IsFalse(ishadow.IsClosed);

        var ienc = module.Interfaces[2];
        Assert.AreEqual("IInternalEncoding", ienc.Name);
        Assert.AreEqual(InterfaceScope.Shader, ienc.Scope);
        Assert.IsTrue(ienc.IsExported);
        Assert.IsTrue(ienc.IsClosed);
    }

    [TestMethod]
    public void Parse_TemplateWithSlotsAndComposePasses_Succeeds()
    {
        var dsl = @"
module ""Ghost.Rendering.Lit""
{
    import ""Ghost.Rendering.Interfaces"";

    export template ""LitTemplate""
    {
        slot
        {
            IBSDF;
            IShadow = CSMPCFShadow;
            IFog = NoFog;
        }

        pass ""GBuffer""
        {
            compose
            {
                IBSDF;
            }

            mesh ""Shaders/GBuffer.hlsl"" : ""MeshMain"";
            pixel ""Shaders/GBuffer.hlsl"" : ""PixelMain"";
        }

        pass ""DeferredLighting""
        {
            compose
            {
                IBSDF;
                IShadow;
                IFog;
            }

            pixel ""Shaders/Deferred.hlsl"" : ""DeferredLightingMain"";
        }

        pass ""DepthOnly""
        {
            mesh ""Shaders/Depth.hlsl"" : ""DepthMain"";
        }
    }
}
";
        var result = DSLShaderCompiler.ParseDSLDocument(dsl);
        Assert.IsTrue(result.IsSuccess, result.Message);

        var doc = result.Value;
        Assert.AreEqual(1, doc.Modules.Count);

        var module = doc.Modules[0];
        Assert.AreEqual(1, module.Imports.Count);
        Assert.AreEqual("Ghost.Rendering.Interfaces", module.Imports[0].ModuleName);

        Assert.AreEqual(1, module.Templates.Count);
        var template = module.Templates[0];
        Assert.AreEqual("LitTemplate", template.Name);
        Assert.IsTrue(template.IsExported);

        Assert.AreEqual(3, template.Slots.Count);
        Assert.AreEqual("IBSDF", template.Slots[0].InterfaceName);
        Assert.IsNull(template.Slots[0].DefaultImplementationName);

        Assert.AreEqual("IShadow", template.Slots[1].InterfaceName);
        Assert.AreEqual("CSMPCFShadow", template.Slots[1].DefaultImplementationName);

        Assert.AreEqual("IFog", template.Slots[2].InterfaceName);
        Assert.AreEqual("NoFog", template.Slots[2].DefaultImplementationName);

        Assert.AreEqual(3, template.Passes.Count);

        var gbuffer = template.Passes[0];
        Assert.AreEqual("GBuffer", gbuffer.Name);
        Assert.IsNotNull(gbuffer.Compose);
        Assert.AreEqual(1, gbuffer.Compose.Interfaces.Count);
        Assert.AreEqual("IBSDF", gbuffer.Compose.Interfaces[0]);
        Assert.AreEqual(2, gbuffer.ShaderEntries.Count);

        var deferred = template.Passes[1];
        Assert.AreEqual("DeferredLighting", deferred.Name);
        Assert.IsNotNull(deferred.Compose);
        Assert.AreEqual(3, deferred.Compose.Interfaces.Count);
        CollectionAssert.AreEqual(new[] { "IBSDF", "IShadow", "IFog" }, deferred.Compose.Interfaces);

        var depth = template.Passes[2];
        Assert.AreEqual("DepthOnly", depth.Name);
        Assert.IsNull(depth.Compose);
    }

    [TestMethod]
    public void Parse_DerivedShaderWithPayloadImplementationsAndBind_Succeeds()
    {
        var dsl = @"
module ""Game.Materials""
{
    import ""Ghost.Rendering.Lit"";

    export shader ""Game/CustomLit"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        payload
        {
            float directSpecularStrength;
            float3 customAnisotropyTangent;
        }

        implementation CustomSurface : ISurface
        {
            static SurfaceData EvaluateSurface(in V2F input, inout Payload payload)
            {
                SurfaceData s = (SurfaceData)0;
                payload.directSpecularStrength = 2.0f;
                return s;
            }
        }

        implementation GGXBSDF : IBSDF
        {
            static float3 Evaluate(in BSDFContext context, in SurfaceData surface, inout Payload payload)
            {
                return surface.albedo * payload.directSpecularStrength;
            }
        }

        bind
        {
            ISurface = CustomSurface;
            IBSDF = GGXBSDF;
        }
    }
}
";
        var result = DSLShaderCompiler.ParseDSLDocument(dsl);
        Assert.IsTrue(result.IsSuccess, result.Message);

        var doc = result.Value;
        Assert.AreEqual(1, doc.Modules.Count);

        var module = doc.Modules[0];
        Assert.AreEqual(1, module.Shaders.Count);

        var shader = module.Shaders[0];
        Assert.AreEqual("Game/CustomLit", shader.Name);
        Assert.AreEqual("Ghost.Rendering.Lit.LitTemplate", shader.TemplateName);
        Assert.IsTrue(shader.IsExported);

        Assert.IsNotNull(shader.Payload);
        StringAssert.Contains(shader.Payload.Body, "directSpecularStrength");
        StringAssert.Contains(shader.Payload.Body, "customAnisotropyTangent");

        Assert.AreEqual(2, shader.Implementations.Count);
        Assert.AreEqual("CustomSurface", shader.Implementations[0].Name);
        Assert.AreEqual("ISurface", shader.Implementations[0].InterfaceName);
        StringAssert.Contains(shader.Implementations[0].Body, "EvaluateSurface");

        Assert.AreEqual("GGXBSDF", shader.Implementations[1].Name);
        Assert.AreEqual("IBSDF", shader.Implementations[1].InterfaceName);

        Assert.IsNotNull(shader.Bind);
        Assert.AreEqual(2, shader.Bind.Bindings.Count);
        Assert.AreEqual("ISurface", shader.Bind.Bindings[0].InterfaceName);
        Assert.AreEqual("CustomSurface", shader.Bind.Bindings[0].ImplementationName);
        Assert.AreEqual("IBSDF", shader.Bind.Bindings[1].InterfaceName);
        Assert.AreEqual("GGXBSDF", shader.Bind.Bindings[1].ImplementationName);
    }

    [TestMethod]
    public void Parse_ShaderProjectDeclaration_Succeeds()
    {
        var dsl = @"
shader_project ""TestGame""
{
    module ""Ghost.Rendering"";
    module ""Game.Materials"";
    module ""Game.ExperimentalShadows"";

    target ""D3D12_SM66"";
}
";
        var result = DSLShaderCompiler.ParseDSLDocument(dsl);
        Assert.IsTrue(result.IsSuccess, result.Message);

        var doc = result.Value;
        Assert.AreEqual(1, doc.Projects.Count);

        var proj = doc.Projects[0];
        Assert.AreEqual("TestGame", proj.Name);
        Assert.AreEqual(3, proj.Modules.Count);
        CollectionAssert.AreEqual(new[] { "Ghost.Rendering", "Game.Materials", "Game.ExperimentalShadows" }, proj.Modules);
        Assert.AreEqual(1, proj.Targets.Count);
        Assert.AreEqual("D3D12_SM66", proj.Targets[0]);
    }

    [TestMethod]
    public void Parse_TopLevelStandaloneDeclarations_Succeeds()
    {
        var dsl = @"
import ""Ghost.Rendering.Interfaces"";

export template ""LitTemplate""
{
    slot
    {
        IBSDF;
        IShadow = CSMPCFShadow;
    }

    pass ""GBuffer""
    {
        compose
        {
            IBSDF;
        }
        mesh ""Shaders/GBuffer.hlsl"" : ""MeshMain"";
        pixel ""Shaders/GBuffer.hlsl"" : ""PixelMain"";
    }
}

export shader ""Game/Lit"" : ""LitTemplate""
{
    payload
    {
        float customRoughnessScale;
    }

    implementation GGX : IBSDF
    {
        static float3 Evaluate(in BSDFContext ctx, in SurfaceData s, inout Payload p)
        {
            return s.albedo;
        }
    }

    bind
    {
        IBSDF = GGX;
    }
}
";
        var result = DSLShaderCompiler.ParseDSLDocument(dsl);
        Assert.IsTrue(result.IsSuccess, result.Message);

        var doc = result.Value;
        Assert.AreEqual(1, doc.Imports.Count);
        Assert.AreEqual("Ghost.Rendering.Interfaces", doc.Imports[0].ModuleName);

        Assert.AreEqual(1, doc.Templates.Count);
        Assert.AreEqual("LitTemplate", doc.Templates[0].Name);
        Assert.AreEqual(2, doc.Templates[0].Slots.Count);
        Assert.AreEqual(1, doc.Templates[0].Passes.Count);

        Assert.AreEqual(1, doc.Shaders.Count);
        Assert.AreEqual("Game/Lit", doc.Shaders[0].Name);
        Assert.AreEqual("LitTemplate", doc.Shaders[0].TemplateName);
        Assert.IsNotNull(doc.Shaders[0].Payload);
        Assert.AreEqual(1, doc.Shaders[0].Implementations.Count);
        Assert.IsNotNull(doc.Shaders[0].Bind);
        Assert.AreEqual(1, doc.Shaders[0].Bind.Bindings.Count);
    }
}

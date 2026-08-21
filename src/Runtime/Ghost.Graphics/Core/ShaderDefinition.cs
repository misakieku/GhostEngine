using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.Core;

public struct ShaderProperty
{
    public string name;
    public Type type;
    public int size;
}

public class ShaderDefinitionBuilder
{
    private readonly ShaderDefinition _shaderDefinition = new ShaderDefinition();

    public static ShaderDefinitionBuilder New()
    {
        return new ShaderDefinitionBuilder();
    }

    public unsafe ShaderDefinitionBuilder WithProperty<T>(string name)
        where T : unmanaged
    {
        _shaderDefinition.Properties.Add(new ShaderProperty
        {
            name = name,
            type = typeof(T),
            size = sizeof(T)
        });

        return this;
    }

    public ShaderDefinitionBuilder WithPass()
    {
        return this;
    }

    public ShaderDefinitionBuilder WithStruct<T>()
        where T : unmanaged
    {
        _shaderDefinition.Structs.Add(typeof(T));
        return this;
    }

    public ShaderDefinitionBuilder WithInclude(string includePath)
    {
        _shaderDefinition.Includes.Add(includePath);
        return this;
    }

    public ShaderDefinition Build()
    {
        return _shaderDefinition;
    }
}

internal interface IShaderTemplate
{
    ShaderDefinition Build();
}

public abstract class LitTemplate : IShaderTemplate
{
    public struct SurfaceData
    {
        public float4 baseColor;
        public float3 normal;
        public float roughness;
        public float metallic;
        public float ambientOcclusion;
    }

    public struct BSDFData
    {
        public float3 diffuse;
        public float3 specular;
        public float3 emission;
    }

    public interface ISurface
    {
        SurfaceData GetSurfaceData();
    }

    public interface IBSDF
    {
        BSDFData GetBSDFData(in SurfaceData surfaceData);
        float3 Evaluate(in SurfaceData surfaceData, in BSDFData bsdfData);
    }

    public ShaderDefinition Build()
    {
        return ShaderDefinitionBuilder.New()
            .WithStruct<SurfaceData>()
            .WithStruct<BSDFData>()
            .WithProperty<float4>("BaseColor")
            .WithProperty<float>("Roughness")
            .WithProperty<float>("Metallic")
            .Build();
    }
}

public record ShaderDefinition
{
    public List<ShaderProperty> Properties
    {
        get;
    } = new List<ShaderProperty>();

    public List<Type> Structs
    {
        get;
    } = new List<Type>();

    public List<string> Includes
    {
        get;
    } = new List<string>();
}
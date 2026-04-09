using Ghost.DSL.Generator;
using Ghost.DSL.ShaderCompiler;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

//ShaderStructGenerator.GenerateHLSL([typeof(TestStruct), typeof(TestEnum), typeof(TestEnumFlags)], PackingRules.Exact, "C:/Users/Misaki/Downloads/Archive/Test.cs.hlsl");

//return;
#if true
var result =  DSLShaderCompiler.CompileComputeShader("F:\\csharp\\GhostEngine\\src\\Runtime\\Ghost.Graphics\\TestCompute.gcomp");
if (result.IsFailure)
{
    Console.WriteLine(result.Message);
    return;
}

#endif

public struct TestStruct
{
    public int A;
    public float B;
    public Vector3 C;
    public float3x4 D;
}

public enum TestEnum
{
    First,
    Second,
    Third
}

public enum TestEnumFlags
{
    None = 0,
    First = 1 << 0,
    Second = 1 << 1,
    Third = 1 << 2,
}
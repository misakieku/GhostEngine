using Ghost.SDL.Compiler;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

//ShaderStructGenerator.GenerateHLSL([typeof(TestStruct), typeof(TestEnum), typeof(TestEnumFlags)], PackingRules.Exact, "C:/Users/Misaki/Downloads/Archive/Test.cs.hlsl");

//return;

var source = File.ReadAllText("F:/csharp/GhostEngine/Ghost.Graphics/test.gshader");

var lexer = new Lexer(source);
var stream = new TokenStream(lexer.Tokenize());
var shaderInfo = SDLCompiler.ParseShaders(stream);
var model = SDLCompiler.SemanticAnalysis(shaderInfo[0], out var errors);

foreach (var error in errors)
{
    Console.WriteLine(error);
}

if (errors.Count != 0)
{
    return;
}

if (model == null)
{
    Console.WriteLine("Failed to compile shader due to errors.");
    return;
}

var descriptor = SDLCompiler.ResolveShader(model);
SDLCompiler.GenerateShader(descriptor, "C:/Users/Misaki/Downloads/Archive");

Console.WriteLine("Shader compiled successfully:");


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
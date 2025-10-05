using Ghost.Shader;

var source = File.ReadAllText("F:\\csharp\\GhostEngine\\Ghost.Graphics\\test.ghostshader");
var lexer = new Lexer(source);

//foreach (var token in lexer.Tokenize())
//{
//    Console.WriteLine($"{token.type} : '{token.lexeme}' at line {token.line}");
//}

var stream = new TokenStream(lexer.Tokenize().ToArray());
var shaderInfo = ShaderCompiler.ParseShaders(stream);
var model = ShaderCompiler.SemanticAnalysis(shaderInfo[0], out var errors);

foreach (var error in errors)
{
    Console.WriteLine(error);
}
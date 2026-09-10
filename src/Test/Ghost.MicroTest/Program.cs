using Ghost.MicroTest;
using Ghost.MicroTest.Core;

var bytes = System.IO.File.ReadAllBytes(@"F:\csharp\GhostEngine\src\Test\TestGame\bin\Debug\net10.0\Assets\pack_0000.pack");
TestRunner.Run<DslCompilerTest>();
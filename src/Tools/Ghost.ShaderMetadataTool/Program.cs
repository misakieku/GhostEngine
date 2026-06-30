using Ghost.Core;
using Ghost.DSL.Models;
using System.Text.Json;

namespace Ghost.ShaderMetadataTool;

public class Program
{
    public static void Main(string[] args)
    {
        Logger.Impl.OnLogAdded += static (log) => Console.WriteLine($"[{log.Level}] {log.Message}");

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Ghost.ShaderMetadataTool <input_files.txt> <output.json>");
            return;
        }

        var inputFileList = args[0];
        var outputFile = args[1];

        if (!File.Exists(inputFileList))
        {
            Console.WriteLine($"Input file list not found: {inputFileList}");
            return;
        }

        var files = File.ReadAllLines(inputFileList);

        var manifest = new ShaderMetadata();

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                continue;
            }

            var text = File.ReadAllText(file);

            Utility.ExtractShaderProperties(manifest, text);
            Utility.GenerateHLSLTypes(manifest, text);
        }

        foreach (var kvp in manifest.VirtualShader.Keys.ToList())
        {
            var content = manifest.VirtualShader[kvp];
            var macro = System.Text.RegularExpressions.Regex.Replace(kvp.ToUpperInvariant(), @"[^A-Z0-9_]", "_");
            manifest.VirtualShader[kvp] = $"#ifndef {macro}\n#define {macro}\n{content}\n#endif // {macro}\n";
        }

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(outputFile, json);
        Console.WriteLine($"Extracted {manifest.VirtualShader.Count} shader properties to {outputFile}");
        Console.WriteLine($"Generated {manifest.VirtualShader.Count} shader codes.");
    }
}

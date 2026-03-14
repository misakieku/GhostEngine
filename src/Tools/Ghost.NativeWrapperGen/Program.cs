using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Emit;
using Ghost.NativeWrapperGen.Parsing;
using System.Text.Json;

namespace Ghost.NativeWrapperGen;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            var config = LoadConfig(options.ConfigPath);

            var parser = new BindingParser();
            var library = parser.Parse(options.InputPath, config);

            var outputDirectory = options.OutputPath;
            Directory.CreateDirectory(outputDirectory);
            CleanupGeneratedFiles(outputDirectory);

            var emitter = new WrapperGeneratorEmitter();
            foreach (var file in emitter.Emit(library, config))
            {
                var destination = Path.Combine(outputDirectory, file.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(destination, file.Content);
                Console.WriteLine($"Generated {destination}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static WrapperConfig LoadConfig(string configPath)
    {
        var json = File.ReadAllText(configPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        var config = JsonSerializer.Deserialize<WrapperConfig>(json, options);
        return config ?? throw new InvalidOperationException($"Failed to parse config '{configPath}'.");
    }

    private static void CleanupGeneratedFiles(string outputDirectory)
    {
        foreach (var file in Directory.GetFiles(outputDirectory, "*.nativegen.cs", SearchOption.AllDirectories))
        {
            File.Delete(file);
        }
    }
}

internal sealed class CliOptions
{
    public required string ConfigPath { get; init; }
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }

    public static CliOptions Parse(string[] args)
    {
        string? configPath = null;
        string? inputPath = null;
        string? outputPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                    configPath = GetValue(args, ref i, "--config");
                    break;
                case "--input":
                    inputPath = GetValue(args, ref i, "--input");
                    break;
                case "--output":
                    outputPath = GetValue(args, ref i, "--output");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'. Expected --config, --input, --output.");
            }
        }

        if (string.IsNullOrWhiteSpace(configPath) || string.IsNullOrWhiteSpace(inputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Usage: Ghost.NativeWrapperGen --config <path> --input <dir> --output <dir>");
        }

        return new CliOptions
        {
            ConfigPath = Path.GetFullPath(configPath),
            InputPath = Path.GetFullPath(inputPath),
            OutputPath = Path.GetFullPath(outputPath),
        };
    }

    private static string GetValue(string[] args, ref int index, string optionName)
    {
        index++;
        if (index >= args.Length)
        {
            throw new ArgumentException($"Missing value for '{optionName}'.");
        }

        return args[index];
    }
}

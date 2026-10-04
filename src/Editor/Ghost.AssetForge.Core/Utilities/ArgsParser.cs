namespace Ghost.AssetForge.Core.Utilities;

internal static class ArgsParser
{
    public static Dictionary<string, string> Parse(IReadOnlyList<string> args)
    {
        var result = new Dictionary<string, string>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                var key = arg.Substring(2);
                var value = string.Empty;
                if (i + 1 < args.Count && !args[i + 1].StartsWith("--"))
                {
                    value = args[i + 1];
                    i++;
                }

                result[key] = value;
            }
        }

        return result;
    }
}

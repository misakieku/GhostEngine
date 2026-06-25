using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Ghost.AssetBaker;

public static class AppTheme
{
    public static void Register(Func<AppThemeBuilder, AppThemeBuilder> configure)
    {
        var builder = new AppThemeBuilder();
        configure(builder);
        builder.Build();
    }
}

public class AppThemeBuilder
{
    private readonly Dictionary<string, (string Light, string Dark, string HighContrast)> _brushes = new();

    public AppThemeBuilder Add(string key, string light, string dark, string highContrast)
    {
        _brushes[key] = (light, dark, highContrast);
        return this;
    }

    public void Build()
    {
        var app = Application.Current;
        if (app == null) return;

        var themeDicts = app.Resources.ThemeDictionaries;

        // Get or create Light dictionary
        if (!themeDicts.TryGetValue("Light", out var lightObj) || lightObj is not ResourceDictionary lightDict)
        {
            lightDict = new ResourceDictionary();
            themeDicts["Light"] = lightDict;
        }

        // Get or create Dark (Default) dictionary
        if (!themeDicts.TryGetValue("Default", out var darkObj) || darkObj is not ResourceDictionary darkDict)
        {
            darkDict = new ResourceDictionary();
            themeDicts["Default"] = darkDict;
        }

        // Get or create HighContrast dictionary
        if (!themeDicts.TryGetValue("HighContrast", out var hcObj) || hcObj is not ResourceDictionary hcDict)
        {
            hcDict = new ResourceDictionary();
            themeDicts["HighContrast"] = hcDict;
        }

        foreach (var pair in _brushes)
        {
            var key = pair.Key;
            var val = pair.Value;

            lightDict[key] = CreateBrush(val.Light);
            darkDict[key] = CreateBrush(val.Dark);
            hcDict[key] = CreateHCBrush(val.HighContrast);
        }
    }

    private static Brush CreateBrush(string colorHex)
    {
        if (colorHex.StartsWith('#'))
        {
            return new SolidColorBrush(ParseHexColor(colorHex));
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private static Brush CreateHCBrush(string value)
    {
        if (value.StartsWith('#'))
        {
            return new SolidColorBrush(ParseHexColor(value));
        }
        if (Application.Current.Resources.TryGetValue(value, out var res) && res is Brush brush)
        {
            return brush;
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private static Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromArgb(
                255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }
        if (hex.Length == 8)
        {
            return Color.FromArgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16)
            );
        }
        throw new ArgumentException($"Invalid hex color: {hex}");
    }
}

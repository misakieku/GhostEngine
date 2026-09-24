using System;

namespace Ghost.Generator.Templates
{
    public sealed class TemplatePropertyDef
    {
        public string type;
        public string name;
        public string? defaultValue;

        public TemplatePropertyDef(string type, string name, string? defaultValue = null)
        {
            this.type = type;
            this.name = name;
            this.defaultValue = defaultValue;
        }
    }

    public static class BuiltInTemplateProperties
    {
        public static readonly TemplatePropertyDef[] Lit = new[]
        {
            new TemplatePropertyDef("bool", "alphaClip", "false"),
            new TemplatePropertyDef("float", "alphaClipThreshold", "0.5"),
            new TemplatePropertyDef("float4", "doubleSidedConstants", "float4(1.0, 1.0, 1.0, 0.0)"),
        };

        public static readonly TemplatePropertyDef[] Unlit = new[]
        {
            new TemplatePropertyDef("bool", "alphaClip", "false"),
            new TemplatePropertyDef("float", "alphaClipThreshold", "0.5"),
            new TemplatePropertyDef("float4", "doubleSidedConstants", "float4(1.0, 1.0, 1.0, 0.0)"),
        };

        public static readonly TemplatePropertyDef[] Sky = new[]
        {
            new TemplatePropertyDef("float4", "skyTint", "float4(1.0, 1.0, 1.0, 1.0)"),
            new TemplatePropertyDef("float", "exposure", "1.0"),
        };

        public static readonly TemplatePropertyDef[] UI = new[]
        {
            new TemplatePropertyDef("float4", "color", "float4(1.0, 1.0, 1.0, 1.0)"),
            new TemplatePropertyDef("uint", "mainTex", "0"),
        };

        public static bool TryGetBaseProperties(string templateName, out TemplatePropertyDef[] properties)
        {
            if (string.Equals(templateName, "Lit", StringComparison.OrdinalIgnoreCase))
            {
                properties = Lit;
                return true;
            }

            if (string.Equals(templateName, "Unlit", StringComparison.OrdinalIgnoreCase))
            {
                properties = Unlit;
                return true;
            }

            if (string.Equals(templateName, "Sky", StringComparison.OrdinalIgnoreCase))
            {
                properties = Sky;
                return true;
            }

            if (string.Equals(templateName, "UI", StringComparison.OrdinalIgnoreCase))
            {
                properties = UI;
                return true;
            }

            properties = Array.Empty<TemplatePropertyDef>();
            return false;
        }
    }
}

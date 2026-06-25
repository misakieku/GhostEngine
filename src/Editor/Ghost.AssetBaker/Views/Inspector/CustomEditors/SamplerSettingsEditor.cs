using System;
using Ghost.AssetBaker.Attributes;
using Ghost.AssetBaker.Bakers;
using Microsoft.UI.Reactor;
using static Microsoft.UI.Reactor.Factories;
using Microsoft.UI.Reactor.Core;

namespace Ghost.AssetBaker.Views.Inspector.CustomEditors;

[CustomEditor(typeof(TextureBakeSettings.SamplerSettings))]
public class SamplerSettingsEditor : ICustomEditor
{
    public Element Draw(object target, Action<object> onUpdate)
    {
        var settings = (TextureBakeSettings.SamplerSettings)target;

        return Border(
            FlexColumn(
                BodyStrong("Custom Sampler Editor").Foreground(Theme.AccentText),
                Caption("This UI is fully drawn by a Custom Editor, ignoring standard properties!").Margin(bottom: 12),
                
                FlexRow(
                    Body("Max Size:").VAlign(Microsoft.UI.Xaml.VerticalAlignment.Center),
                    ComboBox(Enum.GetNames(typeof(TextureSize)), 
                        Array.IndexOf(Enum.GetValues(typeof(TextureSize)), settings.MaxSize), 
                        idx => 
                        {
                            settings.MaxSize = (TextureSize)Enum.GetValues(typeof(TextureSize)).GetValue(idx);
                            onUpdate(settings);
                        }).Margin(left: 8)
                ),
                
                Button("Reset Defaults", () => 
                {
                    onUpdate(new TextureBakeSettings.SamplerSettings());
                }).Margin(top: 8)
            )
        ).Padding(12).Background(Theme.ControlFillSecondary);
    }
}

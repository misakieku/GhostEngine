using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;

namespace Ghost.Editor.Utilities;

public class ReflectionBinding
{
    private void RefreshField(FieldInfo field, FrameworkElement control, object source)
    {
        var value = field.GetValue(source);

        switch (control)
        {
            case TextBox tb:
                tb.Text = value?.ToString();
                break;
            case NumberBox nb when value is double d:
                nb.Value = d;
                break;
                // Add more controls...
        }
    }

    public void StartPollingField(FieldInfo field, FrameworkElement control, object component)
    {
        var lastValue = field.GetValue(component);

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };

        timer.Tick += (_, _) =>
        {
            var currentValue = field.GetValue(component);
            if (!Equals(currentValue, lastValue))
            {
                RefreshField(field, control, component);
                lastValue = currentValue;
            }
        };

        timer.Start();
    }
}

using Ghost.Engine.Utilities;
using Microsoft.UI.Xaml.Data;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;

namespace Ghost.Editor.Utilities.Converters;

public partial class Vector3ToQuaternionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float3 vector)
        {
            return quaternion.EulerXYZ(vector);
        }

        throw new ArgumentException($"Value must be of type {typeof(float3)}.", nameof(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is quaternion qua)
        {
            return math.EulerXYZ(qua);
        }

        throw new ArgumentException($"Value must be of type {typeof(quaternion)}.", nameof(value));
    }
}
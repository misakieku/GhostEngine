using Ghost.Engine.Utilities;
using Microsoft.UI.Xaml.Data;
using System.Numerics;

namespace Ghost.Editor.Utilities.Converters;

public partial class Vector3ToQuaternionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Vector3 vector)
        {
            return Quaternion.CreateFromYawPitchRoll(vector.Y, vector.X, vector.Z);
        }

        throw new ArgumentException("Value must be of type System.Numerics.Vector3.", nameof(value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Quaternion quaternion)
        {
            return VectorUtility.CreateFromQuaternion(quaternion);
        }
        throw new ArgumentException("Value must be of type System.Numerics.Quaternion.", nameof(value));
    }
}
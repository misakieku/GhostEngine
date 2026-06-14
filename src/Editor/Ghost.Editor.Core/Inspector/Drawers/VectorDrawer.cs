using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.Utilities;
using Microsoft.UI.Xaml;

using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Inspector.Drawers;


[CustomPropertyDrawer(typeof(float2))]
public sealed class Float2Drawer : PropertyDrawer<float2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float2> node)
    {
        var field = new Float2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(float3))]
public sealed class Float3Drawer : PropertyDrawer<float3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float3> node)
    {
        var field = new Float3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(float4))]
public sealed class Float4Drawer : PropertyDrawer<float4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float4> node)
    {
        var field = new Float4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(double2))]
public sealed class Double2Drawer : PropertyDrawer<double2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double2> node)
    {
        var field = new Double2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(double3))]
public sealed class Double3Drawer : PropertyDrawer<double3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double3> node)
    {
        var field = new Double3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(double4))]
public sealed class Double4Drawer : PropertyDrawer<double4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double4> node)
    {
        var field = new Double4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(int2))]
public sealed class Int2Drawer : PropertyDrawer<int2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int2> node)
    {
        var field = new Int2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(int3))]
public sealed class Int3Drawer : PropertyDrawer<int3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int3> node)
    {
        var field = new Int3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(int4))]
public sealed class Int4Drawer : PropertyDrawer<int4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int4> node)
    {
        var field = new Int4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(uint2))]
public sealed class Uint2Drawer : PropertyDrawer<uint2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint2> node)
    {
        var field = new Uint2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(uint3))]
public sealed class Uint3Drawer : PropertyDrawer<uint3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint3> node)
    {
        var field = new Uint3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    
[CustomPropertyDrawer(typeof(uint4))]
public sealed class Uint4Drawer : PropertyDrawer<uint4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint4> node)
    {
        var field = new Uint4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);

        return field;
    }
}
    

[CustomPropertyDrawer(typeof(float2x2))]
public sealed class Float2x2FieldDrawer : PropertyDrawer<float2x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float2x2> node)
    {
        var field = new Float2x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double2x2))]
public sealed class Double2x2FieldDrawer : PropertyDrawer<double2x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double2x2> node)
    {
        var field = new Double2x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int2x2))]
public sealed class Int2x2FieldDrawer : PropertyDrawer<int2x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int2x2> node)
    {
        var field = new Int2x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint2x2))]
public sealed class Uint2x2FieldDrawer : PropertyDrawer<uint2x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint2x2> node)
    {
        var field = new Uint2x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float2x3))]
public sealed class Float2x3FieldDrawer : PropertyDrawer<float2x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float2x3> node)
    {
        var field = new Float2x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double2x3))]
public sealed class Double2x3FieldDrawer : PropertyDrawer<double2x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double2x3> node)
    {
        var field = new Double2x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int2x3))]
public sealed class Int2x3FieldDrawer : PropertyDrawer<int2x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int2x3> node)
    {
        var field = new Int2x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint2x3))]
public sealed class Uint2x3FieldDrawer : PropertyDrawer<uint2x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint2x3> node)
    {
        var field = new Uint2x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float2x4))]
public sealed class Float2x4FieldDrawer : PropertyDrawer<float2x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float2x4> node)
    {
        var field = new Float2x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double2x4))]
public sealed class Double2x4FieldDrawer : PropertyDrawer<double2x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double2x4> node)
    {
        var field = new Double2x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int2x4))]
public sealed class Int2x4FieldDrawer : PropertyDrawer<int2x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int2x4> node)
    {
        var field = new Int2x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint2x4))]
public sealed class Uint2x4FieldDrawer : PropertyDrawer<uint2x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint2x4> node)
    {
        var field = new Uint2x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float3x2))]
public sealed class Float3x2FieldDrawer : PropertyDrawer<float3x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float3x2> node)
    {
        var field = new Float3x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double3x2))]
public sealed class Double3x2FieldDrawer : PropertyDrawer<double3x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double3x2> node)
    {
        var field = new Double3x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int3x2))]
public sealed class Int3x2FieldDrawer : PropertyDrawer<int3x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int3x2> node)
    {
        var field = new Int3x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint3x2))]
public sealed class Uint3x2FieldDrawer : PropertyDrawer<uint3x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint3x2> node)
    {
        var field = new Uint3x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float3x3))]
public sealed class Float3x3FieldDrawer : PropertyDrawer<float3x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float3x3> node)
    {
        var field = new Float3x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double3x3))]
public sealed class Double3x3FieldDrawer : PropertyDrawer<double3x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double3x3> node)
    {
        var field = new Double3x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int3x3))]
public sealed class Int3x3FieldDrawer : PropertyDrawer<int3x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int3x3> node)
    {
        var field = new Int3x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint3x3))]
public sealed class Uint3x3FieldDrawer : PropertyDrawer<uint3x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint3x3> node)
    {
        var field = new Uint3x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float3x4))]
public sealed class Float3x4FieldDrawer : PropertyDrawer<float3x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float3x4> node)
    {
        var field = new Float3x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double3x4))]
public sealed class Double3x4FieldDrawer : PropertyDrawer<double3x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double3x4> node)
    {
        var field = new Double3x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int3x4))]
public sealed class Int3x4FieldDrawer : PropertyDrawer<int3x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int3x4> node)
    {
        var field = new Int3x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint3x4))]
public sealed class Uint3x4FieldDrawer : PropertyDrawer<uint3x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint3x4> node)
    {
        var field = new Uint3x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float4x2))]
public sealed class Float4x2FieldDrawer : PropertyDrawer<float4x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float4x2> node)
    {
        var field = new Float4x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double4x2))]
public sealed class Double4x2FieldDrawer : PropertyDrawer<double4x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double4x2> node)
    {
        var field = new Double4x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int4x2))]
public sealed class Int4x2FieldDrawer : PropertyDrawer<int4x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int4x2> node)
    {
        var field = new Int4x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint4x2))]
public sealed class Uint4x2FieldDrawer : PropertyDrawer<uint4x2>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint4x2> node)
    {
        var field = new Uint4x2Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float4x3))]
public sealed class Float4x3FieldDrawer : PropertyDrawer<float4x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float4x3> node)
    {
        var field = new Float4x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double4x3))]
public sealed class Double4x3FieldDrawer : PropertyDrawer<double4x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double4x3> node)
    {
        var field = new Double4x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int4x3))]
public sealed class Int4x3FieldDrawer : PropertyDrawer<int4x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int4x3> node)
    {
        var field = new Int4x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint4x3))]
public sealed class Uint4x3FieldDrawer : PropertyDrawer<uint4x3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint4x3> node)
    {
        var field = new Uint4x3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
[CustomPropertyDrawer(typeof(float4x4))]
public sealed class Float4x4FieldDrawer : PropertyDrawer<float4x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float4x4> node)
    {
        var field = new Float4x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(double4x4))]
public sealed class Double4x4FieldDrawer : PropertyDrawer<double4x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<double4x4> node)
    {
        var field = new Double4x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(int4x4))]
public sealed class Int4x4FieldDrawer : PropertyDrawer<int4x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<int4x4> node)
    {
        var field = new Int4x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
        
[CustomPropertyDrawer(typeof(uint4x4))]
public sealed class Uint4x4FieldDrawer : PropertyDrawer<uint4x4>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<uint4x4> node)
    {
        var field = new Uint4x4Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.BindTwoWay(node);
        return field;
    }
}
            
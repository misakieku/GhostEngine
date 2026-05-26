using Ghost.Core;
using Ghost.Editor.Core.Inspector;
using Ghost.Engine.Streaming;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ghost.Editor.Core.SceneGraph;

public class HandlePropertyNode<T> : PropertyNode<Handle<T>> where T : unmanaged
{
    public Guid AssetGuid { get; private set; } = Guid.Empty;
    public long ExpectedHandleValue { get; private set; }

    public HandlePropertyNode(PropertyDescriptor descriptor, ComponentNode parent)
        : base(descriptor, parent)
    {
    }

    public void SetHandleFromAsset(Guid assetGuid)
    {
        var assetManager = EditorApplication.GetService<AssetManager>();

        MethodInfo? resolveMethod = null;
        if (typeof(T).Name == "GPUTexture")
            resolveMethod = typeof(AssetManager).GetMethod("ResolveTexture", BindingFlags.Public | BindingFlags.Instance);
        else if (typeof(T).Name == "Mesh")
            resolveMethod = typeof(AssetManager).GetMethod("ResolveMesh", BindingFlags.Public | BindingFlags.Instance);

        Handle<T> handle = default;
        if (resolveMethod != null && assetManager != null)
        {
            var res = resolveMethod.Invoke(assetManager, new object[] { assetGuid });
            if (res != null)
            {
                handle = (Handle<T>)res;
            }
        }
        else
        {
            Logger.Error($"No resolve method found for type {typeof(T).Name}");
        }

        AssetGuid = assetGuid;
        ExpectedHandleValue = UnsafeGetHandleValue(handle);
        SetValueFromUI(handle);
    }

    public void ClearHandle()
    {
        AssetGuid = Guid.Empty;
        ExpectedHandleValue = 0;
        SetValueFromUI(default);
    }

    private static long UnsafeGetHandleValue(Handle<T> handle)
    {
        return System.Runtime.CompilerServices.Unsafe.As<Handle<T>, long>(ref handle);
    }

    public override void SerializeOverride(JsonObject jsonRoot, object boxedComponent)
    {
        if (AssetGuid != Guid.Empty)
        {
            var camelCaseName = char.ToLowerInvariant(Descriptor.Name[0]) + Descriptor.Name.Substring(1);
            if (jsonRoot.ContainsKey(camelCaseName))
                jsonRoot[camelCaseName] = AssetGuid.ToString();
            else
                jsonRoot[Descriptor.Name] = AssetGuid.ToString();
        }
    }

    public override void DeserializeOverride(JsonElement jsonRoot, object boxedComponent)
    {
        var camelCaseName = char.ToLowerInvariant(Descriptor.Name[0]) + Descriptor.Name.Substring(1);

        if (jsonRoot.TryGetProperty(camelCaseName, out var propElement) || jsonRoot.TryGetProperty(Descriptor.Name, out propElement))
        {
            if (propElement.ValueKind == JsonValueKind.String && Guid.TryParse(propElement.GetString(), out var guid) && guid != Guid.Empty)
            {
                var assetManager = EditorApplication.GetService<AssetManager>();

                MethodInfo? resolveMethod = null;
                if (typeof(T).Name == "GPUTexture")
                    resolveMethod = typeof(AssetManager).GetMethod("ResolveTexture", BindingFlags.Public | BindingFlags.Instance);
                else if (typeof(T).Name == "Mesh")
                    resolveMethod = typeof(AssetManager).GetMethod("ResolveMesh", BindingFlags.Public | BindingFlags.Instance);

                if (resolveMethod != null && assetManager != null)
                {
                    var handleObj = resolveMethod.Invoke(assetManager, new object[] { guid });
                    if (handleObj != null)
                    {
                        var fieldInfo = boxedComponent.GetType().GetField(Descriptor.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(boxedComponent, handleObj);
                        }

                        var handle = (Handle<T>)handleObj;
                        var handleValue = System.Runtime.CompilerServices.Unsafe.As<Handle<T>, long>(ref handle);

                        AssetGuid = guid;
                        ExpectedHandleValue = handleValue;
                    }
                }
            }
        }
    }

    public override void Validate(object boxedComponent)
    {
        if (AssetGuid != Guid.Empty)
        {
            var fieldInfo = boxedComponent.GetType().GetField(Descriptor.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                var val = fieldInfo.GetValue(boxedComponent);
                if (val != null)
                {
                    var handle = (Handle<T>)val;
                    var currentVal = System.Runtime.CompilerServices.Unsafe.As<Handle<T>, long>(ref handle);

                    if (currentVal != ExpectedHandleValue)
                    {
                        Logger.Error($"Handle field '{Descriptor.Name}' was modified externally. Guid tracking cleared.");
                        AssetGuid = Guid.Empty;
                        ExpectedHandleValue = 0;
                    }
                }
            }
        }
    }
}

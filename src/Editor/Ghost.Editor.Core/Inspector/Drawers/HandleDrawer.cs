using Ghost.Core;
using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.SceneGraph;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Inspector.Drawers;

internal class HandleDrawer<T> : PropertyDrawer<Handle<T>> where T : unmanaged
{
    public override FrameworkElement CreateControlT(PropertyNode<Handle<T>> model)
    {
        static void UpdateUI(HandlePropertyNode<T> handleNode, ReferenceField field)
        {
            var guid = handleNode?.AssetGuid ?? Guid.Empty;
            field.HasValue = guid != Guid.Empty;
            field.DisplayText = guid != Guid.Empty ? $"{typeof(T).Name} ({guid.ToString().Substring(0, 8)})" : $"None ({typeof(T).Name})";
        }

        var field = new ReferenceField
        {
            TypeLabel = typeof(T).Name,
            Margin = new Thickness(0, 2, 0, 2)
        };

        var handleNode = model as HandlePropertyNode<T>;
        Logger.DebugAssert(handleNode != null);

        field.ValidateDrop = (args) =>
        {
            // For now, assume payload has standard string Guid or we implement format
            return args.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text);
        };

        field.OnDropAccepted = async (args) =>
        {
            if (handleNode == null)
            {
                return;
            }

            var text = await args.DataView.GetTextAsync();
            if (Guid.TryParse(text, out var guid))
            {
                handleNode.SetHandleFromAsset(guid);
                UpdateUI(handleNode, field);
            }
        };

        field.OnClearClicked = () =>
        {
            if (handleNode != null)
            {
                handleNode.ClearHandle();
                UpdateUI(handleNode, field);
            }
        };

        UpdateUI(handleNode, field);

        // When ECS value changes outside of UI
        model.OnValueChanged += (val) =>
        {
            // UI Thread check usually required here, but property model events should be on UI thread or marshaled
            field.DispatcherQueue.TryEnqueue(() =>
            {
                UpdateUI(handleNode, field);
            });
        };

        return field;
    }
}

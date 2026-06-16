using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Inspector.Drawers;

internal class EntityDrawer : PropertyDrawer<Entity>
{
    public override FrameworkElement CreateControlT(PropertyNode<Entity> model)
    {
        static void UpdateUI(Entity val, ReferenceField field)
        {
            if (val.IsValid)
            {
                field.HasValue = true;

                // TODO: For now, just display the Entity ID. We could resolve its SceneGraph Node name in the future.
                field.DisplayText = $"Entity {val.ID}:{val.Generation}";
            }
            else
            {
                field.HasValue = false;
                field.DisplayText = "None (Entity)";
            }
        }

        var field = new ReferenceField
        {
            TypeLabel = "Entity",
            IconGlyph = "\uF158",
            Margin = new Thickness(0, 2, 0, 2),
        };

        field.ValidateDrop += (args) =>
        {
            // TODO: Implement drag and drop for entities from the hierarchy
            return false;
        };

        field.OnClearClicked += () =>
        {
            model.SetValueFromUI(Entity.Invalid);
            UpdateUI(Entity.Invalid, field);
        };
        
        field.OnGotoClicked += () =>
        {
            // TODO: Implement goto functionality for entities
        };

        UpdateUI(model.Value, field);

        model.OnValueChanged += (val) =>
        {
            field.DispatcherQueue.TryEnqueue(() =>
            {
                UpdateUI(val, field);
            });
        };

        return field;
    }
}

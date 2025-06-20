using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Resources;
using Ghost.Editor.Utilities;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Reflection;

namespace Ghost.Editor.Controls.Internal;

internal unsafe sealed partial class ComponentDataView : Control
{
    private StackPanel? _contentContainer;

    private readonly World? _world;
    private readonly Entity _entity = Entity.Invalid;
    private readonly Type? _componentType;

    private EventHandler<object>? _updateHandler;
    private IComponentEditor? _customEditor;
    private PropertyField[]? _propertyFields;

    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(ComponentDataView), new PropertyMetadata(string.Empty));

    internal ComponentDataView()
    {
        DefaultStyleKey = typeof(ComponentDataView);

        Unloaded += (s, e) =>
        {
            CompositionTarget.Rendering -= _updateHandler;
            _contentContainer = null;
            _customEditor = null;
            _propertyFields = null;
        };
    }

    public ComponentDataView(string header, World world, Entity entity, Type componentType) : this()
    {
        HeaderText = header;
        _world = world;
        _entity = entity;
        _componentType = componentType;
    }

    protected override void OnApplyTemplate()
    {
        _contentContainer = (StackPanel)GetTemplateChild("ContentContainer");

        base.OnApplyTemplate();
        ReBuild();
    }

    private void ReflectionUpdate(object? sender, object e)
    {
        if (_propertyFields == null)
        {
            return;
        }

        foreach (var propertyField in _propertyFields)
        {
            propertyField.UpdateValue();
        }
    }

    private void CustomEditorUpdate(object? sender, object e)
    {
        _customEditor!.Update(new ComponentObject(_world!, _entity));
    }

    public void ReBuild()
    {
        if (_contentContainer == null)
        {
            return;
        }

        _contentContainer.Children.Clear();
        if (_world == null || _componentType == null || _entity == Entity.Invalid)
        {
            return;
        }

        var componentObject = new ComponentObject(_world, _entity);
        var editorType = TypeCache.GetTypes().FirstOrDefault(t =>
            typeof(IComponentEditor).IsAssignableFrom(t) &&
            t.GetCustomAttribute<CustomEditorAttribute>()?.TargetType.IsAssignableFrom(_componentType) == true);

        if (editorType != null)
        {
            _customEditor = (IComponentEditor)Activator.CreateInstance(editorType)!;
            _customEditor.Create(componentObject, _contentContainer);
        }
        else
        {
            var fields = _componentType.GetFields(StaticResource.componentPropertyBindingFlags);
            _propertyFields = new PropertyField[fields.Length];

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var component = _world.ComponentStorage.ComponentPools[_componentType.TypeHandle.Value].Get(_entity);
                var propertyField = PropertyField.Create(field.Name, field, component);

                _propertyFields[i] = propertyField;
                _contentContainer.Children.Add(propertyField);
            }
        }

        _updateHandler = _customEditor == null ? ReflectionUpdate : CustomEditorUpdate;
        CompositionTarget.Rendering += _updateHandler;
    }
}
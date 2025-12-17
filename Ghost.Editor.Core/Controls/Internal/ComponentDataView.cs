using Ghost.Core;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.Resources;
using Ghost.Editor.Core.Utilities;
using Ghost.SparseEntities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;

namespace Ghost.Editor.Core.Controls.Internal;

internal unsafe sealed partial class ComponentDataView : Control
{
    private delegate void EditorUpdate();

    private StackPanel? _contentContainer;

    private readonly World? _world;
    private readonly Entity _entity = Entity.Invalid;
    private readonly Type? _componentType;

    private ComponentEditor? _customEditor;
    private PropertyField[]? _propertyFields;
    private EditorUpdate? _editorUpdate;

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
            _customEditor?.Destroy();

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

    private void ReflectionUpdate()
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

    private void CustomEditorUpdate()
    {
        _customEditor!.Update();
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
            typeof(ComponentEditor).IsAssignableFrom(t) &&
            t.GetCustomAttribute<CustomEditorAttribute>()?.TargetType.IsAssignableFrom(_componentType) == true);

        if (editorType != null)
        {
            _customEditor = (ComponentEditor)Activator.CreateInstance(editorType)!;
            _customEditor.Initialize(componentObject);
            _customEditor.Create(_contentContainer);
        }
        else
        {
            var fields = _componentType.GetFields(StaticResource.componentPropertyBindingFlags);
            _propertyFields = new PropertyField[fields.Length];

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (!_world.ComponentStorage.TryGetPool(TypeHandle.Get(_componentType), out var pool))
                {
                    continue;
                }
                var component = pool.Get(_entity);
                var propertyField = PropertyField.Create(field.Name, field, component);

                _propertyFields[i] = propertyField;
                _contentContainer.Children.Add(propertyField);
            }
        }

        _editorUpdate = _customEditor == null ? ReflectionUpdate : CustomEditorUpdate;
        _editorUpdate();

        _world.ComponentChanged += OnComponentChanged;
    }

    private void OnComponentChanged(World world, Entity entity, Type type)
    {
        if (world != _world
            || entity != _entity
            || type != _componentType)
        {
            return;
        }

        _editorUpdate?.Invoke();
    }
}
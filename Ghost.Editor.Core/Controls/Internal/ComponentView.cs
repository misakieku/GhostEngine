using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.Resources;
using Ghost.Editor.Core.Utilities;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Controls.Internal;

internal sealed unsafe partial class ComponentView : Control
{
    private delegate void EditorUpdate();

    private StackPanel? _contentContainer;

    private readonly World? _world;
    private readonly Entity _entity = Entity.Invalid;
    private readonly Type? _componentType;
    private readonly ComponentInfo _componentInfo;

    private object? _managedInstance;
    private void* _pComponentData;

    private ComponentEditor? _customEditor;
    private PropertyField[]? _propertyFields;
    private EditorUpdate? _editorUpdate;

    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(ComponentView), new PropertyMetadata(string.Empty));

    internal ComponentView()
    {
        DefaultStyleKey = typeof(ComponentView);

        Unloaded += (s, e) =>
        {
            _customEditor?.Destroy();

            _contentContainer = null;
            _customEditor = null;
            _propertyFields = null;
        };
    }

    public ComponentView(string header, World world, Entity entity, Type componentType) : this()
    {
        HeaderText = header;

        _world = world;
        _entity = entity;
        _componentType = componentType;
        _componentInfo = ComponentRegistry.GetComponentInfo(componentType);
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
        _customEditor?.Update();
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

        if (_propertyFields != null)
        {
            foreach (var propertyField in _propertyFields)
            {
                propertyField.OnValueChanged -= OnPropertyValueChanged;
            }
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
            var fields = _componentType.GetFields(StaticResource.ComponentPropertyBindingFlags);
            _propertyFields = new PropertyField[fields.Length];

            _pComponentData = _world.EntityManager.GetComponent(_entity, _componentInfo.id);
            _managedInstance = Marshal.PtrToStructure((nint)_pComponentData, _componentType);
            if (_managedInstance == null)
            {
                return;
            }

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var propertyField = PropertyField.Create(field.Name, field, _managedInstance);
                propertyField.OnValueChanged += OnPropertyValueChanged;

                _propertyFields[i] = propertyField;
                _contentContainer.Children.Add(propertyField);
            }
        }

        _editorUpdate = _customEditor == null ? ReflectionUpdate : CustomEditorUpdate;
        _editorUpdate();
    }

    private void OnPropertyValueChanged(PropertyField field)
    {
        if (_managedInstance == null || _pComponentData == null)
        {
            return;
        }

        Marshal.StructureToPtr(_managedInstance, (nint)_pComponentData, false);
    }
}
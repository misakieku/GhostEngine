using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector;

interface IComponentEditor
{
    /// <summary>
    /// Called when the component editor is created.
    /// </summary>
    /// <param name="componentObject">The component data to edit.</param>
    /// <param name="container">The container to add the editor controls to.</param>
    public void Create(ComponentObject componentObject, StackPanel container);

    /// <summary>
    /// Called when the component editor needs to update its UI based on the current state of the component data.
    /// </summary>
    /// <param name="componentObject">The component data to edit.</param>
    public void Update(ComponentObject componentObject);

    /// <summary>
    /// Called when the component editor is destroyed.
    /// </summary>
    /// <param name="componentObject">The component data to edit.</param>
    public void Destroy(ComponentObject componentObject);
}
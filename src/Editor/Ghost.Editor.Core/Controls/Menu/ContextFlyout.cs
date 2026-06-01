using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls;

public sealed partial class ContextFlyout : MenuFlyout
{
    private bool _isPopulated;

    public string ContextMenuTag
    {
        get; set;
    } = string.Empty;

    public ContextFlyout()
    {
        Opening += ContextFlyout_Opening;
    }

    private void PopulateContextMenu()
    {
        var rootNodes = MenuUtility.BuildTree(ContextMenuTag);
        MenuUtility.BuildNodes(rootNodes, Items);
    }

    private void ContextFlyout_Opening(object? sender, object e)
    {
        if (_isPopulated)
        {
            return;
        }

        PopulateContextMenu();
        _isPopulated = true;
    }
}

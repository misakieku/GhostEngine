using Ghost.Core;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml;
using System.Collections;

namespace Ghost.Editor.View.Windows;

/// <summary>
/// Service for handling tab tear-off operations across the editor.
/// </summary>
internal static class TabTearOffService
{
    /// <summary>
    /// Attempts to tear off a tab into a new window.
    /// </summary>
    /// <param name="sourceItems">The collection to remove the item from.</param>
    /// <param name="tabItem">The item to tear off.</param>
    /// <param name="selectionContainer">Optional container to restore selection to on failure.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result TryTearOffTab(IList sourceItems, object tabItem, object? selectionContainer = null)
    {
        int originalIndex = sourceItems.IndexOf(tabItem);
        if (originalIndex == -1)
        {
            return Result.Failure("Item not found in source collection.");
        }

        object? originalSelection = GetSelection(selectionContainer);

        try
        {
            sourceItems.Remove(tabItem);

            try
            {
                App.CreateAndShowDockWindow(tabItem);
                return Result.Success();
            }
            catch (Exception ex)
            {
                // Rollback collection
                sourceItems.Insert(originalIndex, tabItem);
                RestoreSelection(selectionContainer, originalSelection);
                
                Logger.LogError(ex);
                return Result.Failure($"Failed to create tear-off window: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            return Result.Failure($"Failed to remove item from source: {ex.Message}");
        }
    }

    private static object? GetSelection(object? container)
    {
        if (container is DockPanelNode panel) return panel.SelectedItem;
        if (container is Microsoft.UI.Xaml.Controls.TabView tabView) return tabView.SelectedItem;
        return null;
    }

    private static void RestoreSelection(object? container, object? selection)
    {
        if (selection == null) return;
        if (container is DockPanelNode panel) panel.SelectedItem = selection;
        else if (container is Microsoft.UI.Xaml.Controls.TabView tabView) tabView.SelectedItem = selection;
    }
}

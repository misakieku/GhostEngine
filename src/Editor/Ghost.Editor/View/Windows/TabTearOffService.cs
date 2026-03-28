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
                // We no longer create the window here to decouple the service from the app shell.
                // The caller is responsible for window creation (e.g. via an event handler).
                return Result.Success();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);

                // Rollback collection and selection
                try
                {
                    sourceItems.Insert(originalIndex, tabItem);
                    RestoreSelection(selectionContainer, originalSelection);
                }
                catch (Exception rollbackEx)
                {
                    Logger.LogError(rollbackEx);
                    return Result.Failure($"Failed to tear off tab and rollback failed: {ex.Message}. Rollback error: {rollbackEx.Message}");
                }
                
                return Result.Failure($"Failed to tear off tab: {ex.Message}");
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

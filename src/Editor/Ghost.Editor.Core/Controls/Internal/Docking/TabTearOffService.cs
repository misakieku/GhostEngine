using Ghost.Core;
using Microsoft.UI.Xaml.Controls;
using System.Collections;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

/// <summary>
/// Service for handling tab tear-off operations across the editor.
/// </summary>
internal static class TabTearOffService
{
    /// <summary>
    /// Attempts to tear off a tab by removing it from its source and executing a creation callback.
    /// If the creation callback fails, the tab is restored to its original position and selection state.
    /// </summary>
    /// <param name="sourceItems">The collection to remove the item from.</param>
    /// <param name="tabItem">The item to tear off.</param>
    /// <param name="createCallback">The callback to create the new host (e.g. window) for the tab.</param>
    /// <param name="selectionContainer">Optional container to restore selection to on failure.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Result TryTearOffTab(IList sourceItems, object tabItem, Action<object> createCallback, object? selectionContainer = null)
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
                createCallback(tabItem);
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
                    return Result.Failure($"Failed to create tear-off host and rollback failed: {ex.Message}. Rollback error: {rollbackEx.Message}");
                }
                
                return Result.Failure($"Failed to create tear-off host: {ex.Message}");
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
        if (container is TabView tabView) return tabView.SelectedItem;
        return null;
    }

    private static void RestoreSelection(object? container, object? selection)
    {
        if (selection == null) return;
        if (container is DockPanelNode panel) panel.SelectedItem = selection;
        else if (container is TabView tabView) tabView.SelectedItem = selection;
    }
}

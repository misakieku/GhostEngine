using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Ghost.Editor.Helpers;

public static class SystemUtilities
{
    public static async Task<StorageFolder?> OpenFolderPickerAsync(PickerLocationId startLocation = PickerLocationId.DocumentsLibrary, string settingsIdentifier = "")
    {
        var openPicker = new FolderPicker();
        var hWnd = WindowNative.GetWindowHandle(App.GetWindow());
        InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.SuggestedStartLocation = startLocation;
        openPicker.FileTypeFilter.Add("*");
        openPicker.SettingsIdentifier = settingsIdentifier;

        var folder = await openPicker.PickSingleFolderAsync();
        return folder;
    }
}
using System;
using System.Collections.Generic;
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
        var hWnd = WindowNative.GetWindowHandle(App.Window);
        InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.SuggestedStartLocation = startLocation;
        openPicker.FileTypeFilter.Add("*");
        openPicker.SettingsIdentifier = settingsIdentifier;

        var folder = await openPicker.PickSingleFolderAsync();
        return folder;
    }

    public static async Task<StorageFile?> OpenFilePickerAsync(PickerLocationId startLocation = PickerLocationId.DocumentsLibrary, string settingsIdentifier = "", params IEnumerable<string> filter)
    {
        var openPicker = new FileOpenPicker();
        var hWnd = WindowNative.GetWindowHandle(App.Window);
        InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.SuggestedStartLocation = startLocation;
        openPicker.SettingsIdentifier = settingsIdentifier;
        foreach (var fileType in filter)
        {
            openPicker.FileTypeFilter.Add(fileType);
        }

        var file = await openPicker.PickSingleFileAsync();
        return file;
    }
}
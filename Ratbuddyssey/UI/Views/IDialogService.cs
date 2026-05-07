using System.Threading.Tasks;

namespace Ratbuddyssey;

/// <summary>
/// Abstracts the host-window-owned dialogs and lifecycle hooks that the view-model
/// needs to invoke. The Avalonia window implements this so the VM stays
/// independent of view-layer types.
/// </summary>
public interface IDialogService
{
    /// <summary>Show the open-file picker; returns the selected path or null on cancel.</summary>
    Task<string> OpenAdyFileAsync();

    /// <summary>Show the save-as picker; returns the chosen path or null on cancel.</summary>
    Task<string> SaveAdyFileAsAsync(string suggestedName);

    /// <summary>Show the confirm-reload yes/no prompt.</summary>
    Task<bool> ConfirmReloadAsync();

    /// <summary>Confirm with the user that unsaved changes can be discarded. Returns true to proceed.</summary>
    Task<bool> ConfirmDiscardChangesAsync();

    /// <summary>Show the About dialog.</summary>
    Task ShowAboutAsync();

    /// <summary>Show a non-fatal error message to the user.</summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Show the REW <c>.txt</c> import file picker. Default implementation returns null
    /// so existing test fakes don't have to opt in.
    /// </summary>
    Task<string> OpenRewTxtFileAsync() => Task.FromResult<string>(null);

    /// <summary>Request that the desktop application shut down.</summary>
    void RequestExit();
}

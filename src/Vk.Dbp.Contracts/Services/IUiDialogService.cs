namespace Vk.Dbp.Contracts.Services;

/// <summary>
/// Provides UI dialog helpers for application services and view models.
/// </summary>
public interface IUiDialogService
{
    /// <summary>
    /// Shows an informational message.
    /// </summary>
    /// <param name="message">The message body.</param>
    /// <param name="title">The dialog title.</param>
    void ShowInformation(string message, string title = "Information");

    /// <summary>
    /// Shows a warning message.
    /// </summary>
    /// <param name="message">The message body.</param>
    /// <param name="title">The dialog title.</param>
    void ShowWarning(string message, string title = "Warning");

    /// <summary>
    /// Shows an error message.
    /// </summary>
    /// <param name="message">The message body.</param>
    /// <param name="title">The dialog title.</param>
    void ShowError(string message, string title = "Error");

    /// <summary>
    /// Shows a confirmation dialog and returns the user's choice.
    /// </summary>
    /// <param name="message">The message body.</param>
    /// <param name="title">The dialog title.</param>
    /// <returns><c>true</c> when the user confirms; otherwise, <c>false</c>.</returns>
    bool Confirm(string message, string title = "Confirm");
}

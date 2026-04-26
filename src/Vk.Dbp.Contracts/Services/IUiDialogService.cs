namespace Vk.Dbp.Contracts.Services;

public interface IUiDialogService
{
    void ShowInformation(string message, string title = "Information");

    void ShowWarning(string message, string title = "Warning");

    void ShowError(string message, string title = "Error");

    bool Confirm(string message, string title = "Confirm");
}

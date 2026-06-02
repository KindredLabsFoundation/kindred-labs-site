namespace KindredLabs.Web.Models;

public class FormActionsViewModel
{
    public string MainFormId { get; set; } = "mainForm";
    public string DeleteFormId { get; set; } = "deleteForm";
    public string DraftId { get; set; } = string.Empty;
    public string DeleteHandlerPage { get; set; } = string.Empty;
    public string Culture { get; set; } = "en";
    public string ClearLabel { get; set; } = "Clear Form";
    public string DeleteLabel { get; set; } = "Delete Draft";
    public string DeleteConfirmMessage { get; set; } =
        "Are you sure you want to delete this draft? This action cannot be undone.";
}

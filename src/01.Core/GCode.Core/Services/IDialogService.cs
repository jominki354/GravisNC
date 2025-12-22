namespace GCode.Core.Services
{
    public enum ConfirmResult
    {
        Yes,
        No,
        Cancel
    }

    public interface IDialogService
    {
        bool OpenFileDialog(out string filePath, string filter = "All Files|*.*", string initialDirectory = "");
        bool SaveFileDialog(out string filePath, string defaultName = "", string filter = "All Files|*.*", string initialDirectory = "");
        void ShowMessage(string message);
        bool ShowFolderBrowserDialog(out string folderPath);
        ConfirmResult ShowConfirmDialog(string message, string title = "저장 확인");
    }
}

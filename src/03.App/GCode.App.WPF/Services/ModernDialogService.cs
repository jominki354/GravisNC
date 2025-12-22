using System.Windows;
using Microsoft.Win32;
using GCode.Core.Services;
using GCode.App.WPF.Views;

namespace GCode.App.WPF.Services
{
    public class ModernDialogService : IDialogService
    {
        public bool OpenFileDialog(out string filePath, string filter = "All Files|*.*", string initialDirectory = "")
        {
            var dlg = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = initialDirectory
            };

            if (dlg.ShowDialog() == true)
            {
                filePath = dlg.FileName;
                return true;
            }
            filePath = string.Empty;
            return false;
        }

        public bool SaveFileDialog(out string filePath, string defaultName = "", string filter = "All Files|*.*", string initialDirectory = "")
        {
            var dlg = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultName,
                InitialDirectory = initialDirectory
            };

            if (dlg.ShowDialog() == true)
            {
                filePath = dlg.FileName;
                return true;
            }
            filePath = string.Empty;
            return false;
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message, "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowFolderBrowserDialog(out string folderPath)
        {
            var folderDlg = new OpenFolderDialog
            {
                Title = "폴더 선택",
                Multiselect = false
            };

            if (folderDlg.ShowDialog() == true)
            {
                 folderPath = folderDlg.FolderName;
                 return true;
            }

            folderPath = string.Empty;
            return false;
        }

        public ConfirmResult ShowConfirmDialog(string message, string title = "저장 확인")
        {
            // Application.Current.MainWindow를 Owner로 설정하여 모달 동작 보장
            var owner = Application.Current.MainWindow;
            
            var dlg = new ConfirmDialog(message)
            {
                Owner = owner
            };

            dlg.ShowDialog();
            return dlg.Result;
        }
    }
}

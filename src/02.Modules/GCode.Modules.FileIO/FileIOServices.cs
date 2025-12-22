using System;
using System.IO;
using System.Threading.Tasks;
using GCode.Core.Services;
using Microsoft.Win32;
using System.Windows;
using SWF = System.Windows.Forms;

namespace GCode.Modules.FileIO
{
    public class FileService : IFileService
    {
        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path);
        }

        public async Task WriteAllTextAsync(string path, string content)
        {
            await File.WriteAllTextAsync(path, content);
        }
    }

    public class DialogService : IDialogService
    {
        public bool OpenFileDialog(out string filePath, string filter = "All Files|*.*", string initialDirectory = "")
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = filter };
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }
            
            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
                return true;
            }
            filePath = string.Empty;
            return false;
        }

        public bool SaveFileDialog(out string filePath, string defaultName = "", string filter = "All Files|*.*", string initialDirectory = "")
        {
            var dialog = new Microsoft.Win32.SaveFileDialog 
            { 
                FileName = defaultName, 
                Filter = filter 
            };

            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }
            
            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
                return true;
            }
            filePath = string.Empty;
            return false;
        }

        public void ShowMessage(string message)
        {
            System.Windows.MessageBox.Show(message);
        }

        public ConfirmResult ShowConfirmDialog(string message, string title = "저장 확인")
        {
            var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            return result switch
            {
                MessageBoxResult.Yes => ConfirmResult.Yes,
                MessageBoxResult.No => ConfirmResult.No,
                _ => ConfirmResult.Cancel
            };
        }

        public bool ShowFolderBrowserDialog(out string folderPath)
        {
            using var dialog = new SWF.FolderBrowserDialog
            {
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };
            
            var result = dialog.ShowDialog();
            if (result == SWF.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                folderPath = dialog.SelectedPath;
                return true;
            }
            folderPath = string.Empty;
            return false;
        }
    }
}

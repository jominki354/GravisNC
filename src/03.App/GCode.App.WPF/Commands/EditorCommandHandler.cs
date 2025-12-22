using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using GCode.Core.Services;
using GCode.Core.Models;
using GCode.App.WPF.Views;

namespace GCode.App.WPF.Commands
{
    public class EditorCommandHandler
    {
        private readonly MainWindow _window;
        private readonly TabControl _tabs;
        private readonly TreeView _fileTree; // Add TreeView
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;
        private EditorSettings _currentSettings;

        public EditorCommandHandler(MainWindow window, TabControl tabs, TreeView fileTree, IFileService fileService, IDialogService dialogService, ISettingsService settingsService)
        {
            _window = window;
            _tabs = tabs;
            _fileTree = fileTree; // Assign
            _fileService = fileService;
            _dialogService = dialogService;
            _settingsService = settingsService;
            
            // 초기 설정 로드
            _currentSettings = _settingsService.LoadSettings();

            InitializeBindings();
            LoadGCodeHighlighting();
        }

        private void InitializeBindings()
        {
            // File
            _window.CommandBindings.Add(new CommandBinding(AppCommands.NewFile, Execute_NewFile));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.OpenFile, Execute_OpenFile));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.OpenFolder, Execute_OpenFolder));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.SaveFile, Execute_SaveFile, CanExecute_HasEditor));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.SaveAsFile, Execute_SaveAsFile, CanExecute_HasEditor));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.Exit, Execute_Exit));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.CloseTab, Execute_CloseTab, CanExecute_HasEditor));

            // View
            _window.CommandBindings.Add(new CommandBinding(AppCommands.ZoomIn, Execute_ZoomIn, CanExecute_HasEditor));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.ZoomOut, Execute_ZoomOut, CanExecute_HasEditor));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.ToggleExplorer, Execute_ToggleExplorer));
            
            // Settings
            _window.CommandBindings.Add(new CommandBinding(AppCommands.OpenSettings, Execute_OpenSettings));

            // Explorer Operations
            _window.CommandBindings.Add(new CommandBinding(AppCommands.RevealInExplorer, Execute_RevealInExplorer));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.CloseFolder, Execute_CloseFolder));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.Copy, Execute_Copy, CanExecute_ExplorerAction));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.Paste, Execute_Paste, CanExecute_ExplorerAction));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.Delete, Execute_Delete, CanExecute_ExplorerAction));
            _window.CommandBindings.Add(new CommandBinding(AppCommands.Rename, Execute_Rename, CanExecute_ExplorerAction));
        }

        private void Execute_OpenSettings(object sender, ExecutedRoutedEventArgs e)
        {
            OpenSettingsDialog();
        }

        private void LoadGCodeHighlighting()
        {
            try
            {
                using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/GCode.xshd")).Stream;
                using var reader = new XmlTextReader(stream);
                var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting("GCode", new[] { ".nc", ".tap", ".gcode" }, definition);
            }
            catch (Exception ex)
            {
                // 로드 실패 시 무시하거나 기본값 사용
                System.Diagnostics.Debug.WriteLine($"Highlighting Load Failed: {ex.Message}");
            }
        }

        public void ApplySettingsToEditor(TextEditor editor)
        {
            editor.FontFamily = new FontFamily(_currentSettings.FontFamily);
            editor.FontSize = _currentSettings.FontSize;
            editor.FontWeight = _currentSettings.FontWeight == "Bold" ? FontWeights.Bold : FontWeights.Normal;
            
            // Syntax Highlighting 적용
            editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("GCode");
        }
        
        public void OpenSettingsDialog()
        {
            var dlg = new SettingsWindow(_currentSettings) { Owner = _window };
            if (dlg.ShowDialog() == true)
            {
                _currentSettings = dlg.ResultSettings;
                _settingsService.SaveSettings(_currentSettings);
                
                // 열려있는 모든 에디터에 적용
                foreach (TabItem item in _tabs.Items)
                {
                    if (item.Content is TextEditor editor)
                    {
                        ApplySettingsToEditor(editor);
                    }
                }
            }
        }

        // Helpers
        private TextEditor? GetCurrentEditor()
        {
            if (_tabs.SelectedItem is TabItem tab && tab.Content is TextEditor editor)
            {
                return editor;
            }
            return null;
        }

        // --- File Operations ---

        private void Execute_NewFile(object sender, ExecutedRoutedEventArgs e)
        {
            _window.CreateNewTab($"제목없음 {_tabs.Items.Count + 1}");
        }

        private async void Execute_OpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            try 
            {
                if (_dialogService.OpenFileDialog(out string fileName, 
                    "G-Code Files (*.nc;*.ncf;*.gcode)|*.nc;*.ncf;*.gcode|All Files (*.*)|*.*",
                    _currentSettings.LastDirectory))
                {
                    // Update Last Open Directory
                    string dir = Path.GetDirectoryName(fileName) ?? "";
                    if (!string.IsNullOrEmpty(dir) && _currentSettings.LastDirectory != dir)
                    {
                        _currentSettings.LastDirectory = dir;
                        _settingsService.SaveSettings(_currentSettings);
                    }

                    string content = await _fileService.ReadAllTextAsync(fileName);
                    string shortName = Path.GetFileName(fileName);
                    
                    _window.CreateNewTab(shortName, content);
                    
                    if (_tabs.SelectedItem is TabItem tab)
                    {
                        tab.Tag = fileName; // Full Path
                        _window.UpdateBreadcrumb(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                 _dialogService.ShowMessage($"열기 실패: {ex.Message}");
            }
        }
        private void Execute_OpenFolder(object sender, ExecutedRoutedEventArgs e)
        {
            if (_dialogService.ShowFolderBrowserDialog(out string folderPath))
            {
                _window.LoadFolderTree(folderPath);
            }
        }

        private async void Execute_SaveFile(object sender, ExecutedRoutedEventArgs e)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var tab = (TabItem)_tabs.SelectedItem;
            await SaveTabAsync(tab);
        }

        public async Task<bool> SaveTabAsync(TabItem tab)
        {
            if (tab.Content is not TextEditor editor) return false;
            string? currentPath = tab.Tag as string;

            if (string.IsNullOrEmpty(currentPath))
            {
                return await SaveTabAsAsync(tab);
            }

            try
            {
                await _fileService.WriteAllTextAsync(currentPath, editor.Text);
                _window.ClearTabModified(tab);
                _window.SetStatus("저장 완료: " + currentPath);
                return true;
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"저장 실패: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveTabAsAsync(TabItem tab)
        {
            if (tab.Content is not TextEditor editor) return false;

            if (_dialogService.SaveFileDialog(out string fileName, "Untitled.nc", 
                "NC File (*.nc)|*.nc|All Files (*.*)|*.*", 
                _currentSettings.LastDirectory))
            {
                try
                {
                    string dir = Path.GetDirectoryName(fileName) ?? "";
                    if (!string.IsNullOrEmpty(dir) && _currentSettings.LastDirectory != dir)
                    {
                        _currentSettings.LastDirectory = dir;
                        _settingsService.SaveSettings(_currentSettings);
                    }

                    await _fileService.WriteAllTextAsync(fileName, editor.Text);
                    
                    tab.Header = Path.GetFileName(fileName);
                    tab.Tag = fileName;
                    
                    _window.ClearTabModified(tab);
                    _window.UpdateTitle(fileName);
                    _window.UpdateBreadcrumb(fileName);
                    _window.SetStatus("저장 완료: " + fileName);
                    return true;
                }
                catch (Exception ex)
                {
                    _dialogService.ShowMessage($"저장 실패: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        private async void Execute_SaveAsFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (_tabs.SelectedItem is TabItem tab)
            {
                await SaveTabAsAsync(tab);
            }
        }

        private void Execute_Exit(object sender, ExecutedRoutedEventArgs e)
        {
            // Application.Current.Shutdown() bypasses some window closing logic depending on how it's called.
            // Using Close() triggers OnClosing, ensuring our safe exit logic runs.
            _window.Close();
        }

        private async void Execute_CloseTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (_tabs.SelectedItem is TabItem selectedTab)
            {
                if (await _window.ConfirmSaveIfModifiedAsync(selectedTab))
                {
                    _tabs.Items.Remove(selectedTab);
                }
            }
        }

        // --- View Operations ---

        private void Execute_ZoomIn(object sender, ExecutedRoutedEventArgs e)
        {
            if (GetCurrentEditor() is TextEditor editor && editor.FontSize < 40)
            {
                editor.FontSize += 2;
                _window.SetStatus($"Zoom: {editor.FontSize}px");
            }
        }

        private void Execute_ZoomOut(object sender, ExecutedRoutedEventArgs e)
        {
            if (GetCurrentEditor() is TextEditor editor && editor.FontSize > 6)
            {
                editor.FontSize -= 2;
                _window.SetStatus($"Zoom: {editor.FontSize}px");
            }
        }

        private void Execute_ToggleExplorer(object sender, ExecutedRoutedEventArgs e)
        {
            _window.ToggleExplorer();
        }

        // --- CanExecute ---

        private void CanExecute_HasEditor(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GetCurrentEditor() != null;
        }

        // --- Explorer Operations ---

        private void Execute_CloseFolder(object sender, ExecutedRoutedEventArgs e)
        {
            _fileTree.Items.Clear();
            var placeholder = new TreeViewItem { Header = "폴더를 열어주세요", Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)) };
            placeholder.Selected += _window.TreeViewItem_Selected_OpenFolder;
            _fileTree.Items.Add(placeholder);
        }

        private void Execute_RevealInExplorer(object sender, ExecutedRoutedEventArgs e)
        {
            string? path = e.Parameter as string;
            if (string.IsNullOrEmpty(path) && _fileTree.SelectedItem is TreeViewItem item)
            {
                path = item.Tag as string;
            }

            if (!string.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path)))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
             if (e.Parameter is string path)
             {
                 var list = new System.Collections.Specialized.StringCollection { path };
                 Clipboard.SetFileDropList(list);
             }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
             if (e.Parameter is string targetPath && Clipboard.ContainsFileDropList())
             {
                 var files = Clipboard.GetFileDropList();
                 string targetDir = Directory.Exists(targetPath) ? targetPath : Path.GetDirectoryName(targetPath)!;
                 
                 foreach (string? file in files)
                 {
                     if (string.IsNullOrEmpty(file)) continue;

                     string? fileName = Path.GetFileName(file);
                     if (string.IsNullOrEmpty(fileName)) continue;

                     string dest = Path.Combine(targetDir, fileName);
                     try 
                     {
                        if (Directory.Exists(file))
                        {
                            // Directory Copy not implemented for simplicity
                        }
                        else
                        {
                            File.Copy(file, dest, true);
                        }
                     }
                     catch { }
                 }
                 // Refresh Tree? Need to call LoadFolderTree again? 
             }
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is string path)
            {
                if (MessageBox.Show($"'{Path.GetFileName(path)}'을(를) 영구적으로 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try 
                    {
                        if (File.Exists(path)) File.Delete(path);
                        else if (Directory.Exists(path)) Directory.Delete(path, true);
                        
                        // Refresh logic needed. 
                         // _window.LoadFolderTree... but we don't know the root easily. 
                         // Ideally Refresh the parent node.
                    }
                    catch (Exception ex)
                    {
                        _dialogService.ShowMessage($"삭제 실패: {ex.Message}");
                    }
                }
            }
        }

        private void Execute_Rename(object sender, ExecutedRoutedEventArgs e)
        {
            // Rename requires Input UI. 
            _dialogService.ShowMessage("이름 바꾸기 기능은 준비 중입니다.");
        }

        private void CanExecute_ExplorerAction(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is string;
        }
    }
}

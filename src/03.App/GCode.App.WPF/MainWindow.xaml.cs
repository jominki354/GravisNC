using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.Win32;
using GCode.Core.Services;
using GCode.App.WPF.Commands;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Editing;
using System.Windows.Threading;
using System.Collections.Generic;
using GCode.App.WPF.Services; // Ensure GCodeBlock is found

namespace GCode.App.WPF
{
    public partial class MainWindow : Window
    {
        private EditorCommandHandler _commandHandler;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;
        private readonly Services.GCodeParserService _parser; // NEW
        
        // Tab Drag-Drop
        private TabItem? _draggedTab = null;
        private Point _dragStartPoint;

        // Folding
        private readonly GCodeFoldingStrategy _foldingStrategy = new();
        private readonly Dictionary<TextEditor, FoldingManager> _foldingManagers = new();
        private readonly DispatcherTimer _foldingUpdateTimer;

        public MainWindow(IFileService fileService, IDialogService dialogService, ISettingsService settingsService)
        {
            InitializeComponent();
            _fileService = fileService;
            _dialogService = dialogService;
            _settingsService = settingsService;
            _parser = new GCodeParserService(); // NEW
            
            // 앱 시작 시 빈 탭 하나 생성 (삭제됨 - RestoreSession에서 처리)
            _commandHandler = new EditorCommandHandler(this, EditorTabs, FileTree, fileService, dialogService, settingsService);
            EditorTabs.SelectionChanged += EditorTabs_SelectionChanged;
            
            // OS 종료 시 방어 로직
            Application.Current.SessionEnding += Application_SessionEnding;

            // 폴딩 업데이트 타이머 (텍스트 변경 후 500ms 뒤에 계산)
            _foldingUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _foldingUpdateTimer.Tick += FoldingUpdateTimer_Tick;

            RestoreSession();
        }

        private async void RestoreSession()
        {
             var settings = _settingsService.LoadSettings();
             
             // Restore Last Directory
             if (!string.IsNullOrEmpty(settings.LastDirectory) && Directory.Exists(settings.LastDirectory))
             {
                 LoadFolderTree(settings.LastDirectory);
             }

             // Restore Files
             bool restored = false;
             if (settings.OpenFiles != null && settings.OpenFiles.Count > 0)
             {
                 foreach(var path in settings.OpenFiles)
                 {
                     if (File.Exists(path)) 
                     {
                         try {
                             string content = await _fileService.ReadAllTextAsync(path);
                             CreateNewTab(Path.GetFileName(path), content);
                             if (EditorTabs.Items[EditorTabs.Items.Count - 1] is TabItem t) 
                             {
                                 t.Tag = path;
                                 if (EditorTabs.Items.Count == 1) UpdateBreadcrumb(path);
                             }
                             restored = true;
                         } catch { }
                     }
                 }
             }
             
             // Select last tab if restored
             if (restored && EditorTabs.Items.Count > 0)
             {
                 EditorTabs.SelectedIndex = EditorTabs.Items.Count - 1;
             }
             // IF NOT restored, do NOTHING (Request: Start Empty)
        }

        private bool _isClosingForced = false;

        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            // OS 종료(로그오프/시스템종료) 시 OnClosing과 동일한 방어 로직 실행
            // 단, SessionEnding은 취소가 제한적일 수 있음.
            if (!HandleAppExit())
            {
                e.Cancel = true; // 종료 취소 시도
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosingForced) 
            {
                base.OnClosing(e);
                return;
            }

            // 공통 종료 로직 실행
            if (!HandleAppExit())
            {
                e.Cancel = true; // 종료 취소
            }
            else
            {
                base.OnClosing(e);
            }
        }

        /// <summary>
        /// 앱 종료 처리 공통 로직
        /// </summary>
        /// <returns>True if Exit Allowed, False if Cancelled</returns>
        private bool HandleAppExit()
        {
            // 수정된 탭 확인
            var modifiedTabs = EditorTabs.Items.Cast<TabItem>().Where(tab => tab.Header.ToString()?.EndsWith("*") ?? false).ToList();
            
            if (modifiedTabs.Any())
            {
                foreach (var tab in modifiedTabs)
                {
                    // 비동기 실행을 동기처럼 기다려야 함 (OnClosing/SessionEnding은 동기 이벤트)
                    // 하지만 WPF 대화상자는 모달이므로 ShowDialog()는 블로킹됨.
                    // 문제는 SaveTabAsync가 awaitable이라는 점.
                    // 여기서는 Task.Run().Wait() 등을 쓰면 데드락 위험 있음.
                    // 다행히 ConfirmDialog는 ShowDialog()로 블로킹됨.
                    
                   // 핵심: ConfirmSaveIfModifiedAsync 내부 로직을 동기식으로 처리하거나 
                   // 여기서 바로 처리해야 함.
                   
                   bool canClose = ConfirmSaveAndClose(tab);
                   if (!canClose) return false; // 취소됨 -> 종료 중단
                }
            }

            // 모든 탭 처리 완료 -> 저장 후 종료
            SaveSession();
            _isClosingForced = true;
            return true;
        }

        private bool ConfirmSaveAndClose(TabItem tab)
        {
            string header = tab.Header.ToString() ?? "";
            if (!header.EndsWith("*")) return true;

            string fileName = header.TrimEnd('*');
            EditorTabs.SelectedItem = tab; // Show tab to user

            var result = _dialogService.ShowConfirmDialog($"'{fileName}'의 변경 내용을 저장하시겠습니까?");

            if (result == ConfirmResult.Yes)
            {
                // 동기적으로 저장 시도 (Wait for async task safely?)
                // WPF UI Thread에서 .Result나 .Wait()는 위험.
                // 방침: EditorCommandHandler에 동기 저장 메서드를 추가하거나,
                // 여기서 Join을 신중하게 사용.
                // 가장 안전한 방법: DispatcherFrame을 사용하거나, 로직을 분리.
                // 현재 구조상 CommandHandler의 SaveTabAsync는 await를 사용함 (I/O).
                // => Task.Run(() => ...).Result 는 UI 스레드 접근 시 크래시.
                
                // 해결책: 여기서는 간단히 CommandHandler의 저장 로직 호출하되,
                // Result 값만 확인함. SaveTabAsync가 내부적으로 비동기 I/O만 쓰면 되는데
                // UI 접근(SetStatus)이 있어서 문제됨.
                
                // => UI 멈춤 감수하고 동기화 호출.
                var saveTask = _commandHandler.SaveTabAsync(tab);
                // DispatcherLoop를 돌려서 완료 대기
                System.Windows.Threading.DispatcherFrame frame = new System.Windows.Threading.DispatcherFrame();
                saveTask.ContinueWith(_ => frame.Continue = false);
                System.Windows.Threading.Dispatcher.PushFrame(frame);
                
                return saveTask.Result; // True=Success, False=Fail
            }
            else if (result == ConfirmResult.No)
            {
                return true; // 저장 안함 = 닫기 허용
            }
            
            return false; // 취소 (Cancel)
        }

        private void SaveSession()
        {
            var settings = _settingsService.LoadSettings();
            
            // Save Open Files
            settings.OpenFiles = new System.Collections.Generic.List<string>();
            foreach(TabItem tab in EditorTabs.Items) 
            {
                if (tab.Tag is string path && !string.IsNullOrEmpty(path) && File.Exists(path)) 
                    settings.OpenFiles.Add(path);
            }

            // Save Last Directory
            if (FileTree.Items.Count > 0 && FileTree.Items[0] is TreeViewItem root && root.Tag is string dirPath)
            {
                settings.LastDirectory = dirPath;
            }

            _settingsService.SaveSettings(settings);
        }

        // ========== PUBLIC APIs for COMMANDS ==========

        public void CreateNewTab(string title, string content = "")
        {
            var newTab = new TabItem
            {
                Header = title,
                Tag = "", // Default empty path
                AllowDrop = true
            };
            
            // 에디터 생성
            var editor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                ShowLineNumbers = true,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)),
                BorderThickness = new Thickness(0),
                Document = new ICSharpCode.AvalonEdit.Document.TextDocument(content),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(8, 4, 8, 4)
            };
            
            // Apply Settings & Highlighting
            _commandHandler.ApplySettingsToEditor(editor);

            // 옵션 설정
            editor.Options.ShowSpaces = false;
            editor.Options.HighlightCurrentLine = true;
            
            // VS Code 스타일 하이라이트 색상
            editor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromRgb(40, 40, 45));
            editor.TextArea.TextView.CurrentLineBorder = new Pen(Brushes.Transparent, 0);
            editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(80, 0, 122, 204));
            editor.TextArea.SelectionForeground = null;
            
            // 이벤트 연결
            editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            editor.TextChanged += Editor_TextChanged;
            editor.TextArea.TextView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;

            // 폴딩 매니저 설치
            var foldingManager = FoldingManager.Install(editor.TextArea);
            _foldingManagers[editor] = foldingManager;
            _foldingStrategy.UpdateFoldings(foldingManager, editor.Document);

            // [NEW] ModernFoldingMargin (Chevron UI) 적용
            var oldMargin = editor.TextArea.LeftMargins.OfType<FoldingMargin>().FirstOrDefault();
            if (oldMargin != null)
            {
                int index = editor.TextArea.LeftMargins.IndexOf(oldMargin);
                editor.TextArea.LeftMargins.RemoveAt(index);
                editor.TextArea.LeftMargins.Insert(index, new ModernFoldingMargin());
            }

            newTab.Content = editor;
            EditorTabs.Items.Add(newTab);
            EditorTabs.SelectedItem = newTab;
            
            UpdateTitle(title);
            UpdateMinimap();

            // [FIX] 새 탭 생성 시 즉시 포커스
            editor.Loaded += (s, e) => {
                editor.Focus();
                Keyboard.Focus(editor);
            };
            // 이미 Loaded 된 경우 대비
            if (editor.IsLoaded)
            {
                editor.Focus();
                Keyboard.Focus(editor);
            }
        }

        public void MarkTabAsModified(TabItem tab)
        {
            string header = tab.Header.ToString() ?? "";
            if (!header.EndsWith("*"))
            {
                tab.Header = header + "*";
            }
        }

        public void ClearTabModified(TabItem tab)
        {
            string header = tab.Header.ToString() ?? "";
            if (header.EndsWith("*"))
            {
                tab.Header = header.Substring(0, header.Length - 1);
            }
        }

        public async Task<bool> ConfirmSaveIfModifiedAsync(TabItem tab)
        {
            string header = tab.Header.ToString() ?? "";
            if (!header.EndsWith("*")) return true;

            string fileName = header.TrimEnd('*');
            var result = _dialogService.ShowConfirmDialog($"'{fileName}'의 변경 내용을 저장하시겠습니까?");

            if (result == ConfirmResult.Yes)
            {
                // 저장 실행 (비동기로 대기)
                EditorTabs.SelectedItem = tab;
                return await _commandHandler.SaveTabAsync(tab);
            }
            else if (result == ConfirmResult.No)
            {
                return true; // 저장 안 하고 닫기 허용
            }
            
            return false; // 취소 -> 닫기 중단
        }

        public void UpdateTitle(string title)
        {
            TitleText.Text = $"GravisNC - {title}";
        }

        public void SetStatus(string message)
        {
            StatusText.Text = message;
        }

        public void ToggleExplorer()
        {
            ExplorerColumn.Width = ExplorerColumn.Width.Value > 0 ? new GridLength(0) : new GridLength(200);
        }

        public void OpenFolderDialog()
        {
             // Handled by CommandHandler Logic mainly, but exposes Dialog if needed
        }

        // ========== INTERNAL LOGIC ==========

        private void EditorTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditorTabs.SelectedItem is TabItem tab)
            {
                string path = tab.Tag as string ?? "📎 경로 없음";
                UpdateBreadcrumb(path);
                UpdateTitle(tab.Header?.ToString() ?? "");
                
                // Force Minimap Update for new tab
                // Event needs to happen after layout update sometimes, but direct call is usually fine if element exists
                Dispatcher.InvokeAsync(() => UpdateMinimap(), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        public void UpdateBreadcrumb(string path)
        {
            if (EditorTabs.Template.FindName("BreadcrumbText", EditorTabs) is TextBlock textBlock)
            {
                textBlock.Text = string.IsNullOrEmpty(path) ? "📎 경로 없음" : $"📎 {path}";
            }
        }

        private async void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TabItem tab)
            {
                if (await ConfirmSaveIfModifiedAsync(tab))
                {
                    EditorTabs.Items.Remove(tab);
                }
            }
        }

        // ========== TAB DRAG-DROP REORDERING ==========
        
        public void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TabItem tab)
            {
                // 탭 헤더 영역에서만 드래그 시작 (상단 44px)
                Point pos = e.GetPosition(tab);
                if (pos.Y > 44)
                {
                    _draggedTab = null;
                    return;
                }
                
                _draggedTab = tab;
                _dragStartPoint = e.GetPosition(EditorTabs);
            }
        }

        public void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedTab == null)
                return;

            Point currentPos = e.GetPosition(EditorTabs);
            Vector diff = _dragStartPoint - currentPos;

            // Check if mouse moved enough to start drag
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop(_draggedTab, _draggedTab, DragDropEffects.Move);
            }
        }

        public void TabItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(TabItem)) is TabItem sourceTab && sender is TabItem targetTab)
            {
                if (sourceTab == targetTab) return;

                int sourceIndex = EditorTabs.Items.IndexOf(sourceTab);
                int targetIndex = EditorTabs.Items.IndexOf(targetTab);

                if (sourceIndex < 0 || targetIndex < 0) return;

                EditorTabs.Items.Remove(sourceTab);
                EditorTabs.Items.Insert(targetIndex, sourceTab);
                EditorTabs.SelectedItem = sourceTab;
            }
            
            // Hide drop indicator
            if (sender is TabItem dropTab)
            {
                var indicator = FindVisualChild<Border>(dropTab, "DropIndicator");
                if (indicator != null) indicator.Visibility = Visibility.Collapsed;
            }
            _draggedTab = null;
        }

        public void TabItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(TabItem)) is TabItem sourceTab && sender is TabItem targetTab && sourceTab != targetTab)
            {
                e.Effects = DragDropEffects.Move;
                
                // Show drop indicator
                var indicator = FindVisualChild<Border>(targetTab, "DropIndicator");
                if (indicator != null) indicator.Visibility = Visibility.Visible;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        public void TabItem_DragLeave(object sender, DragEventArgs e)
        {
            // Hide drop indicator
            if (sender is TabItem tab)
            {
                var indicator = FindVisualChild<Border>(tab, "DropIndicator");
                if (indicator != null) indicator.Visibility = Visibility.Collapsed;
            }
        }

        private TextEditor? GetCurrentEditor()
        {
            if (EditorTabs.SelectedItem is TabItem tab && tab.Content is TextEditor editor)
            {
                return editor;
            }
            return null;
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            if (sender is ICSharpCode.AvalonEdit.Editing.Caret caret)
            {
                StatusText.Text = $"Ln {caret.Line}, Col {caret.Column}";
            }
        }

        // ========== FOLDER OPERATIONS ==========
        
        public void LoadFolderTree(string path)
        {
            FileTree.Items.Clear();
            var rootItem = new TreeViewItem 
            { 
                Header = $"📁 {Path.GetFileName(path)}",
                Tag = path,
                IsExpanded = true,
                ContextMenu = CreateContextMenu(path)
            };
            
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirItem = new TreeViewItem { Header = $"📁 {Path.GetFileName(dir)}", Tag = dir, ContextMenu = CreateContextMenu(dir) };
                    dirItem.Items.Add(null); // Placeholder
                    dirItem.Expanded += FolderItem_Expanded;
                    rootItem.Items.Add(dirItem);
                }
                
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileItem = new TreeViewItem { Header = $"📄 {Path.GetFileName(file)}", Tag = file, ContextMenu = CreateContextMenu(file) };
                    fileItem.MouseDoubleClick += FileItem_DoubleClick;
                    rootItem.Items.Add(fileItem);
                }
            }
            catch { }
            
            FileTree.Items.Add(rootItem);
        }



        private void FolderItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.Tag is string path)
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    item.Items.Clear();
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(path))
                        {
                            var dirItem = new TreeViewItem { Header = $"📁 {Path.GetFileName(dir)}", Tag = dir, ContextMenu = CreateContextMenu(dir) };
                            dirItem.Items.Add(null);
                            dirItem.Expanded += FolderItem_Expanded;
                            item.Items.Add(dirItem);
                        }
                        foreach (var file in Directory.GetFiles(path))
                        {
                            var fileItem = new TreeViewItem { Header = $"📄 {Path.GetFileName(file)}", Tag = file, ContextMenu = CreateContextMenu(file) };
                            fileItem.MouseDoubleClick += FileItem_DoubleClick;
                            item.Items.Add(fileItem);
                        }
                    }
                    catch { }
                }
            }
        }

        public void TreeViewItem_Selected_OpenFolder(object sender, RoutedEventArgs e)
        {
            AppCommands.OpenFolder.Execute(null, this);
        }

        private ContextMenu CreateContextMenu(string path)
        {
             var menu = new ContextMenu();
             // Explorer Operations
             menu.Items.Add(new MenuItem { Header = "복사", Command = AppCommands.Copy, CommandParameter = path });
             menu.Items.Add(new MenuItem { Header = "붙여넣기", Command = AppCommands.Paste, CommandParameter = path });
             menu.Items.Add(new Separator());
             menu.Items.Add(new MenuItem { Header = "이름 바꾸기", Command = AppCommands.Rename, CommandParameter = path });
             menu.Items.Add(new MenuItem { Header = "삭제", Command = AppCommands.Delete, CommandParameter = path });
             menu.Items.Add(new Separator());
             menu.Items.Add(new MenuItem { Header = "파일 탐색기에서 열기", Command = AppCommands.RevealInExplorer, CommandParameter = path });

             return menu;
        }

        private async void FileItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && item.Tag is string filePath)
            {
                try
                {
                    // Use IFileService instead of System.IO.File
                    string content = await _fileService.ReadAllTextAsync(filePath);
                    string fileName = Path.GetFileName(filePath);
                    CreateNewTab(fileName, content);
                    
                    if (EditorTabs.SelectedItem is TabItem tab)
                    {
                        tab.Tag = filePath;
                        UpdateBreadcrumb(filePath);
                    }
                }
                catch { }
                e.Handled = true;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) { Close(); }
        private void Minimize_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        private void Maximize_Click(object sender, RoutedEventArgs e) 
        { 
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; 
        }

        private void CloseButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn) btn.Background = Brushes.Red;
        }

        private void CloseButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn) btn.Background = Brushes.Transparent;
        }

        // ========== FIND & REPLACE ==========

        private int _lastFindIndex = 0;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // Ctrl+F: Find
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowFindPanel(false);
                e.Handled = true;
            }
            // Ctrl+H: Replace
            else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowFindPanel(true);
                e.Handled = true;
            }
            // Escape: Close Find Panel
            else if (e.Key == Key.Escape && FindReplacePanel.Visibility == Visibility.Visible)
            {
                FindReplacePanel.Visibility = Visibility.Collapsed;
                GetCurrentEditor()?.Focus();
                e.Handled = true;
            }
            // F3: Find Next
            else if (e.Key == Key.F3 && FindReplacePanel.Visibility == Visibility.Visible)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    FindPrevious();
                else
                    FindNext();
                e.Handled = true;
            }
            // Ctrl+PageUp: Previous Tab
            else if (e.Key == Key.PageUp && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                 if (EditorTabs.Items.Count > 1)
                 {
                     int newIndex = EditorTabs.SelectedIndex - 1;
                     if (newIndex < 0) newIndex = EditorTabs.Items.Count - 1;
                     EditorTabs.SelectedIndex = newIndex;
                 }
                 e.Handled = true;
            }
            // Ctrl+PageDown: Next Tab
            else if (e.Key == Key.PageDown && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                 if (EditorTabs.Items.Count > 1)
                 {
                     int newIndex = EditorTabs.SelectedIndex + 1;
                     if (newIndex >= EditorTabs.Items.Count) newIndex = 0;
                     EditorTabs.SelectedIndex = newIndex;
                 }
                 e.Handled = true;
            }
        }

        private void ShowFindPanel(bool showReplace)
        {
            FindReplacePanel.Visibility = Visibility.Visible;
            ReplaceRow.Visibility = showReplace ? Visibility.Visible : Visibility.Collapsed;
            
            // TogglePath 회전 (90도 = 아래쪽, 0도 = 오른쪽)
            if (TogglePath.RenderTransform is RotateTransform rotate)
            {
                rotate.Angle = showReplace ? 90 : 0;
            }

            FindTextBox.Focus();
            FindTextBox.SelectAll();
            
            // Pre-fill with selected text
            var editor = GetCurrentEditor();
            if (editor != null && !string.IsNullOrEmpty(editor.SelectedText))
            {
                FindTextBox.Text = editor.SelectedText;
            }
        }

        private void ToggleReplace_Click(object sender, RoutedEventArgs e)
        {
            bool showReplace = ReplaceRow.Visibility != Visibility.Visible;
            ReplaceRow.Visibility = showReplace ? Visibility.Visible : Visibility.Collapsed;
            
            if (TogglePath.RenderTransform is RotateTransform rotate)
            {
                rotate.Angle = showReplace ? 90 : 0;
            }
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNext();
                e.Handled = true;
            }
        }

        private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();
        private void FindPrevious_Click(object sender, RoutedEventArgs e) => FindPrevious();
        private void CloseFind_Click(object sender, RoutedEventArgs e)
        {
            FindReplacePanel.Visibility = Visibility.Collapsed;
            GetCurrentEditor()?.Focus();
        }

        private void FindNext()
        {
            var editor = GetCurrentEditor();
            if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

            string searchText = FindTextBox.Text;
            string text = editor.Text;
            int startIndex = editor.CaretOffset;

            int foundIndex = text.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (foundIndex == -1 && startIndex > 0)
            {
                // Wrap around
                foundIndex = text.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);
            }

            if (foundIndex >= 0)
            {
                editor.Select(foundIndex, searchText.Length);
                editor.ScrollToLine(editor.Document.GetLineByOffset(foundIndex).LineNumber);
                _lastFindIndex = foundIndex + searchText.Length;
                UpdateFindStatus(foundIndex);
            }
            else
            {
                FindStatus.Text = "결과 없음";
            }
        }

        private void FindPrevious()
        {
            var editor = GetCurrentEditor();
            if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

            string searchText = FindTextBox.Text;
            string text = editor.Text;
            int startIndex = Math.Max(0, editor.SelectionStart - 1);

            int foundIndex = text.LastIndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (foundIndex == -1)
            {
                // Wrap around
                foundIndex = text.LastIndexOf(searchText, text.Length - 1, StringComparison.OrdinalIgnoreCase);
            }

            if (foundIndex >= 0)
            {
                editor.Select(foundIndex, searchText.Length);
                editor.ScrollToLine(editor.Document.GetLineByOffset(foundIndex).LineNumber);
                UpdateFindStatus(foundIndex);
            }
            else
            {
                FindStatus.Text = "결과 없음";
            }
        }

        private void UpdateFindStatus(int foundIndex)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            // Count total matches
            int count = 0;
            int idx = 0;
            while ((idx = editor.Text.IndexOf(FindTextBox.Text, idx, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                idx++;
            }

            FindStatus.Text = $"{count}개 중 일치";
        }

        private void ReplaceOne_Click(object sender, RoutedEventArgs e)
        {
            var editor = GetCurrentEditor();
            if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

            if (editor.SelectedText.Equals(FindTextBox.Text, StringComparison.OrdinalIgnoreCase))
            {
                editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, ReplaceTextBox.Text);
            }
            FindNext();
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            var editor = GetCurrentEditor();
            if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

            string newText = System.Text.RegularExpressions.Regex.Replace(
                editor.Text, 
                System.Text.RegularExpressions.Regex.Escape(FindTextBox.Text), 
                ReplaceTextBox.Text, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            int count = (editor.Text.Length - newText.Length) / (FindTextBox.Text.Length - ReplaceTextBox.Text.Length);
            editor.Document.Text = newText;
            FindStatus.Text = $"{Math.Abs(count)}개 바꿈";
        }

        // ========== MINIMAP ==========

        private void Editor_TextChanged(object? sender, EventArgs e)
        {
            // 성능 최적화: 타이핑 중에는 미니맵과 폴딩을 업데이트하지 않고
            // 입력을 멈춘 뒤 타이머가 돌 때 한꺼번에 처리합니다.
            
            _foldingUpdateTimer.Stop();
            _foldingUpdateTimer.Start();

            // 수정 상태 표시
            if (sender is TextEditor editor && editor.Parent is TabItem tab)
            {
                MarkTabAsModified(tab);
            }
        }

        private void FoldingUpdateTimer_Tick(object? sender, EventArgs e)
        {
            _foldingUpdateTimer.Stop();
            var editor = GetCurrentEditor();
            if (editor == null) return;

            // 1. 폴딩 업데이트
            if (_foldingManagers.TryGetValue(editor, out var manager))
            {
                _foldingStrategy.UpdateFoldings(manager, editor.Document);
            }

            // 2. 미니맵 업데이트 (성능 병목 해결)
            UpdateMinimap();
        }

        private void TextView_ScrollOffsetChanged(object? sender, EventArgs e)
        {
            // 키보드 스크롤(PgUp/Dn) 시 VisualLines 업데이트 후 처리하기 위해 지연 실행
            Dispatcher.InvokeAsync(UpdateMinimapViewport, System.Windows.Threading.DispatcherPriority.Render);
        }

        // ========== CODE OPTIMIZER ==========
        
        private readonly WcsOptimizer _wcsOptimizer = new();

        private void OptimizerBtn_Click(object sender, RoutedEventArgs e)
        {
            // 탐색기 패널 숨기고 최적화 패널 표시 (또는 토글)
            if (OptimizerPanel.Visibility == Visibility.Visible)
            {
                OptimizerPanel.Visibility = Visibility.Collapsed;
                ExplorerPanel.Visibility = Visibility.Visible;
                OptimizerBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x85, 0x85, 0x85));
            }
            else
            {
                ExplorerPanel.Visibility = Visibility.Collapsed;
                OptimizerPanel.Visibility = Visibility.Visible;
                OptimizerBtn.Foreground = Brushes.White;
                
                // 미리보기 업데이트
                RefreshOptimizerPreview();
            }
        }

        private void RefreshOptimizerPreview()
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var preview = _wcsOptimizer.GetPreview(editor.Text);
            OperationList.ItemsSource = preview.Operations;
        }

        private void PreviewOptimization_Click(object sender, RoutedEventArgs e)
        {
            RefreshOptimizerPreview();
        }

        private void ApplyOptimization_Click(object sender, RoutedEventArgs e)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            if (ChkZigZag.IsChecked == true)
            {
                string optimized = _wcsOptimizer.OptimizeZigZag(editor.Text);
                editor.Document.Text = optimized;
                
                MessageBox.Show("WCS Zig-zag 최적화가 적용되었습니다.", "코드 최적화", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                RefreshOptimizerPreview();
            }
        }


        // GCode.xshd 정규식 패턴 (컴파일된 정적 인스턴스)
        private static readonly System.Text.RegularExpressions.Regex _commentRegex = 
            new System.Text.RegularExpressions.Regex(@"\([^)]*\)|;.*$", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex _gCodeRegex = 
            new System.Text.RegularExpressions.Regex(@"\bG\d+(\.\d+)?", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex _mCodeRegex = 
            new System.Text.RegularExpressions.Regex(@"\bM\d+(\.\d+)?", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        private static readonly System.Text.RegularExpressions.Regex _coordRegex = 
            new System.Text.RegularExpressions.Regex(@"\b[XYZIJKFSTRxyzijkfstr][-+]?[0-9]*\.?[0-9]+", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex _paramRegex = 
            new System.Text.RegularExpressions.Regex(@"#[0-9]+", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex _numberRegex = 
            new System.Text.RegularExpressions.Regex(@"\b\d+(\.\d+)?\b", System.Text.RegularExpressions.RegexOptions.Compiled);

        private void UpdateMinimap()
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var canvas = FindMinimapCanvas();
            if (canvas == null) return;

            // 라인 높이 및 간격 설정 (간격 최소화)
            double lineHeight = 2.5;
            double gap = 0.5;
            double itemHeight = lineHeight + gap;
            double charWidth = 1.5;
            
            // 캔버스 높이 명시적 설정
            double totalHeight = editor.LineCount * itemHeight;
            canvas.Height = Math.Max(totalHeight, 100);

            // 뷰포트를 제외한 모든 자식 제거
            var viewport = canvas.Children.Cast<UIElement>().OfType<Border>().FirstOrDefault(b => b.Name == "MinimapViewport");
            canvas.Children.Clear();
            if (viewport != null) canvas.Children.Add(viewport);
            else 
            {
                viewport = new Border
                {
                    Name = "MinimapViewport",
                    Width = 78,
                    Height = 0,
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    IsHitTestVisible = false,
                    Focusable = false
                };
                canvas.Children.Add(viewport);
            }

            int renderLimit = Math.Min(editor.LineCount, 2000);
            var highlighting = editor.SyntaxHighlighting;
            var defaultBrush = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));
            var document = editor.Document;
            
            // DocumentHighlighter로 하이라이팅 정보 가져오기
            ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter? highlighter = null;
            if (highlighting != null)
            {
                highlighter = new ICSharpCode.AvalonEdit.Highlighting.DocumentHighlighter(document, highlighting);
            }

            for (int lineNum = 1; lineNum <= renderLimit; lineNum++)
            {
                var docLine = document.GetLineByNumber(lineNum);
                string lineText = document.GetText(docLine.Offset, docLine.Length);
                
                if (string.IsNullOrWhiteSpace(lineText)) continue;

                string displayLine = lineText.Length > 50 ? lineText.Substring(0, 50) : lineText;
                
                // AvalonEdit의 HighlightLine으로 토큰별 색상 가져오기
                var colorMap = new Brush[displayLine.Length];
                for (int c = 0; c < colorMap.Length; c++) colorMap[c] = defaultBrush;

                if (highlighter != null)
                {
                    try
                    {
                        var highlightedLine = highlighter.HighlightLine(lineNum);
                        foreach (var section in highlightedLine.Sections)
                        {
                            if (section.Color?.Foreground != null)
                            {
                                var wpfColor = section.Color.Foreground.GetColor(null) ?? Colors.White;
                                var brush = new SolidColorBrush(wpfColor);
                                brush.Freeze();
                                
                                int start = Math.Max(0, section.Offset - docLine.Offset);
                                int end = Math.Min(displayLine.Length, start + section.Length);
                                
                                for (int c = start; c < end; c++)
                                {
                                    colorMap[c] = brush;
                                }
                            }
                        }
                    }
                    catch { /* 하이라이팅 실패 시 기본 색상 사용 */ }
                }
                // 간단한 Rectangle으로 렌더링 (잔상 제거)
                double x = 4; // 시작 위치
                double y = (lineNum - 1) * itemHeight;
                int segmentStart = 0;
                Brush? currentBrush = colorMap.Length > 0 ? colorMap[0] : Brushes.Gray;

                for (int c = 1; c <= displayLine.Length; c++)
                {
                    Brush? nextBrush = (c < displayLine.Length) ? colorMap[c] : null;
                    
                    if (nextBrush != currentBrush || c == displayLine.Length)
                    {
                        int segmentLen = c - segmentStart;
                        double segmentWidth = segmentLen * charWidth;

                        if (currentBrush != null)
                        {
                            var rect = new System.Windows.Shapes.Rectangle
                            {
                                Width = segmentWidth,
                                Height = lineHeight,
                                Fill = currentBrush
                            };
                            Canvas.SetLeft(rect, x);
                            Canvas.SetTop(rect, y);
                            canvas.Children.Add(rect);
                        }
                        
                        x += segmentWidth;
                        segmentStart = c;
                        currentBrush = nextBrush;
                    }
                }
            }

            Canvas.SetZIndex(viewport, 100);
            UpdateMinimapViewport();
        }


        private void UpdateMinimapViewport()
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var canvas = FindMinimapCanvas();
            if (canvas == null) return;

            var viewport = canvas.Children.Cast<UIElement>().OfType<Border>().FirstOrDefault(b => b.Name == "MinimapViewport");
            if (viewport == null) return;

            try
            {
                var textView = editor.TextArea.TextView;
                // 시각적 라인이 아직 계산되지 않았으면 스킵
                if (!textView.VisualLinesValid) return;

                // 1. 현재 뷰포트의 시작/끝 시각적 위치에 해당하는 문서 라인 찾기
                // Folding이 적용되어 있어도 '보이는' 영역의 첫 줄과 끝 줄을 정확히 찾음
                var firstDocLine = textView.GetDocumentLineByVisualTop(editor.VerticalOffset);
                var lastDocLine = textView.GetDocumentLineByVisualTop(editor.VerticalOffset + editor.ViewportHeight);

                if (firstDocLine == null) return;

                int firstLineNum = firstDocLine.LineNumber;
                int lastLineNum = lastDocLine?.LineNumber ?? editor.LineCount;

                // 2. 미니맵 좌표계로 변환 (UpdateMinimap의 itemHeight와 일치해야 함)
                double itemHeight = 3.0; 
                double viewportTop = (firstLineNum - 1) * itemHeight;
                double viewportBottom = lastLineNum * itemHeight;
                double viewportHeight = Math.Max(20, viewportBottom - viewportTop);

                // 3. 뷰포트 UI 업데이트
                viewport.Height = viewportHeight;
                Canvas.SetTop(viewport, viewportTop);

                // 4. 미니맵 ScrollViewer 동기화
                // 뷰포트가 미니맵 영역을 벗어나지 않도록 스크롤 추적
                var scrollViewer = FindVisualChild<ScrollViewer>(EditorTabs, "MinimapScrollViewer");
                if (scrollViewer != null)
                {
                    // 뷰포트를 화면 중앙에 위치시키기 위해 오프셋 조정
                    double targetOffset = viewportTop - (scrollViewer.ViewportHeight / 2) + (viewportHeight / 2);
                    scrollViewer.ScrollToVerticalOffset(targetOffset);
                }
            }
            catch 
            {
                // 레이아웃 업데이트 중 일시적 오류 무시
            }
        }

        // ========== MINIMAP INTERACTION ==========

        private bool _isDraggingMinimap = false;

        private void Minimap_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingMinimap = true;
            if (sender is UIElement element)
            {
                element.CaptureMouse();
            }
            GetCurrentEditor()?.Focus();
            MoveEditorToMinimapClick(e.GetPosition(FindMinimapCanvas()));
        }

        private void Minimap_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingMinimap)
            {
                MoveEditorToMinimapClick(e.GetPosition(FindMinimapCanvas()));
            }
        }

        private void Minimap_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingMinimap = false;
            if (sender is UIElement element) element.ReleaseMouseCapture();
        }

        private void MoveEditorToMinimapClick(Point clickPoint)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var canvas = FindMinimapCanvas();
            if (canvas == null || canvas.Height <= 0) return;

            // 클릭한 절대 Y 좌표를 전체 높이 대비 비율로 환산
            double ratio = clickPoint.Y / canvas.Height;
            ratio = Math.Max(0, Math.Min(1, ratio));

            // 에디터 스크롤 이동
            editor.ScrollToVerticalOffset(editor.ExtentHeight * ratio);
        }



        private Canvas? FindMinimapCanvas()
        {
            // EditorTabs에서 MinimapCanvas 찾기
            return FindVisualChild<Canvas>(EditorTabs, "MinimapCanvas");
        }

        private T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                    return element;
                
                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }


    // VisualHost: DrawingVisual을 Canvas에 추가하기 위한 헬퍼 클래스
    public class VisualHost : FrameworkElement
    {
        private Visual? _visual;

        public Visual? Visual
        {
            get => _visual;
            set
            {
                if (_visual != null)
                    RemoveVisualChild(_visual);
                
                _visual = value;
                
                if (_visual != null)
                    AddVisualChild(_visual);
            }
        }

        protected override int VisualChildrenCount => _visual != null ? 1 : 0;

        protected override Visual GetVisualChild(int index)
        {
            if (_visual == null || index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _visual;
        }
    }
        // ===================================
        // Structure Panel Logic
        // ===================================
        private void StructureBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleSidePanel(StructurePanel);
            if (StructurePanel.Visibility == Visibility.Visible)
            {
                RefreshStructure();
            }
        }

        private void RefreshStructure_Click(object sender, RoutedEventArgs e) => RefreshStructure();

        private void RefreshStructure()
        {
            var editor = GetCurrentEditor();
            if (editor == null)
            {
                StructureList.ItemsSource = null;
                return;
            }

            string text = editor.Text;
            var blocks = _parser.Parse(text);
            StructureList.ItemsSource = blocks;
        }

        private void SortByTool_Click(object sender, RoutedEventArgs e)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var blocks = StructureList.ItemsSource as List<GCodeBlock>;
            if (blocks == null || !blocks.Any()) return;

            // Simple Sort: Group by Tool Number
            var sortedBlocks = blocks.OrderBy(b => b.ToolNumber).ToList();
            
            // Reconstruct Text
            if (MessageBox.Show("공구 번호 순서로 코드를 재배치하시겠습니까?\n(경고: 이 작업은 되돌릴 수 없을 수 있습니다.)", 
                                "구조 변경", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string newText = _parser.Reconstruct(sortedBlocks);
                editor.Text = newText;
                RefreshStructure(); // Reload list
            }
        }

        private void StructureList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StructureList.SelectedItem is GCodeBlock block)
            {
                var editor = GetCurrentEditor();
                if (editor == null) return;

                // Sync Cursor
                try
                {
                    // Safe logic using AvalonEdit Document
                    if (block.StartLine < editor.Document.LineCount)
                    {
                        // AvalonEdit lines are 1-based
                        var lineNum = block.StartLine + 1;
                        var lineObject = editor.Document.GetLineByNumber(lineNum); 
                        editor.ScrollToLine(lineNum);
                        editor.Select(lineObject.Offset, lineObject.Length);
                        
                        // Set Caret
                        editor.TextArea.Caret.Line = lineNum;
                        editor.TextArea.Caret.Column = 0;
                        editor.Focus();
                    }
                }
                catch { /* Ignore Range Errors */ }
            }
        }

        private void ToggleSidePanel(FrameworkElement targetPanel)
        {
            bool isOpening = targetPanel.Visibility != Visibility.Visible;

            // Close others
            ExplorerPanel.Visibility = Visibility.Collapsed;
            OptimizerPanel.Visibility = Visibility.Collapsed;
            StructurePanel.Visibility = Visibility.Collapsed;

            if (isOpening)
            {
                targetPanel.Visibility = Visibility.Visible;
                Grid.SetColumn(EditorTabs, 2); // Ensure Content is in Col 2
            }
        }



    }
}

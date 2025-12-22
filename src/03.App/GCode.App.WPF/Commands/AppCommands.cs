using System.Windows.Input;

namespace GCode.App.WPF.Commands
{
    public static class AppCommands
    {
        // File Operations
        public static readonly RoutedUICommand NewFile = new RoutedUICommand
            ("새로 만들기", "NewFile", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control) });

        public static readonly RoutedUICommand OpenFile = new RoutedUICommand
            ("열기", "OpenFile", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control) });

        public static readonly RoutedUICommand OpenFolder = new RoutedUICommand
            ("폴더 열기", "OpenFolder", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand SaveFile = new RoutedUICommand
            ("저장", "SaveFile", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control) });

        public static readonly RoutedUICommand SaveAsFile = new RoutedUICommand
            ("다른 이름으로 저장", "SaveAsFile", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand Exit = new RoutedUICommand(
            "종료", "Exit", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.F4, ModifierKeys.Alt) });

        public static readonly RoutedUICommand CloseTab = new RoutedUICommand(
            "탭 닫기", "CloseTab", typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control) });

        // View Operations
        public static readonly RoutedUICommand ZoomIn = new RoutedUICommand
            ("확대", "ZoomIn", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.OemPlus, ModifierKeys.Control), new KeyGesture(Key.Add, ModifierKeys.Control) });

        public static readonly RoutedUICommand ZoomOut = new RoutedUICommand
            ("축소", "ZoomOut", typeof(AppCommands), 
            new InputGestureCollection { new KeyGesture(Key.OemMinus, ModifierKeys.Control), new KeyGesture(Key.Subtract, ModifierKeys.Control) });

        public static RoutedUICommand ToggleExplorer { get; } = new RoutedUICommand(
            "탐색기 토글", "ToggleExplorer", typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Shift) });

        // Settings
        public static RoutedUICommand OpenSettings { get; } = new RoutedUICommand(
            "설정", "OpenSettings", typeof(AppCommands),
            new InputGestureCollection { new KeyGesture(Key.OemComma, ModifierKeys.Control) }); // Ctrl + ,

        // Explorer Context Commands
        public static readonly RoutedUICommand Rename = new RoutedUICommand("이름 바꾸기", "Rename", typeof(AppCommands), new InputGestureCollection { new KeyGesture(Key.F2) });
        public static readonly RoutedUICommand Delete = new RoutedUICommand("삭제", "Delete", typeof(AppCommands), new InputGestureCollection { new KeyGesture(Key.Delete) });
        public static readonly RoutedUICommand Copy = new RoutedUICommand("복사", "Copy", typeof(AppCommands), new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control) });
        public static readonly RoutedUICommand Paste = new RoutedUICommand("붙여넣기", "Paste", typeof(AppCommands), new InputGestureCollection { new KeyGesture(Key.V, ModifierKeys.Control) });
        public static readonly RoutedUICommand RevealInExplorer = new RoutedUICommand("파일 탐색기에서 열기", "RevealInExplorer", typeof(AppCommands));
        public static readonly RoutedUICommand CloseFolder = new RoutedUICommand("폴더 닫기", "CloseFolder", typeof(AppCommands));

        // Default Edit commands (Cut, Copy, Paste, Undo, Redo) are used from ApplicationCommands
    }
}

# Project Change Log

## [2024-12-22] Session 1 - Phase 1 Complete 🎉

### Project Initialization
*   Defined Project Identity: **GravisNC** (High-performance G-Code Editor).
*   Established 3-Tier Architecture spec (Core, Modules, App).
*   Created Meta Documentation (`README.md`, `SPEC.md`, `TODO.md`, `UIUX.md`, `STYLE.md`).

### UI Development
*   Implemented VS Code Style **Integrated Title Bar** (WindowChrome).
*   Applied **Dark Theme** with custom styles for Menu, ScrollBar, TabItem.
*   Added **Window Drop Shadow** and **Rounded Corners (10px)**.
*   Increased UI element sizes for modern look (Windows 11 Notepad style).
*   Implemented **Multi-Tab (TabControl)** with close button per tab.
*   Connected **Status Bar** with real-time Caret position (Ln, Col).

### Feature Implementation
*   **File Menu:** 새로 만들기, 열기, 저장, 다른 이름으로 저장, 종료.
*   **Edit Menu:** 실행 취소, 다시 실행, 잘라내기, 복사, 붙여넣기.
*   **View Menu:** 확대, 축소 (Font Size).
*   Window Title dynamically updates with current file name.

### Technical Notes
*   AvalonEdit used for high-performance text editing.
*   `AllowsTransparency="True"` + `WindowStyle="None"` for custom chrome.
*   Tab management via dynamic `TabItem` creation in code-behind.

## [2024-12-23] Session 2 - Editor Enhancements & UI Refinement

### Editor Features
*   **Find & Replace:** VS Code style widget implemented with find next/prev, replace, replace all features.
*   **Minimap:** Interactive code preview with scroll synchronization. Optimized for performance with rendering limits.
*   **Line Numbers:** Enabled by default in AvalonEdit.

### UI Improvements
*   **Find Widget UI:** 
    *   Refined layout to match VS Code (compact, dark theme).
    *   Replaced icons with clear text buttons (위로, 아래로, 닫기).
    *   Unified button styling with distinct background and border for visibility.
    *   Fixed layout gaps for a polished look.
*   **Minimap UI:**
    *   Wrapped in ScrollViewer for proper scrolling.
    *   Visual artifact fixes (yellow tint removal).
*   **Code Structure Refinement:**
    *   Refactored Menu/Button logic to `AppCommands` & `EditorCommandHandler` pattern.
    *   Fixed `Zoom In/Out` implementation and keyboard shortcuts (`Ctrl + +/-`).
    *   Restored missing Window Control handlers (`Minimize`, `Maximize`).

### Architectural Refactoring
*   **3-Tier Architecture Implemented:**
    *   **Core Layer (`src/01.Core`):** Created `GCode.Core` library defining `IFileService`, `IDialogService`.
    *   **Modules Layer (`src/02.Modules`):** Created `GCode.Modules.FileIO` implementing core services.
    *   **App Layer (`src/03.App`):** Refactored `GCode.App.WPF` to use **Dependency Injection (DI)**.
*   **Decoupling:** `MainWindow` and `EditorCommandHandler` no longer depend on `System.IO` directly.
*   **(Phase 3) Visuals & Settings Implemented:**
    *   **Settings Module:** Created `GCode.Modules.Settings` for persistent JSON configuration (`%AppData%/GravisNC/settings.json`).
    *   **Syntax Highlighting:** Added `GCode.xshd` resource for coloring G-Codes, M-Codes, and Coordinates.
    *   **Settings UI:** Added `SettingsWindow` (Ctrl+,) to configure Font Family, Size, and Weight.
    *   **Integration:** Settings are applied immediately to all open editors upon save.

## [2024-12-23] Session 3 - UI/UX Refinement & Persistence

### UI/UX Refinement
*   **Tab System Upgrade:**
    *   **Rounded Design:** Tabs now feature rounded top corners (`CornerRadius="8,8,0,0"`) and larger padding (`15,8`) for better clickability.
    *   **Close Button Integration:** Dedicated close button (`✕`) added inside each tab header.
    *   **New Tab Button:** `+` button added to the Tab Bar for quick new file creation.
*   **Tab Navigation:** Implemented `Ctrl + PageUp / PageDown` shortcuts for switching tabs (Left/Right).
*   **Context Menu:** Applied Dark Theme (`#252526` Background) to File Explorer context menus.
*   **Empty State:** Application now starts with a completely empty editor (no "Untitled 1") if no previous session exists.

### Persistence Implementation
*   **Session Restore:** 
    *   Automatically restores open files from the previous session on startup.
    *   Restores the last opened folder (Folder View) in Explorer tree.
*   **EditorSettings Update:** stored open file paths and last directory path in `settings.json`.
*   **Logic Optimization:** `OnClosing` event captures current state to ensure seamless continuity.

## [2024-12-23] Session 4 - UI Polish & Bug Fixes
*   **Minimap Logic Fix:**
    *   Resolved issue where Minimap would not update/appear when switching tabs.
    *   Added event hook `EditorTabs_SelectionChanged` to force `UpdateMinimap()` (async).
*   **Scrollbar UX:**
    *   Enforced `MinHeight="25"` for ScrollBar Thumb to prevent it from becoming too small (1px) on large files.
*   **Explorer Theming:**
    *   Implemented proper **Dark Mode Selection** for File Explorer (`TreeViewItem`).
    *   Active Selection: `#094771` (VS Code Blue).
    *   Inactive Selection: `#37373D`.

## [2024-12-23] Session 5 - UI/UX Polish & Minimap Logic
*   **Custom Confirm Dialog:**
    *   Replaced system `MessageBox` with `ConfirmDialog` (WindowChrome, Dark Theme).
    *   Implemented `ModernDialogService` for consistent dialog handling.
    *   Styled Buttons: Primary(Save), Secondary(Don't Save), Ghost(Cancel).
*   **Minimap Sync Logic Upgrade:**
    *   Fixed keyboard scroll synchronization issue.
    *   Refactored `UpdateMinimapViewport` to use `GetDocumentLineByVisualTop` for accurate mapping with Code Folding.
    *   Implemented `Dispatcher.InvokeAsync(priority: Render)` to handle layout update delays during PageUp/Down.

## [2025-12-22] Session 6 - Context Resume & Maintenance

### Maintenance
*   **Log Cleanup:** Deleted build error logs and warnings (`build_*.txt`, `crash.log`).
*   **Documentation:** Verified `TODO.md` and `LOG.md` status.

## [2025-12-23] Session 7 - Build Fixes & G-Code Structure Feature

### Critical Fixes
*   **Compilation Resolved:** Fixed persistent `CS0103` and `CS0102` errors in `MainWindow.xaml` and code-behind.
*   **SDK Configuration:** Resolved phantom `MSB4276` errors by correcting XAML/C# logic mismatches.
*   **Code Structure:** Corrected a critical brace mismatch in `MainWindow.xaml.cs` that was isolating methods from the class scope.

### New Features
*   **G-Code Structure Panel:**
    *   Added **Structure Panel** (Right Sidebar) for visualizing G-Code blocks.
    *   Implemented `GCodeParserService` to split code by Tool Change (T#) and M6.
    *   **Interactive List:** Clicking a block synchronizes the editor cursor and scroll position.
    *   **UI Integration:** Toggle button added to Activity Bar (Left).

### Refinement
*   **Refactored `MainWindow.xaml`:** Cleaned up duplicate panel definitions and consolidated UI logic.
*   **Build Status:** Restored to **Build Succeeded** state (Exit Code 0).

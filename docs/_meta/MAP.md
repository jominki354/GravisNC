# Project Directory Map
(Dynamic Tree - Update via `find_by_name` or `list_dir`)

## Root Layout
.
├── docs
│   └── _meta          # Single Source of Truth (Project definitions)
└── src
    ├── 01.Core        # Business Logic (Interfaces, Models)
    ├── 02.Modules     # Functional Modules (Services)
    └── 03.App         # Main Application Entry

## Module Detail
src/01.Core
└── GCode.Core         # IFileService, IDialogService

src/02.Modules
├── GCode.Modules.FileIO    # FileService, DialogService
└── GCode.Modules.Settings  # SettingsService, Persistence

src/03.App
└── GCode.App.WPF           # DI Container, MainWindow
    ├── Commands            # AppCommands, EditorCommandHandler
    ├── Views               # SettingsWindow
    └── Resources           # GCode.xshd

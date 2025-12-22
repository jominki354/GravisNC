# GravisNC UI/UX Design Reference

> **Source:** VS Code Dark+ Theme  
> **Reference:** `github.com/microsoft/vscode` (클론: `d:/vscode-ref`)

---

## 1. 색상 팔레트 (Color Palette)

### Core Colors
| Token | Hex | 용도 |
|-------|-----|------|
| `VsBackground` | `#1E1E1E` | 윈도우/에디터 배경 |
| `VsTitleBar` | `#3C3C3C` | 타이틀바, 메뉴 배경 |
| `VsFindWidgetBg` | `#252526` | 찾기 위젯 배경 |
| `VsInputBg` | `#3C3C3C` | 입력창 배경 |
| `VsInputBorder` | `#5A5A5A` | 입력창 테두리 |
| `VsButtonBg` | `#3C3C3C` | 버튼 배경 (Normal) |
| `VsButtonBorder` | `#505050` | 버튼 테두리 (Normal) |
| `VsActivityBar` | `#333333` | 좌측 Activity Bar |
| `VsSidebar` | `#252526` | Explorer 패널 |
| `VsTabBarBg` | `#252526` | 탭바 배경 |
| `VsTabActiveBg` | `#1E1E1E` | 활성 탭 배경 |
| `VsTabInactiveBg` | `#2D2D2D` | 비활성 탭 배경 |
| `VsMenuBg` | `#252526` | 드롭다운 메뉴 배경 |
| `VsMenuHover` | `#094771` | 메뉴 아이템 호버 (파란색) |
| `VsBorder` | `#3C3C3C` | 테두리, 구분선 |

### Text Colors
| Token | Hex | 용도 |
|-------|-----|------|
| `VsForeground` | `#CCCCCC` | 주 텍스트 |
| `VsForegroundDim` | `#858585` | 보조 텍스트, 비활성 아이콘 |
| `VsShortcutText` | `#6E6E6E` | 단축키 텍스트 |

---

## 2. 레이아웃 크기 (Layout Dimensions)

### Activity Bar
**Source:** `activitybarpart.css`
```css
.part.activitybar { width: 48px; }
.menubar { height: 35px; }
```

| 요소 | 값 |
|------|-----|
| Width | 48px |
| Icon Size | 24px (추정) |
| Button Height | 48px |

### Sidebar (Explorer)
**Source:** `sidebarpart.css`
```css
.sidebar > .title > .title-label h2 { text-transform: uppercase; }
.action-label { width: 28px; height: 22px; }
```

| 요소 | 값 |
|------|-----|
| Default Width | 200px |
| Title Height | 35px |
| Action Icon | 28x22px |

### Menubar
**Source:** `menubar.css`
```css
.menubar-menu-title { padding: 0px 8px; border-radius: 5px; }
.fullscreen .menubar:not(.compact) { padding: 4px 5px; }
```

| 요소 | 값 |
|------|-----|
| Title Padding | 0 8px |
| Border Radius | 5px |
| Fullscreen Padding | 4px 5px |

### Tabs
**Source:** `multieditortabscontrol.css`
```css
.tabs-container { height: var(--editor-group-tab-height); } /* 35px */
.tab { padding-left: 10px; }
.tab-label a { font-size: 13px; }
.tab-actions { width: 28px; }
```

| 요소 | 값 |
|------|-----|
| Tab Height | 35px (`--editor-group-tab-height`) |
| Tab Padding Left | 10px |
| Tab Font Size | 13px |
| Close Button Width | 28px |
| Sizing Fit Width | 120px |
| Sizing Shrink Min | 80px |
| Sticky Compact Width | 38px |

### Breadcrumbs
**Source:** `breadcrumbscontrol.css`
```css
.highlighting-tree > .input { padding: 5px 9px; height: 36px; }
.picker-item { line-height: 22px; }
```

| 요소 | 값 |
|------|-----|
| Input Padding | 5px 9px |
| Input Height | 36px |
| Item Line Height | 22px |

---

## 3. 폰트 (Typography)

| 용도 | Font Family | Size |
|------|-------------|------|
| UI 전반 | Segoe UI | 13px |
| 에디터 | Consolas, Menlo | 14px |
| Tab Label | Segoe UI | 13px |
| Shortcut Key | Segoe UI | 12px |

---

## 4. 상호작용 (Interactions)

### Hover States
| 요소 | 효과 |
|------|------|
| Top-level Menu | Background: `#505050` |
| Submenu Item | Background: `#094771` (파란색) |
| Tab (inactive) | Slight brightness increase |
| Activity Bar Icon | Foreground: `#FFFFFF` |

### Focus States
- Outline: 1px solid `var(--vscode-focusBorder)`
- Outline Offset: -1px ~ -8px

### Animation
- Popup: Fade animation
- Duration: ~150ms (추정)

---

## 5. 아이콘 (Icons)

VS Code는 **Codicon** 폰트를 사용합니다.

### 파일 탐색기 아이콘 (Emoji 대체)
| 용도 | Icon |
|------|------|
| 폴더 | 📁 |
| 파일 | 📄 |
| 검색 | 🔍 |
| 설정 | ⚙️ |

---

## 6. WPF 적용 가이드

### App.xaml 리소스 정의
```xml
<Color x:Key="VsBackground">#1E1E1E</Color>
<Color x:Key="VsMenuHover">#094771</Color>
<SolidColorBrush x:Key="WindowBackgroundBrush" Color="{StaticResource VsBackground}"/>
```

### Dialog & Button Styles
*   **Dialog Window:** `WindowChrome` with `CaptionHeight="32"`, `ResizeBorderThickness="5"`.
*   **Button Styles:**
    *   `PrimaryButtonStyle`: Action(Blue) background.
    *   `SecondaryButtonStyle`: Dark Gray background for alternative actions.
    *   `GhostButtonStyle`: Transparent background for cancel/dismiss.

### MenuItem Role 기반 스타일링
- `TopLevelHeader`: 상단 메뉴 (파일, 편집 등)
- `SubmenuItem`: 드롭다운 아이템

### 핵심 원칙
1. **기존 구조 보존** - 전체 파일 덮어쓰기 금지
2. **점진적 수정** - `replace_file_content` 사용
3. **색상 일관성** - 정의된 Color 리소스 사용

---

## 7. 참조 파일 목록

| 파일 경로 | 내용 |
|-----------|------|
| `src/vs/base/browser/ui/menu/menubar.css` | 메뉴바 스타일 |
| `src/vs/workbench/browser/parts/activitybar/media/activitybarpart.css` | Activity Bar |
| `src/vs/workbench/browser/parts/sidebar/media/sidebarpart.css` | Sidebar |
| `src/vs/workbench/browser/parts/editor/media/multieditortabscontrol.css` | 탭 컨트롤 |
| `src/vs/workbench/browser/parts/editor/media/breadcrumbscontrol.css` | Breadcrumb |

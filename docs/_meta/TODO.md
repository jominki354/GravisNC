# Development Roadmap

## Phase 1: The Foundation (에디터 기초) ✅ COMPLETED
기본적인 읽기/쓰기 기능과 모듈 구조를 확립하는 단계입니다.
- [x] **Project Setup**
    - [x] .NET 8 솔루션 및 프로젝트 생성.
    - [x] AvalonEdit NuGet 패키지 설치.
    - [x] App Identity 설정 (GravisNC.exe).
- [x] **UI - Main Layout**
    - [x] VS Code 스타일의 메인 윈도우 레이아웃 구성 (WindowChrome).
    - [x] 상단 메뉴바 (한글화) 및 하단 상태바 배치.
    - [x] 둥근 모서리(CornerRadius=10) 및 그림자 효과 적용.
    - [x] 다크 테마 메뉴 및 스크롤바 스타일 정의.
    - [x] 메뉴/탭 사이즈 확대 (Windows 11 Notepad 스타일).
- [x] **UI - Editor Module**
    - [x] AvalonEdit 컨트롤 배치.
    - [x] **멀티 탭(TabControl) 기능 구현.**
    - [x] 탭 닫기(X) 버튼 구현.
    - [ ] G-Code Syntax Highlighting (.xshd) 작성 및 적용. (Optional)
- [x] **Feature - File I/O**
    - [x] 파일 새로 만들기 기능.
    - [x] 파일 열기/저장 기능 (비동기 처리).
    - [x] 파일명 윈도우 제목 연동.
- [x] **Feature - Edit Menu**
    - [x] 실행 취소 / 다시 실행.
    - [x] 잘라내기 / 복사 / 붙여넣기.
- [x] **Feature - View Menu**
    - [x] 확대 / 축소 (Zoom) - **[Refined]** Shortcut Fixed.
- [x] **Feature - Status Bar**
    - [x] 커서 위치 실시간 표시 (Ln, Col).

## Phase 2: Editor Enhancements (에디터 고도화) ✅ IN PROGRESS
기본 에디터 기능을 넘어선 편의 기능과 코드 탐색 기능을 구현합니다.
- [x] **Find & Replace Widget**
    - [x] 찾기/바꾸기 로직 구현 (AvalonEdit API).
    - [x] VS Code 스타일 UI (상단 우측 오버레이).
    - [x] **UI Refinement:** 버튼 텍스트 변경, 레이아웃 정렬, 스타일 통일.
- [x] **Minimap**
    - [x] 코드 축소 렌더링.
    - [x] 스크롤 동기화 (Editor <-> Minimap).
    - [x] **Advanced Sync:** Visual Line Mapping & Dispatcher (Keyboard/Folding).
- [x] **Custom Dialogs**
    - [x] Save Confirmation Dialog (Dark Theme).
    - [x] Custom Button Styles (Primary, Secondary, Ghost).
- [x] **Architecture Refactoring (3-Tier)**
    - [x] Core/Modules/App 계층 분리.
    - [x] Dependency Injection (DI) 도입.
    - [x] IFileService/IDialogService 인터페이스 추출.
- [x] **Visuals & Settings**
    - [x] G-Code Syntax Highlighting (.xshd).
    - [x] Font Settings (Family, Size, Weight).
    - [x] Settings Persistence (JSON).
- [x] **Session Persistence & UI/UX**
    - [x] **Session Restore:** Open files & Last Folder.
    - [x] **Tabs:** Rounded UI, Close Button, New Tab Button.
    - [x] **Tab Navigation:** Ctrl + PageUp/Down shortcuts.
    - [x] Explorer Context Menu Dark Theme.
    - [x] **Explorer Selection:** Dark Mode Highlight (#094771).
    - [x] **Scrollbar:** MinHeight enforcement for long files.
- [ ] **Advanced Features**
    - [ ] 현재 라인 강조.
- [x] **G-Code Structure & Compilation (Session 7)**
    - [x] **Structure Panel:** G-Code Block Visualization (Side Panel).
    - [x] **Parser:** T# and M6 based block splitting.
    - [x] **Navigation:** Click-to-scroll synchronization.
    - [x] **Stability:** Fixed MSB4276/CS0103 build errors & Class Structure.


## Phase 2: Visualization (시각화 연동) - DEFERRED
텍스트와 3D 뷰어 간의 연결 고리를 만드는 단계입니다.
- [ ] **Module - 3D Renderer**
    - [ ] HelixToolkit Viewport3D 배치.
    - [ ] 기본 카메라 조작 (Zoom, Pan, Rotate) 구현.
- [ ] **Parsing & Conversion**
    - [ ] 파싱된 G-Code 데이터를 3D 좌표로 변환.
    - [ ] 화면에 단순 Line 경로 렌더링.
- [ ] **Synchronization**
    - [ ] 에디터 커서 <-> 3D 경로 하이라이트 동기화.

## Phase 3: Advanced Features (고도화) - DEFERRED
- [ ] 공구(Tool) 모델 렌더링 및 경로 따라가기 애니메이션.
- [ ] 가공 범위(Min/Max Bounds) 계산 및 표시.
- [ ] 사용자 설정 저장 (테마, 폰트).

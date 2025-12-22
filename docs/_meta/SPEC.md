# System Specification

## 1. System Architecture (3-Tier)
본 프로젝트는 **느슨한 결합(Loose Coupling)**과 **높은 응집도(High Cohesion)**를 위해 3계층 구조로 설계되었습니다.

### Layer 1: Core (`src/01.Core/GCode.Core`)
*   **Role:** 순수 비즈니스 로직 및 데이터 모델. UI 의존성 없음. **[Implemented]**
*   **Components:**
    *   `IFileService`: 파일 입출력 추상화 인터페이스.
    *   `IDialogService`: 다이얼로그 서비스 추상화 인터페이스.
    *   `GCodeParser`: (Planned) 정규식(Regex) 기반의 고속 G-Code 파서.
    *   `GCodeCommand`: (Planned) G0, G1, M 코드 등을 추상화한 커맨드 객체 모델.

### Layer 2: Modules (`src/02.Modules/GCode.Modules.*`)
*   **Role:** 재사용 가능한 기능 블록.
*   **Module A: FileIO (`src/02.Modules/GCode.Modules.FileIO`)** **[Implemented]**
    *   `FileService`: `System.IO` 기반 파일 처리 구현.
    *   `DialogService`: `Microsoft.Win32` 및 `System.Windows` 기반 다이얼로그 구현.
*   **Module B: Editor (Planned)**
    *   AvalonEdit 래퍼 및 ViewModels.
*   **Module C: Renderer (Planned)**
    *   HelixToolkit 3D 뷰어.

### Layer 3: Application (`src/03.App/GCode.App.WPF`)
*   **Role:** 애플리케이션 진입점 및 모듈 조립 (Composition Root). **[Implemented]**
*   **Components:**
    *   `App.xaml`: **Dependency Injection (DI)** 컨테이너 설정 (`IServiceProvider`).
    *   `MainWindow`: 기본 도킹 레이아웃 및 `EditorCommandHandler`.
    *   `EditorCommandHandler`: UI 이벤트와 Core 서비스를 연결하는 중재자.

## 2. Data Flow
1.  **Load:** 사용자가 파일을 열면 `App` -> `EditorViewModel` -> `IFileService`를 통해 텍스트 로드.
2.  **Parse:** 텍스트 변경 시 `EditorViewModel` -> `GCodeParser` 비동기 호출 -> `ParsedCommands` 생성.
3.  **Render:** `ParsedCommands`가 `EventAggregator`를 통해 `RendererViewModel`로 전달.
4.  **Visualize:** `RendererViewModel`이 `IToolpathGenerator`를 사용해 3D 라인 생성 -> 화면 갱신.

## 3. Performance Strategy
*   **Virtualization:** AvalonEdit의 UI 가상화를 활용하여 렌더링 부하 최소화.
*   **Async/Await:** 파일 로딩 및 파싱 작업은 UI 스레드를 차단하지 않도록 백그라운드 처리.
*   **Batch Rendering:** Helix Toolkit의 `LineGeometryModel3D`를 사용하여 수만 개의 선분을 한 번의 Draw Call로 처리.

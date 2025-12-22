# GravisNC G-Code Editor

## 1. Project Definition
**GravisNC**는 고성능 산업용 G-Code 에디터 및 시뮬레이션 소프트웨어입니다.
CIMCO Edit와 유사한 기능을 목표로 하며, 대용량 파일 처리 능력, 강력한 구문 강조(Syntax Highlighting), 그리고 정밀한 3D 백플롯(Backplot) 기능을 제공합니다.

### Core Philosophy
*   **Performance First:** 수십만 라인의 G-Code 파일도 즉시 로딩하고 끊김 없이 스크롤링합니다.
*   **Modern UX:** Windows Presentation Foundation (WPF) 기반의 현대적이고 유려한 다크 테마 UI를 제공합니다.
*   **Extensibility:** 3-Tier 아키텍처(Core-Modules-App)를 통해 향후 DNC 및 고급 시뮬레이션 확장이 용이합니다.

## 2. Technology Stack
*   **Framework:** .NET 8 (LTS)
*   **UI System:** WPF (Windows Presentation Foundation)
*   **Architecture:** MVVM (CommunityToolkit.Mvvm)
*   **Editor Engine:** AvalonEdit (High-performance text rendering)
*   **3D Engine:** Helix Toolkit (Hardware accelerated 3D visualization)

## 3. Key Features (Planned)
1.  **Smart Editor:** G-Code 전용 구문 강조, 스마트 자동 완성, 코드 폴딩.
2.  **3D Backplot:** 공구 경로의 실시간 3D 시각화, 회전/줌/팬 조작.
3.  **Analysis:** 경로 범위, 가공 시간 예측, 공구 교체 감지.
4.  **Comparison:** 두 NC 파일 간의 차이점 비교 (Diff).

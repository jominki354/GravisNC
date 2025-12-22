# Quality Assurance Criteria

## 1. Functional Testing
*   **File Load:** 10MB 크기의 G-Code 파일 로딩 시 1초 이내 화면 표시 (UI Freezing 불가).
*   **Parsing:** 지원하지 않는 G-Code 입력 시 크래시 없이 'Unknown Command' 처리.
*   **Rendering:** 3D 뷰어 조작(Zoom/Pan) 시 60FPS 유지 목표.

## 2. Unit Testing Strategy
*   **Scope:** `Gravis.Core` 프로젝트 내의 모든 파서(Parser) 및 계산 로직.
*   **Framework:** xUnit or NUnit.
*   **Coverage Logic:**
    *   `G00`, `G01`, `G02`, `G03` 좌표 변환 정확성 검증.
    *   잘못된 텍스트 형식에 대한 예외 처리 검증.

## 3. UI Testing (Manual)
*   에디터와 3D 뷰어 간의 Sync 기능 (클릭 시 하이라이트) 정확성 확인.
*   다크 모드 가독성 확인 (Contrast Ratio).

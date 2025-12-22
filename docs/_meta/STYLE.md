# Code & Development Style Guide

## 1. 아키텍처 원칙
*   **MVVM 패턴:** ViewModel을 통한 데이터 바인딩 권장.
*   **3-Tier 구조:** Core (로직) / Modules (기능) / App (UI).

## 2. UI/UX 개발 원칙

### ⚠️ 핵심 원칙: **기존 구조 보존**
*   **전체 파일 덮어쓰기 금지:** 새 기능 추가 시 `write_to_file(Overwrite=true)`로 전체 파일을 덮어쓰지 않는다.
*   **`replace_file_content` 사용:** 기존 코드를 유지하고 필요한 부분만 수정한다.
*   **메뉴 구조 보존:** 한번 확정된 메뉴 구조는 건드리지 않고, 추가만 한다.
*   **점진적 개선:** 한 번에 대규모 UI 변경을 하지 않고, 작은 단위로 수정/테스트/확인한다.

### 디자인 토큰 (확정)
| Token | Value | Usage |
|-------|-------|-------|
| `BgDark` | `#1E1E1E` | 윈도우 배경 |
| `BgMedium` | `#252526` | 에디터 배경 |
| `BgLight` | `#2D2D30` | 탭바, 타이틀바 |
| `Border` | `#3E3E42` | 테두리, 구분선 |
| `Hover` | `#3E3E42` | 호버 하이라이트 |
| `Accent` | `#007ACC` | 강조 색상 (상태바) |
| `TextPrimary` | `#CCCCCC` | 주 텍스트 |
| `TextSecondary` | `#888888` | 보조 텍스트 |

### 레이아웃 (확정)
*   **타이틀바 높이:** 44px
*   **탭바 높이:** 36px
*   **상태바 높이:** 28px
*   **CornerRadius:** 8~10px (외곽), 4~6px (내부 요소)

## 3. 코딩 스타일
*   **C# 버전:** 최신 (nullable, pattern matching 사용)
*   **비동기:** 파일 I/O는 항상 `async/await` 사용.
*   **네이밍:** PascalCase (public), camelCase (private), `_` prefix (private field).

## 4. Git 커밋 메시지
*   `[Feat]` 새 기능
*   `[Fix]` 버그 수정
*   `[Style]` UI/UX 변경
*   `[Refactor]` 코드 정리
*   `[Docs]` 문서 변경

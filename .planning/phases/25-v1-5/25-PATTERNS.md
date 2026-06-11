# Phase 25: 마일스톤 v1.5 통합 검증 및 문서화 - Patterns

본 문서는 마일스톤 v1.5 (UI 테스트 자동화 및 SSH 호환성 개선)의 최종 통합 검증 및 문서화 단계를 위해 작성 및 수정될 핵심 문서들의 구성 방식을 명시합니다. 기존 프로젝트에 적용된 아날로그(Analog) 문서들과 소스 코드에 구축된 설계 패턴을 기반으로 매핑 관계를 기술합니다.

---

## 1. 대상 파일 매핑 개요

Phase 25 실행 단계에서 생성되거나 수정될 파일들과 그에 대응하는 기존 프로젝트 내 아날로그 파일의 목록은 다음과 같습니다.

| 대상 파일 (Target File) | 변경 유형 (Action) | 위치 (Path) | 기준 아날로그 파일 (Analog File) | 주요 역할 및 목적 |
| :--- | :--- | :--- | :--- | :--- |
| **TESTING.md** | 신설 | `(루트)/TESTING.md` | 없음 (신규 생성) | 로컬 개발자 검증을 위한 테스트 인프라 환경 셋업, 실행 방법, 트러블슈팅 가이드 기술 |
| **README.md** | 수정 | `(루트)/README.md` | `(루트)/README.md` | 프로젝트 메인 설명서에 테스트 실행 링크 연동 및 개요 명시 |
| **RETROSPECTIVE.md** | 수정 (누적) | `.planning/RETROSPECTIVE.md` | `.planning/RETROSPECTIVE.md` (기존 v1.4/v1.3 회고) | 마일스톤 v1.5 수행에 따른 성과, 한계점, 교훈 및 아키텍처적 분석 회고 기록 |
| **v1.5-MILESTONE-AUDIT.md** | 신설 | `.planning/v1.5-MILESTONE-AUDIT.md` | `.planning/milestones/v1.4-MILESTONE-AUDIT.md` | 요구사항 3-소스 교차 검증 및 전체 테스트 성공 수치, 암호 호환성 등의 감사 보고서 작성 |

---

## 2. 파일별 상세 아날로그 분석 및 코드 패턴

### 2.1 TESTING.md (신설)
로컬 개발 환경에서 bUnit 단위 테스트와 Playwright E2E 테스트를 안정적으로 기동하고, 로컬 디버깅 및 트러블슈팅을 돕기 위해 신설되는 문서입니다.

*   **참조 아날로그 & 기술적 근거**:
    *   **테스트 인프라**: [PlaywrightHostHelper.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Tests/PlaywrightHostHelper.cs), [BunitTestBase.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Tests/BunitTestBase.cs)
    *   **SSH CLI 파이핑 테스트**: `SshServerAuthTests.cs`, `SshCommandPipingTests.cs` 등 OS CLI `ssh` 프로세스를 테스트 내에서 기동하는 패턴.
*   **적용할 마크다운 및 기술 패턴**:
    1.  **테스트 전제 조건 및 설치 가이드**:
        *   `.NET 10.0 SDK` 요구사항 및 OS 내 OpenSSH CLI 클라이언트 활성화 여부 확인 명령 제시.
        *   Playwright 브라우저 드라이버 설치를 위해 `playwright install` (또는 `pwsh bin/Debug/net8.0/playwright.ps1 install`) 가이드 제공.
    2.  **테스트 격리 아키텍처 설명**:
        *   **SQLite DB 격리**: `e2e_test_{Guid:N}.db` 동적 파일 생성 및 Kestrel 서버 기동(`http://localhost:5002`) 후 `Dispose` 시점에서의 클린업 생명주기 기술.
        *   **컴포넌트 테스트 격리**: bUnit 실행 시 매번 독립된 인메모리 DB 컨텍스트를 주입하는 메커니즘 설명.
    3.  **로컬 디버깅 팁**:
        *   Playwright 실행 시 브라우저 동작을 관찰하기 위해 `BrowserTypeLaunchOptions`의 `Headless = false` 모드를 적용하는 환경 변수(또는 코드 수정 요령) 명시.
    4.  **자가 진단(Troubleshooting) 섹션**:
        *   **포트 충돌**: Port 5002 점유 시 대처 방안.
        *   **Playwright 미설치**: 드라이버 누락 에러 발생 시 대처 방안.
        *   **SSH 테스트 실패**: 로컬 머신에 `ssh` CLI 도구가 없거나 경로가 다를 때의 설정 요령.

### 2.2 README.md (수정)
프로젝트의 메인 진입점 문서로, 이번 마일스톤 v1.5에서 추가된 통합 검증 문서(`TESTING.md`)로 개발자들이 쉽게 유입될 수 있도록 수정합니다.

*   **참조 아날로그**:
    *   기존 `README.md` 내 `⚙️ 실행 방법` 및 `📂 주요 코드 구조` 섹션.
*   **적용할 마크다운 패턴**:
    *   `## ⚙️ 실행 방법` 하단에 `### 5. 테스트 실행 방법` 소섹션을 신설.
    *   독립된 `TESTING.md` 문서로 연결되는 마크다운 하이퍼링크 `[TESTING.md](./TESTING.md)`를 명시.
    *   bUnit 컴포넌트 테스트 및 Playwright E2E 브라우저 테스트의 존재와 실행 명령어(`dotnet test`)를 가볍게 요약하여 노출.

### 2.3 RETROSPECTIVE.md (수정)
마일스톤 v1.5의 최종 성과와 아키텍처/보안 관점에서의 아키텍처 영향 요소를 기록하기 위해 기존 회고록 파일의 상단에 새 마일스톤 회고를 누적합니다.

*   **참조 아날로그**:
    *   `.planning/RETROSPECTIVE.md`에 기재되어 있는 `Milestone: v1.4 — 웹훅, LFS, 조직 및 보안 기능 강화` 회고록.
*   **적용할 마크다운 및 작성 패턴**:
    *   파일 최상단에 `## Milestone: v1.5 — UI 테스트 자동화 및 SSH 호환성 개선` 헤더 추가.
    *   **동일한 6대 핵심 질문 구조 유지**:
        1.  **What Was Built**: 구현 완료된 주요 피처 요약 (bUnit 컴포넌트 테스트 스위트, Playwright E2E 프레임워크, Microsoft.DevTunnels.Ssh 기반 SSH 현대화, SshAuthLog 데이터베이스 감사 로깅).
        2.  **What Worked**: 성공적인 통합 및 테스트 패스 경험 (Renci.SshNet 비호환 문제를 실제 OS `ssh` 호출 테스트로 우회 성공한 적응적 설계 등).
        3.  **What Was Inefficient**: 비효율적이었거나 추가 분석이 필요한 부문 기술 (Playwright E2E 실행 시 Kestrel 웹 호스트 기동 지연 및 포트 격리 관리 최적화 필요성 등).
        4.  **Patterns Established**: 이번에 정립된 새로운 기술적 패턴 (SQLite 테스트 환경 격리 라이프사이클 관리 패턴 등).
        5.  **Key Lessons**: 중요한 배운 점 (타사 라이브러리 간 비호환 문제를 OS 기본 프로세스 CLI 바인딩으로 직접 해결할 수 있는 우회 전략의 타당성 등).
        6.  **Cost Observations**: 개발/검증 비용 관점의 관측 데이터.

### 2.4 v1.5-MILESTONE-AUDIT.md (신설)
마일스톤 v1.5의 전체 요구사항 달성 여부를 3-소스 교차 검증하고, Nyquist 컴플라이언스 준수 및 보안성/성능 영향 분석 결과를 담은 독립 감사 보고서입니다.

*   **참조 아날로그**:
    *   `.planning/milestones/v1.4-MILESTONE-AUDIT.md` (가장 구체적이고 완성도 높은 감사 서식 적용).
*   **적용할 마크다운 및 작성 패턴**:
    1.  **YAML Frontmatter**: 마일스톤 메타데이터 정의 적용.
        ```yaml
        ---
        milestone: v1.5
        audited: 2026-06-11T15:40:00Z
        status: passed
        scores:
          requirements: 6/6
          phases: 4/4
          integration: 3/3
          flows: 3/3
        gaps:
          requirements: []
          integration: []
          flows: []
        tech_debt: []
        nyquist:
          compliant_phases: [22, 23, 24, 25]
          partial_phases: []
          missing_phases: []
          noncompliant_phases: []
          overall: compliant
        ---
        ```
    2.  **요구사항 교차 검증 테이블**:
        *   `REQ-ID`, `설명`, `SUMMARY 프론트매터`, `VALIDATION.md`, `코드 증거`, `최종 상태` 칼럼 구성.
        *   bUnit 테스트, Playwright E2E 브라우저 테스트, DevTunnels.Ssh 암호 호환성, SshAuthLog 감사 로깅 등 각 요건 매핑.
    3.  **크로스-페이즈 통합 검증 및 E2E 플로우**:
        *   bUnit UI 격리 ↔ Playwright 브라우저 E2E ↔ DevTunnels.Ssh 서버 호스팅 ↔ SshAuthLog DB 감사의 연동 구조를 설명하고 다이어그램(텍스트) 추가.
    4.  **아키텍처 및 보안 영향 상세 분석**:
        *   **보안적 이점**: `Microsoft.DevTunnels.Ssh` 도입으로 최신 암호 규격(`ed25519`, `rsa-sha2-256` 등) 지원 확보 및 감사 로그 테이블 구축에 따른 보안 가시성 향상.
        *   **성능적 영향**: DB 감사 로깅(`SshAuthLog`) 시 동기적 디스크 I/O가 SSH 인증 완료 반응 속도에 미치는 잠재적 영향력 및 백엔드 스레드 오버헤드 분석 평가.
        *   **정량적 수치 표기**: 전체 104개 테스트 스위트의 성공 지표와 SSH 알고리즘 호환성 수준을 표(Table) 등으로 시각화.

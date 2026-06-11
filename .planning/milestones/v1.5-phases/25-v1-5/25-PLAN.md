---
wave: 1
depends_on: []
files_modified:
  - README.md
  - TESTING.md
  - .planning/RETROSPECTIVE.md
  - .planning/v1.5-MILESTONE-AUDIT.md
autonomous: true
---

# Phase 25 Plan — 마일스톤 v1.5 통합 검증 및 문서화

## Goals
마일스톤 v1.5(UI 테스트 자동화 및 SSH 호환성 개선)의 최종 완성도를 검증하기 위해 bUnit 단위 테스트, Playwright E2E 브라우저 테스트, 그리고 SSH 라이브러리 교체(DevTunnels.Ssh) 및 DB 감사 로깅의 통합 연동 상태를 총체적으로 점검하고, 이와 관련된 가이드 문서(`TESTING.md`, `README.md`)와 마일스톤 감사(`v1.5-MILESTONE-AUDIT.md`), 회고록(`RETROSPECTIVE.md`)을 작성합니다.

## Must Haves
- [ ] [TESTING.md](file:///E:/Workspace/VisualC%23/Aristokeides/TESTING.md) 신설 (D-25-01, D-25-02, D-25-05, D-25-06): 테스트 전제 조건, 설치 가이드, 격리 아키텍처 설명(SQLite, 5002 포트), 디버깅 팁, 트러블슈팅 세부 사항 포함.
- [ ] [README.md](file:///E:/Workspace/VisualC%23/Aristokeides/README.md) 수정 (D-25-01): 테스트 실행 방법 안내를 위한 `TESTING.md` 링크 연결.
- [ ] [.planning/RETROSPECTIVE.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/RETROSPECTIVE.md) 수정 (D-25-03, D-25-04): v1.5에 대한 6대 핵심 질문 기반 누적 회고 추가.
- [ ] [.planning/v1.5-MILESTONE-AUDIT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/v1.5-MILESTONE-AUDIT.md) 신설 (D-25-03, D-25-04): YAML frontmatter, 요구사항 3-소스 교차 검증 테이블, 크로스-페이즈 연동 및 E2E 플로우 가이드, 아키텍처 및 보안/성능 영향 분석, 104개 테스트의 정량적 성공 수치 및 SSH 호환성 분석 포함.
- [ ] 전체 테스트 스위트 최종 실행: `dotnet test` 실행 시 전체 테스트가 성공적으로 패스됨을 확인.


## Tasks

### Wave 1

#### Task 25-01-01: TESTING.md 문서 신설 및 README.md 연동
- **Requirement**: v1.5 통합 검증 및 문서화
- **Verification**: manual

<read_first>
- E:/Workspace/VisualC#/Aristokeides/README.md
- E:/Workspace/VisualC#/Aristokeides/Aristokeides.Tests/PlaywrightHostHelper.cs
- E:/Workspace/VisualC#/Aristokeides/Aristokeides.Tests/BunitTestBase.cs
- E:/Workspace/VisualC#/Aristokeides/Aristokeides.Tests/SshCommandPipingTests.cs
</read_first>

<acceptance_criteria>
- `E:/Workspace/VisualC#/Aristokeides/TESTING.md` 파일이 신규 생성되어 존재함.
- TESTING.md 파일에 `dotnet build`, `playwright install`, `dotnet test` 실행 명령어가 마크다운 코드 블록으로 정의되어 있음.
- TESTING.md 파일에 `e2e_test_{Guid:N}.db` SQLite 격리 데이터베이스 생명주기와 Kestrel `5002` 포트 바인딩 규칙에 대한 설명이 포함되어 있음.
- TESTING.md 파일에 헤디드 모드(`Headless = false`) 디버깅 방법 및 환경 변수 활용 팁이 포함되어 있음.
- TESTING.md 파일에 포트 5002 충돌, Playwright 드라이버 미설치 에러, OS 로컬 `ssh` CLI 부재 시의 트러블슈팅 솔루션이 기술되어 있음.
- `E:/Workspace/VisualC#/Aristokeides/README.md` 파일 내에 `TESTING.md`로 연결되는 마크다운 링크 `[TESTING.md](./TESTING.md)`가 삽입되어 있음.
</acceptance_criteria>

<action>
- `TESTING.md`를 프로젝트 루트에 생성하고 로컬 개발자 검증을 위한 설치 과정(playwright 브라우저 드라이버 설치 포함), E2E 테스트 시의 데이터베이스 격리 기전 및 포트 바인딩 설명, 디버깅을 위한 헤디드 모드 실행법, 주요 트러블슈팅(5002 포트 충돌, 드라이버 누락, SSH CLI 도구 누락) 해결 섹션을 상세히 작성함.
- `README.md`의 `## ⚙️ 실행 방법` 하단에 테스트 가이드 섹션을 추가하고 `TESTING.md`로 이동할 수 있는 상대 경로 링크 `[TESTING.md](./TESTING.md)`를 연동함.
</action>

#### Task 25-01-02: 마일스톤 회고록 누적 및 마일스톤 감사 보고서 작성
- **Requirement**: v1.5 통합 검증 및 문서화
- **Verification**: manual

<read_first>
- E:/Workspace/VisualC#/Aristokeides/.planning/RETROSPECTIVE.md
- E:/Workspace/VisualC#/Aristokeides/.planning/milestones/v1.4-MILESTONE-AUDIT.md
- E:/Workspace/VisualC#/Aristokeides/.planning/phases/25-v1-5/25-CONTEXT.md
- E:/Workspace/VisualC#/Aristokeides/.planning/phases/25-v1-5/25-RESEARCH.md
</read_first>

<acceptance_criteria>
- `E:/Workspace/VisualC#/Aristokeides/.planning/RETROSPECTIVE.md` 최상단에 `## Milestone: v1.5 — UI 테스트 자동화 및 SSH 호환성 개선` 회고 내용이 추가되어 있으며, 기존 v1.4 이하 회고 텍스트가 훼손되지 않고 보존됨.
- RETROSPECTIVE.md 내 v1.5 회고가 6대 핵심 질문 구조(What Was Built, What Worked, What Was Inefficient, Patterns Established, Key Lessons, Cost Observations)를 정확히 준수하여 기재됨.
- `E:/Workspace/VisualC#/Aristokeides/.planning/v1.5-MILESTONE-AUDIT.md` 감사 보고서가 신규 생성되어 존재함.
- `v1.5-MILESTONE-AUDIT.md`에 `milestone: v1.5`, `scores`, `nyquist` 정보가 포함된 YAML frontmatter가 정확히 삽입되어 있음.
- `v1.5-MILESTONE-AUDIT.md` 내 요구사항 교차 검증 테이블에 v1.5 요구사항들(bUnit 단위 테스트, Playwright E2E UI 테스트, SSH 서버 현대적 호환성 개선)이 `REQ-ID`, `설명`, `SUMMARY 프론트매터`, `VALIDATION.md`, `코드 증거`, `최종 상태` 형태로 명시됨.
- `v1.5-MILESTONE-AUDIT.md`에 전체 테스트 통과 성공 지표(104개 테스트 스위트)와 SSH 호스트 키 알고리즘(ed25519, rsa-sha2-256, rsa-sha2-512) 지원 스펙이 구체적인 표와 정량적 수치 형태로 작성됨.
- `v1.5-MILESTONE-AUDIT.md`에 데이터베이스 감사 로깅(`SshAuthLog`) 적용에 따른 디스크 I/O 성능 영향 및 보안적 이점 분석 아키텍처 요약이 포함됨.
</acceptance_criteria>

<action>
- `RETROSPECTIVE.md`에 v1.5 마일스톤 회고록을 누적 형식으로 작성함.
- `.planning/v1.5-MILESTONE-AUDIT.md` 감사 문서를 생성하고, YAML frontmatter 정의, v1.5 요구사항에 대한 3-소스 교차 검증 테이블 매핑, bUnit 컴포넌트 ↔ Playwright 브라우저 E2E ↔ DevTunnels.Ssh 서버 호스팅 ↔ SshAuthLog DB 감사로 이어지는 크로스-페이즈 통합 아키텍처 요약, 그리고 전체 104개 테스트의 성공 지표와 ed25519, rsa-sha2 등의 SSH 알고리즘 호환성 분석을 정량적 수치 및 표와 함께 작성함.
</action>

#### Task 25-01-03: 전체 테스트 스위트 최종 통합 실행 및 검증
- **Requirement**: v1.5 통합 검증 및 문서화
- **Verification**: automated

<read_first>
- E:/Workspace/VisualC#/Aristokeides/Aristokeides.Tests/BunitTestBase.cs
- E:/Workspace/VisualC#/Aristokeides/Aristokeides.Tests/PlaywrightHostHelper.cs
</read_first>

<acceptance_criteria>
- `dotnet test` 명령을 실행했을 때, 전체 테스트 스위트가 빌드 오류 없이 모두 패스하고 종료 코드(exit code)가 0으로 반환됨.
</acceptance_criteria>

<action>
- 프로젝트의 전체 테스트 스위트를 구동하기 위해 로컬 환경에서 `dotnet test` 명령어를 실행하여 bUnit 테스트 및 Playwright E2E 브라우저 테스트, 그리고 SSH auth/command 통합 테스트를 포함한 검증 수단이 정상 작동하는지 확인하고 에러 발생이 없는지 최종 검증함.
</action>


## Artifacts this phase produces
- **[TESTING.md](file:///E:/Workspace/VisualC%23/Aristokeides/TESTING.md)**: 로컬 개발자 검증을 위한 테스트 인프라 환경 셋업, 실행 방법, 트러블슈팅 가이드를 기재한 신규 문서.
- **[.planning/v1.5-MILESTONE-AUDIT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/v1.5-MILESTONE-AUDIT.md)**: 요구사항 3-소스 교차 검증 및 전체 테스트 성공 수치, 암호 호환성 등의 정량적 수치가 담긴 신규 마일스톤 감사 보고서.
- **[README.md](file:///E:/Workspace/VisualC%23/Aristokeides/README.md)**: `TESTING.md` 상대 링크가 연동 및 업데이트된 메인 설명 파일.
- **[.planning/RETROSPECTIVE.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/RETROSPECTIVE.md)**: v1.5의 피처 요약, 성공 요인, 비효율 원인 분석, 패턴 정립 등이 최상단에 누적 기록된 회고 파일.

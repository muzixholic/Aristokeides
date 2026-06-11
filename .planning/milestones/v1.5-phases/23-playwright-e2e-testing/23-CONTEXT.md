# Phase 23 Context: Playwright 기반 E2E UI 테스트 자동화 환경 구축

이 문서에는 Phase 23에서 구현할 Playwright 기반 E2E(End-to-End) 브라우저 자동화 테스트의 설계 결정 사항과 테스트 인프라 구성 요건을 기록합니다.

## 1. Phase Goals

* `Aristokeides.Tests` 프로젝트에 Playwright 및 Mvc.Testing 의존성을 추가하여 로컬 웹 서버 자동 구동형 E2E 테스트 베이스 완성.
* 4대 사용자 대화형 흐름(인증/세션, 저장소 관리, PR 및 코드리뷰, 이슈 및 칸반)을 크롬(Chromium) 브라우저 헤드리스(Headless) 환경에서 완전 자동으로 검증하는 시나리오 구축.

## 2. Core Decisions

### D-23-01: 백그라운드 웹 호스팅 (Kestrel Server Auto-run)
* **도구**: `Microsoft.AspNetCore.Mvc.Testing` 패키지의 `WebApplicationFactory<Program>`를 활용한다.
* **설계**: 일반적인 API 통합 테스트용 인메모리 클라이언트 대신, 실제 HTTP 포트를 리스닝하는 커스텀 `WebApplicationFactory` 서브클래스(예: `PlaywrightWebApplicationFactory<Program>`)를 구현하여 특정 로컬 포트(예: 임의 포트 또는 고정 포트 `5002` 등)에서 Kestrel 웹 서버가 돌도록 백그라운드로 띄운다. Playwright의 브라우저 인스턴스가 이 로컬 포트로 접속하여 테스트를 수행한다.

### D-23-02: 테스트용 격리 데이터베이스 운영
* **스토리지**: E2E 테스트가 진행되는 동안 독립된 전용 데이터베이스 파일(`e2e_test.db`)을 SQLite로 자동 프로비저닝한다.
* **초기화 정책**: 테스트 클래스 셋업 단계에서 기존 `e2e_test.db` 파일을 깨끗이 지우고, `AppDbContext` 마이그레이션(`MigrateAsync()`)을 기동하여 테이블 스키마를 최신 상태로 새로 구축한다.
* **시드 데이터**: 회원가입이 안 된 기본 상태에서 테스트하거나, 시나리오 진행을 위해 필요한 어드민 계정 등을 프로그래밍 방식으로 DB에 밀어 넣어 테스트 준비를 마친다.

### D-23-03: Playwright 브라우저 종속성 및 드라이버 셋업
* **종속성**: NuGet 패키지 `Microsoft.Playwright`를 테스트 프로젝트에 연동한다.
* **빌드 후 단계**: 빌드가 완료된 후 Playwright 브라우저 바이너리(Chromium)가 준비되도록 빌드 타겟 설정이나 실행 전 스크립트(`dotnet build` 후 `playwright install` 명령어 실행 권장)를 UAT 가이드에 포함한다.

## 3. Gray Areas Resolved

| Gray Area | Selected Option | Rationale |
| :--- | :--- | :--- |
| 웹 서버 구동 방식 | **WebApplicationFactory를 통한 백그라운드 자동 기동** | 사용자가 수동으로 웹 서비스를 띄워야 하는 오버헤드를 없애고 완전한 원클릭 로컬 테스트 및 CI/CD 자동화를 도모하기 위함 |
| 데이터베이스 전략 | **임시 SQLite 파일 DB 격리 기동** | 테스트 도중 데이터 오염을 예방하고, 병렬 및 다중 테스트 구동 시 실제 로컬 DB에 영향을 주지 않기 위함 |

## 4. Next Steps

* [ ] 슬래시 커맨드 `/gsd-plan-phase 23`를 호출하여 구체적인 플레이라이트 설치 명령어, 포트 바인딩 팩토리 클래스, 각 E2E 시나리오별 구현 계획을 마련합니다.

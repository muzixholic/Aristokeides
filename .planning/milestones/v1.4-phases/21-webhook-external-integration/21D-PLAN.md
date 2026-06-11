---
title: "웹훅 설정 및 전송 이력 조회 웹 UI 구현"
phase: 21
wave: 4
depends_on: ["21C"]
files_modified:
  - Aristokeides.Api/Components/Pages/RepositoryWebhooks.razor
  - Aristokeides.Api/Components/Pages/RepositorySettings.razor
autonomous: true
requirements:
  - "RepositoryWebhooks.razor Blazor 컴포넌트를 설계하여 웹훅 CRUD 관리 및 전송 이력(Delivery Log) 타임라인, 상세 로그 모달창을 구현한다."
  - "이력 상세 로그 모달창에서 수동 재전송(Redeliver) 버튼 클릭 시 WebhookService를 연동하여 작업을 비동기 재발송 큐잉하는 기능을 통합한다."
  - "RepositorySettings.razor 페이지에 웹훅 설정 탭 링크를 추가하고, 웹훅 편집 권한(Admin)을 검증하여 UI 접근 제어를 적용한다."
---

# Plan 21D: 웹훅 설정 및 전송 이력 조회 웹 UI 구현

## Objective

저장소 소유자 및 관리자(Admin)가 브라우저 환경에서 직접 웹훅을 추가, 편집, 삭제할 수 있는 Blazor 관리 페이지(`RepositoryWebhooks.razor`)를 구축합니다. 또한, 전송된 결과를 한눈에 보는 타임라인식 전송 로그 목록과 각 전송의 상세 HTTP 요청/응답 페이로드를 보여주는 모달 다이얼로그를 연동하며, 실패 이력을 편리하게 재발송할 수 있는 수동 재전송 버튼 기능을 통합 완성합니다.

## Tasks

<task id="21D-1">
<title>RepositoryWebhooks.razor 웹훅 설정 및 이력 관리 화면 구현</title>
<read_first>
- `Aristokeides.Api/Components/Pages/RepositorySettings.razor` — 기존 리포지토리 설정 UI 구성 및 스타일 참고
</read_first>
<action>
1. `Aristokeides.Api/Components/Pages/RepositoryWebhooks.razor` 신규 작성:
   - **라우팅 주소:** `@page "/{username}/{repoName}/settings/webhooks"`
   - **권한 검사:** 현재 사용자가 해당 저장소의 `Admin` 권한을 소유하고 있는지 서버 측 검증 수행. 미보유 시 "권한이 없습니다." 에러 표시 및 접근 차단.
   - **웹훅 CRUD 폼:**
     - 웹훅 목록 테이블: 등록된 URL, Content-Type, Type(Generic/Slack/Discord), 활성 상태(Badge) 출력.
     - 추가/수정 카드 폼: Payload URL 입력, Webhook Type 드롭다운 선택, Secret 입력(비밀값 입력 폼 제공), Trigger Events 체크박스 제공(Push, Issue, PR 각각 개별 선택 가능), IsActive 토글 지원.
   - **전송 이력 타임라인 리스트:**
     - 특정 웹훅의 상세 보기 아이콘을 클릭하면, 하단에 최근 50개의 `WebhookDelivery` 이력 리스트 노출.
     - 성공(그린 체크) 및 실패(레드 경고) 상태를 직관적인 디자인 토큰을 활용해 배치.
   - **상세 로그 팝업 모달 구현:**
     - 이력 행 클릭 시 상세 모달 창 활성화.
     - 요청 상세(Request Headers, Request Body) 및 응답 상세(Response Headers, Response Body, Http Status Code, Duration Ms)를 스크롤 가능한 텍스트 영역에 출력.
     - **수동 재전송 단추:** 모달 창 우측 하단에 "재전송" 버튼 배치. 클릭 시 비동기로 `WebhookService.RedeliverAsync`를 호출하고 성공 시 전송 목록을 리로드 및 동적 갱신 처리.
</action>
<acceptance_criteria>
- `/settings/webhooks` 페이지가 리포지토리 설정 화면 스타일과 정교하게 통일되어 정상 렌더링된다.
- CRUD 변경 시 즉시 목록에 리프레시 반영된다.
- 실패한 전송 로그에 대해 "재전송" 버튼 클릭 시 성공적으로 백그라운드 발송이 새로 수행되고 이력 목록에 새 로그가 안전하게 탑재된다.
</acceptance_criteria>
</task>

<task id="21D-2">
<title>RepositorySettings.razor 설정 탭 웹훅 링크 추가</title>
<read_first>
- `Aristokeides.Api/Components/Pages/RepositorySettings.razor` — 설정 사이드바 탭 및 권한 매핑 구역 참조
</read_first>
<action>
1. `RepositorySettings.razor` 설정 페이지의 사이드바/상단 탭 메뉴 목록에 "웹훅 설정 (Webhooks)" 메뉴 링크를 추가한다.
   - 링크 클릭 시 `/{username}/{repoName}/settings/webhooks` 경로로 리다이렉션 또는 페이지 전환되도록 처리한다.
</action>
<acceptance_criteria>
- 저장소 설정 페이지 진입 시 웹훅 탭 메뉴가 정상 노출되고 클릭 시 웹훅 설정 관리 페이지로 정상 전환된다.
</acceptance_criteria>
</task>

## must_haves

- 웹훅 설정 페이지는 반드시 해당 저장소의 소유자(Owner) 또는 관리자(`Admin`) 권한을 가진 사용자에게만 진입 및 조작 권한이 허용되어야 한다.
- 상세 전송 로그 팝업 모달창에서 요청/응답 바디는 가독성을 위해 JSON 줄 바꿈 포맷팅을 지원하거나 가독성 있게 렌더링되어야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Components/Pages/RepositoryWebhooks.razor` | 신규 | 리포지토리 웹훅 CRUD 관리 및 전송 이력 타임라인/상세 조회 Blazor 컴포넌트 |
| `Aristokeides.Api/Components/Pages/RepositorySettings.razor` | 수정 | 설정 메뉴 내 웹훅 관리 페이지 이동 탭 링크 추가 |

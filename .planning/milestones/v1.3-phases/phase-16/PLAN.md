# Phase 16 Plan: 관리자 설정 화면 추가

## 1. 🎯 Objective
- 관리자 권한을 가진 사용자(`Role == "Admin"`)만이 접근할 수 있는 전역 시스템 설정 화면을 제공합니다.
- 현재 구성된 데이터베이스 정보 및 SSH, 서버 도메인 등 기타 주요 애플리케이션 설정을 웹 UI를 통해 확인하고 변경할 수 있도록 합니다.

## 2. 📝 Tasks

### Task 1: 설정 관리용 서비스 및 모델 구현
- `Aristokeides.Api/Models/AdminSettingsViewModel.cs` 모델을 생성합니다.
  - 데이터베이스 관련 항목: Provider, ConnectionString 등 (읽기 전용 또는 제한적 수정 지원)
  - 기타 시스템 설정: Ssh.Port, Ssh.Domain, Jwt.Issuer 등 `appsettings.json` 내 주요 항목들.
- `Aristokeides.Api/Services/AdminSettingsService.cs`를 구현하여 `appsettings.json`을 읽어 모델로 변환하고, 수정된 설정을 다시 파일에 기록(Write)하는 기능을 담당하도록 합니다.

### Task 2: `/admin/settings` Blazor UI 컴포넌트 생성
- `Aristokeides.Api/Components/Pages/AdminSettings.razor` 컴포넌트를 생성합니다.
- `@attribute [Authorize(Roles = "Admin")]`를 추가하여 관리자만 접근 가능하도록 보호합니다.
- 페이지 진입 시 `AdminSettingsService`를 통해 현재 설정값들을 불러와 폼에 바인딩합니다.
- 데이터베이스 변경 섹션 구현: DB 설정 변경 시 마이그레이션이 수반되어야 하거나 위험할 수 있으므로, 변경 시 주의사항(경고창)을 띄우거나 "설정 저장 후 재시작 시 적용됨" 등의 안내를 추가합니다.

### Task 3: 글로벌 네비게이션 메뉴에 어드민 링크 추가
- `Aristokeides.Api/Components/Layout/MainLayout.razor` (또는 네비게이션 바 컴포넌트)를 수정합니다.
- `<AuthorizeView Roles="Admin">` 컴포넌트를 사용하여, 현재 로그인한 사용자가 관리자인 경우에만 "System Settings" 또는 "Admin" 링크가 보이도록 추가합니다.

## 3. 🔍 Verification
- [ ] 관리자가 아닌 계정으로 `/admin/settings` 접속 시도시 권한 없음(Access Denied)으로 처리되는지 확인한다.
- [ ] 관리자 계정으로 로그인 시 상단 네비게이션에 Admin/System Settings 링크가 노출되는지 확인한다.
- [ ] 설정 페이지에서 항목을 수정하고 저장하면 `appsettings.json`이 올바르게 갱신되는지 확인한다.
- [ ] 데이터베이스 Provider를 변경하고 저장 후 재구동 시, 새로운 DB 연결이 적용되는지 확인한다.

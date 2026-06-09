# Requirements: v1.3 배포를 위한 작업

## 1. 다중 데이터베이스 지원 (Multi-DB Support)
- **기본 DB:** SQLite (기존과 동일하게 초기 구성 없이 가볍게 시작 가능하도록 지원)
- **추가 지원 DB:** PostgreSQL, MariaDB, MySQL
- Entity Framework Core의 Provider를 런타임 혹은 초기 설정 시 동적으로 선택할 수 있는 구조 적용.

## 2. 최초 설치 관리자 (Setup Wizard)
- 애플리케이션 최초 실행 시 (DB 구성이 완료되지 않았거나, Admin 계정이 없는 경우) 모든 일반 라우팅을 차단하고 설치 화면(`/setup`)으로 리다이렉트.
- **DB 설정:** SQLite, PostgreSQL, MariaDB, MySQL 중 사용할 데이터베이스를 선택하고, 연결 문자열(Connection String) 정보 입력 기능.
- **관리자 생성:** 최초의 관리자(Admin) 계정(ID, 이메일, 비밀번호 등) 설정 기능.
- 설정 완료 후 해당 DB에 마이그레이션 적용, 기본 데이터(씨드) 적재 후 정상 작동 모드로 재시작 혹은 전환.

## 3. 시스템/데이터베이스 설정 화면 (Settings)
- 로그인한 관리자(Admin) 권한이 접근할 수 있는 애플리케이션 설정 페이지 추가.
- 현재 구성된 데이터베이스 종류 및 상태를 조회할 수 있도록 함.
- 차후 설정 변경을 위한 기반 마련 (DB 변경 시 주의사항 안내 등).

## 4. Docker 및 Podman 배포 (Containerization)
- `.NET` 애플리케이션에 최적화된 멀티 스테이지 빌드 기반 `Dockerfile` 작성.
- 영구 보존이 필요한 데이터(Git Repositories, SQLite DB 파일, 설정 파일 등)를 외부로 안전하게 뺄 수 있도록 볼륨(Volume) 마운트 구조 설계.
- `docker-compose.yml` 예제 작성을 통해 컨테이너 환경에서 사용자가 즉시 띄울 수 있도록 가이드 제공.

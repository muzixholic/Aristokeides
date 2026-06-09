# Phase 17 Plan: Docker/Podman 배포 환경 구축

## 1. 🎯 Objective
- 사용자가 복잡한 환경 설정 없이 단일 명령어로 손쉽게 Aristokeides를 구동할 수 있도록 컨테이너화(Docker/Podman) 지원 환경을 구축합니다.
- 멀티 스테이지 빌드를 적용하여 최적화된 `Dockerfile`을 작성하고, `docker-compose.yml`을 통해 볼륨 및 포트 매핑 등 기본 실행 환경을 제공합니다.

## 2. 📝 Tasks

### Task 1: 최적화된 `Dockerfile` 작성
- 프로젝트 루트 경로(`E:\Workspace\VisualC#\Aristokeides\Dockerfile`)에 작성합니다.
- **빌드 스테이지**: `mcr.microsoft.com/dotnet/sdk:10.0` 기반으로 의존성을 복원하고 `dotnet publish`를 통해 릴리즈 빌드를 생성합니다.
- **런타임 스테이지**: `mcr.microsoft.com/dotnet/aspnet:10.0` 기반으로 구성합니다.
  - Aristokeides는 백엔드에서 실제 `git` CLI 명령어(`git upload-pack`, `git receive-pack`)를 호출하므로 런타임 이미지에 반드시 `git` 패키지를 설치해야 합니다. (`apt-get update && apt-get install -y git`)
  - 빌드 스테이지에서 생성된 결과물을 복사하고 `ENTRYPOINT`를 설정합니다.

### Task 2: `docker-compose.yml` 작성
- 프로젝트 루트 경로에 컨테이너 실행을 돕는 `docker-compose.yml` 파일을 작성합니다.
- **네트워크/포트 매핑**: 웹 서비스용 포트(예: 8080:8080)와 SSH Git 접속용 포트(예: 2222:2222)를 개방합니다.
- **볼륨 마운트 (영속성 보장)**: 
  - Git 저장소 디렉토리: `./data/repositories:/app/repositories`
  - SQLite 데이터베이스 파일: `./data/aristokeides.db:/app/aristokeides.db`
  - 설정 파일: `./data/appsettings.json:/app/appsettings.json` (기본 빈 파일이나 초기 설정 매핑 목적)

### Task 3: 런타임 환경 및 문서 가이드 보완 (선택)
- 컨테이너 내부 환경 변수(Environment variables)를 통해 `ASPNETCORE_ENVIRONMENT` 등을 설정합니다.
- 사용자가 직접 DB를 외부 PostgreSQL 등으로 연결하고자 할 때를 대비해 `docker-compose.yml` 주석에 DB 연동 예시를 기입합니다.

## 3. 🔍 Verification
- [ ] `docker build -t aristokeides:latest .` 명령어가 성공적으로 실행되는지 확인한다.
- [ ] 만들어진 런타임 이미지 내부에 `git --version` 명령어가 정상 실행되는지 확인한다.
- [ ] `docker-compose.yml` 파일의 볼륨과 포트 설정이 누락 없이 잘 구성되어 있는지 확인한다.

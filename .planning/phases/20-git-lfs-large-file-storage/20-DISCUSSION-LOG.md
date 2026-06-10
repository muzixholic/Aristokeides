# Phase 20: Git LFS (Large File Storage) 지원 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-10
**Phase:** 20-Git LFS (Large File Storage) 지원
**Areas discussed:** LFS 바이너리 저장 구조 및 경로, LFS Batch API 인증 메커니즘, LFS Locks API 구현 범위, 웹 UI 상에서의 LFS 파일 처리 방식

---

## LFS 바이너리 저장 구조 및 경로

| Option | Description | Selected |
|--------|-------------|----------|
| 글로벌 LFS 저장소 활용 | 동일 파일이 여러 저장소에 중복 업로드될 때 디듀플리케이션(중복 제거)이 가능하며 스토리지 효율성이 높음 | ✓ |
| 개별 저장소 내부 서브디렉토리 활용 | 각 저장소 폴더 단위로 데이터를 관리(삭제, 백업 등)하기가 편리함 | |

**User's choice:** 글로벌 LFS 저장소 활용 (중복 업로드 방지 및 스토리지 공간 절약)

---

## LFS Batch API 인증 메커니즘

| Option | Description | Selected |
|--------|-------------|----------|
| Basic Auth 후 시한성 토큰 발행 | Basic Auth로 최초 로그인 후 실제 파일 전송(Actions)용으로 만료 시간 있는 시한성 임시 토큰 제공. 보안성이 높음 | ✓ |
| 기존 Basic Auth 매 요청 검증 | 기존 로그인 비밀번호/토큰을 매 전송마다 직접 활용. 복잡도가 낮음 | |

**User's choice:** Basic Auth 후 시한성 토큰 발행 (보안성이 뛰어난 임시 토큰 방식 차용)

---

## LFS Locks API 구현 범위

| Option | Description | Selected |
|--------|-------------|----------|
| 실제 DB 테이블 연동을 통한 완전 구현 | LfsLock 테이블을 추가하여 사용자가 잠근 파일의 상태를 기억하고 충돌 푸시를 방지함 | ✓ |
| 단순 Mocking 처리 | Locks API 요청에 대해 항상 성공 상태만 리턴하고 파일 잠금 로직은 구현하지 않음 | |
| Locks API 미지원 | Locks API 호출 시 404 혹은 지원하지 않는 규격으로 처리함 | |

**User's choice:** 실제 DB 테이블 연동을 통한 완전 구현 (LfsLock 테이블 구성 및 실제 동작 보장)

---

## 웹 UI 상에서의 LFS 파일 처리 방식

| Option | Description | Selected |
|--------|-------------|----------|
| LFS 포인터 감지 후 실제 바이너리 표시 | 이미지는 화면 렌더링, 기타 파일은 다운로드 버튼 제공. 사용자 경험 극대화 | ✓ |
| 원시 LFS 포인터 텍스트만 노출 | 포인터 내용(텍스트)만 그대로 뷰어에 표시하고 다운로드용 전용 링크만 제공 | |

**User's choice:** LFS 포인터 감지 후 실제 바이너리 표시 (사용자 경험 개선)

---

## Deferred Ideas

- 클라우드 스토리지(S3 등) 연동: 이번 페이즈에서는 로컬 글로벌 LFS 스토리지에 집중하고, 클라우드 연동은 향후 확장 페이즈로 연기함.

# Phase 12: Repository Management UI - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-08
**Phase:** 12-Repository-Management-UI
**Areas discussed:** 저장소 삭제 확인 방식, 저장소 이름 중복 및 유효성 검증 방식, 설정 변경 시 피드백 방식, 추가 고급 설정 범위

---

## 1. 저장소 삭제 확인 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 안전 모달 사용 | '저장소명'을 텍스트로 직접 입력해야 삭제 버튼이 활성화되는 모달 방식 | ✓ |
| 단순 확인창 사용 | 브라우저 기본 confirm() 다이얼로그 창 사용 | |

**User's choice:** 안전 모달 사용
**Notes:** 실수로 저장소가 지워지는 문제를 방지하기 위해 Gitea/GitHub 스타일의 입력창 활성화 기반 안전 모달을 사용하기로 함.

---

## 2. 저장소 이름 중복 및 유효성 검증 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 제출 시 검증 | 폼을 제출(Submit)할 때 서버 DB 중복 검사 및 오류 박스 노출 | ✓ |
| 실시간 검증 | 입력 중에 비동기 API 조회를 통해 중복 여부를 실시간 피드백 표시 | |

**User's choice:** 제출 시 검증
**Notes:** 실시간 비동기 검사 대신 구현 비용이 저렴하고 확실한 폼 제출 시점의 서버/DB 검증을 적용하기로 함.

---

## 3. 설정 변경 시 피드백 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 페이지 유지 + 성공 메시지 | 설정 페이지에 그대로 머물며 상단에 성공 안내 박스(그린 톤) 노출 | ✓ |
| 메인 리다이렉트 | 저장 완료 후 리포지토리 메인 화면(/Username/RepoName)으로 이동 | |

**User's choice:** 페이지 유지 + 성공 메시지
**Notes:** 저장이 완료된 후에도 추가적인 다른 설정 조작이 용이하도록 페이지를 유지하고, 녹색 톤의 성공 알림 박스를 띄우는 방식으로 합의함.

---

## 4. 추가 고급 설정 범위

| Option | Description | Selected |
|--------|-------------|----------|
| 기본에만 집중 | 이번 페이즈는 이름, 설명, 가시성(IsPrivate) 변경 및 저장소 삭제 기능만 포함 | ✓ |
| 고급 기능 추가 | 아카이브(Archive) 및 타 사용자에게 소유권 이전(Transfer Ownership) UI/기능까지 포함 | |

**User's choice:** 기본에만 집중
**Notes:** 소유권 이전이나 아카이브 등 고급 설정은 이번 마일스톤 범위에서 보류하고, 추후 필요 시 로드맵 백로그에 추가하기로 함.

---

## the agent's Discretion

- 모달 윈도우 레이아웃 및 팝업 트랜지션 애니메이션 디테일
- 삭제 완료 후 대시보드(`/dashboard`)로의 자동 리다이렉트 처리 방식
- 폼 검증 에러 발생 시 입력 필드 테두리 색상 강조(Red border) 등 세부 UI 효과

---

## Deferred Ideas

- 저장소 아카이브(Archive) 기능
- 저장소 소유권 이전(Transfer Ownership) 기능

# Phase 2: Core Git Operations - Discussion Log

**Date:** 2026-05-29

## 저장소 디렉토리 구조
**Options Presented:**
- `{username}/{repo_name}.git` 형태 (디버깅 및 식별 용이)
- `{uuid}.git` 형태 (저장소 이름이나 사용자 이름 변경에 유연함)
**User Selection:** `{username}/{repo_name}.git` 형태 (디버깅 및 식별 용이)

## Git 클라이언트 인증 방식
**Options Presented:**
- 로그인 이메일과 비밀번호를 그대로 사용 (초기 MVP 개발 속도를 위해)
- 별도의 Personal Access Token(PAT) 발급 방식 사용 (보안상 더 안전함)
**User Selection:** 로그인 이메일과 비밀번호를 그대로 사용 (초기 MVP 개발 속도를 위해)

## 저장소 생성 오류 처리
**Options Presented:**
- 먼저 디렉토리를 생성하고 성공하면 DB에 기록 (DB 실패 시 디렉토리 삭제)
- 먼저 DB에 기록하고 성공하면 디렉토리를 생성 (디렉토리 생성 실패 시 DB 레코드 삭제)
- 먼저 DB에 '생성 중' 상태로 기록하고 백그라운드에서 디렉토리를 생성 (비동기 방식)
**User Selection:** 먼저 DB에 '생성 중' 상태로 기록하고 백그라운드에서 디렉토리를 생성 (비동기 방식)

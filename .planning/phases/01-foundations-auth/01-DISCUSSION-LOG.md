# Phase 1: Foundations & Auth - Discussion Log

**Gathered:** 2026-05-29

## Q: DB 초기화 및 마이그레이션
- 앱 시작 시 EF Core Migrate()를 호출하여 자동 적용 (초기 개발 편의성 및 단순성 추구)
- 별도의 CLI 명령어나 스크립트로 분리하여 필요할 때 수동 적용 (실제 운영 환경과 유사한 엄격성)
**User selected:** 앱 시작 시 EF Core Migrate()를 호출하여 자동 적용

## Q: 인증 방식 (Auth Type)
- Cookie 기반 인증 (전통적인 서버 사이드 웹 애플리케이션 방식으로 빠르고 간편함)
- JWT(JSON Web Token) 기반 인증 (API-first 방식으로 향후 외부 클라이언트 연동 용이)
**User selected:** JWT(JSON Web Token) 기반 인증

## Q: 권한 확인 (Role Enforcement)
- 로그인 시점에 역할을 Claims(토큰/쿠키)에 구워 넣고(Baking) 이를 신뢰 (빠른 응답 속도)
- 미들웨어에서 매번 DB를 조회하여 최신 권한을 확인 (엄격한 실시간 제어)
**User selected:** 로그인 시점에 역할을 Claims(토큰/쿠키)에 구워 넣고(Baking) 이를 신뢰

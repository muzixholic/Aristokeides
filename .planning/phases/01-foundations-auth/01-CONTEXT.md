# Phase 1: Foundations & Auth - Context

**Gathered:** 2026-05-29
**Status:** Ready for planning

<domain>
## Phase Boundary

초기 프로젝트 셋업 및 기본 인증/권한 구현
</domain>

<decisions>
## Implementation Decisions

### DB 초기화 및 마이그레이션
- **D-01:** 앱 시작 시 EF Core Migrate()를 호출하여 마이그레이션을 자동 적용합니다. (초기 개발 편의성 및 단순성 추구)

### 인증 방식 (Auth Type)
- **D-02:** JWT(JSON Web Token) 기반 인증을 사용합니다. API-first 방식으로 구현하여 향후 외부 클라이언트 연동에 대비합니다.

### 권한 확인 (Role Enforcement)
- **D-03:** 로그인 시점에 역할을 토큰의 Claims에 구워 넣고(Baking), 서버에서는 이를 신뢰하여 권한을 확인합니다. (빠른 응답 속도 우선)

### the agent's Discretion
나머지 세부적인 C# / .NET 프로젝트 구조, 미들웨어 배치, DB 스키마 설계 등은 요원의 기술적 판단에 따릅니다.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

No external specs — requirements fully captured in decisions above.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- None (Greenfield project)

### Established Patterns
- None (Greenfield project)

### Integration Points
- None (Greenfield project)
</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches.
</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope
</deferred>

---

*Phase: 1-Foundations & Auth*
*Context gathered: 2026-05-29*

# Phase 7: SSH Commit Signature - UI Spec

이 문서는 Phase 7 (SSH 키 기반 커밋 디지털 서명 서버 검증 및 Verified 배지 표시) 구현을 위한 UI 디자인 계약서입니다.

---

## 1. Visual Aesthetics & Design System Tokens

Verified 배지는 커밋의 신뢰성을 나타내는 중요한 시각적 요소입니다. 가독성이 높고 프리미엄 느낌을 주는 정밀한 컬러 토큰을 정의합니다.

### Color Palette & Tokens
- **Verified Background**: `rgba(218, 251, 225, 0.6)` (연한 연두색 반투명, 다크 모드/라이트 모드 범용 대응) 또는 고정색 `#dafbe1`
- **Verified Text & Border**: `#1a7f37` (진한 연두초록색)
- **Hover Micro-interaction**: 마우스 호버 시 배경색이 조금 더 짙어지며(`rgba(218, 251, 225, 0.9)`), 그림자 효과(`box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05)`)를 부여하여 깊이감을 제공합니다.

### Typography
- **Font-Family**: Inter, system-ui, -apple-system, sans-serif
- **Font-Size**: `0.75rem` (12px)
- **Font-Weight**: `600` (Semi-bold)

---

## 2. Components & Layouts

### 2.1 커밋 히스토리 테이블 (`RepoCommits.razor`)
기존 4컬럼 구조에서 5번째 컬럼(서명 컬럼)이 테이블에 추가됩니다.

#### Layout Specification
- **신규 컬럼 제목**: `서명` (테이블 헤더 5번째 위치, 너비 자동 조절)
- **배지 렌더링**: Verified 상태인 커밋 행의 서명 셀에 `Verified` 배지가 배치됩니다. 
- **서명 상태별 렌더링 규칙**:
  - `Verified`: 초록색 배지 (`Verified`) 노출
  - `NoSignature` / `Invalid` / `Unknown` : 배지 미노출 (빈 칸 유지로 리스트의 시각적 노이즈 차단)

#### Responsive Breakpoints
- **Mobile (< 768px)**: 서명 컬럼은 화면 공간 확보를 위해 `display: none` 처리되며, 커밋 해시 옆이나 커밋 메시지 하단에 간소화된 아이콘 형태로 표시되거나 생략됩니다. 여기서는 테이블 자체의 반응형 스크롤 혹은 컬럼 숨김 스타일을 적용합니다.

### 2.2 커밋 상세 페이지
커밋 상세 페이지가 구축되는 경우, 우측 영역 혹은 커밋 메타데이터 영역에 Verified 배지가 상세 설명과 함께 표시됩니다.
- **표시 정보**:
  - 서명 상태 (Verified, Invalid, Unknown)
  - 서명에 사용된 SSH 키의 지문(Fingerprint) (예: `SHA256:abc...`)
  - 서명자(User) 이름 및 키의 알고리즘 (Ed25519, RSA-3072+, ECDSA 등)

---

## 3. UI Implementation Code Snippet (HTML/CSS)

Blazor 컴포넌트 내에 인라인 혹은 CSS 클래스로 활용할 배지 스타일 스니펫입니다.

```html
<span class="badge-verified" title="이 커밋은 인증된 SSH 키로 서명되었습니다.">
    <svg aria-hidden="true" height="12" viewBox="0 0 12 12" width="12" style="fill: currentColor; margin-right: 4px; vertical-align: middle;">
        <path d="M4.881 1.046a.75.75 0 0 1 .737-.546h.764a.75.75 0 0 1 .737.546l.18.616c.075.257.29.45.553.498l.628.115a.75.75 0 0 1 .581.581l.115.628c.048.263.24.478.498.553l.616.18a.75.75 0 0 1 .546.737v.764a.75.75 0 0 1-.546.737l-.616.18a.755.755 0 0 0-.498.553l-.115.628a.75.75 0 0 1-.581.581l-.628.115a.75.75 0 0 0-.553.498l-.18.616a.75.75 0 0 1-.737.546h-.764a.75.75 0 0 1-.737-.546l-.18-.616a.755.755 0 0 0-.553-.498l-.628-.115a.75.75 0 0 1-.581-.581l-.115-.628a.75.75 0 0 0-.498-.553l-.616-.18a.75.75 0 0 1-.546-.737v-.764a.75.75 0 0 1 .546-.737l.616-.18c.257-.075.45-.29.498-.553l.115-.628a.75.75 0 0 1 .581-.581l.628-.115c.263-.048.478-.24.553-.498l.18-.616ZM6 3a3 3 0 1 0 0 6 3 3 0 0 0 0-6Zm1.28 2.28-1.5 1.5a.75.75 0 0 1-1.06 0l-.75-.75a.75.75 0 1 1 1.06-1.06l.22.22.97-.97a.75.75 0 1 1 1.06 1.06Z"></path>
    </svg>
    Verified
</span>

<style>
.badge-verified {
    display: inline-flex;
    align-items: center;
    background-color: #dafbe1;
    color: #1a7f37;
    border: 1px solid rgba(26, 127, 55, 0.2);
    border-radius: 2em;
    padding: 2px 8px;
    font-size: 0.75rem;
    font-weight: 600;
    line-height: 1.2;
    cursor: default;
    transition: background-color 0.15s ease, border-color 0.15s ease;
}

.badge-verified:hover {
    background-color: #d2f9dc;
    border-color: rgba(26, 127, 55, 0.4);
}
</style>
```

---

## 4. UI Safety & UX Checklist
- [x] Verified 배지 컬러 토큰이 라이트 모드(하얀 배경) 및 다크 모드에서도 가독성 규격을 만족하는지 점검.
- [x] SVG 체크 아이콘이 픽셀 깨짐 없이 정렬되어 있는지 점검.
- [x] 호버 상태의 미세 애니메이션(색상 전환)이 부드럽게 작동하는지 점검.
- [x] 모바일 뷰에서 테이블 컬럼이 과도하게 찌그러지지 않고 적절히 숨겨지는지 레이아웃 브레이크포인트 검증.

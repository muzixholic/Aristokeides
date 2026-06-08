# Phase 11: Homepage & Dashboard - Execution Summary

## Overview
- **Phase:** 11
- **Status:** Complete

## Executed Plans
1. **Repository 모델 스키마 확장**: `IsPrivate`, `PrimaryLanguage`, `UpdatedAt` 속성 추가 및 DB 마이그레이션 적용.
2. **루트 리다이렉션 라우팅 처리**: `RootController.cs`를 구현하여 비로그인 시 `/home`으로, 로그인 시 `/dashboard`로 302 리다이렉션.
3. **전역 아이콘 종속성 추가**: Bootstrap Icons CDN 추가.
4. **랜딩 페이지 구현**: 비로그인 사용자를 위한 프로젝트 소개 페이지 (`Home.razor`) 구현.
5. **대시보드 구현**: 로그인 사용자를 위한 자신이 접근 가능한 저장소 목록 뷰 (`Dashboard.razor`) 구현.

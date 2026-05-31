---
phase: 3
status: gaps_found
---

# Phase 3 UAT Results

## Test Cases

### 1. Web Login & Session Cookie
- **Instructions**: Navigate to `/login` and log in using an existing account's email and password.
- **Expected Outcome**: Successful login redirects you to the application and sets a session cookie.
- **Status**: ✅ PASSED

### 2. Repository Root View
- **Instructions**: Navigate to a repository you own (e.g., `/{username}/{repoName}`).
- **Expected Outcome**: The repository root directory, branch selector, and files are displayed. Empty repos should show "빈 저장소입니다".
- **Status**: ✅ PASSED

### 3. File Navigation & Syntax Highlighting
- **Instructions**: Click into a subdirectory and then select a code file.
- **Expected Outcome**: The file contents are displayed with basic syntax highlighting (via highlight.js).
- **Status**: ❌ FAILED (Syntax highlighting missing, text is all black)

### 4. Commit History & Pagination
- **Instructions**: Navigate to the commits page (e.g., `/{username}/{repoName}/commits/{branch}`).
- **Expected Outcome**: A list of commits is displayed with hashes, messages, authors, and dates. "Previous/Next" pagination buttons work.
- **Status**: ✅ PASSED

### 5. Access Control
- **Instructions**: Open an incognito window or log out, then try to access a repository URL directly.
- **Expected Outcome**: You are blocked or redirected to login.
- **Status**: ✅ PASSED

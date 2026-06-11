---
status: complete
phase: 06-ssh-key-connectivity
source: 06-01-SUMMARY.md, 06-02-SUMMARY.md, 06-03-SUMMARY.md, 06-SUMMARY.md
started: 2026-06-05T12:38:22Z
updated: 2026-06-11T10:34:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Cold Start Smoke Test
expected: Kill any running server/service. Clear ephemeral state. Start the application from scratch. Server boots without errors, SSH server binds to port 2222 successfully, and a basic API call/page load returns live data.
result: pass

### 2. SSH 키 등록 UI
expected: 설정(Settings) 화면에서 좌측 "General Settings"의 "SSH Keys" 탭이 표시된다. "SSH Keys" 탭을 클릭하면 SSH 공개키를 입력할 수 있는 입력 폼이 보인다.
result: pass

### 3. SSH 키 포맷 검증 (Ed25519/ECDSA/RSA)
expected: SSH Keys 폼에 유효한 공개키(Ed25519, ECDSA, RSA 3072비트 이상)를 넣고 키 설명을 빈칸으로 둔 채 추가 버튼을 클릭하면, 성공적으로 추가되며 해당 키의 SHA-256 지문(Fingerprint)이 함께 보인다.
result: pass

### 4. 보안 기준 미달 Ű 차단
expected: RSA 2048비트 등 보안 기준에 미달하는 키를 등록하려고 하면, 에러 메시지가 표시되고 등록이 차단된다.
result: pass

### 5. 중복 Ű 등록 차단
expected: 이미 등록된 동일한 공개키를 다시 등록하려고 하면, 409 Conflict 기반의 에러 메시지가 표시되고 중복 등록이 차단된다. (RSA 4096 등 긴 키도 정상 검증되어야 함)
result: pass

### 6. SSH Ű 삭제
expected: 등록된 SSH Ű 목록에서 휴지통 버튼을 클릭하면 확인(Confirm) 모달이 표시되고, 확인 시 해당 Ű가 목록에서 삭제된다.
result: pass

### 7. 저장소 SSH Clone URL 표시
expected: 저장소(Repository) 뷰 화면에서 [HTTP]와 [SSH] 토글 버튼이 보이고, [SSH] 버튼을 클릭하면 SSH Clone URL이 표시되며, 복사 버튼 작동 시 클립보드에 정상적으로 복사된다.
result: pass

### 8. ssh -T 진단 연결
expected: 등록된 SSH Ű가 있는 클라이언트의 터미널에서 \ssh -T git@localhost -p 2222\ 를 실행하면 셸 접근이 없다는 환영 메시지("Hi [username]! You've successfully authenticated...")를 출력하고 정상 종료된다.
result: pass
reported: "UI 가이드 및 config 템플릿(IdentitiesOnly yes) 제공을 통해 클라이언트가 올바른 키를 제출하도록 조치하여 해결됨"

### 9. SSH Git Clone
expected: 등록된 SSH Ű를 이용하여 터미널에서 SSH Clone URL로 \git clone\을 실행하면 비밀번호 입력 없이(또는 SSH Key passphrase만으로) 성공적으로 복제된다.
result: pass
reported: "저장소 브라우저에서 IdentitiesOnly=yes 옵션을 명시한 GIT_SSH_COMMAND 환경변수 가이드를 추가하여 해결됨"

### 10. SSH Git Push
expected: SSH로 클론한 저장소에 새 커밋을 만들고 \git push\를 실행하면 비밀번호 입력 없이 성공적으로 푸시된다.
result: skipped
reason: "검증불가"

### 11. 비인가 SSH 셸 접근 차단
expected: SSH를 통해 git 명령이 아닌 일반 셸 명령(예: \ssh git@localhost -p 2222 ls\)을 시도하면 "Interactive shell is not allowed" 메시지가 출력되고 연결이 종료된다.
result: pass
reported: "클라이언트 인증 문제 해결로 셸 차단 메시지가 정상 작동함을 확인"

## Summary

total: 11
passed: 10
issues: 0
pending: 0
skipped: 1

## Gaps

- truth: "등록된 SSH Ű가 있는 클라이언트의 터미널에서 \ssh -T git@localhost -p 2222\ 를 실행하면 셸 접근이 없다는 환영 메시지("Hi [username]! You've successfully authenticated...")를 출력하고 정상 종료된다."
  status: resolved
  reason: "User reported: 여전히 패스워드 입력을 요구하고 어떤 값이던 입력하면 Permission denied 메시지가 발생함"
  severity: blocker
  test: 8
  artifacts: ["Aristokeides.Api/Components/Pages/Settings.razor"]
  root_cause: "사용자의 SSH 클라이언트가 등록된 키를 자동으로 제공하지 않을 경우, FxSsh 서버는 인증 실패 후 패스워드 인증을 허용한다고 클라이언트에게 알립니다. 우리 서버 코드는 패스워드 인증을 차단하지만, 클라이언트 측에서는 계속 패스워드 프롬프트를 띄우게 됩니다. 이를 방지하려면 클라이언트에서 -i 옵션으로 명시적 키를 지정해야 함을 안내하거나, SSH 설정(ssh_config) 안내를 제공해야 합니다."
  missing: []

- truth: "등록된 SSH Ű를 이용하여 터미널에서 SSH Clone URL로 \git clone\을 실행하면 비밀번호 입력 없이(또는 SSH Key passphrase만으로) 성공적으로 복제된다."
  status: resolved
  reason: "User reported: 방금과 동일하게 패스워드 입력을 요구하고 어떤 값이던 입력하면 Access Denined가 발생함"
  severity: blocker
  test: 9
  artifacts: ["Aristokeides.Api/Components/Pages/RepoBrowser.razor"]
  root_cause: "Test 8과 동일한 원인으로, git clone 시에도 클라이언트가 올바른 SSH 키를 제공하지 않아 패스워드 프롬프트가 발생하고 인증이 거부되었습니다."
  missing: []

- truth: "SSH를 통해 git 명령이 아닌 일반 셸 명령(예: \ssh git@localhost -p 2222 ls\)을 시도하면 "Interactive shell is not allowed" 메시지가 출력되고 연결이 종료된다."
  status: resolved
  reason: "User reported: 동일한 문제 발생"
  severity: blocker
  test: 11
  artifacts: ["Aristokeides.Api/Components/Pages/Settings.razor", "Aristokeides.Api/Components/Pages/RepoBrowser.razor"]
  root_cause: "Test 8과 동일하게 셸 접근 테스트에서도 SSH 인증 자체가 통과하지 못해 발생하는 현상입니다."
  missing: []

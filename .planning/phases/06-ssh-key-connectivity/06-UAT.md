---
status: partial
phase: 06-ssh-key-connectivity
source: 06-01-SUMMARY.md, 06-02-SUMMARY.md, 06-03-SUMMARY.md
started: 2026-06-05T01:23:00Z
updated: 2026-06-05T01:23:00Z
---

## Current Test
<!-- OVERWRITE each test - shows where we are -->

[testing paused — 2 items outstanding]

## Tests

### 1. SSH 키 설정 페이지 접근
expected: 설정(Settings) 화면에 접속했을 때 "General Settings"와 "SSH Keys" 탭이 표시된다. "SSH Keys" 탭을 클릭하면 SSH 공개키를 등록할 수 있는 입력 폼이 나타난다.
result: pass

### 2. SSH 공개키 등록 (Ed25519/ECDSA/RSA)
expected: SSH Keys 탭의 입력 폼에 유효한 공개키(Ed25519, ECDSA, RSA 3072비트 이상)를 붙여넣으면 키 주석이 라벨로 자동 파싱되어 채워지고, 등록 버튼을 클릭하면 목록에 해당 키가 SHA-256 지문(Fingerprint)과 함께 나타난다.
result: pass

### 3. 보안 강도 미달 키 차단
expected: RSA 2048비트 이하의 약한 키 또는 지원하지 않는 알고리즘의 키를 등록하려 하면 에러 배너/메시지가 표시되고 등록이 거부된다.
result: pass

### 4. 중복 키 등록 차단
expected: 이미 등록된 것과 동일한 공개키를 다시 등록하려 하면 409 Conflict 등의 오류 메시지가 표시되고 중복 등록이 차단된다.
result: issue
reported: "rsa 4096으로 생성한 키를 넣어도 포멧이 안맞는다는 에러메시지가 나옴"
severity: major

### 5. SSH 키 삭제
expected: 등록된 SSH 키 목록에서 삭제 버튼을 클릭하면 확인(Confirm) 모달이 표시되고, 확인을 누르면 해당 키가 목록에서 즉시 제거된다.
result: pass

### 6. 저장소 SSH Clone URL 표시
expected: 저장소(Repository) 메인 화면에서 [HTTP]와 [SSH] 프로토콜 토글 위젯이 보이고, [SSH] 를 선택하면 SSH Clone URL(예: git@도메인:사용자명/저장소명.git)이 표시된다. 📋 복사 버튼을 누르면 클립보드에 복사되고 2초간 툴팁이 노출된다.
result: issue
reported: "SSH 토글 위젯은 있지만 클릭해도 아무런 변화가 없음. HTTP 상태에서 복사 버튼을 눌러도 아무런 반응 없음."
severity: major

### 7. ssh -T 진단 연결 (서버 구동 확인)
expected: 로컬 터미널에서 `ssh -T git@localhost` (또는 설정된 도메인/포트)를 실행하면 인증된 사용자 이름이 담긴 웰컴 메시지를 수신하고 정상 종료된다.
result: issue
reported: "접속시 패스워드 입력창이 나오고 아무 값이나 입력했을 시 OnUserAuth에서 e.Key가 null이라 System.ArgumentNullException: 'Value cannot be null. (Parameter 'source')' 에러가 발생함"
severity: blocker

### 8. SSH Git Clone
expected: 등록된 SSH 키를 사용하여 로컬 터미널에서 SSH Clone URL로 `git clone` 명령을 실행하면 저장소가 성공적으로 복제된다.
result: blocked
blocked_by: server
reason: "blocked"

### 9. SSH Git Push
expected: SSH로 클론한 저장소에서 변경 사항을 만들고 `git push`를 실행하면 변경 사항이 서버에 성공적으로 푸시된다.
result: blocked
blocked_by: server
reason: "blocked"

### 10. 비인가 SSH 셸 접근 차단
expected: SSH로 연결 후 git 명령 외의 일반 셸 명령(예: `ssh git@localhost ls`)을 실행하면 "Interactive shell is not allowed" 메시지와 함께 접속이 차단/종료된다.
result: issue
reported: "ssh: connect to host localhost port 22: Connection refused"
severity: major

## Summary

total: 10
passed: 4
issues: 4
pending: 0
skipped: 0
blocked: 2

## Gaps

- truth: "SSH로 연결 후 git 명령 외의 일반 셸 명령(예: `ssh git@localhost ls`)을 실행하면 Interactive shell is not allowed 메시지와 함께 접속이 차단/종료된다."
  status: failed
  reason: "User reported: ssh: connect to host localhost port 22: Connection refused"
  severity: major
  test: 10
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "로컬 터미널에서 `ssh -T git@localhost` (또는 설정된 도메인/포트)를 실행하면 인증된 사용자 이름이 담긴 웰컴 메시지를 수신하고 정상 종료된다."
  status: failed
  reason: "User reported: 접속시 패스워드 입력창이 나오고 아무 값이나 입력했을 시 OnUserAuth에서 e.Key가 null이라 System.ArgumentNullException: 'Value cannot be null. (Parameter 'source')' 에러가 발생함"
  severity: blocker
  test: 7
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "저장소(Repository) 메인 화면에서 [HTTP]와 [SSH] 프로토콜 토글 위젯이 보이고, [SSH] 를 선택하면 SSH Clone URL이 표시되며, 복사 버튼 작동 시 클립보드에 복사되고 툴팁이 노출된다."
  status: failed
  reason: "User reported: SSH 토글 위젯은 있지만 클릭해도 아무런 변화가 없음. HTTP 상태에서 복사 버튼을 눌러도 아무런 반응 없음."
  severity: major
  test: 6
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "이미 등록된 것과 동일한 공개키를 다시 등록하려 하면 409 Conflict 등의 오류 메시지가 표시되고 중복 등록이 차단된다."
  status: failed
  reason: "User reported: rsa 4096으로 생성한 키를 넣어도 포멧이 안맞는다는 에러메시지가 나옴"
  severity: major
  test: 4
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

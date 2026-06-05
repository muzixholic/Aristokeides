# Phase 06: SSH Key Connectivity - Gap Closure Plan

**Target:** .planning/phases/06-ssh-key-connectivity/06-PLAN.md
**Status:** Planned

## Objective
Fix the remaining issues identified during UAT verification for Phase 06.

## Tasks

### 1. Fix SSH Server Port Binding Issue (Test 10)
- **File:** \Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs\
- **Action:** Ensure the SSH server binds correctly to port 2222 as configured, and document that clients must use \-p 2222\ when testing locally. No code changes are required if it's already binding to 2222, but we need to update the connection test instructions. (If any code config is hardcoded to 22, change it to 2222).

### 2. Add Null Check for e.Key in Authentication (Test 7)
- **File:** \Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs\
- **Action:** In the \OnUserAuth\ event handler, add a null check for \e.Key\. If \e.Key\ is null (which happens during password auth or non-publickey attempts), immediately return or reject the auth attempt to prevent \ArgumentNullException\ during \SHA256.HashData(e.Key)\.

### 3. Fix Blazor Interactivity for RepoBrowser (Test 6)
- **File:** \Aristokeides.Api/Components/Pages/RepoBrowser.razor\
- **Action:** Add the \@rendermode Microsoft.AspNetCore.Components.Web.RenderMode.InteractiveServer\ directive at the top of the file to ensure the SSH/HTTP toggle and copy-to-clipboard JavaScript interop function correctly.

### 4. Improve SSH Key Parsing Error Handling (Test 4)
- **File:** \Aristokeides.Api/Services/Ssh/SshKeyParser.cs\
- **Action:** Modify \ParseAndValidatePublicKey\ to handle common copy-paste formatting issues:
  - Replace any \\r\ and \\n\ with empty strings or spaces before processing.
  - Use \Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)\ instead of \Split(' ')\ to tolerate extra spaces.
- **File:** \Aristokeides.Api/Controllers/SshKeysController.cs\
- **Action:** Modify the \catch\ blocks in \Register\ to return the actual \ex.Message\ from the parser (e.g. \BadRequest(new { message = ex.Message })\) instead of masking it with the generic "Invalid key format..." message.


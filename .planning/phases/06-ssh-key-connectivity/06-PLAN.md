# Phase 06: SSH Key Connectivity - 2nd Gap Closure Plan

**Target:** .planning/phases/06-ssh-key-connectivity/06-PLAN.md
**Status:** Completed

## Objective
Fix the remaining issues identified during the 2nd UAT verification for Phase 06.

## Tasks

- [x] Update UI to provide SSH Configuration Instructions

### 1. Update UI to provide SSH Configuration Instructions (Tests 8, 9, 11)
- **File:** \Aristokeides.Api/Components/Pages/Settings.razor\ (or equivalent instructions page if Settings is where keys are registered) and \Aristokeides.Api/Components/Pages/RepoBrowser.razor\
- **Action:** Add clear instructions explaining that users must configure their SSH client to use their specific private key (e.g., via \~/.ssh/config\ \IdentityFile\ directive or by using the \ssh -i\ flag) because SSH does not automatically offer non-default keys. Explain that if the client doesn't offer the correct key, they will see a misleading password prompt which will always fail.
- **Action:** In \RepoBrowser.razor\, update the empty repository instructions to suggest \GIT_SSH_COMMAND="ssh -i /path/to/key"\ if the user is not using ssh-agent.



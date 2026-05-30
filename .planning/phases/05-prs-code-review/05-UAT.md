---
status: testing
phase: 05-prs-code-review
source: 01-SUMMARY.md, 02-SUMMARY.md
started: 2026-05-30T15:05:22Z
updated: 2026-05-30T15:05:22Z
---

## Current Test

number: 1
name: Navigate to Pull Requests
expected: |
  User visits a repository page and clicks "Pull Requests" tab. The PR list page loads and shows the "Create Pull Request" button. If there are no PRs, a placeholder message appears.
awaiting: user response

## Tests

### 1. Navigate to Pull Requests
expected: |
  User visits a repository page and clicks "Pull Requests" tab. The PR list page loads and shows the "Create Pull Request" button. If there are no PRs, a placeholder message appears.
result: [pending]

### 2. Create a Pull Request
expected: |
  User clicks "Create Pull Request", selects Source and Target branches, fills in title and description, and clicks "Create Pull Request". User is redirected to the newly created Pull Request detail page.
result: [pending]

### 3. View Pull Request Details and Diff
expected: |
  User views the Pull Request detail page. The PR title, status (Open), source and target branches are shown. The original description is visible. Below, a "Changes" section shows the inline Diff between branches.
result: [pending]

### 4. Add a Comment
expected: |
  User types a comment in the "Leave a comment" box and clicks "Comment". The new comment appears immediately in the Conversation section above the Diff viewer.
result: [pending]

### 5. Merge Pull Request
expected: |
  User clicks "Merge pull request" in the sidebar. The pull request is merged into the target branch. The status changes to "Merged", the merge button disappears or shows the merge commit SHA, and the PR is closed.
result: [pending]

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0

## Gaps


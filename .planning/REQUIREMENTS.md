# REQUIREMENTS.md

## v1 Requirements

### Authentication
- [ ] **AUTH-01**: User can create an account and log in with email/password
- [ ] **AUTH-02**: System supports basic roles (Admin, Contributor, Reader)

### Repository Management
- [x] **REPO-01**: User can create a new empty Git repository
- [x] **REPO-02**: User can clone, push, and pull via Git Smart HTTP
- [ ] **REPO-03**: User can view repository files, branches, and commit history in the web UI

### Issue Tracking
- [x] **ISSU-01**: User can create, edit, and close issues
- [x] **ISSU-02**: User can view issues on a basic Kanban board

### Code Review
- [ ] **CODE-01**: User can open a Pull Request from one branch to another
- [ ] **CODE-02**: User can view the diff of a Pull Request
- [ ] **CODE-03**: User can leave comments on a Pull Request and merge it

## v2 Requirements (Deferred)
- SSH Git support
- Webhooks
- Advanced Code Review (line-by-line comments)

## Out of Scope
- CI/CD Pipelines (Deferred to keep MVP lean)
- Enterprise-grade cluster deployments
- Wiki and heavy documentation tools

## Traceability
- **Phase 1**: AUTH-01, AUTH-02
- **Phase 2**: REPO-01, REPO-02
- **Phase 3**: REPO-03
- **Phase 4**: ISSU-01, ISSU-02
- **Phase 5**: CODE-01, CODE-02, CODE-03

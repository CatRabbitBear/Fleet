# Fleet Phase 1 Plan: Hardening + Credential Management

## Purpose

This document turns the readiness assessment into a concrete **Phase 1 execution plan** focused on:

1. hardening trust and permission boundaries, and
2. enabling credential management through the web app **without** weakening security.

It also reflects the intended architecture direction:

- adding sibling projects under `src/` is encouraged where it improves separation of concerns,
- data access should be shared across Tray and Blazor via a dedicated data project,
- agent/runtime logic should move into a reusable runtime project.

---

## Phase 1 scope (priority order)

### Priority 1 — Trust boundary hardening for privileged operations

Goal: no privileged action executes without explicit policy evaluation and audit.

### Priority 2 — Secure web-managed credential workflow

Goal: users can manage credentials from web UX, but only Tray writes/reads Windows Credential Manager, and only through a hardened command path.

### Priority 3 — Lightweight architectural extraction to support Phase 1 safely

Goal: introduce minimal new projects needed to avoid leaking privileged/data/runtime concerns into UI projects.

---

## Target architecture during Phase 1

## Trust zones

1. **Requester Zone (lower trust)**
   - Blazor pages
   - browser extension calls
   - localhost API clients

2. **Application Zone (medium trust)**
   - workflow orchestration
   - permission policy evaluation
   - request identity and action classification

3. **Privileged Host Zone (highest trust)**
   - Windows Credential Manager access
   - OS notifications / elevated local actions
   - final authorization for high-risk actions

Rule: crossing into a higher trust zone always requires a signed/validated command contract + policy decision.

---

## Recommended project additions in `src/`

Phase 1 should allow adding **1–2+ new sibling projects** as needed.

## 1) `Fleet.Data` (or `Fleet.Data.Sqlite`)

Responsibilities:

- SQLite connection factory/path resolution
- repositories for:
  - agent/workflow configs
  - artifacts/runs
  - permission decision audit
  - request metadata
- DB migrations/versioning

Why now:

- keeps SQLite concerns out of Blazor UI startup code,
- makes DB equally consumable by Tray and Blazor,
- prepares for future services beyond Blazor.

## 2) `Fleet.Runtime` (or `Fleet.Agents`)

Responsibilities:

- agent pipeline/orchestration
- tool execution abstraction
- provider adapters (or interfaces to provider adapters)
- policy enforcement hooks before tool/action execution

Why now:

- reduces Blazor coupling,
- allows Tray or background workers to reuse runtime logic,
- keeps UI project lean.

## 3) Optional in Phase 1: `Fleet.Host.Abstractions`

Responsibilities:

- contracts for privileged commands (e.g., credential set/delete/read metadata)
- host response models and error codes

Why optional:

- useful if command surface grows quickly,
- can start in `Fleet.Shared` if scope is small, then extract later.

---

## Hardening workstream (point-by-point)

## A) Request identity and trust classification

Implement:

- `RequestIdentity` model with source types:
  - `BlazorUiInteractive`
  - `BrowserExtension`
  - `InternalSystem`
  - `UnknownLocalCaller`
- correlation IDs for every privileged path
- source-aware policy defaults (deny/interactive for ambiguous sources)

Deliverables:

- middleware to attach identity and correlation ID
- identity persisted in audit logs

## B) Action descriptor and policy gate

Implement:

- `ActionDescriptor` with fields such as:
  - `ActionType` (CredentialWrite, ProcessSpawn, FileWrite, NetworkEgress, etc.)
  - `Resource`
  - `RiskLevel`
  - `RequestedBy`
- `IPermissionPolicyService` returning:
  - Allow
  - Deny
  - RequireInteractiveConsent

Deliverables:

- single pre-execution policy gate used by:
  - runtime tool executions
  - credential commands
  - future host actions

## C) Interactive consent integration

Implement:

- use existing notification flow for decisions requiring user confirmation
- enforce timeout + explicit default deny
- capture rationale in audit trail

Deliverables:

- consent prompts include action summary and source
- deny/timeout behavior fully deterministic

## D) Audit and traceability

Implement:

- SQLite audit table(s):
  - request identity
  - action descriptor
  - policy result
  - final outcome
  - timestamps and correlation ID

Deliverables:

- query path for diagnostics page in Blazor
- structured logging aligned to audit records

## E) API hardening baseline

Implement:

- authenticated local session/token for sensitive endpoints
- endpoint-level authorization policies
- anti-forgery/CSRF review for credential-related mutations
- strict model validation and explicit error responses

Deliverables:

- all privileged endpoints require authenticated identity + policy gate
- no anonymous path can invoke privileged host operations

---

## Credential workstream (web UX + Tray authority)

## Non-negotiable security rule

**Windows Credential Manager authority remains in Tray/host layer.**

Blazor can collect input and display status, but cannot directly persist secrets.

## Command flow

1. User opens credential settings page in Blazor.
2. User submits credential change request.
3. Blazor sends **privileged command** to host command endpoint/bus.
4. Host validates:
   - caller identity/session
   - policy result
   - optional interactive consent for high-risk changes
5. Tray credential service writes/deletes credential in Credential Manager.
6. Host returns sanitized result (never returns secret value).
7. Audit log records full decision and outcome metadata.

## Credential API/command design recommendations

Commands:

- `SetCredential(target, value)`
- `DeleteCredential(target)`
- `GetCredentialMetadata(target)` (exists/lastUpdated/scope; no secret value)
- `ValidateCredentialFormat(target, value)` (optional preflight)

Rules:

- deny unknown targets (allowlist only)
- avoid echoing secrets in logs/responses
- redact at logging boundary
- rotate in-memory sensitive buffers quickly where possible

## UX recommendations

- web page can show:
  - required targets and status (set/missing)
  - validation hints
  - last-updated metadata
- web page cannot show existing secret values
- clear restart/reload guidance after updates if runtime config requires it

---

## Definition of Done (Phase 1)

Phase 1 is complete only when **all** criteria below are met.

## Security and policy DoD

- [ ] Every privileged action flows through a unified policy gate.
- [ ] Source identity is attached to every privileged request.
- [ ] Default behavior for unknown/ambiguous source is deny.
- [ ] Interactive consent path enforces timeout + default deny.

## Credential DoD

- [ ] Users can create/update/delete allowed credentials from Blazor UX.
- [ ] Tray/host remains sole writer to Windows Credential Manager.
- [ ] No endpoint returns plain credential secret values.
- [ ] Credential operations are audited with correlation IDs.

## API hardening DoD

- [ ] Sensitive endpoints require authenticated local context.
- [ ] Endpoint authorization policies are implemented and tested.
- [ ] Input validation and error contracts are consistent.

## Data/persistence DoD

- [ ] SQLite access moved to shared data project (or equivalent shared abstraction).
- [ ] Audit schema and migration strategy are in place.
- [ ] Both Tray and Blazor can consume data layer cleanly.

## Architecture DoD

- [ ] Runtime orchestration logic extracted from Blazor into reusable runtime project.
- [ ] Blazor remains primarily UX/API composition layer.
- [ ] Shared contracts remain provider-agnostic (no direct Azure/OpenAI SDK coupling in shared contracts project).

---

## Phase 1 implementation sequence (recommended)

1. **Establish contracts/models first**
   - Request identity, action descriptor, policy result, audit models.
2. **Implement policy gate + audit persistence**
   - wire into one privileged action first (credential write) as reference path.
3. **Add authenticated host command path for credentials**
   - web UX calls host commands, host persists creds.
4. **Expand gate coverage to runtime/tool operations**
   - ensure all risky actions are covered before broad feature additions.
5. **Extract data/runtime projects**
   - migrate code with minimal behavior change.
6. **Run security-focused acceptance tests**
   - unknown source denied, timeout denies, audit completeness, secret redaction.

---

## Risks and mitigation

- **Risk:** implementing web-based credential UX before policy/auth is ready.
  - **Mitigation:** feature flag credential web mutations until gate + auth + audit are live.

- **Risk:** partial migration leaves duplicate data access logic.
  - **Mitigation:** freeze new DB access additions outside `Fleet.Data` after cutover begins.

- **Risk:** runtime extraction introduces regressions.
  - **Mitigation:** snapshot current behavior with integration tests before refactor.

---

## Out of scope for Phase 1

- full multi-user profile/tenant model
- cloud sync of credential metadata
- broad plugin marketplace trust framework
- deep policy UI/editor for non-technical users

These can follow once Phase 1 hardening baseline is complete.

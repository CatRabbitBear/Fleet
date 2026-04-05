# Fleet Readiness Assessment (Tray + Blazor + Shared)

## Scope and framing

This assessment is based on the current proof-of-concept goals in `src/Fleet.Tray/docs/usecases.md` and the implementation in:

- `Fleet.Tray` (desktop host, credentials, notifications, process ownership)
- `Fleet.Blazor` (web UI, API surface, agent pipeline, SQLite persistence, MCP orchestration)
- `Fleet.Shared` (cross-project contracts/services)

The focus is readiness for moving from POC toward safer production architecture, with emphasis on permissions, credential boundaries, and where agent/LLM workflow logic should live.

---

## Executive summary

### What is well prepared right now

1. **Process ownership model is coherent for local-first desktop**
   - `Fleet.Tray` starts and owns the `Fleet.Blazor` host in-process and controls app lifetime.
   - This is a strong base for a local orchestrator pattern where desktop concerns (OS integrations, credentials, notifications) are owned by Tray, while web UX lives in Blazor.

2. **Basic secure-secret handling direction is correct**
   - Credentials are sourced from Windows Credential Manager (not plain-text appsettings) and injected into Blazor configuration at startup.
   - Required credential checks at startup are in place.

3. **Agent execution pipeline exists and is extensible**
   - Pipeline steps (`LoadChatHistory`, `RunChatCompletion`, `SaveOutput`) and a context factory are present.
   - MCP plugin manager/pool shape suggests future multi-tool orchestration is intended.

4. **SQLite persistence exists and is local-user scoped**
   - DB files are created under `%LocalAppData%/Fleet`, which is appropriate for per-user local app state.
   - Separate DBs for plugin manifests and output artifacts already exist.

### What is not yet prepared for safe scaling

1. **No strong authorization boundary between browser clients and privileged host actions**
   - Blazor APIs are callable on localhost HTTPS with broad surface area and no auth.
   - CORS support for extension clients exists, but there is no per-request trust tier or policy engine tied to action risk.

2. **Permission framework is present but not integrated into risky operations**
   - Notification-based permission prompting exists as a service, but agent/plugin execution paths do not consistently require policy checks.

3. **Credential management is Tray-only UI and not exposed via a safe command channel**
   - There is no explicit command bridge for Blazor → Tray privileged actions (e.g., update credential).
   - As a result, web UX cannot currently manage credentials without directly taking on privileged responsibilities.

4. **Data model and persistence are still tactical, not unified domain persistence**
   - SQLite writes are narrow (outputs + MCP manifests) and not yet modeling agent configs, workflows, permission decisions, audit trails, or run lineage from use cases.

5. **Project boundaries are not yet ideal for long-term maintainability**
   - `Fleet.Shared` is currently suitable for contracts and pure abstractions but should not become the home for OpenAI/Azure-specific runtime integrations.

---

## Detailed assessment by concern

## 1) Fleet.Tray owning Fleet.Blazor architecture

### Benefits of the current model

- **Single local authority for privileged OS operations.** Tray can own:
  - Credential Manager access
  - system notifications/toasts
  - future shell/file/task integrations
- **Simple deployment mental model.** One app process tree starts local web UX and API.
- **Good fit for “local copilot” use cases.** This matches your use cases where tasks are local/system-adjacent.

### Risks / limitations to address

- **Privilege inversion risk:** if Blazor endpoints can trigger high-privilege actions without hardened gating, then a web/API surface indirectly gains desktop capabilities.
- **Tight coupling risk:** Tray references Blazor project directly. Over time, UI and host lifecycle can become hard to evolve independently.
- **Remote-surface confusion:** localhost + extension + CORS can blur assumptions about who is trusted.

### Recommendation

Keep Tray-owned architecture, but formalize **three trust zones**:

1. **Untrusted/less-trusted requesters** (browser extension, local HTTP callers)
2. **Application domain services** (agent orchestration/business workflows)
3. **Privileged host adapter** (Tray-only OS operations)

This gives you the upside of current architecture while preserving least privilege.

---

## 2) Safety and permissions readiness

### Current state

- Permission-request primitives exist (`INotificationService`, `PermissionRequest`, toast allow/deny flow).
- But risky operations (tool execution, MCP process launch, file/system actions) do not appear policy-gated end-to-end.
- API controller currently allows `run-task` without authentication/authorization or explicit operation policy checks.

### Gaps

- No **policy engine** (e.g., `IPermissionPolicyService`) with action categories:
  - Read-only local data
  - network egress
  - filesystem write
  - process execution
  - credential access/update
- No **request identity model** (web UI user action, extension origin, internal system action).
- No **durable audit log** of permission decisions and sensitive operations.

### Recommendation

Implement an explicit permission pipeline:

1. Every high-risk action maps to an `ActionDescriptor` (resource + operation + sensitivity).
2. Policy checks run before execution.
3. Policy may auto-allow/deny or require interactive consent via Tray notification.
4. Decision + context are persisted (SQLite audit table) for traceability.

---

## 3) SQLite setup and accessibility across the two architecture legs

### Current state

- SQLite is configured in `Fleet.Blazor` startup and stored in `%LocalAppData%/Fleet`.
- DB handlers are DI-registered in Blazor, and currently used for:
  - MCP server manifests
  - agent output persistence

### What this means for architecture

- Because Tray hosts Blazor in-process, both “legs” can access the same physical DB files **if** they share a common data access layer or API.
- Right now, effective ownership is Blazor-centric (handlers live in Blazor project), so Tray does not naturally participate in DB domain logic.

### Recommendation

Move toward a shared data domain package (or dedicated project), e.g. `Fleet.Core` / `Fleet.Data`:

- EF Core or improved repository abstractions over SQLite
- schemas for:
  - agent definitions
  - workflow definitions
  - runs + steps
  - artifacts
  - permission decisions/audits
  - tool registry/manifests
- keep DB path resolution centralized and injected (do not duplicate path logic per app layer)

This avoids both duplication and Blazor-only ownership of domain persistence.

---

## 4) Where Agent and LLM workflow logic should live

### Should this live in Fleet.Shared?

**Short answer: mostly no for concrete runtime logic.**

`Fleet.Shared` is currently a plain .NET project with lightweight contracts and shared service interfaces. That is a good role. Expanding it to include concrete OpenAI/Azure SDK wiring and heavy workflow orchestration will create problematic coupling:

- pulls cloud/provider dependencies into every consumer
- makes host/UI projects transitively depend on AI runtime stack
- reduces test isolation and multi-provider flexibility

### Recommended placement

- Keep `Fleet.Shared` for:
  - DTOs
  - interfaces/contracts
  - permission/action models
  - small utility abstractions without infrastructure coupling
- Put agent runtime in a new project (suggestion: `Fleet.Agents` or `Fleet.Runtime`):
  - pipeline engine
  - tool execution orchestration
  - model provider adapters
  - policy enforcement hooks
- Put infrastructure integrations in dedicated projects:
  - `Fleet.Providers.AzureOpenAI`
  - `Fleet.Providers.OpenAI`
  - `Fleet.Data.Sqlite`

### Azure/OpenAI dependencies in Shared?

Prefer **not** to put direct Azure/OpenAI dependencies in `Fleet.Shared`. Keep provider SDKs in provider-specific assemblies and expose provider-agnostic interfaces to Shared consumers.

---

## 5) Credential management and whether web app can manage creds

### Current state

- Credential reads/writes are implemented in Tray via Windows Credential Manager helper.
- Credential UI is Tray WPF (`BulkCredentialsWindow` / dashboard action).
- Blazor currently receives credential values via configuration injection during startup.

### Can web app manage credentials directly today?

Not safely/directly with current architecture. There is no explicit secured command channel from Blazor UI/API to Tray privileged actions.

### Are you forced to keep all credential UI in Tray?

**Not strictly forced**, but you should keep **credential persistence authority** in Tray.

Recommended model:

- Blazor can host credential management pages/forms for UX.
- Form submission calls a local authenticated command endpoint (or in-proc command bus) that routes to Tray-owned credential service.
- Tray validates policy and writes to Credential Manager.
- Blazor never directly stores secrets in app DB or plain config.

This gives you better UX without violating privilege boundaries.

---

## Proposed target architecture (incremental)

### Phase 1 (near-term hardening)

1. Add authenticated local API/session boundary for sensitive endpoints.
2. Introduce permission policy service and wire it into all risky actions.
3. Add durable audit tables for permission requests/decisions.
4. Add explicit Tray command interface for privileged operations (credentials first).

### Phase 2 (project boundary cleanup)

1. Extract agent runtime orchestration out of Blazor into `Fleet.Runtime`.
2. Extract SQLite access into `Fleet.Data.Sqlite` (or equivalent).
3. Keep Shared contracts provider-agnostic.
4. Keep Tray as privileged adapter host; keep Blazor as UX/API boundary.

### Phase 3 (use case completeness)

Implement models + CRUD + execution tracing for the use case document:

- configurable agents/workflows
- stored prompts/models/tools/resources
- persisted artifacts and retrieval UI
- extension-origin action handling with permission tiers

---

## Decision log (direct answers to your explicit questions)

1. **Is your Tray-owns-Blazor architecture beneficial?**
   - Yes, strongly for local-first and OS-integrated workflows. Keep it, but formalize trust boundaries.

2. **Where is SQLite set up, and is it available to both legs?**
   - Setup is in Blazor startup, files in `%LocalAppData%/Fleet`. Physically shareable by both legs, but currently Blazor-centric in code ownership.

3. **Where should agent/LLM workflow logic live? Is Shared best?**
   - Shared is not the best home for concrete runtime/provider logic. Use a dedicated runtime project; keep Shared for abstractions/contracts.

4. **Can credentials be managed in web app while still using Credential Manager?**
   - Yes, via a secure Blazor→Tray command path. Persistence authority should remain in Tray. You are not forced to keep all credential UX exclusively in Tray windows.


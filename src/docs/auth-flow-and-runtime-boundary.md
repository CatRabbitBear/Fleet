# Fleet Auth Flow and Runtime Privilege Boundary

_Last updated: April 11, 2026._

## Why this document exists

Before Phase 3 feature expansion, we need one explicit source of truth for:

1. how privileged requests are authenticated and authorized, and
2. where privileged filesystem/process execution is allowed to run.

This prevents accidental regressions while adding workflow/artifact features.

---

## End-to-end auth flow (trusted UI and extension-origin)

## 1) Tray bootstraps local trust context

- `Fleet.Tray` generates a per-process local session token at startup.
- Tray injects the token into Blazor config as `FLEET_LOCAL_SESSION_TOKEN`.
- Tray configures `HttpClient("FleetApi")` to always send:
  - `X-Fleet-Session-Token`
  - `X-Fleet-Caller: blazor-ui`

## 2) Request identity is established first

- `RequestIdentityMiddleware` classifies caller/source from headers.
- A correlation ID is present for every request and written to `RequestIdentityContext`.

## 3) Privileged API endpoint gate

For privileged actions (for example `/api/chat-completions/run-task` and credential mutation routes):

1. `ILocalSessionValidator` rejects invalid/missing session token (`401`).
2. `IPrivilegedActionExecutor.AuthorizeAsync` evaluates policy:
   - `Allow` -> continue,
   - `Deny` -> return `403` and audit,
   - `RequireInteractiveConsent` -> request consent through Tray-hosted notification flow.
3. Final outcome is always audited with correlation + caller metadata.

## 4) Runtime privileged execution gate (new pre-Phase 3 framework)

- Runtime/host adapters now have a shared contract:
  - `IRuntimeExecutionGate`
  - `RuntimeExecutionRequest`
  - `RuntimeExecutionDecision`
- Filesystem/process adapters call this gate before executing OS-sensitive operations.
- The gate bridges to the existing policy/audit pipeline (`IPrivilegedActionExecutor`), so runtime actions get the same enforcement/audit guarantees as controller-entry actions.

---

## Host-owned execution boundary

## Required boundary

- Runtime orchestration can request filesystem/process work.
- Actual privileged execution must remain host-owned.
- In Tray-hosted runs, `TrayRuntimeHostAdapters` are the concrete adapters.

## Implemented hooks

- `IFileSystemHostAdapter` (read/write/list)
- `IProcessHostAdapter` (spawn process)
- `TrayRuntimeHostAdapters` implementation:
  - executes in Tray process,
  - enforces root path boundary (`%LOCALAPPDATA%/Fleet`),
  - invokes runtime policy/audit gate for every operation.

## Standalone fallback

`Fleet.Blazor` keeps local default adapters for non-Tray scenarios, but service registration uses `TryAddScoped` so Tray overrides remain authoritative when hosted.

---

## Pre-Phase 3 guardrails checklist

- [x] Session token remains mandatory on privileged routes.
- [x] Caller identity + correlation IDs flow through request context.
- [x] Policy decisions and final outcomes are audited for privileged actions.
- [x] Runtime filesystem/process adapter contracts are explicit and host-owned.
- [x] Tray has registered concrete runtime filesystem/process adapter hooks.
- [ ] Manual Tray E2E handoff evidence captured in docs for final Phase 2 closeout.

---

## What to verify before starting Phase 3 implementation work

1. Run a Tray-hosted `/agents` request and confirm successful auth path + audit record.
2. Trigger at least one runtime host adapter operation (file write/list or process spawn) and confirm:
   - policy path invoked,
   - audit record present,
   - path guardrails enforced.
3. Capture command outputs/screenshots/log snippets in the Phase 2 closeout artifact.

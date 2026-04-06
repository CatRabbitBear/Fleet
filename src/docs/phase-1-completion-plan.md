# Fleet Phase 1 Completion Plan (Hardening, Credentials, and Runtime Boundaries)

_Last updated: April 6, 2026._

## Why this revision

Phase 1 now has enough implementation in place to move from "hardening started" to "hardening complete" if we close the remaining architecture/documentation gaps around:

1. request authentication + authorization for privileged routes,
2. secure host-owned credential mutation from web workflows, and
3. runtime separation so agents can safely work with the local filesystem without placing privileged execution logic in Blazor.

---

## Phase 1 completion criteria

Phase 1 is considered complete when all items below are true.

## 1) Identity and auth boundary (complete)

- Sensitive API routes require a local session token (`X-Fleet-Session-Token`).
- Request source is stamped (`X-Fleet-Caller`) and normalized to trust categories.
- Correlation IDs are propagated for traceability (`X-Correlation-Id`).
- Unknown callers are denied by policy by default.

## 2) Policy and consent gate (complete)

- Risky actions are mapped to a typed `ActionDescriptor`.
- Policy decisions are deterministic (`Allow`, `Deny`, `RequireInteractiveConsent`).
- Browser-extension initiated high-risk actions require explicit interactive consent.
- Consent timeout defaults to deny.

## 3) Auditability and diagnostics baseline (complete)

- Policy decisions and final outcomes are persisted to SQLite audit storage.
- Every privileged action path writes a durable audit record with source + correlation.
- Diagnostics/audit endpoints and pages can consume these records for operator review.

## 4) Credential authority split (complete)

- Credential persistence authority remains in Tray host (`ICredentialHostService`).
- Blazor can request set/delete/metadata operations only through hardened API routes.
- Secret values are never returned from metadata flows.

## 5) Runtime boundary for local filesystem-capable agents (Phase 1 final architecture decision)

To keep privilege boundaries clean, **agent runtime logic must not stay embedded in Blazor long-term**.

### Decision

Create a sibling runtime abstraction project in the next implementation slice:

- Suggested project: `src/Fleet.Runtime` (or `src/Fleet.Agents.Runtime`).
- Blazor responsibility: UX + API orchestration only.
- Runtime responsibility: agent pipeline/tool execution contracts.
- Host responsibility (Tray): privileged adapters (filesystem/process/system) and final authorization.

### Why this is required for secure local filesystem tooling

If filesystem-capable tool execution is hosted directly in Blazor, the web tier risks becoming the privilege boundary. Keeping runtime contracts outside UI lets us:

- enforce policy before host execution,
- swap execution hosts safely (Tray/background worker), and
- avoid mixing rendering concerns with privileged orchestration.

---

## Project layout target after Phase 1

- `src/Fleet.Blazor`
  - UI, diagnostics, API controllers, request classification.
- `src/Fleet.Tray`
  - process owner, credential manager adapter, interactive consent host.
- `src/Fleet.Data`
  - audit and persistence repositories (already in place).
- `src/Fleet.Shared`
  - contracts, enums, DTOs, permission models.
- `src/Fleet.Runtime` (next extraction)
  - agent orchestration contracts, tool abstraction, runtime composition.

---

## Remaining work to close Phase 1 formally

1. **Documentation move complete:** this document and other phase docs live under `src/docs`.
2. **Runtime extraction kickoff:** scaffold `Fleet.Runtime` and move pipeline interfaces/contracts first.
3. **Filesystem tooling guardrails:** keep filesystem executors in host-owned adapters (not Blazor).
4. **Acceptance checks:** run end-to-end test on `/agents` with local session token + caller identity headers automatically applied by Tray-hosted client.

---

## Out-of-scope for Phase 1 (Phase 2+)

- Multi-user identity model beyond local trusted session.
- Remote auth providers (OIDC/Entra) for non-local deployment topologies.
- Fine-grained per-tool/per-path enterprise policy packs.

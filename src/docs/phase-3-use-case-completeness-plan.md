# Fleet Phase 3 Plan: Use-Case Completeness, Governance, and Operator UX

_Last updated: April 10, 2026._

## Purpose

This document evaluates Phase 1 and Phase 2 progress from the current `src/docs` planning set, then defines a practical Phase 3 implementation plan focused on completing end-user workflow capabilities without weakening the security/runtime boundaries established earlier.

---

## Progress evaluation (Phases 1 and 2)

## Phase 1 status: **Substantially complete with a formal close-out checklist remaining**

Based on:

- `phase-1-completion-plan.md`
- `phase-1-hardening-and-credential-plan.md`

### What appears complete

1. Identity/auth boundary for privileged routes (session token, caller stamping, correlation IDs, default deny for unknown callers).
2. Deterministic policy gate with consent flow integration.
3. Durable audit baseline in SQLite with policy/result logging.
4. Credential authority split: host-owned credential persistence, sanitized metadata-only flows.

### What remains to formally close Phase 1

1. Finalize documentation + acceptance evidence for `/agents` trusted flow end-to-end.
2. Keep runtime/tool execution boundaries aligned to host-owned privileged adapters (especially filesystem/process actions).
3. Ensure the runtime extraction sequence is considered complete for the original “do not keep runtime in Blazor long-term” requirement.

**Assessment:** Phase 1 is functionally delivered for hardening baseline, but should be marked “closed” only after the remaining acceptance and architecture close-out checks are explicitly recorded.

---

## Phase 2 status: **Step 1/2 complete, Step 4 largely complete, Step 3 still active**

Based on:

- `phase-2-runtime-extraction-plan.md`

### Reported completed work

- `Fleet.Runtime` project bootstrap completed.
- Runtime contracts for chat completion moved from Blazor to runtime project.
- Pipeline abstractions extracted to runtime project.
- Runtime adapter interfaces introduced with host-specific adapters in Blazor for continuity.
- Controller-level security/audit tests added for `run-task` (401/403/success/exception audit paths).

### Remaining work before Phase 2 can be considered fully complete

1. Host adapters + policy hooks for filesystem/process/tool boundaries (Step 3).
2. Manual Tray-hosted Blazor Agents tab E2E handoff validation and permanent acceptance artifact (Step 4 handoff).
3. Confirm full policy/audit context propagation across all runtime tool/action execution points, not only controller entry.

**Assessment:** Phase 2 is in late-stage completion; most extraction goals are achieved, but host-adapter policy hooks need explicit completion and proof.

---

## Phase 3 objective

Deliver **use-case completeness** by implementing configurable agents/workflows, persisted execution lineage, artifact retrieval UX, and extension-origin governance tiers—while preserving Phase 1/2 security and architecture constraints.

---

## Phase 3 principles (non-negotiable)

1. **Do not regress security gates**
   - Session validation, caller identity, policy decisions, consent paths, and audit durability remain mandatory for privileged operations.

2. **Runtime stays decoupled from UI**
   - Workflow/pipeline/tool orchestration continues in runtime/domain layers, not in Blazor page/controller code.

3. **Privileged execution remains host-owned**
   - Filesystem/process/system-sensitive actions are executed through explicit host adapters with policy hooks.

4. **Domain persistence becomes first-class**
   - Agent/workflow definitions, run lineage, and artifacts are modelled explicitly in shared data storage.

---

## Proposed Phase 3 scope

## Workstream A — Agent & workflow configuration domain

### Deliverables

- Agent definition model + CRUD:
  - identity, display metadata, prompt template(s), model policy, allowed tools/resources.
- Workflow definition model + CRUD:
  - step graph/sequence, guardrails, handoff conditions, retry policy.
- Versioned configuration snapshots for reproducibility.

### Acceptance criteria

- Operators can create/update/deactivate agents and workflows from Blazor UI.
- Every run references immutable configuration version identifiers.
- Invalid or unsafe configuration updates are blocked with actionable validation errors.

---

## Workstream B — Execution lineage and traceability

### Deliverables

- Run domain schema:
  - run id, initiator identity, correlation id, agent/workflow version refs, start/end timestamps, terminal state.
- Step-level lineage:
  - per-step inputs/outputs metadata, policy decisions, consent events, failures/retries.
- Diagnostics queries/APIs/pages for tracing run history end-to-end.

### Acceptance criteria

- Any agent run can be traced from API entry to final outcome with policy context.
- Audit + lineage correlation works for both trusted interactive and extension-origin requests.
- Operators can filter runs by outcome, caller type, workflow, and date range.

---

## Workstream C — Artifact persistence and retrieval UX

### Deliverables

- Artifact catalog domain model:
  - artifact type, location, checksum/hash, retention policy, producing run/step reference.
- Artifact retrieval UX in Blazor:
  - list/search/filter artifacts, view metadata, safe download/open operations.
- Retention/cleanup jobs and policy-aware deletion path.

### Acceptance criteria

- Artifacts are discoverable by run/workflow and safely retrievable.
- Deletion and retention actions are audited and policy-gated.
- Large artifact handling avoids loading full payloads in-memory by default.

---

## Workstream D — Extension-origin permission tiers

### Deliverables

- Explicit extension-origin action tier model (e.g., low/medium/high risk classes).
- Tier-aware policy defaults + consent requirements.
- Extension-facing error/result contracts that explain deny/consent requirements without leaking sensitive details.

### Acceptance criteria

- Extension-origin requests are consistently classified and policy-evaluated.
- High-risk extension actions require interactive consent or explicit deny-by-default behavior.
- Denials and approvals include full audit context and correlation ids.

---

## Workstream E — Operational readiness and acceptance suite

### Deliverables

- End-to-end test matrix for:
  - trusted UI flow,
  - extension-origin allowed flow,
  - extension-origin denied flow,
  - consent timeout flow,
  - runtime exception/audit flow.
- Manual operator checklist for Tray-hosted deployment verification.
- "Phase 3 readiness report" template for release go/no-go.

### Acceptance criteria

- Automated tests cover critical policy and lineage scenarios.
- Manual checklist can be executed by non-authors.
- Release decision is backed by durable logs and run artifacts.

---

## Suggested implementation order

1. **Finish outstanding Phase 2 Step 3/4 items** (host adapters + policy hooks + manual handoff evidence).
2. **Implement data schemas and repositories** for agent/workflow/run/artifact domains.
3. **Ship agent/workflow CRUD UI + APIs** with versioning and validation.
4. **Integrate full run lineage capture** across runtime pipeline steps.
5. **Add artifact retrieval UX and retention controls**.
6. **Enforce extension tier policy model** + compatibility tests.
7. **Run full acceptance matrix and publish readiness report**.

---

## Definition of done for Phase 3

Phase 3 is complete when all of the following are true:

1. Configurable agents/workflows are fully managed through UI/API with versioned persistence.
2. Every run has queryable end-to-end lineage and policy/audit correlation.
3. Artifacts are persisted, discoverable, retrievable, and governed by retention + policy controls.
4. Extension-origin actions obey explicit risk-tier governance with deterministic outcomes.
5. Automated + manual acceptance checks pass for security, runtime behavior, and operator workflows.
6. Documentation in `src/docs` includes final architecture/state notes and operational runbook links.

---

## Risks and mitigations

1. **Risk: schema sprawl and migration churn**
   - Mitigation: ship schema in slices (agents/workflows first, then lineage, then artifacts) with migration tests.

2. **Risk: policy enforcement gaps in newly added endpoints**
   - Mitigation: central endpoint conventions + test fixtures that fail if policy/audit middleware is bypassed.

3. **Risk: UI coupling to runtime internals returns**
   - Mitigation: enforce runtime interfaces and adapter boundaries in code review/architecture checks.

4. **Risk: artifact growth and local storage pressure**
   - Mitigation: retention defaults, artifact size quotas, and cleanup diagnostics surfaced to operators.

---

## Recommended immediate next actions (next sprint)

1. Create a short Phase 2 close-out checklist document and mark each open Step 3/4 item as complete/incomplete.
2. Draft initial schema proposal for `AgentDefinition`, `WorkflowDefinition`, `Run`, `RunStep`, and `Artifact` entities.
3. Implement a minimal vertical slice:
   - create agent,
   - run agent once,
   - persist run + one artifact,
   - view both in diagnostics.
4. Add extension-origin tiered policy test cases before broadening endpoint surface.


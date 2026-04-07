# Fleet Phase 2 Plan: Runtime Extraction and Agent Host Alignment

_Last updated: April 7, 2026._

## Goal

Phase 2 starts the runtime extraction promised in Phase 1 by moving agent-oriented contracts out of `Fleet.Blazor` and into a dedicated runtime project so privileged host workflows can evolve without UI coupling.

## Definition of done for Step 1

Step 1 is complete when:

1. A new `src/Fleet.Runtime` project exists and is part of the solution.
2. Agent request/response contracts used by `/api/chat-completions/run-task` are sourced from `Fleet.Runtime`.
3. The chat completions runner contract is sourced from `Fleet.Runtime`, while Blazor remains the API/UI orchestration boundary.
4. Existing Phase 1 security flow is preserved for `run-task`:
   - local session token validation,
   - policy evaluation,
   - audit on deny/success/failure.
5. Integration behavior remains unchanged: start `Fleet.Tray`, load `Fleet.Blazor`, then send and receive a message on the **Agents** tab using the configured model.

## Phase 2 delivery steps

## Step 1 — Runtime project bootstrap (implemented)

- Add `Fleet.Runtime` as a new sibling project under `src/`.
- Move foundational runtime-facing contracts into `Fleet.Runtime`:
  - `AgentRequest`, `AgentRequestItem`, `AgentResponse`, `MessageType`
  - `IChatCompletionsRunner`
- Update `Fleet.Blazor` and tests to consume runtime contracts.

## Step 2 — Pipeline abstractions extraction (implemented)

- Moved pipeline contracts and context abstractions into `Fleet.Runtime` under `Fleet.Runtime.Pipeline`:
  - `IAgentPipeline`, `IAgentPipelineBuilder`, `IAgentPipelineStep`, `IPipelineContextFactory`
  - `PipelineContext`, `AgentContext`
- Introduced host-agnostic runtime adapters under `Fleet.Runtime.Adapters`:
  - `IPluginClientAdapter` (plugin client acquisition/release)
  - `IAgentOutputStore` (output persistence)
- Added Blazor host adapters to preserve existing behavior while removing direct runtime dependencies on Blazor infrastructure:
  - `McpPluginClientAdapter` wrapping `McpPluginManager`
  - `SqliteAgentOutputStore` wrapping `SqliteAgentOutputHandler`
- Added tests covering adapter-backed pipeline behavior and context factory runtime wiring.

## Step 3 — Host adapters and policy hooks

- Keep filesystem/process execution in host-owned adapters (Tray) and provide runtime integration through explicit interfaces.
- Ensure policy gate hooks are available at runtime tool/action boundaries (pre-execution and audit context propagation).

## Step 4 — Integration and acceptance hardening

- Add end-to-end coverage for:
  - `/agents` message send/receive path through Tray-hosted Blazor.
  - session token enforcement and policy outcomes.
- Capture traceability assertions (correlation ID + action outcomes) in persisted audit records.

### Step 4 progress update (April 7, 2026)

- Added controller-level security tests for `/api/chat-completions/run-task` that now cover:
  - local session rejection (`401`) when `X-Fleet-Session-Token` validation fails,
  - policy deny behavior (`403`) with audit invocation,
  - successful authorized execution with `Success` audit outcome,
  - runner exception path with `ExecutionFailure` audit outcome.
- This verifies Phase 1 hardening continuity while keeping runtime contracts in `Fleet.Runtime` and Blazor as orchestration/API only.

### Hand-off for manual Fleet.Blazor Agents tab E2E check

Run this simple end-to-end validation from the Tray host:

1. Start `Fleet.Tray`.
2. Open the Fleet.Blazor **Agents** tab.
3. Send a simple prompt (for example: `hello from phase 1 step 4`).
4. Confirm:
   - a normal response is returned in the chat UI,
   - no auth/policy errors are shown for trusted interactive flow,
   - an audit entry is written for the `chat-completions:run-task` action with a populated correlation id and final outcome.

## Notes on auth/authz continuity from Phase 1

Phase 2 refactors should preserve these non-negotiables:

- Privileged routes remain guarded by local session validation.
- Request identity continues to flow into policy evaluation.
- Unknown/untrusted requesters remain deny-by-default.
- Audit trails remain durable and complete for privileged actions.

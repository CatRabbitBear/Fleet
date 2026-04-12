# Phase 3 Testing Checklist (Initial)

This checklist is intended to validate the first Phase 3 vertical slice: agent configuration, run lineage, artifact recording, and extension-tier governance behavior.

## Integration tests to add/run

1. **Agent CRUD + version initialization**
   - Create an agent via `POST /api/agents`.
   - Assert agent is returned from `GET /api/agents` with a non-empty `activeVersionId`.
2. **Run lineage persistence**
   - Execute `POST /api/chat-completions/run-task` with a valid `agentId`.
   - Assert run is visible in `GET /api/agents/runs?agentId=...` with status `Success`.
3. **Artifact association**
   - Execute a run that emits a file path.
   - Assert `GET /api/agents/runs/{runId}/artifacts` returns at least one record.
4. **Policy deny lineage**
   - Force policy deny in test fixture.
   - Assert request returns `403` and run row status is `Denied`.

## End-to-end tests (Tray-hosted)

1. Open `/agents` and create a new agent from UI.
2. Select the agent and run a prompt.
3. Confirm run appears in Recent Runs list.
4. Confirm corresponding audit records are written with matching correlation id.

## Manual spot checks

1. **Extension tier behavior:**
   - For browser extension caller identity + `High` tier agent, ensure API returns `403` with correlation id.
2. **Invalid configuration safety:**
   - Try creating agent with empty name/prompt; ensure API returns validation error.
3. **Lineage robustness:**
   - Force chat runner exception; ensure run status transitions to `ExecutionFailure`.

# Fleet (Tray-hosted Blazor prototype)

Fleet is a proof-of-concept app that hosts a Blazor Server app from a Windows tray process (`Fleet.Tray`).

## What was modernized

- Updated SQLite package usage from a preview package to stable `Microsoft.Data.Sqlite` (`9.0.10`).
- Removed obsolete `Microsoft.AspNetCore` `2.3.0` reference from `Fleet.Tray` (not needed on modern SDK-style `net9.0` app).
- Improved credential handling flow:
  - Credentials are still stored in Windows Credential Manager, but are no longer echoed back into UI fields for secrets.
  - API Key can be left blank in the setup dialog to keep the existing stored key.
  - Added a dashboard action to reopen Azure credential configuration after startup.

## Entry point and startup flow

- Main entry point: `src/Fleet.Tray/App.xaml.cs`.
- On startup, Fleet:
  1. Loads required credentials from Windows Credential Manager.
  2. Prompts for missing values in `BulkCredentialsWindow`.
  3. Starts Blazor host (`Fleet.Blazor`) with those values injected into `IConfiguration`.
  4. Creates tray icon and menu.

## Azure setup (required to connect the app)

This app expects an **Azure AI Foundry project endpoint + model + key** for the Semantic Kernel Azure AI Inference connector.

### 1) Create or open an Azure AI Foundry project

1. Open Azure AI Foundry.
2. Create a new project (or reuse an existing one).
3. Ensure the project has access to a chat-capable model deployment.

### 2) Deploy a model for chat

1. In the project, deploy a model you want Fleet to use.
2. Record the model/deployment identifier you will pass as `FLEET_AZURE_MODEL_ID`.

### 3) Generate an API key and endpoint

1. In the project/API settings, copy:
   - Endpoint URL (`FLEET_AZURE_ENDPOINT`)
   - API key (`FLEET_AZURE_MODEL_KEY`)
2. Confirm the endpoint is an absolute URI.

### 4) Configure CORS exemption value

- `FLEET_CORS_EXCEMPTION` is used by the Blazor host CORS policy.
- For local usage, this should be the exact browser-extension/web origin that must call Fleet.

### 5) Run Fleet and enter credentials

1. Launch `Fleet.Tray`.
2. Fill the Azure setup dialog:
   - Azure Endpoint
   - Azure Model ID
   - API Key
   - Optional CORS exemption
3. Click **OK**. Credentials are persisted in Windows Credential Manager.

## Notes on credential handling

Current behavior is acceptable for a prototype, but for production hardening you should consider:

- Supporting Entra ID (managed identity / OAuth token flow) instead of static key auth.
- Adding key rotation and validation checks before host boot.
- Explicitly separating per-user vs shared-machine secret policies by environment.

## Operational gotchas

- Exit Fleet via tray icon **Exit** to ensure full cleanup.
- The dashboard allows editing credentials, but runtime host config is loaded at startup; restart Fleet after changing credentials.

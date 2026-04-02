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

## Azure OpenAI setup (required to connect the app)

This app expects an **Azure OpenAI endpoint + deployment name + key** using the Semantic Kernel OpenAI connector (`AddAzureOpenAIChatCompletion`).

### 1) Create or open an Azure OpenAI resource

1. Open Azure portal and create/select your Azure OpenAI resource.
2. Ensure your resource has a chat-capable model deployed.

### 2) Deploy a model for chat

1. In the project, deploy a model you want Fleet to use.
2. Record the deployment name you will pass as `FLEET_AZURE_MODEL_ID` (or `FLEET_AZURE_OPENAI_DEPLOYMENT`).

### 3) Gather endpoint and API key

1. In your Azure OpenAI resource keys/settings, copy:
   - Endpoint URL (`FLEET_AZURE_ENDPOINT` or `FLEET_AZURE_OPENAI_ENDPOINT`)
   - API key (`FLEET_AZURE_MODEL_KEY` or `FLEET_AZURE_OPENAI_API_KEY`)
2. Confirm the endpoint is an absolute URI.

### 4) Configure CORS exemption value

- `FLEET_CORS_EXCEMPTION` is used by the Blazor host CORS policy.
- For local usage, this should be the exact browser-extension/web origin that must call Fleet.

### 5) Run Fleet and enter credentials

1. Launch `Fleet.Tray`.
2. Fill the Azure setup dialog:
   - Azure OpenAI Endpoint
   - Deployment Name
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

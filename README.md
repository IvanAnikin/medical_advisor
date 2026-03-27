# Medical Advisor -- Diabetologicky poradce

Czech-language AI chatbot that provides diabetes education and guidance to patients, strictly grounded in 4 verified medical documents. Built with ASP.NET 8 Blazor Server, Microsoft Semantic Kernel, and MudBlazor. Deployed on Azure App Service.

**Production URL:** https://medical-advisor-cz.azurewebsites.net

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Knowledge Base](#knowledge-base)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Running Locally](#running-locally)
- [Testing](#testing)
- [Deployment](#deployment)
- [Azure Resources](#azure-resources)
- [How It Works](#how-it-works)
- [AI Grounding & Safety](#ai-grounding--safety)
- [Theming](#theming)
- [Implementation Plan](#implementation-plan)
- [Cost Estimate](#cost-estimate)
- [Future Improvements](#future-improvements)
- [Troubleshooting](#troubleshooting)

---

## Overview

Medical Advisor is a **single-page chatbot web app** where an AI assistant helps Czech-speaking diabetes patients understand their condition. The AI is powered by Azure OpenAI (`gpt-4.1-mini`) and is **strictly grounded** in 4 official Czech medical documents -- it cannot hallucinate or invent medical advice.

The bot drives the conversation by asking clarifying questions (type of diabetes, current treatment, specific concerns) and provides targeted, short, layperson-friendly answers sourced exclusively from the documents. For anything outside the documents' scope, the bot honestly refuses and refers the patient to their doctor.

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Grounding approach | Context stuffing (full docs in system prompt) | Simple, reliable, no vector DB needed -- 4 docs fit in ~7% of gpt-4.1-mini's 1M token context |
| Framework | ASP.NET 8 Blazor Server | Real-time streaming via SignalR, single deployable, Azure-native |
| AI orchestration | Microsoft Semantic Kernel | .NET-native OpenAI integration, streaming chat, prompt management |
| UI components | MudBlazor | Material Design for Blazor, rich component library, MIT licensed |
| Authentication | None (anonymous) | Low friction for patients -- no login required |
| Session persistence | Browser localStorage | Up to 5 sessions saved client-side, no server DB needed |
| Language | Czech only | Target audience is Czech-speaking diabetes patients |

---

## Features

- **Conversational AI** -- Natural Czech-language dialogue guided by the bot's questions
- **Streaming responses** -- Token-by-token display via SignalR for responsive feel
- **Document grounding** -- All answers strictly sourced from 4 verified medical documents
- **4 diabetes topics** -- Insulin therapy, CGM monitoring, foot care, physical activity
- **Dual theme system** -- Clinical (blue) / Friendly (teal) + Light / Dark mode (4 combinations), switchable via header toggles with cookie persistence
- **Collapsible header** -- Two-row custom header (title + controls) with toggle arrow to collapse/expand, saving screen space
- **Session history** -- Up to 5 conversation sessions saved in browser localStorage, restored via a floating popup with animated cards and Czech pluralization
- **Page reload on title click** -- Clicking the app title saves the current session and reloads the page to start a fresh conversation
- **Markdown rendering** -- Bold, italic, and line breaks in chat messages
- **Error handling** -- Errors displayed inline in chat; graceful degradation for missing documents
- **Mobile responsive** -- Sticky input area, responsive header, proper viewport handling on mobile devices
- **Keyboard shortcuts** -- Enter to send, Shift+Enter for newline
- **Minimal data footprint** -- Anonymous, no user accounts; theme preference in cookies, session history in localStorage only

---

## Architecture

```
+--------------------------------------------------------------+
|                    Azure App Service (B1)                      |
|                                                                |
|  +----------------------------------------------------------+ |
|  |             ASP.NET 8 Blazor Server App                   | |
|  |                                                           | |
|  |  +------------+  +----------------+  +------------------+ | |
|  |  |  Chat UI   |  |  Semantic      |  |  Document        | | |
|  |  |  (Blazor   |->|  Kernel        |->|  Knowledge Base  | | |
|  |  |  Server +  |  |  (Chat         |  |  (4 .txt files,  | | |
|  |  |  MudBlazor)|  |  Completion)   |  |  261K chars)     | | |
|  |  +------------+  +-------+--------+  +------------------+ | |
|  |                          |                                 | |
|  +--------------------------+--------------------------------+ |
|                             |                                  |
+-----------------------------+----------------------------------+
                              | HTTPS API calls
                              v
                 +---------------------------+
                 |   Azure OpenAI Service    |
                 |   (anote-openai,          |
                 |    West Europe,           |
                 |    gpt-4-1-mini)          |
                 +---------------------------+
```

### Data Flow

1. **Startup**: `DocumentService` (singleton) loads 4 `.txt` files from `docs/` and concatenates them (261,539 characters total)
2. **Per session**: `MedicalAdvisorService` (scoped) builds the system prompt by appending all documents to the prompt template from `Prompts/SystemPrompt.txt`
3. **Per message**: User input is added to the Semantic Kernel `ChatHistory`, a fresh chat history including the system prompt is built, and `GetStreamingChatMessageContentsAsync` is called
4. **Streaming**: Response chunks flow back via `IAsyncEnumerable<string>` -> Blazor `StateHasChanged()` -> SignalR -> browser DOM update (token by token)

### Service Lifetimes (Dependency Injection)

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `DocumentService` | **Singleton** | Loads documents once at startup, shared across all sessions |
| `MedicalAdvisorService` | **Scoped** | Per-circuit AI chat orchestration with Semantic Kernel |
| `ConversationState` | **Scoped** | Per-circuit chat messages (UI) + ChatHistory (LLM context) |
| `ThemeService` | **Scoped** | Per-circuit dual theme state (Clinical/Friendly + Light/Dark) |
| `IChatCompletionService` | Registered by SK | Azure OpenAI chat completion via Semantic Kernel |

### Dual History Pattern

`ConversationState` maintains two separate collections:

- **`Messages`** (List\<ChatMessage\>) -- For UI display. Includes the welcome message. Managed by `Home.razor`.
- **`ChatHistory`** (SK ChatHistory) -- For the LLM context. Includes the welcome message as an assistant message so the LLM knows it already greeted the user (prevents double greeting). Managed by `MedicalAdvisorService`.

This separation keeps UI concerns separate from AI concerns while ensuring the LLM has full conversation context.

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 8.0 |
| Web framework | ASP.NET Blazor Server | 8.0 |
| AI orchestration | Microsoft Semantic Kernel | 1.74.0 |
| UI components | MudBlazor | 9.2.0 |
| LLM | Azure OpenAI gpt-4.1-mini | 2025-04-14 |
| Hosting | Azure App Service | B1 (Basic, Linux) |
| Testing | xUnit + Moq + coverlet | 2.5.3 / 4.20.72 / 6.0.0 |

---

## Knowledge Base

The AI's knowledge comes exclusively from 4 Czech medical documents, loaded as plain text files at startup:

| Document | File | Topic | Size |
|----------|------|-------|------|
| Zaciname s inzulinem | `zaciname_s_inzulinem.txt` | Insulin therapy for beginners -- types, injection, dosing, hypo/hyperglycemia | 72,098 chars, 1,774 lines |
| CGM -- Kontinualni monitorace glukozy | `cgm_kontinualni_monitorace.txt` | Continuous glucose monitoring -- systems, interpretation, TIR | 53,067 chars, 1,222 lines |
| Pece o nohy pri diabetu | `pece_o_nohy.txt` | Diabetic foot care -- prevention, risks, hygiene, shoe selection | 24,931 chars, 497 lines |
| Doporuceni -- Fyzicka aktivita | `doporuceni_fyzicka_aktivita.txt` | Physical activity guidelines -- exercise types, blood sugar management during sport | 111,248 chars, 2,291 lines |

**Total: 261,539 characters (~75K tokens, ~7% of gpt-4.1-mini's 1M context window)**

Documents are stored in `src/MedicalAdvisor.Web/docs/` and copied to the publish output via `<Content>` items in the `.csproj`.

### Document Sources

- **Insulin & CGM guides**: Authored by doc. MUDr. Jan Broz, Ph.D. and doc. MUDr. Jana Urbanova, Ph.D. Sponsored by Sanofi.
- **Foot care guide**: Authored by MUDr. Jan Broz et al. Sponsored by Sanofi-Aventis.
- **Physical activity guidelines**: Official clinical standard from the Czech Diabetes Society (CDS), published in DMEV journal, vol. 25, 2022.

---

## Project Structure

```
medical_advisor/
|-- MedicalAdvisor.sln                    # Solution file (2 projects)
|-- README.md                             # This file
|-- TECH_SPEC.md                          # Detailed technical specification (662 lines)
|-- IMPLEMENTATION_STATUS.md              # Implementation tracking & deployment log
|-- .gitignore                            # .NET + macOS + secrets gitignore
|-- conversation_demo.txt                 # Demo conversation (plain text)
|-- conversation_demo.html                # Demo conversation (printable HTML)
|-- docs/                                 # Reference copies of medical documents
|   |-- zaciname_s_inzulinem.txt
|   |-- cgm_kontinualni_monitorace.txt
|   |-- pece_o_nohy.txt
|   +-- doporuceni_fyzicka_aktivita.txt
|
|-- src/MedicalAdvisor.Web/               # Main Blazor Server application
|   |-- MedicalAdvisor.Web.csproj         # Project file (.NET 8, SK 1.74, MudBlazor 9.2)
|   |-- Program.cs                        # Entry point, DI, middleware pipeline
|   |-- appsettings.json                  # Config (Azure OpenAI endpoint/deployment)
|   |-- appsettings.Development.json      # Dev logging overrides
|   |-- Properties/
|   |   +-- launchSettings.json           # Local dev ports (5113, 7128)
|   |-- Models/
|   |   |-- ChatMessage.cs                # ChatRole enum + ChatMessage class
|   |   |-- AppTheme.cs                   # AppTheme enum (Clinical, Friendly)
|   |   +-- ConversationSession.cs        # Serializable session model for localStorage
|   |-- Services/
|   |   |-- DocumentService.cs            # Loads 4 docs at startup (singleton)
|   |   |-- MedicalAdvisorService.cs      # SK streaming chat orchestration (scoped)
|   |   |-- ConversationState.cs          # Per-circuit messages + ChatHistory (scoped)
|   |   +-- ThemeService.cs               # Dual theme state + 4 MudTheme definitions (scoped)
|   |-- Prompts/
|   |   +-- SystemPrompt.txt              # Czech AI persona + grounding rules
|   |-- Components/
|   |   |-- App.razor                     # Root HTML document (lang="cs")
|   |   |-- _Imports.razor                # Global @using directives
|   |   |-- Routes.razor                  # Router with MainLayout default
|   |   |-- Layout/
|   |   |   |-- MainLayout.razor          # Custom collapsible header + theme root + session history
|   |   |   +-- MainLayout.razor.css      # Scoped CSS (empty, MudBlazor handles it)
|   |   |-- Pages/
|   |   |   |-- Home.razor                # Main chat page (streaming + auto-save)
|   |   |   +-- Error.razor               # Standard error page
|   |   +-- Shared/
|   |       |-- ChatMessageBubble.razor   # Message bubble (Markdown + streaming cursor)
|   |       |-- SessionHistory.razor      # Floating popup with localStorage session management
|   |       +-- ThemeSwitcher.razor        # Dual toggle: Clinical/Friendly + Light/Dark
|   |-- wwwroot/
|   |   |-- app.css                       # 4 theme combos, header, session popup, mobile responsive CSS
|   |   |-- favicon.png                   # App favicon
|   |   +-- bootstrap/                    # Bootstrap CSS (template artifact, unused)
|   +-- docs/                             # Medical documents (published with app)
|       |-- zaciname_s_inzulinem.txt
|       |-- cgm_kontinualni_monitorace.txt
|       |-- pece_o_nohy.txt
|       +-- doporuceni_fyzicka_aktivita.txt
|
+-- tests/MedicalAdvisor.Tests/           # Unit test project
    |-- MedicalAdvisor.Tests.csproj       # Test project (xUnit, Moq, coverlet)
    +-- Services/
        |-- DocumentServiceTests.cs       # 6 tests: loading, missing files, content
        |-- ConversationStateTests.cs     # 14 tests: state management, reset, history
        +-- ThemeServiceTests.cs          # 12 tests: themes, switching, events
```

**Total: ~45 files, 57 unit tests**

---

## Prerequisites

- **.NET 8 SDK** (8.0.x)
- **Azure CLI** (for deployment only)
- **Azure subscription** with an Azure OpenAI resource that has a `gpt-4.1-mini` (or compatible) deployment

### .NET SDK Installation (if not already installed)

```bash
# Install via official script
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0

# Add to PATH (add to your shell profile for persistence)
export PATH="$HOME/.dotnet:$PATH"

# Verify
dotnet --version
```

---

## Installation

```bash
# Clone the repository
git clone <repository-url>
cd medical_advisor

# Restore NuGet packages
export PATH="$HOME/.dotnet:$PATH"
dotnet restore
```

---

## Configuration

### Azure OpenAI Settings

The app needs 3 configuration values for Azure OpenAI:

| Key | Description | Where to set |
|-----|-------------|-------------|
| `AzureOpenAI:Endpoint` | Azure OpenAI resource endpoint | `appsettings.json` (already set) |
| `AzureOpenAI:DeploymentName` | Model deployment name | `appsettings.json` (already set) |
| `AzureOpenAI:ApiKey` | API key (secret) | User Secrets (local) or App Service config (prod) |

Current values in `appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://anote-openai.openai.azure.com/",
    "DeploymentName": "gpt-4-1-mini"
  }
}
```

### Setting the API Key (Local Development)

The API key is stored in .NET User Secrets, never in source code:

```bash
export PATH="$HOME/.dotnet:$PATH"
cd src/MedicalAdvisor.Web

# Initialize user secrets (already done, skip if UserSecretsId exists in .csproj)
dotnet user-secrets init

# Set the API key
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-azure-openai-api-key>"
```

User secrets are stored at `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` and are automatically loaded in Development environment.

### AI Parameters

Configured in `MedicalAdvisorService.cs`:

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Temperature | 0.3 | Low creativity -- medical accuracy is critical |
| MaxTokens | 1024 | Sufficient for concise patient-facing answers |
| TopP | 0.9 | Slightly reduced nucleus sampling for consistency |

---

## Running Locally

```bash
export PATH="$HOME/.dotnet:$PATH"

# Build and run
dotnet run --project src/MedicalAdvisor.Web
```

Open **http://localhost:5113** in your browser.

On startup, you will see log output confirming all 4 documents were loaded:

```
info: MedicalAdvisor.Web.Services.DocumentService[0]
      Loaded document 'Zaciname s inzulinem' (zaciname_s_inzulinem.txt): 72098 characters
info: MedicalAdvisor.Web.Services.DocumentService[0]
      Loaded document 'CGM -- Kontinualni monitorace glukozy' (cgm_kontinualni_monitorace.txt): 53067 characters
info: MedicalAdvisor.Web.Services.DocumentService[0]
      Loaded document 'Pece o nohy pri diabetu' (pece_o_nohy.txt): 24931 characters
info: MedicalAdvisor.Web.Services.DocumentService[0]
      Loaded document 'Doporuceni -- Fyzicka aktivita' (doporuceni_fyzicka_aktivita.txt): 111248 characters
info: MedicalAdvisor.Web.Services.DocumentService[0]
      DocumentService initialized -- 4 document(s) loaded, total 261539 characters
```

---

## Testing

```bash
export PATH="$HOME/.dotnet:$PATH"

# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

### Test Coverage

| Test Class | Tests | What It Covers |
|------------|-------|---------------|
| `DocumentServiceTests` | 6 | Document loading, missing files, content verification, delimiters |
| `ConversationStateTests` | 14 | Initial state, message CRUD, ChatHistory sync, reset, session export/import |
| `MedicalAdvisorServiceTests` | 13 | AI service orchestration, streaming, error handling |
| `ThemeServiceTests` | 12 | Default theme, switching, events, MudTheme color values |
| `ChatMessageTests` | 12 | Message creation, properties, role handling |

All tests use **xUnit** as the test framework and **Moq** for mocking `IWebHostEnvironment` and `ILogger`. `DocumentServiceTests` creates temporary directories with sample files and cleans up via `IDisposable`.

---

## Deployment

### Overview

The app is deployed to **Azure App Service** (Linux, B1 Basic tier) via ZIP deploy. The deployment process involves:

1. Building a Release publish bundle with `dotnet publish`
2. Zipping the output
3. Deploying the ZIP via Azure CLI (`az webapp deploy`)

The published bundle includes the compiled app, the 4 medical documents (`docs/`), the system prompt (`Prompts/`), and all static assets (`wwwroot/`).

### Azure Resources Required

| Resource | Name | Details |
|----------|------|---------|
| Resource Group | `medical-advisor-rg` | West Europe |
| App Service Plan | `medical-advisor-plan` | B1 Basic, Linux (~$13/month) |
| Web App | `medical-advisor-cz` | .NET 8 runtime, WebSockets enabled |
| Azure OpenAI | `anote-openai` (shared) | West Europe, `gpt-4-1-mini` deployment |

### Prerequisites for Deployment

1. **.NET 8 SDK** -- for `dotnet publish`
2. **Azure CLI** -- for `az webapp deploy` (install via `pip install azure-cli` in a Python venv, or via Homebrew: `brew install azure-cli`)
3. **Azure login** -- run `az login --use-device-code` before deploying
4. **zip** utility -- available by default on macOS and most Linux distros

### First-Time Azure Setup

Only needed once to create the infrastructure:

```bash
# 1. Install Azure CLI (choose one method)
# Option A: Python venv (recommended for isolation)
python3 -m venv /tmp/az_venv
source /tmp/az_venv/bin/activate
pip install azure-cli

# Option B: Homebrew (macOS)
brew install azure-cli

# 2. Login to Azure
az login --use-device-code

# 3. Create resource group
az group create --name medical-advisor-rg --location westeurope

# 4. Create App Service plan (B1 Basic, Linux)
#    NOTE: F1 Free tier is NOT sufficient -- see "Important Notes" below
az appservice plan create \
  --name medical-advisor-plan \
  --resource-group medical-advisor-rg \
  --sku B1 --is-linux --location westeurope

# 5. Create the web app
az webapp create \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --plan medical-advisor-plan \
  --runtime "DOTNETCORE:8.0"

# 6. Configure app settings (API key + Azure OpenAI config)
az webapp config appsettings set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --settings \
    AzureOpenAI__ApiKey="<your-azure-openai-api-key>" \
    AzureOpenAI__Endpoint="https://anote-openai.openai.azure.com/" \
    AzureOpenAI__DeploymentName="gpt-4-1-mini"

# 7. Enable WebSockets (REQUIRED for Blazor Server / SignalR)
az webapp config set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --web-sockets-enabled true

# 8. Set startup command
az webapp config set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --startup-file "dotnet MedicalAdvisor.Web.dll"

# 9. Increase startup timeout (documents take time to load on cold start)
az webapp config appsettings set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --settings WEBSITES_CONTAINER_START_TIME_LIMIT=600
```

### Deploy / Redeploy (Every Update)

Use this workflow every time you deploy a new version:

```bash
# Ensure .NET SDK and Azure CLI are on PATH
export PATH="$HOME/.dotnet:$PATH"
# If using Python venv for Azure CLI:
# source /tmp/az_venv/bin/activate

# Step 1: Run tests to make sure nothing is broken
dotnet test

# Step 2: Publish a Release build
dotnet publish src/MedicalAdvisor.Web/MedicalAdvisor.Web.csproj \
  -c Release -o ./publish

# Step 3: Create ZIP from publish output
cd publish && zip -r ../deploy.zip . && cd ..

# Step 4: Deploy to Azure App Service
az webapp deploy \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --src-path deploy.zip --type zip

# Step 5: Clean up local artifacts
rm -rf publish deploy.zip

# Step 6: Verify the deployment is running
az webapp show \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --query "state" -o tsv
# Expected output: "Running"
```

After deployment, wait ~60-90 seconds for the cold start, then verify at:
**https://medical-advisor-cz.azurewebsites.net**

### What Gets Published

The `dotnet publish` output includes:

| Folder/File | Source | Purpose |
|-------------|--------|---------|
| `MedicalAdvisor.Web.dll` | Compiled app | Main application binary |
| `docs/*.txt` | `src/.../docs/` | 4 medical documents (261K chars) loaded at startup |
| `Prompts/SystemPrompt.txt` | `src/.../Prompts/` | AI persona + grounding rules template |
| `wwwroot/` | `src/.../wwwroot/` | Static assets (CSS, bootstrap) |
| `wwwroot/_content/MudBlazor/` | NuGet | MudBlazor CSS + JS (auto-included) |
| `appsettings.json` | Config | Azure OpenAI endpoint + deployment name |
| `web.config` | Auto-generated | IIS/Kestrel hosting configuration |
| `runtimes/` | .NET SDK | Platform-specific runtime binaries |

The `docs/` and `Prompts/` folders are included via explicit `<Content>` items in the `.csproj`:

```xml
<ItemGroup>
  <Content Include="docs\**\*" CopyToOutputDirectory="PreserveNewest" />
  <Content Include="Prompts\**\*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### Important Notes

| Topic | Details |
|-------|---------|
| **Minimum tier** | B1 Basic (~$13/month). F1 Free tier's 60 min/day CPU quota gets exhausted during deployment + cold starts with 261K chars of document loading. |
| **Cold start time** | 60-90 seconds on B1 due to Linux container setup + certificate updates + loading 4 documents into memory. `WEBSITES_CONTAINER_START_TIME_LIMIT=600` prevents premature timeout. |
| **WebSockets** | Must be enabled on the App Service. Blazor Server uses SignalR (WebSockets) for all UI updates. Without it, the app will not function. |
| **API key security** | The Azure OpenAI API key is set as an App Service Configuration setting (`AzureOpenAI__ApiKey`), never committed to source code. Locally, use .NET User Secrets. |
| **Git postBuffer** | If `git push` fails with HTTP 400 on large commits, increase the buffer: `git config http.postBuffer 524288000` |
| **No CI/CD** | Currently deployed manually via ZIP deploy. A GitHub Actions pipeline is a planned future improvement. |

### Checking Deployment Status

```bash
# Check if the app is running
az webapp show --name medical-advisor-cz \
  --resource-group medical-advisor-rg --query "state" -o tsv

# Stream live logs (useful for debugging startup issues)
az webapp log tail --name medical-advisor-cz \
  --resource-group medical-advisor-rg

# View recent deployment logs
az webapp deployment list-publishing-credentials \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg
```

---

## Azure Resources

### Current Deployment

| Property | Value |
|----------|-------|
| Production URL | https://medical-advisor-cz.azurewebsites.net |
| Resource Group | `medical-advisor-rg` (West Europe) |
| App Service Plan | `medical-advisor-plan` (B1 Basic, Linux) |
| Web App | `medical-advisor-cz` |
| Azure OpenAI Resource | `anote-openai` (West Europe) |
| Model Deployment | `gpt-4-1-mini` (gpt-4.1-mini, version 2025-04-14, Standard SKU) |
| Subscription | Visual Studio Ultimate with MSDN (`8a3849cc-c762-4a9c-8874-6487046bc245`) |

---

## How It Works

### Request Lifecycle

1. **User types a message** in the chat input and presses Enter (or clicks Send)
2. **`Home.razor`** adds the user message to `ConversationState.Messages` (for UI display) and creates a placeholder streaming `ChatMessage` with `IsStreaming=true`
3. **`MedicalAdvisorService.StreamResponseAsync`** is called:
   - Adds user message to `ConversationState.ChatHistory` (for LLM context)
   - Builds a fresh `ChatHistory`: system prompt (template + 261K chars of documents) + full conversation history
   - Calls `IChatCompletionService.GetStreamingChatMessageContentsAsync` with Temperature=0.3, MaxTokens=1024, TopP=0.9
   - Yields each response chunk as it arrives
4. **`Home.razor`** receives each chunk via `await foreach`, appends it to the streaming message's `Content`, calls `StateHasChanged()` and `ScrollToBottom()`
5. **SignalR** pushes the DOM diff to the browser in real-time
6. When streaming completes, the full response is added to `ChatHistory` and `IsStreaming` is set to false

### System Prompt Structure

```
[SystemPrompt.txt -- persona, role, grounding rules (29 lines)]
[Blank line]
=== DOKUMENT: Zaciname s inzulinem ===
[Full text of insulin therapy guide]

=== DOKUMENT: CGM -- Kontinualni monitorace glukozy ===
[Full text of CGM guide]

=== DOKUMENT: Pece o nohy pri diabetu ===
[Full text of foot care guide]

=== DOKUMENT: Doporuceni -- Fyzicka aktivita ===
[Full text of physical activity guidelines]
```

Total system prompt size: ~261K characters (~75K tokens).

### Component Hierarchy

```
App.razor (root HTML document, @rendermode InteractiveServer)
  +-- Routes.razor (router)
       +-- MainLayout.razor (custom header + theme root + session history)
            |-- MudThemeProvider (bound to ThemeService.CurrentMudTheme)
            |-- Custom HTML header (two-row, collapsible)
            |   |-- Row 1: Title (clickable, reloads page) + toggle arrow
            |   +-- Row 2: Session History button + ThemeSwitcher.razor (dual toggles)
            |-- SessionHistory.razor (floating popup, localStorage persistence)
            +-- Home.razor (chat page, @page "/")
                |-- Welcome cards (quick-reply topic buttons)
                |-- ChatMessageBubble.razor (foreach message)
                +-- Sticky input area (pill-shaped, Enter to send)
```

---

## AI Grounding & Safety

The system prompt enforces strict grounding rules (defined in `Prompts/SystemPrompt.txt`):

1. **Document-only answers** -- The AI must NEVER invent information not present in the 4 documents
2. **Honest refusal** -- If asked about something not covered, it says: "Na toto bohuzel nemam v dostupnych materialech odpoved. Doporucuji se obratit na vaseho osetrujiciho lekare."
3. **No diagnosis** -- The AI never makes medical diagnoses
4. **No medication changes** -- The AI never adjusts dosing; always refers to the patient's doctor
5. **Emergency referral** -- For severe symptoms (severe hypoglycemia, ketoacidosis, acute foot defects), the AI recommends immediate medical attention
6. **Input sanitization** -- `ChatMessageBubble.razor` HTML-encodes all content before rendering Markdown, preventing XSS
7. **API key protection** -- Never in source code; User Secrets (local) or App Service Configuration (production)

---

## Theming

The app supports a **dual theme system** with 4 combinations: Clinical/Friendly x Light/Dark. Both preferences are persisted in browser cookies (1-year expiry) so they survive page reloads.

### Theme Dimensions

| Dimension | Option A | Option B |
|-----------|----------|----------|
| Style | **Clinical** (blue, professional) | **Friendly** (teal, warm) |
| Mode | **Light** (default) | **Dark** |

### Clinical Theme

| Property | Light | Dark |
|----------|-------|------|
| Primary | `#1565C0` (blue) | `#1565C0` |
| Background | `#FAFAFA` | `#1a1a2e` |
| Surface | white | `#16213e` |

### Friendly Theme

| Property | Light | Dark |
|----------|-------|------|
| Primary | `#00897B` (teal) | `#00897B` |
| Background | `#FFF8F0` (warm white) | `#1a1a2e` |
| Surface | white | `#16213e` |

Theme state is managed by `ThemeService` (scoped per circuit) with two independent toggles in the header. CSS custom properties on `.theme-root` are used for non-MudBlazor elements (header, chat bubbles, session popup). Changes propagate via the `OnThemeChanged` event.

---

## Implementation Plan

The project was built in 7 phases:

| Phase | Description | Status |
|-------|-------------|--------|
| 1. Project Scaffolding | Solution, projects, NuGet packages, config | Done |
| 2. Document Service & AI Service | DocumentService, MedicalAdvisorService, ConversationState, SystemPrompt | Done |
| 3. Chat UI | Home.razor, ChatMessageBubble, streaming, auto-scroll, keyboard shortcuts | Done |
| 4. Theming | Dual theme system (Clinical/Friendly + Light/Dark), cookie persistence, CSS custom properties | Done |
| 5. Testing | 57 unit tests (DocumentService, ConversationState, ThemeService, MedicalAdvisorService, ChatMessage) | Done |
| 6. UI Enhancements | Collapsible header, session history (localStorage), mobile responsive, title-click reload | Done |
| 7. Azure Deployment | Resource group, App Service, config, ZIP deploy, verify production URL | Done |

### Key Implementation Decisions Made During Development

1. **MudBlazor 9.x migration**: `AutoGrow` attribute was renamed to `Sizing="InputSizing.Auto"` in MudBlazor 9.x (breaking change from 7.x/8.x)
2. **Dual history pattern**: UI `Messages` and LLM `ChatHistory` are managed separately to prevent double-counting the welcome message and to separate UI from AI concerns
3. **F1 to B1 upgrade**: The Free tier's 60 min/day CPU quota was insufficient; upgraded to B1 Basic (~$13/month)
4. **Publish content fix**: `docs/` and `Prompts/` folders needed explicit `<Content>` items in `.csproj` to be included in the publish output (they are not in `wwwroot/`)
5. **Startup command**: Linux App Service required explicit `dotnet MedicalAdvisor.Web.dll` startup command for ZIP deploy

---

## Cost Estimate

| Resource | Monthly Cost |
|----------|-------------|
| App Service B1 (Basic) | ~$13 |
| Azure OpenAI input (~75K tokens system prompt per API call) | ~$0.03 per API call |
| Azure OpenAI output (~500 tokens avg per response) | ~$0.0008 per response |
| **100 conversations/day x 10 messages each** | **~$18-23/month total** |

Context stuffing means every API call includes the full ~75K token system prompt. At gpt-4.1-mini pricing ($0.40/1M input tokens), that is ~$0.03 per call just for the documents. For heavier usage, consider RAG (retrieval-augmented generation) to reduce input tokens.

---

## Future Improvements

- **RAG with vector search** -- Replace context stuffing with Azure AI Search for better scalability and lower per-call cost
- **User authentication** -- Azure AD B2C or anonymous session tracking
- **Additional documents** -- Expand the knowledge base with more diabetes education materials
- **Feedback mechanism** -- Allow patients to rate responses for quality monitoring
- **CI/CD pipeline** -- GitHub Actions for automated build, test, and deploy on push to main
- **Application Insights** -- Azure Monitor for telemetry, error tracking, and usage analytics
- **Accessibility (a11y)** -- ARIA labels, screen reader support, high contrast theme
- **PWA support** -- Installable mobile experience with offline capability
- **Multi-language support** -- Slovak, English translations
- **Server-side session persistence** -- Cosmos DB or SQLite for cross-device session sync

---

## Troubleshooting

### "AzureOpenAI:ApiKey is not configured"

The API key is missing. For local development:

```bash
cd src/MedicalAdvisor.Web
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
```

For production, set `AzureOpenAI__ApiKey` in Azure App Service Configuration.

### App returns 503 after deployment

The container is still starting. B1 cold starts take 60-90 seconds. Wait and retry. If persistent, check logs:

```bash
source /tmp/az_venv/bin/activate
az webapp log tail --name medical-advisor-cz --resource-group medical-advisor-rg
```

### "DirectoryNotFoundException: docs/..."

The `docs/` folder is missing from the publish output. Ensure the `.csproj` includes:

```xml
<ItemGroup>
  <Content Include="docs\**\*" CopyToOutputDirectory="PreserveNewest" />
  <Content Include="Prompts\**\*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Then re-publish and redeploy.

### App returns 403 "This web app is stopped"

The F1 (Free) tier CPU quota was exceeded. Either wait for UTC midnight (quota reset) or upgrade to B1:

```bash
az appservice plan update --name medical-advisor-plan \
  --resource-group medical-advisor-rg --sku B1
```

### Blazor connection lost / SignalR errors

Ensure WebSockets are enabled on the App Service:

```bash
az webapp config set --name medical-advisor-cz \
  --resource-group medical-advisor-rg --web-sockets-enabled true
```

### dotnet command not found

Add the .NET SDK to your PATH:

```bash
export PATH="$HOME/.dotnet:$PATH"
```

Add this to your shell profile (`~/.zshrc` or `~/.bashrc`) for persistence.

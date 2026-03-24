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
| Database | None (stateless) | No persistence needed; sessions are ephemeral in server memory |
| Language | Czech only | Target audience is Czech-speaking diabetes patients |

---

## Features

- **Conversational AI** -- Natural Czech-language dialogue guided by the bot's questions
- **Streaming responses** -- Token-by-token display via SignalR for responsive feel
- **Document grounding** -- All answers strictly sourced from 4 verified medical documents
- **4 diabetes topics** -- Insulin therapy, CGM monitoring, foot care, physical activity
- **Two selectable themes** -- Clinical (blue) and Friendly (teal), switchable via header toggle
- **Markdown rendering** -- Bold, italic, and line breaks in chat messages
- **Error handling** -- Errors displayed inline in chat; graceful degradation for missing documents
- **Responsive layout** -- Full viewport chat interface with auto-scroll and keyboard shortcuts (Enter to send, Shift+Enter for newline)
- **No data collection** -- Anonymous, stateless, no cookies, no tracking

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
| `ThemeService` | **Scoped** | Per-circuit theme preference |
| `IChatCompletionService` | Registered by SK | Azure OpenAI chat completion via Semantic Kernel |

### Dual History Pattern

`ConversationState` maintains two separate collections:

- **`Messages`** (List\<ChatMessage\>) -- For UI display. Includes the welcome message. Managed by `Home.razor`.
- **`ChatHistory`** (SK ChatHistory) -- For the LLM context. Excludes the welcome message (it is part of the system prompt). Managed by `MedicalAdvisorService`.

This separation prevents the welcome message from being double-counted in the LLM context and keeps UI concerns separate from AI concerns.

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
|   |   +-- AppTheme.cs                   # AppTheme enum (Clinical, Friendly)
|   |-- Services/
|   |   |-- DocumentService.cs            # Loads 4 docs at startup (singleton)
|   |   |-- MedicalAdvisorService.cs      # SK streaming chat orchestration (scoped)
|   |   |-- ConversationState.cs          # Per-circuit messages + ChatHistory (scoped)
|   |   +-- ThemeService.cs               # Theme state + MudTheme definitions (scoped)
|   |-- Prompts/
|   |   +-- SystemPrompt.txt              # Czech AI persona + grounding rules
|   |-- Components/
|   |   |-- App.razor                     # Root HTML document (lang="cs")
|   |   |-- _Imports.razor                # Global @using directives
|   |   |-- Routes.razor                  # Router with MainLayout default
|   |   |-- Layout/
|   |   |   |-- MainLayout.razor          # App bar + MudThemeProvider + content
|   |   |   +-- MainLayout.razor.css      # Scoped CSS (empty, MudBlazor handles it)
|   |   |-- Pages/
|   |   |   |-- Home.razor                # Main chat page (streaming + input)
|   |   |   +-- Error.razor               # Standard error page
|   |   +-- Shared/
|   |       |-- ChatMessageBubble.razor   # Message bubble (Markdown + streaming cursor)
|   |       +-- ThemeSwitcher.razor        # Clinical/Friendly theme toggle
|   |-- wwwroot/
|   |   |-- app.css                       # Chat layout, bubble, cursor CSS
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

**Total: 43 files, 32 unit tests**

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

# Run all 32 tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

### Test Coverage

| Test Class | Tests | What It Covers |
|------------|-------|---------------|
| `DocumentServiceTests` | 6 | Document loading, missing files, content verification, delimiters |
| `ConversationStateTests` | 14 | Initial state, message CRUD, ChatHistory sync, reset, timestamps |
| `ThemeServiceTests` | 12 | Default theme, switching, events, MudTheme color values |

All tests use **xUnit** as the test framework and **Moq** for mocking `IWebHostEnvironment` and `ILogger`. `DocumentServiceTests` creates temporary directories with sample files and cleans up via `IDisposable`.

---

## Deployment

### Azure Resources Required

The app is deployed to Azure App Service with the following resources:

| Resource | Name | Details |
|----------|------|---------|
| Resource Group | `medical-advisor-rg` | West Europe |
| App Service Plan | `medical-advisor-plan` | B1 Basic, Linux (~$13/month) |
| Web App | `medical-advisor-cz` | .NET 8 runtime, WebSockets enabled |
| Azure OpenAI | `anote-openai` (shared) | West Europe, `gpt-4-1-mini` deployment |

### First-Time Setup

```bash
# Install Azure CLI (in a Python venv)
python3 -m venv /tmp/az_venv
source /tmp/az_venv/bin/activate
pip install azure-cli

# Login
az login --use-device-code

# Create resource group
az group create --name medical-advisor-rg --location westeurope

# Create App Service plan (B1 Basic, Linux)
az appservice plan create \
  --name medical-advisor-plan \
  --resource-group medical-advisor-rg \
  --sku B1 --is-linux --location westeurope

# Create web app
az webapp create \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --plan medical-advisor-plan \
  --runtime "DOTNETCORE:8.0"

# Configure app settings (API key + config)
az webapp config appsettings set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --settings \
    AzureOpenAI__ApiKey="<your-api-key>" \
    AzureOpenAI__Endpoint="https://anote-openai.openai.azure.com/" \
    AzureOpenAI__DeploymentName="gpt-4-1-mini"

# Enable WebSockets (required for Blazor Server / SignalR)
az webapp config set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --web-sockets-enabled true

# Set startup command
az webapp config set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --startup-file "dotnet MedicalAdvisor.Web.dll"

# Increase startup timeout (documents take time to load on cold start)
az webapp config appsettings set \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --settings WEBSITES_CONTAINER_START_TIME_LIMIT=600
```

### Deploy / Redeploy

```bash
export PATH="$HOME/.dotnet:$PATH"
source /tmp/az_venv/bin/activate

# Publish Release build
dotnet publish src/MedicalAdvisor.Web/MedicalAdvisor.Web.csproj -c Release -o ./publish

# Create ZIP
cd publish && zip -r ../deploy.zip . && cd ..

# Deploy
az webapp deploy \
  --name medical-advisor-cz \
  --resource-group medical-advisor-rg \
  --src-path deploy.zip --type zip

# Clean up
rm -rf publish deploy.zip
```

### Deployment Notes

- **F1 (Free) tier is insufficient** -- the 60 min/day CPU quota gets consumed during deployment and cold starts with 261K chars of document loading. B1 (Basic) is the minimum viable tier.
- **Cold start** on B1 takes ~60-90 seconds due to Linux container setup + certificate updates + document loading. The `WEBSITES_CONTAINER_START_TIME_LIMIT=600` setting prevents premature timeout.
- **WebSockets must be enabled** for Blazor Server's SignalR connection.
- **`docs/` and `Prompts/` folders** must be in the publish output. This is ensured by `<Content Include="docs\**\*" CopyToOutputDirectory="PreserveNewest" />` in the `.csproj`.

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
App.razor (root HTML document)
  +-- Routes.razor (router)
       +-- MainLayout.razor (app bar + theme provider)
            |-- MudThemeProvider (bound to ThemeService.CurrentMudTheme)
            |-- MudAppBar
            |   |-- "Diabetologicky poradce" (title)
            |   +-- ThemeSwitcher.razor (Clinical/Friendly toggle)
            +-- Home.razor (chat page, @page "/")
                |-- ChatMessageBubble.razor (foreach message)
                +-- MudTextField + MudIconButton (input area)
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

Two MudBlazor themes are available, switchable via the header toggle:

### Clinical Theme (default)

| Property | Value |
|----------|-------|
| Primary | `#1565C0` (blue) |
| Secondary | `#42A5F5` (light blue) |
| AppBar | `#1565C0` |
| Background | `#FAFAFA` |
| Border Radius | Default |

### Friendly Theme

| Property | Value |
|----------|-------|
| Primary | `#00897B` (teal) |
| Secondary | `#4DB6AC` (light teal) |
| AppBar | `#00897B` |
| Background | `#FFF8F0` (warm white) |
| Border Radius | `12px` |

Theme state is managed by `ThemeService` (scoped per circuit). Changes propagate via the `OnThemeChanged` event, which triggers `StateHasChanged` in `MainLayout.razor` to re-render the `MudThemeProvider`.

---

## Implementation Plan

The project was built in 7 phases:

| Phase | Description | Status |
|-------|-------------|--------|
| 1. Project Scaffolding | Solution, projects, NuGet packages, config | Done |
| 2. Document Service & AI Service | DocumentService, MedicalAdvisorService, ConversationState, SystemPrompt | Done |
| 3. Chat UI | Home.razor, ChatMessageBubble, streaming, auto-scroll, keyboard shortcuts | Done |
| 4. Theming | ThemeService, ThemeSwitcher, MudThemeProvider, Clinical + Friendly themes | Done |
| 5. Testing | 32 unit tests (DocumentService, ConversationState, ThemeService) | Done |
| 6. Local Run | Build, test, run locally, verify all documents load | Done |
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
- **Conversation persistence** -- Add database storage (Cosmos DB or SQLite) for chat history
- **User authentication** -- Azure AD B2C or anonymous session tracking
- **Additional documents** -- Expand the knowledge base with more diabetes education materials
- **Feedback mechanism** -- Allow patients to rate responses for quality monitoring
- **CI/CD pipeline** -- GitHub Actions for automated build, test, and deploy on push to main
- **Application Insights** -- Azure Monitor for telemetry, error tracking, and usage analytics
- **Accessibility (a11y)** -- ARIA labels, screen reader support, high contrast theme
- **Mobile optimization** -- PWA support for installable mobile experience
- **Multi-language support** -- Slovak, English translations

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

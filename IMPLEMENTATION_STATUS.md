# Medical Advisor — Implementation Status

## Overall Progress: DEPLOYED TO AZURE

**Production URL: https://medical-advisor-cz.azurewebsites.net**

| Phase | Status | Completed |
|-------|--------|-----------|
| 1. Project Scaffolding | DONE | Yes |
| 2. Document Service & AI Service | DONE | Yes |
| 3. Chat UI | DONE | Yes |
| 4. Theming | DONE | Yes |
| 5. Testing | DONE | 32 tests passing |
| 6. Local Run & Verification | DONE | Yes |
| 7. Azure Deployment | DONE | Yes |

---

## Phase 1: Project Scaffolding

- [x] Create .NET 8 Blazor Server project with solution structure
- [x] Add NuGet packages (Semantic Kernel 1.74.0, MudBlazor 9.2.0, Moq 4.20.72)
- [x] Configure MudBlazor (services, imports, CSS/JS)
- [x] Setup appsettings.json with Azure OpenAI config
- [x] Setup User Secrets for API key (local dev)
- [x] Create .gitignore
- [x] Copy docs/ folder into project
- [x] Verify build (0 warnings, 0 errors)

## Phase 2: Document Service & AI Service

- [x] DocumentService.cs — loads all 4 .txt files at startup (261,539 chars total)
- [x] SystemPrompt.txt — Czech grounding rules + persona
- [x] ChatMessage.cs model (with ChatRole enum, IsStreaming flag)
- [x] ConversationState.cs — scoped per circuit, manages Messages + ChatHistory
- [x] MedicalAdvisorService.cs — Semantic Kernel streaming chat via IChatCompletionService
- [x] Register all services in Program.cs (DI wiring complete)
- [x] Azure OpenAI connection configured (gpt-4-1-mini, West Europe)

## Phase 3: Chat UI

- [x] MainLayout.razor — minimal layout with ThemeSwitcher in app bar
- [x] Home.razor — single page with full chat interface + streaming
- [x] ChatMessageBubble.razor — styled message bubbles (user/assistant)
- [x] ThemeSwitcher.razor — Clinical/Friendly toggle in header
- [x] Streaming responses (token-by-token via SignalR)
- [x] Auto-scroll, welcome message, loading state
- [x] Error handling (catch + display in chat)
- [x] Enter to send, Shift+Enter for newline

## Phase 4: Theming

- [x] AppTheme.cs — Clinical (blue) + Friendly (teal) enum
- [x] ThemeService.cs — scoped state with OnThemeChanged event
- [x] ThemeSwitcher.razor — toggle component in app bar
- [x] MudThemeProvider wired in App.razor
- [x] Custom CSS for chat styling (app.css)

## Phase 5: Testing

- [x] DocumentServiceTests — 6 tests (loading, missing files, content verification)
- [x] ConversationStateTests — 14 tests (state management, reset, chat history)
- [x] ThemeServiceTests — 12 tests (themes, switching, events)
- Total: **32 tests, all passing**

## Phase 6: Local Run

- [x] `dotnet build` — 0 warnings, 0 errors
- [x] `dotnet test` — 32/32 passing
- [x] `dotnet run` — app starts on http://localhost:5113
- [x] All 4 documents loaded at startup
- [x] HTML served correctly with MudBlazor + Czech title

## Phase 7: Azure Deployment

- [x] Created resource group `medical-advisor-rg` (West Europe)
- [x] Created App Service plan `medical-advisor-plan` (B1 Basic, Linux)
- [x] Created web app `medical-advisor-cz` (.NET 8 runtime)
- [x] Configured app settings: `AzureOpenAI__ApiKey`, `AzureOpenAI__Endpoint`, `AzureOpenAI__DeploymentName`
- [x] Enabled WebSockets for SignalR/Blazor Server
- [x] Set startup command: `dotnet MedicalAdvisor.Web.dll`
- [x] Added `docs/` and `Prompts/` to publish output via .csproj `<Content>` items
- [x] ZIP deployed via `az webapp deploy`
- [x] Production URL returns 200 OK with full HTML

---

## Azure Resources

| Resource | Name | Details |
|----------|------|---------|
| Resource Group | `medical-advisor-rg` | West Europe |
| App Service Plan | `medical-advisor-plan` | B1 Basic, Linux (~$13/month) |
| Web App | `medical-advisor-cz` | .NET 8, WebSockets enabled |
| Azure OpenAI | `anote-openai` (shared) | West Europe, `gpt-4-1-mini` deployment |

**Note**: Originally planned for F1 (Free) tier, but upgraded to B1 (Basic) because F1's 60 min/day CPU quota was insufficient for deployment + cold starts with 261K chars of document loading.

## How to Run Locally

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet run --project src/MedicalAdvisor.Web
```

Then open http://localhost:5113 in your browser.

## How to Redeploy

```bash
export PATH="$HOME/.dotnet:$PATH"
source /tmp/az_venv/bin/activate

dotnet publish src/MedicalAdvisor.Web/MedicalAdvisor.Web.csproj -c Release -o ./publish
cd publish && zip -r ../deploy.zip . && cd ..
az webapp deploy --name medical-advisor-cz --resource-group medical-advisor-rg --src-path deploy.zip --type zip
```

## API Key Setup (Local Dev)

```bash
export PATH="$HOME/.dotnet:$PATH"
cd src/MedicalAdvisor.Web
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
```

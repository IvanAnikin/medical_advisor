# Medical Advisor — Technical Specification

## 1. Project Overview

**Medical Advisor** is a Czech-language web chatbot that provides AI-powered diabetes education and guidance directly to patients. The AI is **strictly grounded** in 4 verified medical documents — it cannot hallucinate or invent medical advice. Instead, it only provides information that is explicitly present in the source documents.

The app presents as a **pure chatbot interface** (like an online chat). The AI bot guides the patient through a structured conversation by asking specific questions, then provides targeted advice based on the patient's answers — all sourced from the official documents.

### Knowledge Base (4 Documents)

| Document | Topic | Size | Status |
|----------|-------|------|--------|
| Začínáme s inzulínem | Insulin therapy basics for patients | 52 pages, ~20K tokens | Published |
| CGM — Kontinuální monitorace glukózy | Continuous glucose monitoring guide | 40 pages, ~15K tokens | Upcoming (within 1 month) |
| Péče o nohy diabetikovy | Diabetic foot care | 21 pages, ~7K tokens | Published |
| Doporučení pro FA u DM | Physical activity recommendations for diabetes | 20 pages, ~32K tokens | Published |

**Total: ~75K tokens (~7% of gpt-4.1-mini's context window) — fits comfortably in context stuffing approach.**

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Framework | ASP.NET 8 + Blazor Server | Fast SSR, SignalR streaming, single deployable, Azure-native |
| AI Orchestration | Semantic Kernel | .NET-native OpenAI integration, chat history, prompt templating |
| Knowledge Grounding | Context stuffing (full documents in system prompt) | Simple, reliable, no vector DB needed at ~75K tokens |
| Hosting | Azure App Service Free (F1) | $0/month, sufficient for demo/light use, MSDN subscription |
| AI Backend | Existing `anote-openai` Azure OpenAI (West Europe) | Reuse `gpt-4-1-mini` deployment |
| UX Model | Pure chatbot (single chat window) | Bot guides patient through conversation with questions |
| Authentication | None (anonymous) | Low friction for patients |
| Database | None (stateless) | No persistence, sessions ephemeral in server memory |
| Language | Czech only | Target audience is Czech-speaking diabetes patients |
| UI | Selectable themes (Clinical + Friendly) | User preference via theme switcher |
| Response Delivery | Streaming (token-by-token via SignalR) | Better perceived performance |
| Hallucination Prevention | Strict document grounding | AI must cite/reference source documents, refuse if info not available |

---

## 2. Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    Azure App Service (F1)                     │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │              ASP.NET 8 Blazor Server App                │  │
│  │                                                         │  │
│  │  ┌──────────┐  ┌──────────────┐  ┌──────────────────┐ │  │
│  │  │  Chat UI  │  │  Semantic    │  │  Document        │ │  │
│  │  │  (Blazor  │──│  Kernel      │──│  Knowledge Base  │ │  │
│  │  │  Server)  │  │  Service     │  │  (4 txt files    │ │  │
│  │  │           │  │              │  │   in memory)     │ │  │
│  │  └──────────┘  └──────┬───────┘  └──────────────────┘ │  │
│  │                       │                                 │  │
│  └───────────────────────┼─────────────────────────────────┘  │
│                          │ HTTPS                              │
└──────────────────────────┼────────────────────────────────────┘
                           │
                           ▼
              ┌─────────────────────────┐
              │  Azure OpenAI Service   │
              │  anote-openai           │
              │  West Europe            │
              │  gpt-4-1-mini           │
              └─────────────────────────┘
```

### 2.1 Document Grounding Strategy

**Context Stuffing** — All 4 documents are loaded at application startup and injected into the system prompt for every conversation. At ~75K tokens total, this uses only 7% of gpt-4.1-mini's 1M token context window, leaving ample room for conversation history.

```
System Prompt Structure:
┌─────────────────────────────────────────┐
│  1. Persona & Rules (~500 tokens)       │
│  2. Document: Začínáme s inzulínem      │
│     (~20K tokens)                        │
│  3. Document: CGM                        │
│     (~15K tokens)                        │
│  4. Document: Péče o nohy               │
│     (~7K tokens)                         │
│  5. Document: Fyzická aktivita          │
│     (~32K tokens)                        │
│  6. Grounding rules & citation format   │
│     (~200 tokens)                        │
├─────────────────────────────────────────┤
│  Chat History (user messages +           │
│  assistant responses)                    │
│  (~variable, grows per conversation)     │
└─────────────────────────────────────────┘
```

### 2.2 Component Diagram

```
MedicalAdvisor/
├── Program.cs                          # App entry, DI registration
├── appsettings.json                    # Config (no secrets)
├── Components/
│   ├── App.razor                       # Root component
│   ├── Layout/
│   │   └── MainLayout.razor            # Minimal layout with theme switcher
│   ├── Pages/
│   │   └── Home.razor                  # Single page: the chatbot
│   └── Shared/
│       ├── ChatWindow.razor            # Full chat window component
│       ├── ChatMessageBubble.razor     # Single message bubble
│       ├── ChatInput.razor             # Text input + send button
│       └── ThemeSwitcher.razor         # Theme toggle
├── Services/
│   ├── MedicalAdvisorService.cs        # SK orchestration + document grounding
│   ├── DocumentService.cs             # Loads & caches document content
│   ├── ThemeService.cs                 # Theme state management
│   └── ConversationState.cs            # Per-circuit chat state
├── Models/
│   ├── ChatMessage.cs                  # Chat message model
│   └── AppTheme.cs                     # Theme enum/config
├── Prompts/
│   └── SystemPrompt.txt                # Base persona + grounding rules
├── docs/                               # Knowledge base documents
│   ├── zaciname_s_inzulinem.txt
│   ├── cgm_kontinualni_monitorace.txt
│   ├── pece_o_nohy.txt
│   └── doporuceni_fyzicka_aktivita.txt
├── wwwroot/
│   ├── css/
│   │   └── app.css                     # Chat-specific styles
│   └── favicon.ico
└── Properties/
    └── launchSettings.json
```

---

## 3. User Flow — Guided Chatbot Conversation

The entire UX is a **single chat window**. The bot drives the conversation.

### 3.1 Conversation Flow

```
┌─────────────────────────────────────────────────────────┐
│                    CHAT WINDOW                           │
│                                                          │
│  🤖 Bot: Dobrý den! Jsem váš diabetologický poradce.   │
│     Mohu vám pomoci s informacemi o:                     │
│     • Léčbě inzulínem                                    │
│     • Kontinuální monitoraci glukózy (CGM)               │
│     • Péči o nohy při diabetu                            │
│     • Fyzické aktivitě s diabetem                        │
│                                                          │
│     S čím vám mohu pomoci?                               │
│                                                          │
│  👤 User: Začínám s inzulínem a bojím se                │
│                                                          │
│  🤖 Bot: Rozumím vašim obavám, to je zcela normální.   │
│     Pomohu vám pochopit základy. Nejprve se zeptám:     │
│     Jaký typ diabetu máte? (1. typu nebo 2. typu?)      │
│                                                          │
│  👤 User: 2. typu                                       │
│                                                          │
│  🤖 Bot: Děkuji. U diabetu 2. typu se inzulín nasazuje │
│     když... [answer grounded in "Začínáme s inzulínem"] │
│     Víte již, jaký inzulínový režim vám lékař doporučil?│
│                                                          │
│  ... (conversation continues)                            │
│                                                          │
│  ┌─────────────────────────────────────────┐ ┌───────┐  │
│  │ Napište zprávu...                       │ │Odeslat│  │
│  └─────────────────────────────────────────┘ └───────┘  │
└─────────────────────────────────────────────────────────┘
```

### 3.2 Bot Behavior Rules

1. **Welcome**: Bot introduces itself, lists available topics, asks what the patient needs
2. **Guided questions**: Bot asks specific follow-up questions to understand the patient's situation (diabetes type, current treatment, specific concerns)
3. **Targeted advice**: Based on answers, bot provides relevant information from the documents
4. **Topic switching**: Patient can switch topics at any time ("A co péče o nohy?")
5. **Unknown topics**: If patient asks about something NOT in the documents, bot politely says it doesn't have information on that topic and suggests consulting their doctor
6. **Always conversational**: Short, clear Czech responses, not walls of text. Break information into digestible messages.

---

## 4. Technology Stack

### 4.1 Backend & Framework

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET 8 | LTS |
| Web Framework | ASP.NET Core Blazor Server | 8.0 |
| AI Orchestration | Microsoft.SemanticKernel | Latest stable |
| Azure OpenAI SDK | Azure.AI.OpenAI (via SK) | Latest stable |
| Component Library | MudBlazor | Latest stable |

### 4.2 Why Semantic Kernel

- **Native .NET integration**: First-class C# support, no Python interop
- **Chat completion with streaming**: `IAsyncEnumerable<StreamingChatMessageContent>`
- **Conversation history management**: `ChatHistory` class manages multi-turn conversations perfectly for chatbot UX
- **Large system prompts**: Handles the ~75K token document context naturally
- **Azure OpenAI integration**: Direct connection to existing `anote-openai` resource

### 4.3 Why MudBlazor

- **Chat-ready components**: MudPaper, MudTextField, MudIconButton — build chat UI quickly
- **Built-in theming**: Programmatic theme switching via `MudThemeProvider`
- **Responsive**: Mobile-friendly out of the box
- **Free**: MIT licensed

### 4.4 NuGet Packages

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
<PackageReference Include="MudBlazor" Version="7.*" />
```

---

## 5. Azure OpenAI Integration

### 5.1 Connection Configuration

```json
// appsettings.json (no secrets)
{
  "AzureOpenAI": {
    "Endpoint": "https://anote-openai.openai.azure.com/",
    "DeploymentName": "gpt-4-1-mini",
    "ApiVersion": "2025-04-01-preview"
  }
}
```

API key provided via:
- **Local dev**: User Secrets (`dotnet user-secrets set "AzureOpenAI:ApiKey" "<key>"`)
- **Azure**: App Service Configuration → `AzureOpenAI__ApiKey`

### 5.2 Service Registration

```csharp
// Program.cs
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: config["AzureOpenAI:DeploymentName"],
    endpoint: config["AzureOpenAI:Endpoint"],
    apiKey: config["AzureOpenAI:ApiKey"]
);
builder.Services.AddKernel();
builder.Services.AddSingleton<DocumentService>();    // loads docs once
builder.Services.AddScoped<MedicalAdvisorService>(); // per circuit
builder.Services.AddScoped<ConversationState>();      // per circuit
builder.Services.AddScoped<ThemeService>();            // per circuit
```

### 5.3 Document Loading

```csharp
// DocumentService.cs — Singleton, loads once at startup
public class DocumentService
{
    public string AllDocumentsContent { get; }
    
    public DocumentService(IWebHostEnvironment env)
    {
        var docsPath = Path.Combine(env.ContentRootPath, "docs");
        var sb = new StringBuilder();
        
        sb.AppendLine("=== DOKUMENT: Začínáme s inzulínem ===");
        sb.AppendLine(File.ReadAllText(Path.Combine(docsPath, "zaciname_s_inzulinem.txt")));
        
        sb.AppendLine("=== DOKUMENT: Kontinuální monitorace glukózy (CGM) ===");
        sb.AppendLine(File.ReadAllText(Path.Combine(docsPath, "cgm_kontinualni_monitorace.txt")));
        
        sb.AppendLine("=== DOKUMENT: Péče o nohy diabetikovy ===");
        sb.AppendLine(File.ReadAllText(Path.Combine(docsPath, "pece_o_nohy.txt")));
        
        sb.AppendLine("=== DOKUMENT: Doporučení pro fyzickou aktivitu u diabetes mellitus ===");
        sb.AppendLine(File.ReadAllText(Path.Combine(docsPath, "doporuceni_fyzicka_aktivita.txt")));
        
        AllDocumentsContent = sb.ToString();
    }
}
```

### 5.4 Streaming Chat Pattern

```csharp
// MedicalAdvisorService.cs (conceptual)
public async IAsyncEnumerable<string> StreamResponseAsync(
    string userMessage, ChatHistory history)
{
    history.AddUserMessage(userMessage);
    
    var result = chatCompletion.GetStreamingChatMessageContentsAsync(
        history, executionSettings, kernel);
    
    var fullResponse = new StringBuilder();
    await foreach (var chunk in result)
    {
        fullResponse.Append(chunk.Content);
        yield return chunk.Content ?? "";
    }
    
    history.AddAssistantMessage(fullResponse.ToString());
}
```

---

## 6. System Prompt Design — Document Grounding

### 6.1 System Prompt Structure

```
Jsi český diabetologický poradce — chatbot pro pacienty s diabetem. 
Tvým úkolem je poskytovat pacientům srozumitelné a užitečné informace 
VÝHRADNĚ na základě přiložených odborných dokumentů.

=== TVOJE ROLE ===
- Komunikuj vždy česky, srozumitelným jazykem pro laiky
- Veď konverzaci jako přátelský chat — pokládej otázky, zjišťuj situaci pacienta
- Odpovědi dávej krátké a srozumitelné, ne dlouhé odstavce
- Aktivně se ptej na upřesňující informace (typ diabetu, aktuální léčba, konkrétní obavy)

=== TÉMATA, SE KTERÝMI MŮŽEŠ POMOCI ===
1. Léčba inzulínem (zahájení, typy inzulínu, režimy, dávkování, aplikace, hypoglykémie, hyperglykémie)
2. Kontinuální monitorace glukózy — CGM (princip, systémy, interpretace dat, nastavení)
3. Péče o nohy při diabetu (prevence, rizika, jak chránit nohy, co dělat při potížích)
4. Fyzická aktivita u diabetu (doporučení, typy aktivit, rizika, úprava léčby při sportu)

=== STRIKTNÍ PRAVIDLA GROUNDING ===
- NIKDY nevymýšlej informace, které nejsou v přiložených dokumentech
- Pokud se pacient ptá na něco, co dokumenty nepokrývají, řekni upřímně:
  "Na toto bohužel nemám v dostupných materiálech odpověď. 
   Doporučuji se obrátit na vašeho ošetřujícího lékaře."
- NIKDY nestanov diagnózu
- NIKDY neměň dávkování léků — vždy odkazuj na lékaře
- U závažných symptomů (těžká hypoglykémie, ketoacidóza, akutní defekty na noze) 
  vždy doporuč okamžitou lékařskou pomoc

=== ÚVODNÍ ZPRÁVA ===
Při zahájení konverzace se představ a nabídni témata, se kterými můžeš pomoci.
Zeptej se pacienta, s čím potřebuje poradit.

=== ZNALOSTNÍ BÁZE (dokumenty) ===

{ALL_DOCUMENTS_CONTENT}

=== KONEC ZNALOSTNÍ BÁZE ===
```

### 6.2 Execution Settings

```csharp
var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.3f,   // Low temperature for factual accuracy
    MaxTokens = 1024,     // Keep responses concise (chatbot style)
    TopP = 0.9f
};
```

**Temperature 0.3** is chosen deliberately — low enough to stay factual and grounded, high enough for natural conversational Czech.

---

## 7. Theming System

### 7.1 Theme Definitions

**Clinical/Professional Theme:**
- Deep blue primary (#1565C0), white background, sharp corners
- Clean sans-serif typography, structured layout
- Feeling: Hospital information system, trustworthy

**Friendly/Approachable Theme:**
- Teal/green primary (#00897B), warm white background, rounded corners
- Softer typography with generous spacing
- Feeling: Friendly health app, welcoming

### 7.2 Implementation

Theme switcher in the chat window header — instant switch via MudBlazor's `MudThemeProvider`, no page reload.

---

## 8. Hosting & Deployment

### 8.1 Azure App Service Free (F1) Tier

| Limit | Value | Impact |
|-------|-------|--------|
| CPU | 60 min/day | Sufficient — AI processing is on Azure OpenAI, not on the app server |
| RAM | 1 GB | Enough for Blazor Server + cached documents (~300KB) |
| Storage | 1 GB | More than enough for stateless app + 4 document files |
| SSL | Free on `*.azurewebsites.net` | No custom SSL on F1 |
| Always On | No | Cold start ~5-10s after idle, acceptable for demo |
| Region | West Europe | Co-locate with `anote-openai` for lowest latency |

### 8.2 Cost Estimate

| Resource | Monthly Cost |
|----------|-------------|
| App Service F1 (Free) | $0 |
| Azure OpenAI gpt-4.1-mini input (~75K tokens system prompt per conversation) | ~$0.03 per conversation |
| Azure OpenAI gpt-4.1-mini output (~500 tokens avg per response) | ~$0.0008 per response |
| **100 conversations/day × 10 messages each** | **~$5-10/month** |

**Note**: Context stuffing means every API call includes the full ~75K token system prompt. This is the main cost driver. At gpt-4.1-mini's pricing ($0.40/1M input tokens), that's ~$0.03 per API call just for the documents. For heavier usage, consider RAG to reduce input tokens.

### 8.3 Deployment

```bash
# One-time Azure setup
az group create --name medical-advisor-rg --location westeurope
az appservice plan create --name medical-advisor-plan \
  --resource-group medical-advisor-rg --sku F1 --is-linux
az webapp create --name medical-advisor \
  --resource-group medical-advisor-rg \
  --plan medical-advisor-plan --runtime "DOTNETCORE:8.0"
az webapp config appsettings set --name medical-advisor \
  --resource-group medical-advisor-rg \
  --settings AzureOpenAI__ApiKey="<key>"

# Deploy
dotnet publish -c Release -o ./publish
cd publish && zip -r ../deploy.zip .
az webapp deploy --name medical-advisor \
  --resource-group medical-advisor-rg --src-path ../deploy.zip --type zip
```

### 8.4 CI/CD (GitHub Actions)

```yaml
name: Deploy to Azure
on:
  push:
    branches: [main]
jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet publish -c Release -o ./publish
      - uses: azure/webapps-deploy@v3
        with:
          app-name: medical-advisor
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

---

## 9. Project Structure (Full)

```
medical_advisor/
├── MedicalAdvisor.sln
├── src/
│   └── MedicalAdvisor.Web/
│       ├── MedicalAdvisor.Web.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── Models/
│       │   ├── ChatMessage.cs
│       │   └── AppTheme.cs
│       ├── Services/
│       │   ├── MedicalAdvisorService.cs
│       │   ├── DocumentService.cs
│       │   ├── ThemeService.cs
│       │   └── ConversationState.cs
│       ├── Prompts/
│       │   └── SystemPrompt.txt
│       ├── docs/
│       │   ├── zaciname_s_inzulinem.txt
│       │   ├── cgm_kontinualni_monitorace.txt
│       │   ├── pece_o_nohy.txt
│       │   └── doporuceni_fyzicka_aktivita.txt
│       ├── Components/
│       │   ├── _Imports.razor
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   └── MainLayout.razor.css
│       │   ├── Pages/
│       │   │   └── Home.razor
│       │   └── Shared/
│       │       ├── ChatWindow.razor
│       │       ├── ChatMessageBubble.razor
│       │       ├── ChatInput.razor
│       │       └── ThemeSwitcher.razor
│       └── wwwroot/
│           ├── css/
│           │   └── app.css
│           └── favicon.ico
├── tests/
│   └── MedicalAdvisor.Tests/
│       ├── MedicalAdvisor.Tests.csproj
│       └── Services/
│           ├── MedicalAdvisorServiceTests.cs
│           └── DocumentServiceTests.cs
├── .github/
│   └── workflows/
│       └── deploy.yml
├── .gitignore
├── docs/                                  # Source documents (also copied into project)
│   ├── zaciname_s_inzulinem.txt
│   ├── cgm_kontinualni_monitorace.txt
│   ├── pece_o_nohy.txt
│   └── doporuceni_fyzicka_aktivita.txt
├── TECH_SPEC.md
└── README.md
```

---

## 10. Security Considerations

| Concern | Approach |
|---------|----------|
| API key exposure | User Secrets for dev, App Service Configuration for prod. Never in source code. |
| Patient data privacy | Stateless — no data persisted. Session cleared on disconnect. |
| Document integrity | Documents are bundled with the app, not user-uploadable. |
| Hallucination prevention | System prompt strictly forbids inventing info. Low temperature (0.3). |
| Input sanitization | Blazor Server handles XSS by default. |
| HTTPS | Enforced via Azure App Service. |
| Prompt injection | User input goes into chat history, not into the system prompt template. |

---

## 11. Future Extensibility (Out of Scope for v1)

- **RAG with embeddings**: If documents grow beyond context window, switch to chunked retrieval
- **More documents**: Add new medical guidelines by dropping .txt files into `docs/`
- **Voice input**: Whisper STT (leverage ANOTE expertise)
- **Multilingual**: English/Slovak support via prompt switching
- **Authentication**: Azure AD B2C for patient accounts
- **Analytics**: Application Insights for conversation analytics
- **PDF export**: Export conversation summary
- **Semantic Kernel Plugins**: Drug interaction lookup, appointment scheduling

---

## 12. Implementation Plan

### Phase 1: Project Scaffolding (Est. 1 hour)

| # | Task |
|---|------|
| 1.1 | `dotnet new blazorserver` with solution structure |
| 1.2 | Add NuGet: `Microsoft.SemanticKernel`, `MudBlazor` |
| 1.3 | Configure MudBlazor (services, imports, CSS/JS) |
| 1.4 | Setup `appsettings.json` with Azure OpenAI config |
| 1.5 | Setup User Secrets for API key |
| 1.6 | Create `.gitignore`, copy `docs/` folder into project |
| 1.7 | Verify build & run |

**Deliverable**: Running Blazor Server app with MudBlazor.

---

### Phase 2: Document Service & AI Service (Est. 1-2 hours)

| # | Task |
|---|------|
| 2.1 | `DocumentService.cs` — load all 4 .txt files at startup, cache as single string |
| 2.2 | `SystemPrompt.txt` — write the full system prompt with grounding rules |
| 2.3 | `ChatMessage.cs` — model: Role, Content, Timestamp, IsStreaming |
| 2.4 | `ConversationState.cs` — scoped: ChatHistory, list of ChatMessages |
| 2.5 | `MedicalAdvisorService.cs` — build system prompt with documents, streaming chat |
| 2.6 | Register all services in `Program.cs` |
| 2.7 | Test AI response manually (call service, verify grounded response) |

**Deliverable**: Working AI service that answers from documents only.

---

### Phase 3: Chat UI (Est. 2-3 hours)

| # | Task |
|---|------|
| 3.1 | `MainLayout.razor` — minimal layout with app bar + theme switcher |
| 3.2 | `Home.razor` — single page, renders ChatWindow |
| 3.3 | `ChatWindow.razor` — full chat interface (message list + input) |
| 3.4 | `ChatMessageBubble.razor` — styled bubbles (bot left, user right) |
| 3.5 | `ChatInput.razor` — text field + send button + Enter key support |
| 3.6 | Wire streaming: user sends message → bot streams response token-by-token |
| 3.7 | Auto-scroll to bottom on new messages |
| 3.8 | Initial bot welcome message on page load |
| 3.9 | Loading indicator while bot is typing |
| 3.10 | "New conversation" button to reset |
| 3.11 | Error handling (Azure OpenAI unreachable) |

**Deliverable**: Fully functional chatbot with streaming.

---

### Phase 4: Theming (Est. 1 hour)

| # | Task |
|---|------|
| 4.1 | `AppTheme.cs` — define Clinical + Friendly MudThemes |
| 4.2 | `ThemeService.cs` — scoped service with theme state |
| 4.3 | `ThemeSwitcher.razor` — toggle in header |
| 4.4 | Wire `MudThemeProvider` to `ThemeService` |
| 4.5 | Custom CSS for chat-specific styling (bubbles, scrollbar, etc.) |
| 4.6 | Visual test both themes |

**Deliverable**: Two polished themes, switchable at runtime.

---

### Phase 5: Polish & Testing (Est. 1-2 hours)

| # | Task |
|---|------|
| 5.1 | Unit tests: DocumentService loads correctly, prompt building |
| 5.2 | Test grounding: ask about something NOT in documents, verify refusal |
| 5.3 | Test all 4 topic areas with sample questions |
| 5.4 | Mobile responsiveness (chat window on small screens) |
| 5.5 | Edge cases: empty input, very long input, rapid messages |
| 5.6 | Keyboard: Enter to send, Shift+Enter for newline |

**Deliverable**: Tested, polished chatbot.

---

### Phase 6: Azure Deployment (Est. 30 min)

| # | Task |
|---|------|
| 6.1 | Create Azure resources (resource group, App Service F1, web app) |
| 6.2 | Configure API key in App Service settings |
| 6.3 | `dotnet publish` + ZIP deploy |
| 6.4 | Verify on `https://medical-advisor.azurewebsites.net` |
| 6.5 | Setup GitHub Actions CI/CD |
| 6.6 | Enable HTTPS-only |

**Deliverable**: Live chatbot on Azure.

---

### Total Estimated Time: 6-9 hours

### Phase Dependencies

```
Phase 1 (Scaffolding)
  └──▶ Phase 2 (Documents & AI Service)
         ├──▶ Phase 3 (Chat UI)
         │      └──▶ Phase 5 (Polish & Testing)
         │             └──▶ Phase 6 (Deployment)
         └──▶ Phase 4 (Theming) — can run parallel with Phase 3
```

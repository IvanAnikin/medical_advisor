# Multi-Advisor Architecture — Technical Specification & Implementation Plan

## 1. Overview

Extend the Medical Advisor app from a single hardcoded diabetes advisor to a **multi-advisor platform** where each advisor has its own knowledge base, system prompt, and URL route — but shares the same chat UI, streaming infrastructure, and theme system.

### Goals
- Support N advisors, each configured via `appsettings.json` (no code changes to add a new one)
- Each advisor runs at its own route: `/{advisor-slug}`
- Default route `/` renders the diabetes advisor (current behavior preserved)
- Navbar gets an advisor selector (icon button → popup with advisor list)
- Per-advisor conversation state; global session history (tagged with advisor name)
- Second advisor: **Gestační diabetes** based on `advisor2/GESTdm.docx`

### Non-Goals
- No shared conversation across advisors
- No per-advisor theming (all advisors use the same theme system)
- No authentication or per-user advisor access control
- No vector DB / RAG — stays with context stuffing

---

## 2. Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Advisor definition | `appsettings.json` array | Add advisors without code changes; config-driven |
| Routing | Single page component with `/{Slug}` parameter, `@page "/"` + `@page "/{Slug}"` | Minimal routing changes, reuses existing page |
| Conversation state | Dictionary keyed by advisor ID inside `ConversationState` | Isolated conversations per advisor within same circuit |
| Session history | Global list, each session tagged with advisor ID + name | User requested global view; advisor name shown in UI |
| Document loading | All advisors' docs loaded at startup into `DocumentService` keyed by advisor ID | Same eager-load pattern; keeps startup simple |
| DOCX support | `DocumentFormat.OpenXml` NuGet package, runtime text extraction | No manual conversion step; supports `.docx` and `.txt` transparently |
| System prompts | Separate `.txt` file per advisor in `Prompts/` folder | Full control over each advisor's personality and guardrails |
| Advisor selector UI | MudBlazor `MudMenu` or `MudPopover` triggered by icon button in navbar | Consistent with existing MudBlazor design system |
| Language | All advisors Czech-only | Matches existing UI and target audience |

---

## 3. Configuration Schema

### 3.1 appsettings.json

```json
{
  "Advisors": [
    {
      "Id": "diabetes",
      "Name": "Diabetologický poradce",
      "Slug": "diabetes",
      "Description": "Poradce pro pacienty s diabetem — inzulín, CGM, péče o nohy, fyzická aktivita",
      "Icon": "LocalHospital",
      "DocsFolder": "docs",
      "SystemPromptFile": "Prompts/SystemPrompt.txt",
      "IsDefault": true,
      "WelcomeMessage": "Dobrý den! 👋 Jsem váš diabetologický poradce."
    },
    {
      "Id": "gestational-diabetes",
      "Name": "Gestační diabetes",
      "Slug": "gestacni-diabetes",
      "Description": "Poradce pro gestační diabetes v těhotenství",
      "Icon": "PregnantWoman",
      "DocsFolder": "docs/gestational",
      "SystemPromptFile": "Prompts/GestationalSystemPrompt.txt",
      "IsDefault": false,
      "WelcomeMessage": "Dobrý den! 👋 Jsem poradce pro gestační diabetes."
    }
  ]
}
```

### 3.2 AdvisorConfig Model

```csharp
public class AdvisorConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "Chat";
    public string DocsFolder { get; set; } = "";
    public string SystemPromptFile { get; set; } = "";
    public bool IsDefault { get; set; }
    public string WelcomeMessage { get; set; } = "";
}
```

---

## 4. Architecture Changes

### 4.1 New/Modified Services

#### AdvisorRegistry (NEW — Singleton)
- Loads `Advisors[]` from configuration at startup
- Provides lookup: `GetBySlug(slug)`, `GetById(id)`, `GetDefault()`, `GetAll()`
- Validates config (unique slugs, exactly one default, files exist)

#### DocumentService (MODIFIED — Singleton)
- Currently: loads hardcoded 4 `.txt` files into single `AllDocumentsContent` string
- Changed to: loads docs **per advisor**, keyed by advisor ID
- New method: `GetDocumentsForAdvisor(string advisorId) → string`
- Supports `.txt` (read as-is) and `.docx` (extract text via OpenXml)
- Scans entire `DocsFolder` for supported files (no hardcoded file list)

#### MedicalAdvisorService (MODIFIED — Scoped)
- Currently: reads single `SystemPrompt.txt`, appends single doc content
- Changed to: accepts `AdvisorConfig` (or advisor ID) to build prompt
- `StreamResponseAsync()` gets an additional `advisorId` parameter
- Loads correct system prompt + correct documents for the specified advisor

#### ConversationState (MODIFIED — Scoped)
- Currently: single `Messages` list + single `ChatHistory`
- Changed to: `Dictionary<string, AdvisorConversationState>` keyed by advisor ID
- Each entry holds its own `Messages`, `ChatHistory`, `CurrentSessionId`
- New methods: `GetOrCreate(advisorId)`, `Reset(advisorId)`
- `ToSession()` includes `AdvisorId` + `AdvisorName` in the saved session

### 4.2 Model Changes

#### ConversationSession (MODIFIED)
```csharp
// Add these properties:
public string AdvisorId { get; set; } = "";
public string AdvisorName { get; set; } = "";
```

### 4.3 Component Changes

#### Home.razor (MODIFIED)
- Add route parameter: `@page "/"` + `@page "/{Slug}"`
- `[Parameter] public string? Slug { get; set; }`
- On init and on parameter change: resolve `Slug` → `AdvisorConfig` via `AdvisorRegistry`
- If slug not found → show error or redirect to default
- Pass `advisorId` to `MedicalAdvisorService.StreamResponseAsync()`
- Update welcome message and cards from `AdvisorConfig`
- Update page title to advisor name

#### MainLayout.razor (MODIFIED)
- Add advisor selector icon button in the header (row 1, next to title)
- Title dynamically shows current advisor name (passed via cascading value or route)
- Render `AdvisorSelector` component

#### AdvisorSelector.razor (NEW)
- Icon button (e.g., `MudIconButton` with `Icons.Material.Filled.Apps`)
- On click: shows `MudPopover` or `MudMenu` with list of all advisors
- Each advisor shown as a card/item with: icon, name, description
- Current advisor highlighted
- Clicking navigates to `/{advisor.Slug}` via `NavigationManager`

#### SessionHistory.razor (MODIFIED)
- Show `AdvisorName` on each session card (small label/chip)
- When loading a session, navigate to the correct advisor route

#### WelcomeCards.razor (MODIFIED)
- Currently hardcoded 4 diabetes topic cards
- Changed to: receive card data from `AdvisorConfig` or derive from advisor context
- Option: make welcome cards configurable per advisor in config, or auto-generate

### 4.4 Routing

```
/                        → Default advisor (diabetes)
/diabetes                → Diabetes advisor (explicit)
/gestacni-diabetes       → Gestational diabetes advisor
/{any-future-slug}       → Any future advisor
```

Single `Home.razor` page handles all routes. Slug parameter determines which advisor context to load.

### 4.5 File Structure Changes

```
src/MedicalAdvisor.Web/
├── Models/
│   ├── AdvisorConfig.cs              # NEW — advisor configuration model
│   ├── ChatMessage.cs                # unchanged
│   ├── ConversationSession.cs        # MODIFIED — add AdvisorId, AdvisorName
│   └── AppTheme.cs                   # unchanged
├── Services/
│   ├── AdvisorRegistry.cs            # NEW — loads & provides advisor configs
│   ├── DocumentService.cs            # MODIFIED — per-advisor document loading
│   ├── MedicalAdvisorService.cs      # MODIFIED — advisor-aware prompt building
│   ├── ConversationState.cs          # MODIFIED — per-advisor state dictionary
│   └── ThemeService.cs               # unchanged
├── Components/
│   ├── Pages/
│   │   └── Home.razor                # MODIFIED — slug parameter, advisor context
│   ├── Layout/
│   │   └── MainLayout.razor          # MODIFIED — advisor selector in navbar
│   └── Shared/
│       ├── AdvisorSelector.razor      # NEW — advisor picker popup
│       ├── SessionHistory.razor       # MODIFIED — show advisor name
│       ├── WelcomeCards.razor         # MODIFIED — advisor-specific cards
│       └── ...                        # other shared components unchanged
├── Prompts/
│   ├── SystemPrompt.txt              # existing diabetes prompt (renamed or kept)
│   └── GestationalSystemPrompt.txt   # NEW — gestational diabetes prompt
├── docs/                             # existing diabetes docs (stays as-is)
│   ├── zaciname_s_inzulinem.txt
│   ├── cgm_kontinualni_monitorace.txt
│   ├── pece_o_nohy.txt
│   └── doporuceni_fyzicka_aktivita.txt
└── docs/gestational/                 # NEW — gestational diabetes docs
    └── GESTdm.docx                   # moved/copied from advisor2/
```

---

## 5. DOCX Text Extraction

### Approach
Use `DocumentFormat.OpenXml` (official Microsoft SDK, no external dependencies) to extract plain text from `.docx` files at startup.

### NuGet Package
```xml
<PackageReference Include="DocumentFormat.OpenXml" Version="3.*" />
```

### Extraction Logic (in DocumentService)
```csharp
private static string ExtractTextFromDocx(string filePath)
{
    using var doc = WordprocessingDocument.Open(filePath, false);
    var body = doc.MainDocumentPart?.Document?.Body;
    if (body == null) return "";
    
    var sb = new StringBuilder();
    foreach (var paragraph in body.Elements<Paragraph>())
    {
        sb.AppendLine(paragraph.InnerText);
    }
    return sb.ToString();
}
```

### Document Processing in DocumentService
When scanning an advisor's `DocsFolder`:
1. Find all `.txt` and `.docx` files
2. For `.txt` → read as UTF-8 text
3. For `.docx` → extract text via OpenXml
4. Wrap each document: `=== DOKUMENT: {filename} ===\n\n{content}`
5. Concatenate all into advisor's document content string

---

## 6. Gestational Diabetes Advisor

### System Prompt (`Prompts/GestationalSystemPrompt.txt`)
A new Czech-language system prompt following the same structure as the diabetes one, but scoped to gestational diabetes:
- Role: Czech gestational diabetes advisor chatbot for pregnant patients
- Tone: Friendly, reassuring, clear for layperson
- Behavior: Ask about gestational week, current treatment, concerns
- Guardrails: Same as diabetes (no diagnosis, no dose changes, refer to doctor for serious symptoms)
- Knowledge source: Only from the GESTdm.docx document

### Knowledge Base
- Single document: `GESTdm.docx` (moved to `docs/gestational/`)
- Extracted at startup, injected into system prompt

### Welcome Message & Cards
- Custom welcome message referencing gestational diabetes
- Welcome cards derived from the document's main topics (to be determined after reviewing document content)

---

## 7. Implementation Plan

### Phase 1: Infrastructure (foundation)
| # | Task | Files | Description |
|---|------|-------|-------------|
| 1.1 | Create `AdvisorConfig` model | `Models/AdvisorConfig.cs` | Config POCO with all advisor properties |
| 1.2 | Create `AdvisorRegistry` service | `Services/AdvisorRegistry.cs` | Singleton that loads and validates advisor configs from `IConfiguration` |
| 1.3 | Add advisor config to `appsettings.json` | `appsettings.json` | Define both advisors with all properties |
| 1.4 | Register services in `Program.cs` | `Program.cs` | Register `AdvisorRegistry`, update DI |

### Phase 2: Document & Prompt System (per-advisor knowledge)
| # | Task | Files | Description |
|---|------|-------|-------------|
| 2.1 | Add `DocumentFormat.OpenXml` NuGet | `.csproj` | Add package reference for DOCX reading |
| 2.2 | Refactor `DocumentService` | `Services/DocumentService.cs` | Per-advisor doc loading, auto-scan folder, support `.docx` |
| 2.3 | Refactor `MedicalAdvisorService` | `Services/MedicalAdvisorService.cs` | Accept advisor ID, load correct prompt + docs |
| 2.4 | Refactor `ConversationState` | `Services/ConversationState.cs` | Per-advisor state dictionary |
| 2.5 | Update `ConversationSession` model | `Models/ConversationSession.cs` | Add `AdvisorId`, `AdvisorName` fields |

### Phase 3: Gestational Diabetes Advisor (second advisor)
| # | Task | Files | Description |
|---|------|-------|-------------|
| 3.1 | Create `docs/gestational/` folder | filesystem | Move/copy `GESTdm.docx` |
| 3.2 | Write `GestationalSystemPrompt.txt` | `Prompts/GestationalSystemPrompt.txt` | Czech system prompt for gestational diabetes |
| 3.3 | Test document extraction | manual/test | Verify DOCX text extraction quality |

### Phase 4: UI Changes (routing + advisor selector)
| # | Task | Files | Description |
|---|------|-------|-------------|
| 4.1 | Add slug routing to `Home.razor` | `Components/Pages/Home.razor` | `@page "/{Slug}"`, resolve advisor, advisor-aware chat |
| 4.2 | Create `AdvisorSelector.razor` | `Components/Shared/AdvisorSelector.razor` | Icon button + popup with advisor list |
| 4.3 | Update `MainLayout.razor` | `Components/Layout/MainLayout.razor` | Add advisor selector to navbar, dynamic title |
| 4.4 | Update `SessionHistory.razor` | `Components/Shared/SessionHistory.razor` | Show advisor name on saved sessions |
| 4.5 | Update `WelcomeCards.razor` | `Components/Shared/WelcomeCards.razor` | Advisor-specific welcome cards |

### Phase 5: Testing & Polish
| # | Task | Files | Description |
|---|------|-------|-------------|
| 5.1 | Update existing unit tests | `tests/` | Adapt tests for multi-advisor services |
| 5.2 | Add `AdvisorRegistry` tests | `tests/` | Config validation, slug lookup, default resolution |
| 5.3 | Test advisor switching in UI | manual | Navigate between advisors, verify isolation |
| 5.4 | Test session history across advisors | manual | Save/load sessions, verify advisor tags |
| 5.5 | Update `appsettings.json` in `publish/` | `publish/appsettings.json` | Mirror advisor config for deployment |

---

## 8. Data Flow (Multi-Advisor)

```
User visits /gestacni-diabetes
         │
         ▼
┌─────────────────────────────┐
│ Home.razor                  │
│ Slug = "gestacni-diabetes"  │
│                             │
│ AdvisorRegistry             │
│   .GetBySlug(Slug)          │──→ AdvisorConfig { Id="gestational-diabetes", ... }
│                             │
│ ConversationState            │
│   .GetOrCreate(advisorId)   │──→ Isolated Messages + ChatHistory
│                             │
│ MedicalAdvisorService       │
│   .StreamResponseAsync(     │
│     userMessage,             │
│     advisorId,               │
│     state, ...)              │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│ Build prompt:               │
│  SystemPrompt =             │
│    GestationalSystemPrompt  │
│    .txt content             │
│  + DocumentService          │
│    .GetDocs("gestational-   │
│     diabetes")              │
│    (extracted from           │
│     GESTdm.docx)            │
│  + ChatHistory              │
└──────────┬──────────────────┘
           │
           ▼
    Azure OpenAI (streaming)
```

---

## 9. Migration Notes

- **No breaking changes to existing behavior**: `/` still loads the diabetes advisor
- **Existing session history**: Old sessions without `AdvisorId` field default to `"diabetes"`
- **Existing system prompt**: `SystemPrompt.txt` stays where it is, config just points to it
- **Existing docs folder**: `docs/` stays unchanged, config points to `"docs"` for diabetes advisor
- **Branch**: All work in a new feature branch (e.g., `feature/multi-advisor`)

---

## 10. Future Extensibility

Adding a new advisor requires only:
1. Add entry to `Advisors[]` in `appsettings.json`
2. Create a docs folder with source documents (`.txt` or `.docx`)
3. Write a system prompt `.txt` file
4. Restart the app

No code changes needed.

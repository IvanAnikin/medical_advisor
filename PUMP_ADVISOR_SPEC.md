# Inzulínová pumpa Advisor — Technical Specification & Implementation Plan

## 1. Overview

Add a third advisor — **Inzulínová pumpa** — focused on the Tandem Control IQ hybrid closed-loop insulin pump system. Unlike the existing advisors (diabetes, gestational diabetes) which use **context stuffing** (all documents in the system prompt), this advisor introduces **RAG (Retrieval-Augmented Generation)** because one of its source documents is too large (~114K tokens) to fit in the prompt alongside conversation history.

### Data Sources

| File | Pages | Size | ~Tokens | Role |
|------|-------|------|---------|------|
| `1748523970EMCD33_v03_Tandem_CIQ.pdf` | 2 | 213 KB | ~1,500 | Patient education summary — always included in prompt |
| `Pumpa Tandem (1).pdf` | 368 | 10 MB | ~114,000 | Full user manual — indexed for RAG retrieval |

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Small summary | Pre-extracted to `.txt`, always in system prompt | Gives model orientation on every message; only ~1,500 tokens |
| Large manual | RAG with vector search, top-K chunks injected per query | 114K tokens — too large for context stuffing; would cost 15× more |
| Embedding model | `text-embedding-3-small` (Azure OpenAI) | Cheapest ($0.02/1M tokens), good quality, available in westeurope |
| Vector store | In-memory (cosine similarity on float arrays) | Single instance, ~250 chunks — no need for a vector DB |
| Embedding cache | JSON file on disk | Avoids re-embedding on restart (~$0.004 per full re-index, but saves API calls and 30s startup) |
| PDF text extraction | `UglyToad.PdfPig` (NuGet) | Pure .NET, no native deps, MIT license |
| Chunking | Hybrid: section-based, large sections split at ~500 tokens with ~50 token overlap | Respects document structure; large sections still get granular retrieval |
| Top-K | 5 chunks per query | ~2,500 tokens injected — focused and cheap |

---

## 2. Architecture

### Current Flow (Diabetes & Gestational — Context Stuffing)

```
User message
  → GetSystemPrompt(advisorId)
      → promptTemplate + ALL docs concatenated (~75K / ~31K tokens)
  → ChatHistory: system prompt + conversation
  → Azure OpenAI gpt-5.4-mini (streaming)
  → Response with [QUICK_REPLIES]
```

### New Flow (Inzulínová pumpa — RAG)

```
User message
  → Embed user query (text-embedding-3-small, ~50 tokens)
  → Vector search in-memory: top 5 most relevant chunks from manual
  → Build system prompt:
      1. PumpSystemPrompt.txt (persona + rules, ~800 tokens)
      2. Pre-extracted 2-page summary (always, ~1,500 tokens)
      3. Retrieved chunks (5 × ~500 = ~2,500 tokens)
      4. Instruction: "Answer ONLY from the above context"
  → ChatHistory: system prompt + conversation
  → Azure OpenAI gpt-5.4-mini (streaming)
  → Response with [QUICK_REPLIES]
```

### Cost Comparison (per 100 average requests, mid-conversation)

| Advisor | Approach | Input tokens/req | Cost / 100 req |
|---------|----------|------------------|----------------|
| Diabetes | Context stuffing | ~118,500 | $1.81 |
| Gestational | Context stuffing | ~32,500 | $0.52 |
| Pumpa (if stuffed) | Context stuffing | ~184,000 | $2.79 |
| **Pumpa (RAG)** | **RAG retrieval** | **~6,300** | **$0.12** |

Pricing based on `gpt-5.4-mini` GlobalStandard: $0.15/1M input, $0.60/1M output.
Embedding cost per query: ~$0.000001 (negligible).

---

## 3. Azure Infrastructure Changes

### Current State

Resource: `anote-openai` (West Europe, S0)

| Deployment | Model | SKU | Used by |
|-----------|-------|-----|---------|
| gpt-5-4-mini | gpt-5.4-mini | GlobalStandard 100K TPM | Medical Advisor app |
| gpt-4-1-mini | gpt-4.1-mini | Standard 30K TPM | (legacy) |
| gpt-5-mini | gpt-5-mini | GlobalStandard 101K TPM | Other projects |
| gpt-5-chat | gpt-5-chat | GlobalStandard 100K TPM | Other projects |
| whisper | whisper | Standard 3K TPM | Other projects |

**No embedding models deployed.** Quota available: 2000K TPM for `text-embedding-3-small` (GlobalStandard), 0 used.

### Required: Deploy Embedding Model

```bash
az cognitiveservices account deployment create \
  --name anote-openai \
  --resource-group ANOTE \
  --deployment-name text-embedding-3-small \
  --model-name text-embedding-3-small \
  --model-version 1 \
  --model-format OpenAI \
  --sku-name GlobalStandard \
  --sku-capacity 120
```

- **No new Azure resource** — same `anote-openai` account
- **No new API key** — same key works for both chat and embeddings
- **No monthly cost** — GlobalStandard is pure pay-per-use
- **Cost**: ~$0.02/1M tokens (indexing the entire manual = $0.004)

### Configuration Addition

```json
// appsettings.json — AzureOpenAI section
{
  "AzureOpenAI": {
    "Endpoint": "https://anote-openai.openai.azure.com/",
    "DeploymentName": "gpt-5-4-mini",
    "EmbeddingDeploymentName": "text-embedding-3-small"   // ← NEW
  }
}
```

---

## 4. New Advisor Configuration

### appsettings.json — Advisors array (new entry)

```json
{
  "Id": "insulin-pump",
  "Name": "Inzulínová pumpa",
  "Slug": "inzulinova-pumpa",
  "Description": "Poradce pro inzulínovou pumpu Tandem Control IQ",
  "Icon": "SettingsInputComponent",
  "DocsFolder": "docs/pump",
  "SystemPromptFile": "Prompts/PumpSystemPrompt.txt",
  "IsDefault": false,
  "UseRag": true,
  "RagCacheFile": "docs/pump/embeddings_cache.json",
  "WelcomeMessage": "Dobrý den! 👋 Jsem poradce pro inzulínovou pumpu Tandem Control IQ. Pomohu vám s nastavením, funkcemi a každodenním používáním pumpy."
}
```

### AdvisorConfig Model — new field

```csharp
public bool UseRag { get; set; } = false;       // Existing advisors default to false
public string RagCacheFile { get; set; } = "";   // Path to embeddings cache JSON
```

---

## 5. File Structure (new/modified files)

```
src/MedicalAdvisor.Web/
├── docs/
│   └── pump/                                    ← NEW folder
│       ├── tandem_ciq_summary.txt               ← Pre-extracted from 2-page PDF
│       ├── tandem_manual_full.txt               ← Pre-extracted from 368-page PDF
│       └── embeddings_cache.json                ← Generated on first run, cached
├── Prompts/
│   └── PumpSystemPrompt.txt                     ← NEW system prompt
├── Models/
│   └── AdvisorConfig.cs                         ← MODIFIED (add UseRag, RagCacheFile)
│   └── EmbeddingChunk.cs                        ← NEW model
├── Services/
│   ├── PumpRagService.cs                        ← NEW service (chunking, embedding, retrieval)
│   ├── MedicalAdvisorService.cs                 ← MODIFIED (RAG branch for pump advisor)
│   └── DocumentService.cs                       ← NO CHANGES (still loads summary .txt)
├── Program.cs                                   ← MODIFIED (register embedding + RAG service)
└── appsettings.json                             ← MODIFIED (new advisor + embedding config)
```

---

## 6. Component Design

### 6.1 EmbeddingChunk (Model)

```csharp
public class EmbeddingChunk
{
    public int Index { get; set; }            // Sequential chunk number
    public string Text { get; set; }          // Chunk text content (~500 tokens)
    public string Source { get; set; }         // Source section/page reference
    public float[] Embedding { get; set; }    // 1536-dim vector from text-embedding-3-small
}
```

### 6.2 EmbeddingsCache (serialized to JSON)

```json
{
  "ModelName": "text-embedding-3-small",
  "SourceFileHash": "sha256:abc123...",
  "CreatedAt": "2026-04-06T12:00:00Z",
  "Chunks": [
    {
      "Index": 0,
      "Text": "Co je to hybridní uzavřený okruh?...",
      "Source": "section:1",
      "Embedding": [0.012, -0.034, ...]
    }
  ]
}
```

Cache invalidation: SHA-256 hash of the source `.txt` file. If the hash changes (document updated), re-embed on next startup.

### 6.3 PumpRagService (Singleton)

**Responsibilities:**
1. **Startup**: Load or build embedding index
2. **Per-query**: Embed query → cosine similarity → return top-K chunks

```
Constructor(IConfiguration, ITextEmbeddingGenerationService, ILogger)

InitializeAsync():
  1. Check if embeddings_cache.json exists AND hash matches source file
     → YES: Deserialize chunks + embeddings into memory
     → NO:  
        a. Read tandem_manual_full.txt
        b. Chunk with hybrid strategy (section headings + 500-token windows)
        c. Call embedding API in batches of 20
        d. Serialize to embeddings_cache.json
        e. Hold in memory

RetrieveAsync(string query, int topK = 5) → List<string>:
  1. Embed the query (single API call, ~50 tokens)
  2. Compute cosine similarity against all chunk embeddings
  3. Return top-K chunk texts, sorted by relevance
```

**Chunking strategy (hybrid):**
1. Split text by section headings (lines that are ALL CAPS or match heading patterns)
2. For each section:
   - If ≤ 500 tokens → keep as one chunk
   - If > 500 tokens → split into ~500-token windows with ~50-token overlap
3. Each chunk retains its section heading as metadata (`Source` field)
4. Estimated result: ~200-300 chunks for the 368-page manual

### 6.4 MedicalAdvisorService Changes

The `GetSystemPrompt` method gets a RAG-aware branch:

```
GetSystemPrompt(advisorId):
  if advisor.UseRag:
    → return promptTemplate + summaryDoc only (cached)
    → (retrieved chunks injected per-message, not cached here)
  else:
    → return promptTemplate + ALL docs (existing behavior)

StreamResponseAsync(userMessage, state, streamingMessage, ct, advisorId):
  if advisor.UseRag:
    → chunks = await PumpRagService.RetrieveAsync(userMessage, topK: 5)
    → systemPrompt = cachedPrompt + "\n\n=== RELEVANTNÍ ČÁSTI MANUÁLU ===\n" + chunks
  else:
    → systemPrompt = GetSystemPrompt(advisorId)  // existing path
  ...rest stays the same...
```

### 6.5 PumpSystemPrompt.txt

```
Jsi český poradce pro inzulínovou pumpu Tandem Control IQ — chatbot pro pacienty.
Tvým úkolem je pomáhat pacientům s každodenním používáním pumpy
VÝHRADNĚ na základě přiložených materiálů.

=== TVOJE ROLE ===
- Komunikuj vždy česky, srozumitelným jazykem pro laiky
- Veď konverzaci jako přátelský chat — pokládej otázky, zjišťuj situaci
- Odpovědi dávej krátké a srozumitelné
- Aktivně se ptej na upřesňující informace

=== TÉMATA ===
1. Hybridní uzavřený okruh — jak funguje, co dělá automaticky
2. Dávkování inzulinu — bazál, autokorekce, bolusy, sacharidový poměr
3. Senzor Dexcom — výměna, zahřívání, přesnost
4. Speciální režimy — mód spánku, mód fyzické aktivity
5. Ruční režim — kdy přepnout, náhradní režim (pera)
6. Každodenní péče — výměna kanyly, stahování dat, odpojení pumpy

=== STRIKTNÍ PRAVIDLA ===
- NIKDY nevymýšlej informace mimo přiložené materiály
- Pokud nemáš odpověď: "Na toto nemám v materiálech odpověď. 
  Kontaktujte prosím svého diabetologa nebo technickou podporu Tandem."
- NIKDY nestanov diagnózu ani neměň nastavení pumpy
- Při závažných problémech (selhání pumpy, těžká hypo/hyperglykémie) 
  → okamžitá lékařská pomoc

=== FORMÁT NÁVRHŮ ===
Na konci odpovědi přidej 2-4 návrhy:
[QUICK_REPLIES]
...
[/QUICK_REPLIES]

=== PŘEHLED SYSTÉMU (vždy k dispozici) ===

```

(After this marker, the pre-extracted 2-page summary is concatenated at startup.)

### 6.6 Program.cs Changes

```csharp
// --- Azure OpenAI via Semantic Kernel ---
var aoaiSection = builder.Configuration.GetSection("AzureOpenAI");
var endpoint = aoaiSection["Endpoint"] ?? throw ...;
var deploymentName = aoaiSection["DeploymentName"] ?? throw ...;
var apiKey = aoaiSection["ApiKey"] ?? throw ...;

builder.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);

// NEW: Register embedding generation (only if configured)
var embeddingDeployment = aoaiSection["EmbeddingDeploymentName"];
if (!string.IsNullOrEmpty(embeddingDeployment))
{
    builder.Services.AddAzureOpenAITextEmbeddingGeneration(
        embeddingDeployment, endpoint, apiKey);
    builder.Services.AddSingleton<PumpRagService>();
}

// ...existing services...

// After app.Build():
app.Services.GetRequiredService<AdvisorRegistry>();
app.Services.GetRequiredService<DocumentService>();

// NEW: Initialize RAG index at startup
if (!string.IsNullOrEmpty(embeddingDeployment))
{
    var ragService = app.Services.GetRequiredService<PumpRagService>();
    await ragService.InitializeAsync();
}
```

---

## 7. NuGet Dependencies

| Package | Purpose | Status |
|---------|---------|--------|
| `Microsoft.SemanticKernel` | Chat completion + embedding generation | Already installed |
| `UglyToad.PdfPig` | PDF text extraction (one-time, for pre-extracting docs) | **NEW** (or use a script, then remove) |

Note: `UglyToad.PdfPig` is only needed if we extract PDFs at build-time rather than committing the `.txt` files. Since the decision is to **pre-extract**, we can extract once with a script (Python/PyMuPDF already available) and commit the `.txt` files — no new NuGet dependency needed at runtime.

---

## 8. Pre-Extraction of PDFs

Run once (before implementation), commit the results:

```bash
# Using the existing PyMuPDF venv
source /tmp/pdfenv/bin/activate

python3 -c "
import fitz

# Small PDF → summary
doc = fitz.open('advisor3/1748523970EMCD33_v03_Tandem_CIQ.pdf')
with open('src/MedicalAdvisor.Web/docs/pump/tandem_ciq_summary.txt', 'w') as f:
    for page in doc:
        f.write(page.get_text())
doc.close()

# Large PDF → full manual text
doc = fitz.open('advisor3/Pumpa Tandem (1).pdf')
with open('src/MedicalAdvisor.Web/docs/pump/tandem_manual_full.txt', 'w') as f:
    for page in doc:
        text = page.get_text()
        if text.strip():
            f.write(text)
            f.write('\n\n---\n\n')  # page separator for chunking
doc.close()
"
```

This produces two `.txt` files committed to the repo. No PDF dependency at runtime.

---

## 9. Implementation Plan

### Phase 0: Azure Setup (5 min)
- [ ] Deploy `text-embedding-3-small` on `anote-openai` via az CLI
- [ ] Verify deployment with a test embedding call

### Phase 1: Data Preparation (15 min)
- [ ] Create `src/MedicalAdvisor.Web/docs/pump/` directory
- [ ] Extract small PDF → `tandem_ciq_summary.txt`
- [ ] Extract large PDF → `tandem_manual_full.txt`
- [ ] Verify extracted text quality (spot check headings, tables, special chars)

### Phase 2: Model & Config Changes (15 min)
- [ ] Add `UseRag` and `RagCacheFile` fields to `AdvisorConfig.cs`
- [ ] Add pump advisor entry to `appsettings.json` Advisors array
- [ ] Add `EmbeddingDeploymentName` to `appsettings.json` AzureOpenAI section
- [ ] Create `Prompts/PumpSystemPrompt.txt`

### Phase 3: RAG Service (core implementation) (45 min)
- [ ] Create `Models/EmbeddingChunk.cs`
- [ ] Create `Services/PumpRagService.cs`:
  - Hybrid chunking logic (section headings + 500-token windows)
  - Embedding via `ITextEmbeddingGenerationService`
  - JSON cache read/write with SHA-256 hash validation
  - Cosine similarity search
  - `InitializeAsync()` for startup
  - `RetrieveAsync(query, topK)` for per-query retrieval

### Phase 4: Integration (20 min)
- [ ] Register embedding service + PumpRagService in `Program.cs`
- [ ] Add startup initialization of RAG index
- [ ] Modify `MedicalAdvisorService.StreamResponseAsync` for RAG branch
- [ ] Modify `GetSystemPrompt` to handle RAG advisors (prompt + summary only)

### Phase 5: UI Adjustments (10 min)
- [ ] Add pump-specific topic cards to `WelcomeCards.razor` (if desired)
- [ ] Verify advisor selector shows 3 advisors correctly
- [ ] Test routing at `/inzulinova-pumpa`

### Phase 6: Testing (20 min)
- [ ] Unit test: chunking logic (correct split, overlap, section preservation)
- [ ] Unit test: cosine similarity ranking
- [ ] Unit test: cache serialization/deserialization + hash invalidation
- [ ] Integration test: full RAG flow (embed query → retrieve → prompt building)
- [ ] Manual test: ask pump-specific questions, verify grounding
- [ ] Manual test: ask off-topic questions, verify refusal
- [ ] Verify existing advisors (diabetes, gestational) still work unchanged

### Phase 7: Cache & Deploy (10 min)
- [ ] Generate initial `embeddings_cache.json` (first run)
- [ ] Commit cache file to repo (or add to .gitignore and generate on deploy)
- [ ] Test cold start with cache vs without
- [ ] Deploy to Azure App Service

---

## 10. Risk & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| PDF text extraction garbled (tables, columns) | Bad chunk quality | Spot-check extracted text; manually fix problematic sections |
| Chunking splits mid-sentence | Retrieval misses context | 50-token overlap between windows; section headings preserved |
| Irrelevant chunks retrieved | Bad answers | Tune topK (3-7); add minimum similarity threshold (e.g., 0.7) |
| Cache invalidation missed | Stale embeddings after doc update | SHA-256 hash of source file checked on every startup |
| Embedding API down at startup | App won't start if no cache | Always commit/deploy with a valid cache file; graceful fallback |

---

## 11. Future Considerations (Out of Scope)

- **Making RAG generic** (any advisor can opt in) — straightforward later since `UseRag` flag is already per-advisor
- **Azure AI Search** — if document count grows or multi-instance is needed
- **Hybrid search** (vector + keyword) — for cases where exact terms matter (error codes, model numbers)
- **Re-ranking** — use a cross-encoder to re-rank top-K chunks for better precision
- **Streaming chunk attribution** — show which manual section each answer came from

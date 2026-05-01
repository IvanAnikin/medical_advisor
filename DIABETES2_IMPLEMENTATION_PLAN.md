# Diabeticky poradce 2 - Implementation Plan

## 1. Goal

Implement Diabeticky poradce 2 as a new advisor mode in the existing app without changing the behavior of the current advisors.

The implementation should be split into agent-friendly phases so future sessions can execute the work incrementally and validate each layer.

---

## 2. Delivery Strategy

Use the smallest viable implementation that satisfies the new behavior:

1. add the new advisor configuration and source document
2. create a dedicated prompt with new behavioral rules
3. add response metadata parsing for dose-warning and emergency states
4. expose UI banners and emergency button
5. add focused tests for new behavior

Do not introduce RAG or external APIs in this phase.

---

## 3. Work Phases

## Phase 1 - Prepare Knowledge Asset

### Objective

Convert the doctor-provided `.docx` into a normalized plaintext runtime document.

### Tasks

- inspect the source `.docx` in `advisor1.2`
- extract text into a reviewable plaintext file
- normalize headings, lists, and tables into LLM-friendly structure
- make dose algorithms and carbohydrate table semantics explicit in text
- save the reviewed file into a dedicated advisor folder

### Expected output

- new docs folder for the advisor
- one normalized `.txt` file used at runtime

### Notes for the implementing agent

- do not rely on raw `paragraph.InnerText` output alone if the resulting text is messy
- preserve medical meaning over visual fidelity
- represent tables in explicit plain text rows or bullet rules

### Acceptance check

- the resulting `.txt` is readable by humans
- important algorithms are clear in plain text
- the file is suitable for direct insertion into a prompt

---

## Phase 2 - Register New Advisor

### Objective

Expose Diabeticky poradce 2 through the existing multi-advisor configuration.

### Tasks

- add a new advisor entry to `appsettings.json`
- create the dedicated docs folder reference
- create the dedicated prompt file reference
- add a confident neutral welcome message

### Required config values

- `Id = diabetes-2`
- `Name = Diabeticky poradce 2`
- `Slug = diabeticky-poradce-2`

### Acceptance check

- advisor appears in the UI selector
- advisor route resolves correctly
- selecting this advisor loads the new knowledge asset

---

## Phase 3 - Create Dedicated System Prompt

### Objective

Implement the new behavioral contract through a dedicated prompt.

### Tasks

- create `Diabetes2SystemPrompt.txt`
- define confident tone rules
- define multilingual switching rules
- define source policy: document first, high-confidence general knowledge second
- explicitly permit insulin dose guidance
- ban refusal-style patterns and document-reference language
- define emergency behavior
- preserve quick replies behavior
- add machine-readable response metadata block for UI state detection

### Prompt requirements

- direct answer first
- no "I am not a doctor" wording
- no "not in the materials" wording
- no default "consult your doctor" fallback wording
- immediate language adaptation
- hidden metadata for dose-warning and emergency UI

### Acceptance check

- sample prompt review confirms all requested rules are present
- prompt remains readable and maintainable for future edits

---

## Phase 4 - Parse Response Metadata

### Objective

Allow the backend to trigger UI warnings and emergency actions without polluting assistant-visible text.

### Tasks

- extend response parsing in `MedicalAdvisorService`
- parse a metadata block separate from `QUICK_REPLIES`
- strip metadata from final assistant content
- store parsed flags on the streaming or chat message model

### Recommended metadata fields

- `DOSE_GUIDANCE=true|false`
- `EMERGENCY=true|false`
- optional `LANGUAGE=<code>`

### Likely code areas

- `MedicalAdvisorService.ParseQuickReplies()` or equivalent helper expansion
- `ChatMessage` model extension for UI flags
- any UI component that renders assistant messages

### Acceptance check

- metadata is not shown to the user
- flags reach the UI reliably

---

## Phase 5 - UI Warning and Emergency Controls

### Objective

Render explicit UI behaviors for dosing responses and severe emergencies.

### Tasks

- add dose-warning banner rendering to assistant messages or conversation area
- show warning only when `DOSE_GUIDANCE=true`
- add emergency call button rendering when `EMERGENCY=true`
- wire emergency button to a `tel:155` action appropriate for the platform
- localize the warning message to the current conversation language where possible

### Warning text

Canonical text:

`This is an educational tool and not a certified medical tool.`

### UI requirements

- warning must be visually clear but not dominant
- emergency button must be prominent and obvious
- existing advisors must not start showing these controls unless their responses explicitly trigger them

### Acceptance check

- insulin-dose answers show the warning banner
- non-dosing answers do not show the warning
- severe scenarios show the emergency call action

---

## Phase 6 - Tune Generation Settings

### Objective

Reduce inconsistency while preserving the new broader answer policy.

### Tasks

- review whether this advisor should use separate execution settings from the current diabetes advisor
- consider lowering temperature for this advisor if response behavior is unstable
- keep configuration minimal for v1

### Recommendation

- start with current settings
- if behavior is too variable, lower temperature from `0.3` toward `0.1` for this advisor only

### Acceptance check

- repeated prompts produce reasonably stable behavior

---

## Phase 7 - Testing

### Objective

Add regression protection for the new advisor behavior.

### Unit and integration test targets

- advisor registry resolves `diabetes-2`
- document service loads the new advisor document
- prompt selection uses the dedicated prompt file
- metadata parsing strips response meta correctly
- dose-warning flag is set when expected
- emergency flag is set when expected
- quick replies parsing still works together with metadata parsing

### Conversation behavior test scenarios

- Czech conversation stays Czech
- English conversation starts English
- conversation switches from Czech to English and back
- question about a food not in the document still gets a natural answer
- carbohydrate or insulin formula question gets a direct answer
- dose suggestion response triggers warning banner
- severe hypo scenario triggers emergency UI state

### Acceptance check

- new tests pass
- existing advisor tests remain green

---

## 4. File-Level Implementation Checklist

Expected touched areas:

- `src/MedicalAdvisor.Web/appsettings.json`
- `src/MedicalAdvisor.Web/Prompts/Diabetes2SystemPrompt.txt`
- `src/MedicalAdvisor.Web/docs/diabetes2/...`
- `src/MedicalAdvisor.Web/Models/ChatMessage.cs`
- `src/MedicalAdvisor.Web/Services/MedicalAdvisorService.cs`
- UI component that renders assistant messages and banners

Possible touched areas depending on current UI structure:

- advisor selector or welcome-card components
- session state if response flags are stored persistently

---

## 5. Suggested Agent Session Breakdown

### Session 1

Prepare the normalized plaintext knowledge file and add the advisor config plus prompt file.

### Session 2

Implement backend metadata parsing and message model flags.

### Session 3

Implement UI warning banner and emergency call button.

### Session 4

Add and run tests, then tune prompt wording and generation settings.

This split keeps each session small enough to verify before proceeding.

---

## 6. Definition of Done

The implementation is complete when:

1. Diabeticky poradce 2 is available in the app and routable by slug.
2. It uses a dedicated normalized plaintext document in full-context mode.
3. It answers confidently without document-reference or refusal-style wording.
4. It can use high-confidence general knowledge in addition to the document.
5. It switches language immediately when the user switches language.
6. It may provide insulin dose suggestions.
7. Dose-related answers trigger the warning banner.
8. Severe acute situations trigger the emergency call UI.
9. Existing advisors remain behaviorally unchanged.
10. Automated tests cover the new backend parsing and advisor registration behavior.

---

## 7. Deferred Items

Explicitly deferred from this implementation:

- RAG for the new advisor
- food database integration
- web search
- color-aware docx parser enhancements
- formal medical confidence scoring system
- broad redesign of other advisor prompts
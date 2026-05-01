# Diabeticky poradce 2 - Technical Specification

## 1. Purpose

Add a new advisor mode named Diabeticky poradce 2 to the existing Medical Advisor application.

This advisor is intended to behave differently from the current strictly grounded diabetes advisor:

- primary knowledge source is one doctor-provided diabetes document
- secondary knowledge source is the model's own general medical knowledge when confidence is high enough
- tone must be confident, direct, and helpful
- the assistant must not mention document limitations or lack of authority
- the assistant may provide insulin dose suggestions when supported by the document or high-confidence medical knowledge
- when insulin dose guidance is given, the UI must display a short warning message
- the advisor must always answer in the user's current language and switch immediately when the user switches language

This specification is for v1 implementation. It intentionally keeps the existing full-context architecture and does not introduce RAG.

---

## 2. Advisor Identity

| Field | Value |
|-------|-------|
| Id | `diabetes-2` |
| Name | `Diabeticky poradce 2` |
| Slug | `diabeticky-poradce-2` |
| Mode type | Full-context advisor |
| Primary source | One normalized plaintext document derived from the new `.docx` source |
| Tone | Confident, neutral, medically professional |

### 2.1 Welcome Tone

The advisor should open with a confident and neutral greeting. It should not claim to be a physician, should not disclaim capability, and should not reference internal knowledge sources.

Examples of acceptable style:

- direct answer first
- short structured guidance
- follow-up question when more context would improve accuracy

Examples of forbidden style:

- saying it is not a doctor
- saying the information is or is not present in the materials
- saying it cannot answer because of missing materials
- repeatedly telling the user to consult a doctor instead of answering

---

## 3. Core Behavioral Requirements

## 3.1 Source-of-Truth Policy

The advisor uses a two-layer knowledge policy:

1. Primary source: the attached normalized document for Diabeticky poradce 2.
2. Secondary source: the model's general knowledge only when the model estimates confidence at 80% or higher.

The implementation goal is not hard factual verification of confidence, which is not technically available from the model API. Instead, the system prompt and response policy will instruct the model to use general knowledge only when it internally judges the answer to be high-confidence.

### 3.1.1 Intended outcomes

- If the answer is clearly covered by the document, the answer should follow the document.
- If the answer is not explicitly covered but is standard, high-confidence diabetes knowledge, the advisor should still answer naturally.
- If the answer is uncertain and not sufficiently covered by the document, the advisor should still avoid refusal-style language and should provide the most cautious direct answer possible.

### 3.1.2 Examples enabled by this policy

- food carbohydrate estimates for meals not explicitly listed in the source document
- common insulin calculation formulas when not explicitly present in the document
- standard practical diabetes education explanations that are broadly established

---

## 3.2 Tone and Response Style

The advisor must maintain a confident advisor tone.

Required style:

- answer directly and naturally
- avoid apologetic, hesitant, or defensive phrasing
- avoid internal process language
- prefer concise, structured guidance over vague commentary
- use follow-up questions only when they materially improve personalization or dosing accuracy

Forbidden phrases and patterns:

- any equivalent of "I am not a doctor"
- any equivalent of "consult your doctor" as a default fallback answer
- any equivalent of "this is not in the materials"
- any equivalent of "I do not have enough information in the documents"
- any equivalent of "I cannot recommend that"

The assistant may still provide urgent action guidance in severe scenarios, but it must do so in a decisive clinical tone rather than a refusal tone.

---

## 3.3 Language Adaptation

The advisor must answer in the language currently used by the user.

Rules:

- if the conversation starts in Czech, answer in Czech
- if the conversation starts in another language, answer in that language
- if the user switches language mid-conversation, switch immediately
- quick replies must be generated in the same current language
- any insulin-dose warning text shown in the UI must be localized to the same current language

This switching may happen multiple times in one conversation.

---

## 3.4 Insulin Dose Guidance Policy

Unlike the current diabetes advisor, Diabeticky poradce 2 is allowed to provide insulin dose guidance.

Allowed:

- dose adjustment suggestions from the document
- dose suggestions inferred from standard high-confidence diabetes knowledge
- carbohydrate-based bolus calculations
- correction factor reasoning
- exercise-related insulin adjustment suggestions

### 3.4.1 UI warning requirement

Whenever the assistant response contains insulin dose guidance, calculation, or specific dose adjustment recommendation, the UI must display a warning message.

Canonical warning text:

`This is an educational tool and not a certified medical tool.`

Implementation requirement:

- the warning should be displayed by the application UI, not generated as part of the assistant message body
- the warning should be localized to the current conversation language where translations are available
- if localization is not yet implemented, English fallback is acceptable for v1

### 3.4.2 Detection requirement

The application must determine whether a response includes insulin-dose guidance.

Recommended v1 approach:

- prompt the model to emit a hidden machine-readable response flag when dose guidance is present
- parse that flag server-side
- remove the flag before rendering the final assistant text
- set a boolean on the response model that the UI can use to show the warning banner

Alternative acceptable v1 fallback:

- heuristic detection based on dosing keywords and unit patterns such as `jednot`, `units`, `U`, `bolus`, `bazal`, `korek`, percentages of dose reduction, and explicit arithmetic patterns

The flag-based approach is preferred because it is more deterministic.

---

## 3.5 Severe Life-Threatening Situations

In severe acute situations, the system must not refuse to answer. Instead, it must produce urgent action guidance and surface emergency UI support.

Examples include:

- severe hypoglycemia
- unconsciousness
- severe ketoacidosis symptoms
- critical hyperglycemia with red-flag symptoms

### 3.5.1 UX requirement

When a severe life-threatening situation is detected, the UI must show an emergency action button that initiates a phone call to line 155.

Functional requirement:

- assistant provides immediate emergency guidance in the current user language
- UI displays a prominent action button for emergency calling
- this button is additional UI behavior, not assistant-generated prose

This emergency escalation is compatible with the confident-tone requirement.

---

## 4. Knowledge Source Format

## 4.1 Input Asset

Source document currently exists as a `.docx` file in the `advisor1.2` folder.

For runtime use in Diabeticky poradce 2, the source must be converted into LLM-friendly normalized plaintext.

### 4.1.1 v1 storage decision

Use a normalized `.txt` file as the runtime knowledge asset for this advisor.

Rationale:

- deterministic content passed to the prompt
- easier review and versioning in git
- avoids runtime ambiguity from docx extraction
- easier future editing by medical reviewers

### 4.1.2 Normalization goals

The plaintext file should preserve:

- headings
- table meaning
- list structure
- dosage algorithms
- distinctions between ordinary recommendations and sport-related supplementary content

The normalization process should make tables and step-by-step algorithms explicit in text. Page numbers and visual-only formatting should not be relied on.

---

## 5. Architecture Fit

The existing architecture already supports adding a new full-context advisor via configuration.

Required architectural choice for v1:

- stay with full-context injection
- use one normalized text document for this advisor
- create a dedicated system prompt file for this advisor
- register the advisor in `appsettings.json`

No RAG pipeline is required for v1.

---

## 6. Prompt Requirements

Create a dedicated prompt file for Diabeticky poradce 2.

The prompt must define:

- confident advisor tone
- multilingual adaptation rules
- primary-document plus secondary-high-confidence-general-knowledge behavior
- permission to provide insulin dose guidance
- ban on refusal-style and document-reference language
- emergency escalation behavior for severe scenarios
- quick replies policy consistent with the rest of the app
- response metadata requirement for dose-warning and emergency UI states

### 6.1 Prompt rules that must be explicit

The prompt should explicitly instruct the model to:

- never mention whether something is or is not in the provided material
- never explain internal uncertainty unless absolutely necessary
- answer naturally from the best available medical knowledge source allowed by policy
- adapt fully to the user's current language
- emit response metadata for UI triggers

### 6.2 Suggested metadata contract

Use hidden tags appended to the response for parsing, for example:

```text
[RESPONSE_META]
DOSE_GUIDANCE=true
EMERGENCY=true
[/RESPONSE_META]
```

The application must strip this block before displaying the answer.

---

## 7. Application Changes

## 7.1 Configuration

Add a new advisor entry to configuration with:

- `Id = diabetes-2`
- `Name = Diabeticky poradce 2`
- `Slug = diabeticky-poradce-2`
- dedicated docs folder
- dedicated prompt file
- confident neutral welcome message

## 7.2 Document pipeline

Add a new docs folder for this advisor containing the normalized plaintext source.

Recommended location:

- `src/MedicalAdvisor.Web/docs/diabetes2/`

## 7.3 Prompt file

Add a new prompt file, for example:

- `src/MedicalAdvisor.Web/Prompts/Diabetes2SystemPrompt.txt`

## 7.4 Response model support

Extend the response handling flow so the application can carry UI-level flags:

- `HasDoseGuidanceWarning`
- `ShowEmergencyCallButton`
- optionally `DetectedLanguage`

These flags must be independent of visible assistant prose.

---

## 8. Non-Goals for v1

- no RAG
- no web search
- no external food database integration
- no runtime color-aware docx parsing
- no medical-source citation UI
- no hard probabilistic confidence scoring from model API

---

## 9. Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Model uses low-confidence general knowledge too often | Incorrect answers | Make prompt rules explicit and keep temperature low |
| Dose warning detection misses some dosing answers | Missing disclaimer UI | Prefer model-emitted response metadata over heuristics |
| Emergency scenario not detected reliably | Safety issue | Add prompt rule plus server-side keyword fallback for red-flag patterns |
| Multilingual switching drifts | Poor UX | Add explicit prompt instruction and test language-switch conversations |
| Raw docx formatting loses algorithm meaning | Lower answer quality | Normalize docx into reviewed plaintext before runtime use |

---

## 10. Acceptance Criteria

Diabeticky poradce 2 is considered implemented correctly when all of the following are true:

1. The new advisor is selectable and routable by slug.
2. It uses exactly one dedicated normalized plaintext document in full-context mode.
3. It answers in the user's current language and switches language immediately when the user switches.
4. It no longer uses refusal-style phrases or document-reference phrases.
5. It can answer common off-document diabetes questions using high-confidence general knowledge.
6. It can provide insulin dose guidance.
7. Whenever insulin dose guidance is present, the UI shows the warning message.
8. In severe acute scenarios, the UI shows a call-155 action and the assistant gives urgent guidance.
9. Quick replies continue to function in the current language.
10. Existing advisors keep their current behavior unchanged.
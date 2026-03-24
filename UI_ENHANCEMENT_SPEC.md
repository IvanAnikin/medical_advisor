# UI Enhancement Spec -- Modern Chat Experience

**Branch:** `ui-enhancements`
**Base:** `main`
**Goal:** Transform the current functional chatbot into a modern, soft, patient-friendly experience with clickable quick-reply options, smooth animations, and a refined color palette.

---

## Table of Contents

- [Design Philosophy](#design-philosophy)
- [Current State Analysis](#current-state-analysis)
- [Visual Design System](#visual-design-system)
- [Feature Spec: Quick-Reply Buttons](#feature-spec-quick-reply-buttons)
- [Feature Spec: Visual Polish](#feature-spec-visual-polish)
- [Feature Spec: Animations & Transitions](#feature-spec-animations--transitions)
- [Feature Spec: Enhanced Theming](#feature-spec-enhanced-theming)
- [Feature Spec: Input Area Redesign](#feature-spec-input-area-redesign)
- [Feature Spec: Welcome Experience](#feature-spec-welcome-experience)
- [Architecture Changes](#architecture-changes)
- [Files to Modify](#files-to-modify)
- [Files to Create](#files-to-create)
- [Implementation Plan](#implementation-plan)
- [Testing Plan](#testing-plan)

---

## Design Philosophy

The target audience is **Czech diabetes patients** -- often older adults, not necessarily tech-savvy. Every design decision must prioritize:

1. **Simplicity** -- Clean, uncluttered, obvious what to do next
2. **Comfort** -- Warm colors, soft shapes, gentle animations; nothing jarring or clinical
3. **Accessibility** -- Large touch targets, readable fonts, high contrast where it matters
4. **Guided interaction** -- Quick-reply buttons reduce typing, lower cognitive load, and guide patients through the conversation naturally
5. **Trust** -- Professional enough to be trusted with medical information, friendly enough to not intimidate

**Reference UX patterns:** GitHub Copilot Chat (VS Code), ChatGPT suggestion chips, Intercom Messenger, Ada Health chatbot.

---

## Current State Analysis

### What works well
- Streaming responses (token-by-token)
- Dual theme system (Clinical/Friendly)
- MudBlazor component foundation
- Clean separation of concerns (services, components)

### What needs improvement

| Area | Current | Target |
|------|---------|--------|
| Color palette | Saturated blue (#1565C0) / teal (#00897B) | Soft, muted pastels with warm neutrals |
| Message bubbles | Flat MudPaper with Elevation=1 | Soft shadows, larger radius, subtle gradient |
| Input area | Basic outlined MudTextField | Floating pill-shaped input with soft shadow |
| Interactions | Type-only | Quick-reply buttons + typing |
| Animations | None (instant render) | Fade-in messages, slide-up input, hover effects |
| Welcome | Static text blob | Animated greeting + clickable topic cards |
| Spacing | Tight (12px padding) | Generous whitespace, breathing room |
| Typography | Default MudBlazor | Refined hierarchy, lighter weights for body |
| App bar | Standard MudAppBar | Minimal floating header with subtle backdrop blur |
| Scrollbar | Browser default | Thin, custom-styled, unobtrusive |

---

## Visual Design System

### Color Palette -- "Soft Clinical"

A single unified palette that replaces both Clinical and Friendly themes with one modern, soft design. Two modes are kept but they now share the same design language -- just different accent hues.

#### Light Mode (Clinical)

| Token | Value | Usage |
|-------|-------|-------|
| `--bg-primary` | `#F8F9FC` | Page background -- barely blue-gray |
| `--bg-surface` | `#FFFFFF` | Cards, bubbles, input |
| `--bg-surface-hover` | `#F1F3F9` | Hover state for interactive elements |
| `--bg-user-bubble` | `#E8EDF6` | User message bubble -- soft blue-gray |
| `--bg-assistant-bubble` | `#FFFFFF` | Assistant message bubble -- clean white |
| `--accent` | `#5B7FD6` | Primary accent -- soft periwinkle blue |
| `--accent-light` | `#E8EDF6` | Accent backgrounds, selected states |
| `--accent-hover` | `#4A6BC4` | Accent hover state |
| `--text-primary` | `#1A1D26` | Main text -- near-black |
| `--text-secondary` | `#6B7280` | Timestamps, hints, secondary info |
| `--text-on-accent` | `#FFFFFF` | Text on accent-colored elements |
| `--border` | `#E5E7EB` | Subtle borders, dividers |
| `--shadow-soft` | `0 2px 12px rgba(0,0,0,0.06)` | Soft float shadow |
| `--shadow-medium` | `0 4px 20px rgba(0,0,0,0.08)` | Elevated elements |
| `--quick-reply-bg` | `#F1F3F9` | Quick-reply button background |
| `--quick-reply-border` | `#D4DAE8` | Quick-reply button border |
| `--quick-reply-hover` | `#E0E5F2` | Quick-reply button hover |

#### Light Mode (Friendly)

Same design system, but accent shifts to a soft sage green:

| Token | Value | Usage |
|-------|-------|-------|
| `--bg-primary` | `#F7FAF8` | Page background -- barely green |
| `--bg-user-bubble` | `#E6F0EB` | User message bubble -- soft sage |
| `--accent` | `#5BA88A` | Primary accent -- soft sage green |
| `--accent-light` | `#E6F0EB` | Accent backgrounds |
| `--accent-hover` | `#4A9478` | Accent hover state |
| `--quick-reply-bg` | `#EDF5F0` | Quick-reply button background |
| `--quick-reply-border` | `#C8DDD1` | Quick-reply button border |
| `--quick-reply-hover` | `#DEE9E2` | Quick-reply button hover |

All other tokens remain the same as Clinical.

### Typography

| Element | Font | Weight | Size |
|---------|------|--------|------|
| App title | Inter | 600 (semibold) | 18px |
| Message body | Inter | 400 (regular) | 15px |
| Bold in message | Inter | 600 (semibold) | 15px |
| Timestamp | Inter | 400 | 12px |
| Quick-reply button | Inter | 500 (medium) | 14px |
| Input placeholder | Inter | 400 | 15px |

### Border Radius

| Element | Radius |
|---------|--------|
| Message bubbles | 16px (4px on tail corner) |
| Quick-reply buttons | 20px (full pill) |
| Input field | 24px (full pill) |
| Cards / panels | 16px |
| App bar | 0 (edge to edge, but with backdrop blur) |

### Shadows

| Level | CSS | Usage |
|-------|-----|-------|
| Soft | `0 2px 12px rgba(0,0,0,0.06)` | Bubbles, quick-reply buttons |
| Medium | `0 4px 20px rgba(0,0,0,0.08)` | Input area, floating elements |
| Hover lift | `0 6px 24px rgba(0,0,0,0.10)` | Interactive hover states |

---

## Feature Spec: Quick-Reply Buttons

### Concept

After certain assistant messages, the UI shows **clickable pill-shaped buttons** below the message (similar to GitHub Copilot Chat suggestions). Clicking a button sends that text as the user's message -- identical to typing and pressing Enter.

Two types of quick-reply buttons:

### Type 1: Static Welcome Buttons (hardcoded)

Shown after the welcome message. These are the 4 topic entry points:

```
[ Lecba inzulinem ]  [ Monitorace glukozy (CGM) ]
[ Pece o nohy ]      [ Fyzicka aktivita ]
```

These are **not** generated by the AI. They are hardcoded in the component because the welcome message always offers the same 4 topics.

### Type 2: AI-Suggested Quick Replies (parsed from response)

The AI is instructed (via system prompt update) to end responses with suggested follow-up options in a structured format:

```
[QUICK_REPLIES]
Jak spravne aplikovat inzulin?
Jake jsou typy inzulinu?
Co delat pri hypoglykemii?
[/QUICK_REPLIES]
```

The `MedicalAdvisorService` parses this block out of the response, strips it from the displayed message, and stores the suggestions in the `ChatMessage` model. The UI renders them as clickable buttons.

If the AI doesn't include quick replies (e.g., it's asking a clarifying question), no buttons are shown -- the patient just types their answer.

### Button Behavior

1. Buttons appear with a staggered fade-in animation (50ms delay between each)
2. Clicking a button:
   - Sends the button text as the user message (same flow as typing + Enter)
   - All buttons disappear immediately
   - The selected text appears as a user bubble
3. Buttons are disabled during streaming
4. Max 4 buttons per message (AI is instructed to suggest 2-4 options)
5. Buttons wrap to next line on narrow screens

### Visual Design

```
+--------------------------------------------------+
|  Each button is a pill:                           |
|                                                   |
|  +-----------------------------------------+     |
|  |  Jak spravne aplikovat inzulin?          |     |
|  +-----------------------------------------+     |
|                                                   |
|  Background: var(--quick-reply-bg)                |
|  Border: 1px solid var(--quick-reply-border)      |
|  Border-radius: 20px                              |
|  Padding: 8px 16px                                |
|  Font: Inter 500, 14px                            |
|  Color: var(--accent)                             |
|  Shadow: var(--shadow-soft)                       |
|                                                   |
|  Hover:                                           |
|    Background: var(--quick-reply-hover)            |
|    Border-color: var(--accent)                    |
|    Shadow: var(--shadow-medium)                   |
|    Transform: translateY(-1px)                    |
|    Transition: all 0.2s ease                      |
+--------------------------------------------------+
```

---

## Feature Spec: Visual Polish

### Message Bubbles

**User bubbles:**
- Background: `var(--bg-user-bubble)` (soft blue-gray, NOT the saturated primary)
- Text color: `var(--text-primary)` (near-black, NOT white-on-blue)
- Shadow: `var(--shadow-soft)`
- Border-radius: 16px, bottom-right 4px
- No hard border

**Assistant bubbles:**
- Background: `var(--bg-assistant-bubble)` (white)
- Text color: `var(--text-primary)`
- Shadow: `var(--shadow-soft)`
- Border-radius: 16px, bottom-left 4px
- Subtle left accent border: `3px solid var(--accent)` (thin colored line on the left edge)

**Streaming indicator:**
- Replace blinking `|` cursor with 3 animated dots (typing indicator)
- Dots: 6px circles, color `var(--accent)`, sequential bounce animation
- Shown before any text arrives; once first token arrives, dots disappear and text streams in

### Timestamps
- Smaller (12px), lighter color (`var(--text-secondary)`)
- Appear on hover only (fade in 0.2s) -- saves space, reduces clutter
- Always visible on mobile (no hover)

### Avatar Indicators
- Small circular icon (24px) next to each message
- Assistant: a subtle medical cross or stethoscope icon (MudBlazor icon)
- User: simple person icon
- Color: `var(--text-secondary)` at 60% opacity -- subtle, not distracting

---

## Feature Spec: Animations & Transitions

All animations use CSS transitions and keyframes -- no JavaScript animation libraries needed. Respects `prefers-reduced-motion`.

| Animation | Trigger | CSS | Duration |
|-----------|---------|-----|----------|
| Message fade-in | New message added | `@keyframes fadeInUp` -- opacity 0->1, translateY(8px->0) | 0.3s ease-out |
| Quick-reply stagger | Buttons appear | Same as fade-in, but with `animation-delay: calc(index * 50ms)` | 0.3s + stagger |
| Quick-reply hover | Mouse enter | `transform: translateY(-1px); box-shadow: var(--shadow-medium)` | 0.2s ease |
| Input focus | Focus event | `box-shadow: 0 0 0 3px var(--accent-light); border-color: var(--accent)` | 0.2s ease |
| Streaming dots | During streaming | `@keyframes bounce` -- 3 dots sequential Y offset | 1.4s infinite |
| Theme switch | Theme change | `transition: background-color 0.3s ease, color 0.3s ease` on body/containers | 0.3s |
| Timestamp reveal | Hover on bubble | `opacity: 0 -> 1` | 0.2s ease |
| Send button | Hover | `transform: scale(1.05)` | 0.15s ease |
| Scroll-to-bottom FAB | When scrolled up | Floating button fades in, clicks to scroll to latest message | 0.2s |

### Reduced Motion

```css
@media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
        animation-duration: 0.01ms !important;
        animation-iteration-count: 1 !important;
        transition-duration: 0.01ms !important;
    }
}
```

---

## Feature Spec: Enhanced Theming

### Redesigned MudTheme Definitions

Replace the current saturated themes with the soft palette defined above. Both themes share the same structural design (shadows, radii, spacing) but differ only in accent hue.

**ThemeService.cs changes:**

```csharp
// Clinical: soft periwinkle blue accent
PaletteLight = new PaletteLight
{
    Primary = "#5B7FD6",          // soft blue
    Secondary = "#8DA4E2",        // lighter blue
    AppbarBackground = "#FFFFFF", // white app bar (not colored)
    AppbarText = "#1A1D26",       // dark text on white app bar
    Background = "#F8F9FC",       // barely blue-gray
    Surface = "#FFFFFF",
    TextPrimary = "#1A1D26",
    TextSecondary = "#6B7280",
    DrawerBackground = "#FFFFFF",
    LinesDefault = "#E5E7EB",
};

// Friendly: soft sage green accent
PaletteLight = new PaletteLight
{
    Primary = "#5BA88A",          // soft sage
    Secondary = "#82C4A8",        // lighter sage
    AppbarBackground = "#FFFFFF",
    AppbarText = "#1A1D26",
    Background = "#F7FAF8",       // barely green
    Surface = "#FFFFFF",
    TextPrimary = "#1A1D26",
    TextSecondary = "#6B7280",
    DrawerBackground = "#FFFFFF",
    LinesDefault = "#E5E7EB",
};
```

### App Bar Redesign

- White background (both themes) with subtle bottom shadow
- Backdrop blur effect: `backdrop-filter: blur(12px); background: rgba(255,255,255,0.85);`
- Colored accent only on the app title text (theme color)
- Theme switcher becomes two small pill buttons with icons (palette icon)

---

## Feature Spec: Input Area Redesign

### Current
Basic `MudTextField` (Outlined variant) with a separate Send icon button.

### Target
Floating pill-shaped input container:

```
+------------------------------------------------------------------+
|                                                                    |
|   +------------------------------------------------------------+  |
|   |  (icon)  Napiste svou zpravu...              (send button) |  |
|   +------------------------------------------------------------+  |
|                                                                    |
+------------------------------------------------------------------+
```

- Outer container: subtle top shadow (instead of hard border-top), transparent background
- Input wrapper: pill shape (border-radius: 24px), white background, soft shadow
- Chat icon on the left inside the pill (subtle, secondary color)
- Send button integrated inside the pill on the right
- Send button: circular, accent color, subtle scale on hover
- On focus: accent-colored ring glow (`box-shadow: 0 0 0 3px var(--accent-light)`)
- Auto-grow up to 5 lines (already implemented with `Sizing="InputSizing.Auto"`)

---

## Feature Spec: Welcome Experience

### Current
Static text blob with bullet points in the welcome message.

### Target
Replace the text-only welcome with a visual card layout:

1. **Greeting text** (shorter): "Dobry den! Jsem vas diabetologicky poradce."
2. **Topic cards** -- 4 clickable cards in a 2x2 grid (or horizontal scroll on mobile):

```
+---------------------------+  +---------------------------+
|  (syringe icon)            |  |  (monitor icon)           |
|  Lecba inzulinem           |  |  Monitorace glukozy       |
|  Zahajeni, typy, davkovani |  |  Systemy, interpretace    |
+---------------------------+  +---------------------------+
+---------------------------+  +---------------------------+
|  (foot icon)               |  |  (running icon)           |
|  Pece o nohy               |  |  Fyzicka aktivita         |
|  Prevence, rizika          |  |  Doporuceni, typy         |
+---------------------------+  +---------------------------+
```

Each card:
- Background: `var(--bg-surface)`
- Border: `1px solid var(--border)`
- Border-radius: 16px
- Shadow: `var(--shadow-soft)`
- Hover: shadow lifts, subtle border-color change to accent
- Click: sends the topic name as user message (same as typing it)
- Icon: MudBlazor Material icon, colored with accent
- Staggered fade-in animation on load (100ms between cards)

After any card is clicked, the cards disappear (they served their purpose) and the normal chat flow begins.

---

## Architecture Changes

### Model Changes

**`ChatMessage.cs`** -- Add quick-reply support:

```csharp
public class ChatMessage
{
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsStreaming { get; set; }
    public List<string> QuickReplies { get; set; } = [];  // NEW
    public bool ShowQuickReplies { get; set; } = true;     // NEW - false after one is clicked
}
```

### Service Changes

**`MedicalAdvisorService.cs`** -- Parse `[QUICK_REPLIES]...[/QUICK_REPLIES]` block from AI response:

```csharp
// After streaming completes, parse quick replies from the full response
var (cleanContent, quickReplies) = ParseQuickReplies(fullResponse.ToString());
state.ChatHistory.AddAssistantMessage(cleanContent);
// Return quick replies to the caller via a callback or the streaming message
```

New private method:

```csharp
private static (string Content, List<string> QuickReplies) ParseQuickReplies(string response)
{
    var match = Regex.Match(response, @"\[QUICK_REPLIES\]\s*(.*?)\s*\[/QUICK_REPLIES\]", RegexOptions.Singleline);
    if (!match.Success)
        return (response, []);

    var content = response[..match.Index].TrimEnd();
    var replies = match.Groups[1].Value
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Take(4)
        .ToList();

    return (content, replies);
}
```

**`SystemPrompt.txt`** -- Add quick-reply instruction:

```
=== FORMAT NAVRHU ===
Na konci kazde odpovedi pridej 2-4 navrhy dalsich otazek, ktere by pacienta mohly zajimat.
Pouzij tento format (VZDY na konci odpovedi, pokud nepolozis otazku):

[QUICK_REPLIES]
Prvni navrzena otazka
Druha navrzena otazka
Treti navrzena otazka
[/QUICK_REPLIES]

Pokud pokládáš pacientovi otázku a čekáš na jeho odpověď, NEPŘIDÁVEJ navrhované odpovědi.
```

### Component Changes

**New component: `QuickReplyButtons.razor`**
- Renders a list of pill-shaped buttons
- Each button click fires an `EventCallback<string>` with the button text
- Staggered fade-in CSS animation
- Disabled during streaming

**New component: `WelcomeCards.razor`**
- Renders the 4 topic cards in a grid
- Each card click fires an `EventCallback<string>` with the topic name
- Staggered fade-in animation
- Disappears after first click

**New component: `TypingIndicator.razor`**
- 3 animated bouncing dots
- Shown as a standalone mini-bubble when streaming starts, before first token arrives

**New component: `ScrollToBottomFab.razor`**
- Floating action button that appears when user scrolls up
- Clicking scrolls to the latest message
- Fade in/out animation

---

## Files to Modify

| File | Changes |
|------|---------|
| `Models/ChatMessage.cs` | Add `QuickReplies` list and `ShowQuickReplies` bool |
| `Services/MedicalAdvisorService.cs` | Parse `[QUICK_REPLIES]` block from AI response |
| `Services/ThemeService.cs` | Replace palettes with soft color system |
| `Services/ConversationState.cs` | Shorten welcome message (cards handle topic listing) |
| `Prompts/SystemPrompt.txt` | Add quick-reply format instructions |
| `Components/Pages/Home.razor` | Integrate quick-reply buttons, welcome cards, typing indicator, scroll FAB, new input design |
| `Components/Shared/ChatMessageBubble.razor` | New bubble styling, avatar icons, hover timestamps, accent border |
| `Components/Shared/ThemeSwitcher.razor` | Redesign as pill buttons with icons |
| `Components/Layout/MainLayout.razor` | White app bar with backdrop blur, accent-colored title |
| `Components/App.razor` | Add CSS custom properties, updated font weights |
| `wwwroot/app.css` | Complete rewrite with new design system, animations, custom properties |

## Files to Create

| File | Purpose |
|------|---------|
| `Components/Shared/QuickReplyButtons.razor` | Clickable pill-shaped suggestion buttons |
| `Components/Shared/WelcomeCards.razor` | 4-topic card grid for welcome screen |
| `Components/Shared/TypingIndicator.razor` | 3-dot bounce animation during streaming |
| `Components/Shared/ScrollToBottomFab.razor` | Floating button to scroll to latest message |

---

## Implementation Plan

### Phase 1: Design System Foundation (CSS + Themes)
**Estimated time: 1-2 hours**

| Step | Task | File(s) |
|------|------|---------|
| 1.1 | Rewrite `app.css` with CSS custom properties for the new color palette, shadows, radii, and animations | `wwwroot/app.css` |
| 1.2 | Update `ThemeService.cs` with new soft palettes (Clinical: periwinkle, Friendly: sage) | `Services/ThemeService.cs` |
| 1.3 | Update `App.razor` to set CSS custom properties based on active theme | `Components/App.razor` |
| 1.4 | Add `prefers-reduced-motion` media query | `wwwroot/app.css` |
| 1.5 | Build and verify themes switch correctly | -- |

### Phase 2: Message Bubble Redesign
**Estimated time: 1 hour**

| Step | Task | File(s) |
|------|------|---------|
| 2.1 | Redesign `ChatMessageBubble.razor` -- new colors, shadows, avatar icons, accent border on assistant | `Components/Shared/ChatMessageBubble.razor` |
| 2.2 | Add fade-in animation to new messages | `wwwroot/app.css` |
| 2.3 | Add hover-to-show timestamp behavior | `Components/Shared/ChatMessageBubble.razor`, `wwwroot/app.css` |
| 2.4 | Create `TypingIndicator.razor` (3 bouncing dots) | `Components/Shared/TypingIndicator.razor` |
| 2.5 | Integrate typing indicator into `Home.razor` streaming flow | `Components/Pages/Home.razor` |

### Phase 3: Quick-Reply Buttons
**Estimated time: 2 hours**

| Step | Task | File(s) |
|------|------|---------|
| 3.1 | Add `QuickReplies` and `ShowQuickReplies` to `ChatMessage.cs` | `Models/ChatMessage.cs` |
| 3.2 | Update `SystemPrompt.txt` with quick-reply format instructions | `Prompts/SystemPrompt.txt` |
| 3.3 | Add `ParseQuickReplies()` method to `MedicalAdvisorService.cs` | `Services/MedicalAdvisorService.cs` |
| 3.4 | Wire parsing into streaming completion flow | `Services/MedicalAdvisorService.cs` |
| 3.5 | Create `QuickReplyButtons.razor` component | `Components/Shared/QuickReplyButtons.razor` |
| 3.6 | Integrate quick-reply buttons into `Home.razor` (below each assistant message) | `Components/Pages/Home.razor` |
| 3.7 | Add staggered fade-in CSS for buttons | `wwwroot/app.css` |

### Phase 4: Welcome Experience
**Estimated time: 1.5 hours**

| Step | Task | File(s) |
|------|------|---------|
| 4.1 | Create `WelcomeCards.razor` -- 4-topic card grid with icons | `Components/Shared/WelcomeCards.razor` |
| 4.2 | Shorten welcome message in `ConversationState.cs` | `Services/ConversationState.cs` |
| 4.3 | Add welcome card rendering to `Home.razor` (shown after welcome message, hidden after first interaction) | `Components/Pages/Home.razor` |
| 4.4 | Add staggered card animation CSS | `wwwroot/app.css` |

### Phase 5: Input Area & App Bar Redesign
**Estimated time: 1 hour**

| Step | Task | File(s) |
|------|------|---------|
| 5.1 | Redesign input area in `Home.razor` -- pill shape, integrated send button, focus glow | `Components/Pages/Home.razor`, `wwwroot/app.css` |
| 5.2 | Redesign `MainLayout.razor` -- white app bar with backdrop blur, accent title | `Components/Layout/MainLayout.razor` |
| 5.3 | Redesign `ThemeSwitcher.razor` -- pill buttons with palette icons | `Components/Shared/ThemeSwitcher.razor` |
| 5.4 | Create `ScrollToBottomFab.razor` and integrate into `Home.razor` | `Components/Shared/ScrollToBottomFab.razor` |

### Phase 6: Testing & Polish
**Estimated time: 1-2 hours**

| Step | Task | File(s) |
|------|------|---------|
| 6.1 | Update existing unit tests for model changes (`ChatMessage` new properties) | `tests/` |
| 6.2 | Add unit tests for `ParseQuickReplies()` method | `tests/Services/MedicalAdvisorServiceTests.cs` |
| 6.3 | Add unit tests for updated `ConversationState` welcome message | `tests/Services/ConversationStateTests.cs` |
| 6.4 | Add unit tests for updated `ThemeService` palette values | `tests/Services/ThemeServiceTests.cs` |
| 6.5 | Manual testing: full conversation flow with quick replies | -- |
| 6.6 | Manual testing: theme switching, animations, mobile viewport | -- |
| 6.7 | Build, test, redeploy to Azure | -- |

### Total Estimated Time: 7-10 hours

### Phase Dependency Graph

```
Phase 1 (CSS + Themes)
   |
   +-- Phase 2 (Bubbles)
   |      |
   |      +-- Phase 3 (Quick Replies)
   |      |      |
   |      |      +-- Phase 4 (Welcome)
   |      |
   |      +-- Phase 5 (Input + AppBar)
   |
   +-- Phase 6 (Testing) -- depends on all above
```

Phase 1 must go first (design system). Phases 2-5 depend on Phase 1 but can partially overlap. Phase 6 is last.

---

## Testing Plan

### Automated Tests (xUnit)

| Test | What it verifies |
|------|-----------------|
| `ParseQuickReplies_ExtractsReplies` | Parses `[QUICK_REPLIES]...[/QUICK_REPLIES]` block correctly |
| `ParseQuickReplies_ReturnsEmpty_WhenNoBlock` | No block present -> empty list, content unchanged |
| `ParseQuickReplies_LimitsTo4` | More than 4 suggestions -> only first 4 returned |
| `ParseQuickReplies_StripsBlockFromContent` | Displayed content does not contain the block markers |
| `ParseQuickReplies_HandlesEmptyBlock` | Empty block -> empty list |
| `ChatMessage_QuickReplies_DefaultsToEmpty` | New ChatMessage has empty QuickReplies list |
| `ChatMessage_ShowQuickReplies_DefaultsToTrue` | New ChatMessage has ShowQuickReplies = true |
| `ThemeService_ClinicalPrimary_IsSoftBlue` | `#5B7FD6` (new palette) |
| `ThemeService_FriendlyPrimary_IsSoftSage` | `#5BA88A` (new palette) |
| `ThemeService_AppBarBackground_IsWhite` | Both themes have white app bar |
| `ConversationState_WelcomeMessage_IsShorter` | Updated welcome text is present |

### Manual Tests

| Test | Steps | Expected |
|------|-------|----------|
| Welcome cards | Open app fresh | See greeting + 4 clickable topic cards with icons |
| Card click | Click "Lecba inzulinem" card | Cards disappear, user bubble "Lecba inzulinem" appears, AI responds |
| Quick-reply buttons | Wait for AI response with suggestions | 2-4 pill buttons appear below the message with fade-in |
| Button click | Click a quick-reply button | Buttons disappear, selected text appears as user message, AI responds |
| Typing indicator | Send a message | 3 bouncing dots appear before first token; dots disappear when text starts |
| Theme switch | Toggle Clinical/Friendly | Colors transition smoothly (0.3s), accent changes from blue to sage |
| Animations | Send several messages | Each new message fades in from below |
| Hover effects | Hover over buttons/bubbles | Subtle shadow lift, timestamp appears |
| Reduced motion | Enable OS reduced-motion | All animations are instant, no visual motion |
| Mobile viewport | Resize to 375px width | Cards stack vertically, buttons wrap, input remains usable |
| Scroll FAB | Scroll up in long conversation | Floating "scroll to bottom" button appears; clicking scrolls to latest |

---

## Summary of All Changes

```
Modified files (11):
  .gitignore                              -- hardened secrets coverage
  src/.../Models/ChatMessage.cs           -- +QuickReplies, +ShowQuickReplies
  src/.../Services/MedicalAdvisorService  -- +ParseQuickReplies(), wire into stream
  src/.../Services/ThemeService.cs        -- soft palettes (periwinkle + sage)
  src/.../Services/ConversationState.cs   -- shorter welcome message
  src/.../Prompts/SystemPrompt.txt        -- +quick-reply format instructions
  src/.../Components/Pages/Home.razor     -- integrate all new components
  src/.../Components/Shared/ChatMessageBubble.razor  -- new design, avatars, hover
  src/.../Components/Shared/ThemeSwitcher.razor      -- pill redesign
  src/.../Components/Layout/MainLayout.razor         -- white app bar, blur
  src/.../wwwroot/app.css                -- complete rewrite

New files (4):
  src/.../Components/Shared/QuickReplyButtons.razor
  src/.../Components/Shared/WelcomeCards.razor
  src/.../Components/Shared/TypingIndicator.razor
  src/.../Components/Shared/ScrollToBottomFab.razor

New test file (1):
  tests/.../Services/MedicalAdvisorServiceTests.cs

Updated test files (3):
  tests/.../Services/ConversationStateTests.cs
  tests/.../Services/ThemeServiceTests.cs
  tests/.../Services/DocumentServiceTests.cs  (if needed)
```

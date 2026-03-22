# Medical Advisor

## Overview

Medical Advisor is a Czech-language web chatbot that provides AI-powered diabetes education and guidance to patients. The AI is strictly grounded in four verified medical documents and only provides information explicitly present in these sources. The application is designed as a single-page chatbot interface, guiding patients through structured conversations and providing targeted advice based on their responses.

**Production URL:** https://medical-advisor-cz.azurewebsites.net

---

## Features
- **Strict Document Grounding:** All responses are based solely on four official medical documents (no hallucination).
- **Conversational Chatbot:** Guides users with questions, provides targeted advice, and supports topic switching.
- **Streaming Responses:** Real-time, token-by-token message streaming for a natural chat experience.
- **Theming:** Switch between Clinical (blue) and Friendly (teal) UI themes.
- **No Authentication:** Anonymous, stateless sessions for privacy and simplicity.
- **Responsive UI:** Mobile-friendly, modern chat interface.
- **Error Handling:** Graceful handling of errors and unavailable topics.

---

## Architecture

- **Frontend:** ASP.NET 8 Blazor Server (MudBlazor for UI components and theming)
- **Backend:** .NET 8, Microsoft.SemanticKernel for AI orchestration
- **AI Backend:** Azure OpenAI (gpt-4-1-mini, West Europe)
- **Knowledge Base:** Four medical documents loaded at startup and injected into the system prompt
- **Hosting:** Azure App Service (B1 Basic, Linux)

### Component Diagram

```
[User] ⇄ [Blazor Server App] ⇄ [Semantic Kernel] ⇄ [Azure OpenAI]
           │
           └── [DocumentService: Loads 4 .txt docs]
```

---

## Knowledge Base Documents
- Začínáme s inzulínem (Insulin therapy basics)
- CGM — Kontinuální monitorace glukózy (Continuous glucose monitoring)
- Péče o nohy diabetikovy (Diabetic foot care)
- Doporučení pro fyzickou aktivitu u DM (Physical activity recommendations)

---

## How to Use
1. Open the app in your browser (local or production URL).
2. The bot introduces itself and lists available topics.
3. Type your question or select a topic.
4. The bot guides you with follow-up questions and provides advice strictly from the documents.
5. Switch topics or ask new questions at any time.

---

## Installation (Local Development)

### Prerequisites
- .NET 8 SDK
- Azure OpenAI API key (for local dev)

### Steps
1. Clone the repository.
2. Set your Azure OpenAI API key:
   ```bash
   cd src/MedicalAdvisor.Web
   dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
   ```
3. Run the app:
   ```bash
   dotnet run --project src/MedicalAdvisor.Web
   ```
4. Open http://localhost:5113 in your browser.

---

## Deployment

### Azure App Service (Production)
1. Create Azure resources (resource group, App Service plan, web app).
2. Configure app settings:
   - `AzureOpenAI__ApiKey`
   - `AzureOpenAI__Endpoint`
   - `AzureOpenAI__DeploymentName`
3. Publish and deploy:
   ```bash
   dotnet publish src/MedicalAdvisor.Web/MedicalAdvisor.Web.csproj -c Release -o ./publish
   cd publish && zip -r ../deploy.zip . && cd ..
   az webapp deploy --name medical-advisor-cz --resource-group medical-advisor-rg --src-path deploy.zip --type zip
   ```

### CI/CD (GitHub Actions)
- See `.github/workflows/deploy.yml` for automated deployment steps.

---

## Implementation Plan

1. **Project Scaffolding:** .NET 8 Blazor Server, MudBlazor, Semantic Kernel, config setup
2. **Document & AI Service:** Load documents, build system prompt, implement chat streaming
3. **Chat UI:** Build chat window, message bubbles, input, streaming, error handling
4. **Theming:** Clinical/Friendly themes, theme switcher
5. **Testing:** Unit tests for services and UI logic
6. **Deployment:** Azure App Service setup, publish, and deploy

---

## System Prompt & Grounding Rules
- All answers must be based on the four included documents
- No invented information, no diagnosis, no medication changes
- For unknown topics, the bot politely refuses and refers to a doctor
- Short, clear, conversational Czech responses

---

## Project Structure

```
medical_advisor/
├── src/MedicalAdvisor.Web/
│   ├── Components/           # Blazor UI components
│   ├── Services/             # Document, AI, Theme, Conversation state
│   ├── Models/               # ChatMessage, AppTheme
│   ├── docs/                 # Medical documents
│   ├── Prompts/              # SystemPrompt.txt
│   ├── wwwroot/              # Static assets, CSS
│   ├── appsettings.json      # Config (no secrets)
│   └── ...
├── tests/MedicalAdvisor.Tests/ # Unit tests
├── TECH_SPEC.md              # Technical specification
├── IMPLEMENTATION_STATUS.md  # Implementation progress
└── README.md                 # This file
```

---

## Testing
- 32 unit tests covering document loading, conversation state, and theming
- Run tests with:
  ```bash
  dotnet test
  ```

---

## Security & Privacy
- API keys are never stored in source code (user secrets for dev, Azure config for prod)
- No user data is persisted; sessions are ephemeral
- All content is strictly from bundled documents
- HTTPS enforced in production

---

## Extensibility
- Add new documents by placing .txt files in the `docs/` folder
- Future: RAG, voice input, multilingual, authentication, analytics, PDF export

---

## Authors & Credits
- Built by Ivan Anikin and contributors
- Uses Microsoft.SemanticKernel, MudBlazor, Azure OpenAI

---

## License
MIT License

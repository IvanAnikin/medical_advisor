using MedicalAdvisor.Web.Components;
using MedicalAdvisor.Web.Services;
using Microsoft.SemanticKernel;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// --- MudBlazor ---
builder.Services.AddMudServices();

// --- Blazor / Razor ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Azure OpenAI via Semantic Kernel ---
var aoaiSection = builder.Configuration.GetSection("AzureOpenAI");
var endpoint = aoaiSection["Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
var deploymentName = aoaiSection["DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");
var apiKey = aoaiSection["ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured. Use 'dotnet user-secrets set \"AzureOpenAI:ApiKey\" \"<key>\"' for local dev.");

builder.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);

// --- Azure OpenAI Embeddings (for RAG) ---
var embeddingDeployment = aoaiSection["EmbeddingDeploymentName"];
if (!string.IsNullOrEmpty(embeddingDeployment))
{
    builder.Services.AddAzureOpenAITextEmbeddingGeneration(
        embeddingDeployment, endpoint, apiKey);
    builder.Services.AddSingleton<PumpRagService>();
}

// --- Application services ---
builder.Services.AddSingleton<AdvisorRegistry>();
builder.Services.AddSingleton<DocumentService>();
builder.Services.AddScoped<MedicalAdvisorService>();
builder.Services.AddScoped<ConversationState>();
builder.Services.AddScoped<ThemeService>();

var app = builder.Build();

// Eagerly initialize AdvisorRegistry and DocumentService so config is validated
// and documents are loaded at startup.
app.Services.GetRequiredService<AdvisorRegistry>();
app.Services.GetRequiredService<DocumentService>();

// Initialize RAG index at startup (if embedding model is configured)
if (!string.IsNullOrEmpty(embeddingDeployment))
{
    var ragService = app.Services.GetRequiredService<PumpRagService>();
    await ragService.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Static files MUST run before routing so /{Slug} doesn't intercept .css/.js requests.
app.UseStaticFiles();
app.UseRouting();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

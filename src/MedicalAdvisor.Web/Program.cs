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

// --- Application services ---
builder.Services.AddSingleton<DocumentService>();
builder.Services.AddScoped<MedicalAdvisorService>();
builder.Services.AddScoped<ConversationState>();
builder.Services.AddScoped<ThemeService>();

var app = builder.Build();

// Eagerly initialize DocumentService so documents are loaded at startup.
app.Services.GetRequiredService<DocumentService>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

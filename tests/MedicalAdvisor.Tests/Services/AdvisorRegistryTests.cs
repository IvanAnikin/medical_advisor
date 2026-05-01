using MedicalAdvisor.Web.Services;
using Microsoft.Extensions.Configuration;

namespace MedicalAdvisor.Tests.Services;

public class AdvisorRegistryTests
{
    private static AdvisorRegistry CreateRegistry(Dictionary<string, string?> configValues)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new AdvisorRegistry(config);
    }

    private static Dictionary<string, string?> TwoAdvisorConfig() => new()
    {
        ["Advisors:0:Id"] = "diabetes",
        ["Advisors:0:Name"] = "Diabetologický poradce",
        ["Advisors:0:Slug"] = "diabetes",
        ["Advisors:0:Description"] = "Poradce pro diabetes",
        ["Advisors:0:Icon"] = "LocalHospital",
        ["Advisors:0:DocsFolder"] = "docs",
        ["Advisors:0:SystemPromptFile"] = "Prompts/SystemPrompt.txt",
        ["Advisors:0:IsDefault"] = "true",
        ["Advisors:0:WelcomeMessage"] = "Dobrý den!",
        ["Advisors:1:Id"] = "gestational-diabetes",
        ["Advisors:1:Name"] = "Gestační diabetes",
        ["Advisors:1:Slug"] = "gestacni-diabetes",
        ["Advisors:1:Description"] = "Poradce pro gestační diabetes",
        ["Advisors:1:Icon"] = "PregnantWoman",
        ["Advisors:1:DocsFolder"] = "docs/gestational",
        ["Advisors:1:SystemPromptFile"] = "Prompts/GestationalSystemPrompt.txt",
        ["Advisors:1:IsDefault"] = "false",
        ["Advisors:1:WelcomeMessage"] = "Dobrý den! Jsem poradce pro gestační diabetes.",
    };

    private static Dictionary<string, string?> ThreeAdvisorConfig()
    {
        var values = TwoAdvisorConfig();
        values["Advisors:2:Id"] = "diabetes-2";
        values["Advisors:2:Name"] = "Diabeticky poradce 2";
        values["Advisors:2:Slug"] = "diabeticky-poradce-2";
        values["Advisors:2:Description"] = "Sebevědomý poradce pro diabetes";
        values["Advisors:2:Icon"] = "LocalHospital";
        values["Advisors:2:DocsFolder"] = "docs/diabetes2";
        values["Advisors:2:SystemPromptFile"] = "Prompts/Diabetes2SystemPrompt.txt";
        values["Advisors:2:IsDefault"] = "false";
        values["Advisors:2:WelcomeMessage"] = "Dobrý den.";
        return values;
    }

    [Fact]
    public void GetAll_ReturnsAllConfiguredAdvisors()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var all = registry.GetAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetBySlug_ReturnsCorrectAdvisor()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var advisor = registry.GetBySlug("gestacni-diabetes");

        Assert.NotNull(advisor);
        Assert.Equal("gestational-diabetes", advisor.Id);
        Assert.Equal("Gestační diabetes", advisor.Name);
    }

    [Fact]
    public void GetBySlug_ReturnsNull_ForUnknownSlug()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var advisor = registry.GetBySlug("nonexistent");

        Assert.Null(advisor);
    }

    [Fact]
    public void GetBySlug_IsCaseInsensitive()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var advisor = registry.GetBySlug("DIABETES");

        Assert.NotNull(advisor);
        Assert.Equal("diabetes", advisor.Id);
    }

    [Fact]
    public void GetById_ReturnsCorrectAdvisor()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var advisor = registry.GetById("gestational-diabetes");

        Assert.NotNull(advisor);
        Assert.Equal("gestacni-diabetes", advisor.Slug);
    }

    [Fact]
    public void GetById_ReturnsDiabetes2Advisor()
    {
        var registry = CreateRegistry(ThreeAdvisorConfig());

        var advisor = registry.GetById("diabetes-2");

        Assert.NotNull(advisor);
        Assert.Equal("Diabeticky poradce 2", advisor.Name);
        Assert.Equal("diabeticky-poradce-2", advisor.Slug);
        Assert.Equal("docs/diabetes2", advisor.DocsFolder);
        Assert.Equal("Prompts/Diabetes2SystemPrompt.txt", advisor.SystemPromptFile);
    }

    [Fact]
    public void GetById_ReturnsNull_ForUnknownId()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var advisor = registry.GetById("unknown-id");

        Assert.Null(advisor);
    }

    [Fact]
    public void GetDefault_ReturnsAdvisorWithIsDefaultTrue()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var defaultAdvisor = registry.GetDefault();

        Assert.Equal("diabetes", defaultAdvisor.Id);
        Assert.True(defaultAdvisor.IsDefault);
    }

    [Fact]
    public void Constructor_Throws_WhenNoAdvisorsSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.Throws<InvalidOperationException>(() => new AdvisorRegistry(config));
    }

    [Fact]
    public void Constructor_Throws_WhenAdvisorsArrayIsEmpty()
    {
        // An empty Advisors section that resolves to an empty list
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Advisors:0:Id"] = null,
            })
            .Build();

        // This should throw because either no advisors or validation fails
        Assert.Throws<InvalidOperationException>(() => new AdvisorRegistry(config));
    }

    [Fact]
    public void Constructor_Throws_WhenNoDefault()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Advisors:0:Id"] = "diabetes",
            ["Advisors:0:Name"] = "Diabetologický poradce",
            ["Advisors:0:Slug"] = "diabetes",
            ["Advisors:0:DocsFolder"] = "docs",
            ["Advisors:0:SystemPromptFile"] = "Prompts/SystemPrompt.txt",
            ["Advisors:0:IsDefault"] = "false",
        };

        var ex = Assert.Throws<InvalidOperationException>(() => CreateRegistry(configValues));
        Assert.Contains("IsDefault=true", ex.Message);
    }

    [Fact]
    public void Constructor_Throws_WhenMultipleDefaults()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Advisors:0:Id"] = "a",
            ["Advisors:0:Name"] = "A",
            ["Advisors:0:Slug"] = "a",
            ["Advisors:0:DocsFolder"] = "docs",
            ["Advisors:0:SystemPromptFile"] = "Prompts/A.txt",
            ["Advisors:0:IsDefault"] = "true",
            ["Advisors:1:Id"] = "b",
            ["Advisors:1:Name"] = "B",
            ["Advisors:1:Slug"] = "b",
            ["Advisors:1:DocsFolder"] = "docs2",
            ["Advisors:1:SystemPromptFile"] = "Prompts/B.txt",
            ["Advisors:1:IsDefault"] = "true",
        };

        var ex = Assert.Throws<InvalidOperationException>(() => CreateRegistry(configValues));
        Assert.Contains("IsDefault=true", ex.Message);
    }

    [Fact]
    public void Constructor_Throws_WhenDuplicateSlugs()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Advisors:0:Id"] = "a",
            ["Advisors:0:Name"] = "A",
            ["Advisors:0:Slug"] = "same-slug",
            ["Advisors:0:DocsFolder"] = "docs",
            ["Advisors:0:SystemPromptFile"] = "Prompts/A.txt",
            ["Advisors:0:IsDefault"] = "true",
            ["Advisors:1:Id"] = "b",
            ["Advisors:1:Name"] = "B",
            ["Advisors:1:Slug"] = "same-slug",
            ["Advisors:1:DocsFolder"] = "docs2",
            ["Advisors:1:SystemPromptFile"] = "Prompts/B.txt",
            ["Advisors:1:IsDefault"] = "false",
        };

        var ex = Assert.Throws<InvalidOperationException>(() => CreateRegistry(configValues));
        Assert.Contains("slug", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_Throws_WhenDuplicateIds()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Advisors:0:Id"] = "same-id",
            ["Advisors:0:Name"] = "A",
            ["Advisors:0:Slug"] = "a",
            ["Advisors:0:DocsFolder"] = "docs",
            ["Advisors:0:SystemPromptFile"] = "Prompts/A.txt",
            ["Advisors:0:IsDefault"] = "true",
            ["Advisors:1:Id"] = "same-id",
            ["Advisors:1:Name"] = "B",
            ["Advisors:1:Slug"] = "b",
            ["Advisors:1:DocsFolder"] = "docs2",
            ["Advisors:1:SystemPromptFile"] = "Prompts/B.txt",
            ["Advisors:1:IsDefault"] = "false",
        };

        var ex = Assert.Throws<InvalidOperationException>(() => CreateRegistry(configValues));
        Assert.Contains("ID", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetAll_ReturnsReadOnlyList()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var all = registry.GetAll();

        Assert.IsAssignableFrom<IReadOnlyList<MedicalAdvisor.Web.Models.AdvisorConfig>>(all);
    }

    [Fact]
    public void Advisors_HaveCorrectProperties()
    {
        var registry = CreateRegistry(TwoAdvisorConfig());

        var diabetes = registry.GetById("diabetes");
        Assert.NotNull(diabetes);
        Assert.Equal("LocalHospital", diabetes.Icon);
        Assert.Equal("docs", diabetes.DocsFolder);
        Assert.Equal("Prompts/SystemPrompt.txt", diabetes.SystemPromptFile);

        var gestational = registry.GetById("gestational-diabetes");
        Assert.NotNull(gestational);
        Assert.Equal("PregnantWoman", gestational.Icon);
        Assert.Equal("docs/gestational", gestational.DocsFolder);
    }
}

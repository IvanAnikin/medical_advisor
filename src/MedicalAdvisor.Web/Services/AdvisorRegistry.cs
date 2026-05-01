using MedicalAdvisor.Web.Models;

namespace MedicalAdvisor.Web.Services;

public class AdvisorRegistry
{
    private readonly List<AdvisorConfig> _advisors;
    private readonly Dictionary<string, AdvisorConfig> _bySlug;
    private readonly Dictionary<string, AdvisorConfig> _byId;
    private readonly AdvisorConfig _default;

    public AdvisorRegistry(IConfiguration configuration)
    {
        _advisors = configuration.GetSection("Advisors").Get<List<AdvisorConfig>>()
            ?? throw new InvalidOperationException("No 'Advisors' section found in configuration.");

        if (_advisors.Count == 0)
            throw new InvalidOperationException("At least one advisor must be configured in the 'Advisors' section.");

        // Validate unique IDs
        var duplicateIds = _advisors.GroupBy(a => a.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Count > 0)
            throw new InvalidOperationException($"Duplicate advisor IDs found: {string.Join(", ", duplicateIds)}");

        // Validate unique slugs
        var duplicateSlugs = _advisors.GroupBy(a => a.Slug).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateSlugs.Count > 0)
            throw new InvalidOperationException($"Duplicate advisor slugs found: {string.Join(", ", duplicateSlugs)}");

        // Validate exactly one default
        var defaults = _advisors.Where(a => a.IsDefault).ToList();
        if (defaults.Count == 0)
            throw new InvalidOperationException("Exactly one advisor must have IsDefault=true. None found.");
        if (defaults.Count > 1)
            throw new InvalidOperationException($"Exactly one advisor must have IsDefault=true. Found {defaults.Count}: {string.Join(", ", defaults.Select(d => d.Id))}");

        _default = defaults[0];
        _bySlug = _advisors.ToDictionary(a => a.Slug, StringComparer.OrdinalIgnoreCase);
        _byId = _advisors.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AdvisorConfig> GetAll() => _advisors.AsReadOnly();

    public AdvisorConfig? GetBySlug(string slug)
    {
        _bySlug.TryGetValue(slug, out var advisor);
        return advisor;
    }

    public AdvisorConfig? GetById(string id)
    {
        _byId.TryGetValue(id, out var advisor);
        return advisor;
    }

    public AdvisorConfig GetDefault() => _default;
}

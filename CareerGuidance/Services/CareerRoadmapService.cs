using System.Text.Json;

public class CareerRoadmapService
{
    private readonly IWebHostEnvironment _env;

    public CareerRoadmapService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Dictionary<string, List<string>> LoadRoadmaps()
    {
        string path = Path.Combine(_env.WebRootPath, "data", "career_roadmaps.json");

        if (!File.Exists(path)) return new();

        var json = File.ReadAllText(path);

        // Use case-insensitive dictionary to simplify lookups
        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    public List<string> GetStepsForCareer(string goalTitle)
    {
        var roadmaps = LoadRoadmaps();

        if (string.IsNullOrWhiteSpace(goalTitle))
            return new List<string>();

        // Try exact match ignoring case
        var exactMatch = roadmaps
            .FirstOrDefault(kvp => kvp.Key.Equals(goalTitle, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(exactMatch.Key))
            return exactMatch.Value;

        // Try partial match
        var partialMatch = roadmaps
            .FirstOrDefault(kvp => goalTitle.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));

        return partialMatch.Value ?? new List<string>();
    }
}

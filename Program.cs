using System.Net.Http.Json;
using System.Text.Json;

// 1. Requirement: Provide the GitHub username as an argument
if (args.Length == 0)
{
    Console.WriteLine("Usage: github-activity <username>");
    return;
}

string username = args[0];
using HttpClient client = new();
client.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubActivity-CLI");

try
{
    // 2. Requirement: Fetch using the GitHub API endpoint
    string url = $"https://api.github.com/users/{username}/events";
    var events = await client.GetFromJsonAsync<JsonElement[]>(url);

    if (events == null || events.Length == 0)
    {
        Console.WriteLine("No recent activity found for this user.");
        return;
    }

    // 3. Requirement: Display the fetched activity in the terminal
    Console.WriteLine("Output:");
    foreach (var ev in events.Take(15)) // Limit natin sa 15 para hindi masyadong mahaba
    {
        string type = ev.GetProperty("type").GetString() ?? "";
        string repoName = ev.GetProperty("repo").GetProperty("name").GetString() ?? "unknown/repo";
        JsonElement payload = ev.GetProperty("payload");

        // Custom formatting para magmukhang professional ang output
        switch (type)
        {
            case "PushEvent":
                int commitCount = payload.TryGetProperty("commits", out var commits) ? commits.GetArrayLength() : 0;
                Console.WriteLine($"- Pushed {commitCount} commit(s) to {repoName}");
                break;

            case "IssuesEvent":
                string action = payload.GetProperty("action").GetString() ?? "interacted with";
                Console.WriteLine($"- {char.ToUpper(action[0]) + action.Substring(1)} a new issue in {repoName}");
                break;

            case "WatchEvent":
                Console.WriteLine($"- Starred {repoName}");
                break;

            case "CreateEvent":
                string refType = payload.GetProperty("ref_type").GetString() ?? "repository";
                Console.WriteLine($"- Created a new {refType} in {repoName}");
                break;

            default:
                // Fallback para sa ibang event types
                string eventName = type.Replace("Event", "");
                Console.WriteLine($"- {eventName} in {repoName}");
                break;
        }
    }
}
// Handle errors gracefully (Invalid usernames or API failures)
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine($"Error: The user '{username}' does not exist.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: Failed to fetch activity. {ex.Message}");
}
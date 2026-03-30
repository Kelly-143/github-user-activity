using System.Net.Http.Json;
using System.Text.Json;

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <username>");
    return;
}

string username = args[0];
using HttpClient client = new();
client.DefaultRequestHeaders.UserAgent.ParseAdd("GitHub-Activity-CLI");

try
{
    string url = $"https://api.github.com/users/{username}/events";
    var events = await client.GetFromJsonAsync<JsonElement[]>(url);

    if (events == null || events.Length == 0)
    {
        Console.WriteLine("No recent activity found.");
        return;
    }

    Console.WriteLine("Output:");
    foreach (var ev in events.Take(10)) // Requirements: show recent activity
    {
        string type = ev.GetProperty("type").GetString() ?? "";
        string repoName = ev.GetProperty("repo").GetProperty("name").GetString() ?? "";
        var payload = ev.GetProperty("payload");

        switch (type)
        {
            case "PushEvent":
                // FIXED LOGIC: Check 'size' first, then 'commits' array count
                int count = 0;
                if (payload.TryGetProperty("size", out var sizeProp))
                {
                    count = sizeProp.GetInt32();
                }
                else if (payload.TryGetProperty("commits", out var commitsProp))
                {
                    count = commitsProp.GetArrayLength();
                }

                // Final fallback: If it's a PushEvent, there's at least 1 commit
                if (count == 0) count = 1;

                Console.WriteLine($"- Pushed {count} commit(s) to {repoName}");
                break;

            case "IssuesEvent":
                string action = payload.GetProperty("action").GetString() ?? "";
                Console.WriteLine($"- {char.ToUpper(action[0]) + action.Substring(1)} an issue in {repoName}");
                break;

            case "WatchEvent":
                Console.WriteLine($"- Starred {repoName}");
                break;

            case "CreateEvent":
                Console.WriteLine($"- Created {repoName}");
                break;

            default:
                Console.WriteLine($"- {type.Replace("Event", "")} in {repoName}");
                break;
        }
    }
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine("Error: User not found.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
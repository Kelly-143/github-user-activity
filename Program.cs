using System.Net.Http.Json;
using System.Text.Json;

// 1. Requirement: Accept GitHub username as a command-line argument
if (args.Length == 0)
{
    Console.WriteLine("Usage: github-activity <username>");
    return;
}

string username = args[0];
using HttpClient client = new();

// GitHub API requires a User-Agent header
client.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubActivity-CLI-Kelly143");

try
{
    // 2. Requirement: Fetch activity from the GitHub API endpoint
    string url = $"https://api.github.com/users/{username}/events";

    // Kunin ang data as an array of JsonElements
    var events = await client.GetFromJsonAsync<JsonElement[]>(url);

    if (events == null || events.Length == 0)
    {
        Console.WriteLine($"No recent activity found for user: {username}");
        return;
    }

    // 3. Requirement: Display formatted activity in the terminal
    Console.WriteLine("Output:");
    foreach (var ev in events.Take(15)) // Limit to 15 recent events
    {
        string type = ev.GetProperty("type").GetString() ?? "";
        string repoName = ev.GetProperty("repo").GetProperty("name").GetString() ?? "unknown/repo";
        JsonElement payload = ev.GetProperty("payload");

        // Formatting logic based on event type
        switch (type)
        {
            case "PushEvent":
                // FIXED: Kukunin ang 'size' or bilangin ang 'commits' array para hindi mag-0
                int commitCount = 0;
                if (payload.TryGetProperty("size", out var size))
                {
                    commitCount = size.GetInt32();
                }
                else if (payload.TryGetProperty("commits", out var commits))
                {
                    commitCount = commits.GetArrayLength();
                }
                Console.WriteLine($"- Pushed {commitCount} commit(s) to {repoName}");
                break;

            case "IssuesEvent":
                string issueAction = payload.GetProperty("action").GetString() ?? "opened";
                Console.WriteLine($"- {char.ToUpper(issueAction[0]) + issueAction.Substring(1)} a new issue in {repoName}");
                break;

            case "WatchEvent":
                Console.WriteLine($"- Starred {repoName}");
                break;

            case "CreateEvent":
                string refType = payload.GetProperty("ref_type").GetString() ?? "repository";
                Console.WriteLine($"- Created a new {refType} in {repoName}");
                break;

            case "PullRequestEvent":
                string prAction = payload.GetProperty("action").GetString() ?? "opened";
                Console.WriteLine($"- {char.ToUpper(prAction[0]) + prAction.Substring(1)} a pull request in {repoName}");
                break;

            case "IssueCommentEvent":
                Console.WriteLine($"- Left a comment on an issue in {repoName}");
                break;

            default:
                // Fallback para sa ibang event types (MemberEvent, ForkEvent, etc.)
                string eventName = type.Replace("Event", "");
                Console.WriteLine($"- {eventName} in {repoName}");
                break;
        }
    }
}
// Error Handling: Kapag hindi nahanap ang user o may API issue
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine($"Error: User '{username}' not found. Please check the spelling.");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
{
    Console.WriteLine("Error: API rate limit exceeded. Please try again later.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: Something went wrong. {ex.Message}");
}
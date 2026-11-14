using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jarvis;

/// <summary>Automatic tools run immediately; Confirm tools wait for the user's approval (wiki/security.md).</summary>
public enum Permission { Automatic, Confirm }

/// <summary>A typed action the AI may request. Parameters is a JSON schema; Run receives the parsed arguments.</summary>
public sealed record Tool(string Name, string Description, Permission Permission, JsonObject Parameters,
                          Func<JsonElement, Task<string>> Run);

// The only path from AI or voice to an action: permission check, execution, then an audit log entry.
public sealed class ToolRegistry(IEnumerable<Tool> tools, IReadOnlyDictionary<string, (string Tool, string Arguments)> shortcuts,
                                 Func<Tool, string, Task<bool>> approve, MemoryStore audit)
{
    private readonly Dictionary<string, Tool> _tools = tools.ToDictionary(t => t.Name);

    /// <summary>Tool definitions in the OpenAI Chat Completions "tools" format.</summary>
    public JsonArray Definitions => new(_tools.Values.Select(t => (JsonNode)new JsonObject
    {
        ["type"] = "function",
        ["function"] = new JsonObject
        {
            ["name"] = t.Name,
            ["description"] = t.Description,
            ["parameters"] = t.Parameters.DeepClone(),
        },
    }).ToArray());

    public async Task<string> RunAsync(string name, string arguments)
    {
        if (!_tools.TryGetValue(name, out var tool)) return $"Error: there is no tool named '{name}'.";
        arguments = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
        var approved = false;
        string result;
        try
        {
            using var args = JsonDocument.Parse(arguments);
            approved = tool.Permission == Permission.Automatic || await approve(tool, arguments);
            result = approved ? await tool.Run(args.RootElement) : "The user declined this action. Do not retry it.";
        }
        catch (Exception ex)
        {
            // Any tool failure goes back to the AI as text so it can explain or try another way.
            result = $"Error: {ex.Message}";
        }
        await audit.LogToolAsync(name, arguments, approved, result);
        return result;
    }

    /// <summary>Runs an exact spoken/typed shortcut ("open notepad") without the AI; null if none matches.</summary>
    public async Task<string?> TryRunShortcutAsync(string input)
    {
        // Speech recognition adds punctuation ("Open notepad."), so strip it before matching.
        var text = input.Trim().TrimEnd('.', '!', '?').ToLowerInvariant();
        if (text.StartsWith("launch ")) text = "open " + text["launch ".Length..];
        return shortcuts.TryGetValue(text, out var call) ? await RunAsync(call.Tool, call.Arguments) : null;
    }
}

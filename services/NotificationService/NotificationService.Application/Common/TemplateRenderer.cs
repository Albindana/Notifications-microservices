using System.Text.RegularExpressions;

namespace NotificationService.Application.Common;

/// <summary>
/// Replaces {{Placeholder}} tokens in a template with values from the supplied data.
/// Unmatched placeholders are left untouched so missing data is visible rather than silent.
/// </summary>
public static class TemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

    public static string Render(string template, IReadOnlyDictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return data.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}

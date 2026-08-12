namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class MethodUsageReport
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("O");

    [JsonPropertyName("targetClassName")]
    public string TargetClassName { get; set; } = string.Empty;

    [JsonPropertyName("targetMethodName")]
    public string TargetMethodName { get; set; } = string.Empty;

    [JsonPropertyName("statistics")]
    public AnalysisStatistics Statistics { get; set; } = new();

    [JsonPropertyName("directTestUsages")]
    public List<DirectTestUsage> DirectTestUsages { get; set; } = new();

    [JsonPropertyName("lifecycleUsages")]
    public List<LifecycleUsage> LifecycleUsages { get; set; } = new();

    public void SaveAsJson(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        File.WriteAllText(filePath, JsonSerializer.Serialize(this, options));
    }

    public void SaveAsMarkdown(string filePath)
    {
        EnsureDirectory(filePath);

        var sb = new StringBuilder();
        sb.AppendLine("# Selenium NUnit Method Usage Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {Timestamp}");
        sb.AppendLine($"Target: `{TargetClassName}.{TargetMethodName}`");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Discovered C# files: {Statistics.DiscoveredFiles}");
        sb.AppendLine($"- Analyzed files: {Statistics.AnalyzedFiles}");
        sb.AppendLine($"- Test fixtures: {Statistics.TestFixtures}");
        sb.AppendLine($"- Test methods: {Statistics.TestMethods}");
        sb.AppendLine($"- Direct test usages: {DirectTestUsages.Count}");
        sb.AppendLine($"- Lifecycle usages: {LifecycleUsages.Count}");
        sb.AppendLine();

        sb.AppendLine("## Direct Test Usages");
        sb.AppendLine();
        if (DirectTestUsages.Count == 0)
        {
            sb.AppendLine("No direct test usages found.");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("| Test class | Test method | Invocation | Location |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var usage in DirectTestUsages.OrderBy(usage => usage.FilePath).ThenBy(usage => usage.Line))
            {
                sb.AppendLine($"| `{EscapeMarkdown(usage.TestClassName)}` | `{EscapeMarkdown(usage.TestMethodName)}` | `{EscapeMarkdown(usage.Invocation)}` | {EscapeMarkdown(Location(usage.FilePath, usage.Line))} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Lifecycle Usages");
        sb.AppendLine();
        if (LifecycleUsages.Count == 0)
        {
            sb.AppendLine("No lifecycle usages found.");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("| Test class | Lifecycle method | Kind | Invocation | Tests in class | Location |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var usage in LifecycleUsages.OrderBy(usage => usage.FilePath).ThenBy(usage => usage.Line))
            {
                string tests = string.Join(", ", usage.TestMethods.Select(test => $"`{EscapeMarkdown(test)}`"));
                sb.AppendLine($"| `{EscapeMarkdown(usage.TestClassName)}` | `{EscapeMarkdown(usage.LifecycleMethodName)}` | {EscapeMarkdown(usage.LifecycleKind)} | `{EscapeMarkdown(usage.Invocation)}` | {tests} | {EscapeMarkdown(Location(usage.FilePath, usage.Line))} |");
            }
            sb.AppendLine();
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public void SaveAsCsv(string filePath)
    {
        EnsureDirectory(filePath);

        var sb = new StringBuilder();
        sb.AppendLine("UsageType,FilePath,Line,TestClassName,TestMethodName,LifecycleMethodName,LifecycleKind,Invocation,TestsInClass");

        foreach (var usage in DirectTestUsages.OrderBy(usage => usage.FilePath).ThenBy(usage => usage.Line))
        {
            sb.AppendLine(string.Join(",",
                Csv("Direct"),
                Csv(usage.FilePath),
                usage.Line,
                Csv(usage.TestClassName),
                Csv(usage.TestMethodName),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(usage.Invocation),
                Csv(string.Empty)));
        }

        foreach (var usage in LifecycleUsages.OrderBy(usage => usage.FilePath).ThenBy(usage => usage.Line))
        {
            sb.AppendLine(string.Join(",",
                Csv("Lifecycle"),
                Csv(usage.FilePath),
                usage.Line,
                Csv(usage.TestClassName),
                Csv(string.Empty),
                Csv(usage.LifecycleMethodName),
                Csv(usage.LifecycleKind),
                Csv(usage.Invocation),
                Csv(string.Join("; ", usage.TestMethods))));
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    private static void EnsureDirectory(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string Csv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|");
    }

    private static string Location(string filePath, int line)
    {
        return $"{Path.GetFileName(filePath)}:{line}";
    }
}

public sealed class DirectTestUsage
{
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("testClassName")]
    public string TestClassName { get; set; } = string.Empty;

    [JsonPropertyName("testMethodName")]
    public string TestMethodName { get; set; } = string.Empty;

    [JsonPropertyName("invocation")]
    public string Invocation { get; set; } = string.Empty;
}

public sealed class LifecycleUsage
{
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("testClassName")]
    public string TestClassName { get; set; } = string.Empty;

    [JsonPropertyName("lifecycleMethodName")]
    public string LifecycleMethodName { get; set; } = string.Empty;

    [JsonPropertyName("lifecycleKind")]
    public string LifecycleKind { get; set; } = string.Empty;

    [JsonPropertyName("invocation")]
    public string Invocation { get; set; } = string.Empty;

    [JsonPropertyName("testMethods")]
    public List<string> TestMethods { get; set; } = new();
}

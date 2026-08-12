namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class CategoryReport
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("O");

    [JsonPropertyName("statistics")]
    public AnalysisStatistics Statistics { get; set; } = new();

    [JsonPropertyName("testCases")]
    public List<TestCaseCategoryInfo> TestCases { get; set; } = new();

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
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Selenium NUnit Category Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {Timestamp}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Discovered C# files: {Statistics.DiscoveredFiles}");
        sb.AppendLine($"- Analyzed files: {Statistics.AnalyzedFiles}");
        sb.AppendLine($"- Test fixtures: {Statistics.TestFixtures}");
        sb.AppendLine($"- Test methods: {Statistics.TestMethods}");
        sb.AppendLine($"- Test cases: {TestCases.Count}");
        sb.AppendLine($"- Without category: {TestCases.Count(testCase => testCase.Categories.Count == 0)}");
        sb.AppendLine($"- Distinct categories: {GetCategoryGroups().Count}");
        sb.AppendLine();

        sb.AppendLine("## Categories");
        sb.AppendLine();
        sb.AppendLine("| Category | Test cases |");
        sb.AppendLine("|---|---:|");
        foreach (var group in GetCategoryGroups())
        {
            sb.AppendLine($"| `{EscapeMarkdown(group.Key)}` | {group.Count()} |");
        }
        sb.AppendLine();

        var missing = TestCases.Where(testCase => testCase.Categories.Count == 0).ToList();
        if (missing.Count > 0)
        {
            sb.AppendLine("## Missing Categories");
            sb.AppendLine();
            AppendTestCaseTable(sb, missing);
            sb.AppendLine();
        }

        sb.AppendLine("## Test Cases");
        sb.AppendLine();
        foreach (var classGroup in TestCases
            .GroupBy(testCase => testCase.TestClassName)
            .OrderBy(group => group.Key))
        {
            sb.AppendLine($"### {EscapeMarkdown(classGroup.Key)}");
            sb.AppendLine();
            AppendTestCaseTable(sb, classGroup.OrderBy(testCase => testCase.Line));
            sb.AppendLine();
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public void SaveAsCsv(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sb = new StringBuilder();
        sb.AppendLine("FilePath,Line,TestClassName,TestMethodName,TestKind,Categories,Attribute");

        foreach (var testCase in TestCases.OrderBy(testCase => testCase.FilePath).ThenBy(testCase => testCase.Line))
        {
            sb.AppendLine(string.Join(",",
                Csv(testCase.FilePath),
                testCase.Line,
                Csv(testCase.TestClassName),
                Csv(testCase.TestMethodName),
                Csv(testCase.TestKind),
                Csv(string.Join("; ", testCase.Categories)),
                Csv(testCase.Attribute)));
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    private List<IGrouping<string, TestCaseCategoryInfo>> GetCategoryGroups()
    {
        return TestCases
            .SelectMany(testCase => testCase.Categories.Select(category => new { category, testCase }))
            .GroupBy(item => item.category, item => item.testCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .ToList();
    }

    private static void AppendTestCaseTable(StringBuilder sb, IEnumerable<TestCaseCategoryInfo> testCases)
    {
        sb.AppendLine("| Test | Kind | Categories | Location |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var testCase in testCases)
        {
            string categories = testCase.Categories.Count == 0
                ? "**missing**"
                : string.Join(", ", testCase.Categories.Select(category => $"`{EscapeMarkdown(category)}`"));
            string location = $"{Path.GetFileName(testCase.FilePath)}:{testCase.Line}";
            sb.AppendLine($"| `{EscapeMarkdown(testCase.TestMethodName)}` | {EscapeMarkdown(testCase.TestKind)} | {categories} | {EscapeMarkdown(location)} |");
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
}

public sealed class TestCaseCategoryInfo
{
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("testClassName")]
    public string TestClassName { get; set; } = string.Empty;

    [JsonPropertyName("testMethodName")]
    public string TestMethodName { get; set; } = string.Empty;

    [JsonPropertyName("testKind")]
    public string TestKind { get; set; } = string.Empty;

    [JsonPropertyName("attribute")]
    public string Attribute { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();
}

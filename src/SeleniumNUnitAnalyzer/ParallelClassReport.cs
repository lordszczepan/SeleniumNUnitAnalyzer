namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ParallelClassReport
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("O");

    [JsonPropertyName("statistics")]
    public AnalysisStatistics Statistics { get; set; } = new();

    [JsonPropertyName("classes")]
    public List<ParallelClassInfo> Classes { get; set; } = new();

    public void SaveAsJson(string filePath)
    {
        EnsureDirectory(filePath);
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
        sb.AppendLine("# Selenium NUnit Parallel Class Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {Timestamp}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Test classes: {Classes.Count}");
        sb.AppendLine($"- Class `[Parallelizable]`: {Classes.Count(item => item.IsParallelizable)}");
        sb.AppendLine($"- Class `[NonParallelizable]`: {Classes.Count(item => item.IsNonParallelizable)}");
        sb.AppendLine($"- Assembly-level parallelization: {Classes.Count(item => item.HasAssemblyParallelizable)}");
        int noEffectiveParallelization = Classes.Count(item => item.EffectiveParallelizationSource == "None");
        sb.AppendLine($"- No effective parallelization: {noEffectiveParallelization}");
        sb.AppendLine();
        AppendTable(sb, Classes.OrderBy(item => item.TestClassName));

        File.WriteAllText(filePath, sb.ToString());
    }

    public void SaveAsCsv(string filePath)
    {
        EnsureDirectory(filePath);
        var sb = new StringBuilder();
        sb.AppendLine("FilePath,Line,TestClassName,TestMethodCount,IsParallelizable,ParallelizableArguments,IsNonParallelizable,NonParallelizableArguments,HasAssemblyParallelizable,AssemblyParallelizableArguments,AssemblyLevelOfParallelism,EffectiveParallelizationSource");

        foreach (var item in Classes.OrderBy(item => item.FilePath).ThenBy(item => item.Line))
        {
            sb.AppendLine(string.Join(",",
                Csv(item.FilePath),
                item.Line,
                Csv(item.TestClassName),
                item.TestMethodCount,
                Csv(YesNo(item.IsParallelizable)),
                Csv(item.ParallelizableArguments),
                Csv(YesNo(item.IsNonParallelizable)),
                Csv(item.NonParallelizableArguments),
                Csv(YesNo(item.HasAssemblyParallelizable)),
                Csv(item.AssemblyParallelizableArguments),
                Csv(item.AssemblyLevelOfParallelism),
                Csv(item.EffectiveParallelizationSource)));
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    private static void AppendTable(StringBuilder sb, IEnumerable<ParallelClassInfo> classes)
    {
        sb.AppendLine("| Class | Tests | Parallelizable | Parallelizable args | NonParallelizable | Effective source | Location |");
        sb.AppendLine("|---|---:|---:|---|---:|---|---|");
        foreach (var item in classes)
        {
            string location = Path.GetFileName(item.FilePath) + ":" + item.Line;
            sb.AppendLine($"| `{EscapeMarkdown(item.TestClassName)}` | {item.TestMethodCount} | {YesNo(item.IsParallelizable)} | {EscapeMarkdown(item.ParallelizableArguments)} | {YesNo(item.IsNonParallelizable)} | {EscapeMarkdown(item.EffectiveParallelizationSource)} | {EscapeMarkdown(location)} |");
        }
    }

    private static void EnsureDirectory(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string YesNo(bool value)
    {
        return value ? "Yes" : "No";
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

public sealed class ParallelClassInfo
{
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("testClassName")]
    public string TestClassName { get; set; } = string.Empty;

    [JsonPropertyName("testMethodCount")]
    public int TestMethodCount { get; set; }

    [JsonPropertyName("isParallelizable")]
    public bool IsParallelizable { get; set; }

    [JsonPropertyName("parallelizableArguments")]
    public string ParallelizableArguments { get; set; } = string.Empty;

    [JsonPropertyName("isNonParallelizable")]
    public bool IsNonParallelizable { get; set; }

    [JsonPropertyName("nonParallelizableArguments")]
    public string NonParallelizableArguments { get; set; } = string.Empty;

    [JsonPropertyName("hasAssemblyParallelizable")]
    public bool HasAssemblyParallelizable { get; set; }

    [JsonPropertyName("assemblyParallelizableArguments")]
    public string AssemblyParallelizableArguments { get; set; } = string.Empty;

    [JsonPropertyName("assemblyLevelOfParallelism")]
    public string AssemblyLevelOfParallelism { get; set; } = string.Empty;

    [JsonPropertyName("effectiveParallelizationSource")]
    public string EffectiveParallelizationSource { get; set; } = string.Empty;
}

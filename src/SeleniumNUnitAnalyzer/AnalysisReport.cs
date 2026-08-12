namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class AnalysisReport
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("O");

    [JsonPropertyName("statistics")]
    public AnalysisStatistics Statistics { get; set; } = new();

    [JsonPropertyName("issues")]
    public List<AnalysisIssue> Issues { get; set; } = new();

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
}

public sealed class AnalysisStatistics
{
    [JsonPropertyName("discoveredFiles")]
    public int DiscoveredFiles { get; set; }

    [JsonPropertyName("analyzedFiles")]
    public int AnalyzedFiles { get; set; }

    [JsonPropertyName("testFixtures")]
    public int TestFixtures { get; set; }

    [JsonPropertyName("testMethods")]
    public int TestMethods { get; set; }
}

public sealed class AnalysisIssue
{
    [JsonPropertyName("ruleId")]
    public string RuleId { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Warning";

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("symbolName")]
    public string SymbolName { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

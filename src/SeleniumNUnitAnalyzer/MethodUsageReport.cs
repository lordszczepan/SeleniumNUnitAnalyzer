namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.IO;
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

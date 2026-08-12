namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ProductMatrixReport
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("O");

    [JsonPropertyName("statistics")]
    public AnalysisStatistics Statistics { get; set; } = new();

    [JsonPropertyName("products")]
    public List<string> Products { get; set; } = ProductCatalog.Products
        .Select(product => product.DisplayName)
        .ToList();

    [JsonPropertyName("testCases")]
    public List<ProductMatrixRow> TestCases { get; set; } = new();

    public static ProductMatrixReport FromCategoryReport(CategoryReport categoryReport, bool onlyMissingProducts)
    {
        var rows = categoryReport.TestCases
            .Select(CreateRow)
            .Where(row => !onlyMissingProducts || !row.HasAnyProduct)
            .ToList();

        return new ProductMatrixReport
        {
            Timestamp = categoryReport.Timestamp,
            Statistics = categoryReport.Statistics,
            TestCases = rows
        };
    }

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

    public void SaveAsCsv(string filePath)
    {
        EnsureDirectory(filePath);
        var sb = new StringBuilder();
        var productHeaders = ProductCatalog.Products.Select(product => product.DisplayName);
        sb.AppendLine("FilePath,Line,TestClassName,TestMethodName,TestKind,Categories,HasAnyProduct," +
            string.Join(",", productHeaders.Select(Csv)));

        foreach (var row in TestCases.OrderBy(row => row.FilePath).ThenBy(row => row.Line))
        {
            var values = new List<string>
            {
                Csv(row.FilePath),
                row.Line.ToString(),
                Csv(row.TestClassName),
                Csv(row.TestMethodName),
                Csv(row.TestKind),
                Csv(string.Join("; ", row.Categories)),
                Csv(row.HasAnyProduct ? "Yes" : "No")
            };

            values.AddRange(ProductCatalog.Products.Select(product =>
                Csv(row.Products.TryGetValue(product.DisplayName, out bool hasProduct) && hasProduct ? "Yes" : "No")));

            sb.AppendLine(string.Join(",", values));
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public void SaveAsMarkdown(string filePath, bool onlyMissingProducts)
    {
        EnsureDirectory(filePath);
        var sb = new StringBuilder();
        sb.AppendLine(onlyMissingProducts ? "# Missing Product Report" : "# Product Matrix Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {Timestamp}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Test cases in report: {TestCases.Count}");
        sb.AppendLine($"- Without product: {TestCases.Count(row => !row.HasAnyProduct)}");
        foreach (var product in ProductCatalog.Products)
        {
            int count = TestCases.Count(row => row.Products.TryGetValue(product.DisplayName, out bool hasProduct) && hasProduct);
            sb.AppendLine($"- {product.DisplayName}: {count}");
        }
        sb.AppendLine();

        sb.AppendLine("## Test Cases");
        sb.AppendLine();
        sb.Append("| Test class | Test method | Categories | Has product |");
        foreach (var product in ProductCatalog.Products)
        {
            sb.Append($" {product.DisplayName} |");
        }
        sb.AppendLine(" Location |");

        sb.Append("|---|---|---|---:|");
        foreach (var _ in ProductCatalog.Products)
        {
            sb.Append("---:|");
        }
        sb.AppendLine("---|");

        foreach (var row in TestCases.OrderBy(row => row.TestClassName).ThenBy(row => row.Line))
        {
            sb.Append($"| `{EscapeMarkdown(row.TestClassName)}` ");
            sb.Append($"| `{EscapeMarkdown(row.TestMethodName)}` ");
            sb.Append($"| {EscapeMarkdown(string.Join(", ", row.Categories))} ");
            sb.Append($"| {(row.HasAnyProduct ? "Yes" : "No")} ");
            foreach (var product in ProductCatalog.Products)
            {
                bool hasProduct = row.Products.TryGetValue(product.DisplayName, out bool value) && value;
                sb.Append($"| {(hasProduct ? "Yes" : "No")} ");
            }
            sb.AppendLine($"| {EscapeMarkdown(Path.GetFileName(row.FilePath) + ":" + row.Line)} |");
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    private static ProductMatrixRow CreateRow(TestCaseCategoryInfo testCase)
    {
        var matchedProducts = ProductCatalog.MatchProducts(testCase.Categories)
            .Select(product => product.DisplayName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new ProductMatrixRow
        {
            FilePath = testCase.FilePath,
            Line = testCase.Line,
            TestClassName = testCase.TestClassName,
            TestMethodName = testCase.TestMethodName,
            TestKind = testCase.TestKind,
            Categories = testCase.Categories,
            HasAnyProduct = matchedProducts.Count > 0,
            Products = ProductCatalog.Products.ToDictionary(
                product => product.DisplayName,
                product => matchedProducts.Contains(product.DisplayName))
        };
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
}

public sealed class ProductMatrixRow
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

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    [JsonPropertyName("hasAnyProduct")]
    public bool HasAnyProduct { get; set; }

    [JsonPropertyName("products")]
    public Dictionary<string, bool> Products { get; set; } = new();
}

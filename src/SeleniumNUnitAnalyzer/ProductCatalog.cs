namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.Linq;

public static class ProductCatalog
{
    public static IReadOnlyList<ProductDefinition> Products { get; } =
    [
        new("RCChange", "RC-Change", ["Tags.RCChange", "RCChange", "RC-Change"]),
        new("RCDMS", "RC-DMS", ["Tags.RCDMS", "RCDMS", "RC-DMS"]),
        new("RCDeviation", "RC-Deviation", ["Tags.RCDeviation", "RCDeviation", "RC-Deviation"]),
        new("RCQMS", "RC-QMS", ["Tags.RCQMS", "RCQMS", "RC-QMS"]),
        new("RCSLM", "RC-SLM", ["Tags.RCSLM", "RCSLM", "RC-SLM"]),
        new("RCTraining", "RC-Training", ["Tags.RCTraining", "RCTraining", "RC-Training"]),
        new("RCVLM", "RC-VLM", ["Tags.RCVLM", "RCVLM", "RC-VLM"])
    ];

    public static IReadOnlyList<ProductDefinition> MatchProducts(IEnumerable<string> categories)
    {
        var normalizedCategories = categories
            .Select(NormalizeCategory)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Products
            .Where(product => product.Matchers.Any(matcher => normalizedCategories.Contains(NormalizeCategory(matcher))))
            .ToList();
    }

    private static string NormalizeCategory(string category)
    {
        return category.Trim().Trim('"').Trim('\'');
    }
}

public sealed record ProductDefinition(
    string Key,
    string DisplayName,
    IReadOnlyList<string> Matchers);

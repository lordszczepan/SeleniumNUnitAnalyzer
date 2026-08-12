namespace SeleniumNUnitAnalyzer;

using System;
using System.IO;
using System.Linq;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            if (!options.IsValid(out string errorMessage))
            {
                Console.Error.WriteLine($"Error: {errorMessage}");
                PrintUsage();
                return 1;
            }

            var analyzer = new TestProjectAnalyzer(options);

            if (options.ParallelClasses)
            {
                var parallelReport = analyzer.ListParallelClasses();
                PrintParallelClassSummary(parallelReport);

                string parallelReportPath = options.ReportPath ??
                    Path.Combine(options.TargetDirectory, "SeleniumNUnitParallelClassReport.json");
                string parallelMarkdownPath = Path.ChangeExtension(parallelReportPath, ".md");
                string parallelCsvPath = Path.ChangeExtension(parallelReportPath, ".csv");
                TrySaveReport("JSON", parallelReportPath, () => parallelReport.SaveAsJson(parallelReportPath));
                TrySaveReport("Markdown", parallelMarkdownPath, () => parallelReport.SaveAsMarkdown(parallelMarkdownPath));
                TrySaveReport("CSV", parallelCsvPath, () => parallelReport.SaveAsCsv(parallelCsvPath));

                Console.WriteLine();
                Console.WriteLine($"JSON report path: {parallelReportPath}");
                Console.WriteLine($"Markdown report path: {parallelMarkdownPath}");
                Console.WriteLine($"CSV report path: {parallelCsvPath}");

                return parallelReport.Classes.Any(item => item.EffectiveParallelizationSource == "None") ? 2 : 0;
            }

            if (options.ProductMatrix || options.MissingProducts)
            {
                var categoryReport = analyzer.ListCategories();
                var productReport = ProductMatrixReport.FromCategoryReport(categoryReport, options.MissingProducts);
                PrintProductMatrixSummary(productReport, options.MissingProducts);

                string defaultFileName = options.MissingProducts
                    ? "SeleniumNUnitMissingProductsReport.json"
                    : "SeleniumNUnitProductMatrixReport.json";
                string productReportPath = options.ReportPath ??
                    Path.Combine(options.TargetDirectory, defaultFileName);
                string productMarkdownPath = Path.ChangeExtension(productReportPath, ".md");
                string productCsvPath = Path.ChangeExtension(productReportPath, ".csv");
                TrySaveReport("JSON", productReportPath, () => productReport.SaveAsJson(productReportPath));
                TrySaveReport("Markdown", productMarkdownPath, () => productReport.SaveAsMarkdown(productMarkdownPath, options.MissingProducts));
                TrySaveReport("CSV", productCsvPath, () => productReport.SaveAsCsv(productCsvPath));

                Console.WriteLine();
                Console.WriteLine($"JSON report path: {productReportPath}");
                Console.WriteLine($"Markdown report path: {productMarkdownPath}");
                Console.WriteLine($"CSV report path: {productCsvPath}");

                return options.MissingProducts && productReport.TestCases.Count > 0 ? 2 : 0;
            }

            if (options.ListCategories)
            {
                var categoryReport = analyzer.ListCategories();
                PrintCategoryReportSummary(categoryReport);

                string categoryReportPath = options.ReportPath ??
                    Path.Combine(options.TargetDirectory, "SeleniumNUnitCategoryReport.json");
                string categoryMarkdownPath = Path.ChangeExtension(categoryReportPath, ".md");
                string categoryCsvPath = Path.ChangeExtension(categoryReportPath, ".csv");
                TrySaveReport("JSON", categoryReportPath, () => categoryReport.SaveAsJson(categoryReportPath));
                TrySaveReport("Markdown", categoryMarkdownPath, () => categoryReport.SaveAsMarkdown(categoryMarkdownPath));
                TrySaveReport("CSV", categoryCsvPath, () => categoryReport.SaveAsCsv(categoryCsvPath));

                Console.WriteLine();
                Console.WriteLine($"JSON report path: {categoryReportPath}");
                Console.WriteLine($"Markdown report path: {categoryMarkdownPath}");
                Console.WriteLine($"CSV report path: {categoryCsvPath}");

                return categoryReport.TestCases.Any(testCase => testCase.Categories.Count == 0) ? 2 : 0;
            }

            if (options.FindUsages)
            {
                var usageReport = analyzer.FindMethodUsages();
                PrintUsageReportSummary(usageReport);

                string usageReportPath = options.ReportPath ??
                    Path.Combine(options.TargetDirectory, "SeleniumNUnitMethodUsageReport.json");
                string usageMarkdownPath = Path.ChangeExtension(usageReportPath, ".md");
                string usageCsvPath = Path.ChangeExtension(usageReportPath, ".csv");
                TrySaveReport("JSON", usageReportPath, () => usageReport.SaveAsJson(usageReportPath));
                TrySaveReport("Markdown", usageMarkdownPath, () => usageReport.SaveAsMarkdown(usageMarkdownPath));
                TrySaveReport("CSV", usageCsvPath, () => usageReport.SaveAsCsv(usageCsvPath));

                Console.WriteLine();
                Console.WriteLine($"JSON report path: {usageReportPath}");
                Console.WriteLine($"Markdown report path: {usageMarkdownPath}");
                Console.WriteLine($"CSV report path: {usageCsvPath}");

                return usageReport.DirectTestUsages.Count == 0 &&
                       usageReport.LifecycleUsages.Count == 0
                    ? 2
                    : 0;
            }

            var report = analyzer.Analyze();

            PrintReportSummary(report);

            string reportPath = options.ReportPath ??
                Path.Combine(options.TargetDirectory, "SeleniumNUnitAnalysisReport.json");
            report.SaveAsJson(reportPath);

            Console.WriteLine();
            Console.WriteLine($"Report saved to: {reportPath}");

            return report.Issues.Count == 0 ? 0 : 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintParallelClassSummary(ParallelClassReport report)
    {
        Console.WriteLine();
        Console.WriteLine("=== PARALLEL CLASS REPORT ===");
        Console.WriteLine();
        Console.WriteLine($"Test classes:              {report.Classes.Count}");
        Console.WriteLine($"Class Parallelizable:      {report.Classes.Count(item => item.IsParallelizable)}");
        Console.WriteLine($"Class NonParallelizable:   {report.Classes.Count(item => item.IsNonParallelizable)}");
        Console.WriteLine($"Assembly-level:            {report.Classes.Count(item => item.HasAssemblyParallelizable)}");
        Console.WriteLine($"No effective setting:      {report.Classes.Count(item => item.EffectiveParallelizationSource == "None")}");
    }

    private static void TrySaveReport(string format, string path, Action save)
    {
        try
        {
            save();
            Console.WriteLine($"{format} report saved.");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Warning: Could not save {format} report to '{path}'. {ex.Message}");
        }
    }

    private static void PrintProductMatrixSummary(ProductMatrixReport report, bool onlyMissingProducts)
    {
        Console.WriteLine();
        Console.WriteLine(onlyMissingProducts ? "=== MISSING PRODUCT REPORT ===" : "=== PRODUCT MATRIX REPORT ===");
        Console.WriteLine();
        Console.WriteLine($"Test cases in report:      {report.TestCases.Count}");
        Console.WriteLine($"Without product:           {report.TestCases.Count(row => !row.HasAnyProduct)}");
        foreach (var product in ProductCatalog.Products)
        {
            int count = report.TestCases.Count(row =>
                row.Products.TryGetValue(product.DisplayName, out bool hasProduct) && hasProduct);
            Console.WriteLine($"{product.DisplayName,-26}{count}");
        }
    }

    private static void PrintCategoryReportSummary(CategoryReport report)
    {
        Console.WriteLine();
        Console.WriteLine("=== TEST CATEGORY REPORT ===");
        Console.WriteLine();
        Console.WriteLine($"Discovered C# files:       {report.Statistics.DiscoveredFiles}");
        Console.WriteLine($"Analyzed files:            {report.Statistics.AnalyzedFiles}");
        Console.WriteLine($"Test fixtures:             {report.Statistics.TestFixtures}");
        Console.WriteLine($"Test methods:              {report.Statistics.TestMethods}");
        Console.WriteLine($"Test cases:                {report.TestCases.Count}");
        Console.WriteLine($"Without category:          {report.TestCases.Count(testCase => testCase.Categories.Count == 0)}");
        Console.WriteLine();

        foreach (var group in report.TestCases
            .GroupBy(testCase => testCase.TestClassName)
            .OrderBy(group => group.Key))
        {
            Console.WriteLine(group.Key);
            foreach (var testCase in group.OrderBy(testCase => testCase.Line))
            {
                string categories = testCase.Categories.Count == 0
                    ? "<missing>"
                    : string.Join(", ", testCase.Categories);
                Console.WriteLine($"  {testCase.TestMethodName} [{testCase.TestKind}] -> {categories}");
            }
        }
    }

    private static void PrintUsageReportSummary(MethodUsageReport report)
    {
        Console.WriteLine();
        Console.WriteLine("=== METHOD USAGE REPORT ===");
        Console.WriteLine();
        Console.WriteLine($"Target:                    {report.TargetClassName}.{report.TargetMethodName}");
        Console.WriteLine($"Discovered C# files:       {report.Statistics.DiscoveredFiles}");
        Console.WriteLine($"Analyzed files:            {report.Statistics.AnalyzedFiles}");
        Console.WriteLine($"Test fixtures:             {report.Statistics.TestFixtures}");
        Console.WriteLine($"Test methods:              {report.Statistics.TestMethods}");
        Console.WriteLine($"Direct test usages:        {report.DirectTestUsages.Count}");
        Console.WriteLine($"Lifecycle usages:          {report.LifecycleUsages.Count}");
        Console.WriteLine();

        if (report.DirectTestUsages.Count > 0)
        {
            Console.WriteLine("Direct test usages:");
            foreach (var usage in report.DirectTestUsages.OrderBy(usage => usage.FilePath).ThenBy(usage => usage.Line))
            {
                Console.WriteLine($"  {usage.TestClassName}.{usage.TestMethodName} ({usage.FilePath}:{usage.Line})");
            }
            Console.WriteLine();
        }

        if (report.LifecycleUsages.Count > 0)
        {
            Console.WriteLine("Lifecycle usages:");
            foreach (var usage in report.LifecycleUsages.OrderBy(usage => usage.FilePath).ThenBy(usage => usage.Line))
            {
                Console.WriteLine($"  {usage.TestClassName}.{usage.LifecycleMethodName} [{usage.LifecycleKind}] ({usage.FilePath}:{usage.Line})");
                Console.WriteLine($"    Tests: {string.Join(", ", usage.TestMethods)}");
            }
        }
    }

    private static void PrintReportSummary(AnalysisReport report)
    {
        Console.WriteLine();
        Console.WriteLine("=== SELENIUM/NUNIT ANALYSIS REPORT ===");
        Console.WriteLine();
        Console.WriteLine($"Discovered C# files:        {report.Statistics.DiscoveredFiles}");
        Console.WriteLine($"Analyzed files:             {report.Statistics.AnalyzedFiles}");
        Console.WriteLine($"Test fixtures:              {report.Statistics.TestFixtures}");
        Console.WriteLine($"Test methods:               {report.Statistics.TestMethods}");
        Console.WriteLine($"Issues:                     {report.Issues.Count}");
        Console.WriteLine();

        foreach (var group in report.Issues.GroupBy(issue => issue.FilePath))
        {
            Console.WriteLine(group.Key);
            foreach (var issue in group.OrderBy(issue => issue.Line))
            {
                Console.WriteLine($"  Line {issue.Line}: {issue.RuleId} [{issue.Severity}] {issue.Message}");
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"<target-directory>\" [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --report <path>                 Custom JSON report path");
        Console.WriteLine("  --allow-non-parallel-fixtures   Do not report [NonParallelizable] fixtures");
        Console.WriteLine("  --list-categories              List effective categories for every test case");
        Console.WriteLine("  --product-matrix               Create Yes/No product columns for every test case");
        Console.WriteLine("  --missing-products             List test cases without product categories");
        Console.WriteLine("  --parallel-classes             List parallelization settings for test classes");
        Console.WriteLine("  --find-usages                   Find tests using a target method");
        Console.WriteLine("  --class <name>                  Target class for --find-usages");
        Console.WriteLine("  --method <name>                 Target method for --find-usages");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\"");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --report \"C:\\Temp\\report.json\"");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --list-categories");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --product-matrix");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --missing-products");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --parallel-classes");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --find-usages --class LoginPage --method Login");
        Console.WriteLine();
    }
}

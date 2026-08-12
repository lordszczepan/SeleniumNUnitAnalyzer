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

            if (options.FindUsages)
            {
                var usageReport = analyzer.FindMethodUsages();
                PrintUsageReportSummary(usageReport);

                string usageReportPath = options.ReportPath ??
                    Path.Combine(options.TargetDirectory, "SeleniumNUnitMethodUsageReport.json");
                usageReport.SaveAsJson(usageReportPath);

                Console.WriteLine();
                Console.WriteLine($"Report saved to: {usageReportPath}");

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
        Console.WriteLine("  --find-usages                   Find tests using a target method");
        Console.WriteLine("  --class <name>                  Target class for --find-usages");
        Console.WriteLine("  --method <name>                 Target method for --find-usages");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\"");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --report \"C:\\Temp\\report.json\"");
        Console.WriteLine("  SeleniumNUnitAnalyzer.exe \"C:\\Projects\\MySeleniumTests\" --find-usages --class LoginPage --method Login");
        Console.WriteLine();
    }
}

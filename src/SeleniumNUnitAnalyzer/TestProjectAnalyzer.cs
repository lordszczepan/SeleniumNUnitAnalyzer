namespace SeleniumNUnitAnalyzer;

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class TestProjectAnalyzer
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
        "packages",
        "TestResults"
    };

    private readonly CommandLineOptions _options;

    public TestProjectAnalyzer(CommandLineOptions options)
    {
        _options = options;
    }

    public AnalysisReport Analyze()
    {
        var files = DiscoverCSharpFiles(_options.TargetDirectory).ToList();
        var report = new AnalysisReport
        {
            Statistics = { DiscoveredFiles = files.Count }
        };

        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: file);
            var analyzer = new NUnitTestFileAnalyzer(syntaxTree, _options);
            var result = analyzer.Analyze();

            report.Statistics.AnalyzedFiles++;
            report.Statistics.TestFixtures += result.TestFixtures;
            report.Statistics.TestMethods += result.TestMethods;
            report.Issues.AddRange(result.Issues);
        }

        return report;
    }

    public MethodUsageReport FindMethodUsages()
    {
        var files = DiscoverCSharpFiles(_options.TargetDirectory).ToList();
        var report = new MethodUsageReport
        {
            TargetClassName = _options.TargetClassName ?? string.Empty,
            TargetMethodName = _options.TargetMethodName ?? string.Empty,
            Statistics = { DiscoveredFiles = files.Count }
        };

        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: file);
            var analyzer = new MethodUsageAnalyzer(
                syntaxTree,
                _options.TargetClassName ?? string.Empty,
                _options.TargetMethodName ?? string.Empty);
            var result = analyzer.Analyze();

            report.Statistics.AnalyzedFiles++;
            report.Statistics.TestFixtures += result.TestFixtures;
            report.Statistics.TestMethods += result.TestMethods;
            report.DirectTestUsages.AddRange(result.DirectTestUsages);
            report.LifecycleUsages.AddRange(result.LifecycleUsages);
        }

        return report;
    }

    private static IEnumerable<string> DiscoverCSharpFiles(string rootDirectory)
    {
        var pending = new Stack<string>();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            string current = pending.Pop();

            foreach (string directory in Directory.EnumerateDirectories(current))
            {
                if (!ExcludedDirectories.Contains(Path.GetFileName(directory)))
                {
                    pending.Push(directory);
                }
            }

            foreach (string file in Directory.EnumerateFiles(current, "*.cs"))
            {
                yield return file;
            }
        }
    }
}

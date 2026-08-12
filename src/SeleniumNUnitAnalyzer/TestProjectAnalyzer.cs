namespace SeleniumNUnitAnalyzer;

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class TestProjectAnalyzer
{
    private readonly CommandLineOptions _options;

    public TestProjectAnalyzer(CommandLineOptions options)
    {
        _options = options;
    }

    public AnalysisReport Analyze()
    {
        var files = ProjectFileDiscoverer.DiscoverCSharpFiles(_options.TargetDirectory).ToList();
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
        var files = ProjectFileDiscoverer.DiscoverCSharpFiles(_options.TargetDirectory).ToList();
        var report = new MethodUsageReport
        {
            TargetClassName = _options.TargetClassName ?? string.Empty,
            TargetMethodName = _options.TargetMethodName ?? string.Empty,
            Statistics = { DiscoveredFiles = files.Count }
        };

        var syntaxTrees = new List<Microsoft.CodeAnalysis.SyntaxTree>();
        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: file);
            syntaxTrees.Add(syntaxTree);
            report.Statistics.AnalyzedFiles++;
        }

        var analyzer = new RecursiveMethodUsageAnalyzer(
            syntaxTrees,
            _options.TargetClassName ?? string.Empty,
            _options.TargetMethodName ?? string.Empty);
        var result = analyzer.Analyze();

        report.Statistics.TestFixtures = result.TestFixtures;
        report.Statistics.TestMethods = result.TestMethods;
        report.DirectTestUsages.AddRange(result.DirectTestUsages);
        report.LifecycleUsages.AddRange(result.LifecycleUsages);

        return report;
    }

    public CategoryReport ListCategories()
    {
        var files = ProjectFileDiscoverer.DiscoverCSharpFiles(_options.TargetDirectory).ToList();
        var report = new CategoryReport
        {
            Statistics = { DiscoveredFiles = files.Count }
        };

        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: file);
            var analyzer = new CategoryAnalyzer(syntaxTree);
            var result = analyzer.Analyze();

            report.Statistics.AnalyzedFiles++;
            report.Statistics.TestFixtures += result.TestFixtures;
            report.Statistics.TestMethods += result.TestMethods;
            report.TestCases.AddRange(result.TestCases);
        }

        return report;
    }

    public ParallelClassReport ListParallelClasses()
    {
        var files = ProjectFileDiscoverer.DiscoverCSharpFiles(_options.TargetDirectory).ToList();
        var report = new ParallelClassReport
        {
            Statistics = { DiscoveredFiles = files.Count }
        };

        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: file);
            var analyzer = new ParallelClassAnalyzer(syntaxTree);
            var result = analyzer.Analyze();

            report.Statistics.AnalyzedFiles++;
            report.Statistics.TestFixtures += result.TestFixtures;
            report.Statistics.TestMethods += result.TestMethods;
            report.Classes.AddRange(result.Classes);
        }

        return report;
    }
}

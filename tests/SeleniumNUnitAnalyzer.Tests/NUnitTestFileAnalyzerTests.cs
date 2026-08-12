namespace SeleniumNUnitAnalyzer.Tests;

using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public class NUnitTestFileAnalyzerTests
{
    [Test]
    public void Analyze_TestWithoutCategory_ReportsIssue()
    {
        string source = """
using NUnit.Framework;

[TestFixture]
[Parallelizable]
public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.Issues.Single().RuleId, Is.EqualTo("NUNIT001"));
    }

    [Test]
    public void Analyze_TestWithFixtureCategory_DoesNotReportCategoryIssue()
    {
        string source = """
using NUnit.Framework;

[TestFixture]
[Category("Smoke")]
[Parallelizable]
public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.Issues.Any(issue => issue.RuleId == "NUNIT001"), Is.False);
    }

    [Test]
    public void Analyze_FixtureWithoutParallelizable_ReportsIssue()
    {
        string source = """
using NUnit.Framework;

[TestFixture]
[Category("Smoke")]
public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.Issues.Any(issue => issue.RuleId == "NUNIT002"), Is.True);
    }

    [Test]
    public void Analyze_NonParallelizableCanBeAllowed()
    {
        string source = """
using NUnit.Framework;

[TestFixture]
[Category("Smoke")]
[NonParallelizable]
public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source, allowNonParallelFixtures: true);

        Assert.That(result.Issues.Any(issue => issue.RuleId == "NUNIT002"), Is.False);
    }

    [Test]
    public void Analyze_AssemblyLevelParallelization_DoesNotReportFixtureIssue()
    {
        string source = """
using NUnit.Framework;

[assembly: Parallelizable]

[TestFixture]
[Category("Smoke")]
public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.Issues.Any(issue => issue.RuleId == "NUNIT002"), Is.False);
    }

    [Test]
    public void Analyze_TestCaseCountsAsTestMethod()
    {
        string source = """
using NUnit.Framework;

[Parallelizable]
public class SearchTests
{
    [TestCase("phone")]
    [Category("Regression")]
    public void CanSearch(string phrase)
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.TestFixtures, Is.EqualTo(1));
        Assert.That(result.TestMethods, Is.EqualTo(1));
        Assert.That(result.Issues, Is.Empty);
    }

    private static FileAnalysisResult Analyze(string source, bool allowNonParallelFixtures = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Tests.cs");
        var options = new CommandLineOptions
        {
            TargetDirectory = ".",
            AllowNonParallelFixtures = allowNonParallelFixtures
        };

        return new NUnitTestFileAnalyzer(syntaxTree, options).Analyze();
    }
}

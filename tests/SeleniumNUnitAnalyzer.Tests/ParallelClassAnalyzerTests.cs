namespace SeleniumNUnitAnalyzer.Tests;

using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public class ParallelClassAnalyzerTests
{
    [Test]
    public void Analyze_ClassParallelizable_ReportsArguments()
    {
        string source = """
using NUnit.Framework;

[Parallelizable(ParallelScope.Fixtures)]
public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);
        var item = result.Classes.Single();

        Assert.That(item.IsParallelizable, Is.True);
        Assert.That(item.ParallelizableArguments, Is.EqualTo("ParallelScope.Fixtures"));
        Assert.That(item.EffectiveParallelizationSource, Is.EqualTo("Class Parallelizable"));
    }

    [Test]
    public void Analyze_ClassNonParallelizable_ReportsEffectiveSource()
    {
        string source = """
using NUnit.Framework;

[NonParallelizable]
public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);
        var item = result.Classes.Single();

        Assert.That(item.IsNonParallelizable, Is.True);
        Assert.That(item.EffectiveParallelizationSource, Is.EqualTo("Class NonParallelizable"));
    }

    [Test]
    public void Analyze_AssemblyParallelizable_ReportsAssemblySource()
    {
        string source = """
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(4)]

public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);
        var item = result.Classes.Single();

        Assert.That(item.HasAssemblyParallelizable, Is.True);
        Assert.That(item.AssemblyParallelizableArguments, Is.EqualTo("ParallelScope.Fixtures"));
        Assert.That(item.AssemblyLevelOfParallelism, Is.EqualTo("4"));
        Assert.That(item.EffectiveParallelizationSource, Is.EqualTo("Assembly"));
    }

    [Test]
    public void Analyze_NoParallelization_ReportsNone()
    {
        string source = """
using NUnit.Framework;

public class LoginTests
{
    [Test]
    public void CanLogin()
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.Classes.Single().EffectiveParallelizationSource, Is.EqualTo("None"));
    }

    private static ParallelClassAnalysisResult Analyze(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Tests.cs");
        return new ParallelClassAnalyzer(syntaxTree).Analyze();
    }
}

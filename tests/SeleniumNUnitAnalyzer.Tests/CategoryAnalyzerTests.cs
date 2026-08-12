namespace SeleniumNUnitAnalyzer.Tests;

using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public class CategoryAnalyzerTests
{
    [Test]
    public void Analyze_ClassCategory_AppliesToTestCases()
    {
        string source = """
using NUnit.Framework;

[Category(Tags.RCChange)]
public class ChangeControlCreationTests
{
    [Test]
    public void ShouldCreateChangeControl()
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.TestCases.Single().Categories, Is.EquivalentTo(new[] { "Tags.RCChange" }));
    }

    [Test]
    public void Analyze_MethodCategory_AppliesToTestCase()
    {
        string source = """
using NUnit.Framework;

public class SlmTests
{
    [TestCase("admin")]
    [Category(Tags.RCSLM)]
    public void ShouldChangeSelectedSystemStatus(string user)
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.TestCases.Single().Categories, Is.EquivalentTo(new[] { "Tags.RCSLM" }));
    }

    [Test]
    public void Analyze_TestCaseCategoryNamedArgument_AppliesToThatTestCase()
    {
        string source = """
using NUnit.Framework;

public class CipTests
{
    [TestCase("approver", ExpectedResult = true, Category = Tags.Problem)]
    public bool ShouldEnterNewCipView(string user)
    {
        return true;
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.TestCases.Single().Categories, Is.EquivalentTo(new[] { "Tags.Problem" }));
    }

    [Test]
    public void Analyze_MultipleTestCases_CreatesOneEntryPerAttribute()
    {
        string source = """
using NUnit.Framework;

[Category(Tags.Smoke)]
public class CipTests
{
    [TestCase("approver", Category = Tags.Problem)]
    [TestCase("owner", Category = Tags.CIP)]
    public void ShouldEnterNewCipView(string user)
    {
    }
}
""";

        var result = Analyze(source);

        Assert.That(result.TestCases, Has.Count.EqualTo(2));
        Assert.That(result.TestCases[0].Categories, Is.EquivalentTo(new[] { "Tags.Smoke", "Tags.Problem" }));
        Assert.That(result.TestCases[1].Categories, Is.EquivalentTo(new[] { "Tags.Smoke", "Tags.CIP" }));
    }

    private static CategoryAnalysisResult Analyze(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Tests.cs");
        return new CategoryAnalyzer(syntaxTree).Analyze();
    }
}

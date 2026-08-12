namespace SeleniumNUnitAnalyzer.Tests;

using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public class RecursiveMethodUsageAnalyzerTests
{
    [Test]
    public void Analyze_TestCallingMethodThatCallsTarget_ReportsTestUsage()
    {
        string pageSource = """
public class ValidationPlanSettingsPage
{
    public ValidationPlanSettingsPage EnterEditMode()
    {
        return this;
    }

    public void SetDocumentAuthor(string name)
    {
        EnterEditMode();
    }
}
""";
        string testSource = """
using NUnit.Framework;

public class ValidationTests
{
    [Test]
    public void CanSetAuthor()
    {
        var valPlanPage = dashboard.OpenDeliverablePageByName("Validation Plan") as ValidationPlanSettingsPage;
        valPlanPage.SetDocumentAuthor("author");
    }
}
""";

        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(pageSource, path: "ValidationPlanSettingsPage.cs"),
            CSharpSyntaxTree.ParseText(testSource, path: "ValidationTests.cs")
        };

        var result = new RecursiveMethodUsageAnalyzer(
            syntaxTrees,
            "ValidationPlanSettingsPage",
            "EnterEditMode").Analyze();

        Assert.That(result.DirectTestUsages.Single().TestMethodName, Is.EqualTo("CanSetAuthor"));
        Assert.That(result.DirectTestUsages.Single().Invocation, Is.EqualTo(@"valPlanPage.SetDocumentAuthor(""author"")"));
    }
}

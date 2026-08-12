namespace SeleniumNUnitAnalyzer.Tests;

using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;

[TestFixture]
public class MethodUsageAnalyzerTests
{
    [Test]
    public void Analyze_StaticTargetCallInTest_ReportsDirectUsage()
    {
        string source = """
using NUnit.Framework;

public class LoginTests
{
    [Test]
    public void CanLogin()
    {
        LoginPage.Login();
    }
}
""";

        var result = Analyze(source, "LoginPage", "Login");

        Assert.That(result.DirectTestUsages.Single().TestMethodName, Is.EqualTo("CanLogin"));
        Assert.That(result.LifecycleUsages, Is.Empty);
    }

    [Test]
    public void Analyze_FieldTargetCallInTest_ReportsDirectUsage()
    {
        string source = """
using NUnit.Framework;

public class LoginTests
{
    private LoginPage _loginPage;

    [Test]
    public void CanLogin()
    {
        _loginPage.Login();
    }
}
""";

        var result = Analyze(source, "LoginPage", "Login");

        Assert.That(result.DirectTestUsages.Single().Invocation, Is.EqualTo("_loginPage.Login()"));
    }

    [Test]
    public void Analyze_LocalTargetCallInTest_ReportsDirectUsage()
    {
        string source = """
using NUnit.Framework;

public class LoginTests
{
    [Test]
    public void CanLogin()
    {
        var loginPage = new LoginPage();
        loginPage.Login();
    }
}
""";

        var result = Analyze(source, "LoginPage", "Login");

        Assert.That(result.DirectTestUsages.Single().Invocation, Is.EqualTo("loginPage.Login()"));
    }

    [Test]
    public void Analyze_AsExpressionTargetAlias_ReportsDirectUsage()
    {
        string source = """
using NUnit.Framework;

public class ValidationTests
{
    [Test]
    public void CanEditValidationPlan()
    {
        var valPlanPage = dashboard.OpenDeliverablePageByName("Validation Plan") as ValidationPlanSettingsPage;
        valPlanPage.EnterEditMode();
    }
}
""";

        var result = Analyze(source, "ValidationPlanSettingsPage", "EnterEditMode");

        Assert.That(result.DirectTestUsages.Single().Invocation, Is.EqualTo("valPlanPage.EnterEditMode()"));
    }

    [Test]
    public void Analyze_TargetCallInSetUp_ReportsLifecycleUsageWithClassTests()
    {
        string source = """
using NUnit.Framework;

public class LoginTests
{
    private LoginPage _loginPage;

    [SetUp]
    public void BeforeEach()
    {
        _loginPage.Login();
    }

    [Test]
    public void CanOpenAccount()
    {
    }

    [TestCase("admin")]
    public void CanOpenAccountForRole(string role)
    {
    }
}
""";

        var result = Analyze(source, "LoginPage", "Login");
        var usage = result.LifecycleUsages.Single();

        Assert.That(usage.TestClassName, Is.EqualTo("LoginTests"));
        Assert.That(usage.LifecycleKind, Is.EqualTo("SetUp"));
        Assert.That(usage.TestMethods, Is.EquivalentTo(new[] { "CanOpenAccount", "CanOpenAccountForRole" }));
    }

    [Test]
    public void Analyze_DifferentClassMethod_IsIgnored()
    {
        string source = """
using NUnit.Framework;

public class LoginTests
{
    [Test]
    public void CanLogin()
    {
        MenuPage.Login();
    }
}
""";

        var result = Analyze(source, "LoginPage", "Login");

        Assert.That(result.DirectTestUsages, Is.Empty);
        Assert.That(result.LifecycleUsages, Is.Empty);
    }

    private static MethodUsageAnalysisResult Analyze(string source, string className, string methodName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Tests.cs");
        return new MethodUsageAnalyzer(syntaxTree, className, methodName).Analyze();
    }
}

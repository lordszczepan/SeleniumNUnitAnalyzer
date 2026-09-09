namespace SeleniumNUnitAnalyzer.Tests;

using NUnit.Framework;

[TestFixture]
public class CommandLineOptionsTests
{
    [Test]
    public void IsValid_UnknownOption_ReturnsError()
    {
        var options = CommandLineOptions.Parse([
            TestContext.CurrentContext.WorkDirectory,
            "--parallel-classe"
        ]);

        bool isValid = options.IsValid(out string errorMessage);

        Assert.That(isValid, Is.False);
        Assert.That(errorMessage, Does.Contain("--parallel-classe"));
    }

    [Test]
    public void IsValid_ParallelClassesOption_IsAccepted()
    {
        var options = CommandLineOptions.Parse([
            TestContext.CurrentContext.WorkDirectory,
            "--parallel-classes"
        ]);

        bool isValid = options.IsValid(out string errorMessage);

        Assert.That(isValid, Is.True, errorMessage);
        Assert.That(options.ParallelClasses, Is.True);
    }
}

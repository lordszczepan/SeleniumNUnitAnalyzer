namespace SeleniumNUnitAnalyzer.Tests;

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class ProductMatrixReportTests
{
    [Test]
    public void FromCategoryReport_MapsProductCategoriesToYesNoColumns()
    {
        var categoryReport = CreateReport(
            new TestCaseCategoryInfo
            {
                TestClassName = "ChangeTests",
                TestMethodName = "ShouldCreateChange",
                TestKind = "TestCase",
                Categories = ["Tags.RCChange", "Tags.RCQMS"]
            });

        var report = ProductMatrixReport.FromCategoryReport(categoryReport, onlyMissingProducts: false);
        var row = report.TestCases.Single();

        Assert.That(row.HasAnyProduct, Is.True);
        Assert.That(row.Products["RC-Change"], Is.True);
        Assert.That(row.Products["RC-QMS"], Is.True);
        Assert.That(row.Products["RC-DMS"], Is.False);
    }

    [Test]
    public void FromCategoryReport_StringLiteralProductCategory_IsRecognized()
    {
        var categoryReport = CreateReport(
            new TestCaseCategoryInfo
            {
                TestClassName = "DmsTests",
                TestMethodName = "ShouldOpenDocument",
                TestKind = "Test",
                Categories = ["\"RC-DMS\""]
            });

        var report = ProductMatrixReport.FromCategoryReport(categoryReport, onlyMissingProducts: false);

        Assert.That(report.TestCases.Single().Products["RC-DMS"], Is.True);
    }

    [Test]
    public void FromCategoryReport_OnlyMissingProducts_FiltersRowsWithoutProduct()
    {
        var categoryReport = CreateReport(
            new TestCaseCategoryInfo
            {
                TestClassName = "ChangeTests",
                TestMethodName = "ShouldCreateChange",
                TestKind = "TestCase",
                Categories = ["Tags.RCChange"]
            },
            new TestCaseCategoryInfo
            {
                TestClassName = "AuditTrailTests",
                TestMethodName = "ShouldLogChange",
                TestKind = "TestCase",
                Categories = ["Tags.AuditTrail"]
            });

        var report = ProductMatrixReport.FromCategoryReport(categoryReport, onlyMissingProducts: true);

        Assert.That(report.TestCases.Single().TestMethodName, Is.EqualTo("ShouldLogChange"));
        Assert.That(report.TestCases.Single().HasAnyProduct, Is.False);
    }

    private static CategoryReport CreateReport(params TestCaseCategoryInfo[] testCases)
    {
        return new CategoryReport
        {
            TestCases = new List<TestCaseCategoryInfo>(testCases)
        };
    }
}

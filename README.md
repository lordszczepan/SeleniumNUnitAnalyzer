# Selenium NUnit Analyzer

Static analyzer for Selenium tests written with NUnit.

The tool scans only files that are part of discovered `.csproj` files when possible. This helps avoid reporting tests from stale `.cs` files that still exist on disk but are no longer included in a project.

## Build

```bash
cd SeleniumNUnitAnalyzer
dotnet restore
dotnet build
dotnet test
```

## Target Paths

Use the test project folder when a report only needs test files:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests"
```

Use a parent folder when a report needs both tests and page model/helper projects:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite"
```

This is especially useful for recursive method usage lookup.

## Quality Analysis

Checks for missing test metadata:

- `NUNIT001`: test method has no `[Category]` on the method, fixture, `[Test]`, or `[TestCase]`
- `NUNIT002`: test fixture has no `[Parallelizable]`, or is explicitly `[NonParallelizable]`

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests"
```

Allow explicitly non-parallel fixtures:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --allow-non-parallel-fixtures
```

Default output:

- `SeleniumNUnitAnalysisReport.json`

## Category Listing

Lists effective categories for every `[Test]`, `[TestCase]`, `[TestCaseSource]`, or `[Theory]`.

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --list-categories
```

Recognized category styles:

```csharp
[Category(Tags.ProductArea)]
public class ExampleTests

[TestCase("admin")]
[Category(Tags.Workflow)]
public void should_run_workflow(string user)

[TestCase("admin", ExpectedResult = true, Category = Tags.Smoke)]
public bool should_return_expected_result(string user)
```

Default outputs:

- `SeleniumNUnitCategoryReport.json`
- `SeleniumNUnitCategoryReport.md`
- `SeleniumNUnitCategoryReport.csv`

## Product Matrix

Creates Yes/No product columns for every test case. A test can belong to more than one product.

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --product-matrix
```

Product columns are defined in `src/SeleniumNUnitAnalyzer/ProductCatalog.cs`.

Default outputs:

- `SeleniumNUnitProductMatrixReport.json`
- `SeleniumNUnitProductMatrixReport.md`
- `SeleniumNUnitProductMatrixReport.csv`

## Missing Products

Lists only test cases that do not have any recognized product category.

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --missing-products
```

Default outputs:

- `SeleniumNUnitMissingProductsReport.json`
- `SeleniumNUnitMissingProductsReport.md`
- `SeleniumNUnitMissingProductsReport.csv`

## Parallel Class Listing

Lists parallelization settings for every test class.

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --parallel-classes
```

The report includes:

- class-level `[Parallelizable]`
- class-level `[NonParallelizable]`
- assembly-level `[Parallelizable]`
- assembly-level `[LevelOfParallelism]`
- parallelization arguments/scopes
- effective source: `Class Parallelizable`, `Class NonParallelizable`, `Assembly`, or `None`

Default outputs:

- `SeleniumNUnitParallelClassReport.json`
- `SeleniumNUnitParallelClassReport.md`
- `SeleniumNUnitParallelClassReport.csv`

## Method Usage Lookup

Finds tests that use a specific page/helper method.

Use the parent folder when the target method is declared in a page model project and used by a separate test project:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite" --find-usages --class LoginPage --method Login
```

The lookup is recursive:

1. Finds direct calls to the target method.
2. Finds methods that call the target method.
3. Finds methods or tests that call those methods.
4. Continues until it reaches test methods or lifecycle methods.

The report includes:

- direct test methods that use the target method directly or through intermediate methods
- lifecycle methods (`SetUp`, `TearDown`, `OneTimeSetUp`, `OneTimeTearDown`) that use the target method
- the test class that owns each lifecycle method
- all test methods contained in that class

Recognized object patterns include:

```csharp
LoginPage.Login();
_loginPage.Login();
var loginPage = new LoginPage();
loginPage.Login();
new LoginPage().Login();
var loginPage = pageFactory.Create() as LoginPage;
loginPage.Login();
```

Default outputs:

- `SeleniumNUnitMethodUsageReport.json`
- `SeleniumNUnitMethodUsageReport.md`
- `SeleniumNUnitMethodUsageReport.csv`

## Custom Report Path

Use `--report` to choose a custom JSON path. For reports that also create Markdown and CSV, the `.md` and `.csv` files are written next to the JSON file.

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --product-matrix --report "C:\Temp\ProductMatrix.json"
```

Creates:

- `C:\Temp\ProductMatrix.json`
- `C:\Temp\ProductMatrix.md`
- `C:\Temp\ProductMatrix.csv`

Another example:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite" --find-usages --class DashboardPage --method OpenDetails --report "C:\Temp\MethodUsage.json"
```

## Command Summary

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests"
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --allow-non-parallel-fixtures
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --list-categories
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --product-matrix
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --missing-products
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite\Tests" --parallel-classes
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\SeleniumSuite" --find-usages --class LoginPage --method Login
```

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Analysis completed successfully and no blocking findings were found |
| `1` | Invalid input or fatal error |
| `2` | Analysis completed with findings, or method usage lookup found no usages |

## Excluded Directories

The analyzer skips common generated/dependency folders:

- `.git`
- `bin`
- `obj`
- `packages`
- `TestResults`

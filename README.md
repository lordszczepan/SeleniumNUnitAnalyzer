# Selenium NUnit Analyzer

Static analyzer for Selenium tests written with NUnit.

The first version scans C# files and reports missing test metadata that often causes test suite drift:

- `NUNIT001`: test method has no `[Category]` on the method or fixture
- `NUNIT002`: test fixture has no `[Parallelizable]`, or is explicitly `[NonParallelizable]` without opt-in allowance

## Build

```bash
cd SeleniumNUnitAnalyzer
dotnet restore
dotnet build
dotnet test
```

## Usage

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\MySeleniumTests"
```

Optional custom report path:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\MySeleniumTests" --report "C:\Temp\SeleniumNUnitAnalysisReport.json"
```

Allow fixtures that are explicitly marked as non-parallel:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\MySeleniumTests" --allow-non-parallel-fixtures
```

The tool writes `SeleniumNUnitAnalysisReport.json` in the analyzed directory unless `--report` is provided.

## Method Usage Lookup

To find tests that use a specific page/helper method:

```bash
dotnet run --project src/SeleniumNUnitAnalyzer -- "C:\Projects\MySeleniumTests" --find-usages --class LoginPage --method Login
```

This creates `SeleniumNUnitMethodUsageReport.json` and prints:

- direct test methods that call the target method
- lifecycle methods (`SetUp`, `TearDown`, `OneTimeSetUp`, `OneTimeTearDown`) that call the target method
- the test class that owns that lifecycle method
- all test methods contained in that class

The lookup recognizes common syntax patterns such as:

```csharp
LoginPage.Login();
_loginPage.Login();
var loginPage = new LoginPage();
loginPage.Login();
new LoginPage().Login();
```

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Analysis completed and no issues were found |
| `1` | Invalid input or fatal error |
| `2` | Analysis completed and issues were found |

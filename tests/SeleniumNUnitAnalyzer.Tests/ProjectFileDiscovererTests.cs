namespace SeleniumNUnitAnalyzer.Tests;

using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

[TestFixture]
public class ProjectFileDiscovererTests
{
    [Test]
    public void DiscoverCSharpFiles_OldStyleProject_ReturnsOnlyCompileIncludes()
    {
        string root = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Included"));
            Directory.CreateDirectory(Path.Combine(root, "Deleted"));
            File.WriteAllText(Path.Combine(root, "Included", "ActiveTests.cs"), "public class ActiveTests { }");
            File.WriteAllText(Path.Combine(root, "Deleted", "OldTests.cs"), "public class OldTests { }");
            File.WriteAllText(Path.Combine(root, "Sample.csproj"), """
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
    <Compile Include="Included\ActiveTests.cs" />
  </ItemGroup>
</Project>
""");

            var files = ProjectFileDiscoverer.DiscoverCSharpFiles(root);

            Assert.That(files.Select(Path.GetFileName), Is.EquivalentTo(new[] { "ActiveTests.cs" }));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DiscoverCSharpFiles_ProjectWithCompileRemove_ExcludesRemovedFiles()
    {
        string root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "ActiveTests.cs"), "public class ActiveTests { }");
            File.WriteAllText(Path.Combine(root, "OldTests.cs"), "public class OldTests { }");
            File.WriteAllText(Path.Combine(root, "Sample.csproj"), """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Remove="OldTests.cs" />
  </ItemGroup>
</Project>
""");

            var files = ProjectFileDiscoverer.DiscoverCSharpFiles(root);

            Assert.That(files.Select(Path.GetFileName), Is.EquivalentTo(new[] { "ActiveTests.cs" }));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "SeleniumNUnitAnalyzerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}

namespace SeleniumNUnitAnalyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public sealed class NUnitTestFileAnalyzer
{
    private readonly SyntaxTree _syntaxTree;
    private readonly CommandLineOptions _options;

    public NUnitTestFileAnalyzer(SyntaxTree syntaxTree, CommandLineOptions options)
    {
        _syntaxTree = syntaxTree;
        _options = options;
    }

    public FileAnalysisResult Analyze()
    {
        var result = new FileAnalysisResult();
        var root = _syntaxTree.GetRoot();
        bool hasAssemblyParallelization = root is CompilationUnitSyntax compilationUnit &&
            compilationUnit.AttributeLists.Any(attributeList =>
                    attributeList.Target?.Identifier.ValueText == "assembly" &&
                    (HasAttribute(attributeList, "Parallelizable") ||
                     HasAttribute(attributeList, "LevelOfParallelism")));

        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var testMethods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(IsTestMethod)
                .ToList();

            bool isFixture = IsTestFixture(classDeclaration) || testMethods.Count > 0;
            if (!isFixture)
            {
                continue;
            }

            result.TestFixtures++;

            if (!hasAssemblyParallelization && !HasAttribute(classDeclaration.AttributeLists, "Parallelizable"))
            {
                bool hasNonParallelizable = HasAttribute(classDeclaration.AttributeLists, "NonParallelizable");
                if (!hasNonParallelizable || !_options.AllowNonParallelFixtures)
                {
                    result.Issues.Add(CreateIssue(
                        "NUNIT002",
                        classDeclaration.Identifier.Text,
                        classDeclaration,
                        hasNonParallelizable
                            ? "Fixture is marked as NonParallelizable. Verify this is intentional for Selenium execution."
                            : "Fixture is missing [Parallelizable]. Add [Parallelizable] or mark the exception explicitly."));
                }
            }

            foreach (var method in testMethods)
            {
                result.TestMethods++;

                if (!HasCategory(method) && !HasCategory(classDeclaration))
                {
                    result.Issues.Add(CreateIssue(
                        "NUNIT001",
                        method.Identifier.Text,
                        method,
                        "Test is missing [Category]. Add a method or fixture category."));
                }
            }
        }

        return result;
    }

    private static bool IsTestFixture(ClassDeclarationSyntax classDeclaration)
    {
        return HasAttribute(classDeclaration.AttributeLists, "TestFixture");
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method)
    {
        return HasAttribute(method.AttributeLists, "Test") ||
               HasAttribute(method.AttributeLists, "TestCase") ||
               HasAttribute(method.AttributeLists, "Theory");
    }

    private static bool HasCategory(MemberDeclarationSyntax member)
    {
        return HasAttribute(member.AttributeLists, "Category") ||
               HasCategoryNamedArgument(member.AttributeLists);
    }

    private static bool HasCategoryNamedArgument(SyntaxList<AttributeListSyntax> attributeLists)
    {
        return attributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute =>
                IsTestCategoryHost(attribute) &&
                attribute.ArgumentList?.Arguments.Any(argument =>
                    argument.NameEquals?.Name.Identifier.Text == "Category") == true);
    }

    private static bool IsTestCategoryHost(AttributeSyntax attribute)
    {
        string attributeName = NormalizeAttributeName(attribute.Name.ToString());
        return attributeName == "Test" ||
               attributeName == "TestCase" ||
               attributeName == "TestCaseSource";
    }

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        return attributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => NormalizeAttributeName(attribute.Name.ToString()) == attributeName);
    }

    private static bool HasAttribute(AttributeListSyntax attributeList, string attributeName)
    {
        return attributeList.Attributes
            .Any(attribute => NormalizeAttributeName(attribute.Name.ToString()) == attributeName);
    }

    private static string NormalizeAttributeName(string attributeName)
    {
        string shortName = attributeName.Split('.').Last();
        return shortName.EndsWith("Attribute")
            ? shortName[..^"Attribute".Length]
            : shortName;
    }

    private AnalysisIssue CreateIssue(
        string ruleId,
        string symbolName,
        SyntaxNode node,
        string message)
    {
        var lineSpan = node.GetLocation().GetLineSpan();
        return new AnalysisIssue
        {
            RuleId = ruleId,
            FilePath = _syntaxTree.FilePath,
            Line = lineSpan.StartLinePosition.Line + 1,
            SymbolName = symbolName,
            Message = message
        };
    }
}

public sealed class FileAnalysisResult
{
    public int TestFixtures { get; set; }

    public int TestMethods { get; set; }

    public List<AnalysisIssue> Issues { get; } = new();
}

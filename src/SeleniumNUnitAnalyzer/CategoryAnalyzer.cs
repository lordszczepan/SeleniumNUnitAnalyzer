namespace SeleniumNUnitAnalyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public sealed class CategoryAnalyzer
{
    private static readonly HashSet<string> TestAttributes = new()
    {
        "Test",
        "TestCase",
        "TestCaseSource",
        "Theory"
    };

    private readonly SyntaxTree _syntaxTree;

    public CategoryAnalyzer(SyntaxTree syntaxTree)
    {
        _syntaxTree = syntaxTree;
    }

    public CategoryAnalysisResult Analyze()
    {
        var result = new CategoryAnalysisResult();
        var root = _syntaxTree.GetRoot();

        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var testMethods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(IsTestMethod)
                .ToList();

            bool isFixture = HasAttribute(classDeclaration.AttributeLists, "TestFixture") || testMethods.Count > 0;
            if (!isFixture)
            {
                continue;
            }

            result.TestFixtures++;
            result.TestMethods += testMethods.Count;

            var classCategories = ExtractExplicitCategories(classDeclaration.AttributeLists);

            foreach (var method in testMethods)
            {
                var methodCategories = ExtractExplicitCategories(method.AttributeLists);
                var testAttributes = method.AttributeLists
                    .SelectMany(list => list.Attributes)
                    .Where(IsTestAttribute)
                    .ToList();

                foreach (var testAttribute in testAttributes)
                {
                    var categories = classCategories
                        .Concat(methodCategories)
                        .Concat(ExtractNamedArgumentCategories(testAttribute))
                        .Distinct()
                        .ToList();

                    result.TestCases.Add(new TestCaseCategoryInfo
                    {
                        FilePath = _syntaxTree.FilePath,
                        Line = GetLine(testAttribute),
                        TestClassName = classDeclaration.Identifier.Text,
                        TestMethodName = method.Identifier.Text,
                        TestKind = NormalizeAttributeName(testAttribute.Name.ToString()),
                        Attribute = testAttribute.ToString(),
                        Categories = categories
                    });
                }
            }
        }

        return result;
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(IsTestAttribute);
    }

    private static bool IsTestAttribute(AttributeSyntax attribute)
    {
        return TestAttributes.Contains(NormalizeAttributeName(attribute.Name.ToString()));
    }

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        return attributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => NormalizeAttributeName(attribute.Name.ToString()) == attributeName);
    }

    private static List<string> ExtractExplicitCategories(SyntaxList<AttributeListSyntax> attributeLists)
    {
        return attributeLists
            .SelectMany(list => list.Attributes)
            .Where(attribute => NormalizeAttributeName(attribute.Name.ToString()) == "Category")
            .SelectMany(ExtractPositionalArguments)
            .Distinct()
            .ToList();
    }

    private static IEnumerable<string> ExtractNamedArgumentCategories(AttributeSyntax attribute)
    {
        return attribute.ArgumentList?.Arguments
            .Where(argument => argument.NameEquals?.Name.Identifier.Text == "Category")
            .Select(argument => argument.Expression.ToString())
            .Where(category => !string.IsNullOrWhiteSpace(category)) ??
            Enumerable.Empty<string>();
    }

    private static IEnumerable<string> ExtractPositionalArguments(AttributeSyntax attribute)
    {
        return attribute.ArgumentList?.Arguments
            .Where(argument => argument.NameEquals == null)
            .Select(argument => argument.Expression.ToString())
            .Where(category => !string.IsNullOrWhiteSpace(category)) ??
            Enumerable.Empty<string>();
    }

    private static string NormalizeAttributeName(string attributeName)
    {
        string shortName = attributeName.Split('.').Last();
        return shortName.EndsWith("Attribute")
            ? shortName[..^"Attribute".Length]
            : shortName;
    }

    private static int GetLine(SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }
}

public sealed class CategoryAnalysisResult
{
    public int TestFixtures { get; set; }

    public int TestMethods { get; set; }

    public List<TestCaseCategoryInfo> TestCases { get; } = new();
}

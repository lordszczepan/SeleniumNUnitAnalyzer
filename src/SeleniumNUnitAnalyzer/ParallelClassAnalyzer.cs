namespace SeleniumNUnitAnalyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public sealed class ParallelClassAnalyzer
{
    private static readonly HashSet<string> TestAttributes = new()
    {
        "Test",
        "TestCase",
        "TestCaseSource",
        "Theory"
    };

    private readonly SyntaxTree _syntaxTree;

    public ParallelClassAnalyzer(SyntaxTree syntaxTree)
    {
        _syntaxTree = syntaxTree;
    }

    public ParallelClassAnalysisResult Analyze()
    {
        var result = new ParallelClassAnalysisResult();
        var root = _syntaxTree.GetRoot();
        var assemblyParallelizable = root is CompilationUnitSyntax compilationUnit
            ? FindAssemblyParallelizable(compilationUnit)
            : null;
        var assemblyLevelOfParallelism = root is CompilationUnitSyntax compilationUnitWithAttributes
            ? FindAssemblyLevelOfParallelism(compilationUnitWithAttributes)
            : null;

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

            var classParallelizable = FindAttribute(classDeclaration.AttributeLists, "Parallelizable");
            var classNonParallelizable = FindAttribute(classDeclaration.AttributeLists, "NonParallelizable");
            bool hasClassParallelizable = classParallelizable != null;
            bool hasClassNonParallelizable = classNonParallelizable != null;
            bool hasAssemblyParallelizable = assemblyParallelizable != null || assemblyLevelOfParallelism != null;

            result.TestFixtures++;
            result.TestMethods += testMethods.Count;
            result.Classes.Add(new ParallelClassInfo
            {
                FilePath = _syntaxTree.FilePath,
                Line = GetLine(classDeclaration),
                TestClassName = classDeclaration.Identifier.Text,
                TestMethodCount = testMethods.Count,
                IsParallelizable = hasClassParallelizable,
                ParallelizableArguments = classParallelizable == null
                    ? string.Empty
                    : GetArguments(classParallelizable),
                IsNonParallelizable = hasClassNonParallelizable,
                NonParallelizableArguments = classNonParallelizable == null
                    ? string.Empty
                    : GetArguments(classNonParallelizable),
                HasAssemblyParallelizable = hasAssemblyParallelizable,
                AssemblyParallelizableArguments = assemblyParallelizable == null
                    ? string.Empty
                    : GetArguments(assemblyParallelizable),
                AssemblyLevelOfParallelism = assemblyLevelOfParallelism == null
                    ? string.Empty
                    : GetArguments(assemblyLevelOfParallelism),
                EffectiveParallelizationSource = GetEffectiveParallelizationSource(
                    hasClassParallelizable,
                    hasClassNonParallelizable,
                    hasAssemblyParallelizable)
            });
        }

        return result;
    }

    private static string GetEffectiveParallelizationSource(
        bool hasClassParallelizable,
        bool hasClassNonParallelizable,
        bool hasAssemblyParallelizable)
    {
        if (hasClassNonParallelizable)
        {
            return "Class NonParallelizable";
        }

        if (hasClassParallelizable)
        {
            return "Class Parallelizable";
        }

        if (hasAssemblyParallelizable)
        {
            return "Assembly";
        }

        return "None";
    }

    private static AttributeSyntax? FindAssemblyParallelizable(CompilationUnitSyntax compilationUnit)
    {
        return compilationUnit.AttributeLists
            .Where(attributeList => attributeList.Target?.Identifier.ValueText == "assembly")
            .SelectMany(attributeList => attributeList.Attributes)
            .FirstOrDefault(attribute => NormalizeAttributeName(attribute.Name.ToString()) == "Parallelizable");
    }

    private static AttributeSyntax? FindAssemblyLevelOfParallelism(CompilationUnitSyntax compilationUnit)
    {
        return compilationUnit.AttributeLists
            .Where(attributeList => attributeList.Target?.Identifier.ValueText == "assembly")
            .SelectMany(attributeList => attributeList.Attributes)
            .FirstOrDefault(attribute => NormalizeAttributeName(attribute.Name.ToString()) == "LevelOfParallelism");
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => TestAttributes.Contains(NormalizeAttributeName(attribute.Name.ToString())));
    }

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        return FindAttribute(attributeLists, attributeName) != null;
    }

    private static AttributeSyntax? FindAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        return attributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attribute => NormalizeAttributeName(attribute.Name.ToString()) == attributeName);
    }

    private static string GetArguments(AttributeSyntax attribute)
    {
        return attribute.ArgumentList == null
            ? string.Empty
            : string.Join(", ", attribute.ArgumentList.Arguments.Select(argument => argument.ToString()));
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

public sealed class ParallelClassAnalysisResult
{
    public int TestFixtures { get; set; }

    public int TestMethods { get; set; }

    public List<ParallelClassInfo> Classes { get; } = new();
}

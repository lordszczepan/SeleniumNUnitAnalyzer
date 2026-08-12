namespace SeleniumNUnitAnalyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class MethodUsageAnalyzer
{
    private static readonly HashSet<string> TestAttributes = new(StringComparer.Ordinal)
    {
        "Test",
        "TestCase",
        "Theory"
    };

    private static readonly HashSet<string> LifecycleAttributes = new(StringComparer.Ordinal)
    {
        "SetUp",
        "TearDown",
        "OneTimeSetUp",
        "OneTimeTearDown"
    };

    private readonly SyntaxTree _syntaxTree;
    private readonly string _targetClassName;
    private readonly string _targetMethodName;

    public MethodUsageAnalyzer(SyntaxTree syntaxTree, string targetClassName, string targetMethodName)
    {
        _syntaxTree = syntaxTree;
        _targetClassName = targetClassName;
        _targetMethodName = targetMethodName;
    }

    public MethodUsageAnalysisResult Analyze()
    {
        var result = new MethodUsageAnalysisResult();
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

            var classAliases = FindClassLevelAliases(classDeclaration);
            var testMethodNames = testMethods.Select(method => method.Identifier.Text).ToList();

            foreach (var testMethod in testMethods)
            {
                foreach (var invocation in FindTargetInvocations(classDeclaration, testMethod, classAliases))
                {
                    result.DirectTestUsages.Add(new DirectTestUsage
                    {
                        FilePath = _syntaxTree.FilePath,
                        Line = GetLine(invocation),
                        TestClassName = classDeclaration.Identifier.Text,
                        TestMethodName = testMethod.Identifier.Text,
                        Invocation = invocation.ToString()
                    });
                }
            }

            foreach (var lifecycleMethod in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                string? lifecycleKind = GetLifecycleKind(lifecycleMethod);
                if (lifecycleKind == null)
                {
                    continue;
                }

                foreach (var invocation in FindTargetInvocations(classDeclaration, lifecycleMethod, classAliases))
                {
                    result.LifecycleUsages.Add(new LifecycleUsage
                    {
                        FilePath = _syntaxTree.FilePath,
                        Line = GetLine(invocation),
                        TestClassName = classDeclaration.Identifier.Text,
                        LifecycleMethodName = lifecycleMethod.Identifier.Text,
                        LifecycleKind = lifecycleKind,
                        Invocation = invocation.ToString(),
                        TestMethods = testMethodNames
                    });
                }
            }
        }

        return result;
    }

    private IEnumerable<InvocationExpressionSyntax> FindTargetInvocations(
        ClassDeclarationSyntax classDeclaration,
        MethodDeclarationSyntax method,
        HashSet<string> classAliases)
    {
        var aliases = new HashSet<string>(classAliases, StringComparer.Ordinal);
        foreach (string alias in FindMethodLevelAliases(method))
        {
            aliases.Add(alias);
        }

        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => IsTargetInvocation(classDeclaration, aliases, invocation));
    }

    private bool IsTargetInvocation(
        ClassDeclarationSyntax classDeclaration,
        HashSet<string> aliases,
        InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text == _targetMethodName &&
                   classDeclaration.Identifier.Text == _targetClassName;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name.Identifier.Text != _targetMethodName)
        {
            return false;
        }

        return IsTargetReceiver(memberAccess.Expression, aliases);
    }

    private bool IsTargetReceiver(ExpressionSyntax expression, HashSet<string> aliases)
    {
        string receiver = expression.ToString();
        if (receiver == _targetClassName || receiver.EndsWith("." + _targetClassName, StringComparison.Ordinal))
        {
            return true;
        }

        if (aliases.Contains(receiver))
        {
            return true;
        }

        return expression is ObjectCreationExpressionSyntax objectCreation &&
               IsTargetType(objectCreation.Type.ToString());
    }

    private HashSet<string> FindClassLevelAliases(ClassDeclarationSyntax classDeclaration)
    {
        var aliases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in classDeclaration.Members.OfType<FieldDeclarationSyntax>())
        {
            if (!IsTargetType(field.Declaration.Type.ToString()))
            {
                continue;
            }

            foreach (var variable in field.Declaration.Variables)
            {
                aliases.Add(variable.Identifier.Text);
            }
        }

        foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (IsTargetType(property.Type.ToString()))
            {
                aliases.Add(property.Identifier.Text);
            }
        }

        return aliases;
    }

    private IEnumerable<string> FindMethodLevelAliases(MethodDeclarationSyntax method)
    {
        foreach (var declaration in method.DescendantNodes().OfType<VariableDeclarationSyntax>())
        {
            bool isTargetType = IsTargetType(declaration.Type.ToString());

            foreach (var variable in declaration.Variables)
            {
                if (isTargetType || IsTargetInitializer(variable.Initializer?.Value))
                {
                    yield return variable.Identifier.Text;
                }
            }
        }
    }

    private bool IsTargetInitializer(ExpressionSyntax? expression)
    {
        return expression switch
        {
            ObjectCreationExpressionSyntax objectCreation => IsTargetType(objectCreation.Type.ToString()),
            BinaryExpressionSyntax binaryExpression when binaryExpression.IsKind(SyntaxKind.AsExpression) =>
                IsTargetType(binaryExpression.Right.ToString()),
            CastExpressionSyntax castExpression => IsTargetType(castExpression.Type.ToString()),
            ParenthesizedExpressionSyntax parenthesizedExpression => IsTargetInitializer(parenthesizedExpression.Expression),
            _ => false
        };
    }

    private bool IsTargetType(string typeName)
    {
        string shortName = typeName.Split('.').Last().TrimEnd('?');
        return shortName == _targetClassName;
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => TestAttributes.Contains(NormalizeAttributeName(attribute.Name.ToString())));
    }

    private static string? GetLifecycleKind(MethodDeclarationSyntax method)
    {
        return method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Select(attribute => NormalizeAttributeName(attribute.Name.ToString()))
            .FirstOrDefault(LifecycleAttributes.Contains);
    }

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        return attributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => NormalizeAttributeName(attribute.Name.ToString()) == attributeName);
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

public sealed class MethodUsageAnalysisResult
{
    public int TestFixtures { get; set; }

    public int TestMethods { get; set; }

    public List<DirectTestUsage> DirectTestUsages { get; } = new();

    public List<LifecycleUsage> LifecycleUsages { get; } = new();
}

namespace SeleniumNUnitAnalyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RecursiveMethodUsageAnalyzer
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

    private readonly IReadOnlyList<SyntaxTree> _syntaxTrees;
    private readonly MethodId _target;

    public RecursiveMethodUsageAnalyzer(
        IReadOnlyList<SyntaxTree> syntaxTrees,
        string targetClassName,
        string targetMethodName)
    {
        _syntaxTrees = syntaxTrees;
        _target = new MethodId(targetClassName, targetMethodName);
    }

    public MethodUsageAnalysisResult Analyze()
    {
        var methods = CollectMethods();
        var reached = FindReachableMethods(methods);
        var result = new MethodUsageAnalysisResult();

        foreach (var testClass in methods
            .Where(method => method.IsTestFixture)
            .GroupBy(method => new { method.SyntaxTree.FilePath, method.ClassName }))
        {
            result.TestFixtures++;
            result.TestMethods += testClass.Count(method => method.IsTestMethod);
        }

        foreach (var method in methods.Where(method => method.IsTestMethod || method.LifecycleKind != null))
        {
            var invocations = FindInvocationsToReachedMethods(method, reached).ToList();
            if (invocations.Count == 0)
            {
                continue;
            }

            if (method.IsTestMethod)
            {
                foreach (var invocation in invocations)
                {
                    result.DirectTestUsages.Add(new DirectTestUsage
                    {
                        FilePath = method.SyntaxTree.FilePath,
                        Line = GetLine(invocation),
                        TestClassName = method.ClassName,
                        TestMethodName = method.MethodName,
                        Invocation = invocation.ToString()
                    });
                }
            }
            else if (method.LifecycleKind != null)
            {
                var testMethods = methods
                    .Where(candidate => candidate.ClassName == method.ClassName && candidate.IsTestMethod)
                    .Select(candidate => candidate.MethodName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                foreach (var invocation in invocations)
                {
                    result.LifecycleUsages.Add(new LifecycleUsage
                    {
                        FilePath = method.SyntaxTree.FilePath,
                        Line = GetLine(invocation),
                        TestClassName = method.ClassName,
                        LifecycleMethodName = method.MethodName,
                        LifecycleKind = method.LifecycleKind,
                        Invocation = invocation.ToString(),
                        TestMethods = testMethods
                    });
                }
            }
        }

        return result;
    }

    private List<MethodInfo> CollectMethods()
    {
        var methods = new List<MethodInfo>();

        foreach (var syntaxTree in _syntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classMethods = classDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .ToList();
                bool isTestFixture = HasAttribute(classDeclaration.AttributeLists, "TestFixture") ||
                                     classMethods.Any(IsTestMethod);
                var classAliases = FindClassLevelAliases(classDeclaration);

                foreach (var method in classMethods)
                {
                    methods.Add(new MethodInfo(
                        syntaxTree,
                        classDeclaration.Identifier.Text,
                        method.Identifier.Text,
                        method,
                        classAliases,
                        isTestFixture,
                        IsTestMethod(method),
                        GetLifecycleKind(method)));
                }
            }
        }

        return methods;
    }

    private HashSet<MethodId> FindReachableMethods(IReadOnlyList<MethodInfo> methods)
    {
        var reached = new HashSet<MethodId> { _target };
        bool changed;

        do
        {
            changed = false;
            foreach (var method in methods)
            {
                if (reached.Contains(method.Id))
                {
                    continue;
                }

                if (FindInvocationsToReachedMethods(method, reached).Any())
                {
                    reached.Add(method.Id);
                    changed = true;
                }
            }
        }
        while (changed);

        return reached;
    }

    private IEnumerable<InvocationExpressionSyntax> FindInvocationsToReachedMethods(
        MethodInfo method,
        HashSet<MethodId> reached)
    {
        var aliases = new HashSet<string>(method.ClassAliases, StringComparer.Ordinal);
        foreach (string alias in FindMethodLevelAliases(method.Syntax))
        {
            aliases.Add(alias);
        }

        return method.Syntax.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => IsInvocationToReachedMethod(method.ClassName, aliases, invocation, reached));
    }

    private bool IsInvocationToReachedMethod(
        string containingClassName,
        HashSet<string> aliases,
        InvocationExpressionSyntax invocation,
        HashSet<MethodId> reached)
    {
        if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            return reached.Contains(new MethodId(containingClassName, identifier.Identifier.Text));
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        string methodName = memberAccess.Name.Identifier.Text;
        return reached.Any(target =>
            target.MethodName == methodName &&
            IsTargetReceiver(containingClassName, aliases, memberAccess.Expression, target.ClassName));
    }

    private bool IsTargetReceiver(
        string containingClassName,
        HashSet<string> aliases,
        ExpressionSyntax expression,
        string targetClassName)
    {
        string receiver = expression.ToString();
        if (receiver == targetClassName || receiver.EndsWith("." + targetClassName, StringComparison.Ordinal))
        {
            return true;
        }

        if ((receiver == "this" || receiver == "base") && containingClassName == targetClassName)
        {
            return true;
        }

        if (aliases.Contains(receiver))
        {
            return true;
        }

        return expression is ObjectCreationExpressionSyntax objectCreation &&
               IsTargetType(objectCreation.Type.ToString(), targetClassName);
    }

    private HashSet<string> FindClassLevelAliases(ClassDeclarationSyntax classDeclaration)
    {
        var aliases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in classDeclaration.Members.OfType<FieldDeclarationSyntax>())
        {
            if (!IsTargetType(field.Declaration.Type.ToString(), _target.ClassName))
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
            if (IsTargetType(property.Type.ToString(), _target.ClassName))
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
            bool isTargetType = IsTargetType(declaration.Type.ToString(), _target.ClassName);

            foreach (var variable in declaration.Variables)
            {
                if (isTargetType || IsTargetInitializer(variable.Initializer?.Value, _target.ClassName))
                {
                    yield return variable.Identifier.Text;
                }
            }
        }
    }

    private bool IsTargetInitializer(ExpressionSyntax? expression, string targetClassName)
    {
        return expression switch
        {
            ObjectCreationExpressionSyntax objectCreation => IsTargetType(objectCreation.Type.ToString(), targetClassName),
            BinaryExpressionSyntax binaryExpression when binaryExpression.IsKind(SyntaxKind.AsExpression) =>
                IsTargetType(binaryExpression.Right.ToString(), targetClassName),
            CastExpressionSyntax castExpression => IsTargetType(castExpression.Type.ToString(), targetClassName),
            ParenthesizedExpressionSyntax parenthesizedExpression => IsTargetInitializer(parenthesizedExpression.Expression, targetClassName),
            _ => false
        };
    }

    private static bool IsTargetType(string typeName, string targetClassName)
    {
        string shortName = typeName.Split('.').Last().TrimEnd('?');
        return shortName == targetClassName;
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

    private sealed record MethodInfo(
        SyntaxTree SyntaxTree,
        string ClassName,
        string MethodName,
        MethodDeclarationSyntax Syntax,
        HashSet<string> ClassAliases,
        bool IsTestFixture,
        bool IsTestMethod,
        string? LifecycleKind)
    {
        public MethodId Id { get; } = new(ClassName, MethodName);
    }

    private sealed record MethodId(string ClassName, string MethodName);
}

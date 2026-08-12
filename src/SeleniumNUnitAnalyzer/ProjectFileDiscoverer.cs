namespace SeleniumNUnitAnalyzer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public static class ProjectFileDiscoverer
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
        "packages",
        "TestResults"
    };

    public static IReadOnlyList<string> DiscoverCSharpFiles(string rootDirectory)
    {
        var projectFiles = FindProjectFiles(rootDirectory);
        if (projectFiles.Count == 0)
        {
            return DiscoverCSharpFilesFromDirectory(rootDirectory).ToList();
        }

        var files = projectFiles
            .SelectMany(DiscoverCSharpFilesFromProject)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return files.Count > 0
            ? files
            : DiscoverCSharpFilesFromDirectory(rootDirectory).ToList();
    }

    private static IReadOnlyList<string> FindProjectFiles(string rootDirectory)
    {
        var topLevelProjectFiles = Directory
            .EnumerateFiles(rootDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (topLevelProjectFiles.Count > 0)
        {
            return topLevelProjectFiles;
        }

        return EnumerateProjectFiles(rootDirectory)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> EnumerateProjectFiles(string rootDirectory)
    {
        var pending = new Stack<string>();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            string current = pending.Pop();

            foreach (string directory in Directory.EnumerateDirectories(current))
            {
                if (!ExcludedDirectories.Contains(Path.GetFileName(directory)))
                {
                    pending.Push(directory);
                }
            }

            foreach (string file in Directory.EnumerateFiles(current, "*.csproj"))
            {
                yield return file;
            }
        }
    }

    private static IReadOnlyList<string> DiscoverCSharpFilesFromProject(string projectFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile) ?? ".";
        var document = XDocument.Load(projectFile);
        var compileItems = document
            .Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .ToList();

        var includePatterns = compileItems
            .SelectMany(element => SplitMsBuildItemList((string?)element.Attribute("Include")))
            .ToList();
        var removePatterns = compileItems
            .SelectMany(element => SplitMsBuildItemList((string?)element.Attribute("Remove")))
            .ToList();

        var files = includePatterns.Count > 0
            ? includePatterns.SelectMany(pattern => ResolveProjectPattern(projectDirectory, pattern))
            : DiscoverCSharpFilesFromDirectory(projectDirectory);

        return files
            .Where(File.Exists)
            .Where(file => !IsRemoved(projectDirectory, file, removePatterns))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> ResolveProjectPattern(string projectDirectory, string pattern)
    {
        string normalizedPattern = pattern.Replace('/', Path.DirectorySeparatorChar);
        if (HasWildcard(normalizedPattern))
        {
            return DiscoverCSharpFilesFromDirectory(projectDirectory)
                .Where(file => MatchesPattern(projectDirectory, file, normalizedPattern));
        }

        return [Path.GetFullPath(Path.Combine(projectDirectory, normalizedPattern))];
    }

    private static bool IsRemoved(string projectDirectory, string file, IReadOnlyList<string> removePatterns)
    {
        return removePatterns.Any(pattern =>
        {
            string normalizedPattern = pattern.Replace('/', Path.DirectorySeparatorChar);
            if (HasWildcard(normalizedPattern))
            {
                return MatchesPattern(projectDirectory, file, normalizedPattern);
            }

            string removedFile = Path.GetFullPath(Path.Combine(projectDirectory, normalizedPattern));
            return string.Equals(removedFile, file, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static IEnumerable<string> DiscoverCSharpFilesFromDirectory(string rootDirectory)
    {
        var pending = new Stack<string>();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            string current = pending.Pop();

            foreach (string directory in Directory.EnumerateDirectories(current))
            {
                if (!ExcludedDirectories.Contains(Path.GetFileName(directory)))
                {
                    pending.Push(directory);
                }
            }

            foreach (string file in Directory.EnumerateFiles(current, "*.cs"))
            {
                yield return Path.GetFullPath(file);
            }
        }
    }

    private static IEnumerable<string> SplitMsBuildItemList(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool HasWildcard(string pattern)
    {
        return pattern.Contains('*') || pattern.Contains('?');
    }

    private static bool MatchesPattern(string projectDirectory, string file, string pattern)
    {
        string relativePath = Path.GetRelativePath(projectDirectory, file)
            .Replace(Path.DirectorySeparatorChar, '/');
        string normalizedPattern = pattern.Replace(Path.DirectorySeparatorChar, '/');
        string regexPattern = "^" + Regex.Escape(normalizedPattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", "[^/]") + "$";

        return Regex.IsMatch(relativePath, regexPattern, RegexOptions.IgnoreCase);
    }
}

namespace SeleniumNUnitAnalyzer;

using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class CommandLineOptions
{
    public string TargetDirectory { get; set; } = string.Empty;

    public string? ReportPath { get; set; }

    public bool AllowNonParallelFixtures { get; set; }

    public bool FindUsages { get; set; }

    public bool ListCategories { get; set; }

    public bool ProductMatrix { get; set; }

    public bool MissingProducts { get; set; }

    public bool ParallelClasses { get; set; }

    public string? TargetClassName { get; set; }

    public string? TargetMethodName { get; set; }

    public List<string> UnknownOptions { get; } = [];

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--report":
                    if (i + 1 < args.Length)
                    {
                        options.ReportPath = args[++i].Trim('"');
                    }
                    break;
                case "--allow-non-parallel-fixtures":
                    options.AllowNonParallelFixtures = true;
                    break;
                case "--find-usages":
                    options.FindUsages = true;
                    break;
                case "--list-categories":
                    options.ListCategories = true;
                    break;
                case "--product-matrix":
                    options.ProductMatrix = true;
                    break;
                case "--missing-products":
                    options.MissingProducts = true;
                    break;
                case "--parallel-classes":
                    options.ParallelClasses = true;
                    break;
                case "--class":
                    if (i + 1 < args.Length)
                    {
                        options.TargetClassName = args[++i].Trim('"');
                    }
                    break;
                case "--method":
                    if (i + 1 < args.Length)
                    {
                        options.TargetMethodName = args[++i].Trim('"');
                    }
                    break;
                default:
                    if (arg.StartsWith("--"))
                    {
                        options.UnknownOptions.Add(arg);
                    }
                    else
                    {
                        options.TargetDirectory = arg.Trim('"');
                    }
                    break;
            }
        }

        return options;
    }

    public bool IsValid(out string errorMessage)
    {
        if (UnknownOptions.Count > 0)
        {
            errorMessage = $"Unknown option: {string.Join(", ", UnknownOptions)}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetDirectory))
        {
            errorMessage = "Target directory is required.";
            return false;
        }

        if (!Directory.Exists(TargetDirectory))
        {
            errorMessage = $"Target directory does not exist: {TargetDirectory}";
            return false;
        }

        int selectedModes = new[] { FindUsages, ListCategories, ProductMatrix, MissingProducts, ParallelClasses }
            .Count(selected => selected);
        if (selectedModes > 1)
        {
            errorMessage = "Use only one report mode at a time.";
            return false;
        }

        bool hasUsageTarget = !string.IsNullOrWhiteSpace(TargetClassName) || !string.IsNullOrWhiteSpace(TargetMethodName);
        if (hasUsageTarget && !FindUsages)
        {
            errorMessage = "Use --class/--method only with --find-usages.";
            return false;
        }

        if (FindUsages || hasUsageTarget)
        {
            if (string.IsNullOrWhiteSpace(TargetClassName))
            {
                errorMessage = "Target class is required when finding usages. Use --class <class-name>.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(TargetMethodName))
            {
                errorMessage = "Target method is required when finding usages. Use --method <method-name>.";
                return false;
            }

            FindUsages = true;
        }

        errorMessage = string.Empty;
        return true;
    }
}

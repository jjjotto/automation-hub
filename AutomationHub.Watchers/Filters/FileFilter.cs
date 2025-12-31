using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AutomationHub.Watchers.Filters;

public abstract class FileFilter
{
    public string Pattern { get; }

    protected FileFilter(string pattern)
    {
        Pattern = pattern ?? string.Empty;
    }

    protected static string GetComparableName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.GetFileName(trimmed) ?? string.Empty;
    }

    public abstract bool Matches(string path);

    public static FileFilter Create(string kind, string pattern)
    {
        return kind?.ToLowerInvariant() switch
        {
            "startswith" => new StartsWithFilter(pattern),
            "endswith" => new EndsWithFilter(pattern),
            "contains" => new ContainsFilter(pattern),
            "regex" => new RegexFilter(pattern),
            _ => new AllFileFilter(pattern)
        };
    }
}

public sealed class AllFileFilter : FileFilter
{
    public AllFileFilter(string pattern) : base(pattern)
    {
    }

    public override bool Matches(string path) => !string.IsNullOrWhiteSpace(path);
}

public sealed class StartsWithFilter : FileFilter
{
    public StartsWithFilter(string pattern) : base(pattern)
    {
    }

    public override bool Matches(string path) => GetComparableName(path).StartsWith(Pattern, StringComparison.OrdinalIgnoreCase);
}

public sealed class EndsWithFilter : FileFilter
{
    public EndsWithFilter(string pattern) : base(pattern)
    {
    }

    public override bool Matches(string path) => GetComparableName(path).EndsWith(Pattern, StringComparison.OrdinalIgnoreCase);
}

public sealed class ContainsFilter : FileFilter
{
    public ContainsFilter(string pattern) : base(pattern)
    {
    }

    public override bool Matches(string path) => GetComparableName(path).Contains(Pattern, StringComparison.OrdinalIgnoreCase);
}

public sealed class RegexFilter : FileFilter
{
    private readonly Regex _regex;

    public RegexFilter(string pattern) : base(pattern)
    {
        _regex = new Regex(pattern ?? string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public override bool Matches(string path) => _regex.IsMatch(GetComparableName(path));
}

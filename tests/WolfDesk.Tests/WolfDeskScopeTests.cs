using System.Text.RegularExpressions;

namespace dev.kaldiroglu.WolfDesk.Tests;

/// <summary>
/// What the README's "What WolfDesk is, and what it is not" says: a library with a demo, no
/// network API, no database, and no package but the test framework. These tests fail when
/// the code stops matching it.
/// </summary>
public partial class WolfDeskScopeTests
{
    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WolfDesk.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("No WolfDesk.slnx above the test run");
    }

    private static IEnumerable<string> Files(string folder, string pattern) =>
        Directory.EnumerateFiles(Path.Combine(Root, folder), pattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Order();

    private static string Relative(string path) => Path.GetRelativePath(Root, path).Replace('\\', '/');

    [GeneratedRegex("PackageReference Include=\"([\\w.]+)\"")]
    private static partial Regex PackageReference();

    [Fact]
    public void TheOnlyPackagesAreTheTestFrameworks()
    {
        var packages = Files(".", "*.csproj")
            .SelectMany(p => PackageReference().Matches(File.ReadAllText(p))
                .Select(m => $"{Relative(p)}: {m.Groups[1].Value}"))
            .ToList();
        Assert.Equal(
        [
            "tests/WolfDesk.Tests/WolfDesk.Tests.csproj: Microsoft.NET.Test.Sdk",
            "tests/WolfDesk.Tests/WolfDesk.Tests.csproj: xunit",
            "tests/WolfDesk.Tests/WolfDesk.Tests.csproj: xunit.runner.visualstudio",
        ], packages);
    }

    [Fact]
    public void EveryProjectUsesThePlainSdk()
    {
        var sdks = Files(".", "*.csproj")
            .Where(p => !File.ReadAllText(p).Contains("<Project Sdk=\"Microsoft.NET.Sdk\">"))
            .Select(Relative)
            .ToList();
        Assert.Empty(sdks);
    }

    [GeneratedRegex(@"System\.Net|System\.Data|\bHttp[A-Z]\w*|\bSocket\w*|AspNetCore|DbConnection|SqlConnection|Npgsql")]
    private static partial Regex NetworkOrDatabase();

    [Fact]
    public void TheCodeUsesNoNetworkOrDatabaseTypes()
    {
        var found = Files("src", "*.cs")
            .SelectMany(f => File.ReadLines(f).Where(l => NetworkOrDatabase().IsMatch(l))
                .Select(l => $"{Relative(f)}: {l.Trim()}"))
            .ToList();
        Assert.Empty(found);
    }

    [Fact]
    public void TheDemoIsTheOnlyProgram()
    {
        var executables = Files(".", "*.csproj")
            .Where(p => File.ReadAllText(p).Contains("<OutputType>Exe</OutputType>"))
            .Select(Relative)
            .ToList();
        Assert.Equal(["src/WolfDesk.Demo/WolfDesk.Demo.csproj"], executables);

        var mains = Files("src", "*.cs")
            .Where(f => File.ReadLines(f).Any(l => l.Contains("static void Main(")))
            .Select(Relative)
            .ToList();
        Assert.Equal(["src/WolfDesk.Demo/Program.cs"], mains);
    }
}

using System.Text.RegularExpressions;

namespace dev.kaldiroglu.WolfDesk.Tests;

/// <summary>
/// Billing and tenants are layered (chapter 8): each layer is a namespace, and each layer
/// depends only on the layer beneath it (p. 120). The one exception is the book's own: a
/// service layer starts and ends the database transaction itself, as <c>UserService.Create</c>
/// does on p. 123. A file reaches another layer through a <c>using</c>, or through a name
/// qualified from the context's namespace, such as <c>BusinessLogic.VolumeDiscount</c>;
/// these tests read both, outside comments.
/// </summary>
public partial class LayeredArchitectureTests
{
    private const string Root = "dev.kaldiroglu.WolfDesk.";
    private static readonly string Source = Path.Combine(FindRoot(), "src", "WolfDesk");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WolfDesk.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("No WolfDesk.slnx above the test run");
    }

    [GeneratedRegex(@"^using dev\.kaldiroglu\.WolfDesk\.([\w.]+);", RegexOptions.Multiline)]
    private static partial Regex Using();

    [GeneratedRegex(@"//[^\n]*|/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();

    private static string Code(string file) => Comment().Replace(File.ReadAllText(file), " ");

    private static IEnumerable<string> FilesOf(string layer) =>
        Directory.EnumerateFiles(Path.Combine(Source, layer.Replace('.', Path.DirectorySeparatorChar)), "*.cs");

    /// <summary>Every WolfDesk namespace the files of <paramref name="layer"/> reach, other than their own.</summary>
    private static SortedSet<string> DependenciesOf(string layer, IEnumerable<string> siblings)
    {
        var context = layer[..layer.IndexOf('.')];
        var found = new SortedSet<string>();
        foreach (var file in FilesOf(layer))
        {
            var code = Code(file);
            foreach (Match m in Using().Matches(code)) found.Add(m.Groups[1].Value);
            foreach (var sibling in siblings)
            {
                var name = sibling[(context.Length + 1)..];
                if (Regex.IsMatch(code, $@"(?<![\w.]){Regex.Escape(name)}\.[A-Z]")
                    || code.Contains($"{context}.{name}.") || code.Contains(Root + sibling + "."))
                    found.Add(sibling);
            }
        }
        found.Remove(layer);
        return found;
    }

    private static SortedSet<string> LayersOf(string context) =>
        new(Directory.EnumerateDirectories(Path.Combine(Source, context))
            .Select(d => $"{context}.{Path.GetFileName(d)}"));

    private static void AssertLayers(string context, Dictionary<string, string[]> layers)
    {
        Assert.Equal(new SortedSet<string>(layers.Keys), LayersOf(context));
        foreach (var (layer, allowed) in layers)
        {
            Assert.True(new SortedSet<string>(allowed).SetEquals(DependenciesOf(layer, layers.Keys)),
                $"{layer} depends on {string.Join(", ", DependenciesOf(layer, layers.Keys))}");
        }
    }

    [Fact]
    public void BillingHasThreeLayersAndEachDependsOnlyOnTheOneBeneathIt() =>
        AssertLayers("Billing", new()
        {
            ["Billing.Presentation"] = ["Billing.BusinessLogic", "Ticketing.PublishedLanguage"],
            ["Billing.BusinessLogic"] = ["Billing.DataAccess"],
            ["Billing.DataAccess"] = [],
        });

    [Fact]
    public void TenantsHasThreeLayersAndEachDependsOnlyOnTheOneBeneathIt() =>
        AssertLayers("Tenants", new()
        {
            ["Tenants.Service"] = ["Tenants.BusinessLogic", "Tenants.DataAccess"],
            ["Tenants.BusinessLogic"] = ["Tenants.DataAccess"],
            ["Tenants.DataAccess"] = [],
        });

    [GeneratedRegex(@"\bdb\.(\w+)\(")]
    private static partial Regex DatabaseCall();

    [Fact]
    public void TenantsServiceLayerUsesTheDatabaseOnlyForTheTransaction()
    {
        var calls = new SortedSet<string>(FilesOf("Tenants.Service")
            .SelectMany(f => DatabaseCall().Matches(Code(f)).Select(m => m.Groups[1].Value)));
        Assert.Equal(["Commit", "Rollback", "StartTransaction"], calls);
    }
}

using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis.MSBuild;

namespace ProjectDependencyAnalyzer.ConsolePlayground;

internal class Runner2
{
    public static async Task Run(string projectPath)
    {
        // --- 1) Load the raw MSBuild project ---
        var projectCollection = new ProjectCollection();
        var msbuildProject = projectCollection.LoadProject(projectPath);

        // 2) Gather sets of names:
        //   A) ProjectReference → project assembly names
        var projectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pr in msbuildProject.GetItems("ProjectReference"))
        {
            var include = pr.EvaluatedInclude;
            var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, include));
            var other = projectCollection.LoadProject(path);
            projectNames.Add(other.GetPropertyValue("AssemblyName"));
        }

        //   B) PackageReference → package IDs (we assume assembly name == package ID here; if different,
        //      you can also read <PackageReference><HintPath>…</HintPath></PackageReference> or
        //      use NuGet APIs to resolve)
        var packageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pkg in msbuildProject.GetItems("PackageReference"))
            packageNames.Add(pkg.EvaluatedInclude);

        // --- 3) Open Roslyn workspace & compilation ---
        using var ws = MSBuildWorkspace.Create();
        var proj = await ws.OpenProjectAsync(projectPath);
        var comp = await proj.GetCompilationAsync();

        // 4) Classify each referenced assembly
        var projRefs = new List<string>();
        var pkgRefs = new List<string>();
        var frameworkRefs = new List<string>();

        foreach (var asm in comp!.ReferencedAssemblyNames)
        {
            var name = asm.Name;
            if (projectNames.Contains(name))
                projRefs.Add(name);
            else if (packageNames.Contains(name))
                pkgRefs.Add(name);
            else
                frameworkRefs.Add(name);
        }

        // 5) Print
        Console.WriteLine("=== ProjectReferences ===");
        foreach (var n in projRefs.OrderBy(x => x))
            Console.WriteLine($"  • {n}");

        Console.WriteLine("\n=== PackageReferences ===");
        foreach (var n in pkgRefs.OrderBy(x => x))
            Console.WriteLine($"  • {n}");

        Console.WriteLine("\n=== Framework/SDK References ===");
        foreach (var n in frameworkRefs.OrderBy(x => x))
            Console.WriteLine($"  • {n}");
    }
}

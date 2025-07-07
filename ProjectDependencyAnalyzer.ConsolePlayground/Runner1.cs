using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace ProjectDependencyAnalyzer.ConsolePlayground;

internal class Runner1
{
    public static async Task Run(string projectPath)
    {
        using var workspace = MSBuildWorkspace.Create();

        Console.WriteLine("Loading project...");
        Project project = await workspace.OpenProjectAsync(projectPath);
        Compilation? compilation = await project.GetCompilationAsync();

        if (compilation == null)
        {
            Console.WriteLine("Could not compile project.");
            return;
        }

        var usedAssemblies = new HashSet<string>();

        Console.WriteLine("Analyzing symbol usage...");
        foreach (Document document in project.Documents)
        {
            SyntaxTree? syntax = await document.GetSyntaxTreeAsync();
            if (syntax == null)
            {
                continue;
            }

            SemanticModel semanticModel = compilation.GetSemanticModel(syntax);
            SyntaxNode root = await syntax.GetRootAsync();

            IEnumerable<IdentifierNameSyntax> identifiers = root
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>();

            foreach (var identifier in identifiers)
            {
                ISymbol? symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
                if (symbol == null)
                {
                    continue;
                }

                var containingAssembly = symbol.ContainingAssembly;
                if (containingAssembly != null && !containingAssembly.IsInteractive && !containingAssembly.IsImplicitlyDeclared)
                {
                    usedAssemblies.Add(containingAssembly.Name);
                }
            }
        }

        Console.WriteLine($"\nUsed assemblies ({usedAssemblies.Count}):");
        foreach (string? asm in usedAssemblies.OrderBy(x => x))
        {
            Console.WriteLine($"- {asm}");
        }

        // 1) Collect into separate lists
        var usedRefs = new List<string>();
        var unusedRefs = new List<string>();

        foreach (AssemblyIdentity reference in compilation.ReferencedAssemblyNames)
        {
            if (usedAssemblies.Contains(reference.Name))
            {
                usedRefs.Add(reference.Name);
            }
            else
            {
                unusedRefs.Add(reference.Name);
            }
        }

        // 2) Print USED group
        Console.WriteLine("\nUSED project references:");
        foreach (string? name in usedRefs.OrderBy(n => n))
        {
            Console.WriteLine($"[USED] {name}");
        }

        // 3) Print UNUSED group
        Console.WriteLine("\nUNUSED project references:");
        foreach (string? name in unusedRefs.OrderBy(n => n))
        {
            Console.WriteLine($"[UNUSED] {name}");
        }
    }
}

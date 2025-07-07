using Microsoft.Build.Locator;
using ProjectDependencyAnalyzer.ConsolePlayground;

IEnumerable<VisualStudioInstance> query = MSBuildLocator.QueryVisualStudioInstances();

var instance = query.MaxBy(x => x.Version);

if (instance == null)
{
    throw new ArgumentException("Please install the latest .NET SDK");
}

MSBuildLocator.RegisterInstance(instance);

string projectPath = "";

await Runner1.Run(projectPath);

Console.ReadKey();

//GetProjReferences(csprojPath);

//static void GetProjReferences(string csprojPath)
//{
//    var pc = new ProjectCollection();
//    var project = pc.LoadProject(csprojPath);

//    var projectRefs = project.GetItems("ProjectReference");

//    foreach (var pr in projectRefs)
//    {
//        string include = pr.EvaluatedInclude;
//        string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csprojPath), include));

//        Console.WriteLine("→ Project reference:");
//        Console.WriteLine("  Relative: " + include);
//        Console.WriteLine("  Full:     " + fullPath);
//    }
//}
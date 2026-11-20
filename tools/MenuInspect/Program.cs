using Mono.Cecil;

string assemblyPath = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/steamapps/common/Dyson Sphere Program/DSPGAME_Data/Managed/Assembly-CSharp.dll");

if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
    Console.Error.WriteLine("Usage: dotnet run --project MenuInspect.csproj -- <path-to-Assembly-CSharp.dll>");
    Environment.Exit(1);
}

var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
var menu = FindType("UIMainMenu");
var button = FindType("UIMainMenuButton");

Console.WriteLine($"Assembly: {assemblyPath}");
Console.WriteLine("UIMainMenu fields:");
foreach (var field in menu.Fields)
    Console.WriteLine($"  {field.Attributes} {field.FieldType} {field.Name}");

PrintMethods(menu, "_OnCreate", "_OnOpen", "_OnClose", "OnNewGameButtonClick", "OnContinueButtonClick", "OnLoadGameButtonClick", "OnOptionsButtonClick");

Console.WriteLine("UIMainMenuButton fields:");
foreach (var field in button.Fields)
    Console.WriteLine($"  {field.Attributes} {field.FieldType} {field.Name}");

var root = FindType("UIRoot");
PrintMethods(root, "OpenMainMenuUI", "CloseMainMenuUI");

TypeDefinition FindType(string name) => assembly.MainModule.Types.First(type => type.Name == name);

void PrintMethods(TypeDefinition type, params string[] methodNames)
{
    foreach (var method in type.Methods.Where(method => methodNames.Contains(method.Name)))
    {
        Console.WriteLine($"METHOD {method.FullName}");
        foreach (var parameter in method.Parameters)
            Console.WriteLine($"  {parameter.Index}: {parameter.ParameterType} {parameter.Name}");
        if (method.HasBody)
            foreach (var instruction in method.Body.Instructions)
                Console.WriteLine($"  {instruction}");
    }
}

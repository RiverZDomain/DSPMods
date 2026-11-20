using Mono.Cecil;

string assemblyPath = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/steamapps/common/Dyson Sphere Program/DSPGAME_Data/Managed/Assembly-CSharp.dll");

if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
    Console.Error.WriteLine("Usage: dotnet run --project MinerInspect.csproj -- <path-to-Assembly-CSharp.dll>");
    Environment.Exit(1);
}

var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
var module = assembly.MainModule;
var miner = FindType("MinerComponent");
var station = FindType("StationComponent");

Console.WriteLine($"Assembly: {assemblyPath}");
Console.WriteLine($"MinerComponent value type: {miner.IsValueType}");
PrintMethods(miner, "InternalUpdate");
PrintMethods(station, "UpdateVeinCollection", "InternalTickLocal", "HasLocalSupply", "HasLocalDemand", "RematchLocalPairs", "DetermineDispatch");
PrintEnum("EMinerType");
PrintEnum("ELogisticStorage");

var store = FindType("StationStore");
Console.WriteLine("StationStore fields:");
foreach (var field in store.Fields)
    Console.WriteLine($"  {field.FieldType} {field.Name}");

Console.WriteLine("NewMinerComponent call sites:");
foreach (var type in module.Types)
foreach (var method in type.Methods.Where(method => method.HasBody))
foreach (var instruction in method.Body.Instructions)
    if (instruction.Operand is MethodReference reference && reference.Name == "NewMinerComponent")
        Console.WriteLine($"  {method.FullName}: IL_{instruction.Offset:x4}");

TypeDefinition FindType(string name) => module.Types.First(type => type.Name == name);

void PrintEnum(string name)
{
    var type = FindType(name);
    Console.WriteLine($"{name} values:");
    foreach (var field in type.Fields.Where(field => field.IsLiteral))
        Console.WriteLine($"  {field.Name} = {field.Constant}");
}

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

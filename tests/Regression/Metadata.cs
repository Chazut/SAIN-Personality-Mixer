using Mono.Cecil;

internal static class Metadata
{
    internal static void Check(string pluginPath, string sainDirectory, string game, Action<bool, string> check)
    {
        using var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(sainDirectory);
        resolver.AddSearchDirectory(Path.Combine(game, "BepInEx", "core"));
        resolver.AddSearchDirectory(Path.Combine(game, "EscapeFromTarkov_Data", "Managed"));
        using var plugin = AssemblyDefinition.ReadAssembly(pluginPath, new ReaderParameters { AssemblyResolver = resolver });
        using var sain = AssemblyDefinition.ReadAssembly(Path.Combine(sainDirectory, "SAIN.dll"), new ReaderParameters { AssemblyResolver = resolver });
        var target = sain.MainModule.GetType("SAIN.Models.Preset.Personalities.PersonalityDictionary").Methods.Single(m => m.Name == "GetPersonality");
        var entry = plugin.MainModule.GetType("SAINPersonalityMixer.Plugin");
        var prefix = entry.Methods.Single(m => m.Name == "Prefix");
        check(target.Parameters.Count == 2 && target.Parameters[1].IsOut, "SAIN target shape changed");
        check(prefix.Parameters[0].ParameterType.FullName == target.DeclaringType.FullName, "Harmony instance signature");
        check(prefix.Parameters[1].ParameterType.FullName == target.Parameters[0].ParameterType.FullName, "Harmony first argument signature");
        check(prefix.Parameters[2].ParameterType.FullName == target.Parameters[1].ParameterType.FullName, "Harmony out argument signature");
        check(prefix.Parameters[3].ParameterType.FullName == target.ReturnType.FullName + "&", "Harmony result signature");
        foreach (var member in plugin.MainModule.GetMemberReferences().Where(m => m.DeclaringType.Namespace.StartsWith("SAIN.")))
        {
            var resolved = member switch
            {
                MethodReference method => method.Resolve() != null,
                FieldReference field => field.Resolve() != null,
                _ => true
            };
            check(resolved, "Unresolved SAIN API: " + member.FullName);
        }
        var dependency = entry.CustomAttributes.Single(a => a.AttributeType.Name == "BepInDependency");
        check((string)dependency.ConstructorArguments[0].Value == "me.sol.sain", "SAIN hard dependency missing");
        check(plugin.Name.Version.ToString() == "1.0.0.0", "Plugin version");
        foreach (var member in sain.MainModule.GetMemberReferences().Where(m => m.DeclaringType.GetElementType().Scope.Name == "SAIN.Preset.Shared"))
        {
            bool resolved = member switch
            {
                MethodReference method => method.Resolve() != null,
                FieldReference field => field.Resolve() != null,
                _ => true
            };
            check(resolved, "SAIN cannot use the selected Shared assembly: " + member.FullName);
        }
        using var gameAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(game, "EscapeFromTarkov_Data", "Managed", "Assembly-CSharp.dll"));
        var roles = gameAssembly.MainModule.GetType("EFT.WildSpawnType");
        check((int)roles.Fields.Single(f => f.Name == "pmcBEAR").Constant == 51, "BEAR boundary role differs from installed game");
        check((int)roles.Fields.Single(f => f.Name == "pmcUSEC").Constant == 52, "USEC boundary role differs from installed game");
        Console.WriteLine("Binary API validation passed: " + sainDirectory);
    }
}

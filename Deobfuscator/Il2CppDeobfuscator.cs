using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector.Cpp;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.Reflection;

namespace Beebyte_Deobfuscator.Deobfuscator
{
    public class Il2CppDeobfuscator : IDeobfuscator
    {
        public Il2CppDeobfuscator()
        {

        }
        public LookupModel Process(TypeModel model, BeebyteDeobfuscatorPlugin plugin)
        {
            PluginServices services = PluginServices.For(plugin);

            services.StatusUpdate("Loading unobfuscated application");
            var il2cppClean = Il2CppInspector.Il2CppInspector.LoadFromFile(plugin.BinaryPath.Value, plugin.MetadataPath.Value, statusCallback: services.StatusUpdate);
            if (plugin.CompilerType.Value != CppCompiler.GuessFromImage(il2cppClean[0].BinaryImage)) throw new System.ArgumentException("Cross compiler deobfuscation has not been implemented yet");
            services.StatusUpdate("Creating type model for unobfuscated application");
            var modelClean = new TypeModel(il2cppClean[0]);

            services.StatusUpdate("Creating LookupModel for obfuscated application");
            LookupModel lookupModel = new LookupModel(model, modelClean, plugin.NamingRegex.Value);
            services.StatusUpdate("Deobfuscating binary");
            lookupModel.TranslateTypes(true);
            return lookupModel;
        }
    }
}
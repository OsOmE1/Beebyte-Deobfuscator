using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector.Cpp;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.Reflection;

namespace Beebyte_Deobfuscator.Deobfuscator
{
    public class Il2CppDeobfuscator : IDeobfuscator
    {
        public LookupModule Process(TypeModel model, BeebyteDeobfuscatorPlugin plugin)
        {
            if (!plugin.CompilerType.HasValue)
            {
                return null;
            }

            PluginServices services = PluginServices.For(plugin);

            services.StatusUpdate("Loading unobfuscated application");
            var il2cppClean = Il2CppInspector.Il2CppInspector.LoadFromPackage(new[] { plugin.BinaryPath }, statusCallback: services.StatusUpdate);
            if (il2cppClean == null)
            {
                il2cppClean = Il2CppInspector.Il2CppInspector.LoadFromFile(plugin.BinaryPath, plugin.MetadataPath, statusCallback: services.StatusUpdate);
            }

            if (il2cppClean == null)
            {
                throw new System.ArgumentException("Could not load unobfuscated application");
            }

            if (plugin.CompilerType.Value != CppCompiler.GuessFromImage(il2cppClean[0].BinaryImage))
            {
                throw new System.ArgumentException("Cross compiler deobfuscation has not been implemented yet");
            }
            services.StatusUpdate("Creating type model for unobfuscated application");
            var modelClean = new TypeModel(il2cppClean[0]);
            if (modelClean == null)
            {
                throw new System.ArgumentException("Could not create type model for unobfuscated application");
            }

            services.StatusUpdate("Creating LookupModel for obfuscated application");
            LookupModule lookupModel = new LookupModule(plugin.NamingRegex);
            lookupModel.Init(model.ToLookupModel(statusCallback: services.StatusUpdate), modelClean.ToLookupModel(statusCallback: services.StatusUpdate));
            services.StatusUpdate("Deobfuscating binary");
            lookupModel.TranslateTypes(true, statusCallback: services.StatusUpdate);

            plugin.CompilerType = null;
            return lookupModel;
        }
    }
}
using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector.Cpp;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.Reflection;

namespace Beebyte_Deobfuscator.Deobfuscator
{
    public class MonoDeobfuscator : IDeobfuscator
    {
        public MonoDeobfuscator()
        {

        }
        public LookupModel Process(TypeModel model, BeebyteDeobfuscatorPlugin plugin)
        {
            PluginServices services = PluginServices.For(plugin);

            services.StatusUpdate("Creating model for Mono dll");
            if (plugin.CompilerType.Value != CppCompilerType.MSVC) throw new System.ArgumentException("Cross compiler deobfuscation has not been implemented yet");
            MonoDecompiler.MonoDecompiler monoDecompiler = MonoDecompiler.MonoDecompiler.FromFile(plugin.MonoPath);
            if (monoDecompiler == null) return null;

            services.StatusUpdate("Creating LookupModel for obfuscated application");

            LookupModel lookupModel = new LookupModel(plugin.NamingRegex);
            lookupModel.Init(model.ToLookupModule(lookupModel), monoDecompiler.GetLookupModule(lookupModel));
            services.StatusUpdate("Deobfuscating binary");
            lookupModel.TranslateTypes();
            return lookupModel;
        }
    }
}

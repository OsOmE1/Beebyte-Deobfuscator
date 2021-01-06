using Beebyte_Deobfuscator.Lookup;
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
            MonoDecompiler.MonoDecompiler monoDecompiler = MonoDecompiler.MonoDecompiler.FromFile(plugin.MonoPath.Value);
            if (monoDecompiler == null) return null;

            services.StatusUpdate("Creating LookupModel for obfuscated application");

            LookupModel lookupModel = new LookupModel(model, monoDecompiler.GetTypes(), plugin.NamingRegex.Value);
            services.StatusUpdate("Deobfuscating binary");
            lookupModel.TranslateTypes();
            return lookupModel;
        }
    }
}

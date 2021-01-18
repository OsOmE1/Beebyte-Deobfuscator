using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector;
using Il2CppInspector.Cpp;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.Reflection;

namespace Beebyte_Deobfuscator.Deobfuscator
{
    public class ApkDeobfuscator : IDeobfuscator
    {
        public ApkDeobfuscator()
        {

        }
        public LookupModel Process(TypeModel model, BeebyteDeobfuscatorPlugin plugin)
        {
            PluginServices services = PluginServices.For(plugin);
            services.StatusUpdate("Loading unobfuscated APK");

            var il2cppClean = Il2CppInspector.Il2CppInspector.LoadFromPackage(new[] { plugin.ApkPath });
            if (plugin.CompilerType.Value != CppCompiler.GuessFromImage(il2cppClean[0].BinaryImage))
            {
                throw new System.ArgumentException("Cross compiler deobfuscation has not been implemented yet");
            }
            
            services.StatusUpdate("Creating type model for unobfuscated APK");
            var modelClean = new TypeModel(il2cppClean[0]);

            services.StatusUpdate("Creating LookupModel for obfuscated APK");
            LookupModel lookupModel = new LookupModel(model, modelClean, plugin.NamingRegex);
            services.StatusUpdate("Deobfuscating binary");
            lookupModel.TranslateTypes();

            return lookupModel;
        }
    }
}
using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector;
using Il2CppInspector.PluginAPI;
using Il2CppInspector.Reflection;
using System.Collections.Generic;

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
            if (!(plugin.FileFormat is APKReader)) throw new System.ArgumentException("APK deobfuscation can only be used with obfuscated APKs");

            services.StatusUpdate("Loading unobfuscated APK");

            var il2cppClean = Il2CppInspector.Il2CppInspector.LoadFromPackage(new[] { plugin.ApkPath.Value }, new LoadOptions() { ImageBase = 0 });

            services.StatusUpdate("Creating type model for unobfuscated APK");
            var modelClean = new TypeModel(il2cppClean[0]);

            services.StatusUpdate("Creating LookupModel for obfuscated APK");
            LookupModel lookupModel = new LookupModel(model, modelClean, plugin.NamingRegex.Value);
            services.StatusUpdate("Deobfuscating binary");
            lookupModel.TranslateTypes();

            return lookupModel;
        }
    }
}
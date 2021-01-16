using dnlib.DotNet;
using System.Collections.Generic;
using System.IO;

namespace Beebyte_Deobfuscator.MonoDecompiler
{
    internal class MonoDecompiler
    {
        ModuleDefMD Module;
        public MonoDecompiler(ModuleDefMD module)
        {
            Module = module;
        }

        public IEnumerable<TypeDef> GetTypes() => Module.GetTypes();

        public static MonoDecompiler FromFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            ModuleContext modCtx = ModuleDef.CreateModuleContext();
            ModuleDefMD module = ModuleDefMD.Load(path, modCtx);

            return new MonoDecompiler(module);
        }
    }
}

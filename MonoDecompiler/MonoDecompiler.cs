using Beebyte_Deobfuscator.Lookup;
using dnlib.DotNet;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Beebyte_Deobfuscator.MonoDecompiler
{
    public class MonoDecompiler
    {
        ModuleDefMD Module;
        public MonoDecompiler(ModuleDefMD module)
        {
            Module = module;
        }

        public LookupModule GetLookupModule(LookupModel lookupModel)
        {
            List<LookupType> types = Module.GetTypes().ToLookupTypeList(lookupModel).ToList();
            List<string> namespaces = new List<string>();
            foreach(LookupType type in types)
            {
                if (type == null) continue;
                if (!namespaces.Contains(type.Namespace)) namespaces.Add(type.Namespace);
            }
            return new LookupModule(namespaces, types);
        }

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

using Beebyte_Deobfuscator.Lookup;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Beebyte_Deobfuscator.MonoDecompiler
{
    public class MonoDecompiler
    {
        private readonly ModuleDefMD Module;
        public MonoDecompiler(ModuleDefMD module)
        {
            Module = module;
        }

        public LookupModule GetLookupModule(LookupModel lookupModel, EventHandler<string> statusCallback = null)
        {
            List<LookupType> types = Module.GetTypes().ToLookupTypeList(lookupModel, statusCallback: statusCallback).ToList();
            List<string> namespaces = types.Where(t => t != null).Select(t => t.Namespace).Distinct().ToList();

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

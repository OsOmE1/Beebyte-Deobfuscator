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
        public readonly ModuleDefMD Module;
        public MonoDecompiler(ModuleDefMD module)
        {
            Module = module;
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

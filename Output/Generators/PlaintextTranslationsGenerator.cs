using Beebyte_Deobfuscator.Lookup;
using System.IO;
using System.Linq;

namespace Beebyte_Deobfuscator.Output.Generators
{
    class PlaintextTranslationsGenerator : IGenerator
    {
        public void Generate(BeebyteDeobfuscatorPlugin plugin, LookupModule module)
        {
            using var exportFile = new FileStream(plugin.ExportPath + Path.DirectorySeparatorChar + "translations.txt", FileMode.Create);
            StreamWriter output = new StreamWriter(exportFile);

            foreach (Translation translation in module.Translations.Where(t => t.CleanName != t.ObfName))
            {
                output.WriteLine($"{translation.ObfName}/{translation.CleanName}");
            }
            output.Close();
        }
    }
}

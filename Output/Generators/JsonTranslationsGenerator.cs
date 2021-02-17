using Beebyte_Deobfuscator.Lookup;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Beebyte_Deobfuscator.Output.Generators
{
    class JsonTranslations
    {
        public Dictionary<string, string> Types { get; set; }
        public Dictionary<string, string> Fields { get; set; }
    }
    class JsonTranslationsGenerator : IGenerator
    {
        public void Generate(BeebyteDeobfuscatorPlugin plugin, LookupModule module)
        {
            JsonTranslations translations = new JsonTranslations
            {
                Types = module.Translations.Where(t => t.Type == TranslationType.TypeTranslation && t.ObfName != t.CleanName).ToDictionary(t => t.ObfName, t => t.CleanName),
                Fields = module.Translations.Where(t => t.Type == TranslationType.FieldTranslation && t.ObfName != t.CleanName).ToDictionary(t => t.ObfName, t => t.CleanName),
            };
            using var exportFile = new FileStream(plugin.ExportPath + Path.DirectorySeparatorChar + "translations.json", FileMode.Create);
            StreamWriter output = new StreamWriter(exportFile);
            string jsonString = JsonSerializer.Serialize(translations);

            output.Write(jsonString);
            output.Close();
        }
    }
}

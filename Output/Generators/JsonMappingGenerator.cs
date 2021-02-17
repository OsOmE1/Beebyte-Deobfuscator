using Beebyte_Deobfuscator.Lookup;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator.Output.Generators
{
    class JsonTypeMapping
    {
        public string Name { get; set; }
        public List<string> KnownTranslations { get; set; }
        public List<JsonFieldMapping> Fields { get; set; }
    }
    class JsonFieldMapping
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Offset { get; set; }
        public List<string> KnownTranslations { get; set; }
    }
    class JsonMethodMapping
    {
        public string Name { get; set; }
        public List<string> KnownTranslations { get; set; }
    }
    class JsonMappingGenerator : IGenerator
    {
        public void Generate(BeebyteDeobfuscatorPlugin plugin, LookupModule module)
        {
            List<Translation> translations = module.Translations.Where(t =>
                t.Type == TranslationType.TypeTranslation &&
                !Regex.IsMatch(t.CleanName, @"\+<.*(?:>).*__[1-9]{0,4}|[A-z]*=.{1,4}|<.*>") &&
                !Regex.IsMatch(t.CleanName, module.NamingRegex) &&
                (t._type?.DeclaringType.IsEmpty ?? false) &&
                !(t._type?.IsArray ?? false) &&
                !(t._type?.IsGenericType ?? false) &&
                !(t._type?.IsNested ?? false) &&
                !(t._type?.Namespace.Contains("System") ?? false) &&
                !(t._type?.Namespace.Contains("MS") ?? false)
            ).ToList();

            List<JsonTypeMapping> mappings = new List<JsonTypeMapping>();
            foreach (Translation translation in translations)
            {
                List<string> KnownTypeTranslations = new List<string>();
                if (translation.CleanName != translation.ObfName)
                {
                    KnownTypeTranslations.Add(translation.ObfName);
                }

                JsonTypeMapping typeMapping = new JsonTypeMapping { Name = translation.CleanName, KnownTranslations = KnownTypeTranslations };
                List<JsonFieldMapping> fieldMappings = new List<JsonFieldMapping>();
                foreach (LookupField field in translation._type.Fields)
                {
                    List<string> KnownFieldTranslations = new List<string>();
                    if (field.Translation != null && field.Translation?.CleanName != field.Translation?.ObfName)
                    {
                        KnownFieldTranslations.Add(field.Translation.ObfName);
                    }
                    fieldMappings.Add(new JsonFieldMapping
                    {
                        Name = field.CSharpName,
                        Type = field.Type.IsEmpty ? "unknown" : Regex.IsMatch(field.Type.Name, module.NamingRegex) ? "unknown" : field.Type.CSharpName,
                        Offset = string.Format("0x{0:X}", field.Offset),
                        KnownTranslations = KnownFieldTranslations
                    });
                }
                typeMapping.Fields = fieldMappings;
                mappings.Add(typeMapping);
            }

            using var exportFile = new FileStream(plugin.ExportPath + Path.DirectorySeparatorChar + "mappings.json", FileMode.Create);
            StreamWriter output = new StreamWriter(exportFile);
            string jsonString = JsonSerializer.Serialize(mappings);
            output.Write(jsonString);
        }
    }
}

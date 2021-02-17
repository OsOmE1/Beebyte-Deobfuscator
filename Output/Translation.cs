using Beebyte_Deobfuscator.Lookup;
using Beebyte_Deobfuscator.Output.Generators;
using Il2CppInspector.PluginAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Beebyte_Deobfuscator.Output
{
    public enum TranslationType
    {
        TypeTranslation,
        FieldTranslation,
    }

    public enum ExportType
    {
        None,
        PlainText,
        Classes,
        JsonTranslations,
        JsonMappings
    }

    public interface IGenerator
    {
        public abstract void Generate(BeebyteDeobfuscatorPlugin plugin, LookupModule module);

        public static IGenerator GetGenerator(BeebyteDeobfuscatorPlugin plugin)
        {
            return plugin.Export switch
            {
                ExportType.Classes => new Il2CppTranslatorGenerator(),
                ExportType.PlainText => new PlaintextTranslationsGenerator(),
                ExportType.JsonTranslations => new JsonTranslationsGenerator(),
                ExportType.JsonMappings => new JsonMappingGenerator(),
                _ => null,
            };
        }
    }

    public class Translation : IEquatable<Translation>
    {
        public readonly TranslationType Type;
        public string ObfName;
        public string CleanName;

        public LookupField _field;
        public LookupType _type;

        public Translation(string obfName, LookupType type)
        {
            ObfName = obfName;
            CleanName = type.Name;
            _type = type;
            Type = TranslationType.TypeTranslation;
        }

        public Translation(string obfName, LookupField field)
        {
            ObfName = obfName;
            CleanName = field.Name;
            _field = field;
            Type = TranslationType.FieldTranslation;
        }

        public static void Export(BeebyteDeobfuscatorPlugin plugin, LookupModule lookupModule)
        {
            PluginServices services = PluginServices.For(plugin);

            services.StatusUpdate("Generating output..");
            if (!lookupModule.Translations.Any(t => t.CleanName != t.ObfName))
            {
                return;
            }
            List<Translation> filteredTranslations = lookupModule.Translations
                .Where(t => !t.CleanName.EndsWith('&'))
                .GroupBy(t => t.CleanName)
                .Select(t => t.First())
                .GroupBy(t => t.ObfName)
                .Select(t => t.First())
                .ToList();
            lookupModule.Translations.Clear();
            lookupModule.Translations.AddRange(filteredTranslations);

            IGenerator.GetGenerator(plugin).Generate(plugin, lookupModule);
        }

        public bool Equals([AllowNull] Translation other)
        {
            return other.CleanName == CleanName && other.ObfName == ObfName;
        }
        public override string ToString()
        {
            return $"{ObfName}/{CleanName}";
        }
    }
}

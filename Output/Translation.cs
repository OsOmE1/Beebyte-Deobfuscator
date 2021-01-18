using Beebyte_Deobfuscator.Lookup;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
    }
    public class Translation
    {
        private readonly TranslationType Type;
        string ObfName;
        string CleanName;

        public LookupField _field;
        private LookupType _type;

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

        public string ToPlainExport()
        {
            return $"{ObfName}/{CleanName}";
        }

        public string ToClassExport()
        {
            return Type switch
            {
                TranslationType.FieldTranslation => $"            _type.AddField(new FieldTranslator() {{ Offset = 0x{_field.Offset:X}, Static = {(_field.IsStatic ? "true" : "false")}, Name = \"{CleanName}\", TranslateName = true }});",
                _ => "",
            };
        }

        public static async Task Export(BeebyteDeobfuscatorPlugin plugin, LookupModel lookupModel)
        {
            if (!lookupModel.Translations.Any(t => t.CleanName != t.ObfName)) return;
            switch (plugin.Export)
            {
                case ExportType.PlainText:
                    await ExportPlainText(plugin.ExportPath, lookupModel);
                    break;
                case ExportType.Classes:
                    await ExportClasses(plugin.ExportPath, plugin.PluginName, lookupModel);
                    break;
            }
        }

        private static async Task ExportPlainText(string ExportPath, LookupModel lookupModel)
        {
            using var exportFile = new FileStream(ExportPath + Path.DirectorySeparatorChar + "output.txt", FileMode.Create);
            StreamWriter output = new StreamWriter(exportFile);

            foreach (Translation translation in lookupModel.Translations)
            {
                if (!translation.CleanName.Equals(translation.ObfName)) await output.WriteLineAsync(translation.ToPlainExport());
            }

            output.Close();
        }

        private static async Task ExportClasses(string ExportPath, string pluginName, LookupModel lookupModel)
        {
            foreach (Translation translation in lookupModel.Translations)
            {
                if (translation._type == null) continue;
                List<Translation> fieldTranslations = lookupModel.Translations.Where(t => t.Type.Equals(TranslationType.FieldTranslation) && t._field.DeclaringType.Name.Equals(translation._type.Name) || t.CleanName.Equals("currentPlayerRoom")).ToList();

                using var exportFile = new FileStream(ExportPath + Path.DirectorySeparatorChar + $"{Helpers.SanitzeFileName(translation.CleanName)}.cs", FileMode.Create);
                StreamWriter output = new StreamWriter(exportFile);

                string start = Output.ClassOutputTop;
                start = start.Replace("#CLASSNAME#", translation.CleanName);
                start = start.Replace("#PLUGINNAME#", pluginName);
                start = start.Replace("#LOCATOR#", translation.GenerateLocater(lookupModel));
                await output.WriteAsync(start);

                foreach (Translation t in fieldTranslations)
                {
                    await output.WriteLineAsync(t.ToClassExport());
                }

                string end = Output.ClassOutputBottom;
                await output.WriteAsync(end);

                output.Close();
            }
        }

        private string GenerateLocater(LookupModel lookupModel)
        {
            if (_type == null) return null;
            if (_type.Fields.Count(f => !f.IsStatic && !f.IsLiteral) == 0 && _type.Fields.Count(f => f.IsStatic && !f.IsLiteral) == 0) return "            return null;";
            List<string> fieldSequence = new List<string>();
            List<string> staticFieldSequence = new List<string>();

            if (_type.Fields.Count(f => !f.IsStatic && !f.IsLiteral) != 0)
                foreach (LookupField field in _type.Fields.Where(f => !f.IsStatic && !f.IsLiteral))
                {
                    if (field.Type.Namespace == "UnityEngine" || field.Type.Namespace == "System") fieldSequence.Add(field.Type.Name);
                    else fieldSequence.Add("*");
                }

            if (_type.Fields.Count(f => f.IsStatic && !f.IsLiteral) != 0)
                foreach (LookupField field in _type.Fields.Where(f => f.IsStatic && !f.IsLiteral))
                {
                    if (field.Type.Namespace == "UnityEngine" || field.Type.Namespace == "System") staticFieldSequence.Add(field.Type.Name);
                    else staticFieldSequence.Add("*");
                }

            int fieldSequenceMatchCount = lookupModel.ObfTypes.Count(t => t.FieldSequenceEqual(fieldSequence));
            int staticFieldSequenceMatchCount = lookupModel.ObfTypes.Count(t => t.StaticFieldSequenceEqual(staticFieldSequence));

            if (fieldSequenceMatchCount > 1 && staticFieldSequenceMatchCount > 1) return "            return null;";


            if (fieldSequenceMatchCount == 1)
            {
                string locator = "            _type = new TypeTranslator(Helpers.FindTypeWithFieldSequence(new List<string>() { ";
                fieldSequence.ForEach((s) => locator += $"\"{s}\", ");
                locator = locator.Remove(locator.Length - 2);
                locator += " }));";
                return locator;
            }
            else
            {
                string locator = "            _type = new TypeTranslator(Helpers.FindTypeWithStaticFieldSequence(new List<string>() { ";
                staticFieldSequence.ForEach((s) => locator += $"\"{s}\", ");
                locator = locator.Remove(locator.Length - 2);
                locator += " }));";
                return locator;
            }
        }
    }
}

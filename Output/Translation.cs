using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector.PluginAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        public string ObfName;
        public string CleanName;

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

        public static void Export(BeebyteDeobfuscatorPlugin plugin, LookupModel lookupModel)
        {
            PluginServices services = PluginServices.For(plugin);

            services.StatusUpdate("Generating output..");
            if (!lookupModel.Translations.Any(t => t.CleanName != t.ObfName))
            {
                return;
            }
            switch (plugin.Export)
            {
                case ExportType.PlainText:
                    ExportPlainText(plugin.ExportPath, lookupModel);
                    break;
                case ExportType.Classes:
                    ExportClasses(plugin.ExportPath, plugin.PluginName, lookupModel, statusCallback: services.StatusUpdate);
                    break;
            }
        }

        private static void ExportPlainText(string ExportPath, LookupModel lookupModel)
        {
            using var exportFile = new FileStream(ExportPath + Path.DirectorySeparatorChar + "output.txt", FileMode.Create);
            StreamWriter output = new StreamWriter(exportFile);

            foreach (Translation translation in lookupModel.Translations)
            {
                if (translation.CleanName != translation.ObfName)
                {
                    output.WriteLine(translation.ToPlainExport());
                }
            }

            output.Close();
        }

        private static void ExportClasses(string ExportPath, string pluginName, LookupModel lookupModel, EventHandler<string> statusCallback = null)
        {
            IEnumerable<Translation> translations = lookupModel.Translations.Where(t => 
                !Regex.IsMatch(t.CleanName, @"\+<.*(?:>).*__[1-9]{ 0,4}|[A-z]*=.{1,4}|<.*>") &&
                !Regex.IsMatch(t.CleanName, lookupModel.NamingRegex) &&
                (t._type?.DeclaringType.IsEmpty ?? false) &&
                !(t._type?.IsArray ?? false) &&
                !(t._type?.IsGenericType ?? false) &&
                !(t._type?.IsNested ?? false) &&
                !(t._type?.Namespace.Contains("System") ?? false) &&
                !(t._type?.Namespace.Contains("MS") ?? false)
            );
            int current = 0;
            int total = translations.Count();
            foreach (Translation translation in translations)
            {
                if(translation.CleanName == "Palette")
                {

                }
                statusCallback?.Invoke(translations, $"Exported {current}/{total} classes");

                FileStream exportFile = null;
                if (!translation.CleanName.Contains("+"))
                {
                    exportFile = new FileStream(ExportPath +
                        Path.DirectorySeparatorChar +
                        $"{Helpers.SanitizeFileName(translation.CleanName)}.cs",
                        FileMode.Create);
                }
                else
                {
                    if(!File.Exists($"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs"))
                    {
                        continue;
                    }
                    var lines = File.ReadAllLines($"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs");
                    File.WriteAllLines($"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs", lines.Take(lines.Length - 1).ToArray());
                    exportFile = new FileStream(ExportPath +
                        Path.DirectorySeparatorChar +
                        $"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs",
                        FileMode.Open);
                }

                StreamWriter output = new StreamWriter(exportFile);

                if (!translation.CleanName.Contains("+"))
                {
                    string start = Output.ClassOutputTop;
                    start = start.Replace("#PLUGINNAME#", pluginName);
                    output.Write(start);
                    output.Write($"    [Translator]\n    public struct {translation.CleanName}\n    {{\n");
                }
                else
                {
                    var names = translation.CleanName.Split("+").ToList();
                    names.RemoveAt(0);
                    output.Write($"    [Translator]\n    public struct {string.Join('.', names)}\n    {{\n");
                }
                foreach (LookupField f in translation._type.Fields)
                {
                    output.WriteLine(f.ToFieldExport());
                }
                output.Write("    }\n}");

                output.Close();
                current++;
            }
        }
    }
}

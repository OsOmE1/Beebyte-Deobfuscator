using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector.PluginAPI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator.Output.Generators
{
    class Il2CppTranslatorGenerator : IGenerator
    {
        public void Generate(BeebyteDeobfuscatorPlugin plugin, LookupModule module)
        {
            IEnumerable<Translation> translations = module.Translations.Where(t =>
                t.Type == TranslationType.TypeTranslation &&
                !Regex.IsMatch(t.CleanName, @"\+<.*(?:>).*__[1-9]{0,4}|[A-z]*=.{1,4}|<.*>") &&
                !Regex.IsMatch(t.CleanName, module.NamingRegex) &&
                (t._type?.DeclaringType.IsEmpty ?? false) &&
                !(t._type?.IsArray ?? false) &&
                !(t._type?.IsGenericType ?? false) &&
                !(t._type?.IsNested ?? false) &&
                !(t._type?.Namespace.Contains("System") ?? false) &&
                !(t._type?.Namespace.Contains("MS") ?? false)
            );
            int current = 0;
            int total = translations.Count();
            PluginServices services = PluginServices.For(plugin);

            foreach (Translation translation in translations)
            {
                services.StatusUpdate(translations, $"Exported {current}/{total} classes");

                FileStream exportFile = null;
                if (!translation.CleanName.Contains("+"))
                {
                    exportFile = new FileStream(plugin.ExportPath +
                        Path.DirectorySeparatorChar +
                        $"{Helpers.SanitizeFileName(translation.CleanName)}.cs",
                        FileMode.Create);
                }
                else
                {
                    if (!File.Exists($"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs"))
                    {
                        continue;
                    }
                    var lines = File.ReadAllLines($"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs");
                    File.WriteAllLines($"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs", lines.Take(lines.Length - 1).ToArray());
                    exportFile = new FileStream(plugin.ExportPath +
                        Path.DirectorySeparatorChar +
                        $"{Helpers.SanitizeFileName(translation.CleanName.Split("+")[0])}.cs",
                        FileMode.Open);
                }

                StreamWriter output = new StreamWriter(exportFile);

                if (!translation.CleanName.Contains("+"))
                {
                    string start = Output.ClassOutputTop;
                    start = start.Replace("#PLUGINNAME#", plugin.PluginName);
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

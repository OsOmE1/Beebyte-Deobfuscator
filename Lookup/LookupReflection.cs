using Beebyte_Deobfuscator.Output;
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator.Lookup
{
    public class LookupModule
    {
        public List<string> Namespaces;
        public readonly List<LookupType> Types;
        public LookupModule(List<string> namespaces, List<LookupType> types)
        {
            Namespaces = namespaces;
            Types = types;
        }
    }
    public class LookupType
    {
        private readonly LookupModel Parent;
        public TypeInfo Il2CppType { get; set; }
        public TypeDef MonoType { get; set; }

        public string Name
        {
            get
            {
                return Il2CppType?.BaseName ?? MonoType?.Name;
            }
            set
            {
                if (Il2CppType != null)
                {
                    SetName(value);
                }
                else MonoType.Name = value;
            }
        }

        public string AssemblyName => Il2CppType?.Assembly.ShortName ?? MonoType?.Module.Assembly.Name;
        public string Namespace => Il2CppType?.Namespace ?? MonoType?.Namespace;
        public bool IsEnum => Il2CppType?.IsEnum ?? MonoType?.IsEnum ?? false;
        public bool IsEmpty => Il2CppType == null && MonoType == null;
        public bool ShouldTranslate => Regex.Match(Name, Parent.NamingRegex).Success || Fields.Any(f => Regex.Match(f.Name, Parent.NamingRegex).Success) || Fields.Any(f => f.Translated);
        public bool Translated { get; private set; }
        public LookupType DeclaringType { get; set; }
        public List<LookupField> Fields { get; set; }
        public List<LookupProperty> Properties { get; set; }
        public List<LookupMethod> Methods { get; set; }
        public List<LookupType> Children { get; set; }

        public LookupType(LookupModel lookupModel) { Parent = lookupModel; }
        public void SetName(string name)
        {
            if (!Regex.Match(Name, Parent.NamingRegex).Success && Fields.Any(f => Regex.Match(f.Name, Parent.NamingRegex).Success))
            {
                Parent.Translations.Add(new Translation(Name, this));
                return;
            }

            string obfName = Name;

            if (!ShouldTranslate || IsEnum)
            {
                return;
            }

            Il2CppType.Name = name;
            Parent.Translations.Add(new Translation(obfName, this));
            Translated = true;
        }
        public bool FieldSequenceEqual(IEnumerable<string> baseNames)
        {
            var fieldBaseNames = Fields.Where(f => !f.IsLiteral && !f.IsStatic).Select(f => f.Type.Name).ToList();
            var baseNamesList = baseNames.ToList();
            if (fieldBaseNames.Count != baseNamesList.Count)
            {
                return false;
            }

            for (int i = 0; i < fieldBaseNames.Count; i++)
            {
                if (baseNamesList[i] == "*")
                {
                    continue;
                }
                if (fieldBaseNames[i] != baseNamesList[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool StaticFieldSequenceEqual(IEnumerable<string> baseNames)
        {
            var fieldBaseNames = Fields.Where(f => !f.IsLiteral && f.IsStatic).Select(f => f.Type.Name).ToList();
            var baseNamesList = baseNames.ToList();
            if (fieldBaseNames.Count != baseNamesList.Count)
            {
                return false;
            }

            for (int i = 0; i < fieldBaseNames.Count; i++)
            {
                if (baseNamesList[i] == "*")
                {
                    continue;
                }
                if (fieldBaseNames[i] != baseNamesList[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
    public class LookupField
    {
        private readonly LookupModel Parent;
        public FieldInfo Il2CppField { get; set; }
        public FieldDef MonoField { get; set; }
        public string Name
        {
            get
            {
                return Il2CppField?.Name ?? MonoField?.Name;
            }
            set
            {
                if (Il2CppField != null)
                {
                    SetName(value);
                }
                else
                {
                    MonoField.Name = value;
                }
            }
        }

        public bool IsStatic => Il2CppField?.IsStatic ?? MonoField?.IsStatic ?? false;
        public bool IsLiteral => Il2CppField?.IsLiteral ?? MonoField?.IsLiteral ?? false;
        public long Offset => Il2CppField?.Offset ?? 0x0;
        public bool Translated { get; private set; }
        public bool IsEmpty => Il2CppField == null && MonoField == null;
        public LookupType Type { get; set; }
        public LookupType DeclaringType { get; set; }
        public LookupField(LookupModel lookupModel) { Parent = lookupModel; }

        public void SetName(string name)
        {
            string obfName = Name;

            if (!Regex.Match(obfName, Parent.NamingRegex).Success)
            {
                return;
            }

            Il2CppField.Name = name;
            Parent.Translations.Add(new Translation(obfName, this));
            Translated = true;
        }
    }

    public class LookupProperty
    {
        private readonly LookupModel Parent;
        public PropertyInfo Il2CppProperty { get; set; }
        public PropertyDef MonoProperty { get; set; }
        public string Name
        {
            get
            {
                return Il2CppProperty?.Name ?? MonoProperty?.Name;
            }
            set
            {
                if (Il2CppProperty != null)
                {
                    SetName(value);
                }
                else
                {
                    MonoProperty.Name = value;
                }
            }
        }

        public LookupType PropertyType { get; set; }
        public LookupMethod GetMethod { get; set; }
        public LookupMethod SetMethod { get; set; }
        public int Index { get; set; }
        public bool Translated { get; private set; } = false;
        public bool IsEmpty => Il2CppProperty == null && MonoProperty == null;
        public LookupProperty(LookupModel lookupModel) { Parent = lookupModel; }

        public void SetName(string name)
        {
            string obfName = Name;

            if (!Regex.Match(obfName, Parent.NamingRegex).Success)
            {
                return;
            }

            Il2CppProperty.Name = name;
            Translated = true;
        }
    }

    public class LookupMethod
    {
        private readonly LookupModel Parent;
        public MethodInfo Il2CppMethod { get; set; }
        public MethodDef MonoMethod { get; set; }

        public string Name
        {
            get
            {
                return Il2CppMethod?.Name ?? MonoMethod?.Name;
            }
            set
            {
                if (Il2CppMethod != null)
                {
                    SetName(value);
                }
                else
                {
                    MonoMethod.Name = value;
                }
            }
        }

        public LookupType DeclaringType { get; set; }
        public LookupType ReturnType { get; set; }
        public List<LookupType> ParameterList { get; set; }

        public bool Translated { get; private set; }
        public bool IsEmpty => Il2CppMethod == null && MonoMethod == null;

        public LookupMethod(LookupModel lookupModel) { Parent = lookupModel; }

        public void SetName(string name)
        {
            string obfName = Name;

            if (!Regex.Match(obfName, Parent.NamingRegex).Success)
            {
                return;
            }

            Il2CppMethod.Name = name;
            Translated = true;
        }
    }
}
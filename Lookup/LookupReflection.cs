using Beebyte_Deobfuscator.Output;
using dnlib.DotNet;
using Il2CppInspector.Model;
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
        public readonly TypeModel TypeModel;
        public readonly AppModel AppModel;
        public LookupModule(List<string> namespaces, List<LookupType> types, TypeModel model = null)
        {
            Namespaces = namespaces;
            Types = types;
            if(model != null)
            {
                TypeModel = model;
                AppModel = new AppModel(model);
            }
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
        public string CSharpName => Il2CppType?.CSharpName ?? MonoType?.Name ?? "";
        public string AssemblyName => Il2CppType?.Assembly.ShortName ?? MonoType?.Module.Assembly.Name;
        public string Namespace => Il2CppType?.Namespace ?? MonoType?.Namespace;
        public bool IsGenericType => Il2CppType?.IsGenericType ?? MonoType?.HasGenericParameters ?? false;
        public List<LookupType> GenericTypeParameters { get; set; }
        public bool IsEnum => Il2CppType?.IsEnum ?? MonoType?.IsEnum ?? false;
        public bool IsPrimitive => Il2CppType?.IsPrimitive ?? MonoType?.IsPrimitive ?? false;
        public bool IsArray => Il2CppType?.IsArray ?? MonoType?.TryGetArraySig()?.IsArray ?? false;
        public LookupType ElementType { get; set; }
        public bool IsEmpty => Il2CppType == null && MonoType == null;
        public bool IsNested => Il2CppType?.IsNested ?? MonoType?.IsNested ?? false;
        public bool ShouldTranslate => Regex.Match(Name, Parent.NamingRegex).Success || Fields.Any(f => Regex.Match(f.Name, Parent.NamingRegex).Success) || Fields.Any(f => f.Translated);
        public bool Translated { get; private set; }
        public Translation Translation { get; set; }
        public LookupType DeclaringType { get; set; }
        public List<LookupField> Fields { get; set; }
        public List<LookupProperty> Properties { get; set; }
        public List<LookupMethod> Methods { get; set; }
        public List<LookupType> Children { get; set; }
        public bool Resolved { get; set; }
        public LookupType(LookupModel lookupModel) { Parent = lookupModel; }
        public void SetName(string name)
        {
            if (!Regex.Match(Name, Parent.NamingRegex).Success && Fields.Any(f => Regex.Match(f.Name, Parent.NamingRegex).Success))
            {
                Translation = new Translation(Name, this);
                Parent.Translations.Add(Translation);
                return;
            }

            string obfName = Name;

            if (!ShouldTranslate || IsEnum)
            {
                return;
            }

            Il2CppType.Name = name;
            Translation = new Translation(obfName, this);
            Parent.Translations.Add(Translation);
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

        public void Resolve()
        {
            if (Resolved)
            {
                return;
            }
            Fields = Il2CppType?.DeclaredFields.ToLookupFieldList(Parent).ToList() ?? MonoType?.Fields.ToLookupFieldList(Parent).ToList();
            DeclaringType = Il2CppType?.DeclaringType.ToLookupType(Parent, false) ?? MonoType?.DeclaringType.ToLookupType(Parent, false);
            Properties = Il2CppType?.DeclaredProperties.ToLookupPropertyList(Parent).ToList() ?? MonoType?.Properties.ToLookupPropertyList(Parent).ToList();
            Methods = Il2CppType?.DeclaredMethods.ToLookupMethodList(Parent).ToList() ?? MonoType?.Methods.ToLookupMethodList(Parent).ToList();

            if (!DeclaringType.IsEmpty)
            {
                DeclaringType.Children.Add(this);
            }
        }

        public string GetExportTypeName()
        {
            if(IsPrimitive)
            {
                return CSharpName;
            }

            string typename = "";
            if (!IsEmpty)
            {
                if (Parent.Translations.Any(t => t.CleanName == Name))
                {
                    typename = CSharpName;
                }
            }
            if (typename == "")
            {
                typename = "object";
            }
            return typename;
        }
        public override string ToString()
        {
            return CSharpName;
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
        public string CSharpName => Il2CppField?.CSharpName ?? MonoField?.Name ?? "";
        public bool IsStatic => Il2CppField?.IsStatic ?? MonoField?.IsStatic ?? false;
        public bool IsPublic => Il2CppField?.IsPublic ?? MonoField?.IsPublic ?? false;
        public bool IsPrivate => Il2CppField?.IsPrivate ?? MonoField?.IsPrivate ?? false;
        public bool IsLiteral => Il2CppField?.IsLiteral ?? MonoField?.IsLiteral ?? false;
        public long Offset => Il2CppField?.Offset ?? 0x0;
        public bool Translated { get; private set; }
        public bool IsEmpty => Il2CppField == null && MonoField == null;
        public Translation Translation { get; set; }
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
            Translation = new Translation(obfName, this);
            Parent.Translations.Add(Translation);
            Translated = true;
        }
        public string ToFieldExport()
        {
            string modifiers = $"[TranslatorFieldOffset(0x{Offset:X})]{(IsStatic ? " static" : "")}{(IsPublic ? " public" : "")}{(IsPrivate ? " private" : "")}";
            string fieldType = "";
            if (Type.IsArray)
            {
                fieldType = $"{Type.ElementType?.GetExportTypeName() ?? "object"}[]";
            }
            else if (Type.IsGenericType && Type.GenericTypeParameters.Any())
            {
                fieldType = Type.Name.Split("`")[0] + "<";
                foreach (LookupType type in Type.GenericTypeParameters)
                {
                    fieldType += type.GetExportTypeName() + (Type.GenericTypeParameters[Type.GenericTypeParameters.Count() - 1] != type ? ", " : "");
                }
                fieldType += ">";
            }
            else
            {
                fieldType = Type.GetExportTypeName();
            }
            return $"        {modifiers} {fieldType} {Name};";
        }
        public override string ToString()
        {
            return CSharpName;
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
        public string CSharpName => Il2CppProperty?.CSharpName ?? MonoProperty?.Name ?? "";
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

        public override string ToString()
        {
            return CSharpName;
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
        public string CSharpName => Il2CppMethod?.ToString() ?? MonoMethod?.ToString() ?? "";
        public LookupType DeclaringType { get; set; }
        public LookupType ReturnType { get; set; }
        public List<LookupType> ParameterList { get; set; }
        public bool Translated { get; private set; }
        public bool IsEmpty => Il2CppMethod == null && MonoMethod == null;
        public bool IsPropertymethod
        {
            get
            {
                if(DeclaringType.IsEmpty)
                {
                    return false;
                }
                if(DeclaringType.Properties != null)
                {
                    List<LookupMethod> ts = DeclaringType.Properties.Select(p => p.GetMethod).Union(DeclaringType.Properties.Select(p => p.SetMethod)).ToList();
                    if (ts.Any(m => m.Name == Name))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
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

        public ulong GetAdress(AppModel model)
        {
            if(model == null)
            {
                return 0x0;
            }

            AddressMap map = model.GetAddressMap();
            ulong adress = map.Where(k => k.Value is AppMethod).FirstOrDefault(m => ((AppMethod)m.Value).Method == ((MethodBase)Il2CppMethod)).Key;
            return adress;
        }

        public override string ToString()
        {
            return CSharpName;
        }
    }
}
using Beebyte_Deobfuscator.Output;
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator.Lookup
{
    public class LookupType
    {
        public string Name { get; set; }
        public List<LookupField> Fields { get; set; }
        public string AssemblyName { get; set; }
        public string Namespace { get; set; }
        public LookupType DeclaringType { get; set; }
        public List<LookupProperty> Properties { get; set; }

        public TypeInfo Il2CppType { get; set; }
        public List<LookupType> Children { get; set; }

        public static List<LookupType> TypesFromMono(IEnumerable<TypeDef> monoTypes, LookupModel lookupModel)
        {
            List<LookupType> types = new List<LookupType>();
            foreach (TypeDef type in monoTypes)
            {
                types.Add(FromMono(type, lookupModel));
            }
            return types;
        }

        public static LookupType FromMono(TypeDef type, LookupModel lookupModel)
        {
            if (type == null) return new LookupType { };
            if (lookupModel.ProcessedMonoTypes.Contains(type))
            {
                return lookupModel.MonoTypeMatches[type];
            }
            lookupModel.ProcessedMonoTypes.Add(type);
            LookupType t = new LookupType { Name = type.Name, AssemblyName = type.AssemblyQualifiedName, Namespace = type.Namespace, Children = new List<LookupType>() };
            lookupModel.MonoTypeMatches.Add(type, t);
            lookupModel.MonoTypeMatches[type].Fields = LookupField.FieldsFromMono(type.Fields, lookupModel);
            lookupModel.MonoTypeMatches[type].DeclaringType = FromMono(type.DeclaringType, lookupModel);
            lookupModel.MonoTypeMatches[type].Properties = new List<LookupProperty>();
            if (!lookupModel.MonoTypeMatches[type].DeclaringType.IsEmpty())
            {
                lookupModel.MonoTypeMatches[type].DeclaringType.Children.Add(lookupModel.MonoTypeMatches[type]);
            }
            return lookupModel.MonoTypeMatches[type];
        }


        public static List<LookupType> TypesFromIl2Cpp(IEnumerable<TypeInfo> il2cppTypes, LookupModel lookupModel)
        {
            List<LookupType> types = new List<LookupType>();
            foreach (TypeInfo type in il2cppTypes)
            {
                if (type.CSharpName.Equals("_Module_")) continue;
                types.Add(FromIl2Cpp(type, lookupModel));
            }
            return types;
        }

        public static LookupType FromIl2Cpp(TypeInfo type, LookupModel lookupModel)
        {
            if (type == null) return new LookupType { };
            if (lookupModel.ProcessedIl2CppTypes.Contains(type))
            {
                return lookupModel.Il2CppTypeMatches[type];
            }
            lookupModel.ProcessedIl2CppTypes.Add(type);
            LookupType t = new LookupType { Name = type.BaseName, AssemblyName = type.Assembly.ShortName, Namespace = type.Namespace, Children = new List<LookupType>(), Il2CppType = type };
            lookupModel.Il2CppTypeMatches.Add(type, t);
            lookupModel.Il2CppTypeMatches[type].Fields = LookupField.FieldsFromIl2Cpp(type.DeclaredFields, lookupModel);
            lookupModel.Il2CppTypeMatches[type].DeclaringType = FromIl2Cpp(type.DeclaringType, lookupModel);
            lookupModel.Il2CppTypeMatches[type].Properties = new List<LookupProperty>();
            //Il2CppTypeMatches[type].Properties = LookupProperty.PropertiesFromIl2Cpp(type.DeclaredProperties);

            if (!lookupModel.Il2CppTypeMatches[type].DeclaringType.IsEmpty())
            {
                lookupModel.Il2CppTypeMatches[type].DeclaringType.Children.Add(lookupModel.Il2CppTypeMatches[type]);
            }
            return lookupModel.Il2CppTypeMatches[type];
        }
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(this.Name);
        }
        public void SetName(string name, LookupModel lookupModel)
        {
            string obfName = Name;

            if (!ShouldTranslate(lookupModel)) return;

            Name = name;
            if (Il2CppType != null) Il2CppType.Name = name;
            lookupModel.Translations.Add(new Translation(obfName, this));
        }
        public bool ShouldTranslate(LookupModel lookupModel)
        {
            bool test = Regex.Match(Name, lookupModel.NamingRegex).Success;
            bool test2 = Fields.Count(f => Regex.Match(f.Name, lookupModel.NamingRegex).Success) > 0;
            bool test3 = lookupModel.Translations.Count(t => Fields.Contains(t._field)) > 0;
            return test || test2 || test3;
        }
        public bool FieldSequenceEqual(IEnumerable<string> baseNames)
        {
            var fieldBaseNames = Fields.Where(f => !f.IsLiteral && !f.IsStatic).Select(f => f.Type.Name).ToList();
            var baseNamesList = baseNames.ToList();
            if (fieldBaseNames.Count != baseNamesList.Count) return false;

            for (int i = 0; i < fieldBaseNames.Count; i++)
            {
                if (baseNamesList[i] == "*") continue;
                if (fieldBaseNames[i] != baseNamesList[i]) return false;
            }

            return true;
        }

        public bool StaticFieldSequenceEqual(IEnumerable<string> baseNames)
        {
            var fieldBaseNames = Fields.Where(f => !f.IsLiteral && f.IsStatic).Select(f => f.Type.Name).ToList();
            var baseNamesList = baseNames.ToList();
            if (fieldBaseNames.Count != baseNamesList.Count) return false;

            for (int i = 0; i < fieldBaseNames.Count; i++)
            {
                if (baseNamesList[i] == "*") continue;
                if (fieldBaseNames[i] != baseNamesList[i]) return false;
            }

            return true;
        }
    }
    public class LookupField
    {
        public string Name { get; set; }
        public bool IsStatic { get; set; }
        public bool IsLiteral { get; set; }
        public long Offset { get; set; }
        public LookupType Type { get; set; }
        public LookupType DeclaringType { get; set; }

        public FieldInfo Il2CppField { get; set; }

        public static List<LookupField> FieldsFromMono(IList<FieldDef> monoFields, LookupModel lookupModel)
        {
            List<LookupField> fields = new List<LookupField>();
            foreach (FieldDef field in monoFields)
            {
                fields.Add(FromMono(field, lookupModel));
            }
            return fields;
        }

        public static LookupField FromMono(FieldDef field, LookupModel lookupModel)
        {
            if (field == null) return new LookupField { };
            long fieldOffset = 0x0;
            if (field.FieldOffset != null)
            {
                if (field.FieldOffset.HasValue) fieldOffset = field.FieldOffset.GetValueOrDefault();
            }
            return new LookupField { Name = field.Name, IsStatic = field.IsStatic, IsLiteral = field.IsLiteral, Offset = fieldOffset, Type = LookupType.FromMono(field.FieldType.TryGetTypeDef(), lookupModel), DeclaringType = LookupType.FromMono(field.DeclaringType, lookupModel) };
        }

        public static List<LookupField> FieldsFromIl2Cpp(IReadOnlyCollection<FieldInfo> il2cppFields, LookupModel lookupModel)
        {
            List<LookupField> fields = new List<LookupField>();
            foreach (FieldInfo field in il2cppFields)
            {
                fields.Add(FromIl2Cpp(field, lookupModel));
            }
            return fields;
        }

        public static LookupField FromIl2Cpp(FieldInfo field, LookupModel lookupModel)
        {
            if (field == null) return new LookupField { };
            return new LookupField { Name = field.Name, IsStatic = field.IsStatic, IsLiteral = field.IsLiteral, Offset = field.Offset, Type = LookupType.FromIl2Cpp(field.FieldType, lookupModel), DeclaringType = LookupType.FromIl2Cpp(field.DeclaringType, lookupModel), Il2CppField = field };
        }

        public static bool IsEmpty(LookupField field)
        {
            return string.IsNullOrEmpty(field.Name);
        }
        public void SetName(string name, LookupModel lookupModel)
        {
            string obfName = Name;

            if (!Regex.Match(obfName, lookupModel.NamingRegex).Success) return;

            Name = name;
            if (Il2CppField != null) Il2CppField.Name = name;
            lookupModel.Translations.Add(new Translation(obfName, this));
        }
    }

    public class LookupProperty
    {
        public string Name { get; set; }
        public LookupType PropertyType { get; set; }
        public LookupMethod GetMethod { get; set; }
        public LookupMethod SetMethod { get; set; }
        int Index { get; set; }

        public PropertyInfo Il2CppProperty { get; set; }

        public static List<LookupProperty> PropertiesFromMono(IList<PropertyDef> monoProperties, LookupModel lookupModel)
        {
            List<LookupProperty> properties = new List<LookupProperty>();
            int i = 0;
            foreach (PropertyDef property in monoProperties)
                properties.Add(FromMono(property, i, lookupModel)); i++;
            return properties;
        }

        public static LookupProperty FromMono(PropertyDef property, int index, LookupModel lookupModel)
        {
            return new LookupProperty { Name = property.Name, PropertyType = LookupType.FromMono(property.DeclaringType2, lookupModel), GetMethod = LookupMethod.FromMono(property.GetMethod, lookupModel), SetMethod = LookupMethod.FromMono(property.SetMethod, lookupModel), Index = index };
        }

        public static List<LookupProperty> PropertiesFromIl2Cpp(ReadOnlyCollection<PropertyInfo> il2cppProperties, LookupModel lookupModel)
        {
            List<LookupProperty> properties = new List<LookupProperty>();
            foreach (PropertyInfo property in il2cppProperties)
            {
                properties.Add(FromIl2Cpp(property, lookupModel));
            }
            return properties;
        }

        public static LookupProperty FromIl2Cpp(PropertyInfo property, LookupModel lookupModel)
        {
            if (property == null) return new LookupProperty { };
            return new LookupProperty { Name = property.Name, PropertyType = LookupType.FromIl2Cpp(null, lookupModel), GetMethod = LookupMethod.FromIl2Cpp(null, lookupModel), SetMethod = LookupMethod.FromIl2Cpp(null, lookupModel), Index = property.Index };
        }
        public static bool IsEmpty(LookupProperty property)
        {
            return string.IsNullOrEmpty(property.Name);
        }
        public void SetName(string name)
        {
            Name = name;
            if (Il2CppProperty != null) Il2CppProperty.Name = name;
        }
    }

    public class LookupMethod
    {
        public string Name { get; set; }
        public LookupType DeclaringType { get; set; }
        public LookupType ReturnType { get; set; }
        public List<LookupType> ParameterList { get; set; }

        public MethodInfo Il2CppMethod { get; set; }

        public static List<LookupMethod> MethodsFromMono(IList<MethodDef> monoMethods, LookupModel lookupModel)
        {
            List<LookupMethod> methods = new List<LookupMethod>();
            foreach (MethodDef method in monoMethods)
            {
                methods.Add(FromMono(method, lookupModel));
            }
            return methods;
        }

        public static LookupMethod FromMono(MethodDef method, LookupModel lookupModel)
        {
            List<LookupType> ParameterList = new List<LookupType>();
            IEnumerator<Parameter> parameters = method.Parameters.GetEnumerator();
            while (parameters.MoveNext())
            {
                ParameterList.Add(ReflectionHelpers.MonoTypeSigToType(parameters.Current.Type, lookupModel));

            }
            return new LookupMethod { Name = method.Name, DeclaringType = LookupType.FromMono(method.DeclaringType, lookupModel), ReturnType = ReflectionHelpers.MonoTypeSigToType(method.ReturnType, lookupModel), ParameterList = ParameterList };
        }

        public static List<LookupMethod> MethodsFromIl2Cpp(ReadOnlyCollection<MethodInfo> il2cppMethods, LookupModel lookupModel)
        {
            List<LookupMethod> methods = new List<LookupMethod>();
            foreach (MethodInfo method in il2cppMethods)
            {
                methods.Add(FromIl2Cpp(method, lookupModel));
            }
            return methods;
        }


        public static LookupMethod FromIl2Cpp(MethodInfo method, LookupModel lookupModel)
        {
            if (method == null) return new LookupMethod { };

            List<LookupType> ParameterList = new List<LookupType>();
            ParameterList.AddRange(method.DeclaredParameters.Select(p => LookupType.FromIl2Cpp(p.ParameterType, lookupModel)));

            return new LookupMethod { Name = method.Name, DeclaringType = LookupType.FromIl2Cpp(method.DeclaringType, lookupModel), ParameterList = ParameterList, ReturnType = LookupType.FromIl2Cpp(method.ReturnType, lookupModel), Il2CppMethod = method };
        }
        public static bool IsEmpty(LookupMethod method)
        {
            return string.IsNullOrEmpty(method.Name);
        }
        public void SetName(string name)
        {
            Name = name;
            if (Il2CppMethod != null) Il2CppMethod.Name = name;
        }
    }
    public class ReflectionHelpers
    {
        public static LookupType MonoTypeSigToType(TypeSig typeSig, LookupModel lookupModel)
        {
            LookupType type = null;
            if (typeSig.IsTypeDefOrRef)
            {
                type = LookupType.FromMono(typeSig.TryGetTypeDef(), lookupModel);
            }
            return type;
        }
    }
}
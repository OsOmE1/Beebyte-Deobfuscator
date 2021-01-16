using Beebyte_Deobfuscator.Lookup;
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator
{
    public static class Extensions
    {
        public static LookupType ToLookupType(this TypeInfo type, LookupModel lookupModel)
        {
            if (type == null) return new LookupType(lookupModel) { };
            if (lookupModel.ProcessedIl2CppTypes.Contains(type))
            {
                return lookupModel.Il2CppTypeMatches[type];
            }
            lookupModel.ProcessedIl2CppTypes.Add(type);
            LookupType t = new LookupType(lookupModel) { Il2CppType = type, Children = new List<LookupType>() };
            lookupModel.Il2CppTypeMatches.Add(type, t);
            lookupModel.Il2CppTypeMatches[type].Fields = type.DeclaredFields.ToLookupFieldList(lookupModel).ToList();
            lookupModel.Il2CppTypeMatches[type].DeclaringType = type.DeclaringType.ToLookupType(lookupModel);
            lookupModel.Il2CppTypeMatches[type].Properties = type.DeclaredProperties.ToLookupPropertyList(lookupModel).ToList();
            lookupModel.Il2CppTypeMatches[type].Methods = type.DeclaredMethods.ToLookupMethodList(lookupModel).ToList();

            if (!lookupModel.Il2CppTypeMatches[type].DeclaringType.IsEmpty)
            {
                lookupModel.Il2CppTypeMatches[type].DeclaringType.Children.Add(lookupModel.Il2CppTypeMatches[type]);
            }
            return lookupModel.Il2CppTypeMatches[type];
        }
        public static LookupType ToLookupType(this TypeDef type, LookupModel lookupModel)
        {
            if (type == null) return new LookupType(lookupModel) { };
            if (lookupModel.ProcessedMonoTypes.Contains(type))
            {
                return lookupModel.MonoTypeMatches[type];
            }
            lookupModel.ProcessedMonoTypes.Add(type);
            LookupType t = new LookupType(lookupModel) { MonoType = type, Children = new List<LookupType>() };
            lookupModel.MonoTypeMatches.Add(type, t);
            lookupModel.MonoTypeMatches[type].Fields = type.Fields.ToLookupFieldList(lookupModel).ToList();
            lookupModel.MonoTypeMatches[type].DeclaringType = type.DeclaringType.ToLookupType(lookupModel);
            lookupModel.MonoTypeMatches[type].Properties = type.Properties.ToLookupPropertyList(lookupModel).ToList();
            lookupModel.MonoTypeMatches[type].Methods = type.Methods.ToLookupMethodList(lookupModel).ToList();

            if (!lookupModel.MonoTypeMatches[type].DeclaringType.IsEmpty)
            {
                lookupModel.MonoTypeMatches[type].DeclaringType.Children.Add(lookupModel.MonoTypeMatches[type]);
            }
            return lookupModel.MonoTypeMatches[type];
        }
        public static IEnumerable<LookupType> ToLookupTypeList(this IEnumerable<TypeDef> monoTypes, LookupModel lookupModel)
        {
            foreach (TypeDef type in monoTypes)
            {
                if (type.Name.Contains("Module")) continue;
                yield return type.ToLookupType(lookupModel);
            }
        }
        public static IEnumerable<LookupType> ToLookupTypeList(this IEnumerable<TypeInfo> il2cppTypes, LookupModel lookupModel)
        {
            foreach (TypeInfo type in il2cppTypes)
            {
                if (type.BaseName.Contains("Module")) continue;
                yield return type.ToLookupType(lookupModel);
            }
        }
        public static LookupField ToLookupField(this FieldInfo field, LookupModel lookupModel)
        {
            if (field == null) return new LookupField(lookupModel) { };
            return new LookupField(lookupModel) { Il2CppField = field, Type = field.FieldType.ToLookupType(lookupModel), DeclaringType = field.DeclaringType.ToLookupType(lookupModel) };
        }
        public static LookupField ToLookupField(this FieldDef field, LookupModel lookupModel)
        {
            if (field == null) return new LookupField(lookupModel) { };
            return new LookupField(lookupModel) { MonoField = field, Type = field.FieldType.TryGetTypeDef().ToLookupType(lookupModel), DeclaringType = field.DeclaringType.ToLookupType(lookupModel) };
        }
        public static IEnumerable<LookupField> ToLookupFieldList(this IReadOnlyCollection<FieldInfo> il2cppFields, LookupModel lookupModel)
        {
            foreach (FieldInfo field in il2cppFields)
                yield return field.ToLookupField(lookupModel);
        }
        public static IEnumerable<LookupField> ToLookupFieldList(this IList<FieldDef> monoFields, LookupModel lookupModel)
        {
            foreach (FieldDef field in monoFields)
                yield return field.ToLookupField(lookupModel);
        }
        public static LookupProperty ToLookupProperty(this PropertyDef property, int index, LookupModel lookupModel)
        {
            return new LookupProperty(lookupModel) { PropertyType = property.PropertySig.RetType.TryGetTypeDef().ToLookupType(lookupModel), GetMethod = property.GetMethod.ToLookupMethod(lookupModel), SetMethod = property.SetMethod.ToLookupMethod(lookupModel), Index = index };
        }
        public static LookupProperty ToLookupProperty(this PropertyInfo property, LookupModel lookupModel)
        {
            if (property == null) return new LookupProperty(lookupModel) { };
            return new LookupProperty(lookupModel) { PropertyType = property.PropertyType.ToLookupType(lookupModel), GetMethod = property.GetMethod.ToLookupMethod(lookupModel), SetMethod = property.SetMethod.ToLookupMethod(lookupModel), Index = property.Index };
        }
        public static IEnumerable<LookupProperty> ToLookupPropertyList(this IList<PropertyDef> monoProperties, LookupModel lookupModel)
        {
            int i = 0;
            foreach (PropertyDef property in monoProperties)
            {
                yield return property.ToLookupProperty(i, lookupModel);
                i++;
            }
        }
        public static IEnumerable<LookupProperty> ToLookupPropertyList(this ReadOnlyCollection<PropertyInfo> il2cppProperties, LookupModel lookupModel)
        {
            foreach (PropertyInfo property in il2cppProperties)
                yield return property.ToLookupProperty(lookupModel);
        }
        public static LookupMethod ToLookupMethod(this MethodDef method, LookupModel lookupModel)
        {
            List<LookupType> ParameterList = new List<LookupType>();
            IEnumerator<Parameter> parameters = method.Parameters.GetEnumerator();
            while (parameters.MoveNext())
            {
                if (parameters.Current.Type.IsTypeDefOrRef) ParameterList.Add(parameters.Current.Type.TryGetTypeDef().ToLookupType(lookupModel));

            }
            return new LookupMethod(lookupModel) { DeclaringType = method.DeclaringType.ToLookupType(lookupModel), ReturnType = method.ReturnType.TryGetTypeDef().ToLookupType(lookupModel), ParameterList = ParameterList };
        }
        public static LookupMethod ToLookupMethod(this MethodInfo method, LookupModel lookupModel)
        {
            if (method == null) return new LookupMethod(lookupModel) { };

            List<LookupType> ParameterList = new List<LookupType>();
            ParameterList.AddRange(method.DeclaredParameters.Select(p => p.ParameterType.ToLookupType(lookupModel)));

            return new LookupMethod(lookupModel) { DeclaringType = method.DeclaringType.ToLookupType(lookupModel), ParameterList = ParameterList, ReturnType = method.ReturnType.ToLookupType(lookupModel), Il2CppMethod = method };
        }
        public static IEnumerable<LookupMethod> ToLookupMethodList(this IList<MethodDef> monoMethods, LookupModel lookupModel)
        {
            foreach (MethodDef method in monoMethods)
                yield return method.ToLookupMethod(lookupModel);
        }
        public static IEnumerable<LookupMethod> ToLookupMethodList(this ReadOnlyCollection<MethodInfo> il2cppMethods, LookupModel lookupModel)
        {
            foreach (MethodInfo method in il2cppMethods)
                yield return method.ToLookupMethod(lookupModel);
        }
    }
}

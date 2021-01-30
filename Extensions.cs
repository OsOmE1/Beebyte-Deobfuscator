using Beebyte_Deobfuscator.Lookup;
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Beebyte_Deobfuscator
{
    public static class Extensions
    {
        public static LookupModule ToLookupModule(this TypeModel model, LookupModel lookupModel, EventHandler<string> statusCallback = null)
        {
            return new LookupModule(model.Namespaces, model.Types.Where(
                t => t.Namespace != "System" &&
                t.BaseType?.Namespace != "System" &&
                !t.Namespace.Contains("UnityEngine")
                )
                .ToLookupTypeList(lookupModel, statusCallback).ToList());
        }

        public static LookupType ToLookupType(this TypeInfo type, LookupModel lookupModel, bool recurse)
        {
            if (type == null)
            {
                return new LookupType(lookupModel);
            }
            if (!lookupModel.ProcessedIl2CppTypes.Contains(type))
            {
                lookupModel.ProcessedIl2CppTypes.Add(type);
                LookupType t = new LookupType(lookupModel) { Il2CppType = type, Children = new List<LookupType>() };
                lookupModel.Il2CppTypeMatches.Add(type, t);
            }

            if (!recurse)
            {
                return lookupModel.Il2CppTypeMatches[type];
            }

            lookupModel.Il2CppTypeMatches[type].Fields = type.DeclaredFields.ToLookupFieldList(lookupModel).ToList();
            lookupModel.Il2CppTypeMatches[type].DeclaringType = type.DeclaringType.ToLookupType(lookupModel, false);
            lookupModel.Il2CppTypeMatches[type].Properties = type.DeclaredProperties.ToLookupPropertyList(lookupModel).ToList();
            lookupModel.Il2CppTypeMatches[type].Methods = type.DeclaredMethods.ToLookupMethodList(lookupModel).ToList();

            if (!lookupModel.Il2CppTypeMatches[type].DeclaringType.IsEmpty)
            {
                lookupModel.Il2CppTypeMatches[type].DeclaringType.Children.Add(lookupModel.Il2CppTypeMatches[type]);
            }
            return lookupModel.Il2CppTypeMatches[type];
        }

        public static LookupType ToLookupType(this TypeDef type, LookupModel lookupModel, bool recurse)
        {
            if (type == null)
            {
                return new LookupType(lookupModel);
            }
            if (lookupModel.ProcessedMonoTypes.Contains(type))
            {
                lookupModel.ProcessedMonoTypes.Add(type);
                LookupType t = new LookupType(lookupModel) { MonoType = type, Children = new List<LookupType>() };
                lookupModel.MonoTypeMatches.Add(type, t);
            }

            if (!recurse)
            {
                return lookupModel.MonoTypeMatches[type];
            }

            lookupModel.MonoTypeMatches[type].Fields = type.Fields.ToLookupFieldList(lookupModel).ToList();
            lookupModel.MonoTypeMatches[type].DeclaringType = type.DeclaringType.ToLookupType(lookupModel, false);
            lookupModel.MonoTypeMatches[type].Properties = type.Properties.ToLookupPropertyList(lookupModel).ToList();
            lookupModel.MonoTypeMatches[type].Methods = type.Methods.ToLookupMethodList(lookupModel).ToList();

            if (!lookupModel.MonoTypeMatches[type].DeclaringType.IsEmpty)
            {
                lookupModel.MonoTypeMatches[type].DeclaringType.Children.Add(lookupModel.MonoTypeMatches[type]);
            }
            return lookupModel.MonoTypeMatches[type];
        }

        public static IEnumerable<LookupType> ToLookupTypeList(this IEnumerable<TypeDef> monoTypes, LookupModel lookupModel, EventHandler<string> statusCallback = null)
        {
            int current = 0;
            int total = monoTypes.Count(t => !t.IsNested);
            foreach (TypeDef type in monoTypes)
            {
                if (!type.IsNested)
                {
                    current++;
                    statusCallback?.Invoke(null, $"Loaded {current}/{total} types");
                    yield return type.ToLookupType(lookupModel, true);
                }
            }
        }

        public static IEnumerable<LookupType> ToLookupTypeList(this IEnumerable<TypeInfo> il2cppTypes, LookupModel lookupModel, EventHandler<string> statusCallback = null)
        {
            int current = 0;
            int total = il2cppTypes.Count(t => !t.IsNested);
            foreach (TypeInfo type in il2cppTypes)
            {
                if (!type.IsNested)
                {
                    current++;
                    statusCallback?.Invoke(null, $"Loaded {current}/{total} types");
                    yield return type.ToLookupType(lookupModel, true);
                }
            }
        }

        public static LookupField ToLookupField(this FieldInfo field, LookupModel lookupModel)
        {
            if (field == null)
            {
                return new LookupField(lookupModel);
            }

            return new LookupField(lookupModel)
            {
                Il2CppField = field,
                Type = field.FieldType.ToLookupType(lookupModel, false),
                DeclaringType = field.DeclaringType.ToLookupType(lookupModel, false)
            };
        }

        public static LookupField ToLookupField(this FieldDef field, LookupModel lookupModel)
        {
            if (field == null)
            {
                return new LookupField(lookupModel);
            }

            return new LookupField(lookupModel)
            {
                MonoField = field,
                Type = field.FieldType.TryGetTypeDef().ToLookupType(lookupModel, false),
                DeclaringType = field.DeclaringType.ToLookupType(lookupModel, false)
            };
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
            if (property == null)
            {
                return new LookupProperty(lookupModel);
            }

            return new LookupProperty(lookupModel)
            {
                PropertyType = property.PropertySig.RetType.TryGetTypeDef().ToLookupType(lookupModel, false),
                GetMethod = property.GetMethod.ToLookupMethod(lookupModel),
                SetMethod = property.SetMethod.ToLookupMethod(lookupModel),
                Index = index
            };
        }

        public static LookupProperty ToLookupProperty(this PropertyInfo property, LookupModel lookupModel)
        {
            if (property == null)
            {
                return new LookupProperty(lookupModel);
            }

            return new LookupProperty(lookupModel)
            {
                PropertyType = property.PropertyType.ToLookupType(lookupModel, false),
                GetMethod = property.GetMethod.ToLookupMethod(lookupModel),
                SetMethod = property.SetMethod.ToLookupMethod(lookupModel),
                Index = property.Index
            };
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
            if (method == null)
            {
                return new LookupMethod(lookupModel);
            }

            List<LookupType> ParameterList = new List<LookupType>();
            foreach (Parameter param in method.Parameters)
            {
                if (param.Type.IsTypeDefOrRef)
                {
                    ParameterList.Add(param.Type.TryGetTypeDef().ToLookupType(lookupModel, false));
                }
            }

            return new LookupMethod(lookupModel)
            {
                DeclaringType = method.DeclaringType.ToLookupType(lookupModel, false),
                ReturnType = method.ReturnType.TryGetTypeDef().ToLookupType(lookupModel, false),
                ParameterList = ParameterList
            };
        }

        public static LookupMethod ToLookupMethod(this MethodInfo method, LookupModel lookupModel)
        {
            if (method == null)
            {
                return new LookupMethod(lookupModel);
            }

            List<LookupType> ParameterList = new List<LookupType>();
            ParameterList.AddRange(method.DeclaredParameters.Select(p => p.ParameterType.ToLookupType(lookupModel, false)));

            return new LookupMethod(lookupModel)
            {
                DeclaringType = method.DeclaringType.ToLookupType(lookupModel, false),
                ParameterList = ParameterList,
                ReturnType = method.ReturnType.ToLookupType(lookupModel, false),
                Il2CppMethod = method
            };
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

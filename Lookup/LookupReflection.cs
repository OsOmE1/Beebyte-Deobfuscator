using Beebyte_Deobfuscator.Output;
using dnlib.DotNet;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Beebyte_Deobfuscator.Lookup
{
    public class LookupModel
    {
        public ConcurrentDictionary<TypeDef, LookupType> MonoTypeMatches { get; } = new ConcurrentDictionary<TypeDef, LookupType>();
        public ConcurrentDictionary<TypeInfo, LookupType> Il2CppTypeMatches { get; } = new ConcurrentDictionary<TypeInfo, LookupType>();
        public List<string> Namespaces { get; } = new List<string>();
        public List<LookupType> Types { get; } = new List<LookupType>();
        public TypeModel TypeModel { get; set; }

        public static LookupModel FromTypeModel(TypeModel typeModel, EventHandler<string> statusCallback = null)
        {
            LookupModel model = new LookupModel();
            model.Types.AddRange(typeModel.Types.ToLookupTypeList(model, statusCallback: statusCallback));
            model.Namespaces.AddRange(typeModel.Types.Select(t => t.Namespace).Distinct());
            model.TypeModel = typeModel;
            return model;
        }
        public static LookupModel FromModuleDef(ModuleDef moduleDef, EventHandler<string> statusCallback = null)
        {
            LookupModel model = new LookupModel();
            model.Types.AddRange(moduleDef.Types.ToLookupTypeList(model, statusCallback: statusCallback));
            model.Namespaces.AddRange(moduleDef.Types.Select(t => t.Namespace.String).Distinct());
            return model;
        }
    }
    public class LookupType
    {
        public readonly LookupModel Owner;
        public TypeInfo Il2CppType { get; set; }
        public TypeDef MonoType { get; set; }

        public string Name
        {
            get
            {
                return Il2CppType?.Name ?? MonoType?.Name;
            }
            set
            {
                if (Il2CppType != null)
                {
                    Il2CppType.Name = value;
                }
                else if (MonoType != null)
                {
                    MonoType.Name = value;
                }
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
        public bool Translated { get; private set; }
        public Translation Translation { get; set; }
        public LookupType DeclaringType { get; set; }
        public List<LookupField> Fields { get; set; }
        public List<LookupProperty> Properties { get; set; }
        public List<LookupMethod> Methods { get; set; }
        public List<LookupType> Children { get; set; }
        public bool Resolved { get; set; }
        public LookupType(LookupModel lookupModel) { Owner = lookupModel; }

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
            Fields = Il2CppType?.DeclaredFields.ToLookupFieldList(Owner).ToList() ?? MonoType?.Fields.ToLookupFieldList(Owner).ToList();
            DeclaringType = Il2CppType?.DeclaringType.ToLookupType(Owner, false) ?? MonoType?.DeclaringType.ToLookupType(Owner, false);
            Properties = Il2CppType?.DeclaredProperties.ToLookupPropertyList(Owner).ToList() ?? MonoType?.Properties.ToLookupPropertyList(Owner).ToList();
            Methods = Il2CppType?.DeclaredMethods.ToLookupMethodList(Owner).ToList() ?? MonoType?.Methods.ToLookupMethodList(Owner).ToList();

            if (!DeclaringType.IsEmpty)
            {
                DeclaringType.Children.Add(this);
            }
        }

        public override string ToString()
        {
            if (IsPrimitive)
            {
                return CSharpName;
            }

            string typename = "";
            if (!IsEmpty)
            {
                typename = CSharpName;
            }
            if (typename == "")
            {
                typename = "object";
            }

            if (IsArray)
            {
                typename = $"{ElementType?.ToString() ?? "object"}[]";
            }
            else if (IsGenericType && GenericTypeParameters.Any())
            {
                typename = Name.Split("`")[0] + "<";
                foreach (LookupType t in GenericTypeParameters)
                {
                    typename += t.ToString() + (GenericTypeParameters[GenericTypeParameters.Count() - 1] != t ? ", " : "");
                }
                typename += ">";
            }
            else
            {
                typename = CSharpName;
            }
            return typename;
        }
    }
    public class LookupField
    {
        public readonly LookupModel Owner;
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
                    Il2CppField.Name = value;
                }
                else if (MonoField != null)
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
        public LookupField(LookupModel lookupModel) { Owner = lookupModel; }
        public override string ToString() => CSharpName;
    }


    public class LookupProperty
    {
        public readonly LookupModel Owner;
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
                    Il2CppProperty.Name = value;
                }
                else if (MonoProperty != null)
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
        public LookupProperty(LookupModel lookupModel) { Owner = lookupModel; }

        public override string ToString()
        {
            return CSharpName;
        }
    }

    public class LookupMethod
    {
        public readonly LookupModel Owner;
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
                    Il2CppMethod.Name = value;
                }
                else if (MonoMethod != null)
                {
                    MonoMethod.Name = value;
                }
            }
        }
        public string CSharpName => Il2CppMethod?.ToString() ?? MonoMethod?.ToString() ?? "";
        public LookupType DeclaringType { get; set; }
        public LookupType ReturnType { get; set; }
        public List<LookupType> ParameterList { get; set; }
        public bool IsEmpty => Il2CppMethod == null && MonoMethod == null;
        public ulong Address => Il2CppMethod?.VirtualAddress?.Start ?? 0x0;
        public bool IsPropertymethod
        {
            get
            {
                if (DeclaringType.IsEmpty)
                {
                    return false;
                }
                if (DeclaringType.Properties != null)
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
        public LookupMethod(LookupModel lookupModel) { Owner = lookupModel; }

        public override string ToString() => CSharpName;
    }
}
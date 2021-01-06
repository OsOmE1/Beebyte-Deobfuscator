using Beebyte_Deobfuscator.Output;
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Beebyte_Deobfuscator.Lookup
{
    public class LookupModel
    {
        public List<Translation> Translations = new List<Translation>();

        private Dictionary<int, LookupMatrix> Table;
        private HashSet<int> ReservedKeys;
        private Dictionary<LookupType, LookupType> Matches;
        private HashSet<LookupType> MatchedTypes;

        public List<string> CleanTypeNames;
        public HashSet<LookupType> CleanTypes;
        public List<string> ObfTypeNames;
        public HashSet<LookupType> ObfTypes;

        public List<TypeDef> ProcessedMonoTypes = new List<TypeDef>();
        public List<TypeInfo> ProcessedIl2CppTypes = new List<TypeInfo>();
        public Dictionary<TypeDef, LookupType> MonoTypeMatches = new Dictionary<TypeDef, LookupType>();
        public Dictionary<TypeInfo, LookupType> Il2CppTypeMatches = new Dictionary<TypeInfo, LookupType>();

        public string NamingRegex;

        public LookupModel(TypeModel obfModel, TypeModel cleanModel, string namingRegex)
        {
            if (obfModel == null || cleanModel == null || namingRegex == null) return;
            NamingRegex = namingRegex;
            ProcessedMonoTypes = new List<TypeDef>();
            ProcessedIl2CppTypes = new List<TypeInfo>();
            MonoTypeMatches = new Dictionary<TypeDef, LookupType>();
            Il2CppTypeMatches = new Dictionary<TypeInfo, LookupType>();

            IEnumerable<TypeInfo> obfTypes = obfModel.Types.Where(t => t.Assembly.ShortName == "Assembly-CSharp.dll");
            IEnumerable<TypeInfo> cleanTypes = cleanModel.Types.Where(t => t.Assembly.ShortName == "Assembly-CSharp.dll");

            Init(LookupType.TypesFromIl2Cpp(obfTypes, this), LookupType.TypesFromIl2Cpp(cleanTypes, this));
        }
        public LookupModel(TypeModel obfModel, IEnumerable<TypeDef> cleanTypes, string namingRegex)
        {
            if (obfModel == null || cleanTypes == null || namingRegex == null) return;
            NamingRegex = namingRegex;
            ProcessedMonoTypes = new List<TypeDef>();
            ProcessedIl2CppTypes = new List<TypeInfo>();
            MonoTypeMatches = new Dictionary<TypeDef, LookupType>();
            Il2CppTypeMatches = new Dictionary<TypeInfo, LookupType>();

            IEnumerable<TypeInfo> obfTypes = obfModel.Types.Where(t => t.Assembly.ShortName == "Assembly-CSharp.dll");

            Init(LookupType.TypesFromIl2Cpp(obfTypes, this), LookupType.TypesFromMono(cleanTypes, this));
        }

        private void Init(IEnumerable<LookupType> obfTypes, IEnumerable<LookupType> cleanTypes)
        {
            Table = new Dictionary<int, LookupMatrix>();
            ReservedKeys = new HashSet<int>();
            Matches = new Dictionary<LookupType, LookupType>();
            MatchedTypes = new HashSet<LookupType>();

            CleanTypes = cleanTypes.ToHashSet();
            CleanTypeNames = new List<string>();
            CleanTypeNames.AddRange(cleanTypes.Select(x => x.Name));

            ObfTypes = obfTypes.ToHashSet();
            ObfTypeNames = new List<string>();
            ObfTypeNames.AddRange(obfTypes.Select(x => x.Name));

            int x = obfTypes.MaxObject(t => t.Fields.Count(f => f.IsStatic)).Fields.Count(f => f.IsStatic);
            int y = obfTypes.MaxObject(t => t.Fields.Count(f => f.IsLiteral)).Fields.Count(f => f.IsLiteral);
            int z = obfTypes.MaxObject(t => t.Fields.Count(f => !f.IsStatic && !f.IsLiteral)).Fields.Count(f => !f.IsStatic && !f.IsLiteral);
            int w = obfTypes.MaxObject(t => t.Properties.Count).Properties.Count;


            foreach (LookupType type in obfTypes.Where(t => t.ShouldTranslate(this)))
            {
                if (!type.DeclaringType.IsEmpty()) continue;

                LookupMatrix arr = Lookup(type.Fields.Count);
                if (arr == null)
                {
                    ReservedKeys.Add(type.Fields.Count);
                    LookupMatrix array = new LookupMatrix(x, y, z, w);
                    array.Insert(type);
                    Table.Add(type.Fields.Count, array);
                }
                else
                {
                    arr.Insert(type);
                }
            }
        }

        private LookupMatrix Lookup(int key)
        {
            return (ReservedKeys.Contains(key))
               ? Table[key]
               : null;
        }

        public LookupType GetMatchingType(LookupType type, bool checkoffsets)
        {
            LookupType typeInfo = null;

            LookupMatrix arr = Lookup(type.Fields.Count);

            if (arr == null) return typeInfo;

            List<LookupType> types = arr.Get(type);

            if (types.Count() == 1 && types[0] != null)
            {
                bool t1 = !MatchedTypes.Contains(types[0]);
                bool t2 = !CleanTypeNames.Contains(types[0].Name);
                if (t1 && t2)
                {
                    MatchedTypes.Add(types[0]);
                    Matches.Add(types[0], type);
                    return types[0];
                }
            }

            float best_score = 0.0f;

            foreach (LookupType t in types)
            {
                if (MatchedTypes.Contains(t) || CleanTypeNames.Contains(t.Name)) continue;

                if (t.Name.Equals(type.Name))
                {
                    typeInfo = t;
                    break;
                }
                float score = 0.0f;

                if (checkoffsets)
                    score = (Helpers.CompareFieldOffsets(t, type) + Helpers.CompareFieldTypes(t, type)) / 2;
                else
                    score = Helpers.CompareFieldTypes(t, type);
                if (score > best_score)
                {
                    best_score = score;
                    typeInfo = t;
                }
            }
            if (typeInfo != null && !MatchedTypes.Contains(typeInfo)) Matches.Add(typeInfo, type); MatchedTypes.Add(typeInfo);
            return typeInfo;
        }

        public void TranslateTypes(bool checkoffsets = false)
        {
            foreach (LookupType t in CleanTypes)
            {
                if (!t.DeclaringType.IsEmpty()) continue;
                LookupType matchingType = GetMatchingType(t, checkoffsets);
                if (matchingType == null) continue;

                if (matchingType.Children.Count() > 0 && t.Children.Count() > 0) LookupTranslators.TranslateChildren(matchingType, t, checkoffsets, this);

                matchingType.SetName(t.Name, this);
            }
            TranslateFields(checkoffsets);
        }

        public void TranslateFields(bool checkoffsets)
        {
            //Translate fields for matched types
            foreach (KeyValuePair<LookupType, LookupType> match in Matches)
            {
                LookupTranslators.TranslateFields(match.Key, match.Value, checkoffsets, this);
            }

            //Translate fields for types with the same name
            foreach (LookupType cleanType in CleanTypes)
            {
                LookupType obfType = null;
                List<LookupType> equalNamedTypes = ObfTypes.Where(t => t.Name == cleanType.Name).ToList();
                if (equalNamedTypes.Count() > 0) obfType = equalNamedTypes[0];
                if (!MatchedTypes.Contains(cleanType) && obfType != null)
                {
                    Translations.Add(new Translation(cleanType.Name, cleanType));
                    LookupTranslators.TranslateFields(obfType, cleanType, checkoffsets, this);
                }
            }
        }
    }
}

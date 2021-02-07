using Beebyte_Deobfuscator.Output;
using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator.Lookup
{
    public class LookupModel
    {
        public List<Translation> Translations { get; } = new List<Translation>();
        public LookupModule ObfModule { get; private set; }
        public LookupModule CleanModule { get; private set; }

        private LookupMatrix Matrix = new LookupMatrix();
        private Dictionary<LookupType, LookupType> Matches = new Dictionary<LookupType, LookupType>();
        private HashSet<LookupType> MatchedTypes = new HashSet<LookupType>();
        public List<string> CleanTypeNames { get; } = new List<string>();
        public SortedDictionary<string, LookupType> CleanTypes { get; } = new SortedDictionary<string, LookupType>();
        public List<string> ObfTypeNames { get; } = new List<string>();
        public SortedDictionary<string, LookupType> ObfTypes { get; } = new SortedDictionary<string, LookupType>();

        public List<TypeDef> ProcessedMonoTypes { get; } = new List<TypeDef>();
        public List<TypeInfo> ProcessedIl2CppTypes { get; } = new List<TypeInfo>();
        public Dictionary<TypeDef, LookupType> MonoTypeMatches { get; } = new Dictionary<TypeDef, LookupType>();
        public Dictionary<TypeInfo, LookupType> Il2CppTypeMatches { get; } = new Dictionary<TypeInfo, LookupType>();

        public string NamingRegex;

        public LookupModel(string namingRegex) => NamingRegex = namingRegex;

        public void Init(LookupModule obfModule, LookupModule cleanModule, EventHandler<string> statusCallback = null)
        {
            ObfModule = obfModule;
            CleanModule = cleanModule;

            statusCallback?.Invoke(this, "Sorting obfuscated and un-obfsucated types");
            foreach (LookupType type in cleanModule.Types)
            {
                if (type.IsEmpty)
                {
                    continue;
                }
                if (!CleanTypes.ContainsKey(type.Name))
                {
                    CleanTypes.Add(type.Name, type);
                }
            }
            foreach (LookupType type in obfModule.Types)
            {
                if (type.IsEmpty)
                {
                    continue;
                }
                if (!ObfTypes.ContainsKey(type.Name))
                {
                    ObfTypes.Add(type.Name, type);
                }
            }

            CleanTypeNames.AddRange(cleanModule.Types.Where(x => !x.IsEmpty).Select(x => x.Name));
            ObfTypeNames.AddRange(obfModule.Types.Where(x => !x.IsEmpty).Select(x => x.Name));

            int current = 0;
            int total = ObfTypes.Count(t => t.Value.ShouldTranslate && t.Value.DeclaringType.IsEmpty);
            foreach (var type in ObfTypes.Where(t => t.Value.ShouldTranslate && t.Value.DeclaringType.IsEmpty))
            {
                statusCallback?.Invoke(this, $"Created {current}/{total} Lookup Matrices");
                Matrix.Insert(type.Value);
            }
        }

        public LookupType GetMatchingType(LookupType type, bool checkoffsets)
        {
            LookupType typeInfo = null;

            List<LookupType> types = Matrix.Get(type);

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
                if (MatchedTypes.Contains(t) || CleanTypeNames.Contains(t.Name))
                {
                    continue;
                }

                if (t.Name == type.Name)
                {
                    typeInfo = t;
                    break;
                }
                float score = 0.0f;

                if (checkoffsets)
                {
                    score = (Helpers.CompareFieldOffsets(t, type, this) + Helpers.CompareFieldTypes(t, type, this)) / 2;
                }
                else
                {
                    score = Helpers.CompareFieldTypes(t, type, this);
                }

                if (score > best_score)
                {
                    best_score = score;
                    typeInfo = t;
                }
            }
            if (typeInfo != null && !MatchedTypes.Contains(typeInfo))
            {
                Matches.Add(typeInfo, type);
                MatchedTypes.Add(typeInfo);
            }
            return typeInfo;
        }

        public void TranslateTypes(bool checkoffsets = false, EventHandler<string> statusCallback = null)
        {
            var filteredTypes = CleanTypes.Where(t => t.Value.DeclaringType.IsEmpty && !t.Value.IsEnum && !Regex.Match(t.Value.Name, @"\+<.*(?:>).*__[1-9]{0,4}|[A-z]*=.{1,4}|<.*>").Success);
            int total = filteredTypes.Count();
            int current = 0;
            foreach (var type in filteredTypes)
            {
                current++;
                LookupType matchingType = GetMatchingType(type.Value, checkoffsets);
                if (matchingType == null)
                {
                    continue;
                }

                if (matchingType.Children.Any() && type.Value.Children.Any())
                {
                    LookupTranslators.TranslateChildren(matchingType, type.Value, checkoffsets, this);
                }

                matchingType.Name = type.Key;
                statusCallback?.Invoke(this, $"Deobfuscated {current}/{total} types");
            }
            TranslateFields(checkoffsets);
        }

        public void TranslateFields(bool checkoffsets)
        {
            // Translate fields for matched types
            foreach (var match in Matches)
            {
                LookupTranslators.TranslateFields(match.Key, match.Value, checkoffsets, this);
            }

            // Translate fields for types with the same name
            foreach (var cleanType in CleanTypes.Where(t => ObfTypes.ContainsKey(t.Value.Name)))
            {
                LookupType obfType = ObfTypes[cleanType.Key];
                if (!MatchedTypes.Contains(cleanType.Value) && obfType != null)
                {
                    Translations.Add(new Translation(cleanType.Key, cleanType.Value));
                    LookupTranslators.TranslateFields(obfType, cleanType.Value, checkoffsets, this);
                }
            }
        }
    }
}

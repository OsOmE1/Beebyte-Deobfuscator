using System.Collections.Generic;
using System.Linq;

namespace Beebyte_Deobfuscator.Lookup
{
    class LookupTranslators
    {
        public static void TranslateChildren(LookupType obfType, LookupType cleanType, bool checkoffsets, LookupModel lookupModel)
        {
            List<LookupType> obfChildren = obfType.Children;
            List<LookupType> cleanChildren = cleanType.Children;

            foreach (LookupType obfChild in obfChildren)
            {
                float best_score = 0.0f;
                LookupType best_match = null;

                foreach (LookupType cleanChild in cleanChildren.Where(t => t.Fields.Count == obfChild.Fields.Count))
                {
                    if (cleanChild.Name.Equals(obfChild.Name))
                    {
                        best_match = cleanChild;
                        break;
                    }
                    float score = 0.0f;

                    if (checkoffsets)
                        score = (Helpers.CompareFieldOffsets(cleanChild, obfChild) + Helpers.CompareFieldTypes(cleanChild, obfChild)) / 2;
                    else
                        score = Helpers.CompareFieldTypes(cleanChild, obfChild);

                    if (score > best_score)
                    {
                        best_score = score;
                        best_match = cleanChild;
                    }
                }
                if (best_match == null) continue;

                obfChild.Name = best_match.Name;
                TranslateFields(obfChild, best_match, checkoffsets, lookupModel);
            }
        }
        public static void TranslateFields(LookupType obfType, LookupType cleanType, bool checkoffsets, LookupModel lookupModel)
        {
            List<LookupField> obfGenericFields = obfType.Fields.Where(f => !f.IsStatic && !f.IsLiteral).ToList();
            List<LookupField> cleanGenericFields = cleanType.Fields.Where(f => !f.IsStatic && !f.IsLiteral).ToList();
            foreach (var obField in obfGenericFields.Select((Value, Index) => new { Value, Index }))
            {
                if (cleanGenericFields.Count() == obField.Index) break;

                LookupField cleanField = cleanGenericFields[obField.Index];
                if ((obField.Value.Offset == cleanField.Offset || !checkoffsets) && obField.Value.Name != cleanField.Name)
                {
                    obField.Value.Name = cleanField.Name;
                }
            }

            List<LookupField> obfStaticFields = obfType.Fields.Where(f => f.IsStatic).ToList();
            List<LookupField> cleanStaticFields = cleanType.Fields.Where(f => f.IsStatic).ToList();
            foreach (var obField in obfStaticFields.Select((Value, Index) => new { Value, Index }))
            {
                if (cleanStaticFields.Count() == obField.Index) break;

                LookupField cleanField = cleanStaticFields[obField.Index];
                if ((obField.Value.Offset == cleanField.Offset || !checkoffsets) && obField.Value.Name != cleanField.Name)
                {
                    obField.Value.Name = cleanField.Name;
                }
            }
        }
    }
}

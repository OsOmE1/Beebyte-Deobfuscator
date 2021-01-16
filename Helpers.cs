using Beebyte_Deobfuscator.Lookup;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beebyte_Deobfuscator
{
    class Helpers
    {
        public static bool IsValidRegex(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern) && (pattern.Trim().Length > 0))
            {
                try
                {
                    Regex.Match("", pattern);
                }
                catch (System.ArgumentException)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public static float CompareFieldOffsets(LookupType t1, LookupType t2, LookupModel lookupModel)
        {
            if (t1.Il2CppType == null || t2.Il2CppType == null)
            {
                return 1.0f;
            }

            float comparative_score = 1.0f;

            float score_penalty = comparative_score / t1.Fields.Count(f => !f.IsStatic && !f.IsLiteral);

            foreach (var f1 in t1.Fields.Select((Value, Index) => new { Value, Index }))
            {
                LookupField f2 = t2.Fields[f1.Index];
                if (f1.Value.Name == f2.Name) return 1.5f;
                if (!Regex.Match(f1.Value.Name, lookupModel.NamingRegex).Success && f1.Value.Name != f2.Name) return 0.0f;

                if (f1.Value.IsStatic || f1.Value.IsLiteral) continue;
                if (f1.Value.Offset != f2.Offset) comparative_score -= score_penalty;
            }

            return comparative_score;
        }

        public static float CompareFieldTypes(LookupType t1, LookupType t2, LookupModel lookupModel)
        {
            float comparative_score = 1.0f;

            float score_penalty = comparative_score / t1.Fields.Count(f => !f.IsStatic && !f.IsLiteral);

            foreach (var f1 in t1.Fields.Select((Value, Index) => new { Value, Index }))
            {
                LookupField f2 = t2.Fields[f1.Index];
                if (f1.Value.Name == f2.Name) return 1.5f;
                if (!Regex.Match(f1.Value.Name, lookupModel.NamingRegex).Success && f1.Value.Name != f2.Name) return 0.0f;

                if (f1.Value.IsStatic || f1.Value.IsLiteral) continue;

                if (f1.Value.GetType().Namespace == "System" && f2.GetType().Namespace == "System" && f1.Value.Name.Equals(f2.Name)) comparative_score -= score_penalty;
            }

            return comparative_score;
        }

        public static string SanitzeFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }
    }
}

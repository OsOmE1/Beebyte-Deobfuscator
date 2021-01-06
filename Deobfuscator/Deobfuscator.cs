using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector.Reflection;

namespace Beebyte_Deobfuscator.Deobfuscator
{
    public enum DeobfuscatorType
    {
        Il2Cpp,
        Apk,
        Mono
    }
    public class Deobfuscator
    {
        public static IDeobfuscator GetDeobfuscator(DeobfuscatorType type)
        {
            return type switch
            {
                DeobfuscatorType.Il2Cpp => new Il2CppDeobfuscator(),
                DeobfuscatorType.Apk => new ApkDeobfuscator(),
                DeobfuscatorType.Mono => new MonoDeobfuscator(),
                _ => null,
            };
        }
    }

    public interface IDeobfuscator
    {
        public LookupModel Process(TypeModel obfModel, BeebyteDeobfuscatorPlugin plugin);
    }
}

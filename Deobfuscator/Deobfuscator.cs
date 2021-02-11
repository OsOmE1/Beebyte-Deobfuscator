using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector.Reflection;

namespace Beebyte_Deobfuscator.Deobfuscator
{
    public interface IDeobfuscator
    {
        public LookupModule Process(TypeModel obfModel, BeebyteDeobfuscatorPlugin plugin);
    }

    public enum DeobfuscatorType
    {
        Il2Cpp,
        Mono
    }

    public class Deobfuscator
    {
        public static IDeobfuscator GetDeobfuscator(DeobfuscatorType type)
        {
            return type switch
            {
                DeobfuscatorType.Il2Cpp => new Il2CppDeobfuscator(),
                DeobfuscatorType.Mono => new MonoDeobfuscator(),
                _ => null,
            };
        }
    }
}

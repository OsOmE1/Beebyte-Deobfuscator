using Beebyte_Deobfuscator.Deobfuscator;
using Beebyte_Deobfuscator.Lookup;
using Beebyte_Deobfuscator.Output;
using Il2CppInspector;
using Il2CppInspector.Cpp;
using Il2CppInspector.PluginAPI.V100;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;

namespace Beebyte_Deobfuscator
{
    public class BeebyteDeobfuscatorPlugin : IPlugin, ILoadPipeline
    {
        public string Id => "beebyte-deobfuscator";

        public string Name => "Beebyte Deobfuscator";

        public string Author => "OsOmE1";

        public string Version => "1.0.0";

        public string Description => "Performs comparative deobfuscation for Beebyte";

        private PluginOptionText NamingRegexOption = new PluginOptionText
        {
            Name = "naming-pattern",
            Description = "Regex pattern for the beebyte naming scheme",
            Value = "",
            Required = true,
            Validate = text => Helpers.IsValidRegex(text) ? true : throw new ArgumentException("Must be valid regex!")
        };
        public string NamingRegex => NamingRegexOption.Value;

        private PluginOptionChoice<DeobfuscatorType> FileTypeOption = new PluginOptionChoice<DeobfuscatorType>
        {
            Name = "backend",
            Description = "Select Unity Scripting backend",
            Required = true,
            Value = DeobfuscatorType.Il2Cpp,

            Choices = new Dictionary<DeobfuscatorType, string>
            {
                [DeobfuscatorType.Il2Cpp] = "Il2Cpp",
                [DeobfuscatorType.Mono] = "Mono"
            },

            Style = PluginOptionChoiceStyle.Dropdown
        };
        public DeobfuscatorType FileType => FileTypeOption.Value;

        private PluginOptionFilePath MetadataPathOption = new PluginOptionFilePath
        {
            Name = "clean-metadata-path",
            Description = "Path to unobfuscated global-metadata.dat",
            MustExist = false,
            MustNotExist = false,
            IsFolder = false,
            Required = false
        };
        public string MetadataPath => MetadataPathOption.Value;

        public PluginOptionFilePath BinaryPathOption = new PluginOptionFilePath
        {
            Name = "clean-binary-path",
            Description = "Path to unobfuscated GameAssembly.dll or Package",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = false,
            AllowedExtensions = new Dictionary<string, string>
            {
                ["dll"] = "DLL files",
                ["apk"] = "APK",
                ["xapk"] = "XAPK",
                ["zip"] = "Zip Archive",
                ["ipa"] = "IPA",
                ["aab"] = "AAB",
                ["*"] = "All files"
            }
        };
        public string BinaryPath => BinaryPathOption.Value;

        public PluginOptionFilePath MonoPathOption = new PluginOptionFilePath
        {
            Name = "clean-mono-path",
            Description = "Path to unobfuscated Assembly-CSharp.dll",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = false,
            Validate = path => path.ToLower().EndsWith(".dll") ? true : throw new System.IO.FileNotFoundException($"You must supply a DLL file", path)
        };
        public string MonoPath => MonoPathOption.Value;

        public PluginOptionChoice<ExportType> ExportOption = new PluginOptionChoice<ExportType>
        {
            Name = "export",
            Description = "Select export option",
            Required = true,
            Value = ExportType.None,

            Choices = new Dictionary<ExportType, string>
            {
                [ExportType.None] = "None",
                [ExportType.Classes] = "Classes for Il2CppTranslator",
                [ExportType.PlainText] = "Plain Text",
                [ExportType.JsonTranslations] = "Json Translations",
                [ExportType.JsonMappings] = "Json Mappings"
            },

            Style = PluginOptionChoiceStyle.Dropdown
        };
        public ExportType Export => ExportOption.Value;

        public PluginOptionFilePath ExportPathOption = new PluginOptionFilePath
        {
            Name = "export-path",
            Description = "Path to your export folder",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = true,
        };
        public string ExportPath => ExportPathOption.Value;

        public PluginOptionText PluginNameOption = new PluginOptionText { Name = "plugin-name", Description = "The name of your plugin you want to generate the classes for", Value = "YourPlugin", Required = true };
        public string PluginName => PluginNameOption.Value;

        public List<IPluginOption> Options => new List<IPluginOption> { NamingRegexOption, FileTypeOption, MetadataPathOption, BinaryPathOption, MonoPathOption, ExportOption, ExportPathOption, PluginNameOption };

        public CppCompilerType? CompilerType;

        public BeebyteDeobfuscatorPlugin()
        {
            MetadataPathOption.If = () => FileType.Equals(DeobfuscatorType.Il2Cpp);
            BinaryPathOption.If = () => FileType.Equals(DeobfuscatorType.Il2Cpp);
            MonoPathOption.If = () => FileType.Equals(DeobfuscatorType.Mono);
            ExportPathOption.If = () => !Export.Equals(ExportType.None);
            PluginNameOption.If = () => Export.Equals(ExportType.Classes);
        }

        public void PostProcessImage<T>(FileFormatStream<T> stream, PluginPostProcessImageEventInfo data) where T : FileFormatStream<T>
        {
            if (!CompilerType.HasValue)
            {
                CompilerType = CppCompiler.GuessFromImage(stream);
            }
        }

        public void PostProcessTypeModel(TypeModel model, PluginPostProcessTypeModelEventInfo info)
        {
            IDeobfuscator deobfuscator = Deobfuscator.Deobfuscator.GetDeobfuscator(FileType);
            LookupModule lookupModel = deobfuscator.Process(model, this);

            if (lookupModel != null)
            {
                Translation.Export(this, lookupModel);
            }
            info.IsDataModified = true;
        }
    }
}

using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Il2CppInspector.PluginAPI.V100;
using Il2CppInspector.Reflection;
using Il2CppInspector.PluginAPI;
using Beebyte_Deobfuscator.Deobfuscator;
using Beebyte_Deobfuscator.Output;
using System.Threading.Tasks;
using Beebyte_Deobfuscator.Lookup;
using Il2CppInspector;

namespace Beebyte_Deobfuscator
{
    public class BeebyteDeobfuscatorPlugin : IPlugin, ILoadPipeline
    {
        public string Id => "beebyte-deobfuscator";

        public string Name => "Beebyte Deobfuscator";

        public string Author => "OsOmE1";

        public string Version => "0.7.2";

        public string Description => "Performs comparative deobfuscation for beebyte";

        public PluginOptionText NamingRegex = new PluginOptionText { Name = "naming-pattern", Description = "Regex pattern for the beebyte naming scheme", Value = "", Required = true, Validate = text => Helpers.IsValidRegex(text) ? true : throw new System.ArgumentException("Must be valid regex!") };

        public PluginOptionChoice<DeobfuscatorType> FileType = new PluginOptionChoice<DeobfuscatorType>
        {
            Name = "Compiler",
            Description = "Select Unity compiler",
            Required = true,
            Value = DeobfuscatorType.Il2Cpp,

            Choices = new Dictionary<DeobfuscatorType, string>
            {
                [DeobfuscatorType.Il2Cpp] = "Il2Cpp",
                [DeobfuscatorType.Apk] = "APK",
                [DeobfuscatorType.Mono] = "Mono"
            },

            Style = PluginOptionChoiceStyle.Dropdown
        };

        public PluginOptionFilePath MetadataPath = new PluginOptionFilePath
        {
            Name = "clean-metadata-path",
            Description = "Path to unobfuscated global-metadata.dat",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = false,
            Validate = path => path.ToLower().EndsWith(".dat") ? true : throw new System.IO.FileNotFoundException($"You must supply a Data file", path)
        };

        public PluginOptionFilePath BinaryPath = new PluginOptionFilePath
        {
            Name = "clean-binary-path",
            Description = "Path to unobfuscated GameAssembly.dll",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = false,
            Validate = path => path.ToLower().EndsWith(".dll") ? true : throw new System.IO.FileNotFoundException($"You must supply a DLL file", path)
        };

        public PluginOptionFilePath ApkPath = new PluginOptionFilePath
        {
            Name = "clean-apk-path",
            Description = "Path to unobfuscated APK package file",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = false,
        };

        public PluginOptionFilePath MonoPath = new PluginOptionFilePath
        {
            Name = "clean-mono-path",
            Description = "Path to unobfuscated Assembly-CSharp.dll",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = false,
            Validate = path => path.ToLower().EndsWith(".dll") ? true : throw new System.IO.FileNotFoundException($"You must supply a DLL file", path)
        };

        public PluginOptionChoice<ExportType> Export = new PluginOptionChoice<ExportType>
        {
            Name = "Export",
            Description = "Select export option",
            Required = true,
            Value = ExportType.None,

            Choices = new Dictionary<ExportType, string>
            {
                [ExportType.None] = "None",
                [ExportType.Classes] = "Classes for Il2CppTranslator",
                [ExportType.PlainText] = "Plain Text"
            },

            Style = PluginOptionChoiceStyle.Dropdown
        };

        public PluginOptionFilePath ExportPath = new PluginOptionFilePath
        {
            Name = "export-path",
            Description = "Path to your export folder",
            Required = true,
            MustExist = true,
            MustNotExist = false,
            IsFolder = true,
        };

        public PluginOptionText PluginName = new PluginOptionText { Name = "plugin-name", Description = "The name of your plugin you want to generate the classes for", Value = "YourPlugin", Required = true };

        public List<IPluginOption> Options => new List<IPluginOption> { NamingRegex, FileType, MetadataPath, BinaryPath, ApkPath, MonoPath, Export, ExportPath, PluginName };

        public object FileFormat;

        public BeebyteDeobfuscatorPlugin()
        {
            MetadataPath.If = () => FileType.Value.Equals(DeobfuscatorType.Il2Cpp);
            BinaryPath.If = () => FileType.Value.Equals(DeobfuscatorType.Il2Cpp);
            ApkPath.If = () => FileType.Value.Equals(DeobfuscatorType.Apk);
            MonoPath.If = () => FileType.Value.Equals(DeobfuscatorType.Mono);
            ExportPath.If = () => !Export.Value.Equals(ExportType.None);
            PluginName.If = () => Export.Value.Equals(ExportType.Classes);
        }

        public void LoadPipelineStarting(PluginLoadPipelineStartingEventInfo info) => FileFormat = null;

        public void PostProcessImage<T>(FileFormatStream<T> stream, PluginPostProcessImageEventInfo data) where T : FileFormatStream<T>
        {
            if(FileFormat == null) FileFormat = stream;
        }
        public void PostProcessTypeModel(TypeModel model, PluginPostProcessTypeModelEventInfo info)
        {

            IDeobfuscator deobfuscator = Deobfuscator.Deobfuscator.GetDeobfuscator(FileType.Value);
            LookupModel lookupModel = deobfuscator.Process(model, this);
            if (lookupModel == null) return;

            Task.Run(async () => await Translation.Export(this, lookupModel));
            info.IsDataModified = true;
        }
    }
}

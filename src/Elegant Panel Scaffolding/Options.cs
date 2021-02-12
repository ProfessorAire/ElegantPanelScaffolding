using EPS.UI.Controls;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace EPS
{
    [Description("Options for Touchpanel Class Compilation")]
    [DisplayName("Touchpanel Options")]
    [Serializable]
    public class Options : NotifyOfPropertyChangeBase
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static Options Current = new Options();
#pragma warning restore CA2211 // Non-constant fields should not be visible

        private string version = "v0.1.3-beta";
        [JsonIgnore]
        [Browsable(false)]
        public string Version { get => version; set => SetField(ref version, value); }

        private bool includeCoreFiles = true;
        [Description("If true then the core (template) files are included in the code generation. If you need to modify these files, setting this to false will prevent your changes from being overwritten.")]
        [DisplayName("Include Core Files")]
        public bool IncludeCoreFiles { get => includeCoreFiles; set => SetField(ref includeCoreFiles, value); }

        private string rootNamespace = "ProjectName.UI.Panels"; //"UI";
        [Description("The root namespace for the classes to reside in. Ie: ProjectName.UI.Panels")]
        [DisplayName("Root Namespace")]
        public string RootNamespace
        {
            get => rootNamespace;
            set => SetField(ref rootNamespace, value);
        }

        private bool previewFilePaths;
        [Description("Toggles whether to preview the full file path of files.")]
        [DisplayName("Preview File Paths")]
        public bool PreviewFilePaths
        {
            get => previewFilePaths;
            set => SetField(ref previewFilePaths, value);
        }

        [Browsable(false)]
        public string PanelNamespace { get; set; } = "";

        private string hardkeyPrefix = "Hardkey";
        [Description("The prefix to associate with Hardkeys. Hardkeys are only compiled once per project, not on a per-page basis. If Hardkey Names are provided this prefix is ignored.")]
        [DisplayName("Hardkey Prefix")]
        public string HardkeyPrefix
        {
            get => hardkeyPrefix;
            set => SetField(ref hardkeyPrefix, value);
        }

        private bool parseHardkeys;
        [Description("If enabled then hardkeys are included in the classes generated.")]
        [DisplayName("Include Hardkeys")]
        public bool ParseHardkeys
        {
            get => parseHardkeys;
            set => SetField(ref parseHardkeys, value);
        }

        private string hardkeyNames = "";
        [Description("Comma separated list of hardkey names, starting from Key1.")]
        [DisplayName("Hardkey Names")]
        public string HardkeyNames
        {
            get => hardkeyNames;
            set => SetField(ref hardkeyNames, value);
        }

        private string fieldPrefixes = "";
        [Description("Prefix to prepend to private field names.")]
        [DisplayName("Field Prefixes")]
        public string FieldPrefixes { get => fieldPrefixes; set => SetField(ref fieldPrefixes, value); }

        private string applicationTouchpanelPath = "";
        [Description("The path to the touchpanel to generate classes from.")]
        [DisplayName("Touchpanel Path")]
        [FileType(new string[] { "Touchpanel & Environment Files" }, new string[] { "*.vtz;*.xml;*.c3p;*.zip" })]
        public string ApplicationTouchpanelPath { get => applicationTouchpanelPath; set => SetField(ref applicationTouchpanelPath, value); }

        private string compilePath = "";
        [Description("The path that the generated classes will be saved to.")]
        [DisplayName("Destination Path")]
        [FolderPath()]
        public string CompilePath
        {
            get => compilePath;
            set => SetField(ref compilePath, value);
        }
    }
}

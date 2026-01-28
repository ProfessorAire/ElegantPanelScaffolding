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
        public static Options Current = new();
#pragma warning restore CA2211 // Non-constant fields should not be visible

        private string version = "v0.1.4-beta";
        [JsonIgnore]
        [Browsable(false)]
        public string Version { get => this.version; set => this.SetField(ref this.version, value); }

        private string configurationFilePath = "";
        [JsonIgnore]
        [Browsable(false)]
        public string ConfigurationFilePath { get => this.configurationFilePath; set => this.SetField(ref this.configurationFilePath, value); }

        private bool includeCoreFiles = true;
        [Description("If true then the core (template) files are included in the code generation. If you need to modify these files, setting this to false will prevent your changes from being overwritten.")]
        [DisplayName("Include Core Files")]
        public bool IncludeCoreFiles { get => this.includeCoreFiles; set => this.SetField(ref this.includeCoreFiles, value); }

        private string rootNamespace = "ProjectName.UI.Panels"; //"UI";
        [Description("The root namespace for the classes to reside in. Ie: ProjectName.UI.Panels")]
        [DisplayName("Root Namespace")]
        public string RootNamespace
        {
            get => this.rootNamespace;
            set => this.SetField(ref this.rootNamespace, value);
        }

        private bool previewFilePaths;
        [Description("Toggles whether to preview the full file path of files.")]
        [DisplayName("Preview File Paths")]
        public bool PreviewFilePaths
        {
            get => this.previewFilePaths;
            set => this.SetField(ref this.previewFilePaths, value);
        }

        [Browsable(false)]
        public string PanelNamespace { get; set; } = "";

        private string hardkeyPrefix = "Hardkey";
        [Description("The prefix to associate with Hardkeys. Hardkeys are only compiled once per project, not on a per-page basis. If Hardkey Names are provided this prefix is ignored.")]
        [DisplayName("Hardkey Prefix")]
        public string HardkeyPrefix
        {
            get => this.hardkeyPrefix;
            set => this.SetField(ref this.hardkeyPrefix, value);
        }

        private bool parseHardkeys;
        [Description("If enabled then hardkeys are included in the classes generated.")]
        [DisplayName("Include Hardkeys")]
        public bool ParseHardkeys
        {
            get => this.parseHardkeys;
            set => this.SetField(ref this.parseHardkeys, value);
        }

        private string hardkeyNames = "";
        [Description("Comma separated list of hardkey names, starting from Key1.")]
        [DisplayName("Hardkey Names")]
        public string HardkeyNames
        {
            get => this.hardkeyNames;
            set => this.SetField(ref this.hardkeyNames, value);
        }

        private string fieldPrefixes = "";
        [Description("Prefix to prepend to private field names.")]
        [DisplayName("Field Prefixes")]
        public string FieldPrefixes { get => this.fieldPrefixes; set => this.SetField(ref this.fieldPrefixes, value); }

        private string applicationTouchpanelPath = "";
        [Description("The path to the touchpanel to generate classes from.")]
        [DisplayName("Touchpanel Path")]
        [FileType(new string[] { "Touchpanel & Environment Files" }, new string[] { "*.vtz;*.xml;*.c3p;*.zip" })]
        public string ApplicationTouchpanelPath { get => this.applicationTouchpanelPath; set => this.SetField(ref this.applicationTouchpanelPath, value); }

        private string compilePath = "";
        [Description("The path that the generated classes will be saved to.")]
        [DisplayName("Destination Path")]
        [FolderPath()]
        public string CompilePath
        {
            get => this.compilePath;
            set => this.SetField(ref this.compilePath, value);
        }

        private string commonPath = "";
        [Description("The path that classes common between touchpanels will be generated in. These files exist in the namespace Evands.EPS.Common.")]
        [DisplayName("Core Files Path")]
        [FolderPath()]
        public string CommonPath
        {
            get => this.commonPath;
            set => this.SetField(ref this.commonPath, value);
        }

        private bool includeHelperFiles = true;
        [Description("When true will include helper files alongside the common files.")]
        [DisplayName("Include Helper Files")]
        public bool IncludeHelperFiles
        {
            get => this.includeHelperFiles;
            set => this.SetField(ref this.includeHelperFiles, value);
        }

        private bool implementINotifyPropertyChanged = true;
        [Description("When true will implement the INotifyPropertyChanged interface on all classes.")]
        [DisplayName("Implement INotifyPropertyChanged")]
        public bool ImplementINotifyPropertyChanged
        {
            get => this.implementINotifyPropertyChanged;
            set => this.SetField(ref this.implementINotifyPropertyChanged, value);
        }

        /// <summary>
        /// Converts an absolute path to a relative path based on the configuration file path.
        /// </summary>
        /// <param name="configFilePath">The path to the configuration file.</param>
        /// <param name="absolutePath">The absolute path to convert.</param>
        /// <returns>The relative path, or the original absolute path if conversion fails.</returns>
        public static string MakeRelativePath(string configFilePath, string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(configFilePath) || 
                string.IsNullOrWhiteSpace(absolutePath) ||
                !System.IO.Path.IsPathRooted(absolutePath))
            {
                return absolutePath;
            }

            var configDir = System.IO.Path.GetDirectoryName(configFilePath);
            if (string.IsNullOrWhiteSpace(configDir))
            {
                return absolutePath;
            }

            try
            {
                var configUri = new Uri(configDir + System.IO.Path.DirectorySeparatorChar);
                var pathUri = new Uri(absolutePath);
                
                if (configUri.Scheme != pathUri.Scheme)
                {
                    return absolutePath;
                }

                var relativeUri = configUri.MakeRelativeUri(pathUri);
                var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
                
                return relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            catch
            {
                // If URI creation fails, return the original path
                return absolutePath;
            }
        }

        /// <summary>
        /// Converts a relative path to an absolute path based on the configuration file path.
        /// </summary>
        /// <param name="configFilePath">The path to the configuration file.</param>
        /// <param name="relativePath">The relative path to convert.</param>
        /// <returns>The absolute path, or the original relative path if conversion fails.</returns>
        public static string MakeAbsolutePath(string configFilePath, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) ||
                System.IO.Path.IsPathRooted(relativePath) ||
                string.IsNullOrWhiteSpace(configFilePath))
            {
                return relativePath;
            }

            var configDir = System.IO.Path.GetDirectoryName(configFilePath);
            if (string.IsNullOrWhiteSpace(configDir))
            {
                return relativePath;
            }

            return System.IO.Path.GetFullPath(System.IO.Path.Combine(configDir, relativePath));
        }
    }
}

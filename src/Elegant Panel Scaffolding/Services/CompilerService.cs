using EPS.CodeGen.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

namespace EPS.Services
{
    public interface ICompilerService
    {
        /// <summary>Processes the touchpanel file and returns generated code files.</summary>
        Task<IReadOnlyList<GeneratedFile>> PreviewAsync(Options options);

        /// <summary>Compiles and writes generated code files to disk.</summary>
        Task<CompileResult> CompileAsync(Options options);
    }

    public readonly record struct GeneratedFile(string Name, string Path, string Content);

    public readonly record struct CompileResult(bool Success, string? ErrorMessage);

    public class CompilerService : ICompilerService
    {
        public async Task<IReadOnlyList<GeneratedFile>> PreviewAsync(Options options)
        {
            var builder = await TouchpanelProcessor.ProcessFileAsync(options);
            if (builder is null) return Array.Empty<GeneratedFile>();

            var results = new List<GeneratedFile>();
            foreach (var (className, classPath, nameSpace) in builder.Build("", builder.ClassName))
            {
                var name = options.PreviewFilePaths ? classPath : $"{className}.g.cs";
                results.Add(new GeneratedFile(name, classPath, nameSpace.ToString()));
            }
            return results;
        }

        public async Task<CompileResult> CompileAsync(Options options)
        {
            ClassBuilder? builder;
            try
            {
                builder = await TouchpanelProcessor.ProcessFileAsync(options);
            }
            catch (Exception ex)
            {
                return new CompileResult(false, ex.Message);
            }

            if (builder is null)
                return new CompileResult(false, "Processor returned no output.");

            try
            {
                foreach (var (_, classPath, nameSpace) in builder.Build("", builder.ClassName))
                {
                    var dir = Path.GetDirectoryName(classPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllText(classPath, nameSpace.ToString());
                }

                if (options.IncludeCoreFiles)
                {
                    var coreDir = Directory.CreateDirectory(
                        Path.Combine(options.CommonPath, @"Evands\EPS\Common"));

                    WriteEmbeddedResource("BooleanValueChangedEventArgs.g.cs", coreDir.FullName);
                    WriteEmbeddedResource("UShortValueChangedEventArgs.g.cs", coreDir.FullName);
                    WriteEmbeddedResource("StringValueChangedEventArgs.g.cs", coreDir.FullName);
                    WriteEmbeddedResource("PanelActions.g.cs", coreDir.FullName);
                    WriteEmbeddedResource("DeviceHelper.g.cs", coreDir.FullName);
                    WriteEmbeddedResource("ObjectEventArgs.g.cs", coreDir.FullName);
                    WriteEmbeddedResource("PanelUIBase.g.cs", coreDir.FullName);
                    WriteEmbeddedResource("IListItemProvider.g.cs", coreDir.FullName);
                }

                if (options.IncludeHelperFiles)
                {
                    var listsDir = Directory.CreateDirectory(
                        Path.Combine(options.CommonPath, @"Evands\EPS\Lists"));

                    WriteEmbeddedResource("ListBase.g.cs", listsDir.FullName);
                    WriteEmbeddedResource("ListItemBase.g.cs", listsDir.FullName);
                    WriteEmbeddedResource("SelectedItemChangedEventArgs.g.cs", listsDir.FullName);
                    WriteEmbeddedResource("ValueChangedEventArgs.g.cs", listsDir.FullName);
                    WriteEmbeddedResource("ValueSourceChangedEventArgs.g.cs", listsDir.FullName);
                    WriteEmbeddedResource("ItemSelectionChangedEventArgs.g.cs", listsDir.FullName);
                }
            }
            catch (Exception ex)
            {
                return new CompileResult(false, ex.Message);
            }

            return new CompileResult(true, null);
        }

        private static void WriteEmbeddedResource(string fileName, string destinationDirectory)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"EPS.Resources.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            using var reader = new StreamReader(stream);

            File.WriteAllText(Path.Combine(destinationDirectory, fileName), reader.ReadToEnd());
        }

        /// <summary>
        /// Opens a ZIP/VTZ archive and returns its entries. Uses System.IO.Compression
        /// rather than the former SharpZipLib dependency.
        /// </summary>
        public static ZipArchive OpenZip(string filePath) =>
            ZipFile.OpenRead(filePath);
    }
}

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EPS.Services
{
    public interface IOptionsService
    {
        Options Current { get; }
        event Action OptionsChanged;

        /// <summary>Saves the current session state to %APPDATA% for auto-restore on next launch.</summary>
        void SaveSession();

        /// <summary>Loads the previously saved session from %APPDATA%, if one exists.</summary>
        void LoadSession();

        /// <summary>Saves options to a user-chosen .eps file (paths converted to relative).</summary>
        /// <returns>True on success.</returns>
        bool SaveToFile(string filePath);

        /// <summary>Loads options from a .eps file and resolves relative paths to absolute.</summary>
        /// <returns>True on success.</returns>
        bool LoadFromFile(string filePath);
    }

    public class OptionsService : IOptionsService
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _sessionFilePath;

        public Options Current { get; private set; } = new Options();

        public event Action? OptionsChanged;

        public OptionsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _sessionFilePath = Path.Combine(appData, "Elegant Panel Scaffolding", "CurrentSession", "EPS.Report");
            LoadSession();
            Options.Current = Current;
        }

        public void SaveSession()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_sessionFilePath)!);
                File.WriteAllText(_sessionFilePath, JsonSerializer.Serialize(Current, s_jsonOptions));
            }
            catch { /* best-effort */ }
        }

        public void LoadSession()
        {
            if (!File.Exists(_sessionFilePath)) return;
            try
            {
                var loaded = JsonSerializer.Deserialize<Options>(File.ReadAllText(_sessionFilePath), s_jsonOptions);
                if (loaded is not null)
                {
                    Current = loaded;
                    Options.Current = Current;
                    OptionsChanged?.Invoke();
                }
            }
            catch { /* corrupt session — start fresh */ }
        }

        public bool SaveToFile(string filePath)
        {
            var original = (CompilePath: Current.CompilePath,
                            CommonPath: Current.CommonPath,
                            TpPath: Current.ApplicationTouchpanelPath);
            try
            {
                Current.ConfigurationFilePath = filePath;
                Options.Current.ConfigurationFilePath = filePath;

                Current.CompilePath = Options.MakeRelativePath(filePath, Current.CompilePath);
                Current.CommonPath = Options.MakeRelativePath(filePath, Current.CommonPath);
                Current.ApplicationTouchpanelPath = Options.MakeRelativePath(filePath, Current.ApplicationTouchpanelPath);

                File.WriteAllText(filePath, JsonSerializer.Serialize(Current, s_jsonOptions));
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Current.CompilePath = original.CompilePath;
                Current.CommonPath = original.CommonPath;
                Current.ApplicationTouchpanelPath = original.TpPath;
            }
        }

        public bool LoadFromFile(string filePath)
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<Options>(File.ReadAllText(filePath), s_jsonOptions);
                if (loaded is null) return false;

                loaded.ConfigurationFilePath = filePath;
                loaded.CompilePath = Options.MakeAbsolutePath(filePath, loaded.CompilePath);
                loaded.CommonPath = Options.MakeAbsolutePath(filePath, loaded.CommonPath);
                loaded.ApplicationTouchpanelPath = Options.MakeAbsolutePath(filePath, loaded.ApplicationTouchpanelPath);

                Current = loaded;
                Options.Current = Current;
                OptionsChanged?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

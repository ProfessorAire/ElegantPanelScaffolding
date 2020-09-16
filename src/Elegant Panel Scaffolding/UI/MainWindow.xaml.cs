using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace EPS.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Options Options
        {
            get => (Options)GetValue(OptionsProperty);
            set => SetValue(OptionsProperty, value);
        }

        // Using a DependencyProperty as the backing store for Options.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options", typeof(Options), typeof(MainWindow), new PropertyMetadata(new Options()));

        private readonly string filePath = "";
        public MainWindow()
        {
            InitializeComponent();
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            filePath = $"{directory}\\Elegant Panel Scaffolding\\CurrentSession\\EPS.Report";

            if (File.Exists(filePath))
            {
                var opt = Newtonsoft.Json.JsonConvert.DeserializeObject<Options>(File.ReadAllText(filePath));
                if (opt != null)
                {
                    Options.Current = opt;
                    Options = Options.Current;
                }
            }

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(Options));
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var browser = new Microsoft.Win32.SaveFileDialog()
            {
                AddExtension = true,
                CheckPathExists = true,
                Filter = "EPS File|*.eps",
                Title = "Save file..."
            };
            if (browser.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(browser.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(Options));
                    ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "File Saved");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, $"Unable to save file: {Path.GetFileName(browser.FileName)}");
                }
            }
            else
            {
                ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, "File Not Saved");
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var browser = new Microsoft.Win32.OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "EPS File|*.eps",
                Title = "Open file..."
            };
            if (browser.ShowDialog() == true)
            {
                try
                {
                    if (File.Exists(browser.FileName))
                    {
                        var opt = Newtonsoft.Json.JsonConvert.DeserializeObject<Options>(File.ReadAllText(browser.FileName));
                        if (opt != null)
                        {
                            Options = opt;
                            Options.Current = Options;
                            ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "File Loaded");
                        }
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, $"Unable to load file: {Path.GetFileName(browser.FileName)}");
                }
            }
            else
            {
                ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, "File Not Loaded");
            }
        }

        private async void Compile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Options.ApplicationTouchpanelPath) && Directory.Exists(Options.CompilePath))
            {
                Compile.IsEnabled = false;
                Preview.IsEnabled = false;
                Properties.IsRootEnabled = false;
                ProgressMeter.Visibility = Visibility.Visible;
                CodeGen.Builders.ClassBuilder? builder;

                try
                {
                    builder = await CodeGen.Builders.TouchpanelProcessor.ProcessFileAsync(Options);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    ShowToast(Color.FromRgb(180, 20, 20), Colors.White, $"Unable to compile project! Exception encountered: {ex.Message}", 7);
                    ProgressMeter.Visibility = Visibility.Collapsed;
                    Compile.IsEnabled = true;
                    Preview.IsEnabled = true;
                    Properties.IsRootEnabled = true;
                    return;
                }

                if (builder != null)
                {
                    var files = builder.Build("", builder.ClassName);
                    foreach (var (_, classPath, nameSpace) in files)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(classPath)))
                        {
                            _ = Directory.CreateDirectory(Path.GetDirectoryName(classPath));
                        }
                        File.WriteAllText(classPath, nameSpace.ToString());
                    }

                    if (Options.IncludeCoreFiles)
                    {
                        var dir = Directory.CreateDirectory(Path.Combine(Path.Combine(Options.CompilePath, builder.NamespaceBase), "Core"));
                        var file1 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/BooleanValueChangedEventArgs.g.cs"));
                        var file2 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/UShortValueChangedEventArgs.g.cs"));
                        var file3 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/StringValueChangedEventArgs.g.cs"));
                        var file4 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/PanelActions.g.cs"));
                        var file5 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/DeviceHelper.g.cs"));
                        var file6 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ObjectEventArgs.g.cs"));
                        var file7 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/PanelUIBase.g.cs"));

                        Func<string, string> replacement = (s) => s.Replace("SharpProTouchpanelDemo.UI", $"{Options.RootNamespace}.{builder.NamespaceBase}");

                        WriteResourcesText(Path.Combine(dir.FullName, "BooleanValueChangedEventArgs.g.cs"), file1.Stream, replacement);
                        WriteResourcesText(Path.Combine(dir.FullName, "UShortValueChangedEventArgs.g.cs"), file2.Stream, replacement);
                        WriteResourcesText(Path.Combine(dir.FullName, "StringValueChangedEventArgs.g.cs"), file3.Stream, replacement);
                        WriteResourcesText(Path.Combine(dir.FullName, "PanelActions.g.cs"), file4.Stream, replacement);
                        WriteResourcesText(Path.Combine(dir.FullName, "DeviceHelper.g.cs"), file5.Stream, replacement);
                        WriteResourcesText(Path.Combine(dir.FullName, "ObjectEventArgs.g.cs"), file6.Stream, replacement);

                        replacement = (s) => s
                            .Replace("SharpProTouchpanelDemo.UI", $"{Options.RootNamespace}.{builder.NamespaceBase}")
                            .Replace("{MethodAccessor}", Options.UseInternalValueSetters ? "internal" : "public");

                        WriteResourcesText(Path.Combine(dir.FullName, "PanelUIBase.g.cs"), file7.Stream, replacement);

                    }

                    ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "File successfully created!");
                }
                else
                {
                    ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Compile project!");
                }
                Preview.IsEnabled = true;
                Compile.IsEnabled = true;
                Properties.IsRootEnabled = true;
                ProgressMeter.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Compile project. Touchpanel or Compile Path does not exist!");
            }
        }

        private static void WriteResourcesText(string path, Stream stream, Func<string, string> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var sr = new StreamReader(stream))
            {
                var text = action(sr.ReadToEnd());
                File.WriteAllText(path, text);
            }
        }

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Options.ApplicationTouchpanelPath))
            {
                Compile.IsEnabled = false;
                Preview.IsEnabled = false;
                Properties.IsRootEnabled = false;
                ProgressMeter.Visibility = Visibility.Visible;
                CodeGen.Builders.ClassBuilder? builder;

                try
                {
                    builder = await CodeGen.Builders.TouchpanelProcessor.ProcessFileAsync(Options);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    ShowToast(Color.FromRgb(180, 20, 20), Colors.White, $"Unable to preview project! Exception encountered: {ex.Message}", 7);
                    ProgressMeter.Visibility = Visibility.Collapsed;
                    Compile.IsEnabled = true;
                    Preview.IsEnabled = true;
                    Properties.IsRootEnabled = true;
                    return;
                }

                if (builder != null)
                {
                    var files = builder.Build("", builder.ClassName);
                    var previewWindow = new Preview();
                    foreach (var (className, classPath, nameSpace) in files)
                    {
                        var detail = new DetailItem();
                        if (Options.Current.PreviewFilePaths)
                        {
                            detail.Name = $"{classPath}";
                        }
                        else
                        {
                            detail.Name = $"{className}.g.cs";
                        }
                        detail.Content = nameSpace.ToString();
                        previewWindow.ItemList.Add(detail);
                    }
                    previewWindow.Show();
                    previewWindow.WindowState = WindowState.Maximized;
                    ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "Preview successfully created!");
                }
                else
                {
                    ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Preview project!");
                }
                ProgressMeter.Visibility = Visibility.Collapsed;
                Compile.IsEnabled = true;
                Preview.IsEnabled = true;
                Properties.IsRootEnabled = true;
                ProgressMeter.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Preview project. Touchpanel path does not exist!");
            }
        }

        private async void ShowToast(Color background, Color foreground, string text, int durationInSeconds = 4)
        {
            ToastText.Text = text;
            ToastText.Foreground = new SolidColorBrush(foreground);
            ToastContainer.Background = new SolidColorBrush(background)
            {
                Opacity = 1
            };
            ToastContainer.Visibility = Visibility.Visible;
            await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));
            ToastContainer.Visibility = Visibility.Collapsed;
            ToastText.Text = "";
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var hw = new HelpAbout();
            _ = hw.ShowDialog();
        }
    }
}

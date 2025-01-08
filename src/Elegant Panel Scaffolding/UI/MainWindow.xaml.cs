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
            get => (Options)this.GetValue(OptionsProperty);
            set => this.SetValue(OptionsProperty, value);
        }

        // Using a DependencyProperty as the backing store for Options.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options", typeof(Options), typeof(MainWindow), new PropertyMetadata(new Options()));

        private readonly string filePath = "";
        public MainWindow()
        {
            this.InitializeComponent();
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            this.filePath = $"{directory}\\Elegant Panel Scaffolding\\CurrentSession\\EPS.Report";

            if (File.Exists(this.filePath))
            {
                var opt = Newtonsoft.Json.JsonConvert.DeserializeObject<Options>(File.ReadAllText(this.filePath));
                if (opt != null)
                {
                    Options.Current = opt;
                    this.Options = Options.Current;
                }
            }

            Closing += this.MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(this.filePath));
                File.WriteAllText(this.filePath, Newtonsoft.Json.JsonConvert.SerializeObject(this.Options));
            }
            catch { }
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
                    File.WriteAllText(browser.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(this.Options));
                    this.ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "File Saved");
                }
                catch
                {
                    this.ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, $"Unable to save file: {Path.GetFileName(browser.FileName)}");
                }
            }
            else
            {
                this.ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, "File Not Saved");
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
                            this.Options = opt;
                            Options.Current = this.Options;
                            this.ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "File Loaded");
                        }
                    }
                }
                catch
                {
                    this.ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, $"Unable to load file: {Path.GetFileName(browser.FileName)}");
                }
            }
            else
            {
                this.ShowToast(Color.FromRgb(180, 20, 20), Colors.Black, "File Not Loaded");
            }
        }

        private async void Compile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(this.Options.ApplicationTouchpanelPath) && Directory.Exists(this.Options.CompilePath))
            {
                this.Compile.IsEnabled = false;
                this.Preview.IsEnabled = false;
                this.Properties.IsRootEnabled = false;
                this.ProgressMeter.Visibility = Visibility.Visible;
                CodeGen.Builders.ClassBuilder? builder;

                try
                {
                    builder = await CodeGen.Builders.TouchpanelProcessor.ProcessFileAsync(this.Options);
                }
                catch (Exception ex)
                {
                    this.ShowToast(Color.FromRgb(180, 20, 20), Colors.White, $"Unable to compile project! Exception encountered: {ex.Message}", 7);
#if DEBUG
                    MessageBox.Show(ex.StackTrace);
#endif
                    this.ProgressMeter.Visibility = Visibility.Collapsed;
                    this.Compile.IsEnabled = true;
                    this.Preview.IsEnabled = true;
                    this.Properties.IsRootEnabled = true;
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

                    if (this.Options.IncludeCoreFiles)
                    {
                        var dir = Directory.CreateDirectory(Path.Combine(this.Options.CommonPath, @"Evands\EPS\Common"));

                        var file1 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/BooleanValueChangedEventArgs.g.cs"));
                        var file2 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/UShortValueChangedEventArgs.g.cs"));
                        var file3 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/StringValueChangedEventArgs.g.cs"));
                        var file4 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/PanelActions.g.cs"));
                        var file5 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/DeviceHelper.g.cs"));
                        var file6 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ObjectEventArgs.g.cs"));
                        var file7 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/PanelUIBase.g.cs"));
                        var file8 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/IListItemProvider.g.cs"));

                        WriteResourcesText(Path.Combine(dir.FullName, "BooleanValueChangedEventArgs.g.cs"), file1.Stream);
                        WriteResourcesText(Path.Combine(dir.FullName, "UShortValueChangedEventArgs.g.cs"), file2.Stream);
                        WriteResourcesText(Path.Combine(dir.FullName, "StringValueChangedEventArgs.g.cs"), file3.Stream);
                        WriteResourcesText(Path.Combine(dir.FullName, "PanelActions.g.cs"), file4.Stream);
                        WriteResourcesText(Path.Combine(dir.FullName, "DeviceHelper.g.cs"), file5.Stream);
                        WriteResourcesText(Path.Combine(dir.FullName, "ObjectEventArgs.g.cs"), file6.Stream);
                        WriteResourcesText(Path.Combine(dir.FullName, "PanelUIBase.g.cs"), file7.Stream);
                        WriteResourcesText(Path.Combine(dir.FullName, "IListItemProvider.g.cs"), file8.Stream);
                    }

                    if (this.Options.IncludeHelperFiles)
                    {
                        var file9 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ListBase.g.cs"));
                        var file10 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ListItemBase.g.cs"));
                        var file11 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/SelectedItemChangedEventArgs.g.cs"));
                        var file12 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ValueChangedEventArgs.g.cs"));
                        var file13 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ValueSourceChangedEventArgs.g.cs"));
                        var file14 = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/ItemSelectionChangedEventArgs.g.cs"));

                        var listsDirectory = Directory.CreateDirectory(Path.Combine(this.Options.CommonPath, @"Evands\EPS\Lists"));

                        WriteResourcesText(Path.Combine(listsDirectory.FullName, "ListBase.g.cs"), file9.Stream);
                        WriteResourcesText(Path.Combine(listsDirectory.FullName, "ListItemBase.g.cs"), file10.Stream);
                        WriteResourcesText(Path.Combine(listsDirectory.FullName, "SelectedItemChangedEventArgs.g.cs"), file11.Stream);
                        WriteResourcesText(Path.Combine(listsDirectory.FullName, "ValueChangedEventArgs.g.cs"), file12.Stream);
                        WriteResourcesText(Path.Combine(listsDirectory.FullName, "ValueSourceChangedEventArgs.g.cs"), file13.Stream);
                        WriteResourcesText(Path.Combine(listsDirectory.FullName, "ItemSelectionChangedEventArgs.g.cs"), file14.Stream);
                    }

                    this.ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "File successfully created!");
                }
                else
                {
                    this.ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Compile project!");
                }
                this.Preview.IsEnabled = true;
                this.Compile.IsEnabled = true;
                this.Properties.IsRootEnabled = true;
                this.ProgressMeter.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Compile project. Touchpanel or Compile Path does not exist!");
            }
        }

        private static void WriteResourcesText(string path, Stream stream)
        {
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
                var text = sr.ReadToEnd();
                File.WriteAllText(path, text);
            }
        }

        private async void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(this.Options.ApplicationTouchpanelPath))
            {
                this.Compile.IsEnabled = false;
                this.Preview.IsEnabled = false;
                this.Properties.IsRootEnabled = false;
                this.ProgressMeter.Visibility = Visibility.Visible;
                CodeGen.Builders.ClassBuilder? builder;

                try
                {
                    builder = await CodeGen.Builders.TouchpanelProcessor.ProcessFileAsync(this.Options);
                }
                catch (Exception ex)
                {
                    this.ShowToast(Color.FromRgb(180, 20, 20), Colors.White, $"Unable to preview project! Exception encountered: {ex.Message}", 7);
#if DEBUG
                    MessageBox.Show(ex.StackTrace);
#endif
                    this.ProgressMeter.Visibility = Visibility.Collapsed;
                    this.Compile.IsEnabled = true;
                    this.Preview.IsEnabled = true;
                    this.Properties.IsRootEnabled = true;
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
                    this.ShowToast(Color.FromRgb(20, 180, 20), Colors.Black, "Preview successfully created!");
                }
                else
                {
                    this.ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Preview project!");
                }
                this.ProgressMeter.Visibility = Visibility.Collapsed;
                this.Compile.IsEnabled = true;
                this.Preview.IsEnabled = true;
                this.Properties.IsRootEnabled = true;
                this.ProgressMeter.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.ShowToast(Color.FromRgb(180, 20, 20), Colors.White, "Unable to Preview project. Touchpanel path does not exist!");
            }
        }

        private async void ShowToast(Color background, Color foreground, string text, int durationInSeconds = 4)
        {
            this.ToastText.Text = text;
            this.ToastText.Foreground = new SolidColorBrush(foreground);
            this.ToastContainer.Background = new SolidColorBrush(background)
            {
                Opacity = 1
            };
            this.ToastContainer.Visibility = Visibility.Visible;
            await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));
            this.ToastContainer.Visibility = Visibility.Collapsed;
            this.ToastText.Text = "";
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var hw = new HelpAbout();
            _ = hw.ShowDialog();
        }
    }
}

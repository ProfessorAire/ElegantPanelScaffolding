using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace EPS.UI.Controls
{
    /// <summary>
    /// Interaction logic for PropertyBrowser.xaml
    /// </summary>
    public partial class PropertyBrowser : UserControl
    {
        public PropertyBrowser() => this.InitializeComponent();

        public object PropertyObject
        {
            get => this.GetValue(PropertyObjectProperty);
            set => this.SetValue(PropertyObjectProperty, value);
        }

        // Using a DependencyProperty as the backing store for PropertyObject.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PropertyObjectProperty =
            DependencyProperty.Register("PropertyObject", typeof(object), typeof(PropertyBrowser), new PropertyMetadata(null, PropertyObjectChanged));

        private static void PropertyObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyBrowser browser)
            {
                browser.UpdateProperties();
            }
        }

        public bool IsRootEnabled
        {
            get => (bool)this.GetValue(IsRootEnabledProperty);
            set => this.SetValue(IsRootEnabledProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsRootEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRootEnabledProperty =
            DependencyProperty.Register("IsRootEnabled", typeof(bool), typeof(PropertyBrowser), new PropertyMetadata(true));

        public void UpdateProperties()
        {
            this.Pane.Children.Clear();
            if (this.PropertyObject == null) { return; }

            var properties = this.PropertyObject.GetType().GetProperties().OrderBy(i => i.Name).OrderBy(i =>
            {
                var order = i.GetCustomAttribute<OrderAttribute>();
                return order?.Order ?? 0;
            });

            var editors = new List<(string name, DetailPart part, PropertyInfo propertyInfo)>();

            var fourThick = new Thickness(4);

            foreach (var property in properties)
            {
                var propertyValue = property.GetValue(this.PropertyObject, null);
                var isTooltippable = true;
                // Continue the loop if the property isn't browsable.
                if (property.CustomAttributes.Where((a) =>
                {
                    return a.AttributeType == typeof(BrowsableAttribute) && ((bool)a.ConstructorArguments[0].Value == false);
                }).Any())
                {
                    continue;
                }
                // Put any else-ifs for specialized statement handling if necessary.
                else
                {
                    var displayNameAttribute = property.GetCustomAttribute(typeof(DisplayNameAttribute));
                    var displayName = ((DisplayNameAttribute)displayNameAttribute)?.DisplayName;
                    var infoAttribute = property.GetCustomAttribute(typeof(DescriptionAttribute));
                    var info = ((DescriptionAttribute)infoAttribute)?.Description;
                    var g = new Grid();
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    g.ColumnDefinitions.Add(new ColumnDefinition());
                    g.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Auto);
                    g.ColumnDefinitions[0].MaxWidth = 200;
                    g.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                    var propertyTitle = new TextBlock()
                    {
                        TextAlignment = TextAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = fourThick,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };

                    if (displayName == null || string.IsNullOrEmpty(displayName))
                    {
                        displayName = property.Name;
                    }

                    propertyTitle.Text = $"{displayName}";

                    Grid.SetColumn(propertyTitle, 0);
                    _ = g.Children.Add(propertyTitle);


                    if (property.PropertyType.Name == nameof(Boolean))
                    {
                        var checkBox = new CheckBox();
                        _ = checkBox.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                        checkBox.Margin = fourThick;
                        Grid.SetColumn(checkBox, 1);
                        _ = g.Children.Add(checkBox);
                    }
                    else if (property.PropertyType.IsEnum)
                    {
                        var comboBox = new ComboBox
                        {
                            Margin = fourThick,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        Grid.SetColumn(comboBox, 1);
                        comboBox.ItemsSource = property.PropertyType.GetEnumNames();
                        _ = comboBox.SetBinding(ComboBox.TextProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                        _ = g.Children.Add(comboBox);

                    }
                    else if (property.PropertyType.Name == nameof(String) &&
                        property.CustomAttributes.Where(a =>
                        {
                            return a.AttributeType == typeof(FolderPathAttribute);
                        }).Any())
                    {
                        var g2 = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        Grid.SetColumn(g2, 1);
                        _ = g.Children.Add(g2);

                        g2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        g2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                        var textBox = new TextBox()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Margin = fourThick,
                            VerticalAlignment = VerticalAlignment.Center,
                            IsReadOnly = false
                        };
                        _ = textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                        Grid.SetColumn(textBox, 0);
                        Grid.SetRow(textBox, 0);
                        _ = g2.Children.Add(textBox);

                        // Create error message TextBlock
                        var errorText = new TextBlock()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Margin = new Thickness(4, 0, 4, 4),
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = System.Windows.Media.Brushes.Red,
                            TextWrapping = TextWrapping.Wrap,
                            Visibility = Visibility.Collapsed
                        };
                        Grid.SetColumn(errorText, 0);
                        Grid.SetRow(errorText, 1);
                        Grid.SetColumnSpan(errorText, 2);
                        _ = g2.Children.Add(errorText);

                        // Add LostFocus event handler for validation
                        textBox.LostFocus += (o, a) =>
                        {
                            var path = textBox.Text;
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                try
                                {
                                    // Check if path is valid
                                    var fullPath = path;
                                    if (!System.IO.Path.IsPathRooted(path))
                                    {
                                        // If it's a relative path, try to resolve it
                                        var options = this.PropertyObject as Options;
                                        if (options != null && !string.IsNullOrWhiteSpace(options.ConfigurationFilePath))
                                        {
                                            fullPath = options.MakeAbsolutePath(path);
                                        }
                                    }

                                    // Validate the path format
                                    var pathRoot = System.IO.Path.GetPathRoot(fullPath);
                                    if (string.IsNullOrWhiteSpace(pathRoot))
                                    {
                                        errorText.Text = "Invalid path format.";
                                        errorText.Visibility = Visibility.Visible;
                                    }
                                    else
                                    {
                                        errorText.Visibility = Visibility.Collapsed;
                                    }
                                }
                                catch
                                {
                                    errorText.Text = "Invalid path format.";
                                    errorText.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                errorText.Visibility = Visibility.Collapsed;
                            }
                        };

                        var browseButton = new Button()
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = fourThick,
                            Content = "..."
                        };

                        browseButton.Click += (o, a) =>
                        {
                            var browser = new System.Windows.Forms.FolderBrowserDialog()
                            {
                                Description = "Select a folder...",
                                ShowNewFolderButton = true,
                                SelectedPath = (string)property.GetValue(this.PropertyObject)
                            };
                            if (browser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                if (!string.IsNullOrWhiteSpace(browser.SelectedPath))
                                {
                                    property.SetValue(this.PropertyObject, browser.SelectedPath);
                                    errorText.Visibility = Visibility.Collapsed;
                                }
                            }
                        };

                        Grid.SetColumn(browseButton, 1);
                        Grid.SetRow(browseButton, 0);
                        _ = g2.Children.Add(browseButton);
                    }
                    else if (property.PropertyType.Name == nameof(String) &&
                        property.CustomAttributes.Where((a) =>
                        {
                            return a.AttributeType == typeof(FileTypeAttribute);
                        }).Any())
                    {

                        var ft = property.GetCustomAttribute(typeof(FileTypeAttribute));
                        var extensions = Array.Empty<string>();
                        var names = Array.Empty<string>();

                        if (ft != null)
                        {
                            extensions = ((FileTypeAttribute)ft)?.ValidExtensions ?? Array.Empty<string>();
                        }

                        if (ft != null)
                        {
                            names = ((FileTypeAttribute)ft)?.FileNames ?? Array.Empty<string>();
                        }

                        var g2 = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        Grid.SetColumn(g2, 1);
                        _ = g.Children.Add(g2);

                        g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        g2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                        var textBox = new TextBox()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Margin = fourThick,
                            VerticalAlignment = VerticalAlignment.Center,
                            IsReadOnly = true
                        };
                        _ = textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                        Grid.SetColumn(textBox, 0);
                        _ = g2.Children.Add(textBox);

                        var browseButton = new Button()
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = fourThick,
                            Content = "..."
                        };
                        var extensionListString = "";
                        var extensionList = new List<string>();
                        var index = 0;
                        foreach (var ext in extensions)
                        {
                            var current = ext;
                            if (!current.StartsWith("*", StringComparison.InvariantCulture))
                            {
                                current = $"*{ext}";
                            }
                            if (!(current[1] == '.'))
                            {
                                current = current.Insert(1, ".");
                            }
                            extensionList.Add(current);
                            current = $"{names[index]}|{current}";
                            if (ext != extensions.Last())
                            {
                                current += "|";
                            }
                            extensionListString += current;
                            index++;
                        }
                        browseButton.Click += (o, a) =>
                        {
                            var browser = new Microsoft.Win32.OpenFileDialog()
                            {
                                AddExtension = true,
                                CheckFileExists = true,
                                CheckPathExists = true,
                                Filter = extensionListString,
                                Title = "Select a file..."
                            };
                            _ = browser.ShowDialog();
                            if (!string.IsNullOrWhiteSpace(browser.FileName))
                            {
                                property.SetValue(this.PropertyObject, browser.FileName);
                            }
                        };

                        Grid.SetColumn(browseButton, 1);
                        _ = g2.Children.Add(browseButton);
                    }
                    else if (property.PropertyType.Name == nameof(String))
                    {
                        var maskStringAttribute = property.GetCustomAttribute(typeof(MaskStringAttribute));
                        var maskChar = ((MaskStringAttribute)maskStringAttribute)?.MaskChar;
                        if (maskChar.HasValue)
                        {
                            var passBox = new PasswordBox()
                            {
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                Margin = fourThick,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            if (!property.CanWrite)
                            {
                                passBox.IsEnabled = false;
                            }
                            passBox.PasswordChar = maskChar.Value;
                            passBox.Password = (string)property.GetValue(this.PropertyObject);
                            passBox.PasswordChanged += (o, a) => property.SetValue(this.PropertyObject, passBox.Password);
                            Grid.SetColumn(passBox, 1);
                            _ = g.Children.Add(passBox);
                        }
                        else
                        {
                            var textBox = new TextBox()
                            {
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                Margin = fourThick,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            if (!property.CanWrite)
                            {
                                textBox.IsReadOnly = true;
                            }
                            _ = textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, Mode = !property.CanWrite ? BindingMode.OneTime : BindingMode.TwoWay });
                            Grid.SetColumn(textBox, 1);
                            _ = g.Children.Add(textBox);
                        }
                    }
                    else if (property.PropertyType.Name == nameof(Int32))
                    {
                        var textBox = new TextBox()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Margin = fourThick,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        if (!property.CanWrite)
                        {
                            textBox.IsReadOnly = true;
                        }
                        _ = textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                        Grid.SetColumn(textBox, 1);
                        textBox.KeyDown += (o, a) =>
                        {
                            if (a.Key is not (>= Key.D0 and <= Key.D9 or >= Key.NumPad0 and <= Key.NumPad9) and not Key.Tab and not Key.LeftCtrl and not Key.RightCtrl)
                            {
                                a.Handled = true;
                                return;
                            }
                        };
                        _ = g.Children.Add(textBox);
                    }
                    else if (property.PropertyType.Name == nameof(List<object>))
                    {
                        isTooltippable = false;
                        foreach (var p in (List<object>)property.GetValue(property.Name))
                        {
                            var pb = new PropertyBrowser();
                            _ = pb.SetBinding(PropertyObjectProperty, new Binding(p.ToString()));
                            _ = g.Children.Add(pb);
                        }
                    }
                    else if (property.PropertyType.Name == typeof(ObservableCollection<object>).Name)
                    {
                        var viewer = new ItemsControl()
                        {
                            ItemTemplate = this.TryFindResource("nestedBrowserTemplate") as DataTemplate,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            Padding = new Thickness(8),
                            BorderBrush = System.Windows.Media.Brushes.Transparent,
                            Margin = new Thickness(8, 16, 4, 4)
                        };
                        _ = viewer.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

                        Grid.SetColumnSpan(viewer, 2);
                        propertyTitle.VerticalAlignment = VerticalAlignment.Top;
                        _ = g.Children.Add(viewer);                        
                    }
                    // ALL OTHER ELSE IFS GO ABOVE HERE!
                    else if (property.PropertyType.IsClass)
                    {
                        isTooltippable = false;
                        var expander = new Expander();
                        var pb = new PropertyBrowser();
                        _ = pb.SetBinding(PropertyObjectProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                        expander.Margin = fourThick;
                        pb.Margin = fourThick;
                        Grid.SetColumn(expander, 0);
                        Grid.SetColumnSpan(expander, 2);
                        expander.Content = pb;
                        expander.Header = propertyTitle.Text;
                        g.Children.Remove(propertyTitle);
                        _ = g.Children.Add(expander);
                    }

                    if (isTooltippable)
                    {
                        var tipStack = new StackPanel();
                        var tipTitle = new TextBlock
                        {
                            MaxWidth = 300,
                            Text = $"{displayName}\r",
                            FontWeight = FontWeights.Bold,
                            TextWrapping = TextWrapping.Wrap
                        };
                        _ = tipStack.Children.Add(tipTitle);
                        if (info != null && !string.IsNullOrEmpty(info))
                        {
                            var tipText = new TextBlock
                            {
                                Text = info,
                                TextWrapping = TextWrapping.Wrap,
                                MaxWidth = 300
                            };
                            _ = tipStack.Children.Add(tipText);
                        }
                        g.ToolTip = tipStack;
                    }

                    this.Pane.RowDefinitions.Add(new RowDefinition());
                    this.Pane.RowDefinitions[this.Pane.RowDefinitions.Count - 1].Height = new GridLength(0, GridUnitType.Auto);
                    Grid.SetRow(g, this.Pane.RowDefinitions.Count - 1);
                    _ = this.Pane.Children.Add(g);
                }

            }

            var bind = new Binding(nameof(this.IsRootEnabled))
            {
                Source = this
            };
            foreach (var exp in ((Grid)this.Pane.Children[0]).Children)
            {
                if (exp.GetType() == typeof(Expander))
                {
                    var obj = ((Expander)exp).Content as FrameworkElement;
                    _ = (obj?.SetBinding(IsEnabledProperty, bind));
                }
            }

        }
    }
}

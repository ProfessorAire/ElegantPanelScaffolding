using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public PropertyBrowser() => InitializeComponent();

        public object PropertyObject
        {
            get => GetValue(PropertyObjectProperty);
            set => SetValue(PropertyObjectProperty, value);
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
            get => (bool)GetValue(IsRootEnabledProperty);
            set => SetValue(IsRootEnabledProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsRootEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRootEnabledProperty =
            DependencyProperty.Register("IsRootEnabled", typeof(bool), typeof(PropertyBrowser), new PropertyMetadata(true));

        public void UpdateProperties()
        {
            Pane.Children.Clear();
            if (PropertyObject == null) { return; }

            var properties = PropertyObject.GetType().GetProperties().OrderBy(i => i.Name);
            var editors = new List<(string name, DetailPart part, PropertyInfo propertyInfo)>();

            var fourThick = new Thickness(4);

            foreach (var property in properties)
            {
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
                    g.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
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

                        browseButton.Click += (o, a) =>
                        {
                            var browser = new System.Windows.Forms.FolderBrowserDialog()
                            {
                                Description = "Select a folder...",
                                ShowNewFolderButton = true,
                                SelectedPath = (string)property.GetValue(PropertyObject)
                            };
                            if (browser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                if (!string.IsNullOrWhiteSpace(browser.SelectedPath))
                                {
                                    property.SetValue(PropertyObject, browser.SelectedPath);
                                }
                            }
                        };

                        Grid.SetColumn(browseButton, 1);
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
                                property.SetValue(PropertyObject, browser.FileName);
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
                            passBox.Password = (string)property.GetValue(PropertyObject);
                            passBox.PasswordChanged += (o, a) => property.SetValue(PropertyObject, passBox.Password);
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
                            _ = textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
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
                            if (!(a.Key >= Key.D0 && a.Key <= Key.D9 || a.Key >= Key.NumPad0 && a.Key <= Key.NumPad9) && a.Key != Key.Tab && a.Key != Key.LeftCtrl && a.Key != Key.RightCtrl)
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

                    Pane.RowDefinitions.Add(new RowDefinition());
                    Pane.RowDefinitions[Pane.RowDefinitions.Count - 1].Height = new GridLength(0, GridUnitType.Auto);
                    Grid.SetRow(g, Pane.RowDefinitions.Count - 1);
                    _ = Pane.Children.Add(g);
                }

            }

            var bind = new Binding(nameof(IsRootEnabled))
            {
                Source = this
            };
            foreach (var exp in ((Grid)Pane.Children[0]).Children)
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

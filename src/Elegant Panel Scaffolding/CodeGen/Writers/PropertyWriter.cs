using EPS.CodeGen.Builders;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class PropertyWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

        public List<string> Getter { get; } = new List<string>();

        public List<string> Setter { get; } = new List<string>();

        public string Name { get; set; } = "";

        public FieldWriter? BackingFieldWriter { get; set; }

        public HelpWriter Help { get; set; }

        public string Type { get; set; } = "";

        public string DefaultValue { get; set; } = "";

        public bool HasSetter { get; set; } = true;

        public bool HasGetter { get; set; } = true;

        public bool PrivateSetter { get; set; }

        public bool PrivateGetter { get; set; }

        public bool UsePropertyChangeEvent { get; set; } = true;

        public bool AlwaysRaisePropertyChangeEvents { get; set; } = true;

        public bool ImplementINotifyPropertyChanged { get; set; }

        private readonly int indentLevel;

        public Modifier Modifier { get; set; } = Modifier.None;

        public Accessor Accessor { get; set; } = Accessor.Public;

        public PropertyWriter(string name, string type, bool createBackingFieldWriter, int indentLevel = 0)
        {
            Name = name;
            Type = type;
            this.indentLevel = indentLevel;

            if (createBackingFieldWriter)
            {
#pragma warning disable CA1308 // Normalize strings to uppercase
                var fieldName = $"{Name.ToLower(CultureInfo.InvariantCulture)[0]}{Name.Substring(1)}";
#pragma warning restore CA1308 // Normalize strings to uppercase

                BackingFieldWriter = new FieldWriter(fieldName, type);
            }

            Help = new HelpWriter(indentLevel);
        }

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indent)
        {
            _ = sb.Clear();

            // Help Stuff.
            var prefix = HasGetter ?
                HasSetter ? "Gets or sets" :
                "Gets" :
                HasSetter ?
                "Sets" :
                string.Empty;

            Help.Summary = prefix + Help.Summary.Replace("Gets or sets", string.Empty).Replace("Gets", string.Empty).Replace("Sets", string.Empty);
            _ = sb.Append(Help.ToString(indent));

            // Then the Property Name.
            _ = sb.Append(indent.GetTabs());
            _ = sb.Append($"{Accessor.GetTextValue()}{Modifier.GetTextValue()}{Type} {Name}");

            // Getter/Setter.
            if ((BackingFieldWriter == null && Setter.Count == 0 && Getter.Count == 0) || (!(UsePropertyChangeEvent || ImplementINotifyPropertyChanged) && HasGetter && Getter.Count == 0 && HasSetter && Setter.Count == 0))
            {
                _ = sb.AppendLine($" {{ {(PrivateGetter ? "private " : "")}get; {(PrivateSetter ? "private " : "")}set; }}");
            }
            else
            {
                // Open Bracket.
                _ = sb.AppendLine();
                _ = sb.Append(indent.GetTabs());
                _ = sb.AppendLine("{");
                indent++;
                // Getter.
                if (HasGetter)
                {
                    if (UsePropertyChangeEvent || ImplementINotifyPropertyChanged || Getter.Count > 0)
                    {
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine($"{(PrivateGetter ? "private " : "")}get ");
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine("{");
                        indent++;

                        if (BackingFieldWriter != null)
                        {
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine($"return {BackingFieldWriter.Name};");
                        }

                        foreach (var l in Getter)
                        {
                            _ = sb.AppendLine(SanitizeSpaces(l, indent));
                            if (l.Contains("{"))
                            {
                                indent++;
                            }
                            else if (l.Contains("}"))
                            {
                                indent--;
                            }
                        }

                        indent--;
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine("}");
                    }
                    else
                    {
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine($"{(PrivateGetter ? "private " : "")}get;");
                    }

                }

                if (HasGetter && HasSetter)
                {
                    _ = sb.AppendLine();
                }

                // Setter.
                if (HasSetter)
                {
                    if (UsePropertyChangeEvent || ImplementINotifyPropertyChanged || Setter.Count > 0)
                    {
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine($"{(PrivateSetter ? "private " : "")}set");
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine("{");
                        indent++;

#pragma warning disable CA1308 // Normalize strings to uppercase
                        var fieldName = BackingFieldWriter?.Name ?? $"{Name.ToLower(CultureInfo.InvariantCulture)[0]}{Name.Substring(1)}";
#pragma warning restore CA1308 // Normalize strings to uppercase

                        if (fieldName == "value")
                        {
                            fieldName = $"this.{fieldName}";
                        }

                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine($"var isChanged = {fieldName} != value;");

                        if (BackingFieldWriter != null)
                        {
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine($"{BackingFieldWriter.Name} = value;");
                        }

                        foreach (var l in Setter)
                        {
                            if (!string.IsNullOrEmpty(l))
                            {
                                _ = sb.AppendLine(SanitizeSpaces(l, indent));
                                if (l.Contains("{"))
                                {
                                    indent++;
                                }
                                else if (l.Contains("}"))
                                {
                                    indent--;
                                }
                            }
                            else
                            {
                                _ = sb.AppendLine();
                            }
                        }

                        if (UsePropertyChangeEvent || ImplementINotifyPropertyChanged)
                        {
                            var argType = "Boolean";
                            if (Type == "ushort")
                            {
                                argType = "UShort";
                            }
                            else if (Type == "string")
                            {
                                argType = "String";
                            }

                            // Check for notifications
                            if (!AlwaysRaisePropertyChangeEvents)
                            {
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine($"if(isChanged)");
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("{");
                                indent++;
                            }

                            if (UsePropertyChangeEvent)
                            {
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine($"var changeEvent = {Name}Changed;");
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine($"if(changeEvent != null)");
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("{");
                                indent++;
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine($"changeEvent.Invoke(this, new {argType}ValueChangedEventArgs(value));");
                                indent--;
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("}");
                            }

                            if (ImplementINotifyPropertyChanged)
                            {
                                _ = sb.AppendLine();

                                if (AlwaysRaisePropertyChangeEvents)
                                {
                                    _ = sb.Append(indent.GetTabs());
                                    _ = sb.AppendLine($"if(isChanged)");
                                    _ = sb.Append(indent.GetTabs());
                                    _ = sb.AppendLine("{");
                                    indent++;
                                }

                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("var propertyChangeEvent = PropertyChanged;");
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("if (propertyChangeEvent != null)");
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("{");
                                indent++;
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine($"propertyChangeEvent.Invoke(this, new PropertyChangedEventArgs(\"{Name}\"));");
                                indent--;
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("}");

                                if (AlwaysRaisePropertyChangeEvents)
                                {
                                    indent--;
                                    _ = sb.Append(indent.GetTabs());
                                    _ = sb.AppendLine("}");
                                }
                            }

                            if (!AlwaysRaisePropertyChangeEvents)
                            {
                                indent--;
                                _ = sb.Append(indent.GetTabs());
                                _ = sb.AppendLine("}");
                            }
                        }

                        indent--;
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine("}");
                    }
                    else
                    {
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine($"{(PrivateSetter ? "private " : "")}set;");
                    }
                }

                // Close Bracket.
                indent--;
                _ = sb.Append(indent.GetTabs());
                _ = sb.Append('}');
            }

            return sb.ToString();
        }

        private static string SanitizeSpaces(string text, int indent) => $"{indent.GetTabs()}{text.Replace("\n", $"\n{indent.GetTabs()}")}";
    }
}

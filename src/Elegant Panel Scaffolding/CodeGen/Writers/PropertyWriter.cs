using EPS.CodeGen.Builders;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class PropertyWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

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
            this.Name = name;
            this.Type = type;
            this.indentLevel = indentLevel;

            if (createBackingFieldWriter)
            {
                var fieldName = $"{this.Name.ToLower(CultureInfo.InvariantCulture)[0]}{this.Name.Substring(1)}";
                this.BackingFieldWriter = new FieldWriter(fieldName, type) { Accessor = Accessor.Private };
            }

            this.Help = new HelpWriter(indentLevel);
        }

        public override string ToString() => this.ToString(this.indentLevel);

        public override string ToString(int indent)
        {
            _ = this.sb.Clear();

            // Help Stuff.
            var prefix = this.HasGetter && !this.PrivateGetter ? "Gets" : string.Empty;
            prefix += !string.IsNullOrWhiteSpace(prefix) && this.HasSetter && !this.PrivateSetter ? " or sets" : string.Empty;
            prefix += string.IsNullOrWhiteSpace(prefix) && this.HasSetter && !this.PrivateSetter ? "Sets" : string.Empty;

            if (prefix != null)
            {
                this.Help.Summary = prefix + this.Help.Summary.Replace("Gets or sets", string.Empty).Replace("Gets", string.Empty).Replace("Sets", string.Empty);
            }

            _ = this.sb.Append(this.Help.ToString(indent));

            // Then the Property Name.
            _ = this.sb.Append(indent.GetTabs());
            _ = this.sb.Append($"{this.Accessor.GetTextValue()}{this.Modifier.GetTextValue()}{this.Type} {this.Name}");

            // Getter/Setter.
            if ((this.BackingFieldWriter == null && this.Setter.Count == 0 && this.Getter.Count == 0) || (!(this.UsePropertyChangeEvent || this.ImplementINotifyPropertyChanged) && this.HasGetter && this.Getter.Count == 0 && this.HasSetter && this.Setter.Count == 0))
            {
                _ = this.sb.AppendLine($" {{ {(this.PrivateGetter ? "private " : "")}get; {(this.PrivateSetter ? "private " : "")}set; }}");
            }
            else
            {
                // Open Bracket.
                _ = this.sb.AppendLine();
                _ = this.sb.Append(indent.GetTabs());
                _ = this.sb.AppendLine("{");
                indent++;
                // Getter.
                if (this.HasGetter)
                {
                    if (this.UsePropertyChangeEvent || this.ImplementINotifyPropertyChanged || this.Getter.Count > 0)
                    {
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine($"{(this.PrivateGetter ? "private " : "")}get ");
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine("{");
                        indent++;

                        if (this.BackingFieldWriter != null)
                        {
                            _ = this.sb.Append(indent.GetTabs());
                            _ = this.sb.AppendLine($"return {this.BackingFieldWriter.Name};");
                        }

                        foreach (var l in this.Getter)
                        {
                            _ = this.sb.AppendLine(SanitizeSpaces(l, indent));
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
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine("}");
                    }
                    else
                    {
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine($"{(this.PrivateGetter ? "private " : "")}get;");
                    }

                }

                if (this.HasGetter && this.HasSetter)
                {
                    _ = this.sb.AppendLine();
                }

                // Setter.
                if (this.HasSetter)
                {
                    if (this.UsePropertyChangeEvent || this.ImplementINotifyPropertyChanged || this.Setter.Count > 0)
                    {
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine($"{(this.PrivateSetter ? "private " : "")}set");
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine("{");
                        indent++;
                        var fieldName = this.BackingFieldWriter?.Name ?? $"{this.Name.ToLower(CultureInfo.InvariantCulture)[0]}{this.Name.Substring(1)}";
                        if (fieldName == "value")
                        {
                            fieldName = $"this.{fieldName}";
                        }

                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine($"var isChanged = {fieldName} != value;");

                        if (this.BackingFieldWriter != null)
                        {
                            _ = this.sb.Append(indent.GetTabs());
                            _ = this.sb.AppendLine($"this.{this.BackingFieldWriter.Name} = value;");
                        }

                        foreach (var l in this.Setter)
                        {
                            if (!string.IsNullOrEmpty(l))
                            {
                                _ = this.sb.AppendLine(SanitizeSpaces(l, indent));
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
                                _ = this.sb.AppendLine();
                            }
                        }

                        if (this.UsePropertyChangeEvent || this.ImplementINotifyPropertyChanged)
                        {
                            var argType = "Boolean";
                            if (this.Type == "ushort")
                            {
                                argType = "UShort";
                            }
                            else if (this.Type == "string")
                            {
                                argType = "String";
                            }

                            // Check for notifications
                            if (!this.AlwaysRaisePropertyChangeEvents)
                            {
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine($"if(isChanged)");
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("{");
                                indent++;
                            }

                            if (this.UsePropertyChangeEvent)
                            {
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine($"var changeEvent = {this.Name}Changed;");
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine($"if(changeEvent != null)");
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("{");
                                indent++;
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine($"changeEvent.Invoke(this, new {argType}ValueChangedEventArgs(value));");
                                indent--;
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("}");
                            }

                            if (this.ImplementINotifyPropertyChanged)
                            {
                                _ = this.sb.AppendLine();

                                if (this.AlwaysRaisePropertyChangeEvents)
                                {
                                    _ = this.sb.Append(indent.GetTabs());
                                    _ = this.sb.AppendLine($"if(isChanged)");
                                    _ = this.sb.Append(indent.GetTabs());
                                    _ = this.sb.AppendLine("{");
                                    indent++;
                                }

                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("var propertyChangeEvent = PropertyChanged;");
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("if (propertyChangeEvent != null)");
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("{");
                                indent++;
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine($"propertyChangeEvent.Invoke(this, new PropertyChangedEventArgs(\"{this.Name}\"));");
                                indent--;
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("}");

                                if (this.AlwaysRaisePropertyChangeEvents)
                                {
                                    indent--;
                                    _ = this.sb.Append(indent.GetTabs());
                                    _ = this.sb.AppendLine("}");
                                }
                            }

                            if (!this.AlwaysRaisePropertyChangeEvents)
                            {
                                indent--;
                                _ = this.sb.Append(indent.GetTabs());
                                _ = this.sb.AppendLine("}");
                            }
                        }

                        indent--;
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine("}");
                    }
                    else
                    {
                        _ = this.sb.Append(indent.GetTabs());
                        _ = this.sb.AppendLine($"{(this.PrivateSetter ? "private " : "")}set;");
                    }
                }

                // Close Bracket.
                indent--;
                _ = this.sb.Append(indent.GetTabs());
                _ = this.sb.Append('}');
            }

            return this.sb.ToString();
        }

        private static string SanitizeSpaces(string text, int indent) => $"{indent.GetTabs()}{text.Replace("\n", $"\n{indent.GetTabs()}")}";
    }
}

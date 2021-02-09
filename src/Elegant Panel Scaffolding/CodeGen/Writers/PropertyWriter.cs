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

        public HelpWriter Help { get; set; }

        public string Type { get; set; } = "";

        public string DefaultValue { get; set; } = "";

        public bool HasSetter { get; set; } = true;

        public bool HasGetter { get; set; } = true;

        public bool PrivateSetter { get; set; }

        public bool PrivateGetter { get; set; }

        public bool UsePropertyChangeEvent { get; set; }

        private readonly int indentLevel;

        public Modifier Modifier { get; set; } = Modifier.None;

        public Accessor Accessor { get; set; } = Accessor.Public;

        public PropertyWriter(string name, string type, int indentLevel = 0)
        {
            Name = name;
            Type = type;
            this.indentLevel = indentLevel;
            Help = new HelpWriter(indentLevel);
        }

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indent)
        {
            _ = sb.Clear();

            // Help Stuff.
            _ = sb.Append(Help.ToString(indent));

            // Then the Property Name.
            _ = sb.Append(indent.GetTabs());
            _ = sb.Append($"{Accessor.GetTextValue()}{Modifier.GetTextValue()}{Type} {Name}");



            // Getter/Setter.
            if (HasGetter && Getter.Count == 0 && HasSetter && Setter.Count == 0)
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
                    if (Getter.Count > 0)
                    {
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine($"{(PrivateGetter ? "private " : "")}get ");
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine("{");
                        indent++;
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

                // Setter.
                if (HasSetter)
                {
                    if (Setter.Count > 0)
                    {
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine($"{(PrivateSetter ? "private " : "")}set");
                        _ = sb.Append(indent.GetTabs());
                        _ = sb.AppendLine("{");
                        indent++;

#pragma warning disable CA1308 // Normalize strings to uppercase
                        var fieldName = $"{Name.ToLower(CultureInfo.InvariantCulture)[0]}{Name.Substring(1)}";
#pragma warning restore CA1308 // Normalize strings to uppercase

                        if (fieldName == "value")
                        {
                            fieldName = $"this.{fieldName}";
                        }

                        if (UsePropertyChangeEvent)
                        {
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine($"var isChanged = {fieldName} != value;");
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

                        if (UsePropertyChangeEvent)
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
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine($"if(isChanged)");
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine("{");
                            indent++;

                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine($"if({Name}Changed != null)");
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine("{");
                            indent++;
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine($"{Name}Changed(this, new {argType}ValueChangedEventArgs(value));");
                            indent--;
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine("}");

                            indent--;
                            _ = sb.Append(indent.GetTabs());
                            _ = sb.AppendLine("}");
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

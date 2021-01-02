using System;
using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class MethodWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

        private List<(string Type, string Name)> Parameters { get; } = new List<(string Type, string Name)>();

        public List<string> MethodLines { get; } = new List<string>();

        public string Name { get; private set; } = "";

        public HelpWriter Help { get; private set; }

        public Modifier Modifier { get; set; } = Modifier.None;

        public string ReturnType { get; private set; } = "void";

        public Accessor Accessor { get; set; } = Accessor.Public;

        private readonly int indentLevel;

        public void AddParameter(string type, string name, string help)
        {
            Parameters.Add((type, name));
            Help.Parameters.Add((name, help));
        }

        public MethodWriter(string methodName, string methodHelp, string returnType = "void", int indentLevel = 0)
        {
            Name = methodName;
            ReturnType = returnType;
            this.indentLevel = indentLevel;
            Help = new HelpWriter(indentLevel)
            {
                Summary = methodHelp
            };
        }

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indent)
        {
            _ = sb.Clear();

            // Help Stuff First
            _ = sb.Append(Help.ToString(indent));

            // Main Method and Parameters
            _ = sb.Append(indent.GetTabs());
            _ = sb.Append($"{Accessor.GetTextValue()}{Modifier.GetTextValue()}{(!string.IsNullOrEmpty(ReturnType) ? $"{ReturnType} " : "")}{Name}(");
            var isFirst = true;
            foreach (var p in Parameters)
            {
                if (!isFirst)
                {
                    _ = sb.Append(", ");
                }
                _ = sb.Append($"{p.Type} {p.Name}");
                isFirst = false;
            }
            _ = sb.Append(')');

            if (Modifier == Modifier.Partial || Modifier == Modifier.Abstract)
            {
                _ = sb.Append(';');
                return sb.ToString();
            }

            _ = sb.AppendLine();
            _ = sb.AppendLine($"{indent.GetTabs()}{{");
            indent++;

            // Method Calls
            for (var i = 0; i < MethodLines.Count; i++)
            {
                var l = MethodLines[i];

                if (l.Contains("}") && !l.Contains("{"))
                {
                    indent--;
                }
                if (!string.IsNullOrEmpty(l))
                {
                    _ = sb.Append(indent.GetTabs());
                    var end = "";
                    if (!l.EndsWith(";", System.StringComparison.InvariantCulture) && i < MethodLines.Count - 1)
                    {
                        if (!(l.EndsWith("{", System.StringComparison.InvariantCulture) || l.EndsWith("}", System.StringComparison.InvariantCulture)))
                        {
                            if (!(MethodLines[i + 1].TrimStart('\t').StartsWith("{", StringComparison.InvariantCulture) || MethodLines[i + 1].TrimStart('\t').StartsWith("}", StringComparison.InvariantCulture) || l.EndsWith(",", System.StringComparison.InvariantCulture)))
                            {
                                end = ";";
                            }
                        }
                    }
                    _ = sb.AppendLine(SanitizeSpaces($"{l}{end}", indent));
                    if (l.Contains("{") && !l.Contains("}"))
                    {
                        indent++;
                    }
                }
                else
                {
                    _ = sb.AppendLine();
                }
            }
            indent--;

            //Close it Out
            _ = sb.Append($"{indent.GetTabs()}}}");

            return sb.ToString();
        }

        private static string SanitizeSpaces(string text, int indent) => text.Replace("\n", $"\n{indent.GetTabs()}");
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class MethodWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

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
            this.Parameters.Add((type, name));
            this.Help.Parameters.Add((name, help));
        }

        public MethodWriter(string methodName, string methodHelp, string returnType = "void", int indentLevel = 0)
        {
            this.Name = methodName;
            this.ReturnType = returnType;
            this.indentLevel = indentLevel;
            this.Help = new HelpWriter(indentLevel)
            {
                Summary = methodHelp
            };
        }

        public override string ToString() => this.ToString(this.indentLevel);

        public override string ToString(int indent)
        {
            _ = this.sb.Clear();

            // Help Stuff First
            _ = this.sb.Append(this.Help.ToString(indent));

            // Main Method and Parameters
            _ = this.sb.Append(indent.GetTabs());
            _ = this.sb.Append($"{this.Accessor.GetTextValue()}{this.Modifier.GetTextValue()}{(!string.IsNullOrEmpty(this.ReturnType) ? $"{this.ReturnType} " : "")}{this.Name}(");
            var isFirst = true;
            foreach (var p in this.Parameters)
            {
                if (!isFirst)
                {
                    _ = this.sb.Append(", ");
                }
                _ = this.sb.Append($"{p.Type} {p.Name}");
                isFirst = false;
            }
            _ = this.sb.Append(')');

            if (this.Modifier is Modifier.Partial or Modifier.Abstract)
            {
                _ = this.sb.Append(';');
                return this.sb.ToString();
            }

            _ = this.sb.AppendLine();
            _ = this.sb.AppendLine($"{indent.GetTabs()}{{");
            indent++;

            // Method Calls
            for (var i = 0; i < this.MethodLines.Count; i++)
            {
                var l = this.MethodLines[i];

                if (l.Contains("}") && !l.Contains("{"))
                {
                    indent--;
                }
                if (!string.IsNullOrEmpty(l))
                {
                    _ = this.sb.Append(indent.GetTabs());
                    var end = "";
                    if (!l.EndsWith(";", System.StringComparison.InvariantCulture) && i < this.MethodLines.Count - 1)
                    {
                        if (!(l.EndsWith("{", System.StringComparison.InvariantCulture) || l.EndsWith("}", System.StringComparison.InvariantCulture)))
                        {
                            if (!(this.MethodLines[i + 1].TrimStart('\t').StartsWith("{", StringComparison.InvariantCulture) || this.MethodLines[i + 1].TrimStart('\t').StartsWith("}", StringComparison.InvariantCulture) || l.EndsWith(",", System.StringComparison.InvariantCulture)))
                            {
                                end = ";";
                            }
                        }
                    }
                    _ = this.sb.AppendLine(SanitizeSpaces($"{l}{end}", indent));
                    if (l.Contains("{") && !l.Contains("}"))
                    {
                        indent++;
                    }
                }
                else
                {
                    _ = this.sb.AppendLine();
                }
            }
            indent--;

            //Close it Out
            _ = this.sb.Append($"{indent.GetTabs()}}}");

            return this.sb.ToString();
        }

        private static string SanitizeSpaces(string text, int indent) => text.Replace("\n", $"\n{indent.GetTabs()}");
    }
}

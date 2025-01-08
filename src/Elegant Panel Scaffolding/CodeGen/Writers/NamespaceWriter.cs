using System;
using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class NamespaceWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

        public List<ClassWriter> Classes { get; } = new List<ClassWriter>();

        private List<string> Usings { get; } = new List<string>();

        public string Name { get; set; }

        public List<string> Headers { get; } = new List<string>();

        public NamespaceWriter(string name) => this.Name = name;

        public void AddUsing(string usingStatement)
        {
            if (!this.Usings.Contains(usingStatement))
            {
                this.Usings.Add(usingStatement);
            }
        }

        public void AddHeader(string headerText)
        {
            if (!this.Headers.Contains(headerText))
            {
                this.Headers.Add(headerText);
            }
        }

        public override string ToString() => this.ToString(0);

        public override string ToString(int indentLevel)
        {
            _ = this.sb.Clear();
            foreach(var h in this.Headers)
            {
                this.sb.AppendLine(h);
            }

            if (this.Headers.Count > 0)
            {
                this.sb.AppendLine();
            }

            this.Usings.Sort();
            foreach (var u in this.Usings)
            {
                _ = this.sb.Append(indentLevel.GetTabs());
                _ = this.sb.AppendLine($"{(u.StartsWith("using", StringComparison.InvariantCulture) ? "" : "using ")}{u}{(u.EndsWith(";", System.StringComparison.InvariantCulture) ? "" : ";")}");
            }

            _ = this.sb.AppendLine();

            _ = this.sb.Append(indentLevel.GetTabs());
            _ = this.sb.AppendLine($"namespace {this.Name}");

            _ = this.sb.Append(indentLevel.GetTabs());
            _ = this.sb.AppendLine("{");
            indentLevel++;

            for (var i = 0; i < this.Classes.Count; i++)
            {
                var c = this.Classes[i];
                _ = this.sb.AppendLine(c.ToString(indentLevel));
                if (i < this.Classes.Count - 1)
                {
                    _ = this.sb.AppendLine();
                }
            }

            _ = this.sb.AppendLine("}");

            return this.sb.ToString();
        }
    }
}

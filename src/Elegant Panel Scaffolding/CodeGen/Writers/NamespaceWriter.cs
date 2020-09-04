using System;
using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class NamespaceWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

        public List<ClassWriter> Classes { get; } = new List<ClassWriter>();

        private List<string> Usings { get; } = new List<string>();

        public string Name { get; set; }

        public NamespaceWriter(string name) => Name = name;

        public void AddUsing(string usingStatement)
        {
            if (!Usings.Contains(usingStatement))
            {
                Usings.Add(usingStatement);
            }
        }

        public override string ToString() => ToString(0);

        public override string ToString(int indentLevel)
        {
            _ = sb.Clear();
            Usings.Sort();
            foreach (var u in Usings)
            {
                _ = sb.Append(indentLevel.GetTabs());
                _ = sb.AppendLine($"{(u.StartsWith("using", StringComparison.InvariantCulture) ? "" : "using ")}{u}{(u.EndsWith(";", System.StringComparison.InvariantCulture) ? "" : ";")}");
            }

            _ = sb.AppendLine();

            _ = sb.Append(indentLevel.GetTabs());
            _ = sb.AppendLine($"namespace {Name}");

            _ = sb.Append(indentLevel.GetTabs());
            _ = sb.AppendLine("{");
            indentLevel++;

            for (var i = 0; i < Classes.Count; i++)
            {
                var c = Classes[i];
                _ = sb.AppendLine(c.ToString(indentLevel));
                if (i < Classes.Count - 1)
                {
                    _ = sb.AppendLine();
                }
            }

            _ = sb.AppendLine("}");

            return sb.ToString();
        }
    }
}

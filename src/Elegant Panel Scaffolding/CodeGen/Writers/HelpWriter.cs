using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class HelpWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

        public string Summary { get; set; } = "";

        public string Remarks { get; set; } = "";

        public string Returns { get; set; } = "";

        public List<(string Name, string Help)> Parameters { get; } = new List<(string Name, string Help)>();

        private readonly int indentLevel;

        public HelpWriter(int indentLevel = 0) => this.indentLevel = indentLevel;

        public override string ToString() => this.ToString(this.indentLevel);

        public override string ToString(int indent)
        {
            var tabs = indent.GetTabs();
            _ = this.sb.Clear();

            if (!string.IsNullOrEmpty(this.Summary))
            {
                _ = this.sb.AppendLine($"{tabs}/// <summary>");
                _ = this.sb.AppendLine($"{tabs}/// {SanitizeSpaces(this.Summary, tabs)}");
                _ = this.sb.AppendLine($"{tabs}/// </summary>");
            }

            foreach (var p in this.Parameters)
            {
                if (p.Help.Contains("\n"))
                {
                    _ = this.sb.AppendLine($"{tabs}/// <param name=\"{p.Name}\">");
                    _ = this.sb.AppendLine($"{tabs}/// {SanitizeSpaces(p.Help, tabs)}");
                    _ = this.sb.AppendLine($"{tabs}/// </param>");
                }
                else
                {
                    _ = this.sb.AppendLine($"{tabs}/// <param name=\"{p.Name}\">{SanitizeSpaces(p.Help, tabs)}</param>");
                }
            }

            if (!string.IsNullOrEmpty(this.Returns))
            {
                if (this.Returns.Contains("\n"))
                {
                    _ = this.sb.AppendLine($"{tabs}/// <returns>");
                    _ = this.sb.AppendLine($"{tabs}/// {SanitizeSpaces(this.Returns, tabs)}");
                    _ = this.sb.AppendLine($"{tabs}/// </returns>");
                }
                else
                {
                    _ = this.sb.AppendLine($"{tabs}/// <returns>{SanitizeSpaces(this.Returns, tabs)}</returns>");
                }
            }

            if (!string.IsNullOrEmpty(this.Remarks))
            {
                if (this.Remarks.Contains("\n"))
                {
                    _ = this.sb.AppendLine($"{tabs}/// <remarks>");
                    _ = this.sb.AppendLine($"{tabs}/// {SanitizeSpaces(this.Remarks, tabs)}");
                    _ = this.sb.AppendLine($"{tabs}/// </remarks>");
                }
                else
                {
                    _ = this.sb.AppendLine($"{tabs}/// <remarks>{SanitizeSpaces(this.Remarks, tabs)}</remarks>");

                }
            }

            return this.sb.ToString();
        }

        private static string SanitizeSpaces(string text, string tabs) => text.Replace("\n", $"\n{tabs}/// ");
    }
}

using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class HelpWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

        public string Summary { get; set; } = "";

        public string Remarks { get; set; } = "";

        public string Returns { get; set; } = "";

        public List<(string Name, string Help)> Parameters { get; } = new List<(string Name, string Help)>();

        private readonly int indentLevel;

        public HelpWriter(int indentLevel = 0) => this.indentLevel = indentLevel;

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indent)
        {
            var tabs = indent.GetTabs();
            _ = sb.Clear();

            if (!string.IsNullOrEmpty(Summary))
            {
                _ = sb.AppendLine($"{tabs}/// <summary>");
                _ = sb.AppendLine($"{tabs}/// {SanitizeSpaces(Summary, tabs)}");
                _ = sb.AppendLine($"{tabs}/// </summary>");
            }

            foreach (var p in Parameters)
            {
                if (p.Help.Contains("\n"))
                {
                    _ = sb.AppendLine($"{tabs}/// <param name=\"{p.Name}\">");
                    _ = sb.AppendLine($"{tabs}/// {SanitizeSpaces(p.Help, tabs)}");
                    _ = sb.AppendLine($"{tabs}/// </param>");
                }
                else
                {
                    _ = sb.AppendLine($"{tabs}/// <param name=\"{p.Name}\">{SanitizeSpaces(p.Help, tabs)}</param>");
                }
            }

            if (!string.IsNullOrEmpty(Returns))
            {
                if (Returns.Contains("\n"))
                {
                    _ = sb.AppendLine($"{tabs}/// <returns>");
                    _ = sb.AppendLine($"{tabs}/// {SanitizeSpaces(Returns, tabs)}");
                    _ = sb.AppendLine($"{tabs}/// </returns>");
                }
                else
                {
                    _ = sb.AppendLine($"{tabs}/// <returns>{SanitizeSpaces(Returns, tabs)}</returns>");
                }
            }

            if (!string.IsNullOrEmpty(Remarks))
            {
                if (Remarks.Contains("\n"))
                {
                    _ = sb.AppendLine($"{tabs}/// <remarks>");
                    _ = sb.AppendLine($"{tabs}/// {SanitizeSpaces(Remarks, tabs)}");
                    _ = sb.AppendLine($"{tabs}/// </remarks>");
                }
                else
                {
                    _ = sb.AppendLine($"{tabs}/// <remarks>{SanitizeSpaces(Remarks, tabs)}</remarks>");

                }
            }

            return sb.ToString();
        }

        private static string SanitizeSpaces(string text, string tabs) => text.Replace("\n", $"\n{tabs} ///");
    }
}

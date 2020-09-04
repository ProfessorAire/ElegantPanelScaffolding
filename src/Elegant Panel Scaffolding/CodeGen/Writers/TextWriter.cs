using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class TextWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

        public HelpWriter Help { get; }

        public List<string> Text { get; } = new List<string>();

        private readonly int indentLevel;

        public TextWriter(string text, int indentLevel = 0)
        {
            Text.Add(text);
            this.indentLevel = indentLevel;
            Help = new HelpWriter(indentLevel);
        }

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indentLevel)
        {
            _ = sb.Clear();
            for (var i = 0; i < Text.Count; i++)
            {
                if (i < Text.Count - 1)
                {
                    _ = sb.AppendLine($"{indentLevel.GetTabs()}{Text[i]}");
                }
                else
                {
                    _ = sb.Append($"{indentLevel.GetTabs()}{Text[i]}");
                }
            }
            return sb.ToString();
        }
    }
}

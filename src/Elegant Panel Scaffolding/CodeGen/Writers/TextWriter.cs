using System.Collections.Generic;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class TextWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

        public HelpWriter Help { get; }

        public List<string> Text { get; } = new List<string>();

        private readonly int indentLevel;

        public TextWriter(int indentLevel = 0)
        {
            this.indentLevel = indentLevel;
            this.Help = new HelpWriter(indentLevel);
        }

        public TextWriter(string text, int indentLevel = 0)
        {
            this.Text.Add(text);
            this.indentLevel = indentLevel;
            this.Help = new HelpWriter(indentLevel);
        }

        public override string ToString() => this.ToString(this.indentLevel);

        public override string ToString(int indentLevel)
        {
            if(this.Text.Count == 0)
            {
                return string.Empty;
            }

            _ = this.sb.Clear();
            for (var i = 0; i < this.Text.Count; i++)
            {
                if (i < this.Text.Count - 1)
                {
                    _ = this.sb.AppendLine($"{indentLevel.GetTabs()}{this.Text[i]}");
                }
                else
                {
                    _ = this.sb.Append($"{indentLevel.GetTabs()}{this.Text[i]}");
                }
            }
            return this.sb.ToString();
        }
    }
}

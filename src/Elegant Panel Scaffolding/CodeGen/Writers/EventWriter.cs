using System.Text;

namespace EPS.CodeGen.Writers
{
    public class EventWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

        public HelpWriter Help { get; }

        public string Name { get; set; }

        public string Handler { get; set; } = "EventHandler";

        public Accessor Accessor { get; set; } = Accessor.Public;

        public Modifier Modifier { get; set; } = Modifier.None;

        private readonly int indentLevel;

        public EventWriter(string name, int indentLevel = 0)
        {
            Name = name;
            this.indentLevel = indentLevel;
            Help = new HelpWriter(indentLevel)
            {
                Summary = "Raised when the associated touchpanel event is received."
            };
        }

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indentLevel)
        {
            _ = sb.Clear();
            _ = sb.Append(Help.ToString(indentLevel));
            _ = sb.Append(indentLevel.GetTabs());
            _ = sb.Append($"{Accessor.GetTextValue()}{Modifier.GetTextValue()}event {Handler} {Name};");
            return sb.ToString();
        }
    }
}

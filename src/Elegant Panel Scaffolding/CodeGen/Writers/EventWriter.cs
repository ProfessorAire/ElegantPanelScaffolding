using System.Text;

namespace EPS.CodeGen.Writers
{
    public class EventWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

        public HelpWriter Help { get; }

        public string Name { get; set; }

        public string Handler { get; set; } = "EventHandler";

        public Accessor Accessor { get; set; } = Accessor.Public;

        public Modifier Modifier { get; set; } = Modifier.None;

        private readonly int indentLevel;

        public EventWriter(string name, int indentLevel = 0)
        {
            this.Name = name;
            this.indentLevel = indentLevel;
            this.Help = new HelpWriter(indentLevel)
            {
                Summary = "Raised when the associated touchpanel event is received."
            };
        }

        public override string ToString() => this.ToString(this.indentLevel);

        public override string ToString(int indentLevel)
        {
            _ = this.sb.Clear();
            _ = this.sb.Append(this.Help.ToString(indentLevel));
            _ = this.sb.Append(indentLevel.GetTabs());
            _ = this.sb.Append($"{this.Accessor.GetTextValue()}{this.Modifier.GetTextValue()}event {this.Handler} {this.Name};");
            return this.sb.ToString();
        }
    }
}

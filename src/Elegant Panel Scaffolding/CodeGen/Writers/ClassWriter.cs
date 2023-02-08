using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class ClassWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

        public List<FieldWriter> Fields { get; } = new List<FieldWriter>();

        public List<PropertyWriter> Properties { get; } = new List<PropertyWriter>();

        public List<MethodWriter> Methods { get; } = new List<MethodWriter>();

        public List<EventWriter> Events { get; } = new List<EventWriter>();

        public List<string> Implements { get; } = new List<string>();

        public HelpWriter Help { get; } = new HelpWriter();

        public string Name { get; set; }

        public Accessor Accessor { get; set; } = Accessor.Public;

        public Modifier Modifier { get; set; } = Modifier.None;

        public bool ImplementINotifyPropertyChanged { get; set; }

        public List<MethodWriter> Constructors { get; } = new List<MethodWriter>();

        private readonly int indentLevel;

        public ClassWriter(string name, int indentLevel = 0)
        {
            this.Name = name;
            this.indentLevel = indentLevel;
        }

        public override string ToString() => this.ToString(this.indentLevel);

        public override string ToString(int indentLevel)
        {
            var indent = indentLevel;

            _ = this.sb.Clear();

            // Help.
            var help = this.Help.ToString(indent);
            if (string.IsNullOrWhiteSpace(help))
            {
                this.Help.Summary = $"Auto-generated {this.Name} class.";
            }

            _ = this.sb.Append(this.Help.ToString(indent));

            // Class Details.
            _ = this.sb.Append(indent.GetTabs());
            _ = this.sb.Append($"{this.Accessor.GetTextValue()}{this.Modifier.GetTextValue()}class {this.Name}");

            var implements = this.Implements;
            if (this.ImplementINotifyPropertyChanged && this.Properties.Count > 0)
            {
                implements.Add("System.ComponentModel.INotifyPropertyChanged");
            }

            for (var i = 0; i < implements.Count; i++)
            {
                if (i == 0)
                {
                    _ = this.sb.Append(" : ");
                }
                else
                {
                    _ = this.sb.Append(", ");
                }
                _ = this.sb.Append(implements[i]);
            }

            _ = this.sb.AppendLine();

            // Open Bracket.
            _ = this.sb.Append(indent.GetTabs());
            _ = this.sb.AppendLine("{");
            indent++;

            var needBreak = false;

            // Fields
            var fields = this.Fields.Union(this.Properties.Where(p => p.BackingFieldWriter != null).Select(p => p.BackingFieldWriter)).ToList();
            for (var i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                if (f != null)
                {
                    needBreak = true;
                    _ = this.sb.AppendLine(f.ToString(indent));
                    if (i < fields.Count - 1)
                    {
                        _ = this.sb.AppendLine();
                    }
                }
            }

            var events = this.Events;

            if (this.ImplementINotifyPropertyChanged && this.Properties.Count > 0)
            {
                var pce = new EventWriter("PropertyChanged")
                {
                    Handler = "System.ComponentModel.PropertyChangedEventHandler"
                };

                events.Add(pce);
            }

            events = events.OrderBy(ew => ew.Name).ToList();

            if (events.Count > 0 && needBreak)
            {
                _ = this.sb.AppendLine();
            }

            // Events
            for (var i = 0; i < events.Count; i++)
            {
                needBreak = true;
                var e = events[i];
                _ = this.sb.AppendLine(e.ToString(indent));
                if (i < events.Count - 1)
                {
                    _ = this.sb.AppendLine();
                }
            }

            if (this.Properties.Count > 0 && needBreak)
            {
                _ = this.sb.AppendLine();
            }
            // Properties
            for (var i = 0; i < this.Properties.Count; i++)
            {
                needBreak = true;
                var p = this.Properties[i];
                _ = this.sb.AppendLine(p.ToString(indent));
                if (i < this.Properties.Count - 1)
                {
                    _ = this.sb.AppendLine();
                }
            }

            if (this.Constructors.Count > 0 && needBreak)
            {
                _ = this.sb.AppendLine();
            }
            // Constructors
            for (var i = 0; i < this.Constructors.Count; i++)
            {
                needBreak = true;
                var c = this.Constructors[i];
                _ = this.sb.AppendLine(c.ToString(indent));
                if (i < this.Constructors.Count - 1)
                {
                    _ = this.sb.AppendLine();
                }
            }

            if (this.Methods.Count > 0 && needBreak)
            {
                _ = this.sb.AppendLine();
            }
            // Methods
            for (var i = 0; i < this.Methods.Count; i++)
            {
                var m = this.Methods[i];
                _ = this.sb.AppendLine(m.ToString(indent));
                if (i < this.Methods.Count - 1)
                {
                    _ = this.sb.AppendLine();
                }
            }

            // Close Bracket
            indent--;
            _ = this.sb.Append(indent.GetTabs());
            _ = this.sb.Append('}');

            return this.sb.ToString();
        }

    }
}

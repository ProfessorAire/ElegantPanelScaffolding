using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class ClassWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

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
            Name = name;
            this.indentLevel = indentLevel;
        }

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indentLevel)
        {
            var indent = indentLevel;

            _ = sb.Clear();

            // Help.
            var help = Help.ToString(indent);
            if (string.IsNullOrWhiteSpace(help))
            {
                Help.Summary = $"Auto-generated {Name} class.";
            }

            _ = sb.Append(Help.ToString(indent));

            // Class Details.
            _ = sb.Append(indent.GetTabs());
            _ = sb.Append($"{Accessor.GetTextValue()}{Modifier.GetTextValue()}class {Name}");

            var implements = Implements;
            if (ImplementINotifyPropertyChanged && Properties.Count > 0)
            {
                implements.Add("System.ComponentModel.INotifyPropertyChanged");
            }

            for (var i = 0; i < implements.Count; i++)
            {
                if (i == 0)
                {
                    _ = sb.Append(" : ");
                }
                else
                {
                    _ = sb.Append(", ");
                }
                _ = sb.Append(implements[i]);
            }

            _ = sb.AppendLine();

            // Open Bracket.
            _ = sb.Append(indent.GetTabs());
            _ = sb.AppendLine("{");
            indent++;

            var needBreak = false;

            // Fields
            var fields = Fields.Union(Properties.Where(p => p.BackingFieldWriter != null).Select(p => p.BackingFieldWriter)).ToList();
            for (var i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                if (f != null)
                {
                    needBreak = true;
                    _ = sb.AppendLine(f.ToString(indent));
                    if (i < fields.Count - 1)
                    {
                        _ = sb.AppendLine();
                    }
                }
            }

            var events = Events;

            if (ImplementINotifyPropertyChanged && Properties.Count > 0)
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
                _ = sb.AppendLine();
            }

            // Events
            for (var i = 0; i < events.Count; i++)
            {
                needBreak = true;
                var e = events[i];
                _ = sb.AppendLine(e.ToString(indent));
                if (i < events.Count - 1)
                {
                    _ = sb.AppendLine();
                }
            }

            if (Properties.Count > 0 && needBreak)
            {
                _ = sb.AppendLine();
            }
            // Properties
            for (var i = 0; i < Properties.Count; i++)
            {
                needBreak = true;
                var p = Properties[i];
                _ = sb.AppendLine(p.ToString(indent));
                if (i < Properties.Count - 1)
                {
                    _ = sb.AppendLine();
                }
            }

            if (Constructors.Count > 0 && needBreak)
            {
                _ = sb.AppendLine();
            }
            // Constructors
            for (var i = 0; i < Constructors.Count; i++)
            {
                needBreak = true;
                var c = Constructors[i];
                _ = sb.AppendLine(c.ToString(indent));
                if (i < Constructors.Count - 1)
                {
                    _ = sb.AppendLine();
                }
            }

            if (Methods.Count > 0 && needBreak)
            {
                _ = sb.AppendLine();
            }
            // Methods
            for (var i = 0; i < Methods.Count; i++)
            {
                var m = Methods[i];
                _ = sb.AppendLine(m.ToString(indent));
                if (i < Methods.Count - 1)
                {
                    _ = sb.AppendLine();
                }
            }

            // Close Bracket
            indent--;
            _ = sb.Append(indent.GetTabs());
            _ = sb.Append('}');

            return sb.ToString();
        }

    }
}

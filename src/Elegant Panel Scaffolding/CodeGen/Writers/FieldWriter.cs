using System.Globalization;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class FieldWriter : WriterBase
    {
        private readonly StringBuilder sb = new StringBuilder();

        public string Name { get; set; } = "";

        public HelpWriter Help { get; set; }

        public string Type { get; set; } = "";

        public string DefaultValue { get; set; } = "";

        private readonly int indentLevel;

        public Modifier Modifier { get; set; } = Modifier.None;

        public Accessor Accessor { get; set; } = Accessor.Public;

        public FieldWriter(string name, string type, int indentLevel = 0)
        {
            Name = name;
            Type = type;
            this.indentLevel = indentLevel;
            Help = new HelpWriter(indentLevel);
        }

        public override string ToString() => ToString(indentLevel);

        public override string ToString(int indent)
        {
            if (string.IsNullOrEmpty(Help.Summary))
            {
#pragma warning disable CA1308 // Normalize strings to uppercase
                if (Name.ToLower(CultureInfo.InvariantCulture)[0] == Name[0])
#pragma warning restore CA1308 // Normalize strings to uppercase
                {
                    if (Accessor == Accessor.Private)
                    {
                        Help.Summary = $"Backing field for the {Name.ToUpperInvariant()[0]}{Name.Substring(1)} property.";
                    }
                    else
                    {
                        Help.Summary = $"{Name} field.";
                    }
                }
                else
                {
                    Help.Summary = $"Provides access to the {Name} object";
                }

            }
            _ = sb.Clear();

            // Help Stuff.
            _ = sb.Append(Help.ToString(indent));

            // Then the Field Name.
            _ = sb.Append(indent.GetTabs());
            _ = sb.Append($"{Accessor.GetTextValue()}{Modifier.GetTextValue()}{(!string.IsNullOrEmpty(Type) ? $"{Type} " : "")}{Name}");

            // Default Value
            if (!string.IsNullOrEmpty(DefaultValue))
            {
                _ = sb.Append($" = {DefaultValue};");
            }
            else
            {
                _ = sb.Append(';');
            }

            return sb.ToString();
        }

    }
}

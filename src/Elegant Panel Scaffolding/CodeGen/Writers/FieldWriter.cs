using System.Globalization;
using System.Text;

namespace EPS.CodeGen.Writers
{
    public class FieldWriter : WriterBase
    {
        private readonly StringBuilder sb = new();

        public string Name { get; set; } = "";

        public HelpWriter Help { get; set; }

        public string Type { get; set; } = "";

        public string DefaultValue { get; set; } = "";

        private readonly int indentLevel;

        public Modifier Modifier { get; set; } = Modifier.None;

        public Accessor Accessor { get; set; } = Accessor.Public;

        public FieldWriter(string name, string type, int indentLevel = 0)
        {
            this.Name = name;
            this.Type = type;
            this.indentLevel = indentLevel;
            this.Help = new HelpWriter(indentLevel);
        }

        public override string ToString() => this.ToString(this.indentLevel);

        public override string ToString(int indentLevel)
        {
            if (string.IsNullOrEmpty(this.Help.Summary))
            {
                if (this.Name.ToLower(CultureInfo.InvariantCulture)[0] == this.Name[0])
                {
                    if (this.Accessor == Accessor.Private)
                    {
                        this.Help.Summary = $"Backing field for the {this.Name.ToUpperInvariant()[0]}{this.Name.Substring(1)} property.";
                    }
                    else
                    {
                        this.Help.Summary = $"{this.Name} field.";
                    }
                }
                else
                {
                    this.Help.Summary = $"Provides access to the {this.Name} object";
                }

            }
            _ = this.sb.Clear();

            // Help Stuff.
            _ = this.sb.Append(this.Help.ToString(indentLevel));

            // Then the Field Name.
            _ = this.sb.Append(indentLevel.GetTabs());
            _ = this.sb.Append($"{this.Accessor.GetTextValue()}{this.Modifier.GetTextValue()}{(!string.IsNullOrEmpty(this.Type) ? $"{this.Type} " : "")}{this.Name}");

            // Default Value
            if (!string.IsNullOrEmpty(this.DefaultValue))
            {
                _ = this.sb.Append($" = {this.DefaultValue};");
            }
            else
            {
                _ = this.sb.Append(';');
            }

            return this.sb.ToString();
        }

    }
}

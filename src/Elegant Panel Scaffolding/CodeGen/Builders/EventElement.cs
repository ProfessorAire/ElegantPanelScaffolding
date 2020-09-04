using System;
using System.Collections.Generic;
using System.Globalization;

namespace EPS.CodeGen.Builders
{
    public class EventElement : ElementBase
    {
        public bool IsButton { get; set; }
        public JoinType ValueType { get; set; } = JoinType.None;
        public ushort Join
        {
            get
            {
                if (ValueType == JoinType.Analog)
                {
                    return (ushort)(join + AnalogOffset);
                }
                else if (ValueType == JoinType.Digital)
                {
                    return (ushort)(join + DigitalOffset);
                }
                else if (ValueType == JoinType.Serial)
                {
                    return (ushort)(join + SerialOffset);
                }
                return join;
            }
            set => join = value;
        }

        public EventElement(string name, ushort join, ushort smartJoin, JoinType valueType, bool isButton = false)
        {
            Name = name;
            Join = join;
            IsButton = isButton;
            ValueType = valueType;
            SmartJoin = smartJoin;
        }

        public EventElement()
        { }

        public string[] GetEventNames()
        {
            if (Join == 0)
            {
                return System.Array.Empty<string>();
            }
            if (IsButton)
            {
                if (Options.Current.CompileButtonReleaseEvents)
                {
                    return new string[] { $"{Name}Pressed", $"{Name}Released" };
                }
                else
                {
                    return new string[] { $"{Name}Pressed" };
                }
            }
            else
            {
                return new string[] { Name };
            }
        }

        public override List<Writers.WriterBase> GetWriters()
        {
            var result = new List<Writers.WriterBase>();

            if (!string.IsNullOrEmpty(ContentOverride))
            {
                var tw = new Writers.TextWriter(ContentOverride, 0);
                tw.Help.Summary = Description;
                result.Add(tw);
                return result;
            }

            var args = "";
            var type = "";

            switch (ValueType)
            {
                case JoinType.Analog:
                case JoinType.SmartAnalog:
                    args = "UShortValueChangedEventArgs";
                    type = "ushort";
                    break;
                case JoinType.Digital:
                case JoinType.SmartDigital:
                    args = "BooleanValueChangedEventArgs";
                    type = "bool";
                    break;
                case JoinType.Serial:
                case JoinType.SmartSerial:
                    args = "StringValueChangedEventArgs";
                    type = "string";
                    break;
                case JoinType.None:
                    args = "BooleanValueChangedEventArgs";
                    type = "bool";
                    break;
            }

            if ((ValueType == JoinType.Digital || ValueType == JoinType.SmartDigital) && IsButton)
            {
                var ewp = new Writers.EventWriter($"{Name}Pressed", 0);
                if (!string.IsNullOrEmpty(Description))
                {
                    ewp.Help.Summary = Description;
                }
                else
                {
                    ewp.Help.Summary = "Raised when the object is Pressed.";
                }
                ewp.Handler = $"EventHandler<{args}>";
                result.Add(ewp);

                var emp = new Writers.MethodWriter($"Raise{Name}Pressed", $"Raises the {Name}Pressed event.", "void", 0)
                {
                    Accessor = Accessor.Private
                };
                emp.MethodLines.Add($"if({Name}Pressed != null)");
                emp.MethodLines.Add("{");
                emp.MethodLines.Add($"{Name}Pressed.Invoke(this, new {args}(true));");
                emp.MethodLines.Add("}");
                result.Add(emp);

                if (Options.Current.CompileButtonReleaseEvents)
                {
                    var ewr = new Writers.EventWriter($"{Name}Released", 0);
                    if (!string.IsNullOrEmpty(Description))
                    {
                        ewr.Help.Summary = Description;
                    }
                    else
                    {
                        ewr.Help.Summary = "Raised when the object is Pressed.";
                    }
                    ewr.Handler = $"EventHandler<{args}>";
                    result.Add(ewr);

                    var emr = new Writers.MethodWriter($"Raise{Name}Released", $"Raises the {Name}Released event.", "void", 0)
                    {
                        Accessor = Accessor.Private
                    };
                    emr.MethodLines.Add($"if({Name}Released != null)");
                    emr.MethodLines.Add("{");
                    emr.MethodLines.Add($"{Name}Released.Invoke(this, new {args}(false));");
                    emr.MethodLines.Add("}");
                    result.Add(emr);
                }
            }
            else
            {
                if(!Name.EndsWith("Changed", StringComparison.InvariantCulture))
                {
                    Name += "Changed";
                }
                var ew = new Writers.EventWriter(Name, 0);
                ew.Help.Summary = !string.IsNullOrEmpty(Description) ? Description : $"Raised when the {Name.Replace("Changed", "")} property changes.";
                ew.Handler = $"EventHandler<{args}>";
                result.Add(ew);

                var pw = new Writers.PropertyWriter(Name.Replace("Changed", ""), type);
#pragma warning disable CA1308 // Normalize strings to uppercase
                pw.Getter.Add($"return {Name.ToLower(CultureInfo.InvariantCulture)[0]}{Name.Replace("Changed", "").Substring(1)};");
#pragma warning restore CA1308 // Normalize strings to uppercase
                pw.HasGetter = true;
                pw.HasSetter = false;
                pw.Help.Summary = "Gets the most recent value for the corresponding touchpanel event.";
                result.Add(pw);

                var em = new Writers.MethodWriter($"Raise{Name}", $"Raises the {Name} event.", "void", 0)
                {
                    Accessor = Accessor.Private
                };
                em.AddParameter($"{type}", "value", "The new value of the property.");

#pragma warning disable CA1308 // Normalize strings to uppercase
                em.MethodLines.Add($"{Name.ToLower(CultureInfo.InvariantCulture)[0]}{Name.Replace("Changed", "").Substring(1)} = value;");
#pragma warning restore CA1308 // Normalize strings to uppercase
                em.MethodLines.Add($"if({Name} != null)");
                em.MethodLines.Add("{");
                em.MethodLines.Add($"{Name}.Invoke(this, new {args}(value));");
                em.MethodLines.Add("}");
                result.Add(em);
            }
            return result;
        }

        public override (string name, ushort join)[] GetData()
        {
            var options = Options.Current;
            if (IsButton)
            {
                if (options.CompileButtonReleaseEvents)
                {
                    return new (string name, ushort join)[]
                    {
                    ($"Raise{Name}Pressed", Join),
                    ($"Raise{Name}Released", Join)
                    };
                }
                else
                {
                    return new (string name, ushort join)[] { ($"Raise{Name}Pressed", Join) };
                }
            }
            else
            {
                return new (string name, ushort join)[]
                {
                    ($"Raise{Name}", Join)
                };
            }
        }

        public (string name, ushort join)[] GetData(Options options, ushort offset)
        {
            if(options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (IsButton)
            {
                if (options.CompileButtonReleaseEvents)
                {
                    return new (string name, ushort join)[]
                    {
                    ($"Raise{Name}Pressed", (ushort)(Join + offset)),
                    ($"Raise{Name}Released", (ushort)(Join + offset))
                    };
                }
                else
                {
                    return new (string name, ushort join)[] { ($"Raise{Name}Pressed", (ushort)(Join + offset)) };
                }
            }
            else
            {
                return new (string name, ushort join)[]
                {
                    ($"Raise{Name}", (ushort)(Join + offset))
                };
            }
        }
    }
}

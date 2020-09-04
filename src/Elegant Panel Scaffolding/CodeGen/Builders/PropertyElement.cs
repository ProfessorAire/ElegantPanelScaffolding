using EPS.CodeGen.Writers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EPS.CodeGen.Builders
{
    public enum PropertyMethod
    {
        ToPanel,
        FromPanel,
        Both,
        Void
    }

    public class PropertyElement : ElementBase
    {
        public JoinType PropertyType { get; set; } = JoinType.Digital;
        public PropertyMethod PropertyMethod { get; set; } = PropertyMethod.ToPanel;
        private readonly bool useLocalChangeEvent;
        public bool IsListElement { get; set; }
        public ushort Join
        {
            get
            {
                if (PropertyType == JoinType.Analog || PropertyType == JoinType.SmartAnalog)
                {
                    return (ushort)(join + AnalogOffset);
                }
                else if (PropertyType == JoinType.Digital || PropertyType == JoinType.SmartDigital)
                {
                    return (ushort)(join + DigitalOffset);
                }
                else if (PropertyType == JoinType.Serial || PropertyType == JoinType.SmartSerial)
                {
                    return (ushort)(join + SerialOffset);
                }
                return join;
            }
            set => join = value;
        }

        public PropertyElement(string name, ushort join, ushort smartJoin, JoinType propertyType, PropertyMethod method = PropertyMethod.ToPanel, bool useLocalChangeEvent = true)
        {
            Name = name;
            Join = join;
            PropertyType = propertyType;
            PropertyMethod = method;
            SmartJoin = smartJoin;
            this.useLocalChangeEvent = useLocalChangeEvent;
        }

        public PropertyElement()
        { }

        public override (string name, ushort join)[] GetData()
        {
            if (PropertyMethod == PropertyMethod.FromPanel || PropertyMethod == PropertyMethod.Both)
            {
                return new (string name, ushort join)[]
                {
                    ($"Raise{Name}Changed", Join)
                };
            }
            return Array.Empty<(string name, ushort join)>();
        }

        public override List<Writers.WriterBase> GetWriters()
        {
            var result = new List<Writers.WriterBase>();

            if (!string.IsNullOrEmpty(ContentOverride))
            {
                var tw = new Writers.TextWriter(ContentOverride);
                tw.Help.Summary = Description;
                result.Add(tw);
                return result;
            }

            var propertyName = $"{Name.ToUpperInvariant()[0]}{Name.Substring(1)}";
#pragma warning disable CA1308 // Normalize strings to uppercase
            var fieldName = $"{Name.ToLower(CultureInfo.InvariantCulture)[0]}{Name.Substring(1)}";
#pragma warning restore CA1308 // Normalize strings to uppercase

            var pw = new Writers.PropertyWriter(propertyName, "bool");
            var fw = new Writers.FieldWriter(fieldName, "bool");

            if (!string.IsNullOrEmpty(Description))
            {
                pw.Help.Summary = Description;
            }
            else
            {
                pw.Help.Summary = "Gets/Sets the value of the property.";
            }

            fw.Help.Summary = $"Underlying field for {propertyName} property.";

            var type = "bool";
            var sigType = "Boolean";
            var offsetText = " + digitalOffset";
            var args = "BooleanValueChangedEventArgs";

            switch (PropertyType)
            {
                case JoinType.Analog:
                case JoinType.SmartAnalog:
                    type = "ushort";
                    sigType = "UShort";
                    offsetText = " + analogOffset";
                    args = "UShortValueChangedEventArgs";
                    break;
                case JoinType.Serial:
                case JoinType.SmartSerial:
                    type = "string";
                    sigType = "String";
                    offsetText = " + serialOffset";
                    args = "StringValueChangedEventArgs";
                    break;
                case JoinType.SrlVisibility:
                    offsetText = " + itemOffset + 2010";
                    break;
                case JoinType.SrlEnable:
                    offsetText = " + itemOffset + 10";
                    break;
            }

            if (!IsListElement)
            {
                offsetText = "";
            }

            pw.Name = propertyName;
            pw.UsePropertyChangeEvent = useLocalChangeEvent;
            pw.Type = type;
            fw.Type = type;

            if (useLocalChangeEvent)
            {
                var ew = new Writers.EventWriter($"{propertyName}Changed")
                {
                    Handler = $"EventHandler<{sigType}ValueChangedEventArgs>"
                };
                ew.Help.Summary = $"Raised when the {propertyName} value changes.";
                result.Add(ew);
            }

            if (PropertyMethod != PropertyMethod.Void)
            {
                if (PropertyMethod == PropertyMethod.ToPanel)
                {
                    pw.Getter.Add($"return {fieldName};");

                    if (fieldName == "value")
                    {
                        fieldName = $"this.{fieldName}";
                    }

                    pw.Setter.Add($"{fieldName} = value;");
                    pw.Setter.Add($"ParentPanel.Send{(SmartJoin > 0 ? "Smart" : "")}Value({(SmartJoin > 0 ? $"{SmartJoin}, " : "")}(ushort)({Join}{offsetText}), value);");

                    var localMw = new Writers.MethodWriter($"Set{propertyName}", "Sets the value of the associated property on a single touchpanel.", "void");
                    localMw.AddParameter($"{type}", "value", "The new value for the property on the panel.");
                    localMw.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated property value on.");
                    localMw.MethodLines.Add($"ParentPanel.Send{(SmartJoin > 0 ? "Smart" : "")}Value({(SmartJoin > 0 ? $"{SmartJoin}, " : "")}(ushort)({Join}{offsetText}), value, panel);");
                    if (useLocalChangeEvent)
                    {
                        localMw.MethodLines.Add($"if ({propertyName}Changed != null)");
                        localMw.MethodLines.Add("{");
                        localMw.MethodLines.Add($"{propertyName}Changed(this, new {sigType}ValueChangedEventArgs(value));");
                        localMw.MethodLines.Add("}");
                    }
                    result.Add(localMw);
                }
                else if (PropertyMethod == PropertyMethod.FromPanel)
                {
                    pw.HasGetter = true;
                    pw.HasSetter = false;
                    pw.Getter.Add($"return {fieldName};");

                    if (fieldName == "value")
                    {
                        fieldName = $"this.{fieldName}";
                    }

                    var em = new MethodWriter($"Raise{Name}Changed", $"Raises the {Name}Changed event.", "void", 0)
                    {
                        Accessor = Accessor.Private
                    };
                    em.AddParameter($"{type}", "value", "The new value of the property.");

#pragma warning disable CA1308 // Normalize strings to uppercase
                    em.MethodLines.Add($"{fieldName} = value;");
#pragma warning restore CA1308 // Normalize strings to uppercase
                    em.MethodLines.Add($"if({Name}Changed != null)");
                    em.MethodLines.Add("{");
                    em.MethodLines.Add($"{Name}Changed.Invoke(this, new {args}(value));");
                    em.MethodLines.Add("}");
                    result.Add(em);

                }
                else if (PropertyMethod == PropertyMethod.Both)
                {
                    pw.Getter.Add($"return {fieldName};");

                    if (fieldName == "value")
                    {
                        fieldName = $"this.{fieldName}";
                    }

                    pw.Setter.Add($"{fieldName} = value;");
                    pw.Setter.Add($"ParentPanel.Send{(SmartJoin > 0 ? "Smart" : "")}Value({(SmartJoin > 0 ? $"{SmartJoin}, " : "")}(ushort)({Join}{offsetText}), value);");

                    var localMw = new Writers.MethodWriter($"Set{propertyName}", "Sets the value of the associated property on a single touchpanel.", "void");
                    localMw.AddParameter($"{type}", "value", "The new value for the property on the panel.");
                    localMw.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated property value on.");
                    localMw.MethodLines.Add($"ParentPanel.Send{(SmartJoin > 0 ? "Smart" : "")}Value({(SmartJoin > 0 ? $"{SmartJoin}, " : "")}(ushort)({Join}{offsetText}), value, panel);");

                    if (useLocalChangeEvent)
                    {
                        localMw.MethodLines.Add($"if ({propertyName}Changed != null)");
                        localMw.MethodLines.Add("{");
                        localMw.MethodLines.Add($"{propertyName}Changed(this, new {sigType}ValueChangedEventArgs(value));");
                        localMw.MethodLines.Add("}");
                    }

                    var em = new MethodWriter($"Raise{Name}Changed", $"Raises the {Name}Changed event.", "void", 0)
                    {
                        Accessor = Accessor.Private
                    };
                    em.AddParameter($"{type}", "value", "The new value of the property.");

#pragma warning disable CA1308 // Normalize strings to uppercase
                    em.MethodLines.Add($"{fieldName} = value;");
#pragma warning restore CA1308 // Normalize strings to uppercase
                    em.MethodLines.Add($"if({Name}Changed != null)");
                    em.MethodLines.Add("{");
                    em.MethodLines.Add($"{Name}Changed.Invoke(this, new {args}(value));");
                    em.MethodLines.Add("}");
                    result.Add(em);

                    result.Add(localMw);
                }
                result.Add(pw);
            }
            else
            {
                var localMw = new Writers.MethodWriter($"{propertyName}", "Sets the value of the associated property on a single touchpanel.", "void");
                localMw.AddParameter($"{type}", "value", "The new value for the property on the panel.");
                localMw.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated property value on.");
                localMw.MethodLines.Add($"ParentPanel.Send{(SmartJoin > 0 ? "Smart" : "")}Value({(SmartJoin > 0 ? $"{SmartJoin}, " : "")}(ushort)({Join}{offsetText}), value);");
                if (useLocalChangeEvent)
                {
                    localMw.MethodLines.Add($"if ({propertyName}Changed != null)");
                    localMw.MethodLines.Add("{");
                    localMw.MethodLines.Add($"{propertyName}Changed(this, new {sigType}ValueChangedEventArgs(value));");
                    localMw.MethodLines.Add("}");
                }
                result.Add(localMw);

                var localMwDiscreet = new Writers.MethodWriter($"Set{propertyName}", "Sets the value of the associated property on a single touchpanel. Any signal changed events won't be fired by this method.", "void");
                localMwDiscreet.AddParameter($"{type}", "value", "The new value for the property on the panel.");
                localMwDiscreet.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated property value on.");
                localMwDiscreet.MethodLines.Add($"ParentPanel.Send{(SmartJoin > 0 ? "Smart" : "")}Value({(SmartJoin > 0 ? $"{SmartJoin}, " : "")}(ushort)({Join}{offsetText}), value, panel);");
                result.Add(localMwDiscreet);
            }

            if (PropertyType == JoinType.Digital)
            {
                var localMw = new Writers.MethodWriter($"Pulse{propertyName}", $"Pulses the {propertyName} digital signal. Any signal changed events won't be fired by this method.", "void");
                localMw.AddParameter("int", "duration", "The duration in milliseconds to pulse the signal for.");
                localMw.MethodLines.Add($"ParentPanel.Pulse({(SmartJoin > 0 ? $"{SmartJoin}, " : "")}(uint)({Join}{offsetText}), duration);");
                result.Add(localMw);
            }
            return result;
        }
    }
}

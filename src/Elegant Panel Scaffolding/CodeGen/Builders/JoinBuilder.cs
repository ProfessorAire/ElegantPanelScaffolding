using EPS.CodeGen.Writers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace EPS.CodeGen.Builders
{
    /// <summary>
    /// Used for all joins, to create writers for code generation.
    /// </summary>
    public class JoinBuilder
    {
        /// <summary>
        /// Gets or sets the number that the join uses.
        /// </summary>
        public uint JoinNumber { get; set; }

        /// <summary>
        /// Gets or sets the number for the SmartObject join #, if the join belongs to a SmartObject.
        /// </summary>
        public uint SmartJoinNumber { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="JoinType"/> associated with the join.
        /// </summary>
        public JoinType JoinType { get; set; } = JoinType.None;

        /// <summary>
        /// Gets or sets the <see cref="JoinDirection"/> the join uses.
        /// </summary>
        public JoinDirection JoinDirection { get; set; } = JoinDirection.Unknown;

        /// <summary>
        /// Gets or sets the <see cref="JoinMethod"/> the join uses. Defaults to <see cref="JoinMethod.Property"/>.
        /// </summary>
        public JoinMethod JoinMethod { get; set; } = JoinMethod.Property;

        /// <summary>
        /// Gets or sets the base name of the join.
        /// </summary>
        public string JoinName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the join's change event. If an empty string this defaults to $"{JoinName}Changed"
        /// </summary>
        public string ChangeEventName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets content to override the builder using. If this is not empty it's the only text that will get output.
        /// </summary>
        public string ContentOverride { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a description to use for help text in certain cases.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the join belongs to a list element that uses offset numbers.
        /// </summary>
        public bool IsListElement { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinBuilder"/> class.
        /// </summary>
        /// <param name="joinNumber">The join's number.</param>
        /// <param name="joinName">The join's name.</param>
        /// <param name="joinType">The join's type.</param>
        /// <param name="joinDirection">The join's direction.</param>
        /// <param name="joinMethod">The join's interaction method.</param>
        public JoinBuilder(uint joinNumber, string joinName, JoinType joinType, JoinDirection joinDirection, JoinMethod joinMethod)
        {
            JoinNumber = joinNumber;
            JoinName = joinName;
            JoinType = joinType;
            JoinDirection = joinDirection;
            JoinMethod = joinMethod;
        }

        public List<TextWriter> GetInitializers()
        {

        }

        /// <summary>
        /// Gets a list of <see cref="WriterBase"/> objects used to construct the data for this object.
        /// </summary>
        /// <returns>A list of <see cref="WriterBase"/> objects.</returns>
        public List<WriterBase> GetWriters()
        {
            if (!string.IsNullOrEmpty(ContentOverride))
            {
                var tw = new TextWriter(ContentOverride);
                tw.Help.Summary = Description;
                return new List<WriterBase>() { tw };
            }

            if (JoinNumber == 0 || JoinType == JoinType.None)
            {
                return new List<WriterBase>(0);
            }

            if (JoinDirection == JoinDirection.ToPanel)
            {
                return GetWritersToPanel();
            }
            else if (JoinDirection == JoinDirection.FromPanel)
            {
                return GetWritersFromPanel();
            }
            else if (JoinDirection == JoinDirection.Both)
            {
                return GetWritersForBoth();
            }
        }

        private List<WriterBase> GetWritersToPanel()
        {
            var result = new List<WriterBase>();

            var sigType = GetJoinTypeString();
            var sigTypeName = GetJoinTypeNameString();
            var offsetText = GetOffsetString();

            var args = $"{sigTypeName}ValueChangedEventArgs";
            var propertyName = FormatPropertyName(JoinName);
            var fieldName = FormatFieldName(JoinName);
            var smartSuffix = SmartJoinNumber > 0 ? "Smart" : string.Empty;
            var smartValue = SmartJoinNumber > 0 ? $"{SmartJoinNumber}, " : string.Empty;

            var changeEventName = GetEventChangeName();

            // First create the EventWriter.
            // This handles change event notifications, which are triggered when the value going to the panel is changed.
            result.Add(GetEventWriter());

            // Next create the property and backing field writers.
            var pw = GetPropertyWriter();
            var fw = GetFieldWriter();

            pw.Getter.Add($"return {fieldName};");

            if(fieldName == "value")
            {
                fieldName = $"this.{fieldName}";
            }

            pw.Setter.Add($"{fieldName} = value;");
            pw.Setter.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value);");

            result.Add(pw);
            result.Add(fw);

            var methodSetter = new MethodWriter($"Set{propertyName}", $"Sets the value of the <see cref=\"{propertyName}\"/> join on a single touchpanel.");
            methodSetter.AddParameter($"{sigType}", "value", "The new value for the join on the touchpanel.");
            methodSetter.AddParameter("BasicTriListWithSmartObject", "panel", "The panel to change the associated join value on.");
            methodSetter.MethodLines.Add($"ParentPanel.Send{smartSuffix}Value({smartValue}(ushort)({JoinNumber}{offsetText}), value, panel);");
            methodSetter.MethodLines.Add($"if ({changeEventName} != null)");
            methodSetter.MethodLines.Add("{");
            methodSetter.MethodLines.Add($"{changeEventName}(this, new {args}(value));");
            methodSetter.MethodLines.Add("}");

            if (JoinType == JoinType.Digital && 
                (JoinDirection == JoinDirection.ToPanel || JoinDirection == JoinDirection.Both))
            {
                var pulseMw = new MethodWriter($"Pulse{propertyName}", $"Pulses the {propertyName} digital signal. Any local signal changed events won't be fired by this method.");
                pulseMw.AddParameter("int", "duration", "The duration in milliseconds to pulse the signal for.");
                pulseMw.MethodLines.Add($"ParentPanel.Pulse({smartSuffix}(uint)({JoinNumber}{offsetText}), duration);");
                result.Add(pulseMw);
            }

            return result;
        }

        private List<WriterBase> GetWritersFromPanel()
        {
            var result = new List<WriterBase>();

            var sigType = GetJoinTypeString();
            var sigTypeName = GetJoinTypeNameString();

            var args = $"{sigTypeName}ValueChangedEventArgs";
            var propertyName = FormatPropertyName(JoinName);
            var fieldName = FormatFieldName(JoinName);
            var smartSuffix = SmartJoinNumber > 0 ? "Smart" : string.Empty;
            var smartValue = SmartJoinNumber > 0 ? $"{SmartJoinNumber}, " : string.Empty;

            var changeEventName = GetEventChangeName();

            // First create the EventWriter.
            // This handles change event notifications, which are triggered when the value going to the panel is changed.
            result.Add(GetEventWriter());

            // Next create the property and backing field writers.
            var pw = GetPropertyWriter();
            var fw = GetFieldWriter();

            pw.Getter.Add($"return {fieldName};");
            pw.HasSetter = false;
            result.Add(pw);
            result.Add(fw);

            if (fieldName == "value")
            {
                fieldName = $"this.{fieldName}";
            }

            var raiseMethod = new MethodWriter($"Raise{changeEventName}", $"Raises the {changeEventName} event.")
            {
                Accessor = Accessor.Private
            };

            raiseMethod.AddParameter($"{sigType}", "value", "The new value of the property.");

            raiseMethod.MethodLines.Add($"{fieldName} = value;");
            raiseMethod.MethodLines.Add($"if ({changeEventName} != null)");
            raiseMethod.MethodLines.Add("{");
            raiseMethod.MethodLines.Add($"{changeEventName}(this, new {args}(value));");
            raiseMethod.MethodLines.Add("}");

            result.Add(raiseMethod);

            return result;
        }

        private List<WriterBase> GetWritersForBoth()
        {
            var result = GetWritersToPanel();

            var fieldName = FormatFieldName(JoinName);
            var changeEventName = GetEventChangeName();
            var sigType = GetJoinTypeString();
            var args = $"{GetJoinTypeNameString()}ValueChangedEventArgs";

            if (fieldName == "value")
            {
                fieldName = $"this.{fieldName}";
            }

            var raiseMethod = new MethodWriter($"Raise{changeEventName}", $"Raises the {changeEventName} event.")
            {
                Accessor = Accessor.Private
            };

            raiseMethod.AddParameter($"{sigType}", "value", "The new value of the property.");

            raiseMethod.MethodLines.Add($"{fieldName} = value;");
            raiseMethod.MethodLines.Add($"if ({changeEventName} != null)");
            raiseMethod.MethodLines.Add("{");
            raiseMethod.MethodLines.Add($"{changeEventName}(this, new {args}(value));");
            raiseMethod.MethodLines.Add("}");

            result.Add(raiseMethod);

            return result;
        }

        /// <summary>
        /// Gets a property writer correctly prepared for use with this join.
        /// </summary>
        /// <returns>A <see cref="PropertyWriter"/> object.</returns>
        private PropertyWriter GetPropertyWriter()
        {
            var propertyName = FormatPropertyName(JoinName);

            var propertyWriter = new PropertyWriter(
                propertyName, GetJoinTypeString());

            if (!string.IsNullOrEmpty(Description))
            {
                propertyWriter.Help.Summary = Description;
            }
            else
            {
                if (propertyWriter.Type == "bool")
                {
                    propertyWriter.Help.Summary = $"Gets or sets a value indicating whether the <see cref=\"{propertyName}\"/> join was last set to true or false.";
                }
                else
                {
                    propertyWriter.Help.Summary = $"Gets or sets a value indicating what the <see cref=\"{propertyName}\"/> join was last set to.";
                }
            }

            return propertyWriter;
        }

        /// <summary>
        /// Gets an event writer prepared for use with this join.
        /// </summary>
        /// <returns>A <see cref="FieldWriter"/> object.</returns>
        private EventWriter GetEventWriter()
        {
            var propName = FormatPropertyName(JoinName);

            var eventWriter = new EventWriter(GetEventChangeName())
            {
                Handler = $"EventHandler<{GetJoinTypeNameString()}ValueChangedEventArgs>"
            };

            eventWriter.Help.Summary = $"Raised when the {propName} value changes.";

            return eventWriter;
        }

        /// <summary>
        /// Gets a field writer prepared for use with this join.
        /// </summary>
        /// <returns>A <see cref="FieldWriter"/> object.</returns>
        private FieldWriter GetFieldWriter()
        {
            var fieldWriter = new FieldWriter(FormatFieldName(JoinName), GetJoinTypeString());
            fieldWriter.Help.Summary = $"Backing field for the <see cref=\"{FormatPropertyName(JoinName)}\"/> property.";

            return fieldWriter;
        }

        /// <summary>
        /// Gets the shorthand version of the signal class type.
        /// </summary>
        /// <returns>A string with the value "bool", "ushort", or "string".</returns>
        private string GetJoinTypeString()
        {
            return JoinType == JoinType.Analog || JoinType == JoinType.SmartAnalog ? "ushort" :
                JoinType == JoinType.Serial || JoinType == JoinType.SmartSerial ? "string" :
                "bool";
        }

        /// <summary>
        /// Gets the full name of a signal class type.
        /// </summary>
        /// <returns>A string with the value "Boolean", "UShort", or "String".</returns>
        private string GetJoinTypeNameString()
        {
            return JoinType == JoinType.Analog || JoinType == JoinType.SmartAnalog ? "UShort" :
                JoinType == JoinType.Serial || JoinType == JoinType.SmartSerial ? "String" :
                "Boolean";
        }

        /// <summary>
        /// Gets the offset string to use for the join's numeric value in offset calculations.
        /// </summary>
        /// <returns>A string representing the text to include in offset calculations.</returns>
        private string GetOffsetString()
        {
            if(!IsListElement)
            {
                return string.Empty;
            }

            switch (JoinType)
            {
                case JoinType.Analog:
                case JoinType.SmartAnalog:
                    return " + analogOffset";
                case JoinType.Digital:
                case JoinType.DigitalButton:
                case JoinType.SmartDigital:
                case JoinType.SmartDigitalButton:
                    return " + digitalOffset";
                case JoinType.Serial:
                case JoinType.SmartSerial:
                    return " + serialOffset";
                case JoinType.SrlVisibility:
                    return " + itemOffset + 2010";
                case JoinType.SrlEnable:
                    return " + itemOffset + 10";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets a string with the correct change event name.
        /// </summary>
        /// <returns>A string to use as the property change event name.</returns>
        private string GetEventChangeName() =>
            string.IsNullOrEmpty(ChangeEventName) ? $"{FormatPropertyName(JoinName)}Changed" : ChangeEventName;

        /// <summary>
        /// Formats a string with an uppercase first letter.
        /// </summary>
        /// <param name="name">The name to normalize as a property name.</param>
        /// <returns>A string valid to use as a field name.</returns>
        private static string FormatPropertyName(string name)
        {
            return SanitizeString($"{name.ToUpperInvariant()[0]}{name.Substring(1)}");
        }

        /// <summary>
        /// Formats a string with a lowercase first letter.
        /// </summary>
        /// <param name="name">The name to normalize as a fieldName.</param>
        /// <returns>A string valid to use as a field name.</returns>
        private static string FormatFieldName(string name)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            return SanitizeString($"{name.ToLower(CultureInfo.InvariantCulture)[0]}{name.Substring(1)}");
#pragma warning restore CA1308 // Normalize strings to uppercase
        }

        /// <summary>
        /// Sanitizes a string to ensure it's a valid value.
        /// </summary>
        /// <param name="value">The string to sanitize.</param>
        /// <returns>A sanitized string.</returns>
        private static string SanitizeString(string value)
        {
            return value
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("*", "Star")
                .Replace("#", "Pound")
                .Replace("!", "ExMark")
                .Replace("@", "AtSign")
                .Replace("$", "Dollar")
                .Replace("%", "Percent")
                .Replace("^", "Carat")
                .Replace("&", "Ampersand")
                .Replace(".", "Dot")
                .Replace("-", "Minus")
                .Replace("=", "Equals")
                .Replace("+", "Plus");
        }
    }
}
